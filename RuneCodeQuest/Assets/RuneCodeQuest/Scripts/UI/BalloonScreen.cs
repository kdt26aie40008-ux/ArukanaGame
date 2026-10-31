using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 風船割り（タイピングの練習）。
    ///
    /// ・1分のあいだ、風船に書かれた言葉を打つ。正しく1文字打つごとに風船がふくらみ、
    ///   最後まで打つと限界までふくらんで「パーン！」と割れる。すぐに次の風船が出てくる
    /// ・言葉は、割った数が増えるほど長くなる（Balloon.PickWord）
    /// ・終わったら「割った風船の合計・正しく打てた数・まちがえた数」と、もらえたほうびを出す
    /// ・ほうび：素材（何回でも。レアな素材ほど、たくさん割らないともらえない）と、
    ///   これまでの合計で もらえるキャラクリの服・ぼうし（1回だけ）
    /// </summary>
    public class BalloonScreen
    {
        private enum Phase { Ready, Play, Result }

        /// <summary>「もどる」を押したとき</summary>
        public System.Action OnClose;

        private Phase phase;
        private float timer;          // Ready：カウントダウン　Play：経過時間
        private int popped, correctKeys, missKeys;
        private bool japanese;
        private TypingWord word;
        private RomajiMatcher matcher;
        private int typedCount;       // 英語のとき、打てた文字数
        private float popTimer;       // 割れた演出の残り時間
        private Color balloonColor, poppedColor;
        private float missFlash;
        private string note;
        private readonly System.Random rng = new System.Random();
        private readonly List<string> recent = new List<string>(); // さっき出た言葉（しばらく出さない）

        // 結果
        private int[] rewards;
        private readonly List<Balloon.Cosmetic> unlocked = new List<Balloon.Cosmetic>();
        private bool newBest;

        private static readonly Color[] Colors =
        {
            new Color(0.95f, 0.35f, 0.40f), new Color(0.35f, 0.65f, 1f), new Color(0.45f, 0.85f, 0.45f),
            new Color(1f, 0.80f, 0.25f), new Color(0.75f, 0.45f, 0.95f), new Color(1f, 0.55f, 0.20f),
        };

        public void Start()
        {
            phase = Phase.Ready;
            timer = 3f;
            popped = correctKeys = missKeys = 0;
            popTimer = 0f;
            note = null;
            japanese = Progress.TypingIsJapanese;
            recent.Clear();
            NextWord();
            SoundKit.BalloonBgm();
            try { Input.imeCompositionMode = IMECompositionMode.Off; } catch { /* 新しい Input System だけのときは何もしない */ }
        }

        private void NextWord()
        {
            word = Balloon.PickWord(japanese, popped, rng, recent);
            matcher = japanese ? new RomajiMatcher(word.Reading) : null;
            typedCount = 0;
            balloonColor = Colors[rng.Next(Colors.Length)];
        }

        /// <summary>打った割合（0〜1）。風船の大きさになる</summary>
        private float Progress01
        {
            get
            {
                if (japanese)
                {
                    int done = matcher.Typed.Length, rest = matcher.Remaining.Length;
                    return done + rest == 0 ? 0 : done / (float)(done + rest);
                }
                return word.Reading.Length == 0 ? 0 : typedCount / (float)word.Reading.Length;
            }
        }

        public void Update()
        {
            float dt = Time.deltaTime;
            if (missFlash > 0) missFlash -= dt;
            if (popTimer > 0) popTimer -= dt;
            switch (phase)
            {
                case Phase.Ready:
                    timer -= dt;
                    if (timer <= 0f) { phase = Phase.Play; timer = 0f; }
                    break;
                case Phase.Play:
                    timer += dt;
                    if (timer >= Balloon.TimeLimit) Finish();
                    break;
                case Phase.Result:
                    UpdateResult(dt);
                    break;
            }
        }

        // ------------------------------------------------------------------
        // 入力
        // ------------------------------------------------------------------

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Escape) { e.Use(); Finish(); return; }
            char c = e.character;
            if (c == '\0' || c == '\n' || c == '\r' || c == '\t') return;
            e.Use();
            if (c > 127)
            {
                note = "日本語入力（IME）がオンになっているよ。半角英数にしてから、ローマ字で打とう。";
                return;
            }

            bool ok, finished;
            if (japanese)
            {
                ok = matcher.Type(c);
                finished = matcher.Done;
            }
            else
            {
                ok = typedCount < word.Reading.Length && word.Reading[typedCount] == c;
                if (ok) typedCount++;
                finished = typedCount >= word.Reading.Length;
            }
            if (!ok)
            {
                missKeys++;
                missFlash = 0.25f;
                SoundKit.Miss();
                return;
            }
            correctKeys++;
            SoundKit.Key();
            note = null;
            if (finished)
            {
                // 限界までふくらんで、パーン！
                popped++;
                popTimer = 0.4f;
                SoundKit.Pop();
                poppedColor = balloonColor;
                NextWord();
            }
        }

        private void Finish()
        {
            phase = Phase.Result;
            SoundKit.StopBgm();
            SoundKit.Chime();
            rewards = Balloon.Rewards(popped);
            for (int i = 0; i < rewards.Length; i++) Progress.AddCraft((CraftMaterial)i, rewards[i]);
            int before = Progress.TotalBalloons;
            Progress.TotalBalloons = before + popped;
            unlocked.Clear();
            unlocked.AddRange(Balloon.NewlyUnlocked(before, before + popped));
            newBest = popped > Progress.BestBalloons;
            if (newBest) Progress.BestBalloons = popped;
            KeyInput.Clear();
            StartResultShow();
        }

        // ------------------------------------------------------------------
        // 画面
        // ------------------------------------------------------------------

        public void Draw()
        {
            UIKit.Nav("balloon-" + phase, phase == Phase.Result);
            if (phase == Phase.Play) HandleKeys();
            if (phase == Phase.Result)
            {
                DrawResult();
                return;
            }

            // 空
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.55f, 0.78f, 0.98f));
            UIKit.Fill(new Rect(0, 560, UIKit.RefWidth, 160), new Color(0.45f, 0.75f, 0.45f));
            if (missFlash > 0) UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(1f, 0.2f, 0.2f, missFlash));

            // 上：のこり時間と、割った数
            float left = Mathf.Max(0f, Balloon.TimeLimit - (phase == Phase.Play ? timer : phase == Phase.Result ? Balloon.TimeLimit : 0f));
            UIKit.Fill(new Rect(40, 20, 1200, 60), new Color(0f, 0f, 0f, 0.45f));
            UIKit.Label(new Rect(60, 24, 400, 52), $"のこり {left:0}秒", UIKit.Heading);
            UIKit.Fill(new Rect(300, 44, 560, 14), new Color(0f, 0f, 0f, 0.5f));
            UIKit.Fill(new Rect(300, 44, 560 * left / Balloon.TimeLimit, 14), left < 10 ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 0.85f, 0.3f));
            UIKit.Label(new Rect(900, 24, 330, 52), $"割った風船：{popped}こ", UIKit.Heading);
            SoundKit.ToggleButton(new Rect(1120, 88, 120, 36));

            switch (phase)
            {
                case Phase.Ready:
                    UIKit.Label(new Rect(0, 250, UIKit.RefWidth, 100), Mathf.CeilToInt(timer).ToString(), UIKit.Huge);
                    UIKit.Label(new Rect(0, 360, UIKit.RefWidth, 60), "風船の言葉を打って、どんどん割ろう！ 1分でいくつ割れるかな？", UIKit.Center);
                    break;
                case Phase.Play:
                    DrawBalloon();
                    break;
                default:
                    DrawResult();
                    break;
            }
        }

        private void DrawBalloon()
        {
            var center = new Vector2(640, 300);

            // 割れたしゅんかんの紙ふぶき
            if (popTimer > 0)
            {
                float t = 1f - popTimer / 0.4f;
                for (int i = 0; i < 24; i++)
                {
                    float a = i * Mathf.PI * 2f / 24f;
                    var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (60 + 220 * t);
                    UIKit.Fill(new Rect(p.x - 6, p.y - 6, 12, 12), i % 2 == 0 ? poppedColor : Colors[i % Colors.Length]);
                }
                UIKit.Label(new Rect(0, center.y - 40, UIKit.RefWidth, 80), UIKit.Colorize("パーン！", Color.white), UIKit.Huge);
                return;
            }

            // 打つほど ふくらむ（長い言葉の風船は、もとから少し大きい）
            float baseSize = 150 + Balloon.Tier(popped) * 20;
            float size = baseSize + 190 * Progress01;
            var r = new Rect(center.x - size / 2, center.y - size * 0.55f, size, size * 1.15f);
            UIKit.TextureTinted(r, SpriteFactory.TextureOf(TownArt.BalloonWhite), balloonColor);

            // 言葉（風船の上）とお手本（下）
            string display = word.Display;
            // 打ったところは緑になる（かなの言葉・英語は1文字ずつ。漢字の言葉は下の読みが緑になる）
            string typed, rest, big;
            if (japanese)
            {
                typed = matcher.Typed;
                rest = matcher.Remaining;
                bool kanaWord = !Furigana.HasKanji(display) && RomajiMatcher.ToHiragana(display).Length == matcher.Kana.Length;
                big = kanaWord ? UIKit.TypedText(display, matcher.KanaDone, UIKit.TypedColor, Color.white) : UIKit.Colorize(display, Color.white);
                if (display != word.Reading)
                    UIKit.Label(new Rect(0, 488, UIKit.RefWidth, 40),
                        word.Reading.Length == matcher.Kana.Length ? UIKit.TypedText(word.Reading, matcher.KanaDone, UIKit.TypedColor, Color.white) : word.Reading, UIKit.Center);
            }
            else
            {
                typed = word.Reading.Substring(0, typedCount);
                rest = word.Reading.Substring(typedCount);
                big = display == word.Reading ? UIKit.TypedText(display, typedCount, UIKit.TypedColor, Color.white) : UIKit.Colorize(display, Color.white);
            }
            UIKit.Label(new Rect(0, center.y - 70, UIKit.RefWidth, 110), big, display.Length > 9 ? UIKit.Title : UIKit.Huge);
            var guide = new Rect(240, 520, 800, 60);
            UIKit.Fill(guide, new Color(0f, 0f, 0f, 0.5f));
            UIKit.Label(guide, UIKit.Colorize(typed.Replace("<", "＜"), UIKit.TypedColor) + UIKit.Colorize(rest.Replace("<", "＜"), UIKit.WarnColor), UIKit.Support);
            if (note != null) UIKit.Label(new Rect(0, 590, UIKit.RefWidth, 40), note, UIKit.SmallCenter);
            UIKit.HelpBar("黄色の文字を打とう（緑は打てたところ）　Esc: 終わる");
        }

        // ------------------------------------------------------------------
        // 結果発表（数がカウントアップ → ランクのはんこ → 素材が1つずつ飛び出す。紙ふぶきと風船が舞う）
        // ------------------------------------------------------------------

        private struct Confetti { public Vector2 Pos, Vel; public Color Color; public float Size, Spin; }
        private struct Floater { public Vector2 Pos; public float Speed, Size; public Color Color; }

        private readonly List<Confetti> confetti = new List<Confetti>();
        private readonly List<Floater> floaters = new List<Floater>();
        private float resultTime;
        private int shownCount;     // カウントアップで出ている数
        private bool stamped;
        private int rewardShown;    // 飛び出した素材の数
        private bool cosmeticShown;

        private const float CountTime = 1.4f, StampAt = 1.7f, RewardAt = 2.2f, RewardGap = 0.35f;

        private string Rank => popped >= 30 ? "S" : popped >= 20 ? "A" : popped >= 10 ? "B" : "C";
        private Color RankColor => popped >= 30 ? new Color(1f, 0.78f, 0.15f) : popped >= 20 ? new Color(0.95f, 0.35f, 0.4f)
            : popped >= 10 ? new Color(0.35f, 0.6f, 1f) : new Color(0.4f, 0.8f, 0.45f);
        private string RankWord => popped >= 30 ? "すごすぎる！" : popped >= 20 ? "すばらしい！" : popped >= 10 ? "いいね！" : "もっと割れるよ！";

        private void StartResultShow()
        {
            resultTime = 0f;
            shownCount = 0;
            stamped = false;
            rewardShown = 0;
            cosmeticShown = false;
            confetti.Clear();
            floaters.Clear();
            for (int i = 0; i < 90; i++)
                confetti.Add(new Confetti
                {
                    Pos = new Vector2(rng.Next(0, 1280), -rng.Next(0, 600)),
                    Vel = new Vector2(rng.Next(-40, 40), rng.Next(120, 260)),
                    Color = Colors[rng.Next(Colors.Length)],
                    Size = rng.Next(8, 16),
                    Spin = (float)rng.NextDouble() * 6f,
                });
            for (int i = 0; i < 12; i++) floaters.Add(NewFloater(true));
        }

        private Floater NewFloater(bool anywhere) => new Floater
        {
            Pos = new Vector2(rng.Next(0, 1280), anywhere ? rng.Next(0, 760) : 760 + rng.Next(0, 200)),
            Speed = rng.Next(40, 110),
            Size = rng.Next(40, 90),
            Color = Colors[rng.Next(Colors.Length)],
        };

        private int RewardKinds
        {
            get
            {
                int n = 0;
                foreach (int r in rewards) if (r > 0) n++;
                return n;
            }
        }

        private void UpdateResult(float dt)
        {
            resultTime += dt;
            // カウントアップ（ピッピッ）
            int target = Mathf.FloorToInt(popped * Mathf.Clamp01(resultTime / CountTime));
            if (target > shownCount)
            {
                shownCount = target;
                SoundKit.Tick();
            }
            // ランクのはんこ（ドン！）
            if (!stamped && resultTime >= StampAt)
            {
                stamped = true;
                SoundKit.Stamp();
            }
            // 素材が1つずつ飛び出す（パーン）
            int kinds = RewardKinds;
            while (rewardShown < kinds && resultTime >= RewardAt + rewardShown * RewardGap)
            {
                rewardShown++;
                SoundKit.Pop();
            }
            // キャラクリのほうび（キラーン）
            if (!cosmeticShown && unlocked.Count > 0 && resultTime >= RewardAt + kinds * RewardGap + 0.3f)
            {
                cosmeticShown = true;
                SoundKit.QuestClear();
            }

            for (int i = 0; i < confetti.Count; i++)
            {
                var c = confetti[i];
                c.Pos += c.Vel * dt;
                c.Pos.x += Mathf.Sin(resultTime * 3f + c.Spin) * 30f * dt;
                if (c.Pos.y > 740 && resultTime < 5f) c.Pos = new Vector2(rng.Next(0, 1280), -20);
                confetti[i] = c;
            }
            for (int i = 0; i < floaters.Count; i++)
            {
                var f = floaters[i];
                f.Pos.y -= f.Speed * dt;
                if (f.Pos.y < -120) f = NewFloater(false);
                floaters[i] = f;
            }
        }

        /// <summary>まるい形（はんこ・数のわく）に使う白い円</summary>
        private static Texture2D circle;
        private static Texture2D Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 64;
                circle = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                        circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d)));
                    }
                circle.Apply();
                return circle;
            }
        }

        private void DrawResult()
        {
            // 空と、ふわふわ上がっていく風船
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.50f, 0.74f, 0.98f));
            UIKit.Fill(new Rect(0, 600, UIKit.RefWidth, 120), new Color(0.45f, 0.75f, 0.45f));
            foreach (var f in floaters)
                UIKit.TextureTinted(new Rect(f.Pos.x - f.Size / 2, f.Pos.y - f.Size * 0.6f, f.Size, f.Size * 1.15f), SpriteFactory.TextureOf(TownArt.BalloonWhite), new Color(f.Color.r, f.Color.g, f.Color.b, 0.55f));

            // 見出し（ぴょんとはねる）
            float bounce = Mathf.Abs(Mathf.Sin(resultTime * 6f)) * 14f * Mathf.Max(0, 1f - resultTime / 1.2f);
            UIKit.Label(new Rect(0, 14 - bounce, UIKit.RefWidth, 70), UIKit.Colorize("結果発表！", Color.white), UIKit.Title);

            var panel = new Rect(140, 90, 1000, 520);
            UIKit.Fill(panel, new Color(0f, 0f, 0f, 0.45f));

            // 割った風船の数（カウントアップ）
            var count = new Rect(panel.x + 40, panel.y + 20, 300, 300);
            UIKit.TextureTinted(count, Circle, new Color(1f, 1f, 1f, 0.95f));
            UIKit.TextureTinted(new Rect(count.x + 12, count.y + 12, count.width - 24, count.height - 24), Circle, new Color(1f, 0.92f, 0.5f));
            UIKit.Label(new Rect(count.x, count.y + 50, count.width, 40), UIKit.Colorize("割った風船", new Color(0.4f, 0.25f, 0.1f)), UIKit.Center);
            UIKit.Label(new Rect(count.x, count.y + 95, count.width, 110), UIKit.Colorize(shownCount.ToString(), new Color(0.85f, 0.3f, 0.2f)), UIKit.Huge);
            UIKit.Label(new Rect(count.x, count.y + 205, count.width, 40), UIKit.Colorize("こ", new Color(0.4f, 0.25f, 0.1f)), UIKit.Center);

            // ランクのはんこ（ドン！と大きく出て、小さくなる）
            if (stamped)
            {
                float t = Mathf.Clamp01((resultTime - StampAt) / 0.25f);
                float size = Mathf.Lerp(320, 170, t);
                var center = new Vector2(panel.xMax - 150, panel.y + 120);
                var r = new Rect(center.x - size / 2, center.y - size / 2, size, size);
                UIKit.TextureTinted(r, Circle, RankColor);
                UIKit.TextureTinted(new Rect(r.x + size * 0.08f, r.y + size * 0.08f, size * 0.84f, size * 0.84f), Circle, Color.Lerp(RankColor, Color.white, 0.25f));
                UIKit.Label(new Rect(r.x, r.y + size * 0.15f, r.width, size * 0.6f), UIKit.Colorize(Rank, Color.white), UIKit.Huge);
                UIKit.Label(new Rect(center.x - 150, r.yMax - 6, 300, 40), UIKit.Colorize(RankWord, Color.white), UIKit.Center);
            }

            // 新記録（ちかちか光る）
            if (newBest && resultTime > CountTime && Mathf.Repeat(resultTime, 0.6f) < 0.42f)
            {
                var nb = new Rect(panel.x + 360, panel.y + 30, 300, 56);
                UIKit.Fill(nb, new Color(1f, 0.8f, 0.2f));
                UIKit.Label(nb, UIKit.Colorize("★ 新記録！ ★", new Color(0.6f, 0.15f, 0.1f)), UIKit.Heading);
            }

            // 正しく打てた数・まちがえた数・正確さ
            int acc = correctKeys + missKeys == 0 ? 100 : correctKeys * 100 / (correctKeys + missKeys);
            StatBox(new Rect(panel.x + 360, panel.y + 110, 190, 110), "正しく打てた", correctKeys.ToString(), new Color(0.3f, 0.7f, 0.4f), 0.3f);
            StatBox(new Rect(panel.x + 560, panel.y + 110, 190, 110), "まちがえた", missKeys.ToString(), new Color(0.85f, 0.35f, 0.35f), 0.6f);
            StatBox(new Rect(panel.x + 360, panel.y + 230, 390, 70), "正確さ", $"{acc}%", new Color(0.3f, 0.55f, 0.9f), 0.9f, true);

            // もらった素材（1つずつ ぽんっと出る）
            UIKit.Label(new Rect(panel.x + 40, panel.y + 334, 600, 34), UIKit.Colorize("もらった素材", new Color(1f, 0.9f, 0.5f)), UIKit.Body);
            int shownKinds = 0;
            float x = panel.x + 40;
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i] <= 0) continue;
                if (shownKinds >= rewardShown) break;
                float age = resultTime - (RewardAt + shownKinds * RewardGap);
                float pop = Mathf.Clamp01(age / 0.18f);
                float s = Mathf.Lerp(90, 60, pop);
                var box = new Rect(x, panel.y + 372, 220, 76);
                UIKit.Fill(box, new Color(1f, 1f, 1f, 0.15f));
                UIKit.Texture(new Rect(box.x + 34 - s / 2, box.y + 38 - s / 2, s, s), SpriteFactory.TextureOf(TownArt.CraftIcon((CraftMaterial)i)));
                UIKit.Label(new Rect(box.x + 74, box.y + 6, 146, 32), Balloon.CraftNames[i], UIKit.Small);
                UIKit.Label(new Rect(box.x + 74, box.y + 34, 146, 40), UIKit.Colorize($"×{rewards[i]}", Color.white), UIKit.Heading);
                x += 232;
                shownKinds++;
            }
            if (RewardKinds == 0 && resultTime > RewardAt)
                UIKit.Label(new Rect(panel.x + 40, panel.y + 380, 900, 40), "なし（1回で5こ割ると、ルーン鉱石がもらえるよ）", UIKit.Body);

            // 次の目標
            if (resultTime > RewardAt + RewardKinds * RewardGap)
            {
                string next = null;
                for (int i = 0; i < Balloon.CraftKinds; i++)
                    if (popped < Balloon.CraftNeed[i]) { next = $"次は1回で {Balloon.CraftNeed[i]}こ割ると「{Balloon.CraftNames[i]}」がもらえる！"; break; }
                string cos = null;
                foreach (var c in Balloon.Cosmetics)
                    if (Progress.TotalBalloons < c.Need) { cos = $"これまでに割った風船 {Progress.TotalBalloons}こ（あと{c.Need - Progress.TotalBalloons}こで「{c.Name}」）"; break; }
                UIKit.Label(new Rect(panel.x + 40, panel.y + 456, 920, 60),
                    (next ?? "全部の素材がもらえた！") + "\n" + (cos ?? $"これまでに割った風船 {Progress.TotalBalloons}こ（ほうびは全部もらった！）"), UIKit.Small);
            }

            // キャラクリのほうび（キャラクターが着て見せてくれる）
            if (cosmeticShown)
            {
                var c = unlocked[unlocked.Count - 1];
                var look = CharacterLook.Decode(Progress.Look.Encode());
                if (c.IsHat) look.Hat = c.Index; else look.Outfit = c.Index;
                var card = new Rect(panel.xMax - 330, panel.y + 300, 300, 200);
                UIKit.Fill(card, new Color(1f, 0.95f, 0.75f, 0.95f));
                float sparkle = Mathf.Abs(Mathf.Sin(resultTime * 5f));
                UIKit.Label(new Rect(card.x, card.y + 4, card.width, 34), UIKit.Colorize("★ キャラクリのほうび ★", Color.Lerp(new Color(0.8f, 0.4f, 0.1f), new Color(1f, 0.7f, 0.1f), sparkle)), UIKit.SmallCenter);
                UIKit.Texture(new Rect(card.x + 20, card.y + 44, 120, 120), SpriteFactory.TextureOf(CharacterArt.Build(look)));
                UIKit.Label(new Rect(card.x + 146, card.y + 60, 150, 80), UIKit.Colorize($"「{c.Name}」\nをもらった！", new Color(0.4f, 0.2f, 0.1f)), UIKit.Body);
                UIKit.Label(new Rect(card.x + 10, card.y + 166, card.width - 20, 30), UIKit.Colorize("宿舎の鏡で着がえられるよ", new Color(0.4f, 0.3f, 0.2f)), UIKit.SmallCenter);
            }

            // 紙ふぶき
            foreach (var cf in confetti)
            {
                float w = cf.Size * Mathf.Abs(Mathf.Sin(resultTime * 4f + cf.Spin)) + 2;
                UIKit.Fill(new Rect(cf.Pos.x - w / 2, cf.Pos.y - cf.Size / 2, w, cf.Size * 0.6f), cf.Color);
            }

            // ボタン（すぐ押してしまわないように、少したってから）。Enter で演出をとばせる
            var e = Event.current;
            if (resultTime < RewardAt + RewardKinds * RewardGap && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space))
            {
                e.Use();
                resultTime = RewardAt + RewardKinds * RewardGap + 0.5f;
            }
            if (resultTime > CountTime)
            {
                if (UIKit.ColorButton(new Rect(370, 630, 260, 60), "もう一度", UIKit.AccentColor, UIKit.BigButton, primary: true))
                    Start();
                if (UIKit.ColorButton(new Rect(650, 630, 260, 60), "もどる", Color.gray, UIKit.BigButton))
                    OnClose?.Invoke();
            }
            SoundKit.ToggleButton(new Rect(1120, 20, 130, 36));
            UIKit.HelpBar("素材は鍛冶屋・防具屋で、杖やローブを作るのに使えるよ　Enter: 演出をとばす");
        }

        /// <summary>結果の数字の箱（少しずつ出てくる）</summary>
        private void StatBox(Rect r, string label, string value, Color color, float appearAt, bool wide = false)
        {
            if (resultTime < appearAt) return;
            float slide = (1f - Mathf.Clamp01((resultTime - appearAt) / 0.25f)) * 40f;
            r.x += slide;
            UIKit.Fill(r, color);
            if (wide)
            {
                UIKit.Label(new Rect(r.x + 16, r.y, 160, r.height), UIKit.Colorize(label, Color.white), UIKit.Body);
                UIKit.Label(new Rect(r.x + 150, r.y, r.width - 160, r.height), UIKit.Colorize(value, Color.white), UIKit.Title);
                return;
            }
            UIKit.Label(new Rect(r.x, r.y + 6, r.width, 30), UIKit.Colorize(label, Color.white), UIKit.SmallCenter);
            UIKit.Label(new Rect(r.x, r.y + 36, r.width, 70), UIKit.Colorize(value, Color.white), UIKit.Title);
        }
    }
}
