using EOLib.Domain.Character;
using NUnit.Framework;

namespace EOLib.Test.Domain.Character
{
    [TestFixture]
    public class SkillTreeRepositoryTest
    {
        private SkillTreeRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _repository = new SkillTreeRepository();
        }

        [Test]
        public void AvailablePointsSubtractSpentRanks()
        {
            _repository.SetRank(SkillTreeStat.CritChance, 2);
            _repository.SetRank(SkillTreeStat.MaxHP, 3);

            Assert.That(_repository.TotalSpent, Is.EqualTo(5));
            Assert.That(_repository.GetAvailablePoints(8), Is.EqualTo(3));
            Assert.That(_repository.GetAvailablePoints(2), Is.EqualTo(0));
        }

        [Test]
        public void BonusesScaleWithRanks()
        {
            _repository.SetRank(SkillTreeStat.CritChance, 2);
            _repository.SetRank(SkillTreeStat.CritDamage, 1);
            _repository.SetRank(SkillTreeStat.MoveSpeed, 4);
            _repository.SetRank(SkillTreeStat.AttackSpeed, 3);
            _repository.SetRank(SkillTreeStat.GoldDrop, 5);
            _repository.SetRank(SkillTreeStat.ExpGain, 2);
            _repository.SetRank(SkillTreeStat.Luck, 7);
            _repository.SetRank(SkillTreeStat.MaxHP, 6);
            _repository.SetRank(SkillTreeStat.HeavyDamage, 2);
            _repository.SetRank(SkillTreeStat.MaxTP, 3);
            _repository.SetRank(SkillTreeStat.SpellAttack, 4);
            _repository.SetRank(SkillTreeStat.PetDamage, 5);
            _repository.SetRank(SkillTreeStat.PetHeal, 6);
            _repository.SetRank(SkillTreeStat.InstanceKill, 2);
            _repository.SetRank(SkillTreeStat.SpellPower, 3);
            _repository.SetRank(SkillTreeStat.SpellHeal, 4);

            var bonuses = _repository.GetBonuses();

            Assert.That(bonuses.CritChancePercent, Is.EqualTo(1));
            Assert.That(bonuses.CritDamagePercent, Is.EqualTo(0.5));
            Assert.That(bonuses.MoveSpeedPercent, Is.EqualTo(12));
            Assert.That(bonuses.AttackSpeedPercent, Is.EqualTo(9));
            Assert.That(bonuses.GoldDropPercent, Is.EqualTo(2.5));
            Assert.That(bonuses.ExpGainPercent, Is.EqualTo(1));
            Assert.That(bonuses.LuckPercent, Is.EqualTo(0.7).Within(0.0001));
            Assert.That(bonuses.MaxHpPercent, Is.EqualTo(1.8).Within(0.0001));
            Assert.That(bonuses.HeavyDamagePercent, Is.EqualTo(1));
            Assert.That(bonuses.MaxTpPercent, Is.EqualTo(3));
            Assert.That(bonuses.SpellAttackPercent, Is.EqualTo(2));
            Assert.That(bonuses.PetDamage, Is.EqualTo(5));
            Assert.That(bonuses.PetHeal, Is.EqualTo(6));
            Assert.That(bonuses.InstanceKillPercent, Is.EqualTo(0.4).Within(0.0001));
            Assert.That(bonuses.SpellPowerPercent, Is.EqualTo(3));
            Assert.That(bonuses.SpellHeal, Is.EqualTo(8));
        }

        [Test]
        public void MoveAndAttackSpeedCapAtDouble()
        {
            _repository.SetRank(SkillTreeStat.MoveSpeed, SkillTreeFormulas.MoveSpeedMaxRank);
            _repository.SetRank(SkillTreeStat.AttackSpeed, SkillTreeFormulas.AttackSpeedMaxRank);

            var bonuses = _repository.GetBonuses();
            Assert.That(bonuses.MoveSpeedPercent, Is.EqualTo(100));
            Assert.That(bonuses.AttackSpeedPercent, Is.EqualTo(100));
        }

        [Test]
        public void CritChanceCapsAtTwentyPercent()
        {
            _repository.SetRank(SkillTreeStat.CritChance, SkillTreeFormulas.CritChanceMaxRank);
            Assert.That(_repository.GetBonuses().CritChancePercent, Is.EqualTo(20));
        }

        [Test]
        public void ResetStateClearsRanksAndLoadedKey()
        {
            _repository.LoadedKey = "host:account:char";
            _repository.SetRank(SkillTreeStat.Luck, 4);

            _repository.ResetState();

            Assert.That(_repository.LoadedKey, Is.Null);
            Assert.That(_repository.TotalSpent, Is.EqualTo(0));
            Assert.That(_repository.GetRank(SkillTreeStat.Luck), Is.EqualTo(0));
        }

        [Test]
        public void MaxRanksMatchRequestedCaps()
        {
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.CritChance), Is.EqualTo(40));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.CritDamage), Is.EqualTo(50));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.GoldDrop), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.ExpGain), Is.EqualTo(70));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.Luck), Is.EqualTo(200));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.MaxHP), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.MaxTP), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.HeavyDamage), Is.EqualTo(40));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.SpellAttack), Is.EqualTo(40));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.PetDamage), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.PetHeal), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.InstanceKill), Is.EqualTo(50));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.SpellPower), Is.EqualTo(100));
            Assert.That(SkillTreeFormulas.GetMaxRank(SkillTreeStat.SpellHeal), Is.EqualTo(100));
        }
    }
}
