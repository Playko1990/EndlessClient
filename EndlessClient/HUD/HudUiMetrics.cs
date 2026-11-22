using System;
using EndlessClient.Rendering;
using EOLib.Shared;
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

        // HP/TP/SP/EXP numeric text under the bars.
        public const float StatusBarTextScale = 2.0f;

        // Floating paperdoll / character info dialog.
        public const float PaperdollScale = 1.5f;

        // Session-EXP (gem) + Quest (Q) tabs beside the status bars.
        public const float StatusSideTabScale = 2.0f;

        // Native HUD side-button stride in classic layout.
        public const int NativeSideButtonStep = 20;

        // Left hub column has 6 buttons that should fill the docked panel height.
        public const int HubButtonCount = 6;

        public const int NativeInventorySlotSize = 26;

        public static bool ShouldEnlargeHud(bool resizable, int windowWidth = 0) =>
            resizable || windowWidth > ClientWindowSizeRepository.DEFAULT_BACKBUFFER_WIDTH;

        public static int InventorySlotSize(bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(NativeInventorySlotSize * PanelScale) : NativeInventorySlotSize;

        public static int SideButtonStep(bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(NativeSideButtonStep * PanelScale) : NativeSideButtonStep;

        /// <summary>
        /// Hub icon size that fills the docked panel height (6 left-side buttons).
        /// </summary>
        public static int HubButtonSize(bool resizable, int scaledPanelHeight)
        {
            if (!ShouldEnlargeHud(resizable))
                return NativeSideButtonStep;

            return Math.Max(1, scaledPanelHeight / HubButtonCount);
        }

        /// <summary>
        /// Slightly larger bitmap font so labels fill enlarged panel cells.
        /// </summary>
        public static string PanelLabelFont(bool resizable) =>
            ShouldEnlargeHud(resizable) ? Constants.FontSize10 : Constants.FontSize08pt5;

        /// <summary>
        /// Numeric values (stats / lvl / skp) are drawn at 2x the panel label size.
        /// </summary>
        public static float PanelValueTextScale(bool resizable) =>
            ShouldEnlargeHud(resizable) ? 2f : 1f;

        public static string ChatPanelFont(bool resizable) =>
            ShouldEnlargeHud(resizable) ? Constants.FontSize10 : Constants.FontSize08;

        public const int NativeChatLineHeight = 13;
        public const int NativeChatLinesToRender = 7;
        public const int NativeScrollArrowSize = 15;
        public const int NativeScrollArrowWidth = 16;

        // Resizable bottom chrome: thicker chat entry, centered inset.
        // Public/Private is drawn as text inside the chat bar (not a separate box).
        public const int ResizableChatBarHeight = 28;
        public const int ResizableStatusBarHeight = 16;
        public const int ResizableBottomChromeGap = 2;
        public const int ResizableChatBarSideInset = 96;
        public const int ResizableChatBarMaxWidth = 920;
        public const int ResizableChatModeLabelPad = 8;

        public static Rectangle ResizableChatBarArea(int windowWidth, int windowHeight)
        {
            var width = Math.Max(420, windowWidth - ResizableChatBarSideInset * 2);
            width = Math.Min(width, ResizableChatBarMaxWidth);
            width = Math.Min(width, Math.Max(420, (int)(windowWidth * 0.72)));
            var x = (windowWidth - width) / 2;
            var y = windowHeight - ResizableStatusBarHeight - ResizableBottomChromeGap - ResizableChatBarHeight;
            return new Rectangle(x, y, width, ResizableChatBarHeight);
        }

        public static Rectangle ResizableStatusBarArea(int windowWidth, int windowHeight)
        {
            var chat = ResizableChatBarArea(windowWidth, windowHeight);
            return new Rectangle(
                chat.X,
                windowHeight - ResizableStatusBarHeight,
                chat.Width,
                ResizableStatusBarHeight);
        }

        public static int ResizableChatBarTop(int windowHeight) =>
            windowHeight - ResizableStatusBarHeight - ResizableBottomChromeGap - ResizableChatBarHeight;

        public static int ChatLineHeight(bool resizable) =>
            Scale(NativeChatLineHeight, resizable);

        public static int ChatLinesToRender(bool resizable) =>
            NativeChatLinesToRender; // line height scales with the panel, so line count stays the same

        public static float UiScale(bool resizable) =>
            ShouldEnlargeHud(resizable) ? PanelScale : 1f;

        /// <summary>
        /// Prefer the panel's actual stretched size over the resizable flag so child
        /// controls stay aligned when DrawArea and config get out of sync.
        /// </summary>
        public static float ScaleFromPanel(Texture2D background, Rectangle drawArea, bool resizable)
        {
            if (background != null && background.Height > 0 && drawArea.Height > 0)
            {
                var fromSize = (float)drawArea.Height / background.Height;
                if (fromSize > 1.01f)
                    return fromSize;
            }

            return UiScale(resizable);
        }

        public static Vector2 ScalePoint(float x, float y, float scale) =>
            new Vector2(x * scale, y * scale);

        public static Rectangle ScaleRectangle(Rectangle rect, float scale)
        {
            if (Math.Abs(scale - 1f) < 0.01f)
                return rect;

            return new Rectangle(
                (int)Math.Round(rect.X * scale),
                (int)Math.Round(rect.Y * scale),
                Math.Max(1, (int)Math.Round(rect.Width * scale)),
                Math.Max(1, (int)Math.Round(rect.Height * scale)));
        }

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
            ScalePoint(x, y, UiScale(resizable));

        public static Rectangle ScaleRectangle(Rectangle rect, bool resizable) =>
            ScaleRectangle(rect, UiScale(resizable));

        public static int Scale(int value, bool resizable) =>
            ShouldEnlargeHud(resizable) ? (int)(value * PanelScale) : value;

        public static int Scale(int value, float scale) =>
            Math.Max(1, (int)Math.Round(value * scale));

        /// <summary>
        /// Scale only width/height of a control whose position is already correct.
        /// </summary>
        public static void ScaleControlSize(XNAControls.XNAButton button, bool resizable)
        {
            if (!ShouldEnlargeHud(resizable) || button == null)
                return;

            button.DrawArea = new Rectangle(
                button.DrawArea.X,
                button.DrawArea.Y,
                Scale(button.DrawArea.Width, resizable: true),
                Scale(button.DrawArea.Height, resizable: true));
            button.ClickArea = new Rectangle(0, 0, button.DrawArea.Width, button.DrawArea.Height);
        }
    }
}
