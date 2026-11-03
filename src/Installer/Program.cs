// ============================================================================
//  ConsoleSplash Setup —— 安装器 / installer
//
//  1. 检查系统是否受支持（Windows 版本、.NET Framework 4.8、WebView2、框架目录）。
//  2. 不支持时按当前语言显示原因，不写任何文件。
//  3. 支持时调用系统自带的解压（tar.exe → PowerShell Expand-Archive → ZipFile）
//     释放同目录的压缩包，然后把 DLL、animations、渲染器放到正确位置。
//
//  也支持无界面自检：
//      ConsoleSplashSetup.exe --check [--lang=zh-CN]
//      ConsoleSplashSetup.exe --install --silent [--lang=ru]
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ConsoleSplash.Setup
{
    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static int Main(string[] args)
        {
            // winexe 没有控制台；命令行模式挂到父控制台才能看到结果。
            if (HasFlag(args, "--check") || HasFlag(args, "--silent"))
            {
                if (!AttachConsole(-1)) AllocConsole();
                StreamWriter writer = new StreamWriter(Console.OpenStandardOutput());
                writer.AutoFlush = true;
                Console.SetOut(writer);
            }

            bool checkOnly = false;
            bool install = false;
            bool silent = false;
            string language = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--check") checkOnly = true;
                else if (arg == "--install") install = true;
                else if (arg == "--silent") silent = true;
                else if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                    language = arg.Substring(7);
            }

            Texts texts = Texts.For(language);

            if (checkOnly)
                return RunCheck(texts, language);

            if (install && silent)
                return RunSilentInstall(texts);

            // 125%/150% 缩放下保持清晰；否则整个窗口会被位图拉伸。
            try { SetProcessDPIAware(); } catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm(texts, language));
            return 0;
        }

        // ------------------------------------------------------------------
        //  自检 / check
        // ------------------------------------------------------------------

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static int RunCheck(Texts texts, string language)
        {
            EnvironmentInfo info = EnvironmentInfo.Detect();

            Console.WriteLine("LANG=" + texts.Code);
            Console.WriteLine("OS=" + info.OsVersion + " ok=" + info.OsOk);
            Console.WriteLine("DOTNET48=" + info.DotNetRelease + " ok=" + info.DotNetOk);
            Console.WriteLine("WEBVIEW2=" + info.WebView2);
            Console.WriteLine("TAR=" + info.TarAvailable);
            Console.WriteLine("LABAPI=" + info.LabApiRoot);
            Console.WriteLine("EXILED=" + info.ExiledRoot);
            Console.WriteLine("SUPPORTED=" + info.Supported);

            if (!info.Supported)
            {
                for (int i = 0; i < info.Blockers.Count; i++)
                    Console.WriteLine("BLOCKER=" + info.Blockers[i]);
            }

            return info.Supported ? 0 : 1;
        }

        private static int RunSilentInstall(Texts texts)
        {
            EnvironmentInfo info = EnvironmentInfo.Detect();
            if (!info.Supported)
            {
                Console.Error.WriteLine(texts.Unsupported);
                return 2;
            }

            List<string> log = new List<string>();
            bool ok = Installer.Run(info, texts, delegate(string line) { log.Add(line); });
            for (int i = 0; i < log.Count; i++) Console.WriteLine(log[i]);
            return ok ? 0 : 3;
        }
    }

    // ----------------------------------------------------------------------
    //  环境检测 / environment detection
    // ----------------------------------------------------------------------

    internal sealed class EnvironmentInfo
    {
        internal Version OsVersion;
        internal bool OsOk;
        internal int DotNetRelease;
        internal bool DotNetOk;
        internal bool WebView2;
        internal bool TarAvailable;
        internal string LabApiRoot;
        internal string ExiledRoot;
        internal List<string> Blockers = new List<string>();

        internal bool HasFramework
        {
            get { return LabApiRoot != null || ExiledRoot != null; }
        }

        internal bool Supported
        {
            get { return OsOk && DotNetOk && HasFramework && Blockers.Count == 0; }
        }

        internal static EnvironmentInfo Detect()
        {
            EnvironmentInfo info = new EnvironmentInfo();

            // Environment.OSVersion 在无 Win10 清单时会谎报 6.2，这里直接问 ntdll。
            info.OsVersion = RealOsVersion();
            info.OsOk = info.OsVersion.Major >= 10;
            if (!info.OsOk) info.Blockers.Add("os");

            info.DotNetRelease = ReadDotNetRelease();
            info.DotNetOk = info.DotNetRelease >= 528040; // .NET Framework 4.8
            if (!info.DotNetOk) info.Blockers.Add("dotnet");

            info.WebView2 = HasWebView2();
            info.TarAvailable = File.Exists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe"));

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sl = Path.Combine(appData, "SCP Secret Laboratory");
            string labApi = Path.Combine(sl, "LabAPI");
            string exiled = Path.Combine(sl, "EXILED");

            if (Directory.Exists(labApi)) info.LabApiRoot = labApi;
            if (Directory.Exists(exiled)) info.ExiledRoot = exiled;
            if (!info.HasFramework) info.Blockers.Add("framework");

            return info;
        }

        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct OsVersionInfo
        {
            public int Size;
            public int Major;
            public int Minor;
            public int Build;
            public int Platform;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 128)]
            public string ServicePack;
        }

        [System.Runtime.InteropServices.DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OsVersionInfo info);

        private static Version RealOsVersion()
        {
            try
            {
                OsVersionInfo info = new OsVersionInfo();
                info.Size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(OsVersionInfo));
                if (RtlGetVersion(ref info) == 0)
                    return new Version(info.Major, info.Minor, info.Build);
            }
            catch
            {
            }

            return Environment.OSVersion.Version;
        }

        private static int ReadDotNetRelease()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key == null) return 0;
                    object value = key.GetValue("Release");
                    if (value == null) return 0;
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasWebView2()
        {
            string[] probes = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Microsoft\EdgeWebView\Application"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\EdgeWebView\Application")
            };

            for (int i = 0; i < probes.Length; i++)
            {
                try
                {
                    if (!Directory.Exists(probes[i])) continue;
                    if (Directory.GetDirectories(probes[i]).Length > 0) return true;
                }
                catch
                {
                }
            }

            return false;
        }
    }

    // ----------------------------------------------------------------------
    //  安装逻辑 / install logic
    // ----------------------------------------------------------------------

    internal static class Installer
    {
        internal static bool Run(EnvironmentInfo info, Texts texts, Action<string> log)
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string staging = null;

            try
            {
                // 1. 找到载荷：同目录的 zip 优先，否则就是已经解压好的目录。
                string zip = FindPackage(exeDirectory);
                string source;

                if (zip != null)
                {
                    log(texts.Extracting + " " + Path.GetFileName(zip));
                    staging = Path.Combine(Path.GetTempPath(),
                        "ConsoleSplashSetup", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    Directory.CreateDirectory(staging);

                    if (!Extract(zip, staging, log))
                    {
                        log(texts.ExtractFailed);
                        return false;
                    }

                    source = NormaliseRoot(staging);
                }
                else
                {
                    log(texts.NoArchive);
                    source = exeDirectory;
                }

                // 2. 逐个框架投放。
                bool any = false;

                if (info.LabApiRoot != null)
                {
                    any |= Deploy(source, info.LabApiRoot, "plugins\\global", true, texts, log);
                }

                if (info.ExiledRoot != null)
                {
                    any |= Deploy(source, info.ExiledRoot, "Plugins", false, texts, log);
                }

                if (!any)
                {
                    log(texts.NothingCopied);
                    return false;
                }

                log(texts.Done);
                return true;
            }
            catch (Exception exception)
            {
                log(texts.Failed + " " + exception.Message);
                return false;
            }
            finally
            {
                if (staging != null)
                {
                    try { Directory.Delete(staging, true); } catch { }
                }
            }
        }

        /// <summary>多个压缩包时选版本号最大的，其次按修改时间。</summary>
        private static string FindPackage(string directory)
        {
            try
            {
                string[] files = Directory.GetFiles(directory, "*.zip");
                if (files.Length == 0) return null;

                string best = null;
                Version bestVersion = null;
                DateTime bestTime = DateTime.MinValue;

                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    Version version = ParseVersion(name);
                    DateTime time = File.GetLastWriteTimeUtc(files[i]);

                    bool better;
                    if (best == null) better = true;
                    else if (version != null && bestVersion != null) better = version > bestVersion;
                    else if (version != null) better = true;
                    else if (bestVersion != null) better = false;
                    else better = time > bestTime;

                    if (better)
                    {
                        best = files[i];
                        bestVersion = version;
                        bestTime = time;
                    }
                }

                return best;
            }
            catch
            {
                return null;
            }
        }

        private static Version ParseVersion(string name)
        {
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                name, "(\\d+)\\.(\\d+)\\.(\\d+)");
            if (!match.Success) return null;

            try
            {
                return new Version(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>优先用系统自带的解压：tar.exe → Expand-Archive → ZipFile。</summary>
        internal static bool Extract(string zip, string destination, Action<string> log)
        {
            string systemTar = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");

            if (File.Exists(systemTar))
            {
                if (RunProcess(systemTar, "-xf \"" + zip + "\" -C \"" + destination + "\""))
                {
                    log("tar.exe OK");
                    return true;
                }
                log("tar.exe failed, trying Expand-Archive");
            }

            string script = "Expand-Archive -LiteralPath '" + zip.Replace("'", "''") +
                "' -DestinationPath '" + destination.Replace("'", "''") + "' -Force";

            if (RunProcess("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\""))
            {
                log("Expand-Archive OK");
                return true;
            }

            log("Expand-Archive failed, trying ZipFile");

            try
            {
                ZipFile.ExtractToDirectory(zip, destination);
                log("ZipFile OK");
                return true;
            }
            catch (Exception exception)
            {
                log("ZipFile failed: " + exception.Message);
                return false;
            }
        }

        private static bool RunProcess(string file, string arguments)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(file, arguments);
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (Process process = Process.Start(info))
                {
                    if (process == null) return false;
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(120000))
                    {
                        try { process.Kill(); } catch { }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string NormaliseRoot(string staging)
        {
            try
            {
                string[] directories = Directory.GetDirectories(staging);
                string[] files = Directory.GetFiles(staging);
                if (directories.Length == 1 && files.Length == 0)
                    return directories[0];
            }
            catch
            {
            }

            return staging;
        }

        private static bool Deploy(string source, string frameworkRoot, string pluginsFolder,
            bool labApi, Texts texts, Action<string> log)
        {
            bool copied = false;

            string plugins = Path.Combine(frameworkRoot, pluginsFolder);
            Directory.CreateDirectory(plugins);

            string[] dlls = labApi
                ? new string[] { "0ConsoleSplash.dll" }
                : new string[] { "0ConsoleSplash.Exiled.dll" };

            for (int i = 0; i < dlls.Length; i++)
            {
                string file = Path.Combine(source, dlls[i]);
                if (!File.Exists(file))
                {
                    if (labApi) file = Path.Combine(source, "0ConsoleSplash.Exiled.dll");
                    else file = Path.Combine(source, "0ConsoleSplash.dll");
                }

                if (File.Exists(file))
                {
                    File.Copy(file, Path.Combine(plugins, Path.GetFileName(file)), true);
                    log(texts.Copied + " " + Path.GetFileName(file));
                    copied = true;
                }
            }

            string animations = Path.Combine(source, "animations");
            if (Directory.Exists(animations))
            {
                CopyDirectory(animations, Path.Combine(plugins, "animations"));
                log(texts.Copied + " animations\\");
                copied = true;
            }

            string renderer = Path.Combine(source, "ConsoleSplashRenderer");
            if (!Directory.Exists(renderer)) renderer = Path.Combine(source, "ConsoleSplash");

            if (Directory.Exists(renderer))
            {
                string target = Path.Combine(frameworkRoot, "ConsoleSplashRenderer");
                CopyDirectory(renderer, target);
                log(texts.Copied + " ConsoleSplashRenderer\\");
                copied = true;
            }

            return copied;
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            string[] files = Directory.GetFiles(source);
            for (int i = 0; i < files.Length; i++)
            {
                string target = Path.Combine(destination, Path.GetFileName(files[i]));
                File.Copy(files[i], target, true);
            }

            string[] directories = Directory.GetDirectories(source);
            for (int i = 0; i < directories.Length; i++)
            {
                CopyDirectory(directories[i],
                    Path.Combine(destination, Path.GetFileName(directories[i])));
            }
        }
    }

    // ----------------------------------------------------------------------
    //  多语言文本 / localized strings
    // ----------------------------------------------------------------------

    internal sealed class Texts
    {
        internal string Code;
        internal string Title;
        internal string Subtitle;
        internal string CheckOs;
        internal string CheckDotNet;
        internal string CheckWebView;
        internal string CheckFramework;
        internal string CheckTar;
        internal string Yes;
        internal string No;
        internal string Install;
        internal string Close;
        internal string Unsupported;
        internal string Ready;
        internal string Extracting;
        internal string ExtractFailed;
        internal string NoArchive;
        internal string Copied;
        internal string Done;
        internal string Failed;
        internal string NothingCopied;
        internal string WarningWebView;
        internal string Language;

        private static readonly Dictionary<string, string[]> Table =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "en", new[] {
                "ConsoleSplash Setup", "Checks the system, then installs the console boot animation.",
                "Windows 10 or newer", ".NET Framework 4.8", "WebView2 Runtime (optional)",
                "LabAPI / EXILED folder", "Built-in archive tool (tar.exe)",
                "Yes", "No", "Install", "Close",
                "This system is not supported. Nothing was changed.",
                "Ready to install.", "Extracting", "Could not extract the package.",
                "No .zip found next to the installer; using the current folder.",
                "Copied", "Installation complete. Start the server to see the animation.",
                "Installation failed:", "Nothing was copied; check the log above.",
                "WebView2 is missing — animated frames will be used instead of HTML.",
                "Language" } },

            { "zh-CN", new[] {
                "ConsoleSplash 安装程序", "先检查系统，再把控制台开启动画装好。",
                "Windows 10 或更高版本", ".NET Framework 4.8", "WebView2 运行时（可选）",
                "LabAPI / EXILED 目录", "系统自带解压（tar.exe）",
                "通过", "缺失", "安装", "关闭",
                "当前系统不受支持，未做任何修改。",
                "可以安装。", "正在解压", "解压失败，安装包可能已损坏。",
                "安装器旁边没有找到压缩包，改用当前目录。",
                "已复制", "安装完成，启动服务器即可看到开启动画。",
                "安装失败：", "没有复制任何文件，请查看上方日志。",
                "缺少 WebView2，将使用预渲染帧而不是 HTML 动画。",
                "语言" } },

            { "zh-TW", new[] {
                "ConsoleSplash 安裝程式", "先檢查系統，再把主控台啟動動畫裝好。",
                "Windows 10 或更新版本", ".NET Framework 4.8", "WebView2 執行階段（選用）",
                "LabAPI / EXILED 目錄", "系統內建解壓（tar.exe）",
                "通過", "缺少", "安裝", "關閉",
                "目前系統不受支援，未做任何變更。",
                "可以安裝。", "正在解壓", "解壓失敗，安裝包可能已損壞。",
                "安裝程式旁找不到壓縮包，改用目前目錄。",
                "已複製", "安裝完成，啟動伺服器即可看到啟動動畫。",
                "安裝失敗：", "沒有複製任何檔案，請查看上方日誌。",
                "缺少 WebView2，將使用預先算圖的畫面而不是 HTML 動畫。",
                "語言" } },

            { "ru", new[] {
                "Установка ConsoleSplash", "Проверяет систему и устанавливает анимацию запуска консоли.",
                "Windows 10 или новее", ".NET Framework 4.8", "Среда WebView2 (необязательно)",
                "Папка LabAPI / EXILED", "Встроенный архиватор (tar.exe)",
                "Есть", "Нет", "Установить", "Закрыть",
                "Эта система не поддерживается. Ничего не изменено.",
                "Можно устанавливать.", "Распаковка", "Не удалось распаковать пакет.",
                "Архив рядом с установщиком не найден; используется текущая папка.",
                "Скопировано", "Установка завершена. Запустите сервер, чтобы увидеть анимацию.",
                "Ошибка установки:", "Ничего не скопировано; смотрите журнал выше.",
                "WebView2 не найден — вместо HTML будет показана последовательность кадров.",
                "Язык" } },

            { "uk", new[] {
                "Встановлення ConsoleSplash", "Перевіряє систему й встановлює анімацію запуску консолі.",
                "Windows 10 або новіша", ".NET Framework 4.8", "Середовище WebView2 (необов'язково)",
                "Тека LabAPI / EXILED", "Вбудований архіватор (tar.exe)",
                "Є", "Немає", "Встановити", "Закрити",
                "Ця система не підтримується. Нічого не змінено.",
                "Можна встановлювати.", "Розпакування", "Не вдалося розпакувати пакет.",
                "Архів поруч з інсталятором не знайдено; використовується поточна тека.",
                "Скопійовано", "Встановлення завершено. Запустіть сервер, щоб побачити анімацію.",
                "Помилка встановлення:", "Нічого не скопійовано; дивіться журнал вище.",
                "WebView2 не знайдено — замість HTML буде показано послідовність кадрів.",
                "Мова" } },

            { "ko", new[] {
                "ConsoleSplash 설치", "시스템을 확인한 뒤 콘솔 부팅 애니메이션을 설치합니다.",
                "Windows 10 이상", ".NET Framework 4.8", "WebView2 런타임 (선택)",
                "LabAPI / EXILED 폴더", "내장 압축 도구 (tar.exe)",
                "있음", "없음", "설치", "닫기",
                "이 시스템은 지원되지 않습니다. 아무것도 변경하지 않았습니다.",
                "설치할 수 있습니다.", "압축 해제 중", "패키지의 압축을 풀 수 없습니다.",
                "설치 프로그램 옆에 zip이 없어 현재 폴더를 사용합니다.",
                "복사됨", "설치 완료. 서버를 시작하면 애니메이션이 나타납니다.",
                "설치 실패:", "복사된 파일이 없습니다. 위 로그를 확인하세요.",
                "WebView2가 없어 HTML 대신 미리 렌더링된 프레임을 재생합니다.",
                "언어" } },

            { "de", new[] {
                "ConsoleSplash Setup", "Prüft das System und installiert die Konsolen-Startanimation.",
                "Windows 10 oder neuer", ".NET Framework 4.8", "WebView2-Runtime (optional)",
                "LabAPI / EXILED-Ordner", "Systemeigenes Entpacken (tar.exe)",
                "Ja", "Nein", "Installieren", "Schließen",
                "Dieses System wird nicht unterstützt. Es wurde nichts geändert.",
                "Bereit zur Installation.", "Entpacken", "Das Paket konnte nicht entpackt werden.",
                "Keine ZIP neben dem Setup gefunden; aktueller Ordner wird verwendet.",
                "Kopiert", "Installation abgeschlossen. Server starten, um die Animation zu sehen.",
                "Installation fehlgeschlagen:", "Es wurde nichts kopiert; siehe Protokoll.",
                "WebView2 fehlt — es werden vorgerenderte Frames statt HTML abgespielt.",
                "Sprache" } },

            { "fr", new[] {
                "Installation de ConsoleSplash", "Vérifie le système puis installe l'animation de démarrage.",
                "Windows 10 ou plus récent", ".NET Framework 4.8", "Runtime WebView2 (facultatif)",
                "Dossier LabAPI / EXILED", "Décompression intégrée (tar.exe)",
                "Oui", "Non", "Installer", "Fermer",
                "Ce système n'est pas pris en charge. Aucune modification.",
                "Prêt à installer.", "Extraction", "Impossible d'extraire l'archive.",
                "Aucun zip à côté de l'installateur ; dossier courant utilisé.",
                "Copié", "Installation terminée. Démarrez le serveur pour voir l'animation.",
                "Échec de l'installation :", "Rien n'a été copié ; voir le journal.",
                "WebView2 absent — les images pré-rendues seront utilisées au lieu du HTML.",
                "Langue" } },

            { "es", new[] {
                "Instalación de ConsoleSplash", "Comprueba el sistema e instala la animación de arranque.",
                "Windows 10 o superior", ".NET Framework 4.8", "Runtime WebView2 (opcional)",
                "Carpeta LabAPI / EXILED", "Descompresión del sistema (tar.exe)",
                "Sí", "No", "Instalar", "Cerrar",
                "Este sistema no es compatible. No se ha modificado nada.",
                "Listo para instalar.", "Extrayendo", "No se pudo extraer el paquete.",
                "No hay ningún zip junto al instalador; se usa la carpeta actual.",
                "Copiado", "Instalación completada. Inicia el servidor para ver la animación.",
                "Error de instalación:", "No se copió nada; revisa el registro.",
                "Falta WebView2: se usarán fotogramas pregenerados en lugar de HTML.",
                "Idioma" } },

            { "it", new[] {
                "Installazione di ConsoleSplash", "Controlla il sistema e installa l'animazione di avvio.",
                "Windows 10 o successivo", ".NET Framework 4.8", "Runtime WebView2 (facoltativo)",
                "Cartella LabAPI / EXILED", "Estrazione integrata (tar.exe)",
                "Sì", "No", "Installa", "Chiudi",
                "Questo sistema non è supportato. Nessuna modifica.",
                "Pronto per l'installazione.", "Estrazione", "Impossibile estrarre il pacchetto.",
                "Nessuno zip accanto all'installer; uso la cartella corrente.",
                "Copiato", "Installazione completata. Avvia il server per vedere l'animazione.",
                "Installazione non riuscita:", "Nessun file copiato; controlla il registro.",
                "WebView2 assente: verranno usati fotogrammi pre-renderizzati invece dell'HTML.",
                "Lingua" } },

            { "pt-BR", new[] {
                "Instalação do ConsoleSplash", "Verifica o sistema e instala a animação de inicialização.",
                "Windows 10 ou mais recente", ".NET Framework 4.8", "Runtime WebView2 (opcional)",
                "Pasta LabAPI / EXILED", "Extração do sistema (tar.exe)",
                "Sim", "Não", "Instalar", "Fechar",
                "Este sistema não é compatível. Nada foi alterado.",
                "Pronto para instalar.", "Extraindo", "Não foi possível extrair o pacote.",
                "Nenhum zip ao lado do instalador; usando a pasta atual.",
                "Copiado", "Instalação concluída. Inicie o servidor para ver a animação.",
                "Falha na instalação:", "Nada foi copiado; veja o log acima.",
                "WebView2 ausente — quadros pré-renderizados serão usados em vez do HTML.",
                "Idioma" } },

            { "pl", new[] {
                "Instalator ConsoleSplash", "Sprawdza system i instaluje animację startową konsoli.",
                "Windows 10 lub nowszy", ".NET Framework 4.8", "Środowisko WebView2 (opcjonalne)",
                "Folder LabAPI / EXILED", "Wbudowane rozpakowywanie (tar.exe)",
                "Tak", "Nie", "Zainstaluj", "Zamknij",
                "Ten system nie jest obsługiwany. Nic nie zmieniono.",
                "Gotowe do instalacji.", "Rozpakowywanie", "Nie udało się rozpakować pakietu.",
                "Brak archiwum obok instalatora; używany jest bieżący folder.",
                "Skopiowano", "Instalacja zakończona. Uruchom serwer, aby zobaczyć animację.",
                "Instalacja nie powiodła się:", "Nic nie skopiowano; sprawdź dziennik.",
                "Brak WebView2 — zamiast HTML zostaną odtworzone gotowe klatki.",
                "Język" } },

            { "tr", new[] {
                "ConsoleSplash Kurulum", "Sistemi denetler ve konsol açılış animasyonunu kurar.",
                "Windows 10 veya üzeri", ".NET Framework 4.8", "WebView2 çalışma zamanı (isteğe bağlı)",
                "LabAPI / EXILED klasörü", "Yerleşik arşiv aracı (tar.exe)",
                "Var", "Yok", "Kur", "Kapat",
                "Bu sistem desteklenmiyor. Hiçbir değişiklik yapılmadı.",
                "Kurmaya hazır.", "Arşiv açılıyor", "Paket açılamadı.",
                "Kurulum dosyasının yanında zip yok; geçerli klasör kullanılıyor.",
                "Kopyalandı", "Kurulum tamamlandı. Animasyonu görmek için sunucuyu başlatın.",
                "Kurulum başarısız:", "Hiçbir dosya kopyalanmadı; günlüğe bakın.",
                "WebView2 yok — HTML yerine önceden oluşturulmuş kareler oynatılacak.",
                "Dil" } },

            { "vi", new[] {
                "Cài đặt ConsoleSplash", "Kiểm tra hệ thống rồi cài hoạt ảnh khởi động bảng điều khiển.",
                "Windows 10 trở lên", ".NET Framework 4.8", "WebView2 Runtime (tùy chọn)",
                "Thư mục LabAPI / EXILED", "Công cụ giải nén hệ thống (tar.exe)",
                "Có", "Không", "Cài đặt", "Đóng",
                "Hệ thống này không được hỗ trợ. Không có thay đổi nào.",
                "Sẵn sàng cài đặt.", "Đang giải nén", "Không thể giải nén gói.",
                "Không tìm thấy tệp zip cạnh trình cài đặt; dùng thư mục hiện tại.",
                "Đã chép", "Cài đặt hoàn tất. Khởi động máy chủ để xem hoạt ảnh.",
                "Cài đặt thất bại:", "Không có tệp nào được chép; xem nhật ký.",
                "Thiếu WebView2 — sẽ dùng khung hình dựng sẵn thay cho HTML.",
                "Ngôn ngữ" } }
        };

        internal static Texts For(string language)
        {
            string code = Normalise(language);
            string[] values;

            if (!Table.TryGetValue(code, out values))
            {
                code = "en";
                values = Table["en"];
            }

            return new Texts
            {
                Code = code,
                Title = values[0],
                Subtitle = values[1],
                CheckOs = values[2],
                CheckDotNet = values[3],
                CheckWebView = values[4],
                CheckFramework = values[5],
                CheckTar = values[6],
                Yes = values[7],
                No = values[8],
                Install = values[9],
                Close = values[10],
                Unsupported = values[11],
                Ready = values[12],
                Extracting = values[13],
                ExtractFailed = values[14],
                NoArchive = values[15],
                Copied = values[16],
                Done = values[17],
                Failed = values[18],
                NothingCopied = values[19],
                WarningWebView = values[20],
                Language = values[21]
            };
        }

        private static string Normalise(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                string lower = language.ToLowerInvariant();
                if (lower.StartsWith("zh")) return lower.IndexOf("tw") >= 0 || lower.IndexOf("hk") >= 0
                    || lower.IndexOf("hant") >= 0 ? "zh-TW" : "zh-CN";
                if (lower.StartsWith("pt")) return "pt-BR";
                if (lower.StartsWith("sr")) return "en";
                return lower.Split('-')[0];
            }

            CultureInfo culture = CultureInfo.CurrentUICulture;
            string name = culture.Name;
            if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                if (name.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "zh-TW";
                return "zh-CN";
            }

            string two = culture.TwoLetterISOLanguageName;
            if (Table.ContainsKey(two)) return two;
            if (Table.ContainsKey(name)) return name;
            return "en";
        }
    }
}
