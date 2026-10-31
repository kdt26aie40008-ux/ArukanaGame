using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// クエスト画面の続き：説明ページを 見やすく かく。
    /// ・長い文は だんらくごとに カードに分けて ならべる（ExplainCards）
    /// ・「目的と星」のページ（DrawGoalPage）
    /// ・「使える命令と 英語のことば」のページ（DrawToolsPage）：命令は ブロックの色の チップ、英語は 表
    /// </summary>
    public partial class QuestScreen
    {
        private Vector2 cardScroll, toolScroll, wordScroll;
        private string cardScrollKey;

        // カードの 左の おびの色（じゅんばんに かわる）
        private static readonly Color[] CardBars =
        {
            new Color(0.30f, 0.59f, 1.00f), new Color(1.00f, 0.67f, 0.10f), new Color(0.45f, 0.80f, 0.50f), new Color(0.60f, 0.40f, 1.00f),
        };

        /// <summary>長い文を、だんらく（空いた行）ごとに カードに分けて かく。入りきらないときは スクロール</summary>
        private void ExplainCards(Rect r, string text)
        {
            var paras = new List<string>();
            foreach (var part in (text ?? "").Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries))
                if (part.Trim().Length > 0) paras.Add(part.Trim('\n'));
            if (paras.Count <= 1) { ExplainText(r, text); return; }

            string key = phase + ":" + page;
            if (key != cardScrollKey) { cardScroll = Vector2.zero; cardScrollKey = key; }
            float w = r.width - 24, inner = w - 40;
            var heights = new float[paras.Count];
            float total = 0;
            for (int i = 0; i < paras.Count; i++) { heights[i] = UIKit.CalcHeight(paras[i], UIKit.Body, inner) + 22; total += heights[i] + 10; }
            bool scroll = total > r.height;
            if (scroll) cardScroll = UIKit.BeginScrollView(r, cardScroll, new Rect(0, 0, w, total));
            float y = scroll ? 0 : r.y, x = scroll ? 0 : r.x;
            for (int i = 0; i < paras.Count; i++)
            {
                var c = new Rect(x, y, w, heights[i]);
                UIKit.Fill(c, new Color(1f, 1f, 1f, 0.06f));
                UIKit.Fill(new Rect(c.x, c.y, 6, c.height), CardBars[i % CardBars.Length]);
                // 1つ目の だんらくは 大きめ（いちばん だいじな ところ）
                UIKit.Label(new Rect(c.x + 22, c.y + 10, inner, c.height - 14), paras[i], i == 0 ? UIKit.Explain : UIKit.Body);
                y += heights[i] + 10;
            }
            if (scroll) UIKit.EndScrollView();
        }

        // ------------------------------------------------------------------
        // ページ1：クエストの目的と 星の条件
        // ------------------------------------------------------------------

        private void DrawGoalPage(Rect body)
        {
            float t = Time.time;
            // お話（左）
            var story = new Rect(body.x + 30, body.y + 70, 560, 196);
            UIKit.Fill(story, new Color(1f, 1f, 1f, 0.06f));
            UIKit.Fill(new Rect(story.x, story.y, story.width, 36), new Color(0.3f, 0.45f, 0.7f));
            UIKit.Label(new Rect(story.x + 14, story.y + 2, story.width - 20, 34), "エリア：" + Level.Theme.Area, UIKit.Body);
            ScrollLabel(new Rect(story.x + 14, story.y + 44, story.width - 24, story.height - 50), Level.Theme.Story, UIKit.Body, ref cardScroll);

            // 目標（右）：ゴールの絵が ふわふわ
            var goal = new Rect(body.x + 610, body.y + 70, 460, 196);
            UIKit.Fill(goal, new Color(1f, 0.85f, 0.3f, 0.12f));
            UIKit.Fill(new Rect(goal.x, goal.y, goal.width, 36), new Color(0.85f, 0.6f, 0.15f));
            UIKit.Label(new Rect(goal.x + 14, goal.y + 2, goal.width - 20, 34), "目標", UIKit.Body);
            float bob = 4f * Mathf.Sin(t * 2.5f);
            UIKit.Texture(new Rect(goal.x + 16, goal.y + 64 + bob, 90, 90), SpriteFactory.TextureOf(QuestArt.Goal(Level.Theme.Goal, false)));
            UIKit.Label(new Rect(goal.x + 118, goal.y + 46, goal.width - 128, goal.height - 52), Level.Goal, UIKit.Body);

            // 星の条件：3まいの カード
            UIKit.Label(new Rect(body.x + 30, body.y + 278, 600, 30), UIKit.Colorize("星の条件（できた条件ごとに 星が1こ）", new Color(1f, 0.85f, 0.2f)), UIKit.Body);
            int best = Progress.StarsOf(Level.Number);
            string record = Progress.StatusOf(Level.Number) == Progress.Passed ? "いままで：通過（星なし）"
                : Progress.StatusOf(Level.Number) == Progress.Cleared ? $"いままでの記録：{QuestStars.Mark(best)}" : "はじめての ちょうせん！";
            UIKit.Label(new Rect(body.xMax - 360, body.y + 280, 330, 30), record, UIKit.Small);
            var conds = QuestStars.Conditions(Level);
            var star = StarTexture();
            for (int i = 0; i < conds.Length; i++)
            {
                var c = new Rect(body.x + 30 + i * 350, body.y + 312, 340, 96);
                UIKit.Fill(c, new Color(1f, 1f, 1f, 0.06f));
                float s = 54f + 4f * Mathf.Sin(t * 3f + i);
                UIKit.TextureTinted(new Rect(c.x + 36 - s / 2f, c.center.y - s / 2f, s, s), star, new Color(1f, 0.82f, 0.2f));
                UIKit.Label(new Rect(c.x + 70, c.y + 8, c.width - 78, c.height - 12), conds[i], UIKit.Body);
            }

            // しばり（おまけの依頼）
            string limits = LevelFactory.UsageLimitText(Level);
            var bottom = new Rect(body.x + 30, body.y + 420, body.width - 60, 50);
            if (limits != null)
            {
                UIKit.Fill(bottom, new Color(0.9f, 0.3f, 0.3f, 0.2f));
                UIKit.Fill(new Rect(bottom.x, bottom.y, 6, bottom.height), UIKit.ErrorColor);
                UIKit.Label(new Rect(bottom.x + 18, bottom.y + 2, bottom.width - 24, bottom.height), UIKit.Colorize("しばり：", UIKit.WarnColor) + limits + "（こえて置くと 失敗）", UIKit.Small);
            }
            else UIKit.Label(bottom, "つぎのページで、使える命令と 英語のことばを たしかめよう →", UIKit.SmallCenter);
        }

        // ------------------------------------------------------------------
        // ページ2：使える命令（ブロックの色の チップ）と 英語ことば表
        // ------------------------------------------------------------------

        private static readonly Color SenseColor = new Color(0.36f, 0.69f, 0.84f); // スクラッチの「調べる」の色

        private void DrawToolsPage(Rect body)
        {
            // 左：使える命令
            var left = new Rect(body.x + 30, body.y + 66, 540, body.height - 110);
            UIKit.Label(new Rect(left.x, left.y, 300, 28), "使える命令", UIKit.Body);
            var chips = new List<(string label, string code, string desc, Color color)>();
            foreach (var c in Level.Commands)
            {
                var info = CommandInfo.Get(c);
                bool motion = c == CommandType.Forward || c == CommandType.TurnLeft || c == CommandType.TurnRight;
                chips.Add((info.BlockLabel, info.PythonCode, info.Description, motion ? ScratchEditor.MotionColor : ScratchEditor.MagicColor));
            }
            if (Level.Structures.Contains(NodeKind.Repeat)) chips.Add(("○回くり返す", "for i in range(3):", "中の命令を 決まった回数 くり返す", ScratchEditor.ControlColor));
            if (Level.Structures.Contains(NodeKind.If)) chips.Add((Level.AllowElse ? "もし／でなければ" : "もし〜なら", Level.AllowElse ? "if 〜: / else:" : "if 〜:", "調べて、はい の ときだけ 動く", ScratchEditor.ControlColor));
            if (Level.Structures.Contains(NodeKind.While)) chips.Add(("〜までくり返す", "while not 〜:", "そうなるまで ずっと くり返す", ScratchEditor.ControlColor));
            if (Level.AllowVariables)
            {
                chips.Add(("n を看板の数に", "n = read_sign()", "看板の数を 変数 n に入れる", ScratchEditor.VariableColor));
                chips.Add(("n を -1 ずつ", "n = n - 1", "n を 1 へらす", ScratchEditor.VariableColor));
                chips.Add(("n 回くり返す", "for i in range(n):", "n の数だけ くり返す", ScratchEditor.VariableColor));
            }
            if (Level.AllowFunctions)
            {
                chips.Add(("関数を作る", "def turn_corner():", "命令を まとめて 名前をつける", ScratchEditor.FunctionColor));
                chips.Add(("関数を使う", "turn_corner()", "まとめた命令を 1つで 動かす", ScratchEditor.FunctionColor));
            }
            foreach (var c in Level.Conditions)
            {
                var info = ConditionInfo.Get(c);
                chips.Add(("調べる：" + info.Label, info.PythonCode, info.Description, SenseColor));
            }
            const float chipH = 66f, chipGap = 8f;
            var view = new Rect(left.x, left.y + 32, left.width, left.height - 32);
            float content = chips.Count * (chipH + chipGap);
            bool scroll = content > view.height;
            if (scroll) toolScroll = UIKit.BeginScrollView(view, toolScroll, new Rect(0, 0, view.width - 20, content));
            float x0 = scroll ? 0 : view.x, y0 = scroll ? 0 : view.y, cwid = scroll ? view.width - 20 : view.width;
            for (int i = 0; i < chips.Count; i++)
            {
                var (label, code, desc, color) = chips[i];
                var r = new Rect(x0, y0 + i * (chipH + chipGap), cwid, chipH);
                // ブロックの形：左に 色のブロック、右に 説明
                var block = new Rect(r.x, r.y, 210, chipH);
                UIKit.Fill(block, color);
                UIKit.Fill(new Rect(block.x, block.yMax - 5, block.width, 5), color * 0.7f);
                UIKit.Label(new Rect(block.x + 10, block.y + 4, block.width - 14, 30), label, UIKit.BlockText);
                UIKit.Label(new Rect(block.x + 10, block.y + 34, block.width - 14, 26), UIKit.Colorize(code, new Color(1f, 1f, 1f, 0.9f)), ChipCode);
                UIKit.Fill(new Rect(block.xMax, r.y, r.width - block.width, chipH), new Color(1f, 1f, 1f, 0.05f));
                UIKit.Label(new Rect(block.xMax + 12, r.y + 4, r.width - block.width - 18, chipH - 6), desc, UIKit.Small);
            }
            if (scroll) UIKit.EndScrollView();

            // 右：英語ことば表
            var right = new Rect(body.x + 590, body.y + 66, body.width - 620, body.height - 110);
            var words = EnglishWords.ForLevel(Level);
            UIKit.Label(new Rect(right.x, right.y, right.width, 28), "このクエストの 英語のことば", UIKit.Body);
            var head = new Rect(right.x, right.y + 32, right.width, 34);
            UIKit.Fill(head, new Color(0.3f, 0.45f, 0.7f));
            float c1 = 130f, c2 = 120f;
            UIKit.Label(new Rect(head.x + 10, head.y + 4, c1, 28), "英語", UIKit.Small);
            UIKit.Label(new Rect(head.x + 10 + c1, head.y + 4, c2, 28), "読み方", UIKit.Small);
            UIKit.Label(new Rect(head.x + 10 + c1 + c2, head.y + 4, head.width - c1 - c2 - 10, 28), "いみ", UIKit.Small);
            const float rowH = 36f;
            var tv = new Rect(right.x, head.yMax, right.width, right.height - 66 - 30);
            bool ts = words.Count * rowH > tv.height;
            if (ts) wordScroll = UIKit.BeginScrollView(tv, wordScroll, new Rect(0, 0, tv.width - 20, words.Count * rowH));
            float tx = ts ? 0 : tv.x, ty = ts ? 0 : tv.y, tw = ts ? tv.width - 20 : tv.width;
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                var row = new Rect(tx, ty + i * rowH, tw, rowH);
                UIKit.Fill(row, i % 2 == 0 ? new Color(1f, 1f, 1f, 0.07f) : new Color(0f, 0f, 0f, 0.12f));
                UIKit.Label(new Rect(row.x + 10, row.y + 4, c1, rowH), UIKit.Colorize(w.Word, new Color(1f, 0.85f, 0.4f)), ChipCode);
                UIKit.Label(new Rect(row.x + 10 + c1, row.y + 6, c2, rowH), w.Reading, UIKit.Small);
                UIKit.Label(new Rect(row.x + 10 + c1 + c2, row.y + 4, row.width - c1 - c2 - 14, rowH), w.Meaning, UIKit.Body);
            }
            if (ts) UIKit.EndScrollView();
            UIKit.Label(new Rect(right.x, right.yMax - 28, right.width, 28), "かばんの「英語ことばカード」でも 見返せるよ", UIKit.SmallCenter);
        }

        private static GUIStyle chipCode;
        private static GUIStyle ChipCode => chipCode ?? (chipCode = new GUIStyle(UIKit.CodeSmall) { richText = true, wordWrap = false, clipping = TextClipping.Clip });

        /// <summary>長いときは スクロールできる 文</summary>
        private static void ScrollLabel(Rect r, string text, GUIStyle style, ref Vector2 scroll)
        {
            float h = UIKit.CalcHeight(text, style, r.width - 20);
            if (h <= r.height) { UIKit.Label(r, text, style); return; }
            scroll = UIKit.BeginScrollView(r, scroll, new Rect(0, 0, r.width - 20, h));
            UIKit.Label(new Rect(0, 0, r.width - 20, h), text, style);
            UIKit.EndScrollView();
        }
    }
}
