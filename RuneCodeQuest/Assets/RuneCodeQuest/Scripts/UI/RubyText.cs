using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// ふりがな（ルビ）付きで文章を描くクラス。UIKit.Label から自動で使われます。
    /// ・漢字の上に小さく読みを表示する
    /// ・枠の幅で自動で折り返す（GUIStyle.wordWrap が true のとき）
    /// ・&lt;color=#RRGGBB&gt; の色タグに対応
    /// 座標と文字の大きさは「実際の画面のピクセル」で受け取ります。
    /// </summary>
    public static class RubyText
    {
        private const float RubyScale = 0.52f; // ふりがなの大きさ（本文に対する割合）

        private class Token
        {
            public string Text;
            public string Ruby;
            public Color Color;
            public bool NewLine;
            public float BaseW, RubyW, W, X;
        }

        private class Line
        {
            public readonly List<Token> Tokens = new List<Token>();
            public float Width;
            public bool Empty = true;
        }

        private static readonly Dictionary<GUIStyle, GUIStyle> plainStyles = new Dictionary<GUIStyle, GUIStyle>();
        private static readonly Dictionary<GUIStyle, GUIStyle> rubyStyles = new Dictionary<GUIStyle, GUIStyle>();
        private static readonly Dictionary<string, float> widthCache = new Dictionary<string, float>();
        private static readonly GUIContent tmp = new GUIContent();

        public static void ClearCache()
        {
            plainStyles.Clear();
            rubyStyles.Clear();
            widthCache.Clear();
        }

        // ------------------------------------------------------------------

        public static void Draw(Rect rect, string text, GUIStyle style)
        {
            var plain = Plain(style);
            var ruby = Ruby(style);
            var inner = style.padding.Remove(rect);
            var lines = Layout(text, style, inner.width, out bool hasRuby);
            Metrics(plain, ruby, hasRuby, out float baseH, out float rubyH);

            float total = TotalHeight(lines, baseH, rubyH, hasRuby);
            float y = inner.y;
            var a = style.alignment;
            if (a == TextAnchor.MiddleLeft || a == TextAnchor.MiddleCenter || a == TextAnchor.MiddleRight) y += (inner.height - total) * 0.5f;
            else if (a == TextAnchor.LowerLeft || a == TextAnchor.LowerCenter || a == TextAnchor.LowerRight) y += inner.height - total;
            bool center = a == TextAnchor.UpperCenter || a == TextAnchor.MiddleCenter || a == TextAnchor.LowerCenter;
            bool right = a == TextAnchor.UpperRight || a == TextAnchor.MiddleRight || a == TextAnchor.LowerRight;

            var oldPlain = plain.normal.textColor;
            var oldRuby = ruby.normal.textColor;
            foreach (var line in lines)
            {
                float lh = LineHeight(line, baseH, rubyH, hasRuby);
                float x0 = inner.x + (center ? (inner.width - line.Width) * 0.5f : right ? inner.width - line.Width : 0f);
                float baseY = y + (line.Empty ? 0f : (hasRuby ? rubyH : 0f));

                // ふりがなのない部分は、同じ色が続くかぎり1回でまとめて描く
                int i = 0;
                while (i < line.Tokens.Count)
                {
                    var t = line.Tokens[i];
                    if (t.Ruby != null)
                    {
                        plain.normal.textColor = t.Color;
                        GUI.Label(new Rect(x0 + t.X + (t.W - t.BaseW) * 0.5f, baseY, t.BaseW + 4, baseH), t.Text, plain);
                        ruby.normal.textColor = t.Color;
                        GUI.Label(new Rect(x0 + t.X + (t.W - t.RubyW) * 0.5f, y, t.RubyW + 4, rubyH + 2), t.Ruby, ruby);
                        i++;
                        continue;
                    }
                    int j = i;
                    var sb = new System.Text.StringBuilder();
                    float w = 0f;
                    while (j < line.Tokens.Count && line.Tokens[j].Ruby == null && line.Tokens[j].Color == t.Color)
                    {
                        sb.Append(line.Tokens[j].Text);
                        w += line.Tokens[j].W;
                        j++;
                    }
                    plain.normal.textColor = t.Color;
                    GUI.Label(new Rect(x0 + t.X, baseY, w + 8, baseH), sb.ToString(), plain);
                    i = j;
                }
                y += lh;
            }
            plain.normal.textColor = oldPlain;
            ruby.normal.textColor = oldRuby;
        }

        public static float CalcHeight(string text, GUIStyle style, float width)
        {
            var lines = Layout(text, style, width - style.padding.horizontal, out bool hasRuby);
            Metrics(Plain(style), Ruby(style), hasRuby, out float baseH, out float rubyH);
            return TotalHeight(lines, baseH, rubyH, hasRuby) + style.padding.vertical;
        }

        // ------------------------------------------------------------------

        private static float LineHeight(Line line, float baseH, float rubyH, bool hasRuby)
        {
            if (line.Empty) return baseH * (hasRuby ? 0.6f : 1f);
            return baseH + (hasRuby ? rubyH : 0f);
        }

        private static float TotalHeight(List<Line> lines, float baseH, float rubyH, bool hasRuby)
        {
            float h = 0f;
            foreach (var l in lines) h += LineHeight(l, baseH, rubyH, hasRuby);
            return h;
        }

        private static void Metrics(GUIStyle plain, GUIStyle ruby, bool hasRuby, out float baseH, out float rubyH)
        {
            tmp.text = "あ";
            baseH = plain.CalcSize(tmp).y;
            rubyH = hasRuby ? ruby.CalcSize(tmp).y * 0.82f : 0f;
        }

        private static List<Line> Layout(string text, GUIStyle style, float maxWidth, out bool hasRuby)
        {
            var tokens = Tokenize(text, style.normal.textColor);
            var plain = Plain(style);
            var ruby = Ruby(style);
            hasRuby = false;

            var lines = new List<Line>();
            var line = new Line();
            float x = 0f;
            foreach (var t in tokens)
            {
                if (t.NewLine)
                {
                    lines.Add(line);
                    line = new Line();
                    x = 0f;
                    continue;
                }
                t.BaseW = Width(plain, t.Text);
                t.RubyW = t.Ruby != null ? Width(ruby, t.Ruby) : 0f;
                t.W = Mathf.Max(t.BaseW, t.RubyW);
                if (t.Ruby != null) hasRuby = true;

                // 折り返し（句読点や閉じカッコは行の先頭に来ないようにする）
                if (style.wordWrap && x > 0f && x + t.W > maxWidth && !NoBreakBefore(t.Text))
                {
                    lines.Add(line);
                    line = new Line();
                    x = 0f;
                }
                t.X = x;
                x += t.W;
                line.Tokens.Add(t);
                line.Width = x;
                if (t.Text.Trim().Length > 0 || t.Ruby != null) line.Empty = false;
            }
            lines.Add(line);
            return lines;
        }

        private static bool NoBreakBefore(string s) => s.Length > 0 && "、。，．,.)）」』！？!?ー～ゃゅょっャュョッ".IndexOf(s[0]) >= 0;

        /// <summary>文章を色タグ・改行・ふりがなで分けて、折り返し用の小さなかたまりにする</summary>
        private static List<Token> Tokenize(string text, Color baseColor)
        {
            var tokens = new List<Token>();
            var colors = new Stack<Color>();
            Color color = baseColor;
            var buf = new System.Text.StringBuilder();

            void Flush()
            {
                if (buf.Length == 0) return;
                foreach (var piece in Furigana.Split(buf.ToString()))
                {
                    if (piece.Ruby != null)
                    {
                        tokens.Add(new Token { Text = piece.Text, Ruby = piece.Ruby, Color = color });
                        continue;
                    }
                    // 英数字のつながり（forward() など）は途中で折り返さない
                    string s = piece.Text;
                    int k = 0;
                    while (k < s.Length)
                    {
                        int end = k + 1;
                        if (IsWordChar(s[k]))
                            while (end < s.Length && IsWordChar(s[end])) end++;
                        tokens.Add(new Token { Text = s.Substring(k, end - k), Color = color });
                        k = end;
                    }
                }
                buf.Clear();
            }

            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '\n')
                {
                    Flush();
                    tokens.Add(new Token { NewLine = true });
                    i++;
                    continue;
                }
                if (c == '<' && string.CompareOrdinal(text, i, "<color=", 0, 7) == 0)
                {
                    int close = text.IndexOf('>', i);
                    if (close > 0 && ColorUtility.TryParseHtmlString(text.Substring(i + 7, close - i - 7).Trim('"'), out var col))
                    {
                        Flush();
                        colors.Push(color);
                        color = col;
                        i = close + 1;
                        continue;
                    }
                }
                if (c == '<' && string.CompareOrdinal(text, i, "</color>", 0, 8) == 0)
                {
                    Flush();
                    color = colors.Count > 0 ? colors.Pop() : baseColor;
                    i += 8;
                    continue;
                }
                buf.Append(c);
                i++;
            }
            Flush();
            return tokens;
        }

        private static bool IsWordChar(char c) => c < 0x80 && (char.IsLetterOrDigit(c) || c == '_' || c == '(' || c == ')' || c == '.');

        private static float Width(GUIStyle s, string text)
        {
            string key = s.fontSize + "|" + (int)s.fontStyle + "|" + text;
            if (widthCache.TryGetValue(key, out float w)) return w;
            tmp.text = text;
            w = s.CalcSize(tmp).x;
            if (widthCache.Count > 20000) widthCache.Clear();
            widthCache[key] = w;
            return w;
        }

        /// <summary>余白なし・折り返しなしの本文用スタイル</summary>
        private static GUIStyle Plain(GUIStyle style)
        {
            if (plainStyles.TryGetValue(style, out var s)) return s;
            s = new GUIStyle(style)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                wordWrap = false,
                richText = false,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Overflow,
                contentOffset = Vector2.zero,
            };
            s.normal.background = null;
            s.hover.background = null;
            s.active.background = null;
            s.focused.background = null;
            plainStyles[style] = s;
            return s;
        }

        /// <summary>ふりがな用の小さなスタイル</summary>
        private static GUIStyle Ruby(GUIStyle style)
        {
            if (rubyStyles.TryGetValue(style, out var s)) return s;
            s = new GUIStyle(Plain(style))
            {
                fontSize = Mathf.Max(9, Mathf.RoundToInt(style.fontSize * RubyScale)),
                fontStyle = FontStyle.Normal,
            };
            rubyStyles[style] = s;
            return s;
        }
    }
}
