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
    /// mesa ocupam o resto da parede da direita) e o ar-condicionado no alto, sobre a mesa.
    /// A salinha é maior, então tudo nela é desenhado menor: as pessoas e as torres usam a versão reduzida ("_p").
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
            public string tamanho;   // sufixo das pessoas e torres nessa escala ("", "_p" ou "_m")
            public string equip;     // sufixo de racks e no-breaks nessa escala ("" ou "_m")
            public Vector2 fundo, esquerda, direita;
            public int casas;
        }

        /// <summary>Uma por cargo. O DevOps usa a sala de racks até ganhar a sua; do SRE em diante, o data center.</summary>
        static readonly Ilustracao[] Ilustracoes =
        {
            new Ilustracao { nome = "armario", tamanho = "", equip = "", fundo = new Vector2(200, 146), esquerda = new Vector2(57.5f, 217), direita = new Vector2(341, 218), casas = 4 },
            new Ilustracao { nome = "salinha", tamanho = "_p", equip = "", fundo = new Vector2(199, 143), esquerda = new Vector2(47, 219), direita = new Vector2(351, 219), casas = 6 },
            new Ilustracao { nome = "racks", tamanho = "_m", equip = "_m", fundo = new Vector2(200, 122), esquerda = new Vector2(42, 200), direita = new Vector2(357, 200), casas = 8 },
            new Ilustracao { nome = "racks", tamanho = "_m", equip = "_m", fundo = new Vector2(200, 122), esquerda = new Vector2(42, 200), direita = new Vector2(357, 200), casas = 8 },
            new Ilustracao { nome = "dc", tamanho = "_m", equip = "_m", fundo = new Vector2(200, 105), esquerda = new Vector2(30, 190), direita = new Vector2(371, 190), casas = 10 },
        };

        PixelCanvas telaIlustrada;
        IsoDesenho dIlustrada;
        SpriteIso fundoIlustrado;
        int cargoIlustrado = -1;

        bool TemIlustracao => true;
        Ilustracao Sala => Ilustracoes[Mathf.Min(E.Cargo, Ilustracoes.Length - 1)];

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
            cargoIlustrado = E.Cargo;
            var tex = ArteGerada.Textura("Salas/" + Sala.nome);
            fundoIlustrado = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            telaIlustrada = new PixelCanvas(tex.width, tex.height);
            dIlustrada = new IsoDesenho(telaIlustrada);
            var r = ArteGerada.AreaOpaca(tex);
            areaArte = new RectInt(r.x - 2, tex.height - r.yMax - 2, r.width + 4, r.height + 4);
        }

        void DesenharIlustrada()
        {
            if (telaIlustrada == null || cargoIlustrado != E.Cargo) MontarIlustrada();
            tela = telaIlustrada; d = dIlustrada;
            desenhandoArte = true;
            tela.Limpar(new Color32(0, 0, 0, 0));
            tela.Imagem(fundoIlustrado.px, fundoIlustrado.w, fundoIlustrado.h, 0, 0);

            tecnicoConsertando = false;
            if (E.Cargo == 0) ArmarioIlustrado(); else if (E.Cargo == 1) SalinhaIlustrada(); else SalaGrandeIlustrada();
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
            string nome = "torre" + Sala.tamanho;
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
                int semente = i;
                fila.Add((-i * 0.01f, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));   // a da frente cobre a de trás
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, s.w, s.h), Tipo = "servidor:" + i, Px = s.px, Prof = -i * 0.01f, Nome = "Servidor torre " + (i + 1) });
                if (travado) Quebrado(l + new Vector2Int(b.frente.x, 0), l + b.frente);
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
        void Quebrado(Vector2Int topo, Vector2Int frente)
        {
            fila.Add((5, () => { if (Piscar()) tela.Texto("!", topo.x - 2, topo.y - 2, IsoDesenho.C("ff3b4e"), true, 2); }));
            if (!tecnicoConsertando) { tecnicoConsertando = true; lugarDoTecnico = new Vector2(frente.x + 12, frente.y + 8); }
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

        const int RacksNaSalinha = 3, TorresNaSalinha = 4, NoBreaksNaSalinha = 2;

        void SalinhaIlustrada()
        {
            // parede da esquerda, da frente para o fundo: as torres que vieram do armário e depois os racks (o primeiro
            // rack fica junto da planta, no fundo)
            TorresNaParede(52, TorresNaSalinha, false);
            Racks(148, RacksNaSalinha);

            // embaixo da janela, entre a planta e a porta: os no-breaks (espelhados: o visor fica virado para a sala)
            var nobreak = CarregarPixelLab("nobreak", true);
            int nNoBreaks = Mathf.Min(E.Nivel(Catalogo.NoBreak), NoBreaksNaSalinha);
            for (int i = 0; i < NoBreaksNaSalinha; i++)
            {
                var l = NaParedeDireita(nobreak, 211 + i * 20);   // a sombra no pé da imagem atrapalha medir a base: passo fixo
                if (i == 0) pontoDoNoBreak = l + new Vector2Int(nobreak.w / 2, nobreak.h / 2);
                if (i >= nNoBreaks) continue;
                int semente = i + 20;
                fila.Add((0.5f + i * 0.01f, () => DesenharSprite(nobreak, l.x, l.y, semente)));
            }
            // ar-condicionado no alto da parede da direita, sobre a mesa
            var ar = CarregarPixelLab("ar_condicionado");
            pontoDoAr = new Vector2Int(330, 112);
            if (ar != null && E.Nivel(Catalogo.ArCondicionado) > 0)
                fila.Add((1, () => DesenharSprite(ar, pontoDoAr.x - ar.w / 2, pontoDoAr.y - ar.h / 2)));

            Alvos.Add(new Alvo { Area = new RectInt(270, 140, 80, 85), Tipo = "equipamento", Prof = -20, Nome = "Mesa do técnico" });   // mesa da ilustração (atrás de tudo que fica em cima dela)
            Caneca("caneca_p", new Vector2Int(338, 180));
            BackupNaMesa(new Vector2Int(322, 186), new Vector2Int(283, 162));
            pontoDoChamado = new Vector2Int(302, 124);
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
                fila.Add((-1.5f + i * 0.01f, () => DesenharRack(s, l, cheio, !quebrado)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, rack.w, rack.h), Tipo = "rack", Px = s.px, Prof = -1.5f + i * 0.01f, Nome = "Rack 42U: " + E.ServidoresRack + " servidores 1U" });
                if (quebrado) Quebrado(l + new Vector2Int(b.frente.x, 0), l + b.frente);
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
        (float de, float ate)[] TrechosDaParede => E.Cargo >= 4 ? new[] { (206f, 268f), (300f, 366f) } : new[] { (264f, 352f) };
        float[] Fileiras => E.Cargo >= 4 ? new[] { 2.6f, 4.5f, 6.4f, 8.3f } : new[] { 2.6f, 4.6f, 6.6f };
        const float InicioDaFileira = 2.2f;

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
                float prof = -8 + l.x * 0.001f;
                fila.Add((prof, () => DesenharRack(s, l, cheio, !quebrado)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, rack.w, rack.h), Tipo = "rack", Px = s.px, Prof = prof, Nome = "Rack 42U: " + E.ServidoresRack + " servidores 1U" });
                if (quebrado) Quebrado(l + new Vector2Int(BaseDe(rack).frente.x, 0), l + BaseDe(rack).frente);
                if (i == 0) pontoDoRack = l + new Vector2Int(rack.w / 2, rack.h / 2);
            }
            var nobreak = CarregarPixelLab("nobreak" + equip, true);
            for (int i = 0; i < Mathf.Min(E.Nivel(Catalogo.NoBreak), 3) && Cabe(9); i++)
            {
                var l = NaParedeDireita(nobreak, x);
                x += 11;
                if (i == 0) pontoDoNoBreak = l + new Vector2Int(nobreak.w / 2, nobreak.h / 2);
                int semente = i + 20;
                fila.Add((-8 + l.x * 0.001f, () => DesenharSprite(nobreak, l.x, l.y, semente)));
            }
            var torre = CarregarPixelLab("torre" + Sala.tamanho);
            var bt = BaseDe(torre);
            for (int i = 0; i < Mathf.Min(E.Torres, 4) && Cabe(LarguraNaDireita(torre)); i++)
            {
                var l = NaParedeDireita(torre, x);
                x += LarguraNaDireita(torre) + 2;
                bool travado = E.Travado(i) || HdDaTorreQueimado(i);
                var s = travado ? Travada("torre" + Sala.tamanho, false) : torre;
                int semente = i;
                fila.Add((-8 + l.x * 0.001f, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, torre.w, torre.h), Tipo = "servidor:" + i, Px = s.px, Prof = -8 + l.x * 0.001f, Nome = "Servidor torre " + (i + 1) });
                if (travado) Quebrado(l + new Vector2Int(bt.frente.x, 0), l + bt.frente);
                pontoDaTorre = l + new Vector2Int(torre.w / 2, torre.h / 2);
            }

            // piso técnico: uma fileira por tipo de máquina (no Analista, os racks cheios ocupam todas)
            var fileiras = Fileiras;
            int f = 0;
            List<Vector2Int> Fileira() => f < fileiras.Length ? LugaresNaFileira(fileiras[f++]) : new List<Vector2Int>();
            var lugaresRacks = Fileira();
            if (cargo == 2) while (f < fileiras.Length) lugaresRacks.AddRange(Fileira());
            string principal = cargo == 2 ? Catalogo.RackCheio : cargo == 3 ? Catalogo.Containers : Catalogo.NoKubernetes;

            // na ponta da primeira fileira: o storage (LEDs brancos; vermelho com disco queimado) e a biblioteca de fitas (rosa)
            int naPonta = 0;
            for (int i = 0; i < Mathf.Min(3, E.NivelStorage) && naPonta < lugaresRacks.Count; i++, naPonta++)
            {
                bool queimado = i == 0 && E.DiscoQueimado;
                MaquinaNaFileira(ComLeds("rack" + equip, queimado ? LedQuebrado : LedStorage), lugaresRacks[naPonta], "storage", "Storage", queimado);
                if (i == 0) pontoDoStorage = lugaresRacks[naPonta] + new Vector2Int(10, 10);
            }
            if (E.Nivel(Catalogo.Backup) > 0 && naPonta < lugaresRacks.Count) { pontoDaFita = lugaresRacks[naPonta] + new Vector2Int(10, 10); MaquinaNaFileira(ComLeds("rack" + equip, LedFita), lugaresRacks[naPonta++], "equipamento", "Biblioteca de fitas (backup)"); }
            if (naPonta > 0) lugaresRacks.RemoveRange(0, naPonta);

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
                Alvos.Add(new Alvo { Area = new RectInt(37, 148, 58, 58), Tipo = "noc", Prof = -20, Nome = "NOC" });
                pontoDoChamado = new Vector2Int(62, 140);
            }
            else
            {
                Alvos.Add(new Alvo { Area = new RectInt(52, 150, 75, 58), Tipo = "equipamento", Prof = -20, Nome = "Mesa do técnico" });
                Caneca("caneca_m", new Vector2Int(112, 163));
                BackupNaMesa(new Vector2Int(81, 179), new Vector2Int(64, 171));
                pontoDoChamado = new Vector2Int(86, 140);
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
            if (quebrado) Quebrado(l + new Vector2Int(BaseDe(s).frente.x, 0), l + BaseDe(s).frente);
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

        // ---------------- Pessoas ----------------

        /// <summary>
        /// Quadro de uma pessoa do PixelLab. Indo para a direita (gx crescendo) ela olha para o sudeste; voltando, para o
        /// noroeste. Andando, alterna os quadros da caminhada (quem_se_0..n); sem eles, usa a pose parada da direção.
        /// </summary>
        SpriteIso QuadroDaPessoa(string quem, bool voltando, bool andando, float fase)
        {
            string dir = voltando ? "nw" : "se";
            if (andando)
            {
                int n = 0;
                while (n < 8 && CarregarPixelLab(quem + "_" + dir + "_" + n) != null) n++;
                if (n > 0) return CarregarPixelLab(quem + "_" + dir + "_" + (Mathf.FloorToInt(t * 6f + fase * 10) % n));
            }
            return CarregarPixelLab(quem + "_" + dir);
        }

        void PessoasIlustradas()
        {
            // corredor na frente das paredes e da cadeira: o técnico vai e volta ao longo de gx; o estagiário anda mais à
            // frente, no sentido contrário
            bool salinha = E.Cargo == 1;
            string tecnico = "tecnico" + Sala.tamanho, estagiario = "estagiario" + Sala.tamanho;
            // nas salas grandes os dois andam no corredor da frente, cada um numa metade
            bool grande = E.Cargo >= 2;
            float frente = Sala.casas - 0.45f;
            float gyTecnico = grande ? frente : salinha ? 3.2f : 2.9f, gyEstagiario = grande ? frente : salinha ? 4.2f : 3.55f;
            bool voltando;
            float gx = grande ? PosicaoAndando(2.4f, Sala.casas * 0.55f, 0f, 0.5f, out voltando)
                : salinha ? PosicaoAndando(1.6f, 4.0f, 0f, 0.5f, out voltando) : PosicaoAndando(1.3f, 3.2f, 0f, 0.55f, out voltando);
            int pulo = Mathf.RoundToInt(Mathf.Abs(Mathf.Sin((t - ultimaCompraEm) * 9f)) * (grande ? 4f : salinha ? 6f : 8f));
            if (tecnicoConsertando)
            {
                var s = CarregarPixelLab(tecnico + "_nw");
                if (s != null) PessoaIlustrada(s, tecnico, new Vector2Int(Mathf.RoundToInt(lugarDoTecnico.x), Mathf.RoundToInt(lugarDoTecnico.y)), 0);
            }
            else if (t - ultimaCompraEm < 1.5f)
            {
                // comemora a compra: pulinhos olhando para a frente
                var s = CarregarPixelLab(tecnico + "_s") ?? CarregarPixelLab(tecnico + "_se");
                if (s != null) PessoaIlustrada(s, tecnico, IP(gx, gyTecnico), pulo);
            }
            else
            {
                var s = QuadroDaPessoa(tecnico, voltando, true, 0f);
                if (s != null) PessoaIlustrada(s, tecnico, IP(gx, gyTecnico), 0);
            }

            if (E.TemEstagiario)
            {
                bool volta;
                float gxE = grande ? PosicaoAndando(Sala.casas * 0.6f, Sala.casas - 0.8f, 0.9f, 0.45f, out volta)
                    : salinha ? PosicaoAndando(1.9f, 3.8f, 0.9f, 0.45f, out volta) : PosicaoAndando(1.5f, 3.0f, 0.9f, 0.45f, out volta);
                pontoDoEstagiario = IP(gxE, gyEstagiario);
                var s = QuadroDaPessoa(estagiario, volta, true, 0.37f);
                if (s != null) PessoaIlustrada(s, estagiario, pontoDoEstagiario, 0);
            }

            if (grande) EngenheirosNosCorredores(tecnico);
        }

        /// <summary>Cor da camisa de cada engenheiro de campo (o técnico com outra camisa).</summary>
        static readonly Color32[] CamisasDosEngenheiros = { new Color32(232, 140, 60, 255), new Color32(150, 110, 200, 255), new Color32(200, 70, 80, 255) };

        /// <summary>
        /// Da sala de racks em diante, engenheiros de campo andam pelos corredores entre as fileiras (um a mais por cargo,
        /// até três). Ficam na mesma profundidade das máquinas: a fileira da frente cobre quem está atrás dela.
        /// </summary>
        void EngenheirosNosCorredores(string tecnico)
        {
            var fileiras = Fileiras;
            var rack = CarregarPixelLab("rack" + Sala.equip);
            float profundidade = rack != null ? (BaseDe(rack).direita.x - BaseDe(rack).frente.x) / ((Sala.fundo.x - Sala.esquerda.x) / Sala.casas) : 0.66f;
            int n = Mathf.Min(E.Cargo - 1, 3, fileiras.Length - 1);
            for (int i = 0; i < n; i++)
            {
                // meio do corredor entre a fileira i e a seguinte
                float gy = (fileiras[i] + profundidade + fileiras[i + 1]) / 2f;
                float gx = PosicaoAndando(InicioDaFileira + 0.2f, Sala.casas - 0.5f, 0.6f + i * 0.31f, 0.4f + i * 0.05f, out bool volta);
                var s = QuadroDaPessoa(tecnico, volta, true, 0.6f + i);
                if (s == null) continue;
                PessoaIlustrada(Recolorido(s, CamisasDosEngenheiros[i]), tecnico, IP(gx, gy), 0, true);
            }
        }

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
        /// Pessoa com os pés no ponto p (sombra no chão; z levanta o corpo, para os pulos). Entre as máquinas (entreMaquinas)
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
                case Catalogo.RackCheio: case Catalogo.PisoElevado: return pontoDoRackCheio + new Vector2Int(10, 10);
                case Catalogo.Hypervisor: case Catalogo.Link10G: return pontoDoHypervisor + new Vector2Int(10, 10);
                case Catalogo.Containers: case Catalogo.ServidorCi: case Catalogo.ImagensEnxutas: case Catalogo.CacheRedis: return pontoDosContainers + new Vector2Int(10, 10);
                case Catalogo.NoKubernetes: case Catalogo.Balanceador: case Catalogo.ServiceMesh: return pontoDosNos + new Vector2Int(10, 10);
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
                float ang = i * 0.449f + i * i * 0.07f, raio = 4 + idade * (26 + (i % 4) * 7);
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 14));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 2, 2, cor);
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
            tela.Ret(p.x - 7, p.y - 13, 14, 13, IsoDesenho.C("1b1a2e"));
            for (int y = 0; y < papel.Length; y++)
                for (int x = 0; x < papel[y].Length; x++)
                    if (papel[y][x] != '.') tela.Pixel(p.x - 6 + x, p.y - 12 + y, IsoDesenho.C(papel[y][x] == 'y' ? "fdf6e3" : "7d82ad"));
            tela.Ret(p.x + 3, p.y - 12, 2, 2, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)(E.SegundosDoChamado / Catalogo.TempoParaAtender);
            tela.Ret(p.x - 8, p.y + 2, 16, 2, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 8, p.y + 2, Mathf.RoundToInt(16 * resta), 2, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 10, p.y - 16, 20, 22), Tipo = "chamado", Prof = 100, Nome = "Chamado urgente" });
        }
    }
}
