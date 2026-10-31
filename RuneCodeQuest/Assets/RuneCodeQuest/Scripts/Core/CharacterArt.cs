using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>プレイヤーが作ったキャラクターの見た目</summary>
    public class CharacterLook
    {
        public string Name = "アル";
        public int HairStyle;  // かみがた
        public int Hair;       // かみの色
        public int Cloth;      // ふくの色
        public int Skin;       // はだの色
        public int Hat;        // ぼうし
        public int Eyes;       // 目の形
        public int EyeColor;   // 目の色
        public int Outfit;     // ふくの形
        public int Accessory;  // アクセサリー

        // パーツきせかえ（CharacterParts）でえらんだ部品（0 からの番号。帽子だけは 0 が「なし」）
        public int Gender;     // 0：男の子 1：女の子
        public int PHair, PEyes, PMouth, PNose, PTop, PBottom, PShoes, PHat;
        public int POutfit = 1; // 全身の服（コーデ）。0 なら なし（ふく・ズボン・くつを 1つずつ えらぶ）。新しいキャラは コーデ1から

        public string Encode() =>
            $"{Name.Replace("|", "")}|{HairStyle}|{Hair}|{Cloth}|{Skin}|{Hat}|{Eyes}|{EyeColor}|{Outfit}|{Accessory}" +
            $"|{Gender}|{PHair}|{PEyes}|{PMouth}|{PNose}|{PTop}|{PBottom}|{PShoes}|{PHat}|{POutfit}";

        public static CharacterLook Decode(string s)
        {
            var look = new CharacterLook();
            if (string.IsNullOrEmpty(s)) return look;
            var p = s.Split('|');
            if (p.Length < 6) return look;
            look.Name = p[0].Length > 0 ? p[0] : look.Name;
            int.TryParse(p[1], out look.HairStyle);
            int.TryParse(p[2], out look.Hair);
            int.TryParse(p[3], out look.Cloth);
            int.TryParse(p[4], out look.Skin);
            int.TryParse(p[5], out look.Hat);
            // 前のセーブデータ（6こ）にはないので、なければ 0 のまま
            if (p.Length >= 10)
            {
                int.TryParse(p[6], out look.Eyes);
                int.TryParse(p[7], out look.EyeColor);
                int.TryParse(p[8], out look.Outfit);
                int.TryParse(p[9], out look.Accessory);
            }
            // パーツきせかえの部品（前のセーブデータにはない）
            if (p.Length >= 19)
            {
                int.TryParse(p[10], out look.Gender);
                int.TryParse(p[11], out look.PHair);
                int.TryParse(p[12], out look.PEyes);
                int.TryParse(p[13], out look.PMouth);
                int.TryParse(p[14], out look.PNose);
                int.TryParse(p[15], out look.PTop);
                int.TryParse(p[16], out look.PBottom);
                int.TryParse(p[17], out look.PShoes);
                int.TryParse(p[18], out look.PHat);
            }
            // コーデ（前のデータには ない → 前の服のまま）
            if (p.Length >= 20) int.TryParse(p[19], out look.POutfit);
            else look.POutfit = 0;
            look.Clamp();
            return look;
        }

        public void Clamp()
        {
            HairStyle = Wrap(HairStyle, CharacterArt.HairStyleNames.Length);
            Hair = Wrap(Hair, CharacterArt.HairColors.Length);
            Cloth = Wrap(Cloth, CharacterArt.ClothColors.Length);
            Skin = Wrap(Skin, CharacterArt.SkinColors.Length);
            Hat = Wrap(Hat, CharacterArt.HatNames.Length);
            Eyes = Wrap(Eyes, CharacterArt.EyeNames.Length);
            EyeColor = Wrap(EyeColor, CharacterArt.EyeColors.Length);
            Outfit = Wrap(Outfit, CharacterArt.OutfitNames.Length);
            Accessory = Wrap(Accessory, CharacterArt.AccessoryNames.Length);
        }

        private static int Wrap(int v, int n) => ((v % n) + n) % n;
    }

    /// <summary>
    /// キャラクターのドット絵（16x16）を、えらんだ見た目から作る。
    /// うしろのかみ → からだとふく → 顔 → 前がみ → ぼうし → アクセサリー の順に重ねて描き、
    /// かみとふくに光とかげをつけて、まわりをふちどる。
    /// かみがた・ぼうしなどを増やしたいときは、名前の配列と、下の描く関数に1つ足してください。
    /// </summary>
    public static class CharacterArt
    {
        public static readonly string[] HairStyleNames =
            { "ショート", "ロング", "ツンツン", "おだんご", "ポニーテール", "ボブ", "ベリーショート", "ツインテール", "アホ毛", "もじゃもじゃ", "ななめ前髪", "オールバック" };
        public static readonly string[] HatNames =
            { "なし", "まほうの帽子", "キャップ", "リボン", "かんむり", "ずきん", "バンダナ", "ねこみみ", "つの", "花かざり",
              "風船のリボン", "シェフのぼうし", "ルーンのかんむり" }; // 10〜12 は風船割りのほうび（Balloon.Cosmetics）
        public static readonly string[] EyeNames = { "ふつう", "にっこり", "キラキラ", "つり目", "ねむそう" };
        public static readonly string[] OutfitNames = { "ローブ", "チュニック", "パーカー", "よろい", "ドレス", "マント", "オーバーオール",
              "金のマント", "虹のローブ" }; // 7〜8 は風船割りのほうび
        public static readonly string[] AccessoryNames = { "なし", "メガネ", "ほっぺ", "マフラー", "そばかす", "ばんそうこう" };

        public static readonly (string name, Color32 color)[] HairColors =
        {
            ("ちゃいろ", C(110, 70, 40)), ("くろ", C(40, 35, 45)), ("きんいろ", C(235, 195, 80)), ("あか", C(200, 70, 50)),
            ("あお", C(70, 120, 220)), ("しろ", C(235, 235, 240)), ("ピンク", C(240, 130, 180)), ("みどり", C(70, 170, 100)),
            ("むらさき", C(140, 90, 200)), ("オレンジ", C(240, 140, 50)),
        };

        public static readonly (string name, Color32 color)[] ClothColors =
        {
            ("あお", C(60, 110, 220)), ("あか", C(210, 60, 60)), ("みどり", C(60, 170, 90)), ("むらさき", C(140, 80, 200)),
            ("きいろ", C(230, 190, 50)), ("くろ", C(50, 50, 60)), ("しろ", C(230, 230, 235)), ("オレンジ", C(240, 140, 50)),
            ("みずいろ", C(100, 200, 235)), ("ピンク", C(240, 130, 180)),
        };

        public static readonly (string name, Color32 color)[] SkinColors =
        {
            ("うすい", C(255, 224, 192)), ("ふつう", C(240, 197, 152)), ("こんがり", C(205, 150, 105)), ("こい", C(150, 102, 70)), ("とてもこい", C(105, 70, 50)),
        };

        public static readonly (string name, Color32 color)[] EyeColors =
        {
            ("くろ", C(35, 30, 45)), ("ちゃいろ", C(110, 70, 40)), ("あお", C(50, 110, 210)), ("みどり", C(40, 150, 80)), ("あか", C(200, 50, 60)), ("むらさき", C(130, 70, 190)),
        };

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        /// <summary>そのぼうし・ふくを使えるか（風船割りのほうびは、もらってから）</summary>
        public static bool HatUnlocked(int i) => Progress.TotalBalloons >= Balloon.NeedFor(true, i);
        public static bool OutfitUnlocked(int i) => Progress.TotalBalloons >= Balloon.NeedFor(false, i);

        /// <summary>まだもらっていないぼうし・ふくを、ふつうのものにもどす</summary>
        public static void RemoveLocked(CharacterLook look)
        {
            if (!HatUnlocked(look.Hat)) look.Hat = 0;
            if (!OutfitUnlocked(look.Outfit)) look.Outfit = 0;
        }

        private static readonly Color32 White = C(255, 255, 255);
        private static readonly Color32 Pants = C(70, 62, 66);
        private static readonly Color32 Shoe = C(90, 58, 40);
        private static readonly Color32 Blush = C(250, 140, 150);
        private static readonly Color32 Gold = C(245, 205, 70);
        private static readonly Color32 GoldDark = C(190, 140, 40);

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Build(CharacterLook look)
        {
            look.Clamp();
            string key = look.Encode().Substring(look.Encode().IndexOf('|'));
            if (cache.TryGetValue(key, out var s) && s != null) return s;

            var c = new PixelArt.Canvas(16, 16);
            Color32 hair = HairColors[look.Hair].color, skin = SkinColors[look.Skin].color, cloth = ClothColors[look.Cloth].color;

            BackHair(c, look.HairStyle, hair);
            if (look.Hat == 5) c.Rect(2, 1, 12, 10, PixelArt.Mul(cloth, 0.8f));  // ずきんのうしろ
            Body(c, look.Outfit, cloth, skin);
            Head(c, skin);
            Face(c, look.Eyes, EyeColors[look.EyeColor].color, skin);
            FrontHair(c, look.HairStyle, hair);
            Hat(c, look.Hat, hair, cloth);
            Accessory(c, look.Accessory, cloth);

            PixelArt.Outline(c, 0.3f);
            s = PixelArt.ToSprite(c, "player" + key);
            cache[key] = s;
            return s;
        }

        // ----- からだ -----

        private static void Head(PixelArt.Canvas c, Color32 skin)
        {
            c.Rect(4, 2, 8, 9, skin);
            c.Rect(3, 3, 10, 7, skin);
            Color32 shade = PixelArt.Mul(skin, 0.86f);
            c.Rect(12, 4, 1, 5, shade);
            c.Rect(4, 10, 8, 1, shade);
        }

        private static void Face(PixelArt.Canvas c, int style, Color32 eye, Color32 skin)
        {
            Color32 dark = PixelArt.Mul(eye, 0.6f);
            foreach (int ex in new[] { 5, 9 })
            {
                switch (style)
                {
                    case 1: // にっこり
                        c.Set(ex - 1, 7, dark); c.Set(ex, 6, dark); c.Set(ex + 1, 6, dark); c.Set(ex + 2, 7, dark);
                        break;
                    case 2: // キラキラ
                        c.Rect(ex, 5, 2, 3, eye); c.Rect(ex, 5, 2, 1, dark);
                        c.Set(ex, 6, White); c.Set(ex + 1, 7, PixelArt.Add(eye, 70));
                        break;
                    case 3: // つり目（外がわが上がる）
                        c.Rect(ex, 7, 2, 1, eye);
                        c.Set(ex == 5 ? ex : ex + 1, 6, dark);
                        break;
                    case 4: // ねむそう
                        c.Rect(ex, 7, 2, 1, dark);
                        break;
                    default: // ふつう
                        c.Rect(ex, 6, 2, 2, eye); c.Rect(ex, 6, 2, 1, dark);
                        c.Set(ex, 6, White);
                        break;
                }
            }
            c.Rect(7, 9, 2, 1, PixelArt.Mul(skin, 0.6f)); // 口
        }

        private static void Body(PixelArt.Canvas c, int outfit, Color32 cloth, Color32 skin)
        {
            Color32 dark = PixelArt.Mul(cloth, 0.75f), light = PixelArt.Add(cloth, 35);
            // 足とくつ（ふくでかくれることもある）
            c.Rect(5, 14, 2, 1, Pants); c.Rect(9, 14, 2, 1, Pants);
            c.Rect(4, 15, 3, 1, Shoe); c.Rect(9, 15, 3, 1, Shoe);
            // 胴とうで
            c.Rect(4, 11, 8, 3, cloth);
            c.Rect(3, 11, 1, 2, cloth); c.Rect(12, 11, 1, 2, dark);
            c.Set(3, 13, skin); c.Set(12, 13, skin);   // 手
            c.Rect(11, 11, 1, 3, dark);                 // 右がわのかげ
            c.Rect(4, 11, 7, 1, light);                 // 肩の光

            switch (outfit)
            {
                case 0: // ローブ（すそが長い・まん中に明るい線）
                    c.Rect(4, 14, 8, 1, dark);
                    c.Rect(7, 11, 2, 3, light);
                    break;
                case 1: // チュニック（ベルト）
                    c.Rect(4, 13, 8, 1, C(120, 78, 45)); c.Set(7, 13, Gold);
                    break;
                case 2: // パーカー（フード・ポケット・ひも）
                    c.Rect(4, 10, 8, 1, dark);
                    c.Rect(6, 13, 4, 1, dark);
                    c.Set(6, 12, White); c.Set(9, 12, White);
                    break;
                case 3: // よろい（銀のむね当てと肩当て）
                    Color32 steel = C(190, 195, 210), steelDark = C(120, 125, 140);
                    c.Rect(5, 11, 6, 3, steel); c.Rect(10, 11, 1, 3, steelDark); c.Set(6, 11, White);
                    c.Rect(2, 11, 2, 1, steel); c.Rect(12, 11, 2, 1, steelDark);
                    c.Rect(7, 12, 2, 2, cloth);
                    break;
                case 4: // ドレス（スカートが広がる）
                    c.Rect(3, 13, 10, 2, cloth); c.Rect(3, 14, 10, 1, dark);
                    c.Rect(6, 11, 4, 1, White);
                    break;
                case 5: // マント（うしろに長い）
                    c.Rect(2, 11, 1, 4, dark); c.Rect(13, 11, 1, 4, dark);
                    c.Rect(4, 11, 8, 1, Gold);
                    c.Rect(4, 13, 8, 1, C(120, 78, 45));
                    break;
                case 7: // 金のマント（風船割りのほうび）：金色のマントとえり
                    c.Rect(2, 11, 1, 4, GoldDark); c.Rect(13, 11, 1, 4, GoldDark);
                    c.Rect(2, 10, 12, 1, Gold);
                    c.Rect(4, 11, 8, 1, Gold); c.Set(7, 12, C(230, 60, 70));
                    c.Rect(4, 13, 8, 1, GoldDark);
                    break;
                case 8: // 虹のローブ（風船割りのほうび）：にじ色のしま
                {
                    var rainbow = new[] { C(235, 80, 80), C(245, 160, 60), C(240, 220, 80), C(90, 200, 110), C(80, 150, 235), C(160, 100, 220) };
                    for (int x = 3; x <= 12; x++)
                        for (int y = 11; y <= 14; y++)
                            if (c.Get(x, y).a != 0 || y == 14) c.Set(x, y, rainbow[(x + y) % rainbow.Length]);
                    c.Rect(4, 14, 8, 1, PixelArt.Mul(rainbow[4], 0.8f));
                    c.Set(3, 13, skin); c.Set(12, 13, skin);
                    break;
                }
                case 6: // オーバーオール（白いシャツ＋つりひも）
                    c.Rect(3, 11, 10, 1, White); c.Set(3, 12, White); c.Set(12, 12, White);
                    c.Rect(5, 11, 1, 1, cloth); c.Rect(10, 11, 1, 1, cloth);
                    c.Rect(5, 12, 6, 2, cloth); c.Set(6, 12, Gold); c.Set(9, 12, Gold);
                    c.Rect(5, 14, 2, 1, cloth); c.Rect(9, 14, 2, 1, cloth);
                    break;
            }
        }

        // ----- かみ -----

        private static void BackHair(PixelArt.Canvas c, int style, Color32 h)
        {
            Color32 d = PixelArt.Mul(h, 0.78f);
            switch (style)
            {
                case 1: // ロング
                    c.Rect(2, 4, 2, 9, d); c.Rect(12, 4, 2, 9, d);
                    break;
                case 4: // ポニーテール
                    c.Rect(13, 3, 2, 7, d); c.Set(14, 10, d); c.Set(13, 3, C(220, 70, 80));
                    break;
                case 5: // ボブ
                    c.Rect(2, 4, 2, 6, d); c.Rect(12, 4, 2, 6, d);
                    break;
                case 7: // ツインテール
                    c.Rect(0, 4, 2, 7, d); c.Set(1, 11, d); c.Rect(14, 4, 2, 7, d); c.Set(14, 11, d);
                    c.Set(2, 4, C(220, 70, 80)); c.Set(13, 4, C(220, 70, 80));
                    break;
            }
        }

        private static void Cap(PixelArt.Canvas c, Color32 h)
        {
            c.Rect(4, 1, 8, 1, h);
            c.Rect(3, 2, 10, 2, h);
            c.Set(3, 4, h); c.Set(12, 4, h);
        }

        private static void FrontHair(PixelArt.Canvas c, int style, Color32 h)
        {
            Color32 hi = PixelArt.Add(h, 45), d = PixelArt.Mul(h, 0.78f);
            switch (style)
            {
                case 2: // ツンツン
                    Cap(c, h);
                    foreach (int x in new[] { 3, 6, 9, 12 }) { c.Set(x, 0, h); c.Set(x, 1, h); }
                    c.Set(4, 4, h); c.Set(6, 4, h); c.Set(8, 4, h); c.Set(10, 4, h);
                    break;
                case 3: // おだんご
                    Cap(c, h);
                    c.Ellipse(3, 2, 2.2f, 2.2f, h); c.Ellipse(13, 2, 2.2f, 2.2f, h);
                    c.Rect(4, 4, 8, 1, h);
                    break;
                case 6: // ベリーショート
                    c.Rect(4, 2, 8, 1, h); c.Rect(3, 3, 10, 1, d);
                    break;
                case 8: // アホ毛
                    Cap(c, h);
                    c.Rect(4, 4, 8, 1, h);
                    c.Set(8, 0, h); c.Set(9, 0, h); c.Set(10, 0, d); // ぴょんと立った毛
                    break;
                case 9: // もじゃもじゃ
                    c.Ellipse(8, 3, 7.5f, 3.6f, h);
                    for (int x = 2; x < 14; x += 2) c.Set(x, 2 + (x / 2) % 2, d);
                    c.Set(3, 5, h); c.Set(12, 5, h);
                    break;
                case 10: // ななめ前髪
                    Cap(c, h);
                    c.Rect(4, 4, 6, 1, h); c.Rect(4, 5, 2, 1, h);
                    break;
                case 11: // オールバック
                    Cap(c, h);
                    c.Rect(4, 1, 8, 1, d);
                    break;
                default: // ショート・ロング・ポニーテール・ボブ・ツインテール（前がみ）
                    Cap(c, h);
                    c.Rect(4, 4, 8, 1, h);
                    c.Set(7, 4, PixelArt.Clear);
                    if (style == 5) { c.Rect(3, 4, 1, 3, h); c.Rect(12, 4, 1, 3, h); }
                    break;
            }
            // かみの光
            if (style != 6) { c.Set(5, 2, hi); c.Set(6, 2, hi); c.Set(5, 3, hi); }
            // 前がみをけしたところは、はだの色にもどす
            if (c.Get(7, 4).a == 0) c.Set(7, 4, c.Get(7, 5));
        }

        // ----- ぼうし -----

        private static void Hat(PixelArt.Canvas c, int hat, Color32 hair, Color32 cloth)
        {
            switch (hat)
            {
                case 1: // まほうの帽子（とんがり・つば・星）
                {
                    Color32 p = C(110, 60, 170), pd = C(80, 40, 130);
                    c.Rect(1, 3, 14, 1, pd); c.Rect(2, 2, 12, 1, p);
                    c.Rect(4, 1, 8, 1, p); c.Rect(6, 0, 4, 1, p); c.Set(10, 0, p); c.Set(11, 0, pd);
                    c.Rect(4, 2, 8, 1, C(240, 200, 70));
                    c.Set(7, 1, C(255, 240, 120));
                    break;
                }
                case 2: // キャップ
                {
                    Color32 r = C(220, 70, 60), rd = C(160, 45, 40);
                    c.Rect(4, 1, 8, 1, r); c.Rect(3, 2, 10, 2, r);
                    c.Rect(3, 4, 11, 1, rd);
                    c.Set(8, 2, White);
                    break;
                }
                case 3: // リボン
                {
                    Color32 r = C(240, 90, 150), rd = C(190, 60, 110);
                    c.Rect(10, 0, 2, 3, r); c.Rect(13, 0, 2, 3, r); c.Rect(12, 1, 1, 1, rd);
                    break;
                }
                case 4: // かんむり
                    c.Rect(4, 1, 8, 2, Gold);
                    foreach (int x in new[] { 4, 7, 8, 11 }) c.Set(x, 0, Gold);
                    c.Set(6, 1, C(230, 60, 70)); c.Set(9, 1, C(80, 170, 255));
                    break;
                case 5: // ずきん（ふちだけ。うしろは Build で先に描く）
                {
                    Color32 hood = PixelArt.Mul(cloth, 0.9f);
                    c.Rect(3, 1, 10, 2, hood); c.Rect(2, 3, 1, 7, hood); c.Rect(13, 3, 1, 7, hood);
                    c.Rect(4, 0, 8, 1, hood);
                    break;
                }
                case 6: // バンダナ
                {
                    Color32 b = C(60, 120, 220);
                    c.Rect(3, 3, 10, 1, b); c.Rect(13, 3, 2, 1, b); c.Set(14, 4, b);
                    c.Set(5, 3, White); c.Set(9, 3, White);
                    break;
                }
                case 7: // ねこみみ
                    foreach (int x in new[] { 3, 10 })
                    {
                        c.Rect(x, 0, 3, 2, hair);
                        c.Set(x + 1, 1, C(250, 160, 180));
                    }
                    c.Set(3, 0, PixelArt.Clear); c.Set(12, 0, PixelArt.Clear);
                    break;
                case 8: // つの
                    c.Rect(3, 0, 1, 2, C(240, 230, 200)); c.Set(4, 1, C(240, 230, 200));
                    c.Rect(12, 0, 1, 2, C(240, 230, 200)); c.Set(11, 1, C(240, 230, 200));
                    break;
                case 10: // 風船のリボン（風船割りのほうび）：リボンと、ふわふわうかぶ小さな風船
                {
                    Color32 r = C(240, 90, 150), b = C(90, 170, 255);
                    c.Rect(3, 1, 2, 2, r); c.Rect(6, 1, 2, 2, r); c.Set(5, 2, PixelArt.Mul(r, 0.7f));
                    c.Ellipse(13, 1.5f, 1.8f, 1.8f, b); c.Set(12, 0, C(200, 230, 255));
                    c.Set(13, 3, C(220, 220, 220));
                    break;
                }
                case 11: // シェフのぼうし（風船割りのほうび）：白くてふわふわ
                {
                    Color32 w = C(250, 250, 250), sh = C(210, 210, 220);
                    c.Ellipse(8, 1.2f, 4.6f, 2.2f, w);
                    c.Rect(4, 2, 8, 2, w);
                    c.Rect(4, 3, 8, 1, sh);
                    c.Set(6, 1, sh); c.Set(9, 1, sh);
                    break;
                }
                case 12: // ルーンのかんむり（風船割りのほうび）：金のかんむりに青いルーン
                    c.Rect(3, 1, 10, 2, Gold);
                    foreach (int x in new[] { 3, 5, 8, 10, 12 }) c.Set(x, 0, Gold);
                    c.Set(7, 0, C(120, 220, 255)); c.Set(8, 0, C(120, 220, 255));
                    c.Set(5, 1, C(80, 170, 255)); c.Set(8, 1, C(170, 90, 230)); c.Set(11, 1, C(80, 170, 255));
                    c.Rect(3, 2, 10, 1, GoldDark);
                    break;
                case 9: // 花かざり
                    foreach (var (x, col) in new[] { (4, C(250, 120, 150)), (7, C(255, 230, 90)), (10, C(150, 120, 250)), (12, C(250, 120, 150)) })
                    {
                        c.Set(x, 1, col); c.Set(x + 1, 2, col); c.Set(x - 1, 2, col); c.Set(x, 2, C(255, 255, 210));
                    }
                    break;
            }
        }

        // ----- アクセサリー -----

        private static void Accessory(PixelArt.Canvas c, int a, Color32 cloth)
        {
            switch (a)
            {
                case 1: // メガネ
                {
                    Color32 f = C(60, 50, 60);
                    foreach (int ex in new[] { 4, 8 })
                    {
                        c.Rect(ex, 5, 4, 1, f); c.Rect(ex, 8, 4, 1, f); c.Rect(ex, 5, 1, 4, f); c.Rect(ex + 3, 5, 1, 4, f);
                    }
                    break;
                }
                case 2: // ほっぺ
                    c.Set(4, 8, Blush); c.Set(11, 8, Blush);
                    break;
                case 3: // マフラー
                {
                    Color32 m = C(230, 70, 70), md = C(170, 45, 45);
                    c.Rect(4, 10, 8, 2, m); c.Rect(4, 11, 8, 1, md);
                    c.Rect(9, 12, 2, 2, m); c.Set(10, 13, md);
                    break;
                }
                case 4: // そばかす
                {
                    Color32 f = C(190, 120, 90);
                    c.Set(4, 8, f); c.Set(5, 9, f); c.Set(10, 9, f); c.Set(11, 8, f);
                    break;
                }
                case 5: // ばんそうこう
                    c.Rect(10, 8, 2, 1, C(240, 210, 160)); c.Set(10, 8, C(220, 180, 130));
                    break;
            }
        }
    }
}
