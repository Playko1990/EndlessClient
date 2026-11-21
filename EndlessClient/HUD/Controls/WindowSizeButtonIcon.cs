using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.HUD.Controls
{
    /// <summary>
    /// Top-right window maximize/restore button (delegates to HudChromeButtonIcons).
    /// </summary>
    public static class WindowSizeButtonIcon
    {
        public static Texture2D Create(GraphicsDevice device, bool maximized)
        {
            return HudChromeButtonIcons.Create(
                device,
                maximized ? HudChromeButtonIcons.Glyph.Restore : HudChromeButtonIcons.Glyph.Maximize);
        }

        public static void SetMaximized(Texture2D texture, bool maximized)
        {
            HudChromeButtonIcons.Repaint(
                texture,
                maximized ? HudChromeButtonIcons.Glyph.Restore : HudChromeButtonIcons.Glyph.Maximize);
        }

        public static Rectangle OutSource => HudChromeButtonIcons.OutSource;
        public static Rectangle OverSource => HudChromeButtonIcons.OverSource;
    }
}