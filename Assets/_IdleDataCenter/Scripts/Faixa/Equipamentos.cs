using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Rack 42U com os servidores 1U encaixados de baixo para cima. Clicar no rack reinicia
    /// o primeiro 1U travado ou, se nenhum estiver, rende um clique normal.
    /// </summary>
    public class RackVisual : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Azul = PixelArt.Hex("5cc8ff"),
                              Apagado = PixelArt.Hex("1f2130"), Vermelho = PixelArt.Hex("ff3b4e");

        class Unidade { public SpriteRenderer corpo, energia, atividade; public bool travado; public float proximo; }

        Faixa faixa;
        Destaque destaque;
        SpriteRenderer alerta;
        readonly List<Unidade> unidades = new List<Unidade>();
        Vector3 posicaoBase;
        float pulo;

        public int Ordem => 3;
        public Vector2 Topo => posicaoBase + new Vector3(12f, 36f, 0f);

        public void Iniciar(Faixa faixa, Transform pai, Vector2 posicao)
        {
            this.faixa = faixa;
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            posicaoBase = posicao;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.Rack;
            sr.sortingOrder = Ordem;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);
            alerta = Filho("Alerta", Arte.Alerta, new Vector2(9, 38), 12);
            alerta.enabled = false;
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

        /// <summary>Garante o número de servidores 1U encaixados (vaga 0 é a de baixo).</summary>
        public void DefinirQuantidade(int quantidade)
        {
            while (unidades.Count < quantidade)
            {
                int vaga = unidades.Count;
                var pos = new Vector2(2, 2 + vaga * 4);
                var u = new Unidade { corpo = Filho("1U", Arte.Servidor1U, pos, Ordem + 1) };
                u.energia = Filho("LED", PixelArt.Pixel, pos + new Vector2(16, 1), Ordem + 2);
                u.atividade = Filho("LED", PixelArt.Pixel, pos + new Vector2(18, 1), Ordem + 2);
                unidades.Add(u);
            }
        }

        /// <summary>travados[v] = a unidade da vaga v está travada.</summary>
        public void DefinirTravados(System.Func<int, bool> travado)
        {
            bool algum = false;
            for (int v = 0; v < unidades.Count; v++)
            {
                unidades[v].travado = travado(v);
                algum |= unidades[v].travado;
            }
            alerta.enabled = algum;
        }

        void Update()
        {
            bool piscar = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
            foreach (var u in unidades)
            {
                if (u.travado)
                {
                    u.energia.color = piscar ? Vermelho : Apagado;
                    u.atividade.color = Apagado;
                    continue;
                }
                u.energia.color = Verde;
                if (Time.time >= u.proximo)
                {
                    bool ligar = u.atividade.color != Azul && Random.value < 0.8f;
                    u.atividade.color = ligar ? Azul : Apagado;
                    u.proximo = Time.time + Random.Range(0.03f, 0.25f);
                }
            }
            if (alerta.enabled) alerta.transform.localPosition = new Vector3(9, 38 + (piscar ? 1 : 0), 0);
            pulo = Mathf.Max(0f, pulo - Time.deltaTime);
            transform.localPosition = posicaoBase + (pulo > 0.08f ? Vector3.up : Vector3.zero);
        }

        public void Pular() => pulo = 0.18f;

        public void Clicar()
        {
            Pular();
            faixa.ClicarRack(Topo);
        }

        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }

    /// <summary>No-break: o visor mostra uma barra por nível e fica vermelho com sobrecarga.</summary>
    public class NoBreakVisual : MonoBehaviour
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Vermelho = PixelArt.Hex("ff3b4e");

        readonly List<SpriteRenderer> barras = new List<SpriteRenderer>();
        bool sobrecarga;

        public void Iniciar(Transform pai, Vector2 posicao)
        {
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.NoBreak;
            sr.sortingOrder = 3;
        }

        public void Atualizar(int nivel, bool comSobrecarga)
        {
            sobrecarga = comSobrecarga;
            while (barras.Count < nivel)
            {
                var b = new GameObject("Bateria").AddComponent<SpriteRenderer>();
                b.transform.SetParent(transform, false);
                b.transform.localPosition = new Vector3(3 + barras.Count * 3, 7, 0);
                b.transform.localScale = new Vector3(2, 2, 1);
                b.sprite = PixelArt.Pixel;
                b.sortingOrder = 4;
                barras.Add(b);
            }
        }

        void Update()
        {
            bool piscar = Mathf.FloorToInt(Time.time / 0.3f) % 2 == 0;
            foreach (var b in barras) b.color = sobrecarga ? (piscar ? Vermelho : new Color(0.3f, 0.1f, 0.1f)) : Verde;
        }
    }

    /// <summary>Ar-condicionado de parede soltando um fluxo de ar frio (pixels azuis descendo).</summary>
    public class ArCondicionadoVisual : MonoBehaviour
    {
        static readonly Color Frio = PixelArt.Hex("7cc8ff");
        float proximo;
        int coluna;

        public void Iniciar(Transform pai, Vector2 posicao)
        {
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Arte.ArCondicionado;
            sr.sortingOrder = 2;
        }

        void Update()
        {
            if (Time.time < proximo) return;
            proximo = Time.time + 0.25f;
            coluna = (coluna + 7) % 20;
            var p = new GameObject("Ar").AddComponent<SpriteRenderer>();
            p.transform.SetParent(transform.parent, false);
            p.transform.localPosition = transform.localPosition + new Vector3(2 + coluna, -1, 0);
            p.sprite = PixelArt.Pixel;
            p.color = Frio;
            p.sortingOrder = 2;
            Flutuante.Aplicar(p.gameObject, 1.2f, -6f);
        }
    }
}
