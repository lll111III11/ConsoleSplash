# Regenerates the pre-rendered fallback frames for animations/html-example.
# 8 FPS, 28 frames = 3.5 seconds, 960x540.
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'animations\html-example\fallback\frames'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Get-ChildItem $out -Filter '*.png' -ErrorAction SilentlyContinue | Remove-Item -Force

$w = 960
$h = 540
$frames = 28
$fps = 8

$bg = [System.Drawing.Color]::FromArgb(5, 8, 11)
$panel = [System.Drawing.Color]::FromArgb(245, 7, 13, 18)
$panelBorder = [System.Drawing.Color]::FromArgb(60, 127, 219, 255)
$titleBar = [System.Drawing.Color]::FromArgb(255, 10, 17, 22)
$cyan = [System.Drawing.Color]::FromArgb(127, 219, 255)
$green = [System.Drawing.Color]::FromArgb(63, 185, 80)
$amber = [System.Drawing.Color]::FromArgb(210, 153, 34)
$text = [System.Drawing.Color]::FromArgb(207, 232, 243)
$muted = [System.Drawing.Color]::FromArgb(91, 107, 118)
$white = [System.Drawing.Color]::White

$font = New-Object System.Drawing.Font('Consolas', 14)
$fontSmall = New-Object System.Drawing.Font('Consolas', 10)
$fontTiny = New-Object System.Drawing.Font('Consolas', 9)
$fontHead = New-Object System.Drawing.Font('Consolas', 13, [System.Drawing.FontStyle]::Bold)

$lines = @(
    @{ delay = 0.05; time = '[0.000]'; text = 'initializing overlay ...........'; status = 'OK'; color = $green },
    @{ delay = 0.38; time = '[0.214]'; text = 'loading animation pack ........'; status = 'OK'; color = $green },
    @{ delay = 0.72; time = '[0.482]'; text = 'mounting console window .......'; status = 'OK'; color = $green },
    @{ delay = 1.06; time = '[0.736]'; text = 'starting server ...............'; status = 'OK'; color = $green },
    @{ delay = 1.40; time = '[0.998]'; text = 'secure. contain. protect. ......'; status = 'READY'; color = $amber },
    @{ delay = 1.74; time = '[1.240]'; text = 'console handoff complete .......'; status = 'OK'; color = $green },
    @{ delay = 2.08; time = '[1.502]'; text = 'waiting for operator input .....'; status = 'PENDING'; color = $muted },
    @{ delay = 2.42; time = '[1.764]'; text = 'press any key to skip'; status = ''; color = $muted },
    @{ delay = 2.76; time = '[2.026]'; text = ''; status = ''; color = $muted }
)

for ($f = 0; $f -lt $frames; $f++) {
    $t = $f / $fps
    $progress = [Math]::Min(1.0, [Math]::Max(0.0, ($t - 0.15) / 3.0))
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear($bg)

    # light band behind the panel
    $bandWidth = 220
    $bandX = [int]($w - ($w + $bandWidth) * ($t / 3.5))
    $bandRect = New-Object System.Drawing.Rectangle $bandX, 0, $bandWidth, $h
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bandRect, ([System.Drawing.Color]::FromArgb(0, 0, 0, 0)), ([System.Drawing.Color]::FromArgb(0, 0, 0, 0)), 0.0
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend 7
    $blend.Colors = @([System.Drawing.Color]::FromArgb(0, 0, 0, 0), [System.Drawing.Color]::FromArgb(20, 127, 219, 255), [System.Drawing.Color]::FromArgb(110, 127, 219, 255), $white, [System.Drawing.Color]::FromArgb(110, 127, 219, 255), [System.Drawing.Color]::FromArgb(20, 127, 219, 255), [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $blend.Positions = @(0.0, 0.15, 0.38, 0.5, 0.62, 0.85, 1.0)
    $brush.InterpolationColors = $blend
    $g.FillRectangle($brush, $bandRect)
    $brush.Dispose()

    # terminal panel
    $panelRect = New-Object System.Drawing.Rectangle 48, 30, 864, 480
    $panelBrush = New-Object System.Drawing.SolidBrush $panel
    $g.FillRectangle($panelBrush, $panelRect)
    $panelBrush.Dispose()
    $pen = New-Object System.Drawing.Pen $panelBorder, 1
    $g.DrawRectangle($pen, $panelRect)
    $pen.Dispose()

    # title bar
    $titleRect = New-Object System.Drawing.Rectangle 49, 31, 862, 26
    $titleBrush = New-Object System.Drawing.SolidBrush $titleBar
    $g.FillRectangle($titleBrush, $titleRect)
    $titleBrush.Dispose()

    $dotY = 43
    $dotBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(248, 81, 73))
    $g.FillEllipse($dotBrush, 62, $dotY, 8, 8)
    $dotBrush.Color = [System.Drawing.Color]::FromArgb(210, 153, 34)
    $g.FillEllipse($dotBrush, 76, $dotY, 8, 8)
    $dotBrush.Color = [System.Drawing.Color]::FromArgb(63, 185, 80)
    $g.FillEllipse($dotBrush, 90, $dotY, 8, 8)
    $dotBrush.Dispose()

    $titleBrush = New-Object System.Drawing.SolidBrush $muted
    $g.DrawString('CONSOLESPLASH // BOOT', $fontTiny, $titleBrush, 110.0, 38.0)
    $titleBrush.Dispose()

    # header
    $cyanBrush = New-Object System.Drawing.SolidBrush $cyan
    $g.DrawString('CONSOLE BOOT SEQUENCE', $fontHead, $cyanBrush, 68.0, 72.0)
    $cyanBrush.Dispose()
    $mutedBrush = New-Object System.Drawing.SolidBrush $muted
    $g.DrawString('SCP:SL DEDICATED', $fontSmall, $mutedBrush, 790.0, 76.0)

    # boot lines
    $lineY = 104
    foreach ($line in $lines) {
        if ($t -ge $line.delay) {
            $timeBrush = New-Object System.Drawing.SolidBrush $muted
            $g.DrawString($line.time, $font, $timeBrush, 68.0, [float]$lineY)
            $timeBrush.Dispose()

            $textBrush = New-Object System.Drawing.SolidBrush $line.color
            $g.DrawString($line.text, $font, $textBrush, 150.0, [float]$lineY)
            $textBrush.Dispose()

            if ($line.status.Length -gt 0) {
                $statusBrush = New-Object System.Drawing.SolidBrush $line.color
                $g.DrawString($line.status, $font, $statusBrush, 812.0, [float]$lineY)
                $statusBrush.Dispose()
            }
        }
        $lineY += 22
    }

    # progress
    $g.DrawString('BOOT PROGRESS', $fontTiny, $mutedBrush, 68.0, 432.0)
    $rightSize = $g.MeasureString('CONSOLESPLASH v1.0', $fontTiny)
    $g.DrawString('CONSOLESPLASH v1.0', $fontTiny, $mutedBrush, 880.0 - $rightSize.Width, 432.0)

    $barRect = New-Object System.Drawing.Rectangle 68, 452, 812, 8
    $barPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 127, 219, 255)), 1
    $g.DrawRectangle($barPen, $barRect)
    $barPen.Dispose()

    $fillWidth = [int](810 * $progress)
    if ($fillWidth -gt 0) {
        $fillRect = New-Object System.Drawing.Rectangle 69, 453, $fillWidth, 6
        $fillBrush = New-Object System.Drawing.SolidBrush $cyan
        $g.FillRectangle($fillBrush, $fillRect)
        $fillBrush.Dispose()
    }

    # footer
    $g.DrawString('MIT LICENSE', $fontTiny, $mutedBrush, 68.0, 486.0)
    $footerRight = $g.MeasureString('SCP: SECRET LABORATORY', $fontTiny)
    $g.DrawString('SCP: SECRET LABORATORY', $fontTiny, $mutedBrush, 880.0 - $footerRight.Width, 486.0)
    $mutedBrush.Dispose()

    # scanlines
    $scanPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(8, 255, 255, 255)), 1
    for ($y = 0; $y -lt $h; $y += 3) {
        $g.DrawLine($scanPen, 0, $y, $w, $y)
    }
    $scanPen.Dispose()

    # hint
    $hintBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(51, 65, 75))
    $hintSize = $g.MeasureString('PRESS ANY KEY TO SKIP', $fontTiny)
    $g.DrawString('PRESS ANY KEY TO SKIP', $fontTiny, $hintBrush, 930.0 - $hintSize.Width, 516.0)
    $hintBrush.Dispose()

    $g.Dispose()
    $path = Join-Path $out ('frame_{0:D3}.png' -f ($f + 1))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$font.Dispose()
$fontSmall.Dispose()
$fontTiny.Dispose()
$fontHead.Dispose()

Write-Host ("generated {0} frames in {1}" -f $frames, $out)
