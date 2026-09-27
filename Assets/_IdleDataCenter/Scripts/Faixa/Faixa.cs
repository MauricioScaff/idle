using System;
using IdleDataCenter.Gerente;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Liga tudo: a economia, o cenário do cargo atual, a loja, o painel de gestão, o HUD da faixa,
    /// os cliques (com "clique atravessando" nas áreas vazias), o save e o progresso offline.
    /// </summary>
    public class Faixa : MonoBehaviour
    {
        const int CenarioX = 8;
        const float IntervaloSalvamento = 30f;
        const int LinhaHud = Cenario.Altura - 9;   // linha do dinheiro, receita e meta no topo do cenário

        static readonly Color Amarelo = PixelArt.Hex("ffd65c"), VerdeClaro = PixelArt.Hex("9be89b"),
                              Laranja = PixelArt.Hex("ffbf3f"), Vermelho = PixelArt.Hex("ff3b4e"),
                              Azul = PixelArt.Hex("a9c7ff");

        Economia economia;
        Camera cam;
        JanelaDesktop janela;
        Cenario cenario;
        Painel painel;
        ResumoOffline resumo;
        ModoGerente gerente;
        PixelTexto textoDinheiro, textoReceita, textoAmbiente;
        BotaoTexto botaoMeta;
        SpriteRenderer setaDica;
        IClicavel sobCursor;
        float proximoSalvamento;
        double ganhoOffline;

        public Loja Loja { get; private set; }

        void Awake()
        {
            economia = new Economia(Salvamento.Carregar());
            ganhoOffline = economia.AplicarOffline(Salvamento.AgoraUnix);
            economia.Comprou += AoComprar;
            economia.Travou += AoTravar;
            economia.Voltou += AoVoltar;
            economia.Promoveu += AoPromover;
            economia.DiscoQueimou += AoQueimarDisco;
            economia.DiscoTrocado += AoTrocarDisco;
            economia.AutomacaoPronta += AoFicarProntaAutomacao;
            economia.DeployQuebrou += AoQuebrarDeploy;
            economia.DeployVoltou += AoVoltarDeploy;
            economia.PicoComecou += AoComecarPico;
            economia.PicoEscalado += AoEscalar;
            economia.SlaViolado += AoViolarSla;
            economia.PicoTerminou += AoTerminarPico;
            economia.ChamadoApareceu += AoAparecerChamado;
            economia.ChamadoEncerrado += AoEncerrarChamado;
            // quem já passou do começo não precisa do tutorial
            if (!economia.TutorialConcluido && (economia.Cargo > 0 || economia.Estado.totalGanho > 2000))
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

            MontarTudo();
            painel = new GameObject("Painel").AddComponent<Painel>();
            painel.Iniciar(this, economia, new Vector2(CenarioX, JanelaDesktop.AlturaVirtual + Painel.Espaco));
            resumo = new GameObject("ResumoOffline").AddComponent<ResumoOffline>();
            resumo.Iniciar(this, economia);
            gerente = new GameObject("ModoGerente").AddComponent<ModoGerente>();
            gerente.Iniciar(this, economia);
            Posicionar();

            if (ganhoOffline > 0)
            {
                Ganho(ganhoOffline, cenario.PrimeiraTorre.Topo + new Vector2(-8, 3));
                resumo.Mostrar();
                AtualizarAlturaJanela();
            }
            proximoSalvamento = Time.time + IntervaloSalvamento;

            // Opções para testes: -painel=<aba> abre o painel, -incidente trava o primeiro servidor, -promover promove
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-painel"))
                {
                    AbrirPainel();
                    painel.AbrirAba(arg.Contains("=") ? arg.Substring(arg.IndexOf('=') + 1) : "visao");
                }
                if (arg == "-incidente") economia.Travar(0);
                if (arg == "-promover") economia.Promover(); // só se as metas estiverem cumpridas
                if (arg == "-alternar-painel") InvokeRepeating(nameof(AlternarPainel), 5f, 4f); // abre e fecha sozinho (teste da janela)
                if (arg == "-alternar-gerente") InvokeRepeating(nameof(AlternarGerente), 5f, 5f);   // entra e sai do modo gerente (teste da janela)
            }

            // O jogo abre no modo em que foi fechado (na primeira vez, no modo gerente).
            // Testes: -gerente e -faixa forçam um modo sem mexer na preferência do jogador; -painel abre na faixa.
            var args = Environment.GetCommandLineArgs();
            bool forcaFaixa = Array.Exists(args, a => a == "-faixa" || a.StartsWith("-painel"));
            bool forcaGerente = Array.Exists(args, a => a == "-gerente");
            if (forcaGerente || (!forcaFaixa && Ajustes.AbrirNoGerente)) AbrirGerente(lembrar: false);
            if (gerente.Aberto && ganhoOffline > 0)
                gerente.Avisar("Enquanto você estava fora: +R$ " + Formatar(ganhoOffline) + " (" + (economia.TaxaOffline * 100).ToString("0") + "% da receita)", 12);
        }

        /// <summary>Monta (ou remonta, na promoção) o cenário do cargo atual, o HUD e a loja.</summary>
        void MontarTudo()
        {
            if (cenario != null) Destroy(cenario.gameObject);
            if (Loja != null) Destroy(Loja.gameObject);
            sobCursor = null;

            cenario = new GameObject("Cenario").AddComponent<Cenario>();
            cenario.Montar(this, economia, new Vector2(CenarioX, 0));
            MontarHud();

            Loja = new GameObject("Loja").AddComponent<Loja>();
            Loja.Iniciar(economia, new Vector2(CenarioX + cenario.Largura + 4, 0));
            cenario.AtualizarIncidentes();
        }

        void MontarHud()
        {
            var raiz = cenario.transform;
            textoDinheiro = PixelTexto.Criar(raiz, new Vector2(4, LinhaHud), Amarelo, 10, true);
            textoReceita = PixelTexto.Criar(raiz, new Vector2(40, LinhaHud), VerdeClaro, 10, true);
            textoAmbiente = PixelTexto.Criar(raiz, new Vector2(80, LinhaHud), Laranja, 10, true);
            botaoMeta = BotaoTexto.Criar(raiz, new Vector2(100, LinhaHud), Azul, AoClicarMeta);
            BotaoIcone.Criar(raiz, Arte.Gerente, new Vector2(cenario.Largura - 32, LinhaHud - 1), AbrirGerente);
            BotaoIcone.Criar(raiz, Arte.Ocultar, new Vector2(cenario.Largura - 24, LinhaHud - 1), Ocultar);
            BotaoIcone.Criar(raiz, Arte.AbrirPainel, new Vector2(cenario.Largura - 16, LinhaHud - 1), AlternarPainel);
            BotaoIcone.Criar(raiz, Arte.Fechar, new Vector2(cenario.Largura - 8, LinhaHud - 1), Application.Quit);

            // Dica do primeiro clique: setinha pulando sobre o servidor
            setaDica = cenario.Decoracao("Dica", Arte.Seta, cenario.PrimeiraTorre.Topo + new Vector2(-10, 3), 11);
            setaDica.enabled = !economia.Estado.jaClicouNoServidor;
        }

        // ---------------- Efeitos ----------------

        /// <summary>Solta um sprite que sobe e some (posição em coordenadas do objeto pai).</summary>
        public void Efeito(Transform pai, Sprite sprite, Vector2 posicaoLocal, float duracao, float subida)
        {
            var sr = new GameObject("Efeito").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(pai, false);
            sr.transform.localPosition = posicaoLocal;
            sr.sprite = sprite;
            sr.sortingOrder = 12;
            Flutuante.Aplicar(sr.gameObject, duracao, subida);
        }

        /// <summary>Texto "+R$ X" subindo a partir de um ponto do cenário.</summary>
        void Ganho(double valor, Vector2 posicaoLocal)
        {
            var texto = PixelTexto.Criar(cenario.transform, posicaoLocal, Amarelo, 13, true);
            texto.Definir("+" + Formatar(valor));
            Flutuante.Aplicar(texto.gameObject, 0.9f, 6f);
        }

        public void Faiscas(Vector2 topo, int quantidade)
        {
            for (int i = 0; i < quantidade; i++)
                Efeito(cenario.transform, Arte.Faisca, topo + new Vector2(UnityEngine.Random.Range(-8, 6), UnityEngine.Random.Range(-12, 0)), 0.5f, 3f);
        }

        // ---------------- Ações do jogador ----------------

        public void ClicarServidor(int servidor, Vector2 topo)
        {
            bool estavaTravado = economia.Travado(servidor);
            double valor = economia.Clicar(servidor);
            if (estavaTravado)
            {
                Faiscas(topo, 3);
                Loja.MostrarAviso("Reiniciado!", 1.2f, VerdeClaro);
                Sons.Conserto();
            }
            else
            {
                Ganho(valor, topo + new Vector2(-4, 1));
                Sons.Moeda();
            }
            setaDica.enabled = false;
        }

        /// <summary>Clique no rack: reinicia o primeiro 1U travado, ou rende um clique normal.</summary>
        public void ClicarRack(Vector2 topo)
        {
            int alvo = economia.Torres;
            for (int s = economia.Torres; s < economia.TotalServidores; s++)
                if (economia.Travado(s)) { alvo = s; break; }
            ClicarServidor(alvo, topo);
        }

        /// <summary>Clique num rack cheio (ou no storage saudável): só rende.</summary>
        public void ClicarEquipamento(Vector2 topo)
        {
            Ganho(economia.ClicarEquipamento(), topo + new Vector2(-4, 1));
            Sons.Moeda();
            setaDica.enabled = false;
        }

        /// <summary>Clique no storage: com disco queimado, troca na hora; senão rende um clique.</summary>
        public void ClicarStorage(Vector2 topo)
        {
            if (!economia.DiscoQueimado) { ClicarEquipamento(topo); return; }
            Faiscas(topo, 3);
            economia.TrocarDisco(porTecnico: false);
        }

        /// <summary>Clique nos containers ou no CI: com deploy quebrado faz o rollback na hora, senão rende um clique.</summary>
        public void ClicarContainers(Vector2 topo)
        {
            if (!economia.DeployQuebrado) { ClicarEquipamento(topo); return; }
            Faiscas(topo, 3);
            economia.FazerRollback(porTecnico: false);
        }

        /// <summary>Clique num nó do cluster: num pico sem escala, escala; senão rende um clique.</summary>
        public void ClicarCluster(Vector2 topo)
        {
            if (economia.EmPico && !economia.PicoFoiEscalado) { Faiscas(topo, 4); economia.Escalar(); return; }
            ClicarEquipamento(topo);
        }

        /// <summary>Escalar pelo painel.</summary>
        public void Escalar() => economia.Escalar();

        /// <summary>Café: a receita dobra por 30 s (recarga de 3 min). Serve para a faixa e para o modo gerente.</summary>
        public bool TomarCafe()
        {
            if (!economia.TomarCafe())
            {
                Loja.MostrarAviso(economia.CafeAtivo ? "O café ainda faz efeito" : "Café em " + Mathf.CeilToInt((float)economia.RecargaDoCafe) + "s", 1.5f);
                return false;
            }
            Loja.MostrarAviso("Café! Receita x2 por 30s", 2.5f, Amarelo);
            cenario.Tecnico.Comemorar();
            Sons.Compra();
            return true;
        }

        /// <summary>Atende o chamado urgente aberto. Retorna o bônus (0 se não havia).</summary>
        public double AtenderChamado()
        {
            double bonus = economia.AtenderChamado();
            if (bonus <= 0) return 0;
            Ganho(bonus, new Vector2(PosicaoDoChamado, 44));
            Loja.MostrarAviso("Chamado resolvido: +R$ " + Formatar(bonus), 2f, VerdeClaro);
            Sons.Moeda();
            return bonus;
        }

        float PosicaoDoChamado => cenario.PosicaoMesa - 4;

        /// <summary>Rollback pelo painel.</summary>
        public void FazerRollback() => economia.FazerRollback(porTecnico: false);

        /// <summary>Trocar o disco pelo painel.</summary>
        public void TrocarDisco() => economia.TrocarDisco(porTecnico: false);

        /// <summary>Reiniciar pelo painel.</summary>
        public void Reiniciar(int servidor)
        {
            if (economia.Travado(servidor)) ClicarServidor(servidor, cenario.TopoDoServidor(servidor));
        }

        public void Promover() => economia.Promover();

        void AoClicarMeta()
        {
            if (economia.EmPico && !economia.PicoFoiEscalado) economia.Escalar();
            else if (economia.PodePromover) Promover();
            else AbrirCarreira();
        }

        // ---------------- Eventos da economia ----------------

        void AoComprar(string id)
        {
            var nova = cenario.AtualizarEquipamentos();
            if (nova != null) Faiscas(nova.Topo, 4);
            if (id == Catalogo.Rack || id == Catalogo.Servidor1U) Faiscas(cenario.TopoDoServidor(economia.TotalServidores - 1), 4);
            cenario.PularTudo();
            cenario.Tecnico.Comemorar();
            Sons.Compra();
            Salvamento.Salvar(economia.Estado);
        }

        void AoTravar(int servidor)
        {
            Loja.MostrarAviso("Servidor travou!", 2f, Vermelho);
            Sons.Alerta();
        }

        void AoVoltar(int servidor, bool peloTecnico)
        {
            if (!peloTecnico) return;
            Loja.MostrarAviso(economia.TemAutomacao(Catalogo.Watchdog) ? "Watchdog reiniciou" : "Técnico consertou", 1.5f, VerdeClaro);
            cenario.Tecnico.Comemorar();
            Sons.Conserto();
        }

        void AoQueimarDisco()
        {
            Loja.MostrarAviso("Disco queimou!", 2f, Vermelho);
            Sons.Alerta();
        }

        void AoTrocarDisco(bool restaurou, double perda, bool peloTecnico)
        {
            if (restaurou) Loja.MostrarAviso("Backup restaurado!", 2f, VerdeClaro);
            else Loja.MostrarAviso("Sem backup: -R$ " + Formatar(perda), 3f, Vermelho);
            if (peloTecnico) cenario.Tecnico.Comemorar();
            Sons.Conserto();
            Salvamento.Salvar(economia.Estado);
        }

        void AoQuebrarDeploy()
        {
            Loja.MostrarAviso("Deploy quebrou!", 2f, Vermelho);
            Sons.Alerta();
        }

        void AoVoltarDeploy(bool peloTecnico)
        {
            Loja.MostrarAviso(economia.TemAutomacao(Catalogo.RollbackAutomatico) ? "Rollback automático" : "Rollback feito", 1.8f, VerdeClaro);
            if (peloTecnico) cenario.Tecnico.Comemorar();
            cenario.LimparEsteira();
            Sons.Conserto();
        }

        void AoComecarPico(string nome)
        {
            Loja.MostrarAviso("Pico: " + nome + "! Escale", 4f, Laranja);
            Sons.Alerta();
        }

        void AoEscalar(bool automatico)
        {
            Loja.MostrarAviso(automatico ? "Autoscaling escalou" : "Cluster escalado: rende 2x", 2.5f, VerdeClaro);
            Sons.Compra();
        }

        void AoViolarSla(double multa)
        {
            Loja.MostrarAviso("SLA violado: -R$ " + Formatar(multa), 3f, Vermelho);
            Sons.Alerta();
        }

        void AoTerminarPico(bool sobreviveu)
        {
            Loja.MostrarAviso(sobreviveu ? "Pico superado!" : "O pico passou", 2.5f, sobreviveu ? VerdeClaro : Laranja);
            if (sobreviveu) { cenario.Tecnico.Comemorar(); Sons.Conserto(); }
            Salvamento.Salvar(economia.Estado);
        }

        void AoAparecerChamado(string texto)
        {
            Loja.MostrarAviso("Chamado: " + texto, 4f, Amarelo);
            Sons.Tique();
        }

        void AoEncerrarChamado(double bonus)
        {
            if (bonus <= 0) Loja.MostrarAviso("O chamado foi embora", 1.5f, Laranja);
        }

        void AoFicarProntaAutomacao(string id)
        {
            Loja.MostrarAviso("Automação pronta: " + Catalogo.BuscarAutomacao(id).Nome, 4f, VerdeClaro);
            cenario.Tecnico.Comemorar();
            Faiscas(cenario.Tela.transform.position - cenario.transform.position + new Vector3(6, 6), 4);
            Sons.Promocao();
            Salvamento.Salvar(economia.Estado);
        }

        /// <summary>Começar a escrever uma automação (pelo painel).</summary>
        public void EscreverAutomacao(string id)
        {
            if (!economia.EscreverAutomacao(id)) return;
            Loja.MostrarAviso("Escrevendo: " + Catalogo.BuscarAutomacao(id).Nome, 2.5f, Azul);
            Sons.Compra();
            Salvamento.Salvar(economia.Estado);
        }

        void AoPromover(int cargo)
        {
            MontarTudo();
            Loja.MostrarAviso("Promovido! " + economia.CargoAtual.Nome, 5f);
            // festa: faíscas por todo o cenário e um coração do técnico
            for (int i = 0; i < 12; i++)
                Efeito(cenario.transform, i % 3 == 0 ? Arte.Coracao : Arte.Faisca,
                    new Vector2(UnityEngine.Random.Range(10, cenario.Largura - 10), UnityEngine.Random.Range(8, 32)), 1.2f, 6f);
            cenario.Tecnico.Comemorar();
            Sons.Promocao();
            Salvamento.Salvar(economia.Estado);
        }

        // ---------------- Painel ----------------

        /// <summary>O painel usa uma escala maior que a faixa (texto legível); na tela, cada pixel dele vale EscalaPainel pixels.</summary>
        float EscalaRelativaDoPainel => janela.EscalaPainel / (float)janela.Escala;

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

        void AbrirCarreira()
        {
            AbrirPainel();
            painel.AbrirCarreira();
        }

        void FecharPainel()
        {
            painel.Fechar();
            AtualizarAlturaJanela();
        }

        void AlternarPainel()
        {
            if (painel.Aberto) FecharPainel();
            else AbrirPainel();
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
            janela.DefinirModoGerente(false);
            AtualizarAlturaJanela();
        }

        // ---------------- Esconder ----------------

        float ocultarEm = -1;

        /// <summary>Botão de esconder: avisa como voltar e some logo depois. O jogo continua rendendo escondido.</summary>
        void Ocultar()
        {
            if (painel.Aberto) FecharPainel();
            Loja.MostrarAviso("Volta com " + JanelaDesktop.Atalho, 1.6f);
            ocultarEm = Time.time + 1.6f;
        }

        // ---------------- Laço ----------------

        void Update()
        {
            if (ocultarEm > 0 && Time.time >= ocultarEm) { ocultarEm = -1; janela.AlternarOculta(); }
            AtualizarCamera();
            PixelTexto.EscalaTexto = EscalaRelativaDoPainel; // texto da faixa na mesma escala do painel
            Posicionar();
            if (gerente.Aberto) janela.DefinirClicavel(true);   // no modo gerente a janela inteira recebe cliques (OnGUI)
            else ProcessarCursor();

            economia.Avancar(Time.deltaTime);
            cenario.AtualizarIncidentes();
            AtualizarHud();

            if (setaDica.enabled)
                setaDica.transform.localPosition = cenario.PrimeiraTorre.Topo + new Vector2(-10, 3 + (Mathf.FloorToInt(Time.time / 0.4f) % 2));

            if (Time.time >= proximoSalvamento)
            {
                proximoSalvamento = Time.time + IntervaloSalvamento;
                Salvamento.Salvar(economia.Estado);
            }
        }

        /// <summary>
        /// Encosta cenário + loja (e o painel) na esquerda ou na direita da tela, conforme os Ajustes.
        /// Tudo em coordenadas inteiras, para a pixel art continuar nítida.
        /// </summary>
        void Posicionar()
        {
            float s = EscalaRelativaDoPainel;
            float larguraTela = Screen.width / (float)janela.Escala;
            float faixaTotal = cenario.Largura + 4 + Loja.Largura;
            float x = Ajustes.Direita ? Mathf.Floor(larguraTela - faixaTotal - CenarioX) : CenarioX;
            cenario.transform.position = new Vector3(x, 0, 0);
            Loja.transform.position = new Vector3(x + cenario.Largura + 4, 0, 0);
            float y = JanelaDesktop.AlturaVirtual + Painel.Espaco;
            painel.transform.localScale = resumo.transform.localScale = Vector3.one * s;
            painel.transform.position = new Vector3(Ajustes.Direita ? Mathf.Floor(larguraTela - Painel.Largura * s - CenarioX) : CenarioX, y, 0);
            resumo.transform.position = new Vector3(Ajustes.Direita ? Mathf.Floor(larguraTela - ResumoOffline.Largura * s - CenarioX) : CenarioX, y, 0);
        }

        void AtualizarHud()
        {
            string dinheiro = "R$ " + Formatar(economia.Dinheiro);
            textoDinheiro.Definir(dinheiro);
            float x = 4 + PixelTexto.LarguraAmpliada(dinheiro) + 5;

            string receita = "+" + Formatar(economia.ReceitaPorSegundo) + "/s";
            textoReceita.Definir(receita);
            textoReceita.transform.localPosition = new Vector3(x, LinhaHud, 0);
            x += PixelTexto.LarguraAmpliada(receita) + 6;

            // Energia e temperatura só importam a partir de Sysadmin
            if (economia.Cargo >= 1)
            {
                string ambiente = $"{economia.ConsumoKw:0.0}/{economia.CapacidadeKw:0.0}kW {economia.Temperatura:0}C";
                if (economia.NaSalaDeRacks) ambiente += $" {economia.TrafegoMbps:0}/{economia.BandaMbps:0}Mb"; // banda a partir de Analista
                textoAmbiente.Definir(ambiente);
                textoAmbiente.DefinirCor(economia.Sobrecarga || economia.Quente || economia.LinkSaturado ? Vermelho : Laranja);
                textoAmbiente.transform.localPosition = new Vector3(x, LinhaHud, 0);
            }
            else textoAmbiente.Definir("");

            // Meta de promoção, alinhada à direita (antes dos ícones)
            var metas = economia.CargoAtual.MetasParaPromocao;
            string meta;
            Color cor = Azul;
            if (economia.EmPico && !economia.PicoFoiEscalado)
            {
                // pico de tráfego: o botão da meta vira o atalho para escalar o cluster
                meta = economia.PicoViolado ? "SLA violado! Escalar" : $"Escalar! {economia.LimiteParaEscalar - economia.SegundosDePico:0}s";
                cor = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0 ? Laranja : Vermelho;
            }
            else if (economia.PodePromover)
            {
                meta = "Promoção!";
                cor = Mathf.FloorToInt(Time.time / 0.4f) % 2 == 0 ? Amarelo : Color.white;
            }
            else if (metas.Length > 0)
            {
                int feitas = 0;
                foreach (var m in metas) if (economia.Cumprida(m)) feitas++;
                meta = $"Meta {feitas}/{metas.Length}";
            }
            else meta = "";
            botaoMeta.Definir(meta, cor);
            botaoMeta.transform.localPosition = new Vector3(cenario.Largura - 46 - PixelTexto.LarguraAmpliada(meta), LinhaHud, 0); // longe do alerta da primeira torre
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
            Vector2 mundo = cam.ScreenToWorldPoint(janela.PosicaoCursor);
            var colisores = Physics2D.OverlapPointAll(mundo);
            janela.DefinirClicavel(colisores.Length > 0);
            // Com a faixa escondida ou o cursor em outro programa, nada de destaque nem clique
            bool nosso = janela.CursorSobreAJanela;
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
            if (!Input.GetMouseButtonDown(0)) return;
            if (escolhido == null)
            {
                // clique fora de tudo (em outro programa ou na área transparente): fecha o painel
                if (painel.Aberto)
                {
                    Debug.Log($"Painel fechado por clique fora (cursor {janela.PosicaoCursor}, sobre a janela: {janela.CursorSobreAJanela})");
                    FecharPainel();
                }
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

        void OnApplicationQuit() => Salvamento.Salvar(economia.Estado);

        public static string Formatar(double v)
        {
            if (v < 1000) return v < 10 && v % 1 != 0 ? v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : ((long)v).ToString();
            string[] sufixos = { "K", "M", "B", "T" };
            int i = -1;
            while (v >= 1000 && i < sufixos.Length - 1) { v /= 1000; i++; }
            return v.ToString(v < 10 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture) + sufixos[i];
        }
    }
}
