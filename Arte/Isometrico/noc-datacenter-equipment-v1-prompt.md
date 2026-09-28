# Data center com NOC — equipamentos

Imagem: `noc-datacenter-equipment-v1.png` (1983 × 793 pixels).

Criada com a ferramenta integrada de geração de imagens, usando `modern-devops-equipment-v1.png` como referência de estilo, câmera e escala. Salva no projeto como folha visual; os sprites ainda não foram recortados ou importados no Unity.

## Conteúdo, da esquerda para a direita

Linha superior:
1. Rack de nós Kubernetes com LEDs cianos.
2. Versão do mesmo rack com LEDs e destaques laranja para pico de tráfego.
3. Load balancer sobre suporte baixo, com indicadores verdes.
4. Mesa curva de NOC com três monitores, gráficos e mapa-múndi.
5. Video wall com três telas.

Linha inferior:
6. Servidor de observabilidade com linha verde de pulsação.
7. Segmento de divisória de vidro.
8. Carrinho elevador de servidores.
9. Porta com leitor de crachá.
10. Robô compacto de manutenção/aspiração.

## Verificação

A folha contém os dez objetos separados e completos, sem texto visível. A segunda geração corrigiu a mesa inicialmente frontal para uma vista em três quartos e substituiu os pequenos LEDs cianos restantes do rack de tráfego por laranja.

O fundo ainda apresenta variações de magenta e NÃO atende ao requisito técnico de #FF00FF uniforme. Amostras em (0,0), (1000,50) e (1982,792): #E90CF1, #F603F9 e #EC21EE, com alpha 255. A projeção exata 2:1, escala entre folhas, limite da paleta, contorno de um pixel e identidade pixel a pixel dos racks não foram garantidos. Precisam de acabamento técnico antes do uso como sprites finais.

## Prompt inicial

Use case: stylized-concept.
Asset type: one pixel art sprite sheet for an isometric idle game, a small modern data center with a NOC.

REFERENCE ROLE:
The attached image is a style, camera, lighting and physical-scale reference for this same game's existing equipment. Match its cabinet heights, furniture scale and dark outlines. Create ONLY the ten new sprites listed here. Do not reuse the old objects. Do not copy any background noise or gradient from the reference.

SHEET LAYOUT:
A spacious wide landscape sheet with exactly TEN complete isolated objects in a loose two-row, five-column layout. Upper row items 1–5 from left to right. Lower row items 6–10 from left to right. Give the long NOC desk and the three-screen video wall extra width; enlarge the canvas instead of shrinking those items or changing their physical scale. Leave generous clean empty magenta gaps and outer margins. Small objects remain appropriately small. No overlap, no touching, no clipping, no floors or room.

OBJECTS:
1. Kubernetes node rack: tall full-height black server cabinet, horizontal server modules and a clear arrangement of many small CYAN LED squares. Same height, width and hardware scale as the tall racks in the reference. No Kubernetes logo or lettering.
2. Exactly the same node rack as item 1: preserve every server, silhouette, detail, angle and dimension. The lights and illuminated front accents are bright ORANGE to communicate a traffic spike. Use hard-edged orange pixel highlights on the hardware only; do not change the cabinet shape, and do not cast a glow or halo on the magenta.
3. Load balancer appliance: a low flat dark metal network box with many tiny GREEN port/status lights, sitting on a small short metal stand. Box and stand form one sprite. Rack-width appliance at the same shared scale; no writing.
4. NOC desk: a LONG gently CURVED workstation desktop, a clearly recognizable concave operator-facing edge and connected support legs, with exactly THREE monitors arranged around the curve. One monitor shows a cyan pixel silhouette of a world map, the other two show a line graph and a bar chart. Keyboard and mouse on the desk, simple unreadable pixel graphics only. No lettering, numbers, chair or person.
5. Big wall-mounted NOC video wall: exactly THREE large rectangular screens side by side in one connected slim black frame with a small mounting bracket visible. Display complementary charts, cyan world map shapes and status blocks without any labels or text. Correct isometric angle for a wall-mounted panel. No wall behind it or supporting floor stand.
6. Observability server: tall dark gray server cabinet with vents and a clearly visible built-in screen showing one bright GREEN heartbeat waveform, angular connected pixels on a dark screen. No text, no medical symbols or labels.
7. Glass partition wall segment: a tall rectangular pane with a slim dark metal frame, small base mounting feet, pale blue glass and a few angular pale reflection marks. Same 2:1 isometric wall orientation and room scale. Flat stylized glass shading, no photographic reflections, no floor or surrounding walls.
8. Server lift / trolley cart: a small wheeled industrial server lift with a sturdy base, four casters, upright lifting mast, push handle, lift mechanism and one raised flat platform suitable for supporting a rack server. One connected sprite, no loose cargo or additional carts.
9. Security badge door: a single closed dark gray door within its own slim frame, a modest window inset, horizontal handle, and a compact badge reader attached to the side of its frame with one cyan indicator. Wall-ready asset in the same isometric wall orientation as the glass partition. Human-sized door, no surrounding wall, exit text, logo or sign.
10. Small robot vacuum / maintenance drone: a low compact circular gray floor robot, dark bumper, tiny wheels tucked under the body, one small cyan sensor lens and a little side brush. Appropriately small relative to the equipment. No flying rotor or ground shadow.

STYLE AND TECHNICAL RULES:
True 2:1 orthographic isometric projection, every asset from the identical camera: top, left and right surfaces visible, vertical edges vertical, ground-plane diagonals use the two-horizontal-pixel to one-vertical-pixel stepping rhythm. Nearest vertical corner faces viewer.
Light from the top-left: left faces lighter, right faces darker. Strong pixel silhouettes and crisp square pixel clusters, dark 1-pixel contour at the working pixel grid, a limited palette of approximately 32 flat colors. Use discrete hard-edged colors, not smooth shading. No gradients, blur, anti-aliasing, photographic noise or texture.
The background must be ONE unlit digital solid-color fill #FF00FF, exactly RGB(255,0,255), fully opaque throughout the canvas and every empty gap. No magenta variations, grain, vignette or texture. All lighting is confined to object pixels; absolutely NO shadows, reflections, glow or colored spill on the background.
No text, labels, letters, numbers, watermarks, borders, grid lines, characters, extra objects or environment.

## Correção

Precise edit of this sprite sheet. Preserve the canvas, ten objects, positions, sizes and all designs except for these two corrections:
1. TOP ROW, FOURTH SPRITE, the curved NOC desk: it currently faces the viewer straight-on. Redraw this whole desk and its three monitors in a true 2:1 isometric THREE-QUARTER view, using exactly the camera of the server racks and wall panels. Rotate the workstation so its long direction recedes diagonally down-right, consistent with the video wall at its right. Keep its long gently curved desktop with a concave operator-facing edge, exactly three monitors, central world map, side line and bar charts, keyboard, mouse and connected legs. The horizontal-plane axes must have 2 horizontal pixels per 1 vertical pixel, no flat horizontal front-view arrangement. Show the desk top and two vertical planes, with left face lighter than right. Fit within this sprite's current allocated area without touching its neighbors.
2. TOP ROW, SECOND SPRITE, traffic-spike rack: change ALL its remaining tiny cyan LEDs to orange or amber. Keep the orange large LEDs. Give the front hardware subtle hard-edged orange highlight pixels to clearly signal the spike, but absolutely no haze, bloom, blur, halo, shadows or light on the background. Preserve the chassis, server layout and cabinet dimensions; keep it a matching variant of the first rack. Do not change the first cyan rack.
Everything else must be unchanged. Maintain crisp chunky pixel art, one-pixel dark working-grid outlines, a limited flat palette, top-left lighting. No text or numbers. Uniform fully opaque #FF00FF background with no gradients or texture; no shadows. Keep every sprite isolated.

