using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using NUnit.Framework;

namespace EOLib.Test.Domain
{
    [TestFixture]
    public class CharacterRenderPropertiesExtensionsTest
    {
        [Test]
        public void ResetAnimationFrames_WhenSitting_RestoresSittingAction()
        {
            var props = new CharacterRenderProperties.Builder
            {
                SitState = SitState.Floor,
                CurrentAction = CharacterActionState.Emote,
                EmoteFrame = 1
            }.ToImmutable();

            var reset = props.ResetAnimationFrames();

            Assert.That(reset.CurrentAction, Is.EqualTo(CharacterActionState.Sitting));
            Assert.That(reset.SitState, Is.EqualTo(SitState.Floor));
        }

        [Test]
        public void WithNextSpellCastFrame_WhenCompleteWhileSitting_RestoresSittingAction()
        {
            // Frame ticks: 0 0 1 1 1 — advancing from the last frame returns to 0
            var props = new CharacterRenderProperties.Builder
            {
                SitState = SitState.Chair,
                CurrentAction = CharacterActionState.SpellCast,
                ActualSpellCastFrame = CharacterRenderProperties.MAX_NUMBER_OF_WALK_FRAMES - 1
            }.ToImmutable();

            var next = props.WithNextSpellCastFrame();

            Assert.That(next.ActualSpellCastFrame, Is.EqualTo(0));
            Assert.That(next.CurrentAction, Is.EqualTo(CharacterActionState.Sitting));
            Assert.That(next.SitState, Is.EqualTo(SitState.Chair));
        }

        [Test]
        public void WithNextAttackFrame_WhenCompleteWhileSitting_RestoresSittingAction()
        {
            var props = new CharacterRenderProperties.Builder
            {
                SitState = SitState.Floor,
                CurrentAction = CharacterActionState.Attacking,
                ActualAttackFrame = CharacterRenderProperties.MAX_NUMBER_OF_WALK_FRAMES - 1
            }.ToImmutable();

            var next = props.WithNextAttackFrame(isRangedWeapon: false);

            Assert.That(next.ActualAttackFrame, Is.EqualTo(0));
            Assert.That(next.CurrentAction, Is.EqualTo(CharacterActionState.Sitting));
        }

        [Test]
        public void WithNextEmoteFrame_WhenCompleteWhileSitting_RestoresSittingAction()
        {
            var props = new CharacterRenderProperties.Builder
            {
                SitState = SitState.Floor,
                CurrentAction = CharacterActionState.Emote,
                EmoteFrame = CharacterRenderProperties.MAX_NUMBER_OF_EMOTE_FRAMES - 1
            }.ToImmutable();

            var next = props.WithNextEmoteFrame();

            Assert.That(next.EmoteFrame, Is.EqualTo(0));
            Assert.That(next.CurrentAction, Is.EqualTo(CharacterActionState.Sitting));
        }
    }
}
