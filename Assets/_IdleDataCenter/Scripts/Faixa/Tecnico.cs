using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O personagem: passeia pelo armário, pisca, às vezes vai "consertar" um servidor
    /// (soltando faíscas) e dá um pulinho com coração quando é clicado.
    /// </summary>
    public class Tecnico : MonoBehaviour, IClicavel
    {
        enum Estado { Parado, Andando, Consertando }

        const float Velocidade = 12f;     // pixels por segundo
        const float MinX = 24f;           // não passa da planta
        const float DistanciaServidor = 11f;

        Faixa faixa;
        SpriteRenderer sr;
        Destaque destaque;
        Estado estado;
        ServidorVelho servidorAlvo;
        float x, alvoX;
        float tempoEstado, proximaPiscada, proximaFaisca, pulo;

        public int Ordem => 5;

        /// <summary>Limite à direita: fica ao lado do servidor mais à esquerda.</summary>
        float MaxX => faixa.XServidorMaisAEsquerda - DistanciaServidor;

        public void Iniciar(Faixa faixa, Transform pai)
        {
            this.faixa = faixa;
            transform.SetParent(pai, false);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.TecnicoParado;
            sr.sortingOrder = Ordem;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);
            x = Mathf.Round((MinX + MaxX) / 2f);
            Parar();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            tempoEstado -= dt;
            x = Mathf.Clamp(x, MinX, MaxX); // um servidor novo pode ter ocupado o lugar

            switch (estado)
            {
                case Estado.Parado:
                    if (tempoEstado <= 0f) EscolherDestino();
                    break;

                case Estado.Andando:
                    alvoX = Mathf.Clamp(alvoX, MinX, MaxX);
                    x = Mathf.MoveTowards(x, alvoX, Velocidade * dt);
                    if (Mathf.Approximately(x, alvoX))
                    {
                        if (servidorAlvo != null) { estado = Estado.Consertando; tempoEstado = 2.5f; sr.flipX = false; }
                        else Parar();
                    }
                    break;

                case Estado.Consertando:
                    if (Time.time >= proximaFaisca && servidorAlvo != null)
                    {
                        proximaFaisca = Time.time + 0.35f;
                        var pos = servidorAlvo.transform.localPosition + new Vector3(Random.Range(-7, 5), Random.Range(6, 16), 0f);
                        faixa.Efeito(Arte.Faisca, pos, 0.3f, 2f);
                    }
                    if (tempoEstado <= 0f) Parar();
                    break;
            }

            AtualizarSprite();
            pulo = Mathf.Max(0f, pulo - dt);
            float altura = pulo > 0f ? Mathf.Round(Mathf.Sin(pulo / 0.4f * Mathf.PI) * 4f) : 0f;
            transform.localPosition = new Vector3(Mathf.Round(x), faixa.AlturaPiso + altura, 0f);
        }

        void EscolherDestino()
        {
            // Às vezes vai até o servidor mais próximo dele (o da esquerda) para "consertar"
            servidorAlvo = Random.value < 0.35f ? faixa.ServidorMaisAEsquerda : null;
            alvoX = servidorAlvo != null ? MaxX : Mathf.Round(Random.Range(MinX, MaxX));
            estado = Estado.Andando;
            sr.flipX = alvoX < x;
        }

        void Parar()
        {
            estado = Estado.Parado;
            servidorAlvo = null;
            tempoEstado = Random.Range(1.5f, 4.5f);
        }

        void AtualizarSprite()
        {
            if (estado == Estado.Andando)
            {
                sr.sprite = Mathf.FloorToInt(Time.time / 0.2f) % 2 == 0 ? Arte.TecnicoPasso : Arte.TecnicoParado;
                return;
            }
            // Piscada de vez em quando
            if (Time.time >= proximaPiscada) proximaPiscada = Time.time + Random.Range(2f, 5f);
            sr.sprite = proximaPiscada - Time.time < 0.12f ? Arte.TecnicoPiscando : Arte.TecnicoParado;
        }

        public void Comemorar()
        {
            if (pulo > 0f) return;
            pulo = 0.4f;
            faixa.Efeito(Arte.Coracao, transform.localPosition + new Vector3(-2f, 17f, 0f), 0.9f, 6f);
        }

        public void Clicar() => Comemorar();

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }
}
