using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.GameExecution;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.NPC;
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

        private static readonly object ActiveLock = new();
        private static readonly List<HealthBarRenderer> ActiveBars = new();

        private readonly IMapActor _parentReference;
        private readonly ICharacterRendererProvider _characterRendererProvider;
        private readonly bool _isNpc;

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
        private Rectangle _stackBounds;
        private bool _registeredActive;

        static HealthBarRenderer()
        {
            _numberSpritesOffset = new Point(40, 28);
            _healthBarSpritesOffset = new Point(0, 28);
            _healthBarBackgroundSource = new Rectangle(0, 28, 40, 7);
        }

        public HealthBarRenderer(IEndlessGameProvider endlessGameProvider,
                                 INativeGraphicsManager nativeGraphicsManager,
                                 ICharacterRendererProvider characterRendererProvider,
                                 IMapActor parentReference)
            : base((Game)endlessGameProvider.Game)
        {
            _parentReference = parentReference;
            _characterRendererProvider = characterRendererProvider;
            _isNpc = parentReference is INPCRenderer;

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
            RegisterActive();

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
            RegisterActive();

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
                UnregisterActive();
                _doneCallback?.Invoke();
                return;
            }

            var barW = _healthBarBackgroundSource.Width * DisplayScale;
            var barH = _healthBarBackgroundSource.Height * DisplayScale;

            // Players get more lift above the hair. NPCs stay tight over the opaque sprite
            // top so adjacent bars don't float up onto the player's head.
            var clearance = _isNpc ? 18f : 52f;
            var centerX = (float)_parentReference.HorizontalCenter;
            var barTop = _parentReference.HealthBarAnchorY - barH - clearance;

            var stackTop = barTop - _frameOffset * DisplayScale - barH;
            var stackBottom = barTop + barH;
            var stackHeight = stackBottom - stackTop;

            if (_isNpc)
                centerX = NudgeNpcBarOffPlayerSprite(centerX, barW, stackTop, stackHeight);

            // Player ↔ NPC (and NPC ↔ NPC) floating bars must not cover each other.
            // Prefer shifting the NPC sideways/down when a player bar is involved.
            SeparateFromOtherBars(ref centerX, ref barTop, barW, stackHeight);
            stackTop = barTop - _frameOffset * DisplayScale - barH;

            _healthBarPosition = new Vector2(centerX - barW / 2f, barTop);
            _stackBounds = MakeStackRect(centerX, stackTop, barW, barTop + barH - stackTop);

            if (_isMiss)
            {
                var missW = _numberSourceRectangles[0].Width * DisplayScale;
                _damageCounterPosition = new Vector2(centerX - missW / 2f, stackTop);
            }
            else
            {
                var digitCount = _numberSourceRectangles.Count;
                _damageCounterPosition = new Vector2(
                    centerX - (digitCount * DigitWidth * DisplayScale / 2f),
                    stackTop);
            }
        }

        /// <summary>
        /// Keep NPC chrome off the main player's sprite silhouette.
        /// </summary>
        private float NudgeNpcBarOffPlayerSprite(float centerX, float barW, float stackTop, float stackHeight)
        {
            var player = _characterRendererProvider.MainCharacterRenderer.Match(
                some: r => r,
                none: () => (ICharacterRenderer)null);
            if (player == null || ReferenceEquals(player, _parentReference))
                return centerX;

            var avoid = player.DrawArea;
            avoid.Inflate(12, 20);

            var pushAway = _parentReference.HorizontalCenter <= avoid.Center.X ? -1f : 1f;

            for (var i = 0; i < 40; i++)
            {
                var barRect = MakeStackRect(centerX, stackTop, barW, stackHeight);
                if (!barRect.Intersects(avoid))
                    break;

                centerX += pushAway * 4f;
            }

            return centerX;
        }

        /// <summary>
        /// Push this bar away from every other visible floating HP stack so player and
        /// NPC bars never sit on top of each other. When a player and NPC collide, the
        /// NPC moves (sideways + slightly down) so the player bar stays above their head.
        /// </summary>
        private void SeparateFromOtherBars(ref float centerX, ref float barTop, float barW, float stackHeight)
        {
            lock (ActiveLock)
            {
                for (var pass = 0; pass < 32; pass++)
                {
                    var stackTop = barTop - _frameOffset * DisplayScale - (_healthBarBackgroundSource.Height * DisplayScale);
                    var myRect = MakeStackRect(centerX, stackTop, barW, stackHeight);
                    myRect.Inflate(3, 3);

                    var moved = false;
                    foreach (var other in ActiveBars)
                    {
                        if (ReferenceEquals(other, this) || !other.Visible || !other.Enabled)
                            continue;

                        var theirs = other._stackBounds;
                        if (theirs.IsEmpty)
                            continue;

                        theirs.Inflate(3, 3);
                        if (!myRect.Intersects(theirs))
                            continue;

                        if (_isNpc != other._isNpc)
                        {
                            // Only the NPC yields — player bar stays parked above their head.
                            if (!_isNpc)
                                continue;

                            var dir = _parentReference.HorizontalCenter <= other._parentReference.HorizontalCenter
                                ? -1f
                                : 1f;
                            centerX += dir * 8f;
                            barTop += 6f;
                        }
                        else
                        {
                            var dir = _parentReference.HorizontalCenter <= other._parentReference.HorizontalCenter
                                ? -1f
                                : 1f;
                            centerX += dir * 6f;
                            barTop += dir > 0 ? 3f : -3f;
                        }

                        moved = true;
                        stackTop = barTop - _frameOffset * DisplayScale - (_healthBarBackgroundSource.Height * DisplayScale);
                        myRect = MakeStackRect(centerX, stackTop, barW, stackHeight);
                        myRect.Inflate(3, 3);
                    }

                    if (!moved)
                        break;
                }
            }
        }

        private static Rectangle MakeStackRect(float centerX, float stackTop, float barW, float stackHeight)
        {
            return new Rectangle(
                (int)Math.Floor(centerX - barW / 2f),
                (int)Math.Floor(stackTop),
                (int)Math.Ceiling(barW),
                (int)Math.Ceiling(Math.Max(1f, stackHeight)));
        }

        private void RegisterActive()
        {
            if (_registeredActive)
                return;

            lock (ActiveLock)
            {
                if (!_registeredActive)
                {
                    ActiveBars.Add(this);
                    _registeredActive = true;
                }
            }
        }

        private void UnregisterActive()
        {
            if (!_registeredActive)
                return;

            lock (ActiveLock)
            {
                ActiveBars.Remove(this);
                _registeredActive = false;
                _stackBounds = Rectangle.Empty;
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
                UnregisterActive();
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
