using System;
using System.Linq;
using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    /// <summary>
    /// Simula um jogador 100% ocioso (nunca clica; o técnico conserta tudo sozinho) que compra sempre
    /// o que o jogo sugere (a melhoria mais barata, ou a que resolve falta de energia, calor ou banda) e,
    /// quando não há melhoria para comprar, escreve a automação mais barata.
    /// Serve para acompanhar o ritmo do jogo quando o balanceamento muda.
    /// </summary>
    public class RitmoTestes
    {
        [Test, Timeout(600000)]   // simula umas 100 horas de jogo
        public void JogadorOciosoEvoluiNoRitmoPlanejado()
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            var promocoes = new double[Catalogo.Cargos.Count];
            var completo = new double[Catalogo.Cargos.Count];
            double primeiroBackup = -1;
            double ipo = -1;
            double melhorUptime = 0, segundosComSla = 0;
            const double limite = 200 * 3600;

            for (double t = 0; t < limite; t += 1)
            {
                e.Avancar(1);
                var sugerida = e.MelhoriaSugerida();
                var barata = sugerida != null && e.PodeComprar(sugerida.Id) ? sugerida : null;
                if (barata != null) e.Comprar(barata.Id);
                var script = Catalogo.Automacoes.Where(a => e.PodeEscrever(a.Id)).OrderBy(a => a.Custo).FirstOrDefault();
                if (script != null && barata == null) e.EscreverAutomacao(script.Id);
                if (barata == null && e.PodeFazerRefresh) e.FazerRefresh();   // como o jogador quando o cartão pede
                if (e.PodePromover) { e.Promover(); promocoes[e.Cargo] = t; }
                if (e.PodeFazerIpo) { e.FazerIpo(); ipo = t; }
                if (e.Estado.backupsRestaurados > 0 && primeiroBackup < 0) primeiroBackup = t;
                melhorUptime = Math.Max(melhorUptime, e.Uptime);
                if (e.Sla.HasValue) segundosComSla++;

                bool tudo = e.MelhoriasDoCargo().Where(m => !m.Gerador && m.NivelMaximo < 30).All(m => e.NoMaximo(m.Id))   // geradores e infraestrutura não acabam
                            && Catalogo.Automacoes.Where(a => a.Cargo <= e.Cargo && e.Cargo >= Catalogo.CargoDasAutomacoes).All(a => e.TemAutomacao(a.Id));
                if (tudo && completo[e.Cargo] == 0) completo[e.Cargo] = t;
                if (e.Cargo == Catalogo.Cargos.Count - 1 && tudo && ipo > 0) break;
            }

            string H(double s) => s <= 0 ? "-" : $"{(int)(s / 3600)}h{(int)(s % 3600 / 60):00}";
            UnityEngine.Debug.Log($"RITMO: Técnico {H(promocoes[Catalogo.CargoTecnico])}; Sysadmin {H(promocoes[Catalogo.CargoSysadmin])} (completo {H(completo[Catalogo.CargoSysadmin])}); Analista {H(promocoes[Catalogo.CargoAnalista])} (1º backup {H(primeiroBackup)}, completo {H(completo[Catalogo.CargoAnalista])}); " +
                                  $"DevOps {H(promocoes[Catalogo.CargoDevOps])} (completo {H(completo[Catalogo.CargoDevOps])}); SRE {H(promocoes[Catalogo.CargoSre])} (completo {H(completo[Catalogo.CargoSre])}, {e.Estado.picosSobrevividos}/{e.Estado.picosTotal} picos); Arquiteto {H(promocoes[Catalogo.CargoArquiteto])} (completo {H(completo[Catalogo.CargoArquiteto])}); CTO {H(promocoes[Catalogo.CargoCto])} (completo {H(completo[Catalogo.CargoCto])}, IPO {H(ipo)}); {e.Estado.incidentesResolvidos} incidentes; " +
                                  $"receita final {e.ReceitaPorSegundo:0}/s; temperatura {e.Temperatura:0} C; " +
                                  $"energia {e.ConsumoKw:0.0}/{e.CapacidadeKw:0.0} kW; banda {e.TrafegoMbps:0}/{e.BandaMbps:0} Mbps; " +
                                  $"uptime final {Economia.FormatarUptime(e.Uptime)}, melhor {Economia.FormatarUptime(melhorUptime)}, com SLA {H(segundosComSla)}");

            // Ritmo de 2026-10-05 (pedido do usuário: 3x mais lento que antes, com preços reais): Sysadmin ~2h30, Analista ~12 h,
            // IPO ~100 h. Ocioso e sem cliques o jogador é o mais lento possível; com cliques e offline tudo anda mais rápido.
            // o Freelancer é a abertura: uns 30 a 45 min até a proposta de emprego
            Assert.That(promocoes[Catalogo.CargoTecnico], Is.InRange(15 * 60, 75 * 60), "contratação (Técnico) fora do ritmo");
            Assert.That(promocoes[Catalogo.CargoSysadmin], Is.InRange(promocoes[Catalogo.CargoTecnico] + 30 * 60, promocoes[Catalogo.CargoTecnico] + 270 * 60), "promoção a Sysadmin fora do ritmo");
            Assert.That(promocoes[Catalogo.CargoAnalista], Is.InRange(6 * 3600, 18 * 3600), "promoção a Analista fora do ritmo");
            Assert.That(promocoes[Catalogo.CargoDevOps], Is.InRange(promocoes[Catalogo.CargoAnalista] + 6 * 3600, promocoes[Catalogo.CargoAnalista] + 24 * 3600), "promoção a DevOps fora do ritmo");
            Assert.That(promocoes[Catalogo.CargoSre], Is.InRange(promocoes[Catalogo.CargoDevOps] + 9 * 3600, promocoes[Catalogo.CargoDevOps] + 36 * 3600), "promoção a SRE fora do ritmo");
            // com geradores sem teto sempre há o que comprar; "completo" aqui é só o que tem limite (melhorias e automações)
            Assert.That(completo[Catalogo.CargoSre], Is.GreaterThan(promocoes[Catalogo.CargoSre] + 1800), "SRE acaba rápido demais");
            Assert.That(promocoes[Catalogo.CargoArquiteto], Is.InRange(promocoes[Catalogo.CargoSre] + 9 * 3600, promocoes[Catalogo.CargoSre] + 42 * 3600), "promoção a Arquiteto fora do ritmo");
            Assert.That(completo[Catalogo.CargoArquiteto], Is.GreaterThan(promocoes[Catalogo.CargoArquiteto] + 1800), "Arquiteto acaba rápido demais");
            Assert.That(promocoes[Catalogo.CargoCto], Is.InRange(promocoes[Catalogo.CargoArquiteto] + 9 * 3600, promocoes[Catalogo.CargoArquiteto] + 48 * 3600), "promoção a CTO fora do ritmo");
            Assert.That(ipo, Is.GreaterThan(promocoes[Catalogo.CargoCto] + 12 * 3600), "IPO rápido demais");
        }
    }
}
