using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.SkillTree
{
    /// <summary>
    /// Builds the Skill Tree HUD button by compositing the reference tree artwork
    /// onto the same wood chrome fill used by the neighboring HUD icons.
    /// </summary>
    public static class SkillTreeHudIcon
    {
        private const string EmbeddedIconName = "SkillTreeIcon.png";

        // Fallback only if chrome sampling fails (matches typical EO side-button wood).
        private static readonly Color FallbackWood = new Color(0x7a, 0x5a, 0x3a, 255);

        public static Texture2D Create(GraphicsDevice graphicsDevice, Texture2D hudButtonSheet, int buttonIndex)
        {
            var widthDelta = hudButtonSheet.Width / 2;
            var heightDelta = hudButtonSheet.Height / 11;

            // Use Inventory button (index 0) chrome so the frame matches neighboring buttons.
            var chromeIndex = 0;
            var outRect = new Rectangle(0, heightDelta * chromeIndex, widthDelta, heightDelta);
            var overRect = new Rectangle(widthDelta, heightDelta * chromeIndex, widthDelta, heightDelta);

            var texture = new Texture2D(graphicsDevice, widthDelta * 2, heightDelta);
            var data = new Color[texture.Width * texture.Height];

            CopyRegion(hudButtonSheet, outRect, data, texture.Width, 0, 0);
            CopyRegion(hudButtonSheet, overRect, data, texture.Width, widthDelta, 0);

            // Match the wood fill from the copied chrome (same as other side icons),
            // not the muddy brown from the reference art's background.
            var woodOut = SampleWoodFill(data, texture.Width, 0, 0, widthDelta, heightDelta);
            var woodOver = SampleWoodFill(data, texture.Width, widthDelta, 0, widthDelta, heightDelta);

            ClearIconInterior(data, texture.Width, 0, 0, widthDelta, heightDelta, woodOut);
            ClearIconInterior(data, texture.Width, widthDelta, 0, widthDelta, heightDelta, woodOver);

            using (var tree = LoadTreeTexture(graphicsDevice))
            {
                var treePixels = new Color[tree.Width * tree.Height];
                tree.GetData(treePixels);

                BlitCentered(data, texture.Width, 0, 0, widthDelta, heightDelta,
                    treePixels, tree.Width, tree.Height);
                BlitCentered(data, texture.Width, widthDelta, 0, widthDelta, heightDelta,
                    treePixels, tree.Width, tree.Height);
            }

            texture.SetData(data);
            return texture;
        }

        public static Rectangle OutSource(Texture2D texture) =>
            new Rectangle(0, 0, texture.Width / 2, texture.Height);

        public static Rectangle OverSource(Texture2D texture) =>
            new Rectangle(texture.Width / 2, 0, texture.Width / 2, texture.Height);

        /// <summary>
        /// Loads the transparent canopy/node-graph tree symbol (same art as the HUD button).
        /// </summary>
        public static Texture2D LoadTreeTexture(GraphicsDevice graphicsDevice)
        {
            var assembly = typeof(SkillTreeHudIcon).Assembly;
            using (var stream = assembly.GetManifestResourceStream(EmbeddedIconName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Missing embedded resource '{EmbeddedIconName}'.");

                // FromStream needs a seekable stream on some platforms.
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    memory.Position = 0;
                    return Texture2D.FromStream(graphicsDevice, memory);
                }
            }
        }

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

        /// <summary>
        /// Average the wood-panel pixels at the chrome corners (where inventory art
        /// does not cover) so the Skill Tree button fill matches its neighbors.
        /// </summary>
        private static Color SampleWoodFill(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            long sumR = 0, sumG = 0, sumB = 0;
            var count = 0;

            void Sample(int x, int y)
            {
                if (x < offsetX || y < offsetY || x >= offsetX + width || y >= offsetY + height)
                    return;

                var c = data[y * stride + x];
                if (c.A < 200)
                    return;

                // Skip black frame / outline pixels.
                if (c.R < 40 && c.G < 40 && c.B < 40)
                    return;

                // Skip saturated icon ink (inventory glyph) — keep muted wood browns.
                if (c.G > c.R + 15 && c.G > c.B + 15)
                    return;

                sumR += c.R;
                sumG += c.G;
                sumB += c.B;
                count++;
            }

            // 4x4 patches in each interior corner of the button chrome.
            const int inset = 2;
            const int patch = 4;
            for (int j = 0; j < patch; j++)
            {
                for (int i = 0; i < patch; i++)
                {
                    Sample(offsetX + inset + i, offsetY + inset + j);
                    Sample(offsetX + width - inset - 1 - i, offsetY + inset + j);
                    Sample(offsetX + inset + i, offsetY + height - inset - 1 - j);
                    Sample(offsetX + width - inset - 1 - i, offsetY + height - inset - 1 - j);
                }
            }

            // Also sample along the top edge center (usually plain wood under the frame).
            var midX = offsetX + width / 2;
            for (int i = -3; i <= 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    Sample(midX + i, offsetY + inset + j);
            }

            if (count == 0)
                return FallbackWood;

            return new Color((int)(sumR / count), (int)(sumG / count), (int)(sumB / count), 255);
        }

        private static void ClearIconInterior(Color[] data, int stride, int offsetX, int offsetY, int width, int height, Color fill)
        {
            // Preserve the 2px chrome frame copied from the sheet.
            for (int y = offsetY + 2; y < offsetY + height - 2; y++)
            {
                for (int x = offsetX + 2; x < offsetX + width - 2; x++)
                    data[y * stride + x] = fill;
            }
        }

        private static bool IsBackdropOrFringe(Color c)
        {
            // Fully/nearly transparent already handled by caller.
            // Near-black bake-in from old exports (not outline ink on the tree).
            if (c.R < 28 && c.G < 28 && c.B < 28 && c.A < 250)
                return true;

            // Light gray / yellow fringe left by JPG/keying.
            if (c.R > 175 && c.G > 175 && c.B > 160 && Math.Abs(c.R - c.G) < 24)
                return true;

            return false;
        }

        private static void BlitCentered(
            Color[] dest,
            int stride,
            int offsetX,
            int offsetY,
            int buttonWidth,
            int buttonHeight,
            Color[] src,
            int srcWidth,
            int srcHeight)
        {
            var maxW = Math.Max(1, buttonWidth - 4);
            var maxH = Math.Max(1, buttonHeight - 4);
            var scale = Math.Min(maxW / (float)srcWidth, maxH / (float)srcHeight);
            var destW = Math.Max(1, (int)Math.Round(srcWidth * scale));
            var destH = Math.Max(1, (int)Math.Round(srcHeight * scale));
            var destX0 = offsetX + (buttonWidth - destW) / 2;
            var destY0 = offsetY + (buttonHeight - destH) / 2;

            for (int y = 0; y < destH; y++)
            {
                var srcY = Math.Min(srcHeight - 1, y * srcHeight / destH);
                for (int x = 0; x < destW; x++)
                {
                    var srcX = Math.Min(srcWidth - 1, x * srcWidth / destW);
                    var sc = src[srcY * srcWidth + srcX];
                    if (sc.A < 16)
                        continue;

                    // Ignore leftover backdrop / fringe from the source art so wood chrome shows through.
                    if (IsBackdropOrFringe(sc))
                        continue;

                    var dx = destX0 + x;
                    var dy = destY0 + y;
                    if (dx < offsetX || dy < offsetY || dx >= offsetX + buttonWidth || dy >= offsetY + buttonHeight)
                        continue;

                    var di = dy * stride + dx;
                    if (sc.A >= 250)
                    {
                        dest[di] = new Color(sc.R, sc.G, sc.B, (byte)255);
                    }
                    else
                    {
                        var dc = dest[di];
                        var a = sc.A / 255f;
                        var inv = 1f - a;
                        dest[di] = new Color(
                            (int)(sc.R * a + dc.R * inv),
                            (int)(sc.G * a + dc.G * inv),
                            (int)(sc.B * a + dc.B * inv),
                            255);
                    }
                }
            }
        }
    }
}
