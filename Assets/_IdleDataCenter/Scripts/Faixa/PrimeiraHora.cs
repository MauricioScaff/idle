using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>Caneca da faixa (em cima da primeira torre): clicar toma um café e a receita dobra por 30 s.</summary>
    public class CanecaCafe : MonoBehaviour, IClicavel
    {
        Faixa faixa;
        Economia economia;
        SpriteRenderer sr;
        Destaque destaque;

        public int Ordem => 6;

        public static CanecaCafe Criar(SpriteRenderer caneca, Faixa faixa, Economia economia)
        {
            var c = caneca.gameObject.AddComponent<CanecaCafe>();
            c.faixa = faixa;
            c.economia = economia;
            c.sr = caneca;
            var col = caneca.gameObject.AddComponent<BoxCollider2D>();
            col.size += new Vector2(2, 2);   // alvo um pouco maior que o desenho
            c.destaque = Destaque.Para(caneca);
            return c;
        }

        void Update()
        {
            // brilha enquanto o café faz efeito; apagada enquanto recarrega
            bool piscar = Mathf.FloorToInt(Time.time / 0.3f) % 2 == 0;
            sr.color = economia.CafeAtivo ? (piscar ? PixelArt.Hex("ffd65c") : Color.white)
                     : economia.PodeTomarCafe ? Color.white : new Color(0.6f, 0.6f, 0.65f);
        }

        public void Clicar() => faixa.TomarCafe();
        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
    }

    /// <summary>Chamado urgente na faixa: um papel pulando sobre a mesa; clicar atende e rende o bônus.</summary>
    public class ChamadoVisual : MonoBehaviour, IClicavel
    {
        static readonly Sprite Papel = PixelArt.Criar(new[]
        {
            "kkkkkkkk",
            "kyyyyyrk",
            "kyWWWyyk",
            "kyyyyyyk",
            "kyWWWWyk",
            "kyyyyyyk",
            "kyWWWyyk",
            "kyyyyyyk",
            "kkkkkkkk",
        }, new Vector2(0.5f, 0f));

        Faixa faixa;
        Economia economia;
        SpriteRenderer sr;
        BoxCollider2D col;
        Destaque destaque;
        Vector2 base_;

        public int Ordem => 11;

        public static ChamadoVisual Criar(Transform pai, Vector2 posicao, Faixa faixa, Economia economia)
        {
            var go = new GameObject("Chamado");
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicao;
            var c = go.AddComponent<ChamadoVisual>();
            c.faixa = faixa;
            c.economia = economia;
            c.base_ = posicao;
            c.sr = go.AddComponent<SpriteRenderer>();
            c.sr.sprite = Papel;
            c.sr.sortingOrder = 11;
            c.col = go.AddComponent<BoxCollider2D>();
            c.col.size += new Vector2(4, 4);
            c.destaque = Destaque.Para(c.sr);
            return c;
        }

        void Update()
        {
            bool ativo = economia.TemChamado;
            sr.enabled = col.enabled = ativo;
            if (!ativo) { destaque.Ligado = false; return; }
            transform.localPosition = base_ + new Vector2(0, Mathf.Round(Mathf.Abs(Mathf.Sin(Time.time * 4)) * 2));
        }

        public void Clicar() => faixa.AtenderChamado();
        public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado && economia.TemChamado;
    }
}
