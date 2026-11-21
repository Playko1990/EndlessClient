using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.Controls
{
    /// <summary>
    /// Procedural wood-chrome HUD buttons: minimize / maximize / restore.
    /// Replaces friend-list person glyphs and chevron placeholders.
    /// </summary>
    public static class HudChromeButtonIcons
    {
        public enum Glyph
        {
            Minimize,
            Maximize,
            Restore
        }

        private const int CellW = 18;
        private const int CellH = 16;

        private static readonly Color Wood = new Color(0x6e, 0x4e, 0x32, 255);
        private static readonly Color WoodLight = new Color(0x8a, 0x66, 0x44, 255);
        private static readonly Color WoodDark = new Color(0x52, 0x3a, 0x24, 255);
        private static readonly Color Rivet = new Color(0xc8, 0xb0, 0x78, 255);
        private static readonly Color GlyphColor = new Color(0xe8, 0xd0, 0x70, 255);
        private static readonly Color GlyphOver = Color.White;

        public static Texture2D Create(GraphicsDevice device, Glyph glyph)
        {
            // Side-by-side out / over states.
            var texture = new Texture2D(device, CellW * 2, CellH);
            var data = new Color[texture.Width * texture.Height];

            PaintCell(data, texture.Width, 0, outState: true, glyph);
            PaintCell(data, texture.Width, CellW, outState: false, glyph);

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Rewrites an existing out|over sheet in place (same size as Create output).
        /// </summary>
        public static void Repaint(Texture2D texture, Glyph glyph)
        {
            if (texture.Width != CellW * 2 || texture.Height != CellH)
                return;

            var data = new Color[texture.Width * texture.Height];
            PaintCell(data, texture.Width, 0, outState: true, glyph);
            PaintCell(data, texture.Width, CellW, outState: false, glyph);
            texture.SetData(data);
        }

        public static Rectangle OutSource => new Rectangle(0, 0, CellW, CellH);
        public static Rectangle OverSource => new Rectangle(CellW, 0, CellW, CellH);

        private static void PaintCell(Color[] data, int stride, int offsetX, bool outState, Glyph glyph)
        {
            var fill = outState ? Wood : WoodLight;
            var edge = outState ? WoodDark : Wood;

            for (int y = 0; y < CellH; y++)
            {
                for (int x = 0; x < CellW; x++)
                {
                    var c = fill;
                    if (x == 0 || y == 0 || x == CellW - 1 || y == CellH - 1)
                        c = edge;
                    data[y * stride + offsetX + x] = c;
                }
            }

            Set(data, stride, offsetX + 2, 2, Rivet);
            Set(data, stride, offsetX + CellW - 3, 2, Rivet);
            Set(data, stride, offsetX + 2, CellH - 3, Rivet);
            Set(data, stride, offsetX + CellW - 3, CellH - 3, Rivet);

            var g = outState ? GlyphColor : GlyphOver;
            switch (glyph)
            {
                case Glyph.Minimize:
                    DrawMinimize(data, stride, offsetX, g);
                    break;
                case Glyph.Maximize:
                    DrawMaximize(data, stride, offsetX, g);
                    break;
                case Glyph.Restore:
                    DrawRestore(data, stride, offsetX, g);
                    break;
            }
        }

        private static void DrawMinimize(Color[] data, int stride, int offsetX, Color c)
        {
            // Classic minimize: short horizontal bar near the bottom.
            for (int x = 4; x <= CellW - 5; x++)
            {
                Set(data, stride, offsetX + x, CellH - 5, c);
                Set(data, stride, offsetX + x, CellH - 6, c);
            }
        }

        private static void DrawMaximize(Color[] data, int stride, int offsetX, Color c)
        {
            // Classic maximize: hollow square.
            const int x0 = 4;
            const int y0 = 3;
            const int w = 10;
            const int h = 9;

            for (int x = 0; x < w; x++)
            {
                Set(data, stride, offsetX + x0 + x, y0, c);
                Set(data, stride, offsetX + x0 + x, y0 + 1, c);
                Set(data, stride, offsetX + x0 + x, y0 + h - 1, c);
            }

            for (int y = 0; y < h; y++)
            {
                Set(data, stride, offsetX + x0, y0 + y, c);
                Set(data, stride, offsetX + x0 + w - 1, y0 + y, c);
            }
        }

        private static void DrawRestore(Color[] data, int stride, int offsetX, Color c)
        {
            // Overlapping squares (restore-down from maximized).
            void Square(int x0, int y0, int w, int h)
            {
                for (int x = 0; x < w; x++)
                {
                    Set(data, stride, offsetX + x0 + x, y0, c);
                    Set(data, stride, offsetX + x0 + x, y0 + h - 1, c);
                }

                for (int y = 0; y < h; y++)
                {
                    Set(data, stride, offsetX + x0, y0 + y, c);
                    Set(data, stride, offsetX + x0 + w - 1, y0 + y, c);
                }

                // Thick top edge.
                for (int x = 0; x < w; x++)
                    Set(data, stride, offsetX + x0 + x, y0 + 1, c);
            }

            Square(6, 2, 8, 7);
            Square(3, 5, 8, 7);
        }

        private static void Set(Color[] data, int stride, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= stride || y >= CellH)
                return;
            var i = y * stride + x;
            if (i < 0 || i >= data.Length)
                return;
            data[i] = c;
        }
    }
}
