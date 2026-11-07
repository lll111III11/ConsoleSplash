// ============================================================================
//  ConsoleSplash.Render —— 无边框 WebView2 窗口 / borderless WebView2 window
// ============================================================================
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace ConsoleSplash.Render
{
    internal sealed class SplashForm : Form
    {
        private readonly string _htmlPath;
        private readonly string _baseDirectory;
        private readonly double _seconds;
        private readonly bool _smooth;
        private string _backdropPath;
        private string _backdropFolder;
        private readonly Rectangle _consoleBounds;
        private Rectangle _fullBounds;
        private readonly Timer _timer;
        private Timer _navFallback;
        private readonly Microsoft.Web.WebView2.WinForms.WebView2 _web;
        private Timer _anim;
        private DateTime _animStarted;
        private Rectangle _animFrom;
        private Rectangle _animTo;
        private int _animDuration;
        private Action _animCompleted;
        private DateTime _started;
        private bool _ready;
        private bool _exiting;
        private bool _closing;

        public bool Failed { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public SplashForm(string htmlPath, string baseDirectory, Rectangle bounds, double seconds,
            bool smooth, int backdropMode)
        {
            _htmlPath = htmlPath;
            _baseDirectory = baseDirectory;
            _seconds = seconds > 0 ? seconds : 3.5;
            _smooth = smooth;
            _consoleBounds = bounds;
            _fullBounds = bounds;

            // 必须先截屏再显示窗口，否则截到的是我们自己的黑色覆盖层。
            // Capture before the window is shown, otherwise we would capture ourselves.
            if (backdropMode > 0) _backdropPath = CaptureBackdrop(bounds, backdropMode, out _backdropFolder);

            if (smooth)
            {
                try
                {
                    Rectangle working = Screen.FromRectangle(bounds).WorkingArea;
                    if (working.Width > 0 && working.Height > 0) _fullBounds = working;
                }
                catch
                {
                }
            }

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            KeyPreview = true;

            _web = new Microsoft.Web.WebView2.WinForms.WebView2();
            _web.Dock = DockStyle.Fill;
            _web.DefaultBackgroundColor = Color.Black;
            Controls.Add(_web);

            _timer = new Timer();
            _timer.Interval = 50;
            _timer.Tick += OnTick;

            KeyDown += delegate { BeginExit(); };
            MouseDown += delegate { BeginExit(); };
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ConsoleSplash", "WebView2");
                Directory.CreateDirectory(userData);

                CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions(
                    "--autoplay-policy=no-user-gesture-required");

                CoreWebView2Environment environment =
                    await CoreWebView2Environment.CreateAsync(null, userData, options);
                await _web.EnsureCoreWebView2Async(environment);

                CoreWebView2 core = _web.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsStatusBarEnabled = false;

                core.WebMessageReceived += delegate(object sender, CoreWebView2WebMessageReceivedEventArgs args)
                {
                    string text;
                    try { text = args.TryGetWebMessageAsString(); }
                    catch { text = args.WebMessageAsJson; }
                    if (text == null) text = args.WebMessageAsJson;
                    if (text.Trim().Trim('"') == "skip") BeginExit();
                };

                core.AddScriptToExecuteOnDocumentCreatedAsync(
                    "(function(){function s(){try{chrome.webview.postMessage('skip')}catch(e){}}" +
                    "window.addEventListener('keydown',s,true);" +
                    "window.addEventListener('mousedown',s,true);})();");

                string baseDirectory = string.IsNullOrEmpty(_baseDirectory)
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : _baseDirectory;

                core.SetVirtualHostNameToFolderMapping("splash.local", baseDirectory,
                    CoreWebView2HostResourceAccessKind.Allow);

                // 需要“缝隙里露出控制台”的动画：把刚截好的全屏图作为 backdrop 注入。
                if (!string.IsNullOrEmpty(_backdropPath) && File.Exists(_backdropPath))
                {
                    core.SetVirtualHostNameToFolderMapping("splash-temp.local", _backdropFolder,
                        CoreWebView2HostResourceAccessKind.Allow);
                    string backdropUrl = "https://splash-temp.local/"
                        + Uri.EscapeDataString(Path.GetFileName(_backdropPath));
                    core.AddScriptToExecuteOnDocumentCreatedAsync(
                        "window.CSPLASH_BACKDROP='" + backdropUrl + "';");
                }

                // 等页面真正加载完再开始计时，否则收缩会比 CSS 收尾光幕早一截。
                // Start the timeline only after the page is loaded so the shrink lines up
                // with the CSS exit curtain.
                core.NavigationCompleted += delegate { StartTimeline(); };

                core.Navigate(FileUrl(_htmlPath, baseDirectory));
                Host.Trace("webview ready, navigating");

                // 导航事件偶尔不回调时兜底，避免覆盖层一直挂着不关。
                _navFallback = new Timer();
                _navFallback.Interval = 2600;
                _navFallback.Tick += delegate
                {
                    _navFallback.Stop();
                    _navFallback.Dispose();
                    _navFallback = null;
                    Host.Trace("navigation fallback fired");
                    StartTimeline();
                };
                _navFallback.Start();

                // 强制显示并置顶，避免被其他窗口盖住。
                ShowWindow(Handle, 5);
                SetWindowPos(Handle, new IntPtr(-1), Bounds.Left, Bounds.Top,
                    Bounds.Width, Bounds.Height, 0x0040 | 0x0001);
                SetForegroundWindow(Handle);
            }
            catch (Exception exception)
            {
                Host.Trace("init failed: " + exception.Message);
                Failed = true;
                Close();
            }
        }

        /// <summary>
        /// 页面加载完成（或兜底计时到点）后启动时间线。
        /// Start the timeline once the page is loaded, or when the fallback timer fires.
        /// </summary>
        private void StartTimeline()
        {
            if (_ready) return;
            _ready = true;
            _started = DateTime.UtcNow;
            _timer.Start();

            if (_navFallback != null)
            {
                _navFallback.Stop();
                _navFallback.Dispose();
                _navFallback = null;
            }

            Host.Trace("timeline started");

            // 平滑开场：画面出来后再从控制台矩形铺满工作区。
            if (_smooth && _fullBounds != _consoleBounds)
            {
                Timer delay = new Timer();
                delay.Interval = 80;
                delay.Tick += delegate
                {
                    delay.Stop();
                    delay.Dispose();
                    if (_exiting || _closing) return;
                    StartAnimation(_consoleBounds, _fullBounds, 520, null);
                };
                delay.Start();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_ready || _exiting || _closing) return;

            double elapsed = (DateTime.UtcNow - _started).TotalSeconds;
            if (_smooth)
            {
                // 收尾光幕铺满后开始缩回控制台矩形，缩完再关窗口。
                double shrinkAt = Math.Max(0.4, _seconds - 0.62);
                if (elapsed >= shrinkAt)
                {
                    _exiting = true;
                    ShrinkAndClose(560);
                }
            }
            else if (elapsed >= _seconds)
            {
                _timer.Stop();
                Close();
            }
        }

        /// <summary>
        /// 跳过时先把画面切到收尾光幕，再关窗口，避免直接硬切回控制台。
        /// Jump to the exit curtain before closing so the skip still transitions.
        /// </summary>
        private void BeginExit()
        {
            if (_exiting) return;
            _exiting = true;

            try
            {
                if (_web.CoreWebView2 != null)
                    _web.CoreWebView2.ExecuteScriptAsync("document.body.classList.add('skipping')");
            }
            catch
            {
            }

            if (_smooth)
            {
                ShrinkAndClose(360);
                return;
            }

            Timer close = new Timer();
            close.Interval = 450;
            close.Tick += delegate
            {
                close.Stop();
                close.Dispose();
                Close();
            };
            close.Start();
        }

        /// <summary>
        /// 从当前矩形缩回控制台矩形，动画结束后再关窗口。
        /// Shrink back onto the console rect, then close once the animation finishes.
        /// </summary>
        private void ShrinkAndClose(int duration)
        {
            if (_closing) return;
            _closing = true;

            try { _timer.Stop(); } catch { }
            StartAnimation(Bounds, _consoleBounds, duration, delegate
            {
                try { Close(); } catch { }
            });
        }

        private void StartAnimation(Rectangle from, Rectangle to, int duration, Action completed)
        {
            StopAnimation();
            if (duration < 1) duration = 1;

            _animFrom = from;
            _animTo = to;
            _animDuration = duration;
            _animCompleted = completed;
            _animStarted = DateTime.UtcNow;

            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += OnAnimationTick;
            _anim.Start();
        }

        private void StopAnimation()
        {
            if (_anim == null) return;
            try
            {
                _anim.Stop();
                _anim.Dispose();
            }
            catch
            {
            }
            _anim = null;
            _animCompleted = null;
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            double progress = (DateTime.UtcNow - _animStarted).TotalMilliseconds / _animDuration;
            if (progress < 0) progress = 0;
            if (progress > 1) progress = 1;

            // 展开用 easeOutCubic，收缩用 easeInOutCubic。
            double eased;
            if (_animTo.Width >= _animFrom.Width)
            {
                double inverse = 1 - progress;
                eased = 1 - inverse * inverse * inverse;
            }
            else
            {
                eased = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;
            }

            int x = (int)Math.Round(_animFrom.X + (_animTo.X - _animFrom.X) * eased);
            int y = (int)Math.Round(_animFrom.Y + (_animTo.Y - _animFrom.Y) * eased);
            int width = (int)Math.Round(_animFrom.Width + (_animTo.Width - _animFrom.Width) * eased);
            int height = (int)Math.Round(_animFrom.Height + (_animTo.Height - _animFrom.Height) * eased);
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            try
            {
                SetWindowPos(Handle, new IntPtr(-1), x, y, width, height, 0x0010);
            }
            catch
            {
            }

            if (progress >= 1)
            {
                Action completed = _animCompleted;
                StopAnimation();
                if (completed != null) completed();
            }
        }

        /// <summary>
        /// 在显示覆盖层之前截下整块屏幕，给“方块缝隙”当背景。
        /// Grab the screen before the overlay appears so tile gaps can show it.
        /// </summary>
        private static string CaptureBackdrop(Rectangle bounds, int mode, out string folder)
        {
            folder = null;
            try
            {
                Screen screen = Screen.FromRectangle(bounds);
                Rectangle area = mode == 2 ? bounds : screen.Bounds;
                if (area.Width <= 0 || area.Height <= 0) return null;

                string directory = Path.Combine(Path.GetTempPath(), "ConsoleSplash");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory,
                    "backdrop-" + Guid.NewGuid().ToString("N") + ".png");

                using (Bitmap bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(area.Left, area.Top, 0, 0, area.Size,
                            CopyPixelOperation.SourceCopy);
                    }
                    bitmap.Save(path, ImageFormat.Png);
                }

                folder = directory;
                return path;
            }
            catch
            {
                return null;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (_navFallback != null)
            {
                try
                {
                    _navFallback.Stop();
                    _navFallback.Dispose();
                }
                catch
                {
                }
                _navFallback = null;
            }

            if (!string.IsNullOrEmpty(_backdropPath))
            {
                try { File.Delete(_backdropPath); } catch { }
            }
        }

        private static string FileUrl(string path, string baseDirectory)
        {
            string relative = Path.GetFileName(path);
            try
            {
                string root = Path.GetFullPath(baseDirectory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                string full = Path.GetFullPath(path);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    relative = full.Substring(root.Length);
            }
            catch
            {
            }

            string[] parts = relative.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder builder = new StringBuilder("https://splash.local/");
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) builder.Append('/');
                builder.Append(Uri.EscapeDataString(parts[i]));
            }
            return builder.ToString();
        }
    }
}
