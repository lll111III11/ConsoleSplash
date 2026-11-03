// ============================================================================
//  ConsoleSplash —— 插件版入口 / plugin-only entry point
//
//  只保留 DLL。默认动画是 animations/html-example/index.html，直接在控制台
//  窗口上播放；HTML 用内嵌 WebView2 渲染，失败时回退到同款预渲染帧。
//  不做全屏覆盖，只覆盖控制台窗口。
//
//  DLL only. The default animation is animations/html-example/index.html,
//  rendered over the console with embedded WebView2; a pre-rendered fallback
//  is used when WebView2 is unavailable. Never fullscreen.
//
//  Linux：加载后写一行日志并跳过，不影响服务器。
//  Linux: loads, logs one line and skips.
// ============================================================================
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using ConsoleSplash.Shared;
#if EXILED_BUILD
using Exiled.API.Enums;
using Exiled.API.Features;
#else
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
#endif

namespace ConsoleSplash.Plugin
{
    /// <summary>插件配置 / plugin configuration.</summary>
    public class SplashConfig
#if EXILED_BUILD
        : Exiled.API.Interfaces.IConfig
#endif
    {
        /// <summary>总开关 / master switch.</summary>
        public bool Enabled { get; set; } = true;

#if EXILED_BUILD
        public bool IsEnabled
        {
            get { return Enabled; }
            set { Enabled = value; }
        }

        public bool Debug { get; set; }
#endif

        /// <summary>加载时隐藏控制台，动画结束后恢复（旧行为，已停用）。</summary>
        public bool HideConsole { get; set; } = true;

        /// <summary>默认动画文件夹 / default animation folder.</summary>
        public string Animation { get; set; } = "html-example";

        /// <summary>动画时长（秒），0 = 使用素材自身长度；HTML 动画始终使用清单时长 / seconds, 0 = asset length; HTML uses its manifest timeline.</summary>
        public double Duration { get; set; } = 0.0;

        /// <summary>任意键跳过 / any key skips the animation.</summary>
        public bool SkipOnKey { get; set; } = true;

        /// <summary>启动时检查更新 / check for updates on startup.</summary>
        public bool CheckUpdates { get; set; } = true;

        /// <summary>更新源 JSON 地址；留空则只提示不检查 / update feed URL; empty disables the check.</summary>
        public string UpdateFeed { get; set; } =
            "https://api.github.com/repos/lll111III11/ConsoleSplash/releases/latest";

        /// <summary>发现新版本时下载 zip 到 updates 目录 / download the release zip when found.</summary>
        public bool DownloadUpdates { get; set; } = false;

        /// <summary>右下角显示跳过提示 / show the skip hint.</summary>
        public bool ShowHint { get; set; } = true;

        /// <summary>光带颜色 / accent color.</summary>
        public string Accent { get; set; } = "#7FDBFF";

        /// <summary>旧版缩放参数，已停用。</summary>
        public double Scale { get; set; } = 0.0;
    }

    internal static class SplashRuntime
    {
        internal static void Run(SplashConfig config, string configDirectory, Version version,
            Action<string> info, Action<string> warn)
        {
            if (!ConsoleWindow.IsWindows)
            {
                info("ConsoleSplash: non-Windows platform; skipped.");
                return;
            }

            ConsoleWindow.EnableDpiAwareness();

            SplashManifest manifest = ResolveManifest(config, configDirectory, info, warn);

            // HTML 动画有自己的完整时间线（开场/过渡/收尾），不用配置里的 duration 覆盖，
            // 否则会把结尾光幕截断。其它类型仍可按配置改时长。
            // HTML packs carry a fixed timeline; do not cut them with config.Duration.
            if (config.Duration > 0 && manifest.Kind != SplashKind.Html)
                manifest.Duration = config.Duration;
            if (!string.IsNullOrWhiteSpace(config.Accent)) manifest.Accent = config.Accent;
            manifest.SkipOnKey = config.SkipOnKey;

            SplashState state = new SplashState();
            state.Manifest = manifest;
            state.ShowHint = config.ShowHint;
            state.PluginDirectory = FindPluginDirectory(configDirectory);
            if (string.IsNullOrEmpty(state.PluginDirectory))
                state.PluginDirectory = SafeDirectory(Assembly.GetExecutingAssembly().Location);
            state.Info = info;
            state.Warn = warn;

            Thread thread = new Thread(PlayThread);
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = "ConsoleSplashOverlay";
            thread.Start(state);
            Trace("run animation=" + manifest.KindText + " asset=" + manifest.AssetPath);

            double seconds = (manifest.Duration > 0 ? manifest.Duration : 60.0) + manifest.Fade + 40.0;
            int timeout = (int)(seconds * 1000.0);
            if (timeout < 40000) timeout = 40000;
            state.Timeout = timeout;

            Thread watchdog = new Thread(WatchdogThread);
            watchdog.IsBackground = true;
            watchdog.Name = "ConsoleSplashWatchdog";
            watchdog.Start(state);

            if (config.CheckUpdates)
            {
                if (string.IsNullOrWhiteSpace(config.UpdateFeed))
                    info("ConsoleSplash: update check skipped (update_feed is empty).");
                else
                    UpdateChecker.Start(config.UpdateFeed.Trim(), version.ToString(),
                        config.DownloadUpdates, info, warn);
            }
        }

        /// <summary>
        /// 窗口创建、控制台隐藏、播放、销毁全部在这条 STA 线程里完成；
        /// 任何异常都只影响这一帧动画，不会波及服务器进程。
        /// Window creation, console hiding, playback and teardown all happen on
        /// this STA thread; a failure only skips the animation.
        /// </summary>
        private static void PlayThread(object parameter)
        {
            SplashState state = (SplashState)parameter;
            try
            {
                state.Info("ConsoleSplash: looking for the console window...");

                IntPtr console = ConsoleWindow.FindHostWindow();
                if (console != IntPtr.Zero)
                {
                    Rectangle target = ConsoleWindow.GetBounds(console);
                    if (target != Rectangle.Empty)
                    {
                        // 覆盖层直接贴在控制台当前位置，先弹出来盖住它。
                        // 不移动、不隐藏控制台，所以过渡时不会“抽一下”。
                        // The overlay sits exactly on the console's current rect and
                        // covers it first; the console is never moved or hidden.
                        state.Console = console;
                        state.Bounds = target;

                        state.Info("ConsoleSplash: target [" + ConsoleWindow.ClassNameOf(console) + "] "
                            + target.Width + "x" + target.Height + " at " + target.Left + "," + target.Top
                            + " | " + state.Manifest.KindText);
                        Trace("target h=" + console + " rect=" + target + " class=["
                            + ConsoleWindow.ClassNameOf(console) + "] pid=" + ConsoleWindow.ProcessIdOf(console)
                            + " title=[" + ConsoleWindow.TitleOfWindow(console) + "]");
                    }
                }

                if (state.Bounds == Rectangle.Empty)
                {
                    Rectangle centered = ConsoleWindow.CenterOnScreen(0.8);
                    if (centered == Rectangle.Empty)
                    {
                        state.Warn("ConsoleSplash: no display available; skipped.");
                        return;
                    }

                    state.Bounds = centered;
                    state.Info("ConsoleSplash: no console window (Steam/headless); playing centered "
                        + centered.Width + "x" + centered.Height + " | " + state.Manifest.KindText);
                }

                PlayBestEffort(state);
            }
            catch (Exception exception)
            {
                state.Warn("ConsoleSplash: " + exception.Message);
                Trace("failed: " + exception.Message);
            }
            finally
            {
                state.Finished = true;
                Trace("finished rect=" + ConsoleWindow.GetBounds(state.Console));
            }
        }

        private static void PlayBestEffort(SplashState state)
        {
            SplashManifest manifest = state.Manifest;

            if (manifest.Kind == SplashKind.Script || !string.IsNullOrEmpty(manifest.MainClass))
            {
                state.Warn("ConsoleSplash: scripts/Java are not supported in the plugin build; using the fallback.");
                PlayFallback(state);
                return;
            }

            if (manifest.Kind == SplashKind.Html)
            {
                state.Info("ConsoleSplash: launching out-of-process renderer...");
                Trace("play html manifestDuration=" + manifest.Duration);
                bool ok = ExternalRenderer.Play(state.PluginDirectory, manifest.AssetPath,
                    manifest.BaseDirectory, state.Bounds,
                    manifest.Duration > 0 ? manifest.Duration : 3.5, state.Info);

                if (ok) return;

                state.Warn("ConsoleSplash: external renderer failed; using the pre-rendered frames.");
                PlayFallback(state);
                return;
            }

            if (manifest.Kind == SplashKind.Video || !string.IsNullOrEmpty(manifest.Audio))
            {
                PlayFallback(state);
                return;
            }

            Win32Splash.Play(manifest, state.Bounds, state.ShowHint, state.Info,
                IntPtr.Zero, false);
        }

        private static void PlayFallback(SplashState state)
        {
            SplashManifest fallback = ResolveFallback(state.Manifest);
            if (fallback == null)
            {
                fallback = SplashManifest.BuiltinDefaults();
                fallback.Accent = state.Manifest.Accent;
                fallback.Background = state.Manifest.Background;
            }

            fallback.SkipOnKey = state.Manifest.SkipOnKey;
            if (state.Manifest.Duration > 0) fallback.Duration = state.Manifest.Duration;
            fallback.Hint = state.ShowHint;

            Win32Splash.Play(fallback, state.Bounds, state.ShowHint, state.Info,
                IntPtr.Zero, false);
        }

        private static SplashManifest ResolveFallback(SplashManifest manifest)
        {
            if (string.IsNullOrEmpty(manifest.Fallback)) return null;

            string path = manifest.ResolvePath(manifest.Fallback);
            if (Directory.Exists(path)) return SplashManifest.FromDirectory(path);
            if (File.Exists(path)) return SplashManifest.ForAsset(path);

            return null;
        }

        private static void WatchdogThread(object parameter)
        {
            SplashState state = (SplashState)parameter;
            Thread.Sleep(state.Timeout);

            if (state.Finished) return;

            state.Warn("ConsoleSplash: watchdog closed the splash.");
            state.Finished = true;
        }

        private static SplashManifest ResolveManifest(SplashConfig config, string configDirectory,
            Action<string> info, Action<string> warn)
        {
            string value = string.IsNullOrWhiteSpace(config.Animation)
                ? "html-example"
                : config.Animation.Trim();

            if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
                value = "html-example";

            string configAnimations = string.IsNullOrEmpty(configDirectory)
                ? null
                : Path.Combine(configDirectory, "animations");

            string pluginDirectory = SafeDirectory(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(pluginDirectory))
                pluginDirectory = FindPluginDirectory(configDirectory);

            string pluginAnimations = string.IsNullOrEmpty(pluginDirectory)
                ? null
                : Path.Combine(pluginDirectory, "animations");

            info("ConsoleSplash: animation search: " + (configAnimations ?? "<null>")
                + " | " + (pluginAnimations ?? "<null>"));

            string[] roots = new string[] { configAnimations, pluginAnimations };
            string path = null;

            for (int i = 0; i < roots.Length && path == null; i++)
            {
                if (string.IsNullOrEmpty(roots[i])) continue;
                string candidate = Path.Combine(roots[i], value);
                if (Directory.Exists(candidate) || File.Exists(candidate))
                    path = candidate;
            }

            if (path == null && !Path.IsPathRooted(value))
            {
                if (!string.IsNullOrEmpty(configDirectory))
                {
                    string candidate = Path.Combine(configDirectory, value);
                    if (Directory.Exists(candidate) || File.Exists(candidate)) path = candidate;
                }
                if (path == null && !string.IsNullOrEmpty(pluginDirectory))
                {
                    string candidate = Path.Combine(pluginDirectory, value);
                    if (Directory.Exists(candidate) || File.Exists(candidate)) path = candidate;
                }
            }

            if (path == null && Path.IsPathRooted(value)) path = value;

            if (path != null)
            {
                if (Directory.Exists(path)) return SplashManifest.FromDirectory(path);
                if (File.Exists(path)) return SplashManifest.Load(path);
            }

            warn("ConsoleSplash: animation not found (" + value + "); put the animations folder next to the DLL "
                + "or under the plugin config directory. Using the built-in sweep.");
            return SplashManifest.BuiltinDefaults();
        }

        private static string SafeDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>独立诊断日志（服务器日志里看不到的部分）。</summary>
        private static void Trace(string message)
        {
            try
            {
                lock (TraceLock)
                {
                    string directory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SCP Secret Laboratory", "ConsoleSplash");
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, "plugin.log"),
                        DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private static readonly object TraceLock = new object();

        private static string FindPluginDirectory(string configDirectory)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string labApi = Path.Combine(baseDirectory, "LabAPI", "plugins", "global");
                if (Directory.Exists(labApi)) return labApi;

                string exiled = Path.Combine(baseDirectory, "EXILED", "Plugins");
                if (Directory.Exists(exiled)) return exiled;
            }
            catch
            {
            }

            try
            {
                if (string.IsNullOrEmpty(configDirectory)) return null;

                DirectoryInfo directory = new DirectoryInfo(configDirectory);
                while (directory != null)
                {
                    if (string.Equals(directory.Name, "LabAPI", StringComparison.OrdinalIgnoreCase))
                        return Path.Combine(directory.FullName, "plugins", "global");
                    if (string.Equals(directory.Name, "EXILED", StringComparison.OrdinalIgnoreCase))
                        return Path.Combine(directory.FullName, "Plugins");

                    directory = directory.Parent;
                }
            }
            catch
            {
            }

            return null;
        }

        private sealed class SplashState
        {
            public IntPtr Console;
            public Rectangle Bounds;
            public SplashManifest Manifest;
            public bool ShowHint;
            public string PluginDirectory;
            public int Timeout;
            public Action<string> Info;
            public Action<string> Warn;

            public volatile bool Finished;
        }
    }

#if EXILED_BUILD
    /// <summary>EXILED 版入口 / EXILED build entry point.</summary>
    public class ConsoleSplashExiled : Plugin<SplashConfig>
    {
        public override string Name
        {
            get { return "0ConsoleSplash"; }
        }

        public override string Author
        {
            get { return "LLL111III11"; }
        }

        public override Version Version
        {
            get { return new Version(1, 1, 0); }
        }

        public override Version RequiredExiledVersion
        {
            get { return new Version(9, 0, 0); }
        }

        public override bool IgnoreRequiredVersionCheck
        {
            get { return true; }
        }

        public override PluginPriority Priority
        {
            get { return PluginPriority.Highest; }
        }

        public override void OnEnabled()
        {
            if (!ConsoleWindow.IsWindows)
            {
                Log.Info("ConsoleSplash: overlay is Windows-only; skipped on this platform.");
                return;
            }

            string directory = null;
            try
            {
                if (ConfigPath != null) directory = Path.GetDirectoryName(ConfigPath);
            }
            catch
            {
            }

            SplashRuntime.Run(Config, directory, Version,
                delegate(string message) { Log.Info(message); },
                delegate(string message) { Log.Warn(message); });
        }

        public override void OnDisabled()
        {
        }
    }
#else
    /// <summary>LabAPI 版入口 / LabAPI build entry point.</summary>
    public class ConsoleSplashPlugin : Plugin<SplashConfig>
    {
        public override string Name
        {
            get { return "0ConsoleSplash"; }
        }

        public override string Description
        {
            get { return "控制台开启动画 / console startup animation"; }
        }

        public override string Author
        {
            get { return "LLL111III11"; }
        }

        public override Version Version
        {
            get { return new Version(1, 1, 0); }
        }

        public override Version RequiredApiVersion
        {
            get { return LabApi.Features.LabApiProperties.CurrentVersion; }
        }

        public override LoadPriority Priority
        {
            get { return LoadPriority.Highest; }
        }

        public override void Enable()
        {
            if (!ConsoleWindow.IsWindows)
            {
                Logger.Info("ConsoleSplash: overlay is Windows-only; skipped on this platform.");
                return;
            }

            string directory = null;
            try
            {
                directory = LabApi.Loader.ConfigurationLoader.GetConfigDirectory(this).FullName;
            }
            catch
            {
            }

            SplashRuntime.Run(Config, directory, Version, Logger.Info, Logger.Warn);
        }

        public override void Disable()
        {
        }
    }
#endif
}
