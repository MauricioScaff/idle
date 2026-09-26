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
            double promocao = -1, sysadminCompleto = -1;
            const double limite = 6 * 3600;

            for (double t = 0; t < limite; t += 1)
            {
                e.Avancar(1);
                var barata = e.MelhoriasDoCargo().Where(m => e.PodeComprar(m.Id)).OrderBy(m => e.Custo(m.Id)).FirstOrDefault();
                if (barata != null) e.Comprar(barata.Id);
                if (e.PodePromover) { e.Promover(); promocao = t; }
                if (e.Cargo == 1 && e.MelhoriasDoCargo().All(m => e.NoMaximo(m.Id))) { sysadminCompleto = t; break; }
            }

            UnityEngine.Debug.Log($"RITMO: promoção a Sysadmin em {promocao / 60:0} min; Sysadmin completo em {sysadminCompleto / 60:0} min; " +
                                  $"{e.Estado.incidentesResolvidos} incidentes; receita final {e.ReceitaPorSegundo:0.0}/s; " +
                                  $"temperatura {e.Temperatura:0} C; energia {e.ConsumoKw:0.00}/{e.CapacidadeKw:0.0} kW");

            // Documento de design: Técnico de 30 a 60 min; Sysadmin de 3 a 5 h (com cliques, bem menos)
            Assert.That(promocao, Is.InRange(10 * 60, 90 * 60), "promoção fora do ritmo planejado");
            Assert.That(sysadminCompleto, Is.GreaterThan(0), "não completou o Sysadmin em 6 h");
        }
    }
}
