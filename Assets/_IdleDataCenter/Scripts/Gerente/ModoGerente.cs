using System.Globalization;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Modo gerente (a tela principal): a mesma janela da faixa cresce para uma janela normal e mostra o data center
    /// em vista isométrica, grande, na frente da cidade (que muda com a hora). A interface flutua por cima em poucas
    /// caixas: dinheiro, meta e cargo no topo; à direita o que pede atenção (ou a próxima compra); embaixo LOJA,
    /// AUTOMAÇÃO e CARREIRA. Usa a mesma Economia da faixa, então tudo vale nos dois modos.
    /// </summary>
    public partial class ModoGerente : MonoBehaviour
    {
        const float W = 1440, H = 900;
        /// <summary>Onde a sala cabe, entre o topo e a barra de botões (a sala pode passar por baixo das caixas).</summary>
        readonly Rect areaSala = new Rect(180, 136, 1080, 664);
        readonly CidadeFundo cidade = new CidadeFundo();

        Faixa faixa;
        Economia E;
        IsoGui ui;

        string janela = "", aviso = "";
        float avisoAte, flashCompra;
        double ultimoDeploy;
        int pagina;

        public bool Aberto { get; private set; }

        public void Iniciar(Faixa faixa, Economia economia)
        {
            this.faixa = faixa;
            E = economia;
            ui = new IsoGui();
            salaIso = new SalaIso(E) { MostrarExpansao = false, PessoasSoltas = true };
            IniciarPrimeiraHora();
            IniciarTerminal();
            IniciarClientes();

            // avisos na barra de notícias (os sons e o save continuam por conta da faixa)
            E.AutomacaoPronta += id => Notificar("Automação pronta: " + Catalogo.BuscarAutomacao(id).Nome, 8);
            E.Travou += _ => Notificar("Servidor travou. Clique no NOC ou em Reiniciar.");
            E.DiscoQueimou += () => Notificar(E.NivelStorage > 0 ? "Disco queimou no storage. Clique no storage para trocar." : "O HD da torre queimou! Clique na torre com o alerta para trocar o disco.");
            E.DeployQuebrou += () => Notificar("Deploy quebrou. Clique em Deploy para o rollback.");
            E.PicoComecou += nome => Notificar("Pico de tráfego: " + nome + "! Escale o cluster.", 10);
            E.QuedaDeEnergia += dc => Notificar("Queda de energia no DC-0" + (dc + 1) + "! Clique no prédio apagado para religar.", 8);
            E.PaneRegional += r => Notificar("Pane regional: " + Catalogo.NomesRegioes[r] + " fora do ar! Clique na região para redirecionar o tráfego.", 8);
            E.Promoveu += c => Notificar((c == Catalogo.CargoTecnico ? "Contratado como " : "Promovido a ") + E.CargoAtual.Nome + "! Novos setores liberados.", 10);
        }

        public void Abrir()
        {
            Aberto = true;
            janela = "";
            Notificar("Data center de " + E.CargoAtual.Nome + ". O botão Faixa deixa o jogo discreto enquanto você trabalha.", 7);
        }

        public void Fechar() => Aberto = false;

        void Update()
        {
            if (!Aberto) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !terminalAberto && !string.IsNullOrEmpty(janela)) Abrir("Visao");   // Esc fecha a janela aberta
            flashCompra = Mathf.Max(0, flashCompra - Time.unscaledDeltaTime);
            AtualizarSala();
            AtualizarTerminal();
        }

        void OnDestroy()
        {
            ui?.Dispose();
        }

        void Notificar(string texto, float segundos = 5) { aviso = texto; avisoAte = Time.unscaledTime + segundos; }

        /// <summary>Abre direto numa seção (teste: -gerente=prestigio) ou numa área (-gerente=dados, -gerente=rede).</summary>
        public void AbrirSecao(string secao)
        {
            if (secao == "dados") { vistaEscolhida = SalaIso.Vista.Dados; return; }
            if (secao == "rede") { vistaEscolhida = SalaIso.Vista.Rede; return; }
            Abrir(char.ToUpperInvariant(secao[0]) + secao.Substring(1));
        }

        /// <summary>Aviso vindo de fora (por exemplo, o resumo de quando o jogo estava fechado).</summary>
        public void Avisar(string texto, float segundos) => Notificar(texto, segundos);

        void Abrir(string secao)
        {
            janela = secao == "Visao" ? "" : secao;
            pagina = 0;
        }

        /// <summary>Teste: força a hora da cidade ao fundo (0 a 24).</summary>
        public void DefinirHora(float hora) => cidade.HoraFixa = hora;

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
                Notificar(E.Cargo < Catalogo.CargoDevOps ? "Deploys chegam no cargo Engenheiro DevOps." : "Instale um host de containers para fazer deploys.");
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
            // a cidade cobre a janela inteira, inclusive as bordas que sobram fora da tela de 1440 x 900
            cidade.Atualizar();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), cidade.Textura, ScaleMode.ScaleAndCrop);
            var anterior = GUI.matrix;
            float escala = Mathf.Min(Screen.width / W, Screen.height / H);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * escala) / 2, (Screen.height - H * escala) / 2, 0),
                Quaternion.identity, new Vector3(escala, escala, 1));
            EventosDoTerminal();   // antes de tudo: aberto, o terminal fica com o teclado e os cliques dele
            DesenharSala();
            Hud();
            PainelHelpDesk();
            PainelClientes();
            CartaoAtencao();
            Barra();
            BotaoDoTerminal();
            if (!Tutorial() && Time.unscaledTime < avisoAte) Aviso(aviso);
            Festa();
            if (!string.IsNullOrEmpty(janela)) Loja();
            Terminal();
            Confetes();   // por cima da loja: a compra pode ter sido feita nela
            GUI.matrix = anterior;
        }
    }
}
