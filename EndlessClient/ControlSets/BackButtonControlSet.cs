using System;
using EndlessClient.Content;
using EndlessClient.Controllers;
using EndlessClient.Rendering;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.ControlSets
{
    public abstract class BackButtonControlSet : BaseControlSet
    {
        protected readonly IMainButtonController _mainButtonController;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;
        private Texture2D _backButtonTexture;

        private XNAButton _backButton;

        protected BackButtonControlSet(IMainButtonController mainButtonController,
                                       IClientWindowSizeRepository clientWindowSizeRepository)
        {
            _mainButtonController = mainButtonController;
            _clientWindowSizeRepository = clientWindowSizeRepository;
        }

        public override void InitializeResources(INativeGraphicsManager gfxManager, IContentProvider contentProvider)
        {
            base.InitializeResources(gfxManager, contentProvider);

            _backButtonTexture = gfxManager.TextureFromResource(GFXTypes.PreLoginUI, 24, true);
        }

        protected override void InitializeControlsHelper(IControlSet currentControlSet)
        {
            _backButton = GetControl(currentControlSet, GameControlIdentifier.BackButton, GetBackButton);

            _allComponents.Add(_backButton);
        }

        private XNAButton GetBackButton()
        {
            var halfH = _backButtonTexture.Height / 2;
            var button = new XNAButton(
                _backButtonTexture,
                new Vector2(_clientWindowSizeRepository.Width - _backButtonTexture.Width, 0),
                new Rectangle(0, 0, _backButtonTexture.Width, halfH),
                new Rectangle(0, halfH, _backButtonTexture.Width, halfH))
            {
                UpdateOrder = -1,
                DrawOrder = 100,
                // Full glyph is clickable (nested window-size chrome was removed).
                ClickArea = new Rectangle(0, 0, _backButtonTexture.Width, halfH)
            };
            button.OnMouseDown += DoBackButtonClick;

            _clientWindowSizeRepository.GameWindowSizeChanged += (o, e) =>
            {
                button.DrawPosition = new Vector2(_clientWindowSizeRepository.Width - _backButtonTexture.Width, 0);
            };

            return button;
        }

        protected virtual void DoBackButtonClick(object sender, EventArgs e)
        {
            _mainButtonController.GoToInitialState();
        }
    }
}
