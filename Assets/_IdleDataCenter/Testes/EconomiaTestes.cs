using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    public class EconomiaTestes
    {
        static Economia Nova(double dinheiro = 0) => new Economia(new EstadoJogo { dinheiro = dinheiro });

        [Test]
        public void ComecaComUmServidorRendendoUmPorSegundo()
        {
            var e = Nova();
            Assert.AreEqual(1, e.Servidores);
            Assert.AreEqual(1, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void AvancarAcumulaReceita()
        {
            var e = Nova();
            e.Avancar(10);
            Assert.AreEqual(10, e.Dinheiro, 1e-9);
        }

        [Test]
        public void NaoCompraSemDinheiro()
        {
            var e = Nova(10);
            Assert.IsFalse(e.Comprar(Catalogo.Ssd));
            Assert.AreEqual(10, e.Dinheiro, 1e-9);
            Assert.AreEqual(0, e.Nivel(Catalogo.Ssd));
        }

        [Test]
        public void SsdDobraAReceitaEDescontaOCusto()
        {
            var e = Nova(100);
            Assert.IsTrue(e.Comprar(Catalogo.Ssd));
            Assert.AreEqual(75, e.Dinheiro, 1e-9);
            Assert.AreEqual(2, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void NaoPassaDoNivelMaximo()
        {
            var e = Nova(1_000_000);
            Assert.IsTrue(e.Comprar(Catalogo.Ssd));
            Assert.IsFalse(e.Comprar(Catalogo.Ssd));
            Assert.IsTrue(e.NoMaximo(Catalogo.Ssd));
        }

        [Test]
        public void CustoDaVentoinhaCresceAcadaNivel()
        {
            var e = Nova(1_000_000);
            Assert.AreEqual(60, e.Custo(Catalogo.Ventoinha), 1e-9);
            e.Comprar(Catalogo.Ventoinha);
            Assert.AreEqual(132, e.Custo(Catalogo.Ventoinha), 1e-9);
        }

        [Test]
        public void ServidoresExtrasMultiplicamAReceitaComMelhorias()
        {
            var e = Nova(1_000_000);
            e.Comprar(Catalogo.Ssd);         // 2 por servidor
            e.Comprar(Catalogo.Ventoinha);   // 3 por servidor
            e.Comprar(Catalogo.Servidor);    // 2 servidores
            Assert.AreEqual(2, e.Servidores);
            Assert.AreEqual(6, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CliqueRendeBaseMaisDobroDaReceita()
        {
            var e = Nova();
            Assert.AreEqual(4, e.Clicar(), 1e-9);
            Assert.IsTrue(e.Estado.jaClicouNoServidor);
        }

        [Test]
        public void OfflineRendeMetadeComLimiteDeDozeHoras()
        {
            var e = Nova();
            Assert.AreEqual(0, e.CalcularGanhoOffline(30), 1e-9, "menos de 1 minuto não conta");
            Assert.AreEqual(1800, e.CalcularGanhoOffline(3600), 1e-9);
            Assert.AreEqual(12 * 3600 * 0.5, e.CalcularGanhoOffline(48 * 3600), 1e-9);
        }

        [Test]
        public void PrimeiroJogoNaoGanhaOffline()
        {
            var e = Nova();
            Assert.AreEqual(0, e.AplicarOffline(1_000_000), 1e-9);
        }

        [Test]
        public void EventoDeCompraAvisaQualMelhoria()
        {
            var e = Nova(1000);
            string recebido = null;
            e.Comprou += id => recebido = id;
            e.Comprar(Catalogo.Ventoinha);
            Assert.AreEqual(Catalogo.Ventoinha, recebido);
        }
    }
}
