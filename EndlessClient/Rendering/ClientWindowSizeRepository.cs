using System;
using System.Collections.Generic;
using AutomaticTypeMapper;
using EOLib;
using EOLib.Graphics;

namespace EndlessClient.Rendering
{
    public interface IClientWindowSizeProvider
    {
        int Width { get; }
        int Height { get; }

        bool Resizable { get; }

        event EventHandler<EventArgs> GameWindowSizeChanged;
    }

    public interface IClientWindowSizeRepository : IResettable, IClientWindowSizeProvider
    {
        new int Width { get; set; }
        new int Height { get; set; }

        new bool Resizable { get; set; }
    }

    [AutoMappedType(IsSingleton = true)]
    public class ClientWindowSizeRepository : IClientWindowSizeProvider, IClientWindowSizeRepository, IResettable
    {
        public const int DEFAULT_BACKBUFFER_WIDTH = 640;
        public const int DEFAULT_BACKBUFFER_HEIGHT = 480;

        private readonly IGameWindowRepository _gameWindowRepository;
        private readonly IGraphicsDeviceRepository _graphicsDeviceRepository;

        private readonly List<EventHandler<EventArgs>> _resizeEvents;

        public int Width
        {
            get => _gameWindowRepository.Window.ClientBounds.Width;
            set
            {
                if (value < DEFAULT_BACKBUFFER_WIDTH)
                    value = DEFAULT_BACKBUFFER_WIDTH;

                var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
                // No-op when already set — ApplyChanges every frame (or on every ClientSizeChanged)
                // thrash-resizes the window and looks like the client bouncing on startup.
                if (gdm.PreferredBackBufferWidth == value)
                    return;

                gdm.PreferredBackBufferWidth = value;
                gdm.ApplyChanges();
                // ClientSizeChanged already notifies GameWindowSizeChanged subscribers; do not
                // invoke _resizeEvents again here (that double-fired the same handlers).
            }
        }

        public int Height
        {
            get => _gameWindowRepository.Window.ClientBounds.Height;
            set
            {
                if (value < DEFAULT_BACKBUFFER_HEIGHT)
                    value = DEFAULT_BACKBUFFER_HEIGHT;

                var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
                if (gdm.PreferredBackBufferHeight == value)
                    return;

                gdm.PreferredBackBufferHeight = value;
                gdm.ApplyChanges();
            }
        }

        public bool Resizable
        {
            get => _gameWindowRepository.Window.AllowUserResizing;
            set => _gameWindowRepository.Window.AllowUserResizing = value;
        }

        public event EventHandler<EventArgs> GameWindowSizeChanged
        {
            add
            {
                _gameWindowRepository.Window.ClientSizeChanged += value;
                _resizeEvents.Add(value);
            }
            remove
            {
                _gameWindowRepository.Window.ClientSizeChanged -= value;
                _resizeEvents.Remove(value);
            }
        }

        public ClientWindowSizeRepository(IGameWindowRepository gameWindowRepository,
                                          IGraphicsDeviceRepository graphicsDeviceRepository)
        {
            _gameWindowRepository = gameWindowRepository;
            _graphicsDeviceRepository = graphicsDeviceRepository;
            _resizeEvents = new List<EventHandler<EventArgs>>();
        }

        public void ResetState()
        {
            foreach (var evnt in _resizeEvents)
                GameWindowSizeChanged -= evnt;
            _resizeEvents.Clear();

            Resizable = false;

            Width = DEFAULT_BACKBUFFER_WIDTH;
            Height = DEFAULT_BACKBUFFER_HEIGHT;
        }
    }
}
