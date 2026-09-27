using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Desenho isométrico em pixel art sobre uma PixelCanvas: projeção 2:1 (piso de 32×16 por quadrado),
    /// losangos de piso, paredes e "caixas" (cuboides) com detalhes nas faces. Tudo o que aparece na sala
    /// do modo gerente é montado com essas peças, então fica no mesmo estilo e na mesma grade de pixels.
    ///
    /// Coordenadas da grade: gx cresce para a direita-baixo na tela, gy para a esquerda-baixo; z é altura em pixels.
    /// Numa caixa em (gx, gy) com tamanho w × d, a face "esquerda" visível é a de gy + d (virada para o corredor
    /// da frente) e a face "direita" é a de gx + w.
    /// </summary>
    public class IsoDesenho
    {
        public const int TW = 32, TH = 16;

        public PixelCanvas Tela { get; }
        public int Ox, Oy;

        public IsoDesenho(PixelCanvas tela) { Tela = tela; }

        public Vector2Int P(float gx, float gy, float z = 0) =>
            new Vector2Int(Mathf.RoundToInt(Ox + (gx - gy) * TW / 2f), Mathf.RoundToInt(Oy + (gx + gy) * TH / 2f - z));

        public static Color32 C(string hex) => PixelCanvas.C(hex.StartsWith("#") ? hex : "#" + hex);

        public static Color32 Escurecer(Color32 c, float f) =>
            new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);

        // ---------------- Primitivas ----------------

        /// <summary>Preenche um polígono convexo (linha a linha).</summary>
        public void Poligono(Color32 cor, params Vector2Int[] p)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var v in p) { min = Mathf.Min(min, v.y); max = Mathf.Max(max, v.y); }
            for (int y = min; y <= max; y++)
            {
                float esq = float.MaxValue, dir = float.MinValue;
                for (int i = 0; i < p.Length; i++)
                {
                    var a = p[i]; var b = p[(i + 1) % p.Length];
                    if (y < Mathf.Min(a.y, b.y) || y > Mathf.Max(a.y, b.y)) continue;
                    if (a.y == b.y) { esq = Mathf.Min(esq, Mathf.Min(a.x, b.x)); dir = Mathf.Max(dir, Mathf.Max(a.x, b.x)); continue; }
                    float x = a.x + (float)(y - a.y) * (b.x - a.x) / (b.y - a.y);
                    esq = Mathf.Min(esq, x); dir = Mathf.Max(dir, x);
                }
                if (dir >= esq) Tela.Ret(Mathf.RoundToInt(esq), y, Mathf.RoundToInt(dir) - Mathf.RoundToInt(esq) + 1, 1, cor);
            }
        }

        public void Linha(Vector2Int a, Vector2Int b, Color32 cor)
        {
            int x = a.x, y = a.y, dx = Mathf.Abs(b.x - a.x), sx = a.x < b.x ? 1 : -1, dy = -Mathf.Abs(b.y - a.y), sy = a.y < b.y ? 1 : -1;
            int erro = dx + dy;
            while (true)
            {
                Tela.Pixel(x, y, cor);
                if (x == b.x && y == b.y) break;
                int e = 2 * erro;
                if (e >= dy) { erro += dy; x += sx; }
                if (e <= dx) { erro += dx; y += sy; }
            }
        }

        /// <summary>Losango de piso cobrindo w × d quadrados a partir de (gx, gy), na altura z.</summary>
        public void Piso(float gx, float gy, float w, float d, Color32 cor, float z = 0) =>
            Poligono(cor, P(gx, gy, z), P(gx + w, gy, z), P(gx + w, gy + d, z), P(gx, gy + d, z));

        /// <summary>Cuboide com topo e as duas faces visíveis, contornado de escuro.</summary>
        public void Caixa(float gx, float gy, float w, float d, int h, Color32 topo, Color32 esquerda, Color32 direita, float z = 0)
        {
            var a = P(gx, gy, z + h); var b = P(gx + w, gy, z + h); var c = P(gx + w, gy + d, z + h); var dd = P(gx, gy + d, z + h);
            var bb = P(gx + w, gy, z); var cc = P(gx + w, gy + d, z); var d0 = P(gx, gy + d, z);
            Poligono(esquerda, dd, c, cc, d0);
            Poligono(direita, c, b, bb, cc);
            Poligono(topo, a, b, c, dd);
            var contorno = C("1b1a2e");
            Linha(a, b, contorno); Linha(b, bb, contorno); Linha(bb, cc, contorno); Linha(cc, d0, contorno);
            Linha(d0, dd, contorno); Linha(dd, a, contorno);
            // aresta da frente, levemente mais clara (dá volume)
            Linha(c, cc, Escurecer(topo, 0.9f));
        }

        /// <summary>Ponto na face esquerda (a de gy + d): u de 0 a 1 ao longo da largura, z em pixels acima de "z0".</summary>
        public Vector2Int FaceEsq(float gx, float gy, float w, float d, float u, float z) => P(gx + u * w, gy + d, z);

        /// <summary>Ponto na face direita (a de gx + w): v de 0 a 1 ao longo da profundidade.</summary>
        public Vector2Int FaceDir(float gx, float gy, float w, float d, float v, float z) => P(gx + w, gy + v * d, z);

        /// <summary>Faixa inclinada na face esquerda (linhas de servidor, grades de ventilação).</summary>
        public void FaixaEsq(float gx, float gy, float w, float d, float u0, float u1, float z, Color32 cor) =>
            Linha(FaceEsq(gx, gy, w, d, u0, z), FaceEsq(gx, gy, w, d, u1, z), cor);

        public void FaixaDir(float gx, float gy, float w, float d, float v0, float v1, float z, Color32 cor) =>
            Linha(FaceDir(gx, gy, w, d, v0, z), FaceDir(gx, gy, w, d, v1, z), cor);

        public void Ponto(Vector2Int p, Color32 cor, int tamanho = 1) => Tela.Ret(p.x, p.y, tamanho, tamanho, cor);

        // ---------------- Paredes ----------------

        /// <summary>Paredes do fundo: a da direita corre ao longo de gy = 0, a da esquerda ao longo de gx = 0.</summary>
        public void Paredes(int largura, int profundidade, int altura, Color32 cor, Color32 cor2, Color32 rodape, Color32 topo)
        {
            // parede de trás à direita (ao longo de gx)
            for (int i = 0; i < largura; i++)
                Poligono(i % 2 == 0 ? cor : cor2, P(i, 0, 0), P(i + 1, 0, 0), P(i + 1, 0, altura), P(i, 0, altura));
            // parede de trás à esquerda (ao longo de gy), um pouco mais escura
            for (int i = 0; i < profundidade; i++)
                Poligono(Escurecer(i % 2 == 0 ? cor : cor2, 0.85f), P(0, i, 0), P(0, i + 1, 0), P(0, i + 1, altura), P(0, i, altura));
            // rodapé e topo das paredes
            Linha(P(0, 0, 1), P(largura, 0, 1), rodape); Linha(P(0, 0, 2), P(largura, 0, 2), rodape);
            Linha(P(0, 0, 1), P(0, profundidade, 1), rodape); Linha(P(0, 0, 2), P(0, profundidade, 2), rodape);
            Linha(P(0, 0, altura), P(largura, 0, altura), topo);
            Linha(P(0, 0, altura), P(0, profundidade, altura), topo);
            Linha(P(0, 0, 0), P(0, 0, altura), C("1b1a2e"));
        }

        /// <summary>Retângulo "pintado" na parede de trás à direita (quadro, telão): de gx0 a gx1, de z0 a z1.</summary>
        public void NaParedeDir(float gx0, float gx1, float z0, float z1, Color32 cor) =>
            Poligono(cor, P(gx0, 0.02f, z0), P(gx1, 0.02f, z0), P(gx1, 0.02f, z1), P(gx0, 0.02f, z1));

        public void NaParedeEsq(float gy0, float gy1, float z0, float z1, Color32 cor) =>
            Poligono(cor, P(0.02f, gy0, z0), P(0.02f, gy1, z0), P(0.02f, gy1, z1), P(0.02f, gy0, z1));
    }
}
