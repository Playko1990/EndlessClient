using System;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;
using XNAControls;

namespace EndlessClient.UIControls
{
    public enum ScrollBarColors
    {
        LightOnDark, //bottom set of light
        LightOnLight, //top set of light
        LightOnMed, //middle set of light
        DarkOnDark //very bottom set
    }

    public class ScrollBar : XNAControl, IScrollHandler
    {
        private Rectangle scrollArea; //area valid for scrolling: always arrowSize from top and bottom
        public int ScrollOffset { get; private set; }
        public int LinesToRender { get; set; }

        private readonly XNAButton _upButton, _downButton, _scrollButton;
        private readonly Texture2D _backgroundTexture;
        private readonly Rectangle? _backgroundTextureSource;
        private readonly int _arrowSize;
        private readonly int _arrowWidth;
        private readonly bool _showArrowButtons;

        private int _totalHeight;

        public override Rectangle DrawArea
        {
            get => base.DrawArea;
            set
            {
                base.DrawArea = value;

                scrollArea = new Rectangle(0, _arrowSize, 0, value.Height - _arrowSize);

                if (_downButton != null)
                    _downButton.DrawPosition = new Vector2(_downButton.DrawPosition.X, value.Height - _arrowSize);
            }
        }

        public ScrollBar(Vector2 locationRelativeToParent,
                         Vector2 size,
                         ScrollBarColors palette,
                         INativeGraphicsManager nativeGraphicsManager,
                         float uiScale = 1f,
                         bool showArrowButtons = true)
        {
            DrawPosition = locationRelativeToParent;
            SetSize((int)size.X, (int)size.Y);
            ScrollOffset = 0;
            _showArrowButtons = showArrowButtons;

            _arrowSize = Math.Max(1, (int)Math.Round(15 * uiScale));
            _arrowWidth = Math.Max(1, (int)Math.Round(16 * uiScale));

            var scrollSpriteSheet = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 29);
            var upArrows = new Rectangle[2];
            var downArrows = new Rectangle[2];
            int vertOff;
            switch (palette)
            {
                case ScrollBarColors.LightOnLight: vertOff = 0; break;
                case ScrollBarColors.LightOnMed: vertOff = 105; break;
                case ScrollBarColors.LightOnDark: vertOff = 180; break;
                case ScrollBarColors.DarkOnDark: vertOff = 255; break;
                default: throw new ArgumentOutOfRangeException(nameof(palette));
            }

            //regions based on verticle offset (which is based on the chosen palette)
            upArrows[0] = new Rectangle(0, vertOff + 15 * 3, 16, 15);
            upArrows[1] = new Rectangle(0, vertOff + 15 * 4, 16, 15);
            downArrows[0] = new Rectangle(0, vertOff + 15, 16, 15);
            downArrows[1] = new Rectangle(0, vertOff + 15 * 2, 16, 15);
            var scrollBox = new Rectangle(0, vertOff, 16, 15);

            // Always create up/down for ScrollBy logic; only parent/draw them when requested.
            _upButton = new XNAButton(scrollSpriteSheet, Vector2.Zero, upArrows[0], upArrows[1]);
            _upButton.OnMouseDown += ArrowClicked;
            _upButton.Visible = _showArrowButtons;
            if (_showArrowButtons)
            {
                _upButton.SetParentControl(this);
                ApplyArrowButtonSize(_upButton, y: 0);
            }

            _downButton = new XNAButton(scrollSpriteSheet, new Vector2(0, size.Y - _arrowSize), downArrows[0], downArrows[1]);
            _downButton.OnMouseDown += ArrowClicked;
            _downButton.Visible = _showArrowButtons;
            if (_showArrowButtons)
            {
                _downButton.SetParentControl(this);
                ApplyArrowButtonSize(_downButton, y: size.Y - _arrowSize);
            }

            _scrollButton = new XNAButton(scrollSpriteSheet, new Vector2(0, _arrowSize), scrollBox, scrollBox);
            _scrollButton.OnClickDrag += OnScrollButtonDragged;
            _scrollButton.SetParentControl(this);
            ApplyArrowButtonSize(_scrollButton, y: _arrowSize);

            scrollArea = new Rectangle(0, _arrowSize, 0, (int)size.Y - _arrowSize);
            _totalHeight = DrawAreaWithParentOffset.Height;
        }

        public ScrollBar(Vector2 locationRelativeToParent, Texture2D backgroundTexture, Rectangle? backgroundTextureSource, ScrollBarColors palette, INativeGraphicsManager nativeGraphicsManager, float uiScale = 1f, bool showArrowButtons = true)
            : this(locationRelativeToParent,
                   (backgroundTextureSource.HasValue ? backgroundTextureSource.Value.Size : backgroundTexture.Bounds.Size).ToVector2(),
                   palette,
                   nativeGraphicsManager,
                   uiScale,
                   showArrowButtons)
        {
            _backgroundTexture = backgroundTexture;
            _backgroundTextureSource = backgroundTextureSource;

            // assume vertical scrollbar : center on background image
            if (_showArrowButtons)
            {
                _upButton.DrawPosition = new Vector2((DrawArea.Width - _upButton.DrawArea.Width) / 2, _upButton.DrawPosition.Y + 1);
                _downButton.DrawPosition = new Vector2((DrawArea.Width - _downButton.DrawArea.Width) / 2, _downButton.DrawPosition.Y);
            }
            _scrollButton.DrawPosition = new Vector2((DrawArea.Width - _scrollButton.DrawArea.Width) / 2, _scrollButton.DrawPosition.Y);
        }

        public override void Initialize()
        {
            if (_showArrowButtons)
            {
                _upButton.Initialize();
                _downButton.Initialize();
            }
            _scrollButton.Initialize();

            base.Initialize();
            SetArrowButtonsVisible(_showArrowButtons);
        }

        public void UpdateDimensions(int numberOfLines)
        {
            _totalHeight = numberOfLines;
        }

        public void ScrollToTop()
        {
            ScrollOffset = 0;
            _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawArea.X, _arrowSize);
        }

        public void ScrollToEnd()
        {
            while (ScrollOffset < _totalHeight - LinesToRender)
                ArrowClicked(_downButton, new EventArgs());
        }

        public void SetScrollOffset(int offset)
        {
            ScrollOffset = offset;
        }

        public void SetDownArrowFlashSpeed(int milliseconds)
        {
            _downButton.FlashSpeed = milliseconds;
        }

        /// <summary>
        /// Scroll content by a number of lines (negative = up, positive = down).
        /// </summary>
        public void ScrollBy(int delta)
        {
            if (delta == 0 || _totalHeight <= LinesToRender)
                return;

            if (delta < 0)
            {
                for (var i = 0; i < -delta; i++)
                    ArrowClicked(_upButton, EventArgs.Empty);
            }
            else
            {
                for (var i = 0; i < delta; i++)
                    ArrowClicked(_downButton, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Hide the built-in arrow sprites when large chrome arrows drive scrolling instead.
        /// Click areas stay disabled; use <see cref="ScrollBy"/> from the large arrows.
        /// </summary>
        public void SetArrowButtonsVisible(bool visible)
        {
            _upButton.Visible = visible;
            _downButton.Visible = visible;
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            if (_backgroundTexture != null)
            {
                _spriteBatch.Begin();
                _spriteBatch.Draw(_backgroundTexture, DrawAreaWithParentOffset, _backgroundTextureSource, Color.White);
                _spriteBatch.End();
            }

            base.OnDrawControl(gameTime);
        }

        //the point of arrowClicked and scrollDragged is to respond to input on the three buttons in such
        //     a way that ScrollOffset is updated and the Y coordinate for the scroll box is updated.
        //     ScrollOffset provides a value that is used within the EOScrollDialog.Draw method.
        //     The Y coordinate for the scroll box determines where it is drawn.
        private void ArrowClicked(object btn, EventArgs e)
        {
            //_totalHeight contains the number of lines to render
            //any less than LinesToRender shouldn't scroll
            if (_totalHeight <= LinesToRender)
                return;

            if (btn == _upButton)
            {
                if (ScrollOffset > 0)
                    ScrollOffset--;
                else
                    return;
            }
            else if (btn == _downButton)
            {
                if (_downButton.FlashSpeed.HasValue)
                    _downButton.FlashSpeed = null; //as soon as it is clicked, stop flashing

                if (ScrollOffset < _totalHeight - LinesToRender)
                    ScrollOffset++;
                else
                    return;
            }
            else
            {
                return;
            }

            var pixelsPerLine = (float)(scrollArea.Height - _scrollButton.DrawArea.Height * 2) / (_totalHeight - LinesToRender);
            _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawPosition.X, _arrowSize + pixelsPerLine * ScrollOffset);
            if (_scrollButton.DrawPosition.Y > scrollArea.Height - _scrollButton.DrawArea.Height)
            {
                _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawPosition.X, scrollArea.Height - _scrollButton.DrawArea.Height);
            }
        }

        private void OnScrollButtonDragged(object btn, MouseEventArgs e)
        {
            if (_downButton.FlashSpeed.HasValue)
                _downButton.FlashSpeed = null; //as soon as we are dragged, stop flashing

            var y = e.Position.Y - (DrawAreaWithParentOffset.Y + _scrollButton.DrawArea.Height / 2);

            if (y < _arrowSize)
                y = _arrowSize + 1;
            else if (y > scrollArea.Height - _scrollButton.DrawArea.Height)
                y = scrollArea.Height - _scrollButton.DrawArea.Height;

            _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawPosition.X, y);

            if (_totalHeight <= LinesToRender)
                return;

            var pixelsPerLine = (double)(scrollArea.Height - _scrollButton.DrawArea.Height * 2) / (_totalHeight - LinesToRender);
            ScrollOffset = (int)Math.Round((y - _arrowSize) / pixelsPerLine);
        }

        protected override bool HandleMouseWheelMoved(IXNAControl control, MouseEventArgs eventArgs)
        {
            if (_totalHeight <= LinesToRender)
                return false;

            //value must be /-120, otherwise you get "Natural" (smooth) scroll. We'll have none of that.
            var dif = eventArgs.ScrollWheelDelta / -120;
            if ((dif < 0 && dif + ScrollOffset >= 0) || (dif > 0 && ScrollOffset + dif <= _totalHeight - LinesToRender))
            {
                ScrollOffset += dif;

                var pixelsPerLine = (float)(scrollArea.Height - _scrollButton.DrawArea.Height * 2) /
                                    (_totalHeight - LinesToRender);
                _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawPosition.X, _arrowSize + pixelsPerLine * ScrollOffset);

                if (_scrollButton.DrawPosition.Y > scrollArea.Height - _scrollButton.DrawArea.Height)
                    _scrollButton.DrawPosition = new Vector2(_scrollButton.DrawPosition.X, scrollArea.Height - _scrollButton.DrawArea.Height);
            }

            return true;
        }

        private void ApplyArrowButtonSize(XNAButton button, float y)
        {
            var x = Math.Max(0, (DrawArea.Width - _arrowWidth) / 2);
            button.DrawArea = new Rectangle(x, (int)y, _arrowWidth, _arrowSize);
            button.ClickArea = new Rectangle(0, 0, _arrowWidth, _arrowSize);
            button.DrawPosition = new Vector2(x, y);
        }
    }
}
