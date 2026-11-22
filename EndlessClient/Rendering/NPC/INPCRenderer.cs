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
        /// Screen-space bounds covering the visible NPC for the current facing
        /// (opaque body + full drawn frame). Use this for clicks so Left/Right/Up/Down feel the same.
        /// </summary>
        Rectangle GetInteractionBounds();

        void DrawToSpriteBatch(SpriteBatch spriteBatch);

        void StartDying();
    }
}
