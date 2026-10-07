using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// O dicionário: no menu, uma aba por cargo com os termos de TI explicados para quem não é da área (os cargos que
    /// ainda não chegaram ficam com cadeado); no jogo, o mouse num termo sublinhado mostra a explicação curta.
    /// </summary>
    public partial class ModoGerente
    {
        int abaDoDicionario;

        /// <summary>Nomes curtos dos cargos para as abas (os completos não cabem os oito numa linha).</summary>
        static readonly string[] CargosCurtos = { "Freelancer", "Técnico", "Sysadmin", "Analista", "DevOps", "SRE", "Arquiteto", "CTO" };

        static string NoJogo(Glossario.Termo t) => Idiomas.T("No jogo:") + " " + Idiomas.T(t.NoJogo);

        bool CargoLiberado(int cargo) => cargo <= E.Cargo || E.IpoFeito;

        void AbrirDicionario()
        {
            abaDoDicionario = Mathf.Clamp(E.Cargo, 0, CargosCurtos.Length - 1);
            menu = TelaDoMenu.Dicionario;
        }

        /// <summary>A explicação do termo sublinhado sob o cursor, perto dele (por cima de tudo).</summary>
        void DicaDoDicionario()
        {
            if (Event.current.type != EventType.Repaint) return;
            var mouse = Event.current.mousePosition;
            Glossario.Termo termo = null;
            for (int i = ui.TermosNaTela.Count - 1; i >= 0 && termo == null; i--)   // o último desenhado é o de cima
                if (ui.TermosNaTela[i].area.Contains(mouse)) termo = ui.TermosNaTela[i].termo;
            if (termo == null) return;

            ui.Dicionario = false;   // nada sublinhado dentro da própria dica
            const float largura = 460;
            float texto = largura - 32;
            float altura = 16 + 10 + 12 + ui.Paragrafo(termo.OQueE, 0, 0, texto, IsoGui.Branco, 2, 6, false) + 6
                           + ui.Paragrafo(NoJogo(termo), 0, 0, texto, IsoGui.Muted, 2, 6, false) + 10;
            float x = Mathf.Clamp(mouse.x + 16, 8, W - largura - 8), y = mouse.y + 24;
            if (y + altura > H - 8) y = Mathf.Max(8, mouse.y - 12 - altura);
            var r = new Rect(x, y, largura, altura);
            ui.Caixa(r, IsoGui.Cor("0d1426"), IsoGui.Cyan);
            ui.Texto(termo.Nome, r.x + 16, r.y + 16, IsoGui.Cyan, 2);
            float yy = r.y + 16 + 10 + 12;
            yy += ui.Paragrafo(termo.OQueE, r.x + 16, yy, texto, IsoGui.Branco) + 6;
            ui.Paragrafo(NoJogo(termo), r.x + 16, yy, texto, IsoGui.Muted);
            ui.Dicionario = true;
        }

        /// <summary>A tela do dicionário (no menu): abas por cargo e os termos em duas colunas.</summary>
        void Dicionario()
        {
            var caixa = new Rect(W / 2 - 600, 50, 1200, 800);
            ui.Caixa(caixa, IsoGui.Painel, IsoGui.Cyan);
            ui.Texto("Dicionário", caixa.x + 28, caixa.y + 22, IsoGui.Branco, 4);
            ui.Texto("Os termos de TI do jogo, explicados para quem não é da área.", caixa.x + 28, caixa.y + 64, IsoGui.Muted, 2);

            // abas: uma por cargo, com cadeado nas que ainda não chegaram
            float ax = caixa.x + 28;
            for (int c = 0; c < CargosCurtos.Length; c++)
            {
                bool liberado = CargoLiberado(c);
                float largura = ui.Largura(CargosCurtos[c], 2) + (liberado ? 30 : 46);
                var r = new Rect(ax, caixa.y + 94, largura, 38);
                if (ui.Botao(r, liberado ? CargosCurtos[c] : "", abaDoDicionario == c ? IsoGui.Cyan : IsoGui.Borda, true)) abaDoDicionario = c;
                if (!liberado)
                {
                    Mapa(Cadeado, r.x + 12, r.center.y - 8, IsoGui.Muted, 3);
                    ui.Texto(CargosCurtos[c], r.x + 34, r.center.y - 5, IsoGui.Muted, 2);
                }
                ax += largura + 8;
            }

            float topo = caixa.y + 156, fundo = caixa.yMax - 84;
            if (!CargoLiberado(abaDoDicionario))
            {
                Mapa(Cadeado, caixa.center.x - 15, topo + 120, IsoGui.Muted, 6);
                ui.Texto("Estes termos aparecem quando você chegar a " + Catalogo.Cargos[abaDoDicionario].Nome + ".", caixa.center.x, topo + 170, IsoGui.Muted, 2, true);
            }
            else
            {
                // duas colunas: enche a primeira e passa para a segunda
                const float coluna = 548;
                float x = caixa.x + 32, y = topo;
                foreach (var t in Glossario.DoCargo(abaDoDicionario))
                {
                    float altura = 10 + 10 + ui.Paragrafo(t.OQueE, 0, 0, coluna, IsoGui.Branco, 2, 6, false) + 4
                                   + ui.Paragrafo(NoJogo(t), 0, 0, coluna, IsoGui.Muted, 2, 6, false);
                    if (y + altura > fundo && x < caixa.center.x) { x = caixa.center.x + 12; y = topo; }
                    ui.Texto(t.Nome, x, y, IsoGui.Cyan, 2);
                    float yy = y + 20;
                    yy += ui.Paragrafo(t.OQueE, x, yy, coluna, IsoGui.Branco) + 4;
                    ui.Paragrafo(NoJogo(t), x, yy, coluna, IsoGui.Muted);
                    y += altura + 20;
                }
            }
            ui.Texto("No jogo, os termos sublinhados mostram a explicação ao passar o mouse.", caixa.x + 32, caixa.yMax - 50, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(caixa.xMax - 252, caixa.yMax - 66, 220, 44), "Voltar", IsoGui.Verde, true, 2)) menu = TelaDoMenu.Inicio;
        }
    }
}
