using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>Interface desenhada no OnGUI (modo gerente e faixa): caixas, barras, botões e texto na fonte do jogo.</summary>
    public sealed class IsoGui : IDisposable
    {
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

        // ---------------- Texto ----------------
        // Letra: Silkscreen (licença OFL, Resources/Fontes; OFL-Silkscreen.txt). A "escala" antiga (altura da maiúscula = 5 × escala)
        // vira um tamanho de fonte com maiúsculas da mesma altura, então os layouts continuam valendo.
        // O texto é desenhado já no tamanho da tela (fora da matriz do GUI), para a fonte ser rasterizada nítida.

        const float TamanhoPorEscala = 7f;          // tamanho da fonte por unidade de escala
        const float TopoDaMaiuscula = 0.30f;        // do topo da linha até o topo da maiúscula, em fração do tamanho
        const float PixelDaLetra = 0.14f;           // na Silkscreen, um "pixel" da letra é 14% do tamanho (maiúscula de 5 pixels = 0,7)

        static Font fonte;
        static GUIStyle estilo;

        static GUIStyle Estilo
        {
            get
            {
                if (estilo != null) return estilo;
                fonte = Resources.Load<Font>("Fontes/Silkscreen-Regular");
                estilo = new GUIStyle { font = fonte, alignment = TextAnchor.UpperLeft, wordWrap = false, clipping = TextClipping.Overflow, richText = false };
                estilo.padding = new RectOffset(0, 0, 0, 0);
                estilo.margin = new RectOffset(0, 0, 0, 0);
                return estilo;
            }
        }

        static int Tamanho(float escala) => Mathf.Max(6, Mathf.RoundToInt(escala * TamanhoPorEscala));

        /// <summary>Largura do texto em unidades do GUI (as mesmas das coordenadas de quem chama).</summary>
        public float Largura(string s, int escala = 2)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = Idiomas.T(s);
            var e = Estilo;
            e.fontSize = Tamanho(escala);
            return e.CalcSize(new GUIContent(s)).x;
        }

        public void Texto(string s, float x, float y, Color? c = null, int escala = 2, bool centro = false)
        {
            if (string.IsNullOrEmpty(s) || Event.current.type != EventType.Repaint) return;
            s = Idiomas.T(s);   // o jogo escreve em português; aqui vira inglês se for o idioma escolhido
            var e = Estilo;
            // tamanho e posição na tela de verdade
            var m = GUI.matrix;
            float s1 = m.m00;
            // tamanho que põe cada pixel da letra num número inteiro de pixels da tela: a letra fica nítida
            int pixel = Mathf.Max(1, Mathf.RoundToInt(Tamanho(escala) * s1 * PixelDaLetra));
            int tamanho = Mathf.RoundToInt(pixel / PixelDaLetra);
            e.fontSize = tamanho;
            var conteudo = new GUIContent(s);
            var tam = e.CalcSize(conteudo);
            Vector3 p = m.MultiplyPoint3x4(new Vector3(x, y, 0));
            float px = Mathf.Round(centro ? p.x - tam.x / 2 : p.x), py = Mathf.Round(p.y - tamanho * TopoDaMaiuscula);
            GUI.matrix = Matrix4x4.identity;
            float sombra = Mathf.Max(1, Mathf.Round(tamanho / 14f));
            e.normal.textColor = new Color(0.035f, 0.075f, 0.13f, (c ?? Branco).a);
            GUI.Label(new Rect(px + sombra, py + sombra, tam.x + 2, tam.y), conteudo, e);
            e.normal.textColor = c ?? Branco;
            GUI.Label(new Rect(px, py, tam.x + 2, tam.y), conteudo, e);
            GUI.matrix = m;
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
            if (clicked) Sons.Tique();
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
        }
    }
}
