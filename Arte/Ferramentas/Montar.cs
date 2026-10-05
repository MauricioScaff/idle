using System;
using System.Collections.Generic;

// Monta as salas na escala da pessoa (80 px): paredes, piso, laje e portas desenhados aqui, pixel a pixel, com as
// cores medidas nas ilustrações do PixelLab; os móveis (mesa, quadro, planta, NOC...) são recortados da ilustração e
// ampliados para a mesma escala. Casa = losango de 64x32 px (vetores gx = (32,16), gy = (-32,16)).
public static class Montar
{
    public const int CW = 32, CH = 16;

    public class Porta { public char parede; public double u0, u1; public int altura; public int moldura, painel, luz, macaneta; public bool dupla; }
    public class Movel { public bool soMaior; public double pedacoMinimo, gx, gy;   // gx, gy: peça avulsa (sem recorte), com a base no ponto do piso
        public string nome, origem, arquivo; public int x, y, w, h; public char parede; public double u; public double escala; public int[] trocas; }   // trocas: pares de cores (de, para)

    /// Uma ilustração de onde se recortam móveis: os pixels, o losango do piso (fundo, esquerda, direita) e as cores do
    /// fundo (paredes e piso), que não fazem parte dos móveis.
    public class Base { public string nome; public int[] px; public int w, h; public double ofx, ofy, oex, oey, odx, ody; public int[] fundo; }
    public static readonly Dictionary<string, Base> Bases = new Dictionary<string, Base>();
    /// Onde salvar os recortes dos móveis (referência para refazer no PixelLab); null: não salva.
    public static string PastaDosMoveis;
    /// Pasta das imagens prontas de móveis (Movel.arquivo), que substituem o recorte.
    public static string PastaDasImagens;

    public class Sala
    {
        public string nome; public int n, altura, capa, laje;
        public double espessura = 0.2;   // da parede, em casas
        public int contorno, capaCor, capaLuz, paredeEsq, paredeDir, pontaEsq, pontaDir, lajeEsq, lajeDir, bordaLuz;
        public string piso; public int[] pisoCores;
        public int faixa = 0, faixaSombra = 0; public double faixaAltura;
        public List<Porta> portas = new List<Porta>();
        public List<Movel> moveis = new List<Movel>();
        // resultado
        public int W, H, Bx, By;
    }

    // ids das superfícies (prioridade de desenho = ordem)
    const int Vazio = 0, ParedeDir = 1, ParedeEsq = 2, Piso = 3, PontaEsq = 4, PontaDir = 5, LajeEsq = 6, LajeDir = 7, CapaDir = 8, CapaEsq = 9, PortaId = 10;
    static int Prioridade(int id)
    {
        switch (id) { case ParedeDir: case ParedeEsq: return 1; case PortaId: return 2; case Piso: return 3; case PontaEsq: case PontaDir: return 4; case LajeEsq: case LajeDir: return 5; case CapaDir: case CapaEsq: return 6; default: return 0; }
    }

    static uint Hash(int a, int b, int c)
    {
        uint h = (uint)(a * 374761393 + b * 668265263 + c * 2147483647);
        h = (h ^ (h >> 13)) * 1274126177;
        return h ^ (h >> 16);
    }

    static int Mistura(int a, int b, double k)
    {
        int r = (int)Math.Round(((a >> 16) & 255) * (1 - k) + ((b >> 16) & 255) * k);
        int g = (int)Math.Round(((a >> 8) & 255) * (1 - k) + ((b >> 8) & 255) * k);
        int bl = (int)Math.Round((a & 255) * (1 - k) + (b & 255) * k);
        return unchecked((int)0xFF000000) | (r << 16) | (g << 8) | bl;
    }

    public static int[] Desenhar(Sala s)
    {
        int n = s.n;
        double w = s.espessura;
        int folga = 12;
        s.W = 2 * n * CW + 2 * (int)Math.Ceiling(w * CW) + 2 * folga;
        s.H = n * 2 * CH + s.altura + s.capa + s.laje + (int)Math.Ceiling(w * 2 * CH) + 2 * folga;
        s.Bx = s.W / 2;
        s.By = folga + s.altura + (int)Math.Ceiling(w * 2 * CH) + 2;
        int W = s.W, H = s.H, Bx = s.Bx, By = s.By;
        var id = new int[W * H];
        var px = new int[W * H];

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double dx = x + 0.5 - Bx, dy = y + 0.5 - By;
                int melhor = Vazio, cor = 0;
                Action<int, int> Por = (i, c) => { if (Prioridade(i) >= Prioridade(melhor)) { melhor = i; cor = c; } };

                // parede da direita (plano gy = 0)
                {
                    double gx = dx / CW, z = (By + CH * gx) - (y + 0.5);
                    if (gx >= 0 && gx <= n && z >= 0 && z <= s.altura) Por(ParedeDir, CorParede(s, false, gx, z));
                }
                // parede da esquerda (plano gx = 0)
                {
                    double gy = -dx / CW, z = (By + CH * gy) - (y + 0.5);
                    if (gy >= 0 && gy <= n && z >= 0 && z <= s.altura) Por(ParedeEsq, CorParede(s, true, gy, z));
                }
                // piso
                {
                    double gx = (dx / CW + dy / CH) / 2, gy = (dy / CH - dx / CW) / 2;
                    if (gx >= 0 && gx <= n && gy >= 0 && gy <= n) Por(Piso, CorPiso(s, x, y, gx, gy));
                }
                // capas (topo das paredes, em z = altura)
                {
                    double ty = dy + s.altura;
                    double gx = (dx / CW + ty / CH) / 2, gy = (ty / CH - dx / CW) / 2;
                    if (gx >= -w && gx <= n && gy >= -w && gy <= 0) Por(CapaDir, s.capaCor);
                    if (gy >= -w && gy <= n && gx >= -w && gx <= 0) Por(CapaEsq, s.capaCor);
                }
                // frente da esquerda (plano gy = n): ponta da parede (gx < 0) e laje (gx >= 0)
                {
                    double gx = dx / CW + n, z = (By + CH * gx + CH * n) - (y + 0.5);
                    if (gx >= -w && gx <= n && z >= -s.laje && z <= (gx < 0 ? s.altura : 0))
                        Por(gx < 0 && z > 0 ? PontaEsq : LajeEsq, gx < 0 && z > 0 ? s.pontaEsq : s.lajeEsq);
                }
                // frente da direita (plano gx = n): ponta da parede (gy < 0) e laje
                {
                    double gy = n - dx / CW, z = (By + CH * n + CH * gy) - (y + 0.5);
                    if (gy >= -w && gy <= n && z >= -s.laje && z <= (gy < 0 ? s.altura : 0))
                        Por(gy < 0 && z > 0 ? PontaDir : LajeDir, gy < 0 && z > 0 ? s.pontaDir : s.lajeDir);
                }
                // portas
                foreach (var p in s.portas)
                {
                    double u = p.parede == 'D' ? dx / CW : -dx / CW;
                    double z = (By + CH * u) - (y + 0.5);
                    if (u >= p.u0 && u <= p.u1 && z >= 0 && z <= p.altura) Por(PortaId, CorPorta(p, u, z));
                }
                id[y * W + x] = melhor; px[y * W + x] = cor;
            }

        // contornos: a borda entre duas superfícies fica escura do lado da que está atrás; a silhueta também
        var saida = (int[])px.Clone();
        var escuro = new bool[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x, a = id[i];
                if (a == Vazio) continue;
                bool linha = false;
                for (int k = 0; k < 4 && !linha; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    int b = nx < 0 || ny < 0 || nx >= W || ny >= H ? Vazio : id[ny * W + nx];
                    if (b == a) continue;
                    if (b == Vazio) linha = true;
                    else if (a == PortaId || b == PortaId) linha = false;   // a porta tem a moldura dela
                    else if (Prioridade(b) > Prioridade(a)) linha = true;
                    else if (Prioridade(b) == Prioridade(a) && a == ParedeDir) linha = true;   // canto entre as paredes
                }
                if (linha) { saida[i] = s.contorno; escuro[i] = true; }
            }
        // brilho: a fileira da capa logo acima da linha da parede e a borda da frente do piso, acima da linha da laje
        for (int y = 0; y < H - 1; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x, abaixo = (y + 1) * W + x;
                if (escuro[i]) continue;
                if ((id[i] == CapaDir || id[i] == CapaEsq) && escuro[abaixo] && (id[abaixo] == ParedeDir || id[abaixo] == ParedeEsq || id[abaixo] == PortaId)) saida[i] = s.capaLuz;
                if (id[i] == Piso && escuro[abaixo] && (id[abaixo] == Piso) && y + 2 < H && (id[(y + 2) * W + x] == LajeEsq || id[(y + 2) * W + x] == LajeDir)) saida[i] = s.bordaLuz;
            }

        // móveis recortados da ilustração
        foreach (var m in s.moveis) ColarMovel(s, m, m.w == 0 ? null : Bases[m.origem ?? s.nome], saida, id, W, H);
        return saida;
    }

    static int CorParede(Sala s, bool esquerda, double u, double z)
    {
        int c = esquerda ? s.paredeEsq : s.paredeDir;
        if (s.faixa != 0)
        {
            double zf = s.altura * s.faixaAltura;
            if (z >= zf && z < zf + 3) return esquerda ? s.faixa : s.faixaSombra;
        }
        return c;
    }

    static int CorPorta(Porta p, double u, double z)
    {
        double largura = (p.u1 - p.u0) * CW;   // em px na horizontal
        double ax = (u - p.u0) * CW, bx = (p.u1 - u) * CW;
        int moldura = 3;
        if (ax < 1 || bx < 1 || z < 1 || z > p.altura - 1) return Mistura(p.moldura, 0x000000, 0.6);
        if (ax < moldura || bx < moldura || z > p.altura - moldura) return p.moldura;
        if (p.dupla && Math.Abs(ax - largura / 2) < 1) return Mistura(p.moldura, 0x000000, 0.4);
        // painel: claro em cima (reflexo no vidro da porta dupla), maçaneta do lado de dentro
        double meio = p.dupla ? largura / 2 : largura;
        double mx = p.dupla ? Math.Abs(ax - largura / 2) : bx;
        if (mx >= 4 && mx < 7 && Math.Abs(z - p.altura * 0.48) < 2) return p.macaneta;
        if (p.dupla && z > p.altura * 0.55 && z < p.altura - moldura - 3 && ((int)(ax + z * 0.5) % 9 == 0)) return p.luz;
        if (!p.dupla && z > p.altura - moldura - 2) return p.luz;
        return p.painel;
    }

    static int CorPiso(Sala s, int x, int y, double gx, double gy)
    {
        var c = s.pisoCores;
        int q = (x - s.Bx) + 2 * (y - s.By) + 2;   // gx*64 (linhas de gx inteiro: q % 64 em {0,1})
        int r = 2 * (y - s.By) - (x - s.Bx) + 1;   // gy*64
        Func<int, int, int> Mod = (a, m) => ((a % m) + m) % m;
        switch (s.piso)
        {
            case "ladrilho":
            {
                // c: base, base2, mancha, rejunte, luz1, luz2
                int a = Mod(q, 64), b = Mod(r, 64);
                if (a <= 1 || b <= 1) return c[3];
                if (a == 2 || a == 3 || b == 2 || b == 3) return c[4];
                if (a == 62 || a == 63 || b == 62 || b == 63) return c[5];
                int tx = (int)Math.Floor(gx), ty = (int)Math.Floor(gy);
                int baseCor = Hash(tx, ty, 1) % 3 == 0 ? c[1] : c[0];
                // manchas: ruído em blocos de 3x2 px
                if (Hash(x / 3, y / 2, tx * 31 + ty) % 23 == 0) return c[2];
                return baseCor;
            }
            case "tabua":
            {
                // c: base, base2, linha, sombra, linhaSombra, emenda
                int prancha = (int)Math.Floor(r / 48.0);
                bool sombra = gx < 0.85;
                int b = Mod(r, 48);
                if (b <= 1) return sombra ? c[4] : c[2];
                // emendas: cada prancha tem uma a cada 3 casas, deslocada
                int emenda = Mod(q + prancha * 77, 192);
                if (emenda <= 1) return sombra ? c[4] : c[5];
                if (sombra) return c[3];
                return prancha % 2 == 0 ? c[0] : c[1];
            }
            default:
            {
                // dc. c: perf1, perf2, divisa, base, rejunte, desenho
                int n = s.n;
                int a = Mod(q, 64), b = Mod(r, 64);
                bool faixa = gx < 1 || gy < 1 || gx > n - 1 || gy > n - 1;
                bool divisa = (Mod(q, 64) <= 1 && (Math.Abs(gx - 1) < 0.05 || Math.Abs(gx - (n - 1)) < 0.05) && gy >= 1 - 0.05 && gy <= n - 1 + 0.05)
                           || (Mod(r, 64) <= 1 && (Math.Abs(gy - 1) < 0.05 || Math.Abs(gy - (n - 1)) < 0.05) && gx >= 1 - 0.05 && gx <= n - 1 + 0.05);
                if (divisa) return c[2];
                if (faixa)
                {
                    if (a <= 1 || b <= 1) return c[4];
                    return ((x + y) & 1) == 0 ? c[0] : c[1];
                }
                // desenho no piso: quadrado interno, as diagonais e a rosa no centro
                double m = n / 2.0, i0 = 2, i1 = n - 2;
                bool quadrado = ((Mod(q, 64) <= 1 && (Math.Abs(gx - i0) < 0.05 || Math.Abs(gx - i1) < 0.05)) && gy >= i0 && gy <= i1)
                             || ((Mod(r, 64) <= 1 && (Math.Abs(gy - i0) < 0.05 || Math.Abs(gy - i1) < 0.05)) && gx >= i0 && gx <= i1);
                bool dentro = gx >= i0 && gx <= i1 && gy >= i0 && gy <= i1;
                bool diagonal = dentro && (x == s.Bx || y == s.By + n * CH - 1);
                double dxm = (gx - m), dym = (gy - m), dist = Math.Sqrt(dxm * dxm + dym * dym);
                double ang = Math.Atan2(dym, dxm);
                bool anel = Math.Abs(dist - 1.3) < 0.03 && Math.Cos(ang * 4) > -0.6 || Math.Abs(dist - 0.85) < 0.03 || Math.Abs(dist - 0.4) < 0.03;
                if (quadrado || (diagonal && (dist > 1.45 || dist < 0.35)) || anel) return c[5];
                if (a <= 1 || b <= 1) return c[4];
                return c[3];
            }
        }
    }

    // ---------------- móveis ----------------

    /// Só o maior pedaço contínuo de pixels opacos (o resto vira transparente).
    static int[] MaiorPedaco(int[] p, int w, int h, double fracaoMinima = 1)
    {
        var rotulo = new int[w * h];
        int melhor = 0, tamMelhor = 0, atual = 0;
        var pilha = new Stack<int>();
        for (int i = 0; i < p.Length; i++)
        {
            if (rotulo[i] != 0 || ((p[i] >> 24) & 255) < 128) continue;
            atual++; int tam = 0;
            pilha.Push(i); rotulo[i] = atual;
            while (pilha.Count > 0)
            {
                int j = pilha.Pop(); tam++;
                int x = j % w, y = j / w;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int n = ny * w + nx;
                        if (rotulo[n] != 0 || ((p[n] >> 24) & 255) < 128) continue;
                        rotulo[n] = atual; pilha.Push(n);
                    }
            }
            if (tam > tamMelhor) { tamMelhor = tam; melhor = atual; }
        }
        // fica o maior e, com fracaoMinima < 1, os outros pedaços com pelo menos essa fração do tamanho dele
        var tamanhos = new int[atual + 1];
        for (int i = 0; i < p.Length; i++) tamanhos[rotulo[i]]++;
        var o = new int[p.Length];
        for (int i = 0; i < p.Length; i++)
            if (rotulo[i] != 0 && (rotulo[i] == melhor || tamanhos[rotulo[i]] >= tamMelhor * fracaoMinima)) o[i] = p[i];
        return o;
    }

    /// Retângulo dos pixels opacos: x0, y0, x1, y1.
    static int[] Caixa(int[] p, int w, int h)
    {
        int x0 = w, y0 = h, x1 = 0, y1 = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (((p[y * w + x] >> 24) & 255) >= 128) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
        return new[] { x0, y0, x1, y1 };
    }

    static double PeOrig(Base s, char parede, double x)
    {
        // y do pé da parede da ilustração original na coluna x
        if (parede == 'E') return s.oey + (s.ofy - s.oey) * (x - s.oex) / (s.ofx - s.oex);
        return s.ofy + (s.ody - s.ofy) * (x - s.ofx) / (s.odx - s.ofx);
    }

    static int Dist(int a, int b)
    {
        return Math.Abs(((a >> 16) & 255) - ((b >> 16) & 255)) + Math.Abs(((a >> 8) & 255) - ((b >> 8) & 255)) + Math.Abs((a & 255) - (b & 255));
    }

    static void ColarMovel(Sala s, Movel m, Base b, int[] tela, int[] id, int W, int H)
    {
        if (m.w == 0)
        {
            // peça avulsa (só a imagem pronta): o meio da base no ponto (gx, gy) do piso
            int fw, fh;
            var im = Dobrar.Ler(System.IO.Path.Combine(PastaDasImagens, m.arquivo), out fw, out fh);
            var c = Caixa(im, fw, fh);
            double px = s.Bx + CW * m.gx - CW * m.gy, py = s.By + CH * m.gx + CH * m.gy;
            int ox = (int)Math.Round(px - (c[0] + c[2]) / 2.0), oy = (int)Math.Round(py - c[3]);
            for (int y = 0; y < fh; y++)
                for (int x = 0; x < fw; x++)
                {
                    int cor = im[y * fw + x];
                    if (((cor >> 24) & 255) < 128) continue;
                    int X = ox + x, Y = oy + y;
                    if (X < 0 || Y < 0 || X >= W || Y >= H || id[Y * W + X] == Vazio) continue;
                    tela[Y * W + X] = cor;
                }
            return;
        }
        int[] orig = b.px; int ow = b.w, oh = b.h;
        // recorte: tira o fundo da ilustração (paredes, piso e as linhas do pé das paredes) que encosta na borda do
        // retângulo; o que tem cor de fundo mas fica cercado pelo móvel (assento, tampo da mesa) continua
        var rec = new int[m.w * m.h];
        var candidato = new bool[m.w * m.h];
        for (int y = 0; y < m.h; y++)
            for (int x = 0; x < m.w; x++)
            {
                int ox = m.x + x, oy = m.y + y;
                if (ox < 0 || oy < 0 || ox >= ow || oy >= oh) { candidato[y * m.w + x] = true; continue; }
                int c = orig[oy * ow + ox];
                if (m.trocas != null) for (int t = 0; t + 1 < m.trocas.Length; t += 2) if (Dist(c, m.trocas[t]) < 40) { c = m.trocas[t + 1]; break; }
                rec[y * m.w + x] = c;
                if (((c >> 24) & 255) < 128) { candidato[y * m.w + x] = true; continue; }
                bool fundo = false;
                foreach (int f in b.fundo) if (Dist(c, f) < 10) { fundo = true; break; }
                bool escura = ((c >> 16) & 255) + ((c >> 8) & 255) + (c & 255) < 60;
                if (!fundo && escura)
                {
                    double pe = ox <= b.ofx ? PeOrig(b, 'E', ox) : PeOrig(b, 'D', ox);
                    fundo = Math.Abs(oy - pe) <= 1.5 || Math.Abs(ox - b.ofx) < 1;
                }
                candidato[y * m.w + x] = fundo;
            }
        var fila = new Queue<int>();
        var tirar = new bool[m.w * m.h];
        for (int x = 0; x < m.w; x++) { fila.Enqueue(x); fila.Enqueue((m.h - 1) * m.w + x); }
        for (int y = 0; y < m.h; y++) { fila.Enqueue(y * m.w); fila.Enqueue(y * m.w + m.w - 1); }
        while (fila.Count > 0)
        {
            int i = fila.Dequeue();
            if (tirar[i] || !candidato[i]) continue;
            tirar[i] = true;
            int x = i % m.w, y = i / m.w;
            if (x > 0) fila.Enqueue(i - 1);
            if (x < m.w - 1) fila.Enqueue(i + 1);
            if (y > 0) fila.Enqueue(i - m.w);
            if (y < m.h - 1) fila.Enqueue(i + m.w);
        }
        for (int i = 0; i < rec.Length; i++) if (tirar[i]) rec[i] = 0;
        int[] img = Dobrar.Scale2x(rec, m.w, m.h);
        int iw = m.w * 2, ih = m.h * 2;
        if (m.escala < 1.99)
        {
            int nw, nh;
            img = Dobrar.Reduzir(img, iw, ih, m.escala / 2.0, out nw, out nh);
            iw = nw; ih = nh;
        }
        if (PastaDosMoveis != null) Dobrar.Gravar(System.IO.Path.Combine(PastaDosMoveis, s.nome + "_" + m.nome + ".png"), img, iw, ih);
        double k = iw / (double)m.w;   // escala efetiva
        int ajusteX = 0, ajusteY = 0;
        if (m.arquivo != null)
        {
            // móvel refeito (PixelLab Pro): entra no lugar do recorte ampliado, com a base (centro e chão) no mesmo ponto
            int fw, fh;
            var novo = Dobrar.Ler(System.IO.Path.Combine(PastaDasImagens, m.arquivo), out fw, out fh);
            if (m.soMaior) novo = MaiorPedaco(novo, fw, fh);   // o Pro às vezes repete um pedaço do objeto na imagem
            else if (m.pedacoMinimo > 0) novo = MaiorPedaco(novo, fw, fh, m.pedacoMinimo);   // tira pedaços soltos pequenos
            int[] cv = Caixa(img, iw, ih), cn = Caixa(novo, fw, fh);
            ajusteX = (cv[0] + cv[2]) / 2 - (cn[0] + cn[2]) / 2;
            ajusteY = cv[3] - cn[3];
            img = novo; iw = fw; ih = fh;
        }

        // ponto de referência: o pé da parede embaixo da borda esquerda do recorte (ou o canto do fundo)
        double rxo, ryo, rxn, ryn;
        if (m.parede == 'C') { rxo = b.ofx; ryo = b.ofy; rxn = s.Bx; ryn = s.By; }
        else
        {
            rxo = m.x; ryo = PeOrig(b, m.parede, m.x);
            if (m.parede == 'E') { rxn = s.Bx - CW * m.u; ryn = s.By + CH * m.u; }
            else { rxn = s.Bx + CW * m.u; ryn = s.By + CH * m.u; }
        }
        int x0 = (int)Math.Round(rxn + (m.x - rxo) * k), y0 = (int)Math.Round(ryn + (m.y - ryo) * k);
        x0 += ajusteX; y0 += ajusteY;
        for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                int c = img[y * iw + x];
                if (((c >> 24) & 255) < 128) continue;
                int X = x0 + x, Y = y0 + y;
                if (X < 0 || Y < 0 || X >= W || Y >= H || id[Y * W + X] == Vazio) continue;   // nada fica para fora da sala
                tela[Y * W + X] = c;
            }
    }
}
