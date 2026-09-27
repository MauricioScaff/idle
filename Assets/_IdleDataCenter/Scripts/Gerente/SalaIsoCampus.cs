using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Arquiteto: a "troca de escala". A sala vira o prédio DC-01 num quarteirão com ruas, carros e árvores,
    /// e cada datacenter novo é outro prédio. Fibra liga os prédios (com dados correndo), a CDN aparece como
    /// nuvens sobre o DC-01 e os geradores ficam ao lado de cada prédio. Queda de energia apaga as janelas.
    /// </summary>
    public partial class SalaIso
    {
        const int Campus = 14;
        // terrenos (canto do lote); o prédio ocupa 3×3 com meio quadrado de recuo
        static readonly Vector2[] Lotes = { new Vector2(1, 1), new Vector2(9, 1), new Vector2(1, 9), new Vector2(9, 9) };
        PixelCanvas telaCampus;

        void DesenharCampus()
        {
            // margens justas: com 464 × 320 a tela cabe no zoom 2 do modo gerente
            if (telaCampus == null) telaCampus = new PixelCanvas(Campus * IsoDesenho.TW + 16, Campus * IsoDesenho.TH + 96);
            tela = telaCampus;
            d = new IsoDesenho(tela) { Ox = 8 + Campus * IsoDesenho.TW / 2, Oy = 84 };
            tela.Limpar(new Color32(0, 0, 0, 0));

            Chao();
            MontarCampus();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompra();
            ChamadoUrgente();   // o chamado flutua sobre o DC-01
            tela.Aplicar();
        }

        bool EhRua(int gx, int gy) => (gx >= 6 && gx < 8) || (gy >= 6 && gy < 8);

        void Chao()
        {
            var grama = IsoDesenho.C("5cae5c"); var grama2 = IsoDesenho.C("54a354");
            var rua = IsoDesenho.C("4a4f63"); var calcada = IsoDesenho.C("9aa3b8");
            for (int gx = 0; gx < Campus; gx++)
            for (int gy = 0; gy < Campus; gy++)
            {
                bool ruaAqui = EhRua(gx, gy);
                bool calcadaAqui = !ruaAqui && (EhRua(gx + 1, gy) || EhRua(gx - 1, gy) || EhRua(gx, gy + 1) || EhRua(gx, gy - 1));
                d.Piso(gx, gy, 1, 1, ruaAqui ? rua : calcadaAqui ? calcada : (gx + gy) % 2 == 0 ? grama : grama2);
            }
            // faixa amarela tracejada no meio das ruas
            var amarelo = IsoDesenho.C("ffd65c");
            for (int i = 0; i < Campus; i++)
            {
                if (i >= 6 && i < 8) continue;
                d.Linha(d.P(i + 0.2f, 7), d.P(i + 0.6f, 7), amarelo);
                d.Linha(d.P(7, i + 0.2f), d.P(7, i + 0.6f), amarelo);
            }
            // borda do terreno
            d.Poligono(IsoDesenho.C("3a6a3a"), d.P(0, Campus), d.P(Campus, Campus), d.P(Campus, Campus, -6), d.P(0, Campus, -6));
            d.Poligono(IsoDesenho.C("2f5a2f"), d.P(Campus, 0), d.P(Campus, Campus), d.P(Campus, Campus, -6), d.P(Campus, 0, -6));
        }

        void MontarCampus()
        {
            int predios = E.TotalDatacenters;
            for (int i = 0; i < Lotes.Length; i++)
            {
                var l = Lotes[i];
                int idx = i;
                if (i < predios)
                {
                    float gx = l.x + 0.5f, gy = l.y + 0.5f;
                    Adicionar(gx, gy, 3, 3, () => Predio(gx, gy, idx));
                    Clicavel(gx, gy, 3, 3, 64, "dc:" + i);
                    Placas.Add(new Placa { Setor = "Campus", Nome = "DC-0" + (i + 1), Pos = d.P(gx + 1.5f, gy + 1.5f, 84), Cor = i == 0 ? IsoGui.Cyan : IsoGui.Laranja });
                    if (E.Nivel(Catalogo.Gerador) > 0 && i > 0) Adicionar(l.x + 3.4f, l.y + 0.2f, 0.5f, 0.5f, () => Gerador(l.x + 3.4f, l.y + 0.2f));
                }
                else
                {
                    // terreno vazio: contorno tracejado; o próximo recebe o marcador de construção
                    fila.Add((l.x + l.y, () => TerrenoVazio(l.x + 0.5f, l.y + 0.5f)));
                    if (i == predios && !E.NoMaximo(Catalogo.Datacenter)) Marcador = d.P(l.x + 2f, l.y + 2f);
                }
                // árvores nos cantos do lote
                float ax = l.x + 0.1f, ay = l.y + 3.4f;
                Adicionar(ax, ay, 0.5f, 0.5f, () => Arvore(ax, ay));
            }

            // fibra: do DC-01 até cada prédio interligado, com pacotes de dados correndo
            for (int i = 1; i <= E.DatacentersInterligados && i < predios; i++)
            {
                int alvo = i;
                fila.Add((1f, () => Fibra(0, alvo)));
            }

            // CDN: nuvens sobre o DC-01 (uma por nível)
            int nuvens = E.Nivel(Catalogo.Cdn);
            if (nuvens > 0) fila.Add((100f, () => NuvensCdn(nuvens)));

            // carros nas ruas
            for (int c = 0; c < 3; c++)
            {
                int k = c;
                float u = ((t * (0.9f + c * 0.3f) + c * 4.3f) % (Campus + 2)) - 1;
                bool horizontal = c != 1;
                float gx = horizontal ? u : 6.3f, gy = horizontal ? (c == 0 ? 6.3f : 7.3f) : u;
                if (c == 2) gx = Campus - 1 - u;
                fila.Add((gx + gy + 0.5f, () => Carro(gx, gy, horizontal, k)));
            }

            // engenheiros andando nas calçadas; drone entregando entre os prédios
            Andando(engenheiro, 0.5f, 5.5f, 5.6f, 0.2f, 0.5f);
            Andando(tecnico, 8.5f, 13f, 5.6f, 0.7f, 0.45f);
            if (E.TemAutomacao(Catalogo.Watchdog))
            {
                float gx = 7 + Mathf.Sin(t * 0.3f) * 5, gy = 7 + Mathf.Cos(t * 0.3f) * 5;
                fila.Add((200f, () => Drone(gx, gy)));
            }
        }

        void Predio(float gx, float gy, int indice)
        {
            bool semEnergia = indice > 0 && E.DatacenterSemEnergia == indice;
            var paredeE = indice == 0 ? IsoDesenho.C("3a4668") : IsoDesenho.C("46506e");
            var paredeD = indice == 0 ? IsoDesenho.C("2c3654") : IsoDesenho.C("363f5a");
            d.Caixa(gx, gy, 3, 3, 64, IsoDesenho.C("6d7390"), paredeE, paredeD);
            // janelas: fileiras de LEDs de racks vistas por fora (apagadas numa queda de energia)
            string[] cores = { "5cff8a", "5cc8ff", "5cff8a", "b48cff" };
            for (int andar = 0; andar < 4; andar++)
            for (int j = 0; j < 6; j++)
            {
                float u = 0.08f + j * 0.15f, z = 52 - andar * 13;
                Color32 cor = semEnergia ? IsoDesenho.C("1b1f2c") : Led(indice * 31 + andar * 7 + j, IsoDesenho.C(cores[(andar + j + indice) % cores.Length]), 1.5f + j % 3);
                var a = d.FaceEsq(gx, gy, 3, 3, u, z);
                tela.Ret(a.x, a.y, 5, 3, IsoDesenho.C("1b1f2c"));
                tela.Ret(a.x + 1, a.y + 1, 3, 1, cor);
                var b = d.FaceDir(gx, gy, 3, 3, u, z);
                tela.Ret(b.x - 5, b.y, 5, 3, IsoDesenho.C("1b1f2c"));
                tela.Ret(b.x - 4, b.y + 1, 3, 1, semEnergia ? cor : IsoDesenho.Escurecer(cor, 0.8f));
            }
            // porta
            var p = d.FaceEsq(gx, gy, 3, 3, 0.45f, 12);
            tela.Ret(p.x, p.y, 8, 12, IsoDesenho.C("1b1f2c"));
            tela.Ret(p.x + 1, p.y + 1, 6, 10, IsoDesenho.C(indice == 0 ? "ffd65c" : "7cc8ff"));
            // telhado: condensadoras de ar
            for (int k = 0; k < 3; k++)
                d.Caixa(gx + 0.3f + k * 0.9f, gy + 0.4f, 0.6f, 0.6f, 6, IsoDesenho.C("e8edf5"), IsoDesenho.C("c9d0dc"), IsoDesenho.C("a9b0bf"), 64);
            if (semEnergia) Alerta(gx + 1.5f, gy + 1.5f, 84);
        }

        void TerrenoVazio(float gx, float gy)
        {
            var cor = IsoDesenho.C("d9c87a");
            var cantos = new[] { d.P(gx, gy), d.P(gx + 3, gy), d.P(gx + 3, gy + 3), d.P(gx, gy + 3) };
            for (int i = 0; i < 4; i++)
            {
                var a = cantos[i]; var b = cantos[(i + 1) % 4];
                int n = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
                for (int k = 0; k < n; k += 4) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(a, b, (float)k / n)), cor, 2);
            }
        }

        void Gerador(float gx, float gy)
        {
            d.Caixa(gx, gy, 0.5f, 0.5f, 10, IsoDesenho.C("ffd65c"), IsoDesenho.C("d9b23a"), IsoDesenho.C("b8932a"));
            var cano = d.P(gx + 0.25f, gy + 0.25f, 10);
            tela.Ret(cano.x, cano.y - 4, 2, 4, IsoDesenho.C("5d6178"));
            if (E.TemQuedaDeEnergia && Piscar(0.2f)) tela.Ret(cano.x - 1, cano.y - 8, 4, 3, new Color32(120, 120, 130, 160));
        }

        void Arvore(float gx, float gy)
        {
            var p = d.P(gx + 0.25f, gy + 0.25f);
            tela.Ret(p.x - 1, p.y - 8, 2, 8, IsoDesenho.C("7d5238"));
            tela.Circulo(p.x, p.y - 12, 6, IsoDesenho.C("3f9b54"));
            tela.Circulo(p.x - 2, p.y - 15, 3, IsoDesenho.C("6fd36f"));
        }

        void Fibra(int de, int para)
        {
            var a = Lotes[de] + new Vector2(2, 2); var b = Lotes[para] + new Vector2(2, 2);
            var pa = d.P(a.x, a.y, 2); var pb = d.P(b.x, b.y, 2);
            var laranja = IsoDesenho.C("ff9f43");
            d.Linha(pa, pb, laranja);
            d.Linha(pa + Vector2Int.up, pb + Vector2Int.up, IsoDesenho.Escurecer(laranja, 0.7f));
            // pacotes de dados indo e voltando
            for (int k = 0; k < 3; k++)
            {
                float f = (t * 0.35f + k / 3f + para * 0.17f) % 1f;
                var q = Vector2Int.RoundToInt(Vector2.Lerp(pa, pb, k % 2 == 0 ? f : 1 - f));
                tela.Ret(q.x - 1, q.y - 1, 3, 3, IsoDesenho.C(k % 2 == 0 ? "fdf6e3" : "5cc8ff"));
            }
        }

        void NuvensCdn(int nivel)
        {
            for (int i = 0; i < nivel; i++)
            {
                var c = d.P(2.5f + i * 0.9f, 2.0f, 110 + Mathf.Sin(t + i) * 3);
                var branco = IsoDesenho.C("e8f4ff");
                tela.Circulo(c.x, c.y, 5, branco);
                tela.Circulo(c.x + 6, c.y + 1, 4, branco);
                tela.Circulo(c.x - 6, c.y + 1, 4, branco);
                tela.Ret(c.x - 9, c.y + 1, 19, 4, branco);
                // dados "chovendo" da nuvem para os usuários
                float f = (t * 1.3f + i * 0.4f) % 1f;
                tela.Pixel(c.x - 3 + i, c.y + 7 + Mathf.RoundToInt(f * 14), IsoDesenho.C("5cc8ff"));
            }
        }

        void Carro(float gx, float gy, bool horizontal, int cor)
        {
            string[] cores = { "ff5d7a", "5aa9ff", "ffd65c" };
            float w = horizontal ? 0.7f : 0.4f, dd = horizontal ? 0.4f : 0.7f;
            var c = IsoDesenho.C(cores[cor]);
            d.Caixa(gx, gy, w, dd, 5, c, IsoDesenho.Escurecer(c, 0.8f), IsoDesenho.Escurecer(c, 0.65f));
            d.Caixa(gx + w * 0.2f, gy + dd * 0.2f, w * 0.6f, dd * 0.6f, 4, IsoDesenho.C("bfe3ff"), IsoDesenho.C("7cc8ff"), IsoDesenho.C("5aa9ff"), 5);
        }
    }
}
