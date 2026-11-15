namespace EOLib.Domain.Character
{
    public sealed class SkillTreeBonuses
    {
        public double CritChancePercent { get; }
        public double CritDamagePercent { get; }
        public double MoveSpeedPercent { get; }
        public double AttackSpeedPercent { get; }
        public double GoldDropPercent { get; }
        public double ExpGainPercent { get; }
        public int Luck { get; }
        public int MaxHp { get; }
        public double HeavyDamagePercent { get; }

        public SkillTreeBonuses(
            double critChancePercent,
            double critDamagePercent,
            double moveSpeedPercent,
            double attackSpeedPercent,
            double goldDropPercent,
            double expGainPercent,
            int luck,
            int maxHp,
            double heavyDamagePercent)
        {
            CritChancePercent = critChancePercent;
            CritDamagePercent = critDamagePercent;
            MoveSpeedPercent = moveSpeedPercent;
            AttackSpeedPercent = attackSpeedPercent;
            GoldDropPercent = goldDropPercent;
            ExpGainPercent = expGainPercent;
            Luck = luck;
            MaxHp = maxHp;
            HeavyDamagePercent = heavyDamagePercent;
        }

        public static SkillTreeBonuses FromRanks(System.Func<SkillTreeStat, int> getRank)
        {
            return new SkillTreeBonuses(
                critChancePercent: getRank(SkillTreeStat.CritChance) * SkillTreeFormulas.CritChancePercentPerRank,
                critDamagePercent: getRank(SkillTreeStat.CritDamage) * SkillTreeFormulas.CritDamagePercentPerRank,
                moveSpeedPercent: getRank(SkillTreeStat.MoveSpeed) * SkillTreeFormulas.MoveSpeedPercentPerRank,
                attackSpeedPercent: getRank(SkillTreeStat.AttackSpeed) * SkillTreeFormulas.AttackSpeedPercentPerRank,
                goldDropPercent: getRank(SkillTreeStat.GoldDrop) * SkillTreeFormulas.GoldDropPercentPerRank,
                expGainPercent: getRank(SkillTreeStat.ExpGain) * SkillTreeFormulas.ExpGainPercentPerRank,
                luck: getRank(SkillTreeStat.Luck) * SkillTreeFormulas.LuckPerRank,
                maxHp: getRank(SkillTreeStat.MaxHP) * SkillTreeFormulas.MaxHpPerRank,
                heavyDamagePercent: getRank(SkillTreeStat.HeavyDamage) * SkillTreeFormulas.HeavyDamagePercentPerRank);
        }
    }

    public static class SkillTreeFormulas
    {
        public const double CritChancePercentPerRank = 1;
        public const double CritDamagePercentPerRank = 5;
        public const double MoveSpeedPercentPerRank = 3;
        public const double AttackSpeedPercentPerRank = 3;
        public const double GoldDropPercentPerRank = 2;
        public const double ExpGainPercentPerRank = 2;
        public const int LuckPerRank = 1;
        public const int MaxHpPerRank = 15;
        public const double HeavyDamagePercentPerRank = 3;
    }
}
