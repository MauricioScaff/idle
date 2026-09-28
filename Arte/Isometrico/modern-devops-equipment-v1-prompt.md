# Sala moderna de DevOps — equipamentos e mobiliário

Imagem: `modern-devops-equipment-v1.png` (1774 × 887 pixels).

Criada com a ferramenta integrada de geração de imagens. Referências: `proper-server-room-equipment-v1.png` e `server-closet-objects-v1.png`. Arquivo salvo no projeto, sem recorte dos sprites ou importação no Unity.

## Conteúdo, da esquerda para a direita

Linha superior:
1. Rack de hypervisor com LEDs roxos.
2. Host de containers em rack curto, módulos azuis e luzes normais.
3. Mesmo modelo de host com luzes vermelhas de falha.
4. Servidor de CI com barra verde de progresso.
5. Rack de switches de rede com cabos de fibra azuis.

Linha inferior:
6. Mesa de trabalho em pé com dois monitores.
7. TV de parede com dashboard e gráficos.
8. Sofá compacto cinza-escuro.
9. Máquina de café sobre um balcão.
10. Puff laranja.

## Verificação

Os dez objetos aparecem completos e separados. O host com falha conserva os módulos azuis e apresenta indicadores vermelhos. Monitores usam traços abstratos para sugerir código; os gráficos não têm legendas.

O fundo NÃO atende ao requisito técnico de magenta uniforme #FF00FF: amostras em (0,0), (800,500) e (1773,886) são #ED0CF1, #FB04FB e #F122F1, com alpha 255. A paleta de aproximadamente 32 cores, o contorno de exatamente um pixel, a projeção matemática 2:1, a escala exata entre folhas e a igualdade pixel a pixel dos hosts não foram garantidos. A folha ainda precisa de acabamento técnico antes de ser usada como sprites finais.

## Prompt usado

Use case: stylized-concept.
Asset type: one pixel art sprite sheet for a modern virtualized server room / DevOps idle game.

REFERENCE IMAGES:
Image 1: the established server room equipment sheet, reference for rack size, isometric camera, materials, outlines and top-left lighting.
Image 2: the original server closet props, reference for chunky pixel clusters, shared furniture scale and the beige PC size anchor.
Create ONLY the ten NEW objects below, not the objects pictured in those references. Tall racks should match the rack height in image 1, approximately twice the beige tower PC height. Do not copy any background color variation from either reference.

LAYOUT:
One wide landscape sheet, exactly ten isolated sprites arranged in two spacious rows of five. Upper row objects 1–5; lower row objects 6–10, in that order from left to right. Give the lower row adequate horizontal room for the standing desk, big TV and sofa. All parts of each object are connected to their parent sprite. Lots of empty magenta between sprites and at all canvas edges, nothing touching or overlapping, no clipping. Keep all items at a consistent physical scale: compact host racks are shorter than full racks, furniture matches the desk in reference 2, smaller props must not be inflated just to fill cells. No room or floor.

UPPER ROW:
1. Hypervisor server rack: tall dark graphite full-height cabinet with clearly visible horizontal server modules, neat PURPLE accent LED strips and small purple status squares. Keep hardware readable and rectangular. Solid pixel lights, no soft halos.
2. Container host: short dark half-height rack, with several distinct BLUE rectangular container-like modules stacked on the front, small vent details, normal green/cyan status lights. These modules should visually read as stacked compute containers integrated into the chassis.
3. EXACT DUPLICATE of object 2, same chassis, dimensions, blue modules, angle and details. Change only its small status lights to RED to indicate a broken deploy. Keep all blue container modules BLUE. No warning text, icons, smoke or fire.
4. CI build server: a tall enclosed dark gray tower cabinet, ventilation details, a small inset screen on its front showing a simple GREEN horizontal progress bar partly filled against a dark track. No words, percentages, numbers or letters.
5. 10G core network switch rack: a tall dark rack with multiple densely populated switch panels and MANY thin BLUE fiber patch cables. Orderly visible blue cable loops routed through cable managers, connected at both ends and kept close to the rack. Visible ports and small cyan LEDs. No speed labels.

LOWER ROW:
6. Standing desk: raised medium-wood desktop on dark height-adjustable metal legs, keyboard and mouse on the desktop, exactly TWO slim flat monitors showing code as small green/cyan pixel dashes in indented rows. Code must be abstract unreadable pixel marks, not actual letters. Desk surface higher than the seated desk in the reference but shorter than full-height racks. Do not include a chair or a person.
7. Big wall-mounted dashboard TV: wide thin dark screen with a visible slim back/mount, in the same isometric angle, showing colorful bar charts, a line chart and simple status blocks. Absolutely no text, digits or labels. No wall behind it and no TV stand.
8. Small dark gray two-seat sofa: two cushions, compact backrest, short armrests and four tiny feet. Upholstery modeled with two or three hard-edged gray tones only, no smooth shading.
9. Coffee machine on a small counter: compact black/silver espresso machine with drip tray and dispenser, on a narrow short wooden counter/cabinet. Treat machine and counter as ONE sprite. No extra coffee cups detached from the counter, no steam.
10. Orange beanbag chair: low soft pear-shaped seat with a clear seat depression and a few angular stitched panel shapes. Chunky pixel silhouette and flat orange palette, not glossy.

STRICT SHARED ART RULES:
True 2:1 orthographic isometric camera, same rotation for every sprite. Horizontal plane diagonals use the exact two-horizontal-pixels / one-vertical-pixel stepping rhythm. We see top, left and right faces, nearest vertical edge toward the viewer. Vertical lines stay vertical. Light comes from top-left, so left faces are lighter and right faces darker.
Authentic chunky pixel art, crisp square pixels, dark one-pixel outline at the working pixel grid, approximately 32 colors. Hard flat color regions with pixel clusters only. No blur, gradients, anti-aliasing, textured render, photographic surface or ambient occlusion.
The backdrop is a digital color-key field, NOT a lit material: EVERY background pixel, including holes in objects and spaces between legs, must be exactly RGB 255,0,255 / HEX #FF00FF, fully opaque, perfectly flat. No background noise, vignette, texture or hue variations. No shadows on the background, no ground, no floor tiles.
NO text, readable code, lettering, numbers, labels, logos, frames, border, grid, watermark, people or extra props.

