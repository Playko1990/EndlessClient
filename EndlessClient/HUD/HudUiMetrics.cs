using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD
{
    /// <summary>
    /// Display scale helpers so classic 640x480 HUD chrome stays readable on large windows.
    /// </summary>
    public static class HudUiMetrics
    {
        public const float PanelScale = 1.35f;
        public const float StatusBarScale = 1.7f;

        public const int NativeInventorySlotSize = 26;

        public static int InventorySlotSize(bool resizable) =>
            resizable ? (int)(NativeInventorySlotSize * PanelScale) : NativeInventorySlotSize;

        public static Rectangle ScaledPanelDrawArea(Texture2D background, bool resizable)
        {
            var width = background.Width;
            var height = background.Height;
            if (resizable)
            {
                width = (int)(width * PanelScale);
                height = (int)(height * PanelScale);
            }

            return new Rectangle(102, 330, width, height);
        }

        public static Vector2 ScalePoint(float x, float y, bool resizable) =>
            resizable ? new Vector2(x * PanelScale, y * PanelScale) : new Vector2(x, y);

        public static Rectangle ScaleRectangle(Rectangle rect, bool resizable)
        {
            if (!resizable)
                return rect;

            return new Rectangle(
                (int)(rect.X * PanelScale),
                (int)(rect.Y * PanelScale),
                (int)(rect.Width * PanelScale),
                (int)(rect.Height * PanelScale));
        }

        public static int Scale(int value, bool resizable) =>
            resizable ? (int)(value * PanelScale) : value;
    }
}
