# Monta as salas na escala da pessoa (Salas/*_hd.png) e gera o rack 1,25x (PixelLab/rack_g.png).
# Rodar: powershell -ExecutionPolicy Bypass -File Arte\Ferramentas\montar.ps1 [pasta de saida]
param([string]$saida = "")
$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot\Dobrar.cs", "$PSScriptRoot\Montar.cs", "$PSScriptRoot\Salas.cs" -ReferencedAssemblies System.Drawing
$base = "$PSScriptRoot\..\PixelLab\salas_base"
$arte = "$PSScriptRoot\..\..\Assets\_IdleDataCenter\Resources\Arte"
$destSalas = if ($saida) { $saida } else { "$arte\Salas" }
$destPl = if ($saida) { $saida } else { "$arte\PixelLab" }

[Montar]::PastaDasImagens = (Resolve-Path "$PSScriptRoot\..\PixelLab").Path
if ($saida) { [Montar]::PastaDosMoveis = $saida }
foreach ($b in [Salas]::Bases()) {
  $w = 0; $h = 0
  $b.px = [Dobrar]::Ler("$base\$($b.nome).png", [ref]$w, [ref]$h)
  $b.w = $w; $b.h = $h
  [Montar]::Bases[$b.nome] = $b
}
foreach ($s in [Salas]::Todas()) {
  $px = [Montar]::Desenhar($s)
  [Dobrar]::Gravar("$destSalas\$($s.nome)_hd.png", $px, $s.W, $s.H)
  $n = $s.n; $bx = $s.Bx; $by = $s.By
  "{0}: {1}x{2} fundo=({3},{4}) esquerda=({5},{6}) direita=({7},{8}) casas={9}" -f $s.nome, $s.W, $s.H, $bx, $by, ($bx - 32 * $n), ($by + 16 * $n), ($bx + 32 * $n), ($by + 16 * $n), $n
}
# rack a 1,25x (uns 2 m perto da pessoa de 80 px): Scale2x e redução pela moda (os LEDs não somem)
$w = 0; $h = 0; $W = 0; $H = 0
$p = [Dobrar]::Ler("$arte\PixelLab\rack.png", [ref]$w, [ref]$h)
$d = [Dobrar]::Scale2x($p, $w, $h)
$r = [Dobrar]::Reduzir($d, $w * 2, $h * 2, 0.625, [ref]$W, [ref]$H)
[Dobrar]::Gravar("$destPl\rack_g.png", $r, $W, $H)
# equipamentos próprios gerados no PixelLab (já na escala do rack 1,25x): recorta e deixa todos com a frente à esquerda
foreach ($e in @(@('storage_v2', 'storage_g', $false), @('fita_v1', 'fita_g', $true), @('rede_v1', 'rede_g', $false), @('torre_pro', 'torre', $true), @('nobreak_pro', 'nobreak', $false), @('ar_pro', 'ar_condicionado', $false), @('rack_pro', 'rack_g', $false), @('predio_dc_pro', 'predio_dc', $false), @('arvore_pro', 'arvore', $false))) {
  $w = 0; $h = 0; $W = 0; $H = 0
  $p = [Dobrar]::Ler("$PSScriptRoot\..\PixelLab\$($e[0]).png", [ref]$w, [ref]$h)
  $r = [Dobrar]::Recortar($p, $w, $h, $e[2], [ref]$W, [ref]$H)
  [Dobrar]::Gravar("$destPl\$($e[1]).png", $r, $W, $H)
  "{0}: {1}x{2}" -f $e[1], $W, $H
}
