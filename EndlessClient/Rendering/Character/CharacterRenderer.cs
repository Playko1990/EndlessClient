using System;
using System.Linq;
using EndlessClient.Audio;
using EndlessClient.Input;
using EndlessClient.Rendering.CharacterProperties;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Effects;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.Metadata;
using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.UIControls;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Map;
using EOLib.Domain.Spells;
using EOLib.IO.Map;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Optional;
using XNAControls;

namespace EndlessClient.Rendering.Character
{
    public class CharacterRenderer : DrawableGameComponent, ICharacterRenderer
    {
        private readonly object _rt_locker_ = new();

        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IHealthBarRendererFactory _healthBarRendererFactory;
        private readonly IChatBubbleFactory _chatBubbleFactory;
        private readonly ICharacterProvider _characterProvider;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly ICharacterPropertyRendererBuilder _characterPropertyRendererBuilder;
        private readonly ICharacterTextures _characterTextures;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IMetadataProvider<HatMetadata> _hatMetadataProvider;
        private readonly IMetadataProvider<WeaponMetadata> _weaponMetadataProvider;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;
        private readonly IEffectRenderer _effectRenderer;

        private readonly bool _isUiControl;

        private EOLib.Domain.Character.Character _character;
        private bool _textureUpdateRequired, _positionIsRelative = true;

        private SpriteBatch _sb;
        private RenderTarget2D _charRenderTarget;
        private Texture2D _outline;

        private BlinkingLabel _nameLabel;
        private string _shoutName = string.Empty;
        private DateTime? _spellCastTime;
        private bool _showName = true;

        private IHealthBarRenderer _healthBarRenderer;
        private Lazy<IChatBubble> _chatBubble;

        private bool _lastIsDead;

        private Color[] _rtColorData;

        // Cached standing skin cell size for DrawArea. Must stay stable across walk/attack
        // frames or the paperdoll anchor bounces every animation tick.
        private int _standingSkinWidth = 18;
        private int _standingSkinHeight = 58;
        private bool _standingSkinSizeCached;

        public EOLib.Domain.Character.Character Character
        {
            get { return _character; }
            set
            {
                if (_character == value) return;
                _textureUpdateRequired = _character.RenderProperties.GetHashCode() != value.RenderProperties.GetHashCode();
                if (_character.RenderProperties.Race != value.RenderProperties.Race
                    || _character.RenderProperties.Gender != value.RenderProperties.Gender)
                {
                    _standingSkinSizeCached = false;
                }
                _character = value;
            }
        }

        public bool Transparent { get; set; }

        public Rectangle DrawArea { get; private set; }

        public ISpellTargetable SpellTarget => Character;

        public int NameLabelY { get; private set; }

        public int HorizontalCenter { get; private set; }

        public int HealthBarAnchorY => DrawArea.Y;

        public bool IsAlive => !Character.RenderProperties.IsDead;

        public CharacterRenderer(Game game,
                                 IRenderTargetFactory renderTargetFactory,
                                 IHealthBarRendererFactory healthBarRendererFactory,
                                 IChatBubbleFactory chatBubbleFactory,
                                 ICharacterProvider characterProvider,
                                 IRenderOffsetCalculator renderOffsetCalculator,
                                 ICharacterPropertyRendererBuilder characterPropertyRendererBuilder,
                                 ICharacterTextures characterTextures,
                                 ICurrentMapProvider currentMapProvider,
                                 IUserInputProvider userInputProvider,
                                 IEffectRendererFactory effectRendererFactory,
                                 IMetadataProvider<HatMetadata> hatMetadataProvider,
                                 IMetadataProvider<WeaponMetadata> weaponMetadataProvider,
                                 ISfxPlayer sfxPlayer,
                                 IClientWindowSizeRepository clientWindowSizeRepository,
                                 EOLib.Domain.Character.Character character,
                                 bool isUiControl)
            : base(game)
        {
            _renderTargetFactory = renderTargetFactory;
            _healthBarRendererFactory = healthBarRendererFactory;
            _chatBubbleFactory = chatBubbleFactory;
            _characterProvider = characterProvider;
            _renderOffsetCalculator = renderOffsetCalculator;
            _characterPropertyRendererBuilder = characterPropertyRendererBuilder;
            _characterTextures = characterTextures;
            _currentMapProvider = currentMapProvider;
            _userInputProvider = userInputProvider;
            _hatMetadataProvider = hatMetadataProvider;
            _weaponMetadataProvider = weaponMetadataProvider;
            _effectRenderer = effectRendererFactory.Create();
            _sfxPlayer = sfxPlayer;
            _clientWindowSizeRepository = clientWindowSizeRepository;
            _character = character;
            _isUiControl = isUiControl;

            _chatBubble = new Lazy<IChatBubble>(() => _chatBubbleFactory.CreateChatBubble(this));

            _clientWindowSizeRepository.GameWindowSizeChanged += RecreateRenderTargetEvent;
        }

        #region Game Component

        public override void Initialize()
        {
            RecreateRenderTarget();

            _sb = new SpriteBatch(Game.GraphicsDevice);

            if (!_isUiControl)
            {
                _nameLabel = new BlinkingLabel(Constants.FontSize08pt5)
                {
                    Visible = true,
                    TextWidth = 89,
                    TextAlign = LabelAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    AutoSize = true,
                    Text = _character?.Name ?? string.Empty,
                    DrawOrder = 30,
                    KeepInClientWindowBounds = false,
                };
                _nameLabel.Initialize();

                if (!_nameLabel.Game.Components.Contains(_nameLabel))
                    _nameLabel.Game.Components.Add(_nameLabel);

                _nameLabel.DrawPosition = GetNameLabelPosition();

                _healthBarRenderer = _healthBarRendererFactory.CreateHealthBarRenderer(this);
                Game.Components.Add(_healthBarRenderer);
            }
            base.Initialize();

            if (_isUiControl)
            {
                // Login/create previews are updated/drawn by CharacterControl only.
                // Staying in Game.Components caused a second update/draw pass that
                // flickered/bounced the character-select sprites.
                Enabled = false;
                if (Game?.Components != null && Game.Components.Contains(this))
                    Game.Components.Remove(this);
            }
        }

        protected override void LoadContent()
        {
            _textureUpdateRequired = true;
            _characterTextures.Refresh(_character.RenderProperties);

            _outline = new Texture2D(GraphicsDevice, 1, 1);
            _outline.SetData(new[] { Color.White });

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Effects can be rendered when character is not visible (leaving map)
            _effectRenderer.Update();

            if (!Visible)
                return;

            if (_positionIsRelative)
                SetGridCoordinatePosition();

            if (_textureUpdateRequired)
            {
                _characterTextures.Refresh(_character.RenderProperties);
                DrawToRenderTarget();

                _textureUpdateRequired = false;
            }

            if (!_isUiControl)
            {
                UpdateNameLabel();
                CheckForDead();
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // In-game characters are already composited by MapEntityRenderer into the map
            // render target, which MapRenderer blits with WorldCamera zoom. Drawing again
            // here with the zoom transform left ghost "character sheets" floating above
            // the player while walking/attacking.
            if (!_isUiControl)
            {
                base.Draw(gameTime);
                return;
            }

            if (!Visible || _sb.IsDisposed)
                return;

            if (!Character.RenderProperties.IsHidden || _characterProvider.MainCharacter.AdminLevel > 0)
            {
                _sb.Begin();
                DrawToSpriteBatch(_sb);

                if (_sb.IsDisposed)
                    return;
                _sb.End();
            }

            base.Draw(gameTime);
        }

        #endregion

        #region ICharacterRenderer

        public void SetAbsoluteScreenPosition(int xPosition, int yPosition)
        {
            SetScreenCoordinates(xPosition, yPosition);
            _positionIsRelative = false;
        }

        public void SetToCenterScreenPosition()
        {
            // Skin can be null briefly during login resize/device reset; never NRE here.
            var skinWidth = _characterTextures?.Skin?.SourceRectangle.Width ?? _standingSkinWidth;
            var skinHeight = _characterTextures?.Skin?.SourceRectangle.Height ?? _standingSkinHeight;
            if (skinWidth <= 0) skinWidth = _standingSkinWidth;
            if (skinHeight <= 0) skinHeight = _standingSkinHeight;

            var xCoord = _clientWindowSizeRepository.Resizable
                ? (_clientWindowSizeRepository.Width - skinWidth) / 2
                : 310;
            var yCoord = _clientWindowSizeRepository.Resizable
                ? (_clientWindowSizeRepository.Height - skinHeight) / 2 - 11
                : (298 - skinHeight) / 2 - skinHeight / 4 - 3;

            SetAbsoluteScreenPosition(xCoord, yCoord);
        }

        public void DrawToSpriteBatch(SpriteBatch spriteBatch)
        {
            _effectRenderer.DrawBehindTarget(spriteBatch);

            if (Visible)
            {
                lock (_rt_locker_)
                {
                    // Blit only the character footprint (+padding for weapon/hair swings).
                    // Drawing the full-window RT could leave stray paperdoll pixels that
                    // looked like floating "character sheets" above the player.
                    const int pad = 64;
                    var srcX = Math.Max(0, DrawArea.X - pad);
                    var srcY = Math.Max(0, DrawArea.Y - pad);
                    var srcW = Math.Min(_charRenderTarget.Width - srcX, DrawArea.Width + pad * 2);
                    var srcH = Math.Min(_charRenderTarget.Height - srcY, DrawArea.Height + pad * 2);
                    if (srcW > 0 && srcH > 0)
                    {
                        var source = new Rectangle(srcX, srcY, srcW, srcH);
                        var dest = new Vector2(srcX, srcY + GetSteppingStoneOffset(Character.RenderProperties));
                        spriteBatch.Draw(_charRenderTarget, dest, source, GetAlphaColor());
                    }
                }
            }

            _effectRenderer.DrawInFrontOfTarget(spriteBatch);
        }

        public void ShowName() => _showName = true;

        public void HideName() => _showName = false;

        #endregion

        #region Update/Drawing Helpers

        private void DrawToRenderTarget()
        {
            var weaponMetadata = _weaponMetadataProvider.GetValueOrDefault(Character.RenderProperties.WeaponGraphic);

            lock (_rt_locker_)
            {
                GraphicsDevice.SetRenderTarget(_charRenderTarget);
                GraphicsDevice.Clear(ClearOptions.Target, Color.Transparent, 0, 0);
                // PointClamp avoids sampling neighboring atlas cells on paperdoll edges.
                _sb.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

                var characterPropertyRenderers = _characterPropertyRendererBuilder
                    .BuildList(_characterTextures, _character.RenderProperties)
                    .Where(x => x.CanRender);
                foreach (var renderer in characterPropertyRenderers)
                    renderer.Render(_sb, DrawArea, weaponMetadata);

                //if (_gameStateProvider.CurrentState == GameStates.None)
                //{
                //    _sb.Draw(_outline, DrawArea.WithSize(DrawArea.Width, 1), Color.Black);
                //    _sb.Draw(_outline, DrawArea.WithPosition(new Vector2(DrawArea.X + DrawArea.Width, DrawArea.Y)).WithSize(1, DrawArea.Height), Color.Black);
                //    _sb.Draw(_outline, DrawArea.WithPosition(new Vector2(DrawArea.X, DrawArea.Y + DrawArea.Height)).WithSize(DrawArea.Width, 1), Color.Black);
                //    _sb.Draw(_outline, DrawArea.WithSize(1, DrawArea.Height), Color.Black);

                //    _sb.Draw(_outline, DrawArea, Color.FromNonPremultiplied(255, 0, 0, 64));
                //}

                _sb.End();
                GraphicsDevice.SetRenderTarget(null);

                ClipHair();
            }
        }

        private Color GetAlphaColor()
        {
            // don't render the transparent character layer if hidden/dead, otherwise the additive blending 
            //      will render it with full alpha
            if (_character.RenderProperties.IsHidden || _character.RenderProperties.IsDead)
                return Transparent ? Color.Transparent : Color.FromNonPremultiplied(255, 255, 255, 128);

            return Transparent
                ? Color.FromNonPremultiplied(255, 255, 255, 128)
                : Color.White;
        }

        private void SetGridCoordinatePosition()
        {
            var centerX = _clientWindowSizeRepository.Resizable ? (_clientWindowSizeRepository.Width - DrawArea.Width) / 2 : 310;
            var centerY = _clientWindowSizeRepository.Resizable ? (_clientWindowSizeRepository.Height - DrawArea.Height) / 2 - 8 : 104;

            var screenX = _renderOffsetCalculator.CalculateOffsetX(_character.RenderProperties) + centerX - GetMainCharacterOffsetX();
            var screenY = _renderOffsetCalculator.CalculateOffsetY(_character.RenderProperties) + centerY - GetMainCharacterOffsetY();

            SetScreenCoordinates(screenX, screenY);
        }

        private void SetScreenCoordinates(int xPosition, int yPosition)
        {
            TryCacheStandingSkinSize();

            if (DrawArea.X != xPosition || DrawArea.Y != yPosition
                || DrawArea.Width != _standingSkinWidth || DrawArea.Height != _standingSkinHeight)
            {
                DrawArea = new Rectangle(xPosition, yPosition, _standingSkinWidth, _standingSkinHeight);
                HorizontalCenter = xPosition + _standingSkinWidth / 2;
                _textureUpdateRequired = true;
            }
        }

        private void TryCacheStandingSkinSize()
        {
            // Login UI previews stay on the vanilla 18x58 anchor so slot layout doesn't jump.
            if (_isUiControl || _standingSkinSizeCached || _characterTextures?.Skin == null)
                return;

            // Only learn size from the standing pose so walk/attack sheet cells don't resize DrawArea
            if (Character.RenderProperties.CurrentAction != CharacterActionState.Standing
                || Character.RenderProperties.SitState != SitState.Standing)
                return;

            var src = _characterTextures.Skin.SourceRectangle;
            if (src.Width < 8 || src.Height < 16)
                return;

            // Ignore multi-race sheet accidents (full column height) — that is what made
            // login/previews bounce and stacked race parts appear under/above the body.
            if (src.Height > 96 || src.Width > 48)
                return;

            _standingSkinWidth = src.Width;
            _standingSkinHeight = src.Height;
            _standingSkinSizeCached = true;
        }

        private int GetMainCharacterOffsetX()
        {
            return _renderOffsetCalculator.CalculateOffsetX(_characterProvider.MainCharacter.RenderProperties);
        }

        private int GetMainCharacterOffsetY()
        {
            return _renderOffsetCalculator.CalculateOffsetY(_characterProvider.MainCharacter.RenderProperties);
        }

        private void UpdateNameLabel()
        {
            if (_isUiControl || _healthBarRenderer == null || _nameLabel == null)
                return;

            if (_healthBarRenderer.Visible)
            {
                _nameLabel.Visible = false;
            }
            else if (DrawArea.Contains(_userInputProvider.CurrentMouseState.Position) && _showName)
            {
                _nameLabel.Visible = true;
                _nameLabel.BlinkRate = null;
                _nameLabel.Text = !string.IsNullOrWhiteSpace(_character.GuildTag) ? $"{_character.Name} {_character.GuildTag}" : _character.Name;
            }
            else if (_shoutName != string.Empty && _nameLabel.Text != _shoutName)
            {
                _nameLabel.Visible = true;
                _nameLabel.BlinkRate = 250;
                _nameLabel.Text = _shoutName;
            }
            else if (_shoutName == string.Empty)
            {
                _nameLabel.Visible = false;
            }

            if (_spellCastTime.HasValue && (DateTime.Now - _spellCastTime.Value).TotalMilliseconds >= 600)
            {
                StopShout();
            }

            _nameLabel.DrawPosition = GetNameLabelPosition();
        }

        private Vector2 GetNameLabelPosition()
        {
            NameLabelY = DrawArea.Y - 12 - (int)(_nameLabel?.ActualHeight ?? 0) + ((int)Character.RenderProperties.SitState) * 10;
            return new Vector2(HorizontalCenter - (_nameLabel.ActualWidth / 2f), NameLabelY);
        }

        private bool GetIsSteppingStone(CharacterRenderProperties renderProps)
        {
            if (_isUiControl)
                return false;

            return _currentMapProvider.CurrentMap.Tiles[renderProps.MapY, renderProps.MapX] == TileSpec.Jump
                || (renderProps.IsActing(CharacterActionState.Walking) && _currentMapProvider.CurrentMap.Tiles[renderProps.GetDestinationY(), renderProps.GetDestinationX()] == TileSpec.Jump);
        }

        private int GetSteppingStoneOffset(CharacterRenderProperties renderProps)
        {
            var isSteppingStone = GetIsSteppingStone(renderProps);

            if (isSteppingStone && renderProps.IsActing(CharacterActionState.Walking))
            {
                switch (renderProps.ActualWalkFrame)
                {
                    case 1: return -8;
                    case 2: return -16;
                    case 3: return -16;
                    case 4: return -8;
                }
            }

            return 0;
        }

        private void CheckForDead()
        {
            if (_lastIsDead != _character.RenderProperties.IsDead)
            {
                _lastIsDead = _character.RenderProperties.IsDead;
                if (_lastIsDead)
                {
                    _sfxPlayer.PlaySfx(SoundEffectID.Dead);
                }
            }
        }

        private void ClipHair()
        {
            if (Character.RenderProperties.HatGraphic == 0 ||
                _hatMetadataProvider.GetValueOrDefault(Character.RenderProperties.HatGraphic).ClipMode != HatMaskType.Standard)
                return;

            lock (_rt_locker_)
            {
                // oof. I really need to learn how to use shaders or stencil buffer.
                // https://gamedev.stackexchange.com/questions/38118/best-way-to-mask-2d-sprites-in-xna/38150#38150

                // note: this operation causes a high number of GC events as the character's frame changes (walking/attacking)
                _charRenderTarget.GetData(_rtColorData);
                for (int i = 0; i < _rtColorData.Length; i++)
                {
                    if (_rtColorData[i] == Color.Black)
                        _rtColorData[i].A = 0;
                }
                _charRenderTarget.SetData(_rtColorData);
            }
        }

        #endregion

        public bool EffectIsPlaying()
        {
            return _effectRenderer.State == EffectState.Playing;
        }

        public void PlayEffect(int graphic)
        {
            if (_effectRenderer.EffectID == graphic && _effectRenderer.State == EffectState.Playing)
                _effectRenderer.Restart();

            _effectRenderer.PlayEffect(graphic, this);
        }

        // Called when the spell cast begins
        public void ShoutSpellPrep(string spellName)
        {
            _shoutName = spellName;
        }

        // Called when the spell prep time ends and the player actually casts the spell
        public void ShoutSpellCast()
        {
            _nameLabel.BlinkRate = null;
            _nameLabel.ForeColor = Color.FromNonPremultiplied(0xf5, 0xc8, 0x9c, 0xff); // todo: make constant for this
            _spellCastTime = DateTime.Now;
        }

        // Called when the shout (spell prep time) should be cancelled without casting
        public void StopShout()
        {
            if (_nameLabel == null)
                return;

            _nameLabel.Visible = false;
            _nameLabel.Text = _character.Name;
            _nameLabel.ForeColor = Color.White;
            _nameLabel.BlinkRate = null;
            _shoutName = string.Empty;
            _spellCastTime = null;
        }

        public void ShowDamageCounter(int damage, int percentHealth, bool isHeal)
        {
            if (isHeal)
                _healthBarRenderer.SetHealth(damage, percentHealth, () => _chatBubble.Value.Show());
            else
                _healthBarRenderer.SetDamage(damage.SomeWhen(d => d > 0), percentHealth, () => _chatBubble.Value.Show());

            _chatBubble.Value.Hide();
        }

        public void ShowChatBubble(string message, bool isGroupChat)
        {
            _chatBubble.Value.SetMessage(message, isGroupChat);
        }

        private void RecreateRenderTarget()
        {
            lock (_rt_locker_)
            {
                try
                {
                    var next = _renderTargetFactory.CreateRenderTarget();
                    var old = _charRenderTarget;
                    _charRenderTarget = next;
                    _rtColorData = new Color[next.Width * next.Height];
                    old?.Dispose();
                }
                catch
                {
                    // GraphicsDevice can be mid-reset during maximize/ApplyChanges.
                    // Keep the previous target; next resize will retry.
                }
            }
        }

        private void RecreateRenderTargetEvent(object sender, EventArgs e)
        {
            RecreateRenderTarget();

            // New RT is empty until the next paperdoll bake — without this flag the
            // character stays invisible after login maximize / window sync.
            _textureUpdateRequired = true;

            if (_character == _characterProvider.MainCharacter)
                SetToCenterScreenPosition();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _outline?.Dispose();

                if (Game != null && Game.Components != null)
                {
                    if (Game.Components.Contains(_nameLabel))
                    {
                        Game.Components.Remove(_nameLabel);
                    }

                    if (Game.Components.Contains(_healthBarRenderer))
                    {
                        Game.Components.Remove(_healthBarRenderer);
                    }
                }
                _nameLabel?.Dispose();
                _healthBarRenderer?.Dispose();

                if (_chatBubble.IsValueCreated)
                    _chatBubble.Value?.Dispose();

                _sb?.Dispose();

                _clientWindowSizeRepository.GameWindowSizeChanged -= RecreateRenderTargetEvent;

                lock (_rt_locker_)
                    _charRenderTarget?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
