using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>
    /// 旅人のクイズ：「プログラムをえらぶ」問題。
    /// 正しいプログラム1つと、まちがったプログラム2つから えらぶ。
    /// プログラムは Node（ブロックと Python の共通の形）で作るので、
    /// ワードブロックの人にはブロックの形で、Python の人には Python のコードで見せる。
    /// </summary>
    public class ProgramQuestion
    {
        public int Chapter;          // 何章の問題か
        public bool Advanced;        // 応用問題
        public string Question;
        public List<Node> Correct;
        public List<Node> Wrong1, Wrong2;
        public string Explain;
    }

    public static class AreaQuizPrograms
    {
        // ----- プログラムを短く書くための道具 -----
        private static Node F => Node.Cmd(CommandType.Forward);
        private static Node L => Node.Cmd(CommandType.TurnLeft);
        private static Node R => Node.Cmd(CommandType.TurnRight);
        private static Node M => Node.Cmd(CommandType.Magic);
        private static Node P => Node.Cmd(CommandType.Pick);
        private static Node U => Node.Cmd(CommandType.Use);
        private static List<Node> S(params Node[] nodes) => new List<Node>(nodes);
        private static Node Rep(int n, params Node[] body) => Node.Repeat(n, body);
        private static Node If(ConditionType c, params Node[] body) => Node.If(c, false, body);
        private static Node IfNot(ConditionType c, params Node[] body) => Node.If(c, true, body);
        private static Node IfElse(ConditionType c, Node[] body, Node[] elseBody) => Node.If(c, false, body, elseBody);
        private static Node Until(ConditionType c, params Node[] body) => Node.While(c, true, body);   // 〈c〉までくり返す
        private static Node WhileIs(ConditionType c, params Node[] body) => Node.While(c, false, body); // 〈c〉あいだくり返す
        private static Node[] A(params Node[] nodes) => nodes;
        private static Node SetN(int v) => Node.Assign("n", Expr.Num(v));
        private static Node ReadN => Node.Assign("n", Expr.Of(Term.Read()));
        private static Node AddN(int d) => Node.Assign("n", Expr.Calc(Term.Var("n"), d >= 0 ? '+' : '-', Term.Num(System.Math.Abs(d))));
        private static Node RepN(params Node[] body) => Node.RepeatBy(Expr.Of(Term.Var("n")), body);
        private static Node WhileN(string op, int v, params Node[] body) =>
            Node.WhileCmp(new Compare { Left = Expr.Of(Term.Var("n")), Op = op, Right = Expr.Num(v) }, body);
        private static Node Def(string name, params Node[] body) => Node.Define(name, body);
        private static Node Call(string name) => Node.Call(name);

        private const ConditionType Obstacle = ConditionType.IsObstacle, Wall = ConditionType.IsBlocked, Goal = ConditionType.AtGoal,
            RightOpen = ConditionType.RightOpen, OnItem = ConditionType.OnItem;

        private static ProgramQuestion Q(int ch, bool adv, string q, List<Node> ok, List<Node> w1, List<Node> w2, string explain) =>
            new ProgramQuestion { Chapter = ch, Advanced = adv, Question = q, Correct = ok, Wrong1 = w1, Wrong2 = w2, Explain = explain };

        public static readonly ProgramQuestion[] All =
        {
            // ===== 第1章 順次処理 =====
            Q(1, false, "2マス進んでから、左を向くプログラムはどれ？",
                S(F, F, L), S(L, F, F), S(F, L, F),
                "プログラムは上から順に1つずつ動く。「進む・進む・左を向く」の順に ならべよう。"),
            Q(1, false, "目の前の岩を消してから、1マス進むプログラムはどれ？",
                S(M, F), S(F, M), S(F, F),
                "岩があるまま進もうとすると ぶつかってしまう。先に まほうで消してから進む。"),
            Q(1, false, "1マス進んで、足元のアイテムをひろうプログラムはどれ？",
                S(F, P), S(P, F), S(F, U),
                "アイテムのあるマスに着いてから ひろう。ひろうのと つかうのは ちがう命令だよ。"),
            Q(1, false, "右を向いてから、2マス進むプログラムはどれ？",
                S(R, F, F), S(F, F, R), S(L, F, F),
                "向きを変えるのが先。右と左を まちがえないように気をつけよう。"),
            Q(1, true, "ゴーレムの向きを、うしろ向き（反対）にするプログラムはどれ？",
                S(R, R), S(R, L), S(R),
                "右を2回向くと、ちょうど反対を向く。右と左を1回ずつだと、元の向きにもどってしまう。"),
            Q(1, true, "2マス進んで右へ曲がり、1マス進んでゴールの宝箱を開けるプログラムはどれ？",
                S(F, F, R, F, U), S(F, F, L, F, U), S(F, F, R, U, F),
                "動きを1つずつ順番に書く。宝箱を開ける（つかう）のは、ゴールに着いてから。"),
            Q(1, true, "1マス進むと、目の前に穴がある。穴を消して、そのあと2マス進むプログラムはどれ？",
                S(F, M, F, F), S(F, F, M, F), S(M, F, F, F),
                "穴の前まで来てから まほう。早すぎても おそすぎても うまくいかないよ。"),

            // ===== 第2章 くり返し =====
            Q(2, false, "4マス進むプログラムで、いちばん短く正しいのはどれ？",
                S(Rep(4, F)), S(Rep(4, F, F)), S(Rep(3, F)),
                "4回くり返す の中に 進む を1つ。中に2つ入れると、4×2＝8マス進んでしまう。"),
            Q(2, false, "「1マス進んで右を向く」を3回するプログラムはどれ？",
                S(Rep(3, F, R)), S(Rep(3, F), R), S(Rep(3, R, F)),
                "くり返したいものは、全部くり返しの中に入れる。中の順番も大事だよ。"),
            Q(2, false, "ちょうど6マス進むプログラムはどれ？",
                S(Rep(3, F, F)), S(Rep(6, F, F)), S(Rep(3, F), F, F),
                "3回くり返す の中に 進む が2つ → 3×2＝6マス。くり返す数 × 中の数 で数えよう。"),
            Q(2, false, "「2マス進んで まほうを使う」を2回するプログラムはどれ？",
                S(Rep(2, F, F, M)), S(Rep(2, F, F), M), S(Rep(2, F), F, M),
                "まほうも くり返したいので、くり返しの中に入れる。外に出すと最後に1回だけになる。"),
            Q(2, true, "「進む・左を向く・進む・右を向く」を3回くり返して、階段のように進むプログラムはどれ？",
                S(Rep(3, F, L, F, R)), S(Rep(3, F, L, F, L)), S(Rep(3, F, L), F, R),
                "階段は 左と右を 交ごに向く。4つの動き全部を くり返しの中に入れよう。"),
            Q(2, true, "1辺3マスの四角形をぐるっと歩く（3マス進んで右を向く、を4回）プログラムはどれ？",
                S(Rep(4, Rep(3, F), R)), S(Rep(4, Rep(3, F, R))), S(Rep(3, Rep(4, F), R)),
                "くり返しの中に くり返し（入れ子）。中は「3マス進む」、外は「それと右を向くのを4回」。"),
            Q(2, true, "「2マス進んでアイテムをひろう」を3回したあと、ゴールでつかうプログラムはどれ？",
                S(Rep(3, F, F, P), U), S(Rep(3, F, F, P, U)), S(Rep(3, F, P), U),
                "つかうのは最後に1回だけなので、くり返しの外（下）に書く。"),

            // ===== 第3章 条件分岐 =====
            Q(3, false, "前に じゃまものがあるときだけ、まほうを使うプログラムはどれ？",
                S(If(Obstacle, M)), S(IfNot(Obstacle, M)), S(If(Goal, M)),
                "もし〈前にじゃまもの〉なら まほう。じゃまものが「ない」ときに使っても意味がない。"),
            Q(3, false, "足元にアイテムがあったら、ひろうプログラムはどれ？",
                S(If(OnItem, P)), S(If(OnItem, U)), S(If(Goal, P)),
                "調べるのは〈足元にアイテム〉、することは ひろう。"),
            Q(3, false, "前がかべなら右を向き、でなければ進むプログラムはどれ？",
                S(IfElse(Wall, A(R), A(F))), S(IfElse(Wall, A(F), A(R))), S(IfElse(Obstacle, A(R), A(F))),
                "もし〜なら の中は「正しいとき」、でなければ の中は「正しくないとき」に動く。"),
            Q(3, false, "1マス先がゴール。もう一度するたびに、あいだに じゃまものが あったり なかったりする。どちらでもゴールできるのは？",
                S(If(Obstacle, M), F), S(F), S(If(Obstacle, M, F)),
                "じゃまものがあるときだけ消して、そのあと「いつも」進む。進むを もしの中に入れると、じゃまものがないとき進まない。"),
            Q(3, true, "5マスの道のどこかに じゃまものがある。1マスずつ調べながら進むプログラムはどれ？",
                S(Rep(5, If(Obstacle, M), F)), S(Rep(5, F, If(Obstacle, M))), S(If(Obstacle, M), Rep(5, F)),
                "毎回「調べてから進む」をくり返す。調べるのが1回だけだと、とちゅうの じゃまものに気づけない。"),
            Q(3, true, "右が通れたら右へ、でなければ左へ曲がってから、1マス進むプログラムはどれ？",
                S(IfElse(RightOpen, A(R), A(L)), F), S(IfElse(RightOpen, A(L), A(R)), F), S(IfElse(Wall, A(R), A(L)), F),
                "〈右が通れる〉が正しいなら右。曲がり終わってから進むので、進むは もしの外。"),
            Q(3, true, "3マス進むとちゅう、アイテムがあるマスでだけ ひろうプログラムはどれ？",
                S(Rep(3, F, If(OnItem, P))), S(Rep(3, F, P)), S(Rep(3, If(OnItem, P))),
                "アイテムがないマスで ひろうとエラー。進むのを わすれると、ずっと同じマスのまま。"),

            // ===== 第4章 〜までくり返す =====
            Q(4, false, "長さの分からない道を、ゴールに着くまで進むプログラムはどれ？",
                S(Until(Goal, F)), S(Rep(5, F)), S(WhileIs(Goal, F)),
                "回数が分からないときは「〜までくり返す」。〈ゴールに着いた〉あいだ だと、最初から動かない。"),
            Q(4, false, "ゴールに着くまで進んで、着いたら宝箱を開けるプログラムはどれ？",
                S(Until(Goal, F), U), S(Until(Goal, F, U)), S(U, Until(Goal, F)),
                "つかうのは着いてから1回だけ。くり返しのあと（下）に書こう。"),
            Q(4, false, "前がかべになるまで進むプログラムはどれ？",
                S(Until(Wall, F)), S(WhileIs(Wall, F)), S(Until(Wall, R)),
                "〈前がかべ〉までくり返す の中で進む。向きを変えるだけだと、いつまでも進まない。"),
            Q(4, false, "ゴールまで、じゃまものを消しながら進むプログラムはどれ？",
                S(Until(Goal, If(Obstacle, M), F)), S(Until(Goal, F)), S(Until(Obstacle, F)),
                "くり返しの中で「もし」を使うと、毎回 調べられる。"),
            Q(4, true, "かべにそって進む。前がかべなら右を向き、でなければ進む、をゴールまで続けるプログラムはどれ？",
                S(Until(Goal, IfElse(Wall, A(R), A(F)))), S(Until(Goal, R, F)), S(IfElse(Wall, A(R), A(F))),
                "「〜までくり返す」の中に「もし〜なら／でなければ」。1回だけでは ゴールまで行けない。"),
            Q(4, true, "ゴールまで進みながら、アイテムがあるマスでは ひろうプログラムはどれ？",
                S(Until(Goal, F, If(OnItem, P))), S(Until(Goal, F), P), S(Until(Goal, P, F)),
                "毎回「進む → もしアイテムがあれば ひろう」。調べずに ひろうと、ないマスでエラーになる。"),
            Q(4, true, "右が通れるようになるまで進んで、右を向くプログラムはどれ？",
                S(Until(RightOpen, F), R), S(WhileIs(RightOpen, F), R), S(Rep(3, F), R),
                "〈右が通れる〉までくり返す。道の長さが分からなくても、曲がり角で ちゃんと止まれる。"),

            // ===== 第5章 変数 =====
            Q(5, false, "看板に書いてある数だけ進むプログラムはどれ？",
                S(ReadN, RepN(F)), S(ReadN, Rep(3, F)), S(RepN(F), ReadN),
                "先に看板を読んで n に入れてから、n 回くり返す。順番が逆だと、n が まだ決まっていない。"),
            Q(5, false, "n を 4 にして、n マス進むプログラムはどれ？",
                S(SetN(4), RepN(F)), S(SetN(4), F), S(SetN(2), RepN(F)),
                "変数 n は数を入れておく箱。n 回くり返す と書くと、中の数（4）だけくり返す。"),
            Q(5, false, "n を 3 にして、n が 0 より大きいあいだ「進んで n を1へらす」プログラムはどれ？",
                S(SetN(3), WhileN(">", 0, F, AddN(-1))), S(SetN(3), WhileN(">", 0, F)), S(SetN(3), WhileN(">", 0, F, AddN(1))),
                "n を へらさないと、いつまでも 0 より大きいまま終わらない。ふやすと もっと終わらない！"),
            Q(5, false, "看板の数より1マス多く進むプログラムはどれ？",
                S(ReadN, AddN(1), RepN(F)), S(ReadN, AddN(-1), RepN(F)), S(ReadN, RepN(F)),
                "n を読んでから n を 1 ずつ変える（＋1）。計算してから くり返しに使う。"),
            Q(5, true, "看板の数だけ進み、右を向いて、もう一度 同じ数だけ進むプログラムはどれ？",
                S(ReadN, RepN(F), R, RepN(F)), S(ReadN, RepN(F, R)), S(ReadN, RepN(F), R, ReadN, RepN(F)),
                "n は 一度入れたら、何回でも使える。着いた先で もう一度読むと、ちがう看板の数になってしまう。"),
            Q(5, true, "1マス・2マス・3マスと だんだん長く進み、そのたびに右を向く（うずまき）プログラムはどれ？",
                S(SetN(1), Rep(3, RepN(F), R, AddN(1))), S(SetN(1), Rep(3, RepN(F), R)), S(SetN(1), Rep(3, AddN(1), RepN(F), R)),
                "進んだあとに n を ＋1。先に ふやすと 2・3・4マスになってしまう。ふやさないと ずっと1マス。"),
            Q(5, true, "ゴールまで何マス進んだか、n に数えるプログラムはどれ？",
                S(SetN(0), Until(Goal, F, AddN(1))), S(Until(Goal, F, AddN(1))), S(SetN(0), Until(Goal, F), AddN(1)),
                "最初に n を 0 にしてから、1マス進むたびに ＋1。くり返しの外で ふやすと1回しか数えない。"),

            // ===== 第6章 関数 =====
            Q(6, false, "「右を向いて2マス進む」を関数 turn_corner にして、2回使うプログラムはどれ？",
                S(Def("turn_corner", R, F, F), Call("turn_corner"), Call("turn_corner")),
                S(Def("turn_corner", R, F, F)),
                S(Call("turn_corner"), Call("turn_corner"), Def("turn_corner", R, F, F)),
                "関数は作っただけでは動かない。作ってから（上）、その下で使う。"),
            Q(6, false, "2マス進む関数 step2 を作って、くり返しで3回使い、6マス進むプログラムはどれ？",
                S(Def("step2", F, F), Rep(3, Call("step2"))), S(Def("step2", F, F), Call("step2")), S(Def("step2", F, F, F), Rep(3, Call("step2"))),
                "関数の中身（2マス）× 使った回数（3回）＝ 6マス。"),
            Q(6, false, "「じゃまものがあれば消して進む」関数 clear_path を、4回使うプログラムはどれ？",
                S(Def("clear_path", If(Obstacle, M), F), Rep(4, Call("clear_path"))),
                S(Def("clear_path", F, If(Obstacle, M)), Rep(4, Call("clear_path"))),
                S(Def("clear_path", If(Obstacle, M), F), Call("clear_path")),
                "関数の中でも順番が大事。調べて消してから進む。そして4回使う。"),
            Q(6, false, "「進んで左を向く」を関数 go_left にまとめて、3回使うプログラムはどれ？",
                S(Def("go_left", F, L), Call("go_left"), Call("go_left"), Call("go_left")),
                S(Def("go_left", F, L), Call("go_left")),
                S(Def("go_left", F, R), Call("go_left"), Call("go_left"), Call("go_left")),
                "関数の名前と中身が合っているかも大事。go_left なら 左を向く。"),
            Q(6, true, "右が通れたら曲がる関数 turn_corner を作り、ゴールまで「turn_corner して進む」をくり返すプログラムはどれ？",
                S(Def("turn_corner", If(RightOpen, R)), Until(Goal, Call("turn_corner"), F)),
                S(Def("turn_corner", If(RightOpen, R)), Until(Goal, F)),
                S(Def("turn_corner", R), Until(Goal, Call("turn_corner"), F)),
                "関数の中に「もし」を入れて、くり返しの中で使う。使わなければ曲がれないし、もしがないと いつも曲がる。"),
            Q(6, true, "1辺2マスの四角形を歩く関数 square を作って、2回使うプログラムはどれ？",
                S(Def("square", Rep(4, F, F, R)), Call("square"), Call("square")),
                S(Def("square", Rep(4, F, R)), Call("square"), Call("square")),
                S(Def("square", Rep(2, F, F, R)), Call("square"), Call("square")),
                "四角形は「2マス進んで右を向く」を4回。2回だと半分しか歩かない。"),
            Q(6, true, "「アイテムがあれば ひろって進む」を3回する関数 pick_line を作り、使ってから右を向いて、もう一度使うプログラムはどれ？",
                S(Def("pick_line", Rep(3, If(OnItem, P), F)), Call("pick_line"), R, Call("pick_line")),
                S(Def("pick_line", Rep(3, If(OnItem, P), F)), R, Call("pick_line")),
                S(Def("pick_line", If(OnItem, P), F), Call("pick_line"), R, Call("pick_line")),
                "関数の中に くり返しを入れられる。作った関数は、何回でも 好きなところで使える。"),
        };

        /// <summary>その章の問題（advanced なら応用問題）</summary>
        public static List<ProgramQuestion> Of(int chapter, bool advanced)
        {
            var list = new List<ProgramQuestion>();
            foreach (var q in All) if (q.Chapter == chapter && q.Advanced == advanced) list.Add(q);
            return list;
        }
    }
}
