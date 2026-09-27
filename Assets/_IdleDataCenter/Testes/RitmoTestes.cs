using System;
using System.Linq;
using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    /// <summary>
    /// Simula um jogador 100% ocioso (nunca clica; o técnico conserta tudo sozinho) que compra sempre
    /// a melhoria mais barata disponível e, quando não há melhoria para comprar, escreve a automação mais barata.
    /// Serve para acompanhar o ritmo do jogo quando o balanceamento muda.
    /// </summary>
    public class RitmoTestes
    {
        [Test]
        public void JogadorOciosoEvoluiNoRitmoPlanejado()
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            var promocoes = new double[Catalogo.Cargos.Count];
            var completo = new double[Catalogo.Cargos.Count];
            double primeiroBackup = -1;
            const double limite = 48 * 3600;

            for (double t = 0; t < limite; t += 1)
            {
                e.Avancar(1);
                var barata = e.MelhoriasDoCargo().Where(m => e.PodeComprar(m.Id)).OrderBy(m => e.Custo(m.Id)).FirstOrDefault();
                if (barata != null) e.Comprar(barata.Id);
                var script = Catalogo.Automacoes.Where(a => e.PodeEscrever(a.Id)).OrderBy(a => a.Custo).FirstOrDefault();
                if (script != null && barata == null) e.EscreverAutomacao(script.Id);
                if (e.PodePromover) { e.Promover(); promocoes[e.Cargo] = t; }
                if (e.Estado.backupsRestaurados > 0 && primeiroBackup < 0) primeiroBackup = t;

                bool tudo = e.MelhoriasDoCargo().All(m => e.NoMaximo(m.Id))
                            && Catalogo.Automacoes.Where(a => a.Cargo <= e.Cargo && e.Cargo >= Catalogo.CargoDasAutomacoes).All(a => e.TemAutomacao(a.Id));
                if (tudo && completo[e.Cargo] == 0) completo[e.Cargo] = t;
                if (e.Cargo == Catalogo.Cargos.Count - 1 && tudo) break;
            }

            string H(double s) => s <= 0 ? "-" : $"{(int)(s / 3600)}h{(int)(s % 3600 / 60):00}";
            UnityEngine.Debug.Log($"RITMO: Sysadmin {H(promocoes[1])} (completo {H(completo[1])}); Analista {H(promocoes[2])} (1º backup {H(primeiroBackup)}, completo {H(completo[2])}); " +
                                  $"DevOps {H(promocoes[3])} (completo {H(completo[3])}); {e.Estado.incidentesResolvidos} incidentes; " +
                                  $"receita final {e.ReceitaPorSegundo:0}/s; temperatura {e.Temperatura:0} C; " +
                                  $"energia {e.ConsumoKw:0.0}/{e.CapacidadeKw:0.0} kW; banda {e.TrafegoMbps:0}/{e.BandaMbps:0} Mbps");

            // Documento de design: Técnico de 30 a 60 min; Sysadmin de 3 a 5 h; Analista de 1 a 2 dias; DevOps de 3 a 5 dias.
            // Ocioso e sem cliques o jogador é o mais lento possível; com cliques e offline tudo anda mais rápido.
            Assert.That(promocoes[1], Is.InRange(10 * 60, 90 * 60), "promoção a Sysadmin fora do ritmo");
            Assert.That(promocoes[2], Is.InRange(2 * 3600, 6 * 3600), "promoção a Analista fora do ritmo");
            Assert.That(promocoes[3], Is.InRange(promocoes[2] + 2 * 3600, promocoes[2] + 8 * 3600), "promoção a DevOps fora do ritmo");
            Assert.That(completo[3], Is.GreaterThan(promocoes[3] + 3 * 3600), "DevOps acaba rápido demais");
        }
    }
}
