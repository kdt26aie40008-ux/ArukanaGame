using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 画面（IMGUI）の見た目をまとめたクラス。
    /// 画面は 1280x720 の座標で設計し、ウィンドウの大きさに合わせて自動で拡大・縮小します。
    ///
    /// 【文字をくっきり表示するしくみ】
    /// 画面全体を引き伸ばすと文字がぼやけるので、位置（Rect）と文字の大きさを
    /// それぞれ実際の画面の大きさに合わせて計算し直してから描いています。
    /// そのため、画面に描くときは GUI.Label ではなく UIKit.Label などを使ってください。
    ///
    /// 色や文字の大きさを変えたいときはここを編集します。
    /// </summary>
    public static class UIKit
    {
        public const float RefWidth = 1280f;
        public const float RefHeight = 720f;

        public static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.15f, 0.97f);
        public static readonly Color SubPanelColor = new Color(0.16f, 0.17f, 0.24f, 1f);
        public static readonly Color TextColor = new Color(0.97f, 0.97f, 1f);
        public static readonly Color SubTextColor = new Color(0.86f, 0.88f, 0.95f);
        public static readonly Color AccentColor = new Color(0.45f, 0.88f, 1f);
        public static readonly Color ErrorColor = new Color(1f, 0.50f, 0.50f);
        public static readonly Color WarnColor = new Color(1f, 0.78f, 0.35f);
        public static readonly Color SuccessColor = new Color(0.50f, 0.95f, 0.55f);

        public static GUIStyle Huge, Support, Title, Heading, Body, Small, SmallCenter, Center, Button, BigButton, SmallButton, HelpText, Code, CodeSmall, CodeArea, Gutter, BlockButton, BlockText, Explain, InputField;

        private static float scale = 1f;
        private static Vector2 offset;
        private static bool initialized;
        private static Font uiFont, codeFont;

        // 拡大率に合わせて文字の大きさを変えたスタイル（拡大率が変わったら作り直す）
        private static readonly Dictionary<GUIStyle, GUIStyle> scaledStyles = new Dictionary<GUIStyle, GUIStyle>();
        private static float scaledFor = -1f;

        public static float Scale => scale;

        /// <summary>OnGUI の最初で呼ぶ（スタイルの準備と拡大縮小の設定）。最後に End() を呼ぶ</summary>
        public static void Begin()
        {
            if (!initialized || uiFont == null) Init();
            // 日本語の入力らんが 画面から消えたら、日本語入力を元にもどす（町を歩くキーが 変換されないように）
            if (imeOn && Time.frameCount - imeFieldFrame > 2) SetIme(false);

            scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            offset = new Vector2(Mathf.Round((Screen.width - RefWidth * scale) * 0.5f),
                                 Mathf.Round((Screen.height - RefHeight * scale) * 0.5f));
            if (!Mathf.Approximately(scale, scaledFor))
            {
                scaledStyles.Clear();
                RubyText.ClearCache();
                scaledFor = scale;
            }
            GUI.matrix = Matrix4x4.identity;
            // 画面の中央に 16:9 の描画エリアを作る（中の座標は左上が 0,0 になる）
            GUI.BeginGroup(new Rect(offset.x, offset.y, RefWidth * scale, RefHeight * scale));

            // キーボード操作：前の回に数えたボタンの数を覚えて、数えなおす
            lastButtonCount = buttonCounter;
            buttonCounter = 0;
            firstPrimary = -1;
            navEnabled = false;
            if (Event.current.type == EventType.MouseDown) ringVisible = false;
        }

        public static void End()
        {
            // 画面が変わった直後は、いちばんよく使うボタン（primary）をえらんでおく
            if (buttonCounter > 0)
            {
                if (focus < 0) focus = firstPrimary >= 0 ? firstPrimary : 0;
                if (focus >= buttonCounter) focus = buttonCounter - 1;
            }
            GUI.EndGroup();
        }

        // ------------------------------------------------------------------
        // キーボードでボタンを操作する（マウスがなくても遊べるように）
        //   矢印キー / Tab : えらぶボタンを動かす　　Enter / スペース : 押す
        // ------------------------------------------------------------------

        private static string navScreen;
        private static bool navEnabled;
        private static int navColumns = 1;
        private static int focus = -1;
        private static int buttonCounter, lastButtonCount, firstPrimary = -1;
        private static bool ringVisible = true;

        /// <summary>
        /// この画面でキーボードのボタン操作を使うかを決める（画面を描く前に呼ぶ）。
        /// screen が変わると、えらんでいるボタンは最初にもどる。columns は「上下キーでいくつ動くか」（ならんだボタン用）
        /// </summary>
        public static void Nav(string screen, bool enabled, int columns = 1)
        {
            if (screen != navScreen)
            {
                navScreen = screen;
                focus = -1;
            }
            navEnabled = enabled;
            navColumns = Mathf.Max(1, columns);
            var e = Event.current;
            if (!enabled || GUIUtility.keyboardControl != 0 || e.type != EventType.KeyDown || lastButtonCount <= 0) return;
            int step = 0;
            switch (e.keyCode)
            {
                case KeyCode.Tab: step = e.shift ? -1 : 1; break;
                case KeyCode.RightArrow: step = 1; break;
                case KeyCode.LeftArrow: step = -1; break;
                case KeyCode.DownArrow: step = navColumns; break;
                case KeyCode.UpArrow: step = -navColumns; break;
            }
            if (step == 0) return;
            if (!ringVisible) ringVisible = true;
            else focus = focus < 0 ? 0 : ((focus + step) % lastButtonCount + lastButtonCount) % lastButtonCount;
            e.Use();
        }

        private static bool IsPressKey(Event e) =>
            e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space);

        /// <summary>画面のピクセル座標（左上が原点）→ 1280x720 の座標</summary>
        public static Vector2 ScreenToUI(Vector2 guiScreenPos) => (guiScreenPos - offset) / scale;

        /// <summary>1280x720 の座標 → 実際の画面のピクセル（ピクセルにそろえると文字がにじまない）</summary>
        public static Rect S(Rect r)
        {
            float x = Mathf.Round(r.x * scale);
            float y = Mathf.Round(r.y * scale);
            return new Rect(x, y, Mathf.Round((r.x + r.width) * scale) - x, Mathf.Round((r.y + r.height) * scale) - y);
        }

        /// <summary>文字の大きさなどを拡大率に合わせたスタイル</summary>
        public static GUIStyle Scaled(GUIStyle style)
        {
            if (scaledStyles.TryGetValue(style, out var s)) return s;
            s = new GUIStyle(style)
            {
                fontSize = Mathf.Max(10, Mathf.RoundToInt(style.fontSize * scale)),
                padding = ScaleOffset(style.padding),
                margin = ScaleOffset(style.margin),
                border = style.border,
            };
            scaledStyles[style] = s;
            return s;
        }

        private static RectOffset ScaleOffset(RectOffset o) =>
            new RectOffset(Mathf.RoundToInt(o.left * scale), Mathf.RoundToInt(o.right * scale),
                           Mathf.RoundToInt(o.top * scale), Mathf.RoundToInt(o.bottom * scale));

        private static void Init()
        {
            initialized = true;
            // 日本語が読みやすい OS のフォントを探す（BIZ UD は読みやすさを重視したフォント）
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "BIZ UDPGothic", "Meiryo UI", "Meiryo", "Yu Gothic UI", "Hiragino Sans", "Noto Sans CJK JP", "Arial" }, 20);
            codeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "BIZ UDGothic", "MS Gothic", "Osaka-Mono", "Consolas", "Menlo", "Courier New" }, 20);

            GUI.skin.font = uiFont;

            // 日本語は太字にするとにじみやすいので、見出しは「大きさと色」で目立たせる
            Title = Make(GUI.skin.label, 46, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            Huge = Make(GUI.skin.label, 76, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);       // タイピングで打つ日本語
            Support = Make(GUI.skin.label, 32, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor);  // タイピングのお手本（ローマ字）
            Heading = Make(GUI.skin.label, 25, FontStyle.Normal, TextAnchor.UpperLeft, AccentColor);
            Body = Make(GUI.skin.label, 19, FontStyle.Normal, TextAnchor.UpperLeft, TextColor);
            Body.wordWrap = true;
            // 説明・解説の文章は大きめ（小学生でも読みやすいように）
            Explain = Make(GUI.skin.label, 24, FontStyle.Normal, TextAnchor.UpperLeft, TextColor);
            Explain.wordWrap = true;
            InputField = new GUIStyle(GUI.skin.textField) { fontSize = 26, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 6, 6) };
            Small = Make(GUI.skin.label, 16, FontStyle.Normal, TextAnchor.UpperLeft, SubTextColor);
            Small.wordWrap = true;
            SmallCenter = Make(GUI.skin.label, 16, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor);
            BlockText = Make(GUI.skin.label, 17, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            Center = Make(GUI.skin.label, 19, FontStyle.Normal, TextAnchor.MiddleCenter, TextColor);
            Center.wordWrap = true;
            Button = Make(GUI.skin.button, 19, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            BigButton = Make(GUI.skin.button, 24, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SmallButton = Make(GUI.skin.button, 16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            HelpText = Make(GUI.skin.label, 16, FontStyle.Normal, TextAnchor.MiddleLeft, SubTextColor);
            BlockButton = Make(GUI.skin.button, 21, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            BlockButton.padding = new RectOffset(12, 8, 4, 4);

            Code = Make(GUI.skin.label, 19, FontStyle.Normal, TextAnchor.UpperLeft, TextColor);
            Code.font = codeFont;
            Code.richText = false;
            CodeSmall = new GUIStyle(Code) { fontSize = 16 };
            CodeArea = new GUIStyle(GUI.skin.textArea) { font = codeFont, fontSize = 21, wordWrap = false };
            CodeArea.padding = new RectOffset(8, 8, 8, 8);
            Gutter = new GUIStyle(CodeArea) { alignment = TextAnchor.UpperRight, padding = new RectOffset(0, 0, 0, 0) };
            Gutter.normal.background = null;
            Gutter.normal.textColor = new Color(0.6f, 0.65f, 0.75f);
        }

        private static GUIStyle Make(GUIStyle baseStyle, int size, FontStyle fs, TextAnchor anchor, Color color)
        {
            var s = new GUIStyle(baseStyle) { fontSize = size, fontStyle = fs, alignment = anchor, richText = true };
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.active.textColor = color;
            s.focused.textColor = color;
            return s;
        }

        // ------------------------------------------------------------------
        // 描画（座標はすべて 1280x720 基準で渡す）
        // ------------------------------------------------------------------

        /// <summary>
        /// 文字を描く。漢字があれば自動でふりがなを付ける。
        /// （プログラム用の等幅フォントのスタイルには付けない）
        /// </summary>
        /// <summary>長い文章をスクロールして読めるように描く（入りきるときはそのまま）</summary>
        public static Vector2 ScrollText(Rect view, Vector2 scroll, string text, GUIStyle style)
        {
            float h = CalcHeight(text, style, view.width - 24) + 12;
            if (h <= view.height)
            {
                Label(view, text, style);
                return Vector2.zero;
            }
            scroll = BeginScrollView(view, scroll, new Rect(0, 0, view.width - 24, h));
            Label(new Rect(0, 0, view.width - 24, h), text, style);
            EndScrollView();
            return scroll;
        }

        public static void Label(Rect r, string text, GUIStyle style)
        {
            if (UseRuby(text, style)) RubyText.Draw(S(r), text, Scaled(style));
            else GUI.Label(S(r), text, Scaled(style));
        }

        /// <summary>文章の高さ（1280x720 基準）。スクロールの大きさを決めるときに使う</summary>
        public static float CalcHeight(string text, GUIStyle style, float width)
        {
            float h = UseRuby(text, style)
                ? RubyText.CalcHeight(text, Scaled(style), width * scale)
                : Scaled(style).CalcHeight(new GUIContent(text), width * scale);
            return h / scale;
        }

        private static bool UseRuby(string text, GUIStyle style) =>
            Furigana.Enabled && style.font == null && Furigana.HasKanji(text);

        public static void Texture(Rect r, Texture tex) => GUI.DrawTexture(S(r), tex);

        /// <summary>打った文字に色をつける：先頭から done 文字を doneColor、のこりを restColor（null なら のこりは出さない）</summary>
        public static string TypedText(string s, int done, Color doneColor, Color? restColor)
        {
            done = Mathf.Clamp(done, 0, s.Length);
            string a = s.Substring(0, done).Replace("<", "＜"), b = s.Substring(done).Replace("<", "＜");
            return (a.Length > 0 ? Colorize(a, doneColor) : "") + (restColor.HasValue && b.Length > 0 ? Colorize(b, restColor.Value) : "");
        }

        /// <summary>打った文字の色（打ったところは緑）</summary>
        public static readonly Color TypedColor = new Color(0.35f, 1f, 0.45f);

        /// <summary>色をつけて画像を描く</summary>
        public static void TextureTinted(Rect r, Texture tex, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(S(r), tex);
            GUI.color = old;
        }

        public static string TextArea(Rect r, string text, GUIStyle style) => GUI.TextArea(S(r), text, Scaled(style));

        public static string TextField(Rect r, string text, GUIStyle style) => GUI.TextField(S(r), text, Scaled(style));

        // ----- 日本語入力（IME）が使える入力らん -----
        private static bool imeOn;          // IME を オンにしているか
        private static int imeFieldFrame = -10; // 日本語の入力らんを さいごに出したフレーム

        private static void SetIme(bool on)
        {
            if (imeOn == on) return;
            imeOn = on;
            // オンのときは いつでも日本語入力、そうでないときは Unity におまかせ（バトルなどは それぞれの画面がオフにする）
            try { Input.imeCompositionMode = on ? IMECompositionMode.On : IMECompositionMode.Auto; } catch { /* 新しい Input System だけのときは何もしない */ }
        }

        /// <summary>
        /// 日本語（ひらがな・漢字）も入力できる 1行の入力らん。controlName は ほかとかぶらない名前。
        /// Enter・Tab・Esc で入力をおえる。ただし 変換中の Enter は、変換の決定に使う（入力はおわらない）
        /// </summary>
        public static string ImeTextField(Rect r, string text, GUIStyle style, string controlName, int maxLength)
        {
            var ev = Event.current;
            bool focused = GUI.GetNameOfFocusedControl() == controlName;
            imeFieldFrame = Time.frameCount;
            if (focused)
            {
                // ほかの画面で日本語入力がオフにされていても、入力らんを えらんだら オンにする
                if (!imeOn || Input.imeCompositionMode != IMECompositionMode.On) { imeOn = false; SetIme(true); }
                if (ev.type == EventType.KeyDown && string.IsNullOrEmpty(Input.compositionString) &&
                    (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter || ev.keyCode == KeyCode.Tab || ev.keyCode == KeyCode.Escape))
                {
                    GUIUtility.keyboardControl = 0;
                    ev.Use();
                }
            }
            else SetIme(false);
            GUI.SetNextControlName(controlName);
            string typed = GUI.TextField(S(r), text ?? "", Scaled(style));
            return typed.Length > maxLength ? typed.Substring(0, maxLength) : typed;
        }

        public static Vector2 BeginScrollView(Rect view, Vector2 scroll, Rect content) =>
            GUI.BeginScrollView(S(view), scroll, S(content));

        public static void EndScrollView() => GUI.EndScrollView();

        /// <summary>色つきの四角を描く</summary>
        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(S(r), Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>枠つきのパネルを描く</summary>
        public static void Panel(Rect r, Color? color = null)
        {
            Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0.45f, 0.55f, 0.75f, 0.9f));
            Fill(r, color ?? PanelColor);
        }

        /// <summary>色つきボタン（押されたら true）</summary>
        /// <param name="primary">画面を開いたとき、キーボードで最初にえらばれているボタンにする</param>
        public static bool ColorButton(Rect r, string text, Color color, GUIStyle style = null, bool enabled = true, bool primary = false)
        {
            var oldBg = GUI.backgroundColor;
            var oldEnabled = GUI.enabled;

            // キーボードでえらべるボタンとして数える（押せないボタンは数えない）
            bool keyPressed = false;
            if (enabled && oldEnabled)
            {
                int id = buttonCounter++;
                if (primary && firstPrimary < 0) firstPrimary = id;
                if (navEnabled && id == focus)
                {
                    if (ringVisible) Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(1f, 0.9f, 0.2f));
                    var e = Event.current;
                    if (GUIUtility.keyboardControl == 0 && IsPressKey(e))
                    {
                        keyPressed = true;
                        e.Use();
                    }
                }
            }

            GUI.backgroundColor = color;
            GUI.enabled = enabled && oldEnabled;
            var st = style ?? Button;
            bool pressed;
            if (UseRuby(text, st))
            {
                // ボタンの枠だけ描いて、文字はふりがな付きで上に重ねる
                pressed = GUI.Button(S(r), GUIContent.none, Scaled(st));
                RubyText.Draw(S(r), text, Scaled(st));
            }
            else
            {
                pressed = GUI.Button(S(r), text, Scaled(st));
            }
            GUI.backgroundColor = oldBg;
            GUI.enabled = oldEnabled;
            return pressed || keyPressed;
        }

        private static GUIStyle roundStyle;

        /// <summary>角の丸い四角（スクラッチのブロックなど）</summary>
        public static void Round(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (roundStyle == null || roundStyle.normal.background == null)
            {
                const int size = 16;
                const float radius = 5f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                        float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                        float a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                tex.Apply();
                roundStyle = new GUIStyle { border = new RectOffset(6, 6, 6, 6) };
                roundStyle.normal.background = tex;
            }
            var old = GUI.color;
            GUI.color = c;
            roundStyle.Draw(S(r), false, false, false, false);
            GUI.color = old;
        }

        public static string Colorize(string text, Color c) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";

        /// <summary>画面下に出す操作説明の帯</summary>
        public static void HelpBar(string text)
        {
            var r = new Rect(0, RefHeight - 36, RefWidth, 36);
            Fill(r, new Color(0f, 0f, 0f, 0.85f));
            Label(new Rect(16, RefHeight - 34, RefWidth - 210, 34), text, HelpText);
            FuriganaToggle(new Rect(RefWidth - 186, RefHeight - 33, 176, 30));
        }

        /// <summary>ふりがな あり／なし の切り替えボタン</summary>
        public static void FuriganaToggle(Rect r)
        {
            string label = Furigana.Enabled ? "ふりがな：あり" : "ふりがな：なし";
            if (ColorButton(r, label, Furigana.Enabled ? AccentColor : Color.gray, SmallButton))
            {
                Furigana.Enabled = !Furigana.Enabled;
                Progress.Furigana = Furigana.Enabled;
            }
        }
    }
}
