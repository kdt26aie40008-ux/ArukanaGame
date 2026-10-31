using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 宿舎の中のドット絵（16x16）：木の床・かべ・窓・家具。Resources/Art/Inn/ に画像があれば その絵を使い、なければ プログラムで描きます。
    /// 家具は背景が透明なので、床の上に重ねて置きます。
    /// </summary>
    public static class InnArt
    {
        private const int Px = 16;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        private static readonly Color32 Wood = C(150, 100, 58), WoodDark = C(105, 68, 38), WoodLight = C(190, 135, 82);

        private static Sprite Cached(string key, System.Func<PixelArt.Canvas> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = PixelArt.ToSprite(make(), key);
            cache[key] = s;
            return s;
        }

        /// <summary>そのマスの家具・床の絵（家具でなければ null）</summary>
        public static Sprite Furniture(char c)
        {
            switch (c)
            {
                case 'f': return Fireplace;
                case 'b': return Bed;
                case 'm': return Mirror;
                case 'd': return Wardrobe;
                case 'p': return Plant;
                case 'k': return Desk;
                case 't': return Table;
                case 'c': return Counter;
                case 'E': return Door;
                default: return null;
            }
        }

        /// <summary>木の床（よこ長の板）</summary>
        public static Sprite Floor => ArtOverrides.InnTile("床") ?? Cached("inn_floor", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 b = C(176, 124, 78), d = C(140, 96, 58), l = C(198, 146, 96);
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                {
                    int row = y / 4, ly = y % 4;
                    int seam = (row % 2 == 0 ? 6 : 13);
                    c.Px[x, y] = ly == 3 || x == seam ? d : ly == 0 ? l : b;
                }
            return c;
        });

        /// <summary>かべ（上はしっくい、下は木の板。窓つきもある）</summary>
        public static Sprite Wall => ArtOverrides.InnTile("かべ") ?? Cached("inn_wall", () => WallArt(false));
        public static Sprite WallWindow => ArtOverrides.InnTile("かべ_窓") ?? Cached("inn_window", () => WallArt(true));

        private static PixelArt.Canvas WallArt(bool window)
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 plaster = C(232, 218, 186), plasterDark = C(205, 190, 160);
            c.Rect(0, 0, Px, 9, plaster);
            c.Rect(0, 8, Px, 1, plasterDark);
            c.Rect(0, 9, Px, 7, Wood);
            for (int x = 3; x < Px; x += 5) c.Rect(x, 9, 1, 7, WoodDark);
            c.Rect(0, 9, Px, 1, WoodLight);
            if (window)
            {
                c.Rect(3, 1, 10, 7, WoodDark);
                c.Rect(4, 2, 8, 5, C(150, 205, 245));
                c.Rect(7, 2, 2, 5, WoodDark);
                c.Set(5, 3, C(230, 245, 255)); c.Set(6, 3, C(230, 245, 255));
                c.Rect(2, 1, 2, 7, C(200, 60, 70)); c.Rect(12, 1, 2, 7, C(200, 60, 70)); // カーテン
            }
            return c;
        }

        /// <summary>じゅうたん（ふちどりともよう）</summary>
        public static Sprite Rug => ArtOverrides.Inn("じゅうたん") ?? Cached("inn_rug", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 red = C(170, 50, 60), gold = C(225, 180, 80), dark = C(130, 35, 45);
            c.Rect(0, 0, Px, Px, red);
            for (int i = 0; i < Px; i += 4) { c.Set(i, i % 8, gold); c.Set(i + 2, (i + 4) % 8 + 8, gold); }
            c.Rect(0, 0, Px, 1, dark); c.Rect(0, 15, Px, 1, dark);
            return c;
        });

        public static Sprite Bed => ArtOverrides.Inn("ベッド") ?? Cached("inn_bed", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 blanket = C(70, 120, 200), blanketDark = C(50, 90, 160), pillow = C(250, 250, 245);
            c.Rect(1, 0, 14, 16, WoodDark);
            c.Rect(2, 1, 12, 14, Wood);
            c.Rect(3, 2, 10, 4, pillow);
            c.Rect(3, 5, 10, 1, C(215, 215, 220));
            c.Rect(2, 6, 12, 9, blanket);
            c.Rect(2, 6, 12, 1, C(120, 165, 235));
            for (int y = 8; y < 15; y += 3) c.Rect(2, y, 12, 1, blanketDark);
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        public static Sprite Fireplace => ArtOverrides.Inn("だんろ") ?? Cached("inn_fire", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 stone = C(150, 145, 140), stoneDark = C(105, 100, 98), fire = C(250, 130, 40), fireHi = C(255, 220, 90);
            c.Rect(0, 0, Px, Px, stone);
            for (int y = 0; y < Px; y += 4) c.Rect(0, y, Px, 1, stoneDark);
            c.Rect(3, 5, 10, 11, C(40, 30, 30));
            c.Ellipse(8, 12, 4, 4, fire);
            c.Ellipse(8, 12.5f, 2, 2.5f, fireHi);
            c.Rect(4, 14, 8, 2, WoodDark);
            return c;
        });

        public static Sprite Mirror => ArtOverrides.Inn("鏡") ?? Cached("inn_mirror", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 gold = C(230, 185, 70), glass = C(170, 215, 240), shine = C(240, 250, 255);
            c.Ellipse(8, 7, 6, 7, gold);
            c.Ellipse(8, 7, 4.6f, 5.6f, glass);
            c.Rect(6, 3, 1, 5, shine); c.Rect(7, 2, 1, 2, shine);
            c.Rect(6, 14, 4, 2, WoodDark);
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        public static Sprite Wardrobe => ArtOverrides.Inn("たんす") ?? Cached("inn_wardrobe", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Rect(1, 0, 14, 16, Wood);
            c.Rect(1, 0, 14, 2, WoodLight);
            c.Rect(7, 2, 2, 14, WoodDark);
            c.Rect(3, 4, 3, 9, WoodDark); c.Rect(10, 4, 3, 9, WoodDark);
            c.Set(6, 9, C(240, 200, 80)); c.Set(9, 9, C(240, 200, 80));
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        public static Sprite Plant => ArtOverrides.Inn("観葉植物") ?? Cached("inn_plant", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            Color32 leaf = C(70, 160, 80), leafHi = C(120, 200, 110), pot = C(190, 100, 60);
            c.Ellipse(8, 6, 6, 5, leaf);
            c.Ellipse(6, 4, 2, 2, leafHi); c.Ellipse(11, 6, 1.5f, 1.5f, leafHi);
            c.Rect(4, 10, 8, 6, pot);
            c.Rect(4, 10, 8, 1, C(220, 130, 80));
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        /// <summary>記録の本をのせた机（ろうそくつき）</summary>
        public static Sprite Desk => ArtOverrides.Inn("机") ?? Cached("inn_desk", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Rect(0, 4, Px, 9, Wood);
            c.Rect(0, 4, Px, 1, WoodLight);
            c.Rect(1, 13, 2, 3, WoodDark); c.Rect(13, 13, 2, 3, WoodDark);
            c.Rect(3, 6, 9, 5, C(250, 245, 230));               // ひらいた本
            c.Rect(7, 6, 1, 5, C(170, 140, 110));
            for (int y = 7; y < 10; y++) { c.Rect(4, y, 2, 1, C(150, 150, 160)); c.Rect(9, y, 2, 1, C(150, 150, 160)); }
            c.Rect(13, 2, 2, 4, C(245, 240, 220));             // ろうそく
            c.Set(13, 1, C(255, 200, 60)); c.Set(14, 0, C(255, 240, 150));
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        public static Sprite Table => ArtOverrides.Inn("テーブル") ?? Cached("inn_table", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Ellipse(8, 7, 7, 5, Wood);
            c.Ellipse(8, 6.5f, 6, 4, WoodLight);
            c.Rect(7, 11, 2, 5, WoodDark);
            c.Rect(5, 4, 3, 3, C(250, 250, 250)); c.Set(6, 5, C(140, 90, 50)); // カップ
            PixelArt.Outline(c, 0.4f);
            return c;
        });

        public static Sprite Counter => ArtOverrides.Inn("カウンター") ?? Cached("inn_counter", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Rect(0, 3, Px, 13, Wood);
            c.Rect(0, 3, Px, 3, WoodLight);
            c.Rect(0, 6, Px, 1, WoodDark);
            for (int x = 2; x < Px; x += 5) c.Rect(x, 8, 1, 8, WoodDark);
            return c;
        });

        /// <summary>出口のドア</summary>
        public static Sprite Door => ArtOverrides.Inn("ドア") ?? Cached("inn_door", () =>
        {
            var c = new PixelArt.Canvas(Px, Px);
            c.Rect(0, 0, Px, Px, WoodDark);
            c.Rect(2, 1, 12, 15, Wood);
            for (int x = 5; x < 14; x += 3) c.Rect(x, 1, 1, 15, WoodDark);
            c.Rect(11, 8, 2, 2, C(240, 200, 80));
            c.Rect(2, 1, 12, 1, WoodLight);
            return c;
        });
    }
}
