using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.SkillTree
{
    /// <summary>
    /// Builds the Skill Tree HUD button: brown chrome with a cloud-canopy tree
    /// and a 1-2-3 node graph overlaid on the foliage (matches the reference art).
    /// </summary>
    public static class SkillTreeHudIcon
    {
        private static readonly Color BackgroundBrown = new Color(0x7a, 0x5a, 0x3a, 255);
        private static readonly Color CanopyGreen = new Color(0x3d, 0x7a, 0x3a, 255);
        private static readonly Color CanopyGreenDark = new Color(0x2e, 0x5e, 0x2c, 255);
        private static readonly Color CanopyGreenHi = new Color(0x5a, 0x96, 0x52, 255);
        private static readonly Color TrunkTan = new Color(0xb8, 0x8a, 0x4a, 255);
        private static readonly Color Outline = new Color(0x12, 0x10, 0x0c, 255);

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

            DrawSkillTreeIcon(data, texture.Width, 0, 0, widthDelta, heightDelta);
            DrawSkillTreeIcon(data, texture.Width, widthDelta, 0, widthDelta, heightDelta);

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

        private static void DrawSkillTreeIcon(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            ClearIconInterior(data, stride, offsetX, offsetY, width, height);

            var cx = offsetX + width / 2;
            var canopyBottom = offsetY + (int)(height * 0.72f);
            var canopyTop = offsetY + Math.Max(2, height / 10);
            var canopyRadiusX = Math.Max(4, width / 2 - 3);
            var canopyRadiusY = Math.Max(4, (canopyBottom - canopyTop) / 2);
            var canopyCy = canopyTop + canopyRadiusY;

            // Scalloped cloud canopy (overlapping circles), then edge outline.
            FillScallopedCanopy(data, stride, cx, canopyCy, canopyRadiusX, canopyRadiusY);
            OutlineFilledEdge(data, stride, offsetX, offsetY, width, height);

            // Trunk
            var trunkW = Math.Max(2, width / 7);
            var trunkTop = canopyBottom - Math.Max(1, height / 16);
            var trunkBottom = offsetY + height - Math.Max(2, height / 8);
            FillRect(data, stride, cx - trunkW / 2, trunkTop, trunkW, trunkBottom - trunkTop, TrunkTan);
            DrawRectOutline(data, stride, cx - trunkW / 2, trunkTop, trunkW, trunkBottom - trunkTop, Outline);

            // 1-2-3 skill node graph centered on the canopy.
            DrawNodeGraph(data, stride, cx, canopyCy, canopyRadiusX, canopyRadiusY);
        }

        private static void FillScallopedCanopy(Color[] data, int stride, int cx, int cy, int rx, int ry)
        {
            // Main body
            FillEllipse(data, stride, cx, cy, rx, ry, CanopyGreen);

            // Scallop lobes around the edge for the cloud-tree silhouette.
            var lobeR = Math.Max(2, Math.Min(rx, ry) / 3);
            FillCircle(data, stride, cx - rx + lobeR, cy - ry / 3, lobeR, CanopyGreen);
            FillCircle(data, stride, cx + rx - lobeR, cy - ry / 3, lobeR, CanopyGreen);
            FillCircle(data, stride, cx - rx / 2, cy - ry + lobeR / 2, lobeR, CanopyGreenHi);
            FillCircle(data, stride, cx + rx / 2, cy - ry + lobeR / 2, lobeR, CanopyGreen);
            FillCircle(data, stride, cx, cy - ry + lobeR / 3, lobeR + 1, CanopyGreenHi);
            FillCircle(data, stride, cx - rx + lobeR / 2, cy + ry / 4, lobeR, CanopyGreenDark);
            FillCircle(data, stride, cx + rx - lobeR / 2, cy + ry / 4, lobeR, CanopyGreenDark);

            // Soft highlight on top-left
            FillCircle(data, stride, cx - rx / 3, cy - ry / 2, Math.Max(1, lobeR / 2), CanopyGreenHi);
        }

        private static bool IsCanopyColor(Color c) =>
            c.A == 255 && c.G > c.R && c.G > c.B && c.G > 40;

        private static void OutlineFilledEdge(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            // Trace canopy silhouette: any green pixel next to non-green becomes black outline.
            var marks = new bool[data.Length];
            for (int y = offsetY + 1; y < offsetY + height - 1; y++)
            {
                for (int x = offsetX + 1; x < offsetX + width - 1; x++)
                {
                    var idx = y * stride + x;
                    if (idx < 0 || idx >= data.Length || !IsCanopyColor(data[idx]))
                        continue;

                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            var n = (y + oy) * stride + (x + ox);
                            if (n < 0 || n >= data.Length || !IsCanopyColor(data[n]))
                            {
                                marks[idx] = true;
                                break;
                            }
                        }
                        if (marks[idx]) break;
                    }
                }
            }

            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i])
                    data[i] = Outline;
            }
        }

        private static void DrawNodeGraph(Color[] data, int stride, int cx, int cy, int rx, int ry)
        {
            // Node radius scales with canopy; keep rings readable on tiny HUD buttons.
            var nodeR = Math.Max(1, Math.Min(rx, ry) / 6);
            if (nodeR < 1) nodeR = 1;

            var topY = cy - ry / 2;
            var midY = cy - ry / 10;
            var botY = cy + ry / 3;

            var midSpread = Math.Max(3, rx / 2);
            var botSpread = Math.Max(4, (int)(rx * 0.72f));

            // Positions: 1 top, 2 mid, 3 bottom
            var n0 = new Point(cx, topY);
            var n1 = new Point(cx - midSpread, midY);
            var n2 = new Point(cx + midSpread, midY);
            var n3 = new Point(cx - botSpread, botY);
            var n4 = new Point(cx, botY);
            var n5 = new Point(cx + botSpread, botY);

            // Edges (pyramid): top→mids, mids→bottom neighbors + both→center
            DrawLine(data, stride, n0.X, n0.Y, n1.X, n1.Y, Outline);
            DrawLine(data, stride, n0.X, n0.Y, n2.X, n2.Y, Outline);
            DrawLine(data, stride, n1.X, n1.Y, n3.X, n3.Y, Outline);
            DrawLine(data, stride, n1.X, n1.Y, n4.X, n4.Y, Outline);
            DrawLine(data, stride, n2.X, n2.Y, n4.X, n4.Y, Outline);
            DrawLine(data, stride, n2.X, n2.Y, n5.X, n5.Y, Outline);

            DrawHollowNode(data, stride, n0.X, n0.Y, nodeR);
            DrawHollowNode(data, stride, n1.X, n1.Y, nodeR);
            DrawHollowNode(data, stride, n2.X, n2.Y, nodeR);
            DrawHollowNode(data, stride, n3.X, n3.Y, nodeR);
            DrawHollowNode(data, stride, n4.X, n4.Y, nodeR);
            DrawHollowNode(data, stride, n5.X, n5.Y, nodeR);
        }

        private static void DrawHollowNode(Color[] data, int stride, int x, int y, int r)
        {
            // Punch a green hole then stroke black so nodes stay hollow over foliage.
            FillCircle(data, stride, x, y, r, CanopyGreen);
            DrawCircleOutline(data, stride, x, y, r, Outline);
            if (r >= 2)
                DrawCircleOutline(data, stride, x, y, r - 1, Outline);
        }

        private static void ClearIconInterior(Color[] data, int stride, int offsetX, int offsetY, int width, int height)
        {
            for (int y = offsetY + 2; y < offsetY + height - 2; y++)
            {
                for (int x = offsetX + 2; x < offsetX + width - 2; x++)
                    SetPixel(data, stride, x, y, BackgroundBrown);
            }
        }

        private static void FillRect(Color[] data, int stride, int x, int y, int w, int h, Color color)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                for (int xx = x; xx < x + w; xx++)
                    SetPixel(data, stride, xx, yy, color);
            }
        }

        private static void DrawRectOutline(Color[] data, int stride, int x, int y, int w, int h, Color color)
        {
            for (int xx = x; xx < x + w; xx++)
            {
                SetPixel(data, stride, xx, y, color);
                SetPixel(data, stride, xx, y + h - 1, color);
            }
            for (int yy = y; yy < y + h; yy++)
            {
                SetPixel(data, stride, x, yy, color);
                SetPixel(data, stride, x + w - 1, yy, color);
            }
        }

        private static void FillCircle(Color[] data, int stride, int cx, int cy, int r, Color color)
        {
            var r2 = r * r;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= r2)
                        SetPixel(data, stride, cx + x, cy + y, color);
                }
            }
        }

        private static void DrawCircleOutline(Color[] data, int stride, int cx, int cy, int r, Color color)
        {
            var r2 = r * r;
            var inner = Math.Max(0, r - 1);
            var inner2 = inner * inner;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    var d = x * x + y * y;
                    if (d <= r2 && d >= inner2)
                        SetPixel(data, stride, cx + x, cy + y, color);
                }
            }
        }

        private static void FillEllipse(Color[] data, int stride, int cx, int cy, int rx, int ry, Color color)
        {
            if (rx <= 0 || ry <= 0)
                return;

            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    if ((x * x) / (float)(rx * rx) + (y * y) / (float)(ry * ry) <= 1f)
                        SetPixel(data, stride, cx + x, cy + y, color);
                }
            }
        }

        private static void DrawLine(Color[] data, int stride, int x0, int y0, int x1, int y1, Color color)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                SetPixel(data, stride, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                    break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
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
