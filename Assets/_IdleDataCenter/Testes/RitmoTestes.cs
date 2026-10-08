using System;
using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    /// <summary>
    /// Simula a carreira inteira com dois jogadores que compram sempre o que o jogo sugere (e, sem nada para comprar,
    /// escrevem a automação mais barata):
    /// - o ocioso nunca clica: o técnico conserta tudo sozinho, devagar;
    /// - o ativo olha a tela a cada 5 s: atende os chamados, resolve os incidentes na hora, escala os picos, toma café,
    ///   aceita clientes e age nos eventos.
    /// As metas são calibradas para os dois terminarem juntas (antes, calibradas só no ocioso, quem jogava de verdade
    /// cumpria chamados e incidentes cedo e ficava horas esperando o faturamento).
    /// </summary>
    public class RitmoTestes
    {
        /// <summary>Quanto tempo (min) cada cargo deve durar com o jogador ativo: a calibração promove nesses tempos (~60 h).</summary>
        static readonly int[] DuracaoAlvo = { 30, 70, 320, 350, 480, 400, 950, 950 };

        static void JogarAtivo(Economia e, double t)
        {
            if (t % 5 != 0) return;   // olha a tela a cada 5 s
            if (e.TemChamado) e.AtenderChamado();
            foreach (var tr in e.Travamentos.ToList()) e.Clicar(tr.servidor);
            if (e.DiscoQueimado) e.TrocarDisco(false);
            if (e.DeployQuebrado) e.FazerRollback(false);
            if (e.TemQuedaDeEnergia) e.Religar();
            if (e.TemPaneRegional) e.Redirecionar();
            if (e.EmPico && !e.PicoFoiEscalado) e.Escalar();
            if (e.PodeTomarCafe) e.TomarCafe();
            if (e.PodeAceitarCliente) e.AceitarCliente();
            if (e.TemEvento && e.BotaoDoEvento != null) e.AgirNoEvento();
        }

        static void Comprar(Economia e)
        {
            var sugerida = e.MelhoriaSugerida();
            var barata = sugerida != null && e.PodeComprar(sugerida.Id) ? sugerida : null;
            if (barata != null) e.Comprar(barata.Id);
            var script = Catalogo.Automacoes.Where(a => e.PodeEscrever(a.Id)).OrderBy(a => a.Custo).FirstOrDefault();
            if (script != null && barata == null) e.EscreverAutomacao(script.Id);
            if (barata == null && e.PodeFazerRefresh) e.FazerRefresh();   // como o jogador quando o cartão pede
        }

        /// <summary>
        /// Calibração dos alvos das metas (rodar à mão: -testFilter Calibrar): o jogador ativo, promovido nos tempos de
        /// DuracaoAlvo sem olhar as metas. O log mostra quanto de cada coisa ele faz em cada cargo.
        /// </summary>
        [Explicit, Timeout(1200000), TestCase(true), TestCase(false)]
        public void Calibrar(bool ativo)
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            double fimDoCargo = DuracaoAlvo[0] * 60, chegou = 0;
            for (double t = 0; t < DuracaoAlvo.Sum() * 60; t += 1)
            {
                e.Avancar(1);
                Comprar(e);
                if (ativo) JogarAtivo(e, t);
                if (t % 600 == 0)
                {
                    var s = e.Estado; var i0 = s.inicioDoCargo;
                    UnityEngine.Debug.Log($"{(ativo ? "CALIB" : "CALIO")} {e.Cargo} +{(t - chegou) / 60:0}: ganho {s.totalGanho - i0.totalGanho:0} incidentes {s.incidentesResolvidos - i0.incidentes} chamados {s.chamadosAtendidos - i0.chamados} " +
                                          $"backups {s.backupsRestaurados - i0.backups} picos {s.picosSobrevividos - i0.picos} servidores {e.TotalServidores} sites {e.Sites} automações {e.AutomacoesAtivas} hosts {e.HostsContainers} " +
                                          $"regiões {e.TotalRegioes} dcs {e.TotalDatacenters} receita {e.ReceitaPorSegundo:0} " + string.Join(" ", Catalogo.Melhorias.Where(d => e.Nivel(d.Id) > 0 && d.Cargo == e.Cargo).Select(d => d.Id + "=" + e.Nivel(d.Id))));
                }
                if (t >= fimDoCargo && e.Cargo < Catalogo.Cargos.Count - 1)
                {
                    var s = e.Estado;
                    s.cargo++;
                    s.inicioDoCargo = new InicioDoCargo { totalGanho = s.totalGanho, incidentes = s.incidentesResolvidos, chamados = s.chamadosAtendidos, backups = s.backupsRestaurados, picos = s.picosSobrevividos };
                    chegou = t;
                    fimDoCargo = t + DuracaoAlvo[e.Cargo] * 60;
                }
            }
        }

        /// <summary>O resultado de uma carreira simulada.</summary>
        class Carreira
        {
            public double[] promocoes = new double[Catalogo.Cargos.Count], duracao = new double[Catalogo.Cargos.Count];
            public double ipo = -1;
            public readonly Dictionary<string, double> metaCumprida = new Dictionary<string, double>();   // "cargo|texto" → segundos desde a chegada
            public double Fracao(string chave) { int c = int.Parse(chave.Split('|')[0]); return duracao[c] > 0 ? metaCumprida[chave] / duracao[c] : 1; }
        }

        static string H(double s) => s <= 0 ? "-" : $"{(int)(s / 3600)}h{(int)(s % 3600 / 60):00}";

        static Carreira Simular(bool ativo)
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            var r = new Carreira();
            double chegouNoCargo = 0;
            for (double t = 0; t < 200 * 3600; t += 1)
            {
                e.Avancar(1);
                Comprar(e);
                if (ativo) JogarAtivo(e, t);
                // cada meta: quando foi cumprida, desde a chegada no cargo (antes da promoção, que zera as metas)
                foreach (var m in e.CargoAtual.MetasParaPromocao)
                {
                    string chave = e.Cargo + "|" + m.Texto;
                    if (!r.metaCumprida.ContainsKey(chave) && e.Cumprida(m)) r.metaCumprida[chave] = t - chegouNoCargo;
                }
                if (e.PodePromover)
                {
                    r.duracao[e.Cargo] = t - chegouNoCargo;
                    e.Promover(); r.promocoes[e.Cargo] = t; chegouNoCargo = t;
                }
                if (e.PodeFazerIpo) { r.duracao[e.Cargo] = t - chegouNoCargo; e.FazerIpo(); r.ipo = t; break; }
            }
            string quem = ativo ? "ATIVO" : "OCIOSO";
            UnityEngine.Debug.Log($"RITMO {quem}: " + string.Join("; ", Enumerable.Range(1, Catalogo.Cargos.Count - 1).Select(c => Catalogo.Cargos[c].Nome + " " + H(r.promocoes[c]))) +
                                  $"; IPO {H(r.ipo)}; receita final {e.ReceitaPorSegundo:0}/s; {e.Estado.incidentesResolvidos} incidentes, {e.Estado.chamadosAtendidos} chamados");
            UnityEngine.Debug.Log($"METAS {quem}: " + string.Join("; ", r.metaCumprida.Keys.Select(k => Catalogo.Cargos[int.Parse(k.Split('|')[0])].Nome + " / " + k.Split('|')[1] + " " + H(r.metaCumprida[k]) + $" ({r.Fracao(k):P0})")));
            return r;
        }

        [Test, Timeout(1200000)]
        public void JogadorAtivoChegaAoIpoPerto60HorasComAsMetasJuntas()
        {
            var r = Simular(ativo: true);
            Assert.That(r.ipo, Is.InRange(45 * 3600, 75 * 3600), "quem joga ativo tem que chegar ao IPO perto das 60 h");
            Assert.That(r.promocoes[Catalogo.CargoTecnico], Is.InRange(10 * 60, 60 * 60), "contratação (Técnico) fora do ritmo");
            // metas juntas: nenhuma pronta muito antes da promoção (antes, chamados e incidentes acabavam cedo e o
            // faturamento segurava o cargo sozinho por horas)
            foreach (var k in r.metaCumprida.Keys)
                Assert.That(r.Fracao(k), Is.GreaterThanOrEqualTo(0.4), "meta fácil demais para o ativo: " + k.Split('|')[1] + " no " + Catalogo.Cargos[int.Parse(k.Split('|')[0])].Nome);
        }

        [Test, Timeout(1200000)]
        public void JogadorOciosoChegaAoIpoPerto90Horas()
        {
            var r = Simular(ativo: false);
            Assert.That(r.ipo, Is.InRange(70 * 3600, 115 * 3600), "quem deixa o jogo rodando sozinho chega ao IPO perto das 90 h");
            foreach (var k in r.metaCumprida.Keys)
                Assert.That(r.Fracao(k), Is.GreaterThanOrEqualTo(0.2), "meta pronta na chegada para o ocioso: " + k.Split('|')[1] + " no " + Catalogo.Cargos[int.Parse(k.Split('|')[0])].Nome);
        }
    }
}
