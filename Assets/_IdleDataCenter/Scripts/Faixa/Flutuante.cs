using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>Faz um objeto subir alguns pixels, sumir aos poucos e se destruir (textos "+R$", corações, faíscas).</summary>
    public class Flutuante : MonoBehaviour
    {
        float duracao, subida, tempo;
        Vector3 origem;
        SpriteRenderer[] renderizadores;
        Color[] coresOriginais;

        public static void Aplicar(GameObject go, float duracao, float subida)
        {
            var f = go.AddComponent<Flutuante>();
            f.duracao = duracao;
            f.subida = subida;
            f.origem = go.transform.localPosition;
            f.renderizadores = go.GetComponentsInChildren<SpriteRenderer>();
            f.coresOriginais = new Color[f.renderizadores.Length];
            for (int i = 0; i < f.renderizadores.Length; i++) f.coresOriginais[i] = f.renderizadores[i].color;
        }

        void Update()
        {
            tempo += Time.deltaTime;
            float k = tempo / duracao;
            if (k >= 1f) { Destroy(gameObject); return; }
            // sobe em passos de 1 pixel inteiro, para não borrar a pixel art
            transform.localPosition = origem + Vector3.up * Mathf.Round(Mathf.Sin(k * Mathf.PI * 0.5f) * subida);
            float alfa = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            for (int i = 0; i < renderizadores.Length; i++)
            {
                var c = coresOriginais[i];
                c.a *= alfa;
                renderizadores[i].color = c;
            }
        }
    }
}
