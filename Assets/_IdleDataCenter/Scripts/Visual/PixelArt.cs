using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Transforma "mapas de caracteres" em sprites de pixel art. Cada caractere é uma cor
    /// da paleta; '.' é transparente. 1 pixel da arte = 1 unidade de mundo.
    /// Evite preto puro (#000000): na faixa, ele vira transparente.
    /// </summary>
    public static class PixelArt
    {
        public static readonly Dictionary<char, Color32> Paleta = new Dictionary<char, Color32>
        {
            ['k'] = Hex("1b1a2e"), // contorno
            ['#'] = Hex("ffffff"), // branco puro (para pintar com cor)
            ['y'] = Hex("fdf6e3"), // branco quente
            // servidor bege dos anos 2000
            ['b'] = Hex("dccca6"), ['B'] = Hex("b9a67f"), ['d'] = Hex("8c7a58"), ['v'] = Hex("6e6250"),
            // técnico
            ['h'] = Hex("5a3a2e"), ['s'] = Hex("f6cfa9"), ['e'] = Hex("2a2238"), ['x'] = Hex("ff9fae"),
            ['c'] = Hex("5aa9ff"), ['C'] = Hex("3a7ad6"), ['p'] = Hex("3d4273"), ['o'] = Hex("2a2336"),
            // planta, vaso e caneca
            ['l'] = Hex("6fd36f"), ['L'] = Hex("3f9b54"), ['t'] = Hex("e0805a"), ['T'] = Hex("b85c3a"),
            ['m'] = Hex("ff8c7a"), ['M'] = Hex("d9604f"),
            // madeira e LED
            ['n'] = Hex("b07a52"), ['N'] = Hex("7d5238"), ['g'] = Hex("5cff8a"),
            // uniforme de Sysadmin e equipamentos de rack
            ['q'] = Hex("4fc9a8"), ['Q'] = Hex("2e9a80"), ['w'] = Hex("8d92ab"), ['W'] = Hex("5d6178"),
            // efeitos
            ['r'] = Hex("ff5d7a"), ['Y'] = Hex("ffd65c"), ['X'] = Hex("ff7a8a"),
            // quadrinho
            ['S'] = Hex("7cc8ff"), ['G'] = Hex("5cc26a"),
        };

        // Cores do cenário (armário)
        static readonly Color32 Parede = Hex("2d3057"), Parede2 = Hex("33376a"), ParedeLinha = Hex("262949");
        static readonly Color32 Rodape = Hex("1f2140"), Piso = Hex("6b4a4f"), PisoClaro = Hex("82595c"), PisoJunta = Hex("573c41");
        static readonly Color32 CorContorno = Hex("1b1a2e");

        static Sprite pixel;

        /// <summary>Sprite de 1×1 pixel branco (pivô embaixo à esquerda), para LEDs, vapor, etc.</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null) pixel = Criar(new[] { "#" }, Vector2.zero);
                return pixel;
            }
        }

        /// <param name="pivo">(0.5, 0) = centro da base. Use larguras pares para manter o pixel alinhado.</param>
        public static Sprite Criar(string[] linhas, Vector2 pivo)
        {
            int h = linhas.Length, w = linhas[0].Length;
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                string linha = linhas[h - 1 - y]; // a primeira linha do mapa é o topo
                for (int x = 0; x < w; x++)
                    px[y * w + x] = x < linha.Length && Paleta.TryGetValue(linha[x], out var c) ? c : new Color32(0, 0, 0, 0);
            }
            return Finalizar(tex, px, pivo);
        }

        /// <summary>Fundo do armário: parede de tábuas, rodapé, piso de madeira, contorno e um quadrinho.</summary>
        public static Sprite Armario(int w, int h, int alturaPiso)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                if (y < alturaPiso - 1) c = (x + y * 7) % 14 == 0 ? PisoJunta : Piso;
                else if (y == alturaPiso - 1) c = PisoClaro;
                else if (y == alturaPiso) c = Rodape;
                else c = x % 10 == 0 ? ParedeLinha : (x / 10) % 2 == 0 ? Parede : Parede2;
                if (x == 0 || x == w - 1 || y == h - 1) c = CorContorno;
                px[y * w + x] = c;
            }
            Carimbar(px, w, Arte.Quadrinho, 80, 34);
            return Finalizar(tex, px, Vector2.zero);
        }

        /// <summary>Salinha do Sysadmin: parede de azulejo, rodapé e piso elevado de datacenter.</summary>
        public static Sprite Salinha(int w, int h, int alturaPiso)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            Color32 azulejo = Hex("3b4270"), azulejo2 = Hex("414a7c"), rejunte = Hex("343a63");
            Color32 piso = Hex("7c8198"), pisoLinha = Hex("5f6479"), furo = Hex("6b7088");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                if (y < alturaPiso)
                    c = x % 8 == 0 || y == alturaPiso - 1 ? pisoLinha : (x % 2 == 0 && y % 2 == 1) ? furo : piso;
                else if (y == alturaPiso) c = Rodape;
                else if (x % 8 == 0 || (y - alturaPiso) % 8 == 0) c = rejunte;
                else c = ((x / 8) + (y - alturaPiso) / 8) % 2 == 0 ? azulejo : azulejo2;
                if (x == 0 || x == w - 1 || y == h - 1) c = CorContorno;
                px[y * w + x] = c;
            }
            Carimbar(px, w, Arte.Quadrinho, 80, 34);
            return Finalizar(tex, px, Vector2.zero);
        }

        /// <summary>
        /// Sala de racks do Analista de Infra: paredes de painel cinza-azulado, piso técnico elevado
        /// (placas com algumas perfuradas, por onde sobe o ar frio) e uma calha de cabos com fibra laranja no teto.
        /// </summary>
        public static Sprite SalaDeRacks(int w, int h, int alturaPiso, bool escura = false)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            // escura = sala virtualizada do DevOps (era "cloud native": azul escuro com LEDs)
            Color32 parede = Hex(escura ? "232a4f" : "4a5680"), parede2 = Hex(escura ? "1f2648" : "46517a"),
                    junta = Hex(escura ? "1a203f" : "3c4670"), faixa = Hex(escura ? "2e7d8f" : "56638f");
            Color32 placa = Hex("9aa3b8"), placaLinha = Hex("6c7389"), furo = Hex("7a8198");
            Color32 calha = Hex("2e3350"), calhaBorda = Hex("5a6184"), fibra = Hex("ff9f43"), fibra2 = Hex("5cc8ff");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c;
                if (y < alturaPiso)
                {
                    // placas de 12 px; uma em cada três é perfurada
                    bool perfurada = (x / 12) % 3 == 1;
                    c = x % 12 == 0 || y == alturaPiso - 1 ? placaLinha : perfurada && x % 2 == 0 && y % 2 == 1 ? furo : placa;
                }
                else if (y == alturaPiso) c = Rodape;
                else if (y >= h - 15 && y <= h - 11)
                {
                    // calha de cabos logo acima dos racks (o topo da parede fica livre para o HUD)
                    int yy = h - 11 - y;
                    c = yy == 0 || yy == 4 ? calhaBorda : yy == 2 && (x / 3) % 5 != 0 ? fibra : yy == 3 && (x / 5) % 4 == 0 ? fibra2 : calha;
                }
                else if (x % 16 == 0) c = junta;
                else if (y == alturaPiso + 14) c = faixa;
                else c = (x / 16) % 2 == 0 ? parede : parede2;
                if (x == 0 || x == w - 1 || y == h - 1) c = CorContorno;
                px[y * w + x] = c;
            }
            Carimbar(px, w, Arte.Quadrinho, 80, 34);
            return Finalizar(tex, px, Vector2.zero);
        }

        /// <summary>Painel de interface: fundo escuro, borda e uma faixa de título no topo.</summary>
        public static Sprite Painel(int w, int h)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            var fundo = Hex("232548");
            var titulo = Hex("2f3263");
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool borda = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                px[y * w + x] = borda ? CorContorno : y >= h - 10 ? titulo : fundo;
            }
            return Finalizar(tex, px, Vector2.zero);
        }

        /// <summary>Retângulo de cor sólida (fundo de botão, destaque de linha).</summary>
        public static Sprite Retangulo(int w, int h)
        {
            var tex = NovaTextura(w, h);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            return Finalizar(tex, px, Vector2.zero);
        }

        static readonly Dictionary<Sprite, Sprite> contornos = new Dictionary<Sprite, Sprite>();

        /// <summary>
        /// Sprite com 1 pixel de contorno branco em volta da silhueta do original (para destacar
        /// o que é clicável). Mesmo pivô do original, então basta colocar como filho na posição zero.
        /// </summary>
        public static Sprite Contorno(Sprite original)
        {
            if (contornos.TryGetValue(original, out var pronto)) return pronto;
            var fonte = original.texture;
            var r = original.rect;
            int w = (int)r.width, h = (int)r.height, W = w + 2, H = h + 2;
            var origem = fonte.GetPixels32();
            bool Opaco(int x, int y) =>
                x >= 0 && y >= 0 && x < w && y < h && origem[((int)r.y + y) * fonte.width + (int)r.x + x].a > 0;

            var tex = NovaTextura(W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int ox = x - 1, oy = y - 1;
                bool vizinho = Opaco(ox - 1, oy) || Opaco(ox + 1, oy) || Opaco(ox, oy - 1) || Opaco(ox, oy + 1);
                if (!Opaco(ox, oy) && vizinho) px[y * W + x] = new Color32(255, 255, 255, 255);
            }
            var pivo = new Vector2((original.pivot.x + 1) / W, (original.pivot.y + 1) / H);
            return contornos[original] = Finalizar(tex, px, pivo);
        }

        static void Carimbar(Color32[] px, int w, string[] linhas, int x0, int y0)
        {
            int h = linhas.Length;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < linhas[h - 1 - y].Length; x++)
                if (Paleta.TryGetValue(linhas[h - 1 - y][x], out var c))
                    px[(y0 + y) * w + x0 + x] = c;
        }

        static Texture2D NovaTextura(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset,
        };

        static Sprite Finalizar(Texture2D tex, Color32[] px, Vector2 pivo)
        {
            tex.SetPixels32(px);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivo, 1f, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return s;
        }

        public static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
