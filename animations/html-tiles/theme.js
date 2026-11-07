/* ============================================================================
   ConsoleSplash —— 主题与文案入口 / theme & copy entry point

   这是唯一需要改的文件 / this is the only file you normally need to touch:

     colors       颜色（背景、主色、正文、弱化文字）
     font         字体
     serverName   开屏显示的服务器名（想保留游戏名就留空）
     windowTitle  终端标题栏文字
     layout       尺寸/间距（任意 CSS 值）
     texts        按语言覆盖文案；"*" 表示所有语言
     introHtml    直接替换开屏区域的 HTML（留空用默认）
     customCss    追加任意 CSS（留空表示不追加）

   想要完全自由就直接改 index.html，甚至整包换成你自己的网页。
   For total control, edit index.html or drop in your own page.
   ========================================================================== */
window.CSPLASH_THEME = {
  colors: {
    background: "#05080B",
    accent: "#7FDBFF",
    text: "#CFE8F3",
    muted: "#5B6B76"
  },

  font: 'Consolas, "Cascadia Mono", "Courier New", monospace',

  serverName: "SCP: SECRET LABORATORY",
  windowTitle: "CONSOLESPLASH // TILES",

  layout: {
    introPadding: "12vh",
    terminalWidth: "min(88vw, 1180px)",
    terminalHeight: "min(84vh, 680px)",
    serverSize: "clamp(28px, 6vh, 60px)"
  },

  // 例 / example:
  // texts: { "zh-CN": { subtitle: "即将打开" }, en: { subtitle: "OPENING SOON" } },
  texts: {},

  introHtml: "",
  customCss: ""
};

(function () {
  "use strict";

  var theme = window.CSPLASH_THEME || {};

  function setVar(name, value) {
    if (value != null && value !== "") {
      document.documentElement.style.setProperty(name, value);
    }
  }

  function injectCss(css) {
    if (!css) return;
    var style = document.createElement("style");
    style.setAttribute("data-csplash-theme", "custom");
    style.textContent = css;
    document.head.appendChild(style);
  }

  function applyTexts() {
    var lang = (window.CS_I18N && window.CS_I18N.lang) || document.documentElement.lang || "en";
    var texts = theme.texts || {};
    var dict = texts[lang] || texts["*"];
    if (!dict) return;

    var nodes = document.querySelectorAll("[data-i18n]");
    for (var i = 0; i < nodes.length; i++) {
      var key = nodes[i].getAttribute("data-i18n");
      if (key && dict[key] != null) nodes[i].textContent = dict[key];
    }
  }

  function apply() {
    var colors = theme.colors || {};
    setVar("--bg", colors.background);
    setVar("--cyan", colors.accent);
    setVar("--text", colors.text);
    setVar("--muted", colors.muted);
    setVar("--font", theme.font);

    var layout = theme.layout || {};
    setVar("--intro-pad", layout.introPadding);
    setVar("--terminal-width", layout.terminalWidth);
    setVar("--terminal-height", layout.terminalHeight);
    setVar("--server-size", layout.serverSize);

    if (theme.introHtml) {
      var intro = document.querySelector(".intro");
      if (intro) intro.innerHTML = theme.introHtml;
    } else {
      if (theme.serverName) {
        var server = document.querySelector(".server");
        if (server) server.textContent = theme.serverName;
      }
      applyTexts();
    }

    if (theme.windowTitle) {
      var title = document.querySelector(".title");
      if (title) title.textContent = theme.windowTitle;
    }

    injectCss(theme.customCss);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", apply);
  } else {
    apply();
  }
})();
