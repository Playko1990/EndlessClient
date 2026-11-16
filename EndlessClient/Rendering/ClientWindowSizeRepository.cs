using System;
using System.Collections.Generic;
using AutomaticTypeMapper;
using EOLib;
using EOLib.Graphics;
using Microsoft.Xna.Framework.Graphics;

#if !LINUX && !OSX
using System.Windows.Forms;
#endif

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

        /// <summary>
        /// Force the backbuffer (and fixed-size window) back in sync after alt-tab / minimize.
        /// </summary>
        void SyncBackBufferWithWindow();

        /// <summary>
        /// Maximize as a normal windowed window: keeps the OS title bar (min/max/close)
        /// and leaves the taskbar visible. Not exclusive fullscreen.
        /// </summary>
        void MaximizeWindowed();

        /// <summary>
        /// Restore from maximized to a specific client size (still bordered/resizable).
        /// </summary>
        void RestoreWindowed(int width, int height);

        bool IsMaximizedWindowed { get; }
    }

    [AutoMappedType(IsSingleton = true)]
    public class ClientWindowSizeRepository : IClientWindowSizeProvider, IClientWindowSizeRepository, IResettable
    {
        public const int DEFAULT_BACKBUFFER_WIDTH = 640;
        public const int DEFAULT_BACKBUFFER_HEIGHT = 480;

        private readonly IGameWindowRepository _gameWindowRepository;
        private readonly IGraphicsDeviceRepository _graphicsDeviceRepository;

        private readonly List<EventHandler<EventArgs>> _resizeEvents;
        private bool _syncing;

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
                if (gdm.PreferredBackBufferWidth == value
                    && PresentationMatches(value, gdm.PreferredBackBufferHeight))
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
                if (gdm.PreferredBackBufferHeight == value
                    && PresentationMatches(gdm.PreferredBackBufferWidth, value))
                    return;

                gdm.PreferredBackBufferHeight = value;
                gdm.ApplyChanges();
            }
        }

        public bool Resizable
        {
            get => _gameWindowRepository.Window.AllowUserResizing;
            set
            {
                _gameWindowRepository.Window.AllowUserResizing = value;
                ApplyStandardWindowChrome();
            }
        }

        public bool IsMaximizedWindowed
        {
            get
            {
#if !LINUX && !OSX
                var form = TryGetForm();
                return form != null && form.WindowState == FormWindowState.Maximized;
#else
                return false;
#endif
            }
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

        public void MaximizeWindowed()
        {
            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (gdm != null)
            {
                gdm.IsFullScreen = false;
                gdm.ApplyChanges();
            }

            var window = _gameWindowRepository.Window;
            if (window != null)
            {
                window.IsBorderless = false;
                window.AllowUserResizing = true;
            }

            ApplyStandardWindowChrome();

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null)
            {
                // Maximized fills the working area: taskbar stays, title bar keeps X / min / max.
                form.WindowState = FormWindowState.Maximized;
                SyncBackBufferWithWindow();
                return;
            }
#endif

            // Fallback (non-Windows): size to a typical working area, not raw DisplayMode.
            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            Width = Math.Max(DEFAULT_BACKBUFFER_WIDTH, display.Width - 16);
            Height = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, display.Height - 80);
            SyncBackBufferWithWindow();
        }

        public void RestoreWindowed(int width, int height)
        {
            width = Math.Max(DEFAULT_BACKBUFFER_WIDTH, width);
            height = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, height);

            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (gdm != null)
            {
                gdm.IsFullScreen = false;
                gdm.ApplyChanges();
            }

            var window = _gameWindowRepository.Window;
            if (window != null)
            {
                window.IsBorderless = false;
                window.AllowUserResizing = true;
            }

            ApplyStandardWindowChrome();

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null)
            {
                form.WindowState = FormWindowState.Normal;
                form.ClientSize = new System.Drawing.Size(width, height);
                SyncBackBufferWithWindow();
                return;
            }
#endif

            Width = width;
            Height = height;
            SyncBackBufferWithWindow();
        }

        public void SyncBackBufferWithWindow()
        {
            if (_syncing)
                return;

            var window = _gameWindowRepository.Window;
            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (window == null || gdm == null)
                return;

            _syncing = true;
            try
            {
                int targetW;
                int targetH;

                if (!Resizable)
                {
                    // Pre-game / fixed mode must stay 640x480. Alt-tab can inflate ClientBounds
                    // while the backbuffer stays small — black bars + dead clicks.
                    targetW = DEFAULT_BACKBUFFER_WIDTH;
                    targetH = DEFAULT_BACKBUFFER_HEIGHT;
                }
                else
                {
                    targetW = Math.Max(DEFAULT_BACKBUFFER_WIDTH, window.ClientBounds.Width);
                    targetH = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, window.ClientBounds.Height);
                }

                var presentationMismatch = !PresentationMatches(targetW, targetH);
                var preferredMismatch =
                    gdm.PreferredBackBufferWidth != targetW ||
                    gdm.PreferredBackBufferHeight != targetH;
                var clientMismatch =
                    window.ClientBounds.Width != targetW ||
                    window.ClientBounds.Height != targetH;

                if (!presentationMismatch && !preferredMismatch && (!clientMismatch || Resizable))
                    return;

                gdm.PreferredBackBufferWidth = targetW;
                gdm.PreferredBackBufferHeight = targetH;
                gdm.ApplyChanges();

                foreach (var evnt in _resizeEvents)
                    evnt(this, EventArgs.Empty);
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ApplyStandardWindowChrome()
        {
#if !LINUX && !OSX
            var form = TryGetForm();
            if (form == null)
                return;

            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimizeBox = true;
            form.ControlBox = true;
#endif
            var window = _gameWindowRepository.Window;
            if (window != null)
                window.IsBorderless = false;
        }

#if !LINUX && !OSX
        private Form TryGetForm()
        {
            var window = _gameWindowRepository.Window;
            if (window == null || window.Handle == IntPtr.Zero)
                return null;

            return Control.FromHandle(window.Handle) as Form;
        }
#endif

        private bool PresentationMatches(int width, int height)
        {
            var gd = _graphicsDeviceRepository.GraphicsDevice;
            if (gd == null)
                return true;

            return gd.PresentationParameters.BackBufferWidth == width
                && gd.PresentationParameters.BackBufferHeight == height;
        }

        public void ResetState()
        {
            foreach (var evnt in _resizeEvents)
                GameWindowSizeChanged -= evnt;
            _resizeEvents.Clear();

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null && form.WindowState == FormWindowState.Maximized)
                form.WindowState = FormWindowState.Normal;
#endif

            Resizable = false;

            Width = DEFAULT_BACKBUFFER_WIDTH;
            Height = DEFAULT_BACKBUFFER_HEIGHT;
        }
    }
}
