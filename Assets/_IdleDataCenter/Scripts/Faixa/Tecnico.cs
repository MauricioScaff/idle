using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O personagem. No dia a dia passeia, digita na mesa ou "confere" o servidor mais próximo.
    /// Quando algo trava, corre até lá e fica consertando (solta faíscas) até o servidor voltar:
    /// sozinho ele leva 30 s, e um clique seu no servidor resolve na hora.
    /// Clicar nele dá um pulinho com coração. O uniforme muda com o cargo.
    /// </summary>
    public class Tecnico : MonoBehaviour, IClicavel
    {
        enum Estado { Parado, Andando, Consertando, Digitando, Emergencia }

        const float Velocidade = 12f;       // pixels por segundo
        const float VelocidadeCorrendo = 26f;
        const float MinX = 44f;             // em frente à mesa (não passa dela)

        Faixa faixa;
        Cenario cenario;
        Arte.Visual visual;
        SpriteRenderer sr;
        Destaque destaque;
        Estado estado;
        float x, alvoX;
        float tempoEstado, proximaPiscada, proximaFaisca, pulo;
        bool indoParaMesa, indoConferir;

        public int Ordem => 5;

        float MaxX => cenario.LimiteTecnico;

        public void Iniciar(Faixa faixa, Cenario cenario, int cargo)
        {
            this.faixa = faixa;
            this.cenario = cenario;
            visual = Arte.Tecnico(cargo);
            transform.SetParent(cenario.transform, false);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = visual.Parado;
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
            float xAntes = x;

            // Emergência tem prioridade sobre qualquer outra coisa
            bool temIncidente = cenario.AlvoDeConserto(out float xIncidente);
            if (temIncidente && estado != Estado.Emergencia)
            {
                if (estado == Estado.Digitando) cenario.Tela.Digitando = false;
                estado = Estado.Emergencia;
            }

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
                        if (indoConferir) { estado = Estado.Consertando; tempoEstado = 2.5f; sr.flipX = false; }
                        else if (indoParaMesa) { estado = Estado.Digitando; tempoEstado = Random.Range(3f, 6f); sr.flipX = true; cenario.Tela.Digitando = true; }
                        else Parar();
                    }
                    break;

                case Estado.Consertando:
                    SoltarFaiscas(cenario.TopoMaisProximo);
                    if (tempoEstado <= 0f) Parar();
                    break;

                case Estado.Digitando:
                    if (tempoEstado <= 0f) Parar();
                    break;

                case Estado.Emergencia:
                    if (!temIncidente) { Parar(); break; }
                    float destino = Mathf.Clamp(xIncidente, MinX, MaxX);
                    sr.flipX = destino < x;
                    x = Mathf.MoveTowards(x, destino, VelocidadeCorrendo * dt);
                    if (Mathf.Approximately(x, destino))
                    {
                        sr.flipX = false;
                        SoltarFaiscas(cenario.TopoDoIncidente);
                    }
                    break;
            }

            AtualizarSprite(!Mathf.Approximately(xAntes, x));
            pulo = Mathf.Max(0f, pulo - dt);
            float altura = pulo > 0f ? Mathf.Round(Mathf.Sin(pulo / 0.4f * Mathf.PI) * 4f) : 0f;
            transform.localPosition = new Vector3(Mathf.Round(x), Cenario.AlturaPiso + altura, 0f);
        }

        void SoltarFaiscas(Vector2 topo)
        {
            if (Time.time < proximaFaisca) return;
            proximaFaisca = Time.time + 0.35f;
            faixa.Efeito(cenario.transform, Arte.Faisca, topo + new Vector2(Random.Range(-7, 5), Random.Range(-12, -2)), 0.3f, 2f);
        }

        void EscolherDestino()
        {
            float sorteio = Random.value;
            indoConferir = sorteio < 0.3f;
            indoParaMesa = sorteio >= 0.3f && sorteio < 0.6f;
            alvoX = indoConferir ? MaxX : indoParaMesa ? MinX : Mathf.Round(Random.Range(MinX + 6f, MaxX));
            estado = Estado.Andando;
            sr.flipX = alvoX < x;
        }

        void Parar()
        {
            if (estado == Estado.Digitando) cenario.Tela.Digitando = false;
            estado = Estado.Parado;
            indoParaMesa = indoConferir = false;
            tempoEstado = Random.Range(1.5f, 4.5f);
        }

        void AtualizarSprite(bool movendo)
        {
            if (movendo)
            {
                float passo = estado == Estado.Emergencia ? 0.1f : 0.2f; // correndo, as pernas vão mais rápido
                sr.sprite = Mathf.FloorToInt(Time.time / passo) % 2 == 0 ? visual.Passo : visual.Parado;
                return;
            }
            if (estado == Estado.Digitando || estado == Estado.Emergencia)
            {
                float ritmo = estado == Estado.Emergencia ? 0.1f : 0.15f;
                sr.sprite = Mathf.FloorToInt(Time.time / ritmo) % 2 == 0 ? visual.DigitandoA : visual.DigitandoB;
                return;
            }
            // Piscada de vez em quando
            if (Time.time >= proximaPiscada) proximaPiscada = Time.time + Random.Range(2f, 5f);
            sr.sprite = proximaPiscada - Time.time < 0.12f ? visual.Piscando : visual.Parado;
        }

        public void Comemorar()
        {
            if (pulo > 0f) return;
            pulo = 0.4f;
            faixa.Efeito(cenario.transform, Arte.Coracao, transform.localPosition + new Vector3(-2f, 17f, 0f), 0.9f, 6f);
        }

        public void Clicar() => Comemorar();

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }
}
