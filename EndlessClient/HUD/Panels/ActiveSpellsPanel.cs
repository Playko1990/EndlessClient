using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using EndlessClient.Audio;
using EndlessClient.ControlSets;
using EndlessClient.HUD;
using EndlessClient.HUD.Controls;
using EndlessClient.HUD.Spells;
using EndlessClient.Rendering;
using EndlessClient.UIControls;
using EOLib.Config;
using EOLib.Domain.Character;
using EOLib.Domain.Login;
using EOLib.Graphics;
using EOLib.IO.Pub;
using EOLib.IO.Repositories;
using EOLib.Localization;
using EOLib.Shared;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using Optional;
using Optional.Collections;
using XNAControls;

namespace EndlessClient.HUD.Panels
{
    public class ActiveSpellsPanel : DraggableHudPanel, IHudPanel, IDraggableItemContainer
    {
        public const int SpellRows = 4;
        public const int SpellRowLength = 8;

        private readonly IStatusLabelSetter _statusLabelSetter;
        private readonly IPlayerInfoProvider _playerInfoProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICharacterInventoryProvider _characterInventoryProvider;
        private readonly IESFFileProvider _esfFileProvider;
        private readonly ISpellSlotDataRepository _spellSlotDataRepository;
        private readonly IHudControlProvider _hudControlProvider;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IConfigurationProvider _configProvider;
        private readonly ISkillTreeActions _skillTreeActions;
        private readonly IHudStateActions _hudStateActions;

        private readonly Dictionary<int, int> _spellSlotMap;
        private readonly List<SpellPanelItem> _childItems;

        private readonly Texture2D _functionKeyLabelSheet;
        private Rectangle _functionKeyRow1Source, _functionKeyRow2Source;

        private readonly XNALabel _selectedSpellName;
        private readonly HudScaledLabel _selectedSpellLevel, _totalSkillPoints;
        private readonly ScrollBar _scrollBar;

        private HashSet<InventorySpell> _cachedSpells;
        private int _cachedAvailableSkillPoints = int.MinValue;
        private Option<int> _cachedSelectedSpell;

        private int _lastScrollOffset;
        private Texture2D _activeSpellIcon;

        public INativeGraphicsManager NativeGraphicsManager { get; }

        public bool ResizableHud { get; }

        public ActiveSpellsPanel(INativeGraphicsManager nativeGraphicsManager,
                                 IStatusLabelSetter statusLabelSetter,
                                 IPlayerInfoProvider playerInfoProvider,
                                 ICharacterProvider characterProvider,
                                 ICharacterInventoryProvider characterInventoryProvider,
                                 IESFFileProvider esfFileProvider,
                                 ISpellSlotDataRepository spellSlotDataRepository,
                                 IHudControlProvider hudControlProvider,
                                 ISfxPlayer sfxPlayer,
                                 IConfigurationProvider configProvider,
                                 ISkillTreeActions skillTreeActions,
                                 IHudStateActions hudStateActions,
                                 IClientWindowSizeProvider clientWindowSizeProvider)
            : base(clientWindowSizeProvider.Resizable)
        {
            NativeGraphicsManager = nativeGraphicsManager;
            ResizableHud = clientWindowSizeProvider.Resizable;
            _statusLabelSetter = statusLabelSetter;
            _playerInfoProvider = playerInfoProvider;
            _characterProvider = characterProvider;
            _characterInventoryProvider = characterInventoryProvider;
            _esfFileProvider = esfFileProvider;
            _spellSlotDataRepository = spellSlotDataRepository;
            _hudControlProvider = hudControlProvider;
            _sfxPlayer = sfxPlayer;
            _configProvider = configProvider;
            _skillTreeActions = skillTreeActions;
            _hudStateActions = hudStateActions;

            _spellSlotMap = GetSpellSlotMap(_playerInfoProvider.LoggedInAccountName, _characterProvider.MainCharacter.Name, _configProvider.Host);
            _childItems = new List<SpellPanelItem>();

            _cachedSpells = new HashSet<InventorySpell>();

            _functionKeyLabelSheet = NativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 58, true);
            _functionKeyRow1Source = new Rectangle(148, 51, 18, 13);
            _functionKeyRow2Source = new Rectangle(148 + 18 * 8, 51, 18, 13);

            var resizable = clientWindowSizeProvider.Resizable;
            var font = HudUiMetrics.PanelLabelFont(resizable);

            BackgroundImage = NativeGraphicsManager.TextureFromResource(GFXTypes.PostLoginUI, 62);
            DrawArea = HudUiMetrics.ScaledPanelDrawArea(BackgroundImage, clientWindowSizeProvider.Resizable);
            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, resizable);

            _selectedSpellName = new XNALabel(font)
            {
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(9, 50, 81, 13), uiScale),
                Visible = false,
                Text = "",
                AutoSize = false,
                TextAlign = LabelAlignment.MiddleCenter,
                ForeColor = ColorConstants.LightGrayText,
            };

            var valueScale = HudUiMetrics.PanelValueTextScale(resizable);
            _selectedSpellLevel = new HudScaledLabel(font, valueScale)
            {
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(32, 78, 42, 15), uiScale),
                Visible = true,
                Text = "0",
                TextAlign = LabelAlignment.MiddleLeft,
                ForeColor = ColorConstants.LightGrayText,
            };

            _skillTreeActions.EnsureLoaded();
            _totalSkillPoints = new HudScaledLabel(font, valueScale)
            {
                DrawArea = HudUiMetrics.ScaleRectangle(new Rectangle(32, 96, 42, 15), uiScale),
                Visible = true,
                Text = _skillTreeActions.GetAvailablePoints().ToString(),
                TextAlign = LabelAlignment.MiddleLeft,
                ForeColor = ColorConstants.LightGrayText,
            };

            // One large scrollbar on the right gutter (no inset mid-panel arrows).
            var scrollPos = HudUiMetrics.ScalePoint(467, 2, uiScale);
            var scrollSize = HudUiMetrics.ScalePoint(16, 115, uiScale);
            _scrollBar = new ScrollBar(scrollPos, scrollSize, ScrollBarColors.LightOnMed, NativeGraphicsManager, uiScale)
            {
                LinesToRender = 2
            };
            _scrollBar.UpdateDimensions(4);
            SetScrollWheelHandler(_scrollBar);

            Game.Exiting += SaveSpellsFile;
        }

        public bool NoItemsDragging() => _childItems.All(x => !x.IsDragging);

        public override void Initialize()
        {
            _selectedSpellName.Initialize();
            _selectedSpellName.SetParentControl(this);

            _selectedSpellLevel.Initialize();
            _selectedSpellLevel.SetParentControl(this);

            _totalSkillPoints.Initialize();
            _totalSkillPoints.SetParentControl(this);

            _scrollBar.Initialize();
            _scrollBar.SetParentControl(this);

            base.Initialize();
        }

        protected override void OnUnconditionalUpdateControl(GameTime gameTime)
        {
            if (!_cachedSpells.SetEquals(_characterInventoryProvider.SpellInventory))
            {
                var added = _characterInventoryProvider.SpellInventory.Where(i => !_cachedSpells.Any(j => i.ID == j.ID));
                var removed = _cachedSpells.Where(i => !_characterInventoryProvider.SpellInventory.Any(j => i.ID == j.ID));
                var updated = _characterInventoryProvider.SpellInventory.Except(added)
                    .Where(i => _cachedSpells.Any(j => i.ID == j.ID && i.Level != j.Level));

                foreach (var spell in removed)
                {
                    var matchedSpell = _childItems.SingleOrNone(x => x.InventorySpell.ID == spell.ID);
                    matchedSpell.MatchSome(childControl =>
                    {
                        childControl.SetControlUnparented();
                        childControl.Dispose();
                        _childItems.Remove(childControl);

                        _spellSlotDataRepository.SpellSlots[childControl.Slot] = Option.None<InventorySpell>();
                    });
                }

                foreach (var spell in updated)
                {
                    var matchedSpell = _childItems.SingleOrNone(x => x.InventorySpell.ID == spell.ID);
                    matchedSpell.MatchSome(childControl =>
                    {
                        childControl.InventorySpell = spell;
                        _spellSlotDataRepository.SelectedSpellSlot.MatchSome(s =>
                        {
                            if (childControl.Slot == s)
                                _selectedSpellLevel.Text = spell.Level.ToString();
                        });
                    });
                }

                foreach (var spell in added)
                {
                    var spellData = _esfFileProvider.ESFFile[spell.ID];

                    var preferredSlot = _spellSlotMap.SingleOrNone(x => x.Value == spell.ID).Map(x => x.Key);
                    var actualSlot = preferredSlot.Match(
                        some: x =>
                        {
                            return _childItems.OfType<SpellPanelItem>().Any(ci => ci.Slot == x)
                                ? GetNextOpenSlot(_childItems)
                                : Option.Some(x);
                        },
                        none: () => GetNextOpenSlot(_childItems));

                    actualSlot.MatchSome(slot =>
                    {
                        var newChild = new SpellPanelItem(this, _sfxPlayer, slot, spell, spellData);
                        newChild.Initialize();
                        newChild.SetParentControl(this);

                        // this is required so scroll works while click+dragging when the control becomes unparented
                        newChild.SetScrollWheelHandler(_scrollBar);

                        newChild.Click += (sender, _) => _spellSlotDataRepository.SelectedSpellSlot = Option.Some(((SpellPanelItem)sender).Slot);
                        newChild.OnMouseOver += SetSpellStatusLabelHover;
                        newChild.DraggingFinishing += HandleItemDoneDragging;

                        _childItems.Add(newChild);
                        _spellSlotDataRepository.SpellSlots[slot] = Option.Some(spell);
                    });
                }

                if (added.Any())
                    UpdateSpellItemsForScroll();

                _cachedSpells = _characterInventoryProvider.SpellInventory.ToHashSet();
            }

            base.OnUnconditionalUpdateControl(gameTime);
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            var kbd = KeyboardExtended.GetState();
            if (kbd.WasKeyJustUp(Keys.RightShift) || kbd.WasKeyJustDown(Keys.RightShift) ||
                kbd.WasKeyJustUp(Keys.LeftShift) || kbd.WasKeyJustDown(Keys.LeftShift))
            {
                SwapFunctionKeySourceRectangles();
            }

            if (_lastScrollOffset != _scrollBar.ScrollOffset)
            {
                UpdateSpellItemsForScroll();
            }

            _skillTreeActions.EnsureLoaded();
            var availableSkillPoints = _skillTreeActions.GetAvailablePoints();
            if (availableSkillPoints != _cachedAvailableSkillPoints)
            {
                _cachedAvailableSkillPoints = availableSkillPoints;
                _totalSkillPoints.Text = availableSkillPoints.ToString();
            }

            _cachedSelectedSpell.Match(
                some: cached => _spellSlotDataRepository.SelectedSpellSlot.Match(
                    some: repoSlot =>
                    {
                        if (cached != repoSlot)
                        {
                            SetSelectedSpell(repoSlot);
                            _cachedSelectedSpell = _spellSlotDataRepository.SelectedSpellSlot;
                        }
                    },
                    none: () =>
                    {
                        ClearSelectedSpell();
                        _cachedSelectedSpell = Option.None<int>();
                    }),
                none: () => _spellSlotDataRepository.SelectedSpellSlot.MatchSome(
                    some: repoSlot =>
                    {
                        SetSelectedSpell(repoSlot);
                        _cachedSelectedSpell = _spellSlotDataRepository.SelectedSpellSlot;
                    }));

            base.OnUpdateControl(gameTime);
        }

        protected override void OnDrawControl(GameTime gameTime)
        {
            base.OnDrawControl(gameTime);

            _spriteBatch.Begin();

            DrawFunctionKeyLabels();
            DrawActiveSpell();

            _spriteBatch.End();
        }

        private void DrawFunctionKeyLabels()
        {
            if (_scrollBar.ScrollOffset >= 2)
                return;

            // Align F1–F8 with spell slot boxes (101 + col*45, 9 + row*52).
            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, ResizableHud);
            var labelSize = HudUiMetrics.ScalePoint(18, 13, uiScale);

            for (int i = 0; i < 8; ++i)
            {
                var offset = _functionKeyRow1Source.Width * i;

                if (_scrollBar.ScrollOffset == 0)
                {
                    var pos = DrawPosition + HudUiMetrics.ScalePoint(101 + 45 * i, 9, uiScale);
                    _spriteBatch.Draw(_functionKeyLabelSheet,
                        new Rectangle((int)pos.X, (int)pos.Y, (int)labelSize.X, (int)labelSize.Y),
                        _functionKeyRow1Source.WithPosition((_functionKeyRow1Source.Location + new Point(offset, 0)).ToVector2()),
                        Color.White);
                }

                if (_scrollBar.ScrollOffset < 2)
                {
                    var yCoord = _scrollBar.ScrollOffset == 0 ? 9 + 52 : 9;
                    var pos = DrawPosition + HudUiMetrics.ScalePoint(101 + 45 * i, yCoord, uiScale);
                    _spriteBatch.Draw(_functionKeyLabelSheet,
                        new Rectangle((int)pos.X, (int)pos.Y, (int)labelSize.X, (int)labelSize.Y),
                        _functionKeyRow2Source.WithPosition((_functionKeyRow2Source.Location + new Point(offset, 0)).ToVector2()),
                        Color.White);
                }
            }
        }

        private void DrawActiveSpell()
        {
            if (_activeSpellIcon == null)
                return;

            var uiScale = HudUiMetrics.ScaleFromPanel(BackgroundImage, DrawArea, ResizableHud);
            var srcRect = new Rectangle(0, 0, _activeSpellIcon.Width / 2, _activeSpellIcon.Height);
            var size = HudUiMetrics.Scale(Math.Max(srcRect.Width, srcRect.Height), uiScale);
            var dstRect = new Rectangle(
                DrawAreaWithParentOffset.X + HudUiMetrics.Scale(32, uiScale),
                DrawAreaWithParentOffset.Y + HudUiMetrics.Scale(14, uiScale),
                size,
                size);
            _spriteBatch.Draw(_activeSpellIcon, dstRect, srcRect, Color.White);
        }

        private void SetSelectedSpell(int slot)
        {
            _childItems.OfType<SpellPanelItem>().SingleOrNone(x => x.Slot == slot)
                .MatchSome(spell =>
                {
                    var spellData = spell.Data;

                    _activeSpellIcon = NativeGraphicsManager.TextureFromResource(GFXTypes.SpellIcons, spellData.Icon);

                    _selectedSpellName.Text = spellData.Name;
                    _selectedSpellName.Visible = true;

                    _selectedSpellLevel.Text = spell.InventorySpell.Level.ToString();
                });
        }

        private void ClearSelectedSpell()
        {
            _activeSpellIcon = null;

            _selectedSpellName.Text = string.Empty;
            _selectedSpellName.Visible = false;

            _selectedSpellLevel.Text = "0";
        }

        private void SetSpellStatusLabelHover(object sender, MouseStateExtended e)
        {
            var spellPanelItem = sender as SpellPanelItem;
            if (spellPanelItem == null || spellPanelItem.IsDragging)
                return;

            _statusLabelSetter.SetStatusLabel(EOResourceID.SKILLMASTER_WORD_SPELL, spellPanelItem.Data.Name);
        }

        private void HandleItemDoneDragging(object sender, DragCompletedEventArgs<ESFRecord> e)
        {
            var item = sender as SpellPanelItem;
            if (item == null)
                return;

            if (e.DragOutOfBounds)
            {
                e.RestoreOriginalSlot = true;
                return;
            }

            var oldSlot = item.Slot;
            var newSlot = item.GetCurrentSlotBasedOnPosition(_scrollBar.ScrollOffset);

            if (oldSlot != newSlot &&
                newSlot < (_scrollBar.ScrollOffset + _scrollBar.LinesToRender) * SpellRowLength &&
                newSlot >= _scrollBar.ScrollOffset * SpellRowLength)
            {
                _childItems.SingleOrNone(x => x.Slot == newSlot)
                    .Match(child =>
                    {
                        var oldDisplaySlot = item.DisplaySlot;
                        var newDisplaySlot = child.DisplaySlot;

                        item.Slot = newSlot;
                        item.DisplaySlot = newDisplaySlot;

                        child.Slot = oldSlot;
                        child.DisplaySlot = oldDisplaySlot;

                        _spellSlotDataRepository.SpellSlots[oldSlot] = Option.Some(child.InventorySpell);
                        _spellSlotDataRepository.SpellSlots[newSlot] = Option.Some(item.InventorySpell);
                        _spellSlotDataRepository.SelectedSpellSlot = Option.Some(newSlot);
                        _spellSlotDataRepository.SpellIsPrepared = false;
                    },
                    () =>
                    {
                        item.Slot = newSlot;
                        item.DisplaySlot = newSlot - (SpellRowLength * _scrollBar.ScrollOffset);

                        _spellSlotDataRepository.SpellSlots[oldSlot] = Option.None<InventorySpell>();
                        _spellSlotDataRepository.SpellSlots[newSlot] = Option.Some(item.InventorySpell);
                        _spellSlotDataRepository.SelectedSpellSlot = Option.Some(newSlot);
                        _spellSlotDataRepository.SpellIsPrepared = false;
                    });

                UpdateSpellItemsForScroll();
            }
        }

        private Option<int> GetNextOpenSlot(IEnumerable<SpellPanelItem> childItems)
        {
            return _spellSlotDataRepository.SpellSlots
                .Select((spellItem, slot) => (SpellItem: spellItem, Slot: slot))
                .Where(pair => !pair.SpellItem.HasValue && childItems.All(x => x.Slot != pair.Slot))
                .FirstOrNone()
                .Map(x => x.Slot);
        }

        private void SwapFunctionKeySourceRectangles()
        {
            var tmpRect = _functionKeyRow2Source;
            _functionKeyRow2Source = _functionKeyRow1Source;
            _functionKeyRow1Source = tmpRect;
        }

        private void UpdateSpellItemsForScroll()
        {
            var firstValidSlot = _scrollBar.ScrollOffset * SpellRowLength;
            var lastValidSlot = firstValidSlot + 2 * SpellRowLength;

            var itemsToHide = _childItems.Where(x => x.Slot < firstValidSlot || x.Slot >= lastValidSlot).ToList();
            foreach (var item in itemsToHide)
            {
                if (!item.IsDragging)
                    item.Visible = false;

                item.DisplaySlot = GetDisplaySlotFromSlot(item.Slot);
            }

            foreach (var item in _childItems.Except(itemsToHide))
            {
                item.Visible = true;
                item.DisplaySlot = item.Slot - firstValidSlot;
            }

            _lastScrollOffset = _scrollBar.ScrollOffset;
        }

        private int GetDisplaySlotFromSlot(int newSlot)
        {
            var offset = _scrollBar.ScrollOffset;
            return newSlot - SpellRowLength * offset;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Exiting -= SaveSpellsFile;
                SaveSpellsFile(null, new ExitingEventArgs());
            }

            base.Dispose(disposing);
        }

        #region Slot loading

        private static Dictionary<int, int> GetSpellSlotMap(string accountName, string characterName, string host)
        {
            var map = new Dictionary<int, int>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !File.Exists(Constants.SpellsFile))
            {
                using var registrySpellsKey = TryGetCharacterRegistryKey(accountName, characterName);
                if (registrySpellsKey != null)
                {
                    for (int i = 0; i < SpellRowLength * SpellRows; ++i)
                    {
                        if (int.TryParse(registrySpellsKey.GetValue($"item{i}")?.ToString() ?? string.Empty, out var id))
                            map[i] = id;
                    }
                }
            }

            var spells = new IniReader(Constants.SpellsFile);
            var spellsKey = $"{host}:{accountName}";
            if (spells.Load() && spells.Sections.ContainsKey(spellsKey))
            {
                var section = spells.Sections[spellsKey];
                foreach (var key in section.Keys.Where(x => x.Contains(characterName, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!key.Contains("."))
                        continue;

                    var slot = key.Split(".")[1];
                    if (!int.TryParse(slot, out var slotIndex))
                        continue;

                    if (int.TryParse(section[key], out var id))
                        map[slotIndex] = id;
                }
            }

            return map;
        }

        [SupportedOSPlatform("Windows")]
        private static RegistryKey TryGetCharacterRegistryKey(string accountName, string characterName)
        {
            using RegistryKey currentUser = Registry.CurrentUser;

            var pathSegments = $"Software\\EndlessClient\\{accountName}\\{characterName}\\spells".Split('\\');
            var currentPath = string.Empty;

            RegistryKey retKey = null;
            foreach (var segment in pathSegments)
            {
                retKey?.Dispose();

                currentPath = Path.Combine(currentPath, segment);
                retKey = currentUser.CreateSubKey(currentPath, RegistryKeyPermissionCheck.ReadSubTree);
            }

            return retKey;
        }

        private void SaveSpellsFile(object sender, ExitingEventArgs e)
        {
            var spells = new IniReader(Constants.SpellsFile);
            var spellsKey = $"{_configProvider.Host}:{_playerInfoProvider.LoggedInAccountName}";

            var section = spells.Load() && spells.Sections.ContainsKey(spellsKey)
                ? spells.Sections[spellsKey]
                : [];

            var existing = section.Where(x => x.Key.Contains(_characterProvider.MainCharacter.Name)).Select(x => x.Key).ToList();
            foreach (var key in existing)
                section.Remove(key);

            foreach (var item in _childItems.OfType<SpellPanelItem>())
                section[$"{_characterProvider.MainCharacter.Name}.{item.Slot}"] = $"{item.InventorySpell.ID}";

            spells.Sections[spellsKey] = section;

            spells.Save();
        }

        #endregion
    }
}
