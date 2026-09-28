using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IdleDataCenter.Ferramentas
{
    /// <summary>
    /// Transforma uma folha de "pixel art" gerada por IA (fundo magenta liso, objetos soltos) em sprites de verdade:
    /// tira o fundo, descobre o tamanho e o alinhamento da grade de pixels, reduz cada pixel do desenho a um pixel
    /// (pegando a cor do centro) e salva cada objeto num PNG recortado, na ordem de leitura (linhas de cima para baixo).
    ///
    /// Uso: Unity -batchmode -quit -executeMethod IdleDataCenter.Ferramentas.LimparFolhaDePixelArt.Executar
    ///      -folha=&lt;png&gt; -saida=&lt;pasta&gt; -nomes=torre,mesa,... [-escala=3.3]
    /// </summary>
    public static class LimparFolhaDePixelArt
    {
        struct Caixa { public int x0, y0, x1, y1; public int Largura => x1 - x0 + 1; public int Altura => y1 - y0 + 1; }

        public static void Executar()
        {
            string folha = null, saida = null, nomes = null;
            float escalaFixa = 0;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("-folha=")) folha = a.Substring(7);
                if (a.StartsWith("-saida=")) saida = a.Substring(7);
                if (a.StartsWith("-nomes=")) nomes = a.Substring(7);
                if (a.StartsWith("-escala=")) float.TryParse(a.Substring(8), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out escalaFixa);
            }
            if (folha == null || saida == null) { Debug.LogError("Uso: -folha=<png> -saida=<pasta> -nomes=a,b,c"); return; }
            Processar(folha, saida, nomes?.Split(',') ?? new string[0], escalaFixa);
        }

        public static void Processar(string folha, string saida, string[] nomes, float escalaFixa = 0)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(folha));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();   // origem embaixo; trabalho com y de cima para baixo
            Color32 P(int x, int y) => px[(h - 1 - y) * w + x];

            // 1. fundo: magenta (e o halo rosado nas bordas)
            var fundo = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) fundo[y * w + x] = EhFundo(P(x, y));

            // 2. objetos: componentes conectados, juntando pedaços próximos (rodinhas da cadeira, fios)
            var caixas = Componentes(fundo, w, h);
            caixas = Juntar(caixas, 14).Where(c => c.Largura * c.Altura > 400).ToList();
            // ordem de leitura: agrupa por linha (centros com altura parecida) e ordena por x
            caixas = caixas.OrderBy(c => c.y0).ToList();
            var linhas = new List<List<Caixa>>();
            foreach (var c in caixas)
            {
                var linha = linhas.FirstOrDefault(l => Math.Abs(Centro(l[0]) - Centro(c)) < Math.Max(l[0].Altura, c.Altura) * 0.6f);
                if (linha == null) linhas.Add(new List<Caixa> { c }); else linha.Add(c);
            }
            caixas = linhas.OrderBy(l => l.Average(Centro)).SelectMany(l => l.OrderBy(c => c.x0)).ToList();

            // 3. grade de pixels: tamanho e deslocamento que melhor reproduzem a imagem
            float escala = escalaFixa;
            float ox = 0, oy = 0;
            if (escala <= 0) (escala, ox, oy) = AcharGrade(P, fundo, w, h, caixas);
            else (_, ox, oy) = AcharGrade(P, fundo, w, h, caixas, escala);
            Debug.Log($"Folha {Path.GetFileName(folha)}: {caixas.Count} objetos, pixel = {escala:0.00} px, deslocamento ({ox:0.0}, {oy:0.0})");

            Directory.CreateDirectory(saida);
            for (int i = 0; i < caixas.Count; i++)
            {
                var c = caixas[i];
                // reduz: um pixel de saída por célula da grade, com a cor do centro da célula
                int gx0 = Mathf.FloorToInt((c.x0 - ox) / escala) - 1, gy0 = Mathf.FloorToInt((c.y0 - oy) / escala) - 1;
                int gx1 = Mathf.CeilToInt((c.x1 - ox) / escala) + 1, gy1 = Mathf.CeilToInt((c.y1 - oy) / escala) + 1;
                int sw = gx1 - gx0 + 1, sh = gy1 - gy0 + 1;
                var saidaPx = new Color32[sw * sh];
                for (int gy = 0; gy < sh; gy++)
                    for (int gx = 0; gx < sw; gx++)
                    {
                        int cx = Mathf.RoundToInt(ox + (gx0 + gx + 0.5f) * escala), cy = Mathf.RoundToInt(oy + (gy0 + gy + 0.5f) * escala);
                        var cor = new Color32(0, 0, 0, 0);
                        if (cx >= c.x0 - 2 && cx <= c.x1 + 2 && cy >= c.y0 - 2 && cy <= c.y1 + 2 && cx >= 0 && cy >= 0 && cx < w && cy < h && !fundo[cy * w + cx])
                            cor = Moda(P, fundo, w, h, cx, cy, Mathf.Max(1, Mathf.FloorToInt(escala / 3)));
                        saidaPx[(sh - 1 - gy) * sw + gx] = cor;
                    }
                TirarFranja(saidaPx, sw, sh);
                var recorte = Recortar(saidaPx, sw, sh, out int rw, out int rh);
                var s = new Texture2D(rw, rh, TextureFormat.RGBA32, false);
                s.SetPixels32(recorte);
                s.Apply();
                string nome = i < nomes.Length && nomes[i].Length > 0 ? nomes[i] : "objeto_" + (i + 1);
                File.WriteAllBytes(Path.Combine(saida, nome + ".png"), s.EncodeToPNG());
                Debug.Log($"  {nome}: {rw}x{rh}");
            }
        }

        static float Centro(Caixa c) => (c.y0 + c.y1) / 2f;

        static bool EhFundo(Color32 c)
        {
            // magenta puro e o halo que a IA deixa na borda (rosado: muito vermelho e azul, pouco verde)
            return c.r > 150 && c.b > 150 && c.g < 110 && Math.Abs(c.r - c.b) < 90;
        }

        /// <summary>Cor mais comum num quadradinho em volta do centro da célula (ignora ruído e halo).</summary>
        static Color32 Moda(Func<int, int, Color32> P, bool[] fundo, int w, int h, int cx, int cy, int raio)
        {
            var contagem = new Dictionary<int, (int n, Color32 cor)>();
            for (int y = cy - raio; y <= cy + raio; y++)
                for (int x = cx - raio; x <= cx + raio; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h || fundo[y * w + x]) continue;
                    var c = P(x, y);
                    int chave = (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3);   // agrupa cores quase iguais
                    contagem[chave] = contagem.TryGetValue(chave, out var v) ? (v.n + 1, v.cor) : (1, c);
                }
            if (contagem.Count == 0) return P(cx, cy);
            var cor = contagem.Values.OrderByDescending(v => v.n).First().cor;
            cor.a = 255;
            return cor;
        }

        static (float escala, float ox, float oy) AcharGrade(Func<int, int, Color32> P, bool[] fundo, int w, int h, List<Caixa> caixas, float fixa = 0)
        {
            // Mede quanto a imagem "reconstruída" (cada célula pintada com a cor do centro) difere da original,
            // numa amostra de pontos dentro dos objetos. A grade certa tem o menor erro.
            var rnd = new System.Random(1);
            var amostra = new List<(int x, int y)>();
            foreach (var c in caixas)
                for (int i = 0; i < 400; i++)
                {
                    int x = rnd.Next(c.x0, c.x1 + 1), y = rnd.Next(c.y0, c.y1 + 1);
                    if (!fundo[y * w + x]) amostra.Add((x, y));
                }
            float melhorErro = float.MaxValue, melhorE = fixa > 0 ? fixa : 3, melhorOx = 0, melhorOy = 0;
            var escalas = fixa > 0 ? new[] { fixa } : Enumerable.Range(0, 81).Select(i => 2.4f + i * 0.04f).ToArray();
            foreach (var e in escalas)
                for (float ox = 0; ox < e; ox += e / 6)
                    for (float oy = 0; oy < e; oy += e / 6)
                    {
                        double erro = 0;
                        foreach (var (x, y) in amostra)
                        {
                            int cx = Mathf.Clamp(Mathf.RoundToInt(ox + (Mathf.Floor((x - ox) / e) + 0.5f) * e), 0, w - 1);
                            int cy = Mathf.Clamp(Mathf.RoundToInt(oy + (Mathf.Floor((y - oy) / e) + 0.5f) * e), 0, h - 1);
                            var a = P(x, y); var b = P(cx, cy);
                            erro += Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
                        }
                        erro /= Math.Max(1, amostra.Count);
                        if (erro < melhorErro) { melhorErro = (float)erro; melhorE = e; melhorOx = ox; melhorOy = oy; }
                    }
            Debug.Log($"Grade: erro médio {melhorErro:0.0} por pixel");
            return (melhorE, melhorOx, melhorOy);
        }

        static List<Caixa> Componentes(bool[] fundo, int w, int h)
        {
            var visto = new bool[w * h];
            var lista = new List<Caixa>();
            var pilha = new Stack<int>();
            for (int i = 0; i < w * h; i++)
            {
                if (fundo[i] || visto[i]) continue;
                var c = new Caixa { x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1 };
                int n = 0;
                pilha.Push(i); visto[i] = true;
                while (pilha.Count > 0)
                {
                    int p = pilha.Pop(), x = p % w, y = p / w; n++;
                    c.x0 = Math.Min(c.x0, x); c.y0 = Math.Min(c.y0, y); c.x1 = Math.Max(c.x1, x); c.y1 = Math.Max(c.y1, y);
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int q = ny * w + nx;
                        if (fundo[q] || visto[q]) continue;
                        visto[q] = true; pilha.Push(q);
                    }
                }
                if (n >= 6) lista.Add(c);   // pontinhos soltos (ruído) ficam de fora
            }
            return lista;
        }

        static List<Caixa> Juntar(List<Caixa> caixas, int distancia)
        {
            bool mudou = true;
            while (mudou)
            {
                mudou = false;
                for (int i = 0; i < caixas.Count && !mudou; i++)
                    for (int j = i + 1; j < caixas.Count && !mudou; j++)
                    {
                        var a = caixas[i]; var b = caixas[j];
                        if (a.x0 - distancia > b.x1 || b.x0 - distancia > a.x1 || a.y0 - distancia > b.y1 || b.y0 - distancia > a.y1) continue;
                        caixas[i] = new Caixa { x0 = Math.Min(a.x0, b.x0), y0 = Math.Min(a.y0, b.y0), x1 = Math.Max(a.x1, b.x1), y1 = Math.Max(a.y1, b.y1) };
                        caixas.RemoveAt(j);
                        mudou = true;
                    }
            }
            return caixas;
        }

        /// <summary>
        /// Tira a franja arroxeada que a IA mistura com o magenta na borda: pixels da borda (vizinhos de
        /// transparência) com cara de magenta escurecido viram transparentes. Duas passadas pegam bordas de 2 pixels.
        /// </summary>
        static void TirarFranja(Color32[] px, int w, int h)
        {
            for (int passada = 0; passada < 2; passada++)
            {
                var remover = new List<int>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var c = px[y * w + x];
                        if (c.a == 0) continue;
                        bool borda = x == 0 || y == 0 || x == w - 1 || y == h - 1
                                     || px[y * w + x - 1].a == 0 || px[y * w + x + 1].a == 0 || px[(y - 1) * w + x].a == 0 || px[(y + 1) * w + x].a == 0;
                        bool arroxeado = c.r > c.g + 35 && c.b > c.g + 35 && Math.Abs(c.r - c.b) < 70;
                        if (borda && arroxeado) remover.Add(y * w + x);
                    }
                foreach (int i in remover) px[i] = new Color32(0, 0, 0, 0);
            }
        }

        static Color32[] Recortar(Color32[] px, int w, int h, out int rw, out int rh)
        {
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 0) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            if (x1 < 0) { rw = rh = 1; return new[] { new Color32(0, 0, 0, 0) }; }
            rw = x1 - x0 + 1; rh = y1 - y0 + 1;
            var r = new Color32[rw * rh];
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++) r[y * rw + x] = px[(y0 + y) * w + x0 + x];
            return r;
        }
    }
}
