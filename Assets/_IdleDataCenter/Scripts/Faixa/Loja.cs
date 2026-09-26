using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Painel de melhorias ao lado do cenário: uma linha por melhoria do cargo atual, com nome, nível e custo.
    /// A faixa de título mostra "MELHORIAS", o efeito da linha sob o cursor, ou um aviso temporário.
    /// </summary>
    public class Loja : MonoBehaviour
    {
        public const int Largura = 112, Altura = 60;

        static readonly Color CorTitulo = PixelArt.Hex("a9c7ff");

        Economia economia;
        readonly List<BotaoMelhoria> botoes = new List<BotaoMelhoria>();
        PixelTexto titulo;
        BotaoMelhoria sobCursor;
        string aviso;
        Color corAviso;
        float fimAviso;

        public void Iniciar(Economia economia, Vector2 posicao)
        {
            this.economia = economia;
            transform.position = posicao;
            var fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Painel(Largura, Altura);
            gameObject.AddComponent<BoxCollider2D>(); // o painel inteiro "segura" o clique
            titulo = PixelTexto.Criar(transform, new Vector2(4, Altura - 7), CorTitulo, 5);
            Reconstruir();
        }

        /// <summary>Recria as linhas para as melhorias do cargo atual (chamar após promoção).</summary>
        public void Reconstruir()
        {
            foreach (var b in botoes) Destroy(b.gameObject);
            botoes.Clear();
            sobCursor = null;
            var lista = economia.MelhoriasDoCargo().ToList();
            int passo = lista.Count <= 3 ? 16 : 12;       // 4 linhas ficam mais juntas
            for (int i = 0; i < lista.Count; i++)
            {
                var b = new GameObject("Botao " + lista[i].Id).AddComponent<BotaoMelhoria>();
                b.Iniciar(this, lista[i], transform, new Vector2(2, Altura - 10 - passo * (i + 1)), Largura - 4, passo - 1);
                botoes.Add(b);
            }
        }

        public void MostrarAviso(string texto, float segundos, Color? cor = null)
        {
            aviso = texto;
            corAviso = cor ?? PixelArt.Hex("ffd65c");
            fimAviso = Time.time + segundos;
        }

        public void CursorSobre(BotaoMelhoria botao, bool sobre)
        {
            if (sobre) sobCursor = botao;
            else if (sobCursor == botao) sobCursor = null;
        }

        public void TentarComprar(MelhoriaDef def)
        {
            if (economia.NoMaximo(def.Id)) return;
            if (!economia.RequisitoOk(def.Id))
                MostrarAviso("Precisa: " + Catalogo.Buscar(def.Requisito).Nome, 1.5f);
            else if (!economia.Comprar(def.Id))
                MostrarAviso("Falta R$ " + Faixa.Formatar(economia.Custo(def.Id) - economia.Dinheiro), 1.5f);
        }

        void Update()
        {
            bool avisando = Time.time < fimAviso;
            string texto = avisando ? aviso
                         : sobCursor == null ? "Melhorias"
                         : !economia.RequisitoOk(sobCursor.Definicao.Id) ? "Precisa: " + Catalogo.Buscar(sobCursor.Definicao.Requisito).Nome
                         : sobCursor.Definicao.Efeito;
            titulo.Definir(texto);
            titulo.DefinirCor(avisando ? corAviso : CorTitulo);

            foreach (var b in botoes)
            {
                var def = b.Definicao;
                int nivel = economia.Nivel(def.Id);
                string nome = def.NivelMaximo > 1 ? $"{def.Nome} {nivel}/{def.NivelMaximo}" : def.Nome;
                if (economia.NoMaximo(def.Id)) b.Mostrar(nome, "OK", BotaoMelhoria.Situacao.Completo);
                else b.Mostrar(nome, "R$ " + Faixa.Formatar(economia.Custo(def.Id)),
                    economia.PodeComprar(def.Id) ? BotaoMelhoria.Situacao.Disponivel : BotaoMelhoria.Situacao.SemDinheiro);
            }
        }
    }

    /// <summary>Uma linha clicável da loja.</summary>
    public class BotaoMelhoria : MonoBehaviour, IClicavel
    {
        public enum Situacao { Disponivel, SemDinheiro, Completo }

        static readonly Color FundoDisponivel = PixelArt.Hex("3b3f7a"), FundoDestaque = PixelArt.Hex("5258a8"),
                              FundoApagado = PixelArt.Hex("2a2d58");
        static readonly Color TextoDisponivel = PixelArt.Hex("fdf6e3"), TextoApagado = PixelArt.Hex("7d82ad"),
                              TextoCompleto = PixelArt.Hex("6fd36f"), Preco = PixelArt.Hex("ffd65c");

        Loja loja;
        SpriteRenderer fundo;
        PixelTexto nome, custo;
        int largura, margemTexto;
        bool destacado;

        public MelhoriaDef Definicao { get; private set; }
        public int Ordem => 4;

        public void Iniciar(Loja loja, MelhoriaDef def, Transform pai, Vector2 posicao, int largura, int altura)
        {
            this.loja = loja;
            this.largura = largura;
            Definicao = def;
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Retangulo(largura, altura);
            fundo.sortingOrder = 1;
            gameObject.AddComponent<BoxCollider2D>();
            margemTexto = (altura - 5) / 2;
            nome = PixelTexto.Criar(transform, new Vector2(3, margemTexto), TextoDisponivel, 3);
            custo = PixelTexto.Criar(transform, new Vector2(0, margemTexto), Preco, 3);
        }

        public void Mostrar(string textoNome, string textoCusto, Situacao s)
        {
            nome.Definir(textoNome);
            custo.Definir(textoCusto);
            custo.transform.localPosition = new Vector3(largura - 3 - PixelTexto.Largura(textoCusto), margemTexto, 0);
            nome.DefinirCor(s == Situacao.Disponivel ? TextoDisponivel : s == Situacao.Completo ? TextoCompleto : TextoApagado);
            custo.DefinirCor(s == Situacao.Disponivel ? Preco : s == Situacao.Completo ? TextoCompleto : TextoApagado);
            fundo.color = s != Situacao.Disponivel ? FundoApagado : destacado ? FundoDestaque : FundoDisponivel;
        }

        public void Clicar() => loja.TentarComprar(Definicao);

        public void DefinirDestaque(bool ligado)
        {
            destacado = ligado;
            loja.CursorSobre(this, ligado);
        }
    }
}
