using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Monta e controla a faixa: câmera pixel-perfect, o armário do técnico com seus objetos,
    /// os cliques (e o "clique atravessando" nas áreas vazias) e uma economia provisória.
    /// </summary>
    public class Faixa : MonoBehaviour
    {
        const int ArmarioX = 8, ArmarioLargura = 120, ArmarioAltura = 44;
        const double ReceitaPorSegundo = 1;

        public int AlturaPiso => 5;
        public ServidorVelho Servidor { get; private set; }

        static readonly Color Amarelo = PixelArt.Hex("ffd65c");

        Camera cam;
        JanelaDesktop janela;
        Transform armario;
        PixelTexto textoDinheiro;
        double dinheiro;
        float acumulado;

        void Start()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // preto puro = transparente na faixa
            janela = gameObject.AddComponent<JanelaDesktop>();
            MontarArmario();
        }

        void MontarArmario()
        {
            armario = new GameObject("Armario").transform;
            armario.position = new Vector3(ArmarioX, 0f, 0f);
            var fundo = armario.gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Armario(ArmarioLargura, ArmarioAltura, AlturaPiso);
            armario.gameObject.AddComponent<BoxCollider2D>(); // o armário inteiro "segura" o clique

            Decoracao("Planta", Arte.Planta, new Vector2(12, AlturaPiso), 2);

            Servidor = new GameObject("Servidor").AddComponent<ServidorVelho>();
            Servidor.Iniciar(this, armario, new Vector2(100, AlturaPiso));

            Decoracao("Caneca", Arte.Caneca, Servidor.Topo + new Vector2(-3, 0), 4)
                .gameObject.AddComponent<Vapor>();

            new GameObject("Tecnico").AddComponent<Tecnico>().Iniciar(this, armario, 24, 86);

            textoDinheiro = PixelTexto.Criar(armario, new Vector2(4, 37), Amarelo, 10);

            var fechar = Decoracao("Fechar", Arte.Fechar, new Vector2(ArmarioLargura - 8, 36), 10);
            fechar.gameObject.AddComponent<BoxCollider2D>();
            fechar.gameObject.AddComponent<BotaoFechar>();
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

        public void Ganhar(double valor, Vector2 posicaoLocal)
        {
            dinheiro += valor;
            var texto = PixelTexto.Criar(armario, posicaoLocal, Amarelo, 12);
            texto.Definir("+" + Formatar(valor));
            Flutuante.Aplicar(texto.gameObject, 0.9f, 6f);
        }

        void Update()
        {
            AtualizarCamera();
            ProcessarCursor();

            // Economia provisória: o servidor rende R$ 1 por segundo
            acumulado += Time.deltaTime;
            while (acumulado >= 1f)
            {
                acumulado -= 1f;
                dinheiro += ReceitaPorSegundo;
            }
            textoDinheiro.Definir("R$ " + Formatar(dinheiro));
        }

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

            if (!Input.GetMouseButtonDown(0)) return;
            IClicavel escolhido = null;
            foreach (var c in colisores)
                if (c.TryGetComponent(out IClicavel alvo) && (escolhido == null || alvo.Ordem > escolhido.Ordem))
                    escolhido = alvo;
            escolhido?.Clicar();
        }

        public static string Formatar(double v)
        {
            if (v < 1000) return ((long)v).ToString();
            string[] sufixos = { "K", "M", "B", "T" };
            int i = -1;
            while (v >= 1000 && i < sufixos.Length - 1) { v /= 1000; i++; }
            return v.ToString(v < 10 ? "0.0" : "0", System.Globalization.CultureInfo.InvariantCulture) + sufixos[i];
        }
    }

    /// <summary>Fumacinha saindo da caneca: dois pixels que sobem e somem, alternados.</summary>
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
    }
}
