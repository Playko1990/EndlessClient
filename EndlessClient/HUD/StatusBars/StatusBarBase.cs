using System;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;
using Optional;
using XNAControls;

namespace EndlessClient.HUD.StatusBars
{
    public abstract class StatusBarBase : XNAControl
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private readonly ICharacterProvider _characterProvider;

        protected readonly XNALabel _label;
        protected readonly Texture2D _texture;

        protected CharacterStats Stats => _characterProvider.MainCharacter.Stats;
        protected Rectangle _sourceRectangleArea;

        protected abstract int StatusBarIndex { get; }

        protected float BarScale =>
            HudUiMetrics.ShouldEnlargeHud(_clientWindowSizeProvider.Resizable, _clientWindowSizeProvider.Width)
                ? HudUiMetrics.StatusBarScale
                : 1f;

        protected int ScaledBarWidth => (int)(_sourceRectangleArea.Width * BarScale);
        protected int ScaledBarHeight => (int)(_sourceRectangleArea.Height * BarScale);

        private Option<DateTime> _labelShowTime;

        public event Action StatusBarClicked;
        public event Action StatusBarClosed;

        protected StatusBarBase(INativeGraphicsManager nativeGraphicsManager,
                                IClientWindowSizeProvider clientWindowSizeProvider,
                                ICharacterProvider characterProvider)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _characterProvider = characterProvider;

            _texture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 58, true);

            _label = new XNALabel(Constants.FontSize08)
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                DrawPosition = new Vector2(6, 15),
                ForeColor = ColorConstants.LightGrayText,
                Visible = false
            };
            _label.SetParentControl(this);

            _sourceRectangleArea = new Rectangle(0, 0, 110, 14);

            if (_clientWindowSizeProvider.Resizable)
                _clientWindowSizeProvider.GameWindowSizeChanged += (o, e) => ChangeStatusBarPosition();
        }

        protected abstract void UpdateLabelText();
        protected abstract void DrawStatusBar();

        public override void Initialize()
        {
            _label.Initialize();
            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            _labelShowTime.MatchSome(x =>
            {
                UpdateLabelText();

                if ((DateTime.Now - x).TotalSeconds >= 4)
                {
                    _label.Visible = false;
                    _labelShowTime = Option.None<DateTime>();

                    StatusBarClosed?.Invoke();
                }
            });

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            DrawStatusBar();

            if (_labelShowTime.HasValue)
            {
                var dest = new Rectangle(
                    DrawAreaWithParentOffset.X,
                    DrawAreaWithParentOffset.Y + ScaledBarHeight - 3,
                    ScaledBarWidth,
                    (int)(DropDownSourceRectangle.Height * BarScale));

                _spriteBatch.Begin();
                _spriteBatch.Draw(_texture, dest, DropDownSourceRectangle, Color.White);
                _spriteBatch.End();
            }

            base.OnDrawControl(gameTime);
        }

        protected override bool HandleMouseDown(IXNAControl control, MouseEventArgs eventArgs)
        {
            _label.Visible = !_label.Visible;
            _labelShowTime = _label.SomeWhen(x => x.Visible).Map(_ => DateTime.Now);

            StatusBarClicked?.Invoke();

            return true;
        }

        protected void ChangeStatusBarPosition()
        {
            var xCoord = (_clientWindowSizeProvider.Width / 2) + StatusBarIndex * ScaledBarWidth;
            DrawPosition = new Vector2(xCoord, 0);
            DrawArea = new Rectangle((int)DrawPosition.X, (int)DrawPosition.Y, ScaledBarWidth, ScaledBarHeight);
            _label.DrawPosition = new Vector2(6 * BarScale, ScaledBarHeight + 1);
        }

        protected void DrawScaledStatusBar(Rectangle source, Rectangle maskSource)
        {
            var dest = new Rectangle(
                DrawAreaWithParentOffset.X,
                DrawAreaWithParentOffset.Y,
                ScaledBarWidth,
                ScaledBarHeight);

            var maskWidth = (int)(maskSource.Width * BarScale);
            var maskDest = new Rectangle(dest.X, dest.Y, maskWidth, dest.Height);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_texture, dest, source, Color.White);
            _spriteBatch.Draw(_texture, maskDest, maskSource, Color.White);
            _spriteBatch.End();
        }

        /// <summary>
        /// Source rectangle for the drop-down box in the texture sprite sheet (shown when control is clicked)
        /// </summary>
        private static Rectangle DropDownSourceRectangle => new Rectangle(220, 30, 110, 21);
    }
}
