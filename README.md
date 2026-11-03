# ConsoleSplash

> 只保留 DLL 的版本。给服务器控制台加一层开启动画，默认是一段终端风格的终端开机引导；覆盖的是控制台窗口，不是全屏。
>
> DLL-only build. A startup animation over the server console, defaulting to a terminal-style boot sequence. It covers the console window, never the whole screen.

许可证 / License: MIT.

## 交付内容 / Deliverables

- `0ConsoleSplash.dll` — LabAPI 插件 / LabAPI plugin
- `0ConsoleSplash.Exiled.dll` — EXILED 插件 / EXILED plugin
- `animations/` — 示例动画，默认 `html-example` / sample animation packs
- `ConsoleSplashRenderer/` — 外部渲染器与 WebView2 依赖 / out-of-process renderer + WebView2 files
- `ConsoleSplashSetup.exe` — 安装器 / one-click installer
- `README.md`、`LICENSE`

没有后台监听，没有常驻进程。动画只在插件加载时播放。

## 架构 / Architecture

插件进程（Unity/Mono）只做三件事：找到控制台窗口、记下它的矩形、拉起外部渲染器。窗口不移动、不隐藏，动画窗口直接盖在它上面。

WebView2 不进入服务器进程。插件用 `powershell.exe` 加载 `ConsoleSplash.Render.dll`，在独立进程里用 WinForms + WebView2 播放 HTML，播完退出；覆盖层消失的同一刻，原位置露出真实控制台。

The plugin never hosts WebView2. It launches `powershell.exe`, which loads `ConsoleSplash.Render.dll` and renders the HTML in its own process; the server process is untouched.

## 一键安装 / One-click install

双击压缩包里的 `ConsoleSplashSetup.exe`，它会先检查系统：

| 检查项 | 说明 |
|---|---|
| Windows 10 或更高 | 低于此版本不写入任何文件 |
| .NET Framework 4.8 | 安装器与渲染器都依赖它 |
| WebView2 运行时 | 可选；缺失时自动退回预渲染帧 |
| LabAPI / EXILED 目录 | 至少存在一个才能安装 |
| 系统解压工具 `tar.exe` | 缺失时依次改用 `Expand-Archive`、`ZipFile` |

不支持时窗口会按当前语言列出缺失项，不修改任何文件。支持时点“安装”，它会调用系统自带解压释放压缩包，然后把 DLL、`animations/`、`ConsoleSplashRenderer/` 放到对应框架目录，LabAPI 与 EXILED 同时存在就两边都装。

命令行：

```powershell
ConsoleSplashSetup.exe --check                 # 只体检，退出码 0=支持 1=不支持
ConsoleSplashSetup.exe --install --silent      # 静默安装
ConsoleSplashSetup.exe --check --lang=ru       # 强制俄语
```

支持 `en`、`zh-CN`、`zh-TW`、`ru`、`uk`、`ko`、`de`、`fr`、`es`、`it`、`pt-BR`、`pl`、`tr`、`vi`，其余语言回退英文。

## 手动安装 / Manual install

1. 插件 DLL 放进插件目录：
   - LabAPI：`LabAPI/plugins/global/`
   - EXILED：`EXILED/Plugins/`
2. 把 `ConsoleSplashRenderer/` 放到 **plugins 目录的上一级**，避免被 LabAPI/EXILED 当作插件扫描：
   - LabAPI：`LabAPI/ConsoleSplashRenderer/`
   - EXILED：`EXILED/ConsoleSplashRenderer/`
3. 把 `animations/` 放在 DLL 旁边，或者放到插件配置目录下。
4. 启动服务器。

The renderer lives next to the `plugins` folder, not inside it, so LabAPI/EXILED never tries to load the WebView2 DLLs as plugins.

插件按以下顺序找动画：

1. `<配置目录>/animations/<Animation>`
2. `<插件 DLL 所在目录>/animations/<Animation>`
3. 内置光扫

## 窗口行为 / Window behaviour

- 找到控制台窗口（经典 conhost、Windows Terminal 的 `CASCADIA_HOSTING_WINDOW_CLASS`、或 LocalAdmin 主窗口）后，覆盖层**直接出现在控制台当前的位置**，大小和控制台完全一致。
- 覆盖层先弹出来盖住控制台，**不移动、不隐藏控制台**，所以过渡开始时不会“抽一下”。
- 动画播完（或任意键跳过）时，白色收尾光幕铺满窗口，覆盖层关闭，控制台在同一位置无缝出现。
- 控制台全程留在覆盖层下面，不需要 `SW_HIDE`/恢复，也就没有最小化、闪烁或窗口跳动。
- 找不到任何窗口时（纯 headless 启动），在屏幕正中心播放 80% 大小的 16:9 动画。

## 默认动画 / Default animation

```yaml
Animation: html-example
```

`animations/html-example/index.html` 是一段 7 秒的完整过渡：

1. 网格地面透视，镜头向前推进；
2. 带阴影的 `SCP: SECRET LABORATORY` 和“即将打开”；
3. 光幕划过，进入终端开机动画；
4. 结尾白色光幕铺满，覆盖层关闭，露出真实控制台。

`animations/html-example/fallback/` 是旧版预渲染帧（28 帧，8 FPS），仅在外部渲染器不可用时使用。

## 多语言 / Localization

`animations/html-example/i18n.js` 内置文本库，语言集合对齐 SCP:SL 客户端 `Translations` 目录（NW 语言列表）：

`ca`、`cs`、`de`、`en`、`es`、`fr`、`gl`、`it`、`ko`、`pl`、`pt-BR`、`ru`、`sk`、`sr`、`sr-Latn`、`tr`、`uk`、`vi`、`zh-CN`、`zh-TW`

选择顺序：

1. 系统时区（`Intl.DateTimeFormat().resolvedOptions().timeZone`）；
2. 时区无法区分时用浏览器语言（例如加泰罗尼亚语/加利西亚语与西班牙同一时区）；
3. 最后回退到英语。

“即将打开”、开机日志、状态、页脚和跳过提示都会按语言替换；`SCP: SECRET LABORATORY` 作为游戏名保留英文。

## 配置 / Config

```yaml
Enabled: true          # 总开关 / master switch
Animation: html-example
Duration: 0            # 对 HTML 无效（用 splash.java 的完整时间线）；GIF/帧序列仍可覆盖
SkipOnKey: true        # 任意键跳过（跳过时也会先走收尾光幕）
ShowHint: true         # 显示跳过提示 / show the skip hint
Accent: "#7FDBFF"      # 光带颜色 / accent color

CheckUpdates: true     # 启动时检查更新 / check for updates
UpdateFeed: ""         # 更新源 JSON；留空则不检查 / JSON feed URL, empty = off
DownloadUpdates: false # 发现新版本时下载 zip 到 updates 目录 / download the zip
```

旧配置里的 `HideConsole`、`Scale` 已不再使用，可以删除。

拒绝更新有三种方式：`CheckUpdates: false` 完全关闭检查；`DownloadUpdates: false` 只提示不下载；`UpdateFeed: ""` 没有更新源。

## 动画包 / Animation package

一个文件夹就是一个动画包，入口 `splash.java`（声明式，不执行 Java 逻辑）：

```java
public class Splash {
    String  name       = "Console Boot";
    String  animation  = "index.html";
    String  fallback   = "fallback";   // HTML/视频失败时播放
    double  duration   = 7.0;
    double  fade       = 0.35;
    boolean skipOnKey  = true;
    String  background = "#05080B";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
```

`animation` 支持：

| 写法 | 渲染方式 |
|---|---|
| `index.html` | 外部渲染器（WebView2，独立进程） |
| `splash.gif` | 插件内 GDI+ |
| `frames` | PNG/JPG 序列，插件内 GDI+ |
| `splash.mp4` / `.webm` | 回退到 `fallback` |
| `builtin:light-sweep` | 插件内 GDI |

`fallback` 指向一个文件夹或 GIF/帧序列；HTML 渲染失败时使用。没有 `fallback` 就回退到内置光扫。

## WebView2 运行时 / WebView2 runtime

HTML 需要 Microsoft Edge WebView2 Runtime。Windows 10/11 通常自带；Windows Server 可能没有。缺失时：

```powershell
powershell -ExecutionPolicy Bypass -File ConsoleSplashRenderer\runtime-pack\Install-WebView2.ps1
```

缺失时插件会自动播放 `fallback/` 预渲染帧，不会卡住启动。

## 构建 / Build

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

输出：

- `dist/0ConsoleSplash.dll`
- `dist/0ConsoleSplash.Exiled.dll`
- `dist/ConsoleSplash/ConsoleSplash.Render.dll` + WebView2 依赖
- `dist/animations/`

`build.ps1 -Exiled` 只构建 EXILED 版。

## 诊断 / Diagnostics

写入 `%APPDATA%\SCP Secret Laboratory\ConsoleSplash\`：

- `plugin.log` — 找到的窗口类名/标题/进程、控制台矩形、动画清单时长
- `renderer-launch.log` — 外部渲染器启动、时长、PID、退出码
- `ConsoleSplashRenderer\render.log` — 渲染器 DPI 换算后的实际矩形

## 限制 / Limits

- 只支持 Windows。
- 需要系统安装 WebView2 Runtime；否则回退到预渲染帧。
- 动画在插件加载后开始播放，控制台可能已经显示了一小段时间。
- 覆盖层跟随控制台当前位置和大小；Windows Terminal 自己改变窗口位置时，动画也会跟着它，不再强制居中。

MIT License. See [LICENSE](LICENSE).
