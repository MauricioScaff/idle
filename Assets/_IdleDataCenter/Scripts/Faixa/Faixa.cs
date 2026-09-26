using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Monta e liga a faixa: câmera pixel-perfect, o armário do técnico, a loja, os cliques
    /// (com "clique atravessando" nas áreas vazias), o save e o progresso offline.
    /// A economia em si fica em Simulacao.Economia.
    /// </summary>
    public class Faixa : MonoBehaviour
    {
        const int ArmarioX = 8, ArmarioLargura = 120, ArmarioAltura = 44;
        const float IntervaloSalvamento = 30f;

        // Servidores do armário, da direita para a esquerda
        static readonly float[] PosicoesServidores = { 100, 84, 68 };

        static readonly Color Amarelo = PixelArt.Hex("ffd65c"), VerdeClaro = PixelArt.Hex("9be89b");

        public int AlturaPiso => 5;
        public ServidorVelho ServidorMaisAEsquerda => servidores[servidores.Count - 1];
        public float XServidorMaisAEsquerda => ServidorMaisAEsquerda.transform.localPosition.x;

        Economia economia;
        Camera cam;
        JanelaDesktop janela;
        Transform armario;
        Loja loja;
        Tecnico tecnico;
        readonly List<ServidorVelho> servidores = new List<ServidorVelho>();
        PixelTexto textoDinheiro, textoReceita;
        SpriteRenderer setaDica;
        IClicavel sobCursor;
        float proximoSalvamento;
        double ganhoOffline;

        void Awake()
        {
            economia = new Economia(Salvamento.Carregar());
            ganhoOffline = economia.AplicarOffline(Salvamento.AgoraUnix);
            economia.Comprou += AoComprar;
        }

        void Start()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // preto puro = transparente na faixa
            janela = gameObject.AddComponent<JanelaDesktop>();

            MontarArmario();
            loja = new GameObject("Loja").AddComponent<Loja>();
            loja.Iniciar(economia, new Vector2(ArmarioX + ArmarioLargura + 4, 0));

            if (ganhoOffline > 0)
            {
                loja.MostrarAviso("Voltou! +R$ " + Formatar(ganhoOffline), 8f);
                Ganho(ganhoOffline, servidores[0].Topo + new Vector2(-8, 3));
            }
            proximoSalvamento = Time.time + IntervaloSalvamento;
        }

        void MontarArmario()
        {
            armario = new GameObject("Armario").transform;
            armario.position = new Vector3(ArmarioX, 0f, 0f);
            var fundo = armario.gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Armario(ArmarioLargura, ArmarioAltura, AlturaPiso);
            armario.gameObject.AddComponent<BoxCollider2D>(); // o armário inteiro "segura" o clique

            Decoracao("Planta", Arte.Planta, new Vector2(12, AlturaPiso), 2);

            for (int i = 0; i < economia.Servidores; i++) AdicionarServidor();
            AtualizarServidores();

            Decoracao("Caneca", Arte.Caneca, servidores[0].Topo + new Vector2(-3, 0), 4)
                .gameObject.AddComponent<Vapor>();

            tecnico = new GameObject("Tecnico").AddComponent<Tecnico>();
            tecnico.Iniciar(this, armario);

            textoDinheiro = PixelTexto.Criar(armario, new Vector2(4, 37), Amarelo, 10);
            textoReceita = PixelTexto.Criar(armario, new Vector2(40, 37), VerdeClaro, 10);

            var fechar = Decoracao("Fechar", Arte.Fechar, new Vector2(ArmarioLargura - 8, 36), 10);
            fechar.gameObject.AddComponent<BoxCollider2D>();
            fechar.gameObject.AddComponent<BotaoFechar>();

            // Dica do primeiro clique: setinha pulando sobre o servidor
            setaDica = Decoracao("Dica", Arte.Seta, servidores[0].Topo + new Vector2(-10, 3), 11);
            setaDica.enabled = !economia.Estado.jaClicouNoServidor;
        }

        void AdicionarServidor()
        {
            var s = new GameObject("Servidor").AddComponent<ServidorVelho>();
            s.Iniciar(this, armario, new Vector2(PosicoesServidores[servidores.Count], AlturaPiso));
            servidores.Add(s);
        }

        void AtualizarServidores()
        {
            foreach (var s in servidores) s.AtualizarVisual(economia.TemSsd, economia.Ventoinhas);
        }

        SpriteRenderer Decoracao(string nome, Sprite sprite, Vector2 posicaoLocal, int ordem)
        {
            var sr = new GameObject(nome).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(armario, false);
            sr.transform.localPosition = posicaoLocal;
            sr.sprite = sprite;
            sr.sortingOrder = ordem;
            return sr;
        }

        /// <summary>Solta um sprite que sobe e some (posição em coordenadas do armário).</summary>
        public void Efeito(Sprite sprite, Vector2 posicaoLocal, float duracao, float subida)
        {
            var sr = Decoracao("Efeito", sprite, posicaoLocal, 12);
            Flutuante.Aplicar(sr.gameObject, duracao, subida);
        }

        /// <summary>Texto "+R$ X" subindo a partir de um ponto do armário.</summary>
        void Ganho(double valor, Vector2 posicaoLocal)
        {
            var texto = PixelTexto.Criar(armario, posicaoLocal, Amarelo, 13);
            texto.Definir("+" + Formatar(valor));
            Flutuante.Aplicar(texto.gameObject, 0.9f, 6f);
        }

        public void ClicarServidor(ServidorVelho servidor)
        {
            double valor = economia.Clicar();
            Ganho(valor, servidor.Topo + new Vector2(-4, 1));
            setaDica.enabled = false;
        }

        void AoComprar(string id)
        {
            if (id == Catalogo.Servidor)
            {
                AdicionarServidor();
                var novo = ServidorMaisAEsquerda;
                for (int i = 0; i < 4; i++) Efeito(Arte.Faisca, novo.Topo + new Vector2(Random.Range(-8, 6), Random.Range(-12, 0)), 0.5f, 3f);
            }
            AtualizarServidores();
            foreach (var s in servidores) s.Pular();
            tecnico.Comemorar();
            Salvamento.Salvar(economia.Estado);
        }

        void Update()
        {
            AtualizarCamera();
            ProcessarCursor();

            economia.Avancar(Time.deltaTime);
            string dinheiro = "R$ " + Formatar(economia.Dinheiro);
            textoDinheiro.Definir(dinheiro);
            textoReceita.Definir("+" + Formatar(economia.ReceitaPorSegundo) + "/s");
            textoReceita.transform.localPosition = new Vector3(4 + PixelTexto.Largura(dinheiro) + 5, 37, 0);

            if (setaDica.enabled)
                setaDica.transform.localPosition = servidores[0].Topo + new Vector2(-10, 3 + (Mathf.FloorToInt(Time.time / 0.4f) % 2));

            if (Time.time >= proximoSalvamento)
            {
                proximoSalvamento = Time.time + IntervaloSalvamento;
                Salvamento.Salvar(economia.Estado);
            }
        }

        void OnApplicationQuit() => Salvamento.Salvar(economia.Estado);

        /// <summary>
        /// Câmera pixel-perfect: cada pixel da arte vira um quadrado inteiro de pixels na tela,
        /// e o canto de baixo à esquerda da janela é a coordenada (0, 0).
        /// </summary>
        void AtualizarCamera()
        {
            int escala = Mathf.Max(1, Screen.height / JanelaDesktop.AlturaVirtual);
            cam.orthographicSize = Screen.height / (2f * escala);
            cam.transform.position = new Vector3(Screen.width / (2f * escala), cam.orthographicSize, -10f);
        }

        void ProcessarCursor()
        {
            Vector2 mundo = cam.ScreenToWorldPoint(janela.PosicaoCursor);
            var colisores = Physics2D.OverlapPointAll(mundo);
            janela.DefinirClicavel(colisores.Length > 0);

            IClicavel escolhido = null;
            foreach (var c in colisores)
                if (c.TryGetComponent(out IClicavel alvo) && (escolhido == null || alvo.Ordem > escolhido.Ordem))
                    escolhido = alvo;

            if (escolhido != sobCursor)
            {
                sobCursor?.DefinirDestaque(false);
                escolhido?.DefinirDestaque(true);
                sobCursor = escolhido;
            }
            if (escolhido != null && Input.GetMouseButtonDown(0)) escolhido.Clicar();
        }

        public static string Formatar(double v)
        {
            if (v < 1000) return v < 10 && v % 1 != 0 ? v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : ((long)v).ToString();
            string[] sufixos = { "K", "M", "B", "T" };
            int i = -1;
            while (v >= 1000 && i < sufixos.Length - 1) { v /= 1000; i++; }
            return v.ToString(v < 10 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture) + sufixos[i];
        }
    }

    /// <summary>Fumacinha saindo da caneca: pixels que sobem e somem, alternando de lado.</summary>
    public class Vapor : MonoBehaviour
    {
        float proximo;
        int lado;

        void Update()
        {
            if (Time.time < proximo) return;
            proximo = Time.time + 0.6f;
            lado = 1 - lado;
            var p = new GameObject("Vapor").AddComponent<SpriteRenderer>();
            p.transform.SetParent(transform.parent, false);
            p.transform.localPosition = transform.localPosition + new Vector3(-2 + lado * 2, 6f, 0f);
            p.sprite = PixelArt.Pixel;
            p.color = new Color(1f, 1f, 1f, 0.8f);
            p.sortingOrder = 4;
            Flutuante.Aplicar(p.gameObject, 1.2f, 4f);
        }
    }

    public class BotaoFechar : MonoBehaviour, IClicavel
    {
        public int Ordem => 10;
        public void Clicar() => Application.Quit();
        // A cor do renderizador multiplica a do sprite: cinza = apagado, branco = cor original acesa
        void Start() => DefinirDestaque(false);
        public void DefinirDestaque(bool ligado) => GetComponent<SpriteRenderer>().color = ligado ? Color.white : new Color(0.7f, 0.7f, 0.7f);
    }
}
