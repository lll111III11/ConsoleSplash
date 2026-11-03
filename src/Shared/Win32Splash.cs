// ============================================================================
//  ConsoleSplash —— Win32 覆盖层 / Win32 overlay
//
//  不依赖 WinForms，直接在控制台窗口上盖一个无边框置顶窗口。
//  No WinForms dependency: a borderless topmost window is drawn straight on
//  top of the console window with GDI.
//
//  默认动画 / default animation: black screen, a light band sweeps from right
//  to left, then the overlay fades out and reveals the console.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ConsoleSplash.Shared
{
    /// <summary>控制台窗口辅助 / console window helpers.</summary>
    public static class ConsoleWindow
    {
        /// <summary>当前系统是否为 Windows / whether the current OS is Windows.</summary>
        public static bool IsWindows
        {
            get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
        }

        /// <summary>让窗口坐标与屏幕坐标使用同一套物理像素。</summary>
        public static void EnableDpiAwareness()
        {
            try { SetProcessDPIAware(); }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public static IntPtr Handle
        {
            get
            {
                try { return GetConsoleWindow(); }
                catch { return IntPtr.Zero; }
            }
        }

        public static string ClassNameOf(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return string.Empty;
            StringBuilder builder = new StringBuilder(256);
            GetClassName(hwnd, builder, builder.Capacity);
            return builder.ToString();
        }

        /// <summary>窗口标题 / window title, for diagnostics.</summary>
        public static string TitleOfWindow(IntPtr hwnd)
        {
            return TitleOf(hwnd);
        }

        /// <summary>窗口所属进程 / owning process id, for diagnostics.</summary>
        public static uint ProcessIdOf(IntPtr hwnd)
        {
            try
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                return pid;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>只有真正的 conhost 窗口才能安全隐藏；Windows Terminal 的伪控制台不能。</summary>
        public static bool CanHide(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && ClassNameOf(hwnd) == "ConsoleWindowClass";
        }

        /// <summary>
        /// 找控制台窗口；LocalAdmin 启动时服务器进程没有可见 conhost，退而找 LocalAdmin 主窗口。
        /// Find the console window; under LocalAdmin the server has no visible conhost,
        /// so fall back to the LocalAdmin main window.
        /// </summary>
        public static IntPtr FindHostWindow()
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                IntPtr console = SafeConsoleHandle();
                if (console != IntPtr.Zero) return console;

                IntPtr localAdmin = FindWindowByTitle("LocalAdmin");
                if (localAdmin != IntPtr.Zero) return localAdmin;

                IntPtr byProcess = FindProcessMainWindow("LocalAdmin");
                if (byProcess != IntPtr.Zero) return byProcess;

                Thread.Sleep(100);
            }

            return IntPtr.Zero;
        }

        private static IntPtr SafeConsoleHandle()
        {
            try
            {
                IntPtr console = GetConsoleWindow();
                if (console == IntPtr.Zero) return IntPtr.Zero;
                if (!IsWindowVisible(console)) return IntPtr.Zero;
                return console;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 枚举顶层窗口找标题匹配的主窗口。回调内部必须吞掉所有异常：
        /// 异常穿过原生 EnumWindows 边界会让 Unity Mono 直接终止进程。
        /// All exceptions are swallowed inside the native callback: letting one
        /// escape EnumWindows kills the Unity Mono process with no managed trace.
        /// </summary>
        private static IntPtr FindWindowByTitle(string titlePart)
        {
            IntPtr best = IntPtr.Zero;
            long bestArea = 0;

            try
            {
                EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
                {
                    try
                    {
                        if (!IsWindowVisible(hwnd)) return true;
                        if (IsIconic(hwnd)) return true;
                        if (GetWindow(hwnd, 4) != IntPtr.Zero) return true;

                        string title = TitleOf(hwnd);
                        if (string.IsNullOrEmpty(title)) return true;
                        if (title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) < 0)
                            return true;

                        RECT rect;
                        if (!GetWindowRect(hwnd, out rect)) return true;
                        long area = (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top);
                        if (area > bestArea)
                        {
                            bestArea = area;
                            best = hwnd;
                        }
                    }
                    catch
                    {
                    }

                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }

            return best;
        }

        private static string TitleOf(IntPtr hwnd)
        {
            try
            {
                StringBuilder title = new StringBuilder(256);
                int length = GetWindowText(hwnd, title, title.Capacity);
                return length > 0 ? title.ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>诊断用：列出可见顶层窗口，写日志定位到底该覆盖哪个窗口。</summary>
        public static string DescribeVisibleWindows(int limit)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int count = 0;

            try
            {
                EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
                {
                    try
                    {
                        if (!IsWindowVisible(hwnd)) return true;
                        if (count >= limit) return false;

                        RECT rect;
                        if (!GetWindowRect(hwnd, out rect)) return true;
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        if (width < 200 || height < 120) return true;

                        builder.Append(" [").Append(ClassNameOf(hwnd)).Append("] ")
                            .Append(width).Append('x').Append(height).Append(' ')
                            .Append(TitleOf(hwnd)).Append(';');
                        count++;
                    }
                    catch
                    {
                    }

                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }

            return builder.ToString();
        }

        private static IntPtr FindProcessMainWindow(string processName)
        {
            try
            {
                System.Diagnostics.Process[] processes =
                    System.Diagnostics.Process.GetProcessesByName(processName);
                IntPtr best = IntPtr.Zero;
                long bestArea = 0;

                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        IntPtr hwnd = processes[i].MainWindowHandle;
                        if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd) || IsIconic(hwnd)) continue;

                        RECT rect;
                        if (!GetWindowRect(hwnd, out rect)) continue;
                        long area = (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top);
                        if (area > bestArea)
                        {
                            bestArea = area;
                            best = hwnd;
                        }
                    }
                    catch
                    {
                    }
                }

                return best;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static Rectangle GetBounds(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return Rectangle.Empty;
            RECT rect;
            if (!GetWindowRect(hwnd, out rect)) return Rectangle.Empty;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width < 80 || height < 60) return Rectangle.Empty;
            return new Rectangle(rect.Left, rect.Top, width, height);
        }

        public static Rectangle PrimaryScreen()
        {
            int width = GetSystemMetrics(0);
            int height = GetSystemMetrics(1);
            if (width <= 0 || height <= 0) return Rectangle.Empty;
            return new Rectangle(0, 0, width, height);
        }

        /// <summary>屏幕居中的 16:9 窗口，没有控制台窗口时使用 / centered 16:9 window.</summary>
        public static Rectangle CenterOnScreen(double fraction)
        {
            Rectangle screen = PrimaryScreen();
            if (screen == Rectangle.Empty) return Rectangle.Empty;

            if (fraction <= 0 || fraction > 1) fraction = 0.8;

            int width = (int)(screen.Width * fraction);
            int height = width * 9 / 16;
            int maxHeight = (int)(screen.Height * fraction);
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * 16 / 9;
            }

            return new Rectangle(screen.Left + (screen.Width - width) / 2,
                screen.Top + (screen.Height - height) / 2, width, height);
        }

        /// <summary>计算覆盖区域：优先贴合控制台，异常时回退全屏 / console rect or fullscreen.</summary>
        public static Rectangle ResolveBounds(bool fullscreen)
        {
            if (fullscreen) return PrimaryScreen();
            IntPtr hwnd = Handle;
            if (CanHide(hwnd) && !IsIconic(hwnd))
            {
                Rectangle rect = GetBounds(hwnd);
                if (rect != Rectangle.Empty) return rect;
            }
            return PrimaryScreen();
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }

    /// <summary>无边框动画窗口 / borderless animation window.</summary>
    public sealed class Win32Splash : IDisposable
    {
        private const int WsPopup = unchecked((int)0x80000000);
        private const int WsVisible = 0x10000000;
        private const int WsExTopmost = 0x00000008;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExLayered = 0x00080000;
        private const int SwShow = 5;
        private const int LwaAlpha = 0x00000002;
        private const uint WmCreate = 0x0001;
        private const uint WmDestroy = 0x0002;
        private const uint WmPaint = 0x000F;
        private const uint WmClose = 0x0010;
        private const uint WmEraseBackground = 0x0014;
        private const uint WmMove = 0x0003;
        private const uint WmNcHitTest = 0x0084;
        private const uint WmExitSizeMove = 0x0232;
        private const int HtClient = 1;
        private const int HtCaption = 2;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint WmKeyDown = 0x0100;
        private const uint WmSysKeyDown = 0x0104;
        private const uint WmTimer = 0x0113;
        private const uint WmLeftButtonDown = 0x0201;
        private const uint WmRightButtonDown = 0x0204;
        private const uint WmMiddleButtonDown = 0x0207;
        private const int CsHRedraw = 0x0002;
        private const int CsVRedraw = 0x0001;
        private const int CsDoubleClicks = 0x0008;
        private const int SwpShowWindow = 0x0040;
        private const int DcBrush = 18;

        private readonly SplashManifest _manifest;
        private readonly Rectangle _bounds;
        private readonly bool _hint;
        private readonly Action<string> _log;
        private readonly IntPtr _console;
        private readonly bool _moveConsole;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly WndProc _wndProc;
        private readonly string _className;
        private readonly IntPtr _hInstance;

        private IntPtr _hwnd;
        private IntPtr _bgBrush;
        private IntPtr _whiteBrush;
        private IntPtr _font;
        private bool _destroyed;
        private bool _fading;
        private bool _loadPending;
        private bool _loadFailed;
        private bool _watchdogStarted;
        private double _fadeStart;
        private double _effectiveDuration;
        private int _alpha = 255;

        private SplashKind _kind;
        private List<Bitmap> _frames;
        private List<int> _delays;
        private int _gifTotalMs;

        public Win32Splash(SplashManifest manifest, Rectangle bounds, bool hint, Action<string> log,
            IntPtr console, bool moveConsole)
        {
            _manifest = manifest ?? SplashManifest.BuiltinDefaults();
            _bounds = bounds;
            _hint = hint;
            _log = log ?? delegate { };
            _console = console;
            _moveConsole = moveConsole;
            _wndProc = WindowProc;
            _className = "ConsoleSplashOverlay_" + Guid.NewGuid().ToString("N");
            _hInstance = GetModuleHandle(null);
            _kind = _manifest.Kind;
            _loadPending = true;
            _effectiveDuration = _manifest.Duration > 0 ? _manifest.Duration : 3.0;
        }

        /// <summary>播放动画，阻塞到窗口关闭 / play and block until the window closes.</summary>
        public static bool Play(SplashManifest manifest, Rectangle bounds, bool hint, Action<string> log)
        {
            return Play(manifest, bounds, hint, log, IntPtr.Zero, false);
        }

        /// <summary>播放并让控制台窗口跟随拖动 / play and keep the console glued to the overlay.</summary>
        public static bool Play(SplashManifest manifest, Rectangle bounds, bool hint, Action<string> log,
            IntPtr console, bool moveConsole)
        {
            if (bounds == Rectangle.Empty) return false;
            try
            {
                using (Win32Splash splash = new Win32Splash(manifest, bounds, hint, log, console, moveConsole))
                    return splash.Run();
            }
            catch (Exception exception)
            {
                if (log != null) log("splash failed: " + exception.Message);
                return false;
            }
        }

        private bool Run()
        {
            WNDCLASSEX windowClass = new WNDCLASSEX();
            windowClass.cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX));
            windowClass.style = CsHRedraw | CsVRedraw | CsDoubleClicks;
            windowClass.lpfnWndProc = _wndProc;
            windowClass.hInstance = _hInstance;
            windowClass.hCursor = LoadCursor(IntPtr.Zero, (IntPtr)32512);
            windowClass.hbrBackground = IntPtr.Zero;
            windowClass.lpszClassName = _className;

            if (RegisterClassEx(ref windowClass) == 0)
            {
                _log("RegisterClassEx failed");
                return false;
            }

            try
            {
                _bgBrush = CreateSolidBrush(ColorUtil.ToColorRef(
                    ColorUtil.Parse(_manifest.Background, Color.Black)));
                _whiteBrush = CreateSolidBrush(ColorUtil.ToColorRef(Color.White));
                _font = CreateFont(-14, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");

                _hwnd = CreateWindowEx(
                    WsExTopmost | WsExToolWindow | WsExLayered,
                    _className, "ConsoleSplash",
                    WsPopup | WsVisible,
                    _bounds.Left, _bounds.Top, _bounds.Width, _bounds.Height,
                    IntPtr.Zero, IntPtr.Zero, _hInstance, IntPtr.Zero);

                if (_hwnd == IntPtr.Zero)
                {
                    _log("CreateWindowEx failed");
                    return false;
                }

                SetLayeredWindowAttributes(_hwnd, 0, 255, LwaAlpha);
                ShowWindow(_hwnd, SwShow);
                SetWindowPos(_hwnd, (IntPtr)(-1), _bounds.Left, _bounds.Top,
                    _bounds.Width, _bounds.Height, SwpShowWindow);
                InvalidateRect(_hwnd, IntPtr.Zero, true);
                UpdateWindow(_hwnd);
                SetForegroundWindow(_hwnd);
                SetFocus(_hwnd);

                MSG message;
                while (GetMessage(out message, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(ref message);
                    DispatchMessage(ref message);
                }

                return true;
            }
            finally
            {
                _destroyed = true;
                if (_font != IntPtr.Zero) DeleteObject(_font);
                if (_whiteBrush != IntPtr.Zero) DeleteObject(_whiteBrush);
                if (_bgBrush != IntPtr.Zero) DeleteObject(_bgBrush);
                UnregisterClass(_className, _hInstance);
                DisposeFrames();
            }
        }

        private void StartWatchdog()
        {
            int timeout = (int)((_effectiveDuration + _manifest.Fade + 20.0) * 1000.0);
            if (timeout < 15000) timeout = 15000;

            Thread watchdog = new Thread(delegate()
            {
                int waited = 0;
                while (waited < timeout && !_destroyed)
                {
                    Thread.Sleep(250);
                    waited += 250;
                }

                if (!_destroyed && _hwnd != IntPtr.Zero)
                    PostMessage(_hwnd, WmClose, IntPtr.Zero, IntPtr.Zero);
            });
            watchdog.IsBackground = true;
            watchdog.Name = "ConsoleSplashWatchdog";
            watchdog.Start();
        }

        private void OnTimer()
        {
            if (_destroyed) return;

            if (_loadPending)
            {
                LoadImages();
                _loadPending = false;
                _clock.Restart();

                if (!_watchdogStarted)
                {
                    _watchdogStarted = true;
                    StartWatchdog();
                }
            }

            double elapsed = _clock.Elapsed.TotalSeconds;

            if (!_moveConsole && _console != IntPtr.Zero) FollowTarget();

            if (!_fading && elapsed >= _effectiveDuration)
            {
                BeginFade();
            }
            else if (_fading)
            {
                double fade = _manifest.Fade;
                if (fade < 0.05) fade = 0.05;
                double progress = (elapsed - _fadeStart) / fade;
                if (progress < 0) progress = 0;
                int alpha = (int)(255 * (1.0 - progress));
                if (alpha < 0) alpha = 0;
                if (alpha != _alpha)
                {
                    _alpha = alpha;
                    SetLayeredWindowAttributes(_hwnd, 0, (byte)alpha, LwaAlpha);
                }

                if (alpha <= 0)
                {
                    DestroyWindow(_hwnd);
                    return;
                }
            }

            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }

        private void BeginFade()
        {
            if (_fading) return;
            _fading = true;
            _fadeStart = _clock.Elapsed.TotalSeconds;

            if (_manifest.Fade <= 0.05)
            {
                DestroyWindow(_hwnd);
            }
        }

        private void LoadImages()
        {
            if (!_manifest.IsNativeKind)
            {
                _kind = SplashKind.Builtin;
                return;
            }

            _kind = _manifest.Kind;
            if (_kind == SplashKind.Builtin) return;

            try
            {
                _frames = new List<Bitmap>();

                if (_kind == SplashKind.Gif)
                {
                    using (Image image = Image.FromFile(_manifest.AssetPath))
                    {
                        FrameDimension dimension = new FrameDimension(image.FrameDimensionsList[0]);
                        int frameCount = image.GetFrameCount(dimension);
                        _delays = ReadGifDelays(image, frameCount);

                        for (int i = 0; i < frameCount; i++)
                        {
                            image.SelectActiveFrame(dimension, i);
                            _frames.Add(new Bitmap(image));
                        }
                    }

                    _gifTotalMs = 0;
                    for (int i = 0; i < _delays.Count; i++) _gifTotalMs += _delays[i];
                    if (_gifTotalMs <= 0) _gifTotalMs = _frames.Count * 100;
                    if (_manifest.Duration <= 0) _effectiveDuration = _gifTotalMs / 1000.0;
                }
                else if (_kind == SplashKind.Frames)
                {
                    for (int i = 0; i < _manifest.FrameFiles.Count; i++)
                    {
                        try { _frames.Add(new Bitmap(_manifest.FrameFiles[i])); }
                        catch { }
                    }

                    if (_frames.Count == 0) throw new InvalidOperationException("no readable frames");
                    if (_manifest.Duration <= 0)
                        _effectiveDuration = _frames.Count / (double)Math.Max(1, _manifest.Fps);
                }
                else if (_kind == SplashKind.Image)
                {
                    _frames.Add(new Bitmap(_manifest.AssetPath));
                }

                _log("loaded " + _kind + " frames=" + _frames.Count);
            }
            catch (Exception exception)
            {
                _loadFailed = true;
                _kind = SplashKind.Builtin;
                _log("image load failed, falling back to builtin: " + exception.Message);
                DisposeFrames();
            }
        }

        private static List<int> ReadGifDelays(Image image, int frameCount)
        {
            List<int> delays = new List<int>();
            try
            {
                PropertyItem property = image.GetPropertyItem(0x5100);
                byte[] data = property.Value;
                for (int i = 0; i < frameCount; i++)
                {
                    int value = 0;
                    int offset = i * 4;
                    if (offset + 3 < data.Length)
                        value = BitConverter.ToInt32(data, offset);
                    if (value <= 0) value = 10;
                    delays.Add(value * 10);
                }
            }
            catch
            {
                for (int i = 0; i < frameCount; i++) delays.Add(100);
            }

            return delays;
        }

        private void Paint()
        {
            PAINTSTRUCT paint;
            IntPtr hdc = BeginPaint(_hwnd, out paint);
            try
            {
                RECT client;
                GetClientRect(_hwnd, out client);
                int width = client.Right - client.Left;
                int height = client.Bottom - client.Top;
                if (width <= 0 || height <= 0) return;

                RECT full = new RECT();
                full.Left = 0;
                full.Top = 0;
                full.Right = width;
                full.Bottom = height;
                FillRect(hdc, ref full, _bgBrush);

                double elapsed = _clock.Elapsed.TotalSeconds;

                if (_kind == SplashKind.Builtin || _loadFailed || _frames == null || _frames.Count == 0)
                {
                    DrawSweep(hdc, width, height, elapsed);
                }
                else
                {
                    DrawFrameBitmap(hdc, width, height, elapsed);
                }

                if (_hint && !_fading)
                    DrawHint(hdc, width, height);
            }
            catch (Exception exception)
            {
                _loadFailed = true;
                _kind = SplashKind.Builtin;
                _log("paint failed: " + exception.Message);
            }
            finally
            {
                EndPaint(_hwnd, ref paint);
            }
        }

        private void DrawSweep(IntPtr hdc, int width, int height, double elapsed)
        {
            double duration = _effectiveDuration;
            if (duration <= 0.05) duration = 3.0;

            double progress = elapsed / duration;
            if (progress < 0) progress = 0;
            if (progress > 1) progress = 1;

            Color background = ColorUtil.Parse(_manifest.Background, Color.Black);
            Color accent = ColorUtil.Parse(_manifest.Accent, Color.FromArgb(127, 219, 255));

            int bandWidth = (int)(width * 0.24);
            if (bandWidth < 140) bandWidth = 140;
            if (bandWidth > width) bandWidth = width;

            double bandX = width - (width + bandWidth) * progress;
            int x0 = (int)bandX;

            Color[] stops = new Color[6];
            stops[0] = background;
            stops[1] = ColorUtil.Blend(background, accent, 0.30);
            stops[2] = accent;
            stops[3] = Color.White;
            stops[4] = accent;
            stops[5] = background;

            int strips = 128;
            IntPtr brush = GetStockObject(DcBrush);
            IntPtr previous = SelectObject(hdc, brush);

            for (int i = 0; i < strips; i++)
            {
                int left = x0 + (int)(bandWidth * (i / (double)strips));
                int right = x0 + (int)(bandWidth * ((i + 1) / (double)strips));
                if (right <= 0 || left >= width) continue;
                if (left < 0) left = 0;
                if (right > width) right = width;
                if (right <= left) continue;

                Color color = BandColor(stops, (i + 0.5) / strips);
                SetDCBrushColor(hdc, ColorUtil.ToColorRef(color));

                RECT strip = new RECT();
                strip.Left = left;
                strip.Top = 0;
                strip.Right = right;
                strip.Bottom = height;
                FillRect(hdc, ref strip, brush);
            }

            SelectObject(hdc, previous);

            int coreX = x0 + (int)(bandWidth * 0.5);
            if (coreX >= 0 && coreX < width)
            {
                RECT core = new RECT();
                core.Left = coreX;
                core.Top = 0;
                core.Right = coreX + 2;
                core.Bottom = height;
                FillRect(hdc, ref core, _whiteBrush);
            }
        }

        private static Color BandColor(Color[] stops, double position)
        {
            if (position < 0) position = 0;
            if (position > 1) position = 1;

            double[] points = { 0.0, 0.22, 0.45, 0.55, 0.78, 1.0 };
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (position <= points[i + 1])
                {
                    double span = points[i + 1] - points[i];
                    double amount = span <= 0 ? 0 : (position - points[i]) / span;
                    return ColorUtil.Blend(stops[i], stops[i + 1], amount);
                }
            }

            return stops[stops.Length - 1];
        }

        private void DrawFrameBitmap(IntPtr hdc, int width, int height, double elapsed)
        {
            if (_frames == null || _frames.Count == 0) return;

            int index = 0;
            if (_kind == SplashKind.Gif)
            {
                int total = _gifTotalMs > 0 ? _gifTotalMs : _frames.Count * 100;
                int position = (int)(elapsed * 1000.0) % total;
                int accumulated = 0;
                for (int i = 0; i < _frames.Count; i++)
                {
                    int delay = _delays != null && i < _delays.Count ? _delays[i] : 100;
                    accumulated += delay;
                    if (position < accumulated)
                    {
                        index = i;
                        break;
                    }
                }
            }
            else
            {
                int fps = Math.Max(1, _manifest.Fps);
                index = (int)(elapsed * fps) % _frames.Count;
            }

            Bitmap frame = _frames[index];
            Rectangle destination = Fit(new Rectangle(0, 0, width, height), frame.Size);

            using (Graphics graphics = Graphics.FromHdc(hdc))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(frame, destination);
            }
        }

        private static Rectangle Fit(Rectangle bounds, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0) return bounds;
            double scale = Math.Min(bounds.Width / (double)size.Width, bounds.Height / (double)size.Height);
            int width = Math.Max(1, (int)(size.Width * scale));
            int height = Math.Max(1, (int)(size.Height * scale));
            return new Rectangle(bounds.Left + (bounds.Width - width) / 2,
                bounds.Top + (bounds.Height - height) / 2, width, height);
        }

        private void DrawHint(IntPtr hdc, int width, int height)
        {
            if (_font == IntPtr.Zero) return;

            string text = "Press any key to skip";
            IntPtr previousFont = SelectObject(hdc, _font);
            SetBkMode(hdc, 1);
            SetTextColor(hdc, 0x00303030);
            TextOut(hdc, width - 190, height - 34, text, text.Length);
            SelectObject(hdc, previousFont);
        }

        private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                switch (message)
                {
                    case WmCreate:
                        SetTimer(hwnd, (IntPtr)1, 16, IntPtr.Zero);
                        return IntPtr.Zero;
                    case WmTimer:
                        OnTimer();
                        return IntPtr.Zero;
                    case WmPaint:
                        Paint();
                        return IntPtr.Zero;
                    case WmEraseBackground:
                        return (IntPtr)1;
                    case WmNcHitTest:
                        if (_moveConsole && IsDragArea(lParam))
                            return (IntPtr)HtCaption;
                        return (IntPtr)HtClient;
                    case WmMove:
                    case WmExitSizeMove:
                        FollowConsole();
                        return IntPtr.Zero;
                    case WmKeyDown:
                    case WmSysKeyDown:
                    case WmLeftButtonDown:
                    case WmRightButtonDown:
                    case WmMiddleButtonDown:
                        if (_manifest.SkipOnKey) BeginFade();
                        return IntPtr.Zero;
                    case WmClose:
                        DestroyWindow(hwnd);
                        return IntPtr.Zero;
                    case WmDestroy:
                        _destroyed = true;
                        KillTimer(hwnd, (IntPtr)1);
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                }
            }
            catch (Exception exception)
            {
                _log("wndproc failed: " + exception.Message);
                try { DestroyWindow(hwnd); }
                catch { }
            }

            return DefWindowProc(hwnd, message, wParam, lParam);
        }

        private bool IsDragArea(IntPtr lParam)
        {
            long value = lParam.ToInt64();
            int screenY = (short)((value >> 16) & 0xFFFF);
            RECT rect;
            if (!GetWindowRect(_hwnd, out rect)) return false;
            return screenY >= rect.Top && screenY < rect.Top + 48;
        }

        private void FollowConsole()
        {
            if (!_moveConsole || _console == IntPtr.Zero || _hwnd == IntPtr.Zero) return;

            RECT rect;
            if (!GetWindowRect(_hwnd, out rect)) return;

            SetWindowPos(_console, IntPtr.Zero, rect.Left, rect.Top,
                rect.Right - rect.Left, rect.Bottom - rect.Top, SwpNoZOrder | SwpNoActivate);
        }

        /// <summary>目标不是 conhost（例如 LocalAdmin）：跟随它的位置和大小，不改变它。</summary>
        private void FollowTarget()
        {
            if (_hwnd == IntPtr.Zero) return;

            RECT target;
            if (!GetWindowRect(_console, out target)) return;

            RECT self;
            if (!GetWindowRect(_hwnd, out self)) return;

            if (target.Left == self.Left && target.Top == self.Top
                && target.Right == self.Right && target.Bottom == self.Bottom)
                return;

            SetWindowPos(_hwnd, IntPtr.Zero, target.Left, target.Top,
                target.Right - target.Left, target.Bottom - target.Top,
                SwpNoZOrder | SwpNoActivate);
        }

        private void DisposeFrames()
        {
            if (_frames == null) return;
            for (int i = 0; i < _frames.Count; i++)
            {
                try { _frames[i].Dispose(); }
                catch { }
            }
            _frames.Clear();
            _frames = null;
        }

        public void Dispose()
        {
            DisposeFrames();
            if (_hwnd != IntPtr.Zero)
            {
                try { DestroyWindow(_hwnd); }
                catch { }
                _hwnd = IntPtr.Zero;
            }
        }

        private delegate IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            [MarshalAs(UnmanagedType.Bool)] public bool fErase;
            public RECT rcPaint;
            [MarshalAs(UnmanagedType.Bool)] public bool fRestore;
            [MarshalAs(UnmanagedType.Bool)] public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);
        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")]
        private static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);
        [DllImport("user32.dll")]
        private static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);
        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);
        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);
        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint crColor);
        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int i);
        [DllImport("gdi32.dll")]
        private static extern uint SetDCBrushColor(IntPtr hdc, uint color);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);
        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);
        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, uint color);
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFont(int height, int width, int escapement, int orientation,
            int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision,
            uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool TextOut(IntPtr hdc, int x, int y, string lpString, int cchString);
        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
}
