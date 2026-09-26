using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>O primeiro servidor do jogador: LED de energia aceso, LED de atividade piscando, e dá um pulinho ao ser clicado.</summary>
    public class ServidorVelho : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Ambar = PixelArt.Hex("ffbf3f"), Apagado = PixelArt.Hex("4a3f30");

        Faixa faixa;
        SpriteRenderer ledAtividade;
        Vector3 posicaoBase;
        float proximoPisca, pulo;

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

            // Os LEDs ficam nos "buracos" do sprite (x = 3 e 5 a partir da borda esquerda, y = 3)
            CriarLed(new Vector2(-3f, 3f)).color = Verde;
            ledAtividade = CriarLed(new Vector2(-1f, 3f));
        }

        SpriteRenderer CriarLed(Vector2 pos)
        {
            var led = new GameObject("LED").AddComponent<SpriteRenderer>();
            led.transform.SetParent(transform, false);
            led.transform.localPosition = pos;
            led.sprite = PixelArt.Pixel;
            led.sortingOrder = Ordem + 1;
            return led;
        }

        void Update()
        {
            // Atividade de disco: pisca em rajadas irregulares
            if (Time.time >= proximoPisca)
            {
                bool aceso = ledAtividade.color != Ambar && Random.value < 0.7f;
                ledAtividade.color = aceso ? Ambar : Apagado;
                proximoPisca = Time.time + (aceso ? Random.Range(0.04f, 0.12f) : Random.Range(0.05f, 0.6f));
            }

            pulo = Mathf.Max(0f, pulo - Time.deltaTime);
            transform.localPosition = posicaoBase + (pulo > 0.08f ? Vector3.up : Vector3.zero);
        }

        public void Clicar()
        {
            pulo = 0.18f;
            faixa.Ganhar(5, Topo + new Vector2(-4f, 1f));
        }
    }
}
