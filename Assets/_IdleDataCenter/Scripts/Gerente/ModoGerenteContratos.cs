using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Contratos de clientes na tela do gerente: a proposta aparece no cartão da direita (aceitar ou recusar) e os contratos
    /// ativos num painel à esquerda, embaixo do help desk, com o SLA, o tempo que falta e o bônus.
    /// </summary>
    public partial class ModoGerente
    {
        Rect RetContratos
        {
            get
            {
                int n = E.Contratos.Count;
                if (n == 0) return Rect.zero;
                var hd = RetHelpDesk;
                float y = hd.height > 0 ? hd.yMax + 10 : TopoDoHelpDesk;
                return new Rect(20, y, 330, 38 + n * LinhaDoChamado + 6);
            }
        }

        static string Porcentagem(double sla) => Economia.FormatarUptime(sla);

        void PainelContratos()
        {
            var contratos = E.Contratos;
            if (contratos.Count == 0) return;
            var r = RetContratos;
            ui.Caixa(r, IsoGui.Painel, IsoGui.Borda);
            ui.Texto("Contratos", r.x + 14, r.y + 12, IsoGui.Muted, 2);
            string bonus = "+" + Numero(Mathf.Round((float)((E.FatorContratos - 1) * 100))) + "%";
            ui.Texto(bonus, r.xMax - 14 - ui.Largura(bonus, 2), r.y + 12, IsoGui.Verde, 2);
            for (int i = 0; i < contratos.Count; i++)
            {
                var c = contratos[i];
                var linha = new Rect(r.x + 8, r.y + 34 + i * LinhaDoChamado, r.width - 16, LinhaDoChamado - 4);
                // folga: quanto o uptime está acima do SLA (perto de quebrar fica laranja e pisca)
                bool apertado = E.Uptime < c.sla + 0.0005;
                var cor = apertado ? (Pisca ? Vermelho : IsoGui.Laranja) : IsoGui.Verde;
                ui.Ret(linha, IsoGui.Cor("141d33"));
                ui.Ret(new Rect(linha.x, linha.y, 4, linha.height), cor);
                string prazo = Prazo(c.restante);
                string direita = Porcentagem(c.sla) + "  " + prazo;
                float larguraDireita = ui.Largura(direita, 2);
                ui.Texto(CaberEm(c.cliente, linha.width - 30 - larguraDireita, 2), linha.x + 12, linha.y + 8, IsoGui.Cor("c8d0de"), 2);
                ui.Texto(direita, linha.xMax - 8 - larguraDireita, linha.y + 8, cor, 2);
                ui.Ret(new Rect(linha.x + 4, linha.yMax - 3, (linha.width - 4) * (1 - Mathf.Clamp01((float)(c.restante / c.duracao))), 2), IsoGui.Cyan);
            }
        }

        /// <summary>A proposta no cartão da direita: quem é, o SLA, o prazo e o bônus; aceitar ou recusar.</summary>
        void CartaoProposta(Rect r)
        {
            var p = E.PropostaDeContrato;
            ui.Caixa(r, IsoGui.Cor("2a2410"), Ouro);
            ui.Texto("Proposta de contrato", r.x + 16, r.y + 14, IsoGui.Muted, 2);
            ui.Texto(CaberEm(p.cliente, r.width - 32, 3), r.x + 16, r.y + 36, Ouro, 3);
            ui.Texto("Uptime " + Porcentagem(p.sla) + " por " + Numero(Mathf.Round((float)(p.duracao / 60))) + " min", r.x + 16, r.y + 66, IsoGui.Branco, 2);
            ui.Texto("+" + Numero(Mathf.Round((float)(p.bonus * 100))) + "% de renda; abaixo disso, multa", r.x + 16, r.y + 88, IsoGui.Muted, 2);
            ui.Barra(new Rect(r.x + 16, r.y + 110, r.width - 32, 6), (float)(p.restante / Catalogo.TempoParaAceitar), Ouro);
            float meia = (r.width - 48) / 2;
            if (E.PodeAceitarContrato)
            {
                if (ui.Botao(new Rect(r.x + 16, r.y + 128, meia, 44), "Aceitar", IsoGui.Verde, Livre, 3))
                {
                    E.AceitarContrato();
                    Sons.Compra();
                    Notificar("Contrato com " + p.cliente + ": mantenha o uptime acima de " + Porcentagem(p.sla) + ".");
                }
            }
            else ui.Texto("Já tem 2 contratos", r.x + 16, r.y + 144, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(r.x + 32 + meia, r.y + 128, meia, 44), "Recusar", IsoGui.Borda, Livre, 3)) E.RecusarContrato();
        }
    }
}
