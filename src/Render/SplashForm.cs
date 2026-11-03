// ============================================================================
//  ConsoleSplash.Render —— 无边框 WebView2 窗口 / borderless WebView2 window
// ============================================================================
using System;
using System.Drawing;
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
        private readonly Timer _timer;
        private readonly Microsoft.Web.WebView2.WinForms.WebView2 _web;
        private DateTime _started;
        private bool _ready;
        private bool _exiting;

        public bool Failed { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public SplashForm(string htmlPath, string baseDirectory, Rectangle bounds, double seconds)
        {
            _htmlPath = htmlPath;
            _baseDirectory = baseDirectory;
            _seconds = seconds > 0 ? seconds : 3.5;

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

                core.Navigate(FileUrl(_htmlPath, baseDirectory));

                _ready = true;
                _started = DateTime.UtcNow;
                _timer.Start();
                Host.Trace("webview ready, navigating");

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

        private void OnTick(object sender, EventArgs e)
        {
            if (!_ready || _exiting) return;
            if ((DateTime.UtcNow - _started).TotalSeconds >= _seconds)
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
