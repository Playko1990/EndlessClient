using System;
using EndlessClient.Rendering.Factories;
using EOLib;
using EOLib.Domain.Character;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Input.InputListeners;
using XNAControls;

namespace EndlessClient.UIControls
{
    public class CreateCharacterControl : CharacterControl
    {
        public event EventHandler Clicked;

        // default properties
        public CreateCharacterControl(ICharacterRendererFactory characterRendererFactory)
            : this(GetDefaultProperties(), characterRendererFactory) { }

        // custom render properties
        public CreateCharacterControl(CharacterRenderProperties renderProperties, ICharacterRendererFactory characterRendererFactory)
            : base(Character.Default.WithRenderProperties(renderProperties.WithDirection(EODirection.Down)), characterRendererFactory)
        {
            SetSize(99, 123);
        }

        protected override Vector2 GetRendererScreenPosition()
        {
            return new Vector2(DrawPositionWithParentOffset.X + 40,
                               DrawPositionWithParentOffset.Y + 36);
        }

        protected override bool HandleMouseDown(IXNAControl control, MouseEventArgs eventArgs)
        {
            var nextDirectionInt = (int)RenderProperties.Direction + 1;
            var nextDirection = (EODirection)(nextDirectionInt % 4);
            RenderProperties = RenderProperties.WithDirection(nextDirection);

            Clicked?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public void NextGender()
        {
            RenderProperties = RenderProperties.WithGender((RenderProperties.Gender + 1) % 2);
        }

        public void NextRace()
        {
            // EO skins are 0-6 (7 rows on the skin sheet)
            RenderProperties = RenderProperties.WithRace((RenderProperties.Race + 1) % 7);
        }

        public void NextHairStyle()
        {
            RenderProperties = RenderProperties.WithHairStyle((RenderProperties.HairStyle + 1) % 21);
        }

        public void NextHairColor()
        {
            RenderProperties = RenderProperties.WithHairColor((RenderProperties.HairColor + 1) % 10);
        }

        private static CharacterRenderProperties GetDefaultProperties()
        {
            return new CharacterRenderProperties.Builder { HairStyle = 1 }.ToImmutable();
        }
    }
}
