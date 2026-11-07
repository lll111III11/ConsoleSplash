# ConsoleSplash build script (UTF-8 BOM for PowerShell 5.1)
# Builds DLL only:
#   0ConsoleSplash.dll / 0ConsoleSplash.Exiled.dll   (plugin, no WebView2)
#   ConsoleSplash/ConsoleSplash.Render.dll           (out-of-process HTML renderer)
param([switch]$Exiled, [switch]$Package)

$version = '1.3.2'

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
$renderDist = Join-Path $dist 'ConsoleSplash'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
New-Item -ItemType Directory -Force -Path $renderDist | Out-Null

$csc = @(Get-ChildItem "$env:ProgramFiles\dotnet\sdk\*\Roslyn\bincore\csc.dll" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending)
if ($csc.Count -eq 0) { throw "csc.dll not found" }
$cscPath = $csc[0].FullName

$managedCandidates = @(
    'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
    'C:\Program Files (x86)\Steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed',
    'C:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed'
)
$managed = $null
foreach ($path in $managedCandidates) {
    if (Test-Path (Join-Path $path 'LabApi.dll')) { $managed = $path; break }
}
if (-not $managed) { throw "game Managed folder not found" }

$webviewPackage = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.web.webview2\1.0.2903.40'
$webviewCore = Join-Path $webviewPackage 'lib\net462\Microsoft.Web.WebView2.Core.dll'
$webviewWinForms = Join-Path $webviewPackage 'lib\net462\Microsoft.Web.WebView2.WinForms.dll'
$webviewNative = Join-Path $webviewPackage 'runtimes\win-x64\native\WebView2Loader.dll'
foreach ($f in @($webviewCore, $webviewWinForms, $webviewNative)) {
    if (-not (Test-Path $f)) { throw "WebView2 file not found: $f" }
}

$framework = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
if (-not (Test-Path (Join-Path $framework 'mscorlib.dll'))) { throw ".NET Framework 4.8 reference assemblies not found" }

$sharedSources = @(
    (Join-Path $root 'src\Shared\SplashManifest.cs'),
    (Join-Path $root 'src\Shared\ColorUtil.cs'),
    (Join-Path $root 'src\Shared\Win32Splash.cs')
)
$pluginSources = @(
    (Join-Path $root 'src\Plugin\PluginMain.cs'),
    (Join-Path $root 'src\Plugin\ExternalRenderer.cs'),
    (Join-Path $root 'src\Plugin\UpdateChecker.cs')
)

function Build-Plugin([bool]$exiled) {
    $suffix = if ($exiled) { '.Exiled' } else { '' }
    $out = Join-Path $dist ("0ConsoleSplash{0}.dll" -f $suffix)

    $refs = @(
        (Join-Path $managed 'mscorlib.dll'),
        (Join-Path $managed 'netstandard.dll'),
        (Join-Path $managed 'System.dll'),
        (Join-Path $managed 'System.Core.dll'),
        (Join-Path $managed 'System.Drawing.dll')
    )

    if ($exiled) {
        $exiledApi = "$env:APPDATA\SCP Secret Laboratory\LabAPI\dependencies\global"
        if (-not (Test-Path (Join-Path $exiledApi 'Exiled.API.dll'))) {
            $exiledApi = "$env:APPDATA\SCP Secret Laboratory\PluginAPI\plugins\global\dependencies"
        }
        if (-not (Test-Path (Join-Path $exiledApi 'Exiled.API.dll'))) { throw "Exiled.API.dll not found" }
        $refs += (Join-Path $exiledApi 'Exiled.API.dll')
        $define = '/define:EXILED_BUILD'
    } else {
        $refs += (Join-Path $managed 'LabApi.dll')
        $define = ''
    }

    $arguments = @($cscPath, '-noconfig', '-nostdlib+', '-target:library', '-platform:AnyCPU',
        '-warn:4', '-nowarn:CS0169,CS0219,CS0414,CS0436', "-out:$out")
    foreach ($reference in $refs) { $arguments += "-r:$reference" }
    if ($define) { $arguments += $define }
    $arguments += $sharedSources
    $arguments += $pluginSources

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "plugin compile failed (exit $LASTEXITCODE)" }
    Write-Host ("BUILD OK -> {0} ({1} bytes)" -f $out, (Get-Item $out).Length)
}

function Build-Render {
    $out = Join-Path $renderDist 'ConsoleSplash.Render.dll'

    $refs = @(
        (Join-Path $framework 'mscorlib.dll'),
        (Join-Path $framework 'System.dll'),
        (Join-Path $framework 'System.Core.dll'),
        (Join-Path $framework 'System.Drawing.dll'),
        (Join-Path $framework 'System.Windows.Forms.dll'),
        $webviewCore,
        $webviewWinForms
    )

    $arguments = @($cscPath, '-noconfig', '-nostdlib+', '-target:library', '-platform:x64',
        '-warn:4', '-nowarn:CS0169,CS0219,CS0414,CS0436', "-out:$out")
    foreach ($reference in $refs) { $arguments += "-r:$reference" }
    $arguments += (Join-Path $root 'src\Render\Host.cs')
    $arguments += (Join-Path $root 'src\Render\SplashForm.cs')

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "renderer compile failed (exit $LASTEXITCODE)" }

    Copy-Item $webviewCore $renderDist -Force
    Copy-Item $webviewWinForms $renderDist -Force
    Copy-Item $webviewNative $renderDist -Force

    Write-Host ("BUILD OK -> {0} ({1} bytes)" -f $out, (Get-Item $out).Length)
}

function Build-Installer {
    $out = Join-Path $dist 'ConsoleSplashSetup.exe'

    $refs = @(
        (Join-Path $framework 'mscorlib.dll'),
        (Join-Path $framework 'System.dll'),
        (Join-Path $framework 'System.Core.dll'),
        (Join-Path $framework 'System.Drawing.dll'),
        (Join-Path $framework 'System.Windows.Forms.dll'),
        (Join-Path $framework 'System.IO.Compression.dll'),
        (Join-Path $framework 'System.IO.Compression.FileSystem.dll')
    )

    $arguments = @($cscPath, '-noconfig', '-nostdlib+', '-target:winexe', '-platform:AnyCPU',
        '-warn:4', '-nowarn:CS0169,CS0219,CS0414,CS0436', "-out:$out")
    foreach ($reference in $refs) { $arguments += "-r:$reference" }
    $arguments += (Join-Path $root 'src\Installer\Program.cs')
    $arguments += (Join-Path $root 'src\Installer\SetupForm.cs')

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "installer compile failed (exit $LASTEXITCODE)" }
    Write-Host ("BUILD OK -> {0} ({1} bytes)" -f $out, (Get-Item $out).Length)
}

if ($Exiled) {
    Build-Plugin $true
} else {
    Build-Plugin $false
    Build-Plugin $true
}
Build-Render
Build-Installer

$animations = Join-Path $root 'animations'
if (Test-Path $animations) { Copy-Item $animations $dist -Recurse -Force }
$runtime = Join-Path $root 'runtime-pack'
if (Test-Path $runtime) { Copy-Item $runtime $renderDist -Recurse -Force }
foreach ($file in @('README.md', 'LICENSE')) {
    $candidate = Join-Path $root $file
    if (Test-Path $candidate) { Copy-Item $candidate $dist -Force }
}

# Remove old EXE-era files from the output root.
foreach ($old in @('ConsoleSplash.exe', 'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll',
    'runtime-pack', 'ConsoleSplash.Render.dll')) {
    Remove-Item (Join-Path $dist $old) -Force -Recurse -ErrorAction SilentlyContinue
}

if ($Package) {
    $stage = Join-Path $env:TEMP ("ConsoleSplash-package-" + $version)
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    Copy-Item (Join-Path $dist 'ConsoleSplashSetup.exe') $stage
    Copy-Item (Join-Path $dist '0ConsoleSplash.dll') $stage
    Copy-Item (Join-Path $dist '0ConsoleSplash.Exiled.dll') $stage
    foreach ($extra in @('README.md', 'LICENSE')) {
        $candidate = Join-Path $dist $extra
        if (Test-Path $candidate) { Copy-Item $candidate $stage }
    }
    Copy-Item (Join-Path $dist 'animations') $stage -Recurse -Force
    Copy-Item (Join-Path $dist 'ConsoleSplash') (Join-Path $stage 'ConsoleSplashRenderer') -Recurse -Force
    Get-ChildItem $stage -Recurse -Include '*.log' -ErrorAction SilentlyContinue | Remove-Item -Force

    $zip = Join-Path $dist ("ConsoleSplash-{0}.zip" -f $version)
    Remove-Item $zip -Force -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host ("PACKAGE OK -> {0} ({1} bytes)" -f $zip, (Get-Item $zip).Length)
}

Write-Host "DONE. Output in $dist"
