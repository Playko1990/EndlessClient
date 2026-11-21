using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.Controls
{
    /// <summary>
    /// Clean top-right window shrink/expand button art: wood chrome + chevron only
    /// (no friend-list person glyph).
    /// </summary>
    public static class WindowSizeButtonIcon
    {
        private const int CellW = 20;
        private const int CellH = 18;

        private static readonly Color Wood = new Color(0x6e, 0x4e, 0x32, 255);
        private static readonly Color WoodLight = new Color(0x8a, 0x66, 0x44, 255);
        private static readonly Color WoodDark = new Color(0x52, 0x3a, 0x24, 255);
        private static readonly Color Rivet = new Color(0xc8, 0xb0, 0x78, 255);
        private static readonly Color Chevron = new Color(0xe8, 0xd0, 0x70, 255);
        private static readonly Color ChevronDark = new Color(0xb0, 0x90, 0x40, 255);

        public static Texture2D Create(GraphicsDevice device)
        {
            // Side-by-side out / over states.
            var texture = new Texture2D(device, CellW * 2, CellH);
            var data = new Color[texture.Width * texture.Height];

            PaintCell(data, texture.Width, 0, outState: true);
            PaintCell(data, texture.Width, CellW, outState: false);

            texture.SetData(data);
            return texture;
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

            // Corner rivets.
            void RivetAt(int x, int y)
            {
                data[y * stride + offsetX + x] = Rivet;
            }

            RivetAt(2, 2);
            RivetAt(CellW - 3, 2);
            RivetAt(2, CellH - 3);
            RivetAt(CellW - 3, CellH - 3);

            // Left-pointing chevron (no person/door glyph).
            var chev = outState ? Chevron : Color.White;
            var chevEdge = outState ? ChevronDark : Chevron;
            DrawChevron(data, stride, offsetX + 6, 4, chev, chevEdge);
        }

        private static void DrawChevron(Color[] data, int stride, int x0, int y0, Color fill, Color edge)
        {
            // Simple `<` made of two diagonal arms.
            for (int i = 0; i < 5; i++)
            {
                Set(data, stride, x0 + 4 - i, y0 + i, fill);
                Set(data, stride, x0 + 5 - i, y0 + i, edge);
                Set(data, stride, x0 + 4 - i, y0 + 8 - i, fill);
                Set(data, stride, x0 + 5 - i, y0 + 8 - i, edge);
            }

            // Thicken the chevron one pixel.
            for (int i = 0; i < 5; i++)
            {
                Set(data, stride, x0 + 3 - i, y0 + i, fill);
                Set(data, stride, x0 + 3 - i, y0 + 8 - i, fill);
            }
        }

        private static void Set(Color[] data, int stride, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= stride)
                return;
            var i = y * stride + x;
            if (i < 0 || i >= data.Length)
                return;
            data[i] = c;
        }
    }
}
