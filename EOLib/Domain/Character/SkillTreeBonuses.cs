using System;

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
        public double LuckPercent { get; }
        public double MaxHpPercent { get; }
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
            double luckPercent,
            double maxHpPercent,
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
            LuckPercent = luckPercent;
            MaxHpPercent = maxHpPercent;
            HeavyDamagePercent = heavyDamagePercent;
            MaxTpPercent = maxTpPercent;
            SpellAttackPercent = spellAttackPercent;
            PetDamage = petDamage;
            PetHeal = petHeal;
            InstanceKillPercent = instanceKillPercent;
            SpellPowerPercent = spellPowerPercent;
            SpellHeal = spellHeal;
        }

        public static SkillTreeBonuses FromRanks(Func<SkillTreeStat, int> getRank)
        {
            return new SkillTreeBonuses(
                critChancePercent: RankBonus(getRank, SkillTreeStat.CritChance, SkillTreeFormulas.CritChancePercentPerRank, SkillTreeFormulas.CritChanceCapPercent),
                critDamagePercent: RankBonus(getRank, SkillTreeStat.CritDamage, SkillTreeFormulas.CritDamagePercentPerRank, SkillTreeFormulas.CritDamageCapPercent),
                moveSpeedPercent: RankBonus(getRank, SkillTreeStat.MoveSpeed, SkillTreeFormulas.MoveSpeedPercentPerRank, SkillTreeFormulas.MoveSpeedCapPercent),
                attackSpeedPercent: RankBonus(getRank, SkillTreeStat.AttackSpeed, SkillTreeFormulas.AttackSpeedPercentPerRank, SkillTreeFormulas.AttackSpeedCapPercent),
                goldDropPercent: RankBonus(getRank, SkillTreeStat.GoldDrop, SkillTreeFormulas.GoldDropPercentPerRank, SkillTreeFormulas.GoldDropCapPercent),
                expGainPercent: RankBonus(getRank, SkillTreeStat.ExpGain, SkillTreeFormulas.ExpGainPercentPerRank, SkillTreeFormulas.ExpGainCapPercent),
                luckPercent: RankBonus(getRank, SkillTreeStat.Luck, SkillTreeFormulas.LuckPercentPerRank, SkillTreeFormulas.LuckCapPercent),
                maxHpPercent: RankBonus(getRank, SkillTreeStat.MaxHP, SkillTreeFormulas.MaxHpPercentPerRank, SkillTreeFormulas.MaxHpCapPercent),
                heavyDamagePercent: RankBonus(getRank, SkillTreeStat.HeavyDamage, SkillTreeFormulas.HeavyDamagePercentPerRank, SkillTreeFormulas.HeavyDamageCapPercent),
                maxTpPercent: getRank(SkillTreeStat.MaxTP) * SkillTreeFormulas.MaxTpPercentPerRank,
                spellAttackPercent: getRank(SkillTreeStat.SpellAttack) * SkillTreeFormulas.SpellAttackPercentPerRank,
                petDamage: getRank(SkillTreeStat.PetDamage) * SkillTreeFormulas.PetDamagePerRank,
                petHeal: getRank(SkillTreeStat.PetHeal) * SkillTreeFormulas.PetHealPerRank,
                instanceKillPercent: getRank(SkillTreeStat.InstanceKill) * SkillTreeFormulas.InstanceKillPercentPerRank,
                spellPowerPercent: getRank(SkillTreeStat.SpellPower) * SkillTreeFormulas.SpellPowerPercentPerRank,
                spellHeal: getRank(SkillTreeStat.SpellHeal) * SkillTreeFormulas.SpellHealPerRank);
        }

        private static double RankBonus(Func<SkillTreeStat, int> getRank, SkillTreeStat stat, double perRank, double cap) =>
            Math.Min(cap, getRank(stat) * perRank);
    }

    public static class SkillTreeFormulas
    {
        // Crit Chance: +0.5%/pt, hard cap 20%
        public const double CritChancePercentPerRank = 0.5;
        public const double CritChanceCapPercent = 20;
        public const int CritChanceMaxRank = 40; // 20 / 0.5

        // Crit Damage: +0.5%/pt, hard cap 25%
        public const double CritDamagePercentPerRank = 0.5;
        public const double CritDamageCapPercent = 25;
        public const int CritDamageMaxRank = 50; // 25 / 0.5

        // Move / Atk Speed: +3%/pt, hard cap +100% (double)
        public const double MoveSpeedPercentPerRank = 3;
        public const double MoveSpeedCapPercent = 100;
        public const int MoveSpeedMaxRank = 34; // clamps at 100%

        public const double AttackSpeedPercentPerRank = 3;
        public const double AttackSpeedCapPercent = 100;
        public const int AttackSpeedMaxRank = 34;

        // Gold Drop: +0.5%/pt, hard cap 50%
        public const double GoldDropPercentPerRank = 0.5;
        public const double GoldDropCapPercent = 50;
        public const int GoldDropMaxRank = 100; // 50 / 0.5

        // Exp Gain: +0.5%/pt, hard cap 35%
        public const double ExpGainPercentPerRank = 0.5;
        public const double ExpGainCapPercent = 35;
        public const int ExpGainMaxRank = 70; // 35 / 0.5

        // Luck: +0.1%/pt, hard cap 20%
        public const double LuckPercentPerRank = 0.1;
        public const double LuckCapPercent = 20;
        public const int LuckMaxRank = 200; // 20 / 0.1

        // Max HP: +0.3%/pt, max 100 points spent
        public const double MaxHpPercentPerRank = 0.3;
        public const double MaxHpCapPercent = 30; // 0.3 * 100
        public const int MaxHpMaxRank = 100;

        // Heavy Damage: +0.5%/pt, hard cap 20%
        public const double HeavyDamagePercentPerRank = 0.5;
        public const double HeavyDamageCapPercent = 20;
        public const int HeavyDamageMaxRank = 40; // 20 / 0.5

        // Max TP: +1%/pt, max 100 points spent
        public const double MaxTpPercentPerRank = 1;
        public const int MaxTpMaxRank = 100;

        // Spell Atk: +0.5%/pt, max 40 points spent
        public const double SpellAttackPercentPerRank = 0.5;
        public const int SpellAttackMaxRank = 40;

        // Pet Damage / Heal: +1 flat/pt, max 100 points
        public const int PetDamagePerRank = 1;
        public const int PetDamageMaxRank = 100;
        public const int PetHealPerRank = 1;
        public const int PetHealMaxRank = 100;

        // Instance Kill: +0.2%/pt, max 50 points spent
        public const double InstanceKillPercentPerRank = 0.2;
        public const int InstanceKillMaxRank = 50;

        // Spell Damage: +1%/pt, max 100 points
        public const double SpellPowerPercentPerRank = 1;
        public const int SpellPowerMaxRank = 100;

        // Spell Heal: +2 flat/pt, max 100 points
        public const int SpellHealPerRank = 2;
        public const int SpellHealMaxRank = 100;

        public static int GetMaxRank(SkillTreeStat stat) => stat switch
        {
            SkillTreeStat.CritChance => CritChanceMaxRank,
            SkillTreeStat.CritDamage => CritDamageMaxRank,
            SkillTreeStat.MoveSpeed => MoveSpeedMaxRank,
            SkillTreeStat.AttackSpeed => AttackSpeedMaxRank,
            SkillTreeStat.GoldDrop => GoldDropMaxRank,
            SkillTreeStat.ExpGain => ExpGainMaxRank,
            SkillTreeStat.Luck => LuckMaxRank,
            SkillTreeStat.MaxHP => MaxHpMaxRank,
            SkillTreeStat.HeavyDamage => HeavyDamageMaxRank,
            SkillTreeStat.MaxTP => MaxTpMaxRank,
            SkillTreeStat.SpellAttack => SpellAttackMaxRank,
            SkillTreeStat.PetDamage => PetDamageMaxRank,
            SkillTreeStat.PetHeal => PetHealMaxRank,
            SkillTreeStat.InstanceKill => InstanceKillMaxRank,
            SkillTreeStat.SpellPower => SpellPowerMaxRank,
            SkillTreeStat.SpellHeal => SpellHealMaxRank,
            _ => 100
        };
    }
}
