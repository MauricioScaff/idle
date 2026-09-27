using System.Globalization;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Modo gerente: a mesma janela da faixa cresce para a área de trabalho inteira e mostra o data center
    /// em vista isométrica (ilustração e interface vindas do protótipo feito no Codex). Usa a mesma Economia
    /// da faixa, então dinheiro, compras, automações e promoções são os mesmos nos dois modos.
    /// Os setores ainda não liberados pelo cargo aparecem escurecidos, com a placa "libera no ...".
    /// Esc (ou "Voltar à faixa") fecha.
    /// </summary>
    public partial class ModoGerente : MonoBehaviour
    {
        const float W = 1440, H = 900;
        readonly Rect areaCentral = new Rect(184, 94, 978, 676);

        Faixa faixa;
        Economia E;
        IsoGui ui;
        Texture2D ambiente, sala, rack, drone;
        int cargoDaSala = -1;
        readonly Texture2D[] passos = new Texture2D[4];
        readonly Rect[] passosUv = new Rect[4];
        readonly float[] passosAspecto = new float[4];
        Texture2D rackDesenhado;

        string selecionado = "Visao", janela = "", aviso = "";
        float avisoAte, flashCompra;
        double ultimoDeploy;
        int pagina;

        public bool Aberto { get; private set; }

        public void Iniciar(Faixa faixa, Economia economia)
        {
            this.faixa = faixa;
            E = economia;
            ui = new IsoGui();
            ambiente = Resources.Load<Texture2D>("Isometrico/DataCenter");
            rack = Resources.Load<Texture2D>("Isometrico/Rack");
            if (rack == null) rack = rackDesenhado = IsoSprites.Rack(true);
            drone = IsoSprites.Drone();
            for (int i = 0; i < passos.Length; i++)
            {
                passos[i] = IsoSprites.Engenheiro(i);
                var area = ArteGerada.AreaOpaca(passos[i]);
                passosUv[i] = new Rect((float)area.x / passos[i].width, (float)area.y / passos[i].height,
                    (float)area.width / passos[i].width, (float)area.height / passos[i].height);
                passosAspecto[i] = (float)area.width / area.height;
            }

            // avisos na barra de notícias (os sons e o save continuam por conta da faixa)
            E.AutomacaoPronta += id => Notificar("Automação pronta: " + Catalogo.BuscarAutomacao(id).Nome, 8);
            E.Travou += _ => Notificar("Servidor travou. Clique no NOC ou em RESOLVER.");
            E.DiscoQueimou += () => Notificar("Disco queimou no storage. Clique em STORAGE para trocar.");
            E.DeployQuebrou += () => Notificar("Deploy quebrou. Clique em DEPLOY para o rollback.");
            E.PicoComecou += nome => Notificar("Pico de tráfego: " + nome + "! Escale o cluster.", 10);
            E.Promoveu += c => Notificar("Promovido a " + E.CargoAtual.Nome + "! Novos setores liberados.", 10);
        }

        public void Abrir()
        {
            Aberto = true;
            janela = "";
            selecionado = "Visao";
            Notificar("Data center de " + E.CargoAtual.Nome + ". Esc volta para a faixa.", 6);
        }

        public void Fechar() => Aberto = false;

        void Update()
        {
            if (!Aberto) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!string.IsNullOrEmpty(janela)) Abrir("Visao");
                else faixa.FecharGerente();
            }
            flashCompra = Mathf.Max(0, flashCompra - Time.unscaledDeltaTime);
        }

        void OnDestroy()
        {
            ui?.Dispose();
            if (rackDesenhado != null) Destroy(rackDesenhado);
            if (drone != null) Destroy(drone);
            foreach (var p in passos) if (p != null) Destroy(p);
        }

        void Notificar(string texto, float segundos = 5) { aviso = texto; avisoAte = Time.unscaledTime + segundos; }

        void Abrir(string secao)
        {
            selecionado = secao;
            janela = secao == "Visao" ? "" : secao;
            pagina = 0;
        }

        static string Numero(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
        static string Dinheiro(double n) => "R$ " + Faixa.Formatar(n);

        // ---------------- Ações ----------------

        /// <summary>No modo gerente dá para comprar também o que ficou para trás nos cargos anteriores.</summary>
        bool PodeComprarAqui(string id) => Catalogo.Buscar(id).Cargo <= E.Cargo && E.PodeComprar(id);

        void Comprar(string id)
        {
            var def = Catalogo.Buscar(id);
            if (def.Cargo > E.Cargo) { Notificar("Libera no cargo " + Catalogo.Cargos[def.Cargo].Nome + "."); return; }
            if (!E.Comprar(id))
            {
                Notificar(E.NoMaximo(id) ? "Já está no nível máximo."
                    : !E.RequisitoOk(id) ? "Precisa antes: " + Catalogo.Buscar(def.Requisito).Nome + "."
                    : "Falta " + Dinheiro(E.Custo(id) - E.Dinheiro) + ".");
                return;
            }
            flashCompra = 1;
            Notificar(NomeLongo(id) + " instalado. A receita já mudou.");
        }

        void Escrever(AutomacaoDef a)
        {
            if (!E.PodeEscrever(a.Id)) return;
            faixa.EscreverAutomacao(a.Id);
            Notificar("O técnico começou a escrever: " + a.Nome + ".");
        }

        int Incidentes => E.Travamentos.Count + (E.DiscoQueimado ? 1 : 0) + (E.DeployQuebrado ? 1 : 0);

        /// <summary>Resolve tudo o que está quebrado agora (como clicar em cada coisa na faixa).</summary>
        void Resolver()
        {
            int quantidade = Incidentes;
            for (int i = E.Travamentos.Count - 1; i >= 0; i--) faixa.Reiniciar(E.Travamentos[i].servidor);
            if (E.DiscoQueimado) faixa.TrocarDisco();
            if (E.DeployQuebrado) faixa.FazerRollback();
            Notificar(quantidade > 0 ? quantidade + (quantidade == 1 ? " incidente resolvido." : " incidentes resolvidos.") : "Tudo funcionando.");
        }

        void Deploy()
        {
            if (E.DeployQuebrado) { faixa.FazerRollback(); Notificar("Rollback feito: os apps voltaram."); return; }
            if (E.HostsContainers == 0)
            {
                Abrir("Compute");
                Notificar(E.Cargo < 3 ? "Deploys chegam no cargo Engenheiro DevOps." : "Instale um host de containers para fazer deploys.");
                return;
            }
            if (Time.realtimeSinceStartupAsDouble - ultimoDeploy < 1) return;
            ultimoDeploy = Time.realtimeSinceStartupAsDouble;
            double ganho = E.ClicarEquipamento();
            Sons.Moeda();
            Notificar("Deploy entregue: +" + Dinheiro(ganho));
        }

        void Escalar()
        {
            if (E.Escalar()) Notificar("Cluster escalado: o pico rende o dobro.");
        }

        // ---------------- Desenho ----------------

        void OnGUI()
        {
            if (!Aberto || ui == null) return;
            ui.Ret(new Rect(0, 0, Screen.width, Screen.height), IsoGui.Fundo);   // cobre a faixa por trás
            // nos primeiros quadros depois de crescer, o Unity ainda desenha no tamanho antigo (da faixa): só o fundo
            if (Screen.height < 400) return;
            var anterior = GUI.matrix;
            float escala = Mathf.Min(Screen.width / W, Screen.height / H);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * escala) / 2, (Screen.height - H * escala) / 2, 0),
                Quaternion.identity, new Vector3(escala, escala, 1));
            DesenharSala();
            Topo(); Navegacao(); Objetivos(); Rodape();
            if (!string.IsNullOrEmpty(janela)) Loja();
            if (Time.unscaledTime < avisoAte)
            {
                var r = new Rect(226, 739, 890, 28);
                ui.Caixa(r, IsoGui.Cor("162c40"), IsoGui.Cyan);
                ui.Texto(aviso, r.center.x, r.y + 9, IsoGui.Branco, 2, true);
            }
            GUI.matrix = anterior;
        }
    }
}
