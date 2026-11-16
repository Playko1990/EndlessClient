using System;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using XNAControls;

namespace EndlessClient.HUD.SkillTree
{
    public class SkillTreePanelMember : XNAControl
    {
        private readonly XNALabel _nameLabel;
        private readonly XNALabel _rankLabel;
        private readonly XNALabel _bonusLabel;
        private readonly XNAButton _trainButton;

        private int _displayIndex;

        public SkillTreeStat Stat { get; }

        public int DisplayIndex
        {
            get => _displayIndex;
            set
            {
                _displayIndex = value;
                DrawArea = new Rectangle(8, 22 + _displayIndex * 16, 450, 15);
            }
        }

        public event EventHandler TrainClicked;

        public SkillTreePanelMember(INativeGraphicsManager nativeGraphicsManager, SkillTreeStat stat)
        {
            Stat = stat;

            _nameLabel = new XNALabel(Constants.FontSize08pt5)
            {
                AutoSize = false,
                DrawArea = new Rectangle(0, 0, 110, 13),
                ForeColor = ColorConstants.LightGrayText,
                Text = GetDisplayName(stat)
            };
            _nameLabel.SetParentControl(this);

            _rankLabel = new XNALabel(Constants.FontSize08pt5)
            {
                AutoSize = false,
                DrawArea = new Rectangle(115, 0, 50, 13),
                ForeColor = ColorConstants.LightGrayText,
                Text = "Lv 0"
            };
            _rankLabel.SetParentControl(this);

            _bonusLabel = new XNALabel(Constants.FontSize08pt5)
            {
                AutoSize = false,
                DrawArea = new Rectangle(170, 0, 230, 13),
                ForeColor = ColorConstants.LightGrayText,
                Text = string.Empty
            };
            _bonusLabel.SetParentControl(this);

            var buttonTexture = nativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 27, true);
            _trainButton = new XNAButton(
                buttonTexture,
                new Vector2(410, 0),
                new Rectangle(215, 386, 19, 15),
                new Rectangle(234, 386, 19, 15))
            {
                FlashSpeed = 500,
                Visible = false
            };
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
        }

        public static string GetDisplayName(SkillTreeStat stat) => stat switch
        {
            SkillTreeStat.CritChance => "Crit Chance",
            SkillTreeStat.CritDamage => "Crit Damage",
            SkillTreeStat.MoveSpeed => "Move Speed",
            SkillTreeStat.AttackSpeed => "Attack Speed",
            SkillTreeStat.GoldDrop => "Gold Drop",
            SkillTreeStat.ExpGain => "Exp Gain",
            SkillTreeStat.Luck => "Luck",
            SkillTreeStat.MaxHP => "Max HP",
            SkillTreeStat.HeavyDamage => "Heavy Damage",
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
            SkillTreeStat.CritChance => $"+{SkillTreeFormulas.CritChancePercentPerRank:0}% / pt",
            SkillTreeStat.CritDamage => $"+{SkillTreeFormulas.CritDamagePercentPerRank:0}% / pt",
            SkillTreeStat.MoveSpeed => $"+{SkillTreeFormulas.MoveSpeedPercentPerRank:0}% / pt",
            SkillTreeStat.AttackSpeed => $"+{SkillTreeFormulas.AttackSpeedPercentPerRank:0}% / pt",
            SkillTreeStat.GoldDrop => $"+{SkillTreeFormulas.GoldDropPercentPerRank:0}% / pt",
            SkillTreeStat.ExpGain => $"+{SkillTreeFormulas.ExpGainPercentPerRank:0}% / pt",
            SkillTreeStat.Luck => $"+{SkillTreeFormulas.LuckPerRank} / pt",
            SkillTreeStat.MaxHP => $"+{SkillTreeFormulas.MaxHpPerRank} / pt",
            SkillTreeStat.HeavyDamage => $"+{SkillTreeFormulas.HeavyDamagePercentPerRank:0}% / pt",
            _ => string.Empty
        };
    }
}
