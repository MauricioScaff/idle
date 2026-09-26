using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>Contorno de 1 pixel piscando em volta de um sprite, para mostrar que ele é clicável.</summary>
    public class Destaque : MonoBehaviour
    {
        static readonly Color Amarelo = PixelArt.Hex("ffd65c");

        SpriteRenderer alvo, contorno;

        public static Destaque Para(SpriteRenderer alvo)
        {
            var go = new GameObject("Contorno");
            go.transform.SetParent(alvo.transform, false);
            var d = go.AddComponent<Destaque>();
            d.alvo = alvo;
            d.contorno = go.AddComponent<SpriteRenderer>();
            d.contorno.sortingOrder = alvo.sortingOrder + 1;
            d.contorno.enabled = false;
            return d;
        }

        public bool Ligado
        {
            get => contorno.enabled;
            set => contorno.enabled = value;
        }

        void LateUpdate()
        {
            if (!contorno.enabled) return;
            contorno.sprite = PixelArt.Contorno(alvo.sprite); // acompanha os quadros de animação
            contorno.flipX = alvo.flipX;
            contorno.color = Mathf.FloorToInt(Time.time / 0.3f) % 2 == 0 ? Amarelo : Color.white;
        }
    }
}
