using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>
    /// 遺跡のエリア（クエストの依頼で行く場所）の地図。ステージごとに形が変わる（Unity の機能は使っていません）。
    ///
    /// ・下の出口から入る。手前にゴーレムとクエストのゴール（光る装置）がある
    /// ・川にかかる橋がこわれている。クエストを終えると、ゴーレムが橋を直して向こう岸へ行ける
    /// ・宝箱・薬草・看板・草むら（アルカナが出る）がある。向こう岸には大きな宝箱
    ///
    /// 文字：
    ///  # かべ  . 道  , 草（かざり）  w 草むら  ~ 川  = 橋（直るまで通れない）  T 木  R 岩
    ///  E 出口  G ゴーレム  g クエストのゴール  C 宝箱  K 大きな宝箱  h 薬草  S 看板
    ///  o 回復の泉（オアシス・温泉）  x キラキラ光る地面（歩ける。調べると何か見つかる）  v 旅人（クイズを出す）
    /// </summary>
    public class AreaLayout
    {
        public const int W = 26, H = 18;

        public int Width = W, Height = H;   // 遺跡は W x H。宿舎の中は小さい
        public bool IsInn;                  // 宿舎の中（Inn() で作ったもの）
        public char[,] Grid = new char[W, H];
        public int ExitX, ExitY;
        public int GolemX, GolemY;
        public int BridgeX;                 // 橋の左の列（2列）
        public readonly List<(int x, int y)> Chests = new List<(int, int)>();  // 0〜1 手前の宝箱、最後が大きな宝箱
        public readonly List<(int x, int y)> Herbs = new List<(int, int)>();
        public int SignX, SignY;
        public int SpringX = -1, SpringY = -1;      // 回復の泉
        public int TravelerX = -1, TravelerY = -1;  // クイズを出す旅人
        public readonly List<(int x, int y)> DigSpots = new List<(int, int)>(); // キラキラ光る地面（手前・向こう岸に1つずつ）

        public char At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : Grid[x, y];

        /// <summary>そのマスを歩けるか（bridgeFixed: 橋が直っているか）</summary>
        public static bool Walkable(char c, bool bridgeFixed)
        {
            switch (c)
            {
                case '.': case ',': case 'w': case 'E': case 'h': case 'r': case 'x': return true;
                case '=': return bridgeFixed;
                default: return false;
            }
        }

        // ------------------------------------------------------------------
        // 宿舎の中（決まった形の部屋）
        //  v 窓のあるかべ  f だんろ  b ベッド  m 鏡  d クローゼット  p 植木ばち  r じゅうたん（歩ける）
        //  k 記録の本の机  t テーブル  c カウンター  n 宿舎のおかみ  E 出口
        // ------------------------------------------------------------------

        private static readonly string[] InnRows =
        {
            "#v###v####v###v#",
            "#f.b.b.b...m.dp#",
            "#..............#",
            "#..rrrrrrrr....#",
            "#..rrrrrrrr..kt#",
            "#..rrrrrrrr....#",
            "#..............#",
            "#ccccn.........#",
            "#p.............#",
            "#######E########",
        };

        public static AreaLayout Inn()
        {
            var a = new AreaLayout { Width = InnRows[0].Length, Height = InnRows.Length, IsInn = true };
            a.Grid = new char[a.Width, a.Height];
            for (int y = 0; y < a.Height; y++)
                for (int x = 0; x < a.Width; x++)
                {
                    char c = InnRows[y][x];
                    a.Grid[x, y] = c;
                    if (c == 'E') { a.ExitX = x; a.ExitY = y; }
                }
            return a;
        }

        public static AreaLayout Generate(int stage)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                var a = TryGenerate(stage, new Random(stage * 977 + attempt * 31 + 13));
                if (a != null) return a;
            }
            return TryGenerate(stage, null); // ここには来ない想定（全ステージ確認ずみ）
        }

        private static AreaLayout TryGenerate(int stage, Random rng)
        {
            bool plain = rng == null; // かざりなしの、いちばん簡単な形
            if (rng == null) rng = new Random(stage);
            var a = new AreaLayout();
            var g = a.Grid;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    g[x, y] = x == 0 || y == 0 || x == W - 1 || y == H - 1 ? '#' : '.';

            // 川（よこに流れて、少しくねくねする）。上の列 = riverTop[x]
            var riverTop = new int[W];
            int r = rng.Next(5, 8);
            for (int x = 1; x < W - 1; x++)
            {
                if (!plain && rng.Next(3) == 0) r = Math.Max(4, Math.Min(8, r + rng.Next(-1, 2)));
                riverTop[x] = r;
                g[x, r] = '~';
                g[x, r + 1] = '~';
            }

            // 橋（2列）
            a.BridgeX = rng.Next(4, W - 6);
            // 橋の2列は 川の高さを そろえる（ずれると 橋の絵と 歩ける所が 合わなくなる）
            int rb = riverTop[a.BridgeX], bx2 = a.BridgeX + 1, side = a.BridgeX + 2;
            g[bx2, riverTop[bx2]] = '.';
            g[bx2, riverTop[bx2] + 1] = '.';
            g[bx2, rb] = '~';
            g[bx2, rb + 1] = '~';
            riverTop[bx2] = rb;
            // となりの列と 川が はなれたら、間を 水でつなぐ
            if (riverTop[side] > rb + 1) { g[side, rb + 1] = '~'; riverTop[side] = rb + 1; }
            if (riverTop[side] + 1 < rb) g[side, rb] = '~';
            for (int x = a.BridgeX; x <= a.BridgeX + 1; x++)
                for (int y = 1; y < H - 1; y++)
                    if (g[x, y] == '~') g[x, y] = '=';
            int below = Math.Max(riverTop[a.BridgeX], riverTop[a.BridgeX + 1]) + 2; // 橋をわたった手前の行

            // ゴーレムとゴール（橋の手前の横）
            bool left = a.BridgeX > W / 2;
            a.GolemX = left ? a.BridgeX - 2 : a.BridgeX + 3;
            a.GolemY = below + 1;
            if (a.GolemY >= H - 3) return null;
            g[a.GolemX, a.GolemY] = 'G';
            int goalX = left ? a.GolemX - 1 : a.GolemX + 1;
            g[goalX, a.GolemY] = 'g';

            // 出口（下）と看板
            a.ExitX = rng.Next(3, W - 3);
            a.ExitY = H - 2;
            g[a.ExitX, a.ExitY] = 'E';
            a.SignX = a.ExitX + (a.ExitX < W / 2 ? 2 : -2);
            a.SignY = H - 3;
            g[a.SignX, a.SignY] = 'S';

            // 予約のマス（この上には何も置かない）：ゴーレムのまわり・橋の前後・出口の前
            var reserved = new HashSet<(int, int)>();
            void Reserve(int cx, int cy, int rad)
            {
                for (int y = cy - rad; y <= cy + rad; y++)
                    for (int x = cx - rad; x <= cx + rad; x++) reserved.Add((x, y));
            }
            Reserve(a.GolemX, a.GolemY, 1);
            Reserve(goalX, a.GolemY, 1);
            Reserve(a.BridgeX, below, 1);
            Reserve(a.BridgeX + 1, below, 1);
            Reserve(a.BridgeX, riverTop[a.BridgeX] - 1, 1);
            Reserve(a.BridgeX + 1, riverTop[a.BridgeX + 1] - 1, 1);
            Reserve(a.ExitX, a.ExitY, 1);
            Reserve(a.SignX, a.SignY, 1);

            bool Free(int x, int y) => x > 0 && y > 0 && x < W - 1 && y < H - 1 && g[x, y] == '.' && !reserved.Contains((x, y));
            bool NearSide(int x, int y) => y >= riverTop[Math.Max(1, Math.Min(W - 2, x))] + 2;

            if (!plain)
            {
                // 草むら：手前に2〜3つ、向こう岸に1つ
                int patches = rng.Next(2, 4);
                for (int i = 0; i < patches + 1; i++)
                {
                    int pw = rng.Next(3, 7), ph = rng.Next(2, 4);
                    int px = rng.Next(1, W - pw);
                    int py = i < patches ? rng.Next(below + 1, H - 2 - ph + 1) : rng.Next(1, Math.Max(2, riverTop[px] - ph));
                    for (int y = py; y < py + ph; y++)
                        for (int x = px; x < px + pw; x++)
                            if (Free(x, y) && (i < patches) == NearSide(x, y)) g[x, y] = 'w';
                }
                // かざりの草
                for (int i = 0; i < 12; i++)
                {
                    int x = rng.Next(1, W - 1), y = rng.Next(1, H - 1);
                    if (Free(x, y)) g[x, y] = ',';
                }
                // 木と岩（道をふさぎすぎないように、あとで確かめる）
                int deco = rng.Next(10, 17);
                for (int i = 0; i < deco; i++)
                {
                    int x = rng.Next(1, W - 1), y = rng.Next(1, H - 1);
                    if (Free(x, y)) g[x, y] = rng.Next(3) == 0 ? 'R' : 'T';
                }
            }

            // 宝箱（手前に1〜2つ）、大きな宝箱（向こう岸）、薬草
            (int, int) Place(char c, bool nearSide)
            {
                for (int i = 0; i < 200; i++)
                {
                    int x = rng.Next(1, W - 1), y = rng.Next(1, H - 1);
                    if ((Free(x, y) || g[x, y] == ',' && !reserved.Contains((x, y))) && NearSide(x, y) == nearSide)
                    {
                        g[x, y] = c;
                        reserved.Add((x, y));
                        return (x, y);
                    }
                }
                return (-1, -1);
            }
            int chests = plain ? 1 : rng.Next(1, 3);
            for (int i = 0; i < chests; i++) a.Chests.Add(Place('C', true));
            a.Chests.Add(Place('K', false));
            int herbs = plain ? 1 : rng.Next(1, 3);
            for (int i = 0; i < herbs; i++) a.Herbs.Add(Place('h', i == 0));
            // 回復の泉・旅人（手前）、キラキラ光る地面（手前と向こう岸に1つずつ）
            (a.SpringX, a.SpringY) = Place('o', true);
            (a.TravelerX, a.TravelerY) = Place('v', true);
            a.DigSpots.Add(Place('x', true));
            a.DigSpots.Add(Place('x', false));
            foreach (var c in a.Chests) if (c.Item1 < 0) return null;
            foreach (var h in a.Herbs) if (h.Item1 < 0) return null;
            foreach (var d in a.DigSpots) if (d.Item1 < 0) return null;
            if (a.SpringX < 0 || a.TravelerX < 0) return null;

            return a.Check() ? a : null;
        }

        /// <summary>出口から、ゴーレム・宝箱・薬草・看板・（橋が直れば）向こう岸の全部に行けるか</summary>
        private bool Check()
        {
            var seen = Reach(true);
            bool NextToSeen(int x, int y) =>
                seen[Math.Max(0, x - 1), y] || seen[Math.Min(W - 1, x + 1), y] || seen[x, Math.Max(0, y - 1)] || seen[x, Math.Min(H - 1, y + 1)];
            if (!NextToSeen(GolemX, GolemY) || !NextToSeen(SignX, SignY)) return false;
            foreach (var (x, y) in Chests) if (!NextToSeen(x, y)) return false;
            foreach (var (x, y) in Herbs) if (!seen[x, y]) return false;
            foreach (var (x, y) in DigSpots) if (!seen[x, y]) return false;

            // 橋が直る前：向こう岸に行けないこと・手前の宝箱とゴーレムには行けること
            var before = Reach(false);
            bool NextToBefore(int x, int y) =>
                before[Math.Max(0, x - 1), y] || before[Math.Min(W - 1, x + 1), y] || before[x, Math.Max(0, y - 1)] || before[x, Math.Min(H - 1, y + 1)];
            if (!NextToBefore(GolemX, GolemY)) return false;
            if (NextToBefore(Chests[Chests.Count - 1].x, Chests[Chests.Count - 1].y)) return false;
            for (int i = 0; i < Chests.Count - 1; i++) if (!NextToBefore(Chests[i].x, Chests[i].y)) return false;
            // 泉と旅人とキラキラ（手前）は、クエストの前から行ける
            if (!NextToBefore(SpringX, SpringY) || !NextToBefore(TravelerX, TravelerY) || !before[DigSpots[0].x, DigSpots[0].y]) return false;
            return before[Herbs[0].x, Herbs[0].y];
        }

        private bool[,] Reach(bool bridgeFixed)
        {
            var seen = new bool[W, H];
            var q = new Queue<(int, int)>();
            q.Enqueue((ExitX, ExitY));
            seen[ExitX, ExitY] = true;
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H || seen[nx, ny] || !Walkable(Grid[nx, ny], bridgeFixed)) continue;
                    seen[nx, ny] = true;
                    q.Enqueue((nx, ny));
                }
            }
            return seen;
        }
    }
}
