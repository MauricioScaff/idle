using System.Globalization;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Modo gerente: a mesma janela da faixa cresce para a área de trabalho inteira e mostra o data center
    /// em vista isométrica (sala desenhada em pixel art pela SalaIso; interface vinda do protótipo feito no Codex). Usa a mesma Economia
    /// da faixa, então dinheiro, compras, automações e promoções são os mesmos nos dois modos.
    /// A sala cresce a cada promoção e mostra tudo o que foi comprado.
    /// É o modo principal do jogo; "Ir para a faixa" encolhe tudo para a faixa discreta acima da barra de tarefas.
    /// </summary>
    public partial class ModoGerente : MonoBehaviour
    {
        const float W = 1440, H = 900;
        readonly Rect areaCentral = new Rect(184, 94, 978, 676);

        Faixa faixa;
        Economia E;
        IsoGui ui;

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
            salaIso = new SalaIso(E);
            IniciarPrimeiraHora();

            // avisos na barra de notícias (os sons e o save continuam por conta da faixa)
            E.AutomacaoPronta += id => Notificar("Automação pronta: " + Catalogo.BuscarAutomacao(id).Nome, 8);
            E.Travou += _ => Notificar("Servidor travou. Clique no NOC ou em RESOLVER.");
            E.DiscoQueimou += () => Notificar("Disco queimou no storage. Clique em STORAGE para trocar.");
            E.DeployQuebrou += () => Notificar("Deploy quebrou. Clique em DEPLOY para o rollback.");
            E.PicoComecou += nome => Notificar("Pico de tráfego: " + nome + "! Escale o cluster.", 10);
            E.QuedaDeEnergia += dc => Notificar("Queda de energia no DC-0" + (dc + 1) + "! Clique no prédio apagado para religar.", 8);
            E.PaneRegional += r => Notificar("Pane regional: " + Catalogo.NomesRegioes[r] + " fora do ar! Clique na região para redirecionar o tráfego.", 8);
            E.Promoveu += c => Notificar("Promovido a " + E.CargoAtual.Nome + "! Novos setores liberados.", 10);
        }

        public void Abrir()
        {
            Aberto = true;
            janela = "";
            selecionado = "Visao";
            Notificar("Data center de " + E.CargoAtual.Nome + ". \"Ir para a faixa\" deixa o jogo discreto enquanto você trabalha.", 7);
        }

        public void Fechar() => Aberto = false;

        void Update()
        {
            if (!Aberto) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !string.IsNullOrEmpty(janela)) Abrir("Visao");   // Esc fecha a janela aberta
            flashCompra = Mathf.Max(0, flashCompra - Time.unscaledDeltaTime);
            AtualizarSala();
        }

        void OnDestroy()
        {
            ui?.Dispose();
        }

        void Notificar(string texto, float segundos = 5) { aviso = texto; avisoAte = Time.unscaledTime + segundos; }

        /// <summary>Aviso vindo de fora (por exemplo, o resumo de quando o jogo estava fechado).</summary>
        public void Avisar(string texto, float segundos) => Notificar(texto, segundos);

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

        int Incidentes => E.Travamentos.Count + (E.DiscoQueimado ? 1 : 0) + (E.DeployQuebrado ? 1 : 0) + (E.TemQuedaDeEnergia ? 1 : 0) + (E.TemPaneRegional ? 1 : 0);

        /// <summary>Resolve tudo o que está quebrado agora (como clicar em cada coisa na faixa).</summary>
        void Resolver()
        {
            int quantidade = Incidentes;
            for (int i = E.Travamentos.Count - 1; i >= 0; i--) faixa.Reiniciar(E.Travamentos[i].servidor);
            if (E.DiscoQueimado) faixa.TrocarDisco();
            if (E.DeployQuebrado) faixa.FazerRollback();
            if (E.TemQuedaDeEnergia) faixa.Religar();
            if (E.TemPaneRegional) faixa.Redirecionar();
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
            Tutorial();
            Festa();
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
