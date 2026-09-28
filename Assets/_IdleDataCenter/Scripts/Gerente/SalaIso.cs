using System;
using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A sala do data center em vista isométrica, desenhada em pixel art a partir do estado da Economia.
    /// Cresce a cada promoção (armário → salinha → sala de racks → sala virtualizada → data center pequeno),
    /// e cada compra aparece no seu lugar: torres, rack, no-breaks, ar-condicionado, storage, rede, laboratório,
    /// NOC, containers, cluster Kubernetes. Fora da sala, um contorno tracejado mostra a próxima expansão.
    ///
    /// Desenha numa PixelCanvas (a textura é ampliada por um fator inteiro no modo gerente) e devolve
    /// onde ficam as coisas clicáveis, as placas dos setores e o marcador de construção.
    /// </summary>
    public partial class SalaIso
    {
        // tamanho da sala (em quadrados) por cargo
        static readonly Vector2Int[] Tamanhos = { new Vector2Int(6, 5), new Vector2Int(9, 7), new Vector2Int(12, 9), new Vector2Int(14, 10), new Vector2Int(16, 11) };
        const int AlturaParede = 56;

        /// <summary>Em que cargo cada setor passa a existir (-1 = não é setor).</summary>
        public static int CargoDoSetor(string id)
        {
            switch (id)
            {
                case "Compute": case "Equipe": return 0;
                case "Energia": case "Refrigeracao": return 1;
                case "Storage": case "Rede": case "Automacao": return 2;
                case "NOC": return 3;
                case "Campus": return 5;
                case "Mundo": return 6;
                default: return -1;
            }
        }

        public struct Alvo { public RectInt Area; public string Tipo; }
        public struct Placa { public string Setor, Nome; public Vector2Int Pos; public Color Cor; }

        readonly Economia E;
        PixelCanvas tela, telaSala;
        IsoDesenho d, dSala;
        int cargoMontado = -1, W, D;
        public Texture2D Textura => tela?.Textura;
        public int Largura => tela.Largura;
        public int Altura => tela.Altura;

        public readonly List<Alvo> Alvos = new List<Alvo>();
        public readonly List<Placa> Placas = new List<Placa>();
        public Vector2Int? Marcador { get; private set; }         // onde fica o "CONSTRUIR" (canvas)
        public Vector2Int? Expansao { get; private set; }         // centro da próxima expansão (canvas)
        public Vector2Int? Chamado { get; private set; }          // onde flutua o chamado urgente (canvas)

        /// <summary>Falso esconde o contorno tracejado da próxima expansão (a faixa não mostra).</summary>
        public bool MostrarExpansao = true;

        /// <summary>Retângulo da sala atual (paredes, piso e laje) na tela, em pixels com origem em cima à esquerda.</summary>
        public RectInt AreaDaSala
        {
            get
            {
                if (desenhandoArte) return areaArte;
                if (dSala == null) return new RectInt(0, 0, Largura, Altura);
                int esquerda = dSala.P(0, D).x, direita = dSala.P(W, 0).x, topo = dSala.P(0, 0).y - AlturaParede, baixo = dSala.P(W, D).y + 6;
                return new RectInt(esquerda, topo, direita - esquerda, baixo - topo);
            }
        }

        string ultimaCompra;
        float ultimaCompraEm = -10;

        /// <summary>Avisa a sala de uma compra: o equipamento novo solta faíscas por um instante.</summary>
        public void Comprou(string id, float agora) { ultimaCompra = id; ultimaCompraEm = agora; }

        // quadros dos personagens (pixels de cima para baixo, já recortados)
        class Quadro { public Color32[] px; public int w, h; }
        Quadro[] tecnico, estagiario, engenheiro;
        int cargoDosPersonagens = -1;

        readonly List<(float prof, Action desenhar)> fila = new List<(float, Action)>();
        float t;

        public SalaIso(Economia economia) { E = economia; }

        // ---------------- Montagem ----------------

        void Montar()
        {
            cargoMontado = E.Cargo;
            var tam = Tamanhos[Mathf.Clamp(E.Cargo, 0, Tamanhos.Length - 1)];
            var prox = Tamanhos[Mathf.Clamp(E.Cargo + 1, 0, Tamanhos.Length - 1)];
            W = tam.x; D = tam.y;
            // a tela cabe a próxima sala (para o contorno da expansão), com margem para as placas
            int largura = (prox.x + prox.y) * IsoDesenho.TW / 2 + 40, altura = (prox.x + prox.y) * IsoDesenho.TH / 2 + AlturaParede + 44;
            telaSala = new PixelCanvas(largura, altura);
            dSala = new IsoDesenho(telaSala) { Ox = 20 + prox.y * IsoDesenho.TW / 2, Oy = AlturaParede + 30 };
            if (cargoDosPersonagens != E.Cargo) CarregarPersonagens();
        }

        void CarregarPersonagens()
        {
            cargoDosPersonagens = E.Cargo;
            Quadro Q(Texture2D tex)
            {
                var r = ArteGerada.AreaOpaca(tex);
                var todos = ArteGerada.PixelsDeCimaParaBaixo(tex);
                var px = new Color32[r.width * r.height];
                int topo = tex.height - r.yMax;  // AreaOpaca usa origem embaixo; os pixels vêm de cima
                for (int y = 0; y < r.height; y++)
                for (int x = 0; x < r.width; x++)
                    px[y * r.width + x] = todos[(topo + y) * tex.width + r.x + x];
                return new Quadro { px = px, w = r.width, h = r.height };
            }
            tecnico = new Quadro[4];
            estagiario = new Quadro[4];
            for (int i = 0; i < 4; i++)
            {
                tecnico[i] = Q(Uniformes.TexturaDoCargo("tecnico_andar_" + i, "tecnico", E.Cargo));
                estagiario[i] = Q(Uniformes.TexturaDoCargo("estagiario_andar_" + i, "estagiario", E.Cargo));
            }
            engenheiro = new Quadro[4];
            for (int i = 0; i < 4; i++) engenheiro[i] = Q(IsoSprites.Engenheiro(i));
        }

        // ---------------- Desenho ----------------

        public enum Vista { Sala, Campus, Mundo }

        /// <param name="vista">Sala (dentro do DC-01), Campus (Arquiteto em diante) ou Mundo (CTO).</param>
        public void Desenhar(float tempo, Vista vista = Vista.Sala)
        {
            t = tempo;
            Alvos.Clear(); Placas.Clear(); fila.Clear();
            Marcador = null; Expansao = null; Chamado = null;
            if (vista == Vista.Mundo) { DesenharMundo(); return; }
            if (vista == Vista.Campus)
            {
                if (cargoDosPersonagens != E.Cargo) CarregarPersonagens();
                DesenharCampus();
                return;
            }
            if (TemArteNova)
            {
                if (cargoDosPersonagens != E.Cargo) CarregarPersonagens();
                DesenharArte();
                return;
            }
            desenhandoArte = false;
            if (cargoMontado != E.Cargo || telaSala == null) Montar();
            tela = telaSala; d = dSala;
            tela.Limpar(new Color32(0, 0, 0, 0));

            PisoEParedes();
            if (MostrarExpansao) ContornoDaExpansao();
            MontarObjetos();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompra();
            ChamadoUrgente();
            tela.Aplicar();
        }

        (Color32 a, Color32 b, Color32 linha, Color32 parede, Color32 parede2, Color32 friso) Paleta()
        {
            switch (Mathf.Clamp(E.Cargo, 0, 4))
            {
                case 0: return (IsoDesenho.C("82595c"), IsoDesenho.C("6b4a4f"), IsoDesenho.C("573c41"), IsoDesenho.C("33376a"), IsoDesenho.C("2d3057"), IsoDesenho.C("1f2140"));
                case 1: return (IsoDesenho.C("8a90a8"), IsoDesenho.C("7c8198"), IsoDesenho.C("5f6479"), IsoDesenho.C("414a7c"), IsoDesenho.C("3b4270"), IsoDesenho.C("1f2140"));
                case 2: return (IsoDesenho.C("a3acc0"), IsoDesenho.C("949db3"), IsoDesenho.C("6c7389"), IsoDesenho.C("4a5680"), IsoDesenho.C("46517a"), IsoDesenho.C("56638f"));
                case 3: return (IsoDesenho.C("3e4668"), IsoDesenho.C("373e5e"), IsoDesenho.C("2a3050"), IsoDesenho.C("232a4f"), IsoDesenho.C("1f2648"), IsoDesenho.C("2e7d8f"));
                default: return (IsoDesenho.C("343b5a"), IsoDesenho.C("2e3451"), IsoDesenho.C("232842"), IsoDesenho.C("1c2240"), IsoDesenho.C("181d38"), IsoDesenho.C("ffa53c"));
            }
        }

        void PisoEParedes()
        {
            var p = Paleta();
            for (int gx = 0; gx < W; gx++)
            for (int gy = 0; gy < D; gy++)
            {
                bool perfurada = E.Cargo == 2 && (gx + gy * 2) % 5 == 0;   // piso técnico: algumas placas com furos
                d.Piso(gx, gy, 1, 1, (gx + gy) % 2 == 0 ? p.a : p.b);
                if (perfurada)
                    for (int i = 1; i < 4; i++)
                    for (int j = 1; j < 4; j++)
                        d.Ponto(d.P(gx + i / 4f, gy + j / 4f), p.linha);
            }
            for (int gx = 0; gx <= W; gx++) d.Linha(d.P(gx, 0), d.P(gx, D), p.linha);
            for (int gy = 0; gy <= D; gy++) d.Linha(d.P(0, gy), d.P(W, gy), p.linha);
            // borda da frente do piso (espessura da laje)
            d.Poligono(IsoDesenho.Escurecer(p.b, 0.6f), d.P(0, D), d.P(W, D), d.P(W, D, -5), d.P(0, D, -5));
            d.Poligono(IsoDesenho.Escurecer(p.b, 0.5f), d.P(W, 0), d.P(W, D), d.P(W, D, -5), d.P(W, 0, -5));
            d.Paredes(W, D, AlturaParede, p.parede, p.parede2, IsoDesenho.C("1b1a2e"), p.friso);

            // quadrinho na parede (desde o armário) e janela da cidade à esquerda
            d.NaParedeEsq(0.4f, 1.6f, 28, 44, IsoDesenho.C("1b1a2e"));
            d.NaParedeEsq(0.5f, 1.5f, 30, 42, IsoDesenho.C("7cc8ff"));
            d.NaParedeEsq(0.9f, 1.5f, 30, 35, IsoDesenho.C("5cc26a"));
        }

        void ContornoDaExpansao()
        {
            if (E.Cargo >= Tamanhos.Length - 1) return;
            var prox = Tamanhos[E.Cargo + 1];
            var cor = IsoDesenho.C("ffb458");
            void Tracejado(Vector2Int a, Vector2Int b)
            {
                int passos = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                for (int i = 0; i <= passos; i++)
                    if ((i / 3) % 2 == 0) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(a, b, (float)i / Mathf.Max(1, passos))), cor);
            }
            Tracejado(d.P(W, 0), d.P(prox.x, 0));
            Tracejado(d.P(prox.x, 0), d.P(prox.x, prox.y));
            Tracejado(d.P(prox.x, prox.y), d.P(0, prox.y));
            Tracejado(d.P(0, prox.y), d.P(0, D));
            Expansao = d.P((W + prox.x) / 2f, (D + prox.y) / 2f);
        }

        // ---------------- Objetos ----------------

        void Adicionar(float gx, float gy, float w, float dd, Action desenhar) => fila.Add((gx + w + gy + dd, desenhar));

        void Clicavel(float gx, float gy, float w, float dd, int h, string tipo, float z = 0)
        {
            var pts = new[] { d.P(gx, gy, z + h), d.P(gx + w, gy, z + h), d.P(gx + w, gy + dd, z), d.P(gx, gy + dd, z), d.P(gx + w, gy + dd, z + h) };
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (var p in pts) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            Alvos.Add(new Alvo { Area = new RectInt(x0, y0, x1 - x0, y1 - y0), Tipo = tipo });
        }

        void NovaPlaca(string setor, string nome, float gx, float gy, float z, Color cor)
        {
            if (E.Cargo < CargoDoSetor(setor)) return;
            Placas.Add(new Placa { Setor = setor, Nome = nome, Pos = d.P(gx, gy, z), Cor = cor });
        }

        bool Piscar(float periodo = 0.25f) => Mathf.FloorToInt(t / periodo) % 2 == 0;
        Color32 Led(int semente, Color32 cor, float taxa = 3f) =>
            Mathf.Sin(t * taxa + semente * 1.7f) > -0.3f ? cor : IsoDesenho.C("22263c");

        void Alerta(float gx, float gy, float z)
        {
            if (!Piscar()) return;
            var p = d.P(gx, gy, z);
            tela.Texto("!", p.x - 1, p.y - 6, IsoDesenho.C("ff3b4e"), true, 2);
        }

        void MontarObjetos()
        {
            // --- sempre: planta, mesa do técnico com CRT ---
            Adicionar(0.15f, 0.15f, 0.6f, 0.6f, () => Planta(0.15f, 0.15f));
            Adicionar(0.1f, 2f, 0.9f, 1.6f, () => MesaComCrt(0.1f, 2f, E.Escrevendo || E.DeployQuebrado));
            Clicavel(0.55f, 3.0f, 0.4f, 0.4f, 14, "cafe", 11);   // caneca na ponta da mesa
            if (E.TemEstagiario) Adicionar(0.1f, 3.9f, 0.9f, 0.9f, () => MesaSimples(0.1f, 3.9f));

            // --- Compute: torres (até 3), rack 42U, racks cheios, hypervisor, containers, CI, Kubernetes ---
            NovaPlaca("Compute", "Compute", 3.2f, 0.5f, AlturaParede + 6, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(3, E.Torres); i++)
            {
                float gx = 2f + i, gy = 0.2f; int idx = i;
                Adicionar(gx, gy, 0.7f, 0.7f, () => Torre(gx, gy, E.Travado(idx), idx));
                Clicavel(gx, gy, 0.7f, 0.7f, 24, "servidor:" + i);
            }
            if (E.TemRack)
            {
                bool travado = false;
                for (int s = E.Torres; s < E.TotalServidores; s++) travado |= E.Travado(s);
                bool trav = travado;
                Adicionar(5.1f, 0.1f, 0.8f, 0.8f, () => Rack(5.1f, 0.1f, 44, E.ServidoresRack, 5, trav ? "vermelho" : "verde", 1));
                Clicavel(5.1f, 0.1f, 0.8f, 0.8f, 44, "rack");
            }
            for (int i = 0; i < Mathf.Min(4, E.RacksCheios); i++)
            {
                float gx = 4f + i; int semente = 10 + i;
                Adicionar(gx, 3f, 0.8f, 0.8f, () => Rack(gx, 3f, 44, 8, 8, "azul", semente));
                Clicavel(gx, 3f, 0.8f, 0.8f, 44, "equipamento");
            }
            if (E.NivelHypervisor > 0)
            {
                Adicionar(8f, 3f, 0.8f, 0.8f, () => Rack(8f, 3f, 44, 8, 8, "lilas", 20, IsoDesenho.C("3a3350"), IsoDesenho.C("2c2640")));
                Clicavel(8f, 3f, 0.8f, 0.8f, 44, "equipamento");
            }
            for (int i = 0; i < Mathf.Min(4, E.HostsContainers); i++)
            {
                float gx = 4f + i; int semente = 30 + i;
                Adicionar(gx, 5.2f, 0.8f, 0.8f, () => HostContainers(gx, 5.2f, semente));
                Clicavel(gx, 5.2f, 0.8f, 0.8f, 30, "containers");
            }
            if (E.TemCi)
            {
                Adicionar(8f, 5.2f, 0.8f, 0.8f, () => ServidorCi(8f, 5.2f));
                Clicavel(8f, 5.2f, 0.8f, 0.8f, 36, "containers");
            }
            for (int i = 0; i < Mathf.Min(6, E.NosKubernetes); i++)
            {
                float gx = 9f + i; int semente = 40 + i;
                Adicionar(gx, 8.6f, 0.7f, 0.7f, () => NoKubernetes(gx, 8.6f, semente));
                Clicavel(gx, 8.6f, 0.7f, 0.7f, 26, "k8s");
            }
            if (E.TemBalanceador) Adicionar(15f, 8.6f, 0.8f, 0.8f, () => Balanceador(15f, 8.6f));

            // --- Energia: no-breaks encostados na parede da esquerda ---
            NovaPlaca("Energia", "Energia", 0.4f, 5.9f, 40, IsoGui.Laranja);
            var posNoBreak = new[] { new Vector2(0.1f, 5.2f), new Vector2(0.1f, 6.1f), new Vector2(1.0f, 6.1f) };
            for (int i = 0; i < Mathf.Min(3, E.Nivel(Catalogo.NoBreak)); i++)
            {
                var p = posNoBreak[i];
                Adicionar(p.x, p.y, 0.8f, 0.7f, () => NoBreak(p.x, p.y));
            }

            // --- Refrigeração: ar de precisão na parede do fundo ---
            NovaPlaca("Refrigeracao", "Refrigeração", 7.2f, 0.5f, AlturaParede + 6, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(3, E.Nivel(Catalogo.ArCondicionado)); i++)
            {
                float gx = 6.2f + i;
                Adicionar(gx, 0.1f, 0.8f, 0.8f, () => ArCondicionado(gx, 0.1f));
            }

            // --- Storage e backup ---
            NovaPlaca("Storage", "Storage", 10.2f, 0.5f, AlturaParede + 6, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(3, E.NivelStorage); i++)
            {
                float gx = 9.2f + i; bool queimado = i == 0 && E.DiscoQueimado;
                Adicionar(gx, 0.1f, 0.8f, 0.8f, () => Storage(gx, 0.1f, queimado));
                Clicavel(gx, 0.1f, 0.8f, 0.8f, 32, "storage");
            }
            if (E.TemBackup) Adicionar(11.2f, 1.1f, 0.7f, 0.6f, () => Fita(11.2f, 1.1f));

            // --- Rede: caixas na parede da esquerda e o rack de 10G ---
            NovaPlaca("Rede", "Rede", 0.3f, 7.9f, 52, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(2, E.Nivel(Catalogo.Link)); i++)
            {
                float gy = 7.2f + i;
                Adicionar(0.05f, gy, 0.3f, 0.7f, () => CaixaDeRede(0.05f, gy));
            }
            if (E.Nivel(Catalogo.Link10G) > 0) Adicionar(1f, 8.1f, 0.7f, 0.7f, () => Rack(1f, 8.1f, 30, 3, 3, E.LinkSaturado ? "vermelho" : "laranja", 50));

            // --- Laboratório (automações) ---
            if (E.AutomacoesLiberadas)
            {
                NovaPlaca("Automacao", "Laboratório", 10f, 5.4f, 36, IsoGui.Roxo);
                Adicionar(9f, 5f, 2f, 0.8f, () => Laboratorio(9f, 5f));
            }

            // --- NOC (DevOps em diante) e telão ---
            if (E.Cargo >= 3)
            {
                NovaPlaca("NOC", "NOC", 5.5f, 7.4f, 34, IsoGui.Cyan);
                Adicionar(4f, 7f, 3f, 0.8f, () => Noc(4f, 7f));
                Clicavel(4f, 7f, 3f, 0.8f, 20, "noc");
            }

            // --- marcador de construção no próximo lugar livre do equipamento principal ---
            MarcadorDeConstrucao();

            // --- pessoas e drone ---
            Personagens();
        }

        void MarcadorDeConstrucao()
        {
            Vector2? lugar = null;
            switch (E.Cargo)
            {
                case 0: if (E.Torres < 3) lugar = new Vector2(2f + E.Torres, 0.2f); break;
                case 1: lugar = new Vector2(5.1f, 0.1f); break;
                case 2: if (E.RacksCheios < 4) lugar = new Vector2(4f + E.RacksCheios, 3f); break;
                case 3: if (E.HostsContainers < 4) lugar = new Vector2(4f + E.HostsContainers, 5.2f); break;
                default: if (E.NosKubernetes < 6) lugar = new Vector2(9f + E.NosKubernetes, 8.6f); break;
            }
            if (lugar == null) return;
            var l = lugar.Value;
            bool ocupado = E.Cargo == 1 && E.TemRack;   // no Sysadmin o marcador fica no rack (servidores 1U entram nele)
            if (!ocupado)
                fila.Add((l.x + l.y + 1.6f, () =>
                {
                    var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                    var a = d.P(l.x, l.y); var b = d.P(l.x + 0.8f, l.y); var c = d.P(l.x + 0.8f, l.y + 0.8f); var e = d.P(l.x, l.y + 0.8f);
                    foreach (var (p, q) in new[] { (a, b), (b, c), (c, e), (e, a) })
                    {
                        int n = Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y));
                        for (int i = 0; i <= n; i += 2) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(p, q, (float)i / Mathf.Max(1, n))), cor);
                    }
                }));
            Marcador = d.P(l.x + 0.4f, l.y + 0.4f, ocupado ? 90 : 0);   // no Sysadmin, acima do rack
        }

        // ---------------- Efeitos ----------------

        /// <summary>Onde está a unidade mais nova de cada compra (para as faíscas de instalação).</summary>
        Vector2? LugarDoItem(string id)
        {
            int n(string i) => Mathf.Max(1, E.Nivel(i));
            switch (id)
            {
                case Catalogo.Servidor: return new Vector2(2.35f + Mathf.Min(3, E.Torres) - 1, 0.55f);
                case Catalogo.Ssd: case Catalogo.Ventoinha: return new Vector2(2.35f, 0.55f);
                case Catalogo.Rack: case Catalogo.Servidor1U: return new Vector2(5.5f, 0.5f);
                case Catalogo.RackCheio: return new Vector2(4.4f + Mathf.Min(4, n(id)) - 1, 3.4f);
                case Catalogo.Hypervisor: return new Vector2(8.4f, 3.4f);
                case Catalogo.Containers: return new Vector2(4.4f + Mathf.Min(4, n(id)) - 1, 5.6f);
                case Catalogo.ServidorCi: return new Vector2(8.4f, 5.6f);
                case Catalogo.NoKubernetes: return new Vector2(9.35f + Mathf.Min(6, n(id)) - 1, 8.95f);
                case Catalogo.Balanceador: return new Vector2(15.4f, 9f);
                case Catalogo.NoBreak: return n(id) == 1 ? new Vector2(0.5f, 5.55f) : n(id) == 2 ? new Vector2(0.5f, 6.45f) : new Vector2(1.4f, 6.45f);
                case Catalogo.ArCondicionado: return new Vector2(6.6f + Mathf.Min(3, n(id)) - 1, 0.5f);
                case Catalogo.Storage: return new Vector2(9.6f + Mathf.Min(3, n(id)) - 1, 0.5f);
                case Catalogo.Backup: return new Vector2(11.55f, 1.4f);
                case Catalogo.Link: return new Vector2(0.2f, 7.55f + Mathf.Min(2, n(id)) - 1);
                case Catalogo.Link10G: return new Vector2(1.35f, 8.45f);
                case Catalogo.Estagiario: return new Vector2(0.55f, 4.35f);
                case Catalogo.Observabilidade: return new Vector2(5.5f, 7.4f);
                default: return null;
            }
        }

        void FaiscasDaCompra()
        {
            float idade = t - ultimaCompraEm;
            if (idade > 1.2f || ultimaCompra == null) return;
            var lugar = LugarDoItem(ultimaCompra);
            if (lugar == null) return;
            var centro = d.P(lugar.Value.x, lugar.Value.y, 20);
            for (int i = 0; i < 14; i++)
            {
                float ang = i * 0.449f + i * i * 0.07f, raio = 4 + idade * (22 + (i % 4) * 6);
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 14));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 2, 2, cor);
            }
        }

        /// <summary>Chamado urgente: um papel flutuando sobre a mesa do técnico, com a barra do tempo que resta.</summary>
        void ChamadoUrgente()
        {
            if (!E.TemChamado) return;
            float bob = Mathf.Sin(t * 4) * 2;
            var p = d.P(1.2f, 2.8f, 62 + bob);
            string[] papel =
            {
                "yyyyyyyyy.",
                "yWWWWWyyyy",
                "yyyyyyyyyy",
                "yWWWWWWWyy",
                "yyyyyyyyyy",
                "yWWWWWyyyy",
                "yyyyyyyyyy",
                "yWWWWWWWyy",
                "yyyyyyyyyy",
            };
            tela.Ret(p.x - 6, p.y - 11, 12, 11, IsoDesenho.C("1b1a2e"));
            tela.Mapa(papel, p.x - 5, p.y - 10);
            tela.Ret(p.x + 3, p.y - 10, 2, 2, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)(E.SegundosDoChamado / Catalogo.TempoParaAtender);
            tela.Ret(p.x - 6, p.y + 2, 12, 2, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 6, p.y + 2, Mathf.RoundToInt(12 * resta), 2, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 8, p.y - 13, 16, 18), Tipo = "chamado" });
        }

        // ---------------- Pessoas ----------------

        void Personagens()
        {
            // técnico e estagiário passeiam no corredor da frente das torres; engenheiros extras chegam com a carreira
            float corredor = E.Cargo >= 2 ? 4.3f : Mathf.Min(D - 1.2f, 3.2f);
            Andando(tecnico, 1.4f, W - 1.3f, 1.55f, 0f, 0.55f);
            if (E.TemEstagiario) Andando(estagiario, 1.4f, W - 1.8f, corredor, 0.37f, 0.5f);
            int extras = Mathf.Clamp(E.Cargo - 1, 0, 3);
            for (int i = 0; i < extras; i++) Andando(engenheiro, 1.6f, W - 2f, i % 2 == 0 ? D - 1.3f : corredor + 0.1f, 0.6f + i * 0.21f, 0.4f + i * 0.07f);

            if (E.TemAutomacao(Catalogo.Watchdog))
            {
                float gx = W * 0.5f + Mathf.Sin(t * 0.4f) * W * 0.3f, gy = D * 0.5f + Mathf.Cos(t * 0.4f) * D * 0.25f;
                fila.Add((gx + gy + 50, () => Drone(gx, gy)));   // voa por cima de tudo
            }
        }

        void Andando(Quadro[] quadros, float gx0, float gx1, float gy, float fase, float velocidade)
        {
            if (gx1 <= gx0) return;
            float ciclo = (t * velocidade * 0.25f + fase) % 2f;
            float u = ciclo < 1f ? ciclo : 2f - ciclo;
            float gx = Mathf.Lerp(gx0, gx1, u);
            bool voltando = ciclo >= 1f;
            fila.Add((gx + gy + 0.4f, () =>
            {
                var q = quadros[Mathf.FloorToInt(t * 6f + fase * 10) % quadros.Length];
                var p = d.P(gx, gy);
                // sombra
                tela.Ret(p.x - 5, p.y - 1, 10, 2, new Color32(20, 20, 40, 90));
                // o sprite olha para a direita; indo "para trás" (gx diminuindo) ele é espelhado
                for (int y = 0; y < q.h; y++)
                for (int x = 0; x < q.w; x++)
                {
                    var c = q.px[y * q.w + (voltando ? q.w - 1 - x : x)];
                    if (c.a > 0) tela.Pixel(p.x - q.w / 2 + x, p.y - q.h + y, c);
                }
            }));
        }

        void Drone(float gx, float gy)
        {
            var p = d.P(gx, gy, 70 + Mathf.Sin(t * 3) * 3);
            string[] mapa = { "w.....w", "wwwwwww", ".wkckw.", "..www.." };
            tela.Mapa(mapa, p.x - 3, p.y - 2);
            if (Piscar(0.4f)) tela.Pixel(p.x, p.y - 3, IsoDesenho.C("5cff8a"));
            var chao = d.P(gx, gy);
            tela.Ret(chao.x - 2, chao.y, 5, 1, new Color32(20, 20, 40, 60));   // sombra no piso
        }

        // ---------------- Móveis e equipamentos ----------------

        void Planta(float gx, float gy)
        {
            d.Caixa(gx + 0.1f, gy + 0.1f, 0.4f, 0.4f, 8, IsoDesenho.C("e0805a"), IsoDesenho.C("b85c3a"), IsoDesenho.C("9a4a2e"));
            var p = d.P(gx + 0.3f, gy + 0.3f, 8);
            tela.Circulo(p.x, p.y - 6, 5, IsoDesenho.C("3f9b54"));
            tela.Circulo(p.x - 3, p.y - 9, 3, IsoDesenho.C("6fd36f"));
            tela.Circulo(p.x + 3, p.y - 10, 3, IsoDesenho.C("5cc26a"));
        }

        void MesaComCrt(float gx, float gy, bool digitando)
        {
            MesaSimples(gx, gy, 1.6f);
            // troféus das empresas vendidas (prestígio)
            for (int i = 0; i < Mathf.Min(5, E.Prestigio.trofeus.Count); i++)
            {
                var tr = d.P(gx + 0.45f, gy + 0.15f + i * 0.2f, 14);
                tela.Ret(tr.x - 2, tr.y - 6, 4, 3, IsoDesenho.C("ffd65c"));
                tela.Ret(tr.x - 1, tr.y - 3, 2, 2, IsoDesenho.C("d9b23a"));
                tela.Ret(tr.x - 2, tr.y - 1, 4, 1, IsoDesenho.C("b8932a"));
            }
            // caneca: com vapor quando dá para tomar café; brilhando enquanto o café faz efeito
            var corCaneca = E.CafeAtivo && Piscar(0.3f) ? IsoDesenho.C("ffd65c") : IsoDesenho.C("ff8c7a");
            d.Caixa(gx + 0.6f, gy + 1.25f, 0.18f, 0.18f, 6, corCaneca, IsoDesenho.C("d9604f"), IsoDesenho.C("b84a3c"), 14);
            if (E.PodeTomarCafe || E.CafeAtivo)
                for (int i = 0; i < 2; i++)
                {
                    float f = (t * 0.7f + i * 0.5f) % 1f;
                    var v = d.P(gx + 0.69f, gy + 1.34f, 21 + f * 10);
                    tela.Pixel(v.x + (i == 0 ? 0 : 1), v.y, new Color32(230, 230, 240, (byte)((1 - f) * 200)));
                }
            // CRT bege virado para o corredor (face direita)
            d.Caixa(gx + 0.15f, gy + 0.4f, 0.5f, 0.6f, 14, IsoDesenho.C("dccca6"), IsoDesenho.C("b9a67f"), IsoDesenho.C("c9b88f"), 14);
            var a = d.FaceDir(gx + 0.15f, gy + 0.4f, 0.5f, 0.6f, 0.15f, 16); var b = d.FaceDir(gx + 0.15f, gy + 0.4f, 0.5f, 0.6f, 0.85f, 16);
            d.Poligono(IsoDesenho.C("0b1a14"), a, b, new Vector2Int(b.x, b.y - 10), new Vector2Int(a.x, a.y - 10));
            for (int l = 0; l < 3; l++)
                if (!digitando || Mathf.FloorToInt(t * 8) % 3 != l)
                    d.Linha(new Vector2Int(a.x + 1, a.y - 8 + l * 3), new Vector2Int(a.x + 1 + 4 + (l * 3) % 5, a.y - 8 + l * 3 - 2), IsoDesenho.C(digitando ? "5cff8a" : "3fbf6a"));
        }

        void MesaSimples(float gx, float gy, float profundidade = 0.9f)
        {
            var madeira = IsoDesenho.C("b07a52");
            d.Caixa(gx, gy, 0.9f, profundidade, 3, madeira, IsoDesenho.C("7d5238"), IsoDesenho.C("8e5f40"), 11);
            // pés
            d.Linha(d.P(gx + 0.85f, gy + profundidade - 0.05f, 11), d.P(gx + 0.85f, gy + profundidade - 0.05f, 0), IsoDesenho.C("5a3a2e"));
            d.Linha(d.P(gx + 0.85f, gy + 0.05f, 11), d.P(gx + 0.85f, gy + 0.05f, 0), IsoDesenho.C("5a3a2e"));
        }

        void Torre(float gx, float gy, bool travado, int semente)
        {
            d.Caixa(gx, gy, 0.7f, 0.7f, 24, IsoDesenho.C("e8dcbc"), IsoDesenho.C("dccca6"), IsoDesenho.C("b9a67f"));
            // baias de disco e botão na frente (face esquerda, virada para o corredor)
            for (int i = 0; i < 3; i++) d.FaixaEsq(gx, gy, 0.7f, 0.7f, 0.2f, 0.8f, 20 - i * 3, IsoDesenho.C("8c7a58"));
            if (E.TemSsd) d.FaixaEsq(gx, gy, 0.7f, 0.7f, 0.25f, 0.5f, 10, IsoDesenho.C("5aa9ff"));
            d.Ponto(d.FaceEsq(gx, gy, 0.7f, 0.7f, 0.75f, 5), travado ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018")) : Led(semente, IsoDesenho.C("5cff8a")), 2);
            if (travado) Alerta(gx + 0.35f, gy + 0.35f, 36);
        }

        /// <summary>Rack: gabinete escuro com unidades em faixas e LEDs (cor por tipo). "unidades" acesas de "total".</summary>
        void Rack(float gx, float gy, int h, int unidades, int total, string led, int semente, Color32? corpo = null, Color32? lado = null)
        {
            var escuro = corpo ?? IsoDesenho.C("2a3142");
            d.Caixa(gx, gy, 0.8f, 0.8f, h, IsoDesenho.Escurecer(escuro, 1.3f), escuro, lado ?? IsoDesenho.C("1f2533"));
            var corLed = led == "vermelho" ? IsoDesenho.C("ff3b4e") : led == "azul" ? IsoDesenho.C("5cc8ff") : led == "lilas" ? IsoDesenho.C("c9b8ff")
                       : led == "laranja" ? IsoDesenho.C("ffa53c") : IsoDesenho.C("5cff8a");
            float passo = (h - 8f) / total;
            for (int u = 0; u < total; u++)
            {
                float z = h - 5 - u * passo;
                d.FaixaEsq(gx, gy, 0.8f, 0.8f, 0.1f, 0.9f, z, IsoDesenho.C("3d4660"));
                if (u < unidades)
                {
                    var c = led == "vermelho" ? (Piscar() ? corLed : IsoDesenho.C("3a1018")) : Led(semente * 7 + u, corLed, 2.5f + u % 3);
                    d.Ponto(d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.2f, z - 1), c);
                    d.Ponto(d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.3f, z - 1), Led(semente * 3 + u, IsoDesenho.C("5cff8a"), 4f));
                }
            }
            if (led == "vermelho") Alerta(gx + 0.4f, gy + 0.4f, h + 12);
        }

        void HostContainers(float gx, float gy, int semente)
        {
            d.Caixa(gx, gy, 0.8f, 0.8f, 30, IsoDesenho.C("3a4460"), IsoDesenho.C("2a3142"), IsoDesenho.C("1f2533"));
            string[] cores = { "5cc8ff", "5cff8a", "ff8cc6", "ffd65c", "b48cff" };
            for (int linha = 0; linha < 3; linha++)
            for (int i = 0; i < 3; i++)
            {
                var p = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.18f + i * 0.25f, 24 - linha * 7);
                Color32 c = E.DeployQuebrado ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018"))
                          : Mathf.Sin(t * 2 + semente + i * 3 + linha) > -0.2f ? IsoDesenho.C(cores[(semente + i + linha) % cores.Length]) : IsoDesenho.C("22263c");
                tela.Ret(p.x, p.y - 2, 3, 3, c);
            }
            if (E.DeployQuebrado && semente == 30) Alerta(gx + 0.4f, gy + 0.4f, 42);
        }

        void ServidorCi(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.8f, 0.8f, 36, IsoDesenho.C("8d92ab"), IsoDesenho.C("6d7390"), IsoDesenho.C("5d6178"));
            var a = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.25f, 24); var b = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.75f, 24);
            d.Poligono(IsoDesenho.C("0f1a2a"), a, b, new Vector2Int(b.x, b.y - 8), new Vector2Int(a.x, a.y - 8));
            var cor = E.DeployQuebrado ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("5cff8a");
            // "check" (ou X) na telinha
            if (E.DeployQuebrado) { d.Linha(new Vector2Int(a.x + 4, a.y - 7), new Vector2Int(a.x + 8, a.y - 1), cor); d.Linha(new Vector2Int(a.x + 8, a.y - 7), new Vector2Int(a.x + 4, a.y - 1), cor); }
            else { d.Linha(new Vector2Int(a.x + 3, a.y - 4), new Vector2Int(a.x + 5, a.y - 2), cor); d.Linha(new Vector2Int(a.x + 5, a.y - 2), new Vector2Int(a.x + 9, a.y - 7), cor); }
            for (int i = 0; i < 3; i++) d.Ponto(d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.3f + i * 0.2f, 8), Led(60 + i, IsoDesenho.C("5cc8ff")));
        }

        void NoKubernetes(float gx, float gy, int semente)
        {
            d.Caixa(gx, gy, 0.7f, 0.7f, 26, IsoDesenho.C("4a5270"), IsoDesenho.C("343b58"), IsoDesenho.C("272d45"));
            bool pico = E.EmPico && !E.PicoFoiEscalado, escalado = E.EmPico && E.PicoFoiEscalado;
            for (int l = 0; l < 4; l++)
            {
                float z = 22 - l * 5;
                d.FaixaEsq(gx, gy, 0.7f, 0.7f, 0.1f, 0.9f, z, IsoDesenho.C("56608a"));
                Color32 c = pico ? (Piscar() ? IsoDesenho.C(E.PicoViolado ? "ff3b4e" : "ffa53c") : IsoDesenho.C("5a3a1a"))
                          : Led(semente + l, IsoDesenho.C(escalado ? "5cc8ff" : "5cff8a"), escalado ? 12f : 3f);
                d.Ponto(d.FaceEsq(gx, gy, 0.7f, 0.7f, 0.25f, z - 2), c);
                d.Ponto(d.FaceEsq(gx, gy, 0.7f, 0.7f, 0.4f, z - 2), c);
            }
            // o "leme" do Kubernetes no topo, bem pequeno
            var topo = d.P(gx + 0.35f, gy + 0.35f, 26);
            tela.Pixel(topo.x, topo.y - 1, IsoDesenho.C("5cc8ff"));
            if (pico && semente == 40) Alerta(gx + 0.35f, gy + 0.35f, 38);
        }

        void Balanceador(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.8f, 0.8f, 12, IsoDesenho.C("8d92ab"), IsoDesenho.C("6d7390"), IsoDesenho.C("5d6178"));
            var a = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.2f, 6); var b = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.8f, 6);
            d.Linha(a, b, IsoDesenho.C("ffd65c"));
            d.Ponto(a, IsoDesenho.C("ffd65c"), 2); d.Ponto(b, IsoDesenho.C("ffd65c"), 2);
        }

        void NoBreak(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.8f, 0.7f, 18, IsoDesenho.C("4a4f63"), IsoDesenho.C("3a3f53"), IsoDesenho.C("2e3244"));
            // visor e LEDs na face direita (virada para dentro da sala)
            var a = d.FaceDir(gx, gy, 0.8f, 0.7f, 0.2f, 14); var b = d.FaceDir(gx, gy, 0.8f, 0.7f, 0.6f, 14);
            d.Poligono(IsoDesenho.C(E.Sobrecarga ? "5a1a24" : "1a3a2a"), a, b, new Vector2Int(b.x, b.y - 4), new Vector2Int(a.x, a.y - 4));
            d.Ponto(d.FaceDir(gx, gy, 0.8f, 0.7f, 0.8f, 6), E.Sobrecarga ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018")) : Led((int)(gy * 10), IsoDesenho.C("5cff8a")));
        }

        void ArCondicionado(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.8f, 0.8f, 34, IsoDesenho.C("e8edf5"), IsoDesenho.C("c9d0dc"), IsoDesenho.C("a9b0bf"));
            for (int i = 0; i < 6; i++) d.FaixaEsq(gx, gy, 0.8f, 0.8f, 0.15f, 0.85f, 28 - i * 3, IsoDesenho.C("8d94a6"));
            d.Ponto(d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.8f, 8), E.Quente ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018")) : IsoDesenho.C("5cff8a"));
            // ar frio subindo
            for (int i = 0; i < 3; i++)
            {
                float f = (t * 0.8f + i * 0.33f) % 1f;
                var p = d.P(gx + 0.2f + i * 0.2f, gy + 0.4f, 34 + f * 14);
                tela.Pixel(p.x, p.y, new Color32(169, 228, 255, (byte)((1 - f) * 200)));
            }
        }

        void Storage(float gx, float gy, bool queimado)
        {
            d.Caixa(gx, gy, 0.8f, 0.8f, 32, IsoDesenho.C("5d6480"), IsoDesenho.C("454b63"), IsoDesenho.C("363b50"));
            for (int linha = 0; linha < 5; linha++)
            for (int i = 0; i < 4; i++)
            {
                var p = d.FaceEsq(gx, gy, 0.8f, 0.8f, 0.14f + i * 0.2f, 27 - linha * 5);
                tela.Ret(p.x, p.y - 1, 2, 2, IsoDesenho.C("8d94a6"));
                Color32 c = queimado && linha == 2 && i == 1 ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018")) : Led(linha * 4 + i + (int)gx, IsoDesenho.C("5cff8a"), 5f);
                tela.Pixel(p.x + 2, p.y - 1, c);
            }
            if (queimado) Alerta(gx + 0.4f, gy + 0.4f, 44);
        }

        void Fita(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.7f, 0.6f, 20, IsoDesenho.C("9aa3b8"), IsoDesenho.C("7c8198"), IsoDesenho.C("6c7389"));
            var p = d.FaceEsq(gx, gy, 0.7f, 0.6f, 0.45f, 12);
            tela.Circulo(p.x, p.y - 1, 3, IsoDesenho.C("2a2d44"));
            tela.Pixel(p.x + (Mathf.FloorToInt(t * 4) % 2 == 0 ? 1 : -1), p.y - 1, IsoDesenho.C("5cc8ff"));
        }

        void CaixaDeRede(float gx, float gy)
        {
            // caixa presa na parede, com cabos laranja e azuis descendo até o chão
            d.Caixa(gx, gy, 0.3f, 0.7f, 12, IsoDesenho.C("8d92ab"), IsoDesenho.C("6d7390"), IsoDesenho.C("5d6178"), 22);
            for (int i = 0; i < 4; i++)
            {
                var a = d.FaceDir(gx, gy, 0.3f, 0.7f, 0.2f + i * 0.2f, 22);
                var cor = IsoDesenho.C(i % 2 == 0 ? "ff9f43" : "5cc8ff");
                d.Linha(a, new Vector2Int(a.x + 2 + i, a.y + 18), cor);
                d.Ponto(d.FaceDir(gx, gy, 0.3f, 0.7f, 0.2f + i * 0.2f, 30), E.LinkSaturado ? (Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("3a1018")) : Led(i + (int)(gy * 5), IsoDesenho.C("5cff8a"), 6f));
            }
        }

        void Laboratorio(float gx, float gy)
        {
            d.Caixa(gx, gy, 2f, 0.8f, 3, IsoDesenho.C("e8edf5"), IsoDesenho.C("c9d0dc"), IsoDesenho.C("a9b0bf"), 11);
            d.Linha(d.P(gx + 1.95f, gy + 0.75f, 11), d.P(gx + 1.95f, gy + 0.75f, 0), IsoDesenho.C("8d94a6"));
            d.Linha(d.P(gx + 0.05f, gy + 0.75f, 11), d.P(gx + 0.05f, gy + 0.75f, 0), IsoDesenho.C("8d94a6"));
            // um monitor por automação ativa (até 4), com o script rodando
            int monitores = Mathf.Clamp(E.AutomacoesAtivas, 1, 4);
            for (int i = 0; i < monitores; i++)
            {
                float mx = gx + 0.15f + i * 0.45f;
                d.Caixa(mx, gy + 0.1f, 0.35f, 0.15f, 10, IsoDesenho.C("2a3142"), IsoDesenho.C("1f2533"), IsoDesenho.C("1b1f2c"), 14);
                var a = d.FaceEsq(mx, gy + 0.1f, 0.35f, 0.15f, 0.15f, 21);
                bool ativa = i < E.AutomacoesAtivas;
                d.Linha(a, new Vector2Int(a.x + 6, a.y + 3), IsoDesenho.C(ativa ? "b48cff" : "3a3350"));
                if (ativa && Mathf.FloorToInt(t * 3 + i) % 2 == 0) d.Linha(new Vector2Int(a.x, a.y + 3), new Vector2Int(a.x + 4, a.y + 5), IsoDesenho.C("5cff8a"));
            }
            // frasco do "laboratório"
            var f = d.P(gx + 1.8f, gy + 0.4f, 14);
            tela.Ret(f.x - 1, f.y - 5, 3, 5, IsoDesenho.C(E.Escrevendo && Piscar(0.5f) ? "5cff8a" : "b48cff"));
        }

        void Noc(float gx, float gy)
        {
            d.Caixa(gx, gy, 3f, 0.8f, 3, IsoDesenho.C("3a4460"), IsoDesenho.C("2a3142"), IsoDesenho.C("1f2533"), 11);
            int incidentes = E.Travamentos.Count + (E.DiscoQueimado ? 1 : 0) + (E.DeployQuebrado ? 1 : 0) + (E.EmPico && !E.PicoFoiEscalado ? 1 : 0);
            for (int i = 0; i < 4; i++)
            {
                float mx = gx + 0.2f + i * 0.7f;
                d.Caixa(mx, gy + 0.1f, 0.55f, 0.15f, 12, IsoDesenho.C("2a3142"), IsoDesenho.C("0f1a2a"), IsoDesenho.C("1b1f2c"), 14);
                // gráfico rolando em cada tela (vermelho se há incidente)
                for (int k = 0; k < 7; k++)
                {
                    var p = d.FaceEsq(mx, gy + 0.1f, 0.55f, 0.15f, 0.1f + k * 0.12f, 18 + Mathf.RoundToInt(Mathf.Sin(t * 2 + k + i) * 3));
                    tela.Pixel(p.x, p.y, IsoDesenho.C(incidentes > 0 && i == 0 ? (Piscar() ? "ff3b4e" : "5a1a24") : i % 2 == 0 ? "5cff8a" : "5cc8ff"));
                }
            }
            // telão do NOC na parede do fundo (tráfego ao vivo; laranja durante um pico)
            if (W > 12)
            {
                d.NaParedeDir(12.2f, 13.9f, 18, 46, IsoDesenho.C("1b1a2e"));
                d.NaParedeDir(12.3f, 13.8f, 20, 44, IsoDesenho.C("0f1a2a"));
                string cor = E.EmPico ? (E.PicoFoiEscalado ? "5cc8ff" : "ffa53c") : "5cff8a";
                for (int k = 0; k < 20; k++)
                {
                    float x = 12.35f + k * 0.07f;
                    float nivel = E.EmPico ? 0.8f : 0.35f;
                    float z = 22 + (nivel + Mathf.Sin(t * 1.5f + k * 0.6f) * 0.12f) * 18;
                    d.Ponto(d.P(x, 0.03f, z), IsoDesenho.C(cor));
                }
            }
        }
    }
}
