using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.NPC
{
    public interface INPCRenderer : IDrawable, IUpdateable, IGameComponent, IDisposable, IMapActor
    {
        EOLib.Domain.NPC.NPC NPC { get; set; }

        bool IsDead { get; }

        bool IsClickablePixel(Point currentMousePosition);

        /// <summary>
        /// Screen-space click box matching the drawn NPC sprite size.
        /// </summary>
        Rectangle GetInteractionBounds();

        void DrawToSpriteBatch(SpriteBatch spriteBatch);

        void StartDying();
    }
}
