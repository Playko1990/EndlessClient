using System;
using EndlessClient.HUD;
using EndlessClient.UIControls;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private static readonly Color GaugeEmpty = new Color(40, 28, 16, 180);
        private static readonly Color GaugeFill = new Color(70, 150, 55, 230);
        private static readonly Color GaugeMaxed = new Color(210, 170, 40, 240);

        private readonly bool _resizable;
        private readonly XNALabel _nameLabel;
        private readonly XNALabel _rankLabel;
        private readonly XNALabel _bonusLabel;
        private readonly ClickableArea _clickArea;
        private readonly Texture2D _pixel;

        private int _rank;
        private bool _canTrain;
        private bool _isMaxed;

        public SkillTreeStat Stat { get; }

        public event EventHandler TrainClicked;

        public SkillTreePanelMember(INativeGraphicsManager nativeGraphicsManager, SkillTreeStat stat, int gridSlot, bool resizable = false)
        {
            Stat = stat;
            _resizable = resizable;

            PlaceInGridSlot(gridSlot);

            var pad = HudUiMetrics.Scale(2, resizable);
            var innerWidth = Math.Max(8, DrawArea.Width - pad * 2);
            var font = HudUiMetrics.ShouldEnlargeHud(resizable) ? Constants.FontSize10 : Constants.FontSize08;

            _nameLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, pad, innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = GetDisplayName(stat)
            };
            _nameLabel.SetParentControl(this);

            _rankLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, HudUiMetrics.Scale(14, resizable), innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = $"0/{SkillTreeFormulas.MaxRankPerSkill}"
            };
            _rankLabel.SetParentControl(this);

            _bonusLabel = new XNALabel(font)
            {
                AutoSize = false,
                DrawArea = new Rectangle(pad, HudUiMetrics.Scale(26, resizable), innerWidth, HudUiMetrics.Scale(12, resizable)),
                ForeColor = ColorConstants.LightGrayText,
                TextAlign = LabelAlignment.MiddleCenter,
                Text = string.Empty
            };
            _bonusLabel.SetParentControl(this);

            // Full-cell hit target so labels don't block training clicks.
            _clickArea = new ClickableArea(new Rectangle(0, 0, DrawArea.Width, DrawArea.Height));
            _clickArea.OnMouseDown += (_, _) =>
            {
                if (_canTrain)
                    TrainClicked?.Invoke(this, EventArgs.Empty);
            };
            _clickArea.SetParentControl(this);

            _pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _ = nativeGraphicsManager;
        }

        public override void Initialize()
        {
            _nameLabel.Initialize();
            _rankLabel.Initialize();
            _bonusLabel.Initialize();
            _clickArea.Initialize();
            base.Initialize();
        }

        public void UpdateDisplay(int rank, string bonusText, bool canTrain)
        {
            _rank = Math.Max(0, Math.Min(rank, SkillTreeFormulas.MaxRankPerSkill));
            _isMaxed = _rank >= SkillTreeFormulas.MaxRankPerSkill;
            _canTrain = canTrain && !_isMaxed;

            _rankLabel.Text = $"{_rank}/{SkillTreeFormulas.MaxRankPerSkill}";
            if (_isMaxed)
            {
                _bonusLabel.Text = "MAXED";
                _bonusLabel.ForeColor = GaugeMaxed;
                _rankLabel.ForeColor = GaugeMaxed;
            }
            else
            {
                _bonusLabel.Text = bonusText;
                _bonusLabel.ForeColor = ColorConstants.LightGrayText;
                _rankLabel.ForeColor = ColorConstants.LightGrayText;
            }
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            _spriteBatch.Begin();
            DrawGauge();
            _spriteBatch.End();

            base.OnDrawControl(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _pixel?.Dispose();

            base.Dispose(disposing);
        }

        private void DrawGauge()
        {
            var area = GaugeArea();
            var abs = new Rectangle(
                DrawAreaWithParentOffset.X + area.X,
                DrawAreaWithParentOffset.Y + area.Y,
                area.Width,
                area.Height);

            _spriteBatch.Draw(_pixel, abs, GaugeEmpty);

            if (_rank <= 0)
                return;

            var fillWidth = Math.Max(1, (int)Math.Round(abs.Width * (_rank / (double)SkillTreeFormulas.MaxRankPerSkill)));
            var fill = new Rectangle(abs.X, abs.Y, fillWidth, abs.Height);
            _spriteBatch.Draw(_pixel, fill, _isMaxed ? GaugeMaxed : GaugeFill);
        }

        private Rectangle GaugeArea()
        {
            var padX = HudUiMetrics.Scale(3, _resizable);
            var y = HudUiMetrics.Scale(38, _resizable);
            var h = Math.Max(4, HudUiMetrics.Scale(10, _resizable));
            return new Rectangle(padX, y, Math.Max(4, DrawArea.Width - padX * 2), h);
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
            SkillTreeStat.MaxTP => "Max TP",
            SkillTreeStat.SpellAttack => "Spell Atk",
            SkillTreeStat.PetDamage => "Pet Dmg",
            SkillTreeStat.PetHeal => "Pet Heal",
            SkillTreeStat.InstanceKill => "Inst Kill",
            SkillTreeStat.SpellPower => "Spell Dmg",
            SkillTreeStat.SpellHeal => "Spell Heal",
            _ => stat.ToString()
        };

        public static string FormatBonus(SkillTreeStat stat, int rank)
        {
            if (rank <= 0)
                return GetBonusHint(stat);

            return FormatBonusAmount(stat, rank);
        }

        private static string FormatBonusAmount(SkillTreeStat stat, int rank) => stat switch
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
            SkillTreeStat.MaxTP => $"+{rank * SkillTreeFormulas.MaxTpPercentPerRank:0}%",
            SkillTreeStat.SpellAttack => $"+{rank * SkillTreeFormulas.SpellAttackPercentPerRank:0}%",
            SkillTreeStat.PetDamage => $"+{rank * SkillTreeFormulas.PetDamagePerRank}",
            SkillTreeStat.PetHeal => $"+{rank * SkillTreeFormulas.PetHealPerRank}",
            SkillTreeStat.InstanceKill => $"+{rank * SkillTreeFormulas.InstanceKillPercentPerRank:0.#}%",
            SkillTreeStat.SpellPower => $"+{rank * SkillTreeFormulas.SpellPowerPercentPerRank:0}%",
            SkillTreeStat.SpellHeal => $"+{rank * SkillTreeFormulas.SpellHealPerRank}",
            _ => string.Empty
        };

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
            SkillTreeStat.MaxTP => $"+{SkillTreeFormulas.MaxTpPercentPerRank:0}%/pt",
            SkillTreeStat.SpellAttack => $"+{SkillTreeFormulas.SpellAttackPercentPerRank:0}%/pt",
            SkillTreeStat.PetDamage => $"+{SkillTreeFormulas.PetDamagePerRank}/pt",
            SkillTreeStat.PetHeal => $"+{SkillTreeFormulas.PetHealPerRank}/pt",
            SkillTreeStat.InstanceKill => $"+{SkillTreeFormulas.InstanceKillPercentPerRank:0.#}%/pt",
            SkillTreeStat.SpellPower => $"+{SkillTreeFormulas.SpellPowerPercentPerRank:0}%/pt",
            SkillTreeStat.SpellHeal => $"+{SkillTreeFormulas.SpellHealPerRank}/pt",
            _ => string.Empty
        };
    }
}
