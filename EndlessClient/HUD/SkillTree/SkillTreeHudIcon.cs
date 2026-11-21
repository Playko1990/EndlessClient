using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.SkillTree
{
    /// <summary>
    /// Builds the Skill Tree HUD button by compositing crisp tree artwork
    /// onto the same wood chrome fill used by the neighboring HUD icons.
    /// </summary>
    public static class SkillTreeHudIcon
    {
        private const string EmbeddedIconName = "SkillTreeIcon.png";

        // Fallback only if chrome sampling fails (matches typical EO side-button wood).
        private static readonly Color FallbackWood = new Color(0x7a, 0x5a, 0x3a, 255);

        /// <param name="buttonSize">
        /// Final on-screen hub button size. When larger than the native sheet cell,
        /// the texture is built at that size with nearest-neighbor scaling so the
        /// tree stays sharp instead of getting a blurry bilinear upscale later.
        /// </param>
        public static Texture2D Create(
            GraphicsDevice graphicsDevice,
            Texture2D hudButtonSheet,
            int buttonIndex,
            int buttonSize = 0)
        {
            var widthDelta = hudButtonSheet.Width / 2;
            var heightDelta = hudButtonSheet.Height / 11;

            // Use Inventory button (index 0) chrome so the frame matches neighboring buttons.
            var chromeIndex = 0;
            var outRect = new Rectangle(0, heightDelta * chromeIndex, widthDelta, heightDelta);
            var overRect = new Rectangle(widthDelta, heightDelta * chromeIndex, widthDelta, heightDelta);

            var cellW = buttonSize > heightDelta ? buttonSize : widthDelta;
            var cellH = buttonSize > heightDelta ? buttonSize : heightDelta;

            var texture = new Texture2D(graphicsDevice, cellW * 2, cellH);
            var data = new Color[texture.Width * texture.Height];

            // Native chrome → destination cells (nearest neighbor when enlarging).
            NearestCopyRegion(hudButtonSheet, outRect, data, texture.Width, 0, 0, cellW, cellH);
            NearestCopyRegion(hudButtonSheet, overRect, data, texture.Width, cellW, 0, cellW, cellH);

            var woodOut = SampleWoodFill(data, texture.Width, 0, 0, cellW, cellH);
            var woodOver = SampleWoodFill(data, texture.Width, cellW, 0, cellW, cellH);

            var frame = Math.Max(2, cellH / 10);
            ClearIconInterior(data, texture.Width, 0, 0, cellW, cellH, woodOut, frame);
            ClearIconInterior(data, texture.Width, cellW, 0, cellW, cellH, woodOver, frame);

            using (var tree = LoadTreeTexture(graphicsDevice))
            {
                var treePixels = new Color[tree.Width * tree.Height];
                tree.GetData(treePixels);

                BlitCenteredNearest(data, texture.Width, 0, 0, cellW, cellH,
                    treePixels, tree.Width, tree.Height, frame);
                BlitCenteredNearest(data, texture.Width, cellW, 0, cellW, cellH,
                    treePixels, tree.Width, tree.Height, frame);
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

        private static void NearestCopyRegion(
            Texture2D source,
            Rectangle sourceRect,
            Color[] dest,
            int destStride,
            int destX,
            int destY,
            int destW,
            int destH)
        {
            var buffer = new Color[sourceRect.Width * sourceRect.Height];
            source.GetData(0, sourceRect, buffer, 0, buffer.Length);

            for (int y = 0; y < destH; y++)
            {
                var srcY = Math.Min(sourceRect.Height - 1, y * sourceRect.Height / destH);
                for (int x = 0; x < destW; x++)
                {
                    var srcX = Math.Min(sourceRect.Width - 1, x * sourceRect.Width / destW);
                    dest[(destY + y) * destStride + (destX + x)] = buffer[srcY * sourceRect.Width + srcX];
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

            var inset = Math.Max(2, width / 10);
            var patch = Math.Max(3, width / 8);
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

        private static void ClearIconInterior(
            Color[] data,
            int stride,
            int offsetX,
            int offsetY,
            int width,
            int height,
            Color fill,
            int frame)
        {
            for (int y = offsetY + frame; y < offsetY + height - frame; y++)
            {
                for (int x = offsetX + frame; x < offsetX + width - frame; x++)
                    data[y * stride + x] = fill;
            }
        }

        private static void BlitCenteredNearest(
            Color[] dest,
            int stride,
            int offsetX,
            int offsetY,
            int buttonWidth,
            int buttonHeight,
            Color[] src,
            int srcWidth,
            int srcHeight,
            int frame)
        {
            var maxW = Math.Max(1, buttonWidth - frame * 2);
            var maxH = Math.Max(1, buttonHeight - frame * 2);
            // Integer scale when possible so pixels stay blocky and sharp.
            var scale = Math.Max(1, Math.Min(maxW / srcWidth, maxH / srcHeight));
            if (scale * srcWidth > maxW || scale * srcHeight > maxH)
            {
                // Fall back to fitting scale (still nearest-neighbor sampled).
                var fit = Math.Min(maxW / (float)srcWidth, maxH / (float)srcHeight);
                var destW = Math.Max(1, (int)Math.Round(srcWidth * fit));
                var destH = Math.Max(1, (int)Math.Round(srcHeight * fit));
                var destX0 = offsetX + (buttonWidth - destW) / 2;
                var destY0 = offsetY + (buttonHeight - destH) / 2;

                for (int y = 0; y < destH; y++)
                {
                    var srcY = Math.Min(srcHeight - 1, y * srcHeight / destH);
                    for (int x = 0; x < destW; x++)
                    {
                        var srcX = Math.Min(srcWidth - 1, x * srcWidth / destW);
                        var sc = src[srcY * srcWidth + srcX];
                        if (sc.A < 250)
                            continue;

                        var dx = destX0 + x;
                        var dy = destY0 + y;
                        if (dx < offsetX || dy < offsetY || dx >= offsetX + buttonWidth || dy >= offsetY + buttonHeight)
                            continue;

                        dest[dy * stride + dx] = new Color(sc.R, sc.G, sc.B, (byte)255);
                    }
                }

                return;
            }

            var outW = srcWidth * scale;
            var outH = srcHeight * scale;
            var x0 = offsetX + (buttonWidth - outW) / 2;
            var y0 = offsetY + (buttonHeight - outH) / 2;

            for (int y = 0; y < outH; y++)
            {
                var srcY = y / scale;
                for (int x = 0; x < outW; x++)
                {
                    var srcX = x / scale;
                    var sc = src[srcY * srcWidth + srcX];
                    if (sc.A < 250)
                        continue;

                    var dx = x0 + x;
                    var dy = y0 + y;
                    if (dx < offsetX || dy < offsetY || dx >= offsetX + buttonWidth || dy >= offsetY + buttonHeight)
                        continue;

                    dest[dy * stride + dx] = new Color(sc.R, sc.G, sc.B, (byte)255);
                }
            }
        }
    }
}
