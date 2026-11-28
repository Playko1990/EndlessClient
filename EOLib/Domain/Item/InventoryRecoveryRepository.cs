using System.Collections.Generic;
using AutomaticTypeMapper;
using EOLib.Domain.Character;

namespace EOLib.Domain.Item
{
    public interface IInventoryRecoveryRepository
    {
        List<InventoryItem> PendingJunk { get; set; }
    }

    public interface IInventoryRecoveryProvider
    {
        IReadOnlyList<InventoryItem> PendingJunk { get; }
    }

    [AutoMappedType(IsSingleton = true)]
    public class InventoryRecoveryRepository : IInventoryRecoveryRepository, IInventoryRecoveryProvider
    {
        public List<InventoryItem> PendingJunk { get; set; } = new List<InventoryItem>();

        IReadOnlyList<InventoryItem> IInventoryRecoveryProvider.PendingJunk => PendingJunk;
    }
}
