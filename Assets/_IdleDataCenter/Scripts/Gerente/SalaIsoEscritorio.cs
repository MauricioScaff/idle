using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// O começo da carreira. Freelancer: o quarto em casa, com a torre velha e a bancada onde chegam os PCs do bairro.
    /// Técnico: o escritório da empresa (PixelLab Pro), com os funcionários nas mesas, a impressora, a cafeteira e o canto
    /// da TI (bancada e armarinho de servidor). Aqui os chamados aparecem como balões sobre quem pediu (a pessoa ou a
    /// bancada); clicar num balão atende e o técnico vai até lá resolver.
    /// </summary>
    public partial class SalaIso
    {
        /// <summary>Onde um chamado pode aparecer: quem pediu, onde o técnico fica para resolver e onde flutua o balão.</summary>
        class LugarDeChamado
        {
            public string tipo;          // "pessoa" ou "bancada"
            public Vector2 grade;        // onde o técnico fica (em casas)
            public string olhar;
            public Vector2Int balao;     // ponta de baixo do balão (pixel da ilustração)
        }

        readonly List<LugarDeChamado> lugaresDeChamado = new List<LugarDeChamado>();

        /// <summary>O chamado que o técnico foi atender (o lugar), e até quando ele fica lá trabalhando.</summary>
        LugarDeChamado atendimento;
        float atendimentoAte;

        // ---------------- Freelancer: o quarto ----------------

        /// <summary>O armário de antes vira o quarto do freelancer: a mesa dele, a torre velha e a bancada de consertos.</summary>
        void QuartoIlustrado()
        {
            ArmarioIlustrado();
            // o marcador compra o próximo site, que entra na torre velha (não aparece torre nova)
            if (!E.NoMaximo(Catalogo.SiteCliente)) Marcador = pontoDaTorre + new Vector2Int(10, 14);
            var bancada = CarregarPixelLab("bancada");
            if (bancada == null) return;
            var l = NoPiso(bancada, 3.75f, 3.55f);
            var b = BaseDe(bancada);
            fila.Add((3, () => DesenharSprite(bancada, l.x, l.y)));
            Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, bancada.w, bancada.h), Tipo = "equipamento", Px = bancada.px, Prof = 3, Nome = "Bancada de consertos" });
            pontoDaBancada = l + new Vector2Int(bancada.w / 2, 18);
            // no quarto todo PC que chega vai para a bancada; o freelancer trabalha pelo lado esquerdo dela (sem subir em cima)
            lugaresDeChamado.Add(new LugarDeChamado { tipo = "bancada", grade = new Vector2(1.95f, 3.2f), olhar = "se", balao = l + new Vector2Int(bancada.w / 2, 4) });

            // o que ocupa o piso do quarto (em casas): planta, mesa, cadeira, a torre velha e a bancada (até a cadeira: sem corredor atrás dela)
            Bloquear(0, 0, 0.7f, 0.7f, 55);          // planta
            Bloquear(1.6f, 0, 4, 1.15f, 40);          // mesa
            Bloquear(2.25f, 0.8f, 2.95f, 1.45f, 35);  // cadeira
            Bloquear(0, 2.6f, 0.75f, 3.7f, 45);       // torre velha
            Bloquear(2.45f, 2.3f, 3.95f, 3.65f, 32);  // bancada (pelos pés)
        }

        // ---------------- Técnico: o escritório ----------------

        /// <summary>Onde senta cada funcionário (em casas), na ordem em que são contratados: as duas mesas do fundo primeiro.</summary>
        static readonly Vector2[] Cadeiras = { new Vector2(1.79f, 3.28f), new Vector2(3.57f, 3.31f), new Vector2(1.66f, 6.07f), new Vector2(3.56f, 6.1f) };

        /// <summary>
        /// As torres da empresa: uma fileira na frente do balcão da cafeteira, entre ele e o armarinho de servidor (que fica na
        /// frente delas e as cobre um pouco). Atrás do armarinho elas sumiam, então a partir da quarta só a placa com o total.
        /// </summary>
        static readonly Vector2[] TorresDoEscritorio = { new Vector2(6.75f, 1.8f), new Vector2(6.05f, 1.8f), new Vector2(5.35f, 1.8f) };

        /// <summary>Onde o técnico trabalha no escritório: na frente da bancada da TI, olhando para ela.</summary>
        (Vector2 lugar, string olhar) LugarNaBancada => (new Vector2(5.35f, 6.45f), "se");   // na frente da bancada, ao lado da mesinha da planta (atrás dela ou colado na borda ele parecia em cima)

        Vector2Int pontoDaBancada, pontoDoFuncionario;

        void EscritorioIlustrado()
        {
            TorresNoPiso(TorresDoEscritorio);
            // o armarinho de servidor (com a ponta da bancada na frente dele) fica na frente das torres
            NaFrenteDaIlustracao(new Vector2(366, 270), new Vector2(391, 258), new Vector2(417, 270), new Vector2(417, 312), new Vector2(391, 326), new Vector2(366, 312));
            Funcionarios();

            // o que ocupa o piso (em casas): planta do fundo, as duas fileiras de mesas, impressora, cafeteira, armarinho de
            // servidor, a bancada da TI e a mesinha da planta da frente (as torres e os funcionários se bloqueiam sozinhos)
            Bloquear(0, 0, 0.6f, 0.6f, 50);           // planta do fundo
            // as mesas medidas pelos cantos dos pés na ilustração (antes estavam curtas e para trás: o técnico parava na
            // ponta da mesa do fundo); a altura conta os monitores
            Bloquear(1.1f, 2.25f, 4.55f, 3.05f, 60);  // mesas do fundo
            Bloquear(1.05f, 4.95f, 4.25f, 6.0f, 60);  // mesas da frente
            Bloquear(2.9f, 0, 3.9f, 0.9f, 55);        // impressora no armarinho
            Bloquear(5.3f, 0, 7, 1.1f, 50);           // balcão da cafeteira
            Bloquear(6.0f, 2.5f, 7, 3.4f, 50);        // armarinho de servidor
            Bloquear(5.85f, 3.6f, 7, 5.95f, 55);      // bancada da TI (pelos pés; a altura conta o gabinete aberto em cima dela)
            Bloquear(6.35f, 6.25f, 7, 7, 45);         // mesinha da planta da frente

            // a cafeteira é o café; a bancada é de onde vêm os chamados de máquina
            Alvos.Add(new Alvo { Area = new RectInt(450, 190, 36, 38), Tipo = "cafe", Prof = 1, Nome = "Café: renda em dobro" });
            Alvos.Add(new Alvo { Area = new RectInt(280, 285, 112, 62), Tipo = "equipamento", Prof = -20, Nome = "Bancada da TI" });
            pontoDaBancada = new Vector2Int(330, 300);
            // a impressora fica atrás das mesas do fundo: quem fosse até ela apareceria por cima das mesas e dos monitores.
            // O chamado da impressora vai para quem pediu (um funcionário), e ela não entra na ronda
            lugaresDeChamado.Add(new LugarDeChamado { tipo = "bancada", grade = LugarNaBancada.lugar, olhar = LugarNaBancada.olhar, balao = new Vector2Int(322, 282) });
            pontosDeRonda.Add((new Vector2(4.95f, 0.75f), "ne"));   // cafeteira (pelo lado: as torres ficam na frente do balcão)
            pontosDeRonda.Add((new Vector2(5.85f, 3.15f), "se"));    // armarinho de servidor

            // o que a loja do Técnico põe na sala: ventilador e filtro de linha perto das torres, HD externo na bancada
            pontoDoVentilador = IP(4.7f, 6.6f) + new Vector2Int(0, -14);
            var ventilador = CarregarPixelLab("ventilador");
            if (ventilador != null && E.Nivel(Catalogo.Ventilador) > 0)
                fila.Add((1, () => DesenharSprite(ventilador, pontoDoVentilador.x - ventilador.w / 2, pontoDoVentilador.y - ventilador.h / 2)));
            pontoDoFiltro = IP(5.55f, 2.95f);
            var filtro = CarregarPixelLab("filtro_linha");
            if (filtro != null && E.Nivel(Catalogo.FiltroDeLinha) > 0)
                fila.Add((2, () => DesenharSprite(filtro, pontoDoFiltro.x - filtro.w / 2, pontoDoFiltro.y - filtro.h + 2)));
            BackupNaMesa(new Vector2Int(356, 303), new Vector2Int(370, 296));
        }

        /// <summary>
        /// Os funcionários nas mesas, sentados e digitando (cada um balança um pouco, fora de compasso). Cada um é um lugar de
        /// chamado: o técnico vai até o lado da cadeira.
        /// </summary>
        void Funcionarios()
        {
            int n = Mathf.Min(E.Funcionarios, Cadeiras.Length);
            for (int i = 0; i < n; i++)
            {
                var s = CarregarPixelLab("funcionario_" + i);
                if (s == null) continue;
                var pe = IP(Cadeiras[i].x, Cadeiras[i].y);
                int pes = Pes(s);
                int k = i;
                fila.Add((9 + pe.y * 0.001f, () =>
                {
                    int balanco = Mathf.Sin(t * 2.2f + k * 1.7f) > 0.7f ? 1 : 0;   // digitando
                    tela.Ret(pe.x - 8, pe.y - 1, 16, 3, new Color32(20, 20, 40, 90));
                    tela.Imagem(s.px, s.w, s.h, pe.x - s.w / 2, pe.y - pes - balanco);
                }));
                Alvos.Add(new Alvo { Area = new RectInt(pe.x - s.w / 2, pe.y - pes, s.w, pes), Tipo = "equipamento", Px = s.px, Prof = 9, Nome = "Funcionário" });
                Bloquear(Cadeiras[i].x - 0.22f, Cadeiras[i].y - 0.22f, Cadeiras[i].x + 0.22f, Cadeiras[i].y + 0.22f, 50);
                lugaresDeChamado.Add(new LugarDeChamado { tipo = "pessoa", grade = Cadeiras[i] + new Vector2(0.65f, 0.1f), olhar = "nw", balao = pe + new Vector2Int(0, -pes - 4) });
                if (i == n - 1) pontoDoFuncionario = pe + new Vector2Int(0, -pes / 2);
            }
        }

        /// <summary>
        /// Um pedaço da ilustração redesenhado por cima do que fica atrás dele: a ilustração é um fundo só, então sem isso uma
        /// torre atrás do armarinho aparecia por cima dele. Contorno convexo em pixels da ilustração, em ordem; a profundidade
        /// é a linha mais baixa (como a das torres no piso).
        /// </summary>
        void NaFrenteDaIlustracao(params Vector2[] contorno)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var p in contorno)
            {
                x0 = Mathf.Min(x0, Mathf.FloorToInt(p.x)); y0 = Mathf.Min(y0, Mathf.FloorToInt(p.y));
                x1 = Mathf.Max(x1, Mathf.CeilToInt(p.x)); y1 = Mathf.Max(y1, Mathf.CeilToInt(p.y));
            }
            string chave = "frente|" + Sala.nome + "|" + x0 + "," + y0 + "," + x1 + "," + y1;
            if (!sprites.TryGetValue(chave, out var pedaco))
            {
                int w = x1 - x0 + 1, h = y1 - y0 + 1;
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (Dentro(new Vector2(x0 + x + 0.5f, y0 + y + 0.5f), contorno))
                            px[y * w + x] = fundoIlustrado.px[(y0 + y) * fundoIlustrado.w + x0 + x];
                pedaco = sprites[chave] = new SpriteIso { px = px, w = w, h = h };
            }
            fila.Add((-5 + y1 * 0.01f, () => tela.Imagem(pedaco.px, pedaco.w, pedaco.h, x0, y0)));
        }

        /// <summary>Canto de cima da imagem para o canto da frente da base do objeto ficar no ponto (gx, gy) do piso.</summary>
        Vector2Int NoPiso(SpriteIso s, float gx, float gy) => IP(gx, gy) - BaseDe(s).frente;

        /// <summary>
        /// Torres no chão, cada uma num dos lugares (em casas, o canto da frente da base). Como na parede: com mais torres
        /// do que lugares, a placa com o total; o contorno marca onde entra a próxima.
        /// </summary>
        void TorresNoPiso(Vector2[] lugares)
        {
            var torre = CarregarPixelLab("torre");
            if (torre == null) return;
            var b = BaseDe(torre);
            int visiveis = Mathf.Min(E.Torres, lugares.Length);
            for (int i = 0; i < visiveis; i++)
            {
                var l = NoPiso(torre, lugares[i].x, lugares[i].y);
                bool travado = E.Travado(i) || HdDaTorreQueimado(i);
                var s = travado ? Travada("torre", false) : torre;
                if (E.ForaDaGarantia) s = Envelhecido(s);
                float prof = -5 + (l.y + s.h) * 0.01f;
                int semente = i;
                fila.Add((prof, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, s.w, s.h), Tipo = "servidor:" + i, Px = s.px, Prof = prof, Nome = "Servidor torre " + (i + 1) });
                if (travado) Quebrado(l + new Vector2Int(b.frente.x, 0), l + b.frente, false);
                Bloquear(lugares[i].x - 0.7f, lugares[i].y - 0.7f, lugares[i].x, lugares[i].y, 55);
                Ronda(l + b.frente, false);
            }
            var ultima = NoPiso(torre, lugares[Mathf.Clamp(E.Torres - 1, 0, lugares.Length - 1)].x, lugares[Mathf.Clamp(E.Torres - 1, 0, lugares.Length - 1)].y);
            pontoDaTorre = ultima + new Vector2Int(b.frente.x, b.frente.y / 2);
            if (E.Torres > lugares.Length)
                Placas.Add(new Placa { Setor = "Compute", Nome = E.Torres + " torres", Pos = ultima + new Vector2Int(b.frente.x, 4), Cor = IsoGui.Cyan });
            if (E.Torres < lugares.Length) ContornoDaBase(torre, NoPiso(torre, lugares[E.Torres].x, lugares[E.Torres].y));
            else Marcador = ultima + new Vector2Int(b.frente.x + 8, b.frente.y + 4);
        }

        // ---------------- Chamados como balões ----------------

        /// <summary>
        /// Quem pediu cada chamado: a bancada quando é máquina (PC, notebook, HD...), senão
        /// um dos funcionários (sempre o mesmo para o mesmo chamado).
        /// </summary>
        LugarDeChamado LugarDo(ChamadoAberto c)
        {
            LugarDeChamado Do(string tipo) { foreach (var l in lugaresDeChamado) if (l.tipo == tipo) return l; return null; }
            if (lugaresDeChamado.Count == 1) return lugaresDeChamado[0];
            string txt = c.texto.ToLowerInvariant();
            foreach (var maquina in new[] { "pc", "notebook", "hd", "tela azul", "pendrive", "teclado", "mouse", "monitor", "celular" })
                if (txt.Contains(maquina)) return Do("bancada") ?? lugaresDeChamado[0];
            var pessoas = lugaresDeChamado.FindAll(l => l.tipo == "pessoa");
            if (pessoas.Count == 0) return Do("bancada") ?? lugaresDeChamado[0];
            int h = 0;
            foreach (char ch in c.texto) h = h * 31 + ch;
            return pessoas[(h & 0x7fffffff) % pessoas.Count];
        }

        static Color32 CorDaPrioridade(int p) => IsoDesenho.C(p == 1 ? "ff5a6a" : p == 2 ? "ff9a3a" : p == 3 ? "ffd65c" : "b8bfcf");

        /// <summary>
        /// Um balão com "!" sobre quem pediu, na cor da prioridade, com o prazo embaixo (e a barrinha verde de quem da equipe
        /// está nele). Dois chamados no mesmo lugar ficam lado a lado. Clicar no balão atende aquele chamado.
        /// </summary>
        void BaloesDeChamado()
        {
            var chamados = E.Chamados;
            var equipe = E.ChamadoDaEquipe;
            var usados = new Dictionary<LugarDeChamado, int>();
            for (int i = 0; i < chamados.Count; i++)
            {
                var c = chamados[i];
                var l = LugarDo(c);
                usados.TryGetValue(l, out int n);
                usados[l] = n + 1;
                float bob = Mathf.Sin(t * 4 + i) * 1.5f;
                var p = l.balao + new Vector2Int(n * 16, Mathf.RoundToInt(bob));
                var cor = CorDaPrioridade(c.prioridade);
                var escuro = IsoDesenho.C("1b1a2e");
                bool urgente = c.prioridade <= 2;
                // desenhado na hora (a fila da cena já foi desenhada): o balão fica por cima de tudo
                {
                    // balão: contorno escuro, miolo branco, rabinho para baixo
                    tela.Ret(p.x - 7, p.y - 17, 14, 13, escuro);
                    tela.Ret(p.x - 6, p.y - 16, 12, 11, IsoDesenho.C("fdf6e3"));
                    tela.Ret(p.x - 2, p.y - 5, 4, 2, escuro);
                    tela.Ret(p.x - 1, p.y - 5, 2, 1, IsoDesenho.C("fdf6e3"));
                    tela.Ret(p.x - 1, p.y - 3, 2, 1, escuro);
                    var exclamacao = urgente && !Piscar() ? IsoDesenho.C("1b1a2e") : cor;
                    tela.Ret(p.x - 1, p.y - 14, 3, 5, exclamacao);
                    tela.Ret(p.x - 1, p.y - 8, 3, 2, exclamacao);
                    tela.Ret(p.x - 6, p.y - 16, 12, 1, cor);   // a faixa de cima diz a prioridade
                    // prazo: barra embaixo do balão
                    float resta = Mathf.Clamp01((float)(c.restante / Catalogo.PrazoDoChamado[c.prioridade - 1]));
                    tela.Ret(p.x - 7, p.y, 14, 2, escuro);
                    tela.Ret(p.x - 7, p.y, Mathf.RoundToInt(14 * resta), 2, resta > 0.3f ? IsoDesenho.C("ffd65c") : IsoDesenho.C("ff3b4e"));
                    if (c == equipe)
                    {
                        tela.Ret(p.x - 7, p.y + 3, 14, 2, escuro);
                        tela.Ret(p.x - 7, p.y + 3, Mathf.RoundToInt(14 * Mathf.Clamp01((float)E.ProgressoDaEquipe)), 2, IsoDesenho.C("5cff8a"));
                    }
                }
                if (i == 0) Chamado = p;
                Alvos.Add(new Alvo { Area = new RectInt(p.x - 9, p.y - 19, 18, 24), Tipo = "chamado:" + i, Prof = 100 + i * 0.01f, Nome = "P" + c.prioridade + ": " + c.texto });
            }
        }

        /// <summary>Você clicou num chamado (índice na ordem de E.Chamados): o técnico vai até quem pediu. Chamar antes de atender.</summary>
        public void IrAtender(int indice)
        {
            var lista = E.Chamados;
            if (indice < 0 || indice >= lista.Count || lugaresDeChamado.Count == 0) return;
            atendimento = LugarDo(lista[indice]);
            atendimentoAte = float.MaxValue;
            if (pessoas.TryGetValue("tecnico", out var tecnico)) tecnico.atendendo = false;   // muda de rumo se já ia para outro
        }

        /// <summary>O técnico indo até um chamado clicado e ficando lá uns segundos resolvendo. Retorna se está ocupado com isso.</summary>
        bool AtenderNoLugar(Trabalhador p)
        {
            if (atendimento == null) return false;
            if (!p.atendendo)
            {
                p.atendendo = true; p.naMesa = false; p.consertando = false;
                Rota(p, atendimento.grade); p.olhandoDestino = atendimento.olhar;
            }
            if (p.caminho.Count == 0 && atendimentoAte == float.MaxValue) atendimentoAte = t + 2.5f;
            if (t < atendimentoAte) { p.paradaAte = t + 1; return true; }
            atendimento = null; p.atendendo = false;
            VoltarParaAMesa(p);
            return true;
        }
    }
}
