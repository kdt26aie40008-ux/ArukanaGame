using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>クエストのマス目（上から見たエリア）とゴーレムを描く。見た目はクエストのテーマで変わる</summary>
    public static class GridView
    {
        private static readonly string[] Arrows = { "▶", "▲", "◀", "▼" };

        public static void Draw(Rect r, GridMap map, P golem, int dir, bool done, bool failure, Level level)
        {
            var th = level.Theme;
            int ch = level.Chapter.Number;
            var water = SpriteFactory.TextureOf(SpriteFactory.WaterOf(ch));
            var wall = SpriteFactory.TextureOf(SpriteFactory.WallOf(ch));
            // 穴のクエストは橋（板）の道、それ以外はエリアの床
            var floor = SpriteFactory.TextureOf(th.Obstacle == ObstacleKind.Hole ? SpriteFactory.Bridge : SpriteFactory.FloorOf(ch));
            var obstacle = SpriteFactory.TextureOf(QuestArt.Obstacle(th.Obstacle));
            var item = th.HasItems ? SpriteFactory.TextureOf(QuestArt.Item(th.Item)) : null;
            var goal = SpriteFactory.TextureOf(QuestArt.Goal(th.Goal, done));

            UIKit.Fill(r, new Color(0.06f, 0.06f, 0.09f));
            float tile = Mathf.Floor(Mathf.Min(r.width / map.W, r.height / map.H));
            float ox = r.x + (r.width - tile * map.W) * 0.5f;
            float oy = r.y + (r.height - tile * map.H) * 0.5f;

            for (int y = 0; y < map.H; y++)
            {
                for (int x = 0; x < map.W; x++)
                {
                    var p = new P(x, y);
                    var t = TileRect(ox, oy, tile, map, p);
                    var cell = map.Get(p);
                    if (cell == Cell.Water)
                    {
                        // 穴のクエストは川、それ以外はかべ
                        UIKit.Texture(t, th.Obstacle == ObstacleKind.Hole ? water : wall);
                        continue;
                    }

                    if (cell == Cell.Obstacle && th.Obstacle == ObstacleKind.Hole)
                    {
                        UIKit.Texture(t, obstacle); // こわれた橋
                    }
                    else
                    {
                        UIKit.Texture(t, floor);
                        // 広い部屋の床は、道より少し暗くする
                        if (cell == Cell.Room) UIKit.Fill(t, new Color(0f, 0f, 0f, 0.3f));
                        if (cell == Cell.Obstacle) UIKit.Texture(t, obstacle);
                    }
                    // 看板（数の書かれた石）
                    int sign = map.SignAt(p);
                    if (sign > 0)
                    {
                        var sr = Inset(t, tile * 0.18f);
                        UIKit.Fill(sr, new Color(0.35f, 0.33f, 0.38f));
                        UIKit.Fill(Inset(sr, 2), new Color(0.82f, 0.80f, 0.74f));
                        UIKit.Label(sr, UIKit.Colorize(sign.ToString(), new Color(0.15f, 0.12f, 0.1f)), UIKit.SmallCenter);
                    }
                    if (cell == Cell.Goal)
                    {
                        if (done) UIKit.Fill(Inset(t, -tile * 0.1f), new Color(1f, 0.9f, 0.3f, 0.35f));
                        UIKit.Texture(Inset(t, tile * 0.06f), goal);
                    }
                    if (item != null && map.HasItem(p))
                        UIKit.Texture(Inset(t, tile * 0.2f), item);
                    if (p == map.Start && p != golem)
                        UIKit.Fill(new Rect(t.x + tile * 0.4f, t.y + tile * 0.4f, tile * 0.2f, tile * 0.2f), new Color(1f, 1f, 1f, 0.35f));
                }
            }

            // 1マスずつの区切り線
            var line = new Color(0f, 0f, 0f, 0.45f);
            for (int x = 0; x <= map.W; x++) UIKit.Fill(new Rect(ox + x * tile - 0.75f, oy, 1.5f, tile * map.H), line);
            for (int y = 0; y <= map.H; y++) UIKit.Fill(new Rect(ox, oy + y * tile - 0.75f, tile * map.W, 1.5f), line);

            // ゴーレムと向き
            var g = TileRect(ox, oy, tile, map, golem);
            if (failure) UIKit.Fill(Inset(g, -2), new Color(1f, 0.3f, 0.3f, 0.55f));
            UIKit.Texture(Inset(g, tile * 0.1f), SpriteFactory.TextureOf(SpriteFactory.Golem));
            var a = new Rect(g.x + GridMap.Dirs[dir].X * tile * 0.42f, g.y - GridMap.Dirs[dir].Y * tile * 0.42f, tile, tile);
            UIKit.Label(a, UIKit.Colorize(Arrows[dir], new Color(1f, 0.92f, 0.3f)), UIKit.SmallCenter);
        }

        private static Rect TileRect(float ox, float oy, float tile, GridMap map, P p) =>
            new Rect(ox + p.X * tile, oy + (map.H - 1 - p.Y) * tile, tile, tile);

        private static Rect Inset(Rect r, float d) => new Rect(r.x + d, r.y + d, r.width - d * 2, r.height - d * 2);
    }
}
