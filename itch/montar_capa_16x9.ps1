# Capa 16:9 (960 x 540) para os posts do devlog e as redes: céu à noite, o escritório renderizado (ExportarSalas) à
# direita, o emblema e o nome à esquerda. Uso: powershell -File montar_capa_16x9.ps1 <escritorio.png> <saida.png>
param([string]$sala, [string]$saida)
Add-Type -AssemblyName System.Drawing
$raiz = Split-Path $PSScriptRoot -Parent
$W = 960; $H = 540
$o = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($o)
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit

# céu: degradê da noite e estrelas (sempre as mesmas)
$ceu = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, $H), [System.Drawing.Color]::FromArgb(255, 18, 16, 38), [System.Drawing.Color]::FromArgb(255, 74, 52, 96))
$g.FillRectangle($ceu, 0, 0, $W, $H)
$sorte = New-Object System.Random 7
for ($i = 0; $i -lt 140; $i++) {
  $x = $sorte.Next($W); $y = $sorte.Next([int]($H * 0.8)); $t = if ($sorte.Next(5) -eq 0) { 2 } else { 1 }
  $a = 120 + $sorte.Next(135)
  $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($a, 255, 246, 227))), $x, $y, $t, $t)
}

# o escritório (pixel art em escala 1, sem borrar), à direita
$img = [System.Drawing.Bitmap]::FromFile($sala)
$g.DrawImage($img, $W - $img.Width - 10, [int](($H - $img.Height) / 2) + 20, $img.Width, $img.Height)

# emblema e nome, à esquerda
$logo = [System.Drawing.Bitmap]::FromFile("$raiz\Assets\_IdleDataCenter\Resources\Arte\logo.png")
$g.DrawImage($logo, [int](205 - $logo.Width), 34, $logo.Width * 2, $logo.Height * 2)
$fontes = New-Object System.Drawing.Text.PrivateFontCollection
$fontes.AddFontFile("$raiz\Assets\_IdleDataCenter\Resources\Fontes\Silkscreen-Bold.ttf")
$familia = $fontes.Families | Where-Object { $_.Name -like "*Silkscreen*" } | Select-Object -First 1
$titulo = New-Object System.Drawing.Font($familia, 36, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$frase = New-Object System.Drawing.Font($familia, 13, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
function Texto($s, $fonte, $y, $cor) {
  $tam = $g.MeasureString($s, $fonte)
  $x = [int](205 - $tam.Width / 2)
  $g.DrawString($s, $fonte, (New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 9, 19, 33))), $x + 3, $y + 3)
  $g.DrawString($s, $fonte, (New-Object System.Drawing.SolidBrush $cor), $x, $y)
}
$amarelo = [System.Drawing.Color]::FromArgb(255, 255, 214, 92); $cinza = [System.Drawing.Color]::FromArgb(255, 200, 208, 222)
Texto "IDLE" $titulo 300 $amarelo
Texto "DATA CENTER" $titulo 342 $amarelo
Texto "from freelancer to CEO" $frase 400 $cinza
Texto "an idle game above your taskbar" $frase 424 $cinza

$o.Save($saida, [System.Drawing.Imaging.ImageFormat]::Png)
"capa: $saida"
