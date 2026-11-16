using System;
using System.Collections.Generic;
using System.Linq;
using AutomaticTypeMapper;

namespace EOLib.Domain.Character
{
    public interface ISkillTreeRepository : IResettable
    {
        string LoadedKey { get; set; }

        IReadOnlyDictionary<SkillTreeStat, int> Ranks { get; }

        void SetRank(SkillTreeStat stat, int rank);

        void ClearRanks();
    }

    public interface ISkillTreeProvider
    {
        string LoadedKey { get; }

        int GetRank(SkillTreeStat stat);

        int TotalSpent { get; }

        int GetAvailablePoints(int serverSkillPoints);

        SkillTreeBonuses GetBonuses();
    }

    [AutoMappedType(IsSingleton = true)]
    public class SkillTreeRepository : ISkillTreeRepository, ISkillTreeProvider
    {
        private readonly Dictionary<SkillTreeStat, int> _ranks;

        public string LoadedKey { get; set; }

        public IReadOnlyDictionary<SkillTreeStat, int> Ranks => _ranks;

        public int TotalSpent => _ranks.Values.Sum();

        public SkillTreeRepository()
        {
            _ranks = CreateEmptyRanks();
            ResetState();
        }

        public int GetRank(SkillTreeStat stat) =>
            _ranks.TryGetValue(stat, out var rank) ? rank : 0;

        public int GetAvailablePoints(int serverSkillPoints) =>
            Math.Max(0, serverSkillPoints - TotalSpent);

        public SkillTreeBonuses GetBonuses() => SkillTreeBonuses.FromRanks(GetRank);

        public void SetRank(SkillTreeStat stat, int rank)
        {
            if (rank < 0)
                throw new ArgumentOutOfRangeException(nameof(rank));

            _ranks[stat] = rank;
        }

        public void ClearRanks()
        {
            foreach (var key in _ranks.Keys.ToList())
                _ranks[key] = 0;
        }

        public void ResetState()
        {
            LoadedKey = null;
            ClearRanks();
        }

        private static Dictionary<SkillTreeStat, int> CreateEmptyRanks()
        {
            var ranks = new Dictionary<SkillTreeStat, int>();
            foreach (SkillTreeStat stat in Enum.GetValues(typeof(SkillTreeStat)))
                ranks[stat] = 0;
            return ranks;
        }
    }
}
