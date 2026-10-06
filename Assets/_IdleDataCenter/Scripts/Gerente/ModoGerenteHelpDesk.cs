using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// O painel do help desk, à esquerda, embaixo do dinheiro: a fila de chamados (P1 vermelho a P4 cinza) com o prazo de
    /// cada um. Clicar numa linha atende aquele chamado. O chamado em que a equipe está trabalhando mostra o progresso.
    /// </summary>
    public partial class ModoGerente
    {
        const float LinhaDoChamado = 30, TopoDoHelpDesk = 140;

        /// <summary>Onde o painel fica (vazio: sem chamados). A sala não recebe cliques embaixo dele.</summary>
        Rect RetHelpDesk
        {
            get
            {
                int n = E.Chamados.Count;
                return n == 0 ? Rect.zero : new Rect(20, TopoDoHelpDesk, 330, 38 + n * LinhaDoChamado + 6);
            }
        }

        static Color CorDaPrioridade(int p) =>
            p == 1 ? IsoGui.Cor("ff5a6a") : p == 2 ? IsoGui.Cor("ff9a3a") : p == 3 ? IsoGui.Cor("ffd65c") : IsoGui.Cor("8a93a6");

        void PainelHelpDesk()
        {
            var chamados = E.Chamados;
            if (chamados.Count == 0) return;
            var r = RetHelpDesk;
            bool p1 = chamados[0].prioridade == 1;
            ui.Caixa(r, IsoGui.Painel, p1 && Pisca ? CorDaPrioridade(1) : IsoGui.Borda);
            ui.Texto(E.Cargo == Catalogo.CargoFreelancer ? "Bancada" : "Help desk", r.x + 14, r.y + 12, IsoGui.Muted, 2);
            string fila = chamados.Count + "/" + Catalogo.ChamadosNaFila;
            ui.Texto(fila, r.xMax - 14 - ui.Largura(fila, 2), r.y + 12, IsoGui.Muted, 2);

            var equipe = E.ChamadoDaEquipe;
            var mouse = Event.current.mousePosition;
            string dicaDoPainel = null;
            for (int i = 0; i < chamados.Count; i++)
            {
                var c = chamados[i];
                var linha = new Rect(r.x + 8, r.y + 34 + i * LinhaDoChamado, r.width - 16, LinhaDoChamado - 4);
                var cor = CorDaPrioridade(c.prioridade);
                bool sobre = Livre && linha.Contains(mouse);
                ui.Ret(linha, sobre ? IsoGui.Cor("23314f") : IsoGui.Cor("141d33"));
                ui.Ret(new Rect(linha.x, linha.y, 4, linha.height), cor);
                ui.Texto("P" + c.prioridade, linha.x + 12, linha.y + 8, cor, 2);
                string prazo = Prazo(c.restante);
                float larguraPrazo = ui.Largura(prazo, 2);
                ui.Texto(CaberEm(c.texto, linha.width - 60 - larguraPrazo, 2), linha.x + 42, linha.y + 8, sobre ? IsoGui.Branco : IsoGui.Cor("c8d0de"), 2);
                ui.Texto(prazo, linha.xMax - 8 - larguraPrazo, linha.y + 8, c.restante < 10 && Pisca ? CorDaPrioridade(1) : IsoGui.Muted, 2);
                // a equipe trabalhando nele: barrinha embaixo
                if (c == equipe) ui.Ret(new Rect(linha.x + 4, linha.yMax - 3, (linha.width - 4) * Mathf.Clamp01((float)E.ProgressoDaEquipe), 2), IsoGui.Verde);
                if (sobre) dicaDoPainel = "Atender: paga " + Dinheiro(E.BonusDe(c.prioridade)) + (c == equipe ? " (a equipe já está nele)" : "");
                if (Livre && GUI.Button(linha, GUIContent.none, GUIStyle.none)) { AtenderChamado(new Vector2(linha.center.x, linha.y), i); Sons.Moeda(); }
            }
            if (dicaDoPainel != null)
            {
                float largura = ui.Largura(dicaDoPainel, 2) + 20;
                var d = new Rect(mouse.x + 18, mouse.y + 20, largura, 28);
                ui.Caixa(d, IsoGui.Cor("0d1426"), IsoGui.Cyan);
                ui.Texto(dicaDoPainel, d.x + 10, d.y + 10, IsoGui.Branco, 2);
            }
        }

        static string Prazo(double s)
        {
            int t = Mathf.CeilToInt((float)s);
            return t >= 60 ? t / 60 + ":" + (t % 60).ToString("00") : t + "s";
        }
    }
}
