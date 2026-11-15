using AutomaticTypeMapper;
using EndlessClient.Audio;
using EOLib.Domain.Character;

namespace EndlessClient.Controllers
{
    [MappedType(BaseType = typeof(ITrainingController))]
    public class TrainingController : ITrainingController
    {
        private readonly ITrainingActions _trainingActions;
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISfxPlayer _sfxPlayer;

        public TrainingController(ITrainingActions trainingActions,
                                  ISkillTreeActions skillTreeActions,
                                  ISfxPlayer sfxPlayer)
        {
            _trainingActions = trainingActions;
            _skillTreeActions = skillTreeActions;
            _sfxPlayer = sfxPlayer;
        }

        public void AddStatPoint(CharacterStat whichStat)
        {
            if (InvalidStat(whichStat))
                return;

            _trainingActions.LevelUpStat(whichStat);
            _sfxPlayer.PlaySfx(SoundEffectID.InventoryPickup);
        }

        public void AddSkillTreePoint(SkillTreeStat whichStat)
        {
            if (_skillTreeActions.TrySpendPoint(whichStat))
                _sfxPlayer.PlaySfx(SoundEffectID.InventoryPickup);
        }

        private static bool InvalidStat(CharacterStat whichStat)
        {
            switch (whichStat)
            {
                case CharacterStat.Strength:
                case CharacterStat.Intelligence:
                case CharacterStat.Wisdom:
                case CharacterStat.Agility:
                case CharacterStat.Constitution:
                case CharacterStat.Charisma: return false;
                default: return true;
            }
        }
    }

    public interface ITrainingController
    {
        void AddStatPoint(CharacterStat whichStat);

        void AddSkillTreePoint(SkillTreeStat whichStat);
    }
}
