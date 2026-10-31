using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>旅人が出すクイズ1問</summary>
    public class QuizQuestion
    {
        public string Question;
        public string[] Choices;   // 3つ
        public int Answer;         // 正しい答えの番号（0〜2）
        public string Explain;     // 解説（まちがえたときも、正解したときも出す）
        public List<Node>[] Programs; // プログラムをえらぶ問題のとき：3つのプログラム（ブロックか Python のコードで見せる）
        public string Category;    // 今の章の問題・応用問題・ふりかえり問題
    }

    /// <summary>
    /// 遺跡のエリアにいる旅人のクイズ。その章で学ぶこと（順次・くり返し・条件分岐・while・変数・関数）から出す。
    /// ワードブロックを選んだ人には、答えがブロックの形のプログラムの問題（正しい1つ・まちがい2つ）を出す。
    /// Python を選んだ人には、Python のコードをえらぶ問題と、Python の言葉の問題を出す。
    /// 問題は「今の章」「応用」「ふりかえり（前の章）」の3種類。
    /// 正解すると金貨と薬草がもらえる（エリアごとに1回）。まちがえても、何回でも挑戦できる。
    /// </summary>
    public static class AreaQuiz
    {
        private static QuizQuestion Q(string q, string a, string b, string c, int answer, string explain) =>
            new QuizQuestion { Question = q, Choices = new[] { a, b, c }, Answer = answer, Explain = explain };

        // ----- Python の問題 -----
        private static readonly QuizQuestion[][] PythonByChapter =
        {
            new[] // 第1章 順次処理
            {
                Q("forward() を3回書いたら、ゴーレムは何マス進む？", "1マス", "3マス", "6マス", 1, "命令は上から順に1つずつ動く。forward() 1回で1マスなので、3回で3マス。"),
                Q("右を向いてから進みたい。正しい順番は？", "forward() → right(90)", "right(90) → forward()", "どちらでも同じ", 1, "プログラムは書いた順番に動く。先に向きを変えてから進もう。"),
                Q("ゴールで宝箱を開ける命令は？", "use_item()", "pick_up()", "use_magic()", 0, "ゴールにあるものを使うのは use_item()。pick_up() は足元のアイテムをひろう命令。"),
                Q("目の前の岩を消す命令は？", "forward()", "use_magic()", "left(90)", 1, "じゃまものは use_magic() で消してから進む。"),
            },
            new[] // 第2章 くり返し
            {
                Q("for i in range(4):\n    forward()\nで何マス進む？", "1マス", "4マス", "5マス", 1, "range(4) は4回くり返す。中の forward() が4回動くので4マス。"),
                Q("同じ命令が10回続くとき、いちばん短く書けるのは？", "10回書く", "for i in range(10): を使う", "while を使う", 1, "回数が決まっているくり返しは for i in range(10): でまとめると短くなる。"),
                Q("for の中身は、どう書く？", "左はしにそろえる", "スペース4つ分 字下げする", "() の中に書く", 1, "for の行の最後に「:」をつけて、次の行から字下げしたものが中身になる。"),
                Q("くり返しの中にくり返しを入れることを何という？", "入れ子", "変数", "関数", 0, "くり返しの中のくり返しは「入れ子（ネスト）」という。"),
            },
            new[] // 第3章 条件分岐
            {
                Q("if obstacle_in_front(): の下の命令は、いつ動く？", "いつも動く", "前にじゃまものがあるときだけ", "動かない", 1, "if は「もし〜なら」。条件が正しいときだけ、中身が動く。"),
                Q("「でなければ」を Python で書くと？", "else:", "elif:", "not:", 0, "if のあとに else: を書くと、条件が正しくないときに動く中身を書ける。"),
                Q("前がかべかどうかを調べる命令は？", "wall_in_front()", "at_goal()", "object_here()", 0, "wall_in_front() は、前がかべや川で進めないとき「はい（True）」になる。"),
                Q("じゃまものの場所が、もう一度するたびに変わる。いちばん大事なことは？", "今の場所だけ成功すればいい", "場所が変わっても成功するように、調べてから動く", "命令をたくさん書く", 1, "場所が変わっても大丈夫なように、決めうちせずに if で調べてから動こう。"),
            },
            new[] // 第4章 〜までくり返す（while）
            {
                Q("while not at_goal(): はいつまでくり返す？", "1回だけ", "ゴールに着くまで", "ずっと終わらない", 1, "not at_goal() は「ゴールに着いていない」。着いていないあいだ、くり返す。"),
                Q("長さが分からない道を進むのに向いているのは？", "for i in range(5):", "while not at_goal():", "if at_goal():", 1, "回数が分からないときは while で「〜まで」くり返す。"),
                Q("while の中で forward() を書き忘れると？", "すぐ終わる", "終わらないくり返しになる", "エラーにならない", 1, "ゴールに近づかないので、いつまでも終わらない。中で進むのをわすれないように。"),
                Q("for と while のちがいは？", "for は回数、while は条件でくり返す", "同じもの", "while は1回だけ", 0, "for は決まった回数、while は条件が正しいあいだくり返す。"),
            },
            new[] // 第5章 変数
            {
                Q("n = 3 のあと n = n - 1 をすると、n はいくつ？", "1", "2", "3", 1, "n - 1 は 3 - 1 = 2。計算した答えが、また n に入る。"),
                Q("n = read_sign() は何をする？", "看板の数を n に入れる", "看板をこわす", "n を消す", 0, "read_sign() は足元の看板の数を読む。その数が変数 n に入る。"),
                Q("for i in range(n): で n が 5 のとき、何回くり返す？", "1回", "5回", "n回とは決まらない", 1, "range(n) は n の中の数だけくり返す。n が 5 なら5回。"),
                Q("変数の名前として正しいのは？", "count", "for", "3n", 0, "for は Python のとくべつな言葉、数字から始まる名前も使えない。count はOK。"),
            },
            new[] // 第6章 総合・関数
            {
                Q("def turn_corner(): は何をしている？", "turn_corner という関数を作る", "すぐにゴーレムが動く", "エラーになる", 0, "def は関数（自分の命令）を作る書き方。作っただけでは動かず、turn_corner() で呼ぶと動く。"),
                Q("関数を使う行は、作る行の…", "上に書く", "下に書く", "どこでもいい", 1, "Python は上から順に読むので、def で作ってから、その下で使う。"),
                Q("右が通れるか調べる命令は？", "right_is_clear()", "right(90)", "wall_in_front()", 0, "right_is_clear() は右どなりのマスが道なら「はい」になる。right(90) は右を向く命令。"),
                Q("長いプログラムを関数に分けるといいことは？", "読みやすくなり、同じ動きを何回も使える", "速く動く", "命令が使えなくなる", 0, "意味のあるまとまりに名前をつけると、読みやすく、くり返し使える。"),
            },
        };

        /// <summary>
        /// そのステージのエリアの旅人が出すクイズ。
        /// ステージと挑戦した回数で「今の章・応用・ふりかえり」が順番に変わる（まちがえて もう一度話すと、ちがう問題になる）。
        /// </summary>
        public static QuizQuestion For(int stage, SolveMode mode, int attempt = 0)
        {
            int ch = System.Math.Max(1, System.Math.Min(PythonByChapter.Length, QuestDatabase.ChapterOf(stage).Number));
            int turn = stage + attempt;
            int kind = turn % 3;                // 0：今の章 1：応用 2：ふりかえり
            if (kind == 2 && ch == 1) kind = 0; // 第1章には ふりかえる章がない
            int pickCh = kind == 2 ? 1 + (stage / 3 + attempt) % (ch - 1) : ch;
            bool advanced = kind == 1;
            string category = kind == 0 ? $"今の章の問題（第{ch}章）" : kind == 1 ? $"応用問題（第{ch}章）" : $"ふりかえり問題（第{pickCh}章）";

            var programs = AreaQuizPrograms.Of(pickCh, advanced);
            int seed = stage / 3 + attempt;
            if (mode == SolveMode.Python && !advanced)
            {
                // Python：コードをえらぶ問題と、言葉の問題をまぜる
                var text = PythonByChapter[pickCh - 1];
                int i = seed % (programs.Count + text.Length);
                if (i >= programs.Count)
                {
                    var t = text[i - programs.Count];
                    return new QuizQuestion { Question = t.Question, Choices = t.Choices, Answer = t.Answer, Explain = t.Explain, Category = category };
                }
                return FromProgram(programs[i], mode, turn, category);
            }
            return FromProgram(programs[seed % programs.Count], mode, turn, category);
        }

        /// <summary>プログラムをえらぶ問題にする（正解の場所は ステージごとに変わる）</summary>
        private static QuizQuestion FromProgram(ProgramQuestion p, SolveMode mode, int turn, string category)
        {
            int answer = turn % 3;
            var order = new List<List<Node>> { p.Wrong1, p.Wrong2 };
            order.Insert(answer, p.Correct);
            var choices = new string[3];
            for (int i = 0; i < 3; i++)
                choices[i] = mode == SolveMode.Python ? ProgramText.ToPython(order[i]).TrimEnd() : string.Join(" / ", ProgramText.ToBlockLines(order[i]));
            return new QuizQuestion
            {
                Question = p.Question, Choices = choices, Answer = answer, Explain = p.Explain, Category = category,
                Programs = order.ToArray(),
            };
        }
    }
}
