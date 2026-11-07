// ============================================================================
//  ConsoleSplash.Render —— 外部渲染宿主入口 / external renderer entry
//
//  这个 DLL 由 powershell.exe 加载，在独立进程里用 WebView2 播放 HTML。
//  WebView2 永远不会进入 Unity/Mono 的服务器进程，所以不会再把服务器带崩。
//
//  Loaded by powershell.exe in its own process; WebView2 never enters the
//  Unity/Mono server process.
// ============================================================================
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ConsoleSplash.Render
{
    public static class Host
    {
        static Host()
        {
            // 让 WebView2Loader.dll 从渲染器所在目录加载。
            try
            {
                SetDllDirectory(Path.GetDirectoryName(typeof(Host).Assembly.Location));
            }
            catch
            {
            }

            // 必须 DPI 感知，否则窗口坐标会被系统缩放，尺寸和位置都会偏。
            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool SetDllDirectory(string path);

        /// <summary>播放并返回退出码：0 成功，2 失败。</summary>
        public static int Run(string htmlPath, string baseDirectory,
            int x, int y, int width, int height, int virtualScreenWidth, int virtualScreenHeight,
            double seconds, bool smooth)
        {
            // 插件进程（Unity）拿到的坐标是 DPI 虚拟化后的，这里换算回物理像素。
            // 直接使用插件给的坐标：覆盖层要和服务器窗口完全重合，不再强制居中。
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            double factor = 1.0;
            if (virtualScreenWidth > 0 && screenWidth > 0)
                factor = screenWidth / (double)virtualScreenWidth;
            if (factor <= 0) factor = 1.0;

            int physicalX = (int)Math.Round(x * factor);
            int physicalY = (int)Math.Round(y * factor);
            int physicalWidth = (int)Math.Round(width * factor);
            int physicalHeight = (int)Math.Round(height * factor);
            if (physicalWidth > screenWidth) physicalWidth = screenWidth;
            if (physicalHeight > screenHeight) physicalHeight = screenHeight;
            if (physicalX < 0) physicalX = 0;
            if (physicalY < 0) physicalY = 0;
            if (physicalX + physicalWidth > screenWidth)
                physicalX = screenWidth - physicalWidth;
            if (physicalY + physicalHeight > screenHeight)
                physicalY = screenHeight - physicalHeight;

            Rectangle bounds = new Rectangle(physicalX, physicalY, physicalWidth, physicalHeight);

            Trace("run html=" + htmlPath + " target=" + width + "x" + height
                + " factor=" + factor + " seconds=" + seconds + " bounds=" + bounds);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SplashForm form = new SplashForm(htmlPath, baseDirectory, bounds, seconds, smooth);

            Application.Run(form);
            Trace("exit failed=" + form.Failed);
            return form.Failed ? 2 : 0;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        internal static void Trace(string message)
        {
            try
            {
                string directory = Path.GetDirectoryName(typeof(Host).Assembly.Location);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    directory = Path.Combine(Path.GetTempPath(), "ConsoleSplash");
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(Path.Combine(directory, "render.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
