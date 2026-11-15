using AutomaticTypeMapper;
using EndlessClient.Audio;
using EndlessClient.GameExecution;
using EndlessClient.Input;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.CharacterProperties;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Effects;
using EndlessClient.Rendering.Metadata;
using EndlessClient.Rendering.Metadata.Models;
using EndlessClient.Rendering.Sprites;
using EOLib.Domain.Character;
using EOLib.Domain.Map;
using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering.Factories
{
    [MappedType(BaseType = typeof(ICharacterRendererFactory))]
    public class CharacterRendererFactory : ICharacterRendererFactory
    {
        private readonly IEndlessGameProvider _gameProvider;
        private readonly IRenderTargetFactory _renderTargetFactory;
        private readonly IHealthBarRendererFactory _healthBarRendererFactory;
        private readonly IChatBubbleFactory _chatBubbleFactory;
        private readonly ICharacterProvider _characterProvider;
        private readonly IRenderOffsetCalculator _renderOffsetCalculator;
        private readonly ICharacterPropertyRendererBuilder _characterPropertyRendererBuilder;
        private readonly ICharacterSpriteCalculator _characterSpriteCalculator;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly IUserInputProvider _userInputProvider;
        private readonly IEffectRendererFactory _effectRendererFactory;
        private readonly IMetadataProvider<HatMetadata> _hatMetadataProvider;
        private readonly IMetadataProvider<WeaponMetadata> _weaponMetadataProvider;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IClientWindowSizeRepository _clientWindowSizeRepository;

        public CharacterRendererFactory(IEndlessGameProvider gameProvider,
                                        IRenderTargetFactory renderTargetFactory,
                                        IHealthBarRendererFactory healthBarRendererFactory,
                                        IChatBubbleFactory chatBubbleFactory,
                                        ICharacterProvider characterProvider,
                                        IRenderOffsetCalculator renderOffsetCalculator,
                                        ICharacterPropertyRendererBuilder characterPropertyRendererBuilder,
                                        ICharacterSpriteCalculator characterSpriteCalculator,
                                        ICurrentMapProvider currentMapProvider,
                                        IUserInputProvider userInputProvider,
                                        IEffectRendererFactory effectRendererFactory,
                                        IMetadataProvider<HatMetadata> hatMetadataProvider,
                                        IMetadataProvider<WeaponMetadata> weaponMetadataProvider,
                                        ISfxPlayer sfxPlayer,
                                        IClientWindowSizeRepository clientWindowSizeRepository)
        {
            _gameProvider = gameProvider;
            _renderTargetFactory = renderTargetFactory;
            _healthBarRendererFactory = healthBarRendererFactory;
            _chatBubbleFactory = chatBubbleFactory;
            _characterProvider = characterProvider;
            _renderOffsetCalculator = renderOffsetCalculator;
            _characterPropertyRendererBuilder = characterPropertyRendererBuilder;
            _characterSpriteCalculator = characterSpriteCalculator;
            _currentMapProvider = currentMapProvider;
            _userInputProvider = userInputProvider;
            _effectRendererFactory = effectRendererFactory;
            _hatMetadataProvider = hatMetadataProvider;
            _weaponMetadataProvider = weaponMetadataProvider;
            _sfxPlayer = sfxPlayer;
            _clientWindowSizeRepository = clientWindowSizeRepository;
        }

        public ICharacterRenderer CreateCharacterRenderer(EOLib.Domain.Character.Character character, bool isUiControl)
        {
            // Each renderer needs its own texture cache. Sharing one instance across login-slot
            // previews made slots fight over Refresh() and caused flickering/bouncing sprites.
            var characterTextures = new CharacterTextures(_characterSpriteCalculator);

            return new CharacterRenderer(
                (Game)_gameProvider.Game,
                _renderTargetFactory,
                _healthBarRendererFactory,
                _chatBubbleFactory,
                _characterProvider,
                _renderOffsetCalculator,
                _characterPropertyRendererBuilder,
                characterTextures,
                _currentMapProvider,
                _userInputProvider,
                _effectRendererFactory,
                _hatMetadataProvider,
                _weaponMetadataProvider,
                _sfxPlayer,
                _clientWindowSizeRepository,
                character,
                isUiControl);
        }
    }
}
