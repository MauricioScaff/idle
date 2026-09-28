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
        const int AParede = 132;   // um pouco mais que a altura de uma pessoa (96)
        const int ACasas = 4;               // armário: 4 × 4 casas (2 × 2 pisos): apertado, como um armário

        PixelCanvas telaArte;
        IsoDesenho dArte;
        Vector2Int aO;
        bool desenhandoArte;
        RectInt areaArte;

        /// <summary>Cargos que já têm a arte nova.</summary>
        bool TemArteNova => E.Cargo == 0;

        /// <summary>
        /// Um sprite da arte nova. "frente" é o x do canto da frente da base (o pixel mais baixo): à esquerda dele
        /// fica a face que corre ao longo de gx, à direita a que corre ao longo de gy. Daí sai quantas casas ele ocupa.
        /// </summary>
        class SpriteIso
        {
            public Color32[] px; public int w, h, frente;
            public List<int> leds = new List<int>();
            public float CasasX => frente / (float)AHW;          // ao longo de gx (face da esquerda)
            public float CasasY => (w - frente) / (float)AHW;    // ao longo de gy (face da direita)
        }
        static readonly Dictionary<string, SpriteIso> sprites = new Dictionary<string, SpriteIso>();

        static SpriteIso Carregar(string nome)
        {
            if (sprites.TryGetValue(nome, out var s)) return s;
            var tex = ArteGerada.Textura("Iso/" + nome);
            s = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            return sprites[nome] = s;
        }

        static SpriteIso Preparar(Color32[] px, int w, int h)
        {
            var s = new SpriteIso { px = px, w = w, h = h, frente = w / 2 };
            // LEDs: pixels bem verdes ou bem vermelhos e claros (piscam no desenho)
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a > 0 && ((c.g > 170 && c.r < 140 && c.b < 140) || (c.r > 190 && c.g < 90 && c.b < 90))) s.leds.Add(i);
            }
            // canto da frente: média dos pixels opacos nas duas linhas mais baixas
            for (int y = h - 1; y >= 0; y--)
            {
                int soma = 0, n = 0;
                for (int yy = y; yy >= Mathf.Max(0, y - 1); yy--)
                    for (int x = 0; x < w; x++)
                        if (px[yy * w + x].a > 0) { soma += x; n++; }
                if (n > 0) { s.frente = soma / n; break; }
            }
            return s;
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

            // na parede: janela da cidade e prateleira (esquerda), quadro (direita, acima das torres)
            NaParedeArte("janela", 0.02f, 1.25f, 52);
            NaParedeArte("prateleira", 0.05f, 3.0f, 58);
        }

        void NaParedeArte(string nome, float gx, float gy, float z)
        {
            var s = Carregar(nome);
            var p = AP(gx, gy, z);
            tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - s.h);
        }

        // ---------------- Objetos ----------------

        struct Colocado { public float gx, gy, casasX, casasY; public RectInt tela; }

        /// <summary>
        /// Põe um sprite no piso com o canto do FUNDO da base em (gx, gy); o tamanho da base sai do próprio sprite,
        /// então ele encosta certinho nas paredes e nos vizinhos. z levanta (em cima de outra coisa).
        /// </summary>
        Colocado Colocar(string nome, float gx, float gy, string clique = null, bool ledsPiscam = true, float z = 0)
        {
            var s = Carregar(nome);
            float cx = s.CasasX, cy = s.CasasY;
            var f = AP(gx + cx, gy + cy, z);
            int x = f.x - s.frente, y = f.y - s.h + 1;
            float semente = gx * 7 + gy * 3;
            fila.Add((gx + cx + gy + cy + (z > 0 ? 0.05f : 0), () => DesenharSprite(s, x, y, ledsPiscam ? semente : -1)));
            var r = new RectInt(x, y, s.w, s.h);
            if (clique != null) Alvos.Add(new Alvo { Area = r, Tipo = clique });
            return new Colocado { gx = gx, gy = gy, casasX = cx, casasY = cy, tela = r };
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

        /// <summary>Onde ficam as torres visíveis (as demais contam, mas não cabem no armário). Recalculado a cada quadro.</summary>
        Vector2[] LugaresDasTorres = new Vector2[0];
        Colocado mesaArte;

        void ObjetosArte()
        {
            const float Parede = 0.08f;   // folga até a parede
            // parede da direita: planta no canto, a mesa com o CRT e depois a fila de torres
            var planta = Colocar("planta", Parede, Parede);
            mesaArte = Colocar("mesa_crt", Parede + planta.casasX + 0.1f, Parede, "equipamento");
            var mesa = mesaArte;
            // caneca em cima da mesa, na ponta livre (clique: café)
            {
                var s = Carregar("caneca");
                int x = mesa.tela.x + Mathf.RoundToInt(mesa.tela.width * 0.72f), y = mesa.tela.y + Mathf.RoundToInt(mesa.tela.height * 0.36f);
                fila.Add((mesa.gx + mesa.casasX + mesa.gy + mesa.casasY + 0.05f, () => DesenharSprite(s, x, y)));
                Alvos.Add(new Alvo { Area = new RectInt(x - 3, y - 3, s.w + 6, s.h + 6), Tipo = "cafe" });
            }
            // cadeira na frente da mesa; lixeira ao lado
            var cadeira = Colocar("cadeira", mesa.gx + mesa.casasX * 0.3f, mesa.gy + mesa.casasY + 0.15f);
            Colocar("lixeira", cadeira.gx + cadeira.casasX + 0.35f, cadeira.gy + 0.25f);

            // torres em fila na parede; quando a fila acaba, uma segunda fila na frente
            var torre = Carregar("torre");
            float inicio = mesa.gx + mesa.casasX + 0.25f, tx = inicio, ty = Parede;
            var lugares = new List<Vector2>();
            while (lugares.Count < 12)
            {
                if (tx + torre.CasasX > ACasas - Parede) { tx = inicio; ty += torre.CasasY + 0.3f; }
                lugares.Add(new Vector2(tx, ty));
                tx += torre.CasasX + 0.06f;
            }
            LugaresDasTorres = lugares.ToArray();
            var meioDaFila = LugaresDasTorres[Mathf.Min(2, LugaresDasTorres.Length - 1)];
            NovaPlacaArte("Compute", "Compute", meioDaFila.x + torre.CasasX / 2, meioDaFila.y, AParede + 8, IsoGui.Cyan);
            NaParedeArte("quadro", meioDaFila.x + torre.CasasX, 0.02f, 66);
            for (int i = 0; i < Mathf.Min(LugaresDasTorres.Length, E.Torres); i++)
            {
                var l = LugaresDasTorres[i];
                bool travado = E.Travado(i);
                var c = Colocar(travado ? "torre_travada" : "torre", l.x, l.y, "servidor:" + i);
                if (travado) fila.Add((l.x + l.y + 3f, () => AlertaArte(c.tela.x + c.tela.width / 2, c.tela.y - 4)));
            }

            // compras do armário que aparecem: filtro de linha no chão, perto das torres; ventilador na parede da esquerda
            if (E.Nivel(Catalogo.FiltroDeLinha) > 0) Colocar("filtro_linha", inicio - 0.1f, mesa.gy + mesa.casasY + 0.35f);
            if (E.Nivel(Catalogo.Ventilador) > 0) Colocar("ventilador", Parede, 1.4f);

            // parede da esquerda, no fundo: caixas (com o roteador em cima, quando o estagiário chega)
            var caixas = Colocar("caixas", Parede, 3.0f);
            if (E.TemEstagiario) Colocar("roteador", Parede + 0.05f, 3.05f, null, true, caixas.tela.height - 16);

            MarcadorArte();
        }

        void NovaPlacaArte(string setor, string nome, float gx, float gy, float z, Color cor) =>
            Placas.Add(new Placa { Setor = setor, Nome = nome, Pos = AP(gx, gy, z), Cor = cor });

        void AlertaArte(int x, int y)
        {
            if (!Piscar()) return;
            tela.Texto("!", x - 2, y - 16, IsoDesenho.C("ff3b4e"), true, 3);
        }

        void MarcadorArte()
        {
            if (E.Torres >= LugaresDasTorres.Length) return;
            var l = LugaresDasTorres[E.Torres];
            var torre = Carregar("torre");
            float w = torre.CasasX, dd = torre.CasasY;
            fila.Add((l.x + l.y + 0.2f, () =>
            {
                var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                var a = AP(l.x, l.y); var b = AP(l.x + w, l.y); var c = AP(l.x + w, l.y + dd); var e = AP(l.x, l.y + dd);
                foreach (var (p, q) in new[] { (a, b), (b, c), (c, e), (e, a) })
                {
                    int n = Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y));
                    for (int i = 0; i <= n; i += 3) d.Ponto(Vector2Int.RoundToInt(Vector2.Lerp(p, q, (float)i / Mathf.Max(1, n))), cor, 2);
                }
            }));
            Marcador = AP(l.x + w / 2, l.y + dd / 2);
        }

        // ---------------- Pessoas e efeitos ----------------

        /// <summary>Sprite de pessoa da arte nova, com o uniforme do cargo (cache por cargo).</summary>
        SpriteIso Pessoa(string nome, string prefixo)
        {
            string chave = nome + "#" + E.Cargo;
            if (sprites.TryGetValue(chave, out var s)) return s;
            var tex = Uniformes.TexturaDoCargo("Iso/" + nome, prefixo, E.Cargo);
            s = new SpriteIso { px = ArteGerada.PixelsDeCimaParaBaixo(tex), w = tex.width, h = tex.height };
            return sprites[chave] = s;
        }

        void PersonagensArte()
        {
            // técnico: conserta a torre travada (ajoelhado ao lado dela), comemora logo depois de uma compra, senão passeia
            int travada = -1;
            for (int i = 0; i < Mathf.Min(LugaresDasTorres.Length, E.Torres); i++)
                if (E.Travado(i)) { travada = i; break; }
            float gxAndando = PosicaoAndando(1.0f, 3.3f, 0f, 0.55f, out bool voltando);
            if (travada >= 0)
            {
                var l = LugaresDasTorres[travada];
                var tr = Carregar("torre");
                PessoaArte(Pessoa("tecnico_conserta", "tecnico"), l.x + tr.CasasX * 0.2f, l.y + tr.CasasY + 0.45f, false);   // ajoelhado na frente da torre
            }
            else if (t - ultimaCompraEm < 1.5f) PessoaArte(Pessoa("tecnico_comemora", "tecnico"), gxAndando, 2.3f, false);
            else PessoaArte(Passo("tecnico", "tecnico", 0f), gxAndando, 2.3f, voltando);

            if (E.TemEstagiario)
            {
                float gx = PosicaoAndando(1.2f, 3.4f, 0.37f, 0.5f, out bool volta);
                PessoaArte(Passo("estagiario", "estagiario", 0.37f), gx, 3.3f, volta);
            }
        }

        /// <summary>Vai e volta entre gx0 e gx1 (a pessoa olha para onde anda).</summary>
        float PosicaoAndando(float gx0, float gx1, float fase, float velocidade, out bool voltando)
        {
            float ciclo = (t * velocidade * 0.2f + fase) % 2f;
            voltando = ciclo >= 1f;
            return Mathf.Lerp(gx0, gx1, ciclo < 1f ? ciclo : 2f - ciclo);
        }

        /// <summary>Quadro da caminhada: passada, parado, outra passada, parado.</summary>
        SpriteIso Passo(string quem, string prefixo, float fase)
        {
            int q = Mathf.FloorToInt(t * 5f + fase * 10) % 4;
            return Pessoa(q == 0 ? quem + "_andar_a" : q == 2 ? quem + "_andar_b" : quem + "_parado", prefixo);
        }

        /// <summary>Desenha uma pessoa com os pés em (gx, gy); espelhada quando anda para trás.</summary>
        void PessoaArte(SpriteIso s, float gx, float gy, bool espelhar)
        {
            fila.Add((gx + gy + 0.4f, () =>
            {
                var p = AP(gx, gy);
                tela.Ret(p.x - 12, p.y - 2, 24, 4, new Color32(20, 20, 40, 90));   // sombra
                int x0 = p.x - s.w / 2, y0 = p.y - s.h;
                for (int y = 0; y < s.h; y++)
                    for (int x = 0; x < s.w; x++)
                    {
                        var c = s.px[y * s.w + (espelhar ? s.w - 1 - x : x)];
                        if (c.a > 0) tela.Pixel(x0 + x, y0 + y, c);
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
            var p = AP(mesaArte.gx + mesaArte.casasX / 2, mesaArte.gy + mesaArte.casasY / 2, 104 + bob);   // flutua sobre a mesa
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
