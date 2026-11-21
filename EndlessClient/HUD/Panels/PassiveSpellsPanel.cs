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
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class PassiveSpellsPanel : DraggableHudPanel
    {
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly ITrainingController _trainingController;
        private readonly IEOMessageBoxFactory _messageBoxFactory;
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISkillTreeProvider _skillTreeProvider;

        private readonly IXNALabel _titleLabel;
        private readonly IXNALabel _availablePointsLabel;
        private readonly List<SkillTreePanelMember> _members;

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

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 62);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, clientWindowSizeProvider.Resizable);

            var resizable = clientWindowSizeProvider.Resizable;

            _titleLabel = new XNALabel(Constants.FontSize08pt5)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(10, 4, 200, 13), resizable),
                ForeColor = ColorConstants.LightGrayText,
                Text = "Skill Tree"
            };
            _titleLabel.SetParentControl(this);

            _availablePointsLabel = new XNALabel(Constants.FontSize08pt5)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(300, 4, 150, 13), resizable),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleRight,
                Text = "sk.pts: 0"
            };
            _availablePointsLabel.SetParentControl(this);

            // One skill per grid box, filling left-to-right / top-to-bottom from slot 0.
            _members = Enum.GetValues(typeof(SkillTreeStat))
                .Cast<SkillTreeStat>()
                .Select((stat, index) =>
                {
                    var member = new SkillTreePanelMember(_nativeGraphicsManager, stat, index, resizable);
                    member.TrainClicked += HandleTrainClicked;
                    member.SetParentControl(this);
                    return member;
                })
                .ToList();

            _skillTreeActions.EnsureLoaded();
        }

        public override void Initialize()
        {
            _titleLabel.Initialize();
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _skillTreeActions.Save();

            base.Dispose(disposing);
        }

        private void RefreshDisplays(bool force)
        {
            var available = _skillTreeActions.GetAvailablePoints();
            var spent = _skillTreeProvider.TotalSpent;
            if (!force && available == _cachedAvailablePoints && spent == _cachedTotalSpent)
                return;

            _cachedAvailablePoints = available;
            _cachedTotalSpent = spent;
            _availablePointsLabel.Text = $"sk.pts: {available}";

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
