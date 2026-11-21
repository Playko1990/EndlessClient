using System;
using EndlessClient.HUD;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD.SkillTree
{
    public class SkillTreePanelMember : XNAControl
    {
        // Matches Active Spells panel grid on PostLoginUI #62 (8x2 = 16 boxes).
        public const int GridColumns = 8;
        public const int NativeCellOriginX = 101;
        public const int NativeCellOriginY = 9;
        public const int NativeCellWidth = 45;
        public const int NativeCellHeight = 52;

        private readonly bool _resizable;
        private readonly XNALabel _nameLabel;
        private readonly XNALabel _rankLabel;
        private readonly XNALabel _bonusLabel;
        private readonly XNAButton _trainButton;

        public SkillTreeStat Stat { get; }

        public event EventHandler TrainClicked;

        public SkillTreePanelMember(INativeGraphicsManager nativeGraphicsManager, SkillTreeStat stat, int gridSlot, bool resizable = false)
        {
            Stat = stat;
            _resizable = resizable;

            PlaceInGridSlot(gridSlot);

            var pad = HudUiMetrics.Scale(2, resizable);
            var innerWidth = Math.Max(8, DrawArea.Width - pad * 2);

            _nameLabel = new XNALabel(Constants.FontSize08)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, pad, innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = GetDisplayName(stat)
            };
            _nameLabel.SetParentControl(this);

            _rankLabel = new XNALabel(Constants.FontSize08)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, HudUiMetrics.Scale(14, resizable), innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = "Lv 0"
            };
            _rankLabel.SetParentControl(this);

            _bonusLabel = new XNALabel(Constants.FontSize08)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, HudUiMetrics.Scale(26, resizable), innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = string.Empty
            };
            _bonusLabel.SetParentControl(this);

            var buttonTexture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, true);
            var arrowSize = HudUiMetrics.Scale(15, resizable);
            _trainButton = new XNAButton(
                buttonTexture,
                new Vector2((DrawArea.Width - arrowSize) / 2f, HudUiMetrics.Scale(36, resizable)),
                new Rectangle(215, 386, 19, 15),
                new Rectangle(234, 386, 19, 15))
            {
                FlashSpeed = 500,
                Visible = false
            };
            _trainButton.DrawArea = new Rectangle(
                (DrawArea.Width - arrowSize) / 2,
                HudUiMetrics.Scale(36, resizable),
                arrowSize,
                arrowSize);
            _trainButton.ClickArea = new Rectangle(0, 0, arrowSize, arrowSize);
            _trainButton.OnMouseDown += (_, _) => TrainClicked?.Invoke(this, EventArgs.Empty);
            _trainButton.SetParentControl(this);
        }

        public override void Initialize()
        {
            _nameLabel.Initialize();
            _rankLabel.Initialize();
            _bonusLabel.Initialize();
            _trainButton.Initialize();
            base.Initialize();
        }

        public void UpdateDisplay(int rank, string bonusText, bool canTrain)
        {
            _rankLabel.Text = $"Lv {rank}";
            _bonusLabel.Text = bonusText;
            _trainButton.Visible = canTrain;
            _trainButton.FlashSpeed = canTrain ? 500 : null;
        }

        private void PlaceInGridSlot(int slot)
        {
            var col = slot % GridColumns;
            var row = slot / GridColumns;
            var x = HudUiMetrics.Scale(NativeCellOriginX + col * NativeCellWidth, _resizable);
            var y = HudUiMetrics.Scale(NativeCellOriginY + row * NativeCellHeight, _resizable);
            var w = HudUiMetrics.Scale(NativeCellWidth - 2, _resizable);
            var h = HudUiMetrics.Scale(NativeCellHeight - 2, _resizable);
            DrawArea = new Rectangle(x, y, w, h);
        }

        public static string GetDisplayName(SkillTreeStat stat) => stat switch
        {
            SkillTreeStat.CritChance => "Crit Chance",
            SkillTreeStat.CritDamage => "Crit Damage",
            SkillTreeStat.MoveSpeed => "Move Speed",
            SkillTreeStat.AttackSpeed => "Atk Speed",
            SkillTreeStat.GoldDrop => "Gold Drop",
            SkillTreeStat.ExpGain => "Exp Gain",
            SkillTreeStat.Luck => "Luck",
            SkillTreeStat.MaxHP => "Max HP",
            SkillTreeStat.HeavyDamage => "Heavy Dmg",
            _ => stat.ToString()
        };

        public static string FormatBonus(SkillTreeStat stat, int rank)
        {
            if (rank <= 0)
                return GetBonusHint(stat);

            return stat switch
            {
                SkillTreeStat.CritChance => $"+{rank * SkillTreeFormulas.CritChancePercentPerRank:0}%",
                SkillTreeStat.CritDamage => $"+{rank * SkillTreeFormulas.CritDamagePercentPerRank:0}%",
                SkillTreeStat.MoveSpeed => $"+{rank * SkillTreeFormulas.MoveSpeedPercentPerRank:0}%",
                SkillTreeStat.AttackSpeed => $"+{rank * SkillTreeFormulas.AttackSpeedPercentPerRank:0}%",
                SkillTreeStat.GoldDrop => $"+{rank * SkillTreeFormulas.GoldDropPercentPerRank:0}%",
                SkillTreeStat.ExpGain => $"+{rank * SkillTreeFormulas.ExpGainPercentPerRank:0}%",
                SkillTreeStat.Luck => $"+{rank * SkillTreeFormulas.LuckPerRank}",
                SkillTreeStat.MaxHP => $"+{rank * SkillTreeFormulas.MaxHpPerRank}",
                SkillTreeStat.HeavyDamage => $"+{rank * SkillTreeFormulas.HeavyDamagePercentPerRank:0}%",
                _ => string.Empty
            };
        }

        private static string GetBonusHint(SkillTreeStat stat) => stat switch
        {
            SkillTreeStat.CritChance => $"+{SkillTreeFormulas.CritChancePercentPerRank:0}%/pt",
            SkillTreeStat.CritDamage => $"+{SkillTreeFormulas.CritDamagePercentPerRank:0}%/pt",
            SkillTreeStat.MoveSpeed => $"+{SkillTreeFormulas.MoveSpeedPercentPerRank:0}%/pt",
            SkillTreeStat.AttackSpeed => $"+{SkillTreeFormulas.AttackSpeedPercentPerRank:0}%/pt",
            SkillTreeStat.GoldDrop => $"+{SkillTreeFormulas.GoldDropPercentPerRank:0}%/pt",
            SkillTreeStat.ExpGain => $"+{SkillTreeFormulas.ExpGainPercentPerRank:0}%/pt",
            SkillTreeStat.Luck => $"+{SkillTreeFormulas.LuckPerRank}/pt",
            SkillTreeStat.MaxHP => $"+{SkillTreeFormulas.MaxHpPerRank}/pt",
            SkillTreeStat.HeavyDamage => $"+{SkillTreeFormulas.HeavyDamagePercentPerRank:0}%/pt",
            _ => string.Empty
        };
    }
}
