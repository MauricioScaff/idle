using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Sala ilustrada (PixelLab): uma ilustração inteira da sala como fundo (Resources/Arte/Salas) e, por cima, as compras
    /// encostadas nas paredes e as pessoas (Resources/Arte/PixelLab). O armário (Técnico) e a salinha (Sysadmin) já são
    /// assim; as outras salas seguem com a arte de SalaIsoArte até ganharem a sua ilustração.
    ///
    /// Armário: as torres se enfileiram na parede da esquerda, embaixo da janela; o ventilador fica preso na parede da
    /// direita, o filtro de linha no chão na frente da mesa e a caneca em cima dela.
    /// Salinha: na parede da esquerda os racks no fundo e as torres na frente; os no-breaks embaixo da janela (a porta e a
    /// mesa ocupam o resto da parede da direita) e o ar-condicionado no alto da parede da esquerda.
    /// Da salinha em diante a sala é montada na escala da pessoa (Arte/Ferramentas/montar.ps1): paredes, piso e portas
    /// desenhados com as cores da ilustração e os móveis recortados dela; tudo no tamanho original, racks a 1,25x ("_g").
    /// </summary>
    public partial class SalaIso
    {
        /// <summary>
        /// Uma ilustração e o losango do piso nela: canto do fundo (onde as paredes se encontram), da esquerda e da direita.
        /// As linhas fundo→esquerda e fundo→direita são o pé das paredes, onde as compras encostam.
        /// </summary>
        class Ilustracao
        {
            public string nome;
            public string equip;     // sufixo dos racks ("_g": 1,25x, uns 2 m perto da pessoa)
            public int escala = 1;   // tamanho dos avisos desenhados na sala (alerta, chamado, faíscas): 2 nas salas grandes
            public Vector2 fundo, esquerda, direita;
            public int casas;
        }

        /// <summary>Uma por cargo. O DevOps usa a sala de racks até ganhar a sua; do SRE em diante, o data center.</summary>
        static readonly Ilustracao[] Ilustracoes =
        {
            new Ilustracao { nome = "armario", equip = "", fundo = new Vector2(200, 146), esquerda = new Vector2(57.5f, 217), direita = new Vector2(341, 218), casas = 4 },
            new Ilustracao { nome = "salinha_hd", equip = "_g", fundo = new Vector2(244, 140), esquerda = new Vector2(20, 252), direita = new Vector2(468, 252), casas = 7 },
            new Ilustracao { nome = "racks_hd", equip = "_g", escala = 2, fundo = new Vector2(338, 152), esquerda = new Vector2(18, 312), direita = new Vector2(658, 312), casas = 10 },
            new Ilustracao { nome = "racks_hd", equip = "_g", escala = 2, fundo = new Vector2(338, 152), esquerda = new Vector2(18, 312), direita = new Vector2(658, 312), casas = 10 },
            new Ilustracao { nome = "dc_hd", equip = "_g", escala = 2, fundo = new Vector2(400, 150), esquerda = new Vector2(16, 342), direita = new Vector2(784, 342), casas = 12 },
        };

        /// <summary>As áreas atrás das portas (Analista em diante): dados e backup, rede e segurança.</summary>
        static readonly Ilustracao AreaDados = new Ilustracao { nome = "dados_hd", equip = "_g", escala = 2, fundo = new Vector2(242, 144), esquerda = new Vector2(18, 256), direita = new Vector2(466, 256), casas = 7 };
        static readonly Ilustracao AreaRede = new Ilustracao { nome = "rede_hd", equip = "_g", escala = 2, fundo = new Vector2(240, 142), esquerda = new Vector2(16, 254), direita = new Vector2(464, 254), casas = 7 };

        PixelCanvas telaIlustrada;
        IsoDesenho dIlustrada;
        SpriteIso fundoIlustrado;
        string salaMontada;
        Ilustracao areaAtual;   // null: a sala do cargo

        bool TemIlustracao => true;
        Ilustracao Sala => areaAtual ?? Ilustracoes[Mathf.Min(E.Cargo, Ilustracoes.Length - 1)];

        /// <summary>O que o marcador "+ ..." compra nesta vista (null: o equipamento principal do cargo).</summary>
        public string ItemDoMarcador { get; private set; }

        /// <summary>Ponto do piso: gx corre ao longo da parede da direita, gy ao longo da da esquerda (em casas).</summary>
        Vector2Int IP(float gx, float gy, float z = 0)
        {
            var s = Sala;
            var p = s.fundo + (s.direita - s.fundo) / s.casas * gx + (s.esquerda - s.fundo) / s.casas * gy;
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y - z));
        }

        /// <summary>y do pé da parede da esquerda / da direita na coluna x (a primeira linha do piso fica logo abaixo).</summary>
        float PeDaParedeEsquerda(float x) => Mathf.Lerp(Sala.esquerda.y, Sala.fundo.y, (x - Sala.esquerda.x) / (Sala.fundo.x - Sala.esquerda.x));
        float PeDaParedeDireita(float x) => Mathf.Lerp(Sala.fundo.y, Sala.direita.y, (x - Sala.fundo.x) / (Sala.direita.x - Sala.fundo.x));

        // ---------------- Sprites do PixelLab ----------------

        static SpriteIso CarregarPixelLab(string nome, bool espelhar = false)
        {
            string chave = "pl/" + nome + (espelhar ? "|espelhado" : "");
            if (sprites.TryGetValue(chave, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Arte/PixelLab/" + nome);
            if (tex == null) return sprites[chave] = null;
            var px = ArteGerada.PixelsDeCimaParaBaixo(tex);
            if (espelhar)
            {
                var e = new Color32[px.Length];
                for (int y = 0; y < tex.height; y++)
                    for (int x = 0; x < tex.width; x++) e[y * tex.width + x] = px[y * tex.width + (tex.width - 1 - x)];
                px = e;
            }
            s = Preparar(px, tex.width, tex.height);
            // os LEDs do PixelLab são de um verde mais escuro que o da arte antiga: qualquer verde bem mais forte que o resto
            s.leds.Clear();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a > 0 && c.g > 110 && c.g > c.r + 40 && c.g > c.b + 40) s.leds.Add(i);
            }
            return sprites[chave] = s;
        }

        /// <summary>O mesmo sprite com os LEDs em vermelho: é assim que um servidor travado aparece.</summary>
        static SpriteIso Travada(string nome, bool espelhar)
        {
            string chave = "pl/" + nome + (espelhar ? "|espelhado" : "") + "|travada";
            if (sprites.TryGetValue(chave, out var s)) return s;
            var o = CarregarPixelLab(nome, espelhar);
            var px = (Color32[])o.px.Clone();
            foreach (int i in o.leds) px[i] = new Color32(255, 70, 80, 255);
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            s.leds.AddRange(o.leds);
            return sprites[chave] = s;
        }

        /// <summary>
        /// Hardware fora da garantia: o plástico e a pintura amarelam um pouco (os LEDs ficam como estão). Cache por sprite.
        /// </summary>
        static SpriteIso Envelhecido(SpriteIso o)
        {
            string chave = "velho|" + o.GetHashCode();
            if (sprites.TryGetValue(chave, out var s)) return s;
            var px = (Color32[])o.px.Clone();
            var leds = new HashSet<int>(o.leds);
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a == 0 || leds.Contains(i)) continue;
                // amarela mais o que é claro (plástico bege); o metal escuro do rack só um pouco
                float k = 0.25f * Mathf.Clamp((c.r + c.g + c.b) / 540f, 0.15f, 1f);
                px[i] = new Color32((byte)Mathf.Lerp(c.r, 196, k), (byte)Mathf.Lerp(c.g, 176, k), (byte)Mathf.Lerp(c.b, 112, k), c.a);
            }
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            s.leds.AddRange(o.leds);
            return sprites[chave] = s;
        }

        /// <summary>Linha mais baixa com pixel opaco: onde ficam os pés da pessoa no quadro.</summary>
        static int Pes(SpriteIso s)
        {
            for (int y = s.h - 1; y >= 0; y--)
                for (int x = 0; x < s.w; x++)
                    if (s.px[y * s.w + x].a > 0) return y;
            return s.h - 1;
        }

        /// <summary>Os quatro cantos da base de um objeto isométrico, lidos da imagem.</summary>
        struct Base { public Vector2Int esquerda, direita, frente, fundo; }
        static readonly Dictionary<SpriteIso, Base> bases = new Dictionary<SpriteIso, Base>();

        /// <summary>
        /// Esquerda e direita: o pixel mais baixo da primeira e da última coluna opaca. Frente: a linha mais baixa.
        /// O canto do fundo fecha o losango (esquerda + direita - frente).
        /// </summary>
        static Base BaseDe(SpriteIso s)
        {
            if (bases.TryGetValue(s, out var b)) return b;
            int MaisBaixo(int x) { for (int y = s.h - 1; y >= 0; y--) if (s.px[y * s.w + x].a > 0) return y; return -1; }
            int xe = 0, xd = s.w - 1;
            while (xe < s.w - 1 && MaisBaixo(xe) < 0) xe++;
            while (xd > 0 && MaisBaixo(xd) < 0) xd--;
            int yf = Pes(s);
            b.esquerda = new Vector2Int(xe, MaisBaixo(xe));
            b.direita = new Vector2Int(xd, MaisBaixo(xd));
            b.frente = new Vector2Int(s.frente, yf);
            b.fundo = b.esquerda + b.direita - b.frente;
            return bases[s] = b;
        }

        /// <summary>Canto de cima da imagem para o objeto encostar na parede da esquerda com o canto esquerdo da base na coluna x.</summary>
        Vector2Int NaParedeEsquerda(SpriteIso s, float x)
        {
            var b = BaseDe(s);
            return new Vector2Int(Mathf.RoundToInt(x) - b.esquerda.x, Mathf.RoundToInt(PeDaParedeEsquerda(x)) + 1 - b.esquerda.y);
        }

        /// <summary>O mesmo na parede da direita: o canto do fundo da base na coluna x.</summary>
        Vector2Int NaParedeDireita(SpriteIso s, float x)
        {
            var b = BaseDe(s);
            return new Vector2Int(Mathf.RoundToInt(x) - b.fundo.x, Mathf.RoundToInt(PeDaParedeDireita(x)) + 1 - b.fundo.y);
        }

        /// <summary>Quanto o objeto ocupa ao longo da parede da esquerda / da direita, em colunas.</summary>
        static int LarguraNaEsquerda(SpriteIso s) => BaseDe(s).fundo.x - BaseDe(s).esquerda.x;
        static int LarguraNaDireita(SpriteIso s) => BaseDe(s).direita.x - BaseDe(s).fundo.x;

        // ---------------- Montagem e desenho ----------------

        void MontarIlustrada()
        {
            salaMontada = Sala.nome;
            var tex = ArteGerada.Textura("Salas/" + Sala.nome);
            fundoIlustrado = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            telaIlustrada = new PixelCanvas(tex.width, tex.height);
            dIlustrada = new IsoDesenho(telaIlustrada);
            var r = ArteGerada.AreaOpaca(tex);
            areaArte = new RectInt(r.x - 2, tex.height - r.yMax - 2, r.width + 4, r.height + 4);
        }

        void DesenharIlustrada()
        {
            areaAtual = null;
            DesenharCena(() => { if (E.Cargo == 0) ArmarioIlustrado(); else if (E.Cargo == 1) SalinhaIlustrada(); else SalaGrandeIlustrada(); });
        }

        /// <summary>Uma das áreas atrás das portas (só existem da sala de racks em diante).</summary>
        void DesenharArea(Vista vista)
        {
            areaAtual = vista == Vista.Dados ? AreaDados : AreaRede;
            DesenharCena(vista == Vista.Dados ? (System.Action)SalaDeDados : SalaDeRede);
        }

        void DesenharCena(System.Action conteudo)
        {
            ItemDoMarcador = null;
            if (telaIlustrada == null || salaMontada != Sala.nome) MontarIlustrada();
            tela = telaIlustrada; d = dIlustrada;
            desenhandoArte = true;
            tela.Limpar(new Color32(0, 0, 0, 0));
            tela.Imagem(fundoIlustrado.px, fundoIlustrado.w, fundoIlustrado.h, 0, 0);

            tecnicoConsertando = false;
            pontosDeRonda.Clear();
            conteudo();
            PessoasIlustradas();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompraIlustrada();
            ChamadoIlustrado();
            DesenharDestaque();
            tela.Aplicar();
        }

        // ---------------- Peças que se repetem ----------------

        Vector2Int pontoDaTorre, pontoDoRack, pontoDoFiltro, pontoDoVentilador, pontoDoNoBreak, pontoDoAr, pontoDoEstagiario;
        Vector2Int pontoDoChamado;

        /// <summary>
        /// Torres em fila na parede da esquerda, da coluna x0 (frente) para o fundo, até caberem 'vagas'. Espelhadas: o painel
        /// fica virado para dentro da sala. Com mais torres do que vagas, uma placa com o total abre a loja de Compute.
        /// Retorna onde a próxima torre entraria (ou null, com a fila cheia).
        /// </summary>
        Vector2Int? TorresNaParede(float x0, int vagas, bool marcar)
        {
            string nome = "torre";
            var torre = CarregarPixelLab(nome, true);
            int passo = LarguraNaEsquerda(torre) + 2;
            Vector2Int Lugar(int i) => NaParedeEsquerda(torre, x0 + i * passo);
            var b = BaseDe(torre);
            int visiveis = Mathf.Min(E.Torres, vagas);
            for (int i = 0; i < visiveis; i++)
            {
                var l = Lugar(i);
                bool travado = E.Travado(i) || HdDaTorreQueimado(i);
                var s = travado ? Travada(nome, true) : torre;
                if (E.ForaDaGarantia) s = Envelhecido(s);
                int semente = i;
                fila.Add((-i * 0.01f, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));   // a da frente cobre a de trás
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, s.w, s.h), Tipo = "servidor:" + i, Px = s.px, Prof = -i * 0.01f, Nome = "Servidor torre " + (i + 1) });
                if (travado) Quebrado(l + new Vector2Int(b.frente.x, 0), l + b.frente, true);
                Ronda(l + b.frente, true);
            }
            if (E.Torres > vagas)
            {
                var l = Lugar(vagas - 1);
                Placas.Add(new Placa { Setor = "Compute", Nome = E.Torres + " torres", Pos = l + new Vector2Int(b.frente.x, 4), Cor = IsoGui.Cyan });
            }
            var ultima = Lugar(Mathf.Clamp(E.Torres - 1, 0, vagas - 1));
            pontoDaTorre = ultima + new Vector2Int(b.frente.x, b.frente.y / 2);
            if (E.Torres < vagas)
            {
                if (marcar) ContornoDaBase(torre, Lugar(E.Torres));
                return Lugar(E.Torres);
            }
            if (marcar) Marcador = ultima + new Vector2Int(b.frente.x + 8, b.frente.y + 4);
            return null;
        }

        /// <summary>Alerta piscando sobre o que quebrou e o técnico indo consertar (o primeiro quebrado chama o técnico).</summary>
        void Quebrado(Vector2Int topo, Vector2Int frente, bool ladoGx)
        {
            fila.Add((5, () => { if (Piscar()) tela.Texto("!", topo.x - 2, topo.y - 2, IsoDesenho.C("ff3b4e"), true, Sala.escala + 1); }));
            if (!tecnicoConsertando) { tecnicoConsertando = true; (lugarDoConserto, olharDoConserto) = NaFrenteDe(frente, ladoGx); }
        }

        /// <summary>Contorno tracejado da base de um objeto no lugar l: onde entra a próxima compra do equipamento principal.</summary>
        void ContornoDaBase(SpriteIso s, Vector2Int l)
        {
            var b = BaseDe(s);
            var a = l + b.esquerda; var c = l + b.fundo; var e = l + b.direita; var f = l + b.frente;
            fila.Add((-9, () =>   // no piso: embaixo de tudo
            {
                var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                foreach (var (p, q) in new[] { (a, c), (c, e), (e, f), (f, a) })
                {
                    int n = Mathf.Max(1, Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y)));
                    for (int i = 0; i <= n; i += 3) tela.Pixel(Mathf.RoundToInt(Mathf.Lerp(p.x, q.x, (float)i / n)), Mathf.RoundToInt(Mathf.Lerp(p.y, q.y, (float)i / n)), cor);
                }
            }));
            Marcador = new Vector2Int((a.x + e.x) / 2, (c.y + f.y) / 2);
        }

        void Caneca(string nome, Vector2Int base_)
        {
            var caneca = CarregarPixelLab(nome);
            if (caneca == null) return;
            int x = base_.x - caneca.w / 2, y = base_.y - caneca.h;
            fila.Add((1, () => DesenharSprite(caneca, x, y)));
            Alvos.Add(new Alvo { Area = new RectInt(x, y, caneca.w, caneca.h), Tipo = "cafe", Px = caneca.px, Prof = 1, Nome = "Café: renda em dobro" });
        }

        // ---------------- Armário (Técnico) ----------------

        void ArmarioIlustrado()
        {
            TorresNaParede(61, 7, true);
            Alvos.Add(new Alvo { Area = new RectInt(244, 128, 90, 108), Tipo = "equipamento", Prof = -20, Nome = "Mesa do técnico" });   // mesa da ilustração (atrás de tudo que fica em cima dela)
            Caneca("caneca", new Vector2Int(308, 176));
            BackupNaMesa(new Vector2Int(294, 182), new Vector2Int(262, 170));
            pontoDoVentilador = new Vector2Int(318, 112);
            var ventilador = CarregarPixelLab("ventilador");
            if (ventilador != null && E.Nivel(Catalogo.Ventilador) > 0)
                fila.Add((1, () => DesenharSprite(ventilador, pontoDoVentilador.x - ventilador.w / 2, pontoDoVentilador.y - ventilador.h / 2)));
            pontoDoFiltro = IP(3.85f, 1.5f);
            var filtro = CarregarPixelLab("filtro_linha");
            if (filtro != null && E.Nivel(Catalogo.FiltroDeLinha) > 0)
                fila.Add((2, () => DesenharSprite(filtro, pontoDoFiltro.x - filtro.w / 2, pontoDoFiltro.y - filtro.h + 2)));
            pontoDoChamado = new Vector2Int(286, 112);
        }

        // ---------------- Salinha (Sysadmin) ----------------

        const int RacksNaSalinha = 3, TorresNaSalinha = 5, NoBreaksNaSalinha = 2;

        void SalinhaIlustrada()
        {
            // parede da esquerda, da frente para o fundo: as torres que vieram do armário e depois os racks (o primeiro
            // rack fica junto da planta, no fundo)
            TorresNaParede(IP(0, 6.7f).x, TorresNaSalinha, false);
            Racks(IP(0, 1.7f).x, RacksNaSalinha);

            // embaixo da janela, entre a planta e a porta: os no-breaks (espelhados: o visor fica virado para a sala)
            var nobreak = CarregarPixelLab("nobreak", true);
            int nNoBreaks = Mathf.Min(E.Nivel(Catalogo.NoBreak), NoBreaksNaSalinha);
            for (int i = 0; i < NoBreaksNaSalinha; i++)
            {
                var l = NaParedeDireita(nobreak, IP(0.9f, 0).x + i * 20);   // a sombra no pé da imagem atrapalha medir a base: passo fixo
                if (i == 0) pontoDoNoBreak = l + new Vector2Int(nobreak.w / 2, nobreak.h / 2);
                if (i >= nNoBreaks) continue;
                int semente = i + 20;
                fila.Add((0.5f + i * 0.01f, () => DesenharSprite(nobreak, l.x, l.y, semente)));
                Ronda(l + BaseDe(nobreak).frente, false);
            }
            // ar-condicionado no alto da parede da esquerda, sobre as torres (espelhado: virado para a sala)
            var ar = CarregarPixelLab("ar_condicionado", true);
            pontoDoAr = IP(0, 4.6f, 92);
            if (ar != null && E.Nivel(Catalogo.ArCondicionado) > 0)
                fila.Add((1, () => DesenharSprite(ar, pontoDoAr.x - ar.w / 2, pontoDoAr.y - ar.h / 2)));

            Alvos.Add(new Alvo { Area = new RectInt(372, 130, 104, 120), Tipo = "equipamento", Prof = -20, Nome = "Mesa do técnico" });   // mesa da ilustração (atrás de tudo que fica em cima dela)
            Caneca("caneca", new Vector2Int(445, 192));
            BackupNaMesa(new Vector2Int(421, 201), new Vector2Int(400, 182));
            pontoDoChamado = new Vector2Int(424, 104);
        }

        /// <summary>
        /// Racks 42U na parede da esquerda, do fundo (coluna xFundo) para a frente. Um rack a cada VagasNoRack servidores 1U
        /// (o primeiro aparece vazio assim que o rack é comprado); os LEDs acendem conforme o rack enche. Racks com
        /// servidor travado ficam com os LEDs vermelhos.
        /// </summary>
        void Racks(float xFundo, int maximo, bool marcar = true)
        {
            string nome = "rack" + Sala.equip;
            var rack = CarregarPixelLab(nome, true);
            var b = BaseDe(rack);
            int passo = LarguraNaEsquerda(rack) + 2;
            Vector2Int Lugar(int i) => NaParedeEsquerda(rack, xFundo - i * passo);
            int vagas = Catalogo.VagasNoRack;
            int racks = E.TemRack ? Mathf.Clamp(Mathf.CeilToInt(E.ServidoresRack / (float)vagas), 1, maximo) : 0;
            bool travado = false;
            for (int s = E.Torres; s < E.TotalServidores; s++) travado |= E.Travado(s);
            for (int i = 0; i < racks; i++)
            {
                var l = Lugar(i);
                // o último rack mostra todos os servidores que sobram (mais de 3 racks cheios não cabem na salinha)
                int nesse = i == racks - 1 ? E.ServidoresRack - i * vagas : vagas;
                float cheio = Mathf.Clamp01(nesse / (float)vagas);
                bool quebrado = travado && i == 0;
                var s = quebrado ? Travada(nome, true) : rack;
                if (E.ForaDaGarantia) s = Envelhecido(s);
                fila.Add((-1.5f + i * 0.01f, () => DesenharRack(s, l, cheio, !quebrado)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, rack.w, rack.h), Tipo = "rack", Px = s.px, Prof = -1.5f + i * 0.01f, Nome = "Rack 42U: " + E.ServidoresRack + " servidores 1U" });
                if (quebrado) Quebrado(l + new Vector2Int(b.frente.x, 0), l + b.frente, true);
                Ronda(l + b.frente, true);
            }
            if (E.ServidoresRack > maximo * vagas)
            {
                var l = Lugar(maximo - 1);
                Placas.Add(new Placa { Setor = "Compute", Nome = E.ServidoresRack + " servidores 1U", Pos = l + new Vector2Int(b.frente.x, 4), Cor = IsoGui.Cyan });
            }
            // marcador: o lugar do primeiro rack (antes de comprar), o lugar do próximo rack quando o atual enche, ou o rack
            // que recebe o próximo servidor
            int proximo = E.TemRack ? Mathf.Min(E.ServidoresRack / vagas, maximo - 1) : 0;
            if (marcar && proximo >= racks) ContornoDaBase(rack, Lugar(proximo));
            else if (marcar) Marcador = Lugar(proximo) + new Vector2Int(b.frente.x, 6);
            pontoDoRack = Lugar(Mathf.Max(0, Mathf.Min(proximo, racks - 1))) + new Vector2Int(b.frente.x, rack.h / 2);
        }

        // ---------------- Sala de racks (Analista), DevOps e data center (SRE em diante) ----------------

        /// <summary>
        /// Cor dos LEDs por tipo de máquina nas fileiras: na vida real tudo isso é servidor de rack, então o rack é o mesmo
        /// e a cor diz o que ele faz.
        /// </summary>
        static readonly Color32 LedRackCheio = new Color32(110, 200, 70, 255), LedHypervisor = new Color32(190, 120, 255, 255),
            LedCi = new Color32(255, 210, 80, 255), LedContainers = new Color32(80, 160, 255, 255), LedK8s = new Color32(70, 230, 230, 255),
            LedPico = new Color32(255, 150, 50, 255), LedBalanceador = new Color32(240, 240, 255, 255), LedQuebrado = new Color32(255, 70, 80, 255),
            LedStorage = new Color32(200, 225, 255, 255), LedFita = new Color32(255, 120, 200, 255);

        static SpriteIso ComLeds(string nome, Color32 cor)
        {
            string chave = "pl/" + nome + "|leds" + cor.r + "," + cor.g + "," + cor.b;
            if (sprites.TryGetValue(chave, out var s)) return s;
            var o = CarregarPixelLab(nome);
            var px = (Color32[])o.px.Clone();
            foreach (int i in o.leds) px[i] = cor;
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            s.leds.AddRange(o.leds);
            return sprites[chave] = s;
        }

        /// <summary>
        /// Trechos livres do pé da parede da direita em cada sala grande (sem portas): equipamentos da parede, em ordem.
        /// Fileiras: profundidade (gy) de cada fileira de racks no piso técnico, do fundo para a frente.
        /// </summary>
        (float de, float ate)[] TrechosDaParede => E.Cargo >= 4 ? new[] { ((float)IP(0.4f, 0).x, (float)IP(2.3f, 0).x), ((float)IP(4.4f, 0).x, (float)IP(11.7f, 0).x) } : new[] { ((float)IP(3.7f, 0).x, (float)IP(9.7f, 0).x) };
        float[] Fileiras => E.Cargo >= 4 ? new[] { 2.8f, 5.0f, 7.2f, 9.4f } : new[] { 2.8f, 5.0f, 7.2f };
        const float InicioDaFileira = 2.8f;

        void SalaGrandeIlustrada()
        {
            int cargo = E.Cargo;
            string equip = Sala.equip;

            // parede da direita: racks 1U, no-breaks e as torres antigas, cada um no próximo trecho livre
            var trechos = TrechosDaParede;
            int trecho = 0;
            float x = trechos[0].de;
            bool Cabe(int largura)
            {
                while (trecho < trechos.Length && x + largura > trechos[trecho].ate) { trecho++; if (trecho < trechos.Length) x = trechos[trecho].de; }
                return trecho < trechos.Length;
            }

            // racks 1U (até 2), no-breaks (até 3) e torres (até 4), todos com a frente virada para a sala
            var rack = CarregarPixelLab("rack" + equip);
            int vagas = Catalogo.VagasNoRack;
            int racks1u = E.TemRack ? Mathf.Clamp(Mathf.CeilToInt(E.ServidoresRack / (float)vagas), 1, 2) : 0;
            bool travado1u = false;
            for (int s = E.Torres; s < E.TotalServidores; s++) travado1u |= E.Travado(s);
            for (int i = 0; i < racks1u && Cabe(LarguraNaDireita(rack)); i++)
            {
                var l = NaParedeDireita(rack, x);
                x += LarguraNaDireita(rack) + 2;
                int nesse = i == racks1u - 1 ? E.ServidoresRack - i * vagas : vagas;
                float cheio = Mathf.Clamp01(nesse / (float)vagas);
                bool quebrado = travado1u && i == 0;
                var s = quebrado ? Travada("rack" + equip, false) : rack;
                if (E.ForaDaGarantia) s = Envelhecido(s);
                float prof = -8 + l.x * 0.001f;
                fila.Add((prof, () => DesenharRack(s, l, cheio, !quebrado)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, rack.w, rack.h), Tipo = "rack", Px = s.px, Prof = prof, Nome = "Rack 42U: " + E.ServidoresRack + " servidores 1U" });
                if (quebrado) Quebrado(l + new Vector2Int(BaseDe(rack).frente.x, 0), l + BaseDe(rack).frente, false);
                Ronda(l + BaseDe(rack).frente, false);
                if (i == 0) pontoDoRack = l + new Vector2Int(rack.w / 2, rack.h / 2);
            }
            var nobreak = CarregarPixelLab("nobreak", true);
            for (int i = 0; i < Mathf.Min(E.Nivel(Catalogo.NoBreak), 3) && Cabe(18); i++)
            {
                var l = NaParedeDireita(nobreak, x);
                x += 20;
                if (i == 0) pontoDoNoBreak = l + new Vector2Int(nobreak.w / 2, nobreak.h / 2);
                int semente = i + 20;
                fila.Add((-8 + l.x * 0.001f, () => DesenharSprite(nobreak, l.x, l.y, semente)));
                Ronda(l + BaseDe(nobreak).frente, false);
            }
            var torre = CarregarPixelLab("torre");
            var bt = BaseDe(torre);
            for (int i = 0; i < Mathf.Min(E.Torres, 4) && Cabe(LarguraNaDireita(torre)); i++)
            {
                var l = NaParedeDireita(torre, x);
                x += LarguraNaDireita(torre) + 2;
                bool travado = E.Travado(i) || HdDaTorreQueimado(i);
                var s = travado ? Travada("torre", false) : torre;
                if (E.ForaDaGarantia) s = Envelhecido(s);
                int semente = i;
                fila.Add((-8 + l.x * 0.001f, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, torre.w, torre.h), Tipo = "servidor:" + i, Px = s.px, Prof = -8 + l.x * 0.001f, Nome = "Servidor torre " + (i + 1) });
                if (travado) Quebrado(l + new Vector2Int(bt.frente.x, 0), l + bt.frente, false);
                Ronda(l + bt.frente, false);
                pontoDaTorre = l + new Vector2Int(torre.w / 2, torre.h / 2);
            }

            // piso técnico: uma fileira por tipo de máquina (no Analista, os racks cheios ocupam todas)
            var fileiras = Fileiras;
            int f = 0;
            List<Vector2Int> Fileira() => f < fileiras.Length ? LugaresNaFileira(fileiras[f++]) : new List<Vector2Int>();
            var lugaresRacks = Fileira();
            if (cargo == 2) while (f < fileiras.Length) lugaresRacks.AddRange(Fileira());
            string principal = cargo == 2 ? Catalogo.RackCheio : cargo == 3 ? Catalogo.Containers : Catalogo.NoKubernetes;

            // portas das áreas: dados e backup (direita) e rede e segurança (esquerda)
            PortasDasAreas();

            var rackCheio = ComLeds("rack" + equip, LedRackCheio);
            for (int i = 0; i < Mathf.Min(lugaresRacks.Count, E.RacksCheios); i++) MaquinaNaFileira(rackCheio, lugaresRacks[i], "equipamento", "Rack cheio");
            if (principal == Catalogo.RackCheio) MarcarProxima(lugaresRacks, E.RacksCheios);
            if (lugaresRacks.Count > 0) pontoDoRackCheio = lugaresRacks[Mathf.Clamp(E.RacksCheios - 1, 0, lugaresRacks.Count - 1)];

            if (cargo >= 3)
            {
                // virtualização: hypervisors (roxo) e o servidor de CI (amarelo)
                var lugares = Fileira();
                int k = 0;
                for (int i = 0; i < Mathf.Min(3, E.NivelHypervisor) && k < lugares.Count; i++, k++) MaquinaNaFileira(ComLeds("rack" + equip, LedHypervisor), lugares[k], "equipamento", "Hypervisor");
                if (E.TemCi && k < lugares.Count) MaquinaNaFileira(ComLeds("rack" + equip, LedCi), lugares[k++], "containers", "Servidor de CI");
                if (lugares.Count > 0) pontoDoHypervisor = lugares[0];

                // containers (azul; o primeiro fica vermelho com o deploy quebrado)
                var hosts = Fileira();
                for (int i = 0; i < Mathf.Min(hosts.Count, E.HostsContainers); i++)
                {
                    bool quebrado = i == 0 && E.DeployQuebrado;
                    MaquinaNaFileira(ComLeds("rack" + equip, quebrado ? LedQuebrado : LedContainers), hosts[i], "containers", quebrado ? "Host de containers: deploy quebrado" : "Host de containers", quebrado);
                }
                if (principal == Catalogo.Containers) MarcarProxima(hosts, E.HostsContainers);
                if (hosts.Count > 0) pontoDosContainers = hosts[Mathf.Clamp(E.HostsContainers - 1, 0, hosts.Count - 1)];
            }
            if (cargo >= 4)
            {
                // o cluster: nós Kubernetes (ciano; laranja num pico sem escala) e o balanceador na ponta
                var nos = Fileira();
                bool pico = E.EmPico && !E.PicoFoiEscalado;
                int vagasK8s = nos.Count - (E.TemBalanceador ? 1 : 0);
                for (int i = 0; i < Mathf.Min(vagasK8s, E.NosKubernetes); i++) MaquinaNaFileira(ComLeds("rack" + equip, pico ? LedPico : LedK8s), nos[i], "k8s", pico ? "Nó Kubernetes: no pico, escale!" : "Nó Kubernetes");
                if (E.TemBalanceador && nos.Count > 0) MaquinaNaFileira(ComLeds("rack" + equip, LedBalanceador), nos[nos.Count - 1], "equipamento", "Balanceador");
                if (principal == Catalogo.NoKubernetes) MarcarProxima(nos, E.NosKubernetes, vagasK8s);
                if (nos.Count > 0) pontoDosNos = nos[Mathf.Clamp(E.NosKubernetes - 1, 0, nos.Count - 1)];
            }

            // canto do escritório (Analista) ou do NOC (SRE em diante), que já vem na ilustração
            if (cargo >= 4)
            {
                Alvos.Add(new Alvo { Area = new RectInt(44, 251, 116, 116), Tipo = "noc", Prof = -20, Nome = "NOC" });
                pontoDoChamado = new Vector2Int(94, 235);
            }
            else
            {
                Alvos.Add(new Alvo { Area = new RectInt(47, 230, 112, 87), Tipo = "equipamento", Prof = -20, Nome = "Mesa do técnico" });
                Caneca("caneca", new Vector2Int(137, 250));
                BackupNaMesa(new Vector2Int(90, 274), new Vector2Int(65, 262));
                pontoDoChamado = new Vector2Int(98, 215);
            }
        }

        Vector2Int pontoDoRackCheio, pontoDoHypervisor, pontoDosContainers, pontoDosNos;
        Vector2Int pontoDoStorage, pontoDaFita, pontoDoHd, pontoDoNas;

        /// <summary>Antes do storage, o disco que queima é o HD da primeira torre: ela fica com o alerta até trocarem.</summary>
        bool HdDaTorreQueimado(int torre) => torre == 0 && E.DiscoQueimado && E.NivelStorage == 0;

        /// <summary>
        /// O começo da linha de backup, em cima da mesa: o HD externo (caixinha preta com LED azul) e o NAS (caixa de dois
        /// discos com LEDs verdes). São pequenos, então são desenhados aqui mesmo, pixel a pixel.
        /// </summary>
        void BackupNaMesa(Vector2Int hd, Vector2Int nas)
        {
            pontoDoHd = hd + new Vector2Int(0, -4);
            pontoDoNas = nas + new Vector2Int(0, -6);
            var contorno = new Color32(20, 20, 28, 255);
            if (E.Nivel(Catalogo.HdExterno) > 0)
                fila.Add((1.1f, () =>
                {
                    tela.Ret(hd.x - 4, hd.y - 4, 9, 4, contorno);
                    tela.Ret(hd.x - 3, hd.y - 3, 7, 2, new Color32(60, 62, 74, 255));
                    tela.Ret(hd.x - 3, hd.y - 3, 7, 1, new Color32(96, 100, 118, 255));
                    tela.Pixel(hd.x + 2, hd.y - 2, Piscar(0.7f) ? new Color32(90, 190, 255, 255) : new Color32(40, 90, 140, 255));
                }));
            if (E.Nivel(Catalogo.Nas) > 0)
                fila.Add((1.1f, () =>
                {
                    tela.Ret(nas.x - 5, nas.y - 12, 11, 12, contorno);
                    tela.Ret(nas.x - 4, nas.y - 11, 9, 10, new Color32(48, 52, 66, 255));
                    tela.Ret(nas.x - 4, nas.y - 11, 9, 1, new Color32(92, 98, 120, 255));
                    for (int b = 0; b < 2; b++)
                    {
                        tela.Ret(nas.x - 3 + b * 4, nas.y - 9, 3, 7, new Color32(30, 32, 42, 255));
                        tela.Pixel(nas.x - 2 + b * 4, nas.y - 8, Piscar(0.5f + b * 0.2f) ? new Color32(110, 220, 90, 255) : new Color32(50, 110, 50, 255));
                    }
                }));
        }

        /// <summary>Cantos de cima das imagens dos racks de uma fileira em gy, um encostado no outro ao longo de gx.</summary>
        List<Vector2Int> LugaresNaFileira(float gy)
        {
            var rack = CarregarPixelLab("rack" + Sala.equip);
            var b = BaseDe(rack);
            var passo = b.frente - b.esquerda;   // a face com os servidores corre ao longo de gx
            var inicio = IP(InicioDaFileira, gy) - b.fundo;
            float largura = (IP(Sala.casas - 0.3f, gy).x - IP(InicioDaFileira, gy).x);
            int n = Mathf.Max(0, Mathf.FloorToInt(largura / Mathf.Max(1, passo.x)));
            var lugares = new List<Vector2Int>();
            for (int i = 0; i < n; i++) lugares.Add(inicio + passo * i);
            return lugares;
        }

        /// <summary>Uma máquina numa fileira (profundidade pela linha da tela: quem está mais embaixo fica na frente).</summary>
        void MaquinaNaFileira(SpriteIso s, Vector2Int l, string clique, string nome, bool quebrado = false)
        {
            float prof = -5 + (l.y + s.h) * 0.01f + l.x * 0.0001f;
            fila.Add((prof, () => DesenharRack(s, l, 1, !quebrado)));
            Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, s.w, s.h), Tipo = clique, Px = s.px, Prof = prof, Nome = nome });
            if (quebrado) Quebrado(l + new Vector2Int(BaseDe(s).frente.x, 0), l + BaseDe(s).frente, false);
            Ronda(l + BaseDe(s).frente, false);
        }

        /// <summary>Contorno no próximo lugar livre da fileira do equipamento principal (ou o botão na frente, com ela cheia).</summary>
        void MarcarProxima(List<Vector2Int> lugares, int ocupados, int limite = -1)
        {
            if (lugares.Count == 0) return;
            if (limite < 0) limite = lugares.Count;
            var rack = CarregarPixelLab("rack" + Sala.equip);
            if (ocupados < limite) ContornoDaBase(rack, lugares[ocupados]);
            else Marcador = lugares[limite - 1] + BaseDe(rack).frente + new Vector2Int(0, 6);
        }

        /// <summary>Rack com os LEDs acesos de cima para baixo até 'cheio' (0 a 1); os outros ficam apagados.</summary>
        void DesenharRack(SpriteIso s, Vector2Int l, float cheio, bool piscar)
        {
            tela.Imagem(s.px, s.w, s.h, l.x, l.y);
            if (s.leds.Count == 0) return;
            // altura dos LEDs: os acesos vão do mais alto até a fração 'cheio' da coluna
            int topo = int.MaxValue, fundo = 0;
            foreach (int i in s.leds) { topo = Mathf.Min(topo, i / s.w); fundo = Mathf.Max(fundo, i / s.w); }
            float corte = topo + (fundo - topo + 1) * cheio;
            bool apagaAgora = piscar && Mathf.Sin(t * 5f + l.x) > 0.6f;
            var apagado = new Color32(30, 40, 30, 255);
            foreach (int i in s.leds)
            {
                bool aceso = i / s.w < corte && !apagaAgora;
                if (!aceso) tela.Pixel(l.x + i % s.w, l.y + i / s.w, apagado);
            }
        }

        // ---------------- Pessoas: rotina ----------------
        //
        // Ninguém anda à toa. O técnico fica na mesa (de pé, virado para o monitor) e só levanta com motivo: consertar o
        // que quebrou, ou de vez em quando dar uma olhada num equipamento e voltar. Estagiário e engenheiros fazem ronda:
        // vão até um equipamento, param olhando para ele alguns segundos e escolhem outro. O caminho segue os corredores
        // (nas salas pequenas, a passagem da frente; nas grandes, o corredor ao lado das fileiras), então ninguém atravessa
        // máquina nem móvel.

        /// <summary>Uma pessoa na sala: onde está (em casas), para onde vai e o que está fazendo.</summary>
        class Trabalhador
        {
            public string quem;                    // prefixo dos sprites ("tecnico")
            public Color32? camisa;                // engenheiros: o técnico com outra camisa
            public Vector2 pos;
            public readonly List<Vector2> caminho = new List<Vector2>();
            public string olhar = "se";            // direção parada: se, sw, ne, nw
            public string olhandoDestino = "se";   // como fica ao chegar
            public float paradaAte;                // até quando fica parada no destino
            public bool naMesa;                    // técnico: está trabalhando na mesa
            public bool consertando;
            public float fase;
        }

        readonly Dictionary<string, Trabalhador> pessoas = new Dictionary<string, Trabalhador>();
        readonly System.Random sorteioDasPessoas = new System.Random();
        string salaDasPessoas;
        float ultimoT = -1;

        /// <summary>Lugares de ronda montados no desenho: onde ficar (em casas) e para onde olhar.</summary>
        readonly List<(Vector2 lugar, string olhar)> pontosDeRonda = new List<(Vector2, string)>();

        /// <summary>Onde o técnico conserta o que quebrou (em casas) e para onde olha.</summary>
        Vector2 lugarDoConserto;
        string olharDoConserto = "nw";

        /// <summary>Mesa de trabalho do técnico em cada sala (em casas) e para onde ele olha nela.</summary>
        (Vector2 lugar, string olhar) Mesa =>
            E.Cargo == 0 ? (new Vector2(2.0f, 1.55f), "ne")     // ao lado da cadeira, virado para o monitor
            : E.Cargo == 1 ? (new Vector2(4.6f, 1.3f), "ne")
            : E.Cargo <= 3 ? (new Vector2(1.7f, 7.4f), "nw")
            : (new Vector2(2.4f, 9.6f), "nw");   // na frente do NOC, fora das fileiras

        /// <summary>Velocidade de quem anda, em casas por segundo (casas menores nas salas grandes).</summary>
        float Velocidade => E.Cargo == 0 ? 0.55f : 0.65f;

        /// <summary>Ponto da tela (pixel da ilustração) para casas: o inverso de IP.</summary>
        Vector2 Grade(Vector2 p)
        {
            var s = Sala;
            Vector2 a = (s.direita - s.fundo) / s.casas, b = (s.esquerda - s.fundo) / s.casas, d = p - s.fundo;
            float det = a.x * b.y - a.y * b.x;
            return new Vector2((d.x * b.y - d.y * b.x) / det, (a.x * d.y - a.y * d.x) / det);
        }

        /// <summary>
        /// O lugar na frente de um objeto, pelo canto da frente da base dele. Encostado na parede da esquerda (ladoGx), a
        /// pessoa fica ao lado dele olhando para noroeste; na parede da direita ou numa fileira, fica na frente olhando para
        /// nordeste.
        /// </summary>
        (Vector2 lugar, string olhar) NaFrenteDe(Vector2Int frente, bool ladoGx)
        {
            var g = Grade(frente);
            return ladoGx ? (g + new Vector2(0.4f, -0.2f), "nw") : (g + new Vector2(-0.2f, 0.4f), "ne");
        }

        void Ronda(Vector2Int frente, bool ladoGx) => pontosDeRonda.Add(NaFrenteDe(frente, ladoGx));

        /// <summary>Caminho pelos corredores: nunca corta o meio das fileiras nem passa pelos móveis.</summary>
        void Rota(Trabalhador p, Vector2 destino)
        {
            p.caminho.Clear();
            if (areaAtual == null && E.Cargo >= 2)
            {
                // salas grandes: troca de corredor pelo corredor ao lado das fileiras
                float lateral = InicioDaFileira - 0.6f;
                if (Mathf.Abs(p.pos.y - destino.y) > 0.05f) { p.caminho.Add(new Vector2(lateral, p.pos.y)); p.caminho.Add(new Vector2(lateral, destino.y)); }
            }
            else
            {
                // salas pequenas: pela passagem da frente
                float frente = E.Cargo == 0 ? 2.9f : areaAtual != null ? 2.6f : 2.7f;
                if (Mathf.Abs(p.pos.x - destino.x) > 0.05f) { p.caminho.Add(new Vector2(p.pos.x, frente)); p.caminho.Add(new Vector2(destino.x, frente)); }
            }
            p.caminho.Add(destino);
        }

        void PessoasIlustradas()
        {
            float dt = ultimoT < 0 ? 0 : Mathf.Clamp(t - ultimoT, 0, 0.5f);
            ultimoT = t;
            if (salaDasPessoas != Sala.nome) { pessoas.Clear(); salaDasPessoas = Sala.nome; }

            string tecnico = "tecnico";
            var quemTem = new List<string>();
            if (areaAtual != null) quemTem.Add("engenheiro0");   // nas áreas, um engenheiro fazendo a ronda
            else
            {
                quemTem.Add("tecnico");
                if (E.TemEstagiario) quemTem.Add("estagiario");
                for (int i = 0; i < (E.Cargo >= 2 ? Mathf.Min(E.Cargo - 1, 3) : 0); i++) quemTem.Add("engenheiro" + i);
            }
            foreach (var chave in new List<string>(pessoas.Keys)) if (!quemTem.Contains(chave)) pessoas.Remove(chave);

            foreach (var chave in quemTem)
            {
                if (!pessoas.TryGetValue(chave, out var p))
                {
                    p = new Trabalhador { fase = (float)sorteioDasPessoas.NextDouble() };
                    p.quem = chave == "estagiario" ? "estagiario" : tecnico;
                    if (chave.StartsWith("engenheiro")) p.camisa = CamisasDosEngenheiros[chave[chave.Length - 1] - '0'];
                    // começa no lugar de trabalho (técnico) ou num ponto de ronda
                    if (chave == "tecnico") { p.pos = Mesa.lugar; p.olhar = Mesa.olhar; p.naMesa = true; p.paradaAte = t + 8 + (float)sorteioDasPessoas.NextDouble() * 15; }
                    else if (pontosDeRonda.Count > 0)
                    {
                        var r = pontosDeRonda[sorteioDasPessoas.Next(pontosDeRonda.Count)];
                        p.pos = r.lugar; p.olhar = r.olhar; p.paradaAte = t + 2 + (float)sorteioDasPessoas.NextDouble() * 4;
                    }
                    else p.pos = areaAtual != null ? new Vector2(Sala.casas / 2f, Sala.casas / 2f) : Mesa.lugar + new Vector2(0.8f, 0.8f);
                    pessoas[chave] = p;
                }
                if (chave == "tecnico") PensarTecnico(p); else PensarRonda(p);
                Andar(p, dt);
                DesenharPessoa(p, chave == "tecnico");
                if (chave == "estagiario") pontoDoEstagiario = IP(p.pos.x, p.pos.y);
            }
        }

        /// <summary>O técnico: conserta o que quebrou; senão trabalha na mesa e de vez em quando vai olhar um equipamento.</summary>
        void PensarTecnico(Trabalhador p)
        {
            if (tecnicoConsertando)
            {
                // indo para outro conserto (ou ainda não foi): refaz o caminho
                var destino = p.caminho.Count > 0 ? p.caminho[p.caminho.Count - 1] : p.pos;
                if (!p.consertando || (destino - lugarDoConserto).sqrMagnitude > 0.01f)
                {
                    p.consertando = true; p.naMesa = false;
                    Rota(p, lugarDoConserto); p.olhandoDestino = olharDoConserto;
                }
                p.paradaAte = t + 1;
                return;
            }
            if (p.consertando)
            {
                // consertou: volta para a mesa
                p.consertando = false;
                VoltarParaAMesa(p);
                return;
            }
            if (p.caminho.Count > 0 || t < p.paradaAte) return;
            if (p.naMesa && pontosDeRonda.Count > 0)
            {
                // levanta para dar uma olhada num equipamento
                var r = pontosDeRonda[sorteioDasPessoas.Next(pontosDeRonda.Count)];
                p.naMesa = false;
                Rota(p, r.lugar); p.olhandoDestino = r.olhar;
                p.paradaAte = float.MaxValue;   // definido ao chegar
            }
            else VoltarParaAMesa(p);
        }

        void VoltarParaAMesa(Trabalhador p)
        {
            p.naMesa = true;
            Rota(p, Mesa.lugar); p.olhandoDestino = Mesa.olhar;
            p.paradaAte = float.MaxValue;
        }

        /// <summary>Ronda: escolhe um equipamento, vai até ele, fica olhando alguns segundos e escolhe outro.</summary>
        void PensarRonda(Trabalhador p)
        {
            if (p.caminho.Count > 0 || t < p.paradaAte || pontosDeRonda.Count == 0) return;
            var r = pontosDeRonda[sorteioDasPessoas.Next(pontosDeRonda.Count)];
            Rota(p, r.lugar); p.olhandoDestino = r.olhar;
            p.paradaAte = float.MaxValue;
        }

        /// <summary>Anda pelo caminho (um eixo de cada vez); ao chegar, vira para o destino e fica parada um tempo.</summary>
        void Andar(Trabalhador p, float dt)
        {
            float passo = Velocidade * dt;
            while (p.caminho.Count > 0 && passo > 0)
            {
                var alvo = p.caminho[0];
                var d = alvo - p.pos;
                float dist = d.magnitude;
                if (dist < 0.001f) { p.caminho.RemoveAt(0); continue; }
                // direção na tela: gx crescendo é sudeste, gy crescendo é sudoeste
                p.olhar = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x > 0 ? "se" : "nw") : (d.y > 0 ? "sw" : "ne");
                if (passo >= dist) { p.pos = alvo; passo -= dist; p.caminho.RemoveAt(0); }
                else { p.pos += d / dist * passo; passo = 0; }
                if (p.caminho.Count == 0)
                {
                    p.olhar = p.olhandoDestino;
                    if (p.paradaAte == float.MaxValue)
                        p.paradaAte = t + (p.naMesa ? 15 + (float)sorteioDasPessoas.NextDouble() * 25 : 3 + (float)sorteioDasPessoas.NextDouble() * 4);
                }
            }
        }

        /// <summary>
        /// Sprite de uma pessoa virada para uma das quatro diagonais. O PixelLab deu sudeste e noroeste; sudoeste e
        /// nordeste são os mesmos espelhados.
        /// </summary>
        SpriteIso Pose(string quem, string dir, bool andando, float fase)
        {
            bool espelhar = dir == "sw" || dir == "ne";
            string lado = dir == "sw" ? "se" : dir == "ne" ? "nw" : dir;
            if (andando)
            {
                int n = 0;
                while (n < 8 && CarregarPixelLab(quem + "_" + lado + "_" + n) != null) n++;
                if (n > 0) return CarregarPixelLab(quem + "_" + lado + "_" + (Mathf.FloorToInt(t * 6f + fase * 10) % n), espelhar);
            }
            return CarregarPixelLab(quem + "_" + lado, espelhar);
        }

        void DesenharPessoa(Trabalhador p, bool ehTecnico)
        {
            bool andando = p.caminho.Count > 0;
            SpriteIso s;
            float pulo = 0;
            if (ehTecnico && !andando && !p.consertando && t - ultimaCompraEm < 1.5f)
            {
                // comemora a compra: pulinhos olhando para a frente
                s = CarregarPixelLab(p.quem + "_s") ?? Pose(p.quem, "se", false, p.fase);
                pulo = Mathf.Abs(Mathf.Sin((t - ultimaCompraEm) * 9f)) * 8f;
            }
            else s = Pose(p.quem, p.olhar, andando, p.fase);
            if (s == null) return;
            if (p.camisa.HasValue) s = Recolorido(s, p.camisa.Value);
            PessoaIlustrada(s, p.quem, IP(p.pos.x, p.pos.y), pulo, areaAtual == null && E.Cargo >= 2);
        }

        /// <summary>Cor da camisa de cada engenheiro de campo (o técnico com outra camisa).</summary>
        static readonly Color32[] CamisasDosEngenheiros = { new Color32(232, 140, 60, 255), new Color32(150, 110, 200, 255), new Color32(200, 70, 80, 255) };

        /// <summary>
        /// O quadro com a camisa trocada: pixels azul-esverdeados vivos do tronco (a calça jeans é mais escura e fica
        /// mais embaixo) ganham a cor nova, mantendo o claro e o escuro de cada um.
        /// </summary>
        static SpriteIso Recolorido(SpriteIso o, Color32 cor)
        {
            string chave = "camisa|" + o.GetHashCode() + "|" + cor.r + "," + cor.g + "," + cor.b;
            if (sprites.TryGetValue(chave, out var s)) return s;
            int topo = 0;
            while (topo < o.h - 1 && !LinhaTemPixel(o, topo)) topo++;
            int pes = Pes(o), altura = pes - topo;
            var px = (Color32[])o.px.Clone();
            for (int y = topo + (int)(altura * 0.3f); y < topo + (int)(altura * 0.66f); y++)
                for (int x = 0; x < o.w; x++)
                {
                    var c = px[y * o.w + x];
                    if (c.a == 0) continue;
                    Color.RGBToHSV(c, out float h, out float sat, out float v);
                    if (h < 0.45f || h > 0.6f || sat < 0.3f || v < 0.35f) continue;
                    float k = Mathf.Clamp(v / 0.75f, 0.4f, 1.3f);
                    px[y * o.w + x] = new Color32((byte)Mathf.Min(255, cor.r * k), (byte)Mathf.Min(255, cor.g * k), (byte)Mathf.Min(255, cor.b * k), c.a);
                }
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            return sprites[chave] = s;
        }

        static bool LinhaTemPixel(SpriteIso s, int y)
        {
            for (int x = 0; x < s.w; x++) if (s.px[y * s.w + x].a > 0) return true;
            return false;
        }

        /// <summary>
        /// Trabalhador com os pés no ponto p (sombra no chão; z levanta o corpo, para os pulos). Entre as máquinas (entreMaquinas)
        /// ela entra na mesma ordem de profundidade das fileiras; senão fica por cima de tudo.
        /// </summary>
        void PessoaIlustrada(SpriteIso s, string quem, Vector2Int p, float z, bool entreMaquinas = false)
        {
            // os pés pela pose parada: os quadros da caminhada dividem a mesma tela, então a pessoa não pula a cada passo
            int pes = Pes(CarregarPixelLab(quem + "_se") ?? s);
            int sombra = s.w > 90 ? 9 : s.w > 60 ? 6 : 4;
            fila.Add((entreMaquinas ? -5 + p.y * 0.01f : 10 + p.y * 0.001f, () =>
            {
                tela.Ret(p.x - sombra, p.y - 1, sombra * 2, 3, new Color32(20, 20, 40, 90));
                tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - pes - Mathf.RoundToInt(z));
            }));
        }

        // ---------------- Efeitos ----------------

        Vector2Int? LugarDoItemIlustrado(string id)
        {
            switch (id)
            {
                case Catalogo.Servidor: case Catalogo.Ssd: case Catalogo.Ventoinha: case Catalogo.PastaTermica: return pontoDaTorre;
                case Catalogo.Rack: case Catalogo.Servidor1U: case Catalogo.CabosOrganizados: case Catalogo.Firmware: return pontoDoRack;
                case Catalogo.FiltroDeLinha: return pontoDoFiltro + new Vector2Int(0, -6);
                case Catalogo.Ventilador: return pontoDoVentilador;
                case Catalogo.NoBreak: return pontoDoNoBreak;
                case Catalogo.ArCondicionado: return pontoDoAr;
                case Catalogo.Storage: return pontoDoStorage;
                case Catalogo.Backup: return pontoDaFita;
                case Catalogo.HdExterno: return pontoDoHd;
                case Catalogo.Nas: return pontoDoNas;
                case Catalogo.RackCheio: case Catalogo.PisoElevado: return pontoDoRackCheio + new Vector2Int(20, 20);
                case Catalogo.Hypervisor: case Catalogo.Link10G: return pontoDoHypervisor + new Vector2Int(20, 20);
                case Catalogo.Containers: case Catalogo.ServidorCi: case Catalogo.ImagensEnxutas: case Catalogo.CacheRedis: return pontoDosContainers + new Vector2Int(20, 20);
                case Catalogo.NoKubernetes: case Catalogo.Balanceador: case Catalogo.ServiceMesh: return pontoDosNos + new Vector2Int(20, 20);
                case Catalogo.Estagiario: return pontoDoEstagiario + new Vector2Int(0, -30);
                default: return null;
            }
        }

        void FaiscasDaCompraIlustrada()
        {
            float idade = t - ultimaCompraEm;
            if (idade > 1.2f || ultimaCompra == null) return;
            var lugar = LugarDoItemIlustrado(ultimaCompra);
            if (lugar == null) return;
            var centro = lugar.Value;
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 0.449f + i * i * 0.07f, raio = (4 + idade * (26 + (i % 4) * 7)) * Sala.escala;
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 14 * Sala.escala));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 2 * Sala.escala, 2 * Sala.escala, cor);
            }
        }

        /// <summary>O chamado urgente: um papel flutuando sobre o monitor, com o tempo que falta embaixo.</summary>
        void ChamadoIlustrado()
        {
            if (!E.TemChamado) return;
            float bob = Mathf.Sin(t * 4) * 2;
            var p = new Vector2Int(pontoDoChamado.x, Mathf.RoundToInt(pontoDoChamado.y + bob));
            string[] papel =
            {
                "yyyyyyyyy.", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy",
            };
            int k = Sala.escala;   // nas salas dobradas o papel também dobra
            tela.Ret(p.x - 7 * k, p.y - 13 * k, 14 * k, 13 * k, IsoDesenho.C("1b1a2e"));
            for (int y = 0; y < papel.Length; y++)
                for (int x = 0; x < papel[y].Length; x++)
                    if (papel[y][x] != '.') tela.Ret(p.x + (x - 6) * k, p.y + (y - 12) * k, k, k, IsoDesenho.C(papel[y][x] == 'y' ? "fdf6e3" : "7d82ad"));
            tela.Ret(p.x + 3 * k, p.y - 12 * k, 2 * k, 2 * k, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)E.FracaoDoPrazoDoChamado;
            tela.Ret(p.x - 8 * k, p.y + 2 * k, 16 * k, 2 * k, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 8 * k, p.y + 2 * k, Mathf.RoundToInt(16 * k * resta), 2 * k, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 10 * k, p.y - 16 * k, 20 * k, 22 * k), Tipo = "chamado", Prof = 100, Nome = "Help desk: atender o chamado mais urgente" });
        }
    }
}
