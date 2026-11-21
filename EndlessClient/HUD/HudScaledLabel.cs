using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using XNAControls;

namespace EndlessClient.HUD
{
    /// <summary>
    /// Bitmap-font label drawn at an explicit scale (XNALabel has no text-scale API).
    /// </summary>
    public class HudScaledLabel : XNAControl
    {
        private readonly string _fontName;
        private readonly float _textScale;
        private BitmapFont _font;

        public string Text { get; set; } = string.Empty;
        public Color ForeColor { get; set; } = Color.White;
        public LabelAlignment TextAlign { get; set; } = LabelAlignment.MiddleLeft;

        public HudScaledLabel(string fontName, float textScale)
        {
            _fontName = fontName;
            _textScale = textScale;
        }

        protected override void LoadContent()
        {
            _font = Game.Content.Load<BitmapFont>(_fontName);
            base.LoadContent();
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            if (_font == null || string.IsNullOrEmpty(Text))
            {
                base.OnDrawControl(gameTime);
                return;
            }

            var size = (Vector2)_font.MeasureString(Text) * _textScale;
            var offset = Vector2.Zero;

            if ((TextAlign & LabelAlignment.Center) == LabelAlignment.Center)
                offset.X = (DrawArea.Width - size.X) / 2f;
            else if ((TextAlign & LabelAlignment.Right) == LabelAlignment.Right)
                offset.X = DrawArea.Width - size.X;

            if ((TextAlign & LabelAlignment.Middle) == LabelAlignment.Middle)
                offset.Y = (DrawArea.Height - size.Y) / 2f;
            else if ((TextAlign & LabelAlignment.Bottom) == LabelAlignment.Bottom)
                offset.Y = DrawArea.Height - size.Y;

            var drawPos = DrawPositionWithParentOffset + offset;
            var transform = Matrix.CreateScale(_textScale) *
                            Matrix.CreateTranslation(drawPos.X, drawPos.Y, 0f);

            _spriteBatch.Begin(transformMatrix: transform);
            _spriteBatch.DrawString(_font, Text, Vector2.Zero, ForeColor);
            _spriteBatch.End();

            base.OnDrawControl(gameTime);
        }
    }
}
