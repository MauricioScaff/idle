using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Texto em fonte de pixel 3×5 com sombra, gerado como sprite. Só regenera quando o texto muda.
    /// </summary>
    public class PixelTexto : MonoBehaviour
    {
        static readonly Dictionary<char, string> Glifos = new Dictionary<char, string>
        {
            ['0'] = "###" + "#.#" + "#.#" + "#.#" + "###",
            ['1'] = ".#." + "##." + ".#." + ".#." + "###",
            ['2'] = "###" + "..#" + "###" + "#.." + "###",
            ['3'] = "###" + "..#" + "###" + "..#" + "###",
            ['4'] = "#.#" + "#.#" + "###" + "..#" + "..#",
            ['5'] = "###" + "#.." + "###" + "..#" + "###",
            ['6'] = "###" + "#.." + "###" + "#.#" + "###",
            ['7'] = "###" + "..#" + ".#." + ".#." + ".#.",
            ['8'] = "###" + "#.#" + "###" + "#.#" + "###",
            ['9'] = "###" + "#.#" + "###" + "..#" + "###",
            ['R'] = "##." + "#.#" + "##." + "#.#" + "#.#",
            ['$'] = ".##" + "##." + ".#." + ".##" + "##.",
            ['+'] = "..." + ".#." + "###" + ".#." + "...",
            ['.'] = "..." + "..." + "..." + "..." + ".#.",
            ['K'] = "#.#" + "#.#" + "##." + "#.#" + "#.#",
            ['M'] = "#.#" + "###" + "#.#" + "#.#" + "#.#",
            ['B'] = "##." + "#.#" + "##." + "#.#" + "##.",
            ['T'] = "###" + ".#." + ".#." + ".#." + ".#.",
            [' '] = "..." + "..." + "..." + "..." + "...",
        };

        SpriteRenderer frente, sombra;
        Texture2D textura;
        string atual;
        Color cor;

        public static PixelTexto Criar(Transform pai, Vector2 posicaoLocal, Color cor, int ordem)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicaoLocal;
            var t = go.AddComponent<PixelTexto>();
            t.frente = go.AddComponent<SpriteRenderer>();
            t.frente.sortingOrder = ordem;
            var s = new GameObject("Sombra");
            s.transform.SetParent(go.transform, false);
            s.transform.localPosition = new Vector3(1f, -1f, 0f);
            t.sombra = s.AddComponent<SpriteRenderer>();
            t.sombra.sortingOrder = ordem - 1;
            t.DefinirCor(cor);
            return t;
        }

        public void DefinirCor(Color c)
        {
            cor = c;
            frente.color = c;
            sombra.color = new Color(0.106f, 0.102f, 0.18f, c.a); // contorno escuro, nunca preto puro
        }

        public void Definir(string texto)
        {
            if (texto == atual) return;
            atual = texto;
            int w = Mathf.Max(1, texto.Length * 4 - 1), h = 5;
            if (textura == null || textura.width != w)
            {
                if (textura != null) Destroy(textura);
                textura = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            }
            var px = new Color32[w * h];
            for (int i = 0; i < texto.Length; i++)
            {
                if (!Glifos.TryGetValue(texto[i], out var g)) continue;
                for (int gy = 0; gy < 5; gy++)
                for (int gx = 0; gx < 3; gx++)
                    if (g[gy * 3 + gx] == '#')
                        px[(4 - gy) * w + i * 4 + gx] = new Color32(255, 255, 255, 255);
            }
            textura.SetPixels32(px);
            textura.Apply();
            if (frente.sprite != null) Destroy(frente.sprite);
            var sprite = Sprite.Create(textura, new Rect(0, 0, w, h), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            frente.sprite = sprite;
            sombra.sprite = sprite;
        }

        void OnDestroy()
        {
            if (frente != null && frente.sprite != null) Destroy(frente.sprite);
            if (textura != null) Destroy(textura);
        }
    }
}
