using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Canto SRE do data center pequeno: os nós do cluster Kubernetes, o balanceador de carga e o telão do NOC
    /// com o gráfico de tráfego ao vivo. Num pico o gráfico dispara e os nós piscam laranja até alguém escalar;
    /// escalado, eles aceleram em azul; com o SLA violado, ficam vermelhos. Clicar num nó escala o cluster.
    /// </summary>
    public class SreVisual : MonoBehaviour
    {
        const int TelaoL = 92, TelaoA = 18;

        static readonly Color Verde = PixelArt.Hex("5cff8a"), Azul = PixelArt.Hex("5cc8ff"), Laranja = PixelArt.Hex("ffa53c"),
                              Vermelho = PixelArt.Hex("ff3b4e"), Apagado = PixelArt.Hex("22263c");

        // Nó do cluster (12×20): chassi fino com 4 lâminas
        static readonly Sprite SpriteNo = PixelArt.Criar(new[]
        {
            "kkkkkkkkkkkk",
            "kWWWWWWWWWWk",
            "kWkkkkkkkkWk",
            "kWkoooooookk",
            "kWkwwwwwwwkk",
            "kWkoooooookk",
            "kWkwwwwwwwkk",
            "kWkoooooookk",
            "kWkwwwwwwwkk",
            "kWkoooooookk",
            "kWkwwwwwwwkk",
            "kWkoooooookk",
            "kWkkkkkkkkWk",
            "kWWWWWWWWWWk",
            "kWcWWWWWWWWk",
            "kWWWWWWWWWWk",
            "kWWWWWWWWWWk",
            "kWWWWWWWWWWk",
            "kWWWWWWWWWWk",
            "kkkkkkkkkkkk",
        }, new Vector2(0.5f, 0f));

        // Balanceador de carga (18×12): caixa baixa com setas saindo para os lados
        static readonly Sprite SpriteBalanceador = PixelArt.Criar(new[]
        {
            "kkkkkkkkkkkkkkkkkk",
            "kWWWWWWWWWWWWWWWWk",
            "kWkkkkkkkkkkkkkkWk",
            "kWkooooooooooookWk",
            "kWkoooyooooyooookk",
            "kWkooyyyyyyyyoookk",
            "kWkoooyooooyooookk",
            "kWkooooooooooookWk",
            "kWkkkkkkkkkkkkkkWk",
            "kWWgWWWWWWWWWWWWWk",
            "kWWWWWWWWWWWWWWWWk",
            "kkkkkkkkkkkkkkkkkk",
        }, new Vector2(0.5f, 0f));

        class No { public SpriteRenderer sr; public readonly List<SpriteRenderer> leds = new List<SpriteRenderer>(); public float proximo; }

        /// <summary>Clique num nó: com pico sem escala, escala; senão rende um clique.</summary>
        class AlvoNo : MonoBehaviour, IClicavel
        {
            public SreVisual dono;
            public Destaque destaque;
            public int Ordem => 3;
            public void Clicar() => dono.faixa.ClicarCluster(dono.Topo);
            public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
        }

        Faixa faixa;
        Economia economia;
        float[] posicoesNos;
        float xBalanceador;
        Vector2 posTelao;
        readonly List<No> nos = new List<No>();
        SpriteRenderer balanceador;
        PixelCanvas telao;
        readonly float[] trafego = new float[TelaoL - 4];
        float proximoQuadro;

        public Vector2 Topo => new Vector2(posicoesNos[0] + 30, Cenario.AlturaPiso + 22);

        public void Iniciar(Faixa faixa, Economia economia, Transform pai, float[] posicoesNos, float xBalanceador, Vector2 posTelao)
        {
            this.faixa = faixa;
            this.economia = economia;
            this.posicoesNos = posicoesNos;
            this.xBalanceador = xBalanceador;
            this.posTelao = posTelao;
            transform.SetParent(pai, false);

            telao = new PixelCanvas(TelaoL, TelaoA);
            var t = Filho("Telao", telao.Sprite, posTelao, 2);
            t.sortingOrder = 2;
            for (int i = 0; i < trafego.Length; i++) trafego[i] = 0.35f + 0.1f * Mathf.Sin(i * 0.4f);
            Atualizar();
        }

        public void Atualizar()
        {
            while (nos.Count < economia.NosKubernetes && nos.Count < posicoesNos.Length)
            {
                var n = new No();
                n.sr = Filho("NoK8s", SpriteNo, new Vector2(posicoesNos[nos.Count], Cenario.AlturaPiso), 3);
                n.sr.gameObject.AddComponent<BoxCollider2D>();
                var alvo = n.sr.gameObject.AddComponent<AlvoNo>();
                alvo.dono = this;
                alvo.destaque = Destaque.Para(n.sr);
                for (int lamina = 0; lamina < 4; lamina++)
                for (int i = 0; i < 2; i++)
                    n.leds.Add(Filho("LED", PixelArt.Pixel, new Vector2(posicoesNos[nos.Count] - 2 + i * 2, Cenario.AlturaPiso + 9 + lamina * 2), 4));
                nos.Add(n);
                faixa.Faiscas(new Vector2(posicoesNos[nos.Count - 1], Cenario.AlturaPiso + 20), 3);
            }
            if (economia.TemBalanceador && balanceador == null)
            {
                balanceador = Filho("Balanceador", SpriteBalanceador, new Vector2(xBalanceador, Cenario.AlturaPiso), 3);
                faixa.Faiscas(new Vector2(xBalanceador, Cenario.AlturaPiso + 12), 3);
            }
        }

        SpriteRenderer Filho(string nome, Sprite sprite, Vector2 pos, int ordem)
        {
            var s = new GameObject(nome).AddComponent<SpriteRenderer>();
            s.transform.SetParent(transform, false);
            s.transform.localPosition = pos;
            s.sprite = sprite;
            s.sortingOrder = ordem;
            return s;
        }

        void Update()
        {
            bool pico = economia.EmPico, escalado = economia.PicoFoiEscalado, violado = economia.PicoViolado;
            bool piscar = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;

            foreach (var n in nos)
            {
                if (pico && !escalado)
                {
                    Color c = violado ? Vermelho : Laranja;
                    foreach (var l in n.leds) l.color = piscar ? c : new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f);
                    continue;
                }
                if (Time.time < n.proximo) continue;
                n.proximo = Time.time + (escalado ? Random.Range(0.03f, 0.1f) : Random.Range(0.15f, 0.5f));
                foreach (var l in n.leds) l.color = Random.value < 0.75f ? (escalado ? Azul : Verde) : Apagado;
            }

            if (Time.time >= proximoQuadro)
            {
                proximoQuadro = Time.time + 0.15f;
                DesenharTelao(pico, escalado, violado, piscar);
            }
        }

        /// <summary>Telão do NOC: gráfico de tráfego rolando para a esquerda; num pico a linha dispara.</summary>
        void DesenharTelao(bool pico, bool escalado, bool violado, bool piscar)
        {
            // novo ponto do gráfico
            System.Array.Copy(trafego, 1, trafego, 0, trafego.Length - 1);
            float alvo = pico ? 0.9f : 0.35f;
            float ultimo = trafego[trafego.Length - 2];
            trafego[trafego.Length - 1] = Mathf.Clamp01(Mathf.Lerp(ultimo, alvo, 0.25f) + Random.Range(-0.06f, 0.06f));

            var C = (System.Func<string, Color32>)PixelCanvas.C;
            telao.Limpar(C("#1b1a2e"));
            telao.Ret(1, 1, TelaoL - 2, TelaoA - 2, C("#0f1a2a"));
            // grade discreta
            for (int x = 2; x < TelaoL - 2; x += 8) telao.Ret(x, 2, 1, TelaoA - 4, C("#16263a"));
            // linha do limite de capacidade (sobe quando escalado)
            int limite = escalado ? 3 : 7;
            telao.Ret(2, limite, TelaoL - 4, 1, C(escalado ? "#2a5a7a" : "#5a3a2a"));
            string cor = !pico ? "#5cff8a" : escalado ? "#5cc8ff" : violado ? "#ff3b4e" : "#ffa53c";
            for (int i = 0; i < trafego.Length; i++)
            {
                int y = TelaoA - 3 - Mathf.RoundToInt(trafego[i] * (TelaoA - 6));
                telao.Pixel(2 + i, y, C(cor));
                telao.Pixel(2 + i, y + 1, C(pico ? "#3a2a1a" : "#1a3a2a"));
            }
            // luz de alerta no canto
            if (pico && !escalado && piscar) telao.Ret(TelaoL - 5, 2, 3, 3, C(violado ? "#ff3b4e" : "#ffa53c"));
            telao.Aplicar();
        }
    }
}
