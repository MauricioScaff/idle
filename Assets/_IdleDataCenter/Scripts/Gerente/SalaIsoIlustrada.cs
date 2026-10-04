using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Sala ilustrada (PixelLab): uma ilustração inteira da sala como fundo (Resources/Arte/Salas) e, por cima, as compras
    /// em lugares fixos e as pessoas (Resources/Arte/PixelLab). Por enquanto só o armário do Técnico; as outras salas
    /// seguem com a arte de SalaIsoArte até ganharem a sua ilustração.
    ///
    /// Armário: as torres compradas se enfileiram encostadas na parede da esquerda, embaixo da janela; o ventilador
    /// fica preso na parede da direita, o filtro de linha no chão na frente da mesa e a caneca em cima dela.
    /// </summary>
    public partial class SalaIso
    {
        static readonly string[] Ilustracoes = { "armario" };

        PixelCanvas telaIlustrada;
        IsoDesenho dIlustrada;
        SpriteIso fundoIlustrado;
        int cargoIlustrado = -1;

        bool TemIlustracao => E.Cargo < Ilustracoes.Length;

        // piso do armário na ilustração (400 × 320): canto do fundo e o passo de uma casa ao longo de gx (parede da direita)
        // e de gy (parede da esquerda). O piso tem 4 × 4 casas, como na sala antiga.
        static readonly Vector2 FundoDoPiso = new Vector2(200, 146), PassoGx = new Vector2(35f, 18f), PassoGy = new Vector2(-35.25f, 17.75f);

        Vector2Int IP(float gx, float gy, float z = 0)
        {
            var p = FundoDoPiso + PassoGx * gx + PassoGy * gy;
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y - z));
        }

        // ---------------- Sprites do PixelLab ----------------

        static SpriteIso CarregarPixelLab(string nome, bool espelhar = false)
        {
            string chave = "pl/" + nome + (espelhar ? "|espelhado" : "");
            if (sprites.TryGetValue(chave, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Arte/PixelLab/" + nome);
            if (tex == null) return sprites[chave] = null;
            var px = ArteGerada.PixelsDeCimaParaBaixo(tex);
            if (espelhar)
            {
                var e = new Color32[px.Length];
                for (int y = 0; y < tex.height; y++)
                    for (int x = 0; x < tex.width; x++) e[y * tex.width + x] = px[y * tex.width + (tex.width - 1 - x)];
                px = e;
            }
            return sprites[chave] = Preparar(px, tex.width, tex.height);
        }

        /// <summary>A torre com os LEDs em vermelho: é assim que ela aparece travada.</summary>
        static SpriteIso TorreTravada()
        {
            const string chave = "pl/torre|travada";
            if (sprites.TryGetValue(chave, out var s)) return s;
            var o = CarregarPixelLab("torre", true);
            var px = (Color32[])o.px.Clone();
            foreach (int i in o.leds) px[i] = new Color32(255, 70, 80, 255);
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            s.leds.AddRange(o.leds);
            return sprites[chave] = s;
        }

        /// <summary>Linha mais baixa com pixel opaco: onde ficam os pés da pessoa no quadro.</summary>
        static int Pes(SpriteIso s)
        {
            for (int y = s.h - 1; y >= 0; y--)
                for (int x = 0; x < s.w; x++)
                    if (s.px[y * s.w + x].a > 0) return y;
            return s.h - 1;
        }

        // ---------------- Montagem e desenho ----------------

        void MontarIlustrada()
        {
            cargoIlustrado = E.Cargo;
            var tex = ArteGerada.Textura("Salas/" + Ilustracoes[E.Cargo]);
            fundoIlustrado = Preparar(ArteGerada.PixelsDeCimaParaBaixo(tex), tex.width, tex.height);
            telaIlustrada = new PixelCanvas(tex.width, tex.height);
            dIlustrada = new IsoDesenho(telaIlustrada);
            var r = ArteGerada.AreaOpaca(tex);
            areaArte = new RectInt(r.x - 2, tex.height - r.yMax - 2, r.width + 4, r.height + 4);
        }

        void DesenharIlustrada()
        {
            if (telaIlustrada == null || cargoIlustrado != E.Cargo) MontarIlustrada();
            tela = telaIlustrada; d = dIlustrada;
            desenhandoArte = true;
            tela.Limpar(new Color32(0, 0, 0, 0));
            tela.Imagem(fundoIlustrado.px, fundoIlustrado.w, fundoIlustrado.h, 0, 0);

            ArmarioIlustrado();
            fila.Sort((a, b) => a.prof.CompareTo(b.prof));
            foreach (var (_, desenhar) in fila) desenhar();
            FaiscasDaCompraIlustrada();
            ChamadoIlustrado();
            tela.Aplicar();
        }

        // ---------------- Armário ----------------

        /// <summary>Quantas torres cabem em fila na parede da esquerda (da quina até a planta).</summary>
        const int VagasNaParede = 7;

        /// <summary>
        /// Canto de cima à esquerda da imagem da torre i (espelhada, painel virado para dentro da sala). A base dela tem o
        /// canto da esquerda em (16, 49) na imagem; esse canto encosta na linha entre a parede e o piso, que sobe meio
        /// pixel a cada pixel para a direita. Cada torre ocupa 16 pixels ao longo da parede.
        /// </summary>
        static Vector2Int LugarDaTorre(int i)
        {
            int x = 61 + i * 16;
            int y = Mathf.RoundToInt(217 - 0.5f * (x - 57.5f)) + 1;
            return new Vector2Int(x - 16, y - 49);
        }

        Vector2Int pontoDaTorre, pontoDoFiltro, pontoDoVentilador, pontoDoEstagiario;

        void ArmarioIlustrado()
        {
            tecnicoConsertando = false;
            var torre = CarregarPixelLab("torre", true);
            int visiveis = Mathf.Min(E.Torres, VagasNaParede);
            // da direita (fundo) para a esquerda (frente): a torre da frente cobre a de trás
            for (int i = 0; i < visiveis; i++)
            {
                var l = LugarDaTorre(i);
                bool travado = E.Travado(i);
                var s = travado ? TorreTravada() : torre;
                float prof = -i * 0.01f;
                int semente = i;
                fila.Add((prof, () => DesenharSprite(s, l.x, l.y, travado ? -1 : semente * 1.7f)));
                Alvos.Add(new Alvo { Area = new RectInt(l.x + 14, l.y + 10, 36, 50), Tipo = "servidor:" + i });
                if (travado)
                {
                    fila.Add((5, () => { if (Piscar()) tela.Texto("!", l.x + 30, l.y + 2, IsoDesenho.C("ff3b4e"), true, 2); }));
                    if (!tecnicoConsertando) { tecnicoConsertando = true; lugarDoTecnico = new Vector2(l.x + 46, l.y + 66); }
                }
            }
            // a partir da 8ª torre, uma placa em cima da última com o total (abre a loja de Compute)
            if (E.Torres > VagasNaParede)
            {
                var l = LugarDaTorre(VagasNaParede - 1);
                Placas.Add(new Placa { Setor = "Compute", Nome = E.Torres + " torres", Pos = new Vector2Int(l.x + 32, l.y + 6), Cor = IsoGui.Cyan });
            }
            var ultima = LugarDaTorre(Mathf.Clamp(E.Torres - 1, 0, VagasNaParede - 1));
            pontoDaTorre = new Vector2Int(ultima.x + 32, ultima.y + 34);

            // onde entra a próxima torre (contorno da base no piso); com a parede cheia, o botão fica na frente da fila
            if (E.Torres < VagasNaParede) MarcadorDaTorre(LugarDaTorre(E.Torres));
            else Marcador = new Vector2Int(ultima.x + 40, ultima.y + 62);

            // mesa (da ilustração): clique rende, como qualquer equipamento
            Alvos.Add(new Alvo { Area = new RectInt(244, 128, 90, 108), Tipo = "equipamento" });

            var caneca = CarregarPixelLab("caneca");
            if (caneca != null)
            {
                fila.Add((1, () => DesenharSprite(caneca, 308 - caneca.w / 2, 176 - caneca.h)));
                Alvos.Add(new Alvo { Area = new RectInt(308 - caneca.w / 2 - 3, 176 - caneca.h - 3, caneca.w + 6, caneca.h + 6), Tipo = "cafe" });
            }
            pontoDoVentilador = new Vector2Int(318, 112);
            var ventilador = CarregarPixelLab("ventilador");
            if (ventilador != null && E.Nivel(Catalogo.Ventilador) > 0)
                fila.Add((1, () => DesenharSprite(ventilador, pontoDoVentilador.x - ventilador.w / 2, pontoDoVentilador.y - ventilador.h / 2)));
            pontoDoFiltro = IP(3.85f, 1.5f);
            var filtro = CarregarPixelLab("filtro_linha");
            if (filtro != null && E.Nivel(Catalogo.FiltroDeLinha) > 0)
                fila.Add((2, () => DesenharSprite(filtro, pontoDoFiltro.x - filtro.w / 2, pontoDoFiltro.y - filtro.h + 2)));

            PessoasIlustradas();
        }

        /// <summary>Contorno tracejado da base da próxima torre: os quatro cantos da base na imagem da torre espelhada.</summary>
        void MarcadorDaTorre(Vector2Int l)
        {
            var a = l + new Vector2Int(16, 49); var b = l + new Vector2Int(30, 41); var c = l + new Vector2Int(47, 49); var e = l + new Vector2Int(33, 57);
            fila.Add((-1, () =>
            {
                var cor = Piscar(0.5f) ? IsoDesenho.C("ffb458") : IsoDesenho.C("d49335");
                foreach (var (p, q) in new[] { (a, b), (b, c), (c, e), (e, a) })
                {
                    int n = Mathf.Max(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y));
                    for (int i = 0; i <= n; i += 3) tela.Pixel(Mathf.RoundToInt(Mathf.Lerp(p.x, q.x, (float)i / n)), Mathf.RoundToInt(Mathf.Lerp(p.y, q.y, (float)i / n)), cor);
                }
            }));
            Marcador = new Vector2Int((a.x + c.x) / 2, (b.y + e.y) / 2);
        }

        // ---------------- Pessoas ----------------

        /// <summary>
        /// Quadro de uma pessoa do PixelLab. Indo para a direita (gx crescendo) ela olha para o sudeste; voltando, para o
        /// noroeste. Andando, alterna os quadros da caminhada (quem_se_0..n); sem eles, usa a pose parada da direção.
        /// </summary>
        SpriteIso QuadroDaPessoa(string quem, bool voltando, bool andando, float fase)
        {
            string dir = voltando ? "nw" : "se";
            if (andando)
            {
                int n = 0;
                while (n < 8 && CarregarPixelLab(quem + "_" + dir + "_" + n) != null) n++;
                if (n > 0) return CarregarPixelLab(quem + "_" + dir + "_" + (Mathf.FloorToInt(t * 6f + fase * 10) % n));
            }
            return CarregarPixelLab(quem + "_" + dir);
        }

        void PessoasIlustradas()
        {
            // corredor na frente das torres e da cadeira: o técnico vai e volta ao longo de gx
            float gx = PosicaoAndando(1.3f, 3.2f, 0f, 0.55f, out bool voltando);
            if (tecnicoConsertando)
            {
                var s = CarregarPixelLab("tecnico_nw");
                if (s != null) PessoaIlustrada(s, "tecnico", new Vector2Int(Mathf.RoundToInt(lugarDoTecnico.x), Mathf.RoundToInt(lugarDoTecnico.y)), 0);
            }
            else if (t - ultimaCompraEm < 1.5f)
            {
                // comemora a compra: pulinhos olhando para a frente
                var s = CarregarPixelLab("tecnico_s") ?? CarregarPixelLab("tecnico_se");
                float pulo = Mathf.Abs(Mathf.Sin((t - ultimaCompraEm) * 9f)) * 8f;
                if (s != null) PessoaIlustrada(s, "tecnico", IP(gx, 2.9f), pulo);
            }
            else
            {
                var s = QuadroDaPessoa("tecnico", voltando, true, 0f);
                if (s != null) PessoaIlustrada(s, "tecnico", IP(gx, 2.9f), 0);
            }

            if (E.TemEstagiario)
            {
                // o estagiário anda mais à frente, no sentido contrário
                float gxE = PosicaoAndando(1.5f, 3.0f, 0.9f, 0.45f, out bool volta);
                pontoDoEstagiario = IP(gxE, 3.55f);
                var s = QuadroDaPessoa("estagiario", volta, true, 0.37f);
                if (s != null) PessoaIlustrada(s, "estagiario", pontoDoEstagiario, 0);
            }
        }

        /// <summary>Pessoa com os pés no ponto p (sombra no chão; z levanta o corpo, para os pulos).</summary>
        void PessoaIlustrada(SpriteIso s, string quem, Vector2Int p, float z)
        {
            // os pés pela pose parada: os quadros da caminhada dividem a mesma tela, então a pessoa não pula a cada passo
            int pes = Pes(CarregarPixelLab(quem + "_se") ?? s);
            fila.Add((10 + p.y * 0.001f, () =>
            {
                tela.Ret(p.x - 9, p.y - 1, 18, 3, new Color32(20, 20, 40, 90));
                tela.Imagem(s.px, s.w, s.h, p.x - s.w / 2, p.y - pes - Mathf.RoundToInt(z));
            }));
        }

        // ---------------- Efeitos ----------------

        Vector2Int? LugarDoItemIlustrado(string id)
        {
            switch (id)
            {
                case Catalogo.Servidor: case Catalogo.Ssd: case Catalogo.Ventoinha: case Catalogo.PastaTermica: return pontoDaTorre;
                case Catalogo.FiltroDeLinha: return pontoDoFiltro + new Vector2Int(0, -6);
                case Catalogo.Ventilador: return pontoDoVentilador;
                case Catalogo.Estagiario: return pontoDoEstagiario + new Vector2Int(0, -40);
                default: return null;
            }
        }

        void FaiscasDaCompraIlustrada()
        {
            float idade = t - ultimaCompraEm;
            if (idade > 1.2f || ultimaCompra == null) return;
            var lugar = LugarDoItemIlustrado(ultimaCompra);
            if (lugar == null) return;
            var centro = lugar.Value;
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 0.449f + i * i * 0.07f, raio = 4 + idade * (26 + (i % 4) * 7);
                var p = new Vector2Int(centro.x + Mathf.RoundToInt(Mathf.Cos(ang) * raio), centro.y + Mathf.RoundToInt(Mathf.Sin(ang) * raio * 0.6f - idade * 14));
                var cor = i % 3 == 0 ? IsoDesenho.C("fdf6e3") : IsoDesenho.C("ffd65c");
                cor.a = (byte)(255 * (1 - idade / 1.2f));
                tela.Ret(p.x, p.y, 2, 2, cor);
            }
        }

        /// <summary>O chamado urgente: um papel flutuando sobre o monitor, com o tempo que falta embaixo.</summary>
        void ChamadoIlustrado()
        {
            if (!E.TemChamado) return;
            float bob = Mathf.Sin(t * 4) * 2;
            var p = new Vector2Int(286, Mathf.RoundToInt(112 + bob));
            string[] papel =
            {
                "yyyyyyyyy.", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy", "yWWWWWyyyy", "yyyyyyyyyy", "yWWWWWWWyy", "yyyyyyyyyy",
            };
            tela.Ret(p.x - 7, p.y - 13, 14, 13, IsoDesenho.C("1b1a2e"));
            for (int y = 0; y < papel.Length; y++)
                for (int x = 0; x < papel[y].Length; x++)
                    if (papel[y][x] != '.') tela.Pixel(p.x - 6 + x, p.y - 12 + y, IsoDesenho.C(papel[y][x] == 'y' ? "fdf6e3" : "7d82ad"));
            tela.Ret(p.x + 3, p.y - 12, 2, 2, IsoDesenho.C(Piscar() ? "ff3b4e" : "ff7a8a"));
            float resta = (float)(E.SegundosDoChamado / Catalogo.TempoParaAtender);
            tela.Ret(p.x - 8, p.y + 2, 16, 2, IsoDesenho.C("1b1a2e"));
            tela.Ret(p.x - 8, p.y + 2, Mathf.RoundToInt(16 * resta), 2, IsoDesenho.C(resta > 0.3f ? "ffd65c" : "ff3b4e"));
            Chamado = p;
            Alvos.Add(new Alvo { Area = new RectInt(p.x - 10, p.y - 16, 20, 22), Tipo = "chamado" });
        }
    }
}
