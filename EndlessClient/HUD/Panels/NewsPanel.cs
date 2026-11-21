using System.Collections.Generic;
using System.Linq;
using EndlessClient.Rendering;
using EndlessClient.Rendering.Chat;
using EndlessClient.UIControls;
using EndlessClient.HUD;
using EOLib.Domain.Login;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class NewsPanel : DraggableHudPanel
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IChatRenderableGenerator _chatRenderableGenerator;
        private readonly INewsProvider _newsProvider;
        private readonly ScrollBar _scrollBar;
        private readonly List<IChatRenderable> _chatRenderables;
        private readonly BitmapFont _chatFont;
        private readonly bool _resizable;

        //cached data fields
        private readonly List<string> _cachedNewsStrings;
        private int _cachedScrollOffset;
        private int _cachedLinesToRender;

        private bool _firstTime = true;

        public NewsPanel(INativeGraphicsManager nativeGraphicsManager,
                         IChatRenderableGenerator chatRenderableGenerator,
                         INewsProvider newsProvider,
                         BitmapFont chatFont,
                         IClientWindowSizeProvider clientWindowSizeProvider)
            : base(clientWindowSizeProvider.Resizable)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _chatRenderableGenerator = chatRenderableGenerator;
            _newsProvider = newsProvider;
            _resizable = clientWindowSizeProvider.Resizable;

            _chatRenderables = new List<IChatRenderable>();
            _chatFont = chatFont;

            _cachedNewsStrings = new List<string>();
            _cachedScrollOffset = -1;
            _cachedLinesToRender = -1;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 48);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, _resizable);

            // Vanilla news scroll chrome: abs (569, 350)-(585, 447) with panel at (102, 330)
            // → relative (467, 20) size 16x97. Scale with the enlarged panel so arrows stay
            // in the right-edge gutter instead of floating mid-panel.
            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, _resizable);
            var scrollPos = HudUiMetrics.ScalePoint(467, 20, uiScale);
            var scrollSize = HudUiMetrics.ScalePoint(16, 97, uiScale);
            _scrollBar = new ScrollBar(scrollPos, scrollSize, ScrollBarColors.LightOnMed, _nativeGraphicsManager, uiScale)
            {
                LinesToRender = HudUiMetrics.ChatLinesToRender(_resizable),
                Visible = true
            };
            _scrollBar.SetParentControl(this);
            SetScrollWheelHandler(_scrollBar);
        }

        public override void Initialize()
        {
            _scrollBar.Initialize();
            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            var newsChanged = false;
            if (!_cachedNewsStrings.SequenceEqual(_newsProvider.NewsText))
            {
                UpdateCachedNewsStrings();
                newsChanged = true;
            }

            if (newsChanged ||
                _cachedScrollOffset != _scrollBar.ScrollOffset ||
                _cachedLinesToRender != _scrollBar.LinesToRender)
            {
                var renderables = _chatRenderableGenerator.GenerateNewsRenderables(_cachedNewsStrings);

                UpdateCachedScrollProperties();
                SetupRenderablesFromCachedValues(renderables);

                if (_firstTime && renderables.Count > _scrollBar.LinesToRender)
                {
                    _scrollBar.SetDownArrowFlashSpeed(500);
                    _firstTime = false;
                }
            }

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            if (_chatRenderables == null)
                return;

            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, _resizable);
            foreach (var renderable in _chatRenderables)
                renderable.Render(this, _spriteBatch, _chatFont, uiScale);
        }

        private void UpdateCachedNewsStrings()
        {
            _cachedNewsStrings.Clear();
            _cachedNewsStrings.AddRange(_newsProvider.NewsText);
        }

        private void UpdateCachedScrollProperties()
        {
            _cachedScrollOffset = _scrollBar.ScrollOffset;
            _cachedLinesToRender = _scrollBar.LinesToRender;
        }

        private void SetupRenderablesFromCachedValues(IReadOnlyList<IChatRenderable> renderables)
        {
            _chatRenderables.Clear();

            //only render based on what the scroll bar's position is
            _chatRenderables.AddRange(renderables.Skip(_cachedScrollOffset).Take(_cachedLinesToRender));
            for (int i = 0; i < _chatRenderables.Count; ++i)
                _chatRenderables[i].DisplayIndex = i;

            //update scrollbar with total number of renderables
            _scrollBar.UpdateDimensions(renderables.Count);
        }
    }
}
