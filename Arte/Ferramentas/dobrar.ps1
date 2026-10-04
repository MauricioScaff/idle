# Gera as salas dobradas (Salas/*_hd.png) e os equipamentos 1,5x (PixelLab/*_g.png) a partir das ilustracoes base.
# Rodar: powershell -ExecutionPolicy Bypass -File Arte\Ferramentas\dobrar.ps1
$ErrorActionPreference = 'Stop'
if (-not ("Dobrar" -as [type])) { Add-Type -Path "$PSScriptRoot\Dobrar.cs" -ReferencedAssemblies System.Drawing }
$salas = "$PSScriptRoot\..\PixelLab\salas_base"
$arte = "$PSScriptRoot\..\..\Assets\_IdleDataCenter\Resources\Arte"

# móveis e objetos da ilustração (ficam só com o Scale2x): x, y, largura, altura (na original)
$manter = @{
  salinha = @(172,82,52,84,  210,66,40,76,  245,78,48,104,  264,132,94,100)
  racks   = @(48,106,50,64,  119,104,18,36,  143,66,40,90,  218,66,42,90,  50,135,82,78)
  dc      = @(34,128,62,82,  93,108,40,64,  268,108,30,58)
}
foreach ($n in $manter.Keys) {
  $w = 0; $h = 0
  $p = [Dobrar]::Ler("$salas\$n.png", [ref]$w, [ref]$h)
  $o = [Dobrar]::Dobro($p, $w, $h, [int[]]$manter[$n], 45)
  [Dobrar]::Gravar("$arte\Salas\${n}_hd.png", $o, $w * 2, $h * 2)
}
# racks, no-breaks e ar-condicionado a 1,5x: Scale2x e redução pela moda (os LEDs não somem)
foreach ($n in 'rack', 'nobreak', 'ar_condicionado') {
  $w = 0; $h = 0; $W = 0; $H = 0
  $p = [Dobrar]::Ler("$arte\PixelLab\$n.png", [ref]$w, [ref]$h)
  $d = [Dobrar]::Scale2x($p, $w, $h)
  $r = [Dobrar]::Reduzir($d, $w * 2, $h * 2, 0.75, [ref]$W, [ref]$H)
  [Dobrar]::Gravar("$arte\PixelLab\${n}_g.png", $r, $W, $H)
}
