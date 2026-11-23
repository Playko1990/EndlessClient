using System;
using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class ArmorRenderer : BaseCharacterPropertyRenderer
    {
        private readonly ISpriteSheet _armorSheet;
        private readonly ISpriteSheet _skinSheet;
        private readonly SkinRenderLocationCalculator _skinRenderLocationCalculator;

        public override bool CanRender => _armorSheet.HasTexture && _renderProperties.ArmorGraphic != 0;

        public ArmorRenderer(CharacterRenderProperties renderProperties,
                             ISpriteSheet armorSheet,
                             ISpriteSheet skinSheet)
            : base(renderProperties)
        {
            _armorSheet = armorSheet;
            _skinSheet = skinSheet;
            _skinRenderLocationCalculator = new SkinRenderLocationCalculator(_renderProperties);
        }

        public override void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea, WeaponMetadata weaponMetadata)
        {
            if (_renderProperties.SitState != SitState.Standing)
            {
                // Lock armor to the sit skin the same way hair does. DrawArea-only
                // bottom-align missed the skin's sit Y nudge, so chair clothes rode up
                // onto the chin with a bare waist gap.
                var skinLoc = _skinRenderLocationCalculator.CalculateDrawLocationOfCharacterSkin(
                    _skinSheet.SourceRectangle, parentCharacterDrawArea, weaponMetadata.Ranged);
                var drawLoc = skinLoc + GetSitOffsetsRelativeToSkin();
                Render(spriteBatch, _armorSheet, drawLoc);
                return;
            }

            var offsets = GetStandingOffsets(parentCharacterDrawArea.Size.ToVector2(), weaponMetadata.Ranged);
            var drawLocStanding = new Vector2(parentCharacterDrawArea.X - 2 + offsets.X, parentCharacterDrawArea.Y + offsets.Y);
            Render(spriteBatch, _armorSheet, drawLocStanding);
        }

        private Vector2 GetSitOffsetsRelativeToSkin()
        {
            var skin = _skinSheet.SourceRectangle;
            var armor = _armorSheet.SourceRectangle;

            var resX = (float)Math.Floor((skin.Width - armor.Width) / 2f);
            var resY = skin.Height - armor.Height;

            if (_renderProperties.SitState == SitState.Chair)
            {
                resX -= 1;
                if (_renderProperties.IsFacing(EODirection.Left, EODirection.Up))
                    resX += _renderProperties.IsFacing(EODirection.Left) ? 1 : -1;
            }
            else
            {
                resX += _renderProperties.IsFacing(EODirection.Left) ? _renderProperties.Gender
                    : _renderProperties.IsFacing(EODirection.Up) ? -_renderProperties.Gender : 0;
                resX -= _renderProperties.IsFacing(EODirection.Down, EODirection.Up)
                    ? (2 - _renderProperties.Gender)
                    : _renderProperties.Gender;

                if (_renderProperties.SitState == SitState.Floor &&
                    _renderProperties.IsFacing(EODirection.Left, EODirection.Up))
                {
                    resX += _renderProperties.IsFacing(EODirection.Left) ? 1 : -1;
                }
            }

            return new Vector2(resX, resY);
        }

        private Vector2 GetStandingOffsets(Vector2 parentCharacterSize, bool ranged)
        {
            var resX = -(float)Math.Floor(Math.Abs(_armorSheet.SourceRectangle.Width - parentCharacterSize.X) / 2);
            var resY = -(float)Math.Floor(Math.Abs(_armorSheet.SourceRectangle.Height - parentCharacterSize.Y) / 2);

            if ((_renderProperties.RenderAttackFrame == 2 && !ranged) ||
                (_renderProperties.RenderAttackFrame == 1 && ranged))
            {
                resX += _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? 4 : 0;

                if (ranged)
                {
                    var factor = _renderProperties.IsFacing(EODirection.Down, EODirection.Left) ? -1 : 1;
                    resX += _renderProperties.IsFacing(EODirection.Left, EODirection.Up)
                        ? 2 * factor
                        : 4 * factor;

                    resY += _renderProperties.IsFacing(EODirection.Down, EODirection.Right) ? 1 : 0;
                }
            }
            else
            {
                resX += 2;
            }

            resY -= (_renderProperties.IsActing(CharacterActionState.Walking) ? 4 : 3) + _renderProperties.Gender;

            return new Vector2(resX, resY);
        }
    }
}
