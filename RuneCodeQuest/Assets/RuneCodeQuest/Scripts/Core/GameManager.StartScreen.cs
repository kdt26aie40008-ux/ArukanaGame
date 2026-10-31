using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：起動したときのスタート画面。
    /// 大きな太文字のタイトルと「PRESS ENTER」。Enter・スペース・クリックでタイトルのメニューへ。
    /// </summary>
    public partial class GameManager
    {
        private GUIStyle startTitleStyle, startTitle2Style, startSubStyle, startPressStyle;

        // 画面の中をふわふわ飛ぶアルカナ（図鑑の最初の行から）
        private static readonly (int area, int index, float x, float y, float speed)[] StartMonsters =
        {
            (0, 0, 120, 520, 0.9f), (1, 2, 1090, 150, 1.2f), (2, 2, 200, 170, 1.0f), (3, 0, 1060, 500, 0.8f), (5, 2, 640, 610, 1.1f),
        };

        private void MakeStartStyles()
        {
            if (startTitleStyle != null) return;
            startTitleStyle = new GUIStyle(UIKit.Huge) { fontSize = 132, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            startTitle2Style = new GUIStyle(UIKit.Huge) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            startSubStyle = new GUIStyle(UIKit.Title) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            startPressStyle = new GUIStyle(UIKit.Title) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
        }

        /// <summary>文字にふちどりをつけて描く（ふちを少しずらして何回か描いてから、上に本体）</summary>
        private static void OutlinedLabel(Rect r, string text, GUIStyle style, Color main, Color edge, float width)
        {
            string e = UIKit.Colorize(text, edge);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) UIKit.Label(new Rect(r.x + dx * width, r.y + dy * width, r.width, r.height), e, style);
            UIKit.Label(r, UIKit.Colorize(text, main), style);
        }

        private void DrawStartScreen()
        {
            MakeStartStyles();
            float t = Time.time;
            UIKit.Nav("start", false);

            // 背景：夜空のグラデーションと、またたく星
            for (int i = 0; i < 12; i++)
            {
                float k = i / 11f;
                UIKit.Fill(new Rect(0, i * 60, UIKit.RefWidth, 61), Color.Lerp(new Color(0.05f, 0.04f, 0.16f), new Color(0.12f, 0.22f, 0.32f), k));
            }
            for (int i = 0; i < 40; i++)
            {
                float x = (i * 337) % 1280, y = (i * 191) % 420;
                float a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 1.5f + i));
                UIKit.Fill(new Rect(x, y, 3, 3), new Color(1f, 1f, 0.85f, a));
            }

            // ふわふわ飛ぶアルカナ
            foreach (var (area, index, x, y, speed) in StartMonsters)
            {
                var e = BattleData.EnemiesByRegion[area][index];
                float bob = Mathf.Sin(t * speed * 2f + x) * 14f, drift = Mathf.Sin(t * speed * 0.6f + y) * 30f;
                UIKit.Texture(new Rect(x - 48 + drift, y - 48 + bob, 96, 96), SpriteFactory.TextureOf(MonsterArt.Build(e)));
            }

            // タイトル（2段：大きな太文字の「アルカナ」と「と 忘れられた世界」。少しだけ上下にゆれる）
            float wave = Mathf.Sin(t * 2f) * 6f;
            var gold = new Color(1f, 0.85f, 0.25f);
            var edge = new Color(0.45f, 0.15f, 0.05f);
            var shadow = new Color(0f, 0f, 0f, 0.6f);
            OutlinedLabel(new Rect(0, 100 + wave + 8, UIKit.RefWidth, 160), QuestDatabase.GameTitleTop, startTitleStyle, shadow, shadow, 4f); // かげ
            OutlinedLabel(new Rect(0, 100 + wave, UIKit.RefWidth, 160), QuestDatabase.GameTitleTop, startTitleStyle, gold, edge, 4f);
            OutlinedLabel(new Rect(0, 252 + wave + 5, UIKit.RefWidth, 84), QuestDatabase.GameTitleBottom, startTitle2Style, shadow, shadow, 3f);
            OutlinedLabel(new Rect(0, 252 + wave, UIKit.RefWidth, 84), QuestDatabase.GameTitleBottom, startTitle2Style, new Color(0.85f, 0.92f, 1f), new Color(0.1f, 0.12f, 0.35f), 3f);
            OutlinedLabel(new Rect(0, 350, UIKit.RefWidth, 50), QuestDatabase.GameSubtitle, startSubStyle, new Color(0.75f, 0.95f, 1f), new Color(0.05f, 0.1f, 0.25f), 2f);

            // PRESS ENTER（点めつ）と、その下に少し小さく日本語
            float blink = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2.4f));
            OutlinedLabel(new Rect(0, 470, UIKit.RefWidth, 60), "PRESS ENTER", startPressStyle, new Color(1f, 1f, 1f, blink), new Color(0f, 0f, 0f, 0.8f * blink), 2f);
            UIKit.Label(new Rect(0, 528, UIKit.RefWidth, 40), UIKit.Colorize("Enter キー（またはクリック）でスタート", new Color(1f, 1f, 1f, 0.8f)), UIKit.Center);
            UIKit.Label(new Rect(0, 676, UIKit.RefWidth, 30), UIKit.Colorize("プログラミング × タイピング RPG", new Color(1f, 1f, 1f, 0.55f)), UIKit.SmallCenter);

            // Enter・スペース・クリックで、タイトルのメニューへ
            var ev = Event.current;
            bool go = (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter || ev.keyCode == KeyCode.Space))
                      || (ev.type == EventType.MouseDown && ev.button == 0);
            if (!go) return;
            ev.Use();
            KeyInput.Clear(); // 同じ Enter で、メニューのボタンまで押されないように
            SoundKit.Chime();
            titlePhase = TitlePhase.Menu;
        }
    }
}
