using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.Rendering.Sprites;
using EOLib.Domain.Character;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class HairRenderer : BaseCharacterPropertyRenderer
    {
        private readonly ISpriteSheet _hairSheet;
        private readonly ISpriteSheet _skinSheet;
        private readonly HairRenderLocationCalculator _hairRenderLocationCalculator;
        private readonly SkinRenderLocationCalculator _skinRenderLocationCalculator;

        public override bool CanRender => _hairSheet.HasTexture && _renderProperties.HairStyle != 0;

        public HairRenderer(CharacterRenderProperties renderProperties,
                            ISpriteSheet hairSheet,
                            ISpriteSheet skinSheet)
            : base(renderProperties)
        {
            _hairSheet = hairSheet;
            _skinSheet = skinSheet;
            _hairRenderLocationCalculator = new HairRenderLocationCalculator(_renderProperties);
            _skinRenderLocationCalculator = new SkinRenderLocationCalculator(_renderProperties);
        }

        public override void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea, WeaponMetadata weaponMetadata)
        {
            Vector2 drawLoc;
            if (_renderProperties.SitState != SitState.Standing)
            {
                // Keep hair locked to the sitting skin (same approach as FaceRenderer) so it
                // cannot drift above a bottom-aligned sit body the way the old independent
                // magic-number Y offsets did.
                var skinLoc = _skinRenderLocationCalculator.CalculateDrawLocationOfCharacterSkin(
                    _skinSheet.SourceRectangle, parentCharacterDrawArea, weaponMetadata.Ranged);
                drawLoc = _hairRenderLocationCalculator.CalculateDrawLocationOfCharacterHairFromSkin(
                    _hairSheet.SourceRectangle, _skinSheet.SourceRectangle, skinLoc);
            }
            else
            {
                drawLoc = _hairRenderLocationCalculator.CalculateDrawLocationOfCharacterHair(
                    _hairSheet.SourceRectangle, parentCharacterDrawArea, weaponMetadata.Ranged);
            }

            Render(spriteBatch, _hairSheet, drawLoc);
        }
    }
}
