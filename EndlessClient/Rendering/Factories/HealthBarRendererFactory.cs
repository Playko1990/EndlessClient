using AutomaticTypeMapper;
using EndlessClient.GameExecution;
using EndlessClient.Rendering.Character;
using EOLib.Graphics;

namespace EndlessClient.Rendering.Factories
{
    [AutoMappedType]
    public class HealthBarRendererFactory : IHealthBarRendererFactory
    {
        private readonly IEndlessGameProvider _endlessGameProvider;
        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly ICharacterRendererProvider _characterRendererProvider;

        public HealthBarRendererFactory(IEndlessGameProvider endlessGameProvider,
                                        INativeGraphicsManager nativeGraphicsManager,
                                        ICharacterRendererProvider characterRendererProvider)
        {
            _endlessGameProvider = endlessGameProvider;
            _nativeGraphicsManager = nativeGraphicsManager;
            _characterRendererProvider = characterRendererProvider;
        }

        public IHealthBarRenderer CreateHealthBarRenderer(IMapActor parentReference)
        {
            return new HealthBarRenderer(
                _endlessGameProvider,
                _nativeGraphicsManager,
                _characterRendererProvider,
                parentReference);
        }
    }

    public interface IHealthBarRendererFactory
    {
        IHealthBarRenderer CreateHealthBarRenderer(IMapActor entity);
    }
}
