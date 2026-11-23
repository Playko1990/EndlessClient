using System;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class HairRenderLocationCalculator
    {
        private readonly CharacterRenderProperties _renderProperties;

        public HairRenderLocationCalculator(CharacterRenderProperties renderProperties)
        {
            _renderProperties = renderProperties;
        }

        public Vector2 CalculateDrawLocationOfCharacterHair(Rectangle hairRectangle, Rectangle parentCharacterDrawArea, bool ranged)
        {
            var resX = -(float)Math.Floor(Math.Abs((float)hairRectangle.Width - parentCharacterDrawArea.Width) / 2) - 1;
            var resY = -(float)Math.Floor(Math.Abs(hairRectangle.Height - (parentCharacterDrawArea.Height / 2f)) / 2) - _renderProperties.Gender;

            var isFlipped = _renderProperties.IsFacing(EODirection.Up, EODirection.Right);

            if (ranged && _renderProperties.RenderAttackFrame == 1)
            {
                var rangedXOff = _renderProperties.Gender == 0 ? 1 : 3;
                resX += rangedXOff * (isFlipped ? 1 : -1);
                resY += _renderProperties.IsFacing(EODirection.Down, EODirection.Right) ? _renderProperties.Gender : 0;
            }
            else if (_renderProperties.RenderAttackFrame == 2)
            {
                resX += isFlipped ? 4 : -4;
                resX += _renderProperties.IsFacing(EODirection.Up)
                    ? _renderProperties.Gender * -2
                    : _renderProperties.IsFacing(EODirection.Left)
                        ? _renderProperties.Gender * 2
                        : 0;

                resY += _renderProperties.IsFacing(EODirection.Up, EODirection.Left) ? 1 : 5;
                resY -= _renderProperties.IsFacing(EODirection.Right, EODirection.Down) ? _renderProperties.Gender : 0;
            }

            var flippedOffset = isFlipped ? 2 : 0;
            return parentCharacterDrawArea.Location.ToVector2() + new Vector2(resX + flippedOffset, resY);
        }

        /// <summary>
        /// Positions hair relative to the already-computed sitting skin location so hair
        /// stays on the head when equipment/skin use bottom-aligned sit sprites.
        /// </summary>
        public Vector2 CalculateDrawLocationOfCharacterHairFromSkin(
            Rectangle hairRectangle,
            Rectangle skinRectangle,
            Vector2 skinDrawLocation,
            int skinOpaqueTopY = -1,
            int hairOpaqueTopY = -1)
        {
            var isFlipped = _renderProperties.IsFacing(EODirection.Up, EODirection.Right);

            // Center hair on the skin sprite horizontally, with the same X tweaks used before.
            var resX = -(float)Math.Floor(Math.Abs((float)hairRectangle.Width - skinRectangle.Width) / 2) - 1;
            resX -= 3;

            if (_renderProperties.SitState == SitState.Floor &&
                _renderProperties.IsFacing(EODirection.Left, EODirection.Up))
            {
                resX += _renderProperties.IsFacing(EODirection.Left) ? 2 : -2;
            }

            float headOffsetY;
            if (skinOpaqueTopY >= 0)
            {
                // Align the hair gfx's first opaque row with the sit scalp. Hair sheets often
                // have transparent padding above the spikes — placing the texture top at the
                // scalp leaves the bald sit head exposed above the hair (~8-12px too low).
                var hairPad = hairOpaqueTopY >= 0 ? hairOpaqueTopY : 0;
                // Extra -4 so spikes cover the crown (sit skins bake a bald head underneath).
                headOffsetY = skinOpaqueTopY - hairPad - 4 - _renderProperties.Gender;
            }
            else
            {
                headOffsetY = _renderProperties.SitState == SitState.Floor
                    ? (_renderProperties.IsFacing(EODirection.Right, EODirection.Down) ? 16 : 18) + _renderProperties.Gender
                    : (_renderProperties.IsFacing(EODirection.Right, EODirection.Down) ? 0 : 2) + _renderProperties.Gender;
            }

            var flippedOffset = isFlipped ? 2 : 0;
            return skinDrawLocation + new Vector2(resX + flippedOffset, headOffsetY);
        }
    }
}
