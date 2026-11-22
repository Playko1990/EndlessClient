using System;
using System.Collections.Generic;
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
            ReconcileWithServerPoints();
            return _skillTreeProvider.GetAvailablePoints(
                _characterProvider.MainCharacter.Stats[CharacterStat.SkillPoints]);
        }

        public bool TrySpendPoint(SkillTreeStat stat)
        {
            EnsureLoaded();

            if (GetAvailablePoints() <= 0)
                return false;

            var currentRank = _skillTreeProvider.GetRank(stat);
            if (currentRank >= SkillTreeFormulas.GetMaxRank(stat))
                return false;

            _skillTreeRepository.SetRank(stat, currentRank + 1);
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
                file.Sections[key] = new SortedList<string, string>();

            var section = file.Sections[key];
            foreach (SkillTreeStat skillStat in Enum.GetValues(typeof(SkillTreeStat)))
                section[skillStat.ToString()] = _skillTreeProvider.GetRank(skillStat).ToString();

            file.Save();
            _skillTreeRepository.LoadedKey = key;
        }

        public void ClearForCharacter(Character character)
        {
            if (character == null)
                return;

            var host = _configurationProvider.Host;
            var account = _playerInfoProvider.LoggedInAccountName;
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(account))
                return;

            var keysToRemove = new List<string>
            {
                BuildKey(host, account, character.ID)
            };

            // Legacy key used character name; remove so a remade same-name char cannot inherit ranks.
            if (!string.IsNullOrEmpty(character.Name))
                keysToRemove.Add($"{host}:{account}:{character.Name}");

            var file = new IniReader(Constants.SkillTreeFile);
            if (!file.Load())
            {
                ResetIfLoaded(keysToRemove);
                return;
            }

            var removed = false;
            foreach (var key in keysToRemove)
            {
                if (file.Sections.Remove(key))
                    removed = true;
            }

            if (removed)
                file.Save();

            ResetIfLoaded(keysToRemove);
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
                    _skillTreeRepository.SetRank(
                        skillStat,
                        Math.Min(rank, SkillTreeFormulas.GetMaxRank(skillStat)));
                }
            }

            ReconcileWithServerPoints();
        }

        /// <summary>
        /// Local ranks are client-only. If spent points exceed what the server says the
        /// character has earned (e.g. leftover data after a remade character), wipe them.
        /// </summary>
        private void ReconcileWithServerPoints()
        {
            var character = _characterProvider.MainCharacter;
            if (character == null)
                return;

            var serverSkillPoints = character.Stats[CharacterStat.SkillPoints];
            if (_skillTreeProvider.TotalSpent <= serverSkillPoints)
                return;

            _skillTreeRepository.ClearRanks();
            Save();
        }

        private string CurrentKey()
        {
            var host = _configurationProvider.Host;
            var account = _playerInfoProvider.LoggedInAccountName;
            var character = _characterProvider.MainCharacter;

            if (string.IsNullOrEmpty(host) ||
                string.IsNullOrEmpty(account) ||
                character == null ||
                character.ID <= 0)
            {
                return null;
            }

            return BuildKey(host, account, character.ID);
        }

        private static string BuildKey(string host, string account, int characterId) =>
            $"{host}:{account}:{characterId}";

        private void ResetIfLoaded(IEnumerable<string> keys)
        {
            var loaded = _skillTreeRepository.LoadedKey;
            if (string.IsNullOrEmpty(loaded))
                return;

            foreach (var key in keys)
            {
                if (string.Equals(loaded, key, StringComparison.Ordinal))
                {
                    _skillTreeRepository.ResetState();
                    return;
                }
            }
        }
    }

    public interface ISkillTreeActions
    {
        void EnsureLoaded();

        int GetAvailablePoints();

        bool TrySpendPoint(SkillTreeStat stat);

        void Save();

        void ClearForCharacter(Character character);
    }
}
