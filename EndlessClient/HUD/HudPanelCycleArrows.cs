using System;
using EndlessClient.Audio;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD
{
    /// <summary>
    /// Large up/down arrows that sit on the enlarged panel scroll chrome and cycle
    /// between the main docked HUD panels. Small ScrollBar arrows keep content scrolling.
    /// </summary>
    public static class HudPanelCycleArrows
    {
        private static readonly InGameStates[] CycleOrder =
        {
            InGameStates.Inventory,
            InGameStates.ActiveSpells,
            InGameStates.PassiveSpells,
            InGameStates.Chat,
            InGameStates.Stats,
        };

        public static (XNAButton Up, XNAButton Down) Create(
            INativeGraphicsManager graphicsManager,
            Vector2 scrollBarPosition,
            Vector2 scrollBarSize,
            bool resizable,
            InGameStates currentPanel,
            IHudStateActions hudStateActions,
            ISfxPlayer sfxPlayer)
        {
            var uiScale = HudUiMetrics.UiScale(resizable);
            var arrowW = Math.Max(1, (int)Math.Round(HudUiMetrics.NativeScrollArrowWidth * uiScale));
            var arrowH = Math.Max(1, (int)Math.Round(HudUiMetrics.NativeScrollArrowSize * uiScale));

            var sheet = graphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 29);
            const int vertOff = 105; // LightOnMed palette
            var upOut = new Rectangle(0, vertOff + 15 * 3, 16, 15);
            var upOver = new Rectangle(0, vertOff + 15 * 4, 16, 15);
            var downOut = new Rectangle(0, vertOff + 15, 16, 15);
            var downOver = new Rectangle(0, vertOff + 15 * 2, 16, 15);

            var up = new XNAButton(sheet, scrollBarPosition, upOut, upOver);
            SizeArrowButton(up, scrollBarPosition, arrowW, arrowH);

            var downPos = new Vector2(scrollBarPosition.X, scrollBarPosition.Y + scrollBarSize.Y - arrowH);
            var down = new XNAButton(sheet, downPos, downOut, downOver);
            SizeArrowButton(down, downPos, arrowW, arrowH);

            up.OnMouseDown += (_, _) =>
            {
                sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
                hudStateActions.SwitchToState(Previous(currentPanel));
            };
            down.OnMouseDown += (_, _) =>
            {
                sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
                hudStateActions.SwitchToState(Next(currentPanel));
            };

            return (up, down);
        }

        /// <summary>
        /// Inset the content ScrollBar so its small arrows sit between the large cycle arrows.
        /// </summary>
        public static (Vector2 Position, Vector2 Size) InsetScrollBarArea(Vector2 scrollPos, Vector2 scrollSize, bool resizable)
        {
            var edge = Math.Max(1, (int)Math.Round(HudUiMetrics.NativeScrollArrowSize * HudUiMetrics.UiScale(resizable)));
            return (
                scrollPos + new Vector2(0, edge),
                new Vector2(scrollSize.X, Math.Max(edge * 2, scrollSize.Y - edge * 2)));
        }

        private static void SizeArrowButton(XNAButton button, Vector2 position, int width, int height)
        {
            button.DrawArea = new Rectangle((int)position.X, (int)position.Y, width, height);
            button.ClickArea = new Rectangle(0, 0, width, height);
            button.DrawPosition = position;
        }

        private static InGameStates Next(InGameStates current)
        {
            var index = Array.IndexOf(CycleOrder, current);
            if (index < 0)
                return CycleOrder[0];
            return CycleOrder[(index + 1) % CycleOrder.Length];
        }

        private static InGameStates Previous(InGameStates current)
        {
            var index = Array.IndexOf(CycleOrder, current);
            if (index < 0)
                return CycleOrder[0];
            return CycleOrder[(index - 1 + CycleOrder.Length) % CycleOrder.Length];
        }
    }
}
