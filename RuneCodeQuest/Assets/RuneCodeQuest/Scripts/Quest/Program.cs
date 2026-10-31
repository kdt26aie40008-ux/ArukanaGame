using System.Collections.Generic;
using System.Text;

namespace RuneCodeQuest
{
    /// <summary>ゴーレムに出せる命令</summary>
    public enum CommandType { Forward, TurnLeft, TurnRight, Magic, Pick, Use }

    /// <summary>「もし」「〜までくり返す」で調べられること</summary>
    public enum ConditionType { IsObstacle, IsBlocked, AtGoal, RightOpen, LeftOpen, OnItem }

    /// <summary>プログラムの部品の種類</summary>
    public enum NodeKind
    {
        Command, // 命令1つ
        Repeat,  // ○回くり返す（Python: for i in range(○):）
        If,      // もし〜なら（でなければ）（Python: if 〜: / else:）
        While,   // 〜までくり返す（Python: while 〜:）
        Assign,  // 変数に数を入れる（Python: n = 3 / n = read_sign() / n = n - 1）
        Define,  // 関数を作る（Python: def turn_corner():）。中身は呼んだときに動く
        Call,    // 作った関数を使う（Python: turn_corner()）
    }

    // ------------------------------------------------------------------
    // 変数と計算
    // ------------------------------------------------------------------

    public enum TermKind { Number, Variable, Read }

    /// <summary>式の1つぶん：数（3）・変数（n）・看板を読む（read_sign()）</summary>
    public class Term
    {
        public TermKind Kind;
        public int Number;
        public string Name;

        public static Term Num(int v) => new Term { Kind = TermKind.Number, Number = v };
        public static Term Var(string name) => new Term { Kind = TermKind.Variable, Name = name };
        public static Term Read() => new Term { Kind = TermKind.Read };

        public override string ToString() =>
            Kind == TermKind.Number ? Number.ToString() : Kind == TermKind.Variable ? Name : "read_sign()";
    }

    /// <summary>式：「a」または「a + b」「a - b」（このゲームで使える計算は たし算・ひき算 だけ）</summary>
    public class Expr
    {
        public Term A;
        public char Op;   // '\0' なら計算なし、'+' か '-'
        public Term B;

        public static Expr Of(Term a) => new Expr { A = a };
        public static Expr Num(int v) => Of(Term.Num(v));
        public static Expr Calc(Term a, char op, Term b) => new Expr { A = a, Op = op, B = b };

        public bool IsRead => Op == '\0' && A.Kind == TermKind.Read;

        public override string ToString() => Op == '\0' ? A.ToString() : $"{A} {Op} {B}";
    }

    /// <summary>くらべる条件：「n > 0」「n == 0」など</summary>
    public class Compare
    {
        public Expr Left;
        public string Op;   // > < == != >= <=
        public Expr Right;

        public static readonly string[] Ops = { ">", "<", "==", "!=", ">=", "<=" };

        public override string ToString() => $"{Left} {Op} {Right}";

        /// <summary>ブロックに書く形（== は = と書く）</summary>
        public string Label => $"{Left} {(Op == "==" ? "=" : Op == "!=" ? "≠" : Op)} {Right}";
    }

    /// <summary>
    /// プログラムの部品1つ。
    /// スクラッチのブロックも Python のコードも、読み取るとこの形になります。
    /// だから「同じクエスト条件」をどちらの解き方でも同じように判定できます。
    /// </summary>
    public class Node
    {
        public NodeKind Kind;
        public CommandType Command;
        public ConditionType Condition;
        public Compare Cmp;           // null でなければ「くらべる条件」（Condition は使わない）
        public bool Negate;           // true なら「〜でない」（Python の not）
        public int Count = 3;         // くり返す回数（CountExpr があればそちらを使う）
        public Expr CountExpr;        // くり返す回数を変数で決めるとき（for i in range(n):）
        public string Var;            // Assign：入れる先の変数の名前
        public Expr Value;            // Assign：入れる数
        public bool HasElse;          // もし〜なら／でなければ
        public List<Node> Body = new List<Node>();
        public List<Node> Else = new List<Node>();
        public int Line;              // Python の何行目か（スクラッチでは 0）

        public bool IsContainer => Kind == NodeKind.Repeat || Kind == NodeKind.If || Kind == NodeKind.While || Kind == NodeKind.Define;

        public static Node Cmd(CommandType c) => new Node { Kind = NodeKind.Command, Command = c };

        public static Node Repeat(int count, params Node[] body)
        {
            var n = new Node { Kind = NodeKind.Repeat, Count = count };
            n.Body.AddRange(body);
            return n;
        }

        /// <summary>回数を式（変数など）で決めるくり返し</summary>
        public static Node RepeatBy(Expr count, params Node[] body)
        {
            var n = new Node { Kind = NodeKind.Repeat, CountExpr = count };
            n.Body.AddRange(body);
            return n;
        }

        public static Node If(ConditionType cond, bool negate, Node[] body, Node[] elseBody = null)
        {
            var n = new Node { Kind = NodeKind.If, Condition = cond, Negate = negate, HasElse = elseBody != null };
            n.Body.AddRange(body);
            if (elseBody != null) n.Else.AddRange(elseBody);
            return n;
        }

        public static Node While(ConditionType cond, bool negate, params Node[] body)
        {
            var n = new Node { Kind = NodeKind.While, Condition = cond, Negate = negate };
            n.Body.AddRange(body);
            return n;
        }

        /// <summary>くらべる条件のくり返し（while n > 0:）</summary>
        public static Node WhileCmp(Compare cmp, params Node[] body)
        {
            var n = new Node { Kind = NodeKind.While, Cmp = cmp };
            n.Body.AddRange(body);
            return n;
        }

        public static Node Assign(string name, Expr value) => new Node { Kind = NodeKind.Assign, Var = name, Value = value };

        /// <summary>関数を作る（def name():）。Var が関数の名前</summary>
        public static Node Define(string name, params Node[] body)
        {
            var n = new Node { Kind = NodeKind.Define, Var = name };
            n.Body.AddRange(body);
            return n;
        }

        /// <summary>作った関数を使う（name()）</summary>
        public static Node Call(string name) => new Node { Kind = NodeKind.Call, Var = name };

        public Node Clone()
        {
            var n = (Node)MemberwiseClone();
            n.Body = CloneList(Body);
            n.Else = CloneList(Else);
            return n;
        }

        public static List<Node> CloneList(List<Node> list)
        {
            var r = new List<Node>();
            foreach (var n in list) r.Add(n.Clone());
            return r;
        }
    }

    /// <summary>命令1つぶんの説明（ブロックの文字と Python の名前）</summary>
    public class CommandInfo
    {
        public CommandType Type;
        public string BlockLabel;
        public string PythonName;
        public string PythonArg = "";  // () の中に書くもの（left(90) の 90）。なければ空
        public string Description;

        public string PythonCode => $"{PythonName}({PythonArg})";

        public static readonly CommandInfo[] All =
        {
            new CommandInfo { Type = CommandType.Forward, BlockLabel = "すすむ", PythonName = "forward", Description = "前に1マスすすむ" },
            new CommandInfo { Type = CommandType.TurnLeft, BlockLabel = "左を向く", PythonName = "left", PythonArg = "90", Description = "その場で左に90度まわる（タートルと同じ）" },
            new CommandInfo { Type = CommandType.TurnRight, BlockLabel = "右を向く", PythonName = "right", PythonArg = "90", Description = "その場で右に90度まわる（タートルと同じ）" },
            new CommandInfo { Type = CommandType.Magic, BlockLabel = "まほう", PythonName = "use_magic", Description = "目の前のじゃまもの（穴・岩・いばら・氷・結界）を消す" },
            new CommandInfo { Type = CommandType.Pick, BlockLabel = "ひろう", PythonName = "pick_up", Description = "足元のアイテムをひろう" },
            new CommandInfo { Type = CommandType.Use, BlockLabel = "つかう", PythonName = "use_item", Description = "ゴールにあるもの（灯台・扉・宝箱など）を使う" },
        };

        public static CommandInfo Get(CommandType t)
        {
            foreach (var i in All) if (i.Type == t) return i;
            return All[0];
        }

        public static CommandInfo FindByPythonName(string name)
        {
            foreach (var i in All) if (i.PythonName == name) return i;
            return null;
        }
    }

    /// <summary>調べることの説明</summary>
    public class ConditionInfo
    {
        public ConditionType Type;
        public string Label;     // 前にじゃまもの
        public string LabelNot;  // 前にじゃまものがない
        public string PythonName;
        public string Description;

        public string PythonCode => PythonName + "()";

        public static readonly ConditionInfo[] All =
        {
            new ConditionInfo { Type = ConditionType.IsObstacle, Label = "前にじゃまもの", LabelNot = "前にじゃまものがない", PythonName = "obstacle_in_front", Description = "目の前にじゃまものがあれば はい" },
            new ConditionInfo { Type = ConditionType.IsBlocked, Label = "前がかべ", LabelNot = "前がかべでない", PythonName = "wall_in_front", Description = "目の前が川やかべで進めなければ はい" },
            new ConditionInfo { Type = ConditionType.AtGoal, Label = "ゴールに着いた", LabelNot = "ゴールに着いていない", PythonName = "at_goal", Description = "ゴールのマスにいれば はい" },
            new ConditionInfo { Type = ConditionType.RightOpen, Label = "右が通れる", LabelNot = "右が通れない", PythonName = "right_is_clear", Description = "右どなりのマスが道なら はい" },
            new ConditionInfo { Type = ConditionType.LeftOpen, Label = "左が通れる", LabelNot = "左が通れない", PythonName = "left_is_clear", Description = "左どなりのマスが道なら はい" },
            new ConditionInfo { Type = ConditionType.OnItem, Label = "足元にアイテム", LabelNot = "足元にアイテムがない", PythonName = "object_here", Description = "足元にアイテムがあれば はい" },
        };

        public static ConditionInfo Get(ConditionType t)
        {
            foreach (var i in All) if (i.Type == t) return i;
            return All[0];
        }

        public static ConditionInfo FindByPythonName(string name)
        {
            foreach (var i in All) if (i.PythonName == name) return i;
            return null;
        }

        public static string LabelOf(ConditionType t, bool negate) => negate ? Get(t).LabelNot : Get(t).Label;
    }

    /// <summary>プログラムを数えたり、文字にしたりする道具</summary>
    public static class ProgramText
    {
        /// <summary>ブロック（命令）の数。くり返しや「もし」、変数のブロックも1個と数える</summary>
        public static int Count(List<Node> list)
        {
            int c = 0;
            foreach (var n in list) c += 1 + Count(n.Body) + Count(n.Else);
            return c;
        }

        /// <summary>ブロックの種類の名前（しばりで数えるときに使う）</summary>
        public static string KindName(Node n)
        {
            switch (n.Kind)
            {
                case NodeKind.Repeat: return "くり返す";
                case NodeKind.If: return "もし";
                case NodeKind.While: return "〜までくり返す";
                case NodeKind.Assign: return "変数に入れる";
                case NodeKind.Define: return "関数を作る";
                case NodeKind.Call: return "関数を使う";
                default: return CommandInfo.Get(n.Command).BlockLabel;
            }
        }

        public static string KindName(NodeKind kind) => KindName(new Node { Kind = kind });

        /// <summary>ブロックの種類ごとに、プログラムに置いてある数（入れ子の中もふくむ）</summary>
        public static Dictionary<string, int> CountByKind(List<Node> list)
        {
            var counts = new Dictionary<string, int>();
            void Walk(List<Node> nodes)
            {
                foreach (var n in nodes)
                {
                    string k = KindName(n);
                    counts[k] = counts.TryGetValue(k, out int c) ? c + 1 : 1;
                    Walk(n.Body);
                    Walk(n.Else);
                }
            }
            Walk(list);
            return counts;
        }

        public static bool Uses(List<Node> list, NodeKind kind)
        {
            foreach (var n in list)
                if (n.Kind == kind || Uses(n.Body, kind) || Uses(n.Else, kind)) return true;
            return false;
        }

        /// <summary>Python のコードにする（字下げはスペース4つ）</summary>
        public static string ToPython(List<Node> list)
        {
            var sb = new StringBuilder();
            WritePython(sb, list, 0);
            return sb.ToString();
        }

        private static void WritePython(StringBuilder sb, List<Node> list, int depth)
        {
            string ind = new string(' ', depth * 4);
            if (list.Count == 0)
            {
                sb.Append(ind).Append("pass\n");
                return;
            }
            foreach (var n in list)
            {
                switch (n.Kind)
                {
                    case NodeKind.Command:
                        sb.Append(ind).Append(CommandInfo.Get(n.Command).PythonCode).Append('\n');
                        break;
                    case NodeKind.Assign:
                        sb.Append(ind).Append(n.Var).Append(" = ").Append(n.Value).Append('\n');
                        break;
                    case NodeKind.Repeat:
                        sb.Append(ind).Append("for i in range(").Append(CountText(n)).Append("):\n");
                        WritePython(sb, n.Body, depth + 1);
                        break;
                    case NodeKind.If:
                        sb.Append(ind).Append("if ").Append(Cond(n)).Append(":\n");
                        WritePython(sb, n.Body, depth + 1);
                        if (n.HasElse)
                        {
                            sb.Append(ind).Append("else:\n");
                            WritePython(sb, n.Else, depth + 1);
                        }
                        break;
                    case NodeKind.While:
                        sb.Append(ind).Append("while ").Append(Cond(n)).Append(":\n");
                        WritePython(sb, n.Body, depth + 1);
                        break;
                    case NodeKind.Define:
                        sb.Append(ind).Append("def ").Append(n.Var).Append("():\n");
                        WritePython(sb, n.Body, depth + 1);
                        sb.Append('\n'); // 関数のあとは1行あける（Python の書き方のきまり）
                        break;
                    case NodeKind.Call:
                        sb.Append(ind).Append(n.Var).Append("()\n");
                        break;
                }
            }
        }

        public static string CountText(Node n) => n.CountExpr != null ? n.CountExpr.ToString() : n.Count.ToString();

        private static string Cond(Node n)
        {
            string c = n.Cmp != null ? n.Cmp.ToString() : ConditionInfo.Get(n.Condition).PythonCode;
            return (n.Negate ? "not " : "") + c;
        }

        /// <summary>ブロックの文字の形（解説で使う）。1行ずつのリストで返す</summary>
        public static List<string> ToBlockLines(List<Node> list)
        {
            var lines = new List<string>();
            WriteBlocks(lines, list, 0);
            return lines;
        }

        private static void WriteBlocks(List<string> lines, List<Node> list, int depth)
        {
            string ind = new string('　', depth * 2);
            foreach (var n in list)
            {
                lines.Add(ind + BlockHeader(n));
                if (n.IsContainer)
                {
                    WriteBlocks(lines, n.Body, depth + 1);
                    if (n.Kind == NodeKind.If && n.HasElse)
                    {
                        lines.Add(ind + "でなければ");
                        WriteBlocks(lines, n.Else, depth + 1);
                    }
                }
            }
        }

        /// <summary>ブロックに書かれる文字</summary>
        public static string BlockHeader(Node n)
        {
            switch (n.Kind)
            {
                case NodeKind.Repeat: return $"{CountText(n)} 回くり返す";
                case NodeKind.If: return $"もし 〈{CondLabel(n)}〉 なら";
                case NodeKind.While: return WhileLabel(n);
                case NodeKind.Assign: return AssignLabel(n);
                case NodeKind.Define: return $"関数 {n.Var} を作る";
                case NodeKind.Call: return $"{n.Var} をする";
                default: return CommandInfo.Get(n.Command).BlockLabel;
            }
        }

        /// <summary>条件の文字（「前にじゃまもの」「n > 0」など）</summary>
        public static string CondLabel(Node n)
        {
            if (n.Cmp != null) return (n.Negate ? "ではない " : "") + n.Cmp.Label;
            return ConditionInfo.LabelOf(n.Condition, n.Negate);
        }

        /// <summary>
        /// 「〜までくり返す」の文字。Python の while は「〜のあいだ」くり返すので、
        /// while not ゴールに着いた → 「ゴールに着くまでくり返す」と表示する
        /// </summary>
        public static string WhileLabel(Node n)
        {
            if (n.Cmp != null) return n.Negate ? $"〈{n.Cmp.Label}〉 までくり返す" : $"〈{n.Cmp.Label}〉 のあいだくり返す";
            return n.Negate ? $"〈{ConditionInfo.Get(n.Condition).Label}〉 までくり返す" : $"〈{ConditionInfo.Get(n.Condition).Label}〉 あいだくり返す";
        }

        /// <summary>変数のブロックの文字</summary>
        public static string AssignLabel(Node n)
        {
            var v = n.Value;
            if (v.IsRead) return $"{n.Var} を 看板の数 にする";
            if (v.Op != '\0' && v.A.Kind == TermKind.Variable && v.A.Name == n.Var && v.B.Kind == TermKind.Number)
                return $"{n.Var} を {(v.Op == '+' ? v.B.Number : -v.B.Number)} ずつ変える";
            return $"{n.Var} を {v} にする";
        }
    }
}
