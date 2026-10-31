namespace RuneCodeQuest
{
    /// <summary>マスの種類</summary>
    public enum Cell
    {
        Water, // 川（通れない）
        Floor, // 道
        Obstacle, // じゃまもの（まほうで消すと通れる）
        Goal,  // ゴール（灯台）
        Room,  // 広い部屋の床（歩けるが、道ではない）
    }

    /// <summary>マスの位置（x は右、y は上が＋）</summary>
    public struct P
    {
        public int X, Y;
        public P(int x, int y) { X = x; Y = y; }
        public static P operator +(P a, P b) => new P(a.X + b.X, a.Y + b.Y);
        public static bool operator ==(P a, P b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(P a, P b) => !(a == b);
        public override bool Equals(object o) => o is P p && p == this;
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>
    /// クエストの舞台（上から見たマス目）。
    /// 向き: 0=右(東) 1=上(北) 2=左(西) 3=下(南)
    /// </summary>
    public class GridMap
    {
        public int W, H;
        public Cell[] Cells;
        public bool[] Items;   // アイテムが置いてあるマス
        public int[] Signs;    // 看板の数（0 なら看板なし）
        public P Start;
        public int StartDir;
        public P Goal;

        public static readonly P[] Dirs = { new P(1, 0), new P(0, 1), new P(-1, 0), new P(0, -1) };

        public GridMap(int w, int h)
        {
            W = w;
            H = h;
            Cells = new Cell[w * h];
            Items = new bool[w * h];
            Signs = new int[w * h];
        }

        public bool Inside(P p) => p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H;

        public Cell Get(P p) => Inside(p) ? Cells[p.Y * W + p.X] : Cell.Water;

        public void Set(P p, Cell c)
        {
            if (Inside(p)) Cells[p.Y * W + p.X] = c;
        }

        public GridMap Clone()
        {
            var g = (GridMap)MemberwiseClone();
            g.Cells = (Cell[])Cells.Clone();
            g.Items = (bool[])Items.Clone();
            g.Signs = (int[])Signs.Clone();
            return g;
        }

        public bool HasItem(P p) => Inside(p) && Items[p.Y * W + p.X];

        public int SignAt(P p) => Inside(p) ? Signs[p.Y * W + p.X] : 0;

        public void SetSign(P p, int value)
        {
            if (Inside(p)) Signs[p.Y * W + p.X] = value;
        }

        public void SetItem(P p, bool on)
        {
            if (Inside(p)) Items[p.Y * W + p.X] = on;
        }

        public int CountObstacles()
        {
            int c = 0;
            foreach (var cell in Cells) if (cell == Cell.Obstacle) c++;
            return c;
        }

        public int CountItems()
        {
            int c = 0;
            foreach (var i in Items) if (i) c++;
            return c;
        }
    }
}
