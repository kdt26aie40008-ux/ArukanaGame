using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>アルカナの形</summary>
    public enum MonsterShape { Slime, Bug, Ghost, Serpent, Mimic, King, Fish, Bat, Mushroom, Robot, Eyeball, Rock, Jelly, Bird, Scorpion, Crystal }

    /// <summary>
    /// アルカナのドット絵（32x32）。形ごとに描き、色は敵の色から明るい色・暗い色を作ってかげをつける。
    /// 本番の画像ができたら、ここで返す Sprite を差し替えてください。
    /// </summary>
    public static class MonsterArt
    {
        private const int S = 32;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static readonly Color32 Black = new Color32(25, 22, 30, 255);
        private static readonly Color32 White = new Color32(250, 250, 255, 255);
        private static readonly Color32 Gold = new Color32(245, 200, 60, 255);
        private static readonly Color32 GoldDark = new Color32(185, 130, 30, 255);
        private static readonly Color32 Red = new Color32(230, 60, 70, 255);
        private static readonly Color32 Pink = new Color32(240, 120, 150, 255);

        public static Sprite Build(EnemyInfo e)
        {
            // 画像ファイル（Resources/Art/Arcana/名前.png）があれば、その絵を使う
            var art = ArtOverrides.Arcana(e.ArtKey);
            if (art != null) return art;
            var baseColor = new Color32(e.Color.r, e.Color.g, e.Color.b, 255);
            string key = $"{e.Shape}_{e.Color.r}_{e.Color.g}_{e.Color.b}";
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var c = new PixelArt.Canvas(S, S);
            Color32 dark = PixelArt.Mul(baseColor, 0.62f), light = PixelArt.Add(baseColor, 55);
            switch (e.Shape)
            {
                case MonsterShape.Bug: Bug(c, baseColor, dark, light); break;
                case MonsterShape.Ghost: Ghost(c, baseColor, dark, light); break;
                case MonsterShape.Serpent: Serpent(c, baseColor, dark, light); break;
                case MonsterShape.Mimic: Mimic(c, baseColor, dark, light); break;
                case MonsterShape.King: Slime(c, baseColor, dark, light, true); break;
                case MonsterShape.Fish: FishArt(c, baseColor, dark, light); break;
                case MonsterShape.Bat: Bat(c, baseColor, dark, light); break;
                case MonsterShape.Mushroom: Mushroom(c, baseColor, dark, light); break;
                case MonsterShape.Robot: Robot(c, baseColor, dark, light); break;
                case MonsterShape.Eyeball: Eyeball(c, baseColor, dark, light); break;
                case MonsterShape.Rock: Rock(c, baseColor, dark, light); break;
                case MonsterShape.Jelly: Jelly(c, baseColor, dark, light); break;
                case MonsterShape.Bird: Bird(c, baseColor, dark, light); break;
                case MonsterShape.Scorpion: Scorpion(c, baseColor, dark, light); break;
                case MonsterShape.Crystal: Crystal(c, baseColor, dark, light); break;
                default: Slime(c, baseColor, dark, light, false); break;
            }
            PixelArt.Outline(c, 0.3f);
            s = PixelArt.ToSprite(c, "monster_" + key);
            cache[key] = s;
            return s;
        }

        private static void Eye(PixelArt.Canvas c, int x, int y, Color32 pupil, bool angry)
        {
            c.Ellipse(x + 0.5f, y + 0.5f, 2.6f, 3.1f, White);
            c.Rect(x, y, 2, 3, pupil);
            c.Set(x, y, White);
            if (angry) c.Rect(x - 2, y - 4, 5, 1, Black);
        }

        /// <summary>スライム（キングは大きな王冠つき）</summary>
        private static void Slime(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l, bool king)
        {
            float top = king ? 13 : 10;
            c.Ellipse(16, 22, 14, 10, d);
            c.Ellipse(16, 21, 13.5f, 9.5f, b);
            c.Ellipse(16, top + 2, 4, 4, b);          // てっぺんのしずく
            c.Ellipse(16, 26, 12, 4, d);              // 下のかげ
            c.Rect(0, 30, S, 2, PixelArt.Clear);
            c.Ellipse(10, 16, 3.5f, 2.5f, l);         // 光
            c.Set(9, 15, White); c.Set(10, 15, White);
            Eye(c, 10, 19, Black, king);
            Eye(c, 20, 19, Black, king);
            c.Rect(14, 25, 4, 1, Black);              // 口
            c.Set(13, 24, Black); c.Set(18, 24, Black);
            if (king)
            {
                // 王冠と宝石
                c.Rect(8, 6, 16, 5, Gold);
                c.Rect(8, 10, 16, 1, GoldDark);
                foreach (int x in new[] { 8, 15, 22 }) { c.Rect(x, 2, 2, 4, Gold); c.Set(x, 1, Gold); }
                c.Rect(11, 7, 2, 2, Red); c.Rect(19, 7, 2, 2, Red);
                c.Rect(15, 7, 2, 2, new Color32(90, 200, 255, 255));
            }
        }

        /// <summary>虫（こうらに線・目は赤・触角と6本の足）</summary>
        private static void Bug(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 足
            for (int i = 0; i < 3; i++)
            {
                int y = 17 + i * 4;
                c.Rect(3, y, 5, 1, d); c.Set(2, y + 1, d);
                c.Rect(24, y, 5, 1, d); c.Set(29, y + 1, d);
            }
            c.Ellipse(16, 20, 11, 9, d);
            c.Ellipse(16, 19.5f, 10, 8, b);
            c.Rect(15, 12, 2, 16, d);                 // こうらの線
            c.Ellipse(11, 16, 3, 2, l); c.Ellipse(21, 16, 3, 2, l);
            for (int k = 0; k < 3; k++) { c.Set(10 + k * 2, 22, d); c.Set(19 + k * 2, 22, d); }
            c.Ellipse(16, 9, 6, 5, d);                // 頭
            c.Ellipse(16, 8.5f, 5.5f, 4.5f, PixelArt.Mul(b, 0.8f));
            c.Rect(12, 8, 3, 2, Red); c.Rect(18, 8, 3, 2, Red);
            c.Set(12, 8, White); c.Set(18, 8, White);
            // 触角
            for (int k = 0; k < 5; k++) { c.Set(13 - k, 4 - k / 2, d); c.Set(19 + k, 4 - k / 2, d); }
            c.Rect(7, 1, 2, 2, l); c.Rect(24, 1, 2, 2, l);
        }

        /// <summary>おばけ（下がなみなみ・口をあける）</summary>
        private static void Ghost(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            c.Ellipse(16, 13, 11, 10, b);
            c.Rect(5, 13, 22, 13, b);
            c.Rect(22, 13, 5, 13, PixelArt.Mul(b, 0.85f));
            for (int x = 5; x < 27; x++)
            {
                int wave = (x / 4) % 2 == 0 ? 0 : 2;
                for (int y = 26 - wave; y < 29; y++) c.Set(x, y, (x / 4) % 2 == 0 ? b : PixelArt.Clear);
            }
            c.Ellipse(10, 8, 3, 2, l);
            c.Rect(3, 16, 3, 3, b); c.Rect(26, 15, 3, 3, b); // うで
            c.Ellipse(12, 13, 2, 3, Black); c.Ellipse(20, 13, 2, 3, Black);
            c.Set(11, 12, White); c.Set(19, 12, White);
            c.Ellipse(16, 20, 3, 2.5f, d);
            c.Ellipse(16, 21, 1.5f, 1, Pink);
        }

        /// <summary>くるっと丸まったヘビ（ループ）</summary>
        private static void Serpent(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            c.Ellipse(15, 20, 13, 10, d);
            c.Ellipse(15, 19.5f, 12, 9, b);
            c.Ellipse(15, 20, 6, 4, PixelArt.Clear);
            c.Ellipse(15, 20, 7, 5, d);
            c.Ellipse(15, 20, 5.5f, 3.5f, PixelArt.Clear);
            for (int x = 4; x < 27; x += 3) c.Set(x, 27, l);    // おなかのもよう
            for (int x = 6; x < 25; x += 4) c.Set(x, 12, PixelArt.Mul(b, 0.75f));
            // しっぽ
            c.Rect(2, 18, 3, 2, b); c.Set(1, 17, b);
            // 頭
            c.Ellipse(24, 9, 6.5f, 5, d);
            c.Ellipse(24, 8.5f, 6, 4.5f, b);
            c.Ellipse(22, 7, 2, 1.2f, l);
            c.Rect(24, 7, 2, 2, Gold); c.Set(25, 7, Black);
            c.Rect(29, 11, 2, 1, Red); c.Set(31, 10, Red); c.Set(31, 12, Red); // した
        }

        /// <summary>魚（水しぶきつき）</summary>
        private static void FishArt(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            c.Ellipse(14, 17, 11, 6.5f, d);
            c.Ellipse(14, 16.5f, 10, 5.5f, b);
            c.Ellipse(12, 14, 5, 2, l);                  // おなかの光
            for (int y = 10; y < 24; y++)                // しっぽ
            {
                int w = Mathf.Abs(y - 17) / 2 + 1;
                c.Rect(24, y, w + 2, 1, y < 14 || y > 20 ? b : d);
            }
            c.Ellipse(8, 15, 2, 2, White); c.Set(8, 15, Black); c.Set(7, 15, Black);
            c.Rect(3, 18, 3, 1, d);                       // 口
            c.Rect(13, 9, 6, 2, d);                       // せびれ
            var water = new Color32(140, 200, 250, 255);
            foreach (var (x, y) in new[] { (4, 27), (9, 29), (20, 28), (27, 26), (15, 30) }) { c.Set(x, y, water); c.Set(x + 1, y - 1, water); }
        }

        /// <summary>宝箱にばけたモドキ（ふたの中に目・ぎざぎざの歯・した）</summary>
        private static void Mimic(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 箱
            c.Rect(4, 16, 24, 13, b);
            c.Rect(4, 16, 24, 2, l);
            c.Rect(4, 27, 24, 2, d);
            c.Rect(4, 16, 2, 13, Gold); c.Rect(26, 16, 2, 13, Gold);
            c.Rect(14, 18, 4, 4, Gold); c.Rect(15, 19, 2, 2, GoldDark);
            // ふた（あいている）
            c.Rect(4, 4, 24, 7, d);
            c.Rect(4, 4, 24, 2, b);
            c.Rect(4, 4, 2, 7, Gold); c.Rect(26, 4, 2, 7, Gold);
            // 口の中
            c.Rect(6, 11, 20, 5, new Color32(60, 20, 35, 255));
            for (int x = 6; x < 26; x += 3) { c.Set(x, 11, White); c.Set(x + 1, 11, White); c.Set(x, 12, White); }
            for (int x = 7; x < 26; x += 3) { c.Set(x, 15, White); c.Set(x + 1, 15, White); c.Set(x + 1, 14, White); }
            c.Ellipse(16, 14, 4, 1.5f, Pink);
            // 目（ふたのすきまから光る）
            c.Rect(9, 7, 3, 2, Gold); c.Rect(20, 7, 3, 2, Gold);
            c.Set(10, 7, Black); c.Set(21, 7, Black);
        }

        /// <summary>コウモリ（大きなつばさ・きば）</summary>
        private static void Bat(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // つばさ（ぎざぎざのまく）
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 12; i++)
                {
                    int x = 16 + side * (5 + i);
                    int top = 9 + i / 3, bottom = 22 - (i % 4 == 3 ? 4 : i / 2);
                    for (int y = top; y <= bottom; y++) c.Set(x, y, y == top ? l : (i % 4 == 3 ? d : b));
                }
                for (int i = 0; i < 12; i++) c.Set(16 + side * (5 + i), 9 + i / 3, d); // ほね
            }
            c.Ellipse(16, 17, 6, 7, d);
            c.Ellipse(16, 16.5f, 5.2f, 6.2f, b);
            c.Ellipse(14, 13, 2, 1.5f, l);
            c.Rect(11, 6, 2, 5, b); c.Rect(19, 6, 2, 5, b); c.Set(11, 5, d); c.Set(20, 5, d); // 耳
            c.Rect(13, 14, 2, 2, Red); c.Rect(17, 14, 2, 2, Red);
            c.Set(13, 14, White); c.Set(17, 14, White);
            c.Rect(14, 19, 4, 1, Black);
            c.Set(14, 20, White); c.Set(17, 20, White); // きば
            c.Rect(13, 24, 2, 2, d); c.Rect(17, 24, 2, 2, d); // 足
        }

        /// <summary>キノコ（水玉のかさ・ねむそうな目）</summary>
        private static void Mushroom(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            var stem = new Color32(240, 225, 195, 255);
            var stemDark = new Color32(200, 180, 150, 255);
            c.Ellipse(16, 23, 7, 6, stemDark);
            c.Ellipse(16, 22.5f, 6.2f, 5.5f, stem);
            c.Rect(10, 28, 4, 2, stemDark); c.Rect(18, 28, 4, 2, stemDark); // 足
            c.Ellipse(16, 13, 14, 8, d);
            c.Ellipse(16, 12, 13.5f, 7.5f, b);
            c.Rect(2, 15, 28, 6, PixelArt.Clear);
            c.Rect(3, 15, 26, 2, d);                  // かさのふち
            foreach (var (x, y, r) in new[] { (9, 9, 2.2f), (19, 7, 2.6f), (25, 12, 1.8f), (14, 13, 1.6f), (6, 13, 1.4f) })
                c.Ellipse(x, y, r, r, White);
            c.Ellipse(11, 7, 2, 1, l);
            c.Rect(12, 21, 3, 1, Black); c.Rect(18, 21, 3, 1, Black); // ねむそうな目
            c.Set(12, 22, Black); c.Set(20, 22, Black);
            c.Rect(15, 25, 2, 1, Pink);
        }

        /// <summary>ロボット（四角い頭・アンテナ・画面の目）</summary>
        private static void Robot(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            var screen = new Color32(40, 50, 60, 255);
            var glow = new Color32(120, 255, 170, 255);
            // からだ
            c.Rect(8, 18, 16, 10, d);
            c.Rect(9, 18, 14, 9, b);
            c.Rect(13, 21, 6, 4, screen);
            c.Set(14, 22, Red); c.Set(16, 22, Gold); c.Set(18, 22, glow);
            // うで・足
            c.Rect(3, 19, 4, 2, d); c.Rect(2, 21, 2, 4, d);
            c.Rect(25, 19, 4, 2, d); c.Rect(28, 21, 2, 4, d);
            c.Rect(10, 28, 4, 3, d); c.Rect(18, 28, 4, 3, d);
            // 頭
            c.Rect(7, 5, 18, 12, d);
            c.Rect(8, 5, 16, 11, b);
            c.Rect(8, 5, 16, 2, l);
            c.Rect(10, 8, 12, 6, screen);
            c.Rect(11, 10, 3, 2, glow); c.Rect(18, 10, 3, 2, glow); // 画面の目
            c.Rect(14, 13, 4, 1, glow);
            c.Rect(5, 9, 2, 4, Gold); c.Rect(25, 9, 2, 4, Gold);    // 耳のボルト
            c.Rect(15, 1, 2, 4, d); c.Ellipse(16, 1, 1.5f, 1.5f, Red); // アンテナ
        }

        /// <summary>大きな目玉（血管・しっぽのような足）</summary>
        private static void Eyeball(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // しょく手
            for (int i = 0; i < 4; i++)
            {
                int x0 = 8 + i * 5;
                for (int y = 22; y < 31; y++) c.Set(x0 + ((y / 3) % 2 == 0 ? 0 : 1), y, y > 28 ? l : d);
            }
            c.Ellipse(16, 15, 12, 11, d);
            c.Ellipse(16, 14.5f, 11, 10, new Color32(245, 240, 235, 255));
            // 血管
            foreach (var (x, y) in new[] { (7, 10), (8, 11), (9, 11), (24, 18), (23, 19), (22, 19), (8, 19), (9, 20) }) c.Set(x, y, Red);
            c.Ellipse(17, 15, 6, 6, d);
            c.Ellipse(17, 15, 5, 5, b);               // 黒目のまわり（敵の色）
            c.Ellipse(17, 15, 2.6f, 2.6f, Black);
            c.Ellipse(15, 13, 1.4f, 1.4f, White);
            c.Rect(5, 4, 22, 2, d);                   // まぶた
        }

        /// <summary>岩のゴーレム（つみ上げた石・光る目）</summary>
        private static void Rock(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 石を つみ上げる（下ほど大きい）
            c.Ellipse(16, 25, 12, 5, d); c.Ellipse(16, 24.5f, 11, 4.2f, b);
            c.Ellipse(16, 17, 10, 5, d); c.Ellipse(16, 16.5f, 9, 4.2f, b);
            c.Ellipse(16, 9, 7, 5, d); c.Ellipse(16, 8.5f, 6.2f, 4.2f, b);
            foreach (var (x, y) in new[] { (9, 23), (21, 26), (11, 15), (22, 17), (13, 7) }) { c.Set(x, y, l); c.Set(x + 1, y, l); }
            foreach (var (x, y) in new[] { (14, 26), (24, 23), (18, 15), (19, 9) }) c.Set(x, y, d); // ひび
            // うで（石）
            c.Ellipse(4, 17, 3, 3.5f, d); c.Ellipse(4, 16.5f, 2.4f, 2.8f, b);
            c.Ellipse(28, 17, 3, 3.5f, d); c.Ellipse(28, 16.5f, 2.4f, 2.8f, b);
            // 光る目
            var glow = new Color32(255, 170, 60, 255);
            c.Rect(12, 8, 3, 2, glow); c.Rect(18, 8, 3, 2, glow);
            c.Set(13, 8, Gold); c.Set(19, 8, Gold);
            c.Rect(13, 11, 6, 1, Black);
        }

        /// <summary>クラゲ（すきとおったかさ・ゆらゆらの足）</summary>
        private static void Jelly(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 足（なみなみ）
            for (int i = 0; i < 5; i++)
            {
                int x0 = 8 + i * 4;
                for (int y = 17; y < 31; y++) c.Set(x0 + ((y / 3 + i) % 2), y, y % 4 == 0 ? l : d);
            }
            c.Ellipse(16, 12, 12, 9, d);
            c.Ellipse(16, 11.5f, 11, 8.2f, b);
            c.Rect(4, 15, 24, 4, b);                  // かさのすそ
            for (int x = 4; x < 28; x += 3) c.Set(x, 18, l);
            c.Ellipse(11, 7, 4, 2.4f, l);             // 光
            c.Ellipse(12, 6, 1.6f, 1, White);
            c.Ellipse(16, 12, 5, 3.5f, PixelArt.Mix(b, White, 0.35f)); // すけて見える中身
            c.Rect(11, 11, 2, 3, Black); c.Rect(19, 11, 2, 3, Black);   // 目
            c.Set(11, 11, White); c.Set(19, 11, White);
            c.Rect(15, 15, 2, 1, Pink);
        }

        /// <summary>鳥（つばさを広げた風見どり）</summary>
        private static void Bird(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // つばさ
            for (int i = 0; i < 9; i++)
            {
                c.Rect(2 + i, 12 + i / 2, 1, 9 - i / 2, i % 3 == 0 ? d : b);
                c.Rect(29 - i, 12 + i / 2, 1, 9 - i / 2, i % 3 == 0 ? d : b);
            }
            c.Ellipse(16, 18, 7, 8, d);
            c.Ellipse(16, 17.5f, 6.2f, 7.2f, b);
            c.Ellipse(16, 21, 4, 4, l);               // おなか
            c.Ellipse(16, 9, 5, 4.6f, d);             // 頭
            c.Ellipse(16, 8.6f, 4.4f, 4, b);
            c.Rect(14, 2, 1, 4, d); c.Rect(17, 1, 1, 5, d); c.Rect(15, 3, 2, 1, l); // とさか
            c.Rect(13, 8, 2, 2, Black); c.Rect(18, 8, 2, 2, Black);
            c.Set(13, 8, White); c.Set(18, 8, White);
            c.Rect(15, 10, 3, 2, Gold); c.Set(16, 12, GoldDark); // くちばし
            c.Rect(12, 25, 3, 4, Gold); c.Rect(18, 25, 3, 4, Gold); // 足
            c.Rect(11, 29, 5, 1, GoldDark); c.Rect(17, 29, 5, 1, GoldDark);
        }

        /// <summary>サソリ（大きなはさみ・まがったしっぽの針）</summary>
        private static void Scorpion(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 足
            for (int i = 0; i < 3; i++)
            {
                int x = 10 + i * 5;
                c.Rect(x, 26, 1, 4, d); c.Set(x - 1, 29, d);
                c.Rect(x + 2, 26, 1, 4, d); c.Set(x + 3, 29, d);
            }
            // しっぽ（上にまがる）
            foreach (var (x, y, r) in new[] { (24, 22, 2.8f), (26, 18, 2.6f), (27, 14, 2.4f), (26, 10, 2.2f), (23, 7, 2f) })
            {
                c.Ellipse(x, y, r + 0.7f, r + 0.7f, d);
                c.Ellipse(x, y, r, r, b);
            }
            c.Rect(19, 5, 3, 2, Red); c.Set(18, 6, Red); c.Set(18, 7, White); // 針
            // からだ
            c.Ellipse(15, 23, 9, 5, d);
            c.Ellipse(15, 22.5f, 8.2f, 4.2f, b);
            for (int x = 9; x < 22; x += 3) c.Rect(x, 21, 1, 4, d); // ふし
            c.Ellipse(12, 20, 3, 1.2f, l);
            // はさみ
            c.Ellipse(5, 15, 4, 3.5f, d); c.Ellipse(5, 14.6f, 3.3f, 2.8f, b);
            c.Rect(2, 11, 2, 3, b); c.Rect(6, 11, 2, 3, b);
            c.Rect(7, 17, 4, 2, d);
            // 目
            c.Rect(13, 18, 2, 2, Black); c.Rect(17, 18, 2, 2, Black);
            c.Set(13, 18, White); c.Set(17, 18, White);
        }

        /// <summary>星の結晶（とがった結晶と、まわりの小さな星）</summary>
        private static void Crystal(PixelArt.Canvas c, Color32 b, Color32 d, Color32 l)
        {
            // 大きな結晶（ひし形を重ねる）
            for (int y = 3; y <= 29; y++)
            {
                int half = y <= 16 ? (y - 3) * 9 / 13 : (29 - y) * 9 / 13;
                c.Rect(16 - half, y, half * 2 + 1, 1, y <= 16 ? b : d);
                c.Set(16 - half, y, d); c.Set(16 + half, y, d);
            }
            for (int y = 6; y <= 26; y++) c.Set(16, y, l);       // まん中の光るすじ
            c.Ellipse(12, 11, 2, 3, PixelArt.Mix(l, White, 0.5f));
            // 小さな結晶
            for (int y = 14; y <= 24; y++)
            {
                int half = y <= 19 ? (y - 14) / 2 : (24 - y) / 2;
                c.Rect(4 - half, y, half * 2 + 1, 1, b); c.Rect(28 - half, y, half * 2 + 1, 1, b);
            }
            // 顔
            c.Rect(13, 15, 2, 3, Black); c.Rect(18, 15, 2, 3, Black);
            c.Set(13, 15, White); c.Set(18, 15, White);
            c.Rect(15, 20, 3, 1, Black);
            // まわりの星
            foreach (var (x, y) in new[] { (5, 5), (27, 6), (3, 27), (28, 27) })
            {
                c.Set(x, y, Gold); c.Set(x - 1, y, Gold); c.Set(x + 1, y, Gold); c.Set(x, y - 1, Gold); c.Set(x, y + 1, Gold);
            }
        }
    }
}
