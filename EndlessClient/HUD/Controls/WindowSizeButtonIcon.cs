using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.Controls
{
    /// <summary>
    /// Top-right window shrink/expand button: wood chrome + left chevron only
    /// (no maximize/restore "little windows").
    /// </summary>
    public static class WindowSizeButtonIcon
    {
        private const int CellW = 18;
        private const int CellH = 16;

        private static readonly Color Wood = new Color(0x6e, 0x4e, 0x32, 255);
        private static readonly Color WoodLight = new Color(0x8a, 0x66, 0x44, 255);
        private static readonly Color WoodDark = new Color(0x52, 0x3a, 0x24, 255);
        private static readonly Color Rivet = new Color(0xc8, 0xb0, 0x78, 255);
        private static readonly Color Chevron = new Color(0xe8, 0xd0, 0x70, 255);

        public static Texture2D Create(GraphicsDevice device, bool maximized)
        {
            // Glyph is always the left chevron — maximized flag kept for call-site compatibility.
            _ = maximized;
            var texture = new Texture2D(device, CellW * 2, CellH);
            var data = new Color[texture.Width * texture.Height];

            PaintCell(data, texture.Width, 0, outState: true);
            PaintCell(data, texture.Width, CellW, outState: false);

            texture.SetData(data);
            return texture;
        }

        public static void SetMaximized(Texture2D texture, bool maximized)
        {
            _ = maximized;
            if (texture.Width != CellW * 2 || texture.Height != CellH)
                return;

            var data = new Color[texture.Width * texture.Height];
            PaintCell(data, texture.Width, 0, outState: true);
            PaintCell(data, texture.Width, CellW, outState: false);
            texture.SetData(data);
        }

        public static Rectangle OutSource => new Rectangle(0, 0, CellW, CellH);
        public static Rectangle OverSource => new Rectangle(CellW, 0, CellW, CellH);

        private static void PaintCell(Color[] data, int stride, int offsetX, bool outState)
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

            // Left-pointing chevron only — no overlapping window squares.
            var g = outState ? Chevron : Color.White;
            DrawChevron(data, stride, offsetX, g);
        }

        private static void DrawChevron(Color[] data, int stride, int offsetX, Color c)
        {
            // Centered `<` inside the cell.
            const int x0 = 6;
            const int y0 = 3;

            for (int i = 0; i < 5; i++)
            {
                Set(data, stride, offsetX + x0 + 4 - i, y0 + i, c);
                Set(data, stride, offsetX + x0 + 5 - i, y0 + i, c);
                Set(data, stride, offsetX + x0 + 4 - i, y0 + 8 - i, c);
                Set(data, stride, offsetX + x0 + 5 - i, y0 + 8 - i, c);
            }
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
