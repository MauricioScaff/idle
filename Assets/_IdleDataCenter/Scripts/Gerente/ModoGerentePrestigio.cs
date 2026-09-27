using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Prestígio no modo gerente: as certificações (bônus permanentes comprados com a moeda de prestígio)
    /// e a venda da empresa, com a escolha do desafio da próxima.
    /// </summary>
    public partial class ModoGerente
    {
        void TelaPrestigio(Rect modal)
        {
            var p = E.Prestigio;
            ui.Texto("Certificações: " + p.certificacoes, modal.x + 24, modal.y + 95, IsoGui.Cor("ffd65c"), 3);
            string historico = p.empresasVendidas == 0 ? "Nenhuma empresa vendida ainda"
                : p.empresasVendidas + (p.empresasVendidas == 1 ? " empresa vendida" : " empresas vendidas") + " / " + p.certificacoesGanhas + " ganhas no total";
            ui.Texto(historico, modal.xMax - 24 - ui.Largura(historico, 2), modal.y + 99, IsoGui.Muted, 2);

            for (int i = 0; i < Catalogo.Certificacoes.Count; i++)
            {
                var c = Catalogo.Certificacoes[i];
                var r = new Rect(modal.x + 28 + i % 3 * 306, modal.y + 124 + i / 3 * 162, 294, 152);
                int nivel = E.NivelCertificacao(c.Id);
                bool maximo = nivel >= c.NivelMaximo;
                ui.Caixa(r, IsoGui.Painel, maximo ? IsoGui.Verde : nivel > 0 ? IsoGui.Cor("ffd65c") : IsoGui.Borda);
                ui.Texto(c.Nome, r.x + 12, r.y + 12, IsoGui.Branco, 2);
                ui.Texto("Nível " + nivel + " / " + c.NivelMaximo, r.x + 12, r.y + 34, IsoGui.Cyan, 2);
                ui.Texto(Cortar(c.Efeito, 34), r.x + 12, r.y + 56, IsoGui.Muted, 2);
                if (!maximo) ui.Texto(c.Custo(nivel) + " certificações", r.x + 12, r.y + 80, E.PodeComprarCertificacao(c.Id) ? IsoGui.Cor("ffd65c") : IsoGui.Muted, 2);
                string rotulo = maximo ? "Completo" : E.PodeComprarCertificacao(c.Id) ? "Estudar" : "Faltam certificações";
                if (ui.Botao(new Rect(r.x + 10, r.yMax - 40, r.width - 20, 32), rotulo, IsoGui.Cor("ffd65c"), E.PodeComprarCertificacao(c.Id)))
                {
                    E.ComprarCertificacao(c.Id);
                    Sons.Promocao();
                    Salvamento.Salvar(E.Estado);
                    Notificar("Certificação: " + c.Nome + " (nível " + E.NivelCertificacao(c.Id) + ").");
                }
            }

            // vender a empresa
            var barra = new Rect(modal.x + 28, modal.y + 454, modal.width - 56, 96);
            ui.Caixa(barra, IsoGui.Cor("2a1f10"), IsoGui.Laranja);
            if (!E.PodeVender)
            {
                ui.Texto("Vender a empresa", barra.x + 16, barra.y + 16, IsoGui.Laranja, 3);
                ui.Texto("Libera no cargo SRE. Quanto mais você faturar, mais certificações. Depois do IPO vale o dobro.", barra.x + 16, barra.y + 52, IsoGui.Muted, 2);
                return;
            }
            ui.Texto("Vender a empresa: +" + E.CertificacoesDaVenda + " certificações", barra.x + 16, barra.y + 16, IsoGui.Laranja, 3);
            ui.Texto("Recomeça no armário com os bônus. " + (E.IpoFeito ? "Depois do IPO: vale o dobro." : "Se fizer o IPO antes, vale o dobro."), barra.x + 16, barra.y + 52, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(barra.xMax - 236, barra.y + 26, 220, 36), "Vender...", IsoGui.Laranja)) janela = "Vender";
        }

        /// <summary>Confirmação da venda: escolher o desafio da próxima empresa (ou nenhum).</summary>
        void TelaVender(Rect modal)
        {
            ui.Texto("Você recebe +" + E.CertificacoesDaVenda + " certificações e um troféu para a mesa.", modal.x + 24, modal.y + 100, IsoGui.Cor("ffd65c"), 2);
            ui.Texto("A empresa nova começa no armário, com todos os bônus. Escolha um desafio (opcional):", modal.x + 24, modal.y + 124, IsoGui.Muted, 2);
            for (int i = 0; i < Catalogo.Desafios.Count; i++)
            {
                var dsf = Catalogo.Desafios[i];
                var r = new Rect(modal.x + 20 + i % 2 * 446, modal.y + 150 + i / 2 * 150, 432, 138);
                ui.Caixa(r, IsoGui.Painel, i == 0 ? IsoGui.Verde : IsoGui.Laranja);
                ui.Texto(dsf.Nome, r.x + 14, r.y + 14, IsoGui.Branco, 3);
                ui.Texto(dsf.Descricao, r.x + 14, r.y + 46, IsoGui.Muted, 2);
                ui.Texto(dsf.Multiplicador > 1 ? "Certificações da próxima venda ×" + dsf.Multiplicador.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "Sem bônus nem regras extras",
                    r.x + 14, r.y + 70, IsoGui.Cor("ffd65c"), 2);
                string id = dsf.Id;
                if (ui.Botao(new Rect(r.x + 12, r.yMax - 40, r.width - 24, 30), "Vender e começar", i == 0 ? IsoGui.Verde : IsoGui.Laranja))
                {
                    faixa.VenderEmpresa(id);
                    Abrir("Visao");
                }
            }
            if (ui.Botao(new Rect(modal.center.x - 90, modal.yMax - 44, 180, 30), "Cancelar", IsoGui.Borda)) janela = "Prestigio";
        }
    }
}
