# Server closet — personagens

Imagem: `server-closet-characters-v1.png`

Gerada com a ferramenta integrada de geração de imagens. Referência: `server-closet-objects-v1.png`.

Três linhas, cinco quadros por linha: técnico de TI; estagiário com óculos e crachá; engenheiro com capacete, colete e caixa de ferramentas. Colunas: parado, caminhada A, caminhada B, conserto ajoelhado, comemoração.

## Verificação

Os quinze quadros estão separados e as identidades e roupas foram mantidas. Foi feita uma correção na terceira coluna, mas os dois passos ainda são semelhantes e precisam de revisão para garantir a alternância anatômica das pernas. O fundo gerado contém variações de magenta; o #FF00FF uniforme, a paleta limitada e a escala exata em relação ao PC não foram garantidos. Esta é uma base visual, ainda não uma animação recortada e validada no Unity.

## Prompt inicial

Use case: stylized-concept.
Asset type: ONE pixel art CHARACTER SPRITE SHEET for an early-2000s isometric idle game about an IT technician's tiny server closet.

REFERENCE: the supplied image is a STYLE AND PHYSICAL SCALE REFERENCE ONLY. Match its beige-PC-era game world, palette discipline, crisp pixel outlines and isometric camera. Do NOT draw any of its furniture or equipment on the new sheet.

OUTPUT LAYOUT:
Exactly 15 separate character pose sprites, in EXACTLY 3 horizontal rows and 5 columns.
One consistent character per row. One pose per column. All heads, hands, feet, tools and accessories fully visible, with ample flat magenta between cells, no touching or overlapping, safe magenta outer margins.
Use a sufficiently large canvas, approximately 1536 x 1280, so all 15 sprites have room without reducing the requested physical scale. No drawn grid, dividers, borders, frames, labels or captions.
Within each row, align the feet or lowest supporting body point to a common horizontal baseline; leave extra room above for raised arms. Kneeling poses are naturally shorter and must NOT be enlarged to match standing height. Maintain exact body proportions, outfit, head shape, pixel density and physical scale across every pose of a character.

STRICT STYLE:
True 2:1 isometric camera, slightly elevated, matching Habbo Hotel / SimCity 2000. Every pose faces DOWN-RIGHT / southeast toward the viewer, in the SAME three-quarter view. Show face and chest from the front-right three-quarter angle, nose and toes pointing diagonally to the LOWER RIGHT of the image. The right-facing side of the head is visible. NEVER switch to left-facing, straight front, profile or back-facing poses. Feet and toolboxes follow the shared 2:1 ground-plane axes; vertical bodies upright.
Top-left illumination throughout. Left-facing surfaces lighter, right-facing surfaces darker, flat stepped pixel shading.
Chunky pixel art with clearly visible square pixel clusters, one logical pixel dark outline. Approx. 32-color limited palette across the full sheet. Draw at genuinely low logical resolution, then exact nearest-neighbor integer upscale. Flat colors; no blur, gradients, antialiasing, realistic skin texture, dithering, noise or smooth 3D rendering.
Use stylized adult game-sprite proportions, charming but not toddlers.
Physical scale: from soles to top of head, each STANDING ADULT is approximately 1.6 TIMES the height of the BEIGE TOWER PC in the reference. The reference tower is about 190 rendered pixels tall, so standing adults should be about 300 rendered pixels tall at the same displayed scale. Keep all three adults at that same approximate standing height. Kneeling height reduced naturally; cheering extends hands above the normal head height.

BACKGROUND:
ONE absolutely flat solid opaque magenta fill, exact #FF00FF / RGB 255,0,255 everywhere outside sprites, including holes between limbs and tool handles. This is a literal uniform paint-bucket background, NOT a lit surface. No shadows on the background, no ground shadows, no drop shadows, no floor, no ground plane, no decorative backdrop. NO TEXT or symbols anywhere, including the badge; badge is plain colored pixels with no letters.

ROW 1 — IT TECHNICIAN:
Young adult, messy brown hair, plain medium-blue short sleeve t-shirt, dark indigo jeans, casual sneakers. Exactly the same face, hair, shirt, jeans and sneakers in every frame.
Column 1: standing idle, relaxed arms, facing down-right.
Column 2: walking LEFT FOOT FORWARD toward lower right, right foot back; opposite arm swing. A clearly readable walking keyframe.
Column 3: walking RIGHT FOOT FORWARD toward lower right, left foot back; opposite arm swing. A genuinely distinct opposite walking keyframe, not a mirrored character and not a duplicate.
Column 4: kneeling on one knee, bending slightly to fix something low in front with a screwdriver extended down-right. Show the screwdriver; no machine or workbench.
Column 5: celebrating, BOTH ARMS raised clearly above head, feet on the same virtual baseline, still facing down-right.

ROW 2 — INTERN:
Younger adult, short black hair, rectangular dark glasses, plain orange t-shirt, ID badge on a dark lanyard, beige pants, sneakers. Same identity and outfit across all five frames. Lanyard and glasses remain visible.
Columns 1 through 5: the EXACT SAME pose sequence — idle; walking LEFT FOOT forward; walking RIGHT FOOT forward; kneeling repair with screwdriver; cheering with BOTH ARMS above the head. Every pose facing down-right.

ROW 3 — FIELD ENGINEER:
Adult, yellow hard hat, orange safety vest over a grey short sleeve shirt, practical dark work pants and work shoes, carrying one compact toolbox with a handle. Hard hat and vest remain on in every pose.
Columns 1 through 5: the EXACT SAME pose sequence — idle holding toolbox at side; walking LEFT FOOT forward carrying toolbox; walking RIGHT FOOT forward carrying toolbox; kneeling repair with screwdriver and the toolbox placed immediately beside the knee inside this sprite's cell; cheering with BOTH ARMS lifted, holding the toolbox by its handle in one raised hand. Every pose facing down-right.
The toolbox is small relative to the body and is always part of the engineer's pose, never a separate sixteenth sprite.

FINAL CHECK:
Exactly 3 rows x 5 poses = 15 sprites.
Each row a distinct requested character, same identity within its row.
All 15 face DOWN-RIGHT, including kneeling and cheering.
Each walking pair alternates which foot is forward without changing facing direction.
Same 2:1 camera, common scale, top-left light, dark one-pixel outlines, chunky crisp pixels.
No words, labels, numbers, shadows or environment.
Entire empty background one perfectly uniform flat magenta #FF00FF.

## Correção de caminhada

Edit this exact 3-row, 5-column pixel art character sprite sheet. Correct ONLY the THREE WALKING SPRITES IN COLUMN 3 (one in each row). Preserve every other sprite, the character designs, their positions, their size, outfits, lighting, outlines and southeast / DOWN-RIGHT facing direction.

PROBLEM: columns 2 and 3 currently depict practically the SAME stride. They must be two genuinely OPPOSITE keyframes of a walk cycle, not duplicate images.

KEEP COLUMN 2 UNCHANGED.
In COLUMN 3, for ALL THREE CHARACTERS:
- REVERSE THE STRIDE PHASE relative to column 2. The leg currently extended forward must now be behind the hips, and the leg currently behind must now be extended FORWARD diagonally DOWN-RIGHT.
- Swap which thigh crosses IN FRONT of the other. The near leg now trails behind with a visibly bent knee and lifted heel, while the other leg swings ahead and plants its heel toward the lower right. Keep the hips and head in the same place; do not mirror the whole sprite.
- The arms swing in the opposite phase from column 2: move the arm that was forward backward, and the arm that was backward forward. Make the silhouette visibly DIFFERENT from column 2 while still naturally walking down-right.
- For the field engineer, keep the toolbox attached to the same anatomical hand, swinging with that arm, while reversing the stride. Keep the hard hat and vest identical.
- Knees and shoes must remain visibly separated so the alternate foot placement reads clearly. An unmistakable reverse walking step, not a small shading change.

ALL INVARIANTS:
Exactly 15 sprites in the same 3 rows and 5 columns.
Rows: messy brown-haired blue-shirt technician; black-haired intern with glasses, orange shirt and lanyard, beige pants; yellow hard-hat field engineer with orange vest, gray shirt and toolbox.
Columns: idle, walk phase A, CORRECTED walk phase B, kneeling screwdriver repair, cheering arms up.
All face DOWN-RIGHT in same three-quarter isometric view; never mirror a character to face left.
Same character identity, scale and consistent baselines.
Crisp square pixel art, 1 logical pixel dark outline, limited palette, flat stepped shading, light from upper left.
No text, no labels, no floor, no shadows.
Solid opaque magenta #FF00FF background throughout, as a pure flat fill with no texture or variations.
Do not move, regenerate or redesign any of the other twelve sprites.

