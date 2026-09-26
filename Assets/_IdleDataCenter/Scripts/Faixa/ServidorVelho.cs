using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Um servidor velho: LED de energia aceso, LED de atividade piscando e dá um pulinho ao ser clicado.
    /// As melhorias aparecem nele: SSD deixa o LED azul e mais agitado, e cada ventoinha surge girando na frente.
    /// </summary>
    public class ServidorVelho : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Ambar = PixelArt.Hex("ffbf3f"),
                              Azul = PixelArt.Hex("5cc8ff"), Apagado = PixelArt.Hex("4a3f30");

        // Onde cada ventoinha aparece, em pixels a partir do pé do servidor (o sprite vai de x = -6 a 5)
        static readonly Vector2[] PosicoesVentoinhas = { new Vector2(-5, 6), new Vector2(-1, 6), new Vector2(-3, 10) };

        Faixa faixa;
        SpriteRenderer ledAtividade;
        SpriteRenderer[] ventoinhas;
        Destaque destaque;
        Vector3 posicaoBase;
        float proximoPisca, pulo;
        bool ssd;

        public int Ordem => 3;

        /// <summary>Topo do gabinete, em coordenadas do armário.</summary>
        public Vector2 Topo => posicaoBase + new Vector3(0f, 18f, 0f);

        public void Iniciar(Faixa faixa, Transform pai, Vector2 posicaoLocal)
        {
            this.faixa = faixa;
            transform.SetParent(pai, false);
            transform.localPosition = posicaoLocal;
            posicaoBase = transform.localPosition;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.Servidor;
            sr.sortingOrder = Ordem;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);

            // Os LEDs ficam nos "buracos" do sprite (x = 3 e 5 a partir da borda esquerda, y = 3)
            NovoFilho("LED", PixelArt.Pixel, new Vector2(-3f, 3f)).color = Verde;
            ledAtividade = NovoFilho("LED", PixelArt.Pixel, new Vector2(-1f, 3f));

            ventoinhas = new SpriteRenderer[PosicoesVentoinhas.Length];
            for (int i = 0; i < ventoinhas.Length; i++)
            {
                ventoinhas[i] = NovoFilho("Ventoinha", Arte.VentoinhaA, PosicoesVentoinhas[i]);
                ventoinhas[i].enabled = false;
            }
        }

        SpriteRenderer NovoFilho(string nome, Sprite sprite, Vector2 pos)
        {
            var sr = new GameObject(nome).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.localPosition = pos;
            sr.sprite = sprite;
            sr.sortingOrder = Ordem + 1;
            return sr;
        }

        /// <summary>Mostra as melhorias compradas.</summary>
        public void AtualizarVisual(bool temSsd, int quantidadeVentoinhas)
        {
            ssd = temSsd;
            for (int i = 0; i < ventoinhas.Length; i++) ventoinhas[i].enabled = i < quantidadeVentoinhas;
        }

        void Update()
        {
            // Atividade de disco: pisca em rajadas irregulares (com SSD, azul e bem mais rápido)
            if (Time.time >= proximoPisca)
            {
                Color aceso = ssd ? Azul : Ambar;
                bool ligar = ledAtividade.color != aceso && Random.value < (ssd ? 0.9f : 0.7f);
                ledAtividade.color = ligar ? aceso : Apagado;
                float fator = ssd ? 0.4f : 1f;
                proximoPisca = Time.time + fator * (ligar ? Random.Range(0.04f, 0.12f) : Random.Range(0.05f, 0.6f));
            }

            // Ventoinhas giram (cada uma um pouco defasada para não parecerem sincronizadas)
            for (int i = 0; i < ventoinhas.Length; i++)
                if (ventoinhas[i].enabled)
                    ventoinhas[i].sprite = Mathf.FloorToInt(Time.time * 12f + i) % 2 == 0 ? Arte.VentoinhaA : Arte.VentoinhaB;

            pulo = Mathf.Max(0f, pulo - Time.deltaTime);
            transform.localPosition = posicaoBase + (pulo > 0.08f ? Vector3.up : Vector3.zero);
        }

        public void Pular() => pulo = 0.18f;

        public void Clicar()
        {
            Pular();
            faixa.ClicarServidor(this);
        }

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }
}
