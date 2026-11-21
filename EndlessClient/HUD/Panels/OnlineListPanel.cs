using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.Audio;
using EndlessClient.ControlSets;
using EndlessClient.HUD.Controls;
using EndlessClient.Rendering;
using EndlessClient.Services;
using EndlessClient.UIControls;
using EndlessClient.HUD;
using EOLib.Domain.Online;
using EOLib.Domain.Party;
using EOLib.Extensions;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moffat.EndlessOnline.SDK.Protocol.Net.Server;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using Optional.Unsafe;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class OnlineListPanel : DraggableHudPanel
    {
        private enum Filter
        {
            All,
            Friends,
            Admins,
            Party,
            Max
        }

        private const int DRAW_NAME_X = 18,
                          DRAW_OFFSET_Y = 23;

        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IHudControlProvider _hudControlProvider;
        private readonly IOnlinePlayerProvider _onlinePlayerProvider;
        private readonly IPartyDataProvider _partyDataProvider;
        private readonly IFriendIgnoreListService _friendIgnoreListService;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly bool _resizable;

        private readonly BitmapFont _chatFont;

        private readonly List<OnlinePlayerInfo> _onlineList;
        private readonly IXNALabel _totalNumberOfPlayers;
        private readonly ScrollBar _scrollBar;
        private readonly ClickableArea _filterClickArea;

        private readonly Texture2D _weirdOffsetTextureSheet, _chatIconsTexture;
        private readonly Rectangle[] _filterTextureSources;

        private HashSet<OnlinePlayerInfo> _cachedList;

        private Filter _filter;
        private List<OnlinePlayerInfo> _filteredList;
        private IReadOnlyList<string> _friendList;

        public OnlineListPanel(INativeGraphicsManager nativeGraphicsManager,
                               IHudControlProvider hudControlProvider,
                               IOnlinePlayerProvider onlinePlayerProvider,
                               IPartyDataProvider partyDataProvider,
                               IFriendIgnoreListService friendIgnoreListService,
                               ISfxPlayer sfxPlayer,
                               BitmapFont chatFont,
                               IClientWindowSizeProvider clientWindowSizeProvider)
            : base(clientWindowSizeProvider.Resizable)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _hudControlProvider = hudControlProvider;
            _onlinePlayerProvider = onlinePlayerProvider;
            _partyDataProvider = partyDataProvider;
            _friendIgnoreListService = friendIgnoreListService;
            _sfxPlayer = sfxPlayer;
            _chatFont = chatFont;
            _onlineList = new List<OnlinePlayerInfo>();

            _resizable = clientWindowSizeProvider.Resizable;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 36);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, _resizable);

            _totalNumberOfPlayers = new XNALabel(HudUiMetrics.PanelLabelFont(_resizable))
            {
                AutoSize = false,
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleRight,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(454, 1, 27, 14), _resizable),
                BackColor = Color.Transparent,
            };
            _totalNumberOfPlayers.SetParentControl(this);

            var scrollPos = HudUiMetrics.ScalePoint(467, 20, _resizable);
            var scrollSize = HudUiMetrics.ScalePoint(16, 97, _resizable);
            _scrollBar = new ScrollBar(scrollPos, scrollSize, ScrollBarColors.LightOnMed, _nativeGraphicsManager)
            {
                LinesToRender = 7,
                Visible = true
            };
            _scrollBar.SetParentControl(this);
            SetScrollWheelHandler(_scrollBar);

            _filterClickArea = new ClickableArea(HudUiMetrics.ScaleRectangle(new Rectangle(2, 2, 14, 14), _resizable));
            _filterClickArea.SetParentControl(this);
            _filterClickArea.OnMouseDown += FilterClickArea_Click;

            _weirdOffsetTextureSheet = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, true);
            _chatIconsTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 32, true);

            _filterTextureSources = new Rectangle[4];
            for (int i = 0; i < _filterTextureSources.Length; ++i)
                _filterTextureSources[i] = new Rectangle(i % 2 == 0 ? 0 : 12, i >= 2 ? 246 : 233, 12, 13);

            _cachedList = new HashSet<OnlinePlayerInfo>();
            _filter = Filter.All;
            _filteredList = new List<OnlinePlayerInfo>();
        }

        public override void Initialize()
        {
            _totalNumberOfPlayers.Initialize();
            _scrollBar.Initialize();
            _filterClickArea.Initialize();

            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            if (!_cachedList.SetEquals(_onlinePlayerProvider.OnlinePlayers))
            {
                _cachedList = _onlinePlayerProvider.OnlinePlayers.ToHashSet();

                // keep the friends list data from overriding the displayed data in this panel
                // it will be friends list data if all titles (or any field other than name) are empty
                if (!_cachedList.All(x => x.Title == string.Empty))
                {
                    _onlineList.Clear();
                    _onlineList.AddRange(_cachedList);

                    _onlineList.Sort((a, b) => a.Name.CompareTo(b.Name));
                    _filteredList = new List<OnlinePlayerInfo>(_onlineList);

                    _totalNumberOfPlayers.Text = $"{_onlineList.Count}";
                    _scrollBar.UpdateDimensions(_onlineList.Count);
                    _scrollBar.ScrollToTop();

                    _friendList = _friendIgnoreListService.LoadList(Constants.FriendListFile);

                    ApplyFilter();
                }
            }

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            var rowH = HudUiMetrics.Scale(13, _resizable);
            var iconX = HudUiMetrics.Scale(4, _resizable);
            var nameX = HudUiMetrics.Scale(DRAW_NAME_X, _resizable);
            var titleX = HudUiMetrics.Scale(133, _resizable);
            var guildX = HudUiMetrics.Scale(245, _resizable);
            var classX = HudUiMetrics.Scale(359, _resizable);
            var offsetY = HudUiMetrics.Scale(DRAW_OFFSET_Y, _resizable);

            _spriteBatch.Begin();

            _spriteBatch.Draw(
                _weirdOffsetTextureSheet,
                new Vector2(
                    DrawAreaWithParentOffset.X + HudUiMetrics.Scale(4, _resizable),
                    DrawAreaWithParentOffset.Y + HudUiMetrics.Scale(2, _resizable)),
                _filterTextureSources[(int)_filter],
                Color.White);

            // todo: either a) use renderable approach like Chat/News or b) remove renderable approach in favor of this simpler method
            for (int i = _scrollBar.ScrollOffset; i < _scrollBar.ScrollOffset + _scrollBar.LinesToRender && i < _filteredList.Count; ++i)
            {
                int yCoord = offsetY + DrawAreaWithParentOffset.Y + (i - _scrollBar.ScrollOffset) * rowH;
                _spriteBatch.Draw(_chatIconsTexture, new Vector2(DrawAreaWithParentOffset.X + iconX, yCoord), GetChatIconSourceRectangle(_filteredList[i].Icon), Color.White);
                _spriteBatch.DrawString(_chatFont, _filteredList[i].Name, new Vector2(DrawAreaWithParentOffset.X + nameX, yCoord), Color.Black);
                _spriteBatch.DrawString(_chatFont, _filteredList[i].Title, new Vector2(DrawAreaWithParentOffset.X + titleX, yCoord), Color.Black);
                _spriteBatch.DrawString(_chatFont, _filteredList[i].Guild, new Vector2(DrawAreaWithParentOffset.X + guildX, yCoord), Color.Black);
                _spriteBatch.DrawString(_chatFont, _filteredList[i].Class, new Vector2(DrawAreaWithParentOffset.X + classX, yCoord), Color.Black);
            }

            _spriteBatch.End();
        }

        protected override bool HandleMouseDown(IXNAControl control, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButton.Right)
                return false;

            var mousePos = eventArgs.Position;
            if (mousePos.X >= DrawAreaWithParentOffset.X + DRAW_NAME_X && mousePos.X <= _scrollBar.DrawAreaWithParentOffset.X &&
                mousePos.Y >= DrawAreaWithParentOffset.Y + DRAW_OFFSET_Y && mousePos.Y <= DrawAreaWithParentOffset.Y + DrawAreaWithParentOffset.Height)
            {
                var index = (mousePos.Y - (DrawAreaWithParentOffset.Y + DRAW_OFFSET_Y)) / 13;
                if (index >= 0 && index <= _filteredList.Count)
                {
                    var name = _filteredList[_scrollBar.ScrollOffset + index].Name;
                    _hudControlProvider.GetComponent<ChatTextBox>(HudControlIdentifier.ChatTextBox).Text = $"!{name} ";
                }
            }

            return true;
        }

        private void FilterClickArea_Click(object sender, EventArgs e)
        {
            _sfxPlayer.PlaySfx(SoundEffectID.DialogButtonClick);
            _filter = (Filter)(((int)_filter + 1) % (int)Filter.Max);
            _scrollBar.ScrollToTop();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            switch (_filter)
            {
                case Filter.Friends: _filteredList = _onlineList.Where(x => _friendList.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)).ToList(); break;
                case Filter.Admins: _filteredList = _onlineList.Where(IsAdminIcon).ToList(); break;
                case Filter.Party: _filteredList = _onlineList.Where(x => _partyDataProvider.Members.Any(y => string.Equals(y.Name, x.Name, StringComparison.InvariantCultureIgnoreCase))).ToList(); break;
                case Filter.All:
                default: _filteredList = new List<OnlinePlayerInfo>(_onlineList); break;
            }

            _scrollBar.UpdateDimensions(_filteredList.Count);
        }

        private static bool IsAdminIcon(OnlinePlayerInfo onlineInfo)
        {
            return onlineInfo.Icon switch
            {
                CharacterIcon.Gm or CharacterIcon.Hgm or CharacterIcon.GmParty or CharacterIcon.HgmParty => true,
                _ => false,
            };
        }

        private static Rectangle? GetChatIconSourceRectangle(CharacterIcon icon)
        {
            var (x, y, width, height) = icon.ToChatIcon().GetChatIconRectangleBounds().ValueOrDefault();
            return new Rectangle(x, y, width, height);
        }
    }
}
