using System;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>Texto em fonte de pixel que funciona como botão (a área de clique acompanha o tamanho do texto).</summary>
    public class BotaoTexto : MonoBehaviour, IClicavel
    {
        PixelTexto texto;
        BoxCollider2D colisor;
        Action acao;
        Color cor;
        bool destacado;

        public int Ordem => 10;

        public static BotaoTexto Criar(Transform pai, Vector2 posicao, Color cor, Action acao)
        {
            var t = PixelTexto.Criar(pai, posicao, cor, 10);
            var b = t.gameObject.AddComponent<BotaoTexto>();
            b.texto = t;
            b.cor = cor;
            b.acao = acao;
            b.colisor = t.gameObject.AddComponent<BoxCollider2D>();
            return b;
        }

        public void Definir(string s, Color c)
        {
            cor = c;
            texto.Definir(s);
            int w = PixelTexto.Largura(s);
            colisor.size = new Vector2(w + 2, 7);
            colisor.offset = new Vector2(w / 2f, 2.5f);
            texto.DefinirCor(destacado ? Color.white : cor);
        }

        public float Largura => PixelTexto.Largura(texto.Texto ?? "");

        public void Clicar() => acao?.Invoke();

        public void DefinirDestaque(bool ligado)
        {
            destacado = ligado;
            texto.DefinirCor(ligado ? Color.white : cor);
        }
    }
}
