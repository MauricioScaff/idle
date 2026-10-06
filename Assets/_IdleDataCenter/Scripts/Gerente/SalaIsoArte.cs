using System;
using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A sala desenhada com a arte gerada (Resources/Arte/Iso: sprites limpos pela ferramenta LimparFolhaDePixelArt,
    /// na escala do manifesto Arte/Isometrico/folhas.txt). A grade segue a arte: cada piso gerado cobre 2 × 2 casas
    /// de 64 × 30 pixels. As paredes são desenhadas em código com as cores da arte (encaixam certinho no piso).
    ///
    /// Salas: armário (Técnico), salinha (Sysadmin), sala de racks (Analista), sala virtualizada (DevOps) e data center
    /// (SRE, que também é o DC-01 por dentro do Arquiteto em diante). O campus e o mapa-múndi seguem desenhados
    /// em código (SalaIsoCampus, SalaIsoMundo). Regra da arrumação: o que é alto fica
    /// encostado nas paredes do fundo e o que é baixo na frente, para nada esconder as pessoas.
    /// </summary>
    public partial class SalaIso
    {
        const int AHW = 32, AHH = 15;       // meia casa da grade da arte nova
        const int AParede = 132;            // um pouco mais que a altura de uma pessoa (96)
        const float Encosto = 0.08f;        // folga entre um objeto e a parede

        PixelCanvas telaArte;
        IsoDesenho dArte;
        Vector2Int aO;
        int aW, aD, cargoDaArte = -1;       // tamanho da sala em casas (ao longo de gx e de gy)
        bool desenhandoArte;
        RectInt areaArte;

        /// <summary>Todos os cargos usam a arte nova por dentro (do Arquiteto em diante, o DC-01 é o data center do SRE).</summary>
        bool TemArteNova => true;

        /// <summary>Armário 4 × 4 (apertado), salinha 6 × 6, sala de racks 8 × 8, sala virtualizada 10 × 10, data center 12 × 12 casas.</summary>
        static readonly int[] TamanhoDaSala = { 4, 4, 6, 8, 10, 12 };

        // ---------------- Sprites ----------------

        /// <summary>
        /// Um sprite da arte nova. "frente" é o x do canto da frente da base (o pixel mais baixo): à esquerda dele
        /// fica a face que corre ao longo de gx, à direita a que corre ao longo de gy. Daí sai quantas casas ele ocupa.
        /// </summary>
        class SpriteIso
        {
            public Color32[] px; public int w, h, frente;
            public List<int> leds = new List<int>();
            public float CasasX => frente / (float)AHW;          // ao longo de gx (face da esquerda)
            public float CasasY => (w - frente) / (float)AHW;    // ao longo de gy (face da direita)
        }
        static readonly Dictionary<string, SpriteIso> sprites = new Dictionary<string, SpriteIso>();

        static SpriteIso Carregar(string nome)
        {
            if (sprites.TryGetValue(nome, out var s)) return s;
            var tex = ArteGerada.Textura("Iso/" + nome);
            s = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            return sprites[nome] = s;
        }

        /// <summary>O mesmo sprite espelhado: no isométrico isso troca as direções (o que corria ao longo de gx passa a correr ao longo de gy).</summary>
        static SpriteIso Espelhado(string nome)
        {
            string chave = nome + "|espelhado";
            if (sprites.TryGetValue(chave, out var s)) return s;
            var o = Carregar(nome);
            var px = new Color32[o.px.Length];
            for (int y = 0; y < o.h; y++)
                for (int x = 0; x < o.w; x++) px[y * o.w + x] = o.px[y * o.w + (o.w - 1 - x)];
            return sprites[chave] = Preparar(px, o.w, o.h);
        }

        static SpriteIso Preparar(Color32[] px, int w, int h)
        {
            var s = new SpriteIso { px = px, w = w, h = h, frente = w / 2 };
            // LEDs: pixels bem verdes ou bem vermelhos e claros (piscam no desenho)
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a > 0 && ((c.g > 170 && c.r < 140 && c.b < 140) || (c.r > 190 && c.g < 90 && c.b < 90))) s.leds.Add(i);
            }
            // canto da frente: média dos pixels opacos nas duas linhas mais baixas
            for (int y = h - 1; y >= 0; y--)
            {
                int soma = 0, n = 0;
                for (int yy = y; yy >= Mathf.Max(0, y - 1); yy--)
                    for (int x = 0; x < w; x++)
                        if (px[yy * w + x].a > 0) { soma += x; n++; }
                if (n > 0) { s.frente = soma / n; break; }
            }
            return s;
        }

        Vector2Int AP(float gx, float gy, float z = 0) =>
            new Vector2Int(Mathf.RoundToInt(aO.x + (gx - gy) * AHW), Mathf.RoundToInt(aO.y + (gx + gy) * AHH - z));

        // ---------------- Montagem e desenho ----------------

        void MontarArte()
        {
            cargoDaArte = E.Cargo;
            aW = aD = TamanhoDaSala[Mathf.Clamp(E.Cargo, 0, TamanhoDaSala.Length - 1)];
            int largura = (aW + aD) * AHW + 48, altura = (aW + aD) * AHH + AParede + 48;
            telaArte = new PixelCanvas(largura, altura);
            dArte = new IsoDesenho(telaArte);
            aO = new Vector2Int(24 + aD * AHW, AParede + 24);
            var esq = AP(0, aD); var dir = AP(aW, 0); var topo = AP(0, 0, AParede); var baixo = AP(aW, aD);
            areaArte = new RectInt(esq.x - 4, topo.y - 4, dir.x - esq.x + 8, baixo.y + 8 - topo.y);
        }

        void DesenharArte()
        {
            if (telaArte == null || cargoDaArte != E.Cargo) MontarArte();
            tela = telaArte; d = dArte;
            desenhandoArte = true;
            tela.Limpar(new Color32(0, 0, 0, 0));

            ParedesEPisoArte();
            switch (E.Cargo)
            {
                case Catalogo.CargoFreelancer:
                case Catalogo.CargoTecnico: Armario(); break;
                case Catalogo.CargoSysadmin: Salinha(); break;
                default: SalaGrande(); break;
            }
            PersonagensArte();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompraArte();
            ChamadoArte();
            tela.Aplicar();
        }

        // ---------------- Paredes e piso ----------------

        /// <summary>Cores das paredes por cargo: face da esquerda, face da direita, rodapés e friso do topo.</summary>
        static readonly string[][] CoresDasParedes =
        {
            new[] { "2f4c80", "28416f", "1c2b4f", "18264a", "7a9bd4", "6886bd" },   // armário: azul-marinho
            new[] { "2f4c80", "28416f", "1c2b4f", "18264a", "7a9bd4", "6886bd" },   // armário: azul-marinho
            new[] { "3f5a86", "364f78", "243553", "1f2f4c", "8aa6d6", "7893c4" },   // salinha: azul acinzentado
            new[] { "5b6a8a", "4f5d7c", "2e3750", "283149", "a9b8d6", "95a5c6" },   // sala de racks: cinza técnico
            new[] { "2f3a5a", "293350", "1b2238", "171d31", "6a7aa8", "5d6c98" },   // sala virtualizada: grafite azulado
            new[] { "242b47", "1f2540", "141a2d", "111627", "d08a3a", "b8782f" },   // data center: escuro, friso laranja
        };

        void ParedesEPisoArte()
        {
            var cores = CoresDasParedes[Mathf.Clamp(E.Cargo, 0, CoresDasParedes.Length - 1)];
            Color32 C(int i) => IsoDesenho.C(cores[i]);
            d.Poligono(C(0), AP(0, 0), AP(0, aD), AP(0, aD, AParede), AP(0, 0, AParede));
            d.Poligono(C(1), AP(0, 0), AP(aW, 0), AP(aW, 0, AParede), AP(0, 0, AParede));
            d.Poligono(C(2), AP(0, 0), AP(0, aD), AP(0, aD, 7), AP(0, 0, 7));
            d.Poligono(C(3), AP(0, 0), AP(aW, 0), AP(aW, 0, 7), AP(0, 0, 7));
            d.Poligono(C(4), AP(0, 0, AParede), AP(0, aD, AParede), AP(-0.12f, aD, AParede + 3), AP(-0.12f, -0.12f, AParede + 3));
            d.Poligono(C(5), AP(0, 0, AParede), AP(aW, 0, AParede), AP(aW, -0.12f, AParede + 3), AP(-0.12f, -0.12f, AParede + 3));
            var canto = AP(0, 0);
            tela.Ret(canto.x, canto.y - AParede, 1, AParede, IsoDesenho.C("101a33"));

            // piso: cada peça cobre 2 × 2 casas. Na sala de racks, piso técnico (elevado, com furos) fora do canto do escritório
            for (int gy = 0; gy < aD; gy += 2)
                for (int gx = 0; gx < aW; gx += 2)
                {
                    bool tecnico = E.Cargo >= Catalogo.CargoAnalista && gx >= 4;   // o lado da esquerda (escritório, lounge, NOC) fica em madeira
                    var s = Carregar(tecnico ? "piso_elevado" : (gx + gy) / 2 % 2 == 0 ? "piso_a" : "piso_b");
                    var p = AP(gx, gy);
                    tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - 1);
                }
            // espessura da laje na frente
            d.Poligono(IsoDesenho.C("4a2b30"), AP(0, aD), AP(aW, aD), AP(aW, aD, -6), AP(0, aD, -6));
            d.Poligono(IsoDesenho.C("3b2227"), AP(aW, 0), AP(aW, aD), AP(aW, aD, -6), AP(aW, 0, -6));
        }

        /// <summary>Quadro, janela, prateleira... presos na parede: gx = 0 é a parede da esquerda, gy = 0 a da direita.</summary>
        void NaParedeArte(string nome, float gx, float gy, float z)
        {
            var s = Carregar(nome);
            var p = AP(gx, gy, z);
            tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - s.h);
        }

        // ---------------- Colocar objetos ----------------

        struct Colocado { public float gx, gy, casasX, casasY; public RectInt tela; public float FimX => gx + casasX; public float FimY => gy + casasY; }

        /// <summary>
        /// Põe um sprite no piso com o canto do FUNDO da base em (gx, gy); o tamanho da base sai do próprio sprite,
        /// então ele encosta certinho nas paredes e nos vizinhos. z levanta (em cima de outra coisa).
        /// </summary>
        Colocado Colocar(string nome, float gx, float gy, string clique = null, bool ledsPiscam = true, float z = 0, bool espelhar = false)
        {
            var s = espelhar ? Espelhado(nome) : Carregar(nome);
            float cx = s.CasasX, cy = s.CasasY;
            var f = AP(gx + cx, gy + cy, z);
            int x = f.x - s.frente, y = f.y - s.h + 1;
            float semente = gx * 7 + gy * 3;
            fila.Add((gx + cx + gy + cy + (z > 0 ? 0.05f : 0), () => DesenharSprite(s, x, y, ledsPiscam ? semente : -1)));
            var r = new RectInt(x, y, s.w, s.h);
            if (clique != null) Alvos.Add(new Alvo { Area = r, Tipo = clique });
            return new Colocado { gx = gx, gy = gy, casasX = cx, casasY = cy, tela = r };
        }

        void DesenharSprite(SpriteIso s, int x, int y, float sementeLed = -1)
        {
            tela.Imagem(s.px, s.w, s.h, x, y);
            if (sementeLed < 0 || s.leds.Count == 0) return;
            // LEDs piscando: de vez em quando cada um apaga um instante
            bool apagado = Mathf.Sin(t * 5f + sementeLed) > 0.6f;
            if (!apagado) return;
            foreach (int i in s.leds) tela.Pixel(x + i % s.w, y + i / s.w, new Color32(30, 40, 30, 255));
        }

        /// <summary>Uma fila de objetos iguais ao longo da parede da direita (gx cresce) a partir de (gx, gy), até gxMax.</summary>
        List<Vector2> FilaEmX(string nome, float gx, float gy, float gxMax, int quantidade, float folga = 0.06f)
        {
            var s = Carregar(nome);
            var lugares = new List<Vector2>();
            for (float x = gx; lugares.Count < quantidade && x + s.CasasX <= gxMax + 0.01f; x += s.CasasX + folga) lugares.Add(new Vector2(x, gy));
            return lugares;
        }

        /// <summary>Uma fila ao longo da parede da esquerda (gy cresce).</summary>
        List<Vector2> FilaEmY(string nome, float gx, float gy, float gyMax, int quantidade, float folga = 0.06f)
        {
            var s = Carregar(nome);
            var lugares = new List<Vector2>();
            for (float y = gy; lugares.Count < quantidade && y + s.CasasY <= gyMax + 0.01f; y += s.CasasY + folga) lugares.Add(new Vector2(gx, y));
            return lugares;
        }

        void NovaPlacaArte(string setor, string nome, float gx, float gy, float z, Color cor) =>
            Placas.Add(new Placa { Setor = setor, Nome = nome, Pos = AP(gx, gy, z), Cor = cor });

        void AlertaArte(int x, int y)
        {
            if (!Piscar()) return;
            tela.Texto("!", x - 2, y - 16, IsoDesenho.C("ff3b4e"), true, 3);
        }

        /// <summary>Contorno tracejado laranja no piso: onde entra a próxima compra do equipamento principal.</summary>
        void MarcadorEm(float gx, float gy, float w, float dd)
        {
            fila.Add((gx + gy + 0.2f, () =>
            {
                var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                var a = AP(gx, gy); var b = AP(gx + w, gy); var c = AP(gx + w, gy + dd); var e = AP(gx, gy + dd);
                foreach (var (p, q) in new[] { (a, b), (b, c), (c, e), (e, a) })
                {
                    int n = Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y));
                    for (int i = 0; i <= n; i += 3) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(p, q, (float)i / Mathf.Max(1, n))), cor, 2);
                }
            }));
            Marcador = AP(gx + w / 2, gy + dd / 2);
        }

        // ---------------- Peças que se repetem nas salas ----------------

        Colocado mesaArte;
        Vector2[] LugaresDasTorres = new Vector2[0];
        Vector2 lugarDoTecnico;      // onde o técnico se ajoelha para consertar (se algo travou)
        bool tecnicoConsertando;

        /// <summary>Planta no canto, mesa com o CRT e caneca (café), cadeira e lixeira. Retorna onde a mesa acaba (gx).</summary>
        float CantoDoTecnico()
        {
            var planta = Colocar("planta", Encosto, Encosto);
            mesaArte = Colocar("mesa_crt", planta.FimX + 0.1f, Encosto, "equipamento");
            var mesa = mesaArte;
            var caneca = Carregar("caneca");
            int x = mesa.tela.x + Mathf.RoundToInt(mesa.tela.width * 0.72f), y = mesa.tela.y + Mathf.RoundToInt(mesa.tela.height * 0.36f);
            fila.Add((mesa.FimX + mesa.FimY + 0.05f, () => DesenharSprite(caneca, x, y)));
            Alvos.Add(new Alvo { Area = new RectInt(x - 3, y - 3, caneca.w + 6, caneca.h + 6), Tipo = "cafe" });
            var cadeira = Colocar("cadeira", mesa.gx + mesa.casasX * 0.3f, mesa.FimY + 0.15f);
            Colocar("lixeira", cadeira.FimX + 0.35f, cadeira.gy + 0.25f);
            return mesa.FimX;
        }

        /// <summary>Torres em fila a partir de (gx, gy) até gxMax; quando a fila acaba, uma segunda fila na frente.</summary>
        void Torres(float gx, float gy, float gxMax, int maximo)
        {
            var torre = Carregar("torre");
            var lugares = FilaEmX("torre", gx, gy, gxMax, maximo);
            if (lugares.Count < maximo) lugares.AddRange(FilaEmX("torre", gx, gy + torre.CasasY + 0.3f, gxMax, maximo - lugares.Count));
            LugaresDasTorres = lugares.ToArray();
            if (LugaresDasTorres.Length == 0) return;
            var meio = LugaresDasTorres[Mathf.Min(1, LugaresDasTorres.Length - 1)];
            NovaPlacaArte("Compute", "Compute", meio.x + torre.CasasX / 2, meio.y, AParede + 8, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(LugaresDasTorres.Length, E.Torres); i++)
            {
                var l = LugaresDasTorres[i];
                bool travado = E.Travado(i);
                var c = Colocar(travado ? "torre_travada" : "torre", l.x, l.y, "servidor:" + i);
                if (travado)
                {
                    fila.Add((l.x + l.y + 3f, () => AlertaArte(c.tela.x + c.tela.width / 2, c.tela.y - 4)));
                    if (!tecnicoConsertando) { tecnicoConsertando = true; lugarDoTecnico = new Vector2(l.x + torre.CasasX * 0.2f, l.y + torre.CasasY + 0.45f); }
                }
            }
        }

        // ---------------- Armário (Técnico) ----------------

        void Armario()
        {
            tecnicoConsertando = false;
            NaParedeArte("janela", 0.02f, 1.25f, 52);
            NaParedeArte("prateleira", 0.05f, 3.0f, 58);
            float fimDaMesa = CantoDoTecnico();
            Torres(fimDaMesa + 0.25f, Encosto, aW - Encosto, 12);
            if (LugaresDasTorres.Length > 1) NaParedeArte("quadro", LugaresDasTorres[1].x + Carregar("torre").CasasX, 0.02f, 66);
            if (E.Torres < LugaresDasTorres.Length) { var l = LugaresDasTorres[E.Torres]; var tr = Carregar("torre"); MarcadorEm(l.x, l.y, tr.CasasX, tr.CasasY); }

            if (E.Nivel(Catalogo.FiltroDeLinha) > 0) Colocar("filtro_linha", fimDaMesa + 0.15f, mesaArte.FimY + 0.35f);
            if (E.Nivel(Catalogo.Ventilador) > 0) Colocar("ventilador", Encosto, 1.4f);
            var caixas = Colocar("caixas", Encosto, 3.0f);
            if (E.TemEstagiario) Colocar("roteador", Encosto + 0.05f, 3.05f, null, true, caixas.tela.height - 16);
        }

        // ---------------- Salinha (Sysadmin) ----------------

        Colocado rackArte;

        /// <summary>O rack 42U e os servidores 1U: um rack a cada 5 servidores (o primeiro vazio até chegar o primeiro 1U).</summary>
        void Racks1U(List<Vector2> lugares)
        {
            if (!E.TemRack || lugares.Count == 0) return;
            int racks = Mathf.Clamp(Mathf.CeilToInt(E.ServidoresRack / (float)Catalogo.VagasNoRack), 1, lugares.Count);
            bool travado = false;
            for (int s = E.Torres; s < E.TotalServidores; s++) travado |= E.Travado(s);
            for (int i = 0; i < racks; i++)
            {
                var l = lugares[i];
                var c = Colocar(E.ServidoresRack == 0 ? "rack_vazio" : "rack_1u", l.x, l.y, "rack");
                if (i == 0) rackArte = c;
                if (travado && i == 0)
                {
                    fila.Add((l.x + l.y + 3f, () => AlertaArte(c.tela.x + c.tela.width / 2, c.tela.y - 4)));
                    if (!tecnicoConsertando) { tecnicoConsertando = true; lugarDoTecnico = new Vector2(c.FimX + 0.2f, l.y + 0.3f); }
                }
            }
            if (E.Nivel(Catalogo.CabosOrganizados) > 0) Colocar("cabos", lugares[0].x + Carregar("rack_1u").CasasX + 0.1f, lugares[0].y + 0.2f);
        }

        /// <summary>No-breaks encostados na parede da esquerda, a partir de gy; retorna onde a fila acabou.</summary>
        float NoBreaks(float gy, float gyMax)
        {
            int n = E.Nivel(Catalogo.NoBreak);
            var lugares = FilaEmY("nobreak", Encosto, gy, gyMax, Mathf.Max(1, Mathf.Min(n, 6)));
            var nb = Carregar("nobreak");
            if (lugares.Count > 0) NovaPlacaArte("Energia", "Energia", Encosto + nb.CasasX / 2, lugares[0].y + nb.CasasY, 70, IsoGui.Laranja);
            for (int i = 0; i < Mathf.Min(n, lugares.Count); i++) Colocar("nobreak", lugares[i].x, lugares[i].y);
            return lugares.Count > 0 ? lugares[lugares.Count - 1].y + nb.CasasY : gy;
        }

        /// <summary>Ar-condicionado: o split na parede e, a partir do segundo nível, portáteis no chão.</summary>
        void ArCondicionado(float gxParede, float gxChao, float gyChao)
        {
            int n = E.Nivel(Catalogo.ArCondicionado);
            NovaPlacaArte("Refrigeracao", "Refrigeração", gxParede, 0.02f, AParede - 14, IsoGui.Cyan);
            if (n >= 1) NaParedeArte("ar_split", gxParede, 0.02f, 98);
            var portatil = Carregar("ar_portatil");
            for (int i = 1; i < Mathf.Min(n, 4); i++) Colocar("ar_portatil", gxChao + (i - 1) * (portatil.CasasX + 0.1f), gyChao);
        }

        void Salinha()
        {
            tecnicoConsertando = false;
            float fimDaMesa = CantoDoTecnico();
            NaParedeArte("janela", fimDaMesa - 1.0f, 0.02f, 70);
            // parede da direita: torres depois da mesa (uma fila só; o resto vai para o rack) e a estante no fim
            var estante = Carregar("estante");
            Torres(fimDaMesa + 0.25f, Encosto, aW - Encosto - estante.CasasX - 0.15f, 6);
            Colocar("estante", aW - Encosto - estante.CasasX, Encosto);
            ArCondicionado(fimDaMesa + 1.3f, fimDaMesa + 0.3f, 2.2f);
            // parede da esquerda: racks 1U no fundo, no-breaks depois, e o quadro branco perto da frente
            Racks1U(FilaEmY("rack_1u", Encosto, 1.0f, 4.0f, 3));
            var rack = Carregar("rack_1u");
            float depoisDosRacks = E.TemRack ? 1.0f + Mathf.Clamp(Mathf.CeilToInt(E.ServidoresRack / (float)Catalogo.VagasNoRack), 1, 3) * (rack.CasasY + 0.06f) : 1.0f;
            float fimNoBreaks = NoBreaks(depoisDosRacks + 0.1f, aD - 1.2f);
            Colocar("quadro_branco", Encosto, Mathf.Min(fimNoBreaks + 0.2f, aD - 1.2f));

            // marcador: o rack (os servidores 1U entram nele) ou o lugar dele, se ainda não foi comprado
            if (!E.TemRack) MarcadorEm(Encosto, 1.0f, rack.CasasX, rack.CasasY);
            else Marcador = AP(rackArte.gx + rackArte.casasX / 2, rackArte.gy + rackArte.casasY / 2, 130);
        }

        // ---------------- Sala de racks (Analista), sala virtualizada (DevOps) e data center (SRE) ----------------

        /// <summary>Onde começam as fileiras do piso técnico e o corredor entre elas.</summary>
        const float InicioDasFileiras = 5.1f, PrimeiraFileira = 2.9f, Corredor = 1.2f;

        void SalaGrande()
        {
            tecnicoConsertando = false;
            int cargo = E.Cargo;
            float fimDaMesa = CantoDoTecnico();
            Torres(fimDaMesa + 0.25f, Encosto, fimDaMesa + 2.4f, 4);
            ArCondicionado(fimDaMesa + 0.9f, fimDaMesa + 0.4f, 2.4f);

            // parede da direita, depois das torres: ar de precisão (a sala de racks já vem com ele), storage e fita
            float gx = fimDaMesa + 2.6f;
            Colocar("crac", gx, Encosto);
            gx += Carregar("crac").CasasX + 0.15f;
            NovaPlacaArte("Storage", "Storage", gx + 0.8f, Encosto, AParede + 8, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(3, E.NivelStorage); i++)
            {
                bool queimado = i == 0 && E.DiscoQueimado;
                var c = Colocar(queimado ? "storage_queimado" : "storage", gx, Encosto, "storage");
                if (queimado) fila.Add((c.FimX + c.FimY + 3f, () => AlertaArte(c.tela.x + c.tela.width / 2, c.tela.y - 4)));
                gx = c.FimX + 0.06f;
            }
            if (E.TemBackup) { var f = Colocar("fita", gx + 0.1f, Encosto); gx = f.FimX + 0.1f; }
            if (cargo >= Catalogo.CargoDevOps && gx + 1 < aW) NaParedeArte("tv_dashboard", gx + 0.9f, 0.02f, 70);

            // parede da esquerda: racks 1U, no-breaks, links de fibra na parede
            Racks1U(FilaEmY("rack_1u", Encosto, 1.0f, 3.0f, 2));
            float fimNoBreaks = NoBreaks(2.9f, 5.6f);
            NovaPlacaArte("Rede", "Rede", 0.02f, fimNoBreaks + 0.6f, 104, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(2, E.Nivel(Catalogo.Link)); i++) NaParedeArte("link_fibra", 0.02f, fimNoBreaks + 0.4f + i * 0.9f, 60);

            // a frente da esquerda muda com o cargo: laboratório (Analista), lounge (DevOps), NOC (SRE)
            if (cargo == Catalogo.CargoAnalista)
            {
                Colocar("extintor", Encosto, aD - 0.9f);
                Colocar("quadro_branco", 1.1f, aD - 1.6f);
                Colocar("estante", 2.3f, aD - 1.5f);
                NovaPlacaArte("Automacao", "Laboratório", 1.7f, aD - 1.6f, 110, IsoGui.Roxo);
            }
            else if (cargo == Catalogo.CargoDevOps) Lounge();
            else Noc();

            // piso técnico: uma fileira por tipo de equipamento, com corredores entre elas
            var rc = Carregar("rack_cheio");
            float gy = PrimeiraFileira;
            string principal = cargo == Catalogo.CargoAnalista ? "rack_cheio" : cargo == Catalogo.CargoDevOps ? "containers" : "k8s";
            Vector2? marcador = null;

            // racks cheios (no Analista ocupam duas fileiras)
            var lugaresRacks = Fileira("rack_cheio", ref gy, cargo == Catalogo.CargoAnalista ? 2 : 1);
            for (int i = 0; i < Mathf.Min(lugaresRacks.Count, E.RacksCheios); i++) Colocar("rack_cheio", lugaresRacks[i].x, lugaresRacks[i].y, "equipamento");
            if (principal == "rack_cheio" && E.RacksCheios < lugaresRacks.Count) marcador = lugaresRacks[E.RacksCheios];

            if (cargo >= Catalogo.CargoDevOps)
            {
                // virtualização: hypervisors, o servidor de CI e o switch 10G
                var lugares = Fileira("hypervisor", ref gy, 1);
                int k = 0;
                for (int i = 0; i < Mathf.Min(3, E.NivelHypervisor) && k < lugares.Count; i++, k++) Colocar("hypervisor", lugares[k].x, lugares[k].y, "equipamento");
                if (E.TemCi && k < lugares.Count) { Colocar("servidor_ci", lugares[k].x, lugares[k].y, "containers"); k++; }
                for (int i = 0; i < Mathf.Min(2, E.Nivel(Catalogo.Link10G)) && k < lugares.Count; i++, k++) Colocar("switch_10g", lugares[k].x, lugares[k].y, "equipamento");

                // containers (o primeiro fica vermelho com o deploy quebrado)
                var hosts = Fileira("containers", ref gy, 1);
                for (int i = 0; i < Mathf.Min(hosts.Count, E.HostsContainers); i++)
                {
                    bool quebrado = i == 0 && E.DeployQuebrado;
                    var c = Colocar(quebrado ? "containers_quebrado" : "containers", hosts[i].x, hosts[i].y, "containers");
                    if (quebrado) fila.Add((c.FimX + c.FimY + 3f, () => AlertaArte(c.tela.x + c.tela.width / 2, c.tela.y - 4)));
                }
                if (principal == "containers" && E.HostsContainers < hosts.Count) marcador = hosts[E.HostsContainers];
            }
            if (cargo >= Catalogo.CargoSre)
            {
                // o cluster: nós Kubernetes (laranja num pico sem escala) e o balanceador na ponta
                var nos = Fileira("k8s", ref gy, 1);
                bool pico = E.EmPico && !E.PicoFoiEscalado;
                int n = Mathf.Min(nos.Count - (E.TemBalanceador ? 1 : 0), E.NosKubernetes);
                for (int i = 0; i < n; i++) Colocar(pico ? "k8s_pico" : "k8s", nos[i].x, nos[i].y, "k8s");
                if (E.TemBalanceador && nos.Count > 0) { var l = nos[nos.Count - 1]; Colocar("balanceador", l.x, l.y + 0.2f, "equipamento"); }
                if (principal == "k8s" && E.NosKubernetes < nos.Count - (E.TemBalanceador ? 1 : 0)) marcador = nos[E.NosKubernetes];
            }
            if (marcador.HasValue) { var p = Carregar(principal); MarcadorEm(marcador.Value.x, marcador.Value.y, p.CasasX, p.CasasY); }
        }

        /// <summary>Lugares de uma ou mais fileiras de um equipamento no piso técnico; gy avança para a próxima fileira.</summary>
        List<Vector2> Fileira(string nome, ref float gy, int fileiras)
        {
            var s = Carregar(nome);
            var lugares = new List<Vector2>();
            for (int f = 0; f < fileiras && gy + s.CasasY <= aD - 0.3f; f++)
            {
                lugares.AddRange(FilaEmX(nome, InicioDasFileiras, gy, aW - 0.15f, 20, 0.03f));
                gy += Carregar("rack_cheio").CasasY + Corredor;
            }
            return lugares;
        }

        /// <summary>DevOps: sofá, puff e máquina de café na frente da esquerda, com a mesa de pé do time.</summary>
        void Lounge()
        {
            Colocar("cafeteira", Encosto, aD - 1.9f);
            Colocar("extintor", Encosto, aD - 0.7f);
            Colocar("sofa", 1.3f, aD - 1.7f);
            Colocar("puff", 3.2f, aD - 1.4f);
            Colocar("mesa_dev", 1.4f, aD - 3.9f, "noc");
            NovaPlacaArte("NOC", "NOC", 2.0f, aD - 3.9f, 100, IsoGui.Cyan);
            Colocar("quadro_branco", 3.3f, aD - 4.0f);
            NovaPlacaArte("Automacao", "Laboratório", 3.7f, aD - 4.0f, 104, IsoGui.Roxo);
        }

        /// <summary>SRE: a sala do NOC (mesa curva e telão), separada do piso técnico por uma divisória de vidro.</summary>
        void Noc()
        {
            NaParedeArte("telao", 0.02f, aD - 3.4f, 64);
            NaParedeArte("porta", 0.02f, aD - 0.9f, 0);
            Colocar("mesa_noc", 1.0f, aD - 4.3f, "noc");
            NovaPlacaArte("NOC", "NOC", 1.7f, aD - 4.3f, 104, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(3, E.Nivel(Catalogo.Observabilidade)); i++) Colocar("observabilidade", 3.0f + i * 0.95f, aD - 4.6f, "equipamento");
            Colocar("sofa", 1.3f, aD - 1.6f);
            Colocar("cafeteira", 3.4f, aD - 1.5f);
            Colocar("quadro_branco", 4.2f, aD - 3.0f);
            NovaPlacaArte("Automacao", "Laboratório", 4.6f, aD - 3.0f, 104, IsoGui.Roxo);
            // divisória de vidro entre o NOC e o piso técnico
            var vidro = Espelhado("vidro");   // espelhado: o painel corre ao longo de gy, paralelo à parede da esquerda
            for (float y = aD - 5.2f; y + vidro.CasasY <= aD - 0.1f; y += vidro.CasasY + 0.02f) Colocar("vidro", InicioDasFileiras - 0.5f, y, null, true, 0, true);
            Colocar("carrinho", InicioDasFileiras + 0.2f, aD - 1.3f);
        }

        // ---------------- Pessoas e efeitos ----------------

        /// <summary>Sprite de pessoa da arte nova, com o uniforme do cargo (cache por cargo).</summary>
        SpriteIso Pessoa(string nome, string prefixo)
        {
            string chave = nome + "#" + E.Cargo;
            if (sprites.TryGetValue(chave, out var s)) return s;
            var tex = Uniformes.TexturaDoCargo("Iso/" + nome, prefixo, E.Cargo);
            s = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            return sprites[chave] = s;
        }

        void PersonagensArte()
        {
            // corredor onde as pessoas andam: na frente do canto do técnico
            int cargo = E.Cargo;
            float gy = cargo <= Catalogo.CargoTecnico ? 2.3f : cargo == Catalogo.CargoSysadmin ? 3.0f : 2.4f;
            float gxFim = cargo <= Catalogo.CargoTecnico ? 3.3f : cargo == Catalogo.CargoSysadmin ? 4.6f : 3.6f;   // da sala de racks em diante, só no lado do escritório
            float gxAndando = PosicaoAndando(1.2f, gxFim, 0f, 0.55f, out bool voltando);
            // técnico: conserta o que travou (ajoelhado ao lado), comemora logo depois de uma compra, senão passeia
            if (tecnicoConsertando) PessoaArte(Pessoa("tecnico_conserta", "tecnico"), lugarDoTecnico.x, lugarDoTecnico.y, false);
            else if (t - ultimaCompraEm < 1.5f) PessoaArte(Pessoa("tecnico_comemora", "tecnico"), gxAndando, gy, false);
            else PessoaArte(Passo("tecnico", "tecnico", 0f), gxAndando, gy, voltando);

            if (E.TemEstagiario)
            {
                // o estagiário anda numa linha mais à frente, no sentido contrário (não se sobrepõe ao técnico)
                float gx = PosicaoAndando(1.4f, gxFim - 0.3f, 0.9f, 0.45f, out bool volta);
                PessoaArte(Passo("estagiario", "estagiario", 0.37f), gx, gy + 1.3f, volta);
            }
            // da sala de racks em diante, engenheiros de campo nos corredores entre as fileiras (um por cargo)
            float profundidade = Carregar("rack_cheio").CasasY;
            for (int i = 0; i < Mathf.Min(cargo - Catalogo.CargoSysadmin, 3); i++)
            {
                float corredor = PrimeiraFileira + profundidade + 0.6f + i * (profundidade + Corredor);
                if (corredor > aD - 0.5f) break;
                float gx = PosicaoAndando(InicioDasFileiras + 0.1f, aW - 0.5f, 0.6f + i * 0.31f, 0.4f + i * 0.05f, out bool volta);
                PessoaArte(Passo("engenheiro", "engenheiro", 0.6f + i), gx, corredor, volta);
            }
            // no SRE, o robô de limpeza passeia pelo corredor da frente
            if (cargo >= Catalogo.CargoSre)
            {
                float gx = PosicaoAndando(InicioDasFileiras, aW - 0.6f, 0.2f, 0.25f, out _);
                Colocar("robo", gx, aD - 0.6f);
            }
        }

        /// <summary>Vai e volta entre gx0 e gx1 (a pessoa olha para onde anda).</summary>
        float PosicaoAndando(float gx0, float gx1, float fase, float velocidade, out bool voltando)
        {
            float ciclo = (t * velocidade * 0.2f + fase) % 2f;
            voltando = ciclo >= 1f;
            return Mathf.Lerp(gx0, gx1, ciclo < 1f ? ciclo : 2f - ciclo);
        }

        /// <summary>Quadro da caminhada: passada, parado, outra passada, parado.</summary>
        SpriteIso Passo(string quem, string prefixo, float fase)
        {
            int q = Mathf.FloorToInt(t * 5f + fase * 10) % 4;
            return Pessoa(q == 0 ? quem + "_andar_a" : q == 2 ? quem + "_andar_b" : quem + "_parado", prefixo);
        }

        /// <summary>Desenha uma pessoa com os pés em (gx, gy); espelhada quando anda para trás.</summary>
        void PessoaArte(SpriteIso s, float gx, float gy, bool espelhar)
        {
            fila.Add((gx + gy + 0.4f, () =>
            {
                var p = AP(gx, gy);
                tela.Ret(p.x - 12, p.y - 2, 24, 4, new Color32(20, 20, 40, 90));   // sombra
                int x0 = p.x - s.w / 2, y0 = p.y - s.h;
                for (int y = 0; y < s.h; y++)
                    for (int x = 0; x < s.w; x++)
                    {
                        var c = s.px[y * s.w + (espelhar ? s.w - 1 - x : x)];
                        if (c.a > 0) tela.Pixel(x0 + x, y0 + y, c);
                    }
            }));
        }

        Vector2? LugarDoItemArte(string id)
        {
            var torre = LugaresDasTorres.Length > 0 ? LugaresDasTorres[Mathf.Clamp(E.Torres - 1, 0, LugaresDasTorres.Length - 1)] + new Vector2(0.3f, 0.3f) : (Vector2?)null;
            switch (id)
            {
                case Catalogo.Servidor: case Catalogo.Ssd: case Catalogo.Ventoinha: case Catalogo.PastaTermica: return torre;
                case Catalogo.Rack: case Catalogo.Servidor1U: case Catalogo.CabosOrganizados: case Catalogo.Firmware:
                    return E.TemRack ? new Vector2(rackArte.gx + rackArte.casasX / 2, rackArte.gy + rackArte.casasY / 2) : (Vector2?)null;
                case Catalogo.FiltroDeLinha: return new Vector2(mesaArte.FimX + 0.4f, mesaArte.FimY + 0.5f);
                case Catalogo.Ventilador: return new Vector2(0.4f, 1.7f);
                case Catalogo.Estagiario: return new Vector2(0.5f, 3.3f);
                case Catalogo.NoBreak: return new Vector2(0.4f, 3.5f);
                case Catalogo.ArCondicionado: return new Vector2(mesaArte.FimX + 1f, 0.3f);
                case Catalogo.RackCheio: case Catalogo.PisoElevado: return new Vector2(5f, 3.8f);
                case Catalogo.Storage: case Catalogo.Backup: return new Vector2(mesaArte.FimX + 4f, 0.4f);
                case Catalogo.Link: return new Vector2(0.3f, 5.5f);
                case Catalogo.Containers: case Catalogo.ServidorCi: case Catalogo.ImagensEnxutas: case Catalogo.CacheRedis: return new Vector2(InicioDasFileiras + 1f, PrimeiraFileira + 4.5f);
                case Catalogo.Hypervisor: case Catalogo.Link10G: return new Vector2(InicioDasFileiras + 1f, PrimeiraFileira + 2.5f);
                case Catalogo.NoKubernetes: case Catalogo.Balanceador: case Catalogo.ServiceMesh: return new Vector2(InicioDasFileiras + 1f, PrimeiraFileira + 6.5f);
                case Catalogo.Observabilidade: return new Vector2(3.5f, aD - 4f);
                default: return null;
            }
        }

        void FaiscasDaCompraArte()
        {
            float idade = t - ultimaCompraEm;
            if (idade > 1.2f || ultimaCompra == null) return;
            var lugar = LugarDoItemArte(ultimaCompra);
            if (lugar == null) return;
            var centro = AP(lugar.Value.x, lugar.Value.y, 40);
            for (int i = 0; i < 18; i++)
            {
                float ang = i * 0.449f + i * i * 0.07f, raio = 6 + idade * (36 + (i % 4) * 10);
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 20));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 3, 3, cor);
            }
        }

        void ChamadoArte()
        {
            if (!E.TemChamado) return;
            float bob = Mathf.Sin(t * 4) * 3;
            var p = AP(mesaArte.gx + mesaArte.casasX / 2, mesaArte.gy + mesaArte.casasY / 2, 104 + bob);   // flutua sobre a mesa
            string[] papel =
            {
                "yyyyyyyyy.", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy",
            };
            // o papel em dobro, para combinar com a escala da arte nova
            tela.Ret(p.x - 12, p.y - 22, 24, 22, IsoDesenho.C("1b1a2e"));
            for (int y = 0; y < papel.Length; y++)
                for (int x = 0; x < papel[y].Length; x++)
                    if (papel[y][x] != '.') tela.Ret(p.x - 10 + x * 2, p.y - 20 + y * 2, 2, 2, IsoDesenho.C(papel[y][x] == 'y' ? "fdf6e3" : "7d82ad"));
            tela.Ret(p.x + 6, p.y - 20, 4, 4, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)E.FracaoDoPrazoDoChamado;
            tela.Ret(p.x - 12, p.y + 3, 24, 3, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 12, p.y + 3, Mathf.RoundToInt(24 * resta), 3, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 14, p.y - 24, 28, 32), Tipo = "chamado" });
        }
    }
}
