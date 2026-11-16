using System;
using System.Collections.Generic;
using System.Linq;
using AutomaticTypeMapper;
using EndlessClient.Audio;
using EndlessClient.Content;
using EndlessClient.Controllers;
using EndlessClient.ControlSets;
using EndlessClient.GameExecution;
using EndlessClient.HUD.Boss;
using EndlessClient.HUD.Chat;
using EndlessClient.HUD.Panels;
using EndlessClient.HUD.Spells;
using EndlessClient.HUD.StatusBars;
using EndlessClient.Input;
using EndlessClient.Network;
using EndlessClient.Rendering;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.Map;
using EndlessClient.Rendering.Metadata;
using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.Rendering.NPC;
using EndlessClient.UIControls;
using EOLib;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Login;
using EOLib.Domain.Map;
using EOLib.Domain.Pathing;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using EOLib.Localization;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD.Controls
{
    [AutoMappedType(IsSingleton = true)]
    public class HudControlsFactory : IHudControlsFactory
    {
        private const int HUD_BASE_LAYER = 100;
        private const int HUD_CONTROL_LAYER = 130;

        private readonly IHudButtonController _hudButtonController;
        private readonly IHudPanelFactory _hudPanelFactory;
        private readonly IMapRendererFactory _mapRendererFactory;
        private readonly IUserInputHandlerFactory _userInputHandlerFactory;
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IGraphicsDeviceProvider _graphicsDeviceProvider;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;
        private readonly IEndlessGameProvider _endlessGameProvider;
        private readonly ICharacterRepository _characterRepository;
        private readonly IPlayerInfoProvider _playerInfoProvider;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;
        private readonly IUserInputRepository _userInputRepository;
        private readonly IStatusLabelSetter _statusLabelSetter;
        private readonly IStatusLabelTextProvider _statusLabelTextProvider;
        private readonly IContentProvider _contentProvider;
        private readonly IHudControlProvider _hudControlProvider;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IChatModeCalculator _chatModeCalculator;
        private readonly IExperienceTableProvider _experienceTableProvider;
        private readonly IPathFinder _pathFinder;
        private readonly ICharacterActions _characterActions;
        private readonly IWalkValidationActions _walkValidationActions;
        private readonly IChatBubbleActions _chatBubbleActions;
        private readonly IUnknownEntitiesRequestActions _unknownEntitiesRequestActions;
        private readonly IUserInputTimeProvider _userInputTimeProvider;
        private readonly ISpellSlotDataRepository _spellSlotDataRepository;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IMiniMapRendererFactory _miniMapRendererFactory;
        private readonly INewsProvider _newsProvider;
        private readonly IFixedTimeStepRepository _fixedTimeStepRepository;
        private readonly IClickDispatcherFactory _clickDispatcherFactory;
        private readonly IMetadataProvider<WeaponMetadata> _weaponMetadataProvider;
        private readonly ILocalizedStringFinder _localizedStringFinder;
        private readonly ICharacterRendererProvider _characterRendererProvider;
        private readonly INPCRendererProvider _npcRendererProvider;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly IBossHealthProvider _bossHealthProvider;
        private IChatController _chatController;
        private IMainButtonController _mainButtonController;

        // Resizable HUD chrome: side buttons + panels move as one docked unit.
        private readonly List<(InGameStates State, XNAButton Button)> _stateButtons = new();
        private readonly List<DraggableHudPanel> _dockPanels = new();
        private Vector2 _dockPanelPosition;
        private bool _hudChromeMinimized;
        private XNAButton _hudMinimizeButton;
        private XNAButton _hudRestoreButton;
        private DraggableHudPanel _lastVisibleDockPanel;

        public HudControlsFactory(IHudButtonController hudButtonController,
                                  IHudPanelFactory hudPanelFactory,
                                  IMapRendererFactory mapRendererFactory,
                                  IUserInputHandlerFactory userInputHandlerFactory,
                                  INativeGraphicsManager nativeGraphicsManager,
                                  IGraphicsDeviceProvider graphicsDeviceProvider,
                                  IClientWindowSizeRepository clientWindowSizeRepository,
                                  IEndlessGameProvider endlessGameProvider,
                                  ICharacterRepository characterRepository,
                                  IPlayerInfoProvider playerInfoProvider,
                                  ICurrentMapStateRepository currentMapStateRepository,
                                  IUserInputRepository userInputRepository,
                                  IStatusLabelSetter statusLabelSetter,
                                  IStatusLabelTextProvider statusLabelTextProvider,
                                  IContentProvider contentProvider,
                                  IHudControlProvider hudControlProvider,
                                  ICurrentMapProvider currentMapProvider,
                                  IChatModeCalculator chatModeCalculator,
                                  IExperienceTableProvider experienceTableProvider,
                                  IPathFinder pathFinder,
                                  ICharacterActions characterActions,
                                  IWalkValidationActions walkValidationActions,
                                  IChatBubbleActions chatBubbleActions,
                                  IUnknownEntitiesRequestActions unknownEntitiesRequestActions,
                                  IUserInputTimeProvider userInputTimeProvider,
                                  ISpellSlotDataRepository spellSlotDataRepository,
                                  ISfxPlayer sfxPlayer,
                                  IMiniMapRendererFactory miniMapRendererFactory,
                                  INewsProvider newsProvider,
                                  IFixedTimeStepRepository fixedTimeStepRepository,
                                  IClickDispatcherFactory clickDispatcherFactory,
                                  IMetadataProvider<WeaponMetadata> weaponMetadataProvider,
                                  ILocalizedStringFinder localizedStringFinder,
                                  ICharacterRendererProvider characterRendererProvider,
                                  INPCRendererProvider npcRendererProvider,
                                  IENFFileProvider enfFileProvider,
                                  IBossHealthProvider bossHealthProvider)
        {
            _hudButtonController = hudButtonController;
            _hudPanelFactory = hudPanelFactory;
            _mapRendererFactory = mapRendererFactory;
            _userInputHandlerFactory = userInputHandlerFactory;
            _nativeGraphicsManager = nativeGraphicsManager;
            _graphicsDeviceProvider = graphicsDeviceProvider;
            _clientWindowSizeRepository = clientWindowSizeRepository;
            _endlessGameProvider = endlessGameProvider;
            _characterRepository = characterRepository;
            _playerInfoProvider = playerInfoProvider;
            _currentMapStateRepository = currentMapStateRepository;
            _userInputRepository = userInputRepository;
            _statusLabelSetter = statusLabelSetter;
            _statusLabelTextProvider = statusLabelTextProvider;
            _contentProvider = contentProvider;
            _hudControlProvider = hudControlProvider;
            _currentMapProvider = currentMapProvider;
            _chatModeCalculator = chatModeCalculator;
            _experienceTableProvider = experienceTableProvider;
            _pathFinder = pathFinder;
            _characterActions = characterActions;
            _walkValidationActions = walkValidationActions;
            _chatBubbleActions = chatBubbleActions;
            _unknownEntitiesRequestActions = unknownEntitiesRequestActions;
            _userInputTimeProvider = userInputTimeProvider;
            _spellSlotDataRepository = spellSlotDataRepository;
            _sfxPlayer = sfxPlayer;
            _miniMapRendererFactory = miniMapRendererFactory;
            _newsProvider = newsProvider;
            _fixedTimeStepRepository = fixedTimeStepRepository;
            _clickDispatcherFactory = clickDispatcherFactory;
            _weaponMetadataProvider = weaponMetadataProvider;
            _localizedStringFinder = localizedStringFinder;
            _characterRendererProvider = characterRendererProvider;
            _npcRendererProvider = npcRendererProvider;
            _enfFileProvider = enfFileProvider;
            _bossHealthProvider = bossHealthProvider;
        }

        public void InjectChatController(IChatController chatController,
                                         IMainButtonController mainButtonController)
        {
            _chatController = chatController;
            _mainButtonController = mainButtonController;
        }

        public IReadOnlyDictionary<HudControlIdentifier, IGameComponent> CreateHud()
        {
            var characterAnimator = CreateCharacterAnimator();
            var mapRenderer = _mapRendererFactory.CreateMapRenderer();

            _stateButtons.Clear();
            _dockPanels.Clear();
            _hudChromeMinimized = false;
            if (_clientWindowSizeRepository.Resizable)
            {
                _dockPanelPosition = GetDefaultDockPanelPosition();
                var panelConfig = new IniReader(Constants.PanelLayoutFile);
                if (panelConfig.Load() &&
                    panelConfig.GetValue("PANELS", "Dock:X", out int dockX) &&
                    panelConfig.GetValue("PANELS", "Dock:Y", out int dockY))
                {
                    _dockPanelPosition = new Vector2(dockX, dockY);
                }
            }

            var controls = new Dictionary<HudControlIdentifier, IGameComponent>
            {
                {HudControlIdentifier.CurrentUserInputTracker, CreateCurrentUserInputTracker()},

                {HudControlIdentifier.CharacterAnimator, characterAnimator},
                {HudControlIdentifier.NPCAnimator, CreateNPCAnimator()},
                {HudControlIdentifier.MapRenderer, mapRenderer},
                {HudControlIdentifier.StatusIcons, CreatePlayerStatusIconRenderer()},
                {HudControlIdentifier.MiniMapRenderer, _miniMapRendererFactory.Create()},

                {HudControlIdentifier.ClickDispatcher, CreateClickDispatcher(mapRenderer)},

                {HudControlIdentifier.HudBackground, CreateHudBackground()},

                {HudControlIdentifier.InventoryButton, CreateStateChangeButton(InGameStates.Inventory)},
                {HudControlIdentifier.ViewMapButton, CreateStateChangeButton(InGameStates.ViewMapToggle)},
                {HudControlIdentifier.ActiveSpellsButton, CreateStateChangeButton(InGameStates.ActiveSpells)},
                {HudControlIdentifier.PassiveSpellsButton, CreateStateChangeButton(InGameStates.PassiveSpells)},
                {HudControlIdentifier.ChatButton, CreateStateChangeButton(InGameStates.Chat)},
                {HudControlIdentifier.StatsButton, CreateStateChangeButton(InGameStates.Stats)},
                {HudControlIdentifier.OnlineListButton, CreateStateChangeButton(InGameStates.OnlineList)},
                {HudControlIdentifier.PartyButton, CreateStateChangeButton(InGameStates.Party)},
                {HudControlIdentifier.MacroButton, CreateStateChangeButton(InGameStates.Macro)},
                {HudControlIdentifier.SettingsButton, CreateStateChangeButton(InGameStates.Settings)},
                {HudControlIdentifier.HelpButton, CreateStateChangeButton(InGameStates.Help)},

                {HudControlIdentifier.FriendList, CreateFriendListButton()},
                {HudControlIdentifier.IgnoreList, CreateIgnoreListButton()},

                {HudControlIdentifier.NewsPanel, CreateStatePanel(InGameStates.News)},
                {HudControlIdentifier.InventoryPanel, CreateStatePanel(InGameStates.Inventory)},
                {HudControlIdentifier.ActiveSpellsPanel, CreateStatePanel(InGameStates.ActiveSpells)},
                {HudControlIdentifier.PassiveSpellsPanel, CreateStatePanel(InGameStates.PassiveSpells)},
                {HudControlIdentifier.ChatPanel, CreateStatePanel(InGameStates.Chat)},
                {HudControlIdentifier.StatsPanel, CreateStatePanel(InGameStates.Stats)},
                {HudControlIdentifier.OnlineListPanel, CreateStatePanel(InGameStates.OnlineList)},
                {HudControlIdentifier.PartyPanel, CreateStatePanel(InGameStates.Party)},
                //macro panel
                {HudControlIdentifier.SettingsPanel, CreateStatePanel(InGameStates.Settings)},
                {HudControlIdentifier.HelpPanel, CreateStatePanel(InGameStates.Help)},

                {HudControlIdentifier.HudMinimizeButton, CreateHudMinimizeButton()},
                {HudControlIdentifier.HudRestoreButton, CreateHudRestoreButton()},

                {HudControlIdentifier.SessionExpButton, CreateSessionExpButton()},
                {HudControlIdentifier.QuestsButton, CreateQuestButton()},

                {HudControlIdentifier.HPStatusBar, CreateHPStatusBar()},
                {HudControlIdentifier.TPStatusBar, CreateTPStatusBar()},
                {HudControlIdentifier.SPStatusBar, CreateSPStatusBar()},
                {HudControlIdentifier.TNLStatusBar, CreateTNLStatusBar()},
                {HudControlIdentifier.BossHealthBar, CreateBossHealthBar()},

                {HudControlIdentifier.ChatModePictureBox, CreateChatModePictureBox()},
                {HudControlIdentifier.ChatTextBox, CreateChatTextBox()},
                {HudControlIdentifier.ClockLabel, CreateClockLabel()},
                {HudControlIdentifier.StatusLabel, CreateStatusLabel()},

                {HudControlIdentifier.PeriodicStatUpdater, CreatePeriodicStatUpdater()},
                {HudControlIdentifier.UserInputHandler, CreateUserInputHandler()},
                {HudControlIdentifier.UnknownEntitiesRequester, CreateUnknownEntitiesRequester()},
                {HudControlIdentifier.PeriodicEmoteHandler, CreatePeriodicEmoteHandler(characterAnimator)},

                {HudControlIdentifier.PreviousUserInputTracker, CreatePreviousUserInputTracker()}
            };

            if (_clientWindowSizeRepository.Resizable)
                WireHudChromeDocking();

            return controls;
        }

        private PlayerStatusIconRenderer CreatePlayerStatusIconRenderer()
        {
            return new PlayerStatusIconRenderer(
                _nativeGraphicsManager,
                (ICharacterProvider)_characterRepository,
                (ISpellSlotDataProvider)_spellSlotDataRepository,
                _currentMapProvider, _clientWindowSizeRepository);
        }

        private IClickDispatcher CreateClickDispatcher(IMapRenderer mapRenderer)
        {
            var dispatcher = _clickDispatcherFactory.Create();
            dispatcher.DrawOrder = mapRenderer.DrawOrder;
            return dispatcher;
        }

        private HudBackgroundFrame CreateHudBackground()
        {
            var frame = new HudBackgroundFrame(_nativeGraphicsManager, _graphicsDeviceProvider)
            {
                DrawOrder = HUD_BASE_LAYER,
                Visible = !_clientWindowSizeRepository.Resizable,
            };

            // Keep classic frame off once the window is resizable so it cannot leave a
            // stray chat strip covering the chat-mode sword.
            _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) =>
            {
                frame.Visible = !_clientWindowSizeRepository.Resizable;
            };

            return frame;
        }

        private IXNAButton CreateStateChangeButton(InGameStates whichState)
        {
            if (whichState == InGameStates.News)
                throw new ArgumentOutOfRangeException(nameof(whichState), "News state does not have a button associated with it");
            var buttonIndex = (int)whichState;

            var mainButtonTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 25);
            var widthDelta = mainButtonTexture.Width / 2;
            var heightDelta = mainButtonTexture.Height / 11;

            XNAButton retButton;
            if (!_clientWindowSizeRepository.Resizable)
            {
                var xPosition = buttonIndex < 6 ? 62 : 590;
                var yPosition = (buttonIndex < 6 ? 330 : 350) + (buttonIndex < 6 ? buttonIndex : buttonIndex - 6) * 20;

                retButton = new XNAButton(
                    mainButtonTexture,
                    new Vector2(xPosition, yPosition),
                    new Rectangle(0, heightDelta * buttonIndex, widthDelta, heightDelta),
                    new Rectangle(widthDelta, heightDelta * buttonIndex, widthDelta, heightDelta))
                {
                    DrawOrder = HUD_CONTROL_LAYER
                };
            }
            else
            {
                retButton = new XNAButton(
                    mainButtonTexture,
                    GetDockedButtonPosition(buttonIndex),
                    new Rectangle(0, heightDelta * buttonIndex, widthDelta, heightDelta),
                    new Rectangle(widthDelta, heightDelta * buttonIndex, widthDelta, heightDelta))
                {
                    DrawOrder = HUD_CONTROL_LAYER
                };

                _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) =>
                {
                    if (!_hudChromeMinimized)
                        retButton.DrawPosition = GetDockedButtonPosition(buttonIndex);
                };
            }

            retButton.OnMouseDown += (_, _) => DoHudStateChangeClick(whichState);
            retButton.OnMouseEnter += (_, _) => _statusLabelSetter.SetStatusLabel(
                EOResourceID.STATUS_LABEL_TYPE_BUTTON,
                EOResourceID.STATUS_LABEL_HUD_BUTTON_HOVER_FIRST + buttonIndex);

            _stateButtons.Add((whichState, retButton));
            return retButton;
        }

        private Vector2 GetDockedButtonPosition(int buttonIndex)
        {
            var panelTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 44);
            var panelWidth = panelTexture.Width;
            var panelPos = _dockPanelPosition;

            var xPosition = buttonIndex < 6
                ? panelPos.X - 40
                : panelPos.X + panelWidth;
            var yPosition = panelPos.Y
                + (buttonIndex < 6 ? 0 : 20)
                + (buttonIndex < 6 ? buttonIndex : buttonIndex - 6) * 20;

            return new Vector2(xPosition, yPosition);
        }

        private Vector2 GetDefaultDockPanelPosition()
        {
            var panelTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 44);
            return new Vector2(
                (_clientWindowSizeRepository.Width - panelTexture.Width) / 2f,
                _clientWindowSizeRepository.Height - 45 - panelTexture.Height);
        }

        private IXNAButton CreateFriendListButton()
        {
            Func<Vector2> getFriendListDrawPosition = () => new Vector2(_clientWindowSizeRepository.Width - 48, _clientWindowSizeRepository.Height - 37);
            var button = new XNAButton(
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, false),
                _clientWindowSizeRepository.Resizable ? getFriendListDrawPosition() : new Vector2(592, 312),
                new Rectangle(0, 260, 17, 15),
                new Rectangle(0, 276, 17, 15))
            {
                DrawOrder = HUD_CONTROL_LAYER + 10
            };
            button.OnMouseDown += (_, _) => _hudButtonController.ClickFriendList();
            button.OnMouseDown += (_, _) => _sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
            button.OnMouseOver += (o, e) => _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_BUTTON, EOResourceID.STATUS_LABEL_FRIEND_LIST);

            if (_clientWindowSizeRepository.Resizable)
            {
                _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) => button.DrawPosition = getFriendListDrawPosition();
            }

            return button;
        }

        private IXNAButton CreateIgnoreListButton()
        {
            Func<Vector2> getIgnoreListDrawPosition = () => new Vector2(_clientWindowSizeRepository.Width - 31, _clientWindowSizeRepository.Height - 37);
            var button = new XNAButton(
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, false),
                _clientWindowSizeRepository.Resizable ? getIgnoreListDrawPosition() : new Vector2(609, 312),
                new Rectangle(17, 260, 17, 15),
                new Rectangle(17, 276, 17, 15))
            {
                DrawOrder = HUD_CONTROL_LAYER + 10
            };
            button.OnMouseDown += (_, _) => _hudButtonController.ClickIgnoreList();
            button.OnMouseDown += (_, _) => _sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
            button.OnMouseOver += (o, e) => _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_BUTTON, EOResourceID.STATUS_LABEL_IGNORE_LIST);

            if (_clientWindowSizeRepository.Resizable)
            {
                _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) => button.DrawPosition = getIgnoreListDrawPosition();
            }

            return button;
        }

        private void DoHudStateChangeClick(InGameStates whichState)
        {
            // Tab clicks always open/switch panels — never leave the chrome minimized.
            if (_hudChromeMinimized)
                SetHudChromeMinimized(false);

            switch (whichState)
            {
                case InGameStates.Inventory: _hudButtonController.ClickInventory(); break;
                case InGameStates.ViewMapToggle: _hudButtonController.ClickViewMapToggle(); break;
                case InGameStates.ActiveSpells: _hudButtonController.ClickActiveSpells(); break;
                case InGameStates.PassiveSpells: _hudButtonController.ClickPassiveSpells(); break;
                case InGameStates.Chat:
                    _hudButtonController.ClickChat();
                    _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_ACTION, EOResourceID.STATUS_LABEL_CHAT_PANEL_NOW_VIEWED);
                    break;
                case InGameStates.Stats:
                    _hudButtonController.ClickStats();
                    _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_ACTION, EOResourceID.STATUS_LABEL_STATS_PANEL_NOW_VIEWED);
                    break;
                case InGameStates.OnlineList:
                    _hudButtonController.ClickOnlineList();
                    _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_ACTION, EOResourceID.STATUS_LABEL_ONLINE_PLAYERS_NOW_VIEWED);
                    break;
                case InGameStates.Party: _hudButtonController.ClickParty(); break;
                case InGameStates.Macro: break;
                case InGameStates.Settings: _hudButtonController.ClickSettings(); break;
                case InGameStates.Help:
                    _hudButtonController.ClickHelp();
                    _statusLabelSetter.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_ACTION, EOResourceID.STATUS_LABEL_HUD_BUTTON_HOVER_LAST);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(whichState), whichState, null);
            }

            _sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
        }

        private IGameComponent CreateStatePanel(InGameStates whichState)
        {
            DraggableHudPanel retPanel;

            switch (whichState)
            {
                case InGameStates.Inventory: retPanel = _hudPanelFactory.CreateInventoryPanel(); break;
                case InGameStates.ActiveSpells: retPanel = _hudPanelFactory.CreateActiveSpellsPanel(); break;
                case InGameStates.PassiveSpells: retPanel = _hudPanelFactory.CreatePassiveSpellsPanel(); break;
                case InGameStates.Chat: retPanel = _hudPanelFactory.CreateChatPanel(); break;
                case InGameStates.Stats: retPanel = _hudPanelFactory.CreateStatsPanel(); break;
                case InGameStates.OnlineList: retPanel = _hudPanelFactory.CreateOnlineListPanel(); break;
                case InGameStates.Party: retPanel = _hudPanelFactory.CreatePartyPanel(); break;
                case InGameStates.Settings: retPanel = _hudPanelFactory.CreateSettingsPanel(); break;
                case InGameStates.Help: retPanel = _hudPanelFactory.CreateHelpPanel(); break;
                case InGameStates.News: retPanel = _hudPanelFactory.CreateNewsPanel(); break;
                default: throw new ArgumentOutOfRangeException(nameof(whichState), whichState, "Panel specification is out of range.");
            }

            retPanel.Activated += () => retPanel.DrawOrder = _hudControlProvider.HudPanels.Select(x => x.DrawOrder).Max() + 1;

            //news is visible by default when loading the game if news text is set
            retPanel.Visible = (_newsProvider.NewsText.Any() && whichState == InGameStates.News) ||
                               (!_newsProvider.NewsText.Any() && whichState == InGameStates.Chat);

            if (_clientWindowSizeRepository.Resizable)
            {
                retPanel.UpdateOrder = -1;
                _dockPanels.Add(retPanel);

                if (_dockPanels.Count == 1)
                    _dockPanelPosition = GetDefaultDockPanelPosition();

                retPanel.DrawArea = retPanel.DrawArea.WithPosition(_dockPanelPosition);

                _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) =>
                {
                    if (_hudChromeMinimized)
                        return;

                    // Keep the dock on-screen after resize; panels share one position.
                    if (_clientWindowSizeRepository.Width < _dockPanelPosition.X + retPanel.DrawArea.Width)
                        _dockPanelPosition = new Vector2(_clientWindowSizeRepository.Width - retPanel.DrawArea.Width, _dockPanelPosition.Y);
                    if (_clientWindowSizeRepository.Height < _dockPanelPosition.Y + retPanel.DrawArea.Height)
                        _dockPanelPosition = new Vector2(_dockPanelPosition.X, _clientWindowSizeRepository.Height - retPanel.DrawArea.Height);

                    ApplyDockPositionToChrome();
                };

                // Ignore per-panel saved layouts that stack multiple windows; one shared dock.
                var panelConfig = new IniReader(Constants.PanelLayoutFile);
                if (panelConfig.Load())
                {
                    if (panelConfig.GetValue("PANELS", "Dock:X", out int x) &&
                        panelConfig.GetValue("PANELS", "Dock:Y", out int y))
                    {
                        _dockPanelPosition = new Vector2(x, y);
                        retPanel.DrawPosition = _dockPanelPosition;
                    }
                }
            }

            return retPanel;
        }

        private void WireHudChromeDocking()
        {
            ApplyDockPositionToChrome();

            foreach (var panel in _dockPanels)
            {
                panel.DragMoved += () =>
                {
                    _dockPanelPosition = panel.DrawPosition;
                    foreach (var other in _dockPanels)
                    {
                        if (!ReferenceEquals(other, panel))
                            other.DrawPosition = _dockPanelPosition;
                    }

                    RepositionStateButtons();
                    RepositionMinimizeButton();
                };

                panel.DragCompleted += () =>
                {
                    _dockPanelPosition = panel.DrawPosition;
                    ApplyDockPositionToChrome();
                    SaveDockPosition();
                };

                panel.Activated += () =>
                {
                    // Keep siblings under the same dock when one panel is brought forward.
                    foreach (var other in _dockPanels)
                        other.DrawPosition = _dockPanelPosition;
                    RepositionStateButtons();
                    RepositionMinimizeButton();
                };
            }
        }

        private Vector2 GetHudRestoreButtonPosition()
        {
            // Just above the chat bar on the right (chat row is Height-40), above Add Friend.
            return new Vector2(
                _clientWindowSizeRepository.Width - 48,
                _clientWindowSizeRepository.Height - 58);
        }

        private void ApplyDockPositionToChrome()
        {
            foreach (var panel in _dockPanels)
                panel.DrawPosition = _dockPanelPosition;

            RepositionStateButtons();
            RepositionMinimizeButton();
        }

        private void RepositionStateButtons()
        {
            foreach (var (state, button) in _stateButtons)
            {
                var buttonIndex = (int)state;
                button.DrawPosition = GetDockedButtonPosition(buttonIndex);
            }
        }

        private void RepositionMinimizeButton()
        {
            if (_hudMinimizeButton == null)
                return;

            var panelTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 44);
            _hudMinimizeButton.DrawPosition = new Vector2(
                _dockPanelPosition.X + panelTexture.Width - 22,
                _dockPanelPosition.Y - 18);
        }

        private void SaveDockPosition()
        {
            try
            {
                var panelConfig = new IniReader(Constants.PanelLayoutFile);
                panelConfig.Load();
                if (!panelConfig.Sections.ContainsKey("PANELS"))
                    panelConfig.Sections["PANELS"] = new System.Collections.Generic.SortedList<string, string>();

                panelConfig.Sections["PANELS"]["Dock:X"] = ((int)_dockPanelPosition.X).ToString();
                panelConfig.Sections["PANELS"]["Dock:Y"] = ((int)_dockPanelPosition.Y).ToString();
                panelConfig.Save();
            }
            catch
            {
                // layout persistence is best-effort
            }
        }

        private XNAButton CreateHudMinimizeButton()
        {
            var sheet = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, false);
            _hudMinimizeButton = new XNAButton(
                sheet,
                new Vector2(0, 0),
                new Rectangle(0, 260, 17, 15),
                new Rectangle(0, 276, 17, 15))
            {
                DrawOrder = HUD_CONTROL_LAYER + 20,
                Visible = _clientWindowSizeRepository.Resizable
            };

            if (_clientWindowSizeRepository.Resizable)
                RepositionMinimizeButton();

            _hudMinimizeButton.OnMouseDown += (_, _) =>
            {
                _sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
                SetHudChromeMinimized(true);
            };

            return _hudMinimizeButton;
        }

        private XNAButton CreateHudRestoreButton()
        {
            var sheet = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, false);
            _hudRestoreButton = new XNAButton(
                sheet,
                GetHudRestoreButtonPosition(),
                new Rectangle(17, 260, 17, 15),
                new Rectangle(17, 276, 17, 15))
            {
                DrawOrder = HUD_CONTROL_LAYER + 20,
                Visible = false
            };

            _clientWindowSizeRepository.GameWindowSizeChanged += (_, _) =>
            {
                _hudRestoreButton.DrawPosition = GetHudRestoreButtonPosition();
            };

            _hudRestoreButton.OnMouseDown += (_, _) =>
            {
                _sfxPlayer.PlaySfx(SoundEffectID.ButtonClick);
                SetHudChromeMinimized(false);
            };

            return _hudRestoreButton;
        }

        private void SetHudChromeMinimized(bool minimized)
        {
            _hudChromeMinimized = minimized;

            foreach (var (_, button) in _stateButtons)
                button.Visible = !minimized;

            if (_hudMinimizeButton != null)
                _hudMinimizeButton.Visible = !minimized && _clientWindowSizeRepository.Resizable;

            if (_hudRestoreButton != null)
            {
                _hudRestoreButton.Visible = minimized;
                if (minimized)
                    _hudRestoreButton.DrawPosition = GetHudRestoreButtonPosition();
            }

            if (minimized)
            {
                _lastVisibleDockPanel = _dockPanels.FirstOrDefault(p => p.Visible);
                foreach (var panel in _dockPanels)
                    panel.Visible = false;
            }
            else
            {
                ApplyDockPositionToChrome();
                var panelToShow = _lastVisibleDockPanel ?? _dockPanels.FirstOrDefault();
                if (panelToShow != null)
                {
                    foreach (var panel in _dockPanels)
                        panel.Visible = false;
                    panelToShow.Visible = true;
                }
            }
        }

        private IGameComponent CreateSessionExpButton()
        {
            var btn = new XNAButton(
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 58, transparent: true),
                new Vector2(55, 0),
                new Rectangle(331, 30, 22, 14),
                new Rectangle(331, 30, 22, 14))
            {
                DrawOrder = HUD_CONTROL_LAYER
            };
            btn.OnMouseDown += (_, _) => _hudButtonController.ClickSessionExp();
            btn.OnMouseDown += (_, _) => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return btn;
        }

        private IGameComponent CreateQuestButton()
        {
            var btn = new XNAButton(
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 58, transparent: true),
                new Vector2(77, 0),
                new Rectangle(353, 30, 22, 14),
                new Rectangle(353, 30, 22, 14))
            {
                DrawOrder = HUD_CONTROL_LAYER
            };
            btn.OnMouseDown += (_, _) => _hudButtonController.ClickQuestStatus();
            btn.OnMouseDown += (_, _) => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return btn;
        }

        private IGameComponent CreateHPStatusBar()
        {
            var statusBar = new HPStatusBar(_nativeGraphicsManager, _clientWindowSizeRepository, (ICharacterProvider)_characterRepository) { DrawOrder = HUD_CONTROL_LAYER };
            statusBar.StatusBarClicked += () => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return statusBar;
        }

        private IGameComponent CreateTPStatusBar()
        {
            var statusBar = new TPStatusBar(_nativeGraphicsManager, _clientWindowSizeRepository, (ICharacterProvider)_characterRepository) { DrawOrder = HUD_CONTROL_LAYER };
            statusBar.StatusBarClicked += () => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return statusBar;
        }

        private IGameComponent CreateSPStatusBar()
        {
            var statusBar = new SPStatusBar(_nativeGraphicsManager, _clientWindowSizeRepository, (ICharacterProvider)_characterRepository) { DrawOrder = HUD_CONTROL_LAYER };
            statusBar.StatusBarClicked += () => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return statusBar;
        }

        private IGameComponent CreateBossHealthBar()
        {
            return new BossHealthBar(
                _graphicsDeviceProvider.GraphicsDevice,
                (ICharacterProvider)_characterRepository,
                (ICurrentMapStateProvider)_currentMapStateRepository,
                _enfFileProvider,
                _bossHealthProvider,
                _clientWindowSizeRepository)
            {
                DrawOrder = HUD_CONTROL_LAYER + 10
            };
        }

        private IGameComponent CreateTNLStatusBar()
        {
            var statusBar = new TNLStatusBar(_nativeGraphicsManager, _clientWindowSizeRepository, (ICharacterProvider)_characterRepository, _experienceTableProvider) { DrawOrder = HUD_CONTROL_LAYER };
            statusBar.StatusBarClicked += () => _sfxPlayer.PlaySfx(SoundEffectID.HudStatusBarClick);
            return statusBar;
        }

        private ChatModePictureBox CreateChatModePictureBox()
        {
            return new ChatModePictureBox(_nativeGraphicsManager, _clientWindowSizeRepository, _chatModeCalculator, _hudControlProvider)
            {
                // Above chat bar chrome so the sword is never covered by a stray bar segment.
                DrawOrder = HUD_CONTROL_LAYER + 25
            };
        }

        private ChatTextBox CreateChatTextBox()
        {
            var chatTextBox = new ChatTextBox(_nativeGraphicsManager, _clientWindowSizeRepository, _contentProvider)
            {
                Text = "",
                Selected = true,
                Visible = true,
                DrawOrder = HUD_CONTROL_LAYER,
            };
            chatTextBox.OnEnterPressed += (_, _) => _chatController.SendChatAndClearTextBox();
            chatTextBox.OnClicked += (_, _) => _chatController.SelectChatTextBox();
            chatTextBox.OnTextChanged += (_, _) => _chatController.ClearAndWarnIfJailAndGlobal();

            return chatTextBox;
        }

        private TimeLabel CreateClockLabel()
        {
            return new TimeLabel(_clientWindowSizeRepository) { DrawOrder = HUD_CONTROL_LAYER + 1 };
        }

        private PeriodicStatUpdaterComponent CreatePeriodicStatUpdater()
        {
            return new PeriodicStatUpdaterComponent(_endlessGameProvider, _characterRepository);
        }

        private UnknownEntitiesRequester CreateUnknownEntitiesRequester()
        {
            return new UnknownEntitiesRequester(_endlessGameProvider, _clientWindowSizeRepository, (ICharacterProvider)_characterRepository, _currentMapStateRepository,
                _npcRendererProvider, _characterRendererProvider, _unknownEntitiesRequestActions);
        }

        private StatusBarLabel CreateStatusLabel()
        {
            return new StatusBarLabel(_nativeGraphicsManager, _clientWindowSizeRepository, _statusLabelTextProvider)
            {
                Visible = true,
                DrawOrder = HUD_CONTROL_LAYER,
            };
        }

        private CurrentUserInputTracker CreateCurrentUserInputTracker()
        {
            return new CurrentUserInputTracker(_endlessGameProvider, _userInputRepository);
        }

        private IUserInputHandler CreateUserInputHandler()
        {
            return _userInputHandlerFactory.CreateUserInputHandler();
        }

        private ICharacterAnimator CreateCharacterAnimator()
        {
            return new CharacterAnimator(
                _endlessGameProvider, _characterRepository, _playerInfoProvider, _currentMapStateRepository,
                _currentMapProvider, _spellSlotDataRepository, _characterActions,
                _walkValidationActions, _pathFinder, _fixedTimeStepRepository,
                _weaponMetadataProvider);
        }

        private INPCAnimator CreateNPCAnimator()
        {
            return new NPCAnimator(_endlessGameProvider, _currentMapStateRepository, _fixedTimeStepRepository);
        }

        private IPeriodicEmoteHandler CreatePeriodicEmoteHandler(ICharacterAnimator characterAnimator)
        {
            return new PeriodicEmoteHandler(
                _endlessGameProvider, _characterActions, _chatBubbleActions,
                _userInputTimeProvider, _characterRepository, characterAnimator,
                _statusLabelSetter, _mainButtonController, _localizedStringFinder,
                _sfxPlayer);
        }

        private PreviousUserInputTracker CreatePreviousUserInputTracker()
        {
            return new PreviousUserInputTracker(_endlessGameProvider, _userInputRepository);
        }
    }
}
