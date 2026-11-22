using System;
using System.Collections.Generic;
using EndlessClient.Audio;
using EndlessClient.Dialogs;
using EndlessClient.HUD;
using EndlessClient.HUD.Controls;
using EndlessClient.HUD.Panels;
using EOLib.Domain.Character;
using EOLib.Graphics;
using EOLib.IO.Extensions;
using EOLib.IO.Pub;
using EOLib.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using Optional;
using XNAControls;

namespace EndlessClient.HUD.Inventory
{
    public class InventoryPanelItem : DraggablePanelItem<EIFRecord>
    {
        private const int MaxStatLines = 12;
        private const float TooltipTextScale = 1.1f;
        private const int StatLineHeight = 13; // ~12px line * TooltipTextScale
        private const uint TooltipTextPadX = 4;
        private const uint TooltipTextPadY = 2;

        private readonly InventoryPanel _inventoryPanel;
        private readonly IActiveDialogProvider _activeDialogProvider;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IItemStatCompareService _itemStatCompareService;
        private readonly Texture2D _itemGraphic;
        private readonly Texture2D _highlightBackground;
        private readonly Texture2D _tooltipBackground;
        private readonly HudScaledLabel _nameLabel;
        private readonly List<HudScaledLabel> _statLabels = new List<HudScaledLabel>();

        private int _slot;
        private Option<Vector2> _highlightDrawPosition;
        private bool _tooltipVisible;

        public int Slot
        {
            get => _slot;
            set
            {
                _slot = value;
                DrawPosition = GetPosition(_slot);
                UpdateNameLabelPosition();
            }
        }

        public InventoryItem InventoryItem { get; set; }

        public string Text
        {
            get => _nameLabel.Text;
            set
            {
                _nameLabel.Text = value;
                _nameLabel.ResizeToText(TooltipTextPadX, TooltipTextPadY);
                UpdateNameLabelPosition();
            }
        }

        public event EventHandler<EIFRecord> DoubleClick;

        public override Rectangle EventArea => IsDragging ? DrawArea : DrawAreaWithParentOffset;

        // uses absolute coordinates
        protected override Rectangle GridArea
        {
            get
            {
                var origin = HudUiMetrics.ScalePoint(12, 8, _inventoryPanel.ResizableHud).ToPoint();
                var size = HudUiMetrics.ScalePoint(363, 102, _inventoryPanel.ResizableHud).ToPoint();
                return new Rectangle(
                    _parentContainer.DrawPositionWithParentOffset.ToPoint() + origin,
                    size);
            }
        }

        public InventoryPanelItem(IItemNameColorService itemNameColorService,
                                  InventoryPanel inventoryPanel,
                                  IActiveDialogProvider activeDialogProvider,
                                  ISfxPlayer sfxPlayer,
                                  IItemStatCompareService itemStatCompareService,
                                  int slot,
                                  InventoryItem inventoryItem,
                                  EIFRecord data)
            : base(inventoryPanel)
        {
            _inventoryPanel = inventoryPanel;
            _activeDialogProvider = activeDialogProvider;
            _sfxPlayer = sfxPlayer;
            _itemStatCompareService = itemStatCompareService;

            Slot = slot;
            InventoryItem = inventoryItem;
            Data = data;

            _itemGraphic = inventoryPanel.NativeGraphicsManager.TextureFromResource(GFXTypes.Items, 2 * data.Graphic, transparent: true);
            _highlightBackground = new Texture2D(Game.GraphicsDevice, 1, 1);
            _highlightBackground.SetData(new[] { Color.FromNonPremultiplied(200, 200, 200, 60) });
            _tooltipBackground = new Texture2D(Game.GraphicsDevice, 1, 1);
            _tooltipBackground.SetData(new[] { Color.FromNonPremultiplied(16, 16, 16, 245) });

            _nameLabel = new HudScaledLabel(Constants.FontSize08, TooltipTextScale)
            {
                Visible = false,
                TextAlign = LabelAlignment.MiddleLeft,
                ForeColor = itemNameColorService.GetColorForInventoryDisplay(Data),
                Text = string.Empty
            };

            for (int i = 0; i < MaxStatLines; i++)
            {
                _statLabels.Add(new HudScaledLabel(Constants.FontSize08, TooltipTextScale)
                {
                    Visible = false,
                    TextAlign = LabelAlignment.MiddleLeft,
                    ForeColor = Color.White,
                    Text = string.Empty
                });
            }

            OnMouseEnter += (_, _) =>
            {
                if (!_parentContainer.NoItemsDragging() || !_activeDialogProvider.PaperdollDialog.Match(d => d.NoItemsDragging(), () => true))
                    return;

                ShowTooltip();
            };
            OnMouseOver += InventoryPanelItem_OnMouseOver;
            OnMouseLeave += (_, _) => HideTooltip();

            DraggingStarted += (_, _) =>
            {
                HideTooltip();
                _sfxPlayer.PlaySfx(SoundEffectID.InventoryPickup);
            };

            var (slotWidth, slotHeight) = Data.Size.GetDimensions();
            var slotPx = _inventoryPanel.SlotPixelSize;
            SetSize(slotWidth * slotPx - 3, slotHeight * slotPx - 3);
        }

        public int GetCurrentSlotBasedOnPosition()
        {
            if (!IsDragging)
                return Slot;

            var slotPx = _inventoryPanel.SlotPixelSize;
            return (int)((DrawPosition.X - OldOffset.X) / slotPx) + InventoryPanel.InventoryRowSlots * (int)((DrawPosition.Y - OldOffset.Y) / slotPx);
        }

        public override void Initialize()
        {
            _nameLabel.Initialize();
            _nameLabel.SetParentControl(_parentContainer);
            _nameLabel.ResizeToText(TooltipTextPadX, TooltipTextPadY);

            foreach (var label in _statLabels)
            {
                label.Initialize();
                label.SetParentControl(_parentContainer);
            }

            base.Initialize();
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            _spriteBatch.Begin();

            _highlightDrawPosition.MatchSome(drawPosition =>
            {
                if (GridArea.Contains(DrawArea.WithPosition(drawPosition)))
                    _spriteBatch.Draw(_highlightBackground, DrawArea.WithPosition(drawPosition), Color.White);
            });

            // Stretch item art to the scaled slot size so icons fill enlarged inventory cells.
            if (IsDragging)
            {
                var dest = new Rectangle(
                    (int)DrawPosition.X,
                    (int)DrawPosition.Y,
                    DrawArea.Width,
                    DrawArea.Height);
                _spriteBatch.Draw(_itemGraphic, dest, Color.FromNonPremultiplied(255, 255, 255, 128));
            }
            else
            {
                _spriteBatch.Draw(
                    _itemGraphic,
                    DrawAreaWithParentOffset,
                    Color.FromNonPremultiplied(255, 255, 255, 255));
            }

            if (_tooltipVisible)
            {
                var tooltipArea = GetTooltipBackgroundArea();
                _spriteBatch.Draw(_tooltipBackground, tooltipArea, Color.White);
            }

            _spriteBatch.End();
            base.OnDrawControl(gameTime);
        }

        private void InventoryPanelItem_OnMouseOver(object sender, MouseStateExtended e)
        {
            if (!GridArea.Contains(e.Position))
                return;

            var currentSlot = GetCurrentSlotBasedOnPosition();
            _highlightDrawPosition = Option.Some(GetPosition(currentSlot) + (IsDragging ? OldOffset : ImmediateParent.DrawPositionWithParentOffset));
        }

        protected override bool HandleDoubleClick(IXNAControl control, MouseEventArgs eventArgs)
        {
            if (IsDragging)
            {
                // roll back the first click in the double click
                base.HandleMouseDown(control, eventArgs);
            }

            DoubleClick?.Invoke(control, Data);

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nameLabel.Dispose();
                foreach (var label in _statLabels)
                    label.Dispose();
                _highlightBackground.Dispose();
                _tooltipBackground.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ShowTooltip()
        {
            var lines = _itemStatCompareService.GetInventoryHoverLines(Data);
            _nameLabel.Visible = true;
            _tooltipVisible = true;

            for (int i = 0; i < _statLabels.Count; i++)
            {
                if (i < lines.Count)
                {
                    _statLabels[i].Text = lines[i].Text;
                    _statLabels[i].ForeColor = lines[i].Color;
                    _statLabels[i].Visible = true;
                    _statLabels[i].ResizeToText(TooltipTextPadX, TooltipTextPadY);
                }
                else
                {
                    _statLabels[i].Visible = false;
                    _statLabels[i].Text = string.Empty;
                }
            }

            UpdateNameLabelPosition();
        }

        private void HideTooltip()
        {
            _nameLabel.Visible = false;
            _tooltipVisible = false;
            _highlightDrawPosition = Option.None<Vector2>();

            foreach (var label in _statLabels)
            {
                label.Visible = false;
            }
        }

        private Rectangle GetTooltipBackgroundArea()
        {
            var visibleStats = 0;
            var maxWidth = _nameLabel.DrawArea.Width;

            foreach (var label in _statLabels)
            {
                if (!label.Visible)
                    continue;

                visibleStats++;
                if (label.DrawArea.Width > maxWidth)
                    maxWidth = label.DrawArea.Width;
            }

            var height = _nameLabel.DrawArea.Height + visibleStats * StatLineHeight + 4;
            var pos = _nameLabel.DrawPositionWithParentOffset;
            return new Rectangle((int)pos.X - 2, (int)pos.Y - 1, maxWidth + 6, height);
        }

        private void UpdateNameLabelPosition()
        {
            if (_nameLabel == null)
                return;

            // the name label is parented to the inventory panel so that all name labels draw over all items
            var actualPosition = DrawPosition;

            var tooltipWidth = Math.Max(_nameLabel.DrawArea.Width, GetVisibleStatWidth());
            if (actualPosition.X + tooltipWidth + DrawArea.Width > GridArea.Width)
            {
                _nameLabel.DrawPosition = new Vector2(actualPosition.X - tooltipWidth - 4, actualPosition.Y);
            }
            else
            {
                _nameLabel.DrawPosition = new Vector2(actualPosition.X + DrawArea.Width + 4, actualPosition.Y);
            }

            DrawOrder = _parentContainer.DrawOrder + 2;
            _nameLabel.DrawOrder = _parentContainer.DrawOrder + 200;

            var lineIndex = 0;
            foreach (var label in _statLabels)
            {
                label.DrawPosition = new Vector2(
                    _nameLabel.DrawPosition.X,
                    _nameLabel.DrawPosition.Y + _nameLabel.DrawArea.Height + 2 + lineIndex * StatLineHeight);
                label.DrawOrder = _parentContainer.DrawOrder + 201;
                if (label.Visible)
                    lineIndex++;
            }
        }

        private int GetVisibleStatWidth()
        {
            var width = 0;
            foreach (var label in _statLabels)
            {
                if (label.Visible && label.DrawArea.Width > width)
                    width = label.DrawArea.Width;
            }
            return width;
        }

        protected override void OnDraggingFinished(DragCompletedEventArgs<EIFRecord> args)
        {
            base.OnDraggingFinished(args);

            if (args.ContinueDrag)
                return;

            _sfxPlayer.PlaySfx(SoundEffectID.InventoryPlace);

            if (!args.RestoreOriginalSlot)
                Slot = GetCurrentSlotBasedOnPosition();
            else
                DrawPosition = GetPosition(Slot);

            HideTooltip();
            UpdateNameLabelPosition();
        }

        private Vector2 GetPosition(int slot)
        {
            var slotPx = _inventoryPanel.SlotPixelSize;
            var origin = HudUiMetrics.ScalePoint(13, 9, _inventoryPanel.ResizableHud);
            return new Vector2(
                origin.X + slotPx * (slot % InventoryPanel.InventoryRowSlots),
                origin.Y + slotPx * (slot / InventoryPanel.InventoryRowSlots));
        }
    }
}
