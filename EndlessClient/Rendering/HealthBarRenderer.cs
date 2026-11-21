using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.GameExecution;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Optional;

namespace EndlessClient.Rendering
{
    public class HealthBarRenderer : DrawableGameComponent, IHealthBarRenderer
    {
        private const int DigitWidth = 9;

        // Drawn above characters/NPCs — 2x native size for readability on large windows.
        private const float DisplayScale = 2f;

        private readonly IMapActor _parentReference;

        private readonly SpriteBatch _spriteBatch;
        private readonly Texture2D _sourceTexture;

        private static readonly Point _numberSpritesOffset, _healthBarSpritesOffset;
        private static readonly Rectangle _healthBarBackgroundSource;

        private Action _doneCallback;
        private bool _isMiss;
        private List<Rectangle> _numberSourceRectangles;
        private Rectangle _healthBarSourceRectangle;

        private float _frameOffset;
        private Vector2 _damageCounterPosition, _healthBarPosition;

        static HealthBarRenderer()
        {
            _numberSpritesOffset = new Point(40, 28);
            _healthBarSpritesOffset = new Point(0, 28);
            _healthBarBackgroundSource = new Rectangle(0, 28, 40, 7);
        }

        public HealthBarRenderer(IEndlessGameProvider endlessGameProvider,
                                 INativeGraphicsManager nativeGraphicsManager,
                                 IMapActor parentReference)
            : base((Game)endlessGameProvider.Game)
        {
            _parentReference = parentReference;

            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            _sourceTexture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 58, true);

            _numberSourceRectangles = [];
            UpdateOrder = DrawOrder = 99;
            Enabled = Visible = false;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void SetDamage(Option<int> value, int percentHealth, Action doneCallback = null)
        {
            Enabled = Visible = true;
            _isMiss = !value.HasValue;
            _doneCallback = doneCallback;
            _frameOffset = 0;

            _numberSourceRectangles.Clear();

            value.Match(
                some: v => _numberSourceRectangles.AddRange(
                    GetNumberSourceRectangles(v, isHeal: false)
                        .Select(x => new Rectangle(_numberSpritesOffset + x.Location, x.Size))),
                none: () => _numberSourceRectangles.Add(new Rectangle(_numberSpritesOffset + new Point(92, 0), new Point(30, 11))));

            _healthBarSourceRectangle = GetHealthBarSourceRectangle(percentHealth);
            _healthBarSourceRectangle = new Rectangle(_healthBarSpritesOffset + _healthBarSourceRectangle.Location, _healthBarSourceRectangle.Size);
        }

        public void SetHealth(int value, int percentHealth, Action doneCallback = null)
        {
            Enabled = Visible = true;
            _isMiss = false;
            _doneCallback = doneCallback;
            _frameOffset = 0;

            _numberSourceRectangles.Clear();
            _numberSourceRectangles.AddRange(
                GetNumberSourceRectangles(value, isHeal: true)
                    .Select(x => new Rectangle(_numberSpritesOffset + x.Location, x.Size)));

            _healthBarSourceRectangle = GetHealthBarSourceRectangle(percentHealth);
            _healthBarSourceRectangle = new Rectangle(_healthBarSpritesOffset + _healthBarSourceRectangle.Location, _healthBarSourceRectangle.Size);
        }

        public override void Update(GameTime gameTime)
        {
            _frameOffset += .1f;
            if (_frameOffset > 4)
            {
                Enabled = Visible = false;
                _doneCallback?.Invoke();
            }

            var barW = _healthBarBackgroundSource.Width * DisplayScale;
            var barH = _healthBarBackgroundSource.Height * DisplayScale;

            // Anchor just above the sprite top. NameLabelY can land mid-body on NPCs
            // (tile-based name placement), which made the 2x bar cover the sprite.
            var barTop = _parentReference.DrawArea.Y - barH - 4f;

            _healthBarPosition = new Vector2(
                _parentReference.HorizontalCenter - barW / 2f,
                barTop);

            if (_isMiss)
            {
                var missW = _numberSourceRectangles[0].Width * DisplayScale;
                var xPos = _parentReference.HorizontalCenter - missW / 2f;
                var yPos = barTop - _frameOffset * DisplayScale - barH;
                _damageCounterPosition = new Vector2(xPos, yPos);
            }
            else
            {
                var digitCount = _numberSourceRectangles.Count;
                var xPos = _parentReference.HorizontalCenter - (digitCount * DigitWidth * DisplayScale / 2f);
                var yPos = barTop - _frameOffset * DisplayScale - barH;
                _damageCounterPosition = new Vector2(xPos, yPos);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var numberNdx = 0;
            foreach (var numberSource in _numberSourceRectangles)
            {
                var dest = new Rectangle(
                    (int)(_damageCounterPosition.X + numberNdx * DigitWidth * DisplayScale),
                    (int)_damageCounterPosition.Y,
                    (int)(numberSource.Width * DisplayScale),
                    (int)(numberSource.Height * DisplayScale));
                _spriteBatch.Draw(_sourceTexture, dest, numberSource, Color.White);
                numberNdx++;
            }

            var barBgDest = new Rectangle(
                (int)_healthBarPosition.X,
                (int)_healthBarPosition.Y,
                (int)(_healthBarBackgroundSource.Width * DisplayScale),
                (int)(_healthBarBackgroundSource.Height * DisplayScale));
            _spriteBatch.Draw(_sourceTexture, barBgDest, _healthBarBackgroundSource, Color.White);

            var barFillDest = new Rectangle(
                (int)_healthBarPosition.X,
                (int)_healthBarPosition.Y,
                (int)(_healthBarSourceRectangle.Width * DisplayScale),
                (int)(_healthBarSourceRectangle.Height * DisplayScale));
            _spriteBatch.Draw(_sourceTexture, barFillDest, _healthBarSourceRectangle, Color.White);

            _spriteBatch.End();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spriteBatch.Dispose();
            }

            base.Dispose(disposing);
        }

        private static IEnumerable<Rectangle> GetNumberSourceRectangles(int value, bool isHeal)
        {
            var yCoord = isHeal ? 11 : 0;

            var digits = value.ToString();
            for (int i = 0; i < digits.Length; ++i)
            {
                int next = int.Parse($"{digits[i]}");
                yield return new Rectangle(next * DigitWidth, yCoord, 8, 11);
            }
        }

        private static Rectangle GetHealthBarSourceRectangle(int percentHealth)
        {
            // width of health bar is 40
            // percent -> pixels: (percentHealth / 100) * 40
            const double HealthBarFactor = .4;

            if (percentHealth >= 50)
                return new Rectangle(0, 7, (int)Math.Round(percentHealth * HealthBarFactor), 7);
            else if (percentHealth >= 25)
                return new Rectangle(0, 14, (int)Math.Round(percentHealth * HealthBarFactor), 7);
            else
                return new Rectangle(0, 21, (int)Math.Round(percentHealth * HealthBarFactor), 7);
        }
    }

    public interface IHealthBarRenderer : IDrawable, IGameComponent, IDisposable
    {
        void SetDamage(Option<int> value, int percentHealth, Action doneCallback = null);

        void SetHealth(int value, int percentHealth, Action doneCallback = null);
    }
}
