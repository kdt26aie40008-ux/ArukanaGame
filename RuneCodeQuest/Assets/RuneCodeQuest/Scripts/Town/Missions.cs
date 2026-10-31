namespace RuneCodeQuest
{
    /// <summary>
    /// 見習いの手帳：はじめの村（南西の区画）の町長がくれる「はじめての冒険」のおつかい。
    /// できたものは町長に話すと ごほうび（1つにつき金貨）。全部できたら、さらにごほうび。
    /// </summary>
    public static class Missions
    {
        public const int BoardSeen = 0, Accepted = 1, EnteredRuin = 2, BeatBugmon = 3, OpenedChest = 4, Wished = 5, PettedCat = 6;

        public static readonly string[] Names =
        {
            "依頼板を見る",
            "依頼を引き受ける",
            "遺跡のエリアに入る",
            "アルカナを1体たおす",
            "町の宝箱を開ける",
            "噴水で願いごとをする",
            "ねこをなでる",
        };

        public const int RewardCoins = 10;                 // 1つできるごとに
        public const int BonusCoins = 50, BonusHerbs = 3;  // 全部できたら
        public const int BonusBit = 7;                     // 全部のごほうびをもらったか（Progress.MissionsClaimed のビット）

        public static int Count => Names.Length;
    }
}
