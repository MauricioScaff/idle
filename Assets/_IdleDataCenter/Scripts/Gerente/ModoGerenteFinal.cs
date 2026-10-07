using System.Globalization;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A tela de final, no IPO: o sino da bolsa e fogos por cima do mapa, o resumo da carreira (números e quando chegou a
    /// cada cargo) e duas saídas: continuar como CEO ou vender a empresa e recomeçar com o prestígio. Os créditos abrem
    /// daqui e também do menu.
    /// </summary>
    public partial class ModoGerente
    {
        bool finalAberto, creditosNoFinal;
        float finalDesde;

        /// <summary>Abre a tela de final (uma vez por empresa: depois, o resumo fica na carreira).</summary>
        void AbrirFinal()
        {
            finalAberto = true;
            creditosNoFinal = false;
            finalDesde = Time.unscaledTime;
            janela = "";
            E.Estado.finalVisto = true;
        }

        static string Duracao(double s) => s < 0 ? "-" : (int)(s / 3600) + "h" + ((int)(s % 3600 / 60)).ToString("00", CultureInfo.InvariantCulture);

        /// <summary>O sino da bolsa (pixels): a boca embaixo, o badalo e a alça.</summary>
        static readonly string[] Sino =
        {
            "....##....",
            "...####...",
            "..######..",
            ".########.",
            ".########.",
            ".########.",
            "##########",
            "##########",
            "....##....",
        };

        void TelaFinal()
        {
            if (!finalAberto) return;
            float t = Time.unscaledTime - finalDesde;
            ui.Dicionario = false;
            ui.Ret(new Rect(-200, -200, W + 400, H + 400), new Color(.02f, .03f, .07f, .8f));
            Fogos(t);
            if (creditosNoFinal) { Creditos(() => creditosNoFinal = false); return; }

            // o sino balançando e o "IPO!"
            float balanco = Mathf.Sin(t * 6) * Mathf.Max(0, 3 - t) * 4;
            Mapa(Sino, W / 2 - 30 + balanco, 34, Ouro, 6);
            if (t < 3 && Mathf.Sin(t * 12) > 0) ui.Texto("DING!", W / 2 + 60, 44, Ouro, 3);
            ui.Texto("IPO!", W / 2, 106, Ouro, 8, true);
            ui.Texto("A empresa abriu o capital. Você é o CEO.", W / 2, 166, IsoGui.Branco, 3, true);
            ui.Texto("De freelancer no quarto a CEO de uma nuvem global", W / 2, 198, IsoGui.Muted, 2, true);

            // o resumo: os números à esquerda, a carreira à direita
            var caixa = new Rect(W / 2 - 540, 236, 1080, 470);
            ui.Caixa(caixa, IsoGui.Painel, Ouro);
            ui.Texto("A sua carreira", caixa.x + 32, caixa.y + 24, Ouro, 3);
            var s = E.Estado;
            var numeros = new (string nome, string valor)[]
            {
                (s.tempoCompleto ? "Tempo de jogo" : "Tempo de jogo (desde a atualização)", Duracao(s.segundosJogados)),
                ("Faturamento total", Dinheiro(s.totalGanho)),
                ("Incidentes resolvidos", Numero(s.incidentesResolvidos)),
                ("Chamados atendidos", Numero(s.chamadosAtendidos)),
                ("Backups restaurados", Numero(s.backupsRestaurados)),
                ("Picos de tráfego superados", Numero(s.picosSobrevividos)),
                ("Problemas resolvidos no terminal", Numero(s.comandosCertos)),
                ("Conquistas", E.NumeroDeConquistas + " / " + Catalogo.Conquistas.Count),
            };
            float y = caixa.y + 76;
            foreach (var (nome, valor) in numeros)
            {
                ui.Texto(nome, caixa.x + 32, y, IsoGui.Muted, 2);
                ui.Texto(valor, caixa.x + 500 - ui.Largura(valor, 2), y, IsoGui.Branco, 2);
                y += 40;
            }

            float cx = caixa.x + 580;
            ui.Texto("Quando chegou a cada cargo", cx, caixa.y + 76, IsoGui.Muted, 2);
            y = caixa.y + 110;
            for (int c = 0; c < Catalogo.Cargos.Count; c++)
            {
                double em = s.chegouNoCargoEm != null && c < s.chegouNoCargoEm.Count ? s.chegouNoCargoEm[c] : -1;
                ui.Texto(Catalogo.Cargos[c].Nome, cx, y, IsoGui.Branco, 2);
                string quando = Duracao(em);
                ui.Texto(quando, caixa.xMax - 36 - ui.Largura(quando, 2), y, IsoGui.Cyan, 2);
                y += 36;
            }
            ui.Texto("CEO (IPO)", cx, y, Ouro, 2);
            string ipo = Duracao(s.ipoEm);
            ui.Texto(ipo, caixa.xMax - 36 - ui.Largura(ipo, 2), y, Ouro, 2);

            // as saídas
            float by = caixa.yMax + 28;
            if (ui.Botao(new Rect(W / 2 - 520, by, 340, 56), "Continuar como CEO", IsoGui.Verde, true, 3)) finalAberto = false;
            if (ui.Botao(new Rect(W / 2 - 160, by, 400, 56), "Vender e recomeçar", IsoGui.Roxo, true, 3)) { finalAberto = false; Abrir("Vender"); }
            if (ui.Botao(new Rect(W / 2 + 260, by, 260, 56), "Créditos", IsoGui.Borda, true, 3)) creditosNoFinal = true;
            ui.Texto("Vendendo depois do IPO, as certificações valem o dobro.", W / 2, by + 74, IsoGui.Muted, 2, true);
            ui.Dicionario = true;
        }

        /// <summary>Fogos: estouros em pontos sorteados (sempre os mesmos para o mesmo instante), que abrem e somem.</summary>
        void Fogos(float t)
        {
            var cores = new[] { Ouro, IsoGui.Cyan, IsoGui.Verde, IsoGui.Roxo, IsoGui.Laranja };
            const float periodo = 1.4f;
            for (int k = 0; k < 4; k++)
            {
                float fase = t + k * periodo / 4;
                int rodada = Mathf.FloorToInt(fase / periodo);
                float idade = fase - rodada * periodo;
                var sorte = new System.Random(rodada * 7 + k * 131);
                var centro = new Vector2(80 + (float)sorte.NextDouble() * (W - 160), 40 + (float)sorte.NextDouble() * 260);
                var cor = cores[sorte.Next(cores.Length)];
                cor.a = Mathf.Clamp01(1.2f - idade / periodo * 1.4f);
                float raio = 20 + idade * 110;
                for (int i = 0; i < 18; i++)
                {
                    float a = i * Mathf.PI * 2 / 18;
                    var p = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a) + idade * 0.3f) * raio;
                    ui.Ret(new Rect(Mathf.Round(p.x), Mathf.Round(p.y), 6, 6), cor);
                }
            }
        }

        /// <summary>Os créditos (no menu e na tela de final).</summary>
        void Creditos(System.Action voltar)
        {
            ui.Dicionario = false;
            var caixa = new Rect(W / 2 - 400, 150, 800, 600);
            ui.Caixa(caixa, IsoGui.Painel, Ouro);
            float y = caixa.y + 40;
            ui.Texto("IDLE DATA CENTER", W / 2, y, Ouro, 5, true);
            y += 70;
            ui.Texto("Feito por", W / 2, y, IsoGui.Muted, 2, true);
            ui.Texto("Mauricio Romeo Scaff Filho", W / 2, y + 30, IsoGui.Branco, 3, true);
            ui.Texto("Felipe Kesrouani Borges", W / 2, y + 66, IsoGui.Branco, 3, true);
            y += 140;
            foreach (var (titulo, texto) in new[]
            {
                ("Arte", "Gerada com o PixelLab"),
                ("Fonte", "Silkscreen, de Jason Kottke (SIL Open Font License)"),
                ("Motor", "Unity"),
            })
            {
                ui.Texto(titulo, W / 2, y, IsoGui.Muted, 2, true);
                ui.Texto(texto, W / 2, y + 26, IsoGui.Branco, 2, true);
                y += 70;
            }
            ui.Texto("Obrigado por jogar!", W / 2, y + 10, IsoGui.Cyan, 3, true);
            if (ui.Botao(new Rect(W / 2 - 110, caixa.yMax - 70, 220, 44), "Voltar", IsoGui.Verde, true, 2)) voltar();
            ui.Dicionario = true;
        }
    }
}
