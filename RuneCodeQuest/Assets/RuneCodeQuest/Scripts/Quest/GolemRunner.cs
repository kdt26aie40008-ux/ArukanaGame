using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>クエスト条件を満たせなかった理由（エラーとは別もの）</summary>
    public enum FailKind
    {
        None,
        EmptyProgram,    // 命令が1つもない
        TooManyBlocks,   // ブロック（命令）の数が上限をこえた
        BlockedByObstacle, // じゃまものの前で止まった
        HitWall,         // 川やかべの方へ進もうとした
        UseNotAtGoal,    // ゴールに着く前に つかう を使った
        ItemsLeft,       // アイテムをひろいきっていない
        NotAtGoal,       // 命令が終わったのに、ゴールに着いていない
        NotUsed,         // ゴールには着いたが、つかう を使っていない
        EndlessLoop,     // くり返しが終わらない
        VariableProblem, // 変数に数が入っていない・くり返す回数がおかしい
    }

    /// <summary>実行の1コマ（アニメーション用）</summary>
    public class RunStep
    {
        public int Test;          // 何番目のテストか（0から）
        public Node Node;         // 実行した部品（最初の状態では null）
        public GridMap Map;       // そのときのマス（消したじゃまもの・ひろったアイテムも反映）
        public P Pos;
        public int Dir;
        public bool Done;         // ゴールのものを使った
        public string Log;
        public bool IsFailure;
    }

    public class RunResult
    {
        public readonly List<RunStep> Steps = new List<RunStep>();
        public bool Success;
        public FailKind Fail = FailKind.None;
        public string FailMessage;
        public Node FailedNode;
        public int FailedTest = -1;
        public int TestCount;
    }

    /// <summary>
    /// ゴーレムを動かす係（プログラムの実行）。
    /// 命令を上から順番に実行し、くり返し・もし・〜までくり返す も処理します。
    /// 終わらないくり返しで固まらないように、動ける回数に上限があります。
    /// </summary>
    public static class GolemRunner
    {
        public const int MaxActions = 600;
        public const int MaxLoopChecks = 5000;

        private class Stop : Exception
        {
            public FailKind Kind;
            public Node Node;
            public Stop(FailKind k, Node n, string msg) : base(msg) { Kind = k; Node = n; }
        }

        private class Ctx
        {
            public GridMap Map;
            public P Pos;
            public int Dir;
            public bool Done;
            public int Actions;
            public int Checks;
            public int Test;
            public RunResult R;
            public QuestTheme Theme;
            public readonly Dictionary<string, int> Vars = new Dictionary<string, int>();
            public readonly Dictionary<string, Node> Funcs = new Dictionary<string, Node>(); // def で作った関数
            public int CallDepth;
        }

        public static RunResult Run(Level level, List<Node> program)
        {
            var r = new RunResult { TestCount = level.Tests.Count };
            var theme = level.Theme ?? QuestTheme.Of(level.Number);
            // 「つかう」をまだ習っていないステージは、ゴールに着けばクリア
            bool needsUse = level.Commands.Contains(CommandType.Use);

            int count = ProgramText.Count(program);
            if (count == 0)
            {
                Snapshot(NewCtx(level.Tests[0], 0, r, theme), null, "ゴーレムは スタートで 命令を待っている。", false);
                return Fail(r, FailKind.EmptyProgram, null, 0,
                    "命令が1つもありません。ゴーレムは何をすればいいか分からず、じっとしています。");
            }
            if (level.MaxBlocks > 0 && count > level.MaxBlocks)
            {
                Snapshot(NewCtx(level.Tests[0], 0, r, theme), null, "ゴーレムは スタートで 命令を待っている。", false);
                return Fail(r, FailKind.TooManyBlocks, null, 0,
                    $"命令（ブロック）が {count} 個あります。このステージでは {level.MaxBlocks} 個までです。");
            }

            for (int t = 0; t < level.Tests.Count; t++)
            {
                var c = NewCtx(level.Tests[t], t, r, theme);
                string head = level.Tests.Count > 1 ? $"テスト{t + 1}: " : "";
                Snapshot(c, null, head + "スタート！", false);
                try
                {
                    Exec(c, program);
                    if (c.Pos != c.Map.Goal)
                        throw new Stop(FailKind.NotAtGoal, null, "すべての命令が終わりましたが、ゴーレムはまだゴールに着いていません。");
                    if (c.Map.CountItems() > 0)
                        throw new Stop(FailKind.ItemsLeft, null, $"{theme.ItemName}が まだ {c.Map.CountItems()} こ 残っています。");
                    if (needsUse && !c.Done)
                        throw new Stop(FailKind.NotUsed, null, $"ゴールには着きましたが、{theme.GoalName}を「つかう」をしていません。");
                }
                catch (Stop s)
                {
                    if (s.Kind != FailKind.NotAtGoal && s.Kind != FailKind.NotUsed && !(s.Kind == FailKind.ItemsLeft && s.Node == null))
                        Snapshot(c, s.Node, head + s.Message, true);
                    string where = level.Tests.Count > 1 ? $"テスト{t + 1}で: " : "";
                    return Fail(r, s.Kind, s.Node, t, where + s.Message);
                }
                if (!needsUse) c.Done = true;
                if (level.Tests.Count > 1) Snapshot(c, null, $"テスト{t + 1}: 成功！", false);
                else if (!needsUse) Snapshot(c, null, "ゴールにたどり着いた！", false);
            }
            r.Success = true;
            return r;
        }

        private static Ctx NewCtx(GridMap test, int t, RunResult r, QuestTheme theme) =>
            new Ctx { Map = test.Clone(), Pos = test.Start, Dir = test.StartDir, Test = t, R = r, Theme = theme };

        private static void Exec(Ctx c, List<Node> list)
        {
            foreach (var n in list)
            {
                switch (n.Kind)
                {
                    case NodeKind.Command:
                        Act(c, n);
                        break;
                    case NodeKind.Assign:
                        int value = Value(c, n.Value, n);
                        c.Vars[n.Var] = value;
                        Snapshot(c, n, (c.R.TestCount > 1 ? $"テスト{c.Test + 1}: " : "") + $"変数 {n.Var} は {value} になった。", false);
                        break;
                    case NodeKind.Repeat:
                        int times = n.CountExpr != null ? Value(c, n.CountExpr, n) : n.Count;
                        if (times > 100) throw new Stop(FailKind.VariableProblem, n, $"くり返す回数が {times} 回になりました。多すぎます。");
                        for (int i = 0; i < times; i++)
                        {
                            Tick(c, n);
                            Exec(c, n.Body);
                        }
                        break;
                    case NodeKind.If:
                        if (Check(c, n)) Exec(c, n.Body);
                        else if (n.HasElse) Exec(c, n.Else);
                        break;
                    case NodeKind.While:
                        while (Check(c, n))
                        {
                            Tick(c, n);
                            Exec(c, n.Body);
                        }
                        break;
                    case NodeKind.Define:
                        // Python と同じく、def の行を通ったときに関数ができる（中身はまだ動かない）
                        c.Funcs[n.Var] = n;
                        break;
                    case NodeKind.Call:
                        if (!c.Funcs.TryGetValue(n.Var, out var def))
                            throw new Stop(FailKind.VariableProblem, n, $"関数 {n.Var} は、まだ作られていません。使う行より上で def して作ろう。");
                        Tick(c, n);
                        // 関数の中で自分を呼ぶと終わらないので、深さに上限をつける
                        if (++c.CallDepth > 20)
                            throw new Stop(FailKind.EndlessLoop, n, $"関数 {n.Var} の中で、関数を呼びつづけて終わりませんでした。");
                        Exec(c, def.Body);
                        c.CallDepth--;
                        break;
                }
            }
        }

        private static void Tick(Ctx c, Node n)
        {
            if (++c.Checks > MaxLoopChecks)
                throw new Stop(FailKind.EndlessLoop, n, "くり返しが いつまでも 終わりませんでした。");
        }

        private static bool Check(Ctx c, Node n)
        {
            Tick(c, n);
            bool v = n.Cmp != null ? CompareValues(c, n.Cmp, n) : Eval(c.Map, c.Pos, c.Dir, n.Condition);
            return n.Negate ? !v : v;
        }

        private static bool CompareValues(Ctx c, Compare cmp, Node n)
        {
            int a = Value(c, cmp.Left, n), b = Value(c, cmp.Right, n);
            switch (cmp.Op)
            {
                case ">": return a > b;
                case "<": return a < b;
                case "==": return a == b;
                case "!=": return a != b;
                case ">=": return a >= b;
                default: return a <= b;
            }
        }

        /// <summary>式の値を計算する（read_sign() は足元の看板の数）</summary>
        private static int Value(Ctx c, Expr e, Node n)
        {
            int a = TermValue(c, e.A, n);
            if (e.Op == '\0') return a;
            int b = TermValue(c, e.B, n);
            return e.Op == '+' ? a + b : a - b;
        }

        private static int TermValue(Ctx c, Term t, Node n)
        {
            switch (t.Kind)
            {
                case TermKind.Number: return t.Number;
                case TermKind.Read: return c.Map.SignAt(c.Pos);
                default:
                    if (c.Vars.TryGetValue(t.Name, out int v)) return v;
                    throw new Stop(FailKind.VariableProblem, n, $"変数 {t.Name} に、まだ数が入っていません。先に「{t.Name} = 〜」で数を入れよう。");
            }
        }

        public static bool Eval(GridMap map, P pos, int dir, ConditionType cond)
        {
            switch (cond)
            {
                case ConditionType.IsObstacle: return map.Get(pos + GridMap.Dirs[dir]) == Cell.Obstacle;
                case ConditionType.IsBlocked: return map.Get(pos + GridMap.Dirs[dir]) == Cell.Water;
                case ConditionType.AtGoal: return pos == map.Goal;
                case ConditionType.RightOpen: return map.Get(pos + GridMap.Dirs[(dir + 3) % 4]) != Cell.Water;
                case ConditionType.LeftOpen: return map.Get(pos + GridMap.Dirs[(dir + 1) % 4]) != Cell.Water;
                case ConditionType.OnItem: return map.HasItem(pos);
            }
            return false;
        }

        private static void Act(Ctx c, Node n)
        {
            if (++c.Actions > MaxActions)
                throw new Stop(FailKind.EndlessLoop, n, "命令を実行しすぎました（くり返しが終わらない？）。");

            string head = c.R.TestCount > 1 ? $"テスト{c.Test + 1}: " : "";
            var th = c.Theme;
            P front = c.Pos + GridMap.Dirs[c.Dir];
            switch (n.Command)
            {
                case CommandType.Forward:
                    var cell = c.Map.Get(front);
                    if (cell == Cell.Water) throw new Stop(FailKind.HitWall, n, "目の前がかべ（川）で、進めませんでした。");
                    if (cell == Cell.Obstacle) throw new Stop(FailKind.BlockedByObstacle, n, $"目の前に{th.ObstacleName}があって、進めませんでした。");
                    c.Pos = front;
                    Snapshot(c, n, head + (c.Pos == c.Map.Goal ? "すすんで、ゴールに着いた！" : "すすんだ。"), false);
                    break;
                case CommandType.TurnLeft:
                    c.Dir = (c.Dir + 1) % 4;
                    Snapshot(c, n, head + "左を向いた。", false);
                    break;
                case CommandType.TurnRight:
                    c.Dir = (c.Dir + 3) % 4;
                    Snapshot(c, n, head + "右を向いた。", false);
                    break;
                case CommandType.Magic:
                    if (c.Map.Get(front) == Cell.Obstacle)
                    {
                        c.Map.Set(front, Cell.Floor);
                        Snapshot(c, n, head + $"まほうで{th.ObstacleName}を消した！", false);
                    }
                    else Snapshot(c, n, head + "まほうを使ったけど、何も起きなかった。", false);
                    break;
                case CommandType.Pick:
                    if (c.Map.HasItem(c.Pos))
                    {
                        c.Map.SetItem(c.Pos, false);
                        Snapshot(c, n, head + $"{th.ItemName}をひろった！", false);
                    }
                    else Snapshot(c, n, head + "足元には何もなかった。", false);
                    break;
                case CommandType.Use:
                    if (c.Pos != c.Map.Goal)
                        throw new Stop(FailKind.UseNotAtGoal, n, $"ゴールの{th.GoalName}に着く前に「つかう」をしました。");
                    if (c.Map.CountItems() > 0)
                        throw new Stop(FailKind.ItemsLeft, n, $"{th.ItemName}が まだ {c.Map.CountItems()} こ 残っているのに「つかう」をしました。");
                    c.Done = true;
                    Snapshot(c, n, head + QuestTheme.GoalDone[(int)th.Goal], false);
                    break;
            }
        }

        private static void Snapshot(Ctx c, Node n, string log, bool failure)
        {
            c.R.Steps.Add(new RunStep
            {
                Test = c.Test, Node = n, Map = c.Map.Clone(), Pos = c.Pos, Dir = c.Dir, Done = c.Done,
                Log = log, IsFailure = failure,
            });
        }

        private static RunResult Fail(RunResult r, FailKind kind, Node node, int test, string message)
        {
            r.Success = false;
            r.Fail = kind;
            r.FailMessage = message;
            r.FailedNode = node;
            r.FailedTest = test;
            return r;
        }
    }
}
