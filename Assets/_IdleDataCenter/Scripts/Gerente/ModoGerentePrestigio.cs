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
            ui.Texto("CERTIFICACOES: " + p.certificacoes, modal.x + 24, modal.y + 95, IsoGui.Cor("ffd65c"), 3);
            string historico = p.empresasVendidas == 0 ? "NENHUMA EMPRESA VENDIDA AINDA"
                : p.empresasVendidas + (p.empresasVendidas == 1 ? " EMPRESA VENDIDA" : " EMPRESAS VENDIDAS") + " / " + p.certificacoesGanhas + " GANHAS NO TOTAL";
            ui.Texto(historico, modal.xMax - 24 - PixelCanvas.LarguraTexto(historico) * 2, modal.y + 99, IsoGui.Muted, 2);

            for (int i = 0; i < Catalogo.Certificacoes.Count; i++)
            {
                var c = Catalogo.Certificacoes[i];
                var r = new Rect(modal.x + 20 + i % 3 * 298, modal.y + 124 + i / 3 * 138, 284, 128);
                int nivel = E.NivelCertificacao(c.Id);
                bool maximo = nivel >= c.NivelMaximo;
                ui.Caixa(r, IsoGui.Painel, maximo ? IsoGui.Verde : nivel > 0 ? IsoGui.Cor("ffd65c") : IsoGui.Borda);
                ui.Texto(c.Nome.ToUpperInvariant(), r.x + 12, r.y + 12, IsoGui.Branco, 2);
                ui.Texto("NIVEL " + nivel + " / " + c.NivelMaximo, r.x + 12, r.y + 32, IsoGui.Cyan, 1);
                ui.Texto(c.Efeito.ToUpperInvariant(), r.x + 12, r.y + 50, IsoGui.Muted, 1);
                if (!maximo) ui.Texto(c.Custo(nivel) + " CERTIFICACOES", r.x + 12, r.y + 68, E.PodeComprarCertificacao(c.Id) ? IsoGui.Cor("ffd65c") : IsoGui.Muted, 2);
                string rotulo = maximo ? "COMPLETO" : E.PodeComprarCertificacao(c.Id) ? "ESTUDAR" : "FALTAM CERTIFICACOES";
                if (ui.Botao(new Rect(r.x + 10, r.yMax - 34, r.width - 20, 26), rotulo, IsoGui.Cor("ffd65c"), E.PodeComprarCertificacao(c.Id)))
                {
                    E.ComprarCertificacao(c.Id);
                    Sons.Promocao();
                    Salvamento.Salvar(E.Estado);
                    Notificar("Certificação: " + c.Nome + " (nível " + E.NivelCertificacao(c.Id) + ").");
                }
            }

            // vender a empresa
            var barra = new Rect(modal.x + 20, modal.y + 408, modal.width - 40, 88);
            ui.Caixa(barra, IsoGui.Cor("2a1f10"), IsoGui.Laranja);
            if (!E.PodeVender)
            {
                ui.Texto("VENDER A EMPRESA", barra.x + 16, barra.y + 16, IsoGui.Laranja, 3);
                ui.Texto("LIBERA NO CARGO SRE. QUANTO MAIS VOCE FATURAR, MAIS CERTIFICACOES. DEPOIS DO IPO VALE O DOBRO.", barra.x + 16, barra.y + 52, IsoGui.Muted, 1);
                return;
            }
            ui.Texto("VENDER A EMPRESA: +" + E.CertificacoesDaVenda + " CERTIFICACOES", barra.x + 16, barra.y + 16, IsoGui.Laranja, 3);
            ui.Texto("RECOMECA NO ARMARIO COM OS BONUS. " + (E.IpoFeito ? "DEPOIS DO IPO: VALE O DOBRO." : "SE FIZER O IPO ANTES, VALE O DOBRO."), barra.x + 16, barra.y + 52, IsoGui.Muted, 1);
            if (ui.Botao(new Rect(barra.xMax - 236, barra.y + 26, 220, 36), "VENDER...", IsoGui.Laranja)) janela = "Vender";
        }

        /// <summary>Confirmação da venda: escolher o desafio da próxima empresa (ou nenhum).</summary>
        void TelaVender(Rect modal)
        {
            ui.Texto("VOCE RECEBE +" + E.CertificacoesDaVenda + " CERTIFICACOES E UM TROFEU PARA A MESA.", modal.x + 24, modal.y + 100, IsoGui.Cor("ffd65c"), 2);
            ui.Texto("A EMPRESA NOVA COMECA NO ARMARIO, COM TODOS OS BONUS. ESCOLHA UM DESAFIO (OPCIONAL):", modal.x + 24, modal.y + 124, IsoGui.Muted, 2);
            for (int i = 0; i < Catalogo.Desafios.Count; i++)
            {
                var dsf = Catalogo.Desafios[i];
                var r = new Rect(modal.x + 20 + i % 2 * 446, modal.y + 150 + i / 2 * 150, 432, 138);
                ui.Caixa(r, IsoGui.Painel, i == 0 ? IsoGui.Verde : IsoGui.Laranja);
                ui.Texto(dsf.Nome.ToUpperInvariant(), r.x + 14, r.y + 14, IsoGui.Branco, 3);
                ui.Texto(dsf.Descricao.ToUpperInvariant(), r.x + 14, r.y + 46, IsoGui.Muted, 2);
                ui.Texto(dsf.Multiplicador > 1 ? "CERTIFICACOES DA PROXIMA VENDA X" + dsf.Multiplicador.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "SEM BONUS NEM REGRAS EXTRAS",
                    r.x + 14, r.y + 70, IsoGui.Cor("ffd65c"), 1);
                string id = dsf.Id;
                if (ui.Botao(new Rect(r.x + 12, r.yMax - 40, r.width - 24, 30), "VENDER E COMECAR", i == 0 ? IsoGui.Verde : IsoGui.Laranja))
                {
                    faixa.VenderEmpresa(id);
                    Abrir("Visao");
                }
            }
            if (ui.Botao(new Rect(modal.center.x - 90, modal.yMax - 44, 180, 30), "CANCELAR", IsoGui.Borda)) janela = "Prestigio";
        }
    }
}
