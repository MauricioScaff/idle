using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O personagem: passeia pelo armário, pisca, às vezes vai "consertar" o servidor
    /// (soltando faíscas) e dá um pulinho com coração quando é clicado.
    /// </summary>
    public class Tecnico : MonoBehaviour, IClicavel
    {
        enum Estado { Parado, Andando, Consertando }

        const float Velocidade = 12f; // pixels por segundo

        Faixa faixa;
        SpriteRenderer sr;
        Estado estado;
        float x, alvoX, minX, maxX;
        float tempoEstado, proximaPiscada, proximaFaisca, pulo;
        bool indoAoServidor;

        public int Ordem => 5;

        public void Iniciar(Faixa faixa, Transform pai, float minX, float maxX)
        {
            this.faixa = faixa;
            this.minX = minX;
            this.maxX = maxX;
            transform.SetParent(pai, false);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.TecnicoParado;
            sr.sortingOrder = Ordem;
            gameObject.AddComponent<BoxCollider2D>();
            x = Mathf.Round((minX + maxX) / 2f);
            Parar();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            tempoEstado -= dt;

            switch (estado)
            {
                case Estado.Parado:
                    if (tempoEstado <= 0f) EscolherDestino();
                    break;

                case Estado.Andando:
                    x = Mathf.MoveTowards(x, alvoX, Velocidade * dt);
                    if (Mathf.Approximately(x, alvoX))
                    {
                        if (indoAoServidor) { estado = Estado.Consertando; tempoEstado = 2.5f; sr.flipX = false; }
                        else Parar();
                    }
                    break;

                case Estado.Consertando:
                    if (Time.time >= proximaFaisca)
                    {
                        proximaFaisca = Time.time + 0.35f;
                        var pos = faixa.Servidor.transform.localPosition + new Vector3(Random.Range(-7, 5), Random.Range(6, 16), 0f);
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
            indoAoServidor = Random.value < 0.35f;
            alvoX = indoAoServidor ? faixa.Servidor.transform.localPosition.x - 11f : Mathf.Round(Random.Range(minX, maxX));
            alvoX = Mathf.Clamp(alvoX, minX, maxX);
            estado = Estado.Andando;
            sr.flipX = alvoX < x;
        }

        void Parar()
        {
            estado = Estado.Parado;
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

        public void Clicar()
        {
            if (pulo > 0f) return;
            pulo = 0.4f;
            faixa.Efeito(Arte.Coracao, transform.localPosition + new Vector3(-2f, 17f, 0f), 0.9f, 6f);
        }
    }
}
