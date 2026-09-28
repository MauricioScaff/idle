# Sala de servidores — infraestrutura avançada

Imagem: `proper-server-room-equipment-v1.png` (1774 × 887 pixels).

Gerada com a ferramenta integrada de imagens, usando as folhas de equipamentos, objetos do armário e tiles como referências. O arquivo é uma folha visual; ainda não foi recortado ou importado no Unity.

## Conteúdo

1. Rack completo com LEDs verdes e azuis.
2. Rack completo com um LED vermelho indicando falha.
3. Rack de storage com gavetas de discos.
4. Storage com um LED vermelho indicando disco com falha.
5. Biblioteca de fitas de backup.
6. Caixa de terminação óptica com cabos laranja.
7. Ar-condicionado de precisão (CRAC).
8. Tile de piso elevado com perfurações.
9. Eletrocalha com cabos e suportes.
10. Extintor de incêndio.

## Verificação e acabamento pendente

Os dez objetos estão separados, sem texto, e os estados de falha são visíveis. A segunda geração ampliou o tile ventilado para aproximar sua largura dos tiles anteriores.

O fundo continua com variações de magenta apesar da instrução explícita e da tentativa de correção: amostras em (0,0), (800,500) e (1773,886) são #ED0DF1, #FA04FB e #EE22F1, todas opacas. Portanto, ele ainda não atende ao requisito técnico de #FF00FF uniforme. A paleta de cerca de 32 cores, os contornos de exatamente um pixel, a escala modular e a igualdade pixel a pixel entre cada par não estão garantidos. A folha precisa desse acabamento antes de uso como sprites finais.

## Prompt inicial

Use case: stylized-concept.
Asset type: a single pixel-art sprite sheet for an isometric idle management game, a proper server room in the mid-2000s.

REFERENCE ROLES:
Image 1 is the equipment sheet: match the black rack design, camera, materials and shared object scale.
Image 2 is the original closet sheet: match its chunky pixel art and use the beige tower PC as the size anchor. Each tall rack is approximately twice the tower PC's height. Do not include the reference objects in the new sheet.
Image 3 is a room tiles sheet: use only the footprint and true 2:1 diamond geometry of its floor tiles for the new perforated floor tile.
The new sheet must have a perfectly uniform background, even if references have color variation.

COMPOSITION:
Exactly TEN isolated objects, one of each item below. A spacious landscape canvas, with five objects across the upper band in order 1-5, and five across the lower band in order 6-10. Allow wider spacing around the floor tile and cable tray. Maintain a shared physical scale across the whole sheet: do not enlarge little props to fill their slots. Keep every object fully visible, with abundant uninterrupted magenta between sprites and around canvas edges. No contact, overlap, clipping, scenery, room, grid lines, captions or decorative frame.

OBJECTS:
1. Full black 42U server rack, packed from top to bottom with thin silver/dark server faceplates, vent details and many tiny square GREEN and BLUE LEDs. The top and two vertical faces are visible. About twice the height of the beige tower PC reference.
2. An exact duplicate of object 1, same chassis, equipment, silhouette, proportions, position of highlights and all details; change ONLY ONE green status LED on a server near the center to RED. No other red LEDs, no warning symbol, smoke or sparks.
3. Tall storage array rack in a dark chassis, disk shelves filled with rows of many small individual hot-swap drive bays and tiny green status LEDs. Distinguish it clearly from thin compute servers. Same tall-rack scale as object 1.
4. An exact duplicate of object 3, preserving every drive and chassis detail; change ONLY ONE green drive LED to RED on a single failed drive near the center. No other changes.
5. Tape backup library: a medium-height dark gray cabinet with multiple tape cartridge slots, loading mail slot, recessed drive mechanisms, and a small cyan status screen showing simple colored blocks only. No writing.
6. Small wall-mounted fiber optic termination/link box with orange fiber cables neatly emerging and curving below it, compact ports and mounting tabs. Draw the box and attached fibers as one isolated sprite. No wall behind it.
7. Tall gray precision air conditioning cabinet, CRAC, approximately rack height: substantial upper ventilation grille, lower access panels, small control display with colored squares only. No readable characters.
8. ONE raised floor ventilation tile: a thin steel-gray isometric diamond with a regular pattern of small dark ventilation holes. The top diamond has exact 2:1 width-to-height ratio and the same footprint size as the reference floor tile; show only a minimal dark thickness edge, no supports or surrounding floor.
9. A horizontal metal cable tray section mounted along the implied back wall, following the same 2:1 isometric direction: open ladder-like tray with a small neat bundle of blue and orange cables and two mounting brackets. No wall behind it. One connected sprite.
10. Small red fire extinguisher with black hose, handle, pressure gauge and plain blank light label. No lettering or symbols. Natural small prop scale.

PIXEL TECHNIQUE AND CAMERA:
Genuine chunky pixel art with crisp square pixel clusters, a one-pixel dark contour at the working pixel grid, about 32 flat colors total. Flat fills with discrete hard-edged color steps only. All objects use the identical orthographic true 2:1 isometric camera: diagonal ground-plane edges step 2 pixels sideways per 1 pixel vertically, vertical edges stay vertical, top and left and right surfaces visible, nearest vertical edge facing the viewer. Top-left lighting throughout: left-facing planes lighter, right-facing planes darker. LEDs are tiny solid colored squares, with no glow bleeding onto the background.
Every background pixel MUST be solid RGB(255,0,255), HEX #FF00FF, fully opaque, including gaps and holes. Absolutely NO background texture, noise, gradient, vignette, hue variation, ground, cast shadow, drop shadow, blur, anti-aliasing, smooth rendering or photorealism. NO text, numbers, letters, labels, logos, watermark or border anywhere.

## Correção

Edit this sprite sheet with ONLY these technical corrections, preserving all ten sprites and their order.
1. The perforated raised-floor tile (lower row, center) is too small compared with the original floor tiles of this game's asset set. Increase only this sprite from its current approximately 258-pixel width to approximately 410-pixel width on this same 1774-pixel-wide canvas. Keep its center at the same horizontal position. Its top surface must remain a precise 2:1 isometric diamond: 410 pixels wide and 205 pixels tall, plus a thin dark thickness edge. Enlarge through crisp pixel-art redrawing, no interpolation or blurring. Retain its steel-gray finish and orderly dark holes. Leave all other objects at their current size and position, with no overlaps.
2. Replace every background pixel with exactly flat opaque #FF00FF, RGB(255,0,255). This is a sprite extraction key, it must be ONE uniform color with absolutely no gradient, noise, texture or shading. Do not add any ground shadow.
INVARIANTS: preserve both compute racks as the matching pair, with exactly one red LED on the second; preserve both storage racks as the matching pair, with exactly one red LED on the second. Preserve all equipment designs, rack heights, silhouettes, top-left lighting, the two-row arrangement, the same orthographic 2:1 isometric camera and small-prop scale. No text or symbols, no new objects, no frames, no scenery. Hard-edged square pixel clusters, dark one-pixel working-grid outlines, limited flat palette, no anti-aliasing.

