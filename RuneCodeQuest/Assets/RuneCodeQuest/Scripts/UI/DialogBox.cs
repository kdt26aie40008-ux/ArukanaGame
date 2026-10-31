using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 会話の窓（画面の下に出る）。セリフは1文字ずつ出てきて、そのあいだ「ポッポッ」としゃべる音が鳴る。
    /// Enter・スペース・E・クリックで、文字を全部出す → もう一度で次へ。
    /// セリフの {name} は、プレイヤーのなまえに置きかわる。
    /// </summary>
    public class DialogBox
    {
        private const float CharsPerSecond = 40f;

        private readonly Queue<(string speaker, string text, System.Action after)> lines = new Queue<(string, string, System.Action)>();
        private (string speaker, string text, System.Action after)? current;
        private float shown;      // 出ている文字の数
        private int lastVoice;    // さいごに音を鳴らした文字の数

        public bool Active => current != null;

        /// <summary>セリフを足す。最後のセリフを読み終わったら after を呼ぶ</summary>
        public void Show(string speaker, string[] texts, System.Action after = null)
        {
            for (int i = 0; i < texts.Length; i++)
                lines.Enqueue((speaker, texts[i], i == texts.Length - 1 ? after : null));
            if (current == null) Next();
        }

        public void Show(string speaker, string text, System.Action after = null) => Show(speaker, new[] { text }, after);

        private void Next()
        {
            var done = current?.after;
            current = lines.Count > 0 ? lines.Dequeue() : ((string, string, System.Action)?)null;
            shown = 0f;
            lastVoice = 0;
            done?.Invoke();
        }

        public void Draw()
        {
            if (current == null) return;
            UIKit.Nav("dialog", false);
            var c = current.Value;
            string full = c.text.Replace("{name}", Progress.Look.Name);
            // 色などのタグがある文は、とちゅうで切るとこわれるので、はじめから全部出す
            if (full.IndexOf('<') >= 0) shown = full.Length;

            var e = Event.current;
            if ((e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space || e.keyCode == KeyCode.E))
                || e.type == EventType.MouseDown)
            {
                e.Use();
                KeyInput.Clear();
                if (shown < full.Length) shown = full.Length; // まだ出ている途中なら、全部出す
                else
                {
                    Next();
                    if (current == null) return;
                    c = current.Value;
                    full = c.text.Replace("{name}", Progress.Look.Name);
                }
            }

            // 1文字ずつ出す。2文字ごとに しゃべる音
            if (e.type == EventType.Repaint && shown < full.Length)
            {
                shown = Mathf.Min(full.Length, shown + Time.unscaledDeltaTime * CharsPerSecond);
                if ((int)shown - lastVoice >= 2)
                {
                    lastVoice = (int)shown;
                    SoundKit.Talk();
                }
            }

            var box = new Rect(60, 500, 1160, 170);
            UIKit.Panel(box);
            // 話している人の顔の絵（あれば 左に出して、文は その右）
            var face = PortraitOf(c.speaker);
            float left = 0f;
            if (face != null)
            {
                var frame = new Rect(box.x + 14, box.y - 74, 220, 230);
                UIKit.Fill(frame, UIKit.SubPanelColor);
                UIKit.Texture(new Rect(frame.x + 5, frame.y + 5, frame.width - 10, frame.height - 10), face);
                left = 236f;
            }
            if (!string.IsNullOrEmpty(c.speaker))
            {
                // 話している人・場所の名前は、ふきだしの上に出す（長い名前でも本文に重ならないように、幅を合わせて上にずらす）
                float w = Mathf.Clamp(c.speaker.Length * 28 + 48, 220, 760);
                var plate = new Rect(box.x + 16 + left, box.y - 54, w, 48);
                UIKit.Fill(plate, UIKit.SubPanelColor);
                UIKit.Label(new Rect(plate.x + 14, plate.y, plate.width - 20, plate.height), c.speaker, UIKit.Heading);
            }
            UIKit.Label(new Rect(box.x + 30 + left, box.y + 22, box.width - 60 - left, box.height - 54), full.Substring(0, Mathf.Min(full.Length, (int)shown)), UIKit.Explain);
            if (shown >= full.Length)
                UIKit.Label(new Rect(box.xMax - 330, box.yMax - 34, 320, 30), "▼ Enter・クリックで つぎへ", UIKit.SmallCenter);
        }

        /// <summary>
        /// 話している人の顔の絵（Resources/Art/Portrait/〈村〉_名前.png か 名前.png）。なければ null（文だけ）。
        /// 馬車の御者は 国ごとに ちがう人なので、村の名前つきの絵を さきに さがす
        /// </summary>
        private static Texture PortraitOf(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return null;
            var art = ArtOverrides.Get($"Art/Portrait/{TownData.Current.Name}_{speaker}") ?? ArtOverrides.Get($"Art/Portrait/{speaker}");
            return art != null ? art.texture : null;
        }
    }
}
