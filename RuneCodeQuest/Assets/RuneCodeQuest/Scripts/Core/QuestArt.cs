using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// クエストの見た目（ゴール・アイテム・じゃまもの）の仮のドット絵。
    /// 8x8 の文字列で描き（'.' は透明）、PixelArt で 16x16 に細かくしています（なめらか・かげ・ふちどり）。
    /// 本番の画像ができたら、ここで返す Sprite を差し替えてください。
    /// </summary>
    public static class QuestArt
    {
        private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'K', new Color32(30, 30, 40, 255) },    // 黒
            { 'W', new Color32(255, 255, 255, 255) }, // 白
            { 'D', new Color32(90, 80, 75, 255) },    // 暗い石
            { 'P', new Color32(150, 150, 165, 255) }, // 石
            { 'C', new Color32(90, 230, 255, 255) },  // 水色
            { 'Y', new Color32(255, 230, 80, 255) },  // 光
            { 'A', new Color32(230, 180, 40, 255) },  // 金
            { 'N', new Color32(150, 95, 50, 255) },   // 木
            { 'E', new Color32(220, 60, 60, 255) },   // 赤
            { 'G', new Color32(90, 200, 110, 255) },  // 緑
            { 'V', new Color32(40, 120, 50, 255) },   // こい緑
            { 'M', new Color32(240, 110, 180, 255) }, // ピンク
            { 'O', new Color32(255, 170, 80, 255) },  // オレンジ
            { 'I', new Color32(170, 220, 250, 255) }, // 氷
            { 'F', new Color32(190, 110, 255, 255) }, // 結界のむらさき
        };

        // ----- ゴール（[0]=まだ [1]=使ったあと） -----
        private static readonly string[][][] GoalArts =
        {
            new[] // 灯台
            {
                new[] { "...DD...", "..DPPD..", "...DD...", "...PP...", "...PP...", "...PP...", "..PPPP..", ".DDDDDD." },
                new[] { "...YY...", "..YWWY..", "...YY...", "...PP...", "...PP...", "...PP...", "..PPPP..", ".DDDDDD." },
            },
            new[] // 扉
            {
                new[] { "..DDDD..", ".DNNNND.", "DNNNNNND", "DNNNNNND", "DNNNNAND", "DNNNNNND", "DNNNNNND", "DDDDDDDD" },
                new[] { "..DDDD..", ".DKKKKD.", "DKKKKKKD", "DKKYKKKD", "DKKKKKKD", "DKKKKKKD", "DKKKKKKD", "DDDDDDDD" },
            },
            new[] // 宝箱
            {
                new[] { "........", "..NNNN..", ".NAAAAN.", "NNNNNNNN", "NNNAANNN", "NNNNNNNN", "NNNNNNNN", "........" },
                new[] { ".Y.YY.Y.", "..YAAY..", ".NAYYAN.", "NNNNNNNN", "NNNAANNN", "NNNNNNNN", "NNNNNNNN", "........" },
            },
            new[] // 魔法陣
            {
                new[] { "..PPPP..", ".P....P.", "P..PP..P", "P.P..P.P", "P.P..P.P", "P..PP..P", ".P....P.", "..PPPP.." },
                new[] { "..CCCC..", ".C.YY.C.", "C.YCCY.C", "CYC..CYC", "CYC..CYC", "C.YCCY.C", ".C.YY.C.", "..CCCC.." },
            },
            new[] // 鐘
            {
                new[] { "...DD...", "..PPPP..", ".PPPPPP.", ".PPPPPP.", ".PPPPPP.", "PPPPPPPP", "...PP...", "........" },
                new[] { "Y..DD..Y", ".YAAAAY.", ".AAAAAA.", "YAAAAAAY", ".AAAAAA.", "AAAAAAAA", "Y..AA..Y", "........" },
            },
            new[] // 石像
            {
                new[] { "..PPPP..", ".PDPPDP.", ".PPPPPP.", "PPPPPPPP", "P.PPPP.P", "..PPPP..", "..P..P..", ".DDDDDD." },
                new[] { "..PPPP..", ".PYPPYP.", ".PPPPPP.", "PPPPPPPP", "P.PPPP.P", "..PPPP..", "..P..P..", ".DDDDDD." },
            },
            new[] // 泉
            {
                new[] { "........", "........", "........", "PPPPPPPP", ".PDDDDP.", ".PPPPPP.", "..PPPP..", ".PPPPPP." },
                new[] { "...CC...", "..C..C..", ".C.CC.C.", "PPPPPPPP", ".PCCCCP.", ".PPPPPP.", "..PPPP..", ".PPPPPP." },
            },
            new[] // 旗
            {
                new[] { "........", "........", "........", "........", ".D......", ".D......", ".D......", "DDD....." },
                new[] { ".EEEE...", ".EEEEE..", ".EEEE...", ".D......", ".D......", ".D......", ".D......", "DDD....." },
            },
        };

        // ----- アイテム（ItemKind の順。[0] は None なので空） -----
        private static readonly string[][] ItemArts =
        {
            null,
            new[] { "........", "...CC...", "..CWCC..", ".CCCCCC.", "..CCCC..", "...CC...", "........", "........" }, // 水晶
            new[] { "........", ".AAA....", "A...A...", ".AAAAAAA", "......A.", "....A.A.", "........", "........" }, // かぎ
            new[] { "........", "..G..G..", ".GGG.GG.", "..GGGG..", "...GG...", "...VV...", "...VV...", "........" }, // 薬草
            new[] { "........", "...WW...", "..WWWW..", ".WWWWWW.", ".WWWOWW.", ".WWWWWW.", "..WWWW..", "........" }, // たまご
            new[] { "...Y....", "...YY...", "YYYYYYY.", ".YYYYY..", "..YYYY..", ".YY.YY..", ".Y...Y..", "........" }, // 星のかけら
            new[] { "........", "..AAAA..", ".AYYYYA.", ".AYAAYA.", ".AYAAYA.", ".AYYYYA.", "..AAAA..", "........" }, // 金貨
            new[] { "........", "..M.M...", ".MMYMM..", "..MMM...", "...G....", ".GGG....", "...G....", "........" }, // 花
            new[] { "........", ".EEEEEE.", ".EWWWWE.", ".EWWWWE.", ".EWWWWE.", ".EEEEEE.", ".DDDDDD.", "........" }, // まほうの本
        };

        // ----- じゃまもの（ObstacleKind の順。穴は SpriteFactory.BridgeBroken を使う） -----
        private static readonly string[][] ObstacleArts =
        {
            null,
            new[] { "........", "..PPPP..", ".PPPPPD.", "PPWPPPPD", "PPPPPPPD", "PPPPPPDD", ".DDDDDD.", "........" }, // 岩
            new[] { "V..V..V.", ".VGV.VG.", "..V.GV..", ".VGVV.V.", "V.V.GV.V", ".GV.V.G.", "V..VGV..", ".V..V..V" }, // いばら
            new[] { "IIIIIIII", "IWIIIIWI", "IIWIIIII", "IIIIIWII", "IIIIIIWI", "IWIIIIII", "IIWIIIII", "IIIIIIII" }, // 氷
            new[] { "F.F.F.F.", ".F.F.F.F", "F.FMF.F.", ".FMMMF.F", "F.FMF.F.", ".F.F.F.F", "F.F.F.F.", ".F.F.F.F" }, // 結界
        };

        // ----- 持ち物リストで使う絵 -----
        private static readonly string[] PocketArt = { "..NNNN..", ".N....N.", "NNNNNNNN", "NOOOOOON", "NOONNOON", "NOOOOOON", "NOOOOOON", ".NNNNNN." };
        private static readonly string[] WandArt = { ".....Y..", "....YWY.", ".....Y..", "....N...", "...N....", "..N.....", ".N......", "N......." };

        public static Sprite Pocket => Make("pocket", PocketArt);

        // ----- マップの飾り（章ごと：石の柱・サボテン・木・雪だるま・歯車・たいまつ） -----
        private static readonly string[][] DecorArts =
        {
            new[] { "..PPPP..", ".PPPPPP.", "..PDDP..", "..PDDP..", "..PDDP..", "..PDDP..", ".PPPPPP.", "PPPPPPPP" },
            new[] { "...GG...", "...GG...", "G..GG..G", "GG.GG.GG", ".GGGGGG.", "...GG...", "...GG...", "..NNNN.." },
            new[] { "..GGGG..", ".GGGGGG.", "GGGVGGGG", "GGGGGGVG", ".GGGGGG.", "...NN...", "...NN...", "..NNNN.." },
            new[] { "..KKKK..", "..KKKK..", ".KKKKKK.", "..WKWW..", "..WWWW..", ".WWWWWW.", ".WWOWWW.", "..WWWW.." },
            new[] { "..A..A..", ".AAAAAA.", "AAA..AAA", ".A....A.", ".A....A.", "AAA..AAA", ".AAAAAA.", "..A..A.." },
            new[] { "...OY...", "..OYYO..", "...OO...", "...NN...", "...NN...", "...NN...", "..NNNN..", ".DDDDDD." },
        };

        public static Sprite Decor(int chapter)
        {
            int i = Mathf.Clamp(chapter, 1, DecorArts.Length) - 1;
            return Make("decor" + i, DecorArts[i]);
        }
        public static Sprite Wand => Make("wand", WandArt);

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Goal(GoalKind kind, bool done) =>
            Make($"goal{(int)kind}_{(done ? 1 : 0)}", GoalArts[(int)kind][done ? 1 : 0]);

        public static Sprite Item(ItemKind kind) =>
            kind == ItemKind.None ? null : Make($"item{(int)kind}", ItemArts[(int)kind]);

        /// <summary>じゃまものの絵（穴はこわれた橋の絵）</summary>
        public static Sprite Obstacle(ObstacleKind kind) =>
            kind == ObstacleKind.Hole ? SpriteFactory.BridgeBroken : Make($"obstacle{(int)kind}", ObstacleArts[(int)kind]);

        /// <summary>8x8 の絵を 16x16 に細かくして（なめらか・かげ・ふちどり）Sprite にする</summary>
        private static Sprite Make(string key, string[] art)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = PixelArt.ToSprite(PixelArt.Upscale(art, Palette), key);
            cache[key] = s;
            return s;
        }
    }
}
