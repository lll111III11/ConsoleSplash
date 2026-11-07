# ConsoleSplash

> DLL-only build. A startup animation over the server console, defaulting to a terminal-style boot sequence. It covers the console window, never the whole screen.
>
> The default pack also carries a procedurally synthesised soundtrack: a 100 BPM four-bar loop that keeps layering up — piano arpeggios, string pad, bass, timpani and snare/hats (in the spirit of the Light Containment lockdown cue), plus transition whooshes. Generated with WebAudio; no audio files in the package. Sound is off by default.
>
> Why are you even interested in something this pointless? (
>
> 只保留 DLL 的版本。给服务器控制台加一层开启动画，默认是一段终端风格的终端开机引导；覆盖的是控制台窗口，不是全屏。
>
> 默认动画附带一套实时合成的音效：100 BPM、四小节一循环的渐进式编曲——钢琴分解和弦、弦垫、贝斯，配定音鼓与军鼓/踩镲（走向参考轻收容封锁那段的推进感），外加光幕横扫与收尾重击。全部由 WebAudio 现场生成，压缩包里没有任何音频文件。音效默认关闭。
>
> 为什么你们会对这种无聊的东西感兴趣 (

## Sound / 音效

Sound is **off by default**. Set `window.CONSOLESPLASH_SOUND = true` in `animations/html-example/index.html` to enable the procedural soundtrack; it ends on a resolved Dm chord and fades out smoothly before the window closes.

```html
<script>window.CONSOLESPLASH_SOUND = false;</script>
```

The arrangement is a 100 BPM four-bar loop (Dm – Bb – F – A) that adds a layer every bar: piano arpeggio, pad, bass, timpani, snare and hats. No audio files are needed.

音效**默认关闭**，不会在别人的服务器上突然响起来。要开启，把 `animations/html-example/index.html` 里的 `window.CONSOLESPLASH_SOUND` 改成 `true`。开启后是 100 BPM、四小节一循环的渐进编曲（Dm – Bb – F – A），随小节叠加声部；结尾用 Dm 和弦收束，并在 6.55–6.92 秒平滑淡出。

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

## 最短接入 / Minimal setup

**只用安装器**：双击 `ConsoleSplashSetup.exe` → 体检 → 安装。它会把下面三样一次放好，LabAPI 和 EXILED 同时存在就两边都装。

想手动放，只需要记住这个目录树：

```
SCP Secret Laboratory/
└─ LabAPI/
   ├─ ConsoleSplashRenderer/        ← 整个文件夹放这里（不要放进 plugins）
   │   ├─ ConsoleSplash.Render.dll
   │   └─ WebView2Loader.dll ...
   └─ plugins/
      └─ global/
         ├─ 0ConsoleSplash.dll
         └─ animations/
            └─ html-example/        ← 动画包
               ├─ splash.java
               ├─ index.html
               ├─ i18n.js
               ├─ theme.js          ← 只改这个就能换主题
               └─ audio.js
```

三步：

1. `0ConsoleSplash.dll` → `plugins/global/`（EXILED 则是 `EXILED/Plugins/`）
2. `ConsoleSplashRenderer/` → `LabAPI/ConsoleSplashRenderer/`（EXILED 则是 `EXILED/ConsoleSplashRenderer/`）
3. `animations/` → `plugins/global/animations/`

启动服务器即生效。渲染器也会在 `plugins/global/ConsoleSplash/` 和 DLL 同级目录里自动查找，放错位置不会直接失败。

要换动画：把新文件夹丢进 `animations/`，在配置里把 `Animation:` 改成文件夹名即可；没有 `splash.java` 时会自动找 `index.html` → `frames/` → `*.gif` → `*.mp4`。

## 窗口行为 / Window behaviour

- 找到控制台窗口（经典 conhost、Windows Terminal 的 `CASCADIA_HOSTING_WINDOW_CLASS`、或 LocalAdmin 主窗口）后，覆盖层先**精确贴住控制台**，所以第一帧不会跳。
- **平滑开场**：覆盖层随后从控制台矩形向外铺开，盖住工作区，再在动画结尾随白色光幕收缩回控制台矩形，收完才关窗口。
- 控制台全程留在覆盖层下面，不移动、不隐藏，全程没有最小化、闪烁或窗口跳动。
- 动画播完（或任意键跳过）时同样走收缩过渡，控制台在原位置无缝出现。
- 不想要这个效果就把配置里的 `SmoothTransition` 设为 `false`，恢复“直接贴住控制台播放”。
- 找不到任何窗口时（纯 headless 启动），在屏幕正中心播放 80% 大小的 16:9 动画。

## 默认动画 / Default animation

自带两套默认覆盖，按文件夹名或 UIID 任选：

| UIID | 文件夹 | 效果 |
|---|---|---|
| `console-boot` | `html-example` | 网格地面 + 服务器名 + 光幕 + 终端，结尾白幕收进控制台 |
| `tiles` | `html-tiles` | 斜滑块滑入控制台大小 → 斜边扫过合上缝隙 → 终端 → 从底部斜着逐块划走 |

```yaml
Animation: html-example   # 按文件夹名
UIID: tiles               # 或按动画声明的 ID，优先于 Animation
```

`html-example` 的时间线：

1. 网格地面透视，镜头向前推进；
2. 带阴影的 `SCP: SECRET LABORATORY` 和“即将打开”；
3. 光幕划过，进入终端开机动画；
4. 结尾白色光幕铺满，覆盖层关闭，露出真实控制台。

`html-tiles` 的时间线（只覆盖控制台，不铺满屏幕）：

1. 斜滑块从两侧一块块滑进控制台矩形，块与块之间留缝；
2. 斜边光带扫过，缝隙按顺序合上，拼成完整画面；
3. 服务器名与“即将打开”，随后进入终端过场；
4. 结束后滑块一块接一块从底部斜着划走，露出控制台。

`html-example/fallback/` 是旧版预渲染帧（28 帧，8 FPS），仅在外部渲染器不可用时使用。

## 定制 / Customize

每套动画包的样式、文案、尺寸都收在自己的 **`theme.js`** 里（如 `animations/html-tiles/theme.js`），改完保存即可，不用重新编译：

| 字段 | 作用 |
|---|---|
| `colors.background` / `accent` / `text` / `muted` | 背景、主色、正文、弱化文字 |
| `font` | 整个动画的字体栈 |
| `serverName` | 开屏服务器名；留空保留 `SCP: SECRET LABORATORY` |
| `windowTitle` | 终端标题栏文字 |
| `layout.introPadding` / `terminalWidth` / `terminalHeight` / `serverSize` | 间距与终端尺寸，填任意 CSS 值 |
| `texts` | 按语言覆盖文案，`"*"` 表示所有语言 |
| `introHtml` | 直接替换开屏区域 HTML，留空用默认 |
| `customCss` | 追加任意 CSS，例如隐藏某个元素、换字体、加动画 |

例子：把主色改成橙红、服务器名改成自己的、只改中文副标题：

```js
window.CSPLASH_THEME = {
  colors: { background: "#0B0505", accent: "#FF6B3D" },
  serverName: "MY SERVER",
  texts: { "zh-CN": { subtitle: "即将进入收容" } },
  customCss: ".rule { box-shadow: 0 0 24px #FF6B3D; }"
};
```

想完全自由就三种做法，按需选：

1. **改 `theme.js`** — 90% 的定制都在这里。
2. **改 `index.html`** — 页面就是普通 HTML/CSS/JS，随便改布局；记得保持 7 秒时间线（或同步改 `splash.java` 的 `duration`）。
3. **换整个包** — 在 `animations/` 下新建文件夹放自己的 `index.html`，配置里改 `Animation: 文件夹名`。

音效开关也在 `index.html`：`window.CONSOLESPLASH_SOUND`，默认 `false`。

声音/主题/渲染器都不需要重新编译 DLL；只有改 C# 才需要跑 `build.ps1`。

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
UIID: ""               # 按动画包声明的 ID 选择，优先于 Animation / pick a pack by UIID
Duration: 0            # 对 HTML 无效（用 splash.java 的完整时间线）；GIF/帧序列仍可覆盖
SkipOnKey: true        # 任意键跳过（跳过时也会先走收尾光幕）
ShowHint: true         # 显示跳过提示 / show the skip hint
Accent: "#7FDBFF"      # 光带颜色 / accent color
SmoothTransition: true # 平滑开场：铺满工作区再缩回控制台 / expand then shrink back

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
    String  uiid       = "console-boot"; // 配置里填这个 ID 就能选中它
    String  animation  = "index.html";
    String  fallback   = "fallback";     // HTML/视频失败时播放
    double  duration   = 7.0;
    double  fade       = 0.35;
    boolean skipOnKey  = true;
    boolean smooth     = true;           // 是否铺满屏幕再缩回；false = 只贴控制台
    String  backdrop   = "";             // "" / "screen" / "console"
    String  background = "#05080B";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
```

**UIID 系统**：`uiid` 是动画包自己声明的身份，和文件夹名无关。配置里写：

```yaml
UIID: tiles
```

启动时会扫描 `animations/*/splash.java`，找 `uiid` 匹配的包（大小写不敏感），找到就播放；没找到才退回 `Animation`，再退回内置光扫。自己做动画时只要写一个 `uiid`，把文件夹丢进 `animations/`，配置里填 ID 即可。

`backdrop` 用于“缝隙里露出控制台”的效果：渲染器会在显示覆盖层之前截屏，`screen` 截整屏，`console` 只截控制台矩形，截图会注入成页面的 `window.CSPLASH_BACKDROP`。`smooth = false` 时覆盖层全程只贴控制台，不铺满屏幕。

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
