using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>
    /// クエストの星の条件（それぞれの条件で 星が1こずつ）。
    /// ★1つ目：クリアする　★2つ目：1回目の起動でクリア　★3つ目：命令の数がお手本以下でクリア
    /// ステージ一覧から やり直してクリアしても、前より多ければ 星が記録される（Progress.MarkCleared）
    /// </summary>
    public static class QuestStars
    {
        /// <summary>★3つ目の命令の数（お手本と同じ数まで）</summary>
        public static int CommandLimit(Level level) => ProgramText.Count(level.Solution);

        /// <summary>3つの条件の文</summary>
        public static string[] Conditions(Level level) => new[]
        {
            "ゴールまで クリアする",
            "1回目の起動で クリアする",
            $"命令（ブロック）を {CommandLimit(level)}こ以下にして クリアする",
        };

        /// <summary>クリアしたときに、それぞれの条件ができたか</summary>
        public static bool[] Earned(Level level, int attemptsUsed, List<Node> program) => new[]
        {
            true,
            attemptsUsed == 1,
            ProgramText.Count(program) <= CommandLimit(level),
        };

        public static int Count(bool[] earned)
        {
            int n = 0;
            foreach (var e in earned) if (e) n++;
            return n;
        }

        public static string Mark(int stars) => new string('★', stars) + new string('☆', 3 - stars);
    }
}
