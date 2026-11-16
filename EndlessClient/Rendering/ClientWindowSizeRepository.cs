using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        void SyncBackBufferWithWindow();
        void MaximizeWindowed();
        void RestoreWindowed(int width, int height);
        bool IsMaximizedWindowed { get; }

        /// <summary>
        /// Call once per frame. MonoGame often skips ClientSizeChanged on maximize,
        /// leaving ClientBounds / PreferredBackBuffer stuck at 640x480 (black void).
        /// </summary>
        void MaintainFrame();
    }

    [AutoMappedType(IsSingleton = true)]
    public class ClientWindowSizeRepository : IClientWindowSizeProvider, IClientWindowSizeRepository, IResettable
    {
        public const int DEFAULT_BACKBUFFER_WIDTH = 640;
        public const int DEFAULT_BACKBUFFER_HEIGHT = 480;

        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;

        private readonly IGameWindowRepository _gameWindowRepository;
        private readonly IGraphicsDeviceRepository _graphicsDeviceRepository;

        private readonly List<EventHandler<EventArgs>> _resizeEvents;
        private bool _syncing;
        private bool _keepMaximized;
#if !LINUX && !OSX
        private bool _formHooksAttached;
#endif
        private int _pendingMaximizeFrames;
        private int _lastSeenClientW;
        private int _lastSeenClientH;

        public int Width
        {
            get => GetRealClientWidth();
            set
            {
                if (value < DEFAULT_BACKBUFFER_WIDTH)
                    value = DEFAULT_BACKBUFFER_WIDTH;

                var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
                if (gdm.PreferredBackBufferWidth == value
                    && PresentationMatches(value, gdm.PreferredBackBufferHeight))
                    return;

                gdm.PreferredBackBufferWidth = value;
                gdm.ApplyChanges();
                ReassertMaximizedIfNeeded();
            }
        }

        public int Height
        {
            get => GetRealClientHeight();
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
                ReassertMaximizedIfNeeded();
            }
        }

        public bool Resizable
        {
            get => _gameWindowRepository.Window.AllowUserResizing;
            set
            {
                _gameWindowRepository.Window.AllowUserResizing = value;
                ApplyStandardWindowChrome();
                EnsureFormHooks();
            }
        }

        public bool IsMaximizedWindowed
        {
            get
            {
                if (_keepMaximized)
                    return true;
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
            _keepMaximized = true;
            _pendingMaximizeFrames = 45; // ~0.5s of MaintainFrame retries

            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (gdm != null)
                gdm.IsFullScreen = false;

            var window = _gameWindowRepository.Window;
            if (window == null)
                return;

            window.IsBorderless = false;
            window.AllowUserResizing = true;
            ApplyStandardWindowChrome();
            EnsureFormHooks();

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null)
            {
                // Do not rely on Maximize alone — ApplyChanges undoes it and ClientSizeChanged
                // often does not fire. Explicitly fill the working area, sync the backbuffer to
                // Form.ClientSize (not stale GameWindow.ClientBounds), then maximize.
                var working = Screen.FromHandle(form.Handle).WorkingArea;

                form.SuspendLayout();
                form.WindowState = FormWindowState.Normal;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MaximizeBox = true;
                form.MinimizeBox = true;
                form.ControlBox = true;
                form.Bounds = working;
                form.ResumeLayout(true);

                ApplyBackBufferSize(form.ClientSize.Width, form.ClientSize.Height);

                NativeShowWindow(form.Handle, SW_MAXIMIZE);
                if (form.WindowState != FormWindowState.Maximized)
                    form.WindowState = FormWindowState.Maximized;

                if (NativeGetClientRect(form.Handle, out var rect))
                {
                    ApplyBackBufferSize(
                        Math.Max(DEFAULT_BACKBUFFER_WIDTH, rect.Right - rect.Left),
                        Math.Max(DEFAULT_BACKBUFFER_HEIGHT, rect.Bottom - rect.Top));
                }
                else
                {
                    ApplyBackBufferSize(form.ClientSize.Width, form.ClientSize.Height);
                }

                NativeShowWindow(form.Handle, SW_MAXIMIZE);
                return;
            }

            // Form handle not ready yet — size from Win32 work area and retry via MaintainFrame.
            if (window.Handle != IntPtr.Zero && TryGetMonitorWorkArea(window.Handle, out var workW, out var workH))
            {
                ApplyBackBufferSize(workW, workH);
                NativeShowWindow(window.Handle, SW_MAXIMIZE);
                return;
            }
#endif

            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            ApplyBackBufferSize(
                Math.Max(DEFAULT_BACKBUFFER_WIDTH, display.Width - 16),
                Math.Max(DEFAULT_BACKBUFFER_HEIGHT, display.Height - 80));
        }

        public void RestoreWindowed(int width, int height)
        {
            _keepMaximized = false;
            _pendingMaximizeFrames = 0;

            width = Math.Max(DEFAULT_BACKBUFFER_WIDTH, width);
            height = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, height);

            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (gdm != null)
                gdm.IsFullScreen = false;

            var window = _gameWindowRepository.Window;
            if (window != null)
            {
                window.IsBorderless = false;
                window.AllowUserResizing = true;
            }

            ApplyStandardWindowChrome();

#if !LINUX && !OSX
            var hwnd = window?.Handle ?? IntPtr.Zero;
            if (hwnd != IntPtr.Zero)
                NativeShowWindow(hwnd, SW_RESTORE);

            var form = TryGetForm();
            if (form != null)
            {
                form.WindowState = FormWindowState.Normal;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.ClientSize = new System.Drawing.Size(width, height);
                ApplyBackBufferSize(form.ClientSize.Width, form.ClientSize.Height);
                return;
            }
#endif

            ApplyBackBufferSize(width, height);
        }

        public void SyncBackBufferWithWindow()
        {
            if (_syncing)
                return;

            var window = _gameWindowRepository.Window;
            if (window == null)
                return;

            int targetW;
            int targetH;

            if (!Resizable)
            {
                targetW = DEFAULT_BACKBUFFER_WIDTH;
                targetH = DEFAULT_BACKBUFFER_HEIGHT;
            }
            else if (!TryGetRealClientSize(out targetW, out targetH))
            {
                targetW = Math.Max(DEFAULT_BACKBUFFER_WIDTH, window.ClientBounds.Width);
                targetH = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, window.ClientBounds.Height);
            }

            ApplyBackBufferSize(targetW, targetH);
            ReassertMaximizedIfNeeded();
        }

        public void MaintainFrame()
        {
            EnsureFormHooks();

            if (!Resizable)
                return;

            if (_pendingMaximizeFrames > 0)
            {
                _pendingMaximizeFrames--;
                ApplyMaximizePass();
            }
            else if (_keepMaximized)
            {
                ReassertMaximizedIfNeeded();
            }

            if (!TryGetRealClientSize(out var clientW, out var clientH))
                return;

            if (clientW == _lastSeenClientW && clientH == _lastSeenClientH)
            {
                // Still verify presentation — alt-tab can desync without changing Form size.
                var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
                if (gdm != null
                    && gdm.PreferredBackBufferWidth == clientW
                    && gdm.PreferredBackBufferHeight == clientH
                    && PresentationMatches(clientW, clientH))
                {
                    return;
                }
            }

            _lastSeenClientW = clientW;
            _lastSeenClientH = clientH;
            ApplyBackBufferSize(clientW, clientH);
            ReassertMaximizedIfNeeded();
        }

        private void ApplyMaximizePass()
        {
#if !LINUX && !OSX
            var form = TryGetForm();
            if (form == null)
            {
                var window = _gameWindowRepository.Window;
                if (window?.Handle != IntPtr.Zero)
                    NativeShowWindow(window.Handle, SW_MAXIMIZE);
                return;
            }

            if (form.WindowState != FormWindowState.Maximized)
            {
                var working = Screen.FromHandle(form.Handle).WorkingArea;
                if (form.WindowState == FormWindowState.Normal
                    && (form.Bounds.Width < working.Width - 32 || form.Bounds.Height < working.Height - 32))
                {
                    form.Bounds = working;
                }

                NativeShowWindow(form.Handle, SW_MAXIMIZE);
                form.WindowState = FormWindowState.Maximized;
            }

            if (NativeGetClientRect(form.Handle, out var rect))
            {
                ApplyBackBufferSize(
                    Math.Max(DEFAULT_BACKBUFFER_WIDTH, rect.Right - rect.Left),
                    Math.Max(DEFAULT_BACKBUFFER_HEIGHT, rect.Bottom - rect.Top));
            }
            else
            {
                ApplyBackBufferSize(form.ClientSize.Width, form.ClientSize.Height);
            }
#endif
        }

        private void ApplyBackBufferSize(int targetW, int targetH)
        {
            if (_syncing)
                return;

            var gdm = _graphicsDeviceRepository.GraphicsDeviceManager;
            if (gdm == null)
                return;

            targetW = Math.Max(DEFAULT_BACKBUFFER_WIDTH, targetW);
            targetH = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, targetH);

            _syncing = true;
            try
            {
                var presentationMismatch = !PresentationMatches(targetW, targetH);
                var preferredMismatch =
                    gdm.PreferredBackBufferWidth != targetW ||
                    gdm.PreferredBackBufferHeight != targetH;

                if (presentationMismatch || preferredMismatch)
                {
                    gdm.IsFullScreen = false;
                    gdm.PreferredBackBufferWidth = targetW;
                    gdm.PreferredBackBufferHeight = targetH;
                    gdm.ApplyChanges();
                }

                _lastSeenClientW = targetW;
                _lastSeenClientH = targetH;

                foreach (var evnt in _resizeEvents)
                    evnt(this, EventArgs.Empty);
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ReassertMaximizedIfNeeded()
        {
            if (!_keepMaximized)
                return;

            var window = _gameWindowRepository.Window;
            if (window?.Handle == null || window.Handle == IntPtr.Zero)
                return;

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null && form.WindowState == FormWindowState.Maximized)
                return;

            NativeShowWindow(window.Handle, SW_MAXIMIZE);
            if (form != null && form.WindowState != FormWindowState.Maximized)
                form.WindowState = FormWindowState.Maximized;
#endif
        }

        private void ApplyStandardWindowChrome()
        {
#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null)
            {
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MaximizeBox = true;
                form.MinimizeBox = true;
                form.ControlBox = true;
            }
#endif
            var window = _gameWindowRepository.Window;
            if (window != null)
                window.IsBorderless = false;
        }

        private void EnsureFormHooks()
        {
#if !LINUX && !OSX
            if (_formHooksAttached)
                return;

            var form = TryGetForm();
            if (form == null)
                return;

            form.SizeChanged += (_, _) =>
            {
                if (_syncing)
                    return;
                SyncBackBufferWithWindow();
            };
            form.Resize += (_, _) =>
            {
                if (_syncing)
                    return;
                SyncBackBufferWithWindow();
            };
            _formHooksAttached = true;
#endif
        }

        private int GetRealClientWidth()
        {
            if (TryGetRealClientSize(out var w, out _))
                return w;
            return _gameWindowRepository.Window.ClientBounds.Width;
        }

        private int GetRealClientHeight()
        {
            if (TryGetRealClientSize(out _, out var h))
                return h;
            return _gameWindowRepository.Window.ClientBounds.Height;
        }

        private bool TryGetRealClientSize(out int width, out int height)
        {
            width = 0;
            height = 0;

#if !LINUX && !OSX
            var form = TryGetForm();
            if (form != null && form.ClientSize.Width > 0 && form.ClientSize.Height > 0)
            {
                width = form.ClientSize.Width;
                height = form.ClientSize.Height;
                return true;
            }

            var window = _gameWindowRepository.Window;
            if (window?.Handle != IntPtr.Zero && NativeGetClientRect(window.Handle, out var rect))
            {
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
                if (width > 0 && height > 0)
                    return true;
            }
#endif
            return false;
        }

#if !LINUX && !OSX
        private Form TryGetForm()
        {
            var window = _gameWindowRepository.Window;
            if (window == null || window.Handle == IntPtr.Zero)
                return null;

            if (Control.FromHandle(window.Handle) is Form fromHandle)
                return fromHandle;

            foreach (Form open in Application.OpenForms)
            {
                if (open.Handle == window.Handle)
                    return open;
            }

            return null;
        }

        private static bool TryGetMonitorWorkArea(IntPtr hwnd, out int width, out int height)
        {
            width = 0;
            height = 0;
            var monitor = NativeMonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
            if (monitor == IntPtr.Zero)
                return false;

            var info = new NativeMonitorInfo();
            info.cbSize = Marshal.SizeOf<NativeMonitorInfo>();
            if (!NativeGetMonitorInfo(monitor, ref info))
                return false;

            width = Math.Max(DEFAULT_BACKBUFFER_WIDTH, info.rcWork.Right - info.rcWork.Left);
            height = Math.Max(DEFAULT_BACKBUFFER_HEIGHT, info.rcWork.Bottom - info.rcWork.Top);
            return true;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo lpmi);

        private static bool NativeShowWindow(IntPtr hWnd, int cmd) => ShowWindow(hWnd, cmd);

        private static bool NativeGetClientRect(IntPtr hWnd, out NativeRect rect) => GetClientRect(hWnd, out rect);

        private static IntPtr NativeMonitorFromWindow(IntPtr hwnd, uint flags) => MonitorFromWindow(hwnd, flags);

        private static bool NativeGetMonitorInfo(IntPtr monitor, ref NativeMonitorInfo info) => GetMonitorInfo(monitor, ref info);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NativeMonitorInfo
        {
            public int cbSize;
            public NativeRect rcMonitor;
            public NativeRect rcWork;
            public uint dwFlags;
        }
#else
        private static bool NativeShowWindow(IntPtr hWnd, int cmd) => false;
        private static bool NativeGetClientRect(IntPtr hWnd, out NativeRect rect)
        {
            rect = default;
            return false;
        }

        private struct NativeRect
        {
            public int Left, Top, Right, Bottom;
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
            _keepMaximized = false;
            _pendingMaximizeFrames = 0;
            _lastSeenClientW = 0;
            _lastSeenClientH = 0;

            foreach (var evnt in _resizeEvents)
                GameWindowSizeChanged -= evnt;
            _resizeEvents.Clear();

#if !LINUX && !OSX
            var window = _gameWindowRepository.Window;
            if (window?.Handle != null && window.Handle != IntPtr.Zero)
                NativeShowWindow(window.Handle, SW_RESTORE);

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
