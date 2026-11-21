using System;
using EndlessClient.HUD;
using EndlessClient.HUD.Panels;
using EOLib.Domain.Chat;
using EOLib.Extensions;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Optional.Unsafe;
using XNAControls;

namespace EndlessClient.Rendering.Chat
{
    public class ChatRenderable : IChatRenderable
    {
        private const int ICON_GRAPHIC_X_OFF = 3;
        private const int CHAT_MESSAGE_X_OFF = 20;

        private readonly INativeGraphicsManager _nativeGraphicsManager;

        private readonly string _partialMessage;

        // Vanilla news/chat content starts 20px below the panel top (abs Y 350 with panel at 330).
        protected virtual int HeaderYOffset => 3;

        public int DisplayIndex { get; set; }

        public ChatData Data { get; private set; }

        public ChatRenderable(INativeGraphicsManager nativeGraphicsManager,
                              int displayIndex,
                              ChatData data,
                              string partialMessage = null)
        {
            _nativeGraphicsManager = nativeGraphicsManager;

            DisplayIndex = displayIndex;
            Data = data;
            _partialMessage = partialMessage;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ChatRenderable)) return false;
            var other = (ChatRenderable)obj;

            return other.Data.Equals(Data)
                && other._partialMessage.Equals(_partialMessage);
        }

        public override int GetHashCode()
        {
            var hash = 397 ^ Data.GetHashCode();
            hash = (hash * 397) ^ DisplayIndex.GetHashCode();
            hash = (hash * 397) ^ _partialMessage.GetHashCode();
            return hash;
        }

        public void Render(IHudPanel parentPanel, SpriteBatch spriteBatch, BitmapFont chatFont, float uiScale = 1f)
        {
            if (uiScale <= 0f)
                uiScale = 1f;

            var lineHeight = HudUiMetrics.NativeChatLineHeight * uiScale;
            var iconOff = ICON_GRAPHIC_X_OFF * uiScale;
            var msgOff = CHAT_MESSAGE_X_OFF * uiScale;
            var headerOff = HeaderYOffset * uiScale;

            // Match panel background drawing (DrawAreaWithParentOffset), not local DrawPosition alone.
            var panelOrigin = parentPanel is XNAControl control
                ? control.DrawPositionWithParentOffset
                : parentPanel.DrawPosition;
            var pos = panelOrigin + new Vector2(0, DisplayIndex * lineHeight);

            spriteBatch.Begin();

            var iconSrc = GetChatIconRectangle(Data.Icon);
            if (iconSrc.HasValue)
            {
                var iconDest = new Rectangle(
                    (int)Math.Round(pos.X + iconOff),
                    (int)Math.Round(pos.Y + headerOff),
                    Math.Max(1, (int)Math.Round(iconSrc.Value.Width * uiScale)),
                    Math.Max(1, (int)Math.Round(iconSrc.Value.Height * uiScale)));
                spriteBatch.Draw(_nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 32, true),
                                 iconDest,
                                 iconSrc,
                                 Color.White);
            }

            string strToDraw;
            if (string.IsNullOrEmpty(Data.Who))
                strToDraw = _partialMessage;
            else
                strToDraw = Data.Who + "  " + _partialMessage;

            spriteBatch.End();

            var textPos = new Vector2(pos.X + msgOff, pos.Y + headerOff);
            // Scale glyphs around the origin, then place at textPos (same pattern as HudScaledLabel).
            var transform = Matrix.CreateScale(uiScale) *
                            Matrix.CreateTranslation(textPos.X, textPos.Y, 0f);
            spriteBatch.Begin(transformMatrix: transform);
            spriteBatch.DrawString(chatFont, strToDraw, Vector2.Zero, Data.ChatColor.ToColor());
            spriteBatch.End();
        }

        private static Rectangle? GetChatIconRectangle(ChatIcon icon)
        {
            if (icon == ChatIcon.None)
                return null;

            var (x, y, width, height) = icon.GetChatIconRectangleBounds().ValueOrDefault();
            if (width <= 0 || height <= 0)
                return null;

            return new Rectangle(x, y, width, height);
        }
    }

    public class NewsChatRenderable : ChatRenderable
    {
        // Vanilla DrawNewsText uses abs Y = 14*row + 350 → 20px below panel top (330).
        protected override int HeaderYOffset => 20;

        public NewsChatRenderable(INativeGraphicsManager nativeGraphicsManager, int displayIndex, ChatData data, string partialMessage)
            : base(nativeGraphicsManager, displayIndex, data, partialMessage)
        {
        }
    }
}
