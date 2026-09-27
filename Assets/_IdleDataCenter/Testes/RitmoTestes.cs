using System;
using System.Linq;
using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    /// <summary>
    /// Simula um jogador 100% ocioso (nunca clica; o técnico conserta tudo sozinho) que compra sempre
    /// a melhoria mais barata disponível. Serve para acompanhar o ritmo do jogo quando o balanceamento muda.
    /// </summary>
    public class RitmoTestes
    {
        [Test]
        public void JogadorOciosoEvoluiNoRitmoPlanejado()
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            var promocoes = new double[Catalogo.Cargos.Count];
            double sysadminCompleto = -1, analistaCompleto = -1, primeiroBackup = -1;
            const double limite = 24 * 3600;

            for (double t = 0; t < limite; t += 1)
            {
                e.Avancar(1);
                var barata = e.MelhoriasDoCargo().Where(m => e.PodeComprar(m.Id)).OrderBy(m => e.Custo(m.Id)).FirstOrDefault();
                if (barata != null) e.Comprar(barata.Id);
                var script = Catalogo.Automacoes.Where(a => e.PodeEscrever(a.Id)).OrderBy(a => a.Custo).FirstOrDefault();
                if (script != null && barata == null) e.EscreverAutomacao(script.Id);
                if (e.PodePromover) { e.Promover(); promocoes[e.Cargo] = t; }
                bool completo = e.MelhoriasDoCargo().All(m => e.NoMaximo(m.Id))
                                && (e.Cargo < Catalogo.CargoDasAutomacoes || e.AutomacoesAtivas == Catalogo.Automacoes.Count);
                if (e.Cargo == 1 && completo && sysadminCompleto < 0) sysadminCompleto = t;
                if (e.Estado.backupsRestaurados > 0 && primeiroBackup < 0) primeiroBackup = t;
                if (e.Cargo == 2 && completo) { analistaCompleto = t; break; }
            }

            UnityEngine.Debug.Log($"RITMO: Sysadmin em {promocoes[1] / 60:0} min (completo em {sysadminCompleto / 60:0} min); " +
                                  $"Analista em {promocoes[2] / 60:0} min; 1º backup restaurado em {primeiroBackup / 60:0} min; " +
                                  $"Analista completo (com automações) em {analistaCompleto / 60:0} min; {e.Estado.incidentesResolvidos} incidentes; " +
                                  $"receita final {e.ReceitaPorSegundo:0.0}/s; temperatura {e.Temperatura:0} C; " +
                                  $"energia {e.ConsumoKw:0.0}/{e.CapacidadeKw:0.0} kW; banda {e.TrafegoMbps:0}/{e.BandaMbps:0} Mbps");

            // Documento de design: Técnico de 30 a 60 min; Sysadmin de 3 a 5 h; Analista de 1 a 2 dias.
            // Ocioso e sem cliques o jogador é o mais lento possível; com cliques e offline tudo anda mais rápido.
            Assert.That(promocoes[1], Is.InRange(10 * 60, 90 * 60), "promoção a Sysadmin fora do ritmo");
            Assert.That(promocoes[2], Is.InRange(2 * 3600, 6 * 3600), "promoção a Analista fora do ritmo");
            Assert.That(analistaCompleto, Is.GreaterThan(promocoes[2] + 2 * 3600), "Analista acaba rápido demais");
        }
    }
}
