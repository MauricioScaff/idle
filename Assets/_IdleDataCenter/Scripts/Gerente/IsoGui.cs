using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>UI em pixels inteiros e fonte bitmap compartilhada com o jogo original.</summary>
    public sealed class IsoGui : IDisposable
    {
        readonly Dictionary<string, Texture2D> textos = new Dictionary<string, Texture2D>();
        readonly Queue<string> ordem = new Queue<string>();
        public static readonly Color Fundo = Cor("101c30"), Painel = Cor("172941"), Borda = Cor("335775"),
            Branco = Cor("e5f5ff"), Muted = Cor("8babc5"), Cyan = Cor("41d7f5"), Verde = Cor("5fe29b"),
            Laranja = Cor("ffb458"), Roxo = Cor("b798ff");

        public static Color Cor(string hex) => PixelArt.Hex(hex);

        public void Ret(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public void Caixa(Rect r, Color? fill = null, Color? border = null)
        {
            Ret(new Rect(r.x + 3, r.y + 4, r.width, r.height), Cor("091221"));
            Ret(r, border ?? Borda);
            Ret(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), fill ?? Painel);
            Ret(new Rect(r.x + 2, r.y + 2, r.width - 4, 2), new Color(1, 1, 1, .06f));
        }

        Texture2D Fonte(string valor)
        {
            valor = PixelTexto.Normalizar(valor);
            if (textos.TryGetValue(valor, out var t)) return t;
            if (textos.Count >= 384)
            {
                string antigo = ordem.Dequeue();
                UnityEngine.Object.Destroy(textos[antigo]);
                textos.Remove(antigo);
            }
            var canvas = new PixelCanvas(Mathf.Max(1, PixelCanvas.LarguraTexto(valor)), 5);
            canvas.Texto(valor, 0, 0, Color.white, false);
            canvas.Aplicar();
            UnityEngine.Object.Destroy(canvas.Sprite);
            textos[valor] = canvas.Textura;
            ordem.Enqueue(valor);
            return canvas.Textura;
        }

        public void Texto(string s, float x, float y, Color? c = null, int escala = 2, bool centro = false)
        {
            if (string.IsNullOrEmpty(s)) return;
            var t = Fonte(s);
            float w = t.width * escala;
            var r = new Rect(Mathf.Round(centro ? x - w / 2 : x), Mathf.Round(y), w, 5 * escala);
            GUI.color = Cor("091321");
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 2, r.width, r.height), t);
            GUI.color = c ?? Branco;
            GUI.DrawTexture(r, t);
            GUI.color = Color.white;
        }

        public bool Botao(Rect r, string titulo, Color accent, bool enabled = true, int escala = 2)
        {
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            Caixa(r, enabled ? (hover ? Color.Lerp(accent, Painel, .45f) : Color.Lerp(accent, Painel, .75f)) : Cor("1c2839"),
                enabled ? accent : Borda);
            Ret(new Rect(r.x + 2, r.yMax - 5, r.width - 4, 3), new Color(0, 0, 0, .22f));
            Texto(titulo, r.center.x, r.center.y - escala * 2.5f, enabled ? Branco : Muted, escala, true);
            bool old = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = old;
            return clicked;
        }

        public void Barra(Rect r, double proporcao, Color c)
        {
            Ret(r, Borda);
            Ret(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Fundo);
            float w = (r.width - 6) * Mathf.Clamp01((float)proporcao);
            Ret(new Rect(r.x + 3, r.y + 3, w, r.height - 6), c);
            Ret(new Rect(r.x + 3, r.y + 3, w, 2), Color.Lerp(c, Color.white, .35f));
        }

        public void Icone(int tipo, float x, float y, Color c, int escala = 2)
        {
            // Símbolos construídos em pixels: rack, gráfico, energia, rede e pesquisa.
            string[] linhas;
            switch (tipo % 6)
            {
                case 0: linhas = new[] { "######", "#....#", "#.#.##", "######", "#....#", "#.#.##", "######" }; break;
                case 1: linhas = new[] { ".....#", "...#.#", "...#.#", ".#.#.#", ".#.#.#", ".#.#.#", "######" }; break;
                case 2: linhas = new[] { "...##.", "..##..", ".##...", "#####.", "..##..", ".##...", ".#...." }; break;
                case 3: linhas = new[] { "..##..", "..##..", "..##..", "######", "#....#", "##..##", "##..##" }; break;
                case 4: linhas = new[] { ".####.", "..##..", "..##..", ".#..#.", "#....#", "######", ".####." }; break;
                default: linhas = new[] { "..##..", ".####.", "######", "#.##.#", "######", ".####.", "..##.." }; break;
            }
            for (int j = 0; j < linhas.Length; j++)
                for (int i = 0; i < linhas[j].Length; i++)
                    if (linhas[j][i] == '#') Ret(new Rect(x + i * escala, y + j * escala, escala, escala), c);
        }

        public void Dispose()
        {
            foreach (var t in textos.Values) UnityEngine.Object.Destroy(t);
            textos.Clear(); ordem.Clear();
        }
    }
}
