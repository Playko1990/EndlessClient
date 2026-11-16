using System;
using EndlessClient.Content;
using EndlessClient.Rendering;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using Optional;

namespace EndlessClient.UIControls
{
    /// <summary>
    /// Special instance of an XNATextBox that should ignore input from the number pad (which is used for Emotes)
    /// </summary>
    public class ChatTextBox : ClearableTextBox
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;

        private bool _ignoreAllInput;
        private Option<DateTime> _endMuteTime;

        private readonly Rectangle? _leftSide, _background, _rightSide;

        // Friend/ignore icons sit on the right end of the chat row in resizable mode.
        private const int ResizableRightIconReserve = 52;
        private const int ResizableLeftCapPad = 12;

        public ChatTextBox(INativeGraphicsManager nativeGraphicsManager,
                           IClientWindowSizeProvider clientWindowSizeProvider,
                           IContentProvider contentManagerProvider)
            : base(Rectangle.Empty, // (124, 308, 440, 19)
                Constants.FontSize08,
                caretTexture: contentManagerProvider.Textures[ContentProvider.Cursor])
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _clientWindowSizeProvider = clientWindowSizeProvider;

            if (_clientWindowSizeProvider.Resizable)
            {
                _leftSide = new Rectangle(10, 308, 12, 20);
                _background = new Rectangle(22, 308, 560, 20);
                _rightSide = new Rectangle(578, 308, 11, 20);

                // Bottom chrome must stay pinned; clamping to window bounds caused a stray
                // short bar segment that covered the chat-mode sword.
                KeepInClientWindowBounds = false;

                ApplyResizableLayout();
                _clientWindowSizeProvider.GameWindowSizeChanged += (o, e) => ApplyResizableLayout();
            }
            else
            {
                DrawArea = new Rectangle(124, 308, 440, 20);
            }

            MaxChars = 140;
            _endMuteTime = Option.None<DateTime>();
        }

        public override void Initialize()
        {
            base.Initialize();

            // This must be done after Initialize because MaxWidth uses the sprite font.
            // SpriteFont is loaded in LoadContent which is called by Initialize.
            MaxWidth = _clientWindowSizeProvider.Resizable
                ? Math.Max(40, DrawArea.Width - ResizableLeftCapPad - ResizableRightIconReserve)
                : 440;
        }

        public void SetMuted(DateTime endMuteTime)
        {
            _ignoreAllInput = true;
            _endMuteTime = Option.Some(endMuteTime);
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            _endMuteTime.MatchSome(endTime =>
            {
                if (DateTime.Now > endTime)
                {
                    _endMuteTime = Option.None<DateTime>();
                    _ignoreAllInput = false;
                }
            });

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            if (_clientWindowSizeProvider.Resizable && _background != null)
            {
                // Always draw from DrawArea (one bar only) — never a second Y via stale DrawPosition.
                var area = DrawAreaWithParentOffset;
                var texture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 1, true);
                var capTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 1, false);

                _spriteBatch.Begin(samplerState: SamplerState.LinearWrap);
                _spriteBatch.Draw(texture, area, _background, Color.White);
                _spriteBatch.End();

                _spriteBatch.Begin();
                _spriteBatch.Draw(capTexture, area.Location.ToVector2(), _leftSide, Color.White);
                _spriteBatch.Draw(
                    capTexture,
                    new Vector2(area.Right - _rightSide.Value.Width, area.Y),
                    _rightSide,
                    Color.White);
                _spriteBatch.End();
            }

            base.OnDrawControl(gameTime);
        }

        protected override bool HandleTextInput(KeyboardEventArgs eventArgs)
        {
            if (_ignoreAllInput)
                return false;

            if (!IsSpecialInput(eventArgs.Key, eventArgs.Modifiers))
                base.HandleTextInput(eventArgs);

            return true;
        }

        private void ApplyResizableLayout()
        {
            // Match the status/bottom bar length: edge-to-edge. Sword sits above this row.
            DrawArea = new Rectangle(0, _clientWindowSizeProvider.Height - 40, _clientWindowSizeProvider.Width, 20);
            LeftPadding = ResizableLeftCapPad;
            MaxWidth = Math.Max(40, DrawArea.Width - ResizableLeftCapPad - ResizableRightIconReserve);
        }

        private bool IsSpecialInput(Keys k, KeyboardModifiers modifiers)
        {
            return k == Keys.Decimal || (k >= Keys.NumPad0 && k <= Keys.NumPad9) || modifiers == KeyboardModifiers.Alt;
        }
    }
}
