// ============================================================================
//  ConsoleSplash —— 动画清单解析 / animation manifest parser
//
//  默认读取 splash.java（声明式，不执行 Java 逻辑），也接受 .txt / .ini。
//  Reads splash.java by default (declarative, no Java logic is executed); .txt
//  and .ini are accepted too.
//
//  支持的字段 / supported fields:
//    name, animation, duration, fade, fps, loop, skipOnKey, background,
//    accent, audio, volume, hint, script, main
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleSplash.Shared
{
    /// <summary>动画类型 / Animation kind.</summary>
    public enum SplashKind
    {
        Builtin,
        Gif,
        Frames,
        Image,
        Html,
        Video,
        Script
    }

    /// <summary>一个动画包的清单 / Manifest of one animation package.</summary>
    public sealed class SplashManifest
    {
        public string SourcePath = string.Empty;
        public string BaseDirectory = string.Empty;
        public string Name = string.Empty;
        public string Animation = "builtin:light-sweep";
        public double Duration = 3.0;
        public double Fade = 0.45;
        public int Fps = 30;
        public bool Loop = false;
        public bool SkipOnKey = true;
        public string Background = "#000000";
        public string Accent = "#7FDBFF";
        public string Audio = string.Empty;
        public double Volume = 0.8;
        public bool Hint = false;

        /// <summary>动画包声明的唯一 ID / unique id declared by the pack.</summary>
        public string Uiid = string.Empty;

        /// <summary>
        /// 覆盖层是否做“铺满屏幕再缩回”的平滑过渡；false = 只贴在控制台上。
        /// false keeps the overlay exactly on the console for the whole animation.
        /// </summary>
        public bool Smooth = true;

        /// <summary>
        /// 播放前截屏当背景：空/false = 不截，screen = 整屏，console = 控制台矩形。
        /// Backdrop capture mode: empty/false, "screen" or "console".
        /// </summary>
        public string Backdrop = string.Empty;

        /// <summary>把 Backdrop 文本转成模式：0 无，1 整屏，2 控制台矩形。</summary>
        public int BackdropMode
        {
            get
            {
                string value = (Backdrop ?? string.Empty).Trim().ToLowerInvariant();
                if (value == "console" || value == "window") return 2;
                if (value == "screen" || value == "true" || value == "on" || value == "yes" || value == "1") return 1;
                return 0;
            }
        }

        /// <summary>HTML/视频无法渲染时的回退素材 / fallback asset when HTML/video cannot render.</summary>
        public string Fallback = string.Empty;

        /// <summary>可选：Java 主类或 .java 文件 / optional Java main class or .java file.</summary>
        public string MainClass = string.Empty;

        /// <summary>可选：外部脚本 / optional external script.</summary>
        public string Script = string.Empty;

        public SplashKind Kind { get; private set; }
        public string AssetPath { get; private set; }
        public List<string> FrameFiles { get; private set; }
        public string ResolutionError { get; private set; }

        private static readonly Regex JavaField = new Regex(
            @"(?m)^\s*(?:(?:public|private|protected|static|final|readonly|transient|volatile)\s+)*" +
            @"(?:String|string|double|float|int|long|short|byte|bool|boolean|var|Object)\s+" +
            @"([A-Za-z_]\w*)\s*=\s*([^;]+);",
            RegexOptions.Compiled);

        private static readonly Regex KeyValueField = new Regex(
            @"(?m)^\s*([A-Za-z_][\w.-]*)\s*[:=]\s*(.+?)\s*$",
            RegexOptions.Compiled);

        public SplashManifest()
        {
            FrameFiles = new List<string>();
            AssetPath = string.Empty;
            ResolutionError = string.Empty;
        }

        /// <summary>没有素材时的默认内置动画 / built-in animation with no assets.</summary>
        public static SplashManifest BuiltinDefaults()
        {
            SplashManifest manifest = new SplashManifest();
            manifest.Kind = SplashKind.Builtin;
            manifest.Name = "Light Sweep";
            manifest.Animation = "builtin:light-sweep";
            return manifest;
        }

        /// <summary>把一个素材文件直接作为动画（不按清单解析）/ treat a file as an animation asset.</summary>
        public static SplashManifest ForAsset(string path)
        {
            SplashManifest manifest = new SplashManifest();
            if (string.IsNullOrWhiteSpace(path)) return manifest;

            manifest.SourcePath = Path.GetFullPath(path);
            manifest.BaseDirectory = Path.GetDirectoryName(manifest.SourcePath);
            manifest.Animation = Path.GetFileName(path);
            manifest.Resolve();
            return manifest;
        }

        /// <summary>从文件或文件夹加载 / load from a file or a directory.</summary>
        public static SplashManifest Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return BuiltinDefaults();
            if (Directory.Exists(path)) return FromDirectory(path);
            if (!File.Exists(path)) return BuiltinDefaults();

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".java" || extension == ".txt" || extension == ".ini" || extension == ".conf")
                return LoadManifestFile(path);

            SplashManifest manifest = new SplashManifest();
            manifest.SourcePath = Path.GetFullPath(path);
            manifest.BaseDirectory = Path.GetDirectoryName(manifest.SourcePath);
            manifest.Animation = Path.GetFileName(path);
            manifest.Resolve();
            return manifest;
        }

        /// <summary>从一个动画文件夹加载：优先 splash.java，其次 index.html / gif / frames。</summary>
        public static SplashManifest FromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return BuiltinDefaults();

            string full = Path.GetFullPath(directory);
            string[] manifests = { "splash.java", "splash.txt", "splash.ini", "manifest.java" };
            for (int i = 0; i < manifests.Length; i++)
            {
                string candidate = Path.Combine(full, manifests[i]);
                if (File.Exists(candidate)) return LoadManifestFile(candidate);
            }

            SplashManifest auto = new SplashManifest();
            auto.SourcePath = full;
            auto.BaseDirectory = full;

            string index = Path.Combine(full, "index.html");
            if (File.Exists(index))
            {
                auto.Animation = "index.html";
                auto.Resolve();
                return auto;
            }

            string frames = Path.Combine(full, "frames");
            if (Directory.Exists(frames))
            {
                auto.Animation = "frames";
                auto.Resolve();
                if (auto.Kind == SplashKind.Frames) return auto;
            }

            string[] gifs = Directory.GetFiles(full, "*.gif", SearchOption.TopDirectoryOnly);
            if (gifs.Length > 0)
            {
                Array.Sort(gifs, NaturalComparer.Instance);
                auto.Animation = Path.GetFileName(gifs[0]);
                auto.Resolve();
                return auto;
            }

            string[] videos = Directory.GetFiles(full, "*.mp4", SearchOption.TopDirectoryOnly);
            if (videos.Length == 0) videos = Directory.GetFiles(full, "*.webm", SearchOption.TopDirectoryOnly);
            if (videos.Length > 0)
            {
                Array.Sort(videos, NaturalComparer.Instance);
                auto.Animation = Path.GetFileName(videos[0]);
                auto.Resolve();
                return auto;
            }

            auto.Kind = SplashKind.Builtin;
            auto.Animation = "builtin:light-sweep";
            return auto;
        }

        private static SplashManifest LoadManifestFile(string path)
        {
            SplashManifest manifest = new SplashManifest();
            manifest.SourcePath = Path.GetFullPath(path);
            manifest.BaseDirectory = Path.GetDirectoryName(manifest.SourcePath);

            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                manifest.Resolve();
                return manifest;
            }

            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".java")
            {
                foreach (Match match in JavaField.Matches(text))
                    manifest.Apply(match.Groups[1].Value, match.Groups[2].Value);
            }
            else
            {
                foreach (Match match in KeyValueField.Matches(text))
                    manifest.Apply(match.Groups[1].Value, match.Groups[2].Value);
            }

            manifest.Resolve();
            return manifest;
        }

        private void Apply(string key, string raw)
        {
            if (string.IsNullOrEmpty(key)) return;
            string value = CleanValue(raw);

            switch (key.Trim().ToLowerInvariant())
            {
                case "name":
                case "title":
                    Name = value;
                    break;
                case "animation":
                case "source":
                    Animation = value;
                    break;
                case "duration":
                case "seconds":
                    Duration = ParseDouble(value, Duration);
                    break;
                case "fade":
                case "fadeduration":
                    Fade = ParseDouble(value, Fade);
                    break;
                case "fps":
                case "framerate":
                    Fps = (int)ParseDouble(value, Fps);
                    break;
                case "loop":
                    Loop = ParseBool(value, Loop);
                    break;
                case "skiponkey":
                case "skip":
                    SkipOnKey = ParseBool(value, SkipOnKey);
                    break;
                case "background":
                case "bg":
                    Background = value;
                    break;
                case "accent":
                case "color":
                    Accent = value;
                    break;
                case "audio":
                case "music":
                    Audio = value;
                    break;
                case "volume":
                    Volume = ParseDouble(value, Volume);
                    break;
                case "hint":
                case "showhint":
                    Hint = ParseBool(value, Hint);
                    break;
                case "uiid":
                case "ui-id":
                case "id":
                    Uiid = value;
                    break;
                case "backdrop":
                case "showbackdrop":
                case "capture":
                    Backdrop = value;
                    break;
                case "smooth":
                case "smoothopen":
                case "expand":
                    Smooth = ParseBool(value, Smooth);
                    break;
                case "fallback":
                case "fallbackanimation":
                    Fallback = value;
                    break;
                case "script":
                case "external":
                    Script = value;
                    break;
                case "main":
                case "mainclass":
                    MainClass = value;
                    break;
            }
        }

        private static string CleanValue(string raw)
        {
            string value = (raw ?? string.Empty).Trim();
            if (value.EndsWith(";", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 1).Trim();

            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                    value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }

        private static double ParseDouble(string value, double fallback)
        {
            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    return fallback;
            }
        }

        private void Resolve()
        {
            Kind = SplashKind.Builtin;
            AssetPath = string.Empty;
            FrameFiles.Clear();
            ResolutionError = string.Empty;

            if (!string.IsNullOrEmpty(Script))
            {
                if (ResolveScript(Script)) return;
            }

            string value = (Animation ?? string.Empty).Trim();
            if (value.StartsWith("script:", StringComparison.OrdinalIgnoreCase))
            {
                if (ResolveScript(value.Substring(7))) return;
                value = "builtin:light-sweep";
            }

            if (value.Length == 0 || value.StartsWith("builtin", StringComparison.OrdinalIgnoreCase))
            {
                Kind = SplashKind.Builtin;
                return;
            }

            string path = value;
            if (!Path.IsPathRooted(path) && !string.IsNullOrEmpty(BaseDirectory))
                path = Path.Combine(BaseDirectory, path);

            if (Directory.Exists(path))
            {
                AssetPath = Path.GetFullPath(path);
                LoadFrames(AssetPath);
                if (FrameFiles.Count > 0)
                {
                    Kind = SplashKind.Frames;
                    return;
                }

                string[] gifs = Directory.GetFiles(AssetPath, "*.gif", SearchOption.TopDirectoryOnly);
                if (gifs.Length > 0)
                {
                    Array.Sort(gifs, NaturalComparer.Instance);
                    AssetPath = gifs[0];
                    Kind = SplashKind.Gif;
                    return;
                }

                ResolutionError = "directory has no frames: " + value;
                Kind = SplashKind.Builtin;
                return;
            }

            if (File.Exists(path))
            {
                ClassifyFile(path);
                return;
            }

            ResolutionError = "animation not found: " + value;
            Kind = SplashKind.Builtin;
        }

        private bool ResolveScript(string scriptValue)
        {
            string path = scriptValue;
            if (!Path.IsPathRooted(path) && !string.IsNullOrEmpty(BaseDirectory))
                path = Path.Combine(BaseDirectory, path);

            if (!File.Exists(path))
            {
                ResolutionError = "script not found: " + scriptValue;
                return false;
            }

            AssetPath = Path.GetFullPath(path);
            Kind = SplashKind.Script;
            return true;
        }

        private void ClassifyFile(string path)
        {
            AssetPath = Path.GetFullPath(path);
            string extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension)
            {
                case ".gif":
                    Kind = SplashKind.Gif;
                    return;
                case ".html":
                case ".htm":
                    Kind = SplashKind.Html;
                    return;
                case ".mp4":
                case ".webm":
                case ".m4v":
                case ".mov":
                    Kind = SplashKind.Video;
                    return;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                    Kind = SplashKind.Image;
                    return;
                case ".py":
                case ".js":
                case ".bat":
                case ".cmd":
                case ".ps1":
                case ".exe":
                case ".java":
                    Kind = SplashKind.Script;
                    return;
                default:
                    ResolutionError = "unsupported animation type: " + extension;
                    Kind = SplashKind.Builtin;
                    return;
            }
        }

        private void LoadFrames(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
            List<string> frames = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLowerInvariant();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp")
                    frames.Add(files[i]);
            }

            frames.Sort(NaturalComparer.Instance);
            FrameFiles.AddRange(frames);
        }

        /// <summary>相对路径解析 / resolve a relative path against the package directory.</summary>
        public string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (Path.IsPathRooted(path)) return path;
            if (string.IsNullOrEmpty(BaseDirectory)) return path;
            return Path.Combine(BaseDirectory, path);
        }

        /// <summary>把音频字段补全为绝对路径，缺失则清空 / resolve audio and clear it when missing.</summary>
        public void ResolveAudio()
        {
            if (string.IsNullOrEmpty(Audio)) return;
            string path = ResolvePath(Audio);
            if (File.Exists(path)) Audio = Path.GetFullPath(path);
            else Audio = string.Empty;
        }

        /// <summary>强制回退到内置光扫 / force the built-in fallback.</summary>
        public void ForceBuiltin()
        {
            Kind = SplashKind.Builtin;
            AssetPath = string.Empty;
            Animation = "builtin:light-sweep";
            ResolutionError = string.Empty;
        }

        public bool IsNativeKind
        {
            get
            {
                return Kind == SplashKind.Builtin || Kind == SplashKind.Gif
                    || Kind == SplashKind.Frames || Kind == SplashKind.Image;
            }
        }

        public string KindText
        {
            get
            {
                switch (Kind)
                {
                    case SplashKind.Gif: return "gif";
                    case SplashKind.Frames: return "frames(" + FrameFiles.Count + ")";
                    case SplashKind.Image: return "image";
                    case SplashKind.Html: return "html";
                    case SplashKind.Video: return "video";
                    case SplashKind.Script: return "script";
                    default: return "builtin";
                }
            }
        }
    }

    /// <summary>按数字排序帧文件名 / natural sort for frame file names.</summary>
    public sealed class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new NaturalComparer();

        public int Compare(string x, string y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            int i = 0;
            int j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                {
                    int si = i;
                    int sj = j;
                    while (i < x.Length && char.IsDigit(x[i])) i++;
                    while (j < y.Length && char.IsDigit(y[j])) j++;

                    string nx = StripLeadingZeros(x.Substring(si, i - si));
                    string ny = StripLeadingZeros(y.Substring(sj, j - sj));
                    if (nx.Length != ny.Length) return nx.Length - ny.Length;

                    int cmp = string.Compare(nx, ny, StringComparison.Ordinal);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    int cmp = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                    if (cmp != 0) return cmp;
                    i++;
                    j++;
                }
            }

            return (x.Length - i) - (y.Length - j);
        }

        /// <summary>
        /// 手写去前导零：Unity Mono 的 mscorlib 没有 TrimStart(char) 单字符重载。
        /// Hand-rolled: the Unity Mono mscorlib has no single-char TrimStart overload.
        /// </summary>
        private static string StripLeadingZeros(string value)
        {
            int start = 0;
            while (start < value.Length && value[start] == '0') start++;
            return start == 0 ? value : value.Substring(start);
        }
    }
}
