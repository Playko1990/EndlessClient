using System;
using EndlessClient.Audio;
using EndlessClient.UIControls;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD
{
    /// <summary>
    /// Large up/down arrows on the panel’s right scroll chrome that scroll content.
    /// Use these instead of the small built-in ScrollBar arrow sprites.
    /// </summary>
    public static class HudPanelContentScrollArrows
    {
        public static (XNAButton Up, XNAButton Down) Create(
            INativeGraphicsManager graphicsManager,
            Vector2 scrollBarPosition,
            Vector2 scrollBarSize,
            float uiScale,
            ScrollBar scrollBar,
            ISfxPlayer sfxPlayer = null)
        {
            if (uiScale <= 0f)
                uiScale = 1f;

            var arrowW = Math.Max(1, (int)Math.Round(HudUiMetrics.NativeScrollArrowWidth * uiScale));
            var arrowH = Math.Max(1, (int)Math.Round(HudUiMetrics.NativeScrollArrowSize * uiScale));

            var sheet = graphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 29);
            const int vertOff = 105; // LightOnMed palette
            var upOut = new Rectangle(0, vertOff + 15 * 3, 16, 15);
            var upOver = new Rectangle(0, vertOff + 15 * 4, 16, 15);
            var downOut = new Rectangle(0, vertOff + 15, 16, 15);
            var downOver = new Rectangle(0, vertOff + 15 * 2, 16, 15);

            var up = new XNAButton(sheet, scrollBarPosition, upOut, upOver);
            SizeArrowButton(up, scrollBarPosition, arrowW, arrowH);

            var downPos = new Vector2(scrollBarPosition.X, scrollBarPosition.Y + scrollBarSize.Y - arrowH);
            var down = new XNAButton(sheet, downPos, downOut, downOver);
            SizeArrowButton(down, downPos, arrowW, arrowH);

            up.OnMouseDown += (_, _) =>
            {
                sfxPlayer?.PlaySfx(SoundEffectID.ButtonClick);
                scrollBar.ScrollBy(-1);
            };
            down.OnMouseDown += (_, _) =>
            {
                sfxPlayer?.PlaySfx(SoundEffectID.ButtonClick);
                down.FlashSpeed = null;
                scrollBar.ScrollBy(1);
            };

            return (up, down);
        }

        private static void SizeArrowButton(XNAButton button, Vector2 position, int width, int height)
        {
            button.DrawArea = new Rectangle((int)position.X, (int)position.Y, width, height);
            button.ClickArea = new Rectangle(0, 0, width, height);
            button.DrawPosition = position;
        }
    }
}
