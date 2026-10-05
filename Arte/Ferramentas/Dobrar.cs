using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Dobra pixel art (2x) sem engrossar as linhas: Scale2x para as formas, e as linhas finas (1 px) da imagem original
// são redesenhadas com 1 px na imagem nova (isométricas 2:1, horizontais e verticais). Xadrez (dither) vira xadrez fino.
// Retângulos 'manter' (coordenadas da original) ficam só com o Scale2x (móveis e objetos da ilustração).
public static class Dobrar
{
    public static int[] Ler(string arq, out int w, out int h)
    {
        var b = new Bitmap(arq);
        w = b.Width; h = b.Height;
        var r = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var px = new int[w * h];
        Marshal.Copy(r.Scan0, px, 0, px.Length);
        b.UnlockBits(r); b.Dispose();
        return px;
    }

    public static void Gravar(string arq, int[] px, int w, int h)
    {
        var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var r = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, r.Scan0, px.Length);
        b.UnlockBits(r);
        b.Save(arq, ImageFormat.Png); b.Dispose();
    }

    static int Dist(int a, int b)
    {
        int aa = (a >> 24) & 255, ab = (b >> 24) & 255;
        if ((aa < 128) != (ab < 128)) return 999;
        if (aa < 128) return 0;
        return Math.Abs(((a >> 16) & 255) - ((b >> 16) & 255)) + Math.Abs(((a >> 8) & 255) - ((b >> 8) & 255)) + Math.Abs((a & 255) - (b & 255));
    }

    public static int[] Scale2x(int[] p, int w, int h)
    {
        var o = new int[w * 2 * h * 2];
        int W = w * 2;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int P = p[y * w + x];
                int A = y > 0 ? p[(y - 1) * w + x] : P, D = y < h - 1 ? p[(y + 1) * w + x] : P;
                int C = x > 0 ? p[y * w + x - 1] : P, B = x < w - 1 ? p[y * w + x + 1] : P;
                int e0 = P, e1 = P, e2 = P, e3 = P;
                if (C == A && C != D && A != B) e0 = A;
                if (A == B && A != C && B != D) e1 = B;
                if (D == C && D != B && C != A) e2 = C;
                if (B == D && B != A && D != C) e3 = D;
                o[(2 * y) * W + 2 * x] = e0; o[(2 * y) * W + 2 * x + 1] = e1;
                o[(2 * y + 1) * W + 2 * x] = e2; o[(2 * y + 1) * W + 2 * x + 1] = e3;
            }
        return o;
    }

    // famílias de linha: 0 = 2:1 descendo para a direita, 1 = 2:1 descendo para a esquerda, 2 = horizontal, 3 = vertical
    static int Chave(int f, int x, int y)
    {
        switch (f)
        {
            case 0: return (int)Math.Floor((2 * y - x + 1) / 2.0);
            case 1: return (int)Math.Floor((2 * y + x) / 2.0);
            case 2: return y;
            default: return x;
        }
    }

    static int Longo(int f) { return f == 3 ? 4 : 6; }

    public static int[] Dobro(int[] p, int w, int h, int[] manter, int limiar)
    {
        int W = w * 2, H = h * 2;
        var o = Scale2x(p, w, h);
        Func<int, int, bool> Mantido = (x, y) =>
        {
            for (int i = 0; i + 3 < manter.Length; i += 4)
                if (x >= manter[i] && y >= manter[i + 1] && x < manter[i] + manter[i + 2] && y < manter[i + 1] + manter[i + 3]) return true;
            return false;
        };
        Func<int, int, int> Px = (x, y) => p[Math.Max(0, Math.Min(h - 1, y)) * w + Math.Max(0, Math.Min(w - 1, x))];

        // pixels finos: diferentes dos dois vizinhos de cima/baixo (fino na vertical) ou dos dois do lado (fino na horizontal)
        var finoV = new bool[w * h]; var finoH = new bool[w * h];
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int c = p[y * w + x];
                if (((c >> 24) & 255) < 128 || Mantido(x, y)) continue;
                finoV[y * w + x] = Dist(c, Px(x, y - 1)) > limiar && Dist(c, Px(x, y + 1)) > limiar;
                finoH[y * w + x] = Dist(c, Px(x - 1, y)) > limiar && Dist(c, Px(x + 1, y)) > limiar;
            }

        // xadrez: o pixel é igual aos diagonais e diferente dos quatro vizinhos (que são iguais entre si)
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                if (Mantido(x, y)) continue;
                int c = p[y * w + x], q = Px(x + 1, y);
                if (c != q && Px(x - 1, y) == q && Px(x, y - 1) == q && Px(x, y + 1) == q && Px(x + 1, y + 1) == c && Px(x - 1, y - 1) == c)
                {
                    finoV[y * w + x] = finoH[y * w + x] = false;
                    for (int dy = 0; dy < 2; dy++)
                        for (int dx = 0; dx < 2; dx++)
                            o[(2 * y + dy) * W + 2 * x + dx] = ((dx + dy) & 1) == 0 ? c : q;
                }
            }

        // agrupa os finos em linhas retas por família e mede o trecho contínuo de cada pixel
        var familia = new int[w * h];
        var comprimento = new int[w * h];
        for (int i = 0; i < familia.Length; i++) familia[i] = -1;
        for (int f = 0; f < 4; f++)
        {
            var grupos = new Dictionary<long, List<int>>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    bool fino = f == 3 ? finoH[i] : finoV[i];
                    if (!fino) continue;
                    long k = Chave(f, x, y);   // mesma reta (cada pixel guarda a sua cor)
                    List<int> l;
                    if (!grupos.TryGetValue(k, out l)) grupos[k] = l = new List<int>();
                    l.Add(i);
                }
            foreach (var l in grupos.Values)
            {
                // ordena pela posição ao longo da reta e quebra nos buracos
                l.Sort((a, b) => f == 3 ? (a / w).CompareTo(b / w) : (a % w).CompareTo(b % w));
                int ini = 0;
                for (int j = 1; j <= l.Count; j++)
                {
                    bool quebra = j == l.Count;
                    if (!quebra)
                    {
                        int pa = f == 3 ? l[j - 1] / w : l[j - 1] % w, pb = f == 3 ? l[j] / w : l[j] % w;
                        quebra = pb - pa > 1;
                    }
                    if (!quebra) continue;
                    int n = j - ini;
                    for (int m = ini; m < j; m++)
                        if (n > comprimento[l[m]]) { comprimento[l[m]] = n; familia[l[m]] = f; }
                    ini = j;
                }
            }
        }

        // redesenha: no bloco 2x2 de cada pixel de linha, só o que cai na reta fina fica com a cor da linha; o resto pega
        // a cor do vizinho do mesmo lado da reta
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x, f = familia[i];
                if (f < 0 || comprimento[i] < Longo(f)) continue;
                int c = p[i];
                int k = Chave(f, x, y);
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int X = 2 * x + dx, Y = 2 * y + dy;
                        bool naReta; int lado;   // lado: -1 antes (cima/esquerda), +1 depois
                        switch (f)
                        {
                            case 0:
                            {
                                // centro da reta original: 2y - x = 2k (centros) -> na dobrada 2Y - X = 4k
                                int yr = (int)Math.Floor((4 * k + X + 0.5) / 2.0);
                                naReta = Y == yr; lado = Y < yr ? -1 : 1; break;
                            }
                            case 1:
                            {
                                int yr = (int)Math.Floor((4 * k + 4 - (X + 0.5)) / 2.0);
                                naReta = Y == yr; lado = Y < yr ? -1 : 1; break;
                            }
                            case 2: naReta = dy == 0; lado = 1; break;
                            default: naReta = dx == 0; lado = 1; break;
                        }
                        int cor = c;
                        if (!naReta)
                        {
                            if (f == 3) cor = lado < 0 ? Px(x - 1, y) : Px(x + 1, y);
                            else cor = lado < 0 ? Px(x, y - 1) : Px(x, y + 1);
                        }
                        o[Y * W + X] = cor;
                    }
            }
        return o;
    }

    /// Recorta no retângulo dos pixels opacos (alfa binário: abaixo de 128 vira transparente), espelhando se pedir.
    public static int[] Recortar(int[] p, int w, int h, bool espelhar, out int W, out int H)
    {
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (((p[y * w + x] >> 24) & 255) >= 128) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        W = x1 - x0 + 1; H = y1 - y0 + 1;
        var o = new int[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int c = p[(y0 + y) * w + x0 + (espelhar ? W - 1 - x : x)];
                o[y * W + x] = ((c >> 24) & 255) >= 128 ? (c | unchecked((int)0xFF000000)) : 0;
            }
        return o;
    }

    /// Reduz pela cor mais frequente de cada bloco (alfa binário; LED verde vivo não some; empate fica com o mais escuro).
    public static int[] Reduzir(int[] p, int w, int h, double fator, out int W, out int H)
    {
        W = (int)Math.Round(w * fator); H = (int)Math.Round(h * fator);
        var o = new int[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int x0 = (int)Math.Floor(x / fator), x1 = Math.Min(w - 1, (int)Math.Ceiling((x + 1) / fator) - 1);
                int y0 = (int)Math.Floor(y / fator), y1 = Math.Min(h - 1, (int)Math.Ceiling((y + 1) / fator) - 1);
                var cont = new Dictionary<int, int>();
                int opacos = 0, total = 0, led = 0; bool temLed = false;
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++)
                    {
                        int c = p[yy * w + xx]; total++;
                        if (((c >> 24) & 255) < 128) continue;
                        opacos++;
                        int n; cont.TryGetValue(c, out n); cont[c] = n + 1;
                        int r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255;
                        if (g > 110 && g > r + 40 && g > b + 40) { temLed = true; led = c; }
                    }
                if (opacos * 2 < total || cont.Count == 0) { o[y * W + x] = 0; continue; }
                if (temLed) { o[y * W + x] = led; continue; }
                int melhor = 0, mn = -1, ml = 999;
                foreach (var kv in cont)
                {
                    int c = kv.Key, l = ((c >> 16) & 255) + ((c >> 8) & 255) + (c & 255);
                    if (kv.Value > mn || (kv.Value == mn && l < ml)) { mn = kv.Value; ml = l; melhor = c; }
                }
                o[y * W + x] = melhor;
            }
        return o;
    }
}
