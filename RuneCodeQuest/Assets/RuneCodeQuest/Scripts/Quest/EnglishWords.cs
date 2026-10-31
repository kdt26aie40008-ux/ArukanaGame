using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>英語のことば 1つ（クエストの Python に出てくる 英単語）</summary>
    public class EnglishWord
    {
        public string Word;     // forward
        public string Reading;  // フォワード
        public string Meaning;  // 前へ
        public string Note;     // プログラムでの 使い方
    }

    /// <summary>
    /// クエストの Python に出てくる 英語のことばの表。
    /// use_magic のような名前は「use」「magic」に分けて、1つずつ 英単語として 覚えられるようにする。
    /// 見たことばは Progress に記録して、かばんの「英語ことばカード」で 見返せる。
    /// </summary>
    public static class EnglishWords
    {
        private static EnglishWord W(string word, string reading, string meaning, string note) =>
            new EnglishWord { Word = word, Reading = reading, Meaning = meaning, Note = note };

        /// <summary>すべての ことば（カードの ならび順）</summary>
        public static readonly EnglishWord[] All =
        {
            // うごく
            W("forward", "フォワード", "前へ", "forward() … 前に1マス すすむ"),
            W("left", "レフト", "左", "left(90) … 左に 90度 まわる"),
            W("right", "ライト", "右", "right(90) … 右に 90度 まわる"),
            W("turn", "ターン", "まわる・曲がる", "turn_corner … かどを 曲がる"),
            W("corner", "コーナー", "かど", "turn_corner … かどを 曲がる"),
            // つかう・ひろう
            W("use", "ユーズ", "使う", "use_magic() … まほうを 使う"),
            W("magic", "マジック", "まほう", "use_magic() … まほうを 使う"),
            W("item", "アイテム", "道具・もの", "use_item() … ゴールのものを 使う"),
            W("pick", "ピック", "つまむ・とる", "pick_up() … ひろう（pick up で「ひろいあげる」）"),
            W("up", "アップ", "上へ", "pick_up() … ひろいあげる"),
            // 調べる
            W("obstacle", "オブスタクル", "じゃまもの", "obstacle_in_front() … 前に じゃまものが ある？"),
            W("in", "イン", "〜の中・〜に", "in_front … 前に／for i in range … 〜の中で"),
            W("front", "フロント", "前", "obstacle_in_front() … 前に じゃまもの"),
            W("wall", "ウォール", "かべ", "wall_in_front() … 前が かべ？"),
            W("at", "アット", "〜に（場所）", "at_goal() … ゴールに いる？"),
            W("goal", "ゴール", "ゴール・目的地", "at_goal() … ゴールに いる？"),
            W("is", "イズ", "〜である", "right_is_clear() … 右は 通れる？"),
            W("clear", "クリア", "ひらけている・何もない", "left_is_clear() … 左は 通れる？"),
            W("object", "オブジェクト", "もの", "object_here() … ここに ものが ある？"),
            W("here", "ヒア", "ここ", "object_here() … ここに ものが ある？"),
            // くり返し・もし
            W("for", "フォー", "〜のあいだ（くり返す）", "for i in range(3): … 3回 くり返す"),
            W("range", "レンジ", "はんい", "range(3) … 0, 1, 2 の 3つぶん"),
            W("if", "イフ", "もし", "if 〜: … もし 〜なら"),
            W("else", "エルス", "そうでなければ", "else: … そうでなければ"),
            W("while", "ワイル", "〜のあいだ", "while 〜: … 〜のあいだ くり返す"),
            W("not", "ノット", "〜でない", "while not at_goal(): … ゴールでないあいだ"),
            // 変数
            W("read", "リード", "読む", "read_sign() … 看板を 読む"),
            W("sign", "サイン", "看板・しるし", "read_sign() … 看板を 読む"),
            W("n", "エヌ", "数（number の 頭文字）", "n = read_sign() … 看板の数を n に 入れる"),
            W("i", "アイ", "回数（index の 頭文字）", "for i in range(3): … 何回目かを i で 数える"),
            // 関数
            W("def", "デフ", "決める・作る（define の 短い形）", "def turn_corner(): … 自分の命令を 作る"),
        };

        private static Dictionary<string, EnglishWord> byWord;

        public static EnglishWord Find(string w)
        {
            if (byWord == null)
            {
                byWord = new Dictionary<string, EnglishWord>();
                foreach (var e in All) byWord[e.Word] = e;
            }
            return byWord.TryGetValue(w, out var r) ? r : null;
        }

        /// <summary>文字の中の 英単語（表にあるもの）を、出てきた順に（同じものは1回）</summary>
        public static List<EnglishWord> InText(string text)
        {
            var list = new List<EnglishWord>();
            var seen = new HashSet<string>();
            var sb = new System.Text.StringBuilder();
            void Flush()
            {
                if (sb.Length == 0) return;
                var e = Find(sb.ToString());
                if (e != null && seen.Add(e.Word)) list.Add(e);
                sb.Clear();
            }
            foreach (char c in text + " ")
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) sb.Append(char.ToLowerInvariant(c));
                else Flush(); // _ や ( や 数字・空白で 区切る
            }
            return list;
        }

        /// <summary>そのクエストに 出てくる 英単語（使える命令・調べること・くり返しなど・お手本の答え）</summary>
        public static List<EnglishWord> ForLevel(Level level)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in level.Commands) sb.Append(CommandInfo.Get(c).PythonCode).Append(' ');
            foreach (var c in level.Conditions) sb.Append(ConditionInfo.Get(c).PythonCode).Append(' ');
            if (level.Structures.Contains(NodeKind.Repeat)) sb.Append("for i in range ");
            if (level.Structures.Contains(NodeKind.If)) sb.Append(level.AllowElse ? "if else " : "if ");
            if (level.Structures.Contains(NodeKind.While)) sb.Append("while not ");
            if (level.AllowVariables) sb.Append("n read_sign ");
            if (level.AllowFunctions) sb.Append("def turn_corner ");
            sb.Append(ProgramText.ToPython(level.Solution));
            return InText(sb.ToString());
        }
    }
}
