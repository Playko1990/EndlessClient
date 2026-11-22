using System;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Graphics;
using Microsoft.Xna.Framework;

namespace EndlessClient.HUD.StatusBars
{
    public class TPStatusBar : StatusBarBase
    {
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly ISkillTreeProvider _skillTreeProvider;

        protected override int StatusBarIndex => -1;

        public TPStatusBar(INativeGraphicsManager nativeGraphicsManager,
                           IClientWindowSizeProvider clientWindowSizeProvider,
                           ICharacterProvider characterProvider,
                           ISkillTreeActions skillTreeActions,
                           ISkillTreeProvider skillTreeProvider)
            : base(nativeGraphicsManager, clientWindowSizeProvider, characterProvider)
        {
            _skillTreeActions = skillTreeActions;
            _skillTreeProvider = skillTreeProvider;
            _sourceRectangleArea.Offset(_sourceRectangleArea.Width, 0);
            ChangeStatusBarPosition();
        }

        protected override void UpdateLabelText()
        {
            SetLabelText($"{Stats[CharacterStat.TP]}/{EffectiveMaxTp}");
        }

        protected override void DrawStatusBar()
        {
            //todo: figure out these magic numbers
            var srcWidth = 25 + (int)Math.Round(Stats[CharacterStat.TP] / (double)EffectiveMaxTp * 79);
            var maskSrc = new Rectangle(_sourceRectangleArea.X, _sourceRectangleArea.Height, srcWidth, _sourceRectangleArea.Height);
            DrawScaledStatusBar(_sourceRectangleArea, maskSrc);
        }

        private int EffectiveMaxTp
        {
            get
            {
                try
                {
                    _skillTreeActions.EnsureLoaded();
                    var baseMax = Stats[CharacterStat.MaxTP];
                    var bonus = (int)Math.Round(baseMax * _skillTreeProvider.GetBonuses().MaxTpPercent / 100.0);
                    return Math.Max(1, baseMax + bonus);
                }
                catch
                {
                    return Math.Max(1, Stats[CharacterStat.MaxTP]);
                }
            }
        }
    }
}
