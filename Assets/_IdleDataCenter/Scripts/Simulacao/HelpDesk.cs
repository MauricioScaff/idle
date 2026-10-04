using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Um chamado na fila do help desk.</summary>
    [Serializable]
    public class ChamadoAberto
    {
        public string texto;
        public int prioridade;     // 1 (o site caiu) a 4 (trocar o papel de parede)
        public double restante;    // segundos até estourar o prazo
    }

    public static partial class Catalogo
    {
        public const string HelpDesk = "helpdesk", ServiceDesk = "servicedesk";

        // --- Help desk: fila de chamados P1 a P4 ---
        public const double PrimeiroChamado = 90, IntervaloChamadoMin = 45, IntervaloChamadoMax = 120;
        public const int ChamadosNaFila = 5;
        /// <summary>Prazo e pagamento (segundos de receita) por prioridade, de P1 a P4.</summary>
        public static readonly double[] PrazoDoChamado = { 30, 60, 120, 300 };
        public static readonly double[] BonusDoChamadoEmSegundos = { 60, 30, 12, 6 };
        /// <summary>Chamado fechado pela equipe paga só uma parte (o resto é o salário dela).</summary>
        public const double FracaoDaEquipe = 0.5;
        /// <summary>P1 que estoura o prazo: o serviço ficou fora esse tanto (conta no uptime).</summary>
        public const double SegundosForaPorP1 = 5;
        /// <summary>Quanto a equipe (estagiário, help desk) leva para fechar um chamado sozinha.</summary>
        public const double TempoDaEquipeNoChamado = 20;

        public static readonly string[][] TextosDosChamados =
        {
            new[] { "O site caiu!", "Banco de dados fora do ar", "Ninguém consegue logar", "Pagamentos falhando", "Certificado expirou em produção" },
            new[] { "Sistema lento", "E-mail não chega", "VPN caindo toda hora", "Relatório do diretor travou", "Backup de ontem falhou" },
            new[] { "Impressora não imprime", "Esqueci a senha", "Planilha travou", "Senha do Wi-Fi", "Instalar o Office", "É sempre o DNS" },
            new[] { "Trocar o papel de parede", "Mouse sem pilha", "Cadeira rangendo", "Quero um monitor maior", "Migalha no teclado", "Pode olhar meu celular?" },
        };
    }

    /// <summary>
    /// Help desk: os chamados chegam numa fila (até 5) com prioridade. P1 é o site fora do ar: 30 s de prazo, paga 1 min de
    /// receita e, se estourar, conta como serviço fora no uptime. P4 é o papel de parede: 5 min, paga pouco. O estagiário
    /// fecha os P4 sozinho; o analista de help desk, os P3 e P4; o service desk 24h, também os P2. P1 é sempre com você.
    /// </summary>
    public partial class Economia
    {
        /// <summary>Chamado novo na fila.</summary>
        public event Action<ChamadoAberto> ChamadoApareceu;
        /// <summary>Chamado fechado: atendido (bônus &gt; 0), resolvido pela equipe (porEquipe) ou estourou o prazo (bônus 0).</summary>
        public event Action<ChamadoAberto, double, bool> ChamadoEncerrado;

        /// <summary>A fila, do mais urgente (P1, menos tempo) para o menos.</summary>
        public List<ChamadoAberto> Chamados
        {
            get
            {
                MigrarChamadoAntigo();
                var l = new List<ChamadoAberto>(Estado.chamados);
                l.Sort((a, b) => a.prioridade != b.prioridade ? a.prioridade.CompareTo(b.prioridade) : a.restante.CompareTo(b.restante));
                return l;
            }
        }

        ChamadoAberto MaisUrgente { get { var l = Chamados; return l.Count > 0 ? l[0] : null; } }

        public bool TemChamado => Estado.chamados.Count > 0 || Estado.chamadoRestante > 0;
        public string TextoDoChamado => MaisUrgente?.texto ?? "";
        public int PrioridadeDoChamado => MaisUrgente?.prioridade ?? 0;
        public double SegundosDoChamado => MaisUrgente?.restante ?? 0;
        /// <summary>Quanto ainda falta do prazo do chamado mais urgente (1 = acabou de chegar).</summary>
        public double FracaoDoPrazoDoChamado => MaisUrgente == null ? 0 : MaisUrgente.restante / Catalogo.PrazoDoChamado[MaisUrgente.prioridade - 1];
        /// <summary>Chamado P1 ou P2 esperando: é o que vale um alerta.</summary>
        public bool TemChamadoUrgente => MaisUrgente != null && MaisUrgente.prioridade <= 2;
        public double BonusDoChamado => BonusDe(PrioridadeDoChamado == 0 ? 3 : PrioridadeDoChamado);
        public double BonusDe(int prioridade) => Math.Max(25, ReceitaPorSegundo * Catalogo.BonusDoChamadoEmSegundos[prioridade - 1]);

        /// <summary>A pior prioridade que a equipe fecha sozinha (5: nenhuma).</summary>
        public int EquipeFechaAte => Nivel(Catalogo.ServiceDesk) > 0 ? 2 : Nivel(Catalogo.HelpDesk) > 0 ? 3 : TemEstagiario ? 4 : 5;

        /// <summary>O chamado em que a equipe está trabalhando agora (ou null) e quanto falta (0 a 1).</summary>
        public ChamadoAberto ChamadoDaEquipe { get { foreach (var c in Chamados) if (c.prioridade >= EquipeFechaAte) return c; return null; } }
        public double ProgressoDaEquipe => Estado.helpDeskTrabalho / Catalogo.TempoDaEquipeNoChamado;

        /// <summary>Saves de antes da fila tinham um chamado só: vira um P3.</summary>
        void MigrarChamadoAntigo()
        {
            if (Estado.chamadoRestante <= 0) return;
            if (Estado.chamados.Count < Catalogo.ChamadosNaFila)
                Estado.chamados.Add(new ChamadoAberto { texto = Estado.chamadoTexto, prioridade = 3, restante = Catalogo.PrazoDoChamado[2] });
            Estado.chamadoRestante = 0;
            Estado.chamadoTexto = "";
        }

        void AvancarChamados(double segundos)
        {
            MigrarChamadoAntigo();
            // os prazos correm; P1 que estoura é serviço fora do ar
            for (int i = Estado.chamados.Count - 1; i >= 0; i--)
            {
                var c = Estado.chamados[i];
                c.restante -= segundos;
                if (c.restante > 0) continue;
                Estado.chamados.RemoveAt(i);
                if (c.prioridade == 1) Estado.uptime *= Math.Exp(-Catalogo.SegundosForaPorP1 / Catalogo.JanelaDoUptime);
                ChamadoEncerrado?.Invoke(c, 0, false);
            }

            // a equipe fecha sozinha o que é da alçada dela, um de cada vez (o mais urgente que ela pode)
            var daEquipe = ChamadoDaEquipe;
            if (daEquipe == null) Estado.helpDeskTrabalho = 0;
            else
            {
                Estado.helpDeskTrabalho += segundos;
                if (Estado.helpDeskTrabalho >= Catalogo.TempoDaEquipeNoChamado)
                {
                    Estado.helpDeskTrabalho = 0;
                    Fechar(daEquipe, porEquipe: true);
                }
            }

            if (Estado.proximoChamado < 0) Estado.proximoChamado = Catalogo.PrimeiroChamado;
            Estado.proximoChamado -= segundos;
            if (Estado.proximoChamado <= 0)
            {
                Estado.proximoChamado = Catalogo.IntervaloChamadoMin + sorteio.NextDouble() * (Catalogo.IntervaloChamadoMax - Catalogo.IntervaloChamadoMin);
                AbrirChamado();
            }
        }

        /// <summary>Prioridade sorteada: no armário não tem P1 (nem site para cair); da sala de racks em diante, ele aparece.</summary>
        int SortearPrioridade()
        {
            double r = sorteio.NextDouble();
            double[] pesos = Estado.cargo == 0 ? new[] { 0, 0.15, 0.45, 0.40 } : Estado.cargo == 1 ? new[] { 0.05, 0.20, 0.40, 0.35 } : new[] { 0.12, 0.23, 0.35, 0.30 };
            for (int p = 0; p < 4; p++) { if (r < pesos[p]) return p + 1; r -= pesos[p]; }
            return 4;
        }

        /// <summary>Abre um chamado (prioridade 0 = sorteada). Com a fila cheia, não entra. Retorna o chamado ou null.</summary>
        public ChamadoAberto AbrirChamado(int prioridade = 0)
        {
            MigrarChamadoAntigo();
            if (Estado.chamados.Count >= Catalogo.ChamadosNaFila) return null;
            if (prioridade < 1 || prioridade > 4) prioridade = SortearPrioridade();
            var textos = Catalogo.TextosDosChamados[prioridade - 1];
            var c = new ChamadoAberto { texto = textos[sorteio.Next(textos.Length)], prioridade = prioridade, restante = Catalogo.PrazoDoChamado[prioridade - 1] };
            Estado.chamados.Add(c);
            ChamadoApareceu?.Invoke(c);
            return c;
        }

        /// <summary>Atende o chamado mais urgente e recebe o bônus. Retorna o valor (0 se a fila está vazia).</summary>
        public double AtenderChamado() => MaisUrgente == null ? 0 : Fechar(MaisUrgente, porEquipe: false);

        /// <summary>Atende um chamado da fila (índice na ordem de Chamados).</summary>
        public double AtenderChamado(int indice)
        {
            var l = Chamados;
            return indice < 0 || indice >= l.Count ? 0 : Fechar(l[indice], porEquipe: false);
        }

        double Fechar(ChamadoAberto c, bool porEquipe)
        {
            double bonus = BonusDe(c.prioridade) * (porEquipe ? Catalogo.FracaoDaEquipe : 1);
            Estado.chamados.Remove(c);
            Ganhar(bonus);
            Estado.chamadosAtendidos++;
            if (!porEquipe && c.prioridade == 1) Estado.chamadosP1++;
            ChamadoEncerrado?.Invoke(c, bonus, porEquipe);
            return bonus;
        }

        /// <summary>Com o jogo fechado ninguém abre chamado: a fila esvazia.</summary>
        void LimparChamadosOffline()
        {
            Estado.chamados.Clear();
            Estado.chamadoRestante = 0; Estado.chamadoTexto = "";
            Estado.helpDeskTrabalho = 0;
            Estado.proximoChamado = Catalogo.IntervaloChamadoMin;
        }
    }
}
