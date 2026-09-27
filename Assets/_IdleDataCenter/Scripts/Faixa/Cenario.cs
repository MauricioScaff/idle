using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O cômodo onde o técnico trabalha, montado conforme o cargo: armário (Técnico de TI), salinha com rack,
    /// no-break e refrigeração (Sysadmin) ou sala de racks com storage, backup e link (Analista de Infra).
    /// No DevOps, a sala de racks escurece e ganha o canto virtualizado (DevOpsVisual).
    /// Na promoção, o cenário inteiro é destruído e montado de novo.
    /// Coordenadas locais: x a partir da borda esquerda, y a partir de baixo (piso em y = AlturaPiso).
    /// </summary>
    public class Cenario : MonoBehaviour
    {
        public const int Altura = 60, AlturaPiso = 6;
        const float DistanciaServidor = 17f;   // do centro do técnico ao centro da torre

        /// <summary>Onde fica cada coisa em cada cômodo (x do centro, no piso).</summary>
        class Planta
        {
            public int Largura;
            public float[] Torres;               // da direita para a esquerda (a primeira é a do começo do jogo)
            public float Rack, NoBreak, Refrigeracao, Storage, Fita;
            public float[] RacksCheios = new float[0];
            public Vector2 Link;
            public float Hypervisor, Ci, Balanceador;
            public float[] NosKubernetes = new float[0];
            public Vector2 Telao;
            public float[] HostsContainers = new float[0];
        }

        static readonly Planta Armario = new Planta { Largura = 200, Torres = new float[] { 176, 152, 128 } };
        static readonly Planta Salinha = new Planta { Largura = 290, Torres = new float[] { 170, 146, 122 }, Rack = 200, NoBreak = 226, Refrigeracao = 262 };
        // Na sala de racks o storage e o backup ficam no caminho do técnico (ele troca disco na frente deles)
        static readonly Planta SalaDeRacks = new Planta
        {
            Largura = 456, Torres = new float[] { 234, 210, 186 }, Rack = 262, RacksCheios = new float[] { 288, 314, 340, 366 },
            NoBreak = 392, Refrigeracao = 428, Storage = 124, Fita = 150, Link = new Vector2(392, 26),
        };
        // DevOps: a sala de racks ganha o canto virtualizado à direita (hypervisor, containers, CI e a esteira)
        static readonly Planta SalaVirtualizada = new Planta
        {
            Largura = 624, Torres = SalaDeRacks.Torres, Rack = SalaDeRacks.Rack, RacksCheios = SalaDeRacks.RacksCheios,
            NoBreak = SalaDeRacks.NoBreak, Refrigeracao = SalaDeRacks.Refrigeracao, Storage = SalaDeRacks.Storage,
            Fita = SalaDeRacks.Fita, Link = SalaDeRacks.Link,
            Hypervisor = 474, HostsContainers = new float[] { 498, 518, 538, 558 }, Ci = 596,
        };
        // SRE: o data center pequeno ganha o cluster Kubernetes, o balanceador e o telão do NOC à direita
        static readonly Planta DataCenterPequeno = new Planta
        {
            Largura = 780, Torres = SalaDeRacks.Torres, Rack = SalaDeRacks.Rack, RacksCheios = SalaDeRacks.RacksCheios,
            NoBreak = SalaDeRacks.NoBreak, Refrigeracao = SalaDeRacks.Refrigeracao, Storage = SalaDeRacks.Storage,
            Fita = SalaDeRacks.Fita, Link = SalaDeRacks.Link,
            Hypervisor = SalaVirtualizada.Hypervisor, HostsContainers = SalaVirtualizada.HostsContainers, Ci = SalaVirtualizada.Ci,
            NosKubernetes = new float[] { 628, 642, 656, 670, 684, 698 }, Balanceador = 724, Telao = new Vector2(624, 30),
        };

        Faixa faixa;
        Economia economia;
        readonly List<ServidorVelho> torres = new List<ServidorVelho>();
        Planta planta;
        RackVisual rack;
        readonly List<RackVisual> racksCheios = new List<RackVisual>();
        EquipamentoVisual noBreak, refrigeracao, storage, fita, link;
        RackVisual hypervisor;
        DevOpsVisual devOps;
        SreVisual sre;
        Tecnico estagiario;

        public int Largura { get; private set; }
        public TelaTerminal Tela { get; private set; }
        public Tecnico Tecnico { get; private set; }
        public ServidorVelho PrimeiraTorre => torres[0];

        /// <summary>Onde o técnico para para digitar (em frente à mesa).</summary>
        public float PosicaoMesa => 74f;
        /// <summary>O técnico anda até aqui (fica ao lado da torre mais à esquerda).</summary>
        public float LimiteTecnico => torres[torres.Count - 1].X - DistanciaServidor;
        public Vector2 TopoMaisProximo => torres[torres.Count - 1].Topo;
        public Vector2 TopoDoIncidente { get; private set; }
        /// <summary>Tem automação sendo escrita: o técnico passa a maior parte do tempo digitando na mesa.</summary>
        public bool EscrevendoAutomacao => economia.Escrevendo;

        public void Montar(Faixa faixa, Economia economia, Vector2 posicao)
        {
            this.faixa = faixa;
            this.economia = economia;
            transform.position = posicao;
            planta = economia.Cargo >= 4 ? DataCenterPequeno : economia.Cargo == 3 ? SalaVirtualizada : economia.Cargo == 2 ? SalaDeRacks : economia.Cargo == 1 ? Salinha : Armario;
            Largura = planta.Largura;

            var fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = economia.Cargo >= 2 ? PixelArt.SalaDeRacks(Largura, Altura, AlturaPiso, escura: economia.Cargo >= 3)
                         : economia.Cargo == 1 ? PixelArt.Salinha(Largura, Altura, AlturaPiso)
                         : PixelArt.Armario(Largura, Altura, AlturaPiso);
            gameObject.AddComponent<BoxCollider2D>(); // o cômodo inteiro "segura" o clique

            Decoracao("Planta", ArteGerada.Objeto("planta"), new Vector2(13, AlturaPiso), 2);
            MontarMesa();
            RelogioParede.Criar(Decoracao("Relogio", Arte.Relogio, new Vector2(104, 42), 1));

            AtualizarEquipamentos();

            Decoracao("Caneca", Arte.Caneca, torres[0].Topo + new Vector2(-2, -1), 4).gameObject.AddComponent<Vapor>();

            Tecnico = new GameObject("Tecnico").AddComponent<Tecnico>();
            Tecnico.Iniciar(faixa, this, economia.Cargo);
            AtualizarEquipamentos(); // o estagiário, se já foi contratado, entra depois do técnico
        }

        /// <summary>Mesa com o CRT (arte do PixelLab); a tela ganha um terminal animado por cima do texto desenhado.</summary>
        void MontarMesa()
        {
            var mesa = Decoracao("Mesa", ArteGerada.Objeto("mesa_crt"), new Vector2(44, AlturaPiso), 2);
            // o texto verde da tela é achado pela cor; o terminal animado cobre exatamente essa área
            var texto = ArteGerada.Leds(mesa.sprite);
            int x0 = texto.Min(p => p.x), x1 = texto.Max(p => p.x), y0 = texto.Min(p => p.y), y1 = texto.Max(p => p.y);
            Tela = TelaTerminal.Criar(mesa.transform, new Vector2(x0, y0), x1 - x0 + 1, y1 - y0 + 1, CorDeFundoDaTela(mesa.sprite, x0, y0, x1, y1), 3);
        }

        /// <summary>A cor escura mais comum dentro da tela (o fundo do CRT), ignorando o texto verde.</summary>
        static Color32 CorDeFundoDaTela(Sprite s, int x0, int y0, int x1, int y1)
        {
            var contagem = new Dictionary<Color32, int>();
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Color32 c = s.texture.GetPixel((int)(s.rect.x + s.pivot.x) + x, (int)(s.rect.y + s.pivot.y) + y);
                if (c.a == 0 || c.g > 140) continue;
                contagem[c] = contagem.TryGetValue(c, out int n) ? n + 1 : 1;
            }
            return contagem.Count > 0 ? contagem.OrderByDescending(k => k.Value).First().Key : new Color32(20, 40, 28, 255);
        }

        public SpriteRenderer Decoracao(string nome, Sprite sprite, Vector2 posicaoLocal, int ordem)
        {
            var sr = new GameObject(nome).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.localPosition = posicaoLocal;
            sr.sprite = sprite;
            sr.sortingOrder = ordem;
            return sr;
        }

        /// <summary>Cria o que foi comprado e ainda não está no cenário. Retorna a torre nova, se surgiu uma.</summary>
        public ServidorVelho AtualizarEquipamentos()
        {
            ServidorVelho nova = null;
            while (torres.Count < economia.Torres && torres.Count < planta.Torres.Length)
            {
                nova = new GameObject("Torre").AddComponent<ServidorVelho>();
                nova.Iniciar(faixa, transform, new Vector2(planta.Torres[torres.Count], AlturaPiso), torres.Count);
                torres.Add(nova);
            }
            foreach (var t in torres) t.AtualizarVisual(economia.TemSsd, economia.Ventoinhas);

            if (economia.TemRack && rack == null)
            {
                rack = new GameObject("Rack").AddComponent<RackVisual>();
                rack.Iniciar(faixa, transform, new Vector2(planta.Rack, AlturaPiso));
            }
            rack?.DefinirQuantidade(economia.ServidoresRack);

            if (economia.Nivel(Catalogo.NoBreak) > 0 && noBreak == null)
            {
                noBreak = new GameObject("NoBreak").AddComponent<EquipamentoVisual>();
                noBreak.Iniciar(transform, "nobreak", new Vector2(planta.NoBreak, AlturaPiso), false);
            }
            if (economia.Nivel(Catalogo.ArCondicionado) > 0 && refrigeracao == null)
            {
                refrigeracao = new GameObject("Refrigeracao").AddComponent<EquipamentoVisual>();
                refrigeracao.Iniciar(transform, "refrigeracao", new Vector2(planta.Refrigeracao, AlturaPiso), true);
            }
            while (racksCheios.Count < economia.RacksCheios && racksCheios.Count < planta.RacksCheios.Length)
            {
                var r = new GameObject("RackCheio").AddComponent<RackVisual>();
                r.Iniciar(faixa, transform, new Vector2(planta.RacksCheios[racksCheios.Count], AlturaPiso), faixa.ClicarEquipamento);
                r.DefinirQuantidade(int.MaxValue);
                racksCheios.Add(r);
                if (Tecnico != null) faixa.Faiscas(r.Topo, 4);
            }
            if (economia.NivelStorage > 0 && storage == null)
            {
                storage = new GameObject("Storage").AddComponent<EquipamentoVisual>();
                storage.Iniciar(transform, "storage", new Vector2(planta.Storage, AlturaPiso), false);
                storage.TornarClicavel(() => faixa.ClicarStorage(storage.Topo));
            }
            if (economia.TemBackup && fita == null)
            {
                fita = new GameObject("Fita").AddComponent<EquipamentoVisual>();
                fita.Iniciar(transform, "fita", new Vector2(planta.Fita, AlturaPiso), false);
            }
            if (economia.Nivel(Catalogo.Link) > 0 && link == null)
            {
                link = new GameObject("Link").AddComponent<EquipamentoVisual>();
                link.Iniciar(transform, "link", planta.Link, false);
            }
            if (economia.NivelHypervisor > 0 && hypervisor == null)
            {
                hypervisor = new GameObject("Hypervisor").AddComponent<RackVisual>();
                hypervisor.Iniciar(faixa, transform, new Vector2(planta.Hypervisor, AlturaPiso), faixa.ClicarEquipamento);
                hypervisor.DefinirQuantidade(int.MaxValue);
                hypervisor.Tingir(PixelArt.Hex("c9b8ff"));   // VMs: o rack ganha um tom lilás
                if (Tecnico != null) faixa.Faiscas(hypervisor.Topo, 4);
            }
            if (planta.HostsContainers.Length > 0 && (economia.HostsContainers > 0 || economia.TemCi))
            {
                if (devOps == null)
                {
                    devOps = new GameObject("DevOps").AddComponent<DevOpsVisual>();
                    devOps.Iniciar(faixa, economia, transform, planta.HostsContainers, planta.Ci);
                }
                else devOps.Atualizar();
            }
            if (planta.NosKubernetes.Length > 0 && economia.NosKubernetes > 0)
            {
                if (sre == null)
                {
                    sre = new GameObject("SRE").AddComponent<SreVisual>();
                    sre.Iniciar(faixa, economia, transform, planta.NosKubernetes, planta.Balanceador, planta.Telao);
                }
                else sre.Atualizar();
            }
            if (economia.TemEstagiario && estagiario == null && Tecnico != null)
            {
                estagiario = new GameObject("Estagiario").AddComponent<Tecnico>();
                estagiario.Iniciar(faixa, this, economia.Cargo, "estagiario", 14f);
            }
            return nova;
        }

        /// <summary>Onde soltar efeitos de um servidor (topo da torre, ou topo do rack para os 1U).</summary>
        public Vector2 TopoDoServidor(int servidor) =>
            economia.EhTorre(servidor) && servidor < torres.Count ? torres[servidor].Topo
            : rack != null ? rack.Topo : TopoMaisProximo;

        public void LimparEsteira() => devOps?.LimparEsteira();

        public void PularTudo()
        {
            foreach (var t in torres) t.Pular();
            rack?.Pular();
            foreach (var r in racksCheios) r.Pular();
            hypervisor?.Pular();
        }

        /// <summary>Reflete os servidores travados (LEDs vermelhos e alertas), a sobrecarga e o calor.</summary>
        public void AtualizarIncidentes()
        {
            for (int i = 0; i < torres.Count; i++) torres[i].DefinirTravado(economia.Travado(i));
            rack?.DefinirTravados(vaga => economia.Travado(economia.Torres + vaga));
            noBreak?.DefinirAlerta(economia.Sobrecarga);
            refrigeracao?.DefinirAlerta(economia.Quente);
            storage?.DefinirAlerta(economia.DiscoQueimado);
            link?.DefinirAlerta(economia.LinkSaturado);
        }

        /// <summary>Para onde o técnico deve correr: o servidor travado há mais tempo.</summary>
        public bool AlvoDeConserto(out float x)
        {
            x = 0;
            var lista = economia.Travamentos;
            if (lista.Count == 0)
            {
                // deploy quebrado: o rollback é feito no terminal da mesa
                if (economia.DeployQuebrado)
                {
                    x = PosicaoMesa;
                    TopoDoIncidente = (Vector2)(Tela.transform.position - transform.position) + new Vector2(4, 6);
                    return true;
                }
                // disco queimado: ele troca o disco de frente para o storage
                if (!economia.DiscoQueimado || storage == null) return false;
                x = planta.Storage;
                TopoDoIncidente = storage.Topo;
                return true;
            }
            int s = lista[0].servidor;
            if (economia.EhTorre(s) && s < torres.Count)
            {
                x = torres[s].X - DistanciaServidor;
                TopoDoIncidente = torres[s].Topo;
            }
            else
            {
                x = LimiteTecnico; // o rack fica atrás das torres: ele conserta do lado
                TopoDoIncidente = rack != null ? rack.Topo : TopoMaisProximo;
            }
            return true;
        }
    }
}
