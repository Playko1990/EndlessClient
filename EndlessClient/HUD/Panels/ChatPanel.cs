using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.Audio;
using EndlessClient.ControlSets;
using EndlessClient.HUD.Chat;
using EndlessClient.Rendering;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Sprites;
using EndlessClient.UIControls;
using EndlessClient.HUD;
using EOLib.Domain.Chat;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class ChatPanel : DraggableHudPanel
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IChatActions _chatActions;
        private readonly IChatRenderableGenerator _chatRenderableGenerator;
        private readonly IChatProvider _chatProvider;
        private readonly IHudControlProvider _hudControlProvider;

        private readonly ScrollBar _scrollBar;
        private readonly XNAButton _panelCycleUp, _panelCycleDown;
        private readonly Dictionary<ChatTab, ChatPanelTab> _tabs;

        public bool ResizableHud { get; }

        public ChatTab CurrentTab => _tabs.Single(x => x.Value.Active).Key;

        public ChatPanel(INativeGraphicsManager nativeGraphicsManager,
                         IChatActions chatActions,
                         IChatRenderableGenerator chatRenderableGenerator,
                         IChatProvider chatProvider,
                         IHudControlProvider hudControlProvider,
                         BitmapFont chatFont,
                         IHudStateActions hudStateActions,
                         ISfxPlayer sfxPlayer,
                         IClientWindowSizeProvider clientWindowSizeProvider)
            : base(clientWindowSizeProvider.Resizable)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _chatActions = chatActions;
            _chatRenderableGenerator = chatRenderableGenerator;
            _chatProvider = chatProvider;
            _hudControlProvider = hudControlProvider;
            ResizableHud = clientWindowSizeProvider.Resizable;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 28);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, ResizableHud);

            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, ResizableHud);
            var scrollPos = HudUiMetrics.ScalePoint(467, 2, uiScale);
            var scrollSize = HudUiMetrics.ScalePoint(16, 97, uiScale);
            (_panelCycleUp, _panelCycleDown) = HudPanelCycleArrows.Create(
                _nativeGraphicsManager, scrollPos, scrollSize, uiScale,
                InGameStates.Chat, hudStateActions, sfxPlayer);

            var (innerPos, innerSize) = HudPanelCycleArrows.InsetScrollBarArea(scrollPos, scrollSize, uiScale);
            _scrollBar = new ScrollBar(innerPos, innerSize, ScrollBarColors.LightOnMed, _nativeGraphicsManager, uiScale)
            {
                LinesToRender = HudUiMetrics.ChatLinesToRender(ResizableHud),
                Visible = true
            };
            _scrollBar.SetParentControl(this);
            SetScrollWheelHandler(_scrollBar);

            var tabTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 35);
            var smallSelected = new SpriteSheet(tabTexture, new Rectangle(307, 16, 43, 16));
            var smallUnselected = new SpriteSheet(tabTexture, new Rectangle(264, 16, 43, 16));
            var largeSelected = new SpriteSheet(tabTexture, new Rectangle(132, 16, 132, 16));
            var largeUnselected = new SpriteSheet(tabTexture, new Rectangle(0, 16, 132, 16));

            _tabs = new Dictionary<ChatTab, ChatPanelTab>
            {
                {ChatTab.Local, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.Local, smallSelected, smallUnselected, chatFont, ResizableHud) { Visible = true, Active = true } },
                {ChatTab.Global, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.Global, smallSelected, smallUnselected, chatFont, ResizableHud) { Visible = true, } },
                {ChatTab.Group, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.Group, smallSelected, smallUnselected, chatFont, ResizableHud) { Visible = true, } },
                {ChatTab.System, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.System, smallSelected, smallUnselected, chatFont, ResizableHud) { Visible = true, } },
                {ChatTab.Private1, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.Private1, largeSelected, largeUnselected, chatFont, ResizableHud) {Visible = false} },
                {ChatTab.Private2, new ChatPanelTab(_chatProvider, _hudControlProvider, _chatRenderableGenerator, this, _scrollBar, ChatTab.Private2, largeSelected, largeUnselected, chatFont, ResizableHud) {Visible = false } },
            };

            foreach (var tab in _tabs.Values)
            {
                tab.SetParentControl(this);
                tab.OnClosed += (s, _) => _chatActions.ClosePMTab(((ChatPanelTab)s).Tab);
            }
        }

        public override void Initialize()
        {
            _panelCycleUp.Initialize();
            _panelCycleUp.SetParentControl(this);
            _panelCycleDown.Initialize();
            _panelCycleDown.SetParentControl(this);

            _scrollBar.Initialize();

            foreach (var tab in _tabs.Values)
                tab.Initialize();

            base.Initialize();
        }

        public void TryStartNewPrivateChat(string targetCharacter)
        {
            if (_tabs[ChatTab.Private1].Visible && _tabs[ChatTab.Private2].Visible)
                return;

            if (!string.Equals(_chatProvider.PMTarget1, targetCharacter, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_chatProvider.PMTarget2, targetCharacter, StringComparison.OrdinalIgnoreCase))
            {
                if (_tabs[ChatTab.Private1].Visible)
                {
                    SelectTab(ChatTab.Private2);
                    _tabs[ChatTab.Private2].Text = char.ToUpper(targetCharacter[0]) + targetCharacter[1..];
                }
                else
                {
                    SelectTab(ChatTab.Private1);
                    _tabs[ChatTab.Private1].Text = char.ToUpper(targetCharacter[0]) + targetCharacter[1..];
                }
            }
        }

        public void SelectTab(ChatTab clickedTab)
        {
            if (CurrentTab == ChatTab.Global && clickedTab != ChatTab.Global)
            {
                _chatActions.SetGlobalActive(false);
            }
            else if (CurrentTab != ChatTab.Global && clickedTab == ChatTab.Global)
            {
                _chatActions.SetGlobalActive(true);
            }

            _tabs[CurrentTab].Active = false;

            _tabs[clickedTab].Visible = true;
            _tabs[clickedTab].Active = true;

            _scrollBar.UpdateDimensions(_chatProvider.AllChat[clickedTab].Count);
        }

        public void ClosePMTab(ChatTab whichTab)
        {
            _tabs[whichTab].CloseTab();
        }

        protected override bool HandleMouseDown(IXNAControl control, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button == MouseButton.Right)
            {
                _tabs[CurrentTab].HandleRightClick(eventArgs);
            }

            return base.HandleMouseDown(control, eventArgs);
        }
    }
}
