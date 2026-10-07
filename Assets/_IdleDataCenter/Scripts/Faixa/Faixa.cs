using System;
using IdleDataCenter.Gerente;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Liga tudo: a economia, o monitor da faixa, o painel de gestão, o modo gerente,
    /// os cliques (com "clique atravessando" nas áreas vazias), o save e o progresso offline.
    /// </summary>
    public class Faixa : MonoBehaviour
    {
        const int PainelX = 8;
        const float IntervaloSalvamento = 30f;

        static readonly Color Amarelo = PixelArt.Hex("ffd65c"), VerdeClaro = PixelArt.Hex("9be89b"),
                              Laranja = PixelArt.Hex("ffbf3f"), Vermelho = PixelArt.Hex("ff3b4e"),
                              Azul = PixelArt.Hex("a9c7ff");

        Economia economia;
        Camera cam;
        JanelaDesktop janela;
        MonitorFaixa monitor;
        Painel painel;
        ResumoOffline resumo;
        ModoGerente gerente;
        IClicavel sobCursor;
        float proximoSalvamento;
        double ganhoOffline;
        float larguraSimulada;     // teste: -simular-largura=<pixels de arte>

        void Awake()
        {
            economia = new Economia(Salvamento.Carregar());
            ganhoOffline = economia.AplicarOffline(Salvamento.AgoraUnix);
            economia.Comprou += AoComprar;
            economia.Travou += _ => { Avisar("Servidor travou!", 2f, Vermelho); Sons.Alerta(); };
            economia.Voltou += AoVoltar;
            economia.Promoveu += AoPromover;
            economia.DiscoQueimou += () => { Avisar("Disco queimou!", 2f, Vermelho); Sons.Alerta(); };
            economia.DiscoTrocado += AoTrocarDisco;
            economia.AutomacaoPronta += AoFicarProntaAutomacao;
            economia.DeployQuebrou += () => { Avisar("Deploy quebrou!", 2f, Vermelho); Sons.Alerta(); };
            economia.DeployVoltou += peloTecnico =>
            {
                Avisar(economia.TemAutomacao(Catalogo.RollbackAutomatico) ? "Rollback automático" : "Rollback feito", 1.8f, VerdeClaro);
                Sons.Conserto();
            };
            economia.PicoComecou += nome => { Avisar("Pico: " + nome + "! Escale", 4f, Laranja); Sons.Alerta(); };
            economia.PicoEscalado += automatico => { Avisar(automatico ? "Autoscaling escalou" : "Cluster escalado: rende 2x", 2.5f, VerdeClaro); Sons.Compra(); };
            economia.SlaViolado += multa => { Avisar("SLA violado: -R$ " + Formatar(multa), 3f, Vermelho); Sons.Alerta(); };
            economia.PicoTerminou += AoTerminarPico;
            economia.ChamadoApareceu += c => { if (c.prioridade <= 2) { Avisar("Chamado P" + c.prioridade + ": " + c.texto, 4f, c.prioridade == 1 ? Laranja : Amarelo); Sons.Tique(); } };
            economia.EventoComecou += def => { Avisar(def.Nome + "!", 4f, def.Id == Catalogo.EventoCliente || def.Id == Catalogo.EventoBlackFriday ? Amarelo : Laranja); Sons.Alerta(); };
            economia.EventoTerminou += (def, valor) => { if (valor > 0) Avisar(def.Nome + ": +R$ " + Formatar(valor), 3f, VerdeClaro); else if (valor < 0) Avisar(def.Nome + ": -R$ " + Formatar(-valor), 3f, Vermelho); };
            economia.ClienteProposto += c => Avisar("Cliente novo: " + c.nome + " (abra o gerente)", 4f, Amarelo);
            economia.ClienteCresceu += c => Avisar(c.nome + " virou " + Catalogo.PortesDoSite[c.porte] + "!", 3f, VerdeClaro);
            economia.ClienteSaiu += c => { Avisar(c.nome + " foi embora", 4f, Vermelho); Sons.Alerta(); };
            economia.PedidoFeito += c => Avisar(c.nome + " fez um pedido (abra o gerente)", 4f, Amarelo);
            economia.Conquistou += c => Avisar("Conquista: " + c.Nome + " (+0,5%)", 3f, Amarelo);
            economia.AtaqueBloqueado += (def, quem) => Avisar(quem + " bloqueou " + def.Nome, 3f, VerdeClaro);
            economia.HardwareEnvelheceu += garantia => Avisar(garantia ? "Garantia venceu: refresh!" : "Hardware no fim da vida", 4f, Laranja);
            economia.ChamadoEncerrado += (c, bonus, equipe) => { if (bonus <= 0 && c.prioridade <= 2) Avisar("O P" + c.prioridade + " foi embora", 1.5f, Laranja); };
            economia.QuedaDeEnergia += dc => { Avisar("Queda de energia no DC-0" + (dc + 1) + "!", 3f, Vermelho); Sons.Alerta(); };
            economia.EnergiaVoltou += (dc, sozinho) => { Avisar("DC-0" + (dc + 1) + " religado", 2f, VerdeClaro); Sons.Conserto(); };
            economia.PaneRegional += r => { Avisar("Pane: " + Catalogo.NomesRegioes[r] + "!", 3f, Vermelho); Sons.Alerta(); };
            economia.RegiaoVoltou += (r, sozinho) => { Avisar(Catalogo.NomesRegioes[r] + " de volta", 2f, VerdeClaro); Sons.Conserto(); };
            // o IPO abre o modo gerente na tela de final (feito na faixa, ela apareceria só como um aviso)
            economia.Ipo += () => { Avisar("IPO! Agora você é CEO!", 6f, Amarelo); Sons.Promocao(); Salvamento.Salvar(economia.Estado); AbrirGerente(lembrar: false); };
            economia.Vendeu += AoVender;
            // quem já passou do começo não precisa do tutorial
            if (!economia.TutorialConcluido && (economia.Cargo > Catalogo.CargoFreelancer || economia.Estado.totalGanho > 2000))
                economia.AvancarTutorial(Catalogo.PassosTutorial);
        }

        void Start()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // preto puro = transparente na faixa
            janela = gameObject.AddComponent<JanelaDesktop>();
            PixelTexto.EscalaTexto = EscalaRelativaDoPainel;

            Sons.Iniciar();

            monitor = new GameObject("Monitor").AddComponent<MonitorFaixa>();
            monitor.Iniciar(this, economia, janela);
            painel = new GameObject("Painel").AddComponent<Painel>();
            painel.Iniciar(this, economia, new Vector2(PainelX, JanelaDesktop.AlturaVirtual + Painel.Espaco));
            resumo = new GameObject("ResumoOffline").AddComponent<ResumoOffline>();
            resumo.Iniciar(this, economia);
            gerente = new GameObject("ModoGerente").AddComponent<ModoGerente>();
            gerente.Iniciar(this, economia);

            // Opções para testes: -painel=<aba> abre o painel, -incidente trava o primeiro servidor, -promover promove
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-painel"))
                {
                    AbrirPainel();
                    painel.AbrirAba(arg.Contains("=") ? arg.Substring(arg.IndexOf('=') + 1) : "visao");
                }
                if (arg == "-incidente") economia.Travar(0);
                if (arg == "-ingles") Idiomas.Forcado = true;   // teste: em inglês sem mexer nos ajustes
                if (arg == "-promover") economia.Promover(); // só se as metas estiverem cumpridas
                if (arg == "-alternar-painel") InvokeRepeating(nameof(AlternarPainel), 5f, 4f); // abre e fecha sozinho (teste da janela)
                if (arg == "-alternar-gerente") InvokeRepeating(nameof(AlternarGerente), 5f, 5f);   // entra e sai do modo gerente (teste da janela)
                if (arg.StartsWith("-simular-largura=")) float.TryParse(arg.Substring(17), out larguraSimulada);   // teste de tela estreita
                if (arg.StartsWith("-hora=") && float.TryParse(arg.Substring(6), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float hora)) gerente.DefinirHora(hora);   // teste: cidade ao fundo numa hora fixa
            }
            monitor.LarguraSimulada = larguraSimulada;
            Posicionar();

            if (ganhoOffline > 0)
            {
                Avisar("Enquanto você estava fora: +R$ " + Formatar(ganhoOffline), 8f, Amarelo);
                resumo.Mostrar();
                AtualizarAlturaJanela();
            }
            proximoSalvamento = Time.time + IntervaloSalvamento;

            // O jogo abre no modo em que foi fechado (na primeira vez, no modo gerente).
            // Testes: -gerente e -faixa forçam um modo sem mexer na preferência do jogador; -painel abre na faixa.
            var args = Environment.GetCommandLineArgs();
            bool forcaFaixa = Array.Exists(args, a => a == "-faixa" || a.StartsWith("-painel"));
            bool forcaGerente = Array.Exists(args, a => a == "-gerente" || a.StartsWith("-gerente="));
            if (forcaGerente || (!forcaFaixa && Ajustes.AbrirNoGerente)) AbrirGerente(lembrar: false);
            string secao = Array.Find(args, a => a.StartsWith("-gerente="));
            if (secao != null) gerente.AbrirSecao(secao.Substring(9));
            // a tela inicial (continuar, novo jogo, configurações) abre junto com o modo gerente; os testes vão direto para o jogo
            string[] deTeste = { "-gerente", "-faixa", "-painel", "-terminal", "-atender", "-promover", "-incidente", "-hora", "-alternar", "-simular" };
            bool emTeste = Array.Exists(args, a => Array.Exists(deTeste, t => a.StartsWith(t)));
            if (!emTeste)
            {
                bool naFaixa = !gerente.Aberto;   // quem usava a faixa volta para ela no Continuar
                if (naFaixa) AbrirGerente(lembrar: false);
                gerente.MostrarTelaInicial(ganhoOffline, naFaixa);
            }
            if (gerente.Aberto && ganhoOffline > 0)
                gerente.Avisar("Enquanto você estava fora: +R$ " + Formatar(ganhoOffline) + " (" + (economia.TaxaOffline * 100).ToString("0") + "% da receita)", 12);
        }

        /// <summary>Novo jogo: guarda uma cópia do save atual e recomeça do zero, como freelancer. Retorna o nome da cópia.</summary>
        public string NovoJogo()
        {
            Salvamento.Salvar(economia.Estado);   // a cópia leva o progresso até agora
            string copia = Salvamento.GuardarCopia();
            economia.ComecarDoZero();
            ganhoOffline = 0;
            Salvamento.Salvar(economia.Estado);
            return copia;
        }

        /// <summary>Aviso curto na faixa (a barra de notícias do modo gerente tem os seus).</summary>
        void Avisar(string texto, float segundos, Color? cor = null) => monitor?.Avisar(texto, segundos, cor);

        // ---------------- Ações do jogador ----------------

        /// <summary>Compra uma melhoria (faixa e painel); avisa o que faltou quando não dá.</summary>
        public void TentarComprar(MelhoriaDef def)
        {
            if (economia.NoMaximo(def.Id)) return;
            if (!economia.RequisitoOk(def.Id))
                Avisar("Precisa: " + Catalogo.Buscar(def.Requisito).Nome, 1.5f);
            else if (!economia.Comprar(def.Id))
                Avisar("Falta R$ " + Formatar(economia.Custo(def.Id) - economia.Dinheiro), 1.5f);
        }

        /// <summary>Escalar o cluster num pico.</summary>
        public void Escalar() => economia.Escalar();

        /// <summary>Café: a receita dobra por 30 s (recarga de 3 min). Serve para a faixa e para o modo gerente.</summary>
        public bool TomarCafe()
        {
            if (!economia.TomarCafe())
            {
                Avisar(economia.CafeAtivo ? "O café ainda faz efeito" : "Café em " + Mathf.CeilToInt((float)economia.RecargaDoCafe) + "s", 1.5f);
                return false;
            }
            Avisar("Café! Receita x2 por 30s", 2.5f, Amarelo);
            Sons.Compra();
            return true;
        }

        /// <summary>Atende um chamado da fila (-1: o mais urgente). Retorna o bônus (0 se não havia).</summary>
        public double AtenderChamado(int indice = -1)
        {
            double bonus = indice < 0 ? economia.AtenderChamado() : economia.AtenderChamado(indice);
            if (bonus <= 0) return 0;
            Avisar("Chamado resolvido: +R$ " + Formatar(bonus), 2f, VerdeClaro);
            Sons.Moeda();
            return bonus;
        }

        /// <summary>Redireciona o tráfego da região em pane (painel, modo gerente).</summary>
        public void Redirecionar() => economia.Redirecionar();

        /// <summary>Vende a empresa (prestígio) e começa outra no armário, com o desafio escolhido.</summary>
        public void VenderEmpresa(string desafio) => economia.VenderEmpresa(desafio);

        void AoVender(int certificacoes)
        {
            if (painel.Aberto) painel.Fechar();
            Avisar("Empresa vendida! +" + certificacoes + " certificações", 6f, Amarelo);
            Sons.Promocao();
            Salvamento.Salvar(economia.Estado);
        }

        /// <summary>Abre o capital (fim da carreira).</summary>
        public void FazerIpo() => economia.FazerIpo();

        /// <summary>Religa o datacenter que ficou sem energia (painel, modo gerente).</summary>
        public void Religar() => economia.Religar();

        /// <summary>Rollback pelo painel.</summary>
        public void FazerRollback() => economia.FazerRollback(porTecnico: false);

        /// <summary>Trocar o disco pelo painel.</summary>
        public void TrocarDisco() => economia.TrocarDisco(porTecnico: false);

        /// <summary>Reinicia um servidor travado na hora (faixa, painel, modo gerente).</summary>
        public void Reiniciar(int servidor)
        {
            if (!economia.Travado(servidor)) return;
            economia.Clicar(servidor);
            Avisar("Reiniciado!", 1.2f, VerdeClaro);
            Sons.Conserto();
        }

        public void Promover() => economia.Promover();

        // ---------------- Eventos da economia ----------------

        void AoComprar(string id)
        {
            if (economia.AtingiuMarco(id))
            {
                Avisar("Marco! " + economia.UnidadesDoGerador(id) + "× " + Catalogo.Buscar(id).Nome + ": renda ×2", 4f, Amarelo);
                Sons.Promocao();
            }
            else Sons.Compra();
            Salvamento.Salvar(economia.Estado);
        }

        void AoVoltar(int servidor, bool peloTecnico)
        {
            if (!peloTecnico) return;
            Avisar(economia.TemAutomacao(Catalogo.Watchdog) ? "Watchdog reiniciou" : "Técnico consertou", 1.5f, VerdeClaro);
            Sons.Conserto();
        }

        void AoTrocarDisco(bool restaurou, double perda, bool peloTecnico)
        {
            if (restaurou && perda <= 0) Avisar("Backup restaurado!", 2f, VerdeClaro);
            else if (restaurou) Avisar("Backup salvou a maior parte: -R$ " + Formatar(perda), 3f, Laranja);
            else Avisar("Sem backup: -R$ " + Formatar(perda), 3f, Vermelho);
            Sons.Conserto();
            Salvamento.Salvar(economia.Estado);
        }

        void AoTerminarPico(bool sobreviveu)
        {
            Avisar(sobreviveu ? "Pico superado!" : "O pico passou", 2.5f, sobreviveu ? VerdeClaro : Laranja);
            if (sobreviveu) Sons.Conserto();
            Salvamento.Salvar(economia.Estado);
        }

        void AoFicarProntaAutomacao(string id)
        {
            Avisar("Automação pronta: " + Catalogo.BuscarAutomacao(id).Nome, 4f, VerdeClaro);
            Sons.Promocao();
            Salvamento.Salvar(economia.Estado);
        }

        /// <summary>Começar a escrever uma automação (pelo painel).</summary>
        public void EscreverAutomacao(string id)
        {
            if (!economia.EscreverAutomacao(id)) return;
            Avisar("Escrevendo: " + Catalogo.BuscarAutomacao(id).Nome, 2.5f, Azul);
            Sons.Compra();
            Salvamento.Salvar(economia.Estado);
        }

        void AoPromover(int cargo)
        {
            Avisar((cargo == Catalogo.CargoTecnico ? "Contratado! " : "Promovido! ") + economia.CargoAtual.Nome, 5f);
            Sons.Promocao();
            Salvamento.Salvar(economia.Estado);
        }

        // ---------------- Painel ----------------

        /// <summary>O painel usa uma escala maior que a faixa (texto legível); na tela, cada pixel dele vale EscalaPainel pixels.</summary>
        float EscalaRelativaDoPainel
        {
            get
            {
                // telas estreitas: o painel volta para a escala da faixa, para caber inteiro
                float rel = janela.EscalaPainel / (float)janela.Escala;
                float tela = larguraSimulada > 0 ? larguraSimulada : janela.LarguraVirtualDaTela;
                return Painel.Largura * rel + PainelX * 2 <= tela ? rel : 1f;
            }
        }

        /// <summary>A janela cresce para cima o bastante para o painel ou o aviso de volta (o que estiver aberto).</summary>
        void AtualizarAlturaJanela()
        {
            if (gerente != null && gerente.Aberto) return;   // o modo gerente ocupa a tela toda
            int extra = painel.Aberto ? Painel.Altura : resumo.Aberto ? ResumoOffline.Altura : 0;
            janela.DefinirAlturaVirtual(JanelaDesktop.AlturaVirtual + (extra > 0 ? Painel.Espaco + Mathf.CeilToInt(extra * EscalaRelativaDoPainel) + 2 : 0));
        }

        public void AoFecharResumo() => AtualizarAlturaJanela();

        void AbrirPainel()
        {
            resumo.Fechar();
            painel.Abrir();
            AtualizarAlturaJanela();
        }

        void FecharPainel()
        {
            painel.Fechar();
            AtualizarAlturaJanela();
        }

        public void AlternarPainel()
        {
            if (painel.Aberto) FecharPainel();
            else AbrirPainel();
        }

        /// <summary>O botão ▲ da faixa: abre o modo gerente direto na loja (aba Tudo, com o que dá para comprar primeiro).
        /// O painel antigo da faixa só abre pelo teste -painel.</summary>
        public void AbrirLoja()
        {
            AbrirGerente(lembrar: false);
            gerente.AbrirSecao("melhorias");
        }

        // ---------------- Modo gerente (vista isométrica) ----------------

        /// <summary>Troca a faixa pela vista isométrica do data center, numa janela normal. O botão "Ir para a faixa" volta.</summary>
        public void AbrirGerente() => AbrirGerente(lembrar: true);

        /// <param name="lembrar">Guarda como o modo em que o jogo abre da próxima vez (só em ações do jogador).</param>
        void AbrirGerente(bool lembrar)
        {
            if (gerente.Aberto) return;
            if (lembrar) Ajustes.AbrirNoGerente = true;
            if (painel.Aberto) painel.Fechar();
            resumo.Fechar();
            gerente.Abrir();
            monitor.Visivel = false;
            janela.DefinirModoGerente(true);
            Sons.Tique();
        }

        void AlternarGerente() { if (gerente.Aberto) FecharGerente(lembrar: false); else AbrirGerente(lembrar: false); }

        public void FecharGerente() => FecharGerente(lembrar: true);

        void FecharGerente(bool lembrar)
        {
            if (!gerente.Aberto) return;
            if (lembrar) Ajustes.AbrirNoGerente = false;
            gerente.Fechar();
            monitor.Visivel = true;
            janela.DefinirModoGerente(false);
            AtualizarAlturaJanela();
        }

        // ---------------- Esconder ----------------

        float ocultarEm = -1;

        /// <summary>Botão de esconder: avisa como voltar e some logo depois. O jogo continua rendendo escondido.</summary>
        public void Ocultar()
        {
            if (painel.Aberto) FecharPainel();
            Avisar("Volta com " + JanelaDesktop.Atalho, 1.6f);
            ocultarEm = Time.time + 1.6f;
        }

        // ---------------- Laço ----------------

        void Update()
        {
            if (ocultarEm > 0 && Time.time >= ocultarEm) { ocultarEm = -1; janela.AlternarOculta(); }
            AtualizarCamera();
            PixelTexto.EscalaTexto = EscalaRelativaDoPainel; // texto da faixa na mesma escala do painel
            Posicionar();
            // minimizar no modo gerente leva para a faixa (sem mudar como o jogo abre da próxima vez)
            if (gerente.Aberto && janela.Minimizada) FecharGerente(lembrar: false);
            if (gerente.Aberto) janela.DefinirClicavel(true);   // no modo gerente a janela inteira recebe cliques (OnGUI)
            else ProcessarCursor();

            Economia.Ingles = Idiomas.Ingles;   // números no formato do idioma escolhido
            economia.Avancar(Time.deltaTime);
            Sons.Ambiente(economia.Cargo, economia.TotalServidores, economia.Nivel(Catalogo.ArCondicionado) > 0);

            if (Time.time >= proximoSalvamento)
            {
                proximoSalvamento = Time.time + IntervaloSalvamento;
                Salvamento.Salvar(economia.Estado);
            }
        }

        /// <summary>
        /// Encosta o painel (e o aviso de volta) na esquerda ou na direita da tela, conforme os Ajustes;
        /// o monitor se posiciona sozinho. Tudo em coordenadas inteiras, para a pixel art continuar nítida.
        /// </summary>
        void Posicionar()
        {
            float s = EscalaRelativaDoPainel;
            float larguraTela = larguraSimulada > 0 ? larguraSimulada : Screen.width / (float)janela.Escala;
            float y = JanelaDesktop.AlturaVirtual + Painel.Espaco;
            painel.transform.localScale = resumo.transform.localScale = Vector3.one * s;
            float arrastado = Mathf.Round(monitor.DeslocamentoX / janela.Escala);   // a faixa foi arrastada para o lado: o painel vai junto
            painel.transform.position = new Vector3((Ajustes.Direita ? Mathf.Floor(larguraTela - Painel.Largura * s - PainelX) : PainelX) + arrastado, y, 0);
            resumo.transform.position = new Vector3((Ajustes.Direita ? Mathf.Floor(larguraTela - ResumoOffline.Largura * s - PainelX) : PainelX) + arrastado, y, 0);
        }

        /// <summary>
        /// Câmera pixel-perfect: cada pixel da arte vira um quadrado inteiro de pixels na tela,
        /// e o canto de baixo à esquerda da janela é a coordenada (0, 0).
        /// </summary>
        void AtualizarCamera()
        {
            int escala = janela.Escala;
            cam.orthographicSize = Screen.height / (2f * escala);
            cam.transform.position = new Vector3(Screen.width / (2f * escala), cam.orthographicSize, -10f);
        }

        void ProcessarCursor()
        {
            // Com a faixa escondida ou o cursor em outro programa, nada de destaque nem clique
            bool nosso = janela.CursorSobreAJanela;
            bool clicou = Input.GetMouseButtonDown(0);
            Vector2 mundo = cam.ScreenToWorldPoint(janela.PosicaoCursor);
            var colisores = Physics2D.OverlapPointAll(mundo);   // painel e aviso de volta
            bool sobreMonitor = monitor.Contem(janela.PosicaoCursor);
            janela.DefinirClicavel(colisores.Length > 0 || sobreMonitor);
            if (!nosso) colisores = Array.Empty<Collider2D>();

            IClicavel escolhido = null;
            foreach (var c in colisores)
                if (c.TryGetComponent(out IClicavel alvo) && (escolhido == null || alvo.Ordem > escolhido.Ordem))
                    escolhido = alvo;

            if (escolhido != sobCursor)
            {
                if (sobCursor as UnityEngine.Object != null) sobCursor.DefinirDestaque(false);
                escolhido?.DefinirDestaque(true);
                sobCursor = escolhido;
            }
            if (escolhido is Painel p) p.DefinirCursor(mundo);
            if (escolhido is ResumoOffline r) r.DefinirCursor(mundo);

            if (painel.Aberto && Input.GetKeyDown(KeyCode.Escape)) FecharPainel();
            // o monitor só recebe o clique quando o painel não está por cima
            if (monitor.Processar(janela.PosicaoCursor, nosso, clicou && escolhido == null)) return;
            if (!clicou) return;
            if (escolhido == null)
            {
                // clique fora de tudo (em outro programa ou na área transparente): fecha o painel
                if (painel.Aberto) FecharPainel();
                return;
            }
            if (escolhido is Painel painelClicado)
            {
                painelClicado.ClicarEm(mundo);
                if (!painel.Aberto) AtualizarAlturaJanela(); // fechou pelo "x"
            }
            else if (escolhido is ResumoOffline resumoClicado) resumoClicado.ClicarEm(mundo);
            else escolhido.Clicar();
        }

        void OnApplicationQuit()
        {
            Salvamento.Salvar(economia.Estado);
            Idiomas.SalvarFaltando();   // em inglês: lista o que apareceu sem tradução (para completar a tabela)
        }

        /// <summary>Dinheiro em português (ver Economia.FormatarDinheiro): R$ 16.342, R$ 1,23 mi, R$ 45,6 bi.</summary>
        public static string Formatar(double v) => Economia.FormatarDinheiro(v);
    }
}
