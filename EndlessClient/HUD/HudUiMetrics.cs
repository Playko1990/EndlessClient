using EndlessClient.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD
{
    /// <summary>
    /// Display scale helpers so classic 640x480 HUD chrome stays readable on large windows.
    /// </summary>
    public static class HudUiMetrics
    {
        // Panel size the user liked.
        public const float PanelScale = 1.85f;

        // Slightly enlarged vs classic 14px bars; less than the previous 3x pass.
        public const float StatusBarScale = 2.0f;

        // Native HUD side-button stride in classic layout.
        public const int NativeSideButtonStep = 20;

        public const int NativeInventorySlotSize = 26;

        public static bool ShouldEnlargeHud(bool resizable, int windowWidth = 0) =>
            resizable || windowWidth > ClientWindowSizeRepository.DEFAULT_BACKBUFFER_WIDTH;

        public static int InventorySlotSize(bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(NativeInventorySlotSize * PanelScale) : NativeInventorySlotSize;

        public static int SideButtonStep(bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(NativeSideButtonStep * PanelScale) : NativeSideButtonStep;

        public static Rectangle ScaledPanelDrawArea(Texture2D background, bool resizable)
        {
            var width = background.Width;
            var height = background.Height;
            if (ShouldEnlargeHud(resizable))
            {
                width = (int)(width * PanelScale);
                height = (int)(height * PanelScale);
            }

            return new Rectangle(102, 330, width, height);
        }

        public static Vector2 ScalePoint(float x, float y, bool resizable) =>
            ShouldEnlargeHud(resizable) ? new Vector2(x * PanelScale, y * PanelScale) : new Vector2(x, y);

        public static Rectangle ScaleRectangle(Rectangle rect, bool resizable)
        {
            if (!ShouldEnlargeHud(resizable))
                return rect;

            return new Rectangle(
                (int)(rect.X * PanelScale),
                (int)(rect.Y * PanelScale),
                (int)(rect.Width * PanelScale),
                (int)(rect.Height * PanelScale));
        }

        public static int Scale(int value, bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(value * PanelScale) : value;
    }
}
