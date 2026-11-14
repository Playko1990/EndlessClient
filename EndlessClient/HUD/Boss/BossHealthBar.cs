using System;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Domain.Map;
using EOLib.IO.Repositories;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.HUD.Boss
{
    public class BossHealthBar : XNAControl
    {
        public const int ShowDistanceTiles = 12;

        private const int BarWidth = 420;
        private const int BarHeight = 18;
        private const int TopMargin = 18;

        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapStateProvider _currentMapStateProvider;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly IBossHealthProvider _bossHealthProvider;
        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;

        private readonly Texture2D _pixel;
        private readonly XNALabel _nameLabel;
        private readonly XNALabel _percentLabel;

        private string _activeBossName = string.Empty;
        private int _activePercent = 100;
        private bool _hasActiveBoss;

        public BossHealthBar(GraphicsDevice graphicsDevice,
                             ICharacterProvider characterProvider,
                             ICurrentMapStateProvider currentMapStateProvider,
                             IENFFileProvider enfFileProvider,
                             IBossHealthProvider bossHealthProvider,
                             IClientWindowSizeProvider clientWindowSizeProvider)
        {
            _characterProvider = characterProvider;
            _currentMapStateProvider = currentMapStateProvider;
            _enfFileProvider = enfFileProvider;
            _bossHealthProvider = bossHealthProvider;
            _clientWindowSizeProvider = clientWindowSizeProvider;

            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _nameLabel = new XNALabel(Constants.FontSize09)
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Text = string.Empty,
                Visible = false
            };
            _nameLabel.SetParentControl(this);

            _percentLabel = new XNALabel(Constants.FontSize08)
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Text = string.Empty,
                Visible = false
            };
            _percentLabel.SetParentControl(this);

            Visible = false;
            UpdateDrawArea();

            if (_clientWindowSizeProvider.Resizable)
                _clientWindowSizeProvider.GameWindowSizeChanged += (_, _) => UpdateDrawArea();
        }

        public override void Initialize()
        {
            _nameLabel.Initialize();
            _percentLabel.Initialize();
            base.Initialize();
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            UpdateActiveBoss();
            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            if (!_hasActiveBoss)
            {
                base.OnDrawControl(gameTime);
                return;
            }

            var barArea = GetBarArea();
            var fillWidth = (int)Math.Round(barArea.Width * (_activePercent / 100.0));
            fillWidth = Math.Max(0, Math.Min(barArea.Width, fillWidth));

            var fillColor = GetFillColor(_activePercent);
            var fillArea = new Rectangle(barArea.X, barArea.Y, fillWidth, barArea.Height);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_pixel, new Rectangle(barArea.X - 2, barArea.Y - 2, barArea.Width + 4, barArea.Height + 4), new Color(0, 0, 0, 180));
            _spriteBatch.Draw(_pixel, barArea, new Color(40, 40, 40, 220));
            if (fillWidth > 0)
                _spriteBatch.Draw(_pixel, fillArea, fillColor);
            _spriteBatch.Draw(_pixel, new Rectangle(barArea.X, barArea.Y, barArea.Width, 1), Color.Black * 0.6f);
            _spriteBatch.Draw(_pixel, new Rectangle(barArea.X, barArea.Bottom - 1, barArea.Width, 1), Color.Black * 0.6f);
            _spriteBatch.Draw(_pixel, new Rectangle(barArea.X, barArea.Y, 1, barArea.Height), Color.Black * 0.6f);
            _spriteBatch.Draw(_pixel, new Rectangle(barArea.Right - 1, barArea.Y, 1, barArea.Height), Color.Black * 0.6f);
            _spriteBatch.End();

            base.OnDrawControl(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _pixel.Dispose();

            base.Dispose(disposing);
        }

        private void UpdateActiveBoss()
        {
            var character = _characterProvider.MainCharacter;
            var cx = character.X;
            var cy = character.Y;

            string nearestName = null;
            var nearestDistance = int.MaxValue;
            var nearestPercent = 100;

            foreach (var npc in _currentMapStateProvider.NPCs)
            {
                if (npc.ID <= 0 || npc.ID > _enfFileProvider.ENFFile.Length)
                    continue;

                var record = _enfFileProvider.ENFFile[npc.ID];
                if (record.Boss <= 0)
                    continue;

                var distance = Math.Max(Math.Abs(cx - npc.X), Math.Abs(cy - npc.Y));
                if (distance > ShowDistanceTiles)
                    continue;

                var percent = _bossHealthProvider.PercentHealthByNpcIndex.TryGetValue(npc.Index, out var known)
                    ? known
                    : 100;

                if (distance < nearestDistance || (distance == nearestDistance && percent < nearestPercent))
                {
                    nearestName = record.Name;
                    nearestDistance = distance;
                    nearestPercent = percent;
                }
            }

            if (nearestName == null)
            {
                _hasActiveBoss = false;
                Visible = false;
                _nameLabel.Visible = false;
                _percentLabel.Visible = false;
                return;
            }

            _hasActiveBoss = true;
            Visible = true;
            _activeBossName = nearestName;
            _activePercent = nearestPercent;

            UpdateDrawArea();

            _nameLabel.Text = _activeBossName;
            _nameLabel.Visible = true;
            _nameLabel.DrawPosition = new Vector2(
                Math.Max(0, (BarWidth - _nameLabel.DrawArea.Width) / 2f),
                0);

            _percentLabel.Text = $"{_activePercent}%";
            _percentLabel.Visible = true;
            _percentLabel.DrawPosition = new Vector2(
                Math.Max(0, (BarWidth - _percentLabel.DrawArea.Width) / 2f),
                16 + Math.Max(0, (BarHeight - _percentLabel.DrawArea.Height) / 2f));
        }

        private void UpdateDrawArea()
        {
            var width = _clientWindowSizeProvider.Width;
            DrawArea = new Rectangle((width - BarWidth) / 2, TopMargin, BarWidth, BarHeight + 24);
        }

        private Rectangle GetBarArea()
        {
            return new Rectangle(DrawAreaWithParentOffset.X, DrawAreaWithParentOffset.Y + 16, BarWidth, BarHeight);
        }

        private static Color GetFillColor(int percent)
        {
            if (percent >= 50)
                return new Color(48, 180, 72);
            if (percent >= 25)
                return new Color(220, 180, 40);
            return new Color(200, 48, 48);
        }
    }
}
