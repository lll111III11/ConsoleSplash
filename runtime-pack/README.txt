ConsoleSplash - WebView2 runtime pack
=====================================

HTML / MP4 animations need the Microsoft Edge WebView2 Runtime.
The plugin embeds the WebView2 SDK, but the browser runtime itself must be
installed on the machine.

Windows 10 and Windows 11 usually ship with it. Windows Server and some
stripped-down systems do not.

If the plugin log says WebView2 is missing:

1. Run Install-WebView2.ps1 in this folder, or
2. Download and install the Evergreen Standalone Installer manually:
   https://developer.microsoft.com/microsoft-edge/webview2/

After installation, restart the server. Built-in light sweep, GIF and PNG
sequences do not need WebView2. The default HTML animation also has a
pre-rendered fallback and will still play without the runtime.
