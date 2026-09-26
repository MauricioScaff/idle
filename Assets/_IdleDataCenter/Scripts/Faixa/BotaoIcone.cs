using System;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>Ícone pequeno clicável (abrir o painel, fechar o jogo). Fica apagado e acende sob o cursor.</summary>
    public class BotaoIcone : MonoBehaviour, IClicavel
    {
        SpriteRenderer sr;
        Action acao;

        public int Ordem => 10;

        public static BotaoIcone Criar(Transform pai, Sprite sprite, Vector2 posicao, Action acao)
        {
            var sr = new GameObject("Botao " + sprite.name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(pai, false);
            sr.transform.localPosition = posicao;
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            var b = sr.gameObject.AddComponent<BotaoIcone>();
            b.sr = sr;
            b.acao = acao;
            // área de clique um pouco maior que o desenho, para não exigir mira de sniper
            var col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size += new Vector2(2, 2);
            b.DefinirDestaque(false);
            return b;
        }

        public void Clicar() => acao?.Invoke();

        // A cor do renderizador multiplica a do sprite: cinza = apagado, branco = cor original acesa
        public void DefinirDestaque(bool ligado) => sr.color = ligado ? Color.white : new Color(0.7f, 0.7f, 0.7f);
    }
}
