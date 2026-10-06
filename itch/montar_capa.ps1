# Monta a capa do itch.io (630 x 500) com arte do próprio jogo: céu à noite, o escritório renderizado (ExportarSalas),
# o emblema e o nome na fonte Silkscreen. Uso: powershell -File montar_capa.ps1 <escritorio.png> <saida.png>
param([string]$sala, [string]$saida)
Add-Type -AssemblyName System.Drawing
$raiz = Split-Path $PSScriptRoot -Parent
$W = 630; $H = 500
$o = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($o)
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit

# céu: degradê da noite e estrelas (sempre as mesmas)
$ceu = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, $H), [System.Drawing.Color]::FromArgb(255, 18, 16, 38), [System.Drawing.Color]::FromArgb(255, 74, 52, 96))
$g.FillRectangle($ceu, 0, 0, $W, $H)
$sorte = New-Object System.Random 7
for ($i = 0; $i -lt 90; $i++) {
  $x = $sorte.Next($W); $y = $sorte.Next([int]($H * 0.75)); $t = if ($sorte.Next(5) -eq 0) { 2 } else { 1 }
  $a = 120 + $sorte.Next(135)
  $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($a, 255, 246, 227))), $x, $y, $t, $t)
}

# o escritório (pixel art em escala 1, sem borrar)
$img = [System.Drawing.Bitmap]::FromFile($sala)
$g.DrawImage($img, [int](($W - $img.Width) / 2), $H - $img.Height + 6, $img.Width, $img.Height)

# emblema no canto vazio de cima à esquerda
$logo = [System.Drawing.Bitmap]::FromFile("$raiz\Assets\_IdleDataCenter\Resources\Arte\logo.png")
$g.DrawImage($logo, 18, 62, $logo.Width, $logo.Height)

# nome e frase, na fonte do jogo, com sombra
$fontes = New-Object System.Drawing.Text.PrivateFontCollection
$fontes.AddFontFile("$raiz\Assets\_IdleDataCenter\Resources\Fontes\Silkscreen-Bold.ttf")
$fontes.AddFontFile("$raiz\Assets\_IdleDataCenter\Resources\Fontes\Silkscreen-Regular.ttf")
$familiaBold = $fontes.Families | Where-Object { $_.Name -like "*Silkscreen*" } | Select-Object -First 1
$titulo = New-Object System.Drawing.Font($familiaBold, 32, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$frase = New-Object System.Drawing.Font($familiaBold, 16, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
function Texto($s, $fonte, $y, $cor) {
  $tam = $g.MeasureString($s, $fonte)
  $x = [int](($W - $tam.Width) / 2)
  $g.DrawString($s, $fonte, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 9, 19, 33))), $x + 3, $y + 3)
  $g.DrawString($s, $fonte, (New-Object System.Drawing.SolidBrush $cor), $x, $y)
}
Texto "IDLE DATA CENTER" $titulo 14 ([System.Drawing.Color]::FromArgb(255, 255, 214, 92))
Texto "from freelancer to CEO" $frase 54 ([System.Drawing.Color]::FromArgb(255, 200, 208, 222))

$o.Save($saida, [System.Drawing.Imaging.ImageFormat]::Png)
"capa: $saida"
