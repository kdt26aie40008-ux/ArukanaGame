using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>装備を作るための素材（風船割りでもらえる）。下のものほどレア</summary>
    public enum CraftMaterial { RuneOre, WindCrystal, StarStone, RainbowGem }

    /// <summary>
    /// 風船割り（タイピングの練習）のルール（Unity の機能は使っていません）。
    ///
    /// ・1分のあいだ、風船に書かれた言葉を打つ。1文字ごとに風船がふくらみ、打ち終わると割れる
    /// ・言葉は、割った数が増えるほど長くなる
    /// ・1回ごとに、割った数に合わせて素材がもらえる（何回でも）。レアな素材ほど、たくさん割らないともらえない
    /// ・これまでに割った合計で、キャラクリの服・ぼうしがもらえる（1回だけ）
    /// </summary>
    public static class Balloon
    {
        public const float TimeLimit = 60f;

        public const int CraftKinds = 4;
        public static readonly string[] CraftNames = { "ルーン鉱石", "風の結晶", "星くずの石", "虹の宝石" };

        /// <summary>素材をもらうのに必要な、1回で割る数（ルーン鉱石は10こ以上で2こ）</summary>
        public static readonly int[] CraftNeed = { 5, 15, 25, 35 };

        /// <summary>1回で割った数から、もらえる素材の数</summary>
        public static int[] Rewards(int popped)
        {
            var r = new int[CraftKinds];
            if (popped >= CraftNeed[0]) r[(int)CraftMaterial.RuneOre] = popped >= 10 ? 2 : 1;
            for (int i = 1; i < CraftKinds; i++)
                if (popped >= CraftNeed[i]) r[i] = 1;
            return r;
        }

        /// <summary>キャラクリのほうび（これまでに割った合計で、1回だけもらえる）</summary>
        public class Cosmetic
        {
            public string Name;
            public bool IsHat;      // true ならぼうし、false ならふく
            public int Index;       // CharacterArt.HatNames / OutfitNames の番号
            public int Need;        // これまでに割った合計
        }

        public static readonly Cosmetic[] Cosmetics =
        {
            new Cosmetic { Name = "風船のリボン", IsHat = true, Index = 10, Need = 50 },
            new Cosmetic { Name = "シェフのぼうし", IsHat = true, Index = 11, Need = 150 },
            new Cosmetic { Name = "金のマント", IsHat = false, Index = 7, Need = 300 },
            new Cosmetic { Name = "虹のローブ", IsHat = false, Index = 8, Need = 500 },
            new Cosmetic { Name = "ルーンのかんむり", IsHat = true, Index = 12, Need = 800 },
        };

        /// <summary>そのぼうし・ふくに必要な合計（ほうびでなければ 0）</summary>
        public static int NeedFor(bool isHat, int index)
        {
            foreach (var c in Cosmetics) if (c.IsHat == isHat && c.Index == index) return c.Need;
            return 0;
        }

        /// <summary>合計が before から after になったときに、新しくもらえたもの</summary>
        public static List<Cosmetic> NewlyUnlocked(int before, int after)
        {
            var list = new List<Cosmetic>();
            foreach (var c in Cosmetics) if (before < c.Need && after >= c.Need) list.Add(c);
            return list;
        }

        // ------------------------------------------------------------------
        // 言葉（割った数が増えるほど長くなる）
        // ------------------------------------------------------------------

        /// <summary>何こ目の風船か → 言葉の長さの段階（0〜3）</summary>
        public static int Tier(int index) => index < 5 ? 0 : index < 10 ? 1 : index < 15 ? 2 : 3;

        private static readonly (string d, string r)[][] JapaneseTiers =
        {
            new[] { ("いぬ", "いぬ"), ("ねこ", "ねこ"), ("はな", "はな"), ("そら", "そら"), ("うみ", "うみ"), ("やま", "やま"), ("ほし", "ほし"), ("つえ", "つえ"), ("かぎ", "かぎ"), ("くも", "くも"), ("みず", "みず"), ("いし", "いし") },
            new[] { ("まほう", "まほう"), ("たまご", "たまご"), ("りんご", "りんご"), ("ふうせん", "ふうせん"), ("きんか", "きんか"), ("やくそう", "やくそう"), ("ごーれむ", "ごーれむ"), ("すいか", "すいか"), ("めいれい", "めいれい"), ("くだもの", "くだもの"), ("まちかど", "まちかど") },
            new[] { ("くりかえし", "くりかえし"), ("としょかん", "としょかん"), ("とけいだい", "とけいだい"), ("あるかな", "あるかな"), ("すいしゃごや", "すいしゃごや"), ("るーんこーど", "るーんこーど"), ("ぷろぐらむ", "ぷろぐらむ"), ("宝箱", "たからばこ"), ("図書館", "としょかん"), ("ぼうけんしゃ", "ぼうけんしゃ"), ("ふうせんわり", "ふうせんわり") },
            new[] { ("魔法をとなえる", "まほうをとなえる"), ("町を元にもどす", "まちをもとにもどす"), ("命令を順番にならべる", "めいれいをじゅんばんにならべる"), ("ゴールまでくり返す", "ごーるまでくりかえす"), ("風船をわる", "ふうせんをわる"), ("宝箱を開ける", "たからばこをあける"), ("星を集める", "ほしをあつめる"), ("アルカナをたおす", "あるかなをたおす"), ("ゴーレムがすすむ", "ごーれむがすすむ"), ("橋をなおす", "はしをなおす") },
        };

        private static readonly string[][] EnglishTiers =
        {
            new[] { "if", "for", "def", "not", "map", "key", "run", "sun", "pop", "box" },
            new[] { "pass", "else", "left", "right", "while", "range", "magic", "golem", "loop", "rune", "break" },
            new[] { "forward", "pick_up", "at_goal", "use_item", "use_magic", "read_sign", "left(90)", "right(90)", "object_here", "wall_in_front" },
            new[] { "forward()", "for i in range(3):", "while not at_goal():", "if wall_in_front():", "def turn_corner():", "n = read_sign()", "use_magic()", "pick_up()", "if object_here():", "while n > 0:", "right_is_clear()" },
        };

        /// <summary>しばらく同じ言葉を出さないために、おぼえておく数</summary>
        public const int RecentCount = 6;

        /// <summary>
        /// 何こ目の風船かに合わせて言葉を選ぶ。recent（さっき出た言葉）に入っているものは選ばない
        /// （段階の言葉が少なくて全部入っているときは、いちばん前に出たものから使う）
        /// </summary>
        public static TypingWord PickWord(bool japanese, int index, Random rng, IList<string> recent = null)
        {
            int t = Tier(index);
            var candidates = new List<TypingWord>();
            if (japanese)
                foreach (var (d, r) in JapaneseTiers[t]) candidates.Add(new TypingWord { Display = d, Reading = r });
            else
                foreach (var w in EnglishTiers[t]) candidates.Add(new TypingWord { Display = w, Reading = w });
            return PickFresh(candidates, rng, recent);
        }

        /// <summary>候補の中から、さっき出ていないものをえらぶ。えらんだ言葉は recent に入れる</summary>
        public static TypingWord PickFresh(List<TypingWord> candidates, Random rng, IList<string> recent)
        {
            var fresh = recent == null ? candidates : candidates.FindAll(w => !recent.Contains(w.Display));
            TypingWord pick;
            if (fresh.Count > 0) pick = fresh[rng.Next(fresh.Count)];
            else
            {
                // 全部さっき出ていたら、いちばん前に出たものを使う
                pick = candidates[0];
                int best = int.MaxValue;
                foreach (var w in candidates)
                {
                    int i = recent.IndexOf(w.Display);
                    if (i < best) { best = i; pick = w; }
                }
            }
            if (recent != null)
            {
                recent.Remove(pick.Display);
                recent.Add(pick.Display);
                while (recent.Count > RecentCount) recent.RemoveAt(0);
            }
            return pick;
        }
    }
}
