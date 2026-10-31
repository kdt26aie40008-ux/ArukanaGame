using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>プレイヤーが歩けるマップ（町・遺跡のエリア）</summary>
    public interface IWalkMap
    {
        /// <summary>その場所（世界の座標）を歩けるか</summary>
        bool IsWalkable(Vector2 world);
    }

    /// <summary>マップの上で道を探す（タップで移動するとき用）</summary>
    public static class WalkPath
    {
        /// <summary>from から to まで、歩けるマスを通る道。なければ null</summary>
        public static List<Vector2> Find(IWalkMap map, Vector2 from, Vector2 to)
        {
            var start = new Vector2Int(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y));
            var goal = new Vector2Int(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y));
            if (!map.IsWalkable(goal)) return null;
            var prev = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (p == goal) break;
                foreach (var d in dirs)
                {
                    var n = p + d;
                    if (prev.ContainsKey(n) || !map.IsWalkable(n)) continue;
                    prev[n] = p;
                    queue.Enqueue(n);
                }
            }
            if (!prev.ContainsKey(goal)) return null;
            var path = new List<Vector2>();
            for (var c = goal; c != start; c = prev[c]) path.Add(c);
            path.Add(start); // まず今いるマスのまん中へ（かべの角をすりぬけないように）
            path.Reverse();
            return path;
        }

        /// <summary>at のとなり（上下左右）までの、いちばん近い道</summary>
        public static List<Vector2> NextTo(IWalkMap map, Vector2 from, Vector2 at)
        {
            List<Vector2> best = null;
            foreach (var d in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                var route = Find(map, from, at + d);
                if (route != null && (best == null || route.Count < best.Count)) best = route;
            }
            return best;
        }
    }
}
