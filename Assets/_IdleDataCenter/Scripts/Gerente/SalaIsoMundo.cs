using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// CTO: o mapa-múndi à noite (PixelLab Pro), como no telão de um NOC. Continentes com luzes de cidade, a sede na América do Sul,
    /// as regiões abertas brilhando, cabos submarinos com pacotes atravessando o oceano, cataventos da energia
    /// renovável e o brilho roxo dos clusters de GPU. Pane regional deixa a região vermelha piscando.
    /// </summary>
    public partial class SalaIso
    {
        const int MapaL = 460, MapaA = 300;
        PixelCanvas telaMundo;

        /// <summary>Onde fica cada região (0 = sede, na América do Sul).</summary>
        public static readonly Vector2[] PosicaoRegiao = { new Vector2(.32f, .71f), new Vector2(.24f, .31f), new Vector2(.50f, .22f), new Vector2(.83f, .32f) };

        /// <summary>O mapa à noite desenhado no PixelLab Pro (Resources/Arte/Salas/mundo.png), carregado uma vez.</summary>
        Color32[] mapaPixelLab;

        Vector2Int M(Vector2 f) => new Vector2Int(Mathf.RoundToInt(f.x * MapaL), Mathf.RoundToInt(f.y * MapaA));

        void DesenharMundo()
        {
            if (telaMundo == null) telaMundo = new PixelCanvas(MapaL, MapaA);
            tela = telaMundo;
            d = new IsoDesenho(tela);   // só as primitivas (polígono, linha)
            if (mapaPixelLab == null)
            {
                var tex = ArteGerada.Textura("Salas/mundo");
                mapaPixelLab = tex != null ? ArteGerada.PixelsDeCimaParaBaixo(tex) : new Color32[MapaL * MapaA];
            }
            tela.Imagem(mapaPixelLab, MapaL, MapaA, 0, 0);

            // cabos submarinos: da sede para cada região ligada, em arco, com pacotes
            int ligadas = E.RegioesLigadas;
            for (int r = 1; r <= ligadas && r <= E.RegioesExtras; r++) Cabo(0, r);

            // regiões
            for (int r = 0; r < PosicaoRegiao.Length; r++) Regiao(r);

            // cataventos (energia renovável) perto da sede, um por nível
            for (int i = 0; i < E.Nivel(Catalogo.Renovavel); i++) Catavento(M(PosicaoRegiao[0]) + new Vector2Int(-22 + i * 11, 24));

            tela.Aplicar();
        }

        void Cabo(int de, int para)
        {
            var a = (Vector2)M(PosicaoRegiao[de]); var b = (Vector2)M(PosicaoRegiao[para]);
            var meio = (a + b) / 2 + new Vector2(0, -30);   // arco, como nas projeções de rota
            Vector2 Ponto(float u) => (1 - u) * (1 - u) * a + 2 * (1 - u) * u * meio + u * u * b;
            var ciano = IsoDesenho.C("41d7f5");
            for (int k = 0; k < 60; k++)
                if (k % 3 != 2) tela.Pixel(Mathf.RoundToInt(Ponto(k / 60f).x), Mathf.RoundToInt(Ponto(k / 60f).y), ciano);
            for (int k = 0; k < 2; k++)
            {
                float f = (t * 0.25f + k * 0.5f + para * 0.13f) % 1f;
                var p = Ponto(k == 0 ? f : 1 - f);
                tela.Ret(Mathf.RoundToInt(p.x) - 1, Mathf.RoundToInt(p.y) - 1, 3, 3, IsoDesenho.C("fdf6e3"));
            }
        }

        void Regiao(int r)
        {
            var p = M(PosicaoRegiao[r]);
            bool aberta = r == 0 || r <= E.RegioesExtras;
            bool pane = r > 0 && E.RegiaoEmPane == r;
            string nome = Catalogo.NomesRegioes[r];
            if (!aberta)
            {
                // região ainda não aberta: anel tracejado apagado; a próxima recebe o marcador
                for (int k = 0; k < 16; k += 2)
                {
                    float ang = k / 16f * Mathf.PI * 2;
                    tela.Pixel(p.x + Mathf.RoundToInt(Mathf.Cos(ang) * 7), p.y + Mathf.RoundToInt(Mathf.Sin(ang) * 7), IsoDesenho.C("5d6178"));
                }
                if (r == E.RegioesExtras + 1 && !E.NoMaximo(Catalogo.Regiao)) Marcador = p + new Vector2Int(0, 8);
                return;
            }
            float pulso = (Mathf.Sin(t * 3 + r) + 1) / 2;
            string cor = pane ? (Piscar() ? "ff3b4e" : "5a1a24") : r == 0 ? "ffd65c" : "5cff8a";
            int raio = 4 + Mathf.RoundToInt(pulso * 3);
            var halo = IsoDesenho.C(cor); halo.a = 70;
            tela.Circulo(p.x, p.y, raio + 3, halo);
            tela.Circulo(p.x, p.y, 3, IsoDesenho.C(cor));
            // brilho roxo da nuvem de IA (GPU) na sede
            if (r == 0 && E.Nivel(Catalogo.Gpu) > 0)
            {
                var roxo = IsoDesenho.C("b48cff"); roxo.a = (byte)(60 + pulso * 80);
                for (int k = 0; k < E.Nivel(Catalogo.Gpu); k++) tela.Circulo(p.x, p.y, 12 + k * 5, roxo);
                tela.Circulo(p.x, p.y, 3, IsoDesenho.C("ffd65c"));
            }
            if (pane && Piscar()) tela.Texto("!", p.x - 1, p.y - 16, IsoDesenho.C("ff3b4e"), true, 2);
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 12, p.y - 12, 24, 24), Tipo = "regiao:" + r });
            Placas.Add(new Placa { Setor = "Mundo", Nome = r == 0 ? "Sede: " + nome : nome, Pos = p + new Vector2Int(0, -14), Cor = r == 0 ? IsoGui.Cor("ffd65c") : IsoGui.Verde });
        }

        void Catavento(Vector2Int p)
        {
            var branco = IsoDesenho.C("e8f4ff");
            tela.Ret(p.x, p.y - 8, 1, 8, branco);
            float ang = t * 4;
            for (int k = 0; k < 3; k++)
            {
                float a = ang + k * 2.094f;
                d.Linha(new Vector2Int(p.x, p.y - 8), new Vector2Int(p.x + Mathf.RoundToInt(Mathf.Cos(a) * 5), p.y - 8 + Mathf.RoundToInt(Mathf.Sin(a) * 5)), branco);
            }
        }
    }
}
