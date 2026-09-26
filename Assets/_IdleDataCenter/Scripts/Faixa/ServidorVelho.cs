using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Servidor torre: LED de energia aceso, LED de atividade piscando e dá um pulinho ao ser clicado.
    /// As melhorias aparecem nele (SSD deixa o LED azul, ventoinhas giram na frente) e, quando trava,
    /// os LEDs ficam vermelhos e um alerta flutua em cima até alguém reiniciar.
    /// </summary>
    public class ServidorVelho : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Ambar = PixelArt.Hex("ffbf3f"),
                              Azul = PixelArt.Hex("5cc8ff"), Apagado = PixelArt.Hex("4a3f30"), Vermelho = PixelArt.Hex("ff3b4e");

        // Onde cada ventoinha aparece, em pixels a partir do pé do servidor (o sprite vai de x = -6 a 5)
        static readonly Vector2[] PosicoesVentoinhas = { new Vector2(-5, 6), new Vector2(-1, 6), new Vector2(-3, 10) };

        Faixa faixa;
        SpriteRenderer ledEnergia, ledAtividade, alerta;
        SpriteRenderer[] ventoinhas;
        Destaque destaque;
        Vector3 posicaoBase;
        float proximoPisca, pulo;
        bool ssd, travado;

        public int Indice { get; private set; }
        public int Ordem => 3;
        public float X => posicaoBase.x;

        /// <summary>Topo do gabinete, em coordenadas do cenário.</summary>
        public Vector2 Topo => posicaoBase + new Vector3(0f, 18f, 0f);

        public void Iniciar(Faixa faixa, Transform pai, Vector2 posicaoLocal, int indice)
        {
            this.faixa = faixa;
            Indice = indice;
            transform.SetParent(pai, false);
            transform.localPosition = posicaoLocal;
            posicaoBase = transform.localPosition;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.Servidor;
            sr.sortingOrder = Ordem;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);

            // Os LEDs ficam nos "buracos" do sprite (x = 3 e 5 a partir da borda esquerda, y = 3)
            ledEnergia = NovoFilho("LED", PixelArt.Pixel, new Vector2(-3f, 3f));
            ledEnergia.color = Verde;
            ledAtividade = NovoFilho("LED", PixelArt.Pixel, new Vector2(-1f, 3f));

            ventoinhas = new SpriteRenderer[PosicoesVentoinhas.Length];
            for (int i = 0; i < ventoinhas.Length; i++)
            {
                ventoinhas[i] = NovoFilho("Ventoinha", Arte.VentoinhaA, PosicoesVentoinhas[i]);
                ventoinhas[i].enabled = false;
            }

            alerta = NovoFilho("Alerta", Arte.Alerta, new Vector2(-2f, 29f)); // acima da caneca
            alerta.sortingOrder = 12;
            alerta.enabled = false;
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

        public void DefinirTravado(bool sim)
        {
            if (sim == travado) return;
            travado = sim;
            alerta.enabled = sim;
            if (!sim) ledEnergia.color = Verde;
        }

        void Update()
        {
            if (travado)
            {
                // pisca vermelho, ventoinhas param e o alerta sobe e desce
                bool aceso = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
                ledEnergia.color = aceso ? Vermelho : Apagado;
                ledAtividade.color = Apagado;
                alerta.transform.localPosition = new Vector3(-2f, 29f + (aceso ? 1f : 0f), 0f);
            }
            else if (Time.time >= proximoPisca)
            {
                // Atividade de disco: rajadas irregulares (com SSD, azul e bem mais rápido)
                Color cor = ssd ? Azul : Ambar;
                bool ligar = ledAtividade.color != cor && Random.value < (ssd ? 0.9f : 0.7f);
                ledAtividade.color = ligar ? cor : Apagado;
                float fator = ssd ? 0.4f : 1f;
                proximoPisca = Time.time + fator * (ligar ? Random.Range(0.04f, 0.12f) : Random.Range(0.05f, 0.6f));
            }

            for (int i = 0; i < ventoinhas.Length; i++)
                if (ventoinhas[i].enabled && !travado)
                    ventoinhas[i].sprite = Mathf.FloorToInt(Time.time * 12f + i) % 2 == 0 ? Arte.VentoinhaA : Arte.VentoinhaB;

            pulo = Mathf.Max(0f, pulo - Time.deltaTime);
            transform.localPosition = posicaoBase + (pulo > 0.08f ? Vector3.up : Vector3.zero);
        }

        public void Pular() => pulo = 0.18f;

        public void Clicar()
        {
            Pular();
            faixa.ClicarServidor(Indice, Topo);
        }

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }
}
