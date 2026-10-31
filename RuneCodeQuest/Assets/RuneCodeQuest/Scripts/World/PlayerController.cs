using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>プレイヤーの移動（WASD / 矢印キー）。壁や川にはぶつかって止まる</summary>
    public class PlayerController : MonoBehaviour
    {
        public float Speed = 4f;

        /// <summary>今の場所での速さ（町の階段・坂・はしごの上では 少しゆっくり）</summary>
        private float SpeedHere => map is TownMap town && town.IsStairs(transform.position) ? Speed * 0.7f : Speed;
        public bool CanMove = true;

        private const float HalfSize = 0.3f; // 当たり判定の半分の大きさ
        private IWalkMap map;
        private SpriteRenderer sr;

        // タップ（クリック）で移動するときの道
        private List<Vector2> path;
        private System.Action onArrive;

        /// <summary>道にそって歩く。着いたら onArrive を呼ぶ（キーボードで動かすと取り消し）</summary>
        public void WalkTo(List<Vector2> newPath, System.Action arrive = null)
        {
            path = newPath != null && newPath.Count > 0 ? newPath : null;
            onArrive = arrive;
            if (path == null) arrive?.Invoke();
        }

        public bool IsWalking => path != null;

        public void StopWalking()
        {
            path = null;
            onArrive = null;
        }

        public void Init(IWalkMap map)
        {
            this.map = map;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Player;
            sr.sortingOrder = 10;
        }

        /// <summary>見た目を、今のキャラクリにそろえる（宿舎の身じたくのあと）</summary>
        public void RefreshLook() => sr.sprite = SpriteFactory.Player;

        /// <summary>歩くマップを変える（町 ⇔ 遺跡のエリア）</summary>
        public void SetMap(IWalkMap newMap, Vector2 position)
        {
            map = newMap;
            StopWalking();
            transform.position = position;
        }

        private void Update()
        {
            if (!CanMove || map == null) return;

            Vector2 dir = KeyInput.MoveAxis();
            if (dir != Vector2.zero) path = null; // キーボードを使ったら、タップの移動はやめる
            if (dir == Vector2.zero)
            {
                FollowPath();
                return;
            }
            if (dir.x != 0) sr.flipX = dir.x < 0;
            Look(dir);

            Vector2 pos = transform.position;
            Vector2 delta = dir * SpeedHere * Time.deltaTime;

            // 横と縦を別々に動かすと、壁にそってすべるように動ける
            Vector2 tryX = pos + new Vector2(delta.x, 0f);
            if (CanStand(tryX)) pos = tryX;
            Vector2 tryY = pos + new Vector2(0f, delta.y);
            if (CanStand(tryY)) pos = tryY;

            transform.position = pos;
        }

        private void FollowPath()
        {
            if (path == null) return;
            Vector2 pos = transform.position;
            Vector2 next = path[0];
            if (next.x != pos.x) sr.flipX = next.x < pos.x;
            Look(next - pos);
            pos = Vector2.MoveTowards(pos, next, SpeedHere * Time.deltaTime);
            transform.position = pos;
            if ((pos - next).sqrMagnitude > 0.0001f) return;
            path.RemoveAt(0);
            if (path.Count > 0) return;
            path = null;
            var done = onArrive;
            onArrive = null;
            done?.Invoke();
        }

        /// <summary>向いている方向（世界の座標で。上が北）。地図に矢印で出す</summary>
        public Vector2Int Facing { get; private set; } = Vector2Int.down;

        private void Look(Vector2 d)
        {
            if (d.sqrMagnitude < 0.0001f) return;
            Facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                ? (d.x > 0 ? Vector2Int.right : Vector2Int.left)
                : (d.y > 0 ? Vector2Int.up : Vector2Int.down);
        }

        private bool CanStand(Vector2 p)
        {
            return map.IsWalkable(p + new Vector2(-HalfSize, -HalfSize))
                && map.IsWalkable(p + new Vector2(HalfSize, -HalfSize))
                && map.IsWalkable(p + new Vector2(-HalfSize, HalfSize))
                && map.IsWalkable(p + new Vector2(HalfSize, HalfSize));
        }

        /// <summary>押し戻す（敵から逃げたときなど）。歩けない場所には動かない</summary>
        public void PushAway(Vector2 from, float distance)
        {
            Vector2 pos = transform.position;
            Vector2 dir = (pos - from).sqrMagnitude > 0.0001f ? (pos - from).normalized : Vector2.left;
            for (float d = distance; d > 0.05f; d -= 0.1f)
            {
                Vector2 target = pos + dir * d;
                if (CanStand(target))
                {
                    transform.position = target;
                    return;
                }
            }
        }
    }
}
