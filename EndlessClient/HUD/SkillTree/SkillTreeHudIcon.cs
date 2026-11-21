using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.SkillTree
{
    /// <summary>
    /// Builds a Passive Spells / skill-tree HUD button that matches other HUD button chrome
    /// and draws a conifer-style tree glyph instead of the default funky icon.
    /// </summary>
    public static class SkillTreeHudIcon
    {
        public static Texture2D Create(GraphicsDevice graphicsDevice, Texture2D hudButtonSheet, int buttonIndex)
        {
            var widthDelta = hudButtonSheet.Width / 2;
            var heightDelta = hudButtonSheet.Height / 11;

            // Use Inventory button (index 0) chrome so the frame/color matches neighboring buttons.
            var chromeIndex = 0;
            var outRect = new Rectangle(0, heightDelta * chromeIndex, widthDelta, heightDelta);
            var overRect = new Rectangle(widthDelta, heightDelta * chromeIndex, widthDelta, heightDelta);

            var texture = new Texture2D(graphicsDevice, widthDelta * 2, heightDelta);
            var data = new Color[texture.Width * texture.Height];

            CopyRegion(hudButtonSheet, outRect, data, texture.Width, 0, 0);
            CopyRegion(hudButtonSheet, overRect, data, texture.Width, widthDelta, 0);

            DrawPineTree(data, texture.Width, 0, 0, widthDelta, heightDelta);
            DrawPineTree(data, texture.Width, widthDelta, 0, widthDelta, heightDelta);

            texture.SetData(data);
            return texture;
        }

        public static Rectangle OutSource(Texture2D texture) =>
            new Rectangle(0, 0, texture.Width / 2, texture.Height);

        public static Rectangle OverSource(Texture2D texture) =>
            new Rectangle(texture.Width / 2, 0, texture.Width / 2, texture.Height);

        private static void CopyRegion(Texture2D source, Rectangle sourceRect, Color[] dest, int destWidth, int destX, int destY)
        {
            var buffer = new Color[sourceRect.Width * sourceRect.Height];
            source.GetData(0, sourceRect, buffer, 0, buffer.Length);

            for (int y = 0; y < sourceRect.Height; y++)
            {
                for (int x = 0; x < sourceRect.Width; x++)
                {
                    dest[(destY + y) * destWidth + (destX + x)] = buffer[y * sourceRect.Width + x];
                }
            }
        }

        private static void DrawPineTree(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            // Clear the copied inventory glyph so only chrome + our tree remain.
            ClearIconInterior(data, stride, offsetX, offsetY, width, height);

            var centerX = offsetX + width / 2;
            var top = offsetY + Math.Max(2, height / 8);
            var bottom = offsetY + height - Math.Max(3, height / 6);
            var trunkTop = bottom - Math.Max(3, height / 5);
            var trunkWidth = Math.Max(2, width / 8);

            var needle = new Color(0x2f, 0x6b, 0x32, 255);
            var needleHi = new Color(0x4a, 0x8f, 0x45, 255);
            var trunk = new Color(0x6b, 0x48, 0x2b, 255);

            // Three stacked triangles for a 🌲 silhouette.
            FillTriangle(data, stride, centerX, top, width / 2 - 2, top + (trunkTop - top) / 3, needleHi);
            FillTriangle(data, stride, centerX, top + (trunkTop - top) / 5, width / 2 - 1, top + 2 * (trunkTop - top) / 3, needle);
            FillTriangle(data, stride, centerX, top + (trunkTop - top) / 3, width / 2, trunkTop, needle);

            for (int y = trunkTop; y < bottom; y++)
            {
                for (int x = centerX - trunkWidth / 2; x <= centerX + trunkWidth / 2; x++)
                    SetPixel(data, stride, x, y, trunk);
            }
        }

        private static void ClearIconInterior(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            // Keep a 1px chrome border; wipe the interior icon area to the average brown of EO buttons.
            var fill = new Color(0xb8, 0xa0, 0x78, 255);
            for (int y = offsetY + 2; y < offsetY + height - 2; y++)
            {
                for (int x = offsetX + 2; x < offsetX + width - 2; x++)
                    SetPixel(data, stride, x, y, fill);
            }
        }

        private static void FillTriangle(Color[] data, int stride, int apexX, int apexY, int halfWidth, int baseY, Color color)
        {
            if (baseY <= apexY)
                return;

            for (int y = apexY; y <= baseY; y++)
            {
                var t = (y - apexY) / (float)(baseY - apexY);
                var w = Math.Max(1, (int)Math.Round(halfWidth * t));
                for (int x = apexX - w; x <= apexX + w; x++)
                    SetPixel(data, stride, x, y, color);
            }
        }

        private static void SetPixel(Color[] data, int stride, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= stride)
                return;
            var index = y * stride + x;
            if (index >= 0 && index < data.Length)
                data[index] = color;
        }
    }
}
