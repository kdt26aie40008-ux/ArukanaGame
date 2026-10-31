using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// クエスト画面（ステージの装置を調べると開く）。
    ///
    /// 画面の流れ:
    ///   Intro（学ぶ内容・ステージの説明）→ ChooseMode（初回だけ：解き方を選ぶ）→ Edit（命令を作る）
    ///   →「起動」→ Running（ゴーレムが順番に動く）→ Result（クリア / エラー / 条件未達成 + ヒント）
    ///   → クリア or 挑戦回数を使い切る（章が進むほど多い）→ Explanation（解説 →「もう一度する（問題が少し変わる）」か「クエストを終える」（エリアか町にもどる））
    ///   クリアすると星（★1 クリア ／ ★2 1回目でクリア ／ ★3 命令の数がお手本以下）がもらえる
    ///
    /// ルール（回数・判定・ヒント）は QuestSession、ゴーレムの動きは GolemRunner、
    /// 問題は LevelFactory にあります。このクラスは「表示とボタン」だけを担当します。
    /// </summary>
    public partial class QuestScreen
    {
        private enum Phase { Intro, ChooseMode, Edit, Running, Result, Explanation }

        public QuestSession Session { get; private set; }

        /// <summary>ステージのクエストをクリアしたとき（引数: ステージ番号・星の数）。もらえたものの文章を返す（なければ null）</summary>
        public System.Func<int, int, string> OnCleared;
        /// <summary>解説のあと「クエストを終える」を選んだとき</summary>
        public System.Action OnProceed;
        /// <summary>「とじる」を押したとき</summary>
        public System.Action OnClose;

        private Phase phase = Phase.Intro;
        private int page;
        private List<InfoPage> introPages = new List<InfoPage>();
        private bool introFromEdit;
        private string note;
        private LaunchOutcome outcome;
        private int stepIndex;
        private float stepTimer;
        private int previewTest;
        private Vector2 editorScroll, listScroll, resultScroll, answerScroll, textScroll;
        private string textScrollKey;   // ページが変わったらスクロールを先頭にもどす
        private string rewardText;      // クリアで手に入れたアイテム
        private int earnedStars;        // 今回の星
        private bool[] earnedFlags;     // 星の条件ごとに できたか
        private int prevStars;          // クリアする前の 星の記録
        private bool wasPassed;         // クリアする前は「通過」（星なし）だった
        private bool rankUp;            // ランクが上がったか
        private bool showPocket;        // 持ち物（ポケット）を開いているか

        /// <summary>クエスト中の持ち物：「つかう」で何をするか・そうび・このクエストのアイテム・持っているアイテム</summary>
        private void DrawPocket()
        {
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.55f));
            var p = new Rect(200, 70, 880, 590);
            UIKit.Panel(p);
            var th = Level.Theme;
            UIKit.Texture(new Rect(p.x + 24, p.y + 16, 44, 44), SpriteFactory.TextureOf(SpriteFactory.Player));
            UIKit.Label(new Rect(p.x + 80, p.y + 12, 600, 50), $"{Progress.Look.Name} の持ち物", UIKit.Heading);
            if (UIKit.ColorButton(new Rect(p.xMax - 130, p.y + 16, 110, 46), "とじる", new Color(0.8f, 0.4f, 0.4f), primary: true))
                showPocket = false;

            // 「つかう」で何をするか
            float y = p.y + 76;
            UIKit.Fill(new Rect(p.x + 20, y, p.width - 40, 108), UIKit.SubPanelColor);
            UIKit.Label(new Rect(p.x + 34, y + 4, 400, 34), UIKit.Colorize("「つかう」で できること", UIKit.AccentColor), UIKit.Body);
            UIKit.Texture(new Rect(p.x + 34, y + 44, 52, 52), SpriteFactory.TextureOf(QuestArt.Goal(th.Goal, false)));
            string useText = Level.Commands.Contains(CommandType.Use)
                ? th.UseText
                : "このステージでは「つかう」はまだ使わないよ。ゴールに着けばクリア！";
            UIKit.Label(new Rect(p.x + 100, y + 42, p.width - 130, 64), useText, UIKit.Explain);

            // そうび
            y += 122;
            UIKit.Label(new Rect(p.x + 30, y, 300, 30), "そうび", UIKit.Small);
            UIKit.Texture(new Rect(p.x + 30, y + 30, 48, 48), SpriteFactory.TextureOf(QuestArt.Wand));
            UIKit.Label(new Rect(p.x + 90, y + 26, p.width - 120, 34), "まほうの杖", UIKit.Body);
            UIKit.Label(new Rect(p.x + 90, y + 58, p.width - 120, 30), "「まほう」でじゃまものを消したり、「つかう」でゴールのものを動かしたりする杖。", UIKit.Small);

            // このクエストのアイテム
            y += 100;
            UIKit.Label(new Rect(p.x + 30, y, 400, 30), "このクエストのアイテム", UIKit.Small);
            if (th.HasItems)
            {
                var st = CurrentStep();
                int test = st != null ? st.Test : Mathf.Clamp(previewTest, 0, Level.Tests.Count - 1);
                int total = Level.Tests[test].CountItems();
                int got = st != null ? total - st.Map.CountItems() : 0;
                UIKit.Texture(new Rect(p.x + 30, y + 30, 48, 48), SpriteFactory.TextureOf(QuestArt.Item(th.Item)));
                UIKit.Label(new Rect(p.x + 90, y + 26, p.width - 120, 34), $"{th.ItemName}　ひろった {got} / {total}", UIKit.Body);
                UIKit.Label(new Rect(p.x + 90, y + 58, p.width - 120, 30), "アイテムのあるマスで「ひろう」。すべてひろってから「つかう」！", UIKit.Small);
            }
            else UIKit.Label(new Rect(p.x + 90, y + 34, p.width - 120, 34), "このクエストでは、アイテムを集めなくていいよ。", UIKit.Body);

            // 持っているアイテム（これまでに集めたもの）
            y += 100;
            UIKit.Label(new Rect(p.x + 30, y, 400, 30), "これまでに集めたアイテム", UIKit.Small);
            float x = p.x + 30;
            int shown = 0;
            for (int k = 1; k < QuestTheme.ItemNames.Length; k++)
            {
                int count = Progress.ItemCount((ItemKind)k);
                if (count <= 0) continue;
                UIKit.Texture(new Rect(x, y + 32, 40, 40), SpriteFactory.TextureOf(QuestArt.Item((ItemKind)k)));
                UIKit.Label(new Rect(x + 42, y + 38, 60, 30), $"×{count}", UIKit.Body);
                x += 104;
                shown++;
            }
            if (shown == 0) UIKit.Label(new Rect(p.x + 30, y + 34, p.width - 60, 34), "まだ何も持っていない。", UIKit.Body);
        }

        /// <summary>大きな文字の説明文（入りきらないときはスクロール）</summary>
        private void ExplainText(Rect r, string text)
        {
            string key = phase + ":" + page;
            if (key != textScrollKey) { textScroll = Vector2.zero; textScrollKey = key; }
            textScroll = UIKit.ScrollText(r, textScroll, text, UIKit.Explain);
        }
        private readonly ScratchEditor scratch = new ScratchEditor();

        // 画面の配置（1280x720 基準）
        private static readonly Rect Window = new Rect(30, 10, 1220, 664);
        private static readonly Rect GoalRect = new Rect(50, 74, 1180, 46);
        private static readonly Rect GridRect = new Rect(50, 130, 520, 360);
        private static readonly Rect TabsRect = new Rect(50, 498, 520, 32);
        private static readonly Rect LogRect = new Rect(50, 538, 520, 66);
        private static readonly Rect FullEditorRect = new Rect(590, 130, 640, 474);
        private static readonly Rect LimitRect = new Rect(590, 130, 640, 50);

        /// <summary>命令を作る場所（おまけのしばりがあるときは、上にしばりの行を出すので少し下げる）</summary>
        private Rect EditorRect => Level.UsageLimits == null ? FullEditorRect
            : new Rect(FullEditorRect.x, LimitRect.yMax + 4, FullEditorRect.width, FullEditorRect.yMax - LimitRect.yMax - 4);
        private static readonly Rect ResultRect = new Rect(590, 130, 640, 236);
        private static readonly Rect ListingRect = new Rect(590, 374, 640, 230);
        private const float BottomY = 614, BottomH = 48;

        private Level Level => Session.Level;

        // ------------------------------------------------------------------
        // 開く
        // ------------------------------------------------------------------

        /// <summary>装置を調べたときに呼ぶ。同じステージなら続きから再開する</summary>
        public void Open(int stage)
        {
            note = null;
            if (Session == null || Session.Level.Number != stage)
            {
                Session = new QuestSession(stage);
                Session.Mode = Progress.PreferredMode;
                outcome = null;
                previewTest = 0;
                StartIntro(false);
                return;
            }
            if (phase == Phase.Running) phase = Phase.Result;
        }

        private void StartIntro(bool fromEdit)
        {
            introPages = new List<InfoPage>();
            var concept = fromEdit ? QuestDatabase.LatestConcept(Level.Template) : Level.NewConcept;
            if (concept != null)
            {
                introPages.AddRange(concept.Pages);
                // 新しいブロックの使い方（手順 ＋ ブロックの絵）を 1ページ目の次に
                var how = ConceptExamples.For(concept);
                if (how != null) introPages.Insert(1, how);
            }
            introPages.AddRange(LevelPages());
            Progress.MarkEnglish(EnglishWords.ForLevel(Level).ConvertAll(w => w.Word)); // 英語ことばカードに ふやす
            introFromEdit = fromEdit;
            page = 0;
            phase = Phase.Intro;
        }

        /// <summary>ステージの説明：「目的と星」のページと、「使える命令と 英語のことば」のページ（絵で かく）</summary>
        private IEnumerable<InfoPage> LevelPages()
        {
            yield return new InfoPage(Level.Title, "") { Special = 1 };
            yield return new InfoPage("使える命令と 英語のことば", "") { Special = 2 };
        }

        public void Update()
        {
            if (phase != Phase.Running) return;
            int count = outcome.Run.Steps.Count;
            float stepSeconds = Mathf.Clamp(16f / count, 0.1f, 0.6f);
            stepTimer += Time.deltaTime;
            if (stepTimer < stepSeconds) return;
            stepTimer = 0f;
            stepIndex++;
            SoundKit.GolemStep(); // ゴーレムが1歩うごく
            if (stepIndex >= count - 1) FinishRun();
        }

        // ------------------------------------------------------------------
        // 描画
        // ------------------------------------------------------------------

        public void Draw()
        {
            HandleHotkeys();
            if (phase == Phase.Edit && lastPhase != Phase.Edit && Session.Mode == SolveMode.Python) focusPython = true;
            lastPhase = phase;
            // 命令づくりの間は、矢印キーはスクラッチ（または文字の入力）に使うので、ボタンのキーボード操作はしない
            bool editing = phase == Phase.Edit && !showPocket;
            UIKit.Nav(showPocket ? "quest-pocket" : $"quest-{phase}-{page}", !editing);

            // ポケットを開いている間は、うしろの画面は押せないようにする
            bool oldEnabled = GUI.enabled;
            if (showPocket) GUI.enabled = false;
            UIKit.Panel(Window);
            DrawHeader();

            switch (phase)
            {
                case Phase.Intro: DrawIntro(); break;
                case Phase.ChooseMode: DrawChooseMode(); break;
                case Phase.Explanation: DrawExplanation(); break;
                default: DrawWorkspace(); break;
            }

            UIKit.HelpBar(HelpText());
            GUI.enabled = oldEnabled;
            if (showPocket) DrawPocket();
        }

        private Phase lastPhase = Phase.Intro;
        private bool focusPython;

        /// <summary>
        /// キーボードのショートカット（マウスがなくても遊べるように）
        ///   F5 起動　F2 スクラッチ⇔Python　F3 持ち物　F4 テストの切りかえ　F1 説明　Esc とじる
        /// </summary>
        private void HandleHotkeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.F3:
                    scratch.CancelDrag();
                    GUIUtility.keyboardControl = 0;
                    showPocket = !showPocket;
                    e.Use();
                    return;
                case KeyCode.Escape:
                    e.Use();
                    if (scratch.IsCarrying) scratch.CancelDrag();   // 持っているブロックを もとにもどす
                    else if (showPocket) showPocket = false;
                    else if (GUIUtility.keyboardControl != 0) GUIUtility.keyboardControl = 0; // 入力をやめる
                    else if (phase != Phase.Running)
                    {
                        scratch.CancelDrag();
                        OnClose?.Invoke();
                    }
                    return;
            }
            if (showPocket || phase != Phase.Edit) return;
            switch (e.keyCode)
            {
                case KeyCode.F5: e.Use(); Launch(); break;
                case KeyCode.F2: e.Use(); SwitchMode(Session.Mode == SolveMode.Scratch ? SolveMode.Python : SolveMode.Scratch); break;
                case KeyCode.F1: e.Use(); scratch.CancelDrag(); StartIntro(true); break;
                case KeyCode.F4: e.Use(); previewTest = (previewTest + 1) % Level.Tests.Count; break;
            }
        }

        private string HelpText()
        {
            switch (phase)
            {
                case Phase.Edit:
                    return Session.Mode == SolveMode.Scratch
                        ? "タップで持つ→タップで置く　2本指タップで消す　キー: ↑↓←→ Space BS ＋− C V　F5 起動"
                        : "Tab 字下げ　F5 起動　F2 スクラッチへ　F3 持ち物　Esc 入力をやめる";
                case Phase.Running:
                    return "ゴーレムが命令を上から順番に実行中…　マス目をタップ / Space: スキップ　F3: 持ち物";
                default:
                    return "矢印キー・Tab でえらんで Enter（マウスでもOK）　F3: 持ち物　Esc: とじる";
            }
        }

        /// <summary>上の帯：タイトル・のこり回数・解き方の切り替え・とじる</summary>
        private void DrawHeader()
        {
            UIKit.Label(new Rect(50, 12, 500, 48), Level.Title + (Session.IsSimilarProblem ? "（少し変わった問題）" : ""), UIKit.Heading);

            // のこりの挑戦回数（章が進むほど多い）：数字と ●
            string dots = "";
            for (int i = 0; i < Session.MaxAttempts; i++)
                dots += UIKit.Colorize("●", i < Session.AttemptsLeft ? UIKit.AccentColor : Color.gray);
            UIKit.Label(new Rect(556, 6, 230, 28), $"のこり {Session.AttemptsLeft} / {Session.MaxAttempts} 回", UIKit.Small);
            UIKit.Label(new Rect(556, 32, 230, 28), dots, UIKit.Small);

            if (phase == Phase.Edit)
            {
                if (UIKit.ColorButton(new Rect(790, 18, 124, 40), "スクラッチ", Session.Mode == SolveMode.Scratch ? UIKit.AccentColor : Color.gray))
                    SwitchMode(SolveMode.Scratch);
                if (UIKit.ColorButton(new Rect(918, 18, 110, 40), "Python", Session.Mode == SolveMode.Python ? UIKit.AccentColor : Color.gray))
                    SwitchMode(SolveMode.Python);
            }

            // ポケット（持ち物）
            var pocketRect = new Rect(1036, 18, 100, 40);
            if (UIKit.ColorButton(pocketRect, "　　持ち物", new Color(0.85f, 0.6f, 0.3f), UIKit.SmallButton))
            {
                scratch.CancelDrag();
                GUIUtility.keyboardControl = 0;
                showPocket = true;
            }
            UIKit.Texture(new Rect(pocketRect.x + 6, pocketRect.y + 6, 28, 28), SpriteFactory.TextureOf(QuestArt.Pocket));

            if (phase != Phase.Running && UIKit.ColorButton(new Rect(1140, 18, 96, 40), "とじる", new Color(0.8f, 0.4f, 0.4f)))
            {
                scratch.CancelDrag();
                OnClose?.Invoke();
            }
        }

        private void SwitchMode(SolveMode mode)
        {
            GUIUtility.keyboardControl = 0;
            scratch.CancelDrag();
            string msg = Session.SwitchMode(mode, out bool ok);
            if (ok) Progress.PreferredMode = mode;
            if (ok && mode == SolveMode.Python) focusPython = true;
            note = msg;
        }

        // ----- Intro ------------------------------------------------------

        private void DrawIntro()
        {
            var p = introPages[page];
            var body = new Rect(90, 90, 1100, 500);
            UIKit.Panel(body, UIKit.SubPanelColor);
            UIKit.Label(new Rect(body.x + 30, body.y + 16, body.width - 60, 48), p.Title, UIKit.Heading);
            if (p.Special == 1) DrawGoalPage(body);
            else if (p.Special == 2) DrawToolsPage(body);
            else if (p.Example == null) ExplainCards(new Rect(body.x + 30, body.y + 72, body.width - 50, body.height - 112), p.Body);
            else
            {
                // 左：くわしい手順　右：ブロックの絵（Python の人は コード）
                ExplainCards(new Rect(body.x + 30, body.y + 72, 600, body.height - 112), p.Body);
                DrawInfoPicture(new Rect(body.x + 650, body.y + 66, body.width - 680, body.height - 110), p);
            }
            UIKit.Label(new Rect(body.x, body.yMax - 36, body.width - 30, 30), $"{page + 1} / {introPages.Count}", UIKit.SmallCenter);

            if (page > 0 && UIKit.ColorButton(new Rect(90, 604, 200, 52), "← もどる", Color.gray, UIKit.BigButton))
                page--;

            bool last = page >= introPages.Count - 1;
            string next = !last ? "つぎへ →" : introFromEdit ? "命令づくりにもどる" : "はじめる →";
            if (UIKit.ColorButton(new Rect(890, 604, 300, 52), next, UIKit.AccentColor, UIKit.BigButton, primary: true))
            {
                if (!last) page++;
                else if (!Progress.HasChosenMode) phase = Phase.ChooseMode;
                else
                {
                    phase = Phase.Edit;
                    previewTest = 0;
                }
            }
        }

        // ----- ChooseMode -------------------------------------------------

        private void DrawChooseMode()
        {
            UIKit.Label(new Rect(50, 80, 1180, 48), "どちらの方法で命令を作る？（あとから切り替えられます）", UIKit.Center);

            var a = new Rect(110, 150, 500, 440);
            var b = new Rect(670, 150, 500, 440);
            UIKit.Panel(a, UIKit.SubPanelColor);
            UIKit.Panel(b, UIKit.SubPanelColor);

            UIKit.Label(new Rect(a.x + 24, a.y + 16, a.width - 48, 44), "スクラッチ", UIKit.Heading);
            UIKit.Label(new Rect(a.x + 24, a.y + 70, a.width - 48, 150),
                "ブロックをマウスでドラッグして、パズルのようにつなげます。\nはじめての人におすすめ！", UIKit.Body);
            var b1 = new Rect(a.x + 24, a.y + 230, 170, 34);
            UIKit.Round(b1, ScratchEditor.EventColor);
            UIKit.Label(new Rect(b1.x + 10, b1.y, b1.width, b1.height), "▶ 起動したとき", UIKit.BlockText);
            var b2 = new Rect(a.x + 24, a.y + 266, 150, 34);
            UIKit.Round(b2, ScratchEditor.MotionColor);
            UIKit.Label(new Rect(b2.x + 10, b2.y, b2.width, b2.height), "すすむ", UIKit.BlockText);
            if (UIKit.ColorButton(new Rect(a.x + 100, a.y + 360, 300, 56), "スクラッチで始める", UIKit.AccentColor, UIKit.BigButton, primary: true))
                ChooseMode(SolveMode.Scratch);

            UIKit.Label(new Rect(b.x + 24, b.y + 16, b.width - 48, 44), "Python（パイソン）", UIKit.Heading);
            UIKit.Label(new Rect(b.x + 24, b.y + 70, b.width - 48, 150),
                "本物のプログラミング言語 Python を、キーボードで入力します。", UIKit.Body);
            UIKit.Label(new Rect(b.x + 24, b.y + 230, 400, 80), "for i in range(3):\n    forward()", UIKit.Code);
            if (UIKit.ColorButton(new Rect(b.x + 100, b.y + 360, 300, 56), "Python で始める", UIKit.AccentColor, UIKit.BigButton))
                ChooseMode(SolveMode.Python);
        }

        private void ChooseMode(SolveMode mode)
        {
            Progress.PreferredMode = mode;
            Session.Mode = mode;
            phase = Phase.Edit;
            previewTest = 0;
        }

        // ----- Edit / Running / Result -----------------------------------

        private void DrawWorkspace()
        {
            UIKit.Fill(GoalRect, UIKit.SubPanelColor);
            UIKit.Label(new Rect(GoalRect.x + 14, GoalRect.y + 3, GoalRect.width - 28, 42),
                $"{UIKit.Colorize("目標", UIKit.AccentColor)}  {Level.Goal}", UIKit.Body);

            DrawGrid();
            DrawTestTabs();
            if (phase != Phase.Result && Level.Tests.Count <= 1) DrawStarConditions();
            DrawLog();

            switch (phase)
            {
                case Phase.Edit:
                    DrawUsageLimits();
                    scratch.Highlight = null;
                    scratch.Failed = null;
                    if (Session.Mode == SolveMode.Scratch) scratch.Draw(EditorRect, Level, Session.Blocks, !showPocket);
                    else DrawPythonEditor();
                    DrawEditButtons();
                    break;
                case Phase.Running:
                    DrawUsageLimits();
                    var step = CurrentStep();
                    scratch.Highlight = step?.Node;
                    if (Session.Mode == SolveMode.Scratch) scratch.Draw(EditorRect, Level, Session.Blocks, false);
                    else DrawListing(EditorRect, step?.Node?.Line ?? -1);
                    if (UIKit.ColorButton(new Rect(590, BottomY, 220, BottomH), "スキップ ≫", Color.gray, UIKit.BigButton, primary: true))
                    {
                        stepIndex = outcome.Run.Steps.Count - 1;
                        FinishRun();
                    }
                    break;
                case Phase.Result:
                    DrawResult();
                    scratch.Highlight = null;
                    scratch.Failed = outcome.Run?.FailedNode;
                    if (Session.Mode == SolveMode.Scratch) scratch.Draw(ListingRect, Level, Session.Blocks, false);
                    else DrawListing(ListingRect, -1);
                    DrawResultButtons();
                    break;
            }
        }

        /// <summary>
        /// グリッドの下：星の条件（★クリア ★1回目でクリア ★命令○こ以下）。命令の数は 今いくつ置いているかも出す
        /// </summary>
        private void DrawStarConditions()
        {
            var r = new Rect(GridRect.x, GridRect.yMax + 4, GridRect.width, 40);
            UIKit.Fill(r, new Color(0.18f, 0.16f, 0.08f));
            int now = ProgramText.Count(Session.BuildProgram(out _)), limit = QuestStars.CommandLimit(Level);
            var gold = new Color(1f, 0.85f, 0.2f);
            string first = Session.AttemptsUsed == 0 ? UIKit.Colorize("★1回目でクリア", gold) : UIKit.Colorize("★1回目でクリア（今回は とれないよ）", new Color(1f, 1f, 1f, 0.4f));
            string cmd = UIKit.Colorize($"★命令{limit}こ以下", gold) + UIKit.Colorize($"（今 {now}こ）", now <= limit ? UIKit.SuccessColor : UIKit.WarnColor);
            UIKit.Label(new Rect(r.x + 10, r.y, r.width - 20, r.height), $"星の条件　{UIKit.Colorize("★クリア", gold)}　{first}　{cmd}", UIKit.Small);
        }

        /// <summary>
        /// おまけのしばりの行：ブロックごとに「置いた数/置ける数」。こえていたら赤、使えないものは ×。
        /// Python のときは、書いているコードを読みとって数える
        /// </summary>
        private void DrawUsageLimits()
        {
            if (Level.UsageLimits == null) return;
            UIKit.Fill(LimitRect, new Color(0.30f, 0.20f, 0.10f));
            var program = Session.BuildProgram(out _);
            var used = ProgramText.CountByKind(program);
            var sb = new System.Text.StringBuilder(UIKit.Colorize("しばり ", UIKit.WarnColor));
            foreach (var l in Level.UsageLimits)
            {
                int n = used.TryGetValue(l.Name, out int c) ? c : 0;
                string text = l.Max == 0 ? $"{l.Name}×" : $"{l.Name} {n}/{l.Max}";
                Color col = n > l.Max ? UIKit.ErrorColor : l.Max == 0 ? Color.gray : n == l.Max ? UIKit.SuccessColor : UIKit.TextColor;
                sb.Append(UIKit.Colorize(text, col)).Append("　");
            }
            UIKit.Label(new Rect(LimitRect.x + 8, LimitRect.y + 2, LimitRect.width - 16, LimitRect.height - 2), sb.ToString(), UIKit.Small);
        }

        private RunStep CurrentStep()
        {
            if (outcome?.Run == null || (phase != Phase.Running && phase != Phase.Result)) return null;
            return outcome.Run.Steps[Mathf.Clamp(stepIndex, 0, outcome.Run.Steps.Count - 1)];
        }

        private void DrawGrid()
        {
            // 実行中にマス目をタップ（クリック）すると、最後まで飛ばす
            var ev = Event.current;
            if (phase == Phase.Running && !showPocket && ev.type == EventType.MouseDown && GridRect.Contains(ev.mousePosition / UIKit.Scale))
            {
                ev.Use();
                stepIndex = outcome.Run.Steps.Count - 1;
                FinishRun();
            }
            var st = CurrentStep();
            if (st != null) GridView.Draw(GridRect, st.Map, st.Pos, st.Dir, st.Done, st.IsFailure, Level);
            else
            {
                var map = Level.Tests[Mathf.Clamp(previewTest, 0, Level.Tests.Count - 1)];
                GridView.Draw(GridRect, map, map.Start, map.StartDir, false, false, Level);
            }
        }

        /// <summary>テストが2つ以上あるときだけ、切り替えボタンを出す</summary>
        private void DrawTestTabs()
        {
            if (Level.Tests.Count <= 1) return;
            var st = CurrentStep();
            int active = st != null ? st.Test : previewTest;
            for (int i = 0; i < Level.Tests.Count; i++)
            {
                var r = new Rect(TabsRect.x + i * 110, TabsRect.y, 102, TabsRect.height);
                if (UIKit.ColorButton(r, $"テスト{i + 1}", i == active ? UIKit.AccentColor : Color.gray, UIKit.SmallButton, phase == Phase.Edit))
                    previewTest = i;
            }
        }

        /// <summary>左下の1〜2行のお知らせ（実行中は、今の動き）</summary>
        private void DrawLog()
        {
            var inner = new Rect(LogRect.x + 4, LogRect.y, LogRect.width - 8, LogRect.height);
            string text;
            if (phase == Phase.Edit) text = note != null ? UIKit.Colorize(note, UIKit.WarnColor) : null;
            else if (outcome?.Run == null) text = UIKit.Colorize("エラーがあるので、ゴーレムは動いていません。", UIKit.ErrorColor);
            else
            {
                var stp = outcome.Run.Steps[Mathf.Clamp(stepIndex, 0, outcome.Run.Steps.Count - 1)];
                text = stp.IsFailure ? UIKit.Colorize(stp.Log, UIKit.ErrorColor) : stp.Log;
            }
            if (text == null) return;
            UIKit.Fill(LogRect, UIKit.SubPanelColor);
            UIKit.Label(new Rect(inner.x + 8, inner.y + 4, inner.width - 16, inner.height - 8), text, UIKit.Small);
        }

        // ----- Python -----------------------------------------------------

        private const string PyField = "PythonArea";

        private void DrawPythonEditor()
        {
            var r = EditorRect;
            UIKit.Fill(r, UIKit.SubPanelColor);
            var area = new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20 - 96);
            HandlePythonKeys();

            float lineH = UIKit.CodeArea.lineHeight;
            float textH = Mathf.Max(area.height - 4, UIKit.CodeArea.CalcHeight(new GUIContent(Session.PythonText + "\n"), area.width - 70));
            const float gutter = 44f;
            editorScroll = UIKit.BeginScrollView(area, editorScroll, new Rect(0, 0, area.width - 20, textH));
            int lines = Session.PythonText.Split('\n').Length;
            for (int i = 0; i < lines; i++)
                UIKit.Label(new Rect(0, UIKit.CodeArea.padding.top + i * lineH, gutter - 6, lineH), (i + 1).ToString(), UIKit.Gutter);
            GUI.SetNextControlName(PyField);
            Session.PythonText = UIKit.TextArea(new Rect(gutter, 0, area.width - 20 - gutter, textH), Session.PythonText, UIKit.CodeArea);
            if (focusPython && !showPocket)
            {
                GUI.FocusControl(PyField);
                focusPython = false;
            }
            UIKit.EndScrollView();

            // 使える書き方（入力の参考）
            var refRect = new Rect(r.x + 10, r.yMax - 100, r.width - 20, 90);
            UIKit.Fill(refRect, new Color(0.10f, 0.11f, 0.16f));
            var words = new List<string>();
            foreach (var c in Level.Commands) words.Add(CommandInfo.Get(c).PythonCode);
            if (Level.Structures.Contains(NodeKind.Repeat)) words.Add("for i in range(3):");
            if (Level.Structures.Contains(NodeKind.If)) words.Add(Level.AllowElse ? "if 〜: / else:" : "if 〜:");
            if (Level.Structures.Contains(NodeKind.While)) words.Add("while not 〜:");
            foreach (var c in Level.Conditions) words.Add(ConditionInfo.Get(c).PythonCode);
            if (Level.AllowVariables) words.AddRange(new[] { "n = read_sign()", "n = n - 1", "range(n)", "while n > 0:" });
            if (Level.AllowFunctions) words.AddRange(new[] { "def turn_corner():", "turn_corner()" });
            int cols = Mathf.Max(3, (words.Count + 3) / 4);
            float colW = (refRect.width - 16) / cols;
            for (int i = 0; i < words.Count; i++)
                UIKit.Label(new Rect(refRect.x + 8 + (i / 4) * colW, refRect.y + 4 + (i % 4) * 21, colW, 22), words[i], UIKit.CodeSmall);
        }

        /// <summary>Tab でスペース4つ、「:」の後の Enter で自動字下げ</summary>
        private void HandlePythonKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || GUI.GetNameOfFocusedControl() != PyField) return;
            var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (te == null) return;

            if (e.keyCode == KeyCode.Tab || e.character == '\t')
            {
                if (e.keyCode == KeyCode.Tab) te.ReplaceSelection("    ");
                Session.PythonText = te.text;
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.character == '\n')
            {
                if (e.keyCode != KeyCode.None)
                {
                    string text = te.text;
                    int pos = Mathf.Clamp(te.cursorIndex, 0, text.Length);
                    int lineStart = pos == 0 ? 0 : text.LastIndexOf('\n', pos - 1) + 1;
                    string line = text.Substring(lineStart, pos - lineStart);
                    int indent = 0;
                    while (indent < line.Length && line[indent] == ' ') indent++;
                    if (line.TrimEnd().EndsWith(":")) indent += 4;
                    te.ReplaceSelection("\n" + new string(' ', indent));
                    Session.PythonText = te.text;
                }
                e.Use();
            }
        }

        /// <summary>Python のコードを1行ずつ表示（今の行・エラーの行に色をつける）</summary>
        private void DrawListing(Rect r, int currentLine)
        {
            UIKit.Fill(r, UIKit.SubPanelColor);
            var rows = Session.PythonText.Replace("\r", "").Split('\n');
            var errorLines = new HashSet<int>();
            if (outcome != null) foreach (var e in outcome.Errors) errorLines.Add(e.Line);
            int failed = phase == Phase.Result && outcome?.Run?.FailedNode != null ? outcome.Run.FailedNode.Line : -1;

            var view = new Rect(r.x + 10, r.y + 10, r.width - 20, r.height - 20);
            const float rowH = 26f;
            if (currentLine > 0)
            {
                float target = (currentLine - 1) * rowH * UIKit.Scale;
                if (target < listScroll.y || target > listScroll.y + (view.height - rowH * 2) * UIKit.Scale)
                    listScroll.y = Mathf.Max(0, target - view.height * 0.4f * UIKit.Scale);
            }
            listScroll = UIKit.BeginScrollView(view, listScroll, new Rect(0, 0, view.width - 20, rows.Length * rowH + 8));
            for (int i = 0; i < rows.Length; i++)
            {
                int line = i + 1;
                var rr = new Rect(0, i * rowH, view.width - 20, rowH);
                if (line == currentLine) UIKit.Fill(rr, new Color(1f, 0.85f, 0.2f, 0.45f));
                else if (errorLines.Contains(line)) UIKit.Fill(rr, new Color(1f, 0.3f, 0.3f, 0.45f));
                else if (line == failed) UIKit.Fill(rr, new Color(1f, 0.55f, 0.2f, 0.45f));
                UIKit.Label(new Rect(4, rr.y + 2, 40, rowH), line.ToString().PadLeft(2), UIKit.CodeSmall);
                UIKit.Label(new Rect(48, rr.y + 2, rr.width - 48, rowH), rows[i], UIKit.CodeSmall);
            }
            UIKit.EndScrollView();
        }

        private void DrawEditButtons()
        {
            if (UIKit.ColorButton(new Rect(590, BottomY, 300, BottomH), "▶ 起動", UIKit.SuccessColor, UIKit.BigButton))
                Launch();
            if (UIKit.ColorButton(new Rect(900, BottomY, 160, BottomH), "ぜんぶ消す", Color.gray))
            {
                Session.Blocks.Clear();
                Session.PythonText = QuestSession.PythonTemplate;
                GUIUtility.keyboardControl = 0;
                note = null;
            }
            if (UIKit.ColorButton(new Rect(1070, BottomY, 160, BottomH), "説明を見る", Color.gray))
            {
                scratch.CancelDrag();
                StartIntro(true);
            }
        }

        private void DrawResult()
        {
            var r = ResultRect;
            UIKit.Fill(r, UIKit.SubPanelColor);
            var inner = new Rect(r.x + 14, r.y + 6, r.width - 28, r.height - 12);

            string title, kindNote;
            Color color;
            switch (outcome.Kind)
            {
                case LaunchKind.Success:
                    title = "クリア！ " + QuestTheme.GoalDone[(int)Level.Theme.Goal];
                    kindNote = Level.Tests.Count > 1 ? "3つのテストすべてで成功しました。" : "ゴーレムは最後までやりとげました。";
                    kindNote += "\n" + StarResultText();
                    if (rankUp) kindNote += "\n" + UIKit.Colorize($"ランクが {PlayerStats.Rank} に上がった！ 体力がふえた！", UIKit.SuccessColor);
                    if (rewardText != null) kindNote += "\n" + UIKit.Colorize(rewardText, UIKit.WarnColor);
                    color = UIKit.SuccessColor;
                    break;
                case LaunchKind.RuleBreak:
                    title = "ルール違反：しばりをこえています";
                    kindNote = "おまけのしばりより多くブロックを置いているので、ゴーレムは動きませんでした。";
                    color = UIKit.ErrorColor;
                    break;
                case LaunchKind.CodeError:
                    title = "エラー：命令を読めませんでした";
                    kindNote = "書き方にまちがいがあります。ゴーレムは動いていません。";
                    color = UIKit.ErrorColor;
                    break;
                default:
                    title = "条件未達成：命令は動いたけれど…";
                    kindNote = "書き方は正しいけれど、目標にはとどきませんでした。";
                    color = UIKit.WarnColor;
                    break;
            }
            UIKit.Label(new Rect(inner.x, inner.y, inner.width, 44), UIKit.Colorize(title, color), UIKit.Heading);

            var sb = new System.Text.StringBuilder();
            sb.Append(kindNote).Append('\n');
            if (outcome.Kind == LaunchKind.CodeError)
            {
                foreach (var e in outcome.Errors)
                {
                    string where = e.Line > 0 ? $"{e.Line}行目: " : "";
                    sb.Append(UIKit.Colorize("● " + where + e.Message, UIKit.ErrorColor)).Append('\n');
                    sb.Append("　→ ").Append(e.Fix).Append('\n');
                }
            }
            else if (outcome.Kind == LaunchKind.RuleBreak)
            {
                foreach (var b in outcome.RuleBreaks) sb.Append(UIKit.Colorize("● " + b, UIKit.ErrorColor)).Append('\n');
            }
            else if (outcome.Kind == LaunchKind.ConditionFail)
            {
                sb.Append(UIKit.Colorize("● " + outcome.Run.FailMessage, UIKit.WarnColor)).Append('\n');
            }
            if (outcome.Kind != LaunchKind.Success)
            {
                if (outcome.QuestOver) sb.Append('\n').Append("挑戦回数を使い切りました。解説を見て、考え方を確かめよう。");
                else
                {
                    sb.Append('\n').Append(UIKit.Colorize("ヒント: ", UIKit.AccentColor)).Append(outcome.Hint);
                    sb.Append('\n').Append(UIKit.Colorize($"あと {Session.AttemptsLeft}回 ためせるよ。まちがえて直すのも、大事な練習！", new Color(1f, 1f, 1f, 0.8f)));
                }
            }

            var view = new Rect(inner.x, inner.y + 46, inner.width, inner.height - 46);
            string text = sb.ToString();
            float h = UIKit.CalcHeight(text, UIKit.Body, view.width - 20) + 8;
            resultScroll = UIKit.BeginScrollView(view, resultScroll, new Rect(0, 0, view.width - 20, h));
            UIKit.Label(new Rect(0, 0, view.width - 20, h), text, UIKit.Body);
            UIKit.EndScrollView();
        }

        /// <summary>結果の星：3つの条件の できた・できない と、前の記録からどう変わったか</summary>
        private string StarResultText()
        {
            var gold = new Color(1f, 0.85f, 0.2f);
            var conds = QuestStars.Conditions(Level);
            var sb = new System.Text.StringBuilder(UIKit.Colorize(QuestStars.Mark(earnedStars), gold));
            if (earnedStars == 3) sb.Append("　星3つ！ すごい！");
            for (int i = 0; i < conds.Length; i++)
            {
                bool ok = earnedFlags != null && earnedFlags[i];
                sb.Append('\n').Append(ok ? UIKit.Colorize("✓ ", UIKit.SuccessColor) : UIKit.Colorize("✗ ", new Color(1f, 1f, 1f, 0.45f)))
                  .Append(UIKit.Colorize(conds[i], ok ? Color.white : new Color(1f, 1f, 1f, 0.55f)));
            }
            if (wasPassed) sb.Append('\n').Append(UIKit.Colorize("前は「通過」だったけど、クリアしたので 星がもらえた！", UIKit.SuccessColor));
            else if (prevStars > 0 && earnedStars > prevStars) sb.Append('\n').Append(UIKit.Colorize($"記録を更新！ {QuestStars.Mark(prevStars)} → {QuestStars.Mark(earnedStars)}", UIKit.SuccessColor));
            else if (prevStars > earnedStars) sb.Append('\n').Append(UIKit.Colorize($"いままでの記録 {QuestStars.Mark(prevStars)} は そのまま", new Color(1f, 1f, 1f, 0.6f)));
            return sb.ToString();
        }

        private void DrawResultButtons()
        {
            if (outcome.Kind == LaunchKind.Success || outcome.QuestOver)
            {
                if (UIKit.ColorButton(new Rect(590, BottomY, 360, BottomH), "解説を見る →", UIKit.AccentColor, UIKit.BigButton, primary: true))
                {
                    phase = Phase.Explanation;
                    page = 0;
                    shownPage = -1;
                    answerScroll = Vector2.zero;
                }
            }
            else if (UIKit.ColorButton(new Rect(590, BottomY, 360, BottomH), "命令を直して もう一度", UIKit.AccentColor, UIKit.BigButton, primary: true))
            {
                phase = Phase.Edit;
                note = null;
            }
        }

        private void Launch()
        {
            GUIUtility.keyboardControl = 0;
            scratch.CancelDrag();
            note = null;
            rewardText = null;
            outcome = Session.Launch();
            if (outcome.Kind == LaunchKind.CodeError || outcome.Kind == LaunchKind.RuleBreak) SoundKit.Miss(); // ゴーレムは動かない
            else SoundKit.Launch();
            stepIndex = 0;
            stepTimer = 0f;
            listScroll = Vector2.zero;
            resultScroll = Vector2.zero;
            phase = outcome.Kind == LaunchKind.CodeError || outcome.Kind == LaunchKind.RuleBreak ? Phase.Result : Phase.Running;
        }

        private void FinishRun()
        {
            phase = Phase.Result;
            if (outcome.Kind != LaunchKind.Success)
            {
                SoundKit.QuestFail();
                return;
            }
            SoundKit.QuestClear();
            // 星：条件ごとに1こ（★クリア ／ ★1回目の起動でクリア ／ ★命令の数がお手本以下）
            earnedFlags = QuestStars.Earned(Level, Session.AttemptsUsed, outcome.Program);
            earnedStars = QuestStars.Count(earnedFlags);
            prevStars = Progress.StarsOf(Level.Number);
            wasPassed = Progress.StatusOf(Level.Number) == Progress.Passed;
            int rankBefore = PlayerStats.Rank;
            Progress.MarkCleared(Level.Number, earnedStars);
            rankUp = PlayerStats.Rank > rankBefore;
            rewardText = null;
            if (Level.Theme.HasItems)
            {
                int got = Level.Tests[0].CountItems();
                Progress.AddItem(Level.Theme.Item, got);
                rewardText = $"{Level.Theme.ItemName}を {got}こ 手に入れた！（町のポケットで見られるよ）";
            }
            if (Session.IsSimilarProblem) Progress.AddSimilarClear();
            string extra = OnCleared?.Invoke(Level.Number, earnedStars);
            if (extra != null) rewardText = rewardText == null ? extra : rewardText + "\n" + extra;
        }

        // ----- Explanation -------------------------------------------------
        //   1ページ目：解説（左に文章、右に ブロックの絵）
        //   2ページ目：お手本（左に プログラムの絵、右に お手本で ゴーレムが動くアニメーション）
        //   3ページ目：結果（とった星・紙ふぶき）と「もう一度する／クエストを終える」

        private int shownPage = -1;       // アニメーションを始めたページ（ページが変わったら 最初から）
        private float pageStart;          // そのページを開いた時間
        private RunResult answerRun;      // お手本で動かした記録（2ページ目のアニメーション）
        private Level answerRunLevel;
        private bool answerAsPython;      // お手本を Python で見る
        private int starsSounded;         // 3ページ目：音を鳴らした星の数

        /// <summary>説明ページの絵（ブロックの絵。まとめる前 ▼ まとめたあと もある）</summary>
        private void DrawInfoPicture(Rect pic, InfoPage p)
        {
            UIKit.Fill(pic, new Color(0.95f, 0.95f, 0.97f));
            var mode = Progress.HasChosenMode ? Session.Mode : SolveMode.Scratch;
            var ink = new Color(0.25f, 0.25f, 0.35f);
            if (p.Before != null)
            {
                float half = (pic.height - 40) / 2f;
                UIKit.Label(new Rect(pic.x + 10, pic.y + 4, pic.width - 20, 26), UIKit.Colorize("まとめる前", ink), UIKit.Small);
                ProgramPicture.Draw(new Rect(pic.x + 12, pic.y + 30, pic.width - 24, half - 30), p.Before, mode);
                UIKit.Label(new Rect(pic.x, pic.y + half, pic.width, 40), UIKit.Colorize("▼", new Color(0.8f, 0.45f, 0f)), UIKit.Center);
                UIKit.Label(new Rect(pic.x + 10, pic.y + half + 36, pic.width - 20, 26), UIKit.Colorize("まとめたあと：" + p.Caption, ink), UIKit.Small);
                ProgramPicture.Draw(new Rect(pic.x + 12, pic.y + half + 62, pic.width - 24, half - 26), p.Example, mode);
            }
            else
            {
                UIKit.Label(new Rect(pic.x + 10, pic.y + 4, pic.width - 20, 26), UIKit.Colorize("お手本：" + p.Caption, ink), UIKit.Small);
                ProgramPicture.Draw(new Rect(pic.x + 12, pic.y + 34, pic.width - 24, pic.height - 44), p.Example, mode);
            }
        }

        private void DrawExplanation()
        {
            if (page != shownPage)
            {
                shownPage = page;
                pageStart = Time.time;
                starsSounded = 0;
            }
            var body = new Rect(90, 90, 1100, 506);
            UIKit.Panel(body, UIKit.SubPanelColor);
            var inner = new Rect(body.x + 30, body.y + 14, body.width - 60, body.height - 28);
            string head = Session.Cleared ? UIKit.Colorize("クリアおめでとう！ ", UIKit.SuccessColor) : UIKit.Colorize("挑戦終了。 ", UIKit.WarnColor);

            if (page == 0)
            {
                var ex = Level.Chapter.Explanation;
                UIKit.Label(new Rect(inner.x, inner.y, inner.width, 48), head + ex.Title, UIKit.Heading);
                string text = ex.Body + "\n\n" + UIKit.Colorize("このステージのポイント：", UIKit.AccentColor) + Level.Tip;
                // 右に絵：今のブロックの使い方（なければ このステージの地図）
                var how = ConceptExamples.For(QuestDatabase.LatestConcept(Level.Template));
                ExplainText(new Rect(inner.x, inner.y + 58, 590, inner.height - 96), text);
                var pic = new Rect(inner.x + 610, inner.y + 54, inner.width - 610, inner.height - 92);
                if (how != null) DrawInfoPicture(pic, how);
                else
                {
                    var map = Level.Tests[0];
                    UIKit.Label(new Rect(pic.x, pic.y, pic.width, 26), UIKit.Colorize("このステージの地図", UIKit.AccentColor), UIKit.Small);
                    GridView.Draw(new Rect(pic.x, pic.y + 30, pic.width, pic.height - 30), map, map.Start, map.StartDir, false, false, Level);
                }
            }
            else if (page == 1)
            {
                DrawModelAnswer(inner);
            }
            else
            {
                DrawSummaryAndChoice(body, inner);
                return;
            }

            UIKit.Label(new Rect(body.x, body.yMax - 34, body.width, 30), $"{page + 1} / 3", UIKit.SmallCenter);
            if (page > 0 && UIKit.ColorButton(new Rect(90, 606, 200, 52), "← もどる", Color.gray, UIKit.BigButton))
                page--;
            if (UIKit.ColorButton(new Rect(890, 606, 300, 52), "つぎへ →", UIKit.AccentColor, UIKit.BigButton, primary: true))
                page++;
        }

        /// <summary>お手本：左に プログラムの絵、右に お手本で ゴーレムが動くようす（くり返し流れる）</summary>
        private void DrawModelAnswer(Rect inner)
        {
            UIKit.Label(new Rect(inner.x, inner.y, inner.width, 48), "お手本の答え", UIKit.Heading);
            UIKit.Label(new Rect(inner.x, inner.y + 44, inner.width - 260, 36), "同じ動きをすれば、ちがう書き方でも正解です。右の絵で 動きをたしかめよう。", UIKit.Body);
            if (UIKit.ColorButton(new Rect(inner.xMax - 250, inner.y + 6, 250, 40), answerAsPython ? "ブロックで見る" : "Python で見る", Color.gray, UIKit.SmallButton))
                answerAsPython = !answerAsPython;

            // 左：お手本のプログラム
            var prog = new Rect(inner.x, inner.y + 84, 470, inner.height - 124);
            UIKit.Fill(prog, new Color(0.95f, 0.95f, 0.97f));
            ProgramPicture.Draw(new Rect(prog.x + 10, prog.y + 10, prog.width - 20, prog.height - 20), Level.Solution, answerAsPython ? SolveMode.Python : SolveMode.Scratch);

            // 右：お手本で動かしたようす（1つ目のテスト）。さいごまで行ったら 少し止まって また最初から
            if (answerRun == null || answerRunLevel != Level)
            {
                answerRun = GolemRunner.Run(Level, Level.Solution);
                answerRunLevel = Level;
            }
            var steps = new List<RunStep>();
            foreach (var s in answerRun.Steps) if (s.Test == 0) steps.Add(s);
            var grid = new Rect(inner.x + 490, inner.y + 84, inner.width - 490, inner.height - 160);
            if (steps.Count > 0)
            {
                float stepSec = Mathf.Clamp(10f / steps.Count, 0.15f, 0.5f);
                int loop = steps.Count + Mathf.CeilToInt(1.5f / stepSec); // さいごで 1.5秒 止まる
                int i = Mathf.Min(steps.Count - 1, Mathf.FloorToInt((Time.time - pageStart) / stepSec) % loop);
                var st = steps[i];
                GridView.Draw(grid, st.Map, st.Pos, st.Dir, st.Done, false, Level);
                string log = i == steps.Count - 1 ? UIKit.Colorize("ゴール！", UIKit.SuccessColor) + "（また最初から 動くよ）" : st.Log ?? "";
                UIKit.Label(new Rect(grid.x, grid.yMax + 4, grid.width, 30), $"{i + 1} / {steps.Count}　" + log, UIKit.Small);
            }
        }

        // ----- 星の絵（プログラムでかく。1回だけ作る） -----
        private static Texture2D starTex;

        private static Texture2D StarTexture()
        {
            if (starTex != null) return starTex;
            const int S = 96;
            starTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            // 5つのとんがりの星（外の半径と 内の半径を 交互に結ぶ形）の中か調べる。4×4 でぼかして ふちを なめらかに
            var pts = new Vector2[10];
            for (int k = 0; k < 10; k++)
            {
                float a = Mathf.PI / 2f + k * Mathf.PI / 5f;
                float rad = (k % 2 == 0 ? 0.48f : 0.2f) * S;
                pts[k] = new Vector2(S / 2f + Mathf.Cos(a) * rad, S / 2f + Mathf.Sin(a) * rad - S * 0.03f);
            }
            bool Inside(Vector2 p)
            {
                bool c = false;
                for (int a = 0, b = 9; a < 10; b = a++)
                    if ((pts[a].y > p.y) != (pts[b].y > p.y) && p.x < (pts[b].x - pts[a].x) * (p.y - pts[a].y) / (pts[b].y - pts[a].y) + pts[a].x) c = !c;
                return c;
            }
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
                        if (Inside(new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))) hit++;
                    starTex.SetPixel(x, y, new Color(1f, 1f, 1f, hit / 16f));
                }
            starTex.Apply();
            return starTex;
        }

        /// <summary>はねるように 出てくる大きさ（0 → 1.25 → 1）</summary>
        private static float Pop(float t)
        {
            if (t <= 0f) return 0f;
            if (t < 0.25f) return Mathf.Lerp(0f, 1.25f, t / 0.25f);
            if (t < 0.45f) return Mathf.Lerp(1.25f, 1f, (t - 0.25f) / 0.2f);
            return 1f;
        }

        /// <summary>紙ふぶき（クリアしたとき）</summary>
        private static void DrawConfetti(Rect area, float time)
        {
            for (int i = 0; i < 46; i++)
            {
                float speed = 60f + (i * 37 % 50);
                float x = area.x + (i * 137 % 1000) / 1000f * area.width + Mathf.Sin(time * 2f + i) * 14f;
                float y = area.y + (time * speed + i * 53) % area.height;
                var c = Color.HSVToRGB((i * 0.137f) % 1f, 0.6f, 1f);
                c.a = 0.85f;
                float w = 6f + (i % 3) * 2f, h = 10f + Mathf.Abs(Mathf.Sin(time * 4f + i)) * 6f; // くるくる回って 見える
                UIKit.Fill(new Rect(x, y, w, h), c);
            }
        }

        /// <summary>結果と「次はどうする？」：とった星を大きく出して お祝い</summary>
        private void DrawSummaryAndChoice(Rect body, Rect inner)
        {
            float t = Time.time - pageStart;
            bool cleared = Session.Cleared;
            var gold = new Color(1f, 0.82f, 0.2f);
            if (cleared)
            {
                // うしろを 少し明るく ＋ 紙ふぶき
                UIKit.Fill(body, new Color(1f, 0.85f, 0.4f, 0.06f + 0.03f * Mathf.Sin(t * 3f)));
                DrawConfetti(body, t);
            }

            string title = !cleared ? UIKit.Colorize("挑戦終了！ つぎは クリアを目指そう", UIKit.WarnColor)
                : earnedStars == 3 ? UIKit.Colorize("パーフェクト！ 星3つ！", gold)
                : UIKit.Colorize("クエストクリア！", UIKit.SuccessColor);
            UIKit.Label(new Rect(inner.x, inner.y - 4, inner.width, 56), title, UIKit.Title);

            // 星3つ：1つずつ はねて出てくる（とれなかった星は かげ）
            var conds = QuestStars.Conditions(Level);
            var tex = StarTexture();
            const float size = 104f, gap = 250f;
            float cx0 = inner.center.x - gap;
            for (int i = 0; i < 3; i++)
            {
                bool got = cleared && earnedFlags != null && i < earnedFlags.Length && earnedFlags[i];
                float appear = 0.35f + i * 0.45f;
                float s = Pop((t - appear) / 0.8f);
                if (got && t >= appear && starsSounded <= i)
                {
                    starsSounded = i + 1;
                    SoundKit.Coin(); // 星が出るたびに キラン
                }
                var center = new Vector2(cx0 + i * gap, inner.y + 116);
                // かげ（台）は いつも出す
                UIKit.TextureTinted(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), tex, new Color(0f, 0f, 0f, 0.35f));
                if (s > 0f && got)
                {
                    float glow = size * s * (1.35f + 0.08f * Mathf.Sin(t * 5f + i));
                    UIKit.TextureTinted(new Rect(center.x - glow / 2f, center.y - glow / 2f, glow, glow), tex, new Color(1f, 0.9f, 0.4f, 0.3f));
                    float d = size * s;
                    UIKit.TextureTinted(new Rect(center.x - d / 2f, center.y - d / 2f, d, d), tex, gold);
                    // キラッと光る 小さな星
                    float tw = 0.5f + 0.5f * Mathf.Sin(t * 6f + i * 2f);
                    UIKit.TextureTinted(new Rect(center.x + size * 0.3f, center.y - size * 0.5f, 22f * tw, 22f * tw), tex, new Color(1f, 1f, 1f, 0.9f * tw));
                }
                // 星の下に 条件（とれたら ★）
                if (t >= appear)
                {
                    string cond = i < conds.Length ? conds[i] : "";
                    string line = got ? UIKit.Colorize("★ " + cond, UIKit.SuccessColor) : UIKit.Colorize("☆ " + cond, new Color(1f, 1f, 1f, 0.55f));
                    UIKit.Label(new Rect(center.x - 120, center.y + size / 2f + 2, 240, 54), line, UIKit.Center);
                }
            }

            // ひとこと（星がそろってから）
            float after = 0.35f + 3 * 0.45f;
            if (t >= after)
            {
                var sb = new System.Text.StringBuilder();
                if (!cleared) sb.Append("お手本を見れば大丈夫。もう一度してみよう（問題が少しだけ変わるよ）。");
                else
                {
                    sb.Append($"{Session.AttemptsUsed} 回目の起動でクリア！ ");
                    if (wasPassed) sb.Append(UIKit.Colorize("前は「通過」だったけど、星がもらえた！ ", UIKit.SuccessColor));
                    else if (prevStars > 0 && earnedStars > prevStars) sb.Append(UIKit.Colorize($"記録を更新！ {QuestStars.Mark(prevStars)} → {QuestStars.Mark(earnedStars)} ", UIKit.SuccessColor));
                    else if (earnedStars < 3) sb.Append("のこりの星も ねらってみよう！ ");
                    if (rankUp) sb.Append(UIKit.Colorize($"ランクが {PlayerStats.Rank} に上がった！ ", UIKit.SuccessColor));
                    if (rewardText != null) sb.Append('\n').Append(UIKit.Colorize(rewardText, UIKit.WarnColor));
                }
                sb.Append('\n').Append(UIKit.Colorize("プログラムは ここで役立つ：", UIKit.AccentColor)).Append(Level.Chapter.RealWorld);
                ExplainBody(new Rect(inner.x + 20, inner.y + 226, inner.width - 40, 140), sb.ToString());
            }

            // ボタン（ふわふわ ゆれて 押したくなる）
            float bob = 3f * Mathf.Sin(t * 3f);
            if (UIKit.ColorButton(new Rect(inner.x + 40, inner.y + 376, 460, 86),
                    "もう一度する\n（問題が少しだけ変わる）", UIKit.AccentColor, UIKit.Button))
            {
                Session.StartLevel(Level.Number, Level.Variant + 1);
                outcome = null;
                previewTest = 0;
                phase = Phase.Edit;
                note = Level.ChangesOnRetry
                    ? "もう一度：考え方は同じ。じゃまものの場所や道の長さが少し変わったよ！（挑戦回数はもどったよ）"
                    : "もう一度：考え方は同じ。長さや配置が少し変わったよ！（挑戦回数はもどったよ）";
            }
            string nextLabel = "クエストを終える\n（資材を持って帰ろう）";
            if (UIKit.ColorButton(new Rect(inner.x + 540, inner.y + 376 + bob, 460, 86), nextLabel, UIKit.SuccessColor, UIKit.Button, primary: true))
                ProceedNext();

            if (UIKit.ColorButton(new Rect(90, 606, 200, 52), "← もどる", Color.gray, UIKit.BigButton))
                page--;
        }

        /// <summary>結果ページの 小さめの文章（長いときはスクロール）</summary>
        private void ExplainBody(Rect r, string text)
        {
            string key = phase + ":" + page;
            if (key != textScrollKey) { textScroll = Vector2.zero; textScrollKey = key; }
            textScroll = UIKit.ScrollText(r, textScroll, text, UIKit.Body);
        }

        /// <summary>クエストを終えて、エリア（または町）にもどる。クリアできなかったときも「解説を見て通過」として次のステージを開ける</summary>
        private void ProceedNext()
        {
            if (!Session.Cleared) Progress.MarkPassed(Level.Number);
            OnProceed?.Invoke();
        }
    }
}
