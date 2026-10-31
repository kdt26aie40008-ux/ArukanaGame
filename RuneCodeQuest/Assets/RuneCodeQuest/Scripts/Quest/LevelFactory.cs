using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>
    /// 100レベルの問題を作る係。
    ///
    /// 【しくみ】
    /// 1. レベルごとに「お手本の答え（プログラム）」を決める
    /// 2. お手本どおりにゴーレムを動かして、通った道を「橋」にする（Carve）
    /// 3. 条件分岐を学ぶレベルからは、穴の場所や道の長さをテストごとに変える
    /// 4. 作った橋をお手本で実際に解いてみて、解けることを確かめる
    ///
    /// variant（似た問題の番号）を変えると、同じ考え方で数値や配置が少しちがう問題になります。
    /// レベルの中身を変えたいときは Build の中の各レベルの行を編集します。
    /// </summary>
    public static class LevelFactory
    {
        public const int MaxW = 16, MaxH = 11;

        private static readonly Dictionary<long, Level> cache = new Dictionary<long, Level>();

        /// <summary>関数（def）が使えるようになるステージ（第6章のとちゅう。じゃまものが出てきて、命令が長くなるところ）</summary>
        public const int FunctionsFrom = 86;

        public static Level Create(int number, int variant)
        {
            number = Math.Max(1, Math.Min(QuestDatabase.LevelCount, number));
            long key = number * 100000L + variant;
            if (cache.TryGetValue(key, out var cached)) return cached;

            var rng = new Random(number * 7919 + variant * 104729 + 17);
            Level level = null;
            for (int attempt = 0; attempt < 400 && level == null; attempt++)
            {
                var candidate = NewLevel(number, variant);
                if (Build(candidate, rng, variant) && AddItems(candidate, rng) && Verify(candidate)) level = candidate;
            }
            if (level == null)
            {
                // ここに来ることはない想定（LevelFactory のテストで全レベルを確認済み）
                level = NewLevel(number, variant);
                BuildStraight(level, 4, new int[0]);
            }
            // もう一度する（variant が1以上）のに、前と まったく同じ地図になったときは、左右反対にして少し変える
            if (variant > 0 && SameAsBefore(level, number, variant)) TryMirror(level);

            // テストは1つだけにする（3つ作った問題は、3つともお手本で解けることを確かめてから 1つ目だけ使う）。
            // そのかわり、解説のあとの「もう一度する」で、じゃまものの場所や長さが少し変わった問題になる
            if (level.Tests.Count > 1)
            {
                level.ChangesOnRetry = true;
                level.Tests.RemoveRange(1, level.Tests.Count - 1);
            }
            Decorate(level);
            if (Rebuild.IsExtra(number)) ApplyUsageLimits(level);
            cache[key] = level;
            return level;
        }

        /// <summary>
        /// おまけの「しばり」：ブロックの種類ごとに、お手本の答えと同じ数までしか置けない。
        /// お手本で使っていないブロックは使えない（0こ）。お手本どおりなら必ず解ける
        /// </summary>
        private static void ApplyUsageLimits(Level level)
        {
            var used = ProgramText.CountByKind(level.Solution);
            var limits = new List<UsageLimit>();
            void Add(string name)
            {
                if (limits.Exists(l => l.Name == name)) return;
                limits.Add(new UsageLimit { Name = name, Max = used.TryGetValue(name, out int n) ? n : 0 });
            }
            foreach (var c in level.Commands) Add(CommandInfo.Get(c).BlockLabel);
            foreach (var s in level.Structures) Add(ProgramText.KindName(s));
            if (level.AllowVariables) Add(ProgramText.KindName(NodeKind.Assign));
            if (level.AllowFunctions) { Add(ProgramText.KindName(NodeKind.Define)); Add(ProgramText.KindName(NodeKind.Call)); }
            level.UsageLimits = limits;
        }

        /// <summary>しばりの一覧の文（例：すすむ 2こまで・くり返す 1こまで・右を向く 使えない）</summary>
        public static string UsageLimitText(Level level)
        {
            if (level.UsageLimits == null) return null;
            var parts = new List<string>();
            foreach (var l in level.UsageLimits) parts.Add(l.Max == 0 ? $"{l.Name} 使えない" : $"{l.Name} {l.Max}こまで");
            return string.Join("・", parts);
        }

        /// <summary>しばりを守っているか調べる。破っていたら、その理由の文を返す（守っていれば空）</summary>
        public static List<string> CheckUsageLimits(Level level, List<Node> program)
        {
            var broken = new List<string>();
            if (level.UsageLimits == null) return broken;
            var used = ProgramText.CountByKind(program);
            foreach (var l in level.UsageLimits)
            {
                int n = used.TryGetValue(l.Name, out int c) ? c : 0;
                if (n <= l.Max) continue;
                broken.Add(l.Max == 0
                    ? $"「{l.Name}」は使えないのに、{n}こ使っています。"
                    : $"「{l.Name}」は {l.Max}こまでなのに、{n}こ使っています。");
            }
            return broken;
        }

        // ------------------------------------------------------------------
        // もう一度するときに、問題を少しだけ変える（左右反対）
        // ------------------------------------------------------------------

        private static bool SameMap(GridMap a, GridMap b)
        {
            if (a.W != b.W || a.H != b.H || a.Start != b.Start || a.Goal != b.Goal || a.StartDir != b.StartDir) return false;
            for (int i = 0; i < a.Cells.Length; i++)
                if (a.Cells[i] != b.Cells[i] || a.Items[i] != b.Items[i] || a.Signs[i] != b.Signs[i]) return false;
            return true;
        }

        /// <summary>1つ前の回（と最初の回）と、1つ目の地図が同じか</summary>
        private static bool SameAsBefore(Level level, int number, int variant)
        {
            if (level.Tests.Count == 0) return false;
            var first = Create(number, 0);
            if (first.Tests.Count > 0 && SameMap(level.Tests[0], first.Tests[0])) return true;
            if (variant > 1)
            {
                var prev = Create(number, variant - 1);
                if (prev.Tests.Count > 0 && SameMap(level.Tests[0], prev.Tests[0])) return true;
            }
            return false;
        }

        /// <summary>地図とお手本を左右反対にする（お手本で解けなければ、元にもどす）</summary>
        private static void TryMirror(Level level)
        {
            var tests = level.Tests;
            var solution = level.Solution;
            level.Tests = new List<GridMap>();
            foreach (var m in tests) level.Tests.Add(MirrorMap(m));
            level.Solution = MirrorNodes(solution);
            if (Verify(level)) return;
            level.Tests = tests;
            level.Solution = solution;
        }

        private static GridMap MirrorMap(GridMap m)
        {
            var r = m.Clone();
            for (int y = 0; y < m.H; y++)
                for (int x = 0; x < m.W; x++)
                {
                    int from = y * m.W + x, to = y * m.W + (m.W - 1 - x);
                    r.Cells[to] = m.Cells[from];
                    r.Items[to] = m.Items[from];
                    r.Signs[to] = m.Signs[from];
                }
            r.Start = new P(m.W - 1 - m.Start.X, m.Start.Y);
            r.Goal = new P(m.W - 1 - m.Goal.X, m.Goal.Y);
            r.StartDir = m.StartDir == 0 ? 2 : m.StartDir == 2 ? 0 : m.StartDir; // 右 ⇔ 左（上・下はそのまま）
            return r;
        }

        /// <summary>お手本の 右を向く ⇔ 左を向く、右が通れる ⇔ 左が通れる を入れかえる</summary>
        private static List<Node> MirrorNodes(List<Node> list)
        {
            var result = new List<Node>();
            if (list == null) return result;
            foreach (var n in list)
            {
                var c = n.Clone();
                if (c.Kind == NodeKind.Command)
                    c.Command = c.Command == CommandType.TurnLeft ? CommandType.TurnRight : c.Command == CommandType.TurnRight ? CommandType.TurnLeft : c.Command;
                if (c.Condition == ConditionType.RightOpen) c.Condition = ConditionType.LeftOpen;
                else if (c.Condition == ConditionType.LeftOpen) c.Condition = ConditionType.RightOpen;
                c.Body = MirrorNodes(n.Body);
                c.Else = MirrorNodes(n.Else);
                result.Add(c);
            }
            return result;
        }

        /// <summary>お手本で全テストに成功するか確かめる</summary>
        public static bool Verify(Level level) =>
            level.Tests.Count > 0 && GolemRunner.Run(level, level.Solution).Success &&
            (level.MaxBlocks == 0 || ProgramText.Count(level.Solution) <= level.MaxBlocks);

        private static Level NewLevel(int n, int variant)
        {
            int t = TemplateOf(n);
            var level = new Level { Number = n, Template = t, Variant = variant, Chapter = QuestDatabase.ChapterOf(n), Theme = QuestTheme.Of(t) };
            if (variant == 0 && QuestDatabase.Concepts.TryGetValue(t, out var c)) level.NewConcept = c;

            // ステージが進むと、できることが増える
            //   1〜 すすむ・まほう  3〜 つかう  7〜 向きを変える  13〜 くり返し  25〜 もし
            //   27〜 足元にアイテム  32〜 でなければ・前がかべ  43〜 〜までくり返す  61〜 変数  81〜 右／左が通れる  86〜 関数（def）
            level.Commands.AddRange(new[] { CommandType.Forward, CommandType.Magic });
            if (n >= 3) level.Commands.Add(CommandType.Use);
            if (n >= 7) level.Commands.InsertRange(1, new[] { CommandType.TurnLeft, CommandType.TurnRight });
            if (n >= 13) level.Structures.Add(NodeKind.Repeat);
            if (n >= 25) level.Structures.Add(NodeKind.If);
            if (n >= 43) level.Structures.Add(NodeKind.While);
            level.AllowElse = n >= 32;
            level.AllowVariables = n >= 61;
            level.AllowFunctions = n >= FunctionsFrom;
            if (n >= 25) level.Conditions.Add(ConditionType.IsObstacle);
            if (n >= 32) level.Conditions.Add(ConditionType.IsBlocked);
            if (n >= 43) level.Conditions.Add(ConditionType.AtGoal);
            if (n >= 81) { level.Conditions.Add(ConditionType.RightOpen); level.Conditions.Add(ConditionType.LeftOpen); }
            if (level.Theme.HasItems)
            {
                int useIndex = level.Commands.IndexOf(CommandType.Use);
                if (useIndex < 0) level.Commands.Add(CommandType.Pick);
                else level.Commands.Insert(useIndex, CommandType.Pick);
                if (n >= 27) level.Conditions.Add(ConditionType.OnItem);
            }
            return level;
        }

        // ------------------------------------------------------------------
        // 部品を作る短い名前
        // ------------------------------------------------------------------

        private static Node F() => Node.Cmd(CommandType.Forward);
        private static Node TL() => Node.Cmd(CommandType.TurnLeft);
        private static Node TR() => Node.Cmd(CommandType.TurnRight);
        private static Node Mag() => Node.Cmd(CommandType.Magic);
        private static Node UseIt() => Node.Cmd(CommandType.Use);
        private static Node Turn(bool left) => left ? TL() : TR();
        private static Node Rep(int n, params Node[] body) => Node.Repeat(n, body);
        private static Node IfObstacle() => Node.If(ConditionType.IsObstacle, false, new[] { Mag() });
        private static Node IfBlocked(bool left) => Node.If(ConditionType.IsBlocked, false, new[] { Turn(left) });
        private static Node UntilGoal(params Node[] body) => Node.While(ConditionType.AtGoal, true, body);

        // ------------------------------------------------------------------
        // 各レベル
        // ------------------------------------------------------------------

        private static bool Build(Level L, Random rng, int variant)
        {
            int n = L.Template;
            bool v = variant > 0;
            if (n >= 101) return BuildVariable(L, rng);
            int Var(int baseValue, int min, int max) => v ? Clamp(baseValue + rng.Next(-1, 2), min, max) : baseValue;
            bool RandomTurn() => rng.Next(2) == 0;

            // ===== 第1章 順次処理（1〜15） =====
            if (n == 1 && !v) { L.Title = "はじまりの橋"; return BuildStraight(L, 3, new[] { 2 }); }
            if (n <= 7)
            {
                int[] lens = { 3, 4, 4, 5, 5, 6, 7 };
                int[] holes = { 1, 1, 1, 2, 1, 2, 3 };
                L.Title = "まっすぐな橋";
                int len = Var(lens[n - 1], 3, 10);
                return BuildStraight(L, len, PickHoles(rng, len - 1, Math.Min(holes[n - 1], len - 1)));
            }
            if (n <= 15)
            {
                int[][] segs =
                {
                    new[] { 3, 3 }, new[] { 3, 2 }, new[] { 2, 3, 2 }, new[] { 3, 3, 3 },
                    new[] { 2, 2, 2, 2 }, new[] { 4, 3, 3 }, new[] { 3, 2, 3, 2 }, new[] { 4, 3, 4, 2 },
                };
                int[] holes = { 0, 1, 1, 2, 2, 3, 3, 4 };
                var s = (int[])segs[n - 8].Clone();
                if (v) for (int i = 0; i < s.Length; i++) s[i] = Var(s[i], 2, 4);
                L.Title = "曲がり角の橋";
                return BuildTurning(L, rng, s, holes[n - 8]);
            }

            // ===== 第2章 くり返し（16〜40） =====
            List<Node> sol;
            switch (n)
            {
                case 16: L.Title = "長い橋"; sol = Prog(Rep(Var(9, 8, 10), F()), UseIt()); break;
                case 17: L.Title = "とても長い橋"; sol = Prog(Rep(Var(12, 11, 13), F()), UseIt()); break;
                case 18: L.Title = "もようのある橋"; sol = Prog(Rep(Var(4, 3, 5), Unit(2, new[] { 1 })), UseIt()); break;
                case 19: L.Title = "もようのある橋"; sol = Prog(Rep(Var(3, 3, 4), Unit(3, new[] { 1 + rng.Next(2) })), UseIt()); break;
                case 20: L.Title = "もようのある長い橋"; sol = Prog(Rep(Var(6, 5, 6), Unit(2, new[] { 1 })), UseIt()); break;
                case 21: L.Title = "ふくざつなもよう"; sol = Prog(Rep(3, Unit(4, v ? new[] { 1, 2 + rng.Next(2) } : new[] { 1, 3 })), UseIt()); break;
                case 22: L.Title = "ふくざつなもよう"; sol = Prog(Rep(Var(4, 3, 4), Unit(3, new[] { 1, 2 })), UseIt()); break;
                case 23: L.Title = "階段の橋"; sol = Prog(Rep(Var(4, 3, 5), F(), TL(), F(), TR()), UseIt()); break;
                case 24: L.Title = "よこ長の階段"; sol = Prog(Rep(3, F(), F(), TL(), F(), TR()), UseIt()); break;
                case 25: L.Title = "たて長の階段"; sol = Prog(Rep(3, F(), TL(), F(), F(), TR()), UseIt()); break;
                case 26: L.Title = "穴のある階段"; sol = Prog(Rep(Var(4, 3, 4), Mag(), F(), TL(), F(), TR()), UseIt()); break;
                case 27: L.Title = "くだりの階段"; sol = Prog(Rep(Var(4, 3, 5), F(), TR(), F(), TL()), UseIt()); break;
                case 28: L.Title = "ジグザグの橋"; sol = Prog(Rep(2, F(), F(), F(), TL(), F(), F(), TL(), F(), F(), F(), TR(), F(), F(), TR()), UseIt()); break;
                case 29: L.Title = "穴のあるジグザグ"; sol = Prog(Rep(2, Mag(), F(), F(), F(), TL(), F(), F(), TL(), Mag(), F(), F(), F(), TR(), F(), F(), TR()), UseIt()); break;
                case 30: L.Title = "長い階段"; sol = Prog(Rep(Var(5, 4, 5), Mag(), F(), TL(), F(), TR()), UseIt()); break;
                case 31: L.Title = "大きな階段"; sol = Prog(Rep(3, Rep(4, F()), TL(), Rep(2, F()), TR()), UseIt()); break;
                case 32: L.Title = "大きな階段"; sol = Prog(Rep(2, Rep(3, F()), TL(), Rep(Var(4, 3, 4), F()), TR()), UseIt()); break;
                case 33: L.Title = "大きな階段"; sol = Prog(Rep(4, Rep(2, F()), TL(), Rep(2, F()), TR()), UseIt()); break;
                case 34: L.Title = "穴だらけの階段"; sol = Prog(Rep(3, Rep(2, Mag(), F()), TL(), Rep(2, F()), TR()), UseIt()); break;
                case 35: L.Title = "大きなジグザグ"; sol = Prog(Rep(2, Rep(Var(4, 3, 5), F()), TL(), Rep(2, F()), TL(), Rep(Var(4, 3, 5), F()), TR(), Rep(2, F()), TR()), UseIt()); break;
                case 36: L.Title = "穴のあるジグザグ"; sol = Prog(Rep(2, Rep(2, Mag(), F(), F()), TL(), Rep(2, F()), TL(), Rep(2, Mag(), F(), F()), TR(), Rep(2, F()), TR()), UseIt()); break;
                case 37: L.Title = "大きなくだり階段"; sol = Prog(Rep(3, Rep(3, F()), TR(), Rep(2, F()), TL()), UseIt()); break;
                case 38: L.Title = "もようの階段"; sol = Prog(Rep(3, Rep(2, F(), Mag(), F()), TL(), Rep(2, F()), TR()), UseIt()); break;
                case 39: L.Title = "ながいジグザグ"; sol = Prog(Rep(2, Rep(3, F()), TL(), Rep(2, F()), TL(), Rep(3, F()), TR(), Rep(2, F()), TR()), UseIt()); break;
                case 40: L.Title = "くり返しの試練"; sol = Prog(Rep(4, Rep(2, Mag(), F()), TL(), Rep(2, F()), TR()), UseIt()); break;
                default: sol = null; break;
            }
            if (sol != null)
            {
                L.Solution = sol;
                var map = Carve(sol, true);
                if (map == null) return false;
                L.Tests.Add(map);
                L.MaxBlocks = ProgramText.Count(sol) + 1;
                return true;
            }

            // ===== 第3章 条件分岐（41〜60）：テストは3つ、穴や曲がり角の場所が変わる =====
            if (n <= 43)
            {
                L.Title = "気まぐれな穴の橋";
                int len = Var(new[] { 5, 7, 9 }[n - 41], 4, 10);
                return BuildRandomObstacles(L, rng, Prog(Rep(len, IfObstacle(), F()), UseIt()), 0.35);
            }
            if (n <= 47)
            {
                L.Title = "曲がった気まぐれ橋";
                bool left = RandomTurn();
                List<Node> p;
                switch (n)
                {
                    case 44: p = Prog(Rep(Var(4, 3, 5), IfObstacle(), F()), Turn(left), Rep(Var(3, 2, 4), IfObstacle(), F()), UseIt()); break;
                    case 45: p = Prog(Rep(3, Rep(2, IfObstacle(), F()), TL(), Rep(2, IfObstacle(), F()), TR()), UseIt()); break;
                    case 46: p = Prog(Rep(2, Rep(3, IfObstacle(), F()), TL(), Rep(3, IfObstacle(), F()), TR()), UseIt()); break;
                    default: p = Prog(Rep(2, Rep(3, IfObstacle(), F()), TL(), Rep(2, IfObstacle(), F()), TL(), Rep(3, IfObstacle(), F()), TR(), Rep(2, IfObstacle(), F()), TR()), UseIt()); break;
                }
                return BuildRandomObstacles(L, rng, p, 0.3);
            }
            if (n <= 52)
            {
                // もし 前がかべ なら 向く、でなければ すすむ（曲がり角の数は同じ、場所がちがう）
                L.Title = "かわる曲がり角";
                bool left = n <= 50;
                int total = Var(new[] { 8, 10, 12, 10, 12 }[n - 48], 7, 13);
                int corners = new[] { 2, 2, 3, 2, 3 }[n - 48];
                var p = Prog(Rep(total + corners, Node.If(ConditionType.IsBlocked, false, new[] { Turn(left) }, new[] { F() })), UseIt());
                return BuildSpiralTests(L, rng, p, left, total, corners, 0);
            }
            if (n <= 60)
            {
                L.Title = n == 60 ? "条件分岐の試練" : "迷いの橋";
                bool left = n != 55 && n != 57 && n != 59;
                int total = Var(new[] { 8, 10, 10, 12, 12, 14, 14, 16 }[n - 53], 7, 16);
                int corners = new[] { 2, 2, 2, 3, 3, 3, 3, 4 }[n - 53];
                var p = Prog(Rep(total, IfBlocked(left), IfObstacle(), F()), UseIt());
                return BuildSpiralTests(L, rng, p, left, total, corners, 0.25);
            }

            // ===== 第4章 〜までくり返す（61〜80）：テストごとに長さがちがう =====
            if (n <= 67)
            {
                L.Title = "長さのわからない橋";
                bool holes = n >= 64;
                int idx = holes ? n - 64 : n - 61;
                int min = 3 + idx, max = 8 + idx * 2;
                var p = holes ? Prog(UntilGoal(IfObstacle(), F()), UseIt()) : Prog(UntilGoal(F()), UseIt());
                return BuildStraightTests(L, rng, p, min, max, holes ? 0.3 : 0);
            }
            if (n <= 76)
            {
                L.Title = "うずまきの道";
                bool left = n <= 72;
                bool holes = n >= 70;
                var p = holes ? Prog(UntilGoal(IfBlocked(left), IfObstacle(), F()), UseIt()) : Prog(UntilGoal(IfBlocked(left), F()), UseIt());
                int maxCorners = n <= 69 ? 2 : 3;
                return BuildSpiralTestsVarying(L, rng, p, left, 1, maxCorners, holes ? 0.25 : 0);
            }
            if (n <= 80)
            {
                L.Title = n == 80 ? "くり返しの奥義" : "でなければの道";
                bool left = n <= 78;
                var p = Prog(UntilGoal(Node.If(ConditionType.IsBlocked, false, new[] { Turn(left) }, new[] { IfObstacle(), F() })), UseIt());
                return BuildSpiralTestsVarying(L, rng, p, left, 2, 3, 0.25);
            }

            // ===== 第5章 総合（81〜100）：曲がりくねった道 =====
            {
                L.Title = n == 100 ? "遺跡の最深部" : n >= 96 ? "大回廊" : "曲がりくねった回廊";
                bool holes = n >= 86;
                var turn = Node.If(ConditionType.IsBlocked, false, new[]
                {
                    Node.If(ConditionType.RightOpen, false, new[] { TR() }, new[] { TL() }),
                });
                List<Node> p;
                if (n >= FunctionsFrom)
                {
                    // 関数（def）を使ったお手本：「曲がる」と「1歩進む」を自分の命令にして、くり返しの中から使う
                    p = Prog(
                        Node.Define("turn_corner", turn),
                        Node.Define("step_forward", IfObstacle(), F()),
                        UntilGoal(Node.Call("turn_corner"), Node.Call("step_forward")),
                        UseIt());
                }
                else p = holes ? Prog(UntilGoal(turn, IfObstacle(), F()), UseIt()) : Prog(UntilGoal(turn, F()), UseIt());
                int segs = 3 + (n - 81) * 7 / 19;      // 3 → 10
                int maxLen = n >= 93 ? 3 : 4;
                double holeRate = holes ? 0.2 + (n - 86) * 0.01 : 0;
                return BuildCorridorTests(L, rng, p, segs, maxLen, holeRate);
            }
        }

        // ------------------------------------------------------------------
        // 問題の組み立て
        // ------------------------------------------------------------------

        private static List<Node> Prog(params Node[] nodes) => new List<Node>(nodes);

        /// <summary>もようの1区切り：p マス進むうち、holes の位置（1から）が穴</summary>
        private static Node[] Unit(int p, int[] holes)
        {
            var list = new List<Node>();
            for (int s = 1; s <= p; s++)
            {
                if (Array.IndexOf(holes, s) >= 0) list.Add(Mag());
                list.Add(F());
            }
            return list.ToArray();
        }

        /// <summary>まっすぐな橋（len 回すすむとゴール。holes は穴の位置 1〜len-1）</summary>
        private static bool BuildStraight(Level L, int len, int[] holes)
        {
            var sol = new List<Node>();
            for (int i = 1; i <= len; i++)
            {
                if (Array.IndexOf(holes, i) >= 0) sol.Add(Mag());
                sol.Add(F());
            }
            if (L.Commands.Contains(CommandType.Use)) sol.Add(UseIt());
            L.Solution = sol;
            var map = Carve(sol, true);
            if (map == null) return false;
            L.Tests.Add(map);
            return true;
        }

        /// <summary>曲がり角のある橋（順次処理だけで解く）</summary>
        private static bool BuildTurning(Level L, Random rng, int[] segs, int holeCount)
        {
            int total = 0;
            foreach (int s in segs) total += s;
            var holes = PickHoles(rng, total - 1, holeCount);
            var sol = new List<Node>();
            int step = 0;
            for (int i = 0; i < segs.Length; i++)
            {
                for (int k = 0; k < segs[i]; k++)
                {
                    step++;
                    if (Array.IndexOf(holes, step) >= 0) sol.Add(Mag());
                    sol.Add(F());
                }
                if (i < segs.Length - 1) sol.Add(Turn(rng.Next(2) == 0));
            }
            if (L.Commands.Contains(CommandType.Use)) sol.Add(UseIt());
            L.Solution = sol;
            var map = Carve(sol, true);
            if (map == null) return false;
            L.Tests.Add(map);
            return true;
        }

        /// <summary>同じ形の橋を3つ作り、穴の場所だけテストごとに変える</summary>
        private static bool BuildRandomObstacles(Level L, Random rng, List<Node> sol, double rate)
        {
            L.Solution = sol;
            var path = new List<P>();
            var map = Carve(sol, true, path);
            if (map == null) return false;
            for (int t = 0; t < 3; t++) L.Tests.Add(WithRandomHoles(map, path, rng, rate));
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        /// <summary>長さがテストごとにちがう、まっすぐな橋</summary>
        private static bool BuildStraightTests(Level L, Random rng, List<Node> sol, int min, int max, double holeRate)
        {
            L.Solution = sol;
            var used = new HashSet<int>();
            for (int t = 0; t < 3; t++)
            {
                int len;
                do { len = rng.Next(min, max + 1); } while (used.Contains(len) && used.Count < max - min + 1);
                used.Add(len);
                var skeleton = new List<Node> { Rep(len, F()), UseIt() };
                var path = new List<P>();
                var map = Carve(skeleton, true, path);
                if (map == null) return false;
                L.Tests.Add(holeRate > 0 ? WithRandomHoles(map, path, rng, holeRate) : map);
            }
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        /// <summary>同じ方向にだけ曲がる道（すすむ回数と曲がる回数は全テスト同じ）</summary>
        private static bool BuildSpiralTests(Level L, Random rng, List<Node> sol, bool left, int total, int corners, double holeRate)
        {
            L.Solution = sol;
            for (int t = 0; t < 3; t++)
            {
                GridMap map = null;
                var path = new List<P>();
                for (int tries = 0; tries < 60 && map == null; tries++)
                {
                    path.Clear();
                    var segs = SplitTotal(rng, total, corners + 1, 2);
                    if (segs == null) return false;
                    map = Carve(SegmentsToSkeleton(segs, left), true, path);
                }
                if (map == null) return false;
                L.Tests.Add(holeRate > 0 ? WithRandomHoles(map, path, rng, holeRate) : map);
            }
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        /// <summary>同じ方向にだけ曲がる道（長さも曲がる回数もテストごとにちがう）</summary>
        private static bool BuildSpiralTestsVarying(Level L, Random rng, List<Node> sol, bool left, int minCorners, int maxCorners, double holeRate)
        {
            L.Solution = sol;
            for (int t = 0; t < 3; t++)
            {
                GridMap map = null;
                var path = new List<P>();
                for (int tries = 0; tries < 60 && map == null; tries++)
                {
                    path.Clear();
                    int corners = rng.Next(minCorners, maxCorners + 1);
                    var segs = new int[corners + 1];
                    for (int i = 0; i < segs.Length; i++) segs[i] = rng.Next(2, 6);
                    map = Carve(SegmentsToSkeleton(segs, left), true, path);
                }
                if (map == null) return false;
                L.Tests.Add(holeRate > 0 ? WithRandomHoles(map, path, rng, holeRate) : map);
            }
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        /// <summary>右にも左にも曲がる、曲がりくねった道</summary>
        private static bool BuildCorridorTests(Level L, Random rng, List<Node> sol, int segCount, int maxLen, double holeRate)
        {
            L.Solution = sol;
            for (int t = 0; t < 3; t++)
            {
                GridMap map = null;
                var path = new List<P>();
                for (int tries = 0; tries < 200 && map == null; tries++)
                {
                    path.Clear();
                    var skeleton = new List<Node>();
                    for (int i = 0; i < segCount; i++)
                    {
                        int len = rng.Next(2, maxLen + 1);
                        for (int k = 0; k < len; k++) skeleton.Add(F());
                        if (i < segCount - 1) skeleton.Add(Turn(rng.Next(2) == 0));
                    }
                    skeleton.Add(UseIt());
                    // 最初の向きもランダムにする
                    int startTurns = rng.Next(4);
                    for (int k = 0; k < startTurns; k++) skeleton.Insert(0, TL());
                    map = Carve(skeleton, true, path);
                }
                if (map == null) return false;
                L.Tests.Add(holeRate > 0 ? WithRandomHoles(map, path, rng, holeRate) : map);
            }
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        private static List<Node> SegmentsToSkeleton(int[] segs, bool left)
        {
            var list = new List<Node>();
            for (int i = 0; i < segs.Length; i++)
            {
                for (int k = 0; k < segs[i]; k++) list.Add(F());
                if (i < segs.Length - 1) list.Add(Turn(left));
            }
            list.Add(UseIt());
            return list;
        }

        /// <summary>total を count 個に分ける（それぞれ min 以上）</summary>
        private static int[] SplitTotal(Random rng, int total, int count, int min)
        {
            if (total < count * min) return null;
            var segs = new int[count];
            for (int i = 0; i < count; i++) segs[i] = min;
            for (int r = total - count * min; r > 0; r--) segs[rng.Next(count)]++;
            return segs;
        }

        private static int[] PickHoles(Random rng, int maxPos, int count)
        {
            var all = new List<int>();
            for (int i = 1; i <= maxPos; i++) all.Add(i);
            var picked = new List<int>();
            for (int i = 0; i < count && all.Count > 0; i++)
            {
                int k = rng.Next(all.Count);
                picked.Add(all[k]);
                all.RemoveAt(k);
            }
            return picked.ToArray();
        }

        /// <summary>道（スタートとゴール以外）に、ランダムに穴をあけたコピーを作る（最低1つ）</summary>
        private static GridMap WithRandomHoles(GridMap baseMap, List<P> path, Random rng, double rate)
        {
            var map = baseMap.Clone();
            var candidates = new List<P>();
            for (int i = 1; i < path.Count - 1; i++) candidates.Add(path[i]);
            if (candidates.Count == 0) return map;
            foreach (var p in candidates) if (rng.NextDouble() < rate) map.Set(p, Cell.Obstacle);
            if (map.CountObstacles() == 0) map.Set(candidates[rng.Next(candidates.Count)], Cell.Obstacle);
            return map;
        }

        // ------------------------------------------------------------------
        // お手本どおりに動いて、通った道を橋にする
        // ------------------------------------------------------------------

        private class CarveState
        {
            public P Pos;
            public int Dir;
            public readonly List<P> Path = new List<P>();
            public readonly HashSet<P> Visited = new HashSet<P>();
            public readonly HashSet<P> Holes = new HashSet<P>();
            public readonly HashSet<P> Items = new HashSet<P>();
            public P? Goal;
            public bool Bad;
        }

        /// <summary>
        /// お手本（命令とくり返しだけの部分）どおりに動いた道から、マップを作る。
        /// 同じマスを2回通る・となりの道どうしがくっつく（thin = true のとき）・大きすぎる場合は null。
        /// 「もし」「〜までくり返す」はここでは読み飛ばす。
        /// </summary>
        private static GridMap Carve(List<Node> program, bool thin, List<P> pathOut = null)
        {
            var s = new CarveState();
            s.Path.Add(s.Pos);
            s.Visited.Add(s.Pos);
            int startDir = -1;
            CarveExec(s, program, ref startDir);
            if (s.Bad || s.Path.Count < 2) return null;
            foreach (var hole in s.Holes) if (!s.Visited.Contains(hole) || hole == s.Path[0]) return null;
            foreach (var item in s.Items) if (s.Holes.Contains(item) || item == s.Path[0]) return null;

            P goal = s.Goal ?? s.Pos;
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var p in s.Path)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            int w = maxX - minX + 3, h = maxY - minY + 3;
            if (w > MaxW || h > MaxH) return null;

            if (thin)
            {
                var index = new Dictionary<P, int>();
                for (int i = 0; i < s.Path.Count; i++) index[s.Path[i]] = i;
                for (int i = 0; i < s.Path.Count; i++)
                    foreach (var d in GridMap.Dirs)
                        if (index.TryGetValue(s.Path[i] + d, out int j) && Math.Abs(i - j) != 1) return null;
            }

            var map = new GridMap(w, h);
            var off = new P(1 - minX, 1 - minY);
            foreach (var p in s.Path) map.Set(p + off, s.Holes.Contains(p) ? Cell.Obstacle : Cell.Floor);
            map.Set(goal + off, Cell.Goal);
            foreach (var item in s.Items) map.SetItem(item + off, true);
            map.Start = s.Path[0] + off;
            map.StartDir = startDir < 0 ? 0 : startDir; // 最初にすすむ方向を向いてスタート
            map.Goal = goal + off;
            if (pathOut != null) foreach (var p in s.Path) pathOut.Add(p + off);
            return map;
        }

        private static void CarveExec(CarveState s, List<Node> list, ref int startDir)
        {
            foreach (var n in list)
            {
                if (s.Bad) return;
                if (n.Kind == NodeKind.Repeat)
                {
                    for (int i = 0; i < n.Count; i++) CarveExec(s, n.Body, ref startDir);
                    continue;
                }
                if (n.Kind != NodeKind.Command) continue;
                switch (n.Command)
                {
                    case CommandType.Forward:
                        if (startDir < 0) startDir = s.Dir;
                        var next = s.Pos + GridMap.Dirs[s.Dir];
                        if (s.Visited.Contains(next)) { s.Bad = true; return; }
                        s.Pos = next;
                        s.Path.Add(next);
                        s.Visited.Add(next);
                        break;
                    case CommandType.TurnLeft: s.Dir = (s.Dir + 1) % 4; break;
                    case CommandType.TurnRight: s.Dir = (s.Dir + 3) % 4; break;
                    case CommandType.Magic: s.Holes.Add(s.Pos + GridMap.Dirs[s.Dir]); break;
                    case CommandType.Use: s.Goal = s.Pos; break;
                    case CommandType.Pick: s.Items.Add(s.Pos); break;
                }
            }
        }

        // ------------------------------------------------------------------
        // 文章
        // ------------------------------------------------------------------

        private static void Decorate(Level L)
        {
            var th = L.Theme;
            L.Title = $"ステージ{L.Number}　{th.Title}";
            var goal = new System.Text.StringBuilder();
            if (th.HasItems) goal.Append($"{th.ItemName}を すべてひろってから、");
            if (L.Commands.Contains(CommandType.Use)) goal.Append($"ゴールで「つかう」をして {th.GoalAction}（{th.UseTool}を使う）。");
            else goal.Append($"ゴールの{th.GoalName}まで たどり着こう。");
            if (L.MaxBlocks > 0) goal.Append($" 命令は {L.MaxBlocks} 個まで。");
            if (L.ChangesOnRetry) goal.Append(" じゃまものの場所や道の長さは、もう一度するたびに変わるよ。");
            L.Goal = goal.ToString();

            int n = L.Template;
            if (n <= 2) L.Tip = "じゃまものの手前で「まほう」→「すすむ」。ゴールまで たどり着こう。";
            else if (n <= 7) L.Tip = "じゃまものの手前で「まほう」→「すすむ」。最後にゴールで「つかう」。";
            else if (n <= 15) L.Tip = "曲がり角では、ゴーレムから見た左右で向きを変える。向きを変えてもマスは進まない。";
            else if (n <= 30) L.Tip = "くり返している「ひとまとまり」を見つけて、くり返しの中に入れる。";
            else if (n <= 40) L.Tip = "「○マスすすむ」の部分もくり返しにすると、くり返しの中にくり返しが入る（入れ子）。";
            else if (n <= 47) L.Tip = "じゃまものの場所は もう一度するたびに変わる。すすむ前に毎回「もし 前にじゃまもの なら まほう」で調べる。";
            else if (n <= 52) L.Tip = "曲がり角の場所は もう一度するたびに変わる。「もし 前がかべ なら 向く、でなければ すすむ」を、すすむ回数＋曲がる回数だけくり返す。";
            else if (n <= 60) L.Tip = "1歩ごとに「曲がり角？」→「じゃまもの？」→「すすむ」の順に調べる。";
            else if (n <= 67) L.Tip = "長さが分からないときは「ゴールに着くまでくり返す」。中には1歩ぶんの動きを入れる。";
            else if (n <= 80) L.Tip = "ゴールに着くまで、1歩ごとに曲がり角とじゃまものを調べてから すすむ。";
            else if (n < FunctionsFrom) L.Tip = "前がかべなら、右が通れるか調べて右か左を向く。じゃまものなら まほう。すすむ。これをゴールに着くまで。";
            else if (n <= 100) L.Tip = "「曲がる」を turn_corner、「1歩進む」を step_forward という関数（def）にまとめて、ゴールに着くまで2つを順に使う。";
            else if (n <= 107) L.Tip = "看板の数を「n = read_sign()」で変数 n に入れて、n 回くり返す。もう一度するたびに看板の数が変わるので、決めうちでは進めない。";
            else if (n <= 113) L.Tip = "「n = n - 1」のように変数を計算すると、くり返すたびに進む数を変えられる。";
            else L.Tip = "「n > 0 のあいだくり返す」と、n が 0 になったところで止まる。何回くり返すかは n しだい。";
            if (th.HasItems) L.Tip += n >= 41 ? " アイテムは「もし 足元にアイテム なら ひろう」で集める。" : " アイテムのあるマスに着いたら「ひろう」。";
        }

        // ------------------------------------------------------------------
        // ステージ番号 → 問題の形（テンプレート番号）
        // ------------------------------------------------------------------

        /// <summary>
        /// ステージ1〜100で使う問題の形。数字は Build の中の「形の番号」です。
        /// 101〜120 は変数の章の問題。並びかえると、ステージの順番を変えられます。
        /// </summary>
        public static readonly int[] StageTemplates =
        {
            // 第1章 順次処理（ステージ1〜12）
            1, 2, 3, 4, 5, 7, 8, 9, 11, 12, 14, 15,
            // 第2章 くり返し（13〜24）：くり返しは短めにして、早めに条件分岐へ
            16, 17, 18, 20, 22, 23, 26, 28, 31, 32, 35, 40,
            // 第3章 条件分岐（25〜42）
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 53, 54, 55, 56, 58, 59, 60,
            // 第4章 〜までくり返す（43〜60）
            61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 75, 77, 78, 79, 80,
            // 第5章 変数（61〜80）
            101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120,
            // 第6章 総合（81〜100）
            81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100,
        };

        public static int TemplateOf(int stage) => StageTemplates[Math.Max(1, Math.Min(StageTemplates.Length, stage)) - 1];

        // ------------------------------------------------------------------
        // 第5章 変数（形の番号 101〜120）：看板の数を読んで進む
        // ------------------------------------------------------------------

        private struct Seg
        {
            public int Len;     // すすむマスの数
            public bool Sign;   // この区間のスタートに看板を置くか（看板の数 = Len）
            public int Turn;    // 区間のあとで向きを変える：+1 左、-1 右、0 なし
            public Seg(int len, bool sign, int turn) { Len = len; Sign = sign; Turn = turn; }
        }

        private static Node ReadN() => Node.Assign("n", Expr.Of(Term.Read()));
        private static Node ForN(params Node[] body) => Node.RepeatBy(Expr.Of(Term.Var("n")), body);
        private static Node AddN(int d) => Node.Assign("n", Expr.Calc(Term.Var("n"), d >= 0 ? '+' : '-', Term.Num(Math.Abs(d))));

        /// <summary>
        /// 変数の問題。広い部屋（かべが遠い）なので「前がかべ」では曲がる場所が分からず、
        /// 看板の数を変数に入れて、その数だけ進む必要がある。テストごとに看板の数が変わる。
        /// </summary>
        private static bool BuildVariable(Level L, Random rng)
        {
            int t = L.Template;
            bool obstacles = t == 107 || t >= 117;
            Node[] step = obstacles ? new[] { IfObstacle(), F() } : new[] { F() };
            List<Node> sol;
            Func<List<Seg>> segs;

            switch (t)
            {
                case 101: // 看板の数だけ進んで、左へ2マス
                case 102:
                    sol = Prog(ReadN(), ForN(step), TL(), F(), F(), UseIt());
                    segs = () => new List<Seg> { new Seg(rng.Next(2, 8), true, +1), new Seg(2, false, 0) };
                    break;
                case 103: // 看板を2回読む
                    sol = Prog(ReadN(), ForN(step), TL(), ReadN(), ForN(step), UseIt());
                    segs = () => new List<Seg> { new Seg(rng.Next(2, 7), true, +1), new Seg(rng.Next(2, 6), true, 0) };
                    break;
                case 104: // 看板を3回読む（右・左）
                    sol = Prog(ReadN(), ForN(step), TR(), ReadN(), ForN(step), TL(), ReadN(), ForN(step), UseIt());
                    segs = () => new List<Seg> { new Seg(rng.Next(2, 6), true, -1), new Seg(rng.Next(2, 5), true, +1), new Seg(rng.Next(2, 6), true, 0) };
                    break;
                case 105: // 「読む → 進む → 曲がる」をくり返す
                case 106:
                case 107:
                {
                    int k = t == 105 ? 3 : 4;
                    sol = Prog(Rep(k, ReadN(), ForN(step), TL()), UseIt());
                    segs = () =>
                    {
                        var list = new List<Seg>();
                        for (int i = 0; i < k; i++) list.Add(new Seg(rng.Next(2, 6), true, +1));
                        return list;
                    };
                    break;
                }
                case 108: // n を 1 ずつへらす（うずまき）
                case 109:
                case 110:
                    sol = Prog(ReadN(), Rep(3, ForN(step), TL(), AddN(-1)), UseIt());
                    segs = () =>
                    {
                        int s = rng.Next(3, 7);
                        return new List<Seg> { new Seg(s, true, +1), new Seg(s - 1, false, +1), new Seg(s - 2, false, +1) };
                    };
                    break;
                case 111: // n を 1 ずつふやす（外へ広がるうずまき）
                case 112:
                case 113:
                    sol = Prog(ReadN(), Rep(4, ForN(step), TL(), AddN(+1)), UseIt());
                    segs = () =>
                    {
                        int s = rng.Next(1, 4);
                        return new List<Seg> { new Seg(s, true, +1), new Seg(s + 1, false, +1), new Seg(s + 2, false, +1), new Seg(s + 3, false, +1) };
                    };
                    break;
                default: // 114〜120：n が 0 より大きいあいだ くり返す（区間の数もテストごとにちがう）
                    sol = Prog(ReadN(), Node.WhileCmp(new Compare { Left = Expr.Of(Term.Var("n")), Op = ">", Right = Expr.Num(0) },
                        ForN(step), TL(), AddN(-1)), UseIt());
                    segs = () =>
                    {
                        int s = rng.Next(2, 7);
                        var list = new List<Seg>();
                        for (int len = s; len >= 1; len--) list.Add(new Seg(len, len == s, +1));
                        return list;
                    };
                    break;
            }

            L.Solution = sol;
            var used = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                GridMap map = null;
                for (int tries = 0; tries < 80 && map == null; tries++)
                {
                    var list = segs();
                    string key = string.Join(",", list.ConvertAll(x => x.Len));
                    if (used.Contains(key)) continue; // 3つのテストは看板の数がちがうようにする
                    map = OpenRoom(list, obstacles ? rng : null);
                    if (map != null) used.Add(key);
                }
                if (map == null) return false;
                L.Tests.Add(map);
            }
            L.MaxBlocks = ProgramText.Count(sol) + 2;
            return true;
        }

        /// <summary>区間どおりに進む道を作り、まわりを広い部屋（かべから1マスはなれる）にする</summary>
        private static GridMap OpenRoom(List<Seg> segs, Random obstacleRng)
        {
            var pos = new P(0, 0);
            int dir = 0;
            var path = new List<P> { pos };
            var visited = new HashSet<P> { pos };
            var signs = new Dictionary<P, int>();
            foreach (var sg in segs)
            {
                if (sg.Len <= 0) return null;
                if (sg.Sign) signs[pos] = sg.Len;
                for (int k = 0; k < sg.Len; k++)
                {
                    pos = pos + GridMap.Dirs[dir];
                    if (!visited.Add(pos)) return null;
                    path.Add(pos);
                }
                if (sg.Turn > 0) dir = (dir + 1) % 4;
                else if (sg.Turn < 0) dir = (dir + 3) % 4;
            }

            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var p in path)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            const int pad = 2; // かべ1マス + 部屋のよゆう1マス
            int w = maxX - minX + 1 + pad * 2, h = maxY - minY + 1 + pad * 2;
            if (w > MaxW || h > MaxH) return null;

            var map = new GridMap(w, h);
            var off = new P(pad - minX, pad - minY);
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                    map.Set(new P(x, y), Cell.Room);
            foreach (var p in path) map.Set(p + off, Cell.Floor);
            foreach (var kv in signs) map.SetSign(kv.Key + off, kv.Value);
            map.Start = path[0] + off;
            map.StartDir = 0;
            map.Goal = pos + off;
            map.Set(map.Goal, Cell.Goal);

            if (obstacleRng != null)
            {
                // 看板のマス・スタート・ゴール以外の道に、じゃまものを置く（最低1つ）
                var cells = new List<P>();
                for (int i = 1; i < path.Count - 1; i++)
                    if (!signs.ContainsKey(path[i])) cells.Add(path[i] + off);
                foreach (var c in cells) if (obstacleRng.NextDouble() < 0.25) map.Set(c, Cell.Obstacle);
                if (map.CountObstacles() == 0 && cells.Count > 0) map.Set(cells[obstacleRng.Next(cells.Count)], Cell.Obstacle);
            }
            return map;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;

        // ------------------------------------------------------------------
        // アイテム（ひろう）を置く
        // ------------------------------------------------------------------

        private static Node PickCmd() => Node.Cmd(CommandType.Pick);
        private static Node IfItem() => Node.If(ConditionType.OnItem, false, new[] { PickCmd() });

        /// <summary>
        /// テーマにアイテムがあれば、橋にアイテムを置いて、お手本にも「ひろう」を足す。
        /// ・第1章（順次処理）: ランダムなマスに置き、着いた所で「ひろう」
        /// ・第2章（くり返し）: くり返しの中の最後の「すすむ」の後に「ひろう」（もように置かれる）
        /// ・第3章から: テストごとにランダムに置き、くり返しの中で「もし 足元にアイテム なら ひろう」
        /// </summary>
        private static bool AddItems(Level L, Random rng)
        {
            if (!L.Theme.HasItems) return true;
            int n = L.Template;

            if (n <= 15)
            {
                var map = L.Tests[0];
                var candidates = new List<P>();
                P pos = map.Start;
                int dir = map.StartDir;
                foreach (var node in L.Solution)
                {
                    if (node.Command == CommandType.Forward)
                    {
                        pos = pos + GridMap.Dirs[dir];
                        if (pos != map.Goal && map.Get(pos) == Cell.Floor) candidates.Add(pos);
                    }
                    else if (node.Command == CommandType.TurnLeft) dir = (dir + 1) % 4;
                    else if (node.Command == CommandType.TurnRight) dir = (dir + 3) % 4;
                }
                if (candidates.Count == 0) return false;
                int k = Clamp(candidates.Count / 3, 1, 4);
                var chosen = new HashSet<P>();
                while (chosen.Count < k) chosen.Add(candidates[rng.Next(candidates.Count)]);
                foreach (var p in chosen) map.SetItem(p, true);

                var sol = new List<Node>();
                pos = map.Start;
                dir = map.StartDir;
                foreach (var node in L.Solution)
                {
                    sol.Add(node);
                    if (node.Command == CommandType.Forward)
                    {
                        pos = pos + GridMap.Dirs[dir];
                        if (chosen.Contains(pos)) sol.Add(PickCmd());
                    }
                    else if (node.Command == CommandType.TurnLeft) dir = (dir + 1) % 4;
                    else if (node.Command == CommandType.TurnRight) dir = (dir + 3) % 4;
                }
                L.Solution = sol;
                return true;
            }

            if (n <= 40)
            {
                Node rep = L.Solution.Find(x => x.Kind == NodeKind.Repeat);
                if (rep == null || !InsertAfterLastForward(rep.Body)) return false;
                var map = Carve(L.Solution, true);
                if (map == null) return false;
                L.Tests[0] = map;
                L.MaxBlocks = ProgramText.Count(L.Solution) + 1;
                return true;
            }

            foreach (var map in L.Tests)
            {
                var cells = new List<P>();
                for (int y = 0; y < map.H; y++)
                    for (int x = 0; x < map.W; x++)
                    {
                        var p = new P(x, y);
                        if (map.Get(p) == Cell.Floor && p != map.Start && map.SignAt(p) == 0) cells.Add(p);
                    }
                if (cells.Count == 0) return false;
                foreach (var p in cells) if (rng.NextDouble() < 0.25) map.SetItem(p, true);
                if (map.CountItems() == 0) map.SetItem(cells[rng.Next(cells.Count)], true);
            }
            InsertItemChecks(L.Solution);
            L.MaxBlocks = ProgramText.Count(L.Solution) + 2;
            return true;
        }

        /// <summary>いちばん最後の「すすむ」のすぐ後に「ひろう」を入れる</summary>
        private static bool InsertAfterLastForward(List<Node> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var node = list[i];
                if (node.Kind == NodeKind.Command && node.Command == CommandType.Forward)
                {
                    list.Insert(i + 1, PickCmd());
                    return true;
                }
                if (node.IsContainer && InsertAfterLastForward(node.Body)) return true;
            }
            return false;
        }

        private static bool ContainsForward(List<Node> list)
        {
            foreach (var node in list)
                if ((node.Kind == NodeKind.Command && node.Command == CommandType.Forward) || ContainsForward(node.Body) || ContainsForward(node.Else))
                    return true;
            return false;
        }

        private static bool IsLoop(Node node) => node.Kind == NodeKind.Repeat || node.Kind == NodeKind.While;

        private static bool HasLoopWithForward(List<Node> list)
        {
            foreach (var node in list)
            {
                if (IsLoop(node) && ContainsForward(node.Body)) return true;
                if (HasLoopWithForward(node.Body) || HasLoopWithForward(node.Else)) return true;
            }
            return false;
        }

        /// <summary>「すすむ」をくり返す、いちばん内側のくり返しの先頭に「もし 足元にアイテム なら ひろう」を入れる</summary>
        private static void InsertItemChecks(List<Node> list)
        {
            foreach (var node in list)
            {
                if (IsLoop(node))
                {
                    if (HasLoopWithForward(node.Body)) InsertItemChecks(node.Body);
                    else if (ContainsForward(node.Body)) node.Body.Insert(0, IfItem());
                }
                else if (node.Kind == NodeKind.If)
                {
                    InsertItemChecks(node.Body);
                    InsertItemChecks(node.Else);
                }
                else if (node.Kind == NodeKind.Define && ContainsForward(node.Body))
                {
                    // 関数の中で進むときは、関数の最初で足元を調べる（step_forward の中で ひろう）
                    node.Body.Insert(0, IfItem());
                }
            }
        }
    }
}
