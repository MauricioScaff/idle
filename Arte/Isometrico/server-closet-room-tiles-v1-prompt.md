# Server closet — piso, paredes e decoração

Imagem: `server-closet-room-tiles-v1.png`

Gerada com a ferramenta integrada de geração de imagens. A folha anterior de objetos foi usada como referência visual.

Sete peças: dois pisos, parede esquerda, parede direita, canto interno, janela noturna e quadro sem texto.

## Limitações verificadas

O fundo gerado contém variações de magenta. O arquivo também apresenta variações de tom nas paredes. O magenta #FF00FF exato, a paleta reduzida sem gradientes, os encaixes e as dimensões modulares precisam de acabamento técnico antes de usar a folha como tileset final no Unity.

## Prompt

Use case: stylized-concept.
Asset type: one pixel art ISOMETRIC ROOM TILESET SPRITE SHEET for a tiny IT technician server closet game.

The supplied image is STYLE REFERENCE ONLY: match its early-2000s isometric pixel art, chunky outlined shapes, limited colors and common camera. Do NOT include or repeat its objects. Generate ONLY the following seven room building pieces, separately, as a clean sprite sheet. This is a set of modular game tiles, NOT a complete assembled room.

STYLE AND GEOMETRY:
True 2:1 isometric projection like Habbo Hotel / SimCity 2000. Every horizontal world axis follows exactly 2 pixels horizontally for every 1 pixel vertically. All vertical edges perfectly vertical. Same fixed camera, no perspective vanishing point. Crisp square pixels and 1-logical-pixel dark outlines. Limited palette, approximately 24-32 flat colors TOTAL. No gradients, no dithering, no grain, no texture noise, no anti-aliasing, no blur, no smooth 3D shading. Light from upper left; all corresponding left-facing surfaces lighter and right-facing surfaces darker.
Use a shared logical tile size: floor diamond is 96 logical pixels wide and 48 logical pixels high. Wall module occupies ONE edge of that diamond (48 logical pixels across by 24 logical pixels of rise), and is 96 logical pixels vertically tall, with very thin wall thickness. All wall segments have the same exact height, edge span, outline weight, top thickness and baseboard height. Window and poster must be scaled to fit WITHIN ONE wall module. Show the sprites cleanly at uniform nearest-neighbor enlargement; do not change scale to fill individual cells.

BACKGROUND:
Fill the entire sheet with exactly one FLAT solid opaque magenta color #FF00FF, RGB 255,0,255. Treat it as a literal solid fill, NOT a lit backdrop. Absolutely no variation in magenta, no grain, no shadow, no gradient, no floor, no checkerboard, no transparency. Any empty spaces between wall panels and inside a corner footprint are also the exact same flat magenta.
No text, labels, letters, numerals, captions, borders, frames around groups or grid lines. The wooden frame belonging to the requested poster and the window frame are allowed.
Generous magenta separation: no different pieces touch or overlap, no cropped edges.

ARRANGE EXACTLY SEVEN SEPARATE PIECES, in three loose rows:
TOP ROW, two isolated pieces centered in their own spaces:
1. One flat isometric floor diamond, exact 2:1 outline. Dark wood/mauve planks, restrained burgundy-brown and dusty mauve tones, a few clean staggered plank seams aligned to a world axis. Very shallow edge of at most one logical pixel. No room walls, no objects on this tile.
2. A SECOND version of exactly the same floor diamond, same outer shape, size, palette and brightness, only vary the plank seam arrangement slightly. The two floor tiles can alternate seamlessly.

MIDDLE ROW, three isolated pieces:
3. BACK-LEFT WALL: one module wide and tall, dark navy blue painted wall panel with a darker baseboard at the bottom. The plane follows the back-left boundary of an isometric room: from its left/front end at lower-left to its right/back end at upper-right, the top and bottom edges RISE to the right at the exact 2:1 slope. See its interior face and thin top cap. Lighter navy on this left wall. No attached floor tile.
4. BACK-RIGHT WALL: the same-height matching module, its top and bottom edges DESCEND to the right at exact 2:1 slope, from the central back corner at upper-left toward its right/front end at lower-right. Interior face darker navy than the left wall. Identical dark baseboard construction. No attached floor tile. These two pieces are opposite wall orientations, not duplicates facing the same way.
5. INNER BACK CORNER: one left-wall module and one right-wall module joined into an inside corner, meeting at a vertical central back edge, forming an inverted V in their top edges. Same exact wall height, module width, thickness, palette and baseboard as pieces 3 and 4. Looking INTO the room corner, see both navy interior wall faces; left is lighter and right is darker. No solid floor attached: the open space below/in front is magenta. This is ONE compact corner sprite, isolated from all other pieces.

BOTTOM ROW, two isolated pieces:
6. SMALL WINDOW FOR LEFT WALL: an isolated window inset and modest pale frame, its shape sheared in the exact same orientation as the BACK-LEFT WALL: top and bottom rise to the right in 2:1 steps, sides vertical. Small night-city view within it: deep blue sky, a few tiny stars, simple dark building silhouettes with a few warm yellow and cyan lit square windows. No surrounding wall panel and no ground. Window small enough to fit one wall segment with generous margins.
7. FRAMED POSTER FOR RIGHT WALL: one small wood-framed picture, sheared to match the BACK-RIGHT WALL: top and bottom descend to the right at the same exact 2:1 angle, sides vertical. Simple retro abstract landscape illustration with muted teal mountains and a tiny warm orange sun. NO WORDS, no text. Isolated frame only, no surrounding wall. It must fit comfortably within a single right wall module.

FINAL CHECK: two matching 2:1 floor diamonds, two matching opposite-direction tall wall segments, one matching inside wall corner, one left-oriented small night-city window, one right-oriented small framed picture. Exactly seven separated usable tile pieces. Shared scale and top-left lighting. Magenta-only empty background.

