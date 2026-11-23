using System;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class SkinRenderLocationCalculator
    {
        private readonly CharacterRenderProperties _renderProperties;

        public SkinRenderLocationCalculator(CharacterRenderProperties renderProperties)
        {
            _renderProperties = renderProperties;
        }

        private const int SitHeadBandHeight = 14;

        /// <summary>
        /// First opaque pixel row within the sheet source rectangle, or -1 if none.
        /// </summary>
        public static int FindOpaqueTopY(ISpriteSheet sheet) =>
            TryGetOpaqueBounds(sheet, headBandOnly: false, out _, out var topY) ? topY : -1;

        /// <summary>
        /// Horizontal mid of opaque pixels. For sit skins, limit to a band under the scalp
        /// so hair aligns to the head — not the full sitting body (which is wider/offset).
        /// </summary>
        public static int FindOpaqueCenterX(ISpriteSheet sheet, bool headBandOnly = false) =>
            TryGetOpaqueBounds(sheet, headBandOnly, out var centerX, out _) ? centerX : -1;

        private static bool TryGetOpaqueBounds(ISpriteSheet sheet, bool headBandOnly, out int centerX, out int topY)
        {
            centerX = -1;
            topY = -1;

            if (sheet == null || !sheet.HasTexture)
                return false;

            var rect = sheet.SourceRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
                return false;

            var data = sheet.GetSourceTextureData<Color>();
            if (data.Length == 0)
                return false;

            var minY = rect.Height;
            for (var y = 0; y < rect.Height; y++)
            {
                var row = y * rect.Width;
                for (var x = 0; x < rect.Width; x++)
                {
                    if (data[row + x].A > 16)
                    {
                        minY = y;
                        y = rect.Height;
                        break;
                    }
                }
            }

            if (minY >= rect.Height)
                return false;

            topY = minY;
            var yEnd = headBandOnly
                ? Math.Min(rect.Height, minY + SitHeadBandHeight)
                : rect.Height;

            var minX = rect.Width;
            var maxX = -1;
            for (var y = minY; y < yEnd; y++)
            {
                var row = y * rect.Width;
                for (var x = 0; x < rect.Width; x++)
                {
                    if (data[row + x].A <= 16)
                        continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
            }

            if (maxX < minX)
                return false;

            centerX = (minX + maxX + 1) / 2;
            return true;
        }

        public Vector2 CalculateDrawLocationOfCharacterSkin(Rectangle skinRectangle, Rectangle parentCharacterDrawArea, bool ranged)
        {
            float resX, resY;

            // Ranged weapon attack frame is offset based on the Left or Right border of the parent character draw area
            // Ranged weapon graphics do not use the centering approach
            if (ranged && _renderProperties.RenderAttackFrame == 1)
            {
                var isFlipped = _renderProperties.IsFacing(EODirection.Right, EODirection.Up);
                var needsExtraOffset = _renderProperties.IsFacing(EODirection.Right, EODirection.Down);

                var startPixelX = isFlipped ? parentCharacterDrawArea.Left : parentCharacterDrawArea.Right;
                var offsetFactor = isFlipped ? 0 : -1;

                resX = startPixelX + ((needsExtraOffset ? 2 : 0) + skinRectangle.Width) * offsetFactor;

                // essentially, is it facing EODirection.Right?
                if (isFlipped && needsExtraOffset)
                    resX += 2;

                // male needs an extra +/- 2
                if (_renderProperties.Gender == 1)
                    resX += 2 * (isFlipped ? 1 : -1);

                resY = parentCharacterDrawArea.Y + (needsExtraOffset ? 1 : 0);
            }
            else if (_renderProperties.SitState != SitState.Standing)
            {
                var factor = _renderProperties.IsFacing(EODirection.Down, EODirection.Left) ? -1 : 1;

                resX = -(int)Math.Floor((float)(skinRectangle.Width - parentCharacterDrawArea.Width));
                resY = parentCharacterDrawArea.Bottom - skinRectangle.Height;

                resX += _renderProperties.IsFacing(EODirection.Down, EODirection.Right) ? 2 * factor : 3 * factor;

                var genderAdjustFactor = (_renderProperties.IsFacing(EODirection.Left, EODirection.Up) ? 0 : 1) * _renderProperties.Gender;
                resX += parentCharacterDrawArea.X + (genderAdjustFactor * factor);

                resY += (_renderProperties.IsFacing(EODirection.Left, EODirection.Up) ? 7 : 5) - _renderProperties.Gender;

                if (_renderProperties.SitState == SitState.Floor)
                {
                    resX += _renderProperties.IsFacing(EODirection.Left, EODirection.Up) ? -factor : 0;
                    resY += _renderProperties.IsFacing(EODirection.Left, EODirection.Up) ? 1 : 0;
                }
            }
            else
            {
                resX = -(float)Math.Floor(Math.Abs((float)skinRectangle.Width - parentCharacterDrawArea.Width) / 2);
                resY = -(float)Math.Floor(Math.Abs((float)skinRectangle.Height - parentCharacterDrawArea.Height) / 2);

                if (_renderProperties.IsActing(CharacterActionState.SpellCast))
                    resY -= 2;

                // This specific frame is a bitch
                if (_renderProperties.Gender == 1 && _renderProperties.RenderAttackFrame == 1)
                    resX += _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? 2 : -2;

                resX += parentCharacterDrawArea.X;
                resY += parentCharacterDrawArea.Y;
            }

            return new Vector2(resX, resY);
        }
    }
}
