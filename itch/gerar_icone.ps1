param([string]$Saida, [string]$Emblema)
Add-Type -AssemblyName System.Drawing
function Cor($h) { [System.Drawing.Color]::FromArgb(255, [Convert]::ToInt32($h.Substring(0,2),16), [Convert]::ToInt32($h.Substring(2,2),16), [Convert]::ToInt32($h.Substring(4,2),16)) }
# dicionário de char diferencia maiúsculas (a hashtable do PowerShell não)
$pal = [System.Collections.Generic.Dictionary[char, System.Drawing.Color]]::new()
$pal['C'] = Cor '35F6EB'   # contorno ciano
$pal['N'] = Cor '212F64'   # céu (cima)
$pal['M'] = Cor '16193D'   # céu (baixo)
$pal['G'] = Cor '7F90AB'   # quina iluminada / topo
$pal['g'] = Cor '4F5E77'   # corpo
$pal['s'] = Cor '343E53'   # lateral na sombra
$pal['b'] = Cor '2C374A'   # base
$pal['k'] = Cor '1B253B'   # frestas da lateral
$pal['P'] = Cor '000103'   # painel
$pal['v'] = Cor '32B760'   # LED verde
$pal['a'] = Cor '58F7EE'   # LED ciano

# 16 px, pixel a pixel: torre em 3/4 (quina clara à esquerda, painel de LEDs, lateral na sombra à direita)
$i16 = @(
  '..CCCCCCCCCCCC..',
  '.CNNNNNNNNNNNNC.',
  'CNNGGGGGGGGGgNNC',
  'CNNGPPPPPPPgsNNC',
  'CNNGPvPaPvPgsNNC',
  'CNNGPPPPPPPgkNNC',
  'CNNGPaPvPPPgsNNC',
  'CMMGPPPPPPPgsMMC',
  'CMMGPvPPPaPgkMMC',
  'CMMGPPPPPPPgsMMC',
  'CMMGPPPaPvPgsMMC',
  'CMMGPPPPPPPgsMMC',
  'CMMGgggggggggMMC',
  'CMbbbbbbbbbbbbMC',
  '.CMMMMMMMMMMMMC.',
  '..CCCCCCCCCCCC..'
)

function Desenhar($linhas, $s) {
  $n = 16 * $s
  $b = [System.Drawing.Bitmap]::new($n, $n)
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $ch = $linhas[$y][$x]
    if ($s -gt 1 -and ($ch -ceq 'v' -or $ch -ceq 'a')) { $ch = [char]'P' }   # nos maiores, LEDs mais finos (abaixo)
    if ($ch -eq [char]'.') { continue }
    for ($j = 0; $j -lt $s; $j++) { for ($i = 0; $i -lt $s; $i++) { $b.SetPixel($x * $s + $i, $y * $s + $j, $pal[[char]$ch]) } }
  } }
  if ($s -gt 2) {
    # contorno sempre com 2 px: o que fica mais para dentro vira céu
    $ciano = $pal['C'].ToArgb()
    $pintar = @()
    for ($y = 0; $y -lt $n; $y++) { for ($x = 0; $x -lt $n; $x++) {
      if ($b.GetPixel($x, $y).ToArgb() -ne $ciano) { continue }
      $perto = $false
      for ($dy = -2; $dy -le 2 -and -not $perto; $dy++) { for ($dx = -2; $dx -le 2; $dx++) {
        if ([Math]::Abs($dx) + [Math]::Abs($dy) -gt 2) { continue }
        $xx = $x + $dx; $yy = $y + $dy
        if ($xx -lt 0 -or $yy -lt 0 -or $xx -ge $n -or $yy -ge $n -or $b.GetPixel($xx, $yy).A -eq 0) { $perto = $true; break }
      } }
      if (-not $perto) { $pintar += ,@($x, $y) }
    } }
    foreach ($p in $pintar) { $b.SetPixel($p[0], $p[1], $(if ($p[1] -lt 7 * $s) { $pal['N'] } else { $pal['M'] })) }
  }
  if ($s -gt 1) {
    # LEDs: painel das colunas 4..10, linhas 3..11 (em unidades de 16)
    $x0 = 4 * $s; $x1 = 11 * $s - 1; $y0 = 3 * $s; $y1 = 12 * $s - 1
    $led = $s - 1; $passo = $s                 # 32: LED 1 px a cada 2; 48: LED 2 px a cada 3
    $lin = 0
    for ($y = $y0 + 1; $y + $led -le $y1; $y += $passo) {
      $col = 0
      for ($x = $x0 + 1; $x + $led -le $x1; $x += $passo) {
        $v = ($lin * 5 + $col * 3 + ($lin -band $col)) % 7
        if ($v -lt 5) {
          $cor = if ($v -eq 1 -or $v -eq 4) { $pal['a'] } else { $pal['v'] }
          for ($j = 0; $j -lt $led; $j++) { for ($i = 0; $i -lt $led; $i++) { $b.SetPixel($x + $i, $y + $j, $cor) } }
        }
        $col++
      }
      $lin++
    }
  }
  $b
}

foreach ($s in 1, 2, 3) {
  $n = 16 * $s
  $img = Desenhar $i16 $s
  $img.Save((Join-Path $Saida "icone_$n.png"), [System.Drawing.Imaging.ImageFormat]::Png)
  $k = [int](192 / $n); $p = [System.Drawing.Bitmap]::new($n * $k, $n * $k)
  $gr = [System.Drawing.Graphics]::FromImage($p); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
  $gr.Clear([System.Drawing.Color]::FromArgb(255, 32, 32, 32)); $gr.DrawImage($img, 0, 0, $n * $k, $n * $k); $gr.Dispose()
  $p.Save((Join-Path $Saida "previa_$n.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
# o emblema nos tamanhos grandes (128 original; 256/512/1024 ampliados sem suavizar)
$imgEmblema = [System.Drawing.Bitmap]::new($Emblema)
foreach ($n in 128, 256, 512, 1024) {
  $p = [System.Drawing.Bitmap]::new($n, $n)
  $gr = [System.Drawing.Graphics]::FromImage($p); $gr.InterpolationMode = 'NearestNeighbor'; $gr.PixelOffsetMode = 'Half'
  $gr.DrawImage($imgEmblema, 0, 0, $n, $n); $gr.Dispose()
  $p.Save((Join-Path $Saida "icone_$n.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
