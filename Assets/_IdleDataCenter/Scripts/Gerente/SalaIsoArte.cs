using System;
using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A sala desenhada com a arte gerada (Resources/Arte/Iso: sprites limpos pela ferramenta LimparFolhaDePixelArt).
    /// A grade segue a arte: cada piso gerado cobre 2 × 2 casas de 64 × 30 pixels. As paredes são desenhadas
    /// em código com as cores da arte (encaixam certinho no piso). Por enquanto vale para o armário do Técnico;
    /// os outros cargos continuam com o desenho em código até ganharem as folhas deles.
    /// </summary>
    public partial class SalaIso
    {
        const int AHW = 32, AHH = 15;       // meia casa da grade da arte nova
        const int AParede = 92;
        const int ACasas = 6;               // armário: 6 × 6 casas (3 × 3 pisos)

        PixelCanvas telaArte;
        IsoDesenho dArte;
        Vector2Int aO;
        bool desenhandoArte;
        RectInt areaArte;

        /// <summary>Cargos que já têm a arte nova.</summary>
        bool TemArteNova => E.Cargo == 0;

        class SpriteIso { public Color32[] px; public int w, h; public List<int> leds = new List<int>(); }
        static readonly Dictionary<string, SpriteIso> sprites = new Dictionary<string, SpriteIso>();

        static SpriteIso Carregar(string nome)
        {
            if (sprites.TryGetValue(nome, out var s)) return s;
            var tex = ArteGerada.Textura("Iso/" + nome);
            s = new SpriteIso { px = ArteGerada.PixelsDeCimaParaBaixo(tex), w = tex.width, h = tex.height };
            // LEDs: pixels bem verdes ou bem vermelhos e claros (piscam no desenho)
            for (int i = 0; i < s.px.Length; i++)
            {
                var c = s.px[i];
                if (c.a > 0 && ((c.g > 170 && c.r < 140 && c.b < 140) || (c.r > 190 && c.g < 90 && c.b < 90))) s.leds.Add(i);
            }
            return sprites[nome] = s;
        }

        Vector2Int AP(float gx, float gy, float z = 0) =>
            new Vector2Int(Mathf.RoundToInt(aO.x + (gx - gy) * AHW), Mathf.RoundToInt(aO.y + (gx + gy) * AHH - z));

        void MontarArte()
        {
            int largura = ACasas * 2 * AHW + 48, altura = ACasas * 2 * AHH + AParede + 48;
            telaArte = new PixelCanvas(largura, altura);
            dArte = new IsoDesenho(telaArte);
            aO = new Vector2Int(24 + ACasas * AHW, AParede + 24);
            var esq = AP(0, ACasas); var dir = AP(ACasas, 0); var topo = AP(0, 0, AParede); var baixo = AP(ACasas, ACasas);
            areaArte = new RectInt(esq.x - 4, topo.y - 4, dir.x - esq.x + 8, baixo.y + 8 - topo.y);
        }

        void DesenharArte()
        {
            if (telaArte == null) MontarArte();
            tela = telaArte; d = dArte;
            desenhandoArte = true;
            tela.Limpar(new Color32(0, 0, 0, 0));

            ParedesEPisoArte();
            ObjetosArte();
            PersonagensArte();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompraArte();
            ChamadoArte();
            tela.Aplicar();
        }

        // ---------------- Sala ----------------

        void ParedesEPisoArte()
        {
            int n = ACasas;
            Color32 C(string h) => IsoDesenho.C(h);
            // paredes do fundo (cores tiradas da arte gerada), rodapé e o friso claro no topo
            d.Poligono(C("2f4c80"), AP(0, 0), AP(0, n), AP(0, n, AParede), AP(0, 0, AParede));
            d.Poligono(C("28416f"), AP(0, 0), AP(n, 0), AP(n, 0, AParede), AP(0, 0, AParede));
            d.Poligono(C("1c2b4f"), AP(0, 0), AP(0, n), AP(0, n, 7), AP(0, 0, 7));
            d.Poligono(C("18264a"), AP(0, 0), AP(n, 0), AP(n, 0, 7), AP(0, 0, 7));
            d.Poligono(C("7a9bd4"), AP(0, 0, AParede), AP(0, n, AParede), AP(-0.12f, n, AParede + 3), AP(-0.12f, -0.12f, AParede + 3));
            d.Poligono(C("6886bd"), AP(0, 0, AParede), AP(n, 0, AParede), AP(n, -0.12f, AParede + 3), AP(-0.12f, -0.12f, AParede + 3));
            var canto = AP(0, 0);
            tela.Ret(canto.x, canto.y - AParede, 1, AParede, C("101a33"));

            // piso: cada peça gerada cobre 2 × 2 casas
            for (int gy = 0; gy < n; gy += 2)
                for (int gx = 0; gx < n; gx += 2)
                {
                    var s = Carregar((gx + gy) / 2 % 2 == 0 ? "piso_a" : "piso_b");
                    var p = AP(gx, gy);
                    tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - 1);
                }
            // espessura da laje na frente
            d.Poligono(C("4a2b30"), AP(0, n), AP(n, n), AP(n, n, -6), AP(0, n, -6));
            d.Poligono(C("3b2227"), AP(n, 0), AP(n, n), AP(n, n, -6), AP(n, 0, -6));

            // na parede: janela da cidade (esquerda), quadro (direita) e a prateleira com os manuais
            NaParedeArte("janela", 0.02f, 1.7f, 34);
            NaParedeArte("quadro", 2.4f, 0.02f, 40);
            NaParedeArte("prateleira", 0.1f, 3.6f, 40);
        }

        void NaParedeArte(string nome, float gx, float gy, float z)
        {
            var s = Carregar(nome);
            var p = AP(gx, gy, z);
            tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - s.h);
        }

        // ---------------- Objetos ----------------

        /// <summary>Um sprite no piso: ocupa (gx..gx+w, gy..gy+dd); a base encosta no canto da frente.</summary>
        void ObjetoArte(string nome, float gx, float gy, float w, float dd, string clique = null, bool ledsPiscam = true, int dy = 0)
        {
            var s = Carregar(nome);
            var centro = AP(gx + w / 2, gy + dd / 2);
            int x = centro.x - s.w / 2, y = AP(gx + w, gy + dd).y - s.h + dy;
            float semente = gx * 7 + gy * 3;
            fila.Add((gx + w + gy + dd, () => DesenharSprite(s, x, y, ledsPiscam ? semente : -1)));
            if (clique != null) Alvos.Add(new Alvo { Area = new RectInt(x, y, s.w, s.h), Tipo = clique });
        }

        void DesenharSprite(SpriteIso s, int x, int y, float sementeLed = -1)
        {
            tela.Imagem(s.px, s.w, s.h, x, y);
            if (sementeLed < 0 || s.leds.Count == 0) return;
            // LEDs piscando: de vez em quando cada um apaga um instante
            bool apagado = Mathf.Sin(t * 5f + sementeLed) > 0.6f;
            if (!apagado) return;
            foreach (int i in s.leds) tela.Pixel(x + i % s.w, y + i / s.w, new Color32(30, 40, 30, 255));
        }

        /// <summary>Onde ficam as torres visíveis (as demais contam, mas não cabem no armário).</summary>
        static readonly Vector2[] LugaresDasTorres =
        {
            new Vector2(3.5f, 0.3f), new Vector2(4.3f, 0.3f), new Vector2(5.1f, 0.3f), new Vector2(5.1f, 1.2f), new Vector2(5.1f, 2.1f), new Vector2(5.1f, 3.0f),
        };

        void ObjetosArte()
        {
            // canto: planta; mesa com o CRT encostada na parede da direita, cadeira na frente
            ObjetoArte("planta", 0.3f, 0.3f, 0.8f, 0.8f);
            ObjetoArte("mesa_crt", 1.3f, 0.2f, 1.9f, 1.1f, "equipamento");
            ObjetoArte("cadeira", 1.9f, 1.4f, 0.8f, 0.8f);
            // caneca na ponta da mesa (clique: café)
            {
                var s = Carregar("caneca");
                var p = AP(3.0f, 1.0f, 38);
                fila.Add((4.35f, () => DesenharSprite(s, p.x - s.w / 2, p.y - s.h)));
                Alvos.Add(new Alvo { Area = new RectInt(p.x - s.w / 2 - 2, p.y - s.h - 2, s.w + 4, s.h + 4), Tipo = "cafe" });
            }

            // torres: as que cabem aparecem; travada aparece com o LED vermelho e um "!"
            NovaPlacaArte("Compute", "Compute", 4.3f, 0.3f, AParede + 6, IsoGui.Cyan);
            for (int i = 0; i < Mathf.Min(LugaresDasTorres.Length, E.Torres); i++)
            {
                var l = LugaresDasTorres[i]; int idx = i;
                bool travado = E.Travado(idx);
                ObjetoArte(travado ? "torre_travada" : "torre", l.x, l.y, 0.7f, 0.7f, "servidor:" + i);
                if (travado) fila.Add((l.x + l.y + 2f, () => AlertaArte(l.x + 0.35f, l.y + 0.35f, 66)));
            }

            // compras do armário que aparecem
            if (E.Nivel(Catalogo.FiltroDeLinha) > 0) ObjetoArte("filtro_linha", 3.6f, 1.3f, 1.2f, 0.6f);
            if (E.Nivel(Catalogo.Ventilador) > 0) ObjetoArte("ventilador", 0.4f, 1.6f, 0.6f, 0.6f);
            if (E.TemEstagiario) ObjetoArte("roteador", 0.4f, 4.3f, 0.8f, 0.6f, null, true, -42);   // o estagiário trouxe o roteador (fica em cima das caixas)

            // decoração
            ObjetoArte("caixas", 0.3f, 4.3f, 0.9f, 0.9f);
            ObjetoArte("lixeira", 1.5f, 4.9f, 0.6f, 0.6f);

            MarcadorArte();
        }

        void NovaPlacaArte(string setor, string nome, float gx, float gy, float z, Color cor) =>
            Placas.Add(new Placa { Setor = setor, Nome = nome, Pos = AP(gx, gy, z), Cor = cor });

        void AlertaArte(float gx, float gy, float z)
        {
            if (!Piscar()) return;
            var p = AP(gx, gy, z);
            tela.Texto("!", p.x - 2, p.y - 10, IsoDesenho.C("ff3b4e"), true, 3);
        }

        void MarcadorArte()
        {
            if (E.Torres >= LugaresDasTorres.Length) return;
            var l = LugaresDasTorres[E.Torres];
            fila.Add((l.x + l.y + 1.4f, () =>
            {
                var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                var a = AP(l.x, l.y); var b = AP(l.x + 0.7f, l.y); var c = AP(l.x + 0.7f, l.y + 0.7f); var e = AP(l.x, l.y + 0.7f);
                foreach (var (p, q) in new[] { (a, b), (b, c), (c, e), (e, a) })
                {
                    int n = Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y));
                    for (int i = 0; i <= n; i += 3) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(p, q, (float)i / Mathf.Max(1, n))), cor, 2);
                }
            }));
            Marcador = AP(l.x + 0.35f, l.y + 0.35f);
        }

        // ---------------- Pessoas e efeitos ----------------

        void PersonagensArte()
        {
            // os sprites de personagem ainda são os antigos: desenhados em dobro para ficar na escala da arte nova
            AndandoArte(tecnico, 1.2f, 4.6f, 3.0f, 0f, 0.55f);
            if (E.TemEstagiario) AndandoArte(estagiario, 1.4f, 4.2f, 4.2f, 0.37f, 0.5f);
        }

        void AndandoArte(Quadro[] quadros, float gx0, float gx1, float gy, float fase, float velocidade)
        {
            float ciclo = (t * velocidade * 0.2f + fase) % 2f;
            float u = ciclo < 1f ? ciclo : 2f - ciclo;
            float gx = Mathf.Lerp(gx0, gx1, u);
            bool voltando = ciclo >= 1f;
            fila.Add((gx + gy + 0.4f, () =>
            {
                var q = quadros[Mathf.FloorToInt(t * 6f + fase * 10) % quadros.Length];
                var p = AP(gx, gy);
                const int e = 2;
                tela.Ret(p.x - 10, p.y - 2, 20, 4, new Color32(20, 20, 40, 90));
                for (int y = 0; y < q.h; y++)
                    for (int x = 0; x < q.w; x++)
                    {
                        var c = q.px[y * q.w + (voltando ? q.w - 1 - x : x)];
                        if (c.a > 0) tela.Ret(p.x - q.w * e / 2 + x * e, p.y - q.h * e + y * e, e, e, c);
                    }
            }));
        }

        Vector2? LugarDoItemArte(string id)
        {
            switch (id)
            {
                case Catalogo.Servidor: { int i = Mathf.Clamp(E.Torres - 1, 0, LugaresDasTorres.Length - 1); return LugaresDasTorres[i] + new Vector2(0.35f, 0.35f); }
                case Catalogo.Ssd: case Catalogo.Ventoinha: case Catalogo.PastaTermica: return LugaresDasTorres[0] + new Vector2(0.35f, 0.35f);
                case Catalogo.FiltroDeLinha: return new Vector2(4.2f, 1.6f);
                case Catalogo.Ventilador: return new Vector2(0.7f, 1.9f);
                case Catalogo.Estagiario: return new Vector2(0.8f, 4.6f);
                default: return null;
            }
        }

        void FaiscasDaCompraArte()
        {
            float idade = t - ultimaCompraEm;
            if (idade > 1.2f || ultimaCompra == null) return;
            var lugar = LugarDoItemArte(ultimaCompra);
            if (lugar == null) return;
            var centro = AP(lugar.Value.x, lugar.Value.y, 40);
            for (int i = 0; i < 18; i++)
            {
                float ang = i * 0.449f + i * i * 0.07f, raio = 6 + idade * (36 + (i % 4) * 10);
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 20));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 3, 3, cor);
            }
        }

        void ChamadoArte()
        {
            if (!E.TemChamado) return;
            float bob = Mathf.Sin(t * 4) * 3;
            var p = AP(2.2f, 0.7f, 110 + bob);
            string[] papel =
            {
                "yyyyyyyyy.", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy",
            };
            // o papel em dobro, para combinar com a escala da arte nova
            tela.Ret(p.x - 12, p.y - 22, 24, 22, IsoDesenho.C("1b1a2e"));
            for (int y = 0; y < papel.Length; y++)
                for (int x = 0; x < papel[y].Length; x++)
                    if (papel[y][x] != '.') tela.Ret(p.x - 10 + x * 2, p.y - 20 + y * 2, 2, 2, IsoDesenho.C(papel[y][x] == 'y' ? "fdf6e3" : "7d82ad"));
            tela.Ret(p.x + 6, p.y - 20, 4, 4, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)(E.SegundosDoChamado / Catalogo.TempoParaAtender);
            tela.Ret(p.x - 12, p.y + 3, 24, 3, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 12, p.y + 3, Mathf.RoundToInt(24 * resta), 3, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 14, p.y - 24, 28, 32), Tipo = "chamado" });
        }
    }
}
