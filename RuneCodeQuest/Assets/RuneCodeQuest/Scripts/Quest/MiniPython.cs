using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuneCodeQuest
{
    /// <summary>Python として読めなかった行の情報（＝エラー）</summary>
    public class CodeError
    {
        public int Line;        // 何行目か（1から）
        public string Code;     // その行の中身
        public string Message;  // 何がいけないか（初心者向け）
        public string Fix;      // どう直せばよいか
    }

    public class ParseResult
    {
        public List<Node> Program = new List<Node>();
        public readonly List<CodeError> Errors = new List<CodeError>();
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// 「ミニPython」の読み取り係。
    ///
    /// 【安全のための設計】
    /// 本物の Python を実行するのではなく、許可した書き方だけを読み取って
    /// ゲーム内のプログラム（Node）に変換します。PC のファイル操作やネット接続などは
    /// そもそも書けないので、任意のコードが実行されることはありません。
    ///
    /// 読める書き方（Python の文法と同じ）
    ///   forward()                       命令（CommandInfo.All にあるもの）
    ///   for i in range(3):              くり返し（字下げした行が中身）
    ///   if obstacle_in_front(): / elif ...: / else:
    ///   while not at_goal():
    ///   n = read_sign() / n -= 1        変数（第5章から）
    ///   def turn_corner(): / turn_corner()  関数を作る・使う（第6章から。() の中は空、使う行より上で作る）
    ///   pass                            何もしない
    ///   # メモ                          コメント
    /// </summary>
    public static class MiniPython
    {
        public const int MaxStatements = 60;
        public const int MaxRepeat = 50;

        private static readonly HashSet<string> NotAllowedWords = new HashSet<string>
        {
            "class", "import", "from", "return", "print", "input", "open", "exec", "eval",
            "lambda", "with", "try", "except", "global", "del", "break", "continue", "yield", "raise",
        };

        private static readonly Regex CallPattern = new Regex(@"^([A-Za-z_][A-Za-z0-9_\.]*)\s*\((.*)\)$");
        private static readonly Regex NamePattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_\.]*$");
        private static readonly Regex FirstWord = new Regex(@"^[A-Za-z_]+");
        private static readonly Regex ForPattern = new Regex(@"^for\s+([A-Za-z_][A-Za-z0-9_]*)\s+in\s+range\s*\((.*)\)$");
        private static readonly Regex DefPattern = new Regex(@"^def\s+([A-Za-z_][A-Za-z0-9_]*)\s*\((.*)\)$");

        // 関数（def）を読むときに使う：作った関数・プログラムのどこかで作る関数・今読んでいる def・いちばん外側の字下げ
        private static readonly HashSet<string> definedFuncs = new HashSet<string>();
        private static readonly HashSet<string> allFuncs = new HashSet<string>();
        private static string currentDef;
        private static int topIndent;

        private class LineInfo
        {
            public int No;
            public int Indent;
            public string Text;
            public string Raw;
        }

        public static ParseResult Parse(string source)
        {
            var result = new ParseResult();
            var lines = new List<LineInfo>();
            string[] raw = (source ?? "").Replace("\r", "").Split('\n');

            // 1. 行ごとに、字下げの深さと中身を調べる
            for (int i = 0; i < raw.Length; i++)
            {
                string code = StripComment(raw[i]).Replace("\t", "    ");
                if (code.Trim().Length == 0) continue;

                int indent = 0;
                while (indent < code.Length && code[indent] == ' ') indent++;
                if (indent < code.Length && code[indent] == '　')
                {
                    AddError(result, i + 1, raw[i], "行の先頭に全角スペースがあります。",
                        "字下げは半角スペースで書きます（Tab キーを押すとスペース4つが入ります）。");
                    continue;
                }
                lines.Add(new LineInfo { No = i + 1, Indent = indent, Text = code.Trim(), Raw = raw[i].Trim() });
            }

            // 2. 字下げをもとに、中身をまとめて読む
            int idx = 0;
            if (lines.Count > 0 && lines[0].Indent > 0)
            {
                AddError(result, lines[0].No, lines[0].Raw, "1行目から字下げ（行の先頭の空白）があります。",
                    "いちばん外側の命令は、左はしにそろえて書きます。");
            }
            topIndent = lines.Count > 0 ? lines[0].Indent : 0;
            definedFuncs.Clear();
            allFuncs.Clear();
            currentDef = null;
            foreach (var li in lines)
            {
                var dm = DefPattern.Match(li.Text.TrimEnd(':').Trim());
                if (dm.Success) allFuncs.Add(dm.Groups[1].Value);
            }
            result.Program = ParseBlock(lines, ref idx, topIndent, result);

            int count = ProgramText.Count(result.Program);
            if (count > MaxStatements)
            {
                result.Errors.Add(new CodeError
                {
                    Line = 0, Code = "", Message = $"命令が多すぎます（{count}個）。",
                    Fix = $"{MaxStatements} 個までにしてください。くり返しを使うと短くできます。",
                });
            }
            return result;
        }

        private static List<Node> ParseBlock(List<LineInfo> lines, ref int idx, int indent, ParseResult result)
        {
            var list = new List<Node>();
            Node lastIf = null; // else / elif をつなげられる「もし」

            while (idx < lines.Count)
            {
                var li = lines[idx];
                if (li.Indent < indent) break;
                if (li.Indent > indent)
                {
                    AddError(result, li.No, li.Raw, "字下げ（行の先頭の空白）が多すぎます。",
                        "上の行とそろえましょう。字下げするのは「:」で終わる行の次の行だけです。");
                    idx++;
                    continue;
                }

                string text = li.Text;
                var err = CheckCharacters(text);
                if (err != null)
                {
                    AddError(result, li.No, li.Raw, err.Message, err.Fix);
                    idx++;
                    SkipDeeper(lines, ref idx, indent);
                    continue;
                }

                if (text.EndsWith(":"))
                {
                    string header = text.Substring(0, text.Length - 1).Trim();
                    idx++;
                    List<Node> body = null;
                    // def の中身を読んでいるあいだは、その関数の名前をおぼえておく（自分を呼んでいないか調べる）
                    var defMatch = DefPattern.Match(header);
                    string outerDef = currentDef;
                    if (defMatch.Success) currentDef = defMatch.Groups[1].Value;
                    if (idx < lines.Count && lines[idx].Indent > indent)
                    {
                        body = ParseBlock(lines, ref idx, lines[idx].Indent, result);
                    }
                    else
                    {
                        AddError(result, li.No, li.Raw, "「:」の次の行に、字下げした中身がありません。",
                            "「:」で終わる行の次の行は、スペース4つ分 右にずらして中身を書きます。");
                        body = new List<Node>();
                    }

                    currentDef = outerDef;
                    var node = ParseHeader(header, li, result, ref lastIf, body);
                    if (node != null) list.Add(node);
                    continue;
                }

                // 「:」がないのに for / if / while などで始まっている
                var fw = FirstWord.Match(text);
                if (fw.Success && (fw.Value == "for" || fw.Value == "if" || fw.Value == "elif" || fw.Value == "else" || fw.Value == "while" || fw.Value == "def"))
                {
                    AddError(result, li.No, li.Raw, $"「{fw.Value}」の行の最後に「:」（コロン）がありません。",
                        "for / if / else / while / def の行は、最後に「:」をつけます。");
                    idx++;
                    SkipDeeper(lines, ref idx, indent);
                    continue;
                }

                lastIf = null;
                idx++;
                if (text == "pass") continue;
                if (IsAssignment(text))
                {
                    var assign = ParseAssignment(text, out var aerr);
                    if (aerr != null) AddError(result, li.No, li.Raw, aerr.Message, aerr.Fix);
                    else
                    {
                        assign.Line = li.No;
                        list.Add(assign);
                    }
                    continue;
                }
                var cmd = ParseCommand(text, out var cmdErr);
                if (cmdErr != null) AddError(result, li.No, li.Raw, cmdErr.Message, cmdErr.Fix);
                else
                {
                    cmd.Line = li.No;
                    list.Add(cmd);
                }
            }
            return list;
        }

        private static void SkipDeeper(List<LineInfo> lines, ref int idx, int indent)
        {
            while (idx < lines.Count && lines[idx].Indent > indent) idx++;
        }

        /// <summary>「:」で終わる行（for / if / elif / else / while / def）を読む</summary>
        private static Node ParseHeader(string header, LineInfo li, ParseResult result, ref Node lastIf, List<Node> body)
        {
            // def turn_corner():  関数を作る
            if (header == "def" || header.StartsWith("def "))
            {
                lastIf = null;
                var dm = DefPattern.Match(header);
                if (!dm.Success)
                {
                    AddError(result, li.No, li.Raw, "def の書き方がちがいます。", "関数は  def turn_corner():  のように、名前の後ろに () と : をつけて書きます。");
                    return null;
                }
                string fname = dm.Groups[1].Value;
                if (li.Indent != topIndent)
                {
                    AddError(result, li.No, li.Raw, "def は、いちばん外側（字下げなし）に書きます。", "for や if の中ではなく、左はしにそろえて def を書こう。");
                    return null;
                }
                if (dm.Groups[2].Value.Trim().Length > 0)
                {
                    AddError(result, li.No, li.Raw, "このゲームの関数は、() の中に何も書きません。", $"def {fname}():  のように書きます。");
                    return null;
                }
                var nerr = CheckFuncName(fname);
                if (nerr != null)
                {
                    AddError(result, li.No, li.Raw, nerr.Message, nerr.Fix);
                    return null;
                }
                if (definedFuncs.Contains(fname))
                {
                    AddError(result, li.No, li.Raw, $"関数 {fname} は、もう上で作っています。", "同じ名前の関数は1つだけにしよう。");
                    return null;
                }
                definedFuncs.Add(fname);
                var def = new Node { Kind = NodeKind.Define, Var = fname, Line = li.No };
                def.Body = body;
                return def;
            }

            // for i in range(3):
            if (header.StartsWith("for"))
            {
                lastIf = null;
                var m = ForPattern.Match(header);
                if (!m.Success)
                {
                    AddError(result, li.No, li.Raw, "for の書き方がちがいます。", "くり返しは  for i in range(3):  のように書きます。");
                    return null;
                }
                string arg = m.Groups[2].Value.Trim();
                if (!int.TryParse(arg, out int n))
                {
                    // range(n) や range(n + 1) のように、変数を使った回数
                    var countExpr = ParseExpr(arg, out var xerr);
                    if (xerr != null)
                    {
                        AddError(result, li.No, li.Raw, $"range() の中の「{arg}」が読めません。" + xerr.Message, xerr.Fix);
                        return null;
                    }
                    var vnode = new Node { Kind = NodeKind.Repeat, CountExpr = countExpr, Line = li.No };
                    vnode.Body = body;
                    return vnode;
                }
                if (n < 1 || n > MaxRepeat)
                {
                    AddError(result, li.No, li.Raw, $"くり返す回数 {n} は使えません。", $"1 から {MaxRepeat} までの数字にしてください。");
                    return null;
                }
                var node = new Node { Kind = NodeKind.Repeat, Count = n, Line = li.No };
                node.Body = body;
                return node;
            }

            if (header == "else")
            {
                if (lastIf == null)
                {
                    AddError(result, li.No, li.Raw, "else の前に if がありません。", "else は if の中身のすぐ後に、if と同じ字下げで書きます。");
                    return null;
                }
                lastIf.HasElse = true;
                lastIf.Else = body;
                lastIf = null;
                return null;
            }

            bool isIf = header.StartsWith("if ") || header.StartsWith("if(");
            bool isElif = header.StartsWith("elif ") || header.StartsWith("elif(");
            bool isWhile = header.StartsWith("while ") || header.StartsWith("while(");

            if (isIf || isElif || isWhile)
            {
                string condText = header.Substring(isIf ? 2 : isElif ? 4 : 5).Trim();
                if (!ParseCondition(condText, out var cond, out bool negate, out var cmp, out var cerr))
                {
                    AddError(result, li.No, li.Raw, cerr.Message, cerr.Fix);
                    if (isIf) lastIf = null;
                    return null;
                }

                if (isWhile)
                {
                    lastIf = null;
                    var w = new Node { Kind = NodeKind.While, Condition = cond, Negate = negate, Cmp = cmp, Line = li.No };
                    w.Body = body;
                    return w;
                }

                var ifNode = new Node { Kind = NodeKind.If, Condition = cond, Negate = negate, Cmp = cmp, Line = li.No };
                ifNode.Body = body;
                if (isElif)
                {
                    if (lastIf == null)
                    {
                        AddError(result, li.No, li.Raw, "elif の前に if がありません。", "elif は if の中身のすぐ後に書きます。");
                        return null;
                    }
                    lastIf.HasElse = true;
                    lastIf.Else = new List<Node> { ifNode };
                    lastIf = ifNode;
                    return null;
                }
                lastIf = ifNode;
                return ifNode;
            }

            lastIf = null;
            var fw = FirstWord.Match(header);
            if (fw.Success && NotAllowedWords.Contains(fw.Value))
            {
                AddError(result, li.No, li.Raw, $"「{fw.Value}」は、このゲームでは使えません。", "使えるのは for / if / elif / else / while / def と、ゴーレムの命令だけです。");
                return null;
            }
            AddError(result, li.No, li.Raw, "この行は読めませんでした。", "「:」で終わる行は for / if / elif / else / while / def のどれかで始めます。");
            return null;
        }

        /// <summary>条件（obstacle_in_front() / not at_goal() など）を読む</summary>
        private static bool ParseCondition(string text, out ConditionType cond, out bool negate, out Compare cmp, out CodeError err)
        {
            cond = ConditionType.IsObstacle;
            negate = false;
            cmp = null;
            err = null;
            text = text.Trim();
            if (text.StartsWith("(") && text.EndsWith(")") && !text.StartsWith("()"))
            {
                string inner = text.Substring(1, text.Length - 2).Trim();
                if (inner.Length > 0 && inner.IndexOf('(') >= 0) text = inner;
            }
            if (text.StartsWith("not ") || text.StartsWith("not("))
            {
                negate = true;
                text = text.Substring(3).Trim();
            }
            if (text == "True" || text == "False" || text == "true" || text == "false")
            {
                err = Err($"「{text}」は、このゲームでは使えません。", "ゴーレムが調べられること（obstacle_in_front() など）を書きます。");
                return false;
            }
            if (text.Contains(" and ") || text.Contains(" or "))
            {
                err = Err("and / or は、このゲームでは使えません。", "条件は obstacle_in_front() や n > 0 のように1つだけ書きます。");
                return false;
            }
            // くらべる条件（n > 0 など）
            var cm = CompareRegex.Match(text);
            if (cm.Success)
            {
                var left = ParseExpr(cm.Groups[1].Value, out var lerr);
                var right = ParseExpr(cm.Groups[3].Value, out var rerr);
                if (lerr != null || rerr != null)
                {
                    var e = lerr ?? rerr;
                    err = Err("くらべる式が読めません。" + e.Message, e.Fix);
                    return false;
                }
                cmp = new Compare { Left = left, Op = cm.Groups[2].Value, Right = right };
                return true;
            }
            if (Regex.IsMatch(text, @"[^=!<>]=[^=]"))
            {
                err = Err("くらべるときは「=」ではなく「==」を使います。", "例: if n == 0:   （= は変数に数を入れるときに使う）");
                return false;
            }

            var m = CallPattern.Match(text);
            if (!m.Success)
            {
                if (NamePattern.IsMatch(text))
                {
                    err = Err($"「{text}」の後ろに () がありません。", $"調べるときも {text}() のように () をつけます。");
                    return false;
                }
                err = Err("条件が読めませんでした。", "例:  if obstacle_in_front():   while not at_goal():");
                return false;
            }
            string name = m.Groups[1].Value;
            var info = ConditionInfo.FindByPythonName(name);
            if (info == null && OldNames.TryGetValue(name, out var renamed))
            {
                err = Err($"「{name}()」は「{renamed}」に名前が変わりました。", $"例:  if {renamed}:");
                return false;
            }
            if (info == null)
            {
                var lower = ConditionInfo.FindByPythonName(name.ToLowerInvariant());
                if (lower != null)
                {
                    err = Err($"「{name}」は大文字・小文字がちがいます。", $"正しくは {lower.PythonCode} です。");
                    return false;
                }
                if (CommandInfo.FindByPythonName(name) != null)
                {
                    err = Err($"{name}() は「動く命令」なので、条件には使えません。", "条件には obstacle_in_front() / wall_in_front() / at_goal() などを使います。");
                    return false;
                }
                string near = NearestCondition(name);
                err = Err($"「{name}」という調べる命令はありません。",
                    near != null ? $"もしかして {near} ですか？" : "使えるのは obstacle_in_front() / wall_in_front() / at_goal() / right_is_clear() / left_is_clear() / object_here() です。");
                return false;
            }
            if (m.Groups[2].Value.Trim().Length > 0)
            {
                err = Err("() の中には何も書きません。", $"{info.PythonCode} のように書きます。");
                return false;
            }
            cond = info.Type;
            return true;
        }

        /// <summary>ふつうの命令（forward() など）を読む</summary>
        // ------------------------------------------------------------------
        // 変数と計算
        // ------------------------------------------------------------------

        private static readonly Regex AssignRegex = new Regex(@"^([A-Za-z_][A-Za-z0-9_]*)\s*(\+=|-=|=)\s*(.*)$");
        private static readonly Regex CompareRegex = new Regex(@"^(.+?)\s*(==|!=|>=|<=|>|<)\s*(.+)$");
        private static readonly Regex VarNamePattern = new Regex(@"^[a-z_][a-z0-9_]*$");
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "for", "in", "range", "if", "elif", "else", "while", "not", "and", "or", "pass", "True", "False", "None", "read_sign", "def",
        };

        private static bool IsAssignment(string text)
        {
            var m = AssignRegex.Match(text);
            // n = 3 は代入、n == 3 はくらべる式なので代入ではない
            return m.Success && !m.Groups[3].Value.TrimStart().StartsWith("=");
        }

        /// <summary>n = 3 / n = read_sign() / n = n - 1 / n += 1 / n -= 1</summary>
        private static Node ParseAssignment(string text, out CodeError err)
        {
            err = null;
            var m = AssignRegex.Match(text);
            string name = m.Groups[1].Value, op = m.Groups[2].Value, rhs = m.Groups[3].Value.Trim();
            err = CheckVarName(name);
            if (err != null) return null;
            if (rhs.Length == 0)
            {
                err = Err("「=」の右がからっぽです。", "n = 3 のように、入れる数を書きます。");
                return null;
            }
            var value = ParseExpr(rhs, out var xerr);
            if (xerr != null)
            {
                err = xerr;
                return null;
            }
            if (op != "=")
            {
                // n += 2 は n = n + 2 と同じ
                if (value.Op != '\0')
                {
                    err = Err($"「{op}」の右には、数を1つだけ書きます。", $"例: {name} {op} 1");
                    return null;
                }
                value = Expr.Calc(Term.Var(name), op[0], value.A);
            }
            return Node.Assign(name, value);
        }

        /// <summary>関数の名前：小文字の英字と _（turn_corner など）。命令や Python の言葉とかぶらないこと</summary>
        private static CodeError CheckFuncName(string name)
        {
            if (CommandInfo.FindByPythonName(name) != null || ConditionInfo.FindByPythonName(name) != null)
                return Err($"「{name}」はゴーレムの命令の名前なので、関数の名前には使えません。", "turn_corner や step_forward のような、べつの名前にしよう。");
            if (Keywords.Contains(name) || NotAllowedWords.Contains(name) || name == "def")
                return Err($"「{name}」は Python のとくべつな言葉なので、関数の名前には使えません。", "turn_corner のような名前にしよう。");
            if (!VarNamePattern.IsMatch(name))
                return Err($"関数の名前「{name}」は、小文字の英字と _ で書こう。", "例: turn_corner");
            return null;
        }

        private static CodeError CheckVarName(string name)
        {
            if (CommandInfo.FindByPythonName(name) != null || ConditionInfo.FindByPythonName(name) != null)
                return Err($"「{name}」は命令の名前なので、変数の名前には使えません。", "n や count のような名前にしよう。");
            if (allFuncs.Contains(name))
                return Err($"「{name}」は関数の名前なので、変数の名前には使えません。", "n や count のような名前にしよう。");
            if (Keywords.Contains(name) || NotAllowedWords.Contains(name))
                return Err($"「{name}」は Python のとくべつな言葉なので、変数の名前には使えません。", "n や count のような名前にしよう。");
            if (!VarNamePattern.IsMatch(name))
                return Err($"変数の名前「{name}」は、小文字の英字で書こう。", "例: n や count");
            return null;
        }

        /// <summary>式：「数」「変数」「read_sign()」を1つ、または「+」「-」で2つつないだもの</summary>
        private static Expr ParseExpr(string text, out CodeError err)
        {
            err = null;
            text = text.Trim();
            if (text.Contains("*") || text.Contains("/") || text.Contains("%"))
            {
                err = Err("かけ算・わり算は、このゲームでは使えません。", "たし算（+）とひき算（-）だけ使えます。");
                return null;
            }
            // 先頭のマイナス（-1）は数の一部として読む
            int opIndex = -1;
            for (int i = 1; i < text.Length; i++)
                if (text[i] == '+' || text[i] == '-')
                {
                    if (opIndex >= 0)
                    {
                        err = Err("計算は1回まで（a + b か a - b）です。", "例: n = n - 1");
                        return null;
                    }
                    opIndex = i;
                }
            if (opIndex < 0)
            {
                var t = ParseTerm(text, out err);
                return t == null ? null : Expr.Of(t);
            }
            var a = ParseTerm(text.Substring(0, opIndex), out err);
            if (a == null) return null;
            var b = ParseTerm(text.Substring(opIndex + 1), out err);
            if (b == null) return null;
            return Expr.Calc(a, text[opIndex], b);
        }

        private static Term ParseTerm(string text, out CodeError err)
        {
            err = null;
            text = text.Trim();
            if (text.Length == 0)
            {
                err = Err("数か変数が足りません。", "例: n - 1");
                return null;
            }
            if (int.TryParse(text, out int v))
            {
                if (v > 1000 || v < -1000)
                {
                    err = Err($"数 {v} は大きすぎます。", "-1000 から 1000 までの数にしよう。");
                    return null;
                }
                return Term.Num(v);
            }
            if (Regex.IsMatch(text, @"^read_sign\s*\(\s*\)$")) return Term.Read();
            if (text == "read_sign")
            {
                err = Err("「read_sign」の後ろに () がありません。", "看板を読むときは read_sign() と書きます。");
                return null;
            }
            if (Regex.IsMatch(text, @"^read\s*\(\s*\)$"))
            {
                err = Err("「read()」は「read_sign()」に名前が変わりました。", "看板の数を読むときは n = read_sign() と書きます。");
                return null;
            }
            if (text.Contains("("))
            {
                err = Err($"「{text}」は数を返さない命令です。", "数を返すのは read_sign()（看板を読む）だけです。");
                return null;
            }
            err = CheckVarName(text);
            return err == null ? Term.Var(text) : null;
        }

        private static Node ParseCommand(string text, out CodeError err)
        {
            err = null;
            var first = FirstWord.Match(text);
            if (first.Success && NotAllowedWords.Contains(first.Value))
            {
                err = Err($"「{first.Value}」は、このゲームでは使えません。", "使えるのはゴーレムの命令と for / if / while / def だけです。");
                return null;
            }
            if (text.EndsWith(";")) text = text.TrimEnd(';').TrimEnd();

            var m = CallPattern.Match(text);
            if (!m.Success)
            {
                if (NamePattern.IsMatch(text))
                {
                    err = Err($"「{text}」の後ろに () がありません。", $"命令を実行するときは {text}() のように、最後に () をつけます。");
                    return null;
                }
                if (text.Contains("(") && !text.Contains(")"))
                {
                    err = Err("閉じカッコ「)」が足りません。", "「(」と「)」はセットで書きます。例: forward()");
                    return null;
                }
                if (text.Contains("="))
                {
                    err = Err("この行は読めませんでした。", "変数に数を入れるときは  n = 3  や  n = read_sign()  のように書きます。");
                    return null;
                }
                err = Err("この行は命令として読めませんでした。", "例: forward() のように「命令の名前()」の形で書きます。");
                return null;
            }

            string name = m.Groups[1].Value;
            string args = m.Groups[2].Value.Trim();
            if (name.Contains("."))
            {
                string last = name.Substring(name.LastIndexOf('.') + 1);
                err = Err($"「{name}」のような「.」つきの書き方は、使いません。", $"命令の名前だけを書きます。例: {last}()");
                return null;
            }

            var info = CommandInfo.FindByPythonName(name);
            if (info == null && allFuncs.Contains(name))
            {
                // def で作った関数を使う
                if (name == currentDef)
                {
                    err = Err($"関数 {name} の中で、{name}() 自身を呼んでいます。", "このゲームでは、関数の中で自分を呼ぶことはできません（終わらなくなるため）。");
                    return null;
                }
                if (!definedFuncs.Contains(name))
                {
                    err = Err($"関数 {name} は、まだ作られていません。",
                        $"Python は上から順に読むので、{name}() を使う行より上に  def {name}():  を書きます。");
                    return null;
                }
                if (args.Length > 0)
                {
                    err = Err($"() の中に「{args}」が入っています。", $"このゲームの関数は {name}() のように、() の中に何も書きません。");
                    return null;
                }
                return Node.Call(name);
            }
            if (info == null && OldNames.TryGetValue(name, out var renamed))
            {
                err = Err($"「{name}()」は「{renamed}」に名前が変わりました。", $"本物の Python（タートルなど）と同じ書き方で {renamed} と書きます。");
                return null;
            }
            if (info == null)
            {
                var lower = CommandInfo.FindByPythonName(name.ToLowerInvariant());
                if (lower != null)
                {
                    err = Err($"「{name}」は大文字・小文字がちがいます。", $"Python は大文字と小文字を区別します。正しくは {lower.PythonCode} です。");
                    return null;
                }
                if (ConditionInfo.FindByPythonName(name) != null)
                {
                    err = Err($"{name}() は「調べるだけ」の命令です。", $"if {name}():  や  while not {name}():  のように使います。");
                    return null;
                }
                string near = NearestCommand(name);
                err = Err($"「{name}」という命令はありません。",
                    near != null ? $"もしかして {near} ですか？ つづりを確かめてみましょう。" : "使える命令は右下の一覧を見てください。");
                return null;
            }
            if (info.PythonArg.Length > 0)
            {
                // left(90) / right(90)：タートルと同じく、まわる角度を書く。ゴーレムは90度ずつ
                if (args.Length == 0)
                {
                    err = Err($"{name}() の () の中に、まわる角度がありません。", $"タートルと同じように {info.PythonCode} と書きます。");
                    return null;
                }
                if (!int.TryParse(args, out int deg))
                {
                    err = Err($"() の中の「{args}」は角度の数ではありません。", $"{info.PythonCode} のように数を書きます。");
                    return null;
                }
                if (deg != 90)
                {
                    err = Err($"ゴーレムは90度ずつしか まわれません（{deg}度は むり）。",
                        deg == 180 ? $"180度まわるときは {info.PythonCode} を2回書きます。" : $"{info.PythonCode} と書きます。");
                    return null;
                }
                return Node.Cmd(info.Type);
            }
            if (args.Length > 0)
            {
                err = Err($"() の中に「{args}」が入っています。",
                    $"{info.PythonCode} の () の中には何も書きません。何回もしたいときは for を使います。");
                return null;
            }
            return Node.Cmd(info.Type);
        }

        /// <summary>全角文字など、よくある入力ミス</summary>
        private static CodeError CheckCharacters(string text)
        {
            if (text.Contains("（") || text.Contains("）"))
                return Err("カッコが全角「（）」になっています。", "半角の「()」で書きます。日本語入力をオフにしてから入力しましょう。");
            if (text.Contains("："))
                return Err("コロンが全角「：」になっています。", "半角の「:」で書きます。");
            foreach (char c in text)
                if (c > 0x7F)
                    return Err("全角の文字や日本語が混ざっています。", "命令は半角英字で書きます。日本語のメモは # の後ろに書きます。");
            return null;
        }

        private static string StripComment(string line)
        {
            int idx = line.IndexOf('#');
            return idx >= 0 ? line.Substring(0, idx) : line;
        }

        private static CodeError Err(string message, string fix) => new CodeError { Message = message, Fix = fix };

        private static void AddError(ParseResult r, int line, string code, string message, string fix) =>
            r.Errors.Add(new CodeError { Line = line, Code = code, Message = message, Fix = fix });

        /// <summary>前の名前 → 今の名前（前のバージョンで覚えた人に教える）</summary>
        private static readonly Dictionary<string, string> OldNames = new Dictionary<string, string>
        {
            { "turn_left", "left(90)" }, { "turn_right", "right(90)" }, { "magic", "use_magic()" }, { "pick", "pick_up()" }, { "use", "use_item()" },
            { "is_obstacle", "obstacle_in_front()" }, { "is_blocked", "wall_in_front()" }, { "right_is_open", "right_is_clear()" },
            { "left_is_open", "left_is_clear()" }, { "on_item", "object_here()" }, { "read", "read_sign()" },
        };

        private static string NearestCommand(string name)
        {
            string best = null;
            int bestD = 3;
            foreach (var i in CommandInfo.All)
            {
                int d = Levenshtein(name.ToLowerInvariant(), i.PythonName);
                if (d < bestD) { bestD = d; best = i.PythonCode; }
            }
            return best;
        }

        private static string NearestCondition(string name)
        {
            string best = null;
            int bestD = 3;
            foreach (var i in ConditionInfo.All)
            {
                int d = Levenshtein(name.ToLowerInvariant(), i.PythonName);
                if (d < bestD) { bestD = d; best = i.PythonCode; }
            }
            return best;
        }

        private static int Levenshtein(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            return d[a.Length, b.Length];
        }
    }
}
