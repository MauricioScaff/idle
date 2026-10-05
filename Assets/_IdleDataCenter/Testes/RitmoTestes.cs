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
        [Test]
        public void JogadorOciosoEvoluiNoRitmoPlanejado()
        {
            var e = new Economia(new EstadoJogo(), new Random(42));
            var lutas = new System.Collections.Generic.List<string>();
            double agora = 0;
            e.ChefeTerminou += (c, venceu, _) => lutas.Add($"{c.Id}:{(venceu ? "V" : "D")}@{(int)(agora / 60)}min(def {string.Join("/", c.Ataques.Select(a => e.ValorDefesa(a.Tipo).ToString("0.00")))})");
            var promocoes = new double[Catalogo.Cargos.Count];
            var completo = new double[Catalogo.Cargos.Count];
            double primeiroBackup = -1;
            double ipo = -1;
            double melhorUptime = 0, segundosComSla = 0;
            const double limite = 200 * 3600;

            for (double t = 0; t < limite; t += 1)
            {
                agora = t;
                int cargoAntes = e.Cargo;
                e.Avancar(1);
                if (e.Cargo != cargoAntes) promocoes[e.Cargo] = t;   // venceu o chefe
                if (e.IpoFeito && ipo < 0) ipo = t;
                var sugerida = e.MelhoriaSugerida();
                var barata = sugerida != null && e.PodeComprar(sugerida.Id) ? sugerida : null;
                if (barata != null) e.Comprar(barata.Id);
                var script = Catalogo.Automacoes.Where(a => e.PodeEscrever(a.Id)).OrderBy(a => a.Custo).FirstOrDefault();
                if (script != null && barata == null) e.EscreverAutomacao(script.Id);
                if (barata == null && e.PodeFazerRefresh) e.FazerRefresh();   // como o jogador quando o cartão pede
                // depois de perder, segue a dica: compra o que reforça o ponto fraco
                if (e.RecargaDoChefe > 0 && e.ChefeDoCargo != null)
                {
                    var reforco = e.MelhoriaParaDefesa(e.PontoFraco(e.ChefeDoCargo));
                    if (reforco != null && e.PodeComprar(reforco.Id)) e.Comprar(reforco.Id);
                }
                if (e.PodeEnfrentarChefe) e.EnfrentarChefe();   // nunca escolhe nada: vale sempre a opção segura
                if (e.Estado.backupsRestaurados > 0 && primeiroBackup < 0) primeiroBackup = t;
                melhorUptime = Math.Max(melhorUptime, e.Uptime);
                if (e.Sla.HasValue) segundosComSla++;

                bool tudo = e.MelhoriasDoCargo().Where(m => !m.Gerador && m.NivelMaximo < 30).All(m => e.NoMaximo(m.Id))   // geradores e infraestrutura não acabam
                            && Catalogo.Automacoes.Where(a => a.Cargo <= e.Cargo && e.Cargo >= Catalogo.CargoDasAutomacoes).All(a => e.TemAutomacao(a.Id));
                if (tudo && completo[e.Cargo] == 0) completo[e.Cargo] = t;
                if (e.Cargo == Catalogo.Cargos.Count - 1 && tudo && ipo > 0) break;
            }

            string H(double s) => s <= 0 ? "-" : $"{(int)(s / 3600)}h{(int)(s % 3600 / 60):00}";
            UnityEngine.Debug.Log("CHEFES: " + string.Join(" ", lutas));
            UnityEngine.Debug.Log($"RITMO: Sysadmin {H(promocoes[1])} (completo {H(completo[1])}); Analista {H(promocoes[2])} (1º backup {H(primeiroBackup)}, completo {H(completo[2])}); " +
                                  $"DevOps {H(promocoes[3])} (completo {H(completo[3])}); SRE {H(promocoes[4])} (completo {H(completo[4])}, {e.Estado.picosSobrevividos}/{e.Estado.picosTotal} picos); Arquiteto {H(promocoes[5])} (completo {H(completo[5])}); CTO {H(promocoes[6])} (completo {H(completo[6])}, IPO {H(ipo)}); {e.Estado.incidentesResolvidos} incidentes; chefes {e.Estado.chefesVencidos} vencidos, {e.Estado.derrotasChefe} derrotas; " +
                                  $"receita final {e.ReceitaPorSegundo:0}/s; temperatura {e.Temperatura:0} C; " +
                                  $"energia {e.ConsumoKw:0.0}/{e.CapacidadeKw:0.0} kW; banda {e.TrafegoMbps:0}/{e.BandaMbps:0} Mbps; " +
                                  $"uptime final {Economia.FormatarUptime(e.Uptime)}, melhor {Economia.FormatarUptime(melhorUptime)}, com SLA {H(segundosComSla)}");

            // Documento de design: Técnico de 30 a 60 min; Sysadmin de 3 a 5 h; Analista de 1 a 2 dias; DevOps de 3 a 5 dias; SRE 1 semana.
            // Ocioso e sem cliques o jogador é o mais lento possível; com cliques e offline tudo anda mais rápido.
            Assert.That(promocoes[1], Is.InRange(10 * 60, 90 * 60), "promoção a Sysadmin fora do ritmo");
            Assert.That(promocoes[2], Is.InRange(2 * 3600, 6 * 3600), "promoção a Analista fora do ritmo");
            Assert.That(promocoes[3], Is.InRange(promocoes[2] + 2 * 3600, promocoes[2] + 8 * 3600), "promoção a DevOps fora do ritmo");
            Assert.That(promocoes[4], Is.InRange(promocoes[3] + 3 * 3600, promocoes[3] + 12 * 3600), "promoção a SRE fora do ritmo");
            // com geradores sem teto sempre há o que comprar; "completo" aqui é só o que tem limite (melhorias e automações)
            Assert.That(completo[4], Is.GreaterThan(promocoes[4] + 1800), "SRE acaba rápido demais");
            Assert.That(promocoes[5], Is.InRange(promocoes[4] + 3 * 3600, promocoes[4] + 14 * 3600), "promoção a Arquiteto fora do ritmo");
            Assert.That(completo[5], Is.GreaterThan(promocoes[5] + 1800), "Arquiteto acaba rápido demais");
            Assert.That(promocoes[6], Is.InRange(promocoes[5] + 3 * 3600, promocoes[5] + 16 * 3600), "promoção a CTO fora do ritmo");
            Assert.That(ipo, Is.GreaterThan(promocoes[6] + 4 * 3600), "IPO rápido demais");
        }
    }
}
