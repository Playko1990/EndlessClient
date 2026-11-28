using System;
using AutomaticTypeMapper;
using EOLib.IO.Pub;

namespace EOLib.Domain.Item
{
    [AutoMappedType]
    public class ItemStringService : IItemStringService
    {
        public string GetStringForMapDisplay(EIFRecord record, int amount)
        {
            if (record == null)
                return string.Empty;

            if (amount <= 0)
                return record.Name;

            return GetStringForInventoryDisplay(record, amount);
        }

        public string GetStringForInventoryDisplay(EIFRecord record, int amount)
        {
            if (record.ID == 1)
                return $"{amount} {record.Name}";

            return amount == 1 ? record.Name : $"{record.Name} x{amount}";
        }
    }

    public interface IItemStringService
    {
        string GetStringForMapDisplay(EIFRecord record, int amount);

        string GetStringForInventoryDisplay(EIFRecord record, int amount);
    }
}
