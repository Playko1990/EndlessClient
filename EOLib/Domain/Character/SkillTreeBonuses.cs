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
        public double MaxTpPercent { get; }
        public double SpellAttackPercent { get; }
        public int PetDamage { get; }
        public int PetHeal { get; }
        public double InstanceKillPercent { get; }
        public double SpellPowerPercent { get; }
        public int SpellHeal { get; }

        public SkillTreeBonuses(
            double critChancePercent,
            double critDamagePercent,
            double moveSpeedPercent,
            double attackSpeedPercent,
            double goldDropPercent,
            double expGainPercent,
            int luck,
            int maxHp,
            double heavyDamagePercent,
            double maxTpPercent,
            double spellAttackPercent,
            int petDamage,
            int petHeal,
            double instanceKillPercent,
            double spellPowerPercent,
            int spellHeal)
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
            MaxTpPercent = maxTpPercent;
            SpellAttackPercent = spellAttackPercent;
            PetDamage = petDamage;
            PetHeal = petHeal;
            InstanceKillPercent = instanceKillPercent;
            SpellPowerPercent = spellPowerPercent;
            SpellHeal = spellHeal;
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
                heavyDamagePercent: getRank(SkillTreeStat.HeavyDamage) * SkillTreeFormulas.HeavyDamagePercentPerRank,
                maxTpPercent: getRank(SkillTreeStat.MaxTP) * SkillTreeFormulas.MaxTpPercentPerRank,
                spellAttackPercent: getRank(SkillTreeStat.SpellAttack) * SkillTreeFormulas.SpellAttackPercentPerRank,
                petDamage: getRank(SkillTreeStat.PetDamage) * SkillTreeFormulas.PetDamagePerRank,
                petHeal: getRank(SkillTreeStat.PetHeal) * SkillTreeFormulas.PetHealPerRank,
                instanceKillPercent: getRank(SkillTreeStat.InstanceKill) * SkillTreeFormulas.InstanceKillPercentPerRank,
                spellPowerPercent: getRank(SkillTreeStat.SpellPower) * SkillTreeFormulas.SpellPowerPercentPerRank,
                spellHeal: getRank(SkillTreeStat.SpellHeal) * SkillTreeFormulas.SpellHealPerRank);
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
        public const double MaxTpPercentPerRank = 1;
        public const double SpellAttackPercentPerRank = 1;
        public const int PetDamagePerRank = 1;
        public const int PetHealPerRank = 1;
        public const double InstanceKillPercentPerRank = 0.5;
        public const double SpellPowerPercentPerRank = 1;
        public const int SpellHealPerRank = 2;
    }
}
