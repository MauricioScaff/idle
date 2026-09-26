using System;
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
        const int LinhaHud = Cenario.Altura - 7;   // linha do dinheiro, receita e meta no topo do cenário

        static readonly Color Amarelo = PixelArt.Hex("ffd65c"), VerdeClaro = PixelArt.Hex("9be89b"),
                              Laranja = PixelArt.Hex("ffbf3f"), Vermelho = PixelArt.Hex("ff3b4e"),
                              Azul = PixelArt.Hex("a9c7ff");

        Economia economia;
        Camera cam;
        JanelaDesktop janela;
        Cenario cenario;
        Painel painel;
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
        }

        void Start()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // preto puro = transparente na faixa
            janela = gameObject.AddComponent<JanelaDesktop>();

            MontarTudo();
            painel = new GameObject("Painel").AddComponent<Painel>();
            painel.Iniciar(this, economia, new Vector2(CenarioX, JanelaDesktop.AlturaVirtual + Painel.Espaco));

            if (ganhoOffline > 0)
            {
                Loja.MostrarAviso("Voltou! +R$ " + Formatar(ganhoOffline), 8f);
                Ganho(ganhoOffline, cenario.PrimeiraTorre.Topo + new Vector2(-8, 3));
            }
            proximoSalvamento = Time.time + IntervaloSalvamento;

            // Opções para testes: -painel=<aba> abre o painel, -incidente trava o primeiro servidor
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-painel"))
                {
                    AbrirPainel();
                    painel.AbrirAba(arg.Contains("=") ? arg.Substring(arg.IndexOf('=') + 1) : "visao");
                }
                if (arg == "-incidente") economia.Travar(0);
            }
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
            textoDinheiro = PixelTexto.Criar(raiz, new Vector2(4, LinhaHud), Amarelo, 10);
            textoReceita = PixelTexto.Criar(raiz, new Vector2(40, LinhaHud), VerdeClaro, 10);
            textoAmbiente = PixelTexto.Criar(raiz, new Vector2(80, LinhaHud), Laranja, 10);
            botaoMeta = BotaoTexto.Criar(raiz, new Vector2(100, LinhaHud), Azul, AoClicarMeta);
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
            var texto = PixelTexto.Criar(cenario.transform, posicaoLocal, Amarelo, 13);
            texto.Definir("+" + Formatar(valor));
            Flutuante.Aplicar(texto.gameObject, 0.9f, 6f);
        }

        void Faiscas(Vector2 topo, int quantidade)
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
            }
            else Ganho(valor, topo + new Vector2(-4, 1));
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

        /// <summary>Reiniciar pelo painel.</summary>
        public void Reiniciar(int servidor)
        {
            if (economia.Travado(servidor)) ClicarServidor(servidor, cenario.TopoDoServidor(servidor));
        }

        public void Promover() => economia.Promover();

        void AoClicarMeta()
        {
            if (economia.PodePromover) Promover();
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
            Salvamento.Salvar(economia.Estado);
        }

        void AoTravar(int servidor) => Loja.MostrarAviso("Servidor travou!", 2f, Vermelho);

        void AoVoltar(int servidor, bool peloTecnico)
        {
            if (!peloTecnico) return;
            Loja.MostrarAviso("Técnico consertou", 1.5f, VerdeClaro);
            cenario.Tecnico.Comemorar();
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
            Salvamento.Salvar(economia.Estado);
        }

        // ---------------- Painel ----------------

        /// <summary>O painel usa uma escala maior que a faixa (texto legível); na tela, cada pixel dele vale EscalaPainel pixels.</summary>
        float EscalaRelativaDoPainel => janela.EscalaPainel / (float)janela.Escala;

        void AbrirPainel()
        {
            painel.Abrir();
            janela.DefinirAlturaVirtual(JanelaDesktop.AlturaVirtual + Painel.Espaco + Mathf.CeilToInt(Painel.Altura * EscalaRelativaDoPainel) + 2);
        }

        void AbrirCarreira()
        {
            AbrirPainel();
            painel.AbrirCarreira();
        }

        void FecharPainel()
        {
            painel.Fechar();
            janela.DefinirAlturaVirtual(JanelaDesktop.AlturaVirtual);
        }

        void AlternarPainel()
        {
            if (painel.Aberto) FecharPainel();
            else AbrirPainel();
        }

        // ---------------- Laço ----------------

        void Update()
        {
            AtualizarCamera();
            painel.transform.localScale = Vector3.one * EscalaRelativaDoPainel;
            ProcessarCursor();

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

        void AtualizarHud()
        {
            string dinheiro = "R$ " + Formatar(economia.Dinheiro);
            textoDinheiro.Definir(dinheiro);
            float x = 4 + PixelTexto.Largura(dinheiro) + 5;

            string receita = "+" + Formatar(economia.ReceitaPorSegundo) + "/s";
            textoReceita.Definir(receita);
            textoReceita.transform.localPosition = new Vector3(x, LinhaHud, 0);
            x += PixelTexto.Largura(receita) + 6;

            // Energia e temperatura só importam a partir de Sysadmin
            if (economia.Cargo >= 1)
            {
                string ambiente = $"{economia.ConsumoKw:0.0}/{economia.CapacidadeKw:0.0}kW {economia.Temperatura:0}C";
                textoAmbiente.Definir(ambiente);
                textoAmbiente.DefinirCor(economia.Sobrecarga || economia.Quente ? Vermelho : Laranja);
                textoAmbiente.transform.localPosition = new Vector3(x, LinhaHud, 0);
            }
            else textoAmbiente.Definir("");

            // Meta de promoção, alinhada à direita (antes dos ícones)
            var metas = economia.CargoAtual.MetasParaPromocao;
            string meta;
            Color cor = Azul;
            if (economia.PodePromover)
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
            botaoMeta.transform.localPosition = new Vector3(cenario.Largura - 28 - PixelTexto.Largura(meta), LinhaHud, 0); // longe do alerta da primeira torre
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

            if (painel.Aberto && Input.GetKeyDown(KeyCode.Escape)) FecharPainel();
            if (!Input.GetMouseButtonDown(0)) return;
            if (escolhido == null)
            {
                // clique fora de tudo (em outro programa ou na área transparente): fecha o painel
                if (painel.Aberto) FecharPainel();
                return;
            }
            if (escolhido is Painel painelClicado)
            {
                painelClicado.ClicarEm(mundo);
                if (!painel.Aberto) janela.DefinirAlturaVirtual(JanelaDesktop.AlturaVirtual); // fechou pelo "x"
            }
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
