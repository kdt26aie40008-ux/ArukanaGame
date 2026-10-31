using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：遺跡のエリアで できること。
    /// ・砂の遺跡のサボテン：ぶつかるとトゲがささって体力がへる（少し押しもどされる）
    /// ・回復の泉（オアシス・温泉）：体力が全回復
    /// ・キラキラ光る地面：調べると金貨・薬草・資材が見つかる（1か所1回）
    /// ・旅人のクイズ：プログラミングのクイズに正解すると、金貨と薬草（エリアごとに1回。まちがえても何回でも挑戦できる）
    /// </summary>
    public partial class GameManager
    {
        private float cactusCooldown;
        private QuizQuestion quiz;   // 出しているクイズ（null なら出していない）
        private int quizChoice = -1; // えらんだ答え（-1 ならまだ）
        private int quizAttempt;      // このエリアで答えた回数（答えるたびに ちがう問題になる）
        private bool quizRewardNow;   // 今の答えで ごほうびをもらった
        private readonly System.Random digRng = new System.Random();

        private const int CactusDamage = 3;

        private bool QuizOpen => quiz != null;

        // ------------------------------------------------------------------
        // サボテン
        // ------------------------------------------------------------------

        /// <summary>サボテンにぶつかっていたら、体力をへらして押しもどす。体力がなくなったら true</summary>
        private bool CheckCactus(Vector2 p)
        {
            if (cactusCooldown > 0f)
            {
                cactusCooldown -= Time.deltaTime;
                return false;
            }
            foreach (var c in area.Cacti())
            {
                // プレイヤーのからだ（半分 0.3）とサボテンのマス（半分 0.5）が ふれている
                if (Mathf.Abs(p.x - c.x) >= 0.82f || Mathf.Abs(p.y - c.y) >= 0.82f) continue;
                cactusCooldown = 1f;
                PlayerStats.Hp = PlayerStats.Hp - CactusDamage;
                SoundKit.Hurt();
                player.StopWalking();
                player.PushAway(c, 0.45f);
                if (PlayerStats.Hp <= 0)
                {
                    SoundKit.Lose();
                    WakeUpAtInn("目が覚めたか。サボテンのトゲは いたかったな。砂の遺跡では、サボテンに近づきすぎないように歩くんじゃ。");
                    return true;
                }
                ShowToast($"いたっ！ サボテンのトゲがささった！ 体力 -{CactusDamage}（のこり {PlayerStats.Hp}）", 2.5f);
                return false;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // 回復の泉・キラキラ光る地面
        // ------------------------------------------------------------------

        private void DrinkSpring()
        {
            KeyInput.Clear();
            PlayerStats.FullHeal();
            SoundKit.Chime();
            string text = area.Chapter == 2 ? "オアシスの冷たい水を飲んだ。生き返る〜！ 体力が全回復した。"
                : area.Chapter == 4 ? "温泉につかって、ぽかぽか あたたまった。体力が全回復した。"
                : "すきとおった泉の水を飲んだ。体力が全回復した。";
            dialog.Show(area.SpringName, text);
        }

        private void Dig(int i)
        {
            KeyInput.Clear();
            area.MarkDig(i);
            SoundKit.Pop();
            string got;
            int roll = digRng.Next(100);
            if (roll < 40)
            {
                int coins = 10 + digRng.Next(0, 16) + area.Stage / 5;
                Progress.AddItem(ItemKind.Coin, coins);
                got = $"金貨を {coins}枚 見つけた！";
            }
            else if (roll < 70)
            {
                Progress.AddItem(ItemKind.Herb, 1);
                got = "薬草を 1こ 見つけた！";
            }
            else
            {
                var m = Rebuild.MaterialOf(area.Stage);
                Progress.AddMaterial(m, 1);
                got = $"{Rebuild.MaterialNames[(int)m]}を 1こ 見つけた！";
            }
            dialog.Show("キラキラ光る地面", "地面をほってみると……" + got);
        }

        // ------------------------------------------------------------------
        // 旅人のクイズ
        // ------------------------------------------------------------------

        private void TalkToTraveler()
        {
            KeyInput.Clear();
            string hello = area.QuizDone
                ? "また来てくれたね！ ごほうびは もうあげたけど、練習の問題を出すよ。今の章・応用・ふりかえり、いろいろあるよ！"
                : "やあ、見習い魔法使いさん。ルーンコードのクイズを出すよ。正解したら、ごほうびをあげよう！";
            dialog.Show("旅人", hello, () =>
            {
                // ワードブロックの人はブロックの形、Python の人は Python のコードで答えをえらぶ
                quiz = AreaQuiz.For(area.Stage, Progress.PreferredMode, quizAttempt);
                quizChoice = -1;
            });
        }

        /// <summary>クイズの答え合わせ</summary>
        private void AnswerQuiz(int choice)
        {
            quizChoice = choice;
            quizAttempt++;
            quizRewardNow = false;
            if (choice == quiz.Answer)
            {
                SoundKit.QuestClear();
                if (!area.QuizDone)
                {
                    quizRewardNow = true;
                    area.MarkQuiz();
                    int coins = 15 + area.Chapter * 5;
                    Progress.AddItem(ItemKind.Coin, coins);
                    Progress.AddItem(ItemKind.Herb, 1);
                }
            }
            else SoundKit.QuestFail();
        }

        private void DrawQuiz()
        {
            if (quiz.Programs != null) { DrawProgramQuiz(); return; }
            UIKit.Nav("quiz-" + (quizChoice < 0 ? "ask" : "result"), true);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.6f));
            var p = new Rect(190, 70, 900, 560);
            UIKit.Panel(p);
            DrawQuizHeading(p);
            UIKit.Fill(new Rect(p.x + 30, p.y + 70, p.width - 60, 130), UIKit.SubPanelColor);
            UIKit.Label(new Rect(p.x + 50, p.y + 78, p.width - 100, 120), quiz.Question, UIKit.Explain);

            if (quizChoice < 0)
            {
                // 3つの答えから えらぶ（矢印キー＋Enter、またはクリック）
                for (int i = 0; i < quiz.Choices.Length; i++)
                    if (UIKit.ColorButton(new Rect(p.x + 60, p.y + 220 + i * 84, p.width - 120, 70), $"{i + 1}. {quiz.Choices[i]}", UIKit.AccentColor, UIKit.BigButton, true, primary: i == 0))
                        AnswerQuiz(i);
                DrawQuizQuit(p);
                return;
            }

            bool right = quizChoice == quiz.Answer;
            UIKit.Label(new Rect(p.x + 30, p.y + 214, p.width - 60, 60),
                right ? UIKit.Colorize("せいかい！", UIKit.SuccessColor) : UIKit.Colorize($"ざんねん…　答えは「{quiz.Choices[quiz.Answer]}」", UIKit.ErrorColor), UIKit.Title);
            UIKit.Label(new Rect(p.x + 50, p.y + 290, p.width - 100, 140), quiz.Explain, UIKit.Explain);
            DrawQuizReward(new Rect(p.x + 50, p.y + 420, p.width - 100, 40), right);
            DrawQuizClose(new Rect(p.x + 300, p.yMax - 90, 300, 64));
        }

        /// <summary>プログラムをえらぶクイズ：3つのプログラム（ブロック または Python）から正しいものをえらぶ</summary>
        private void DrawProgramQuiz()
        {
            var mode = Progress.PreferredMode;
            UIKit.Nav("quiz-" + (quizChoice < 0 ? "ask" : "result"), true, quizChoice < 0 ? 3 : 1);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.6f));
            var p = new Rect(40, 40, 1200, 640);
            UIKit.Panel(p);
            DrawQuizHeading(p);
            UIKit.Fill(new Rect(p.x + 20, p.y + 64, p.width - 40, 84), UIKit.SubPanelColor);
            UIKit.Label(new Rect(p.x + 36, p.y + 68, p.width - 72, 80), quiz.Question, UIKit.Explain);

            if (quizChoice < 0)
            {
                float w = (p.width - 40 - 2 * 14) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    var card = new Rect(p.x + 20 + i * (w + 14), p.y + 162, w, 400);
                    if (UIKit.ColorButton(card, "", new Color(0.24f, 0.27f, 0.36f), UIKit.Button, true, primary: i == 0))
                        AnswerQuiz(i);
                    UIKit.Label(new Rect(card.x + 12, card.y + 4, 200, 34), UIKit.Colorize($"{i + 1}ばん", UIKit.AccentColor), UIKit.Body);
                    ProgramPicture.Draw(new Rect(card.x + 12, card.y + 40, card.width - 24, card.height - 52), quiz.Programs[i], mode);
                }
                UIKit.Label(new Rect(p.x + 20, p.y + 572, p.width - 40, 30),
                    mode == SolveMode.Python ? "正しい Python のプログラムを 1つ えらぼう（クリック／矢印キー＋Enter）" : "正しいブロックのプログラムを 1つ えらぼう（クリック／矢印キー＋Enter）", UIKit.SmallCenter);
                DrawQuizQuit(p);
                return;
            }

            // 答え合わせ：左に結果と解説、右に正しいプログラム
            bool right = quizChoice == quiz.Answer;
            UIKit.Label(new Rect(p.x + 30, p.y + 164, 700, 60),
                right ? UIKit.Colorize("せいかい！", UIKit.SuccessColor) : UIKit.Colorize($"ざんねん…　答えは {quiz.Answer + 1}ばん", UIKit.ErrorColor), UIKit.Title);
            if (!right) UIKit.Label(new Rect(p.x + 34, p.y + 226, 700, 34), $"えらんだのは {quizChoice + 1}ばん。右の正しいプログラムと くらべてみよう。", UIKit.Body);
            UIKit.Label(new Rect(p.x + 34, p.y + 270, 700, 200), quiz.Explain, UIKit.Explain);
            DrawQuizReward(new Rect(p.x + 34, p.y + 480, 700, 40), right);

            var box = new Rect(p.x + 760, p.y + 162, 420, 400);
            UIKit.Fill(box, new Color(0.24f, 0.27f, 0.36f));
            UIKit.Label(new Rect(box.x + 12, box.y + 4, box.width - 24, 34), UIKit.Colorize($"正しいプログラム（{quiz.Answer + 1}ばん）", UIKit.SuccessColor), UIKit.Body);
            ProgramPicture.Draw(new Rect(box.x + 12, box.y + 40, box.width - 24, box.height - 52), quiz.Programs[quiz.Answer], mode);
            DrawQuizClose(new Rect(p.x + 230, p.yMax - 84, 300, 64));
        }

        private void DrawQuizHeading(Rect p)
        {
            UIKit.Label(new Rect(p.x + 30, p.y + 14, 300, 50), "旅人のクイズ", UIKit.Heading);
            if (!string.IsNullOrEmpty(quiz.Category))
                UIKit.Label(new Rect(p.x + 260, p.y + 18, 500, 40), UIKit.Colorize(quiz.Category, UIKit.WarnColor), UIKit.Body);
        }

        private void DrawQuizQuit(Rect p)
        {
            if (UIKit.ColorButton(new Rect(p.xMax - 160, p.y + 16, 130, 44), "やめる", Color.gray))
                quiz = null;
        }

        private void DrawQuizReward(Rect r, bool right)
        {
            if (right)
                UIKit.Label(r, area.QuizDone && !quizRewardNow
                    ? "このエリアのごほうびは もうもらったよ。"
                    : UIKit.Colorize($"ごほうび：金貨 {15 + area.Chapter * 5}枚 と 薬草 1こ をもらった！", UIKit.WarnColor), UIKit.Body);
            else
                UIKit.Label(r, "もう一度 旅人に話しかけると、ちがう問題で また挑戦できるよ。", UIKit.Body);
        }

        private void DrawQuizClose(Rect r)
        {
            if (UIKit.ColorButton(r, "とじる", UIKit.AccentColor, UIKit.BigButton, primary: true))
            {
                quiz = null;
                KeyInput.Clear();
            }
        }
    }
}
