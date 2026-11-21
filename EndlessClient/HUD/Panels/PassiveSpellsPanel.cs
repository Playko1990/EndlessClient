using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.Audio;
using EndlessClient.Controllers;
using EndlessClient.Dialogs;
using EndlessClient.Dialogs.Factories;
using EndlessClient.HUD;
using EndlessClient.HUD.SkillTree;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class PassiveSpellsPanel : DraggableHudPanel
    {
        // Native coords for the left-rail header well (above the name at y=50).
        // Larger than a normal selected-spell glyph (~36px) so the tree fills the box.
        private const int HeaderIconNativeSize = 42;
        private const int HeaderIconNativeX = 29;
        private const int HeaderIconNativeY = 6;

        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly ITrainingController _trainingController;
        private readonly IEOMessageBoxFactory _messageBoxFactory;
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISkillTreeProvider _skillTreeProvider;
        private readonly bool _resizable;

        private readonly IXNALabel _headerNameLabel;
        private readonly IXNALabel _availablePointsLabel;
        private readonly List<SkillTreePanelMember> _members;
        private readonly Texture2D _headerIcon;
        private readonly XNAButton _panelCycleUp, _panelCycleDown;

        private int _cachedAvailablePoints = int.MinValue;
        private int _cachedTotalSpent = int.MinValue;
        private bool _confirmedTraining;

        public PassiveSpellsPanel(INativeGraphicsManager nativeGraphicsManager,
                                  ITrainingController trainingController,
                                  IEOMessageBoxFactory messageBoxFactory,
                                  ISkillTreeActions skillTreeActions,
                                  ISkillTreeProvider skillTreeProvider,
                                  IHudStateActions hudStateActions,
                                  ISfxPlayer sfxPlayer,
                                  IClientWindowSizeProvider clientWindowSizeProvider)
            : base(clientWindowSizeProvider.Resizable)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _trainingController = trainingController;
            _messageBoxFactory = messageBoxFactory;
            _skillTreeActions = skillTreeActions;
            _skillTreeProvider = skillTreeProvider;
            _resizable = clientWindowSizeProvider.Resizable;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 62);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, _resizable);

            var scrollPos = HudUiMetrics.ScalePoint(467, 2, _resizable);
            var scrollSize = HudUiMetrics.ScalePoint(16, 115, _resizable);
            (_panelCycleUp, _panelCycleDown) = HudPanelCycleArrows.Create(
                _nativeGraphicsManager, scrollPos, scrollSize, _resizable,
                InGameStates.PassiveSpells, hudStateActions, sfxPlayer);

            var font = HudUiMetrics.PanelLabelFont(_resizable);

            // Title + enlarged tree symbol live in the left-rail header box (not the panel corners).
            _headerNameLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(9, 50, 81, 13), _resizable),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = "Skill Tree",
                Visible = true
            };
            _headerNameLabel.SetParentControl(this);

            // Same left-rail "skp" slot as Active Spells (panel art already prints the label).
            _availablePointsLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(32, 96, 42, 15), _resizable),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleLeft,
                Text = "0"
            };
            _availablePointsLabel.SetParentControl(this);

            _headerIcon = SkillTreeHudIcon.LoadTreeTexture(Game.GraphicsDevice);

            // One skill per grid box, filling left-to-right / top-to-bottom from slot 0.
            _members = Enum.GetValues(typeof(SkillTreeStat))
                .Cast<SkillTreeStat>()
                .Select((stat, index) =>
                {
                    var member = new SkillTreePanelMember(_nativeGraphicsManager, stat, index, _resizable);
                    member.TrainClicked += HandleTrainClicked;
                    member.SetParentControl(this);
                    return member;
                })
                .ToList();

            _skillTreeActions.EnsureLoaded();
        }

        public override void Initialize()
        {
            _headerNameLabel.Initialize();
            _availablePointsLabel.Initialize();

            _panelCycleUp.Initialize();
            _panelCycleUp.SetParentControl(this);
            _panelCycleDown.Initialize();
            _panelCycleDown.SetParentControl(this);

            foreach (var member in _members)
                member.Initialize();

            RefreshDisplays(force: true);

            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            _skillTreeActions.EnsureLoaded();

            var available = _skillTreeActions.GetAvailablePoints();
            var spent = _skillTreeProvider.TotalSpent;
            if (available != _cachedAvailablePoints || spent != _cachedTotalSpent)
                RefreshDisplays(force: false);

            if (available <= 0)
                _confirmedTraining = false;

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            _spriteBatch.Begin();
            DrawHeaderIcon();
            _spriteBatch.End();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _skillTreeActions.Save();
                _headerIcon?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void DrawHeaderIcon()
        {
            if (_headerIcon == null)
                return;

            // Larger than a normal selected-spell glyph so it fills the left header well.
            var size = HudUiMetrics.Scale(HeaderIconNativeSize, _resizable);
            var x = DrawAreaWithParentOffset.X + HudUiMetrics.Scale(HeaderIconNativeX, _resizable);
            var y = DrawAreaWithParentOffset.Y + HudUiMetrics.Scale(HeaderIconNativeY, _resizable);
            var dst = new Rectangle(x, y, size, size);

            _spriteBatch.Draw(_headerIcon, dst, Color.White);
        }

        private void RefreshDisplays(bool force)
        {
            var available = _skillTreeActions.GetAvailablePoints();
            var spent = _skillTreeProvider.TotalSpent;
            if (!force && available == _cachedAvailablePoints && spent == _cachedTotalSpent)
                return;

            _cachedAvailablePoints = available;
            _cachedTotalSpent = spent;
            _availablePointsLabel.Text = available.ToString();

            foreach (var member in _members)
            {
                var rank = _skillTreeProvider.GetRank(member.Stat);
                member.UpdateDisplay(rank, SkillTreePanelMember.FormatBonus(member.Stat, rank), available > 0);
            }
        }

        private void HandleTrainClicked(object sender, EventArgs e)
        {
            if (!(sender is SkillTreePanelMember member))
                return;

            if (!_confirmedTraining)
            {
                var dialog = _messageBoxFactory.CreateMessageBox(
                    "Do you want to train?",
                    "Skill tree training",
                    EODialogButtons.OkCancel);

                dialog.DialogClosing += (_, args) =>
                {
                    if (args.Result == XNADialogResult.OK)
                        _confirmedTraining = true;
                };
                dialog.ShowDialog();
                return;
            }

            _trainingController.AddSkillTreePoint(member.Stat);
            RefreshDisplays(force: true);
        }
    }
}
