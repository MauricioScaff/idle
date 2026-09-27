using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Arte gerada no PixelLab (em Resources/Arte). Carrega as imagens, recorta as bordas transparentes,
    /// cria sprites com o pivô no chão, recolore uniformes e acha os LEDs lendo os pixels.
    /// </summary>
    public static class ArteGerada
    {
        static readonly Dictionary<string, Texture2D> texturas = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Texture2D Textura(string nome)
        {
            if (texturas.TryGetValue(nome, out var t)) return t;
            t = Resources.Load<Texture2D>("Arte/" + nome);
            if (t == null) throw new ArgumentException("Arte não encontrada: Resources/Arte/" + nome);
            t.filterMode = FilterMode.Point;
            return texturas[nome] = t;
        }

        /// <summary>Retângulo dos pixels não transparentes (origem embaixo à esquerda, como a textura).</summary>
        public static RectInt AreaOpaca(Texture2D t)
        {
            var px = t.GetPixels32();
            int x0 = t.width, y0 = t.height, x1 = -1, y1 = -1;
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
                if (px[y * t.width + x].a > 0)
                {
                    x0 = Math.Min(x0, x); y0 = Math.Min(y0, y);
                    x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
                }
            return x1 < 0 ? new RectInt(0, 0, t.width, t.height) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        /// <summary>
        /// Objeto (servidor, rack, móvel): recortado ao que é opaco, pivô no centro da base.
        /// O pivô fica num pixel inteiro para a arte não borrar.
        /// </summary>
        public static Sprite Objeto(string nome)
        {
            if (sprites.TryGetValue("obj:" + nome, out var s)) return s;
            var t = Textura(nome);
            var r = AreaOpaca(t);
            var pivo = new Vector2(Mathf.Floor(r.width / 2f) / r.width, 0f);
            s = Sprite.Create(t, new Rect(r.x, r.y, r.width, r.height), pivo, 1f, 0, SpriteMeshType.FullRect);
            s.name = nome;
            return sprites["obj:" + nome] = s;
        }

        /// <summary>
        /// Quadro de personagem: tela inteira, pivô no centro horizontal e na linha dos pés,
        /// para quadros de tamanhos diferentes ficarem alinhados.
        /// </summary>
        public static Sprite Personagem(Texture2D t, string chave)
        {
            if (sprites.TryGetValue("per:" + chave, out var s)) return s;
            var r = AreaOpaca(t);
            var pivo = new Vector2(Mathf.Floor(t.width / 2f) / t.width, (float)r.y / t.height);
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivo, 1f, 0, SpriteMeshType.FullRect);
            s.name = chave;
            return sprites["per:" + chave] = s;
        }

        /// <summary>
        /// Cópia da textura com a roupa azul trocada por outro matiz (0..1), mantendo luz e sombra.
        /// Só mexe abaixo da cabeça e em tons claros o bastante (os olhos e a calça escura ficam iguais).
        /// </summary>
        public static Texture2D TrocarCorDaRoupa(Texture2D origem, float novoMatiz, Vector2Int? cracha = null, float brilho = 1f)
        {
            var px = origem.GetPixels32();
            var r = AreaOpaca(origem);
            int limiteCabeca = r.yMax - Mathf.RoundToInt(r.height * 0.36f); // acima disso é cabeça
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a == 0 || i / origem.width >= limiteCabeca || c.r + c.g + c.b < 200) continue;
                Color.RGBToHSV(c, out float h, out float sat, out float v);
                if (h < 0.52f || h > 0.72f || sat < 0.25f) continue; // só azuis
                var n = (Color32)Color.HSVToRGB(novoMatiz, sat, v * brilho);
                px[i] = new Color32(n.r, n.g, n.b, c.a);
            }
            if (cracha.HasValue)
            {
                var p = cracha.Value;
                px[p.y * origem.width + p.x] = new Color32(253, 246, 227, 255);
                px[(p.y - 1) * origem.width + p.x] = new Color32(90, 150, 220, 255);
            }
            var nova = new Texture2D(origem.width, origem.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            nova.SetPixels32(px);
            nova.Apply();
            return nova;
        }

        /// <summary>
        /// Posições (em pixels, relativas ao pivô do sprite) dos pixels que parecem LED:
        /// cor viva e clara, puxando para verde/azul.
        /// </summary>
        public static List<Vector2Int> Leds(Sprite s)
        {
            var lista = new List<Vector2Int>();
            var t = s.texture;
            var r = s.rect;
            for (int y = 0; y < (int)r.height; y++)
            for (int x = 0; x < (int)r.width; x++)
            {
                var c = t.GetPixel((int)r.x + x, (int)r.y + y);
                float max = Mathf.Max(c.r, c.g, c.b), min = Mathf.Min(c.r, c.g, c.b);
                if (c.a > 0 && max - min > 0.35f && max > 0.66f && c.g >= c.r)
                    lista.Add(new Vector2Int(x - Mathf.RoundToInt(s.pivot.x), y - Mathf.RoundToInt(s.pivot.y)));
            }
            return lista;
        }

        /// <summary>Retorna os pixels de uma textura de cima para baixo (como um canvas), para desenhar na PixelCanvas.</summary>
        public static Color32[] PixelsDeCimaParaBaixo(Texture2D t)
        {
            var px = t.GetPixels32();
            var saida = new Color32[px.Length];
            for (int y = 0; y < t.height; y++)
                Array.Copy(px, y * t.width, saida, (t.height - 1 - y) * t.width, t.width);
            return saida;
        }
    }
}
