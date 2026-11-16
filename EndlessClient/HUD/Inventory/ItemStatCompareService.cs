using System;
using System.Collections.Generic;
using AutomaticTypeMapper;
using EOLib.Domain.Character;
using EOLib.IO;
using EOLib.IO.Extensions;
using EOLib.IO.Pub;
using EOLib.IO.Repositories;
using Microsoft.Xna.Framework;

namespace EndlessClient.HUD.Inventory
{
    public enum ItemStatCompareKind
    {
        Better,
        Worse,
        Equal,
        Neutral
    }

    public sealed class ItemStatCompareLine
    {
        public string Text { get; }
        public ItemStatCompareKind Kind { get; }
        public Color Color { get; }

        public ItemStatCompareLine(string text, ItemStatCompareKind kind)
        {
            Text = text;
            Kind = kind;
            Color = kind switch
            {
                ItemStatCompareKind.Better => new Color(80, 220, 80),
                ItemStatCompareKind.Worse => new Color(220, 80, 80),
                ItemStatCompareKind.Equal => new Color(200, 200, 200),
                _ => new Color(230, 230, 200)
            };
        }
    }

    public interface IItemStatCompareService
    {
        IReadOnlyList<ItemStatCompareLine> GetInventoryHoverLines(EIFRecord hoveredItem);
    }

    [AutoMappedType]
    public class ItemStatCompareService : IItemStatCompareService
    {
        private readonly ICharacterProvider _characterProvider;
        private readonly IPaperdollProvider _paperdollProvider;
        private readonly IPubFileProvider _pubFileProvider;

        public ItemStatCompareService(ICharacterProvider characterProvider,
                                      IPaperdollProvider paperdollProvider,
                                      IPubFileProvider pubFileProvider)
        {
            _characterProvider = characterProvider;
            _paperdollProvider = paperdollProvider;
            _pubFileProvider = pubFileProvider;
        }

        public IReadOnlyList<ItemStatCompareLine> GetInventoryHoverLines(EIFRecord hoveredItem)
        {
            if (hoveredItem == null || hoveredItem.ID <= 0)
                return Array.Empty<ItemStatCompareLine>();

            var lines = new List<ItemStatCompareLine>();
            var equipped = GetEquippedItemForCompare(hoveredItem);
            var canCompare = equipped != null && hoveredItem.GetEquipLocation() != EquipLocation.PAPERDOLL_MAX;

            AddRangeStat(lines, "ATT", hoveredItem.MinDam, hoveredItem.MaxDam,
                equipped?.MinDam ?? 0, equipped?.MaxDam ?? 0, canCompare);
            AddStat(lines, "ACC", hoveredItem.Accuracy, equipped?.Accuracy ?? 0, canCompare);
            AddStat(lines, "DEF", hoveredItem.Armor, equipped?.Armor ?? 0, canCompare);
            AddStat(lines, "EVA", hoveredItem.Evade, equipped?.Evade ?? 0, canCompare);
            AddStat(lines, "HP", hoveredItem.HP, equipped?.HP ?? 0, canCompare);
            AddStat(lines, "TP", hoveredItem.TP, equipped?.TP ?? 0, canCompare);
            AddStat(lines, "STR", hoveredItem.Str, equipped?.Str ?? 0, canCompare);
            AddStat(lines, "INT", hoveredItem.Int, equipped?.Int ?? 0, canCompare);
            AddStat(lines, "WIS", hoveredItem.Wis, equipped?.Wis ?? 0, canCompare);
            AddStat(lines, "AGI", hoveredItem.Agi, equipped?.Agi ?? 0, canCompare);
            AddStat(lines, "CON", hoveredItem.Con, equipped?.Con ?? 0, canCompare);
            AddStat(lines, "CHA", hoveredItem.Cha, equipped?.Cha ?? 0, canCompare);

            return lines;
        }

        private EIFRecord GetEquippedItemForCompare(EIFRecord hoveredItem)
        {
            var equipLocation = hoveredItem.GetEquipLocation();
            if (equipLocation == EquipLocation.PAPERDOLL_MAX)
                return null;

            var characterId = _characterProvider.MainCharacter.ID;
            if (!_paperdollProvider.VisibleCharacterPaperdolls.TryGetValue(characterId, out var paperdollData))
                return null;

            var paperdoll = paperdollData.Paperdoll;
            if (!paperdoll.TryGetValue(equipLocation, out var equippedId) || equippedId == 0)
            {
                // Rings / armlets / bracers have a second slot
                if (equipLocation is EquipLocation.Ring1 or EquipLocation.Armlet1 or EquipLocation.Bracer1)
                {
                    var alt = equipLocation + 1;
                    if (!paperdoll.TryGetValue(alt, out equippedId) || equippedId == 0)
                        return null;
                }
                else
                {
                    return null;
                }
            }

            if (equippedId == hoveredItem.ID)
                return hoveredItem;

            return _pubFileProvider.EIFFile[equippedId];
        }

        private static void AddStat(List<ItemStatCompareLine> lines, string label, int hovered, int equipped, bool canCompare)
        {
            if (hovered == 0 && equipped == 0)
                return;

            if (!canCompare)
            {
                if (hovered != 0)
                    lines.Add(new ItemStatCompareLine($"{label} {hovered}", ItemStatCompareKind.Neutral));
                return;
            }

            var diff = hovered - equipped;
            var kind = diff > 0 ? ItemStatCompareKind.Better
                : diff < 0 ? ItemStatCompareKind.Worse
                : ItemStatCompareKind.Equal;

            var diffText = diff > 0 ? $"(+{diff})" : diff < 0 ? $"({diff})" : "(=)";
            lines.Add(new ItemStatCompareLine($"{label} {hovered} {diffText}", kind));
        }

        private static void AddRangeStat(List<ItemStatCompareLine> lines, string label,
            int hoveredMin, int hoveredMax, int equippedMin, int equippedMax, bool canCompare)
        {
            if (hoveredMin == 0 && hoveredMax == 0 && equippedMin == 0 && equippedMax == 0)
                return;

            if (!canCompare)
            {
                if (hoveredMin != 0 || hoveredMax != 0)
                    lines.Add(new ItemStatCompareLine($"{label} {hoveredMin}-{hoveredMax}", ItemStatCompareKind.Neutral));
                return;
            }

            var minDiff = hoveredMin - equippedMin;
            var maxDiff = hoveredMax - equippedMax;

            ItemStatCompareKind kind;
            if (minDiff >= 0 && maxDiff >= 0 && (minDiff > 0 || maxDiff > 0))
                kind = ItemStatCompareKind.Better;
            else if (minDiff <= 0 && maxDiff <= 0 && (minDiff < 0 || maxDiff < 0))
                kind = ItemStatCompareKind.Worse;
            else if (minDiff == 0 && maxDiff == 0)
                kind = ItemStatCompareKind.Equal;
            else
                kind = minDiff + maxDiff >= 0 ? ItemStatCompareKind.Better : ItemStatCompareKind.Worse;

            string Format(int d) => d > 0 ? $"+{d}" : d.ToString();
            var diffText = (minDiff == 0 && maxDiff == 0)
                ? "(=)"
                : $"({Format(minDiff)}-{Format(maxDiff)})";

            lines.Add(new ItemStatCompareLine($"{label} {hoveredMin}-{hoveredMax} {diffText}", kind));
        }
    }
}
