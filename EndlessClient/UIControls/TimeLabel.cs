using System;
using EndlessClient.HUD;
using EndlessClient.Rendering;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.UIControls
{
    public class TimeLabel : XNALabel
    {
        private DateTime _lastUpdateTime;

        public TimeLabel(IClientWindowSizeProvider windowSizeProvider)
            : base(Constants.FontSize07)
        {
            _lastUpdateTime = DateTime.Now;
            if (windowSizeProvider.Resizable)
                KeepInClientWindowBounds = false;
            DrawArea = GetPositionBasedOnWindowSize(windowSizeProvider);
            windowSizeProvider.GameWindowSizeChanged += (_, _) => DrawArea = GetPositionBasedOnWindowSize(windowSizeProvider);
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            if (DateTime.Now.Second != _lastUpdateTime.Second)
            {
                Text = $"{DateTime.Now.Hour,2:D2}:{DateTime.Now.Minute,2:D2}:{DateTime.Now.Second,2:D2}";

                _lastUpdateTime = DateTime.Now;
            }

            base.OnUpdateControl(gameTime);
        }

        private static Rectangle GetPositionBasedOnWindowSize(IClientWindowSizeProvider windowSizeProvider)
        {
            //original location: 558, 456
            if (windowSizeProvider.Resizable)
            {
                var status = HudUiMetrics.ResizableStatusBarArea(
                    windowSizeProvider.Width,
                    windowSizeProvider.Height);
                return new Rectangle(status.Right - 82, status.Y + 1, 1, 1);
            }

            return new Rectangle(windowSizeProvider.Width - 82, windowSizeProvider.Height - 26, 1, 1);
        }
    }
}
