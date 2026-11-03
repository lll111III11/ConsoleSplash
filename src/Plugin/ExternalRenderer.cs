// ============================================================================
//  ConsoleSplash —— 外部渲染器调用 / external renderer launcher
//
//  插件进程只负责：定位窗口、隐藏、计算居中矩形、拉起宿主进程、等待结束。
//  真正的 WebView2 渲染发生在 powershell.exe 加载的 ConsoleSplash.Render.dll 里。
//
//  The plugin only locates/hides the window and launches an out-of-process host;
//  WebView2 runs inside ConsoleSplash.Render.dll, never inside Unity.
// ============================================================================
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace ConsoleSplash.Plugin
{
    internal static class ExternalRenderer
    {
        internal static bool Play(string pluginDirectory, string htmlPath, string baseDirectory,
            Rectangle bounds, double seconds, Action<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(pluginDirectory))
                {
                    log("ConsoleSplash: plugin directory unknown; external renderer skipped.");
                    return false;
                }

                // 渲染器放在 plugins 扫描路径之外，避免 LabAPI/EXILED 把它当成插件加载。
                // The renderer lives outside plugins/ so LabAPI/EXILED never scans it.
                string renderDll = FindRenderDll(pluginDirectory);
                if (renderDll == null)
                {
                    Trace("render dll missing under " + pluginDirectory);
                    log("ConsoleSplash: external renderer not found (ConsoleSplashRenderer / ConsoleSplash).");
                    return false;
                }

                if (!File.Exists(htmlPath))
                {
                    log("ConsoleSplash: html file not found at " + htmlPath);
                    return false;
                }

                Rectangle screen = ConsoleSplash.Shared.ConsoleWindow.PrimaryScreen();

                string script =
                    "Add-Type -Path '" + Escape(renderDll) + "';" +
                    "[ConsoleSplash.Render.Host]::Run('" + Escape(htmlPath) + "','" +
                    Escape(baseDirectory) + "'," + bounds.Left + "," + bounds.Top + "," +
                    bounds.Width + "," + bounds.Height + "," +
                    screen.Width + "," + screen.Height + "," +
                    seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";

                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                Trace("launching " + renderDll + " seconds=" + seconds + " bounds=" + bounds);

                ProcessStartInfo info = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encoded);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                using (Process process = Process.Start(info))
                {
                    if (process == null)
                    {
                        Trace("Process.Start returned null");
                        log("ConsoleSplash: failed to start the external renderer.");
                        return false;
                    }

                    Trace("started pid=" + process.Id);

                    int timeout = (int)((seconds + 25.0) * 1000.0);
                    DateTime started = DateTime.UtcNow;

                    // 不用 WaitForExit(timeout)：Unity Mono 上它可能不返回。
                    // Avoid WaitForExit(timeout); it can block forever on Unity Mono.
                    while (!process.HasExited)
                    {
                        if ((DateTime.UtcNow - started).TotalMilliseconds > timeout)
                        {
                            try { process.Kill(); } catch { }
                            Trace("timeout, killed");
                            log("ConsoleSplash: external renderer timed out.");
                            return false;
                        }

                        System.Threading.Thread.Sleep(50);
                    }

                    int exitCode = process.ExitCode;
                    Trace("exited code=" + exitCode);

                    bool ok = exitCode == 0;
                    if (!ok) log("ConsoleSplash: external renderer exit code " + exitCode);
                    else log("ConsoleSplash: external renderer finished.");
                    return ok;
                }
            }
            catch (Exception exception)
            {
                log("ConsoleSplash: external renderer failed: " + exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 依次查找：插件目录的上一级（LabAPI/EXILED）下的 ConsoleSplashRenderer，
        /// 再退回旧的 plugins/global/ConsoleSplash。返回 DLL 路径，找不到返回 null。
        /// Look next to the plugins folder first, then the legacy in-plugins copy.
        /// </summary>
        private static string FindRenderDll(string pluginDirectory)
        {
            string[] candidates = new string[2];

            try
            {
                // pluginDirectory = <LabAPI>\plugins\global
                string plugins = Path.GetDirectoryName(pluginDirectory);
                string root = plugins == null ? null : Path.GetDirectoryName(plugins);
                if (!string.IsNullOrEmpty(root))
                    candidates[0] = Path.Combine(Path.Combine(root, "ConsoleSplashRenderer"),
                        "ConsoleSplash.Render.dll");
            }
            catch
            {
            }

            try
            {
                candidates[1] = Path.Combine(Path.Combine(pluginDirectory, "ConsoleSplash"),
                    "ConsoleSplash.Render.dll");
            }
            catch
            {
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    if (!string.IsNullOrEmpty(candidates[i]) && File.Exists(candidates[i]))
                        return candidates[i];
                }
                catch
                {
                }
            }

            return null;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        /// <summary>独立诊断日志，Unity Mono 下单靠服务器日志可能看不到。</summary>
        private static void Trace(string message)
        {
            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SCP Secret Laboratory", "ConsoleSplash");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "renderer-launch.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
