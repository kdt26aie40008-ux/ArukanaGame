using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// プレイヤーの強さ。
    /// ・ランク … クエストで集めた星で上がる（星3つで1ランク）
    /// ・体力の最大値 … ランクとローブで上がる
    /// ・攻撃力（1文字あたりのダメージ） … 杖で上がる
    /// ・守り（受けるダメージがへる） … ローブで上がる
    /// </summary>
    public static class PlayerStats
    {
        public const int StarsPerRank = 3;

        public static int Rank => 1 + Progress.TotalStars / StarsPerRank;

        /// <summary>次のランクまで、あといくつ星が必要か</summary>
        public static int StarsToNextRank => StarsPerRank - Progress.TotalStars % StarsPerRank;

        public static GearInfo Weapon => BattleData.Weapons[Mathf.Clamp(Progress.Weapon, 0, BattleData.Weapons.Length - 1)];
        public static GearInfo Armor => BattleData.Armors[Mathf.Clamp(Progress.Armor, 0, BattleData.Armors.Length - 1)];

        public static int MaxHp => 30 + (Rank - 1) * 4 + Armor.MaxHp;
        public static int Attack => 1 + Weapon.Attack + (Rank - 1) / 5;
        public static int Defense => Armor.Defense;

        public static int Hp
        {
            get => Progress.Hp < 0 ? MaxHp : Mathf.Min(Progress.Hp, MaxHp);
            set
            {
                int v = Mathf.Clamp(value, 0, MaxHp);
                Progress.Hp = v >= MaxHp ? -1 : v;
            }
        }

        public static void Heal(int amount) => Hp = Hp + amount;
        public static void FullHeal() => Progress.Hp = -1;
    }
}
