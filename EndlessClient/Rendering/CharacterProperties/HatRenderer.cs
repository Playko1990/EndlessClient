using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class HatRenderer : BaseCharacterPropertyRenderer
    {
        private readonly ISpriteSheet _hatSheet;
        private readonly ISpriteSheet _hairSheet;
        private readonly ISpriteSheet _skinSheet;
        private readonly HairRenderLocationCalculator _hairRenderLocationCalculator;
        private readonly SkinRenderLocationCalculator _skinRenderLocationCalculator;

        public override bool CanRender => _hatSheet.HasTexture && _renderProperties.HatGraphic != 0;

        public HatRenderer(CharacterRenderProperties renderProperties,
                           ISpriteSheet hatSheet,
                           ISpriteSheet hairSheet,
                           ISpriteSheet skinSheet)
            : base(renderProperties)
        {
            _hatSheet = hatSheet;
            _hairSheet = hairSheet;
            _skinSheet = skinSheet;

            _hairRenderLocationCalculator = new HairRenderLocationCalculator(_renderProperties);
            _skinRenderLocationCalculator = new SkinRenderLocationCalculator(_renderProperties);
        }

        public override void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea, WeaponMetadata weaponMetadata)
        {
            Vector2 hairDrawLoc;
            if (_renderProperties.SitState != SitState.Standing)
            {
                var skinLoc = _skinRenderLocationCalculator.CalculateDrawLocationOfCharacterSkin(
                    _skinSheet.SourceRectangle, parentCharacterDrawArea, weaponMetadata.Ranged);
                hairDrawLoc = _hairRenderLocationCalculator.CalculateDrawLocationOfCharacterHairFromSkin(
                    _hairSheet.SourceRectangle, _skinSheet.SourceRectangle, skinLoc);
            }
            else
            {
                hairDrawLoc = _hairRenderLocationCalculator.CalculateDrawLocationOfCharacterHair(
                    _hairSheet.SourceRectangle, parentCharacterDrawArea, weaponMetadata.Ranged);
            }

            var offsets = GetOffsets();

            Render(spriteBatch, _hatSheet, hairDrawLoc + offsets);
        }

        private Vector2 GetOffsets()
        {
            var xOff = 0f;
            var yOff = -3f;

            var flippedOffset = _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? -2 : 0;

            return new Vector2(xOff + flippedOffset, yOff);
        }
    }
}
