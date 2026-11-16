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

            var bonuses = _repository.GetBonuses();

            Assert.That(bonuses.CritChancePercent, Is.EqualTo(2));
            Assert.That(bonuses.CritDamagePercent, Is.EqualTo(5));
            Assert.That(bonuses.MoveSpeedPercent, Is.EqualTo(12));
            Assert.That(bonuses.AttackSpeedPercent, Is.EqualTo(9));
            Assert.That(bonuses.GoldDropPercent, Is.EqualTo(10));
            Assert.That(bonuses.ExpGainPercent, Is.EqualTo(4));
            Assert.That(bonuses.Luck, Is.EqualTo(7));
            Assert.That(bonuses.MaxHp, Is.EqualTo(90));
            Assert.That(bonuses.HeavyDamagePercent, Is.EqualTo(6));
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
    }
}
