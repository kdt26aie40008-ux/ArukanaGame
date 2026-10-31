using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 町の仮のドット絵（建物・草むら・噴水・依頼板・目的地・資材）。1マス = 16 ドットで、プログラムで描いています。
    /// 本番の画像ができたら、ここで返す Sprite を差し替えてください。
    /// </summary>
    public static class TownArt
    {
        private const int Px = 16; // 1マス = 16ドット
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        private static Sprite Cached(string key, System.Func<PixelArt.Canvas> make, int ppu = 0)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = PixelArt.ToSprite(make(), key, ppu);
            cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------
        // 建物
        // ------------------------------------------------------------------

        /// <summary>
        /// 建物：かわらの屋根・えんとつ・木の柱のかべ・石の土台・わく付きの窓と花のプランター・アーチのドア。
        /// 直る前は灰色で、屋根に穴・ひび・板を打ちつけた窓・工事の足場がある
        /// </summary>
        public static Sprite Building(BuildingInfo b, bool repaired) =>
            Cached($"bld{b.Id}_{repaired}", () => b.Function == BuildingFunction.Ship ? DrawShip(b, repaired) : DrawBuilding(b, repaired), Px);

        /// <summary>
        /// 大きな船（横から見た形）：木の船体・板のすじ・手すり・船室・マストと帆・旗。
        /// こわれた船は、船体に穴・板を打ちつけた あと・マストが折れて 帆が やぶれている
        /// </summary>
        private static PixelArt.Canvas DrawShip(BuildingInfo b, bool repaired)
        {
            int w = b.W * Px, h = b.H * Px;
            var c = new PixelArt.Canvas(w, h);
            Color32 hull = repaired ? C(150, 95, 50) : C(105, 85, 70), hullDark = PixelArt.Mul(hull, 0.68f), hullLight = PixelArt.Add(hull, 30);
            Color32 trim = repaired ? C(b.Roof.r, b.Roof.g, b.Roof.b) : C(110, 100, 95);
            Color32 water = C(60, 125, 210), foam = C(200, 230, 255);
            Color32 sail = repaired ? C(248, 244, 232) : C(170, 165, 150), sailShade = PixelArt.Mul(sail, 0.85f);
            Color32 mast = C(110, 75, 40), cabin = repaired ? C(225, 200, 150) : C(140, 130, 115);

            // 船体（下ほど細い）
            int deck = h - 20, keel = h - 4;
            for (int y = deck; y <= keel; y++)
            {
                int inset = (y - deck) * 6 / (keel - deck + 1);
                for (int x = 2 + inset; x < w - 2 - inset; x++)
                    c.Set(x, y, (y - deck) % 4 == 3 ? hullDark : hull);
            }
            for (int x = 2; x < w - 2; x++) { c.Set(x, deck, trim); c.Set(x, deck + 1, hullLight); }  // ふなべりの色帯
            for (int x = 4; x < w - 4; x += 5) c.Set(x, deck - 2, hullDark);                          // 手すりの柱
            for (int x = 3; x < w - 3; x++) c.Set(x, deck - 3, hullDark);                              // 手すり
            // 水面と波
            for (int x = 0; x < w; x++) { c.Set(x, h - 3, water); c.Set(x, h - 2, water); c.Set(x, h - 1, water); }
            for (int x = 1; x < w; x += 6) { c.Set(x, h - 3, foam); c.Set(x + 1, h - 3, foam); }
            // 船室（後ろ＝右がわ）
            c.Rect(w - 26, deck - 10, 18, 8, cabin);
            c.Rect(w - 26, deck - 11, 18, 1, trim);
            for (int x = w - 23; x < w - 10; x += 5) c.Rect(x, deck - 7, 3, 3, repaired ? C(130, 200, 245) : C(45, 45, 52));

            int mx = w / 2 - 6;
            if (repaired)
            {
                // マスト・大きな帆・旗
                c.Rect(mx, 2, 2, deck - 2, mast);
                for (int y = 5; y < deck - 5; y++)
                {
                    int half = 4 + (y - 5) * 10 / (deck - 10);
                    for (int x = mx - half; x <= mx + half + 1; x++) c.Set(x, y, x <= mx ? sail : sailShade);
                }
                c.Rect(mx - 3, 12, 8, 3, trim); // 帆のもよう
                c.Rect(mx + 2, 1, 7, 4, C(230, 60, 60));
                c.Set(mx + 8, 2, C(250, 220, 80));
            }
            else
            {
                // 折れたマスト・やぶれた帆・船体の穴と 打ちつけた板
                c.Rect(mx, deck - 14, 2, 14, mast);
                for (int k = 0; k < 10; k++) c.Set(mx + 2 + k, deck - 15 + k / 2, mast);   // 折れて たおれたマスト
                for (int y = deck - 12; y < deck - 4; y++)
                    for (int x = mx - 8; x < mx; x++) if ((x * 3 + y * 5) % 7 > 1) c.Set(x, y, sail); // やぶれた帆
                var rng = new System.Random(b.Id * 13);
                for (int i = 0; i < 4; i++)
                {
                    int hx = rng.Next(8, w - 14), hy = rng.Next(deck + 3, keel - 4);
                    c.Rect(hx, hy, 5, 3, C(25, 22, 22));
                }
                for (int k = 0; k < 10; k++) { c.Set(10 + k, deck + 4 + k / 3, C(185, 135, 75)); c.Set(w - 20 + k, deck + 8 - k / 3, C(185, 135, 75)); }
            }
            return c;
        }

        /// <summary>小舟（横2マス）：木のボートと オール</summary>
        public static Sprite Rowboat => Cached("rowboat", () =>
        {
            var c = new PixelArt.Canvas(Px * 2, Px);
            Color32 wood = C(160, 105, 55), dark = C(105, 65, 30), light = C(200, 145, 85), inside = C(120, 80, 45);
            for (int y = 5; y < 13; y++)
            {
                int inset = System.Math.Max(0, (y - 9) * 2);
                for (int x = 2 + inset; x < Px * 2 - 2 - inset; x++) c.Set(x, y, y == 5 ? light : y > 10 ? dark : wood);
            }
            c.Rect(5, 6, Px * 2 - 10, 3, inside);
            for (int x = 7; x < Px * 2 - 7; x += 6) c.Rect(x, 6, 1, 3, wood);   // 腰かけ
            for (int k = 0; k < 9; k++) c.Set(10 + k, 2 + k / 2, dark);          // オール
            return c;
        }, Px);

        private static PixelArt.Canvas DrawBuilding(BuildingInfo b, bool repaired)
        {
            int w = b.W * Px, h = b.H * Px;
            var c = new PixelArt.Canvas(w, h);
            var rng = new System.Random(b.Id * 31);
            var wc = TownData.Villages[b.Village].WallColor; // 村ごとの かべの色

            Color32 roof = repaired ? new Color32(b.Roof.r, b.Roof.g, b.Roof.b, 255) : C(98, 94, 92);
            Color32 roofDark = PixelArt.Mul(roof, 0.72f), roofLight = PixelArt.Mul(PixelArt.Add(roof, 15), 1.15f);
            Color32 wall = repaired ? C(wc.r, wc.g, wc.b) : C(146, 141, 134);
            Color32 wallShade = PixelArt.Mul(wall, 0.88f);
            Color32 beam = repaired ? C(120, 78, 45) : C(92, 86, 80);
            Color32 stone = repaired ? C(150, 145, 140) : C(110, 106, 102);
            Color32 stoneDark = PixelArt.Mul(stone, 0.7f);
            Color32 glass = repaired ? C(130, 200, 245) : C(45, 45, 52);
            Color32 glassHi = repaired ? C(220, 245, 255) : C(70, 70, 78);

            int roofH = Mathf.Max(14, h * 2 / 5);
            int wallTop = roofH;

            // かべ（しっくい＋木の柱）
            c.Rect(0, wallTop, w, h - wallTop, wall);
            for (int y = wallTop; y < h; y++) c.Set(w - 1, y, wallShade);
            for (int x = 0; x < w; x += Px) c.Rect(x, wallTop, 2, h - wallTop, beam);
            c.Rect(w - 2, wallTop, 2, h - wallTop, beam);
            c.Rect(0, wallTop + (h - wallTop) / 2 - 1, w, 2, beam);
            c.Rect(0, wallTop + 2, w, 2, PixelArt.Mul(wall, 0.75f)); // 屋根の下のかげ

            // 石の土台
            c.Rect(0, h - 4, w, 4, stone);
            for (int x = 0; x < w; x += 6) c.Set(x + ((x / 6) % 2) * 3, h - 2, stoneDark);
            c.Rect(0, h - 4, w, 1, PixelArt.Add(stone, 25));

            // 屋根（台形。かわらの段・すじ・てっぺんの明るい線・のきの暗い線）
            for (int y = 0; y < roofH; y++)
            {
                int inset = (roofH - 1 - y) * w / (roofH * 5);
                for (int x = inset; x < w - inset; x++)
                {
                    Color32 col = y % 4 == 3 ? roofDark : roof;
                    if (y % 4 != 3 && (x + (y / 4) * 3) % 6 == 0) col = roofDark;
                    if (y == 0) col = roofLight;
                    c.Px[x, y] = col;
                }
            }
            c.Rect(0, roofH - 1, w, 2, PixelArt.Mul(roof, 0.5f));

            // えんとつ
            int chx = w - Px - 4;
            c.Rect(chx, 0, 6, roofH / 2, repaired ? C(150, 80, 65) : C(100, 95, 92));
            c.Rect(chx - 1, 0, 8, 2, PixelArt.Mul(stone, 0.9f));

            // ドア（アーチ形・板・金のとって・石段）
            int doorCx = (b.W / 2) * Px + Px / 2;
            int doorW = 10, doorH = Mathf.Min(16, h - wallTop - 4);
            int dx = doorCx - doorW / 2, dy = h - 4 - doorH;
            Color32 door = repaired ? C(135, 82, 42) : C(55, 50, 46);
            c.Rect(dx - 1, dy - 1, doorW + 2, doorH + 1, beam);
            c.Rect(dx, dy, doorW, doorH, door);
            c.Set(dx, dy, beam); c.Set(dx + doorW - 1, dy, beam);
            for (int x = dx + 3; x < dx + doorW; x += 3) c.Rect(x, dy + 1, 1, doorH - 1, PixelArt.Mul(door, 0.75f));
            if (repaired) c.Rect(dx + doorW - 3, dy + doorH / 2, 2, 2, C(240, 200, 70));
            c.Rect(dx - 2, h - 4, doorW + 4, 2, PixelArt.Add(stone, 15));

            // 窓（わく・十字・光の反射・花のプランター）
            int wy = wallTop + 6;
            for (int x = 5; x + 9 <= w - 4; x += Px + 2)
            {
                if (x + 9 > dx - 2 && x < dx + doorW + 2) continue; // ドアとかさならないように
                c.Rect(x - 1, wy - 1, 10, 10, beam);
                c.Rect(x, wy, 8, 8, glass);
                c.Rect(x + 3, wy, 2, 8, beam);
                c.Rect(x, wy + 3, 8, 2, beam);
                c.Set(x + 1, wy + 1, glassHi); c.Set(x + 2, wy + 1, glassHi); c.Set(x + 1, wy + 2, glassHi);
                if (repaired)
                {
                    c.Rect(x - 1, wy + 9, 10, 2, C(120, 78, 45));
                    for (int f = 0; f < 4; f++) c.Set(x + f * 2, wy + 8, f % 2 == 0 ? C(235, 70, 90) : C(255, 215, 70));
                }
                else
                {
                    // 板を打ちつけた窓
                    for (int k = 0; k < 8; k++) { c.Set(x + k, wy + k, C(150, 110, 70)); c.Set(x + 7 - k, wy + k, C(150, 110, 70)); }
                }
            }

            if (!repaired)
            {
                Color32 crack = C(60, 56, 54);
                for (int i = 0; i < 7; i++)
                {
                    int cx = rng.Next(3, w - 3), cy = rng.Next(wallTop + 3, h - 6);
                    for (int k = 0; k < 6; k++) c.Set(cx + k / 2 * (k % 2 == 0 ? 1 : -1), cy + k, crack);
                }
                // 屋根の穴
                for (int i = 0; i < 3; i++)
                {
                    int hx = rng.Next(w / 5, w - w / 5 - 6), hy = rng.Next(3, roofH - 5);
                    c.Rect(hx, hy, 6, 3, C(30, 28, 30));
                    c.Rect(hx + 1, hy + 3, 4, 1, C(30, 28, 30));
                }
                // 工事の足場（柱・横木・ななめの支え）
                Color32 wood = C(185, 130, 65), woodDark = C(130, 88, 40);
                for (int x = 2; x < w; x += 14) c.Rect(x, wallTop - 4, 2, h - wallTop + 4, wood);
                c.Rect(0, wallTop + 8, w, 2, wood);
                c.Rect(0, h - 10, w, 2, woodDark);
                for (int k = 0; k < 12 && k < w; k++) c.Set(2 + k, wallTop + 10 + k / 2, woodDark);
            }
            return c;
        }

        // ------------------------------------------------------------------
        // 地面のもの（16x16）
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        // 町の高さ：高い所の地面・がけ・石の階段（村の景色の色に合わせる）
        // ------------------------------------------------------------------

        /// <summary>高い所の地面の色（段が高いほど 明るい）</summary>
        private static Color32 LevelFloor(int theme, int level) =>
            PixelArt.Add(PixelArt.Mul(SpriteFactory.ThemeColor(theme, 0), 1f + 0.07f * level), 6 * level);

        /// <summary>高い所の地面：段が高いほど 明るく、小さな石つぶ</summary>
        public static Sprite HighGround(int theme, int level) => Cached($"high{theme}_{level}", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 floor = LevelFloor(theme, level);
            Color32 dot = PixelArt.Mul(floor, 0.88f), hi = PixelArt.Add(floor, 18);
            c.Rect(0, 0, Px, Px, floor);
            var rng = new System.Random(11 + theme);
            for (int i = 0; i < 10; i++) c.Set(rng.Next(Px), rng.Next(Px), i % 3 == 0 ? hi : dot);
            return c;
        });

        /// <summary>がけの正面（H）：上のふちは その段の地面、岩の かべに 横のすじと たてのひび、下は かげ</summary>
        public static Sprite Cliff(int theme, int level) => Cached($"cliff{theme}_{level}", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 lip = LevelFloor(theme, level);
            Color32 rock = PixelArt.Mix(SpriteFactory.ThemeColor(theme, 1), C(120, 105, 90), 0.45f);
            Color32 rockLight = PixelArt.Add(rock, 22), rockDark = PixelArt.Mul(rock, 0.7f), shadow = PixelArt.Mul(rock, 0.5f);
            c.Rect(0, 0, Px, Px, rock);
            c.Rect(0, 0, Px, 3, lip);                       // 上のふち（高い所の地面）
            c.Rect(0, 3, Px, 1, rockLight);
            for (int y = 6; y < Px - 2; y += 4)               // 岩の横のすじ
                for (int x = 0; x < Px; x++) if ((x + y) % 5 != 0) c.Set(x, y, rockDark);
            var rng = new System.Random(7 + theme);
            for (int i = 0; i < 3; i++)                       // たてのひび
            {
                int x = rng.Next(1, Px - 1), y0 = rng.Next(4, 8);
                for (int k = 0; k < 5; k++) c.Set(x + (k % 2), y0 + k, rockDark);
                c.Set(x - 1, y0, rockLight);
            }
            c.Rect(0, Px - 2, Px, 2, shadow);                 // 下のかげ
            return c;
        });

        /// <summary>石の階段（L）：明るいふみ板と 暗い けこみが 4段。左右に石のふち</summary>
        public static Sprite Stairs(int theme) => Cached("stairs" + theme, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 step = PixelArt.Mix(SpriteFactory.ThemeColor(theme, 0), C(200, 195, 185), 0.5f);
            Color32 stepHi = PixelArt.Add(step, 25), riser = PixelArt.Mul(step, 0.62f), side = PixelArt.Mix(SpriteFactory.ThemeColor(theme, 1), C(120, 105, 90), 0.45f);
            for (int s = 0; s < 4; s++)
            {
                int y = s * 4;
                c.Rect(0, y, Px, 3, step);
                c.Rect(0, y, Px, 1, stepHi);
                c.Rect(0, y + 3, Px, 1, riser);
            }
            c.Rect(0, 0, 2, Px, side);
            c.Rect(Px - 2, 0, 2, Px, PixelArt.Mul(side, 0.8f));
            return c;
        });

        /// <summary>横向きの石の階段（l）：たての段が 4つ。上下に石のふち</summary>
        public static Sprite StairsSide(int theme) => Cached("stairsS" + theme, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 step = PixelArt.Mix(SpriteFactory.ThemeColor(theme, 0), C(200, 195, 185), 0.5f);
            Color32 stepHi = PixelArt.Add(step, 25), riser = PixelArt.Mul(step, 0.62f), side = PixelArt.Mix(SpriteFactory.ThemeColor(theme, 1), C(120, 105, 90), 0.45f);
            for (int s = 0; s < 4; s++)
            {
                int x = s * 4;
                c.Rect(x, 0, 3, Px, step);
                c.Rect(x, 0, 1, Px, stepHi);
                c.Rect(x + 3, 0, 1, Px, riser);
            }
            c.Rect(0, 0, Px, 2, side);
            c.Rect(0, Px - 2, Px, 2, PixelArt.Mul(side, 0.8f));
            return c;
        });

        /// <summary>坂道（/）：上が明るく 下が暗い、ななめの すべり止めの線</summary>
        public static Sprite Slope(int theme, int level) => Cached($"slope{theme}_{level}", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 top = LevelFloor(theme, level), bottom = LevelFloor(theme, System.Math.Max(0, level - 1));
            for (int y = 0; y < Px; y++)
                c.Rect(0, y, Px, 1, PixelArt.Mix(top, PixelArt.Mul(bottom, 0.9f), y / (float)(Px - 1)));
            for (int y = 2; y < Px; y += 4)
                for (int x = 0; x < Px; x++) if ((x + y) % 8 < 3) c.Set(x, y, PixelArt.Mul(top, 0.8f));
            return c;
        });

        /// <summary>がけのふち（0=左 1=右 2=上 が低い）：暗い岩の線と 明るい内がわの線（重ねて使う）</summary>
        public static Sprite Rim(int side) => Cached("rim" + side, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 dark = C(78, 66, 58), mid = C(118, 102, 88), hi = C(232, 222, 196);
            for (int i = 0; i < Px; i++)
            {
                if (side == 0) { c.Set(0, i, dark); c.Set(1, i, mid); c.Set(2, i, mid); c.Set(3, i, hi); }
                else if (side == 1) { c.Set(Px - 1, i, dark); c.Set(Px - 2, i, mid); c.Set(Px - 3, i, mid); c.Set(Px - 4, i, hi); }
                else { c.Set(i, 0, dark); c.Set(i, 1, mid); c.Set(i, 2, hi); }
            }
            return c;
        });

        /// <summary>はしご（がけの正面に かける。重ねて使う）</summary>
        public static Sprite Ladder => Cached("ladder", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 wood = C(170, 115, 60), dark = C(110, 70, 35);
            c.Rect(3, 0, 2, Px, wood); c.Rect(11, 0, 2, Px, wood);
            c.Rect(4, 0, 1, Px, dark); c.Rect(12, 0, 1, Px, dark);
            for (int y = 2; y < Px; y += 4) { c.Rect(3, y, 10, 2, wood); c.Rect(3, y + 1, 10, 1, dark); }
            return c;
        });

        /// <summary>がけの下のかげ（上のほうだけ。SpriteRenderer の色で うすくする）</summary>
        public static Sprite CliffShadow => Cached("cliffshadow", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Rect(0, 0, Px, 5, C(0, 0, 0));
            for (int x = 0; x < Px; x += 2) c.Set(x, 5, C(0, 0, 0));
            return c;
        });

        /// <summary>草むら（アルカナが出る）：背の高い葉が3色でしげる</summary>
        public static Sprite TallGrass => Cached("tallgrass", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 bg = C(58, 135, 62), dark = C(30, 92, 40), mid = C(70, 160, 70), tip = C(140, 210, 110);
            c.Rect(0, 0, Px, Px, bg);
            var rng = new System.Random(5);
            for (int i = 0; i < 9; i++)
            {
                int x = rng.Next(0, Px), baseY = Px - 1 - rng.Next(0, 4), len = rng.Next(6, 11);
                int lean = rng.Next(-1, 2);
                for (int k = 0; k < len; k++)
                {
                    int px = x + (k > len / 2 ? lean : 0);
                    c.Set(px, baseY - k, k < 2 ? dark : k >= len - 2 ? tip : mid);
                }
            }
            return c;
        });

        /// <summary>枯れ草むら（砂の国のアルカナが出る）：砂の上に、かわいた黄土色の草が しげる。小枝や 小石も まじる</summary>
        public static Sprite DryGrass => Cached("drygrass", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 sand = C(222, 196, 140), sandDark = C(196, 166, 110), dark = C(140, 100, 55), mid = C(190, 150, 80), tip = C(236, 212, 140), pebble = C(160, 140, 115);
            c.Rect(0, 0, Px, Px, sand);
            var rng = new System.Random(11);
            for (int i = 0; i < 10; i++) c.Set(rng.Next(0, Px), rng.Next(0, Px), sandDark); // 砂のつぶ
            c.Set(3, 13, pebble); c.Set(4, 13, pebble); c.Set(12, 4, pebble);
            for (int i = 0; i < 10; i++)
            {
                int x = rng.Next(0, Px), baseY = Px - 1 - rng.Next(0, 5), len = rng.Next(4, 9);
                int lean = rng.Next(0, 2) == 0 ? -1 : 1; // 枯れ草は くたっと ななめに たおれる
                for (int k = 0; k < len; k++)
                {
                    int px = x + (k >= len / 2 ? lean : 0) + (k >= len - 2 ? lean : 0);
                    c.Set(px, baseY - k, k < 2 ? dark : k >= len - 2 ? tip : mid);
                }
            }
            // 枯れた小枝
            for (int k = 0; k < 5; k++) c.Set(8 + k, 10 - k / 2, dark);
            c.Set(10, 8, dark); c.Set(10, 7, dark);
            return c;
        });

        /// <summary>枯れ木（砂の国の草むらに立つ）：葉のない ねじれた幹と、ほそい枝</summary>
        public static Sprite DeadTree => Cached("deadtree", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 bark = C(125, 95, 68), dark = C(82, 60, 42), light = C(165, 132, 98), shadow = C(150, 125, 85);
            // 足元のかげ・根っこ
            c.Rect(4, 15, 8, 1, shadow);
            c.Rect(5, 14, 6, 1, dark);
            c.Set(4, 15, dark); c.Set(11, 15, dark);
            // 幹（下のほうで 少し左へ ねじれる）
            for (int y = 7; y <= 13; y++)
            {
                int x = y >= 11 ? 7 : 8;
                c.Set(x - 1, y, dark); c.Set(x, y, bark); c.Set(x + 1, y, light);
            }
            // 太い枝：左上と右上へ
            int[,] left = { { 7, 7 }, { 6, 6 }, { 5, 5 }, { 4, 4 }, { 3, 3 }, { 3, 2 } };
            int[,] right = { { 9, 6 }, { 10, 5 }, { 11, 4 }, { 12, 3 }, { 12, 2 }, { 13, 1 } };
            for (int i = 0; i < left.GetLength(0); i++) { c.Set(left[i, 0], left[i, 1], bark); c.Set(left[i, 0], left[i, 1] + 1, dark); }
            for (int i = 0; i < right.GetLength(0); i++) { c.Set(right[i, 0], right[i, 1], bark); c.Set(right[i, 0], right[i, 1] + 1, dark); }
            // まん中の枝と、ほそい小枝
            c.Set(8, 6, bark); c.Set(8, 5, bark); c.Set(9, 4, bark); c.Set(9, 3, dark); c.Set(7, 4, dark);
            c.Set(2, 4, dark); c.Set(1, 3, dark);          // 左の小枝
            c.Set(5, 3, dark); c.Set(5, 2, dark);          // 左の枝の先の小枝
            c.Set(13, 4, dark); c.Set(14, 3, dark);        // 右の小枝
            c.Set(11, 2, dark); c.Set(10, 1, dark);
            return c;
        });

        /// <summary>噴水：石のふちと、きらきらした水・まん中の水しぶき</summary>
        public static Sprite Fountain => Cached("fountain", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 stone = C(175, 175, 185), stoneDark = C(120, 120, 132), water = C(85, 155, 230), light = C(190, 230, 255);
            c.Rect(0, 0, Px, Px, stone);
            c.Rect(0, Px - 2, Px, 2, stoneDark);
            c.Rect(Px - 2, 0, 2, Px, stoneDark);
            c.Rect(2, 2, Px - 4, Px - 4, water);
            foreach (var (x, y) in new[] { (4, 5), (9, 4), (6, 10), (11, 9), (3, 12) }) { c.Set(x, y, light); c.Set(x + 1, y, light); }
            c.Rect(7, 6, 2, 4, light);
            c.Set(6, 5, light); c.Set(9, 5, light);
            return c;
        });

        /// <summary>依頼板：2本の柱と木の板、ピンでとめた紙</summary>
        public static Sprite Board => Cached("board", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 wood = C(160, 108, 58), dark = C(100, 62, 32), light = C(195, 140, 80), paper = C(248, 240, 210), ink = C(120, 110, 100), pin = C(220, 60, 60);
            c.Rect(2, 8, 2, 8, dark); c.Rect(12, 8, 2, 8, dark);
            c.Rect(0, 2, 16, 10, wood);
            c.Rect(0, 2, 16, 1, light);
            c.Rect(0, 11, 16, 1, dark);
            c.Rect(0, 0, 16, 2, dark);
            c.Rect(2, 4, 5, 6, paper); c.Rect(9, 3, 5, 7, paper);
            for (int y = 6; y < 10; y += 2) { c.Rect(3, y, 3, 1, ink); c.Rect(10, y, 3, 1, ink); }
            c.Set(4, 4, pin); c.Set(11, 3, pin);
            PixelArt.Outline(c);
            return c;
        });

        /// <summary>遺跡の入り口（クエストの目的地）：石のアーチと、青く光る入り口・ルーン文字</summary>
        public static Sprite RuinGate => Cached("ruingate", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 stone = C(150, 145, 135), dark = C(100, 95, 90), hi = C(195, 190, 178), glow = C(90, 200, 255), glow2 = C(200, 245, 255), rune = C(255, 230, 120);
            c.Rect(1, 3, 4, 13, stone); c.Rect(11, 3, 4, 13, stone);
            c.Rect(0, 0, 16, 4, stone);
            c.Rect(0, 0, 16, 1, hi);
            c.Rect(0, 3, 16, 1, dark);
            for (int y = 6; y < 16; y += 4) { c.Rect(1, y, 4, 1, dark); c.Rect(11, y, 4, 1, dark); }
            c.Rect(5, 4, 6, 12, glow);
            c.Rect(6, 6, 4, 10, glow2);
            c.Set(3, 1, rune); c.Set(7, 1, rune); c.Set(8, 2, rune); c.Set(12, 1, rune);
            PixelArt.Outline(c);
            return c;
        });

        /// <summary>目的地の上で ふわふわ動く印（下向きの矢印）</summary>
        public static Sprite Marker => Cached("marker", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 y = C(255, 215, 60), o = C(225, 140, 30), hi = C(255, 245, 170);
            c.Rect(5, 1, 6, 7, y);
            c.Rect(5, 1, 2, 7, hi);
            for (int row = 0; row < 7; row++) c.Rect(1 + row, 8 + row, 14 - row * 2, 1, row == 0 ? hi : y);
            c.Rect(7, 14, 2, 1, o);
            PixelArt.Shade(c);
            PixelArt.Outline(c, 0.3f);
            return c;
        });

        /// <summary>風船割りの風船（白。画面で色をつけて使う）：光・むすび目・ひも</summary>
        public static Sprite BalloonWhite => Cached("balloon", () =>
        {
            var c = new PixelArt.Canvas(32, 37);
            Color32 w = C(250, 250, 250), sh = C(205, 205, 215), hi = C(255, 255, 255), str = C(120, 120, 130);
            c.Ellipse(16, 14, 13, 14, sh);
            c.Ellipse(15, 13, 12, 13, w);
            c.Ellipse(10, 8, 3, 4, hi);
            c.Rect(14, 28, 4, 2, sh);
            c.Rect(15, 30, 2, 1, sh);
            for (int y = 31; y < 37; y++) c.Set(16 + (y % 2), y, str);
            return c;
        });

        /// <summary>町の風船割り屋（色とりどりの風船を、柱にむすんでいる）</summary>
        public static Sprite BalloonStand => Cached("balloonstand", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 wood = C(130, 85, 45), str = C(230, 230, 230);
            c.Rect(7, 8, 2, 8, wood);
            c.Rect(4, 14, 8, 2, wood);
            foreach (var (x, y, col) in new[] { (4, 3, C(235, 80, 90)), (8, 1, C(90, 160, 255)), (12, 4, C(250, 210, 60)), (6, 6, C(110, 210, 110)) })
            {
                c.Ellipse(x + 0.5f, y + 0.5f, 2.3f, 2.6f, col);
                c.Set(x - 1, y - 1, C(255, 255, 255));
                c.Set(x, y + 3, str); c.Set(Mathf.Clamp(x + (8 - x) / 2, 0, 15), y + 4, str);
            }
            PixelArt.Outline(c, 0.3f);
            return c;
        });

        /// <summary>装備の素材のアイコン（ルーン鉱石・風の結晶・星くずの石・虹の宝石）</summary>
        public static Sprite CraftIcon(CraftMaterial m) => Cached("craft" + (int)m, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            switch (m)
            {
                case CraftMaterial.RuneOre: // 灰色の石に、青く光るルーン
                    c.Ellipse(8, 9, 6.5f, 5.5f, C(120, 120, 135));
                    c.Ellipse(7, 8, 4, 3, C(150, 150, 165));
                    c.Set(6, 8, C(90, 200, 255)); c.Set(7, 9, C(90, 200, 255)); c.Set(10, 10, C(90, 200, 255)); c.Set(9, 7, C(160, 230, 255));
                    break;
                case CraftMaterial.WindCrystal: // うすみどりの結晶
                    for (int y = 1; y < 15; y++)
                    {
                        int half = y < 7 ? y / 2 + 1 : (15 - y) / 2 + 1;
                        c.Rect(8 - half, y, half * 2, 1, C(130, 230, 170));
                        c.Set(8 + half - 1, y, C(80, 180, 130));
                    }
                    c.Rect(7, 4, 1, 6, C(220, 255, 235));
                    c.Set(2, 4, C(200, 255, 230)); c.Set(13, 10, C(200, 255, 230));
                    break;
                case CraftMaterial.StarStone: // 星がちらばった黄色い石
                    c.Ellipse(8, 9, 6, 5.5f, C(210, 170, 70));
                    c.Ellipse(7, 8, 3.5f, 3, C(240, 205, 100));
                    foreach (var (x, y) in new[] { (5, 7), (10, 9), (8, 12) }) { c.Set(x, y, C(255, 250, 200)); c.Set(x + 1, y, C(255, 250, 200)); c.Set(x, y - 1, C(255, 250, 200)); }
                    break;
                default: // 虹の宝石
                {
                    var rainbow = new[] { C(235, 80, 80), C(245, 160, 60), C(240, 220, 80), C(90, 200, 110), C(80, 150, 235), C(160, 100, 220) };
                    for (int y = 2; y < 14; y++)
                    {
                        int half = y < 6 ? y + 1 : (14 - y) * 6 / 8 + 1;
                        c.Rect(8 - half, y, half * 2, 1, rainbow[(y - 2) / 2 % rainbow.Length]);
                    }
                    c.Rect(6, 4, 2, 2, C(255, 255, 255));
                    break;
                }
            }
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>遺跡の回復の泉（砂の遺跡はヤシの木のオアシス、雪の遺跡は湯気の出る温泉）</summary>
        public static Sprite Spring(int chapter) => Cached("spring" + chapter, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 stone = C(160, 155, 150), water = chapter == 4 ? C(120, 200, 210) : C(80, 160, 235), hi = C(200, 240, 255);
            c.Ellipse(8, 9.5f, 7.5f, 5.5f, chapter == 2 ? C(220, 190, 120) : stone);
            c.Ellipse(8, 9.5f, 6, 4, water);
            c.Set(5, 8, hi); c.Set(6, 8, hi); c.Set(10, 11, hi);
            if (chapter == 2)
            {
                // ヤシの木
                c.Rect(13, 3, 1, 6, C(140, 95, 50));
                foreach (var (x, y) in new[] { (11, 2), (12, 1), (14, 1), (15, 2), (13, 2) }) c.Set(x, y, C(60, 160, 70));
            }
            else if (chapter == 4)
            {
                // 湯気
                foreach (var (x, y) in new[] { (5, 3), (6, 2), (5, 1), (10, 4), (11, 3), (10, 2) }) c.Set(x, y, C(240, 240, 245));
            }
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>キラキラ光る地面（調べると何か見つかる）</summary>
        public static Sprite Sparkle => Cached("sparkle", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 y = C(255, 230, 90), w = C(255, 255, 220);
            foreach (var (cx, cy, s) in new[] { (5, 6, 2), (11, 9, 3), (7, 12, 1) })
            {
                c.Rect(cx - s, cy, s * 2 + 1, 1, y);
                c.Rect(cx, cy - s, 1, s * 2 + 1, y);
                c.Set(cx, cy, w);
            }
            c.Rect(4, 13, 8, 2, C(150, 110, 70)); // 少し盛り上がった土
            return c;
        });

        /// <summary>村を歩き回るねこ（茶トラ）</summary>
        public static Sprite Cat => ArtOverrides.Character("ねこ") ?? Cached("cat", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 fur = C(240, 160, 70), stripe = C(200, 110, 40), white = C(250, 245, 235), eye = C(40, 40, 50), nose = C(240, 120, 140);
            c.Ellipse(9, 11, 5.5f, 3.5f, fur);            // からだ
            for (int x = 6; x <= 12; x += 3) c.Rect(x, 9, 1, 3, stripe);
            c.Ellipse(5, 7, 3.6f, 3.2f, fur);             // 頭
            c.Rect(2, 3, 2, 2, fur); c.Rect(7, 3, 2, 2, fur); // 耳
            c.Set(2, 3, stripe); c.Set(8, 3, stripe);
            c.Set(4, 7, eye); c.Set(6, 7, eye); c.Set(5, 8, nose);
            c.Rect(4, 9, 3, 1, white);
            c.Rect(5, 13, 2, 2, fur); c.Rect(11, 13, 2, 2, fur); // 足
            c.Rect(14, 7, 1, 4, fur); c.Set(15, 6, stripe);     // しっぽ
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>畑の土（うねのすじ入り）</summary>
        public static Sprite Soil => Cached("soil", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 soil = C(125, 85, 52), dark = C(95, 62, 38), light = C(150, 108, 70), edge = C(80, 120, 60);
            c.Rect(0, 0, Px, Px, soil);
            for (int y = 3; y < Px; y += 4) { c.Rect(1, y, Px - 2, 1, dark); c.Rect(1, y - 1, Px - 2, 1, light); }
            c.Rect(0, 0, Px, 1, edge); c.Rect(0, Px - 1, Px, 1, edge); // ふちの草
            return c;
        });

        /// <summary>畑の作物（1：芽 2：葉っぱ 3：収穫できる薬草）</summary>
        public static Sprite Plant(int stage) => Cached("plant" + stage, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 stem = C(70, 150, 60), leaf = C(110, 200, 80), hi = C(170, 235, 120), berry = C(230, 70, 90), flower = C(255, 220, 80);
            if (stage <= 1)
            {
                c.Rect(7, 10, 2, 4, stem);
                c.Ellipse(5.5f, 10, 2, 1.2f, leaf); c.Ellipse(10.5f, 9.5f, 2, 1.2f, leaf);
            }
            else if (stage == 2)
            {
                c.Rect(7, 6, 2, 8, stem);
                c.Ellipse(4.5f, 10, 3, 1.6f, leaf); c.Ellipse(11.5f, 9, 3, 1.6f, leaf);
                c.Ellipse(5.5f, 6.5f, 2.2f, 1.3f, leaf); c.Ellipse(10.5f, 5.5f, 2.2f, 1.3f, leaf);
                c.Set(4, 9, hi); c.Set(11, 8, hi);
            }
            else
            {
                c.Rect(7, 5, 2, 9, stem);
                c.Ellipse(8, 9, 6, 4.2f, leaf);
                c.Ellipse(8, 5, 4, 3, leaf);
                for (int i = 0; i < 6; i++) c.Set(4 + i * 2 % 9, 6 + (i * 5) % 6, hi);
                c.Ellipse(4, 8, 1.2f, 1.2f, berry); c.Ellipse(12, 10, 1.2f, 1.2f, berry); c.Ellipse(9, 12, 1.2f, 1.2f, berry);
                c.Ellipse(8, 2.5f, 1.8f, 1.8f, flower);
            }
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>図鑑のボタンの絵（虫のマークの本）</summary>
        public static Sprite DexIcon => Cached("dexicon", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 cover = C(70, 120, 200), dark = C(40, 70, 130), page = C(245, 240, 225), bug = C(120, 220, 120), eye = C(30, 30, 40);
            c.Rect(2, 2, 12, 13, cover);
            c.Rect(2, 2, 2, 13, dark);           // 背表紙
            c.Rect(13, 3, 1, 11, page);          // ページ
            c.Ellipse(9, 8, 3, 3, bug);          // 表紙の虫
            c.Set(8, 7, eye); c.Set(10, 7, eye);
            c.Set(6, 5, bug); c.Set(12, 5, bug); // 触角
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>セーブのボタンの絵（羽ペンと記録の紙）</summary>
        public static Sprite SaveIcon => Cached("saveicon", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 paper = C(245, 235, 200), line = C(170, 150, 110), feather = C(250, 250, 255), shade = C(190, 200, 220), nib = C(60, 60, 70);
            c.Rect(2, 4, 10, 11, paper);
            for (int y = 7; y < 14; y += 2) c.Rect(4, y, 6, 1, line);
            for (int i = 0; i < 9; i++) { c.Set(6 + i, 11 - i, feather); c.Set(7 + i, 11 - i, shade); c.Set(6 + i, 10 - i, feather); }
            c.Set(5, 12, nib); c.Set(4, 13, nib);
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>地図で立てたピン（赤い頭の画びょう）</summary>
        public static Sprite Pin => Cached("pin", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 red = C(230, 60, 60), hi = C(255, 150, 150), needle = C(200, 200, 210);
            c.Ellipse(8, 5, 4.5f, 4.5f, red);
            c.Set(6, 3, hi); c.Set(7, 3, hi); c.Set(6, 4, hi);
            c.Rect(7, 9, 2, 6, needle);
            PixelArt.Outline(c, 0.3f);
            return c;
        });

        /// <summary>地図のボタンの絵（まいた地図）</summary>
        public static Sprite MapIcon => Cached("mapicon", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 paper = C(240, 225, 180), line = C(170, 140, 90), road = C(200, 120, 70), x = C(220, 50, 50), sea = C(120, 180, 230);
            c.Rect(1, 3, 14, 10, paper);
            c.Rect(5, 3, 1, 10, line); c.Rect(10, 3, 1, 10, line);
            c.Rect(1, 9, 4, 4, sea);
            for (int i = 0; i < 8; i++) c.Set(3 + i, 5 + (i % 3), road);
            c.Set(12, 5, x); c.Set(13, 6, x); c.Set(13, 5, x); c.Set(12, 6, x);
            PixelArt.Outline(c, 0.35f);
            return c;
        });

        /// <summary>資材のアイコン（木材・石材・歯車・アルカナのかけら）</summary>
        public static Sprite MaterialIcon(Material m) => Cached("mat" + (int)m, () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            switch (m)
            {
                case Material.Wood: // 丸太を3本つんだもの（切り口に年輪）
                    Color32 bark = C(130, 82, 40), barkHi = C(165, 110, 60), core = C(225, 185, 125), ring = C(185, 140, 85);
                    foreach (var (x, y) in new[] { (1, 8), (8, 8), (4, 2) })
                    {
                        c.Ellipse(x + 3.5f, y + 3.5f, 3.6f, 3.6f, bark);
                        c.Rect(x + 1, y, 5, 1, barkHi);
                        c.Ellipse(x + 3.5f, y + 3.5f, 2.4f, 2.4f, core);
                        c.Set(x + 3, y + 3, ring); c.Set(x + 4, y + 3, ring);
                    }
                    break;
                case Material.Stone: // 石のブロック（レンガもよう）
                    Color32 st = C(165, 165, 175), sd = C(110, 110, 122), sh = C(205, 205, 215);
                    c.Rect(1, 3, 14, 11, st);
                    c.Rect(1, 3, 14, 1, sh);
                    c.Rect(1, 8, 14, 1, sd);
                    c.Rect(8, 3, 1, 5, sd); c.Rect(4, 9, 1, 5, sd); c.Rect(11, 9, 1, 5, sd);
                    c.Rect(1, 13, 14, 1, sd);
                    break;
                case Material.Gear: // 歯車（8本の歯と、まん中の穴）
                    Color32 g = C(205, 175, 85), gd = C(150, 120, 50), gh = C(245, 220, 140);
                    c.Ellipse(8, 8, 5.2f, 5.2f, g);
                    foreach (var (x, y) in new[] { (7, 0), (7, 13), (0, 7), (13, 7), (2, 2), (12, 2), (2, 12), (12, 12) }) c.Rect(x, y, 2, 2, g);
                    c.Ellipse(8, 8, 2.2f, 2.2f, PixelArt.Clear);
                    c.Ellipse(8, 8, 3.4f, 3.4f, gd);
                    c.Ellipse(8, 8, 2.2f, 2.2f, PixelArt.Clear);
                    c.Set(5, 4, gh); c.Set(6, 4, gh); c.Set(4, 5, gh);
                    break;
                default: // アルカナのかけら（むらさきの とがった結晶）
                    Color32 p = C(165, 75, 200), pd = C(110, 45, 140), l = C(235, 170, 255);
                    for (int y = 1; y < 15; y++)
                    {
                        int half = y < 6 ? y / 2 + 1 : (15 - y) / 2 + 1;
                        c.Rect(8 - half, y, half * 2, 1, p);
                        c.Set(8 + half - 1, y, pd);
                    }
                    c.Rect(7, 4, 1, 6, l);
                    break;
            }
            PixelArt.Outline(c);
            return c;
        });
    }
}
