# Monta as salas na escala da pessoa (Salas/*_hd.png) e gera o rack 1,25x (PixelLab/rack_g.png).
# Rodar: powershell -ExecutionPolicy Bypass -File Arte\Ferramentas\montar.ps1 [pasta de saida]
param([string]$saida = "")
$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot\Dobrar.cs", "$PSScriptRoot\Montar.cs", "$PSScriptRoot\Salas.cs" -ReferencedAssemblies System.Drawing
$base = "$PSScriptRoot\..\PixelLab\salas_base"
$arte = "$PSScriptRoot\..\..\Assets\_IdleDataCenter\Resources\Arte"
$destSalas = if ($saida) { $saida } else { "$arte\Salas" }
$destPl = if ($saida) { $saida } else { "$arte\PixelLab" }

foreach ($s in [Salas]::Todas()) {
  $w = 0; $h = 0
  $orig = [Dobrar]::Ler("$base\$($s.nome).png", [ref]$w, [ref]$h)
  $px = [Montar]::Desenhar($s, $orig, $w, $h)
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
