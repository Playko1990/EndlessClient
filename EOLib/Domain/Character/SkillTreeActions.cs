using System;
using AutomaticTypeMapper;
using EOLib.Config;
using EOLib.Domain.Login;
using EOLib.Shared;

namespace EOLib.Domain.Character
{
    [AutoMappedType]
    public class SkillTreeActions : ISkillTreeActions
    {
        private readonly ISkillTreeRepository _skillTreeRepository;
        private readonly ISkillTreeProvider _skillTreeProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly IPlayerInfoProvider _playerInfoProvider;
        private readonly IConfigurationProvider _configurationProvider;

        public SkillTreeActions(ISkillTreeRepository skillTreeRepository,
                                ISkillTreeProvider skillTreeProvider,
                                ICharacterProvider characterProvider,
                                IPlayerInfoProvider playerInfoProvider,
                                IConfigurationProvider configurationProvider)
        {
            _skillTreeRepository = skillTreeRepository;
            _skillTreeProvider = skillTreeProvider;
            _characterProvider = characterProvider;
            _playerInfoProvider = playerInfoProvider;
            _configurationProvider = configurationProvider;
        }

        public void EnsureLoaded()
        {
            var key = CurrentKey();
            if (string.IsNullOrEmpty(key) || _skillTreeRepository.LoadedKey == key)
                return;

            Load(key);
        }

        public int GetAvailablePoints()
        {
            EnsureLoaded();
            return _skillTreeProvider.GetAvailablePoints(
                _characterProvider.MainCharacter.Stats[CharacterStat.SkillPoints]);
        }

        public bool TrySpendPoint(SkillTreeStat stat)
        {
            EnsureLoaded();

            if (GetAvailablePoints() <= 0)
                return false;

            var nextRank = _skillTreeProvider.GetRank(stat) + 1;
            _skillTreeRepository.SetRank(stat, nextRank);
            Save();
            return true;
        }

        public void Save()
        {
            var key = _skillTreeRepository.LoadedKey ?? CurrentKey();
            if (string.IsNullOrEmpty(key))
                return;

            var file = new IniReader(Constants.SkillTreeFile);
            file.Load();

            if (!file.Sections.ContainsKey(key))
                file.Sections[key] = new System.Collections.Generic.SortedList<string, string>();

            var section = file.Sections[key];
            foreach (SkillTreeStat skillStat in Enum.GetValues(typeof(SkillTreeStat)))
                section[skillStat.ToString()] = _skillTreeProvider.GetRank(skillStat).ToString();

            file.Save();
            _skillTreeRepository.LoadedKey = key;
        }

        private void Load(string key)
        {
            _skillTreeRepository.ClearRanks();
            _skillTreeRepository.LoadedKey = key;

            var file = new IniReader(Constants.SkillTreeFile);
            if (!file.Load() || !file.Sections.ContainsKey(key))
                return;

            var section = file.Sections[key];
            foreach (SkillTreeStat skillStat in Enum.GetValues(typeof(SkillTreeStat)))
            {
                if (section.TryGetValue(skillStat.ToString(), out var value) &&
                    int.TryParse(value, out var rank) &&
                    rank > 0)
                {
                    _skillTreeRepository.SetRank(skillStat, rank);
                }
            }
        }

        private string CurrentKey()
        {
            var host = _configurationProvider.Host;
            var account = _playerInfoProvider.LoggedInAccountName;
            var character = _characterProvider.MainCharacter?.Name;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(account) || string.IsNullOrEmpty(character))
                return null;

            return $"{host}:{account}:{character}";
        }
    }

    public interface ISkillTreeActions
    {
        void EnsureLoaded();

        int GetAvailablePoints();

        bool TrySpendPoint(SkillTreeStat stat);

        void Save();
    }
}
