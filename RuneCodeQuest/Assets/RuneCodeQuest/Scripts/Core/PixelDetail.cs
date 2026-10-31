using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// ドット絵の仕上げ（すべての絵は Sprite にする前に、ここを通る）。
    /// 1. 2倍の細かさにして、ななめの線をなめらかにする（Scale2x）
    /// 2. 左上から光が当たったように、絵の輪郭に 明るいところ と かげ をつける（2段階）。太くなった ふちどりは 外がわを半透明にして細く見せる
    /// 3. 広い色の面に、こまかい つぶつぶ（質感）をつける
    /// 絵のはしは「となりも同じ色」として扱うので、地面のタイルをならべても つなぎ目に線が出ない。
    /// </summary>
    public static class PixelDetail
    {
        public static bool Enabled = true;
        public const int Scale = 2; // 仕上げで何倍になるか（ToSprite の pixelsPerUnit もこの倍にする）

        public static PixelArt.Canvas Refine(PixelArt.Canvas src)
        {
            if (!Enabled) return src;
            var c = Scale2xClamp(src);
            Light(c);
            Grain(c);
            return c;
        }

        // 絵のはしの外は、いちばん近い はしの色
        private static Color32 At(Color32[,] px, int w, int h, int x, int y) =>
            px[Mathf.Clamp(x, 0, w - 1), Mathf.Clamp(y, 0, h - 1)];

        private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        /// <summary>Scale2x（はしは はしの色をのばして考える）</summary>
        private static PixelArt.Canvas Scale2xClamp(PixelArt.Canvas s)
        {
            var d = new PixelArt.Canvas(s.W * 2, s.H * 2);
            for (int y = 0; y < s.H; y++)
                for (int x = 0; x < s.W; x++)
                {
                    Color32 p = s.Px[x, y];
                    Color32 a = At(s.Px, s.W, s.H, x, y - 1), b = At(s.Px, s.W, s.H, x + 1, y);
                    Color32 l = At(s.Px, s.W, s.H, x - 1, y), dn = At(s.Px, s.W, s.H, x, y + 1);
                    Color32 e0 = p, e1 = p, e2 = p, e3 = p;
                    if (!Same(l, b) && !Same(a, dn))
                    {
                        if (Same(a, l)) e0 = a;
                        if (Same(a, b)) e1 = b;
                        if (Same(dn, l)) e2 = l;
                        if (Same(dn, b)) e3 = b;
                    }
                    d.Px[x * 2, y * 2] = e0;
                    d.Px[x * 2 + 1, y * 2] = e1;
                    d.Px[x * 2, y * 2 + 1] = e2;
                    d.Px[x * 2 + 1, y * 2 + 1] = e3;
                }
            return d;
        }

        private static int Luma(Color32 c) => (c.r * 3 + c.g * 6 + c.b) / 10;

        private static bool Clear(Color32 q) => q.a == 0;

        /// <summary>光とかげ：絵の外がわのふち（輪郭）だけ。左上は明るく、右下は暗く（内がわに もう1段うすく）</summary>
        private static void Light(PixelArt.Canvas c)
        {
            var src = (Color32[,])c.Px.Clone();
            int w = c.W, h = c.H;
            var white = new Color32(255, 255, 240, 255);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = src[x, y];
                    if (p.a == 0 || Luma(p) < 45) continue; // ふちどりの線（とても暗い色）はそのまま
                    // ふちどりの線のすぐ内がわ も「ふち」として考える
                    bool Edge(int dx, int dy)
                    {
                        var q = At(src, w, h, x + dx, y + dy);
                        if (Clear(q)) return true;
                        if (Luma(q) >= 45) return false;
                        return Clear(At(src, w, h, x + dx * 3, y + dy * 3)) || Clear(At(src, w, h, x + dx * 2, y + dy * 2));
                    }
                    if (Edge(1, 1) || Edge(0, 1) || Edge(1, 0)) c.Px[x, y] = PixelArt.Mul(p, 0.84f);
                    else if (Edge(-1, -1) || Edge(0, -1) || Edge(-1, 0)) c.Px[x, y] = PixelArt.Mix(p, white, 0.16f);
                    else if (Edge(2, 2)) c.Px[x, y] = PixelArt.Mul(p, 0.93f);
                    else if (Edge(-2, -2)) c.Px[x, y] = PixelArt.Mix(p, white, 0.07f);
                }
            Soften(c, src);
        }

        /// <summary>2倍で太くなった ふちどりの線の、外がわ半分を半透明にして、細くなめらかに見せる</summary>
        private static void Soften(PixelArt.Canvas c, Color32[,] src)
        {
            int w = c.W, h = c.H;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = src[x, y];
                    if (p.a == 0 || Luma(p) >= 45) continue;
                    bool outside = Clear(At(src, w, h, x - 1, y)) || Clear(At(src, w, h, x + 1, y)) || Clear(At(src, w, h, x, y - 1)) || Clear(At(src, w, h, x, y + 1));
                    // 外がわに面していて、内がわにも同じ線が続いている（＝太い線の外がわ）ときだけ
                    bool thick = Luma(At(src, w, h, x - 1, y)) < 45 && !Clear(At(src, w, h, x - 1, y)) || Luma(At(src, w, h, x + 1, y)) < 45 && !Clear(At(src, w, h, x + 1, y))
                                 || Luma(At(src, w, h, x, y - 1)) < 45 && !Clear(At(src, w, h, x, y - 1)) || Luma(At(src, w, h, x, y + 1)) < 45 && !Clear(At(src, w, h, x, y + 1));
                    if (outside && thick) c.Px[x, y] = new Color32(p.r, p.g, p.b, 150);
                }
        }

        /// <summary>つぶつぶの質感：広い面の中だけ、ところどころ ほんの少し明るく・暗く</summary>
        private static void Grain(PixelArt.Canvas c)
        {
            var src = (Color32[,])c.Px.Clone();
            int w = c.W, h = c.H;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = src[x, y];
                    if (p.a == 0) continue;
                    if (!Same(p, At(src, w, h, x - 1, y)) || !Same(p, At(src, w, h, x + 1, y)) ||
                        !Same(p, At(src, w, h, x, y - 1)) || !Same(p, At(src, w, h, x, y + 1))) continue;
                    int hash = ((x * 73856093) ^ (y * 19349663)) & 15;
                    if (hash == 0) c.Px[x, y] = PixelArt.Mul(p, 0.93f);
                    else if (hash == 1) c.Px[x, y] = PixelArt.Add(p, 7);
                }
        }
    }
}
