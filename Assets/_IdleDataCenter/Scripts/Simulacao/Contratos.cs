using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Um contrato com um cliente (ou a proposta dele, enquanto não foi aceita). Cliente vazio: nenhum.</summary>
    [Serializable]
    public class Contrato
    {
        public string cliente = "";
        public double sla;          // uptime mínimo (0.99 = 99%)
        public double bonus;        // renda a mais enquanto vale (0.15 = +15%)
        public double duracao;      // segundos de contrato
        public double restante;     // proposta: segundos para aceitar; contrato: segundos até cumprir
    }

    public static partial class Catalogo
    {
        // --- Contratos de clientes (Sysadmin em diante) ---
        public const int CargoDosContratos = 1, ContratosAtivos = 2;
        public const double PrimeiraProposta = 480, IntervaloPropostaMin = 360, IntervaloPropostaMax = 600;
        public const double TempoParaAceitar = 90;
        /// <summary>Cumprir paga 2 min de receita; quebrar (uptime abaixo do SLA) custa 3 min.</summary>
        public const double SegundosDoPremioDoContrato = 120, SegundosDaMultaDoContrato = 180;
        public static readonly double[] DuracoesDeContrato = { 15 * 60, 20 * 60, 30 * 60 };
        /// <summary>Os níveis de SLA que um cliente pede e quanto paga a mais por eles.</summary>
        public static readonly (double sla, double bonus)[] NiveisDeContrato = { (0.98, 0.05), (0.99, 0.08), (0.995, 0.12), (0.999, 0.20), (0.9999, 0.30) };
        public static readonly string[] Clientes =
        {
            "Banco Pixel", "Mercadinho Online", "Hospital São Byte", "Streaming Pipoca", "Prefeitura de Bitópolis", "Loja do Seu Zé",
            "Fintech Cripto Feliz", "Delivery Turbo", "Escola Ctrl+Z", "Rede Social Bolha", "Jogo de Fazendinha", "Cartório Digital",
        };
    }

    /// <summary>
    /// Contratos de clientes: de tempos em tempos um cliente propõe um contrato com SLA (uptime mínimo), duração e bônus de
    /// renda. Aceito, ele paga o bônus enquanto vale; se o uptime ficar abaixo do SLA, o contrato quebra com multa; se chegar
    /// ao fim, paga um prêmio. Até dois ao mesmo tempo. O cliente só pede o que o uptime de agora já entrega.
    /// </summary>
    public partial class Economia
    {
        public event Action<Contrato> ContratoProposto;
        /// <summary>Contrato encerrado: cumprido (valor &gt; 0, o prêmio) ou quebrado (valor &lt; 0, a multa).</summary>
        public event Action<Contrato, double> ContratoEncerrado;

        public bool TemPropostaDeContrato => !string.IsNullOrEmpty(Estado.proposta?.cliente);
        public Contrato PropostaDeContrato => TemPropostaDeContrato ? Estado.proposta : null;
        public List<Contrato> Contratos => Estado.contratos;
        public bool PodeAceitarContrato => TemPropostaDeContrato && Estado.contratos.Count < Catalogo.ContratosAtivos;

        public double FatorContratos
        {
            get
            {
                double f = 1;
                foreach (var c in Estado.contratos) f += c.bonus;
                return f;
            }
        }

        void AvancarContratos(double segundos)
        {
            if (Estado.proposta == null) Estado.proposta = new Contrato();
            // os contratos ativos: quebram se o uptime cair abaixo do SLA, pagam o prêmio no fim
            for (int i = Estado.contratos.Count - 1; i >= 0; i--)
            {
                var c = Estado.contratos[i];
                if (Uptime < c.sla)
                {
                    double multa = Math.Min(Estado.dinheiro, ReceitaPorSegundo * Catalogo.SegundosDaMultaDoContrato);
                    Estado.contratos.RemoveAt(i);
                    Estado.dinheiro -= multa;
                    ContratoEncerrado?.Invoke(c, -multa);
                    continue;
                }
                c.restante -= segundos;
                if (c.restante > 0) continue;
                Estado.contratos.RemoveAt(i);
                double premio = ReceitaPorSegundo * Catalogo.SegundosDoPremioDoContrato;
                Ganhar(premio);
                Estado.contratosCumpridos++;
                ContratoEncerrado?.Invoke(c, premio);
            }

            // a proposta na mesa vence se ninguém responde
            if (TemPropostaDeContrato)
            {
                Estado.proposta.restante -= segundos;
                if (Estado.proposta.restante <= 0) RecusarContrato();
                return;
            }
            if (Estado.cargo < Catalogo.CargoDosContratos || Estado.contratos.Count >= Catalogo.ContratosAtivos) return;
            if (Estado.proximaProposta < 0) Estado.proximaProposta = Catalogo.PrimeiraProposta;
            Estado.proximaProposta -= segundos;
            if (Estado.proximaProposta <= 0) ProporContrato();
        }

        /// <summary>Um cliente propõe um contrato que o uptime de agora entrega (o SLA mais alto possível, às vezes um abaixo).</summary>
        public Contrato ProporContrato()
        {
            if (Estado.proposta == null) Estado.proposta = new Contrato();
            Estado.proximaProposta = Catalogo.IntervaloPropostaMin + sorteio.NextDouble() * (Catalogo.IntervaloPropostaMax - Catalogo.IntervaloPropostaMin);
            int maior = -1;
            for (int i = 0; i < Catalogo.NiveisDeContrato.Length; i++)
                if (Uptime >= Catalogo.NiveisDeContrato[i].sla + 0.0005) maior = i;
            if (maior < 0) return null;   // nem 98%: ninguém quer contrato agora
            int nivel = Math.Max(0, maior - (sorteio.NextDouble() < 0.4 ? 1 : 0));
            var n = Catalogo.NiveisDeContrato[nivel];
            Estado.proposta = new Contrato
            {
                cliente = Catalogo.Clientes[sorteio.Next(Catalogo.Clientes.Length)],
                sla = n.sla, bonus = n.bonus,
                duracao = Catalogo.DuracoesDeContrato[sorteio.Next(Catalogo.DuracoesDeContrato.Length)],
                restante = Catalogo.TempoParaAceitar,
            };
            ContratoProposto?.Invoke(Estado.proposta);
            return Estado.proposta;
        }

        public bool AceitarContrato()
        {
            if (!PodeAceitarContrato) return false;
            var p = Estado.proposta;
            Estado.contratos.Add(new Contrato { cliente = p.cliente, sla = p.sla, bonus = p.bonus, duracao = p.duracao, restante = p.duracao });
            Estado.proposta = new Contrato();
            return true;
        }

        public void RecusarContrato()
        {
            Estado.proposta = new Contrato();
            if (Estado.proximaProposta <= 0) Estado.proximaProposta = Catalogo.IntervaloPropostaMin;
        }

        /// <summary>Com o jogo fechado ninguém responde proposta: ela some (os contratos ficam parados).</summary>
        void LimparPropostaOffline() => Estado.proposta = new Contrato();
    }
}
