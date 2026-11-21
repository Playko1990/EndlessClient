using System;
using EndlessClient.ControlSets;
using EndlessClient.HUD;
using EndlessClient.HUD.Chat;
using EndlessClient.HUD.Controls;
using EndlessClient.Rendering;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Optional;
using XNAControls;

namespace EndlessClient.UIControls
{
    public class ChatModePictureBox : XNAPictureBox
    {
        private readonly IChatModeCalculator _chatModeCalculator;
        private readonly IHudControlProvider _hudControlProvider;
        private readonly bool _resizable;

        public enum ChatMode
        {
            NoText,
            Public,
            Private,
            Global,
            Group,
            Admin,
            Muted,
            Guild
        }

        private string _lastChat;
        private Option<DateTime> _endMuteTime;

        /// <summary>Current mode for the in-bar label (resizable HUD draws this as text).</summary>
        public ChatMode CurrentMode { get; private set; } = ChatMode.NoText;

        public ChatModePictureBox(INativeGraphicsManager nativeGraphicsManager,
                                  IClientWindowSizeProvider clientWindowSizeProvider,
                                  IChatModeCalculator chatModeCalculator,
                                  IHudControlProvider hudControlProvider)
        {
            _chatModeCalculator = chatModeCalculator;
            _hudControlProvider = hudControlProvider;
            _resizable = clientWindowSizeProvider.Resizable;

            _lastChat = "";
            _endMuteTime = Option.None<DateTime>();

            Texture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 31);
            StretchMode = StretchMode.CenterInFrame;

            var iconHeight = FrameHeight;
            SourceRectangle = new Rectangle(0, 0, Texture.Width, iconHeight);

            if (_resizable)
            {
                // Mode is painted inside the chat bar as text — hide the old separate sprite box.
                KeepInClientWindowBounds = false;
                Visible = false;
                DrawArea = Rectangle.Empty;
            }
            else
            {
                DrawArea = new Rectangle(16, 309, Texture.Width, iconHeight);
            }
        }

        private int FrameHeight => Math.Max(1, Texture.Height / 8 - 2);

        public static string LabelFor(ChatMode mode) => mode switch
        {
            ChatMode.Private => "PRIVATE",
            ChatMode.Global => "GLOBAL",
            ChatMode.Group => "GROUP",
            ChatMode.Admin => "ADMIN",
            ChatMode.Muted => "MUTED",
            ChatMode.Guild => "GUILD",
            // Idle (NoText) and Public both read as PUBLIC inside the chat bar.
            _ => "PUBLIC"
        };

        public void SetMuted(DateTime endMuteTime)
        {
            _lastChat = "";
            _endMuteTime = Option.Some(endMuteTime);
            UpdateSourceRectangleForMode(ChatMode.Muted);
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            _endMuteTime.Match(
                some: endTime =>
                {
                    if (DateTime.Now >= endTime)
                    {
                        _endMuteTime = Option.None<DateTime>();
                        UpdateSourceRectangleForMode(ChatMode.NoText);
                    }
                },
                none: () =>
                {
                    if (AtLeastOneCharTypedOrDeleted())
                    {
                        UpdateSourceRectangleForMode(_chatModeCalculator.CalculateMode(ChatTextBox.Text));
                        _lastChat = ChatTextBox.Text;
                    }
                });

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            // Resizable HUD: ChatTextBox draws the mode label inside the bar.
            if (_resizable)
                return;

            base.OnDrawControl(gameTime);
        }

        private bool AtLeastOneCharTypedOrDeleted()
        {
            return _hudControlProvider.IsInGame &&
                   ((_lastChat.Length == 0 && ChatTextBox.Text.Length > 0) ||
                    (_lastChat.Length > 0 && ChatTextBox.Text.Length == 0));
        }

        private void UpdateSourceRectangleForMode(ChatMode mode)
        {
            CurrentMode = mode;

            if (!SourceRectangle.HasValue)
                throw new InvalidOperationException("SourceRectangle is expected to have a value.");

            SourceRectangle = new Rectangle(0, (int)((int)mode * (Texture.Height / 8f)), Texture.Width, FrameHeight);
        }

        private ChatTextBox ChatTextBox => _hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox);
    }
}
