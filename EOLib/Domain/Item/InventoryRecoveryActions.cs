using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomaticTypeMapper;
using EOLib.Domain.Character;
using EOLib.IO.Extensions;
using EOLib.IO.Repositories;
using EOLib.Shared;

namespace EOLib.Domain.Item
{
    public interface IInventoryRecoveryActions
    {
        /// <summary>
        /// Remove broken/unknown inventory items locally before the HUD loads,
        /// and queue them to be junked on the server once in-game.
        /// </summary>
        void QuarantineBrokenItems();

        /// <summary>
        /// Send junk packets for anything quarantined during login.
        /// </summary>
        void FlushPendingJunk();
    }

    [AutoMappedType]
    public class InventoryRecoveryActions : IInventoryRecoveryActions
    {
        private readonly ICharacterInventoryRepository _characterInventoryRepository;
        private readonly IInventoryRecoveryRepository _inventoryRecoveryRepository;
        private readonly IEIFFileProvider _eifFileProvider;
        private readonly IItemActions _itemActions;

        public InventoryRecoveryActions(ICharacterInventoryRepository characterInventoryRepository,
                                        IInventoryRecoveryRepository inventoryRecoveryRepository,
                                        IEIFFileProvider eifFileProvider,
                                        IItemActions itemActions)
        {
            _characterInventoryRepository = characterInventoryRepository;
            _inventoryRecoveryRepository = inventoryRecoveryRepository;
            _eifFileProvider = eifFileProvider;
            _itemActions = itemActions;
        }

        public void QuarantineBrokenItems()
        {
            _inventoryRecoveryRepository.PendingJunk = new List<InventoryItem>();

            var stripIds = LoadStripItemIds();
            var eif = _eifFileProvider.EIFFile;

            var toRemove = _characterInventoryRepository.ItemInventory
                .Where(item => item.ItemID > 1)
                .Where(item =>
                    stripIds.Contains(item.ItemID) ||
                    eif == null ||
                    !eif.ContainsId(item.ItemID))
                .ToList();

            foreach (var item in toRemove)
            {
                _characterInventoryRepository.ItemInventory.Remove(item);
                _inventoryRecoveryRepository.PendingJunk.Add(item);
            }
        }

        public void FlushPendingJunk()
        {
            foreach (var item in _inventoryRecoveryRepository.PendingJunk.ToList())
            {
                try
                {
                    _itemActions.JunkItem(item.ItemID, item.Amount);
                }
                catch
                {
                    // Best-effort — local inventory is already clear so login stays usable.
                }
            }

            _inventoryRecoveryRepository.PendingJunk.Clear();
        }

        private static HashSet<int> LoadStripItemIds()
        {
            var ids = new HashSet<int>();
            var path = Constants.StripItemsFile;

            // Always include 664 as emergency recovery for the stuck Crimson spawn case.
            ids.Add(664);

            if (!File.Exists(path))
                return ids;

            try
            {
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    if (int.TryParse(line, out var id) && id > 1)
                        ids.Add(id);
                }
            }
            catch
            {
                // Keep the built-in defaults if the file can't be read.
            }

            return ids;
        }
    }
}
