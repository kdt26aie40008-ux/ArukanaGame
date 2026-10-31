using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// パーツきせかえ：Resources/Art/CharacterParts/ の部品の画像を重ねて、1人のキャラの絵にする。
    /// 部品は、体の上の正しい位置・大きさに そろえてから書き出してある（余白を切りつめた画像＋置く位置 parts.txt）。
    /// 重ねる順番：体 → 目 → 鼻 → 口 → ズボン・スカート → 服 → 靴 → 髪 → 帽子
    /// </summary>
    public static class CharacterParts
    {
        public static readonly string[] Genders = { "男の子", "女の子" };
        private static readonly string[] Order = { "体", "目", "鼻", "口", "下", "服", "靴", "全身", "髪", "帽子" };
        private const string Root = "Art/CharacterParts/";
        /// <summary>町・遺跡で 表示する大きさ（1マスの何倍）。足の下は マスの下に そろえる</summary>
        public const float DisplayScale = 1.4f;

        private static bool loaded;
        private static int size = 300;
        private static readonly Dictionary<string, Vector2Int> positions = new Dictionary<string, Vector2Int>();
        private static readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private const int MaxCache = 24;

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            if (!ArtOverrides.Enabled) return;
            var text = Resources.Load<TextAsset>(Root + "parts");
            if (text == null) return;
            foreach (var raw in text.text.Split('\n'))
            {
                var p = raw.Trim().Split(' ');
                if (p.Length != 3) continue;
                if (p[0] == "canvas") { int.TryParse(p[1], out size); continue; }
                if (!int.TryParse(p[1], out int x) || !int.TryParse(p[2], out int y)) continue;
                positions[p[0]] = new Vector2Int(x, y);
                var key = p[0].Substring(0, p[0].LastIndexOf('/')); // 男の子/髪
                counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            }
        }

        /// <summary>部品の画像があるか（パーツきせかえが使えるか）</summary>
        public static bool Available { get { Load(); return positions.Count > 0; } }

        /// <summary>その性別・種類の部品の数（例：男の子の髪は12）</summary>
        public static int Count(int gender, string category)
        {
            Load();
            return counts.TryGetValue($"{Genders[Mathf.Clamp(gender, 0, 1)]}/{category}", out int n) ? n : 0;
        }

        /// <summary>えらんだ部品を重ねた絵（同じ組み合わせは使い回す）</summary>
        public static Sprite Build(CharacterLook look)
        {
            if (!Available) return null;
            string g = Genders[Mathf.Clamp(look.Gender, 0, 1)];
            var pick = new Dictionary<string, int>
            {
                { "体", 1 }, { "目", look.PEyes + 1 }, { "鼻", look.PNose + 1 }, { "口", look.PMouth + 1 }, { "下", look.PBottom + 1 },
                { "服", look.PTop + 1 }, { "靴", look.PShoes + 1 }, { "髪", look.PHair + 1 }, { "帽子", look.PHat }, // 帽子は 0 なら なし
                { "全身", look.POutfit }, // コーデ：0 なら なし
            };
            // コーデ（全身の服）のときは、ふく・ズボン・くつは 使わない
            if (look.POutfit > 0) { pick["服"] = 0; pick["下"] = 0; pick["靴"] = 0; }
            string key = g;
            foreach (var cat in Order) key += "," + pick[cat];
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var buf = new Color32[size * size];
            foreach (var cat in Order)
            {
                int n = pick[cat];
                if (n <= 0) continue;
                string id = $"{g}/{cat}/{n:00}";
                if (!positions.TryGetValue(id, out var at)) continue;
                var tex = Resources.Load<Texture2D>(Root + id);
                if (tex == null || !tex.isReadable) continue;
                Blit(buf, tex, at.x, at.y);
            }
            var outTex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            outTex.SetPixels32(buf);
            outTex.Apply(true);
            // 二回り大きく（DisplayScale 倍）。ちゅうしんを下げて、足の下が マスの下（-0.5）に来るように
            var sprite = Sprite.Create(outTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f / DisplayScale), size / DisplayScale);
            sprite.name = "parts_" + key;
            if (cache.Count >= MaxCache) cache.Clear();
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>部品の画像を、キャンバスの (x, y)（左上から）に重ねる</summary>
        private static void Blit(Color32[] buf, Texture2D tex, int x, int y)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            for (int ty = 0; ty < h; ty++)
            {
                // テクスチャは下から上、キャンバスの (x, y) は上から下
                int row = size - 1 - (y + (h - 1 - ty));
                if (row < 0 || row >= size) continue;
                for (int tx = 0; tx < w; tx++)
                {
                    int col = x + tx;
                    if (col < 0 || col >= size) continue;
                    var s = px[ty * w + tx];
                    if (s.a == 0) continue;
                    int i = row * size + col;
                    var d = buf[i];
                    float a = s.a / 255f, da = d.a / 255f, oa = a + da * (1 - a);
                    buf[i] = new Color32(
                        (byte)((s.r * a + d.r * da * (1 - a)) / oa), (byte)((s.g * a + d.g * da * (1 - a)) / oa),
                        (byte)((s.b * a + d.b * da * (1 - a)) / oa), (byte)(oa * 255));
                }
            }
        }

        /// <summary>部品の番号を、その性別の部品の数におさめる</summary>
        public static void Clamp(CharacterLook look)
        {
            look.Gender = Mathf.Clamp(look.Gender, 0, 1);
            if (!Available) return;
            look.PHair = Wrap(look.PHair, Count(look.Gender, "髪"));
            look.PEyes = Wrap(look.PEyes, Count(look.Gender, "目"));
            look.PMouth = Wrap(look.PMouth, Count(look.Gender, "口"));
            look.PNose = Wrap(look.PNose, Count(look.Gender, "鼻"));
            look.PTop = Wrap(look.PTop, Count(look.Gender, "服"));
            look.PBottom = Wrap(look.PBottom, Count(look.Gender, "下"));
            look.PShoes = Wrap(look.PShoes, Count(look.Gender, "靴"));
            look.PHat = Wrap(look.PHat, Count(look.Gender, "帽子") + 1); // 0 は なし
            int outfits = Count(look.Gender, "全身");
            // ふく・ズボン・くつの部品が ないときは、コーデは かならず どれか（1〜）。あるときは 0 が「なし」
            look.POutfit = SeparateClothes(look.Gender) ? Wrap(look.POutfit, outfits + 1) : outfits > 0 ? 1 + Wrap(look.POutfit - 1, outfits) : 0;
        }

        private static int Wrap(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;

        /// <summary>ふく・ズボン・くつを 1つずつ えらべるか（その部品の画像があるか）</summary>
        public static bool SeparateClothes(int gender) => Count(gender, "服") > 0;

        /// <summary>おまかせ</summary>
        public static void Randomize(CharacterLook look)
        {
            look.PHair = Random.Range(0, 100);
            look.PEyes = Random.Range(0, 100);
            look.PMouth = Random.Range(0, 100);
            look.PNose = Random.Range(0, 100);
            look.PTop = Random.Range(0, 100);
            look.PBottom = Random.Range(0, 100);
            look.PShoes = Random.Range(0, 100);
            look.PHat = Random.value < 0.4f ? 0 : Random.Range(1, 100);
            look.POutfit = Count(look.Gender, "全身") > 0 && (!SeparateClothes(look.Gender) || Random.value < 0.6f) ? Random.Range(1, 100) : 0;
            Clamp(look);
        }
    }
}
