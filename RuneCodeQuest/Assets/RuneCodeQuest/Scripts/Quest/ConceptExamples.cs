using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>
    /// 新しいブロックが出てきたときの「ブロックの使い方」ページ（くわしい手順 ＋ ブロックの絵）。
    /// 説明の文と、お手本のプログラム（Example）を ならべて見せる。Before があると「まとめる前 → まとめたあと」
    /// </summary>
    public static class ConceptExamples
    {
        private static Node F() => Node.Cmd(CommandType.Forward);
        private static Node TL() => Node.Cmd(CommandType.TurnLeft);
        private static Node TR() => Node.Cmd(CommandType.TurnRight);
        private static Node Mag() => Node.Cmd(CommandType.Magic);
        private static Node Pick() => Node.Cmd(CommandType.Pick);
        private static Node Use() => Node.Cmd(CommandType.Use);
        private static Node Rep(int n, params Node[] body) => Node.Repeat(n, body);
        private static Node If(ConditionType c, Node[] body, Node[] els = null) => Node.If(c, false, body, els);
        private static Node Until(ConditionType c, params Node[] body) => Node.While(c, true, body);
        private static Node ReadN() => Node.Assign("n", Expr.Of(Term.Read()));
        private static Node ForN(params Node[] body) => Node.RepeatBy(Expr.Of(Term.Var("n")), body);
        private static Node AddN(int d) => Node.Assign("n", Expr.Calc(Term.Var("n"), d >= 0 ? '+' : '-', Term.Num(System.Math.Abs(d))));
        private static List<Node> P(params Node[] n) => new List<Node>(n);

        private static InfoPage Page(string title, string body, string caption, List<Node> example, List<Node> before = null) =>
            new InfoPage(title, body) { Caption = caption, Example = example, Before = before };

        /// <summary>そのコンセプト（名前）の「ブロックの使い方」ページ（なければ null）</summary>
        public static InfoPage For(Concept c)
        {
            if (c == null) return null;
            switch (c.Name)
            {
                case "順次処理":
                    return Page("ブロックの使い方：命令をならべる",
                        "① 左の置き場から ブロックをドラッグして、「▶ 起動したとき」の下に つなげる。\n" +
                        "② ゴーレムは いちばん上のブロックから、1つずつ 順番に動く。\n" +
                        "③ 右の絵では：2マス すすむ → 前のじゃまものを まほうで消す → 1マス すすむ。\n\n" +
                        "Python では、1行に 1つずつ 命令を書きます。上の行から 順番に動きます。",
                        "上から順番に 1つずつ", P(F(), F(), Mag(), F()));
                case "つかう":
                    return Page("ブロックの使い方：つかう",
                        "① ゴールのマスまで すすむ。\n" +
                        "② いちばん下に「つかう」を置く。\n\n" +
                        "ゴールに着く前に「つかう」をしても、何も起きません。\n" +
                        "「つかう」は いつも プログラムの いちばん最後です。",
                        "ゴールに着いてから つかう", P(F(), F(), F(), Use()));
                case "ひろう":
                    return Page("ブロックの使い方：ひろう",
                        "① アイテムのあるマスまで すすむ。\n" +
                        "② そこで「ひろう」。\n" +
                        "③ のこりの道を すすんで、ゴールで「つかう」。\n\n" +
                        "アイテムのないマスで「ひろう」をしても、何も ひろえません。\n" +
                        "何マス目に アイテムがあるか、数えてから置こう。",
                        "1マス目で ひろって、ゴールで つかう", P(F(), Pick(), F(), F(), Use()));
                case "向きを変える":
                    return Page("ブロックの使い方：向きを変える",
                        "「左を向く」「右を向く」は、その場で くるっと回るだけ。マスは 進みません。\n\n" +
                        "① 曲がり角まで すすむ。\n" +
                        "② 曲がりたい方を 向く（ゴーレムから見た 左・右）。\n" +
                        "③ 向いた方へ すすむ。\n\n" +
                        "Python では left(90)・right(90)。90 は「90度（直角）回る」という意味です。",
                        "2マス → 左を向く → 2マス", P(F(), F(), TL(), F(), F(), Use()));
                case "くり返し":
                    return Page("ブロックの使い方：○回くり返す",
                        "① 置き場から「○回くり返す」を置く。（コの字の形。口の中に ブロックが入る）\n" +
                        "② 口の中に「すすむ」を入れる。\n" +
                        "　 ブロックの上に 持っていくと、その下のブロックが ぜんぶ口の中に入る。\n" +
                        "③ −・＋ で 回数を 5 にする。\n" +
                        "④ 中の命令を 5回 くり返したら、口の下の「つかう」へ 進む。\n\n" +
                        "Python：for i in range(5): の下に、スペース4つ 字下げして 中身を書く。",
                        "すすむ 5こ → くり返し 1つ", P(Rep(5, F()), Use()), P(F(), F(), F(), F(), F(), Use()));
                case "くり返しの入れ子":
                    return Page("ブロックの使い方：くり返しの中のくり返し",
                        "くり返しの口の中に、もう1つ くり返しを入れられます。\n\n" +
                        "① 外がわの「4回くり返す」の中に、「3回くり返す（すすむ）」を入れる。\n" +
                        "② 外がわが 1回まわるたびに、中の くり返しは 3回 まわる。\n" +
                        "③ だから すすむ は 3 × 2 × 4 ＝ 24回 動くのに、ブロックは とても少ない。\n\n" +
                        "Python：中の for は、外の for より さらに スペース4つ 右に書く。",
                        "すすむ 3こ を くり返しに", P(Rep(4, Rep(3, F()), TL(), Rep(3, F()), TR()), Use()),
                        P(Rep(4, F(), F(), F(), TL(), F(), F(), F(), TR()), Use()));
                case "条件分岐":
                    return Page("ブロックの使い方：もし〜なら",
                        "① 置き場から「もし〜なら」を置く。（くり返しと同じ コの字の形）\n" +
                        "② 〈 〉のボタンを押して、調べることを「前にじゃまもの」にする。\n" +
                        "③ 口の中に「まほう」を入れる。\n" +
                        "④ 動くとき：じゃまものが あれば まほう。なければ 口の中を とばして 下へ。\n\n" +
                        "くり返しの中に入れると、1歩ごとに 調べてくれます。\n" +
                        "Python：if obstacle_in_front(): の下に 字下げして use_magic()",
                        "1歩ごとに じゃまものを調べる", P(Rep(5, If(ConditionType.IsObstacle, new[] { Mag() }), F()), Use()));
                case "足元を調べる":
                    return Page("ブロックの使い方：足元にアイテム",
                        "「もし〜なら」の〈 〉を「足元にアイテム」にすると、今いるマスに アイテムがあるか 調べられます。\n\n" +
                        "① 「もし〈足元にアイテム〉なら」の中に「ひろう」を入れる。\n" +
                        "② それを くり返しの中の「すすむ」の前に置く。\n" +
                        "③ どのマスに アイテムがあっても、ちゃんと ひろえる。\n\n" +
                        "Python：if object_here(): の下に pick_up()",
                        "アイテムがあるマスだけ ひろう", P(Rep(5, If(ConditionType.OnItem, new[] { Pick() }), F()), Use()));
                case "もし〜でなければ":
                    return Page("ブロックの使い方：もし〜なら／でなければ",
                        "「もし〜なら／でなければ」には 口が 2つ あります。\n\n" +
                        "・上の口：はい のとき（前がかべ）→ 左を向く\n" +
                        "・下の口（でなければ）：いいえ のとき → すすむ\n\n" +
                        "どちらか 1つだけが 動きます。両方 動くことは ありません。\n" +
                        "Python：if wall_in_front(): の下に left(90)、else: の下に forward()",
                        "かべなら 曲がる、でなければ すすむ",
                        P(Rep(10, If(ConditionType.IsBlocked, new[] { TL() }, new[] { F() })), Use()));
                case "〜までくり返す":
                    return Page("ブロックの使い方：〜までくり返す",
                        "① 置き場から「〜までくり返す」を置く。〈 〉は「ゴールに着いた」。\n" +
                        "② 口の中に「1歩ぶん」の動きを入れる（じゃまものを調べて → すすむ）。\n" +
                        "③ 回数は 決めなくていい。ゴールに着いたら くり返しが止まって、下の「つかう」へ。\n\n" +
                        "道が 5マスでも 9マスでも、同じプログラムで進めます。\n" +
                        "Python：while not at_goal():（ゴールに着いていない あいだ）",
                        "ゴールに着くまで 1歩ずつ",
                        P(Until(ConditionType.AtGoal, If(ConditionType.IsObstacle, new[] { Mag() }), F()), Use()));
                case "変数":
                    return Page("ブロックの使い方：変数 n",
                        "① 看板の上で「n を 看板の数 にする」。看板の数が 箱 n に入る。\n" +
                        "② 「くり返す」ブロックの 右はしの「変数」ボタンを押す。\n" +
                        "　 「○回くり返す」が「n 回くり返す」に かわる。（もう一度押すと m、もう一度で 数にもどる）\n" +
                        "③ 看板が 4 なら 4回、6 なら 6回 くり返す。\n\n" +
                        "回数を「4」のように 数で決めると、看板の数が かわったときに 失敗します。\n" +
                        "Python：n = read_sign() と for i in range(n):",
                        "看板を読んで、その数だけ すすむ", P(ReadN(), ForN(F()), Use()));
                case "変数の計算":
                    return Page("ブロックの使い方：n を 1 ずつ へらす",
                        "「n を -1 ずつ変える」は、今の n から 1 ひいて、もう一度 n に入れます。\n\n" +
                        "右の絵で 看板が 4 のとき：\n" +
                        "　1回目 4マス → 左 → n は 3\n" +
                        "　2回目 3マス → 左 → n は 2\n" +
                        "　3回目 2マス → 左 → n は 1\n" +
                        "うずまきのように、だんだん 短くなります。\n" +
                        "Python：n = n - 1",
                        "くり返すたびに 1マス 短く", P(ReadN(), Rep(3, ForN(F()), TL(), AddN(-1)), Use()));
                case "変数をくらべる":
                    return Page("ブロックの使い方：n > 0 のあいだ くり返す",
                        "「〈n > 0〉のあいだ くり返す」は、n が 0 より大きいあいだ くり返します。\n\n" +
                        "① 看板の数を n に入れる。\n" +
                        "② くり返しの中で すすむ → n を -1。\n" +
                        "③ n が 0 になったら 止まる。\n\n" +
                        "> は「より大きい」、< は「より小さい」。\n" +
                        "Python：while n > 0:",
                        "n が 0 になるまで すすむ",
                        P(ReadN(), Node.WhileCmp(new Compare { Left = Expr.Of(Term.Var("n")), Op = ">", Right = Expr.Num(0) }, F(), AddN(-1)), Use()));
                case "組み合わせ":
                    return Page("ブロックの使い方：もし の中に もし",
                        "「もし」の口の中に、もう1つ「もし〜なら／でなければ」を入れられます。\n\n" +
                        "・前がかべ のとき だけ、中の「もし」を調べる\n" +
                        "・右が通れる → 右を向く、でなければ → 左を向く\n" +
                        "・そのあと じゃまものを調べて すすむ\n\n" +
                        "これを ゴールに着くまで くり返せば、どんな道でも 進めます。",
                        "曲がる方向を 調べて決める",
                        P(Until(ConditionType.AtGoal,
                            If(ConditionType.IsBlocked, new[] { If(ConditionType.RightOpen, new[] { TR() }, new[] { TL() }) }),
                            If(ConditionType.IsObstacle, new[] { Mag() }), F()), Use()));
                case "関数":
                    return Page("ブロックの使い方：関数を作る・使う",
                        "① 「関数を作る」を置いて、名前をつける（例：turn_corner）。\n" +
                        "② 口の中に、まとめたい命令を入れる。\n" +
                        "③ すると「turn_corner をする」ブロックが 置き場に出る。それを使う。\n\n" +
                        "「関数を作る」は、いつも プログラムの いちばん上に 置かれます。\n" +
                        "作っただけでは動きません。「〜をする」で 使ったときに 中身が動きます。",
                        "曲がる動きを まとめて 名前をつける",
                        P(Node.Define("turn_corner", If(ConditionType.IsBlocked, new[] { If(ConditionType.RightOpen, new[] { TR() }, new[] { TL() }) })),
                          Until(ConditionType.AtGoal, Node.Call("turn_corner"), F()), Use()));
                default:
                    return null;
            }
        }
    }
}
