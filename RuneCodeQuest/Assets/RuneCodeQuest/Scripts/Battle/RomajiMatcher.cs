using System.Collections.Generic;
using System.Text;

namespace RuneCodeQuest
{
    /// <summary>
    /// ひらがなの言葉を、ローマ字で正しく打てているか調べる係。
    /// 「し」は shi・si・ci、「ち」は chi・ti、「ん」は n・nn・xn、「っ」は kka・tch・xtu、「きゃ」は kya・kixya、
    /// のように、いろいろな打ち方を受けつけます（どちらにも読める打ち方は、両方を追いかけて あとの文字で決める）。日本語入力（IME）は使わず、ローマ字で直接打ちます。
    /// </summary>
    public class RomajiMatcher
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "あ", new[] { "a" } }, { "い", new[] { "i", "yi" } }, { "う", new[] { "u", "wu", "whu" } }, { "え", new[] { "e" } }, { "お", new[] { "o" } },
            { "か", new[] { "ka", "ca" } }, { "き", new[] { "ki" } }, { "く", new[] { "ku", "cu", "qu" } }, { "け", new[] { "ke" } }, { "こ", new[] { "ko", "co" } },
            { "さ", new[] { "sa" } }, { "し", new[] { "shi", "si", "ci" } }, { "す", new[] { "su" } }, { "せ", new[] { "se", "ce" } }, { "そ", new[] { "so" } },
            { "た", new[] { "ta" } }, { "ち", new[] { "chi", "ti" } }, { "つ", new[] { "tsu", "tu" } }, { "て", new[] { "te" } }, { "と", new[] { "to" } },
            { "な", new[] { "na" } }, { "に", new[] { "ni" } }, { "ぬ", new[] { "nu" } }, { "ね", new[] { "ne" } }, { "の", new[] { "no" } },
            { "は", new[] { "ha" } }, { "ひ", new[] { "hi" } }, { "ふ", new[] { "fu", "hu" } }, { "へ", new[] { "he" } }, { "ほ", new[] { "ho" } },
            { "ま", new[] { "ma" } }, { "み", new[] { "mi" } }, { "む", new[] { "mu" } }, { "め", new[] { "me" } }, { "も", new[] { "mo" } },
            { "や", new[] { "ya" } }, { "ゆ", new[] { "yu" } }, { "よ", new[] { "yo" } },
            { "ら", new[] { "ra" } }, { "り", new[] { "ri" } }, { "る", new[] { "ru" } }, { "れ", new[] { "re" } }, { "ろ", new[] { "ro" } },
            { "わ", new[] { "wa" } }, { "を", new[] { "wo" } },
            { "が", new[] { "ga" } }, { "ぎ", new[] { "gi" } }, { "ぐ", new[] { "gu" } }, { "げ", new[] { "ge" } }, { "ご", new[] { "go" } },
            { "ざ", new[] { "za" } }, { "じ", new[] { "ji", "zi" } }, { "ず", new[] { "zu" } }, { "ぜ", new[] { "ze" } }, { "ぞ", new[] { "zo" } },
            { "だ", new[] { "da" } }, { "ぢ", new[] { "di" } }, { "づ", new[] { "du" } }, { "で", new[] { "de" } }, { "ど", new[] { "do" } },
            { "ば", new[] { "ba" } }, { "び", new[] { "bi" } }, { "ぶ", new[] { "bu" } }, { "べ", new[] { "be" } }, { "ぼ", new[] { "bo" } },
            { "ぱ", new[] { "pa" } }, { "ぴ", new[] { "pi" } }, { "ぷ", new[] { "pu" } }, { "ぺ", new[] { "pe" } }, { "ぽ", new[] { "po" } },
            { "ぁ", new[] { "xa", "la" } }, { "ぃ", new[] { "xi", "li" } }, { "ぅ", new[] { "xu", "lu" } }, { "ぇ", new[] { "xe", "le" } }, { "ぉ", new[] { "xo", "lo" } }, { "ゎ", new[] { "xwa", "lwa" } }, { "ゔ", new[] { "vu" } },
            { "ゃ", new[] { "xya", "lya" } }, { "ゅ", new[] { "xyu", "lyu" } }, { "ょ", new[] { "xyo", "lyo" } }, { "っ", new[] { "xtu", "ltu", "xtsu", "ltsu" } },
            { "ー", new[] { "-" } }, { "、", new[] { "," } }, { "。", new[] { "." } }, { "！", new[] { "!" } }, { "？", new[] { "?" } }, { "　", new[] { " " } },
            // 小さい「ゃゅょ」がつく音（ひとまとまりで打つ形）
            { "きゃ", new[] { "kya" } }, { "きゅ", new[] { "kyu" } }, { "きょ", new[] { "kyo" } },
            { "しゃ", new[] { "sha", "sya" } }, { "しゅ", new[] { "shu", "syu" } }, { "しょ", new[] { "sho", "syo" } },
            { "ちゃ", new[] { "cha", "tya", "cya" } }, { "ちゅ", new[] { "chu", "tyu", "cyu" } }, { "ちょ", new[] { "cho", "tyo", "cyo" } },
            { "にゃ", new[] { "nya" } }, { "にゅ", new[] { "nyu" } }, { "にょ", new[] { "nyo" } },
            { "ひゃ", new[] { "hya" } }, { "ひゅ", new[] { "hyu" } }, { "ひょ", new[] { "hyo" } },
            { "みゃ", new[] { "mya" } }, { "みゅ", new[] { "myu" } }, { "みょ", new[] { "myo" } },
            { "りゃ", new[] { "rya" } }, { "りゅ", new[] { "ryu" } }, { "りょ", new[] { "ryo" } },
            { "ぎゃ", new[] { "gya" } }, { "ぎゅ", new[] { "gyu" } }, { "ぎょ", new[] { "gyo" } },
            { "じゃ", new[] { "ja", "jya", "zya" } }, { "じゅ", new[] { "ju", "jyu", "zyu" } }, { "じょ", new[] { "jo", "jyo", "zyo" } },
            { "びゃ", new[] { "bya" } }, { "びゅ", new[] { "byu" } }, { "びょ", new[] { "byo" } },
            { "ぴゃ", new[] { "pya" } }, { "ぴゅ", new[] { "pyu" } }, { "ぴょ", new[] { "pyo" } },
            { "てぃ", new[] { "thi" } }, { "でぃ", new[] { "dhi" } }, { "てゅ", new[] { "thu" } }, { "でゅ", new[] { "dhu" } },
            { "とぅ", new[] { "twu" } }, { "どぅ", new[] { "dwu" } },
            { "ふぁ", new[] { "fa", "fwa" } }, { "ふぃ", new[] { "fi", "fwi", "fyi" } }, { "ふぇ", new[] { "fe", "fwe", "fye" } }, { "ふぉ", new[] { "fo", "fwo" } },
            { "ふゃ", new[] { "fya" } }, { "ふゅ", new[] { "fyu" } }, { "ふょ", new[] { "fyo" } },
            // 「ぇ」がつく音（しぇ・ちぇ・じぇ など）
            { "いぇ", new[] { "ye" } }, { "きぇ", new[] { "kye" } }, { "ぎぇ", new[] { "gye" } },
            { "しぇ", new[] { "she", "sye" } }, { "じぇ", new[] { "je", "jye", "zye" } }, { "ちぇ", new[] { "che", "tye", "cye" } },
            { "にぇ", new[] { "nye" } }, { "ひぇ", new[] { "hye" } }, { "びぇ", new[] { "bye" } }, { "ぴぇ", new[] { "pye" } },
            { "みぇ", new[] { "mye" } }, { "りぇ", new[] { "rye" } },
            { "ぢゃ", new[] { "dya" } }, { "ぢゅ", new[] { "dyu" } }, { "ぢょ", new[] { "dyo" } }, { "ぢぇ", new[] { "dye" } },
            // う＋小さい字（うぃ・うぇ など）、く＋小さい字（くぁ など）、つ＋小さい字（つぁ など）
            { "うぁ", new[] { "wha" } }, { "うぃ", new[] { "wi", "whi" } }, { "うぇ", new[] { "we", "whe" } }, { "うぉ", new[] { "who" } },
            { "くぁ", new[] { "qa", "kwa", "qwa" } }, { "くぃ", new[] { "qi", "qwi" } }, { "くぇ", new[] { "qe", "qwe" } }, { "くぉ", new[] { "qo", "qwo" } },
            { "ぐぁ", new[] { "gwa" } },
            { "つぁ", new[] { "tsa" } }, { "つぃ", new[] { "tsi" } }, { "つぇ", new[] { "tse" } }, { "つぉ", new[] { "tso" } },
            // ゔ（ヴ）の音
            { "ゔぁ", new[] { "va" } }, { "ゔぃ", new[] { "vi" } }, { "ゔぇ", new[] { "ve" } }, { "ゔぉ", new[] { "vo" } }, { "ゔゅ", new[] { "vyu" } },
        };

        private readonly List<List<string>> chunks = new List<List<string>>(); // かたまりごとの打ち方の候補
        private readonly List<int> chunkKana = new List<int>();                // かたまりごとの、かなの文字数

        /// <summary>
        /// 打ちとちゅうの「読み方」1つ：何番目のかたまりまで打ち終わったか（Index）と、今のかたまりに打った文字（Buffer）。
        /// 「konn」のように どちらにも読めるときは、いくつもの読み方を同時に追いかける（あとの文字で決まる）。
        /// </summary>
        private struct State
        {
            public int Index;
            public string Buffer;
        }

        private List<State> states = new List<State> { new State { Index = 0, Buffer = "" } };
        private readonly StringBuilder typed = new StringBuilder();

        public string Kana { get; }

        /// <summary>打ち終わった（どれかの読み方で、最後まで打てた）</summary>
        public bool Done => states.Exists(s => s.Index >= chunks.Count);

        public RomajiMatcher(string kana)
        {
            Kana = ToHiragana(kana);
            BuildChunks();
        }

        /// <summary>カタカナをひらがなにする</summary>
        public static string ToHiragana(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(c >= 'ァ' && c <= 'ヶ' ? (char)(c - 0x60) : c);
            return sb.ToString();
        }

        private void BuildChunks()
        {
            var raw = new List<string>();
            for (int i = 0; i < Kana.Length; i++)
            {
                // 2文字でひとまとまり（きゃ・しぇ・うぃ など）
                if (i + 1 < Kana.Length && Table.ContainsKey(Kana.Substring(i, 2)))
                {
                    raw.Add(Kana.Substring(i, 2));
                    i++;
                }
                else raw.Add(Kana[i].ToString());
            }

            for (int i = 0; i < raw.Count; i++)
            {
                string k = raw[i];
                var cand = new List<string>();
                if (k == "っ" && i + 1 < raw.Count)
                {
                    // 「っ」＋次の音：子音を2回（kka）・ち の前は tch（matcha）・xtu＋次の音
                    var next = Candidates(raw[i + 1], i + 2 < raw.Count ? raw[i + 2] : null);
                    foreach (var c in next)
                        if (c.Length > 0 && "aiueon".IndexOf(c[0]) < 0) Add(cand, c[0] + c);
                    foreach (var c in next)
                        if (c.StartsWith("ch")) Add(cand, "t" + c);
                    foreach (var t in Table["っ"]) foreach (var c in next) Add(cand, t + c);
                    chunkKana.Add(1 + raw[i + 1].Length);
                    i++;
                }
                else
                {
                    cand.AddRange(Candidates(k, i + 1 < raw.Count ? raw[i + 1] : null));
                    chunkKana.Add(k.Length);
                }
                chunks.Add(cand);
            }
        }

        private static void Add(List<string> list, string s)
        {
            if (!list.Contains(s)) list.Add(s);
        }

        private static List<string> Candidates(string k, string next)
        {
            var list = new List<string>();
            if (k == "ん")
            {
                // 次が母音・や行でなければ n 1回でもOK（な行の前も OK：konnichiha）。最後の「ん」は nn
                bool single = next != null;
                bool beforeN = false;
                if (next != null && Table.TryGetValue(next.Substring(0, 1), out var nc))
                    foreach (var c in nc)
                    {
                        if ("aiueoy".IndexOf(c[0]) >= 0) single = false;
                        if (c[0] == 'n') beforeN = true;
                    }
                if (single && !beforeN) list.Add("n");
                list.Add("nn");
                if (single && beforeN) list.Add("n"); // お手本は nn のまま
                list.Add("xn");
                return list;
            }
            if (Table.TryGetValue(k, out var arr)) foreach (var a in arr) Add(list, a);
            if (k.Length == 2)
            {
                // きゃ を ki＋xya のように、2つに分けて打ってもOK
                if (Table.TryGetValue(k.Substring(0, 1), out var a) && Table.TryGetValue(k.Substring(1, 1), out var b))
                    foreach (var x in a) foreach (var y in b) Add(list, x + y);
            }
            if (list.Count == 0) list.Add(k); // 英数字などは、そのまま打つ
            return list;
        }

        /// <summary>1文字打つ。正しければ true（どの読み方にも合わなければ false）</summary>
        public bool Type(char ch)
        {
            if (Done) return false;
            ch = char.ToLowerInvariant(ch);
            var next = new List<State>();
            foreach (var s in states)
            {
                if (s.Index >= chunks.Count) continue;
                string buf = s.Buffer + ch;
                var cand = chunks[s.Index];
                if (cand.Contains(buf)) AddState(next, s.Index + 1, "");                     // このかたまりを打ち終わった
                if (cand.Exists(c => c.Length > buf.Length && c.StartsWith(buf))) AddState(next, s.Index, buf); // まだ続きがある
            }
            if (next.Count == 0) return false;
            states = next;
            typed.Append(ch);
            return true;
        }

        private static void AddState(List<State> list, int index, string buffer)
        {
            foreach (var s in list) if (s.Index == index && s.Buffer == buffer) return;
            list.Add(new State { Index = index, Buffer = buffer });
        }

        /// <summary>いちばん先まで進んでいる読み方（お手本と、緑にする文字数に使う）</summary>
        private State Best
        {
            get
            {
                var best = states[0];
                foreach (var s in states)
                    if (s.Index > best.Index || (s.Index == best.Index && s.Buffer.Length > best.Buffer.Length)) best = s;
                return best;
            }
        }

        /// <summary>打ち終わった かなの文字数（日本語の言葉の、どこまで打てたか）</summary>
        public int KanaDone
        {
            get
            {
                int n = 0, index = Best.Index;
                for (int i = 0; i < index && i < chunkKana.Count; i++) n += chunkKana[i];
                return n;
            }
        }

        /// <summary>打った文字</summary>
        public string Typed => typed.ToString();

        /// <summary>お手本：これから打つ文字（打ち方が決まっていれば その打ち方、まだなら よく使う打ち方で表示）</summary>
        public string Remaining
        {
            get
            {
                if (Done) return "";
                var best = Best;
                var sb = new StringBuilder();
                var first = chunks[best.Index].Find(c => c.StartsWith(best.Buffer)) ?? chunks[best.Index][0];
                sb.Append(first.Substring(best.Buffer.Length));
                for (int i = best.Index + 1; i < chunks.Count; i++) sb.Append(chunks[i][0]);
                return sb.ToString();
            }
        }

        /// <summary>お手本全体（打つ前）</summary>
        public string Guide => Typed + Remaining;

        /// <summary>次に打つ1文字（お助け用）</summary>
        public char NextChar => Remaining.Length > 0 ? Remaining[0] : ' ';
    }
}
