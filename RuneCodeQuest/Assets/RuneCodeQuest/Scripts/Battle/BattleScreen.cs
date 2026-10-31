using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// タイピングバトル（アルカナとの勝負）。
    ///
    /// ・表示された言葉を打つと、1文字ごとに攻撃（ダメージ = 攻撃力）
    /// ・言葉を打ち終わると、ボーナスのダメージ。まちがえずに打ち続けるとコンボで強くなる
    /// ・アルカナは時間がたつと こうげきしてくる（下のバーがたまると）
    /// ・Tab で薬草を使う、Esc でにげる（ボスからはにげられない）
    ///
    /// 日本語はローマ字で打つ（RomajiMatcher）。ランクが上がると、お手本が ローマ字 → ひらがな → 漢字 と変わる。
    /// 手が止まったり、まちがえたりすると、お手本がうっすら出てくる（お助け）。
    /// </summary>
    public class BattleScreen
    {
        private enum Phase { Intro, Fight, Won, Lost, Fled }

        /// <summary>バトルが終わったとき（勝った, にげた, 相手）</summary>
        public System.Action<bool, bool, EnemyInfo> OnFinished;

        private Phase phase;
        private EnemyInfo enemy;
        private int enemyHp;
        private float enemyTimer, phaseTimer, idleTimer, flashTimer, damageTimer;
        private int combo, missesOnChar, lastDamage;
        private float fightTime;             // 打ちはじめてからの時間
        private bool timeUp;                 // つりで時間切れになった
        private bool japanese;
        private TypingWord word;
        private RomajiMatcher matcher;   // 日本語のとき
        private int typedCount;          // 英語のとき、打てた文字数
        private string note;
        private readonly System.Random rng = new System.Random();
        private readonly List<string> recent = new List<string>(); // さっき出た言葉（しばらく出さない）

        public bool IsBoss => enemy != null && enemy.Boss;

        public void Start(EnemyInfo e)
        {
            enemy = e;
            enemyHp = e.Hp;
            enemyTimer = 0f;
            phaseTimer = 0f;
            combo = 0;
            note = null;
            fightTime = 0f;
            timeUp = false;
            japanese = Progress.TypingIsJapanese;
            phase = Phase.Intro;
            NextWord();
            TryImeOff();
            SoundKit.BattleBgm();
        }

        /// <summary>日本語入力（IME）を切る（できる環境のときだけ）</summary>
        private static void TryImeOff()
        {
            try { Input.imeCompositionMode = IMECompositionMode.Off; } catch { /* 新しい Input System だけのときは何もしない */ }
        }

        private void NextWord()
        {
            // さっき出た言葉は、しばらく出さない（言葉の数が少ないときのために、ためすのは10回まで）
            for (int i = 0; i < 10; i++)
            {
                word = BattleData.PickWord(japanese, PlayerStats.Rank, rng);
                if (!recent.Contains(word.Display)) break;
            }
            recent.Add(word.Display);
            while (recent.Count > 4) recent.RemoveAt(0);
            matcher = japanese ? new RomajiMatcher(word.Reading) : null;
            typedCount = 0;
            missesOnChar = 0;
            idleTimer = 0f;
        }

        public void Update()
        {
            float dt = Time.deltaTime;
            phaseTimer += dt;
            if (flashTimer > 0) flashTimer -= dt;
            if (damageTimer > 0) damageTimer -= dt;
            if (phase != Phase.Fight && phase != Phase.Intro) SoundKit.StopBgm(); // 勝ち・負け・にげたら止める
            if (phase == Phase.Intro && phaseTimer > 1.2f)
            {
                phase = Phase.Fight;
                phaseTimer = 0f;
            }
            if (phase != Phase.Fight) return;

            idleTimer += dt;
            fightTime += dt;
            if (enemy.TimeLimit > 0 && fightTime >= enemy.TimeLimit)
            {
                // つり：時間切れで、魚に にげられる
                timeUp = true;
                SoundKit.QuestFail();
                phase = Phase.Fled;
                return;
            }
            if (enemy.Attack <= 0) return; // 魚は攻撃してこない
            enemyTimer += dt;
            if (enemyTimer >= enemy.Interval)
            {
                enemyTimer = 0f;
                int dmg = Mathf.Max(1, enemy.Attack - PlayerStats.Defense);
                PlayerStats.Hp = PlayerStats.Hp - dmg;
                flashTimer = 0.4f;
                note = $"{enemy.Name}のこうげき！ {dmg} のダメージ";
                SoundKit.Hurt();
                if (PlayerStats.Hp <= 0)
                {
                    phase = Phase.Lost;
                    SoundKit.Lose();
                }
            }
        }

        // ------------------------------------------------------------------

        public void Draw()
        {
            UIKit.Nav("battle-" + phase, phase != Phase.Fight && phase != Phase.Intro);
            if (phase == Phase.Fight) HandleKeys();

            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.05f, 0.04f, 0.10f, 1f));
            if (flashTimer > 0) UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(1f, 0.2f, 0.2f, flashTimer));

            DrawEnemy();
            DrawPlayer();
            SoundKit.ToggleButton(new Rect(1120, 20, 130, 36));

            switch (phase)
            {
                case Phase.Intro:
                    string intro = enemy.Kind == BattleKind.Fishing ? $"何かが かかった！ {enemy.TimeLimit:0}秒で打ちきろう！"
                        : enemy.Rare ? $"レアモンスター！ {enemy.Name}が あらわれた！"
                        : enemy.Variant ? $"凶暴な亜種！ {enemy.Name}が あらわれた！\n（ふつうは {BattleData.RegionNames[BattleData.ClampRegion(enemy.Region)]}にいるアルカナだ）"
                        : $"{enemy.Name}が あらわれた！";
                    UIKit.Label(new Rect(0, 400, UIKit.RefWidth, 80), intro, UIKit.Title);
                    break;
                case Phase.Fight:
                    DrawWord();
                    break;
                default:
                    DrawResult();
                    break;
            }
        }

        private void DrawEnemy()
        {
            float size = enemy.Boss ? 220 : 170;
            var r = new Rect(640 - size / 2, 70, size, size);
            float shake = damageTimer > 0 ? Mathf.Sin(Time.time * 60f) * 6f : 0f;
            r.x += shake;
            var art = SpriteFactory.TextureOf(MonsterArt.Build(enemy));
            if (phase == Phase.Won) UIKit.TextureTinted(r, art, new Color(1, 1, 1, 0.3f));
            else if (enemy.Variant) UIKit.TextureTinted(r, art, new Color(1f, 0.55f, 0.55f)); // 凶暴な亜種は 赤っぽく
            else UIKit.Texture(r, art);

            UIKit.Label(new Rect(340, 20, 600, 44), enemy.Tier > 0 ? $"{enemy.Name} {UIKit.Colorize("つよさ+" + enemy.Tier, UIKit.WarnColor)}" : enemy.Name, UIKit.Center);
            Bar(new Rect(440, r.yMax + 10, 400, 18), enemyHp / (float)enemy.Hp, new Color(0.9f, 0.3f, 0.3f));
            UIKit.Label(new Rect(850, r.yMax + 2, 200, 34), $"{Mathf.Max(0, enemyHp)} / {enemy.Hp}", UIKit.Small);
            if (phase == Phase.Fight && enemy.TimeLimit > 0)
            {
                Bar(new Rect(440, r.yMax + 34, 400, 8), 1f - fightTime / enemy.TimeLimit, new Color(0.4f, 0.8f, 1f));
                UIKit.Label(new Rect(850, r.yMax + 24, 260, 30), $"にげるまで {Mathf.Max(0, enemy.TimeLimit - fightTime):0}秒", UIKit.Small);
            }
            else if (phase == Phase.Fight && enemy.Attack > 0)
            {
                Bar(new Rect(440, r.yMax + 34, 400, 8), enemyTimer / enemy.Interval, new Color(1f, 0.7f, 0.2f));
                UIKit.Label(new Rect(850, r.yMax + 24, 260, 30), "こうげきまで", UIKit.Small);
            }
            if (damageTimer > 0)
                UIKit.Label(new Rect(r.xMax, r.y + 20, 200, 60), UIKit.Colorize($"-{lastDamage}", new Color(1f, 0.9f, 0.3f)), UIKit.Title);
        }

        private void DrawPlayer()
        {
            var p = new Rect(40, 560, 420, 110);
            UIKit.Panel(p, UIKit.SubPanelColor);
            UIKit.Texture(new Rect(p.x + 10, p.y + 10, 90, 90), SpriteFactory.TextureOf(SpriteFactory.Player));
            UIKit.Label(new Rect(p.x + 110, p.y + 4, 300, 36), $"{Progress.Look.Name}　ランク {PlayerStats.Rank}", UIKit.Body);
            Bar(new Rect(p.x + 110, p.y + 48, 290, 18), PlayerStats.Hp / (float)PlayerStats.MaxHp, new Color(0.3f, 0.85f, 0.4f));
            UIKit.Label(new Rect(p.x + 110, p.y + 68, 300, 34), $"体力 {PlayerStats.Hp} / {PlayerStats.MaxHp}　攻撃 {PlayerStats.Attack}", UIKit.Small);

            if (phase == Phase.Fight)
            {
                int herbs = Progress.ItemCount(ItemKind.Herb);
                if (enemy.Kind == BattleKind.Bugmon && UIKit.ColorButton(new Rect(860, 580, 180, 50), $"薬草 ×{herbs}（Tab）", new Color(0.4f, 0.75f, 0.4f), UIKit.SmallButton, herbs > 0))
                    UseHerb();
                if (!enemy.Boss && UIKit.ColorButton(new Rect(1050, 580, 180, 50), enemy.Kind == BattleKind.Bugmon ? "にげる（Esc）" : "やめる（Esc）", Color.gray, UIKit.SmallButton))
                    Finish(Phase.Fled);
                if (combo >= 3) UIKit.Label(new Rect(480, 580, 360, 50), UIKit.Colorize($"{combo} コンボ！", new Color(1f, 0.8f, 0.2f)), UIKit.Center);
            }
            if (note != null && phase == Phase.Fight) UIKit.Label(new Rect(480, 630, 760, 40), note, UIKit.Small);
        }

        /// <summary>打つ言葉と、お手本</summary>
        private void DrawWord()
        {
            var box = new Rect(100, 296, 1080, 256);
            UIKit.Panel(box, UIKit.SubPanelColor);
            bool help = idleTimer > 2.5f || missesOnChar >= 2;  // お助け：手が止まった・まちがえた

            if (japanese)
            {
                // 上に日本語を大きく、下にお手本（読み・ローマ字）を書く
                var style = BattleData.JapaneseGuide(PlayerStats.Rank);
                // 打ったところは緑になる（かなの言葉は1文字ずつ。漢字の言葉は下の読みが緑になる）
                bool kanaWord = !Furigana.HasKanji(word.Display) && RomajiMatcher.ToHiragana(word.Display).Length == matcher.Kana.Length;
                string big = kanaWord ? UIKit.TypedText(word.Display, matcher.KanaDone, UIKit.TypedColor, Color.white) : word.Display;
                UIKit.Label(new Rect(box.x, box.y + 6, box.width, 112), big, word.Display.Length > 11 ? UIKit.Title : UIKit.Huge);

                // 漢字のときは読み（ひらがな）。漢字の段階では、打ったところだけ出す（お助けのときは全部）
                if (!kanaWord && word.Reading.Length == matcher.Kana.Length)
                {
                    bool showReading = style != BattleData.GuideStyle.Kanji || help;
                    UIKit.Label(new Rect(box.x, box.y + 116, box.width, 40),
                        UIKit.TypedText(word.Reading, matcher.KanaDone, UIKit.TypedColor, showReading ? new Color(1f, 1f, 1f, 0.75f) : (Color?)null), UIKit.Center);
                }
                else if (word.Display != word.Reading && (style != BattleData.GuideStyle.Kanji || help))
                    UIKit.Label(new Rect(box.x, box.y + 116, box.width, 40), UIKit.Colorize(word.Reading, new Color(1f, 1f, 1f, 0.75f)), UIKit.Center);

                // ローマ字のお手本：はじめのうちはいつも、慣れてきたら お助けのときだけ
                bool showRomaji = style == BattleData.GuideStyle.RomajiOnly || style == BattleData.GuideStyle.KanaWithRomaji || help;
                string typed = UIKit.Colorize(matcher.Typed, UIKit.TypedColor);
                string rest = showRomaji
                    ? UIKit.Colorize(matcher.Remaining, help && style != BattleData.GuideStyle.RomajiOnly && style != BattleData.GuideStyle.KanaWithRomaji ? new Color(1, 1, 1, 0.45f) : UIKit.WarnColor)
                    : "";
                UIKit.Label(new Rect(box.x, box.y + 156, box.width, 56), typed + rest, UIKit.Support);
                UIKit.Label(new Rect(box.x, box.y + 214, box.width, 34),
                    showRomaji ? "黄色のローマ字を打とう（緑は打てたところ）" : "ローマ字で打とう（手が止まると お手本が出るよ）", UIKit.SmallCenter);
            }
            else
            {
                string target = word.Reading;
                string typed = UIKit.Colorize(Escape(target.Substring(0, typedCount)), UIKit.TypedColor);
                string rest = Escape(target.Substring(typedCount));
                bool codeFromMeaning = word.Display != word.Reading;
                if (!codeFromMeaning)
                {
                    UIKit.Label(new Rect(box.x, box.y + 40, box.width, 90), typed + rest, UIKit.Title);
                    if (word.Meaning != null) UIKit.Label(new Rect(box.x, box.y + 140, box.width, 40), $"（{word.Meaning}）", UIKit.Center);
                }
                else
                {
                    // 意味を見て、コードを思い出して打つ
                    UIKit.Label(new Rect(box.x, box.y + 24, box.width, 70), word.Display, UIKit.Title);
                    UIKit.Label(new Rect(box.x, box.y + 110, box.width, 50),
                        typed + (help ? UIKit.Colorize(rest, new Color(1, 1, 1, 0.35f)) : ""), UIKit.Center);
                    UIKit.Label(new Rect(box.x, box.y + 166, box.width, 34), "この意味のコードを Python で打とう", UIKit.SmallCenter);
                }
            }
        }

        private static string Escape(string s) => s.Replace("<", "＜");

        private void DrawResult()
        {
            var box = new Rect(240, 330, 800, 210);
            UIKit.Panel(box, UIKit.SubPanelColor);
            string title, text;
            switch (phase)
            {
                case Phase.Won when enemy.Kind == BattleKind.Fishing:
                    title = $"{enemy.Name}を つり上げた！";
                    text = $"つり人が 金貨 {enemy.Coins}枚と交換してくれた！";
                    break;
                case Phase.Fled when enemy.Kind == BattleKind.Fishing:
                    title = timeUp ? $"{enemy.Name}に にげられた…" : "つりをやめた。";
                    text = timeUp ? "時間切れ。もっと速く打てるように、風船割りで練習してみよう！" : "また いつでも つりに来てね。";
                    break;
                case Phase.Won:
                    title = $"{enemy.Name}をたおした！";
                    text = $"金貨を {enemy.Coins} 枚 手に入れた！";
                    break;
                case Phase.Lost:
                    title = "やられてしまった…";
                    text = "宿舎で休んで、また挑戦しよう。持ち物や金貨はへらないよ。";
                    break;
                default:
                    title = "うまく にげられた！";
                    text = "体力が少ないときは、宿舎で休もう。";
                    break;
            }
            UIKit.Label(new Rect(box.x + 20, box.y + 20, box.width - 40, 60), title, UIKit.Heading);
            UIKit.Label(new Rect(box.x + 20, box.y + 80, box.width - 40, 60), text, UIKit.Body);
            if (UIKit.ColorButton(new Rect(box.x + 250, box.y + 140, 300, 56), "つづける", UIKit.AccentColor, UIKit.BigButton, primary: true))
                OnFinished?.Invoke(phase == Phase.Won, phase == Phase.Fled, enemy);
        }

        private static void Bar(Rect r, float t, Color c)
        {
            UIKit.Fill(r, new Color(0f, 0f, 0f, 0.6f));
            UIKit.Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), c);
        }

        // ------------------------------------------------------------------
        // 入力
        // ------------------------------------------------------------------

        private void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Escape && !enemy.Boss) { e.Use(); Finish(Phase.Fled); return; }
            if (e.keyCode == KeyCode.Tab) { e.Use(); UseHerb(); return; }

            char c = e.character;
            if (c == '\0' || c == '\n' || c == '\r' || c == '\t') return;
            e.Use();
            if (c > 127)
            {
                note = "日本語入力（IME）がオンになっているよ。半角英数にしてから、ローマ字で打とう。";
                return;
            }
            TypeChar(c);
        }

        private void TypeChar(char c)
        {
            bool ok, finished;
            if (japanese)
            {
                ok = matcher.Type(c);
                finished = matcher.Done;
            }
            else
            {
                string target = word.Reading;
                ok = typedCount < target.Length && target[typedCount] == c;
                if (ok) typedCount++;
                finished = typedCount >= target.Length;
            }

            if (!ok)
            {
                SoundKit.Miss();
                combo = 0;
                missesOnChar++;
                note = "おしい！ もう一度。";
                return;
            }

            SoundKit.Key();
            idleTimer = 0f;
            missesOnChar = 0;
            combo++;
            Hit(PlayerStats.Attack);
            if (phase != Phase.Fight) return;
            if (finished)
            {
                // 言葉を打ち終わったボーナス（コンボが続くほど大きい）
                Hit(PlayerStats.Attack * 2 + combo / 5);
                note = "言葉を打ち終わった！ ボーナスダメージ！";
                SoundKit.Chime();
                if (phase == Phase.Fight) NextWord();
            }
        }

        private void Hit(int dmg)
        {
            enemyHp -= dmg;
            lastDamage = dmg;
            damageTimer = 0.35f;
            if (enemyHp <= 0)
            {
                Progress.AddItem(ItemKind.Coin, enemy.Coins);
                phase = Phase.Won;
                SoundKit.Win();
            }
        }

        private void UseHerb()
        {
            if (Progress.ItemCount(ItemKind.Herb) <= 0)
            {
                note = "薬草を持っていない。道具屋で買えるよ。";
                return;
            }
            Progress.AddItem(ItemKind.Herb, -1);
            PlayerStats.Heal(BattleData.HerbHeal);
            note = $"薬草を使った！ 体力が {BattleData.HerbHeal} 回復した。";
        }

        private void Finish(Phase p)
        {
            phase = p;
        }
    }
}
