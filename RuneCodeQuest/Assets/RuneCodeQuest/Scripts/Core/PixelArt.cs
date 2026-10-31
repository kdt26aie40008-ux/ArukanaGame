using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// ドット絵を細かく・きれいにする道具。
    /// ・Canvas：色の配列にドットを打つ（[x, y]、y=0 が上）
    /// ・Upscale：8x8 の絵を 16x16 にして、ななめをなめらかにし（Scale2x）、
    ///            かげ（上は明るく・下と右は暗く）とふちどりをつける
    /// ・ToSprite：1枚の絵がちょうど 1マスになる Sprite にする
    /// </summary>
    public static class PixelArt
    {
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public class Canvas
        {
            public readonly int W, H;
            public readonly Color32[,] Px;

            public Canvas(int w, int h)
            {
                W = w;
                H = h;
                Px = new Color32[w, h];
            }

            public Canvas(Color32[,] px)
            {
                Px = px;
                W = px.GetLength(0);
                H = px.GetLength(1);
            }

            public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
            public Color32 Get(int x, int y) => In(x, y) ? Px[x, y] : Clear;
            public void Set(int x, int y, Color32 c) { if (In(x, y)) Px[x, y] = c; }

            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int yy = y; yy < y + h; yy++)
                    for (int xx = x; xx < x + w; xx++) Set(xx, yy, c);
            }

            /// <summary>だ円（中心 cx,cy、半径 rx,ry）をぬる</summary>
            public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Px[x, y] = c;
                    }
            }

            /// <summary>文字の絵を (ox, oy) からぬる（'.' と辞書にない文字はぬらない）</summary>
            public void Stamp(int ox, int oy, string[] art, IDictionary<char, Color32> pal)
            {
                for (int row = 0; row < art.Length; row++)
                    for (int col = 0; col < art[row].Length; col++)
                        if (pal.TryGetValue(art[row][col], out var c)) Set(ox + col, oy + row, c);
            }
        }

        public static Color32 Mul(Color32 c, float k) =>
            new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);

        public static Color32 Add(Color32 c, int d) =>
            new Color32((byte)Mathf.Clamp(c.r + d, 0, 255), (byte)Mathf.Clamp(c.g + d, 0, 255), (byte)Mathf.Clamp(c.b + d, 0, 255), c.a);

        public static Color32 Mix(Color32 a, Color32 b, float t) =>
            new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), (byte)Mathf.Lerp(a.a, b.a, t));

        private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        /// <summary>文字の絵を色の配列にする</summary>
        public static Canvas FromStrings(string[] art, IDictionary<char, Color32> pal)
        {
            int h = art.Length, w = art[0].Length;
            var c = new Canvas(w, h);
            c.Stamp(0, 0, art, pal);
            return c;
        }

        /// <summary>Scale2x：2倍にして、ななめの線をなめらかにする</summary>
        public static Canvas Scale2x(Canvas s)
        {
            var d = new Canvas(s.W * 2, s.H * 2);
            for (int y = 0; y < s.H; y++)
                for (int x = 0; x < s.W; x++)
                {
                    Color32 p = s.Get(x, y), a = s.Get(x, y - 1), b = s.Get(x + 1, y), c = s.Get(x - 1, y), dd = s.Get(x, y + 1);
                    Color32 e0 = p, e1 = p, e2 = p, e3 = p;
                    if (!Same(c, b) && !Same(a, dd))
                    {
                        if (Same(a, c)) e0 = a;
                        if (Same(a, b)) e1 = b;
                        if (Same(dd, c)) e2 = c;
                        if (Same(dd, b)) e3 = b;
                    }
                    d.Px[x * 2, y * 2] = e0;
                    d.Px[x * 2 + 1, y * 2] = e1;
                    d.Px[x * 2, y * 2 + 1] = e2;
                    d.Px[x * 2 + 1, y * 2 + 1] = e3;
                }
            return d;
        }

        /// <summary>かげをつける：色のかたまりの上のふちは明るく、下と右のふちは暗く</summary>
        public static void Shade(Canvas c, float light = 1.18f, float dark = 0.78f)
        {
            var src = (Color32[,])c.Px.Clone();
            var s = new Canvas(src);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    var p = s.Get(x, y);
                    if (p.a == 0) continue;
                    bool topEdge = !Same(s.Get(x, y - 1), p);
                    bool bottomEdge = !Same(s.Get(x, y + 1), p);
                    bool rightEdge = !Same(s.Get(x + 1, y), p);
                    if (bottomEdge || rightEdge) c.Px[x, y] = Mul(p, dark);
                    else if (topEdge) c.Px[x, y] = Mul(Add(p, 10), light);
                }
        }

        /// <summary>ふちどり：絵のまわりの透明なマスに、となりの色をこくした線を引く</summary>
        public static void Outline(Canvas c, float k = 0.35f)
        {
            var src = (Color32[,])c.Px.Clone();
            var s = new Canvas(src);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    if (s.Get(x, y).a != 0) continue;
                    Color32? n = null;
                    foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
                    {
                        var q = s.Get(x + dx, y + dy);
                        if (q.a != 0) { n = q; break; }
                    }
                    if (n.HasValue) c.Px[x, y] = Mul(n.Value, k);
                }
        }

        /// <summary>8x8 の文字の絵 → 16x16 のきれいな絵（なめらか＋かげ＋ふちどり）</summary>
        public static Canvas Upscale(string[] art, IDictionary<char, Color32> pal, bool outline = true)
        {
            var c = Scale2x(FromStrings(art, pal));
            Shade(c);
            if (outline) Outline(c);
            return c;
        }

        /// <summary>
        /// Sprite にする。pixelsPerUnit を省くと、1枚の絵がちょうど1マス（1ユニット）になる。
        /// 何マスもある絵（建物）は、1マスのドット数（16）をわたす
        /// </summary>
        public static Sprite ToSprite(Canvas c, string name, int pixelsPerUnit = 0)
        {
            // 仕上げ：2倍の細かさ＋光とかげ＋質感（大きさは変わらないように、1マスのドット数も2倍にする）
            c = PixelDetail.Refine(c);
            if (pixelsPerUnit > 0 && PixelDetail.Enabled) pixelsPerUnit *= PixelDetail.Scale;
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                    tex.SetPixel(x, c.H - 1 - y, c.Px[x, y]); // テクスチャは下から上
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, c.W, c.H), new Vector2(0.5f, 0.5f), pixelsPerUnit > 0 ? pixelsPerUnit : Mathf.Max(c.W, c.H));
            s.name = name;
            return s;
        }
    }
}
