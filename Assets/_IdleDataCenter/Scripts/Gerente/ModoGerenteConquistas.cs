using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>A aba Conquistas da carreira: todas as conquistas, as feitas em verde, e o bônus de renda que elas dão.</summary>
    public partial class ModoGerente
    {
        void TelaConquistas(Rect modal)
        {
            int feitas = E.NumeroDeConquistas, total = Catalogo.Conquistas.Count;
            ui.Texto("Conquistas: " + feitas + " / " + total, modal.x + 24, modal.y + 95, Ouro, 3);
            string bonus = "+" + ((E.FatorConquistas - 1) * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') + "% de renda, para sempre";
            ui.Texto(bonus, modal.xMax - 24 - ui.Largura(bonus, 2), modal.y + 99, IsoGui.Verde, 2);

            const int colunas = 4;
            float largura = (modal.width - 56 - (colunas - 1) * 8) / colunas, altura = 50;
            for (int i = 0; i < total; i++)
            {
                var c = Catalogo.Conquistas[i];
                bool tem = E.TemConquista(c.Id);
                var r = new Rect(modal.x + 28 + i % colunas * (largura + 8), modal.y + 124 + i / colunas * (altura + 6), largura, altura);
                ui.Caixa(r, tem ? IsoGui.Cor("173a2e") : IsoGui.Painel, tem ? IsoGui.Verde : IsoGui.Borda);
                ui.Texto(CaberEm(c.Nome, r.width - 24, 2), r.x + 12, r.y + 9, tem ? Ouro : IsoGui.Muted, 2);
                ui.Texto(CaberEm(c.Descricao, r.width - 24, 2), r.x + 12, r.y + 29, tem ? IsoGui.Branco : IsoGui.Cor("5a6478"), 2);
            }
        }
    }
}
