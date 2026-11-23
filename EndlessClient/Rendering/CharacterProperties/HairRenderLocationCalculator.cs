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
            int hairOpaqueTopY = -1,
            int skinOpaqueCenterX = -1,
            int hairOpaqueCenterX = -1)
        {
            var isFlipped = _renderProperties.IsFacing(EODirection.Up, EODirection.Right);

            float resX;
            if (skinOpaqueCenterX >= 0 && hairOpaqueCenterX >= 0)
            {
                // Align opaque body midpoints in source space. Both skin and hair FlipHorizontally
                // together for Up/Right, so source-space alignment stays correct when flipped.
                resX = skinOpaqueCenterX - hairOpaqueCenterX;
            }
            else
            {
                resX = -(float)Math.Floor(Math.Abs((float)hairRectangle.Width - skinRectangle.Width) / 2) - 1;
            }

            // Small direction nudges — sit heads are not perfectly centered in the cell.
            if (_renderProperties.IsFacing(EODirection.Left))
                resX += 1;
            else if (_renderProperties.IsFacing(EODirection.Down))
                resX += 2;
            else if (_renderProperties.IsFacing(EODirection.Right))
                resX -= 1;

            float headOffsetY;
            if (skinOpaqueTopY >= 0)
            {
                // Align hair's first opaque row to the sit scalp. Hair sheets pad above the
                // spikes — account for that pad, but do NOT add a large extra upward bias
                // (that left bald crown exposed while hair floated above/left of the neck).
                var hairPad = hairOpaqueTopY >= 0 ? hairOpaqueTopY : 0;
                headOffsetY = skinOpaqueTopY - hairPad + 1 - _renderProperties.Gender;
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
