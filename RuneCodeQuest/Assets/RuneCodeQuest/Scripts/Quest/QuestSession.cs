using System.Collections.Generic;

namespace RuneCodeQuest
{
    public enum SolveMode { Scratch, Python }

    public enum LaunchKind
    {
        Success,       // クリア
        CodeError,     // エラー：プログラムが読めなかった（Python の書き方のまちがい）
        ConditionFail, // 条件未達成：プログラムは動いたが、目的を達成できなかった
        RuleBreak,     // ルール違反：おまけのしばり（ブロックを置ける数）を破った（ゴーレムは動かない）
    }

    /// <summary>「起動」ボタンを押した結果</summary>
    public class LaunchOutcome
    {
        public LaunchKind Kind;
        public List<CodeError> Errors = new List<CodeError>();
        public List<string> RuleBreaks = new List<string>(); // RuleBreak のとき、破ったしばり
        public List<Node> Program = new List<Node>();
        public RunResult Run;           // CodeError のときは null
        public string Hint;
        public int AttemptsLeft;
        public bool QuestOver;          // 挑戦回数を使い切った
    }

    /// <summary>
    /// 1つのレベルの進行状況（見た目とは切り離したルール部分）。
    /// 挑戦回数・解き方の切り替え・ヒントの選び方はここで決めています。
    /// </summary>
    public class QuestSession
    {
        public Level Level { get; private set; }
        public SolveMode Mode = SolveMode.Scratch;
        public List<Node> Blocks = new List<Node>();   // スクラッチの「▶起動したとき」の下にあるブロック
        public string PythonText = PythonTemplate;

        public int AttemptsUsed { get; private set; }
        public int MaxAttempts => QuestDatabase.MaxAttemptsFor(Level.Number);
        public int AttemptsLeft => MaxAttempts - AttemptsUsed;
        public bool Cleared { get; private set; }
        public bool Over => !Cleared && AttemptsLeft <= 0;

        public const string PythonTemplate = "# 1行に1つずつ命令を書こう（# の後ろはメモ）\n";

        public QuestSession(int levelNumber)
        {
            StartLevel(levelNumber, 0);
        }

        public bool IsSimilarProblem => Level.Variant > 0;

        /// <summary>レベルを始める（挑戦回数はリセット）。variant が1以上なら似た問題</summary>
        public void StartLevel(int number, int variant)
        {
            Level = LevelFactory.Create(number, variant);
            AttemptsUsed = 0;
            Cleared = false;
            Blocks = new List<Node>();
            PythonText = PythonTemplate;
        }

        /// <summary>
        /// 解き方を切り替える。作りかけの命令は、もう一方の形に変換して引きつぐ。
        /// 戻り値はプレイヤーへのお知らせ（なければ null）。切り替えられなかったら ok = false。
        /// </summary>
        public string SwitchMode(SolveMode newMode, out bool ok)
        {
            ok = true;
            if (newMode == Mode) return null;

            if (newMode == SolveMode.Python)
            {
                PythonText = Blocks.Count > 0 ? PythonTemplate + ProgramText.ToPython(Blocks) : PythonTemplate;
                Mode = newMode;
                return Blocks.Count > 0 ? "スクラッチのブロックを Python の書き方に変換したよ。" : null;
            }

            var parsed = MiniPython.Parse(PythonText);
            if (parsed.HasErrors)
            {
                ok = false;
                return $"Python にエラーが {parsed.Errors.Count} か所あるので、ブロックに変換できません。直してから切り替えるか、「ぜんぶ消す」を押してね。";
            }
            Blocks = parsed.Program;
            Mode = newMode;
            return Blocks.Count > 0 ? "Python のコードをスクラッチのブロックに変換したよ。" : null;
        }

        /// <summary>現在のプログラムを読み取る（実行はしない）。エラーがあれば errors に入る</summary>
        public List<Node> BuildProgram(out List<CodeError> errors)
        {
            if (Mode == SolveMode.Scratch)
            {
                errors = new List<CodeError>();
                return Blocks;
            }
            var parsed = MiniPython.Parse(PythonText);
            errors = parsed.Errors;
            if (!Level.AllowFunctions && ProgramText.Uses(parsed.Program, NodeKind.Define))
                errors.Add(new CodeError
                {
                    Line = 0, Code = "def",
                    Message = $"関数（def）は、ステージ{LevelFactory.FunctionsFrom}から使えます。",
                    Fix = "このステージでは、for / if / while などを使って書こう。",
                });
            return parsed.Program;
        }

        /// <summary>「起動」：命令を読み取り、ゴーレムを動かして判定する。挑戦回数を1回使う</summary>
        public LaunchOutcome Launch()
        {
            var outcome = new LaunchOutcome();
            outcome.Program = BuildProgram(out var errors);
            AttemptsUsed++;

            if (errors.Count > 0)
            {
                outcome.Kind = LaunchKind.CodeError;
                outcome.Errors = errors;
                outcome.Hint = QuestDatabase.ErrorHint;
            }
            else if ((outcome.RuleBreaks = LevelFactory.CheckUsageLimits(Level, outcome.Program)).Count > 0)
            {
                outcome.Kind = LaunchKind.RuleBreak;
                outcome.Hint = QuestDatabase.RuleBreakHint;
            }
            else
            {
                outcome.Run = GolemRunner.Run(Level, outcome.Program);
                if (outcome.Run.Success)
                {
                    outcome.Kind = LaunchKind.Success;
                    Cleared = true;
                }
                else
                {
                    outcome.Kind = LaunchKind.ConditionFail;
                    outcome.Hint = PickHint(outcome.Run);
                }
            }

            outcome.AttemptsLeft = AttemptsLeft;
            outcome.QuestOver = Over;
            return outcome;
        }

        /// <summary>失敗の理由に合ったヒント + 回数に応じたヒントを組み合わせる</summary>
        private string PickHint(RunResult run)
        {
            var hints = Level.Chapter.Hints;
            string specific = QuestDatabase.FailHints.TryGetValue(run.Fail, out var h) ? h : null;
            if (run.FailedTest > 0)
                specific = "1つ目のテストは成功したのに、別のテストで失敗したよ。決めうちではなく「もし」で調べよう。\n" + specific;
            int idx = System.Math.Min(AttemptsUsed - 1, hints.Count - 1);
            string general = idx >= 0 ? hints[idx] : null;
            if (specific == null) return general;
            if (general == null || AttemptsUsed <= 1) return specific;
            return specific + "\n" + general;
        }
    }
}
