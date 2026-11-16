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
            DrawArea = new Rectangle(100, 0, _sourceRectangleArea.Width, _sourceRectangleArea.Height);
            ChangeStatusBarPosition();
        }

        protected override void UpdateLabelText()
        {
            _label.Text = $"{Stats[CharacterStat.HP]}/{EffectiveMaxHp}";
        }

        protected override void DrawStatusBar()
        {
            //todo: figure out these magic numbers
            var srcWidth = 25 + (int)Math.Round(Stats[CharacterStat.HP] / (double)EffectiveMaxHp * 79);
            var maskSrc = new Rectangle(_sourceRectangleArea.X, _sourceRectangleArea.Height, srcWidth, _sourceRectangleArea.Height);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_texture, DrawPositionWithParentOffset, _sourceRectangleArea, Color.White);
            _spriteBatch.Draw(_texture, DrawPositionWithParentOffset, maskSrc, Color.White);
            _spriteBatch.End();
        }

        private int EffectiveMaxHp
        {
            get
            {
                _skillTreeActions.EnsureLoaded();
                return Math.Max(1, Stats[CharacterStat.MaxHP] + _skillTreeProvider.GetBonuses().MaxHp);
            }
        }
    }
}
