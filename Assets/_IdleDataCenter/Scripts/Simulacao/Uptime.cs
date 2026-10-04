using System;

namespace IdleDataCenter.Simulacao
{
    public static partial class Catalogo
    {
        /// <summary>O uptime é a média da disponibilidade na última hora de jogo (média móvel exponencial).</summary>
        public const double JanelaDoUptime = 3600;

        /// <summary>Os "noves" que pagam a mais: (uptime mínimo, nome do SLA, bônus de receita).</summary>
        public static readonly (double minimo, string nome, double bonus)[] NiveisDeSla =
        {
            (0.99999, "cinco noves", 0.20),
            (0.9999, "quatro noves", 0.10),
            (0.999, "três noves", 0.05),
        };
    }

    /// <summary>
    /// Uptime e SLA: o quanto do tempo o serviço esteve no ar. Cada incidente tira uma parte da disponibilidade enquanto
    /// dura (servidor travado, disco queimado, deploy quebrado, queda de energia, pane regional, ataque, internet caída);
    /// o uptime é a média disso na última hora. Três noves ou mais pagam um bônus de receita, como um contrato de SLA.
    /// </summary>
    public partial class Economia
    {
        /// <summary>Uptime da última hora (0 a 1). Uma empresa nova começa em 99%: os noves se conquistam.</summary>
        public double Uptime => Estado.uptime;

        /// <summary>Quanto do serviço está fora do ar agora (0 a 1).</summary>
        public double Indisponibilidade
        {
            get
            {
                double fora = (double)Estado.travamentos.Count / Math.Max(1, ContagemServidores);
                if (DiscoQueimado) fora += 0.01;     // RAID degradado: o storage segue no ar, mais lento
                if (DeployQuebrado) fora += 0.02;    // um serviço fora até o rollback, não a empresa toda
                if (TemQuedaDeEnergia) fora += 1.0 / (1 + DatacentersExtras);
                if (TemPaneRegional) fora += 1.0 / (1 + RegioesExtras);
                switch (Estado.evento)
                {
                    case Catalogo.EventoInternet: fora += Estado.eventoFase == 1 ? 0.2 : 0.5; break;
                    case Catalogo.AtaqueDdos: fora += Estado.eventoFase == 1 ? 0.15 : 0.5; break;
                    case Catalogo.AtaqueMalware: fora += 0.3; break;
                    case Catalogo.AtaqueRansomware: fora += 0.8; break;
                    case Catalogo.EventoSsl: fora += 0.4; break;
                    case Catalogo.EventoDns: fora += 0.5; break;
                    case Catalogo.EventoFaxineira: fora += 0.3; break;
                }
                return Math.Min(1, fora);
            }
        }

        /// <summary>O SLA que o uptime atual sustenta (null: abaixo de três noves).</summary>
        public (double minimo, string nome, double bonus)? Sla
        {
            get
            {
                foreach (var n in Catalogo.NiveisDeSla) if (Estado.uptime >= n.minimo) return n;
                return null;
            }
        }

        public double FatorSla => 1 + (Sla?.bonus ?? 0);

        void AvancarUptime(double segundos)
        {
            double alvo = 1 - Indisponibilidade;
            Estado.uptime += (alvo - Estado.uptime) * (1 - Math.Exp(-segundos / Catalogo.JanelaDoUptime));
        }

        /// <summary>Uptime como em TI: "99,95%" (mais casas quando está perto de 100%).</summary>
        public static string FormatarUptime(double u)
        {
            double p = u * 100;
            string s = u >= 0.9999 ? p.ToString("0.000") : u >= 0.99 ? p.ToString("0.00") : p.ToString("0.0");
            return s.Replace('.', ',') + "%";
        }
    }
}
