using System;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Graphics;
using Microsoft.Xna.Framework;

namespace EndlessClient.HUD.StatusBars
{
    public class HPStatusBar : StatusBarBase
    {
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISkillTreeProvider _skillTreeProvider;

        protected override int StatusBarIndex => -2;

        public HPStatusBar(INativeGraphicsManager nativeGraphicsManager,
                           IClientWindowSizeProvider clientWindowSizeProvider,
                           ICharacterProvider characterProvider,
                           ISkillTreeActions skillTreeActions,
                           ISkillTreeProvider skillTreeProvider)
            : base(nativeGraphicsManager, clientWindowSizeProvider, characterProvider)
        {
            _skillTreeActions = skillTreeActions;
            _skillTreeProvider = skillTreeProvider;
            ChangeStatusBarPosition();
        }

        protected override void UpdateLabelText()
        {
            SetLabelText($"{Stats[CharacterStat.HP]}/{EffectiveMaxHp}");
        }

        protected override void DrawStatusBar()
        {
            //todo: figure out these magic numbers
            var srcWidth = 25 + (int)Math.Round(Stats[CharacterStat.HP] / (double)EffectiveMaxHp * 79);
            var maskSrc = new Rectangle(_sourceRectangleArea.X, _sourceRectangleArea.Height, srcWidth, _sourceRectangleArea.Height);
            DrawScaledStatusBar(_sourceRectangleArea, maskSrc);
        }

        private int EffectiveMaxHp
        {
            get
            {
                try
                {
                    _skillTreeActions.EnsureLoaded();
                    var baseMax = Stats[CharacterStat.MaxHP];
                    var bonus = (int)Math.Round(baseMax * _skillTreeProvider.GetBonuses().MaxHpPercent / 100.0);
                    return Math.Max(1, baseMax + bonus);
                }
                catch
                {
                    return Math.Max(1, Stats[CharacterStat.MaxHP]);
                }
            }
        }
    }
}
