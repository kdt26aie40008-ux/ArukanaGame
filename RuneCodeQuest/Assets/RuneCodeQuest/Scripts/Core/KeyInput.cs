using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// キーボード入力をまとめて扱うクラス。
    /// IMGUI のキーイベントから押下状態を記録するので、
    /// 「旧 Input Manager」「新 Input System」どちらの設定でも動きます。
    /// GameManager.OnGUI から ProcessEvent、LateUpdate から EndFrame を呼びます。
    /// </summary>
    public static class KeyInput
    {
        private static readonly HashSet<KeyCode> held = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> pressed = new HashSet<KeyCode>();

        public static void ProcessEvent(Event e)
        {
            if (e == null || e.keyCode == KeyCode.None) return;
            if (e.type == EventType.KeyDown)
            {
                if (held.Add(e.keyCode)) pressed.Add(e.keyCode);
            }
            else if (e.type == EventType.KeyUp)
            {
                held.Remove(e.keyCode);
            }
        }

        /// <summary>押しっぱなしの間 true</summary>
        public static bool IsHeld(KeyCode key) => held.Contains(key);

        /// <summary>押した瞬間のフレームだけ true</summary>
        public static bool WasPressed(KeyCode key) => pressed.Contains(key);

        public static void EndFrame() => pressed.Clear();

        public static void Clear()
        {
            held.Clear();
            pressed.Clear();
        }

        /// <summary>WASD / 矢印キーから移動方向を作る</summary>
        public static Vector2 MoveAxis()
        {
            float x = 0f, y = 0f;
            if (IsHeld(KeyCode.A) || IsHeld(KeyCode.LeftArrow)) x -= 1f;
            if (IsHeld(KeyCode.D) || IsHeld(KeyCode.RightArrow)) x += 1f;
            if (IsHeld(KeyCode.S) || IsHeld(KeyCode.DownArrow)) y -= 1f;
            if (IsHeld(KeyCode.W) || IsHeld(KeyCode.UpArrow)) y += 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool InteractPressed() =>
            WasPressed(KeyCode.E) || WasPressed(KeyCode.Space) || WasPressed(KeyCode.Return);
    }
}
