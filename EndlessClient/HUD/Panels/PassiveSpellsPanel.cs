using System;
using System.Collections.Generic;
using System.Linq;
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
        // Left-rail header well on PostLoginUI #62: icon sits above the name row (~y=50),
        // with lvl/skp slots below. Keep the tree square and centered in that well.
        private const int HeaderWellNativeX = 9;
        private const int HeaderWellNativeY = 4;
        private const int HeaderWellNativeWidth = 81;
        private const int HeaderWellNativeHeight = 44;
        private const int HeaderTitleNativeY = 50;
        private const int HeaderTitleNativeHeight = 13;

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

        private int _cachedAvailablePoints = int.MinValue;
        private int _cachedTotalSpent = int.MinValue;
        private bool _confirmedTraining;

        public PassiveSpellsPanel(INativeGraphicsManager nativeGraphicsManager,
                                  ITrainingController trainingController,
                                  IEOMessageBoxFactory messageBoxFactory,
                                  ISkillTreeActions skillTreeActions,
                                  ISkillTreeProvider skillTreeProvider,
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

            var font = HudUiMetrics.PanelLabelFont(_resizable);

            // Title sits under the tree inside the left-rail header box (not panel corners).
            _headerNameLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(
                    new Rectangle(HeaderWellNativeX, HeaderTitleNativeY, HeaderWellNativeWidth, HeaderTitleNativeHeight),
                    _resizable),
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

            // Center a nearly well-filling square icon above the title row.
            var well = HudUiMetrics.ScaleRectangle(
                new Rectangle(HeaderWellNativeX, HeaderWellNativeY, HeaderWellNativeWidth, HeaderWellNativeHeight),
                _resizable);
            var pad = HudUiMetrics.Scale(2, _resizable);
            var size = Math.Max(1, Math.Min(well.Width, well.Height) - pad * 2);
            var x = DrawAreaWithParentOffset.X + well.X + (well.Width - size) / 2;
            var y = DrawAreaWithParentOffset.Y + well.Y + (well.Height - size) / 2;
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
