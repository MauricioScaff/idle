using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Tela verde de terminal do monitor CRT (8×5 pixels). Quando o técnico digita, as linhas
    /// vão crescendo e rolando; parado, só pisca o cursor.
    /// </summary>
    public class TelaTerminal : MonoBehaviour
    {
        static readonly Color32 Letra = PixelArt.Hex("5cff8a"), LetraFraca = PixelArt.Hex("2f8a4f");

        public bool Digitando { get; set; }

        int W, H;
        Color32 Fundo;
        Texture2D textura;
        int[] linhas;   // comprimento de cada linha (a de baixo é a atual)
        int alvoLinha;
        float proximo;

        /// <summary>Tela de w×h pixels na posição (canto de baixo à esquerda) dentro do objeto pai.</summary>
        public static TelaTerminal Criar(Transform pai, Vector2 posicao, int w, int h, Color32 fundo, int ordem)
        {
            var go = new GameObject("Tela");
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posicao;
            var t = go.AddComponent<TelaTerminal>();
            t.W = w; t.H = h; t.Fundo = fundo;
            t.linhas = new int[h];
            t.textura = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(t.textura, new Rect(0, 0, w, h), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = ordem;
            for (int i = 0; i < h - 1; i++) t.linhas[i] = Random.Range(2, w);
            t.alvoLinha = Random.Range(3, w + 1);
            t.Desenhar(true);
            return t;
        }

        void Update()
        {
            if (Time.time < proximo) return;
            proximo = Time.time + (Digitando ? 0.12f : 0.5f);
            if (Digitando)
            {
                int atual = H - 1;
                if (linhas[atual] < alvoLinha) linhas[atual]++;
                else
                {
                    // Enter: tudo sobe uma linha
                    for (int i = 0; i < H - 1; i++) linhas[i] = linhas[i + 1];
                    linhas[atual] = 0;
                    alvoLinha = Random.Range(3, W + 1);
                }
            }
            Desenhar(Mathf.FloorToInt(Time.time / 0.5f) % 2 == 0);
        }

        void Desenhar(bool cursorAceso)
        {
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Fundo;
            for (int i = 0; i < H; i++)
            {
                int y = H - 1 - i; // linha 0 é a de cima
                for (int x = 0; x < linhas[i] && x < W; x++) px[y * W + x] = i == H - 1 ? Letra : LetraFraca;
            }
            int cx = Mathf.Min(linhas[H - 1], W - 1);
            if (cursorAceso) px[cx] = Letra; // cursor na linha de baixo
            textura.SetPixels32(px);
            textura.Apply();
        }
    }

    /// <summary>Fumacinha saindo da caneca: pixels que sobem e somem, alternando de lado.</summary>
    public class Vapor : MonoBehaviour
    {
        float proximo;
        int lado;

        void Update()
        {
            if (Time.time < proximo) return;
            proximo = Time.time + 0.6f;
            lado = 1 - lado;
            var p = new GameObject("Vapor").AddComponent<SpriteRenderer>();
            p.transform.SetParent(transform.parent, false);
            p.transform.localPosition = transform.localPosition + new Vector3(-2 + lado * 2, 6f, 0f);
            p.sprite = PixelArt.Pixel;
            p.color = new Color(1f, 1f, 1f, 0.8f);
            p.sortingOrder = 4;
            Flutuante.Aplicar(p.gameObject, 1.2f, 4f);
        }
    }

    /// <summary>Relógio de parede: ponteiro dos segundos dá a volta em 8 passos, um por segundo.</summary>
    public class RelogioParede : MonoBehaviour
    {
        static readonly Vector2Int[] Passos =
        {
            new Vector2Int(3, 5), new Vector2Int(5, 5), new Vector2Int(5, 3), new Vector2Int(5, 1),
            new Vector2Int(3, 1), new Vector2Int(1, 1), new Vector2Int(1, 3), new Vector2Int(1, 5),
        };

        Transform ponteiro;

        public static void Criar(SpriteRenderer mostrador)
        {
            var r = mostrador.gameObject.AddComponent<RelogioParede>();
            var cor = PixelArt.Hex("1b1a2e");
            Pixel(mostrador, "Centro", new Vector2(3, 3), cor);
            Pixel(mostrador, "Hora", new Vector2(3, 4), cor);
            r.ponteiro = Pixel(mostrador, "Ponteiro", new Vector2(3, 5), PixelArt.Hex("ff5d7a")).transform;
        }

        static SpriteRenderer Pixel(SpriteRenderer pai, string nome, Vector2 pos, Color cor)
        {
            var sr = new GameObject(nome).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(pai.transform, false);
            sr.transform.localPosition = pos;
            sr.sprite = PixelArt.Pixel;
            sr.color = cor;
            sr.sortingOrder = pai.sortingOrder + 1;
            return sr;
        }

        void Update()
        {
            var p = Passos[Mathf.FloorToInt(Time.time) % Passos.Length];
            ponteiro.localPosition = new Vector3(p.x, p.y, 0);
        }
    }
}
