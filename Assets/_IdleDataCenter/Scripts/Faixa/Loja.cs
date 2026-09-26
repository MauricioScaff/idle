using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Painel de melhorias ao lado do armário: uma linha por melhoria com nome, nível e custo.
    /// A faixa de título mostra "MELHORIAS", o efeito da linha sob o cursor, ou um aviso temporário.
    /// </summary>
    public class Loja : MonoBehaviour
    {
        public const int Largura = 112, Altura = 44;

        static readonly Color CorTitulo = PixelArt.Hex("a9c7ff"), CorAviso = PixelArt.Hex("ffd65c");

        Economia economia;
        BotaoMelhoria[] botoes;
        PixelTexto titulo;
        BotaoMelhoria sobCursor;
        string aviso;
        float fimAviso;

        public void Iniciar(Economia economia, Vector2 posicao)
        {
            this.economia = economia;
            transform.position = posicao;
            var fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Painel(Largura, Altura);
            gameObject.AddComponent<BoxCollider2D>(); // o painel inteiro "segura" o clique

            titulo = PixelTexto.Criar(transform, new Vector2(4, 37), CorTitulo, 5);

            var melhorias = Catalogo.Melhorias;
            botoes = new BotaoMelhoria[melhorias.Count];
            for (int i = 0; i < melhorias.Count; i++)
            {
                var b = new GameObject("Botao " + melhorias[i].Id).AddComponent<BotaoMelhoria>();
                b.Iniciar(this, melhorias[i], transform, new Vector2(2, 25 - i * 10), Largura - 4);
                botoes[i] = b;
            }
        }

        public void MostrarAviso(string texto, float segundos)
        {
            aviso = texto;
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
            if (!economia.Comprar(def.Id))
                MostrarAviso("Falta R$ " + Faixa.Formatar(economia.Custo(def.Id) - economia.Dinheiro), 1.5f);
        }

        void Update()
        {
            string texto = Time.time < fimAviso ? aviso
                         : sobCursor != null ? sobCursor.Definicao.Efeito
                         : "Melhorias";
            titulo.Definir(texto);
            titulo.DefinirCor(Time.time < fimAviso ? CorAviso : CorTitulo);

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
        int largura;
        bool destacado;
        Situacao situacao;

        public MelhoriaDef Definicao { get; private set; }
        public int Ordem => 4;

        public void Iniciar(Loja loja, MelhoriaDef def, Transform pai, Vector2 posicao, int largura)
        {
            this.loja = loja;
            this.largura = largura;
            Definicao = def;
            transform.SetParent(pai, false);
            transform.localPosition = posicao;
            fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = PixelArt.Retangulo(largura, 9);
            fundo.sortingOrder = 1;
            gameObject.AddComponent<BoxCollider2D>();
            nome = PixelTexto.Criar(transform, new Vector2(3, 2), TextoDisponivel, 3);
            custo = PixelTexto.Criar(transform, new Vector2(0, 2), Preco, 3);
        }

        public void Mostrar(string textoNome, string textoCusto, Situacao s)
        {
            situacao = s;
            nome.Definir(textoNome);
            custo.Definir(textoCusto);
            custo.transform.localPosition = new Vector3(largura - 3 - PixelTexto.Largura(textoCusto), 2, 0);
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
