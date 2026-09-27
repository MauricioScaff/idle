using System;
using System.IO;
using NUnit.Framework;
using IdleDataCenter.Isometrico;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter.Testes
{
    public class IsometricoTestes
    {
        [Test]
        public void CampanhaComecaOperanteComOrcamentoParaConstruirEPesquisar()
        {
            var jogo = new CentroDadosSimulacao();
            Assert.That(jogo.Racks, Is.EqualTo(2));
            Assert.That(jogo.Economia.ReceitaPorSegundo, Is.GreaterThan(0));
            Assert.That(jogo.Economia.Sobrecarga || jogo.Economia.Quente || jogo.Economia.LinkSaturado, Is.False);
            Assert.That(jogo.Comprar(Catalogo.RackCheio), Is.True);
            Assert.That(jogo.Economia.EscreverAutomacao(Catalogo.Watchdog), Is.True);
            Assert.That(jogo.Economia.Dinheiro, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ConstruirMudaRacksReceitaESaldo()
        {
            var jogo = new CentroDadosSimulacao();
            double saldo = jogo.Economia.Dinheiro, custo = jogo.Economia.Custo(Catalogo.RackCheio), renda = jogo.Economia.ReceitaPorSegundo;
            Assert.That(jogo.Comprar(Catalogo.RackCheio), Is.True);
            Assert.That(jogo.Racks, Is.EqualTo(3));
            Assert.That(jogo.Economia.Dinheiro, Is.EqualTo(saldo - custo));
            Assert.That(jogo.Economia.ReceitaPorSegundo, Is.GreaterThan(renda));
        }

        [Test]
        public void MetasPagamUmaVezMesmoAposRecarregar()
        {
            var jogo = new CentroDadosSimulacao();
            jogo.Economia.Ganhar(10000000);
            while (jogo.Racks < 5) Assert.That(jogo.Comprar(Catalogo.RackCheio), Is.True);
            Assert.That(jogo.Estado.metaQuatroRacks && jogo.Estado.metaCincoRacks, Is.True);
            Assert.That(jogo.RecompensarMetas(), Is.Zero);
            var recarregado = new CentroDadosSimulacao(jogo.Estado);
            Assert.That(recarregado.RecompensarMetas(), Is.Zero);
            Assert.That(recarregado.Comprar(Catalogo.RackCheio), Is.False);
        }

        [Test]
        public void PesquisaAtivaDroneEReparoAutomatico()
        {
            var jogo = new CentroDadosSimulacao(null, new Random(3));
            Assert.That(jogo.Economia.EscreverAutomacao(Catalogo.Watchdog), Is.True);
            jogo.Avancar(180);
            Assert.That(jogo.DroneAtivo, Is.True);
            Assert.That(jogo.Economia.TempoConserto, Is.EqualTo(5));
        }

        [Test]
        public void NaoPermiteCompraSemSaldoOuTempoInvalido()
        {
            var jogo = new CentroDadosSimulacao();
            jogo.Estado.economia.dinheiro = 0;
            Assert.That(jogo.Comprar(Catalogo.RackCheio), Is.False);
            jogo.Avancar(double.NaN); jogo.Avancar(-10); jogo.Avancar(double.PositiveInfinity);
            Assert.That(jogo.Economia.Dinheiro, Is.Zero);
        }

        [Test]
        public void SavePreservaEconomiaPesquisaEMetasComBackup()
        {
            string pasta = Path.Combine(Path.GetTempPath(), "idle-devops-test-" + Guid.NewGuid().ToString("N"));
            string caminho = Path.Combine(pasta, "campanha.json");
            try
            {
                var jogo = new CentroDadosSimulacao();
                jogo.Comprar(Catalogo.RackCheio);
                jogo.Economia.EscreverAutomacao(Catalogo.Watchdog);
                jogo.Estado.metaQuatroRacks = true;
                Assert.That(SaveCentroDados.Salvar(jogo.Estado, caminho), Is.True);
                var lido = new CentroDadosSimulacao(SaveCentroDados.Carregar(caminho));
                Assert.That(lido.Racks, Is.EqualTo(3));
                Assert.That(lido.Economia.Estado.escrevendo, Is.EqualTo(Catalogo.Watchdog));
                Assert.That(lido.Estado.metaQuatroRacks, Is.True);
                Assert.That(lido.Economia.Dinheiro, Is.EqualTo(jogo.Economia.Dinheiro));
                Assert.That(SaveCentroDados.Salvar(lido.Estado, caminho), Is.True);
                Assert.That(File.Exists(caminho + ".bak"), Is.True);
            }
            finally
            {
                foreach (string sufixo in new[] { "", ".bak", ".tmp" }) if (File.Exists(caminho + sufixo)) File.Delete(caminho + sufixo);
                if (Directory.Exists(pasta)) Directory.Delete(pasta);
            }
        }
    }
}
