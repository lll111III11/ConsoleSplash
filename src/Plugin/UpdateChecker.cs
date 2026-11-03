// ============================================================================
//  ConsoleSplash —— 更新检查 / update checker
//
//  启动后在后台读取一个 JSON 更新源，和当前插件版本比较。
//  默认只提示，不自动安装；配置里可以直接关掉检查或只关闭下载。
//
//  Reads a JSON update feed in the background. It only reports by default and
//  never replaces the loaded DLL; the config can disable the check or the
//  download independently.
//
//  支持的更新源格式 / supported feed shapes:
//    { "version": "1.1.0", "url": "https://.../ConsoleSplash-1.1.0.zip", "notes": "..." }
//    GitHub /releases/latest: tag_name, html_url, body, assets[].browser_download_url
// ============================================================================
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleSplash.Plugin
{
    internal static class UpdateChecker
    {
        private const int TimeoutMs = 8000;
        private const long MaxDownloadBytes = 32L * 1024 * 1024;

        /// <summary>后台启动检查，不阻塞插件加载 / start the check without blocking.</summary>
        internal static void Start(string feedUrl, string currentVersion, bool download,
            Action<string> info, Action<string> warn)
        {
            Thread thread = new Thread(delegate()
            {
                Check(feedUrl, currentVersion, download, info, warn);
            });
            thread.IsBackground = true;
            thread.Name = "ConsoleSplashUpdateCheck";
            thread.Start();
        }

        private static void Check(string feedUrl, string currentVersion, bool download,
            Action<string> info, Action<string> warn)
        {
            try
            {
                EnableTls();

                string json = ReadFeed(feedUrl);
                if (json == null) return;

                string latest = FirstMatch(json, "version", "tag_name");
                Version latestVersion = ParseVersion(latest);
                Version current = ParseVersion(currentVersion);

                if (latestVersion == null || current == null)
                {
                    warn("ConsoleSplash: update feed has no readable version.");
                    return;
                }

                if (latestVersion <= current)
                {
                    info("ConsoleSplash: up to date (" + current + ").");
                    return;
                }

                string notes = FirstMatch(json, "notes", "body");
                info("ConsoleSplash: update available " + latestVersion + " (current " + current + ").");
                if (!string.IsNullOrEmpty(notes))
                {
                    notes = notes.Replace("\\n", " ").Replace("\r", " ").Replace("\n", " ");
                    if (notes.Length > 160) notes = notes.Substring(0, 160) + "...";
                    info("ConsoleSplash: " + notes);
                }

                string page = FirstMatch(json, "html_url", "url");
                if (!string.IsNullOrEmpty(page))
                    info("ConsoleSplash: release page: " + page);

                if (!download)
                {
                    info("ConsoleSplash: download_updates is false; not downloading.");
                    return;
                }

                string asset = FirstMatch(json, "browser_download_url", "url");
                if (string.IsNullOrEmpty(asset) || !asset.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    warn("ConsoleSplash: no .zip asset in the update feed; open the release page manually.");
                    return;
                }

                string path = Download(asset, latestVersion.ToString());
                if (path != null)
                    info("ConsoleSplash: update downloaded to " + path
                        + " (replace after stopping the server).");
            }
            catch (Exception exception)
            {
                warn("ConsoleSplash: update check failed: " + exception.Message);
            }
        }

        private static string ReadFeed(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.UserAgent = "ConsoleSplash/1.0.0";
            request.Accept = "application/json";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                return reader.ReadToEnd();
        }

        private static string Download(string url, string version)
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "ConsoleSplash", "updates", version);
            Directory.CreateDirectory(directory);

            string name = Path.GetFileName(new Uri(url).AbsolutePath);
            if (string.IsNullOrEmpty(name)) name = "ConsoleSplash-" + version + ".zip";
            string path = Path.Combine(directory, name);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            request.UserAgent = "ConsoleSplash/1.0.0";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream input = response.GetResponseStream())
            using (FileStream output = File.Create(path))
            {
                byte[] buffer = new byte[8192];
                long total = 0;
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    if (total > MaxDownloadBytes)
                        throw new InvalidOperationException("update archive is larger than 32 MB");
                    output.Write(buffer, 0, read);
                }
            }

            return path;
        }

        private static void EnableTls()
        {
            try
            {
                // SecurityProtocolType.Tls12 = 3072；旧 Mono 没有这个枚举名，用数值。
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            }
            catch
            {
            }
        }

        private static string FirstMatch(string text, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Match match = Regex.Match(text, "\"" + Regex.Escape(keys[i]) + "\"\\s*:\\s*\"([^\"]*)\"");
                if (match.Success) return match.Groups[1].Value;
            }

            return null;
        }

        private static Version ParseVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            value = value.Trim().TrimStart('v', 'V');
            int cut = value.IndexOfAny(new char[] { '-', '+', ' ' });
            if (cut >= 0) value = value.Substring(0, cut);

            try
            {
                return new Version(value);
            }
            catch
            {
                return null;
            }
        }
    }
}
