using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Texto em fonte de pixel 3×5 com sombra, gerado como sprite. Só regenera quando o texto muda.
    /// Só maiúsculas: minúsculas viram maiúsculas e acentos são removidos (ç → C, ã → A).
    /// </summary>
    public class PixelTexto : MonoBehaviour
    {
        // Cada glifo: 5 linhas de 3 colunas, de cima para baixo.
        internal static readonly Dictionary<char, string> Glifos = new Dictionary<char, string>
        {
            ['A'] = G(".#.", "#.#", "###", "#.#", "#.#"), ['B'] = G("##.", "#.#", "##.", "#.#", "##."),
            ['C'] = G(".##", "#..", "#..", "#..", ".##"), ['D'] = G("##.", "#.#", "#.#", "#.#", "##."),
            ['E'] = G("###", "#..", "##.", "#..", "###"), ['F'] = G("###", "#..", "##.", "#..", "#.."),
            ['G'] = G(".##", "#..", "#.#", "#.#", ".##"), ['H'] = G("#.#", "#.#", "###", "#.#", "#.#"),
            ['I'] = G("###", ".#.", ".#.", ".#.", "###"), ['J'] = G("..#", "..#", "..#", "#.#", ".#."),
            ['K'] = G("#.#", "#.#", "##.", "#.#", "#.#"), ['L'] = G("#..", "#..", "#..", "#..", "###"),
            ['M'] = G("#.#", "###", "###", "#.#", "#.#"), ['N'] = G("##.", "#.#", "#.#", "#.#", "#.#"),
            ['O'] = G(".#.", "#.#", "#.#", "#.#", ".#."), ['P'] = G("##.", "#.#", "##.", "#..", "#.."),
            ['Q'] = G(".#.", "#.#", "#.#", "##.", ".##"), ['R'] = G("##.", "#.#", "##.", "#.#", "#.#"),
            ['S'] = G(".##", "#..", ".#.", "..#", "##."), ['T'] = G("###", ".#.", ".#.", ".#.", ".#."),
            ['U'] = G("#.#", "#.#", "#.#", "#.#", "###"), ['V'] = G("#.#", "#.#", "#.#", "#.#", ".#."),
            ['W'] = G("#.#", "#.#", "###", "###", "#.#"), ['X'] = G("#.#", "#.#", ".#.", "#.#", "#.#"),
            ['Y'] = G("#.#", "#.#", ".#.", ".#.", ".#."), ['Z'] = G("###", "..#", ".#.", "#..", "###"),
            ['0'] = G("###", "#.#", "#.#", "#.#", "###"), ['1'] = G(".#.", "##.", ".#.", ".#.", "###"),
            ['2'] = G("###", "..#", "###", "#..", "###"), ['3'] = G("###", "..#", "###", "..#", "###"),
            ['4'] = G("#.#", "#.#", "###", "..#", "..#"), ['5'] = G("###", "#..", "###", "..#", "###"),
            ['6'] = G("###", "#..", "###", "#.#", "###"), ['7'] = G("###", "..#", ".#.", ".#.", ".#."),
            ['8'] = G("###", "#.#", "###", "#.#", "###"), ['9'] = G("###", "#.#", "###", "..#", "###"),
            ['$'] = G(".##", "##.", ".#.", ".##", "##."), ['+'] = G("...", ".#.", "###", ".#.", "..."),
            ['-'] = G("...", "...", "###", "...", "..."), ['.'] = G("...", "...", "...", "...", ".#."),
            [','] = G("...", "...", "...", ".#.", "#.."), [':'] = G("...", ".#.", "...", ".#.", "..."),
            ['/'] = G("..#", "..#", ".#.", "#..", "#.."), ['!'] = G(".#.", ".#.", ".#.", "...", ".#."),
            ['?'] = G("##.", "..#", ".#.", "...", ".#."), ['%'] = G("#.#", "..#", ".#.", "#..", "#.#"),
            ['x'] = G("...", "#.#", ".#.", "#.#", "..."), [' '] = G("...", "...", "...", "...", "..."),
            ['>'] = G("#..", ".#.", "..#", ".#.", "#.."), ['<'] = G("..#", ".#.", "#..", ".#.", "..#"),
            ['('] = G(".#.", "#..", "#..", "#..", ".#."), [')'] = G(".#.", "..#", "..#", "..#", ".#."),
            ['_'] = G("...", "...", "...", "...", "###"),
        };

        static string G(params string[] linhas) => string.Concat(linhas);

        /// <summary>Largura em pixels que um texto ocupa (3 px por letra + 1 de espaço).</summary>
        public static int Largura(string texto) => Mathf.Max(0, Normalizar(texto).Length * 4 - 1);

        SpriteRenderer frente, sombra;
        Texture2D textura;
        string atual;

        public string Texto => atual;

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
            frente.color = c;
            sombra.color = new Color(0.106f, 0.102f, 0.18f, c.a); // contorno escuro, nunca preto puro
        }

        public void Definir(string texto)
        {
            texto = Normalizar(texto);
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
                    if (gy * 3 + gx < g.Length && g[gy * 3 + gx] == '#')
                        px[(4 - gy) * w + i * 4 + gx] = new Color32(255, 255, 255, 255);
            }
            textura.SetPixels32(px);
            textura.Apply();
            if (frente.sprite != null) Destroy(frente.sprite);
            var sprite = Sprite.Create(textura, new Rect(0, 0, w, h), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            frente.sprite = sprite;
            sombra.sprite = sprite;
        }

        /// <summary>Maiúsculas sem acento; o caractere "×" vira o sinal de multiplicação da fonte.</summary>
        public static string Normalizar(string texto)
        {
            var sb = new StringBuilder(texto.Length);
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c == '×' ? 'x' : char.ToUpperInvariant(c)); // '×' vira o glifo de multiplicação
            }
            return sb.ToString();
        }

        void OnDestroy()
        {
            if (frente != null && frente.sprite != null) Destroy(frente.sprite);
            if (textura != null) Destroy(textura);
        }
    }
}
