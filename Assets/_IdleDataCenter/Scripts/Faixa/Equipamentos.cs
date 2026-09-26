using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Rack (arte do PixelLab) com 5 unidades 1U. Os LEDs de cada unidade são achados na imagem e
    /// agrupados por altura; unidade comprada pisca, unidade vazia fica apagada, travada fica vermelha.
    /// Clicar no rack reinicia o primeiro 1U travado ou, se nenhum estiver, rende um clique normal.
    /// </summary>
    public class RackVisual : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Azul = PixelArt.Hex("5cc8ff"),
                              Apagado = PixelArt.Hex("1f2130"), Vermelho = PixelArt.Hex("ff3b4e");

        class Unidade { public List<SpriteRenderer> leds = new List<SpriteRenderer>(); public bool ativa, travado; public float proximo; }

        Faixa faixa;
        Destaque destaque;
        SpriteRenderer alerta;
        readonly List<Unidade> unidades = new List<Unidade>();
        Vector3 posicaoBase;
        float altura, pulo;

        public int Ordem => 3;
        public int Vagas => unidades.Count;
        public Vector2 Topo => posicaoBase + new Vector3(0f, altura, 0f);

        public void Iniciar(Faixa faixa, Transform pai, Vector2 posicao)
        {
            this.faixa = faixa;
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            posicaoBase = posicao;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ArteGerada.Objeto("rack");
            sr.sortingOrder = Ordem;
            altura = sr.sprite.rect.height;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);
            alerta = Filho("Alerta", Arte.Alerta, new Vector2(-2, altura + 2), 12);
            alerta.enabled = false;

            // agrupa os LEDs por altura: cada grupo é uma unidade 1U (de baixo para cima)
            Unidade atual = null;
            int ultimoY = int.MinValue;
            foreach (var p in ArteGerada.Leds(sr.sprite).OrderBy(p => p.y))
            {
                if (atual == null || p.y - ultimoY > 1) unidades.Add(atual = new Unidade());
                atual.leds.Add(Filho("LED", PixelArt.Pixel, p, Ordem + 1));
                ultimoY = p.y;
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

        public void DefinirQuantidade(int quantidade)
        {
            for (int v = 0; v < unidades.Count; v++) unidades[v].ativa = v < quantidade;
        }

        public void DefinirTravados(System.Func<int, bool> travado)
        {
            bool algum = false;
            for (int v = 0; v < unidades.Count; v++)
            {
                unidades[v].travado = unidades[v].ativa && travado(v);
                algum |= unidades[v].travado;
            }
            alerta.enabled = algum;
        }

        void Update()
        {
            bool piscar = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
            foreach (var u in unidades)
            {
                if (!u.ativa) { foreach (var l in u.leds) l.color = Apagado; continue; }
                if (u.travado) { foreach (var l in u.leds) l.color = piscar ? Vermelho : Apagado; continue; }
                if (Time.time < u.proximo) continue;
                u.proximo = Time.time + Random.Range(0.04f, 0.25f);
                for (int i = 0; i < u.leds.Count; i++)
                    u.leds[i].color = i == 0 ? Verde : Random.value < 0.7f ? Azul : Apagado;
            }
            if (alerta.enabled) alerta.transform.localPosition = new Vector3(-2, altura + 2 + (piscar ? 1 : 0), 0);
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

    /// <summary>Equipamento de chão com LEDs que piscam (no-break e refrigeração). Pode ficar em alerta (vermelho).</summary>
    public class EquipamentoVisual : MonoBehaviour
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Vermelho = PixelArt.Hex("ff3b4e"), Escuro = PixelArt.Hex("1f3a2a");

        readonly List<SpriteRenderer> leds = new List<SpriteRenderer>();
        bool alerta, soprarAr;
        float proximo, proximoAr;

        public float Altura { get; private set; }

        public void Iniciar(Transform pai, string arte, Vector2 posicao, bool soltaArFrio)
        {
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            soprarAr = soltaArFrio;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ArteGerada.Objeto(arte);
            sr.sortingOrder = 3;
            Altura = sr.sprite.rect.height;
            foreach (var p in ArteGerada.Leds(sr.sprite))
            {
                var l = new GameObject("LED").AddComponent<SpriteRenderer>();
                l.transform.SetParent(transform, false);
                l.transform.localPosition = new Vector3(p.x, p.y, 0);
                l.sprite = PixelArt.Pixel;
                l.sortingOrder = 4;
                leds.Add(l);
            }
        }

        public void DefinirAlerta(bool sim) => alerta = sim;

        void Update()
        {
            if (Time.time >= proximo)
            {
                proximo = Time.time + (alerta ? 0.25f : Random.Range(0.3f, 1.2f));
                bool piscar = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
                foreach (var l in leds) l.color = alerta ? (piscar ? Vermelho : Escuro) : Random.value < 0.85f ? Verde : Escuro;
            }
            // ar frio subindo da grade
            if (soprarAr && Time.time >= proximoAr)
            {
                proximoAr = Time.time + 0.3f;
                var p = new GameObject("Ar").AddComponent<SpriteRenderer>();
                p.transform.SetParent(transform.parent, false);
                p.transform.localPosition = transform.localPosition + new Vector3(Random.Range(-14, 6), Altura - 2, 0);
                p.sprite = PixelArt.Pixel;
                p.color = PixelArt.Hex("a9e4ff");
                p.sortingOrder = 2;
                Flutuante.Aplicar(p.gameObject, 1.2f, 6f);
            }
        }
    }
}
