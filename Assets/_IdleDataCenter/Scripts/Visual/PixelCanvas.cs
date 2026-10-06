using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Textura onde se desenha pixel art "na mão", quadro a quadro (retângulos, pixels, sprites em mapa
    /// de caracteres e texto na fonte de pixel). Coordenadas com origem no canto de CIMA à esquerda,
    /// como num canvas de navegador. Chame Aplicar() no fim de cada desenho.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Largura, Altura;
        public Texture2D Textura { get; }
        public Sprite Sprite { get; }

        readonly Color32[] px;
        RectInt recorte;

        static readonly Dictionary<string, Color32> cores = new Dictionary<string, Color32>();

        public PixelCanvas(int largura, int altura)
        {
            Largura = largura;
            Altura = altura;
            px = new Color32[largura * altura];
            Textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            Sprite = Sprite.Create(Textura, new Rect(0, 0, largura, altura), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            SemRecorte();
        }

        /// <summary>Cor a partir de "#rrggbb" ou "#rrggbbaa" (com cache).</summary>
        public static Color32 C(string hex)
        {
            if (!cores.TryGetValue(hex, out var c))
            {
                ColorUtility.TryParseHtmlString(hex, out var cor);
                cores[hex] = c = cor;
            }
            return c;
        }

        /// <summary>Limita o desenho a um retângulo (útil para cenas que não podem vazar).</summary>
        public void Recortar(int x, int y, int w, int h) => recorte = new RectInt(x, y, w, h);
        public void SemRecorte() => recorte = new RectInt(0, 0, Largura, Altura);

        public void Limpar(Color32 c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        public void Pixel(int x, int y, Color32 c)
        {
            if (x < recorte.xMin || y < recorte.yMin || x >= recorte.xMax || y >= recorte.yMax) return;
            int i = (Altura - 1 - y) * Largura + x;
            if (c.a == 255) { px[i] = c; return; }
            if (c.a == 0) return;
            // mistura alfa sobre o que já está desenhado
            var d = px[i];
            float a = c.a / 255f;
            px[i] = new Color32((byte)(c.r * a + d.r * (1 - a)), (byte)(c.g * a + d.g * (1 - a)), (byte)(c.b * a + d.b * (1 - a)), 255);
        }

        public void Ret(int x, int y, int w, int h, Color32 c)
        {
            int x0 = Mathf.Max(x, recorte.xMin), y0 = Mathf.Max(y, recorte.yMin);
            int x1 = Mathf.Min(x + w, recorte.xMax), y1 = Mathf.Min(y + h, recorte.yMax);
            for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
                Pixel(xx, yy, c);
        }

        public void Circulo(int cx, int cy, int raio, Color32 c)
        {
            for (int dy = -raio; dy <= raio; dy++)
            for (int dx = -raio; dx <= raio; dx++)
                if (dx * dx + dy * dy <= raio * raio) Pixel(cx + dx, cy + dy, c);
        }

        /// <summary>Copia uma imagem (pixels de cima para baixo, w × h) para a posição (x, y), respeitando a transparência.</summary>
        public void Imagem(Color32[] pixels, int w, int h, int x, int y)
        {
            for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                Pixel(x + i, y + j, pixels[j * w + i]);
        }

        /// <summary>Sprite em mapa de caracteres (cores de PixelArt.Paleta; '.' é transparente).</summary>
        public void Mapa(string[] linhas, int x, int y, bool espelhar = false)
        {
            for (int j = 0; j < linhas.Length; j++)
            {
                string l = linhas[j];
                for (int i = 0; i < l.Length; i++)
                    if (PixelArt.Paleta.TryGetValue(l[i], out var c))
                        Pixel(espelhar ? x + l.Length - 1 - i : x + i, y + j, c);
            }
        }

        /// <summary>Texto na fonte 3×5, com sombra opcional um pixel abaixo e à direita.</summary>
        public void Texto(string s, int x, int y, Color32 cor, bool sombra = true, int escala = 1)
        {
            s = PixelTexto.Normalizar(Idiomas.T(s));
            if (sombra) DesenharTexto(s, x + escala, y + escala, new Color32(27, 26, 46, cor.a), escala);
            DesenharTexto(s, x, y, cor, escala);
        }

        void DesenharTexto(string s, int x, int y, Color32 cor, int escala)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!PixelTexto.Glifos.TryGetValue(s[i], out var g)) continue;
                for (int r = 0; r < 5; r++)
                for (int q = 0; q < 3; q++)
                    if (g[r * 3 + q] == '#') Ret(x + (i * 4 + q) * escala, y + r * escala, escala, escala, cor);
            }
        }

        public static int LarguraTexto(string s, int escala = 1) => PixelTexto.Largura(s) * escala;

        public void Aplicar()
        {
            Textura.SetPixels32(px);
            Textura.Apply(false);
        }
    }
}
