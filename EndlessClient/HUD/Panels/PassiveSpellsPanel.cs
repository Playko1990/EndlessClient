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
        // Left-rail header well: title on top, tree below. Slightly smaller so the trunk
        // clears the bottom edge without lifting into the "Skill Tree" title.
        private const int HeaderIconNativeSize = 50;
        private const int HeaderIconNativeX = 24; // centered in the ~81px left rail
        private const int HeaderIconNativeY = 18;

        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly ITrainingController _trainingController;
        private readonly IEOMessageBoxFactory _messageBoxFactory;
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISkillTreeProvider _skillTreeProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly bool _resizable;

        private readonly IXNALabel _headerNameLabel;
        private readonly HudScaledLabel _levelLabel;
        private readonly HudScaledLabel _availablePointsLabel;
        private readonly List<SkillTreePanelMember> _members;
        private readonly Texture2D _headerIcon;

        private int _cachedAvailablePoints = int.MinValue;
        private int _cachedTotalSpent = int.MinValue;
        private int _cachedLevel = int.MinValue;
        private bool _confirmedTraining;

        public PassiveSpellsPanel(INativeGraphicsManager nativeGraphicsManager,
                                  ITrainingController trainingController,
                                  IEOMessageBoxFactory messageBoxFactory,
                                  ISkillTreeActions skillTreeActions,
                                  ISkillTreeProvider skillTreeProvider,
                                  ICharacterProvider characterProvider,
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
            _characterProvider = characterProvider;
            _resizable = clientWindowSizeProvider.Resizable;

            BackgroundImage = _nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 62);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, _resizable);

            var font = HudUiMetrics.PanelLabelFont(_resizable);
            var valueScale = HudUiMetrics.PanelValueTextScale(_resizable);

            // Title sits above the tree in the left-rail header box.
            _headerNameLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(9, 8, 81, 13), _resizable),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = "Skill Tree",
                Visible = true
            };
            _headerNameLabel.SetParentControl(this);

            // Panel art already prints "lvl" — fill the value next to it (same slot as Active Spells).
            _levelLabel = new HudScaledLabel(font, valueScale)
            {
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(32, 78, 42, 15), _resizable),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleLeft,
                Text = "0"
            };
            _levelLabel.SetParentControl(this);

            // Panel art already prints "skp" — fill available skill points.
            _availablePointsLabel = new HudScaledLabel(font, valueScale)
            {
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

            _ = hudStateActions;
            _ = sfxPlayer;
        }

        public override void Initialize()
        {
            _headerNameLabel.Initialize();
            _levelLabel.Initialize();
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
            var level = _characterProvider.MainCharacter.Stats[CharacterStat.Level];
            if (available != _cachedAvailablePoints ||
                spent != _cachedTotalSpent ||
                level != _cachedLevel)
            {
                RefreshDisplays(force: false);
            }

            if (available <= 0)
                _confirmedTraining = false;

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            // Point sampling keeps the small pixel tree crisp when scaled up.
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
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

            // Top anchored at HeaderIconNativeY; larger size reaches the bottom of the well.
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
            var level = _characterProvider.MainCharacter.Stats[CharacterStat.Level];
            if (!force &&
                available == _cachedAvailablePoints &&
                spent == _cachedTotalSpent &&
                level == _cachedLevel)
            {
                return;
            }

            _cachedAvailablePoints = available;
            _cachedTotalSpent = spent;
            _cachedLevel = level;
            _levelLabel.Text = level.ToString();
            _availablePointsLabel.Text = available.ToString();

            foreach (var member in _members)
            {
                var rank = _skillTreeProvider.GetRank(member.Stat);
                var canTrain = available > 0 && rank < SkillTreeFormulas.MaxRankPerSkill;
                member.UpdateDisplay(rank, SkillTreePanelMember.FormatBonus(member.Stat, rank), canTrain);
            }
        }

        private void HandleTrainClicked(object sender, EventArgs e)
        {
            if (!(sender is SkillTreePanelMember member))
                return;

            var rank = _skillTreeProvider.GetRank(member.Stat);
            if (rank >= SkillTreeFormulas.MaxRankPerSkill)
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
