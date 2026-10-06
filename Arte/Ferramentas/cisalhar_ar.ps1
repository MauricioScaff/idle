# Cisalha o ar-condicionado (como aparece no jogo) para a borda de cima subir 1 px a cada 2 (a parede da esquerda)
param([string]$sai)
Add-Type -AssemblyName System.Drawing
$b = [System.Drawing.Bitmap]::FromFile("C:\Users\Mauri\jogo\Assets\_IdleDataCenter\Resources\Arte\PixelLab\ar_condicionado.png")

function Topo($x) { for ($y = 0; $y -lt $b.Height; $y++) { if ($b.GetPixel($x, $y).A -gt 0) { return $y } }; return -1 }
$x1 = 6; $x2 = $b.Width - 7
$inclinacao = ((Topo $x2) - (Topo $x1)) / ($x2 - $x1)
"inclinacao atual: $inclinacao"
$k = -0.5 - $inclinacao   # quanto cada coluna à direita precisa subir a mais
$extra = [Math]::Ceiling([Math]::Abs($k) * ($b.Width - 1))
$n = New-Object System.Drawing.Bitmap $b.Width, ($b.Height + $extra)
for ($x = 0; $x -lt $b.Width; $x++) {
  $d = [Math]::Round(-$k * ($b.Width - 1 - $x))   # a esquerda desce
  if ($k -gt 0) { $d = [Math]::Round($k * $x) }
  for ($y = 0; $y -lt $b.Height; $y++) { $c = $b.GetPixel($x, $y); if ($c.A -gt 0) { $n.SetPixel($x, $y + $d, $c) } }
}
$n.Save($sai)
"salvo $($n.Width)x$($n.Height)"
