using System;
using EndlessClient.Audio;
using EndlessClient.HUD;
using EndlessClient.HUD.Controls;
using EndlessClient.HUD.Panels;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.IO.Pub;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input.InputListeners;
using XNAControls;

namespace EndlessClient.HUD.Spells
{
    public class SpellPanelItem : DraggablePanelItem<ESFRecord>
    {
        private const int ICON_AREA_WIDTH = 42, ICON_AREA_HEIGHT = 36;

        private readonly ISfxPlayer _sfxPlayer;
        private readonly bool _resizable;

        private readonly Texture2D _spellGraphic;
        private Rectangle _spellGraphicSourceRect;

        private readonly Texture2D _whitePixel;

        public int Slot { get; set; }

        private int _displaySlot;
        public int DisplaySlot
        {
            get => _displaySlot;
            set
            {
                _displaySlot = value;
                DrawPosition = GetDisplayPosition(_displaySlot);
            }
        }

        public InventorySpell InventorySpell { get; set; }

        public override Rectangle EventArea => IsDragging ? DrawArea : DrawAreaWithParentOffset;

        // uses absolute coordinates
        protected override Rectangle GridArea
        {
            get
            {
                var uiScale = PanelUiScale();
                var origin = HudUiMetrics.ScalePoint(98, 6, uiScale).ToPoint();
                var size = HudUiMetrics.ScalePoint(363, 102, uiScale).ToPoint();
                return new Rectangle(
                    _parentContainer.DrawPositionWithParentOffset.ToPoint() + origin,
                    size);
            }
        }

        public event EventHandler<MouseEventArgs> Click;

        public SpellPanelItem(ActiveSpellsPanel spellPanel,
                              ISfxPlayer sfxPlayer,
                              int slot,
                              InventorySpell spell,
                              ESFRecord data)
            : base(spellPanel)
        {
            _sfxPlayer = sfxPlayer;
            _resizable = spellPanel.ResizableHud;

            Slot = DisplaySlot = slot;
            InventorySpell = spell;
            Data = data;

            _spellGraphic = spellPanel.NativeGraphicsManager.TextureFromResource(GFXTypes.SpellIcons, Data.Icon);
            _spellGraphicSourceRect = new Rectangle(0, 0, _spellGraphic.Width / 2, _spellGraphic.Height);

            _whitePixel = new Texture2D(Game.GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });

            var uiScale = PanelUiScale();
            SetSize(
                HudUiMetrics.Scale(ICON_AREA_WIDTH, uiScale),
                HudUiMetrics.Scale(ICON_AREA_HEIGHT, uiScale));
        }

        public int GetCurrentSlotBasedOnPosition(int scrollOffset)
        {
            if (!IsDragging)
                return Slot;

            // old offset X needs to be adjusted since it assumes parent coordinates are the start of the slot grid
            // this works for inventory without adjustment since the grid goes all the way to the parent panel coordinate
            // however, spell panel has 2 slots worth of padding for the selected slot / level up controls
            var uiScale = PanelUiScale();
            var iconW = HudUiMetrics.Scale(ICON_AREA_WIDTH, uiScale);
            var iconH = HudUiMetrics.Scale(ICON_AREA_HEIGHT, uiScale);
            var adjustedOffsetX = OldOffset.X + (iconW * 2);

            return scrollOffset * ActiveSpellsPanel.SpellRowLength +
                (int)((DrawPosition.X - adjustedOffsetX) / iconW) +
                ActiveSpellsPanel.SpellRowLength * (int)((DrawPosition.Y - OldOffset.Y) / iconH);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            _spriteBatch.Begin();

            DrawLevelAndHighlight();
            DrawSpellIcon();

            _spriteBatch.End();

            base.OnDrawControl(gameTime);
        }

        protected override bool HandleMouseDown(IXNAControl control, MouseEventArgs eventArgs)
        {
            if (_parentContainer.NoItemsDragging())
                _sfxPlayer.PlaySfx(SoundEffectID.InventoryPickup);

            Click?.Invoke(control, eventArgs);
            return base.HandleMouseDown(control, eventArgs);
        }

        protected override bool HandleDragStart(IXNAControl control, MouseEventArgs eventArgs)
        {
            _sfxPlayer.PlaySfx(SoundEffectID.InventoryPickup);

            return base.HandleDragStart(control, eventArgs);
        }

        protected override void OnDraggingFinished(DragCompletedEventArgs<ESFRecord> args)
        {
            base.OnDraggingFinished(args);

            _sfxPlayer.PlaySfx(SoundEffectID.InventoryPlace);
            DrawPosition = GetDisplayPosition(DisplaySlot);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _whitePixel.Dispose();
            }

            base.Dispose(disposing);
        }

        private void DrawLevelAndHighlight()
        {
            if (!IsDragging)
            {
                var width = (int)(InventorySpell.Level / 100.0 * HudUiMetrics.Scale(36, _resizable));
                var levelDestinationRectangle = new Rectangle(
                    DrawAreaWithParentOffset.X + HudUiMetrics.Scale(3, _resizable),
                    DrawAreaWithParentOffset.Y + HudUiMetrics.Scale(40, _resizable),
                    width,
                    HudUiMetrics.Scale(6, _resizable));
                _spriteBatch.Draw(_whitePixel, levelDestinationRectangle, Color.FromNonPremultiplied(0xc9, 0xb8, 0x9b, 0xff));
            }

            if (MouseOver)
            {
                if (!IsDragging)
                {
                    _spriteBatch.Draw(_whitePixel, DrawAreaWithParentOffset, Color.FromNonPremultiplied(200, 200, 200, 60));
                }
                else
                {
                    var highlightPosition = GetDisplayPosition(DisplaySlot);
                    var highlightRectangle = new Rectangle((_parentContainer.DrawPositionWithParentOffset + highlightPosition).ToPoint(), DrawArea.Size);

                    if (highlightRectangle.Contains(Mouse.GetState().Position))
                        _spriteBatch.Draw(_whitePixel, highlightRectangle, Color.FromNonPremultiplied(200, 200, 200, 60));
                }
            }
        }

        private void DrawSpellIcon()
        {
            var halfWidth = _spellGraphic.Width / 2;
            _spellGraphicSourceRect = new Rectangle(MouseOver ? halfWidth : 0, 0, halfWidth, _spellGraphic.Height);

            // Scale spell glyphs to the enlarged slot while keeping aspect ratio.
            var maxW = Math.Max(1, DrawArea.Width - HudUiMetrics.Scale(6, _resizable));
            var maxH = Math.Max(1, DrawArea.Height - HudUiMetrics.Scale(10, _resizable));
            var scale = Math.Min(
                maxW / (float)_spellGraphicSourceRect.Width,
                maxH / (float)_spellGraphicSourceRect.Height);
            var drawW = Math.Max(1, (int)(_spellGraphicSourceRect.Width * scale));
            var drawH = Math.Max(1, (int)(_spellGraphicSourceRect.Height * scale));

            Rectangle targetDrawArea;
            if (!IsDragging)
            {
                targetDrawArea = new Rectangle(
                    DrawAreaWithParentOffset.X + (DrawAreaWithParentOffset.Width - drawW) / 2,
                    DrawAreaWithParentOffset.Y + (DrawAreaWithParentOffset.Height - drawH) / 2,
                    drawW,
                    drawH);
            }
            else
            {
                targetDrawArea = new Rectangle(
                    Mouse.GetState().X - drawW / 2,
                    Mouse.GetState().Y - drawH / 2,
                    drawW,
                    drawH);
            }

            _spriteBatch.Draw(_spellGraphic,
                targetDrawArea,
                _spellGraphicSourceRect,
                Color.FromNonPremultiplied(255, 255, 255, IsDragging ? 127 : 255));
        }

        private Vector2 GetDisplayPosition(int slot)
        {
            //start pos: 101, 9 — xdelta: 45; ydelta: 52 (matches PostLoginUI #62 grid)
            var row = slot / ActiveSpellsPanel.SpellRowLength;
            var col = slot % ActiveSpellsPanel.SpellRowLength;
            return HudUiMetrics.ScalePoint(101 + col * 45, 9 + row * 52, PanelUiScale());
        }

        private float PanelUiScale()
        {
            if (_parentContainer is ActiveSpellsPanel panel)
                return HudUiMetrics.ScaleFromPanel(panel.BackgroundImage, panel.DrawArea, _resizable);
            return HudUiMetrics.UiScale(_resizable);
        }
    }
}
