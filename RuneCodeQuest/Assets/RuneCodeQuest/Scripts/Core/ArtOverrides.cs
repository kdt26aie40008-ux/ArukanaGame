using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 画像ファイルの絵（Assets/RuneCodeQuest/Resources/Art/ の下）。
    /// 決まった名前の PNG があれば その絵を使い、なければ プログラムで描いた絵を使う。
    /// ・Art/Arcana/〈アルカナの名前〉.png（例：プチアルカ.png、オルディナ.png）
    /// ・Art/Character/〈町の人の名前〉.png（例：師匠.png、町長.png）、主人公.png、ゴーレム.png、ねこ.png
    /// ・Art/Building/〈村の名前〉_〈建物の名前〉_直った.png ／ _こわれた.png（例：ルーネ村_宿舎_直った.png）
    /// ・Art/Inn/〈名前〉.png：宿舎の中（床・かべ・かべ_窓・ベッド・だんろ・鏡・たんす・観葉植物・机・テーブル・カウンター・じゅうたん・ドア・ランプ など）
    /// 画像は 正方形がおすすめ（1枚が ちょうど1マスの大きさになる）。
    /// </summary>
    public static class ArtOverrides
    {
        public static bool Enabled = true; // Unity の外のテストでは false にする
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path)
        {
            if (!Enabled) return null;
            if (cache.TryGetValue(path, out var s)) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear; // 大きな絵を縮めて使うので、なめらかに（ミップマップは読みこみ設定で作る）
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Mathf.Max(tex.width, tex.height));
                s.name = path;
            }
            cache[path] = s;
            return s;
        }

        /// <summary>はばを unitsWide マスにした絵。中心は (0.5, pivotY)（0 が下はし）</summary>
        public static Sprite Sized(string path, float unitsWide, float pivotY, float pivotX = 0.5f)
        {
            if (!Enabled) return null;
            string key = $"{path}|{unitsWide}|{pivotY}|{pivotX}";
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(pivotX, pivotY), tex.width / unitsWide);
                s.name = key;
            }
            cache[key] = s;
            return s;
        }

        /// <summary>
        /// 地面に立つ絵：はばを unitsWide マスにして、下はしが 置いた場所から bottom マス 下に来るようにする
        /// （建物の絵は、建物の まん中に置くので bottom ＝ 建物の高さの半分）
        /// </summary>
        public static Sprite Grounded(string path, float unitsWide, float bottom, float pivotX = 0.5f)
        {
            if (!Enabled) return null;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            float h = tex.height * unitsWide / tex.width;
            return Sized(path, unitsWide, bottom / h, pivotX);
        }

        /// <summary>建物の絵（Art/Building/〈村〉_〈建物〉_直った／こわれた.png）。なければ null</summary>
        public static Sprite Building(BuildingInfo b, bool repaired) =>
            Grounded($"Art/Building/{TownData.Villages[b.Village].Name}_{b.Name}_{(repaired ? "直った" : "こわれた")}",
                (b.W + 0.6f) * (ArtScale.TryGetValue(b.Name, out float k) ? k : 1f), b.H / 2f + 0.3f,
                DoorAt.TryGetValue(b.Name, out float d) ? d : 0.5f);

        /// <summary>建物の絵の 大きさの 調整（たてに長い 灯台は 少し小さく）</summary>
        private static readonly Dictionary<string, float> ArtScale = new Dictionary<string, float> { { "灯台", 0.8f } };

        /// <summary>建物の絵の 入り口の位置（左はしから 何わり）。まん中でない絵だけ（水車小屋は 右に水車がある）</summary>
        private static readonly Dictionary<string, float> DoorAt = new Dictionary<string, float> { { "水車小屋", 0.38f }, { "よろず屋", 0.45f }, { "灯台", 0.65f },
            { "木の上の宿", 0.45f }, { "薬草屋", 0.58f }, { "葉っぱのしたて屋", 0.39f }, { "風の大樹", 0.53f } };

        /// <summary>宿舎の中の 家具・小物（1マスの大きさ）</summary>
        public static Sprite Inn(string name) => Get("Art/Inn/" + name);
        /// <summary>宿舎の中の 床・かべ（はばが ちょうど1マス）</summary>
        public static Sprite InnTile(string name) => Sized("Art/Inn/" + name, 1f, 0.5f);

        public static Sprite Arcana(string name) => Get("Art/Arcana/" + name);
        public static Sprite Character(string name) => Get("Art/Character/" + name);

        /// <summary>主人公のイラスト（キャラクリで「イラスト」をえらんでいて、画像があるとき）</summary>
        public static Sprite Hero => Progress.LookMode == Progress.LookIllust ? Character("主人公") : null;
    }
}
