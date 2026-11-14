using System.Collections.Generic;
using AutomaticTypeMapper;
using EOLib;

namespace EndlessClient.HUD.Boss
{
    public interface IBossHealthProvider
    {
        IReadOnlyDictionary<int, int> PercentHealthByNpcIndex { get; }
    }

    public interface IBossHealthRepository : IBossHealthProvider
    {
        void SetPercentHealth(int npcIndex, int percentHealth);

        void Remove(int npcIndex);

        void Clear();
    }

    [AutoMappedType(IsSingleton = true)]
    public class BossHealthRepository : IBossHealthRepository, IResettable
    {
        private readonly Dictionary<int, int> _percentHealthByNpcIndex = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> PercentHealthByNpcIndex => _percentHealthByNpcIndex;

        public void SetPercentHealth(int npcIndex, int percentHealth)
        {
            _percentHealthByNpcIndex[npcIndex] = System.Math.Clamp(percentHealth, 0, 100);
        }

        public void Remove(int npcIndex)
        {
            _percentHealthByNpcIndex.Remove(npcIndex);
        }

        public void Clear()
        {
            _percentHealthByNpcIndex.Clear();
        }

        public void ResetState()
        {
            Clear();
        }
    }
}
