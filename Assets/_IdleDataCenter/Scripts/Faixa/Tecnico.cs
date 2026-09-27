using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O personagem (arte do PixelLab). No dia a dia passeia, digita na mesa ou "confere" o servidor
    /// mais próximo. Quando algo trava, corre até lá e fica consertando (solta faíscas) até o servidor
    /// voltar: sozinho ele leva 30 s, e um clique seu no servidor resolve na hora.
    /// Clicar nele dá um pulinho com coração. No cargo de Sysadmin a camiseta vira verde-água; no de Analista, polo azul-marinho.
    /// </summary>
    public class Tecnico : MonoBehaviour, IClicavel
    {
        enum Estado { Parado, Andando, Consertando, Digitando, Emergencia }

        class Quadros { public Sprite Parado; public Sprite[] Andar, Digitar; }

        const float Velocidade = 16f;          // pixels por segundo
        const float VelocidadeCorrendo = 34f;
        const float MatizSysadmin = 0.46f;     // verde-água
        const float MatizAnalista = 0.63f, BrilhoAnalista = 0.55f;  // polo azul-marinho

        static readonly Dictionary<string, Quadros> cache = new Dictionary<string, Quadros>();

        Faixa faixa;
        Cenario cenario;
        Quadros quadros;
        SpriteRenderer sr;
        Destaque destaque;
        Estado estado;
        float x, alvoX;
        float tempoEstado, proximaFaisca, pulo;
        bool indoParaMesa, indoConferir;
        float deslocamento;   // o estagiário para um pouco antes, para não ficar em cima do técnico
        bool usaMesa;

        public int Ordem => 5;

        float MinX => cenario.PosicaoMesa;
        float MaxX => cenario.LimiteTecnico - deslocamento;

        /// <summary>Carrega os quadros de um personagem (prefixo dos arquivos em Resources/Arte). Só o técnico troca de uniforme.</summary>
        static Quadros Carregar(string prefixo, int cargo)
        {
            string chave = prefixo + "#" + cargo;
            if (cache.TryGetValue(chave, out var q)) return q;
            Sprite S(string sufixo)
            {
                var t = ArteGerada.Textura(prefixo + sufixo);
                if (prefixo == "tecnico" && cargo == 1) t = ArteGerada.TrocarCorDaRoupa(t, MatizSysadmin);
                if (prefixo == "tecnico" && cargo >= 2) t = ArteGerada.TrocarCorDaRoupa(t, MatizAnalista, brilho: BrilhoAnalista);
                return ArteGerada.Personagem(t, prefixo + sufixo + "#" + cargo);
            }
            return cache[chave] = new Quadros
            {
                Parado = S("_lado"),
                Andar = new[] { S("_andar_0"), S("_andar_1"), S("_andar_2"), S("_andar_3") },
                Digitar = new[] { S("_digitando_0"), S("_digitando_1") },
            };
        }

        /// <param name="prefixo">"tecnico" (o personagem principal, que também digita na mesa) ou "estagiario".</param>
        public void Iniciar(Faixa faixa, Cenario cenario, int cargo, string prefixo = "tecnico", float deslocamento = 0f)
        {
            this.faixa = faixa;
            this.cenario = cenario;
            this.deslocamento = deslocamento;
            usaMesa = prefixo == "tecnico";
            quadros = Carregar(prefixo, cargo);
            transform.SetParent(cenario.transform, false);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = quadros.Parado;
            sr.sortingOrder = Ordem;

            // área de clique só na silhueta (o quadro tem muita borda transparente)
            var area = ArteGerada.AreaOpaca(quadros.Parado.texture);
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(area.width, area.height);
            col.offset = new Vector2(area.center.x - quadros.Parado.pivot.x, area.center.y - quadros.Parado.pivot.y);
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
                if (estado == Estado.Digitando && usaMesa) cenario.Tela.Digitando = false;
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
                        else if (indoParaMesa) { estado = Estado.Digitando; tempoEstado = cenario.EscrevendoAutomacao ? Random.Range(8f, 14f) : Random.Range(3f, 6f); sr.flipX = true; cenario.Tela.Digitando = true; }
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
                    float destino = Mathf.Clamp(xIncidente - deslocamento, MinX, MaxX);
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
            float altura = pulo > 0f ? Mathf.Round(Mathf.Sin(pulo / 0.4f * Mathf.PI) * 5f) : 0f;
            transform.localPosition = new Vector3(Mathf.Round(x), Cenario.AlturaPiso + altura, 0f);
        }

        void SoltarFaiscas(Vector2 topo)
        {
            if (Time.time < proximaFaisca) return;
            proximaFaisca = Time.time + 0.35f;
            faixa.Efeito(cenario.transform, Arte.Faisca, topo + new Vector2(Random.Range(-8, 6), Random.Range(-14, -2)), 0.3f, 2f);
        }

        void EscolherDestino()
        {
            float sorteio = Random.value;
            if (usaMesa && cenario.EscrevendoAutomacao)
            {
                // escrevendo um script: quase sempre volta para o teclado
                indoConferir = sorteio < 0.1f;
                indoParaMesa = sorteio >= 0.1f && sorteio < 0.85f;
            }
            else
            {
                indoConferir = sorteio < 0.3f;
                indoParaMesa = usaMesa && sorteio >= 0.3f && sorteio < 0.6f;
            }
            alvoX = indoConferir ? MaxX : indoParaMesa ? MinX : Mathf.Round(Random.Range(MinX + 8f, MaxX));
            estado = Estado.Andando;
            sr.flipX = alvoX < x;
        }

        void Parar()
        {
            if (estado == Estado.Digitando && usaMesa) cenario.Tela.Digitando = false;
            estado = Estado.Parado;
            indoParaMesa = indoConferir = false;
            tempoEstado = Random.Range(1.5f, 4.5f);
        }

        void AtualizarSprite(bool movendo)
        {
            if (movendo)
            {
                float passo = estado == Estado.Emergencia ? 0.09f : 0.16f; // correndo, as pernas vão mais rápido
                sr.sprite = quadros.Andar[Mathf.FloorToInt(Time.time / passo) % quadros.Andar.Length];
                return;
            }
            if (estado == Estado.Digitando || estado == Estado.Emergencia)
            {
                float ritmo = estado == Estado.Emergencia ? 0.1f : 0.18f;
                sr.sprite = quadros.Digitar[Mathf.FloorToInt(Time.time / ritmo) % quadros.Digitar.Length];
                return;
            }
            sr.sprite = quadros.Parado;
        }

        public void Comemorar()
        {
            if (pulo > 0f) return;
            pulo = 0.4f;
            faixa.Efeito(cenario.transform, Arte.Coracao, transform.localPosition + new Vector3(-2f, 38f, 0f), 0.9f, 6f);
        }

        public void Clicar() => Comemorar();

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }
}
