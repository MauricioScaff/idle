using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Servidor torre (arte do PixelLab). Os LEDs da própria imagem piscam por cima; o SSD deixa
    /// a atividade azul e agitada; as ventoinhas aparecem na lateral; travado, tudo fica vermelho
    /// e um alerta flutua em cima até alguém reiniciar.
    /// </summary>
    public class ServidorVelho : MonoBehaviour, IClicavel
    {
        static readonly Color Verde = PixelArt.Hex("5cff8a"), Ambar = PixelArt.Hex("ffbf3f"),
                              Azul = PixelArt.Hex("5cc8ff"), Apagado = PixelArt.Hex("3a3428"), Vermelho = PixelArt.Hex("ff3b4e");

        // Ventoinhas na lateral do gabinete (a partir do pé, relativo ao centro)
        static readonly Vector2[] PosicoesVentoinhas = { new Vector2(-9, 7), new Vector2(-9, 12), new Vector2(-9, 17) };

        Faixa faixa;
        readonly List<SpriteRenderer> leds = new List<SpriteRenderer>();
        SpriteRenderer alerta;
        SpriteRenderer[] ventoinhas;
        Destaque destaque;
        Vector3 posicaoBase;
        float altura, proximoPisca, pulo;
        bool ssd, travado;

        public int Indice { get; private set; }
        public int Ordem => 3;
        public float X => posicaoBase.x;
        public Vector2 Topo => posicaoBase + new Vector3(0f, altura, 0f);

        public void Iniciar(Faixa faixa, Transform pai, Vector2 posicaoLocal, int indice)
        {
            this.faixa = faixa;
            Indice = indice;
            transform.SetParent(pai, false);
            transform.localPosition = posicaoLocal;
            posicaoBase = transform.localPosition;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ArteGerada.Objeto("torre");
            sr.sortingOrder = Ordem;
            altura = sr.sprite.rect.height;
            gameObject.AddComponent<BoxCollider2D>();
            destaque = Destaque.Para(sr);

            foreach (var p in ArteGerada.Leds(sr.sprite)) leds.Add(Filho("LED", PixelArt.Pixel, p, Ordem + 1));

            ventoinhas = new SpriteRenderer[PosicoesVentoinhas.Length];
            for (int i = 0; i < ventoinhas.Length; i++)
            {
                ventoinhas[i] = Filho("Ventoinha", Arte.VentoinhaA, PosicoesVentoinhas[i], Ordem + 1);
                ventoinhas[i].enabled = false;
            }

            alerta = Filho("Alerta", Arte.Alerta, new Vector2(-2f, altura + 7), 12); // acima da caneca
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
        }

        void Update()
        {
            if (travado)
            {
                bool aceso = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
                foreach (var l in leds) l.color = aceso ? Vermelho : Apagado;
                alerta.transform.localPosition = new Vector3(-2f, altura + 7 + (aceso ? 1f : 0f), 0f);
            }
            else if (Time.time >= proximoPisca)
            {
                // o primeiro LED é o de energia (sempre verde); os outros piscam com a atividade de disco
                Color atividade = ssd ? Azul : Ambar;
                for (int i = 0; i < leds.Count; i++)
                    leds[i].color = i == 0 ? Verde : Random.value < (ssd ? 0.7f : 0.45f) ? atividade : Apagado;
                proximoPisca = Time.time + (ssd ? Random.Range(0.03f, 0.1f) : Random.Range(0.06f, 0.4f));
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
