using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 仮素材（ドット絵）をプログラムで作るクラス。
    /// 本番の画像ができたら、ここで作っている Sprite を差し替えるだけで見た目を変えられます。
    ///
    /// ・地面のタイル（床・かべ・草・水・橋・門）は 16x16 で、石だたみ・レンガ・草の葉・波などを描く
    /// ・ゴーレムなどは 8x8 の文字列で描いて、PixelArt で 16x16 に細かくする（'.' は透明）
    /// </summary>
    public static class SpriteFactory
    {
        private const int T = 16; // タイル1枚のドット数

        private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            { 'K', new Color32(30, 30, 40, 255) },    // 黒
            { 'G', new Color32(90, 200, 110, 255) },  // スライム
            { 'W', new Color32(255, 255, 255, 255) }, // 白
            { 'C', new Color32(90, 230, 255, 255) },  // 水晶
            { 'P', new Color32(150, 150, 165, 255) }, // 石
            { 'R', new Color32(140, 125, 105, 255) }, // ゴーレムの岩
            { 'M', new Color32(105, 92, 78, 255) },   // ゴーレムのこい岩
            { 'Y', new Color32(255, 230, 80, 255) },  // 光
            { 'D', new Color32(90, 80, 75, 255) },    // 暗い石
            { 'V', new Color32(80, 160, 90, 255) },   // こけ
        };

        private static readonly string[] SlimeArt =
            { "........", "...GG...", "..GGGG..", ".GWGGWG.", ".GKGGKG.", "GGGGGGGG", "GGGGGGGG", ".GGGGGG." };

        private static readonly string[] DeviceArt =
            { "...CC...", "..CWCC..", "...CC...", "..PPPP..", ".PPCCPP.", "..PPPP..", "..PPPP..", ".DDDDDD." };

        private static readonly string[] GolemArt =
            { "..RRRR..", ".RYRRYR.", ".RMRRMR.", "RRRVRRRR", "R.RRRR.R", "M.RMMR.M", "..RRRR..", ".MM..MM." };

        private static readonly string[] BeaconArt =
            { "...YY...", "..YWWY..", "...YY...", "...PP...", "...PP...", "...PP...", "..PPPP..", ".DDDDDD." };

        private static readonly string[] BeaconOffArt =
            { "...DD...", "..DPPD..", "...DD...", "...PP...", "...PP...", "...PP...", "..PPPP..", ".DDDDDD." };

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>プレイヤー（キャラクリで作った見た目）</summary>
        public static Sprite Player => LookSprite(Progress.Look, Progress.LookMode);

        /// <summary>見た目の作り方（イラスト・パーツきせかえ・ドットきせかえ）に合わせた主人公の絵。画像がなければ ドットきせかえ</summary>
        public static Sprite LookSprite(CharacterLook look, int mode)
        {
            Sprite s = null;
            if (mode == Progress.LookIllust) s = ArtOverrides.Character("主人公");
            else if (mode == Progress.LookParts) s = CharacterParts.Build(look);
            return s != null ? s : CharacterArt.Build(look);
        }
        public static Sprite Slime => FromArt("slime", SlimeArt);
        public static Sprite Device => FromArt("device", DeviceArt);
        public static Sprite Golem => ArtOverrides.Character("ゴーレム") ?? FromArt("golem", GolemArt);
        public static Sprite Beacon => FromArt("beacon", BeaconArt);
        public static Sprite BeaconOff => FromArt("beaconOff", BeaconOffArt);

        // ----- 章ごとの景色（床・壁・草・水の色）。[0]=第1章 … [5]=第6章 -----
        private static readonly Color32[][] Themes =
        {
            //            床                       壁                     草                      水
            new[] { C(120, 115, 110), C(70, 62, 60), C(80, 160, 80), C(50, 110, 200) },   // 第1章 はじまりの遺跡
            new[] { C(205, 175, 115), C(135, 100, 65), C(190, 165, 85), C(55, 125, 195) }, // 第2章 砂の遺跡
            new[] { C(105, 115, 90), C(50, 70, 45), C(40, 115, 55), C(40, 95, 125) },      // 第3章 森の遺跡
            new[] { C(200, 205, 218), C(115, 125, 150), C(232, 238, 248), C(120, 170, 225) }, // 第4章 雪の遺跡
            new[] { C(170, 140, 100), C(95, 70, 45), C(150, 120, 80), C(60, 100, 120) },      // 第5章 からくりの都
            new[] { C(85, 72, 100), C(40, 30, 52), C(75, 55, 95), C(45, 40, 100) },        // 第6章 やみの城
            new[] { C(196, 186, 160), C(92, 112, 138), C(110, 172, 120), C(40, 120, 210) }, // 港町（アクアリス）
        };

        public static readonly string[] ThemeNames = { "はじまりの遺跡", "砂の遺跡", "森の遺跡", "雪の遺跡", "からくりの都", "やみの城", "港町" };

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        private static Color32[] ThemeOf(int chapter) => Themes[Mathf.Clamp(chapter - 1, 0, Themes.Length - 1)];

        /// <summary>景色の色（0=床 1=壁 2=草 3=水）。町の がけ・階段の色に使う</summary>
        public static Color32 ThemeColor(int chapter, int index) => ThemeOf(chapter)[index];

        public static Sprite FloorOf(int chapter) => Cached("floor" + chapter, () => Floor(ThemeOf(chapter)[0], chapter));
        public static Sprite WallOf(int chapter) => Cached("wall" + chapter, () => Wall(ThemeOf(chapter)[1], chapter));
        public static Sprite GrassOf(int chapter) => Cached("grass" + chapter, () => Grass(ThemeOf(chapter)[2], chapter));
        public static Sprite WaterOf(int chapter) => Cached("water" + chapter, () => Water(ThemeOf(chapter)[3], chapter));
        public static Sprite Bridge => Cached("bridge", () => Planks(false));
        public static Sprite BridgeBroken => Cached("bridgeBroken", () => Planks(true));
        public static Sprite Gate => Cached("gate", GateArt);

        /// <summary>GUI で描くときに使う Texture（Sprite と同じ画像）</summary>
        public static Texture2D TextureOf(Sprite sprite) => sprite != null ? sprite.texture : Texture2D.whiteTexture;

        private static Sprite FromArt(string key, string[] art) => Cached(key, () => PixelArt.Upscale(art, Palette));

        private static Sprite Cached(string key, System.Func<PixelArt.Canvas> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = PixelArt.ToSprite(make(), key);
            cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------
        // 地面のタイル（16x16）
        // ------------------------------------------------------------------

        /// <summary>石だたみ（4枚の石。すきまは暗く、石の上と左は明るい。ひびが1本）</summary>
        private static PixelArt.Canvas Floor(Color32 b, int seed)
        {
            var c = new PixelArt.Canvas(T, T);
            var rng = new System.Random(seed * 11 + 1);
            Color32 dark = PixelArt.Mul(b, 0.72f), light = PixelArt.Mul(PixelArt.Add(b, 12), 1.1f), mid = PixelArt.Mul(b, 0.9f);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int lx = x % 8, ly = y % 8;
                    Color32 col = rng.NextDouble() < 0.12 ? mid : b;
                    if (lx == 7 || ly == 7) col = dark;
                    else if (lx == 0 || ly == 0) col = light;
                    c.Px[x, y] = col;
                }
            // ひび
            int cx = rng.Next(9, 13), cy = rng.Next(2, 5);
            for (int i = 0; i < 4; i++) c.Set(cx + (i % 2), cy + i, dark);
            return c;
        }

        /// <summary>レンガのかべ（段ごとにずらす。レンガの上は明るく、目地は暗い）</summary>
        private static PixelArt.Canvas Wall(Color32 b, int seed)
        {
            var c = new PixelArt.Canvas(T, T);
            var rng = new System.Random(seed * 13 + 2);
            Color32 mortar = PixelArt.Mul(b, 0.55f), top = PixelArt.Mul(PixelArt.Add(b, 14), 1.15f), shade = PixelArt.Mul(b, 0.85f);
            for (int y = 0; y < T; y++)
            {
                int row = y / 4, ly = y % 4;
                int offset = row % 2 == 0 ? 0 : 4;
                for (int x = 0; x < T; x++)
                {
                    int lx = (x + offset) % 8;
                    Color32 col = rng.NextDouble() < 0.1 ? shade : b;
                    if (ly == 3 || lx == 7) col = mortar;
                    else if (ly == 0) col = top;
                    else if (ly == 2) col = shade;
                    c.Px[x, y] = col;
                }
            }
            return c;
        }

        /// <summary>草（葉っぱを何本も。先は明るく、根もとは暗い。ときどき小さな花）</summary>
        private static PixelArt.Canvas Grass(Color32 b, int seed)
        {
            var c = new PixelArt.Canvas(T, T);
            var rng = new System.Random(seed * 17 + 3);
            Color32 dark = PixelArt.Mul(b, 0.75f), light = PixelArt.Mul(PixelArt.Add(b, 20), 1.15f);
            c.Rect(0, 0, T, T, b);
            for (int i = 0; i < 14; i++)
            {
                int x = rng.Next(0, T), y = rng.Next(2, T);
                c.Set(x, y, dark);
                c.Set(x, y - 1, b);
                c.Set(x + (rng.Next(2) == 0 ? -1 : 1), y - 2, light);
            }
            if (rng.Next(2) == 0)
            {
                int fx = rng.Next(2, T - 2), fy = rng.Next(2, T - 2);
                c.Set(fx, fy, new Color32(255, 240, 120, 255));
                c.Set(fx - 1, fy, new Color32(255, 255, 255, 255));
                c.Set(fx + 1, fy, new Color32(255, 255, 255, 255));
            }
            return c;
        }

        /// <summary>水（波の線と、きらっと光る点）</summary>
        private static PixelArt.Canvas Water(Color32 b, int seed)
        {
            var c = new PixelArt.Canvas(T, T);
            var rng = new System.Random(seed * 19 + 4);
            Color32 deep = PixelArt.Mul(b, 0.85f), wave = PixelArt.Add(b, 45), spark = new Color32(235, 250, 255, 255);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++) c.Px[x, y] = (y / 4) % 2 == 0 ? b : deep;
            foreach (var (wx, wy) in new[] { (2, 3), (9, 7), (4, 11), (11, 14) })
            {
                c.Set(wx, wy, wave); c.Set(wx + 1, wy - 1, wave); c.Set(wx + 2, wy - 1, wave); c.Set(wx + 3, wy, wave);
            }
            c.Set(rng.Next(0, T), rng.Next(0, T), spark);
            return c;
        }

        /// <summary>木の橋（たての板・すきま・くぎ）。こわれた橋は、まん中に穴があいて水が見える</summary>
        private static PixelArt.Canvas Planks(bool broken)
        {
            var c = new PixelArt.Canvas(T, T);
            Color32 wood = C(160, 108, 60), grain = C(135, 88, 48), gap = C(85, 55, 30), nail = C(200, 200, 210), hi = C(190, 135, 80);
            Color32 water = C(50, 110, 200), waterHi = C(110, 170, 240);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int lx = x % 4;
                    Color32 col = lx == 3 ? gap : lx == 0 ? hi : (y + x * 3) % 7 == 0 ? grain : wood;
                    c.Px[x, y] = col;
                }
            for (int x = 1; x < T; x += 4) { c.Set(x, 2, nail); c.Set(x, 13, nail); }
            if (broken)
            {
                // ギザギザの穴
                for (int y = 3; y <= 12; y++)
                {
                    int edge = (y % 3 == 0) ? 3 : (y % 3 == 1 ? 2 : 4);
                    for (int x = edge; x < T - edge; x++)
                        c.Px[x, y] = (x + y) % 5 == 0 ? waterHi : water;
                }
                for (int x = 2; x < T - 2; x += 3) { c.Set(x, 3, gap); c.Set(x + 1, 12, gap); }
            }
            return c;
        }

        /// <summary>出口の門：石のアーチと、光る入り口</summary>
        private static PixelArt.Canvas GateArt()
        {
            var c = new PixelArt.Canvas(T, T);
            Color32 stone = C(160, 150, 135), dark = C(105, 98, 90), hi = C(200, 192, 175), glow = C(255, 220, 120), glow2 = C(255, 245, 200);
            c.Rect(0, 0, T, T, C(110, 100, 90));
            c.Rect(1, 2, 3, 14, stone); c.Rect(12, 2, 3, 14, stone);
            c.Rect(1, 1, 14, 3, stone);
            c.Rect(1, 1, 14, 1, hi);
            for (int y = 4; y < T; y += 3) { c.Rect(1, y, 3, 1, dark); c.Rect(12, y, 3, 1, dark); }
            c.Rect(4, 4, 8, 12, glow);
            c.Rect(6, 6, 4, 10, glow2);
            c.Set(4, 4, stone); c.Set(11, 4, stone);
            return c;
        }
    }
}
