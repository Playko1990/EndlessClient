using System;
using System.Collections.Generic;
using System.Diagnostics;
using EndlessClient.GameExecution;
using EndlessClient.Rendering;
using EOLib.Domain.Character;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;

namespace EndlessClient.Network
{
    public class UnknownEntitiesRequester : GameComponent
    {
        private const int UPPER_SEE_DISTANCE = 12;
        private const int LOWER_SEE_DISTANCE = 15;

        private const int REQUEST_INTERVAL_MS = 1000;

        private readonly IClientWindowSizeProvider _clientWindowSizeProvider;
        private readonly ICharacterProvider _characterProvider;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;
        private readonly IUnknownEntitiesRequestActions _unknownEntitiesRequestActions;

        private readonly Stopwatch _requestTimer;

        public UnknownEntitiesRequester(IEndlessGameProvider gameProvider,
                                        IClientWindowSizeProvider clientWindowSizeProvider,
                                        ICharacterProvider characterProvider,
                                        ICurrentMapStateRepository currentMapStateRepository,
                                        IUnknownEntitiesRequestActions unknownEntitiesRequestActions)
            : base((Game)gameProvider.Game)
        {
            _clientWindowSizeProvider = clientWindowSizeProvider;
            _characterProvider = characterProvider;
            _currentMapStateRepository = currentMapStateRepository;
            _unknownEntitiesRequestActions = unknownEntitiesRequestActions;
            _requestTimer = Stopwatch.StartNew();
        }

        public override void Update(GameTime gameTime)
        {
            if (_requestTimer.ElapsedMilliseconds >= REQUEST_INTERVAL_MS)
            {
                ClearOutOfRangeMapItems();

                if (_currentMapStateRepository.UnknownNPCIndexes.Count > 0 && _currentMapStateRepository.UnknownPlayerIDs.Count > 0)
                {
                    _unknownEntitiesRequestActions.RequestAll();
                }
                else if (_currentMapStateRepository.UnknownNPCIndexes.Count > 0)
                {
                    _unknownEntitiesRequestActions.RequestUnknownNPCs();
                }
                else if (_currentMapStateRepository.UnknownPlayerIDs.Count > 0)
                {
                    _unknownEntitiesRequestActions.RequestUnknownPlayers();
                }

                _requestTimer.Restart();
            }

            base.Update(gameTime);
        }

        private void ClearOutOfRangeMapItems()
        {
            var mc = _characterProvider.MainCharacter;

            // Keep NPCs and other players in map state for as long as we know about them so
            // large/resizable viewports do not blank actors that are still on-screen.
            // Server remove packets (Avatar_Remove, death, map change) still clear them.
            // Map items keep the classic distance cull — they are numerous and less critical.
            var seeDistanceUpper = (int)(_clientWindowSizeProvider.Height / 480.0 * UPPER_SEE_DISTANCE);
            var seeDistanceLower = (int)(_clientWindowSizeProvider.Height / 480.0 * LOWER_SEE_DISTANCE);

            var itemsToRemove = new List<MapItem>();
            foreach (var item in _currentMapStateRepository.MapItems)
            {
                var xDiff = Math.Abs(mc.X - item.X);
                var yDiff = Math.Abs(mc.Y - item.Y);

                if (item.X < mc.X || item.Y < mc.Y)
                {
                    if (xDiff + yDiff > seeDistanceUpper)
                        itemsToRemove.Add(item);
                }
                else if (xDiff + yDiff > seeDistanceLower)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (var item in itemsToRemove)
                _currentMapStateRepository.MapItems.Remove(item);
        }
    }
}
