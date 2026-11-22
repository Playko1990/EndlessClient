using EOLib.Domain.Spells;
using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering
{
    public interface IMapActor
    {
        int NameLabelY { get; }

        int HorizontalCenter { get; }

        /// <summary>
        /// Screen Y used to park floating HP bars (sprite top / opaque top — not name label).
        /// </summary>
        int HealthBarAnchorY { get; }

        bool IsAlive { get; }

        ISpellTargetable SpellTarget { get; }

        Rectangle DrawArea { get; }

        void ShowDamageCounter(int damage, int percentHealth, bool isHeal);

        void ShowChatBubble(string text, bool isGroupChat = false);

        bool EffectIsPlaying();

        void PlayEffect(int effectId);
    }
}
