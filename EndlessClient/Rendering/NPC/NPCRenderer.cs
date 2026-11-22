using System;
using EndlessClient.GameExecution;
using EndlessClient.Input;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Effects;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Extensions;
using EOLib.Domain.NPC;
using EOLib.Domain.Spells;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Optional;
using XNAControls;

namespace EndlessClient.Rendering.NPC
{
    public class NPCRenderer : DrawableGameComponent, INPCRenderer
    {
        private static readonly object _rt_locker_ = new object();

        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly INPCSpriteSheet _npcSpriteSheet;
        private readonly INPCSpriteDataCache _npcSpriteDataCache;
        private readonly IGridDrawCoordinateCalculator _gridDrawCoordinateCalculator;
        private readonly IHealthBarRendererFactory _healthBarRendererFactory;
        private readonly IChatBubbleFactory _chatBubbleFactory;
        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IEffectRenderer _effectRenderer;
        private readonly IHealthBarRenderer _healthBarRenderer;

        private RenderTarget2D _npcRenderTarget;
        private SpriteBatch _spriteBatch;

        private DateTime _lastStandingAnimation;
        private int _fadeAwayAlpha;
        private bool _isDying, _isBlankSprite;

        // Opaque-body mid-X / top-Y within the current frame (cached per frame/direction).
        private int _opaqueCenterInFrame = -1;
        private int _opaqueTopInFrame;
        private NPCFrame _opaqueCenterFrame;
        private EODirection _opaqueCenterDirection;
        private int _opaqueCenterGraphic = -1;

        private XNALabel _nameLabel;
        private IChatBubble _chatBubble;

        public int NameLabelY { get; private set; }

        public int HorizontalCenter { get; private set; }

        public int HealthBarAnchorY => OpaqueSpriteTop;

        public bool IsAlive => !_isDying && !IsDead;

        public Rectangle DrawArea { get; private set; }

        public EOLib.Domain.NPC.NPC NPC { get; set; }

        public ISpellTargetable SpellTarget => NPC;

        public bool IsDead { get; private set; }

        public Rectangle EffectTargetArea { get; private set; }

        public NPCRenderer(IEndlessGameProvider endlessGameProvider,
                           IClientWindowSizeProvider clientWindowSizeProvider,
                           IENFFileProvider enfFileProvider,
                           INPCSpriteSheet npcSpriteSheet,
                           INPCSpriteDataCache npcSpriteDataCache,
                           IGridDrawCoordinateCalculator gridDrawCoordinateCalculator,
                           IHealthBarRendererFactory healthBarRendererFactory,
                           IChatBubbleFactory chatBubbleFactory,
                           IRenderTargetFactory renderTargetFactory,
                           IUserInputProvider userInputProvider,
                           IEffectRendererFactory effectRendererFactory,
                           EOLib.Domain.NPC.NPC initialNPC)
            : base((Game)endlessGameProvider.Game)
        {
            NPC = initialNPC;
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _enfFileProvider = enfFileProvider;
            _npcSpriteSheet = npcSpriteSheet;
            _npcSpriteDataCache = npcSpriteDataCache;
            _gridDrawCoordinateCalculator = gridDrawCoordinateCalculator;
            _healthBarRendererFactory = healthBarRendererFactory;
            _chatBubbleFactory = chatBubbleFactory;
            _renderTargetFactory = renderTargetFactory;
            _userInputProvider = userInputProvider;
            _effectRenderer = effectRendererFactory.Create();

            DrawArea = GetStandingFrameRectangle();

            _lastStandingAnimation = DateTime.Now;
            _fadeAwayAlpha = 255;

            _clientWindowSizeProvider.GameWindowSizeChanged += RecreateRenderTarget;

            _healthBarRenderer = _healthBarRendererFactory.CreateHealthBarRenderer(this);
        }

        public override void Initialize()
        {
            UpdateDrawAreas();

            _nameLabel = new XNALabel(Constants.FontSize08pt5)
            {
                Visible = false,
                TextWidth = 89,
                TextAlign = LabelAlignment.MiddleCenter,
                ForeColor = Color.White,
                AutoSize = true,
                Text = _enfFileProvider.ENFFile[NPC.ID].Name,
                DrawOrder = 30,
                KeepInClientWindowBounds = false,
            };
            _nameLabel.Initialize();

            if (!_nameLabel.Game.Components.Contains(_nameLabel))
                _nameLabel.Game.Components.Add(_nameLabel);

            _nameLabel.DrawPosition = GetNameLabelPosition();

            lock (_rt_locker_)
                _npcRenderTarget = _renderTargetFactory.CreateRenderTarget();

            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            var graphic = _enfFileProvider.ENFFile[NPC.ID].Graphic;
            _npcSpriteDataCache.Populate(graphic);
            _isBlankSprite = _npcSpriteDataCache.IsBlankSprite(graphic);

            Game.Components.Add(_healthBarRenderer);

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;

            UpdateDrawAreas();
            UpdateStandingFrameAnimation();
            UpdateDeadState();
            DrawToRenderTarget();

            var currentMousePosition = _userInputProvider.CurrentMouseState.Position;

            if (DrawArea.Contains(currentMousePosition))
            {
                var chatBubbleIsVisible = _chatBubble != null && _chatBubble.Visible;
                _nameLabel.Visible = !_healthBarRenderer.Visible && !chatBubbleIsVisible && !_isDying && IsClickablePixel(currentMousePosition);
                _nameLabel.DrawPosition = GetNameLabelPosition();
            }
            else
            {
                _nameLabel.Visible = false;
            }

            _effectRenderer.Update();

            base.Update(gameTime);
        }

        // How far from an opaque pixel a click still counts (makes thin quest/shop sprites easier).
        private const int ClickPixelRadius = 12;

        public bool IsClickablePixel(Point currentMousePosition)
        {
            // Whole sprite bounds (plus a little padding) — enough for dialog NPCs with sparse art.
            var padded = DrawArea;
            padded.Inflate(ClickPixelRadius, ClickPixelRadius);
            if (!padded.Contains(currentMousePosition))
                return false;

            var cachedTexture = _npcSpriteDataCache.GetData(_enfFileProvider.ENFFile[NPC.ID].Graphic, NPC.Frame);
            if (_isBlankSprite || cachedTexture.Length == 0)
                return true;

            var currentFrame = _npcSpriteSheet.GetNPCTexture(_enfFileProvider.ENFFile[NPC.ID].Graphic, NPC.Frame, NPC.Direction);
            var frameW = currentFrame.Width;
            var frameH = currentFrame.Height;
            if (frameW <= 0 || frameH <= 0)
                return true;

            var local = currentMousePosition - DrawArea.Location;

            // Exact pixel or any opaque neighbor within radius.
            for (int dy = -ClickPixelRadius; dy <= ClickPixelRadius; dy++)
            {
                for (int dx = -ClickPixelRadius; dx <= ClickPixelRadius; dx++)
                {
                    if (dx * dx + dy * dy > ClickPixelRadius * ClickPixelRadius)
                        continue;

                    var x = local.X + dx;
                    var y = local.Y + dy;
                    if (x < 0 || y < 0 || x >= frameW || y >= frameH)
                        continue;

                    var index = y * frameW + x;
                    if (index < cachedTexture.Length && cachedTexture[index].A > 0)
                        return true;
                }
            }

            // Fallback: still allow clicks anywhere in the padded sprite box for usability.
            return DrawArea.Contains(currentMousePosition);
        }

        public void DrawToSpriteBatch(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            _effectRenderer.DrawBehindTarget(spriteBatch);
            if (_npcRenderTarget != null)
                spriteBatch.Draw(_npcRenderTarget, Vector2.Zero, Color.White);
            _effectRenderer.DrawInFrontOfTarget(spriteBatch);
        }

        public void StartDying()
        {
            _isDying = true;
        }

        public void ShowDamageCounter(int damage, int percentHealth, bool isHeal)
        {
            var optionalDamage = damage.SomeWhen(d => d > 0);
            _healthBarRenderer.SetDamage(optionalDamage, percentHealth, ShowChatBubble);
            _chatBubble?.Hide();

            void ShowChatBubble() => _chatBubble?.Show();
        }

        public void ShowChatBubble(string message, bool isGroupChat)
        {
            if (_chatBubble == null)
                _chatBubble = _chatBubbleFactory.CreateChatBubble(this);
            _chatBubble.SetMessage(message, isGroupChat: false);
        }

        #region Effects

        public bool EffectIsPlaying()
        {
            return _effectRenderer.State == EffectState.Playing;
        }

        public void PlayEffect(int graphic)
        {
            _effectRenderer.PlayEffect(graphic, this);
        }

        #endregion

        private Rectangle GetStandingFrameRectangle()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var baseFrame = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPCFrame.Standing, EODirection.Down);
            return new Rectangle(0, 0, baseFrame.Width, baseFrame.Height);
        }

        private void UpdateDrawAreas()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var frameTexture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPC.Frame, NPC.Direction);
            var metaData = _npcSpriteSheet.GetNPCMetadata(data.Graphic);

            var isUpOrRight = NPC.IsFacing(EODirection.Up, EODirection.Right) ? -1 : 1;
            var isDownOrRight = NPC.IsFacing(EODirection.Down, EODirection.Right) ? -1 : 1;

            int metaDataOffsetX, metaDataOffsetY;
            if (NPC.Frame == NPCFrame.Attack2)
            {
                metaDataOffsetX = metaData.AttackOffsetX * isUpOrRight + (metaData.OffsetX * isUpOrRight);
                metaDataOffsetY = metaData.AttackOffsetY * isDownOrRight - metaData.OffsetY;
            }
            else
            {
                metaDataOffsetX = metaData.OffsetX * isUpOrRight;
                metaDataOffsetY = -metaData.OffsetY;
            }

            var renderCoordinates = _gridDrawCoordinateCalculator.CalculateDrawCoordinates(NPC) +
                new Vector2(metaDataOffsetX - frameTexture.Width / 2, metaDataOffsetY - (frameTexture.Height - 23));
            DrawArea = frameTexture.Bounds.WithPosition(renderCoordinates);

            var horizontalOffset = metaData.OffsetX * (NPC.IsFacing(EODirection.Down, EODirection.Left) ? -1 : 1);

            // EO NPC frames are bottom-aligned with empty padding above the art. Center HP
            // on opaque body pixels so bars sit over the animal, not the padded frame / player.
            EnsureOpaqueBounds(data.Graphic, frameTexture);
            HorizontalCenter = DrawArea.X + _opaqueCenterInFrame;

            var nameLabelGridCoordinates = _gridDrawCoordinateCalculator.CalculateDrawCoordinates(NPC.WithX(NPC.X - 1).WithY(NPC.Y - 1));
            NameLabelY = (int)nameLabelGridCoordinates.Y - metaData.NameLabelOffset;

            EffectTargetArea = DrawArea.WithSize(DrawArea.Width + horizontalOffset * 2, DrawArea.Height);
        }

        /// <summary>
        /// Screen Y of the topmost opaque pixel — HP bars anchor here (not NameLabelY / frame top).
        /// </summary>
        public int OpaqueSpriteTop => DrawArea.Y + _opaqueTopInFrame;

        private void EnsureOpaqueBounds(int graphic, Texture2D frameTexture)
        {
            if (_opaqueCenterInFrame >= 0
                && _opaqueCenterGraphic == graphic
                && _opaqueCenterFrame == NPC.Frame
                && _opaqueCenterDirection == NPC.Direction)
                return;

            _opaqueCenterGraphic = graphic;
            _opaqueCenterFrame = NPC.Frame;
            _opaqueCenterDirection = NPC.Direction;

            var w = frameTexture?.Width ?? 0;
            var h = frameTexture?.Height ?? 0;
            // Frames are bottom-aligned (~23px feet inset). If alpha scan fails, park near the body.
            var fallbackTop = Math.Max(0, h - 48);
            var fallbackMid = Math.Max(0, w / 2);

            if (frameTexture == null || w <= 0 || h <= 0)
            {
                _opaqueCenterInFrame = fallbackMid;
                _opaqueTopInFrame = fallbackTop;
                return;
            }

            var pixels = new Color[w * h];
            frameTexture.GetData(pixels);

            var minX = w;
            var maxX = -1;
            var minY = h;
            var maxY = -1;
            // Stricter alpha so faint sheet noise doesn't count as the whole frame.
            const byte AlphaCut = 32;
            for (int y = 0; y < h; y++)
            {
                var row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].A < AlphaCut)
                        continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                _opaqueCenterInFrame = fallbackMid;
                _opaqueTopInFrame = fallbackTop;
                return;
            }

            var mid = (minX + maxX + 1) / 2;
            _opaqueTopInFrame = minY;

            // Draw flips Left/Down=None, Up/Right=FlipHorizontally — mirror opaque mid to match art.
            var flipped = NPC.IsFacing(EODirection.Up, EODirection.Right);
            _opaqueCenterInFrame = flipped ? w - mid : mid;
        }

        private void UpdateStandingFrameAnimation()
        {
            var now = DateTime.Now;

            var data = _enfFileProvider.ENFFile[NPC.ID];
            var metaData = _npcSpriteSheet.GetNPCMetadata(data.Graphic);

            if (!metaData.HasStandingFrameAnimation
                || !NPC.IsActing(NPCActionState.Standing)
                || (now - _lastStandingAnimation).TotalMilliseconds < 250)
                return;

            _lastStandingAnimation = now;
            NPC = NPC.WithFrame(NPC.Frame == NPCFrame.Standing ? NPCFrame.StandingFrame1 : NPCFrame.Standing);
        }

        private void UpdateDeadState()
        {
            if (!_isDying) return;

            if (_fadeAwayAlpha >= 3)
                _fadeAwayAlpha -= 3;
            IsDead = _fadeAwayAlpha <= 0 && !EffectIsPlaying();
        }

        private void DrawToRenderTarget()
        {
            if (_npcRenderTarget == null)
                return;

            var data = _enfFileProvider.ENFFile[NPC.ID];
            var texture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPC.Frame, NPC.Direction);

            var color = Color.FromNonPremultiplied(255, 255, 255, _fadeAwayAlpha);
            var effects = NPC.IsFacing(EODirection.Left, EODirection.Down) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            lock (_rt_locker_)
            {
                GraphicsDevice.SetRenderTarget(_npcRenderTarget);
                GraphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 1, 0);

                _spriteBatch.Begin();
                _spriteBatch.Draw(texture, DrawArea, null, color, 0, Vector2.Zero, effects, 1);
                _spriteBatch.End();

                GraphicsDevice.SetRenderTarget(null);
            }
        }

        private void RecreateRenderTarget(object sender, EventArgs e)
        {
            lock (_rt_locker_)
            {
                _npcRenderTarget.Dispose();
                _npcRenderTarget = _renderTargetFactory.CreateRenderTarget();
            }
        }

        private Vector2 GetNameLabelPosition()
        {
            return new Vector2(HorizontalCenter - (_nameLabel.ActualWidth / 2f), NameLabelY);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Components.Remove(_healthBarRenderer);
                _healthBarRenderer.Dispose();

                _nameLabel.Dispose();
                _chatBubble?.Dispose();
                _spriteBatch?.Dispose();

                lock (_rt_locker_)
                    _npcRenderTarget?.Dispose();

                _clientWindowSizeProvider.GameWindowSizeChanged -= RecreateRenderTarget;
            }

            base.Dispose(disposing);
        }
    }
}
