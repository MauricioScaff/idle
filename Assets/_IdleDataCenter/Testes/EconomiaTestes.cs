using System;
using IdleDataCenter.Simulacao;
using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    public class EconomiaTestes
    {
        /// <summary>Sorteio controlado: sempre devolve o mesmo número (0.9999 = nunca trava; 0 = sempre trava).</summary>
        class SorteioFixo : Random
        {
            readonly double valor;
            public SorteioFixo(double valor) => this.valor = valor;
            public override double NextDouble() => valor;
        }

        static Economia Nova(double dinheiro = 0, double sorteio = 0.9999) =>
            new Economia(new EstadoJogo { dinheiro = dinheiro }, new SorteioFixo(sorteio));

        static void DefinirNivel(Economia e, string id, int nivel) =>
            e.Estado.melhorias.Add(new NivelMelhoria { id = id, nivel = nivel });

        // ---------- Receita e compras ----------

        [Test]
        public void ComecaComUmServidorRendendoUmPorSegundo()
        {
            var e = Nova();
            Assert.AreEqual(1, e.TotalServidores);
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
        }

        [Test]
        public void SsdDobraAReceitaEDescontaOCusto()
        {
            var e = Nova(150);
            Assert.IsTrue(e.Comprar(Catalogo.Ssd));
            Assert.AreEqual(50, e.Dinheiro, 1e-9);
            Assert.AreEqual(2, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void NaoPassaDoNivelMaximo()
        {
            var e = Nova(1_000_000);
            Assert.IsTrue(e.Comprar(Catalogo.Ssd));
            Assert.IsFalse(e.Comprar(Catalogo.Ssd));
        }

        [Test]
        public void CustoDaVentoinhaCresceAcadaNivel()
        {
            var e = Nova(1_000_000);
            Assert.AreEqual(250, e.Custo(Catalogo.Ventoinha), 1e-9);
            e.Comprar(Catalogo.Ventoinha);
            Assert.AreEqual(550, e.Custo(Catalogo.Ventoinha), 1e-9);
        }

        [Test]
        public void TorresExtrasMultiplicamAReceitaComMelhorias()
        {
            var e = Nova(1_000_000);
            e.Comprar(Catalogo.Ssd);        // 2 por torre
            e.Comprar(Catalogo.Ventoinha);  // 3 por torre
            e.Comprar(Catalogo.Servidor);   // 2 torres
            Assert.AreEqual(6, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CliqueRendeBaseMaisDobroDaReceita()
        {
            var e = Nova();
            Assert.AreEqual(4, e.Clicar(0), 1e-9);
            Assert.IsTrue(e.Estado.jaClicouNoServidor);
        }

        [Test]
        public void LojaMostraSoAsMelhoriasDoCargo()
        {
            var e = Nova();
            CollectionAssert.AreEquivalent(new[] { Catalogo.Ssd, Catalogo.Ventoinha, Catalogo.Servidor, Catalogo.Estagiario },
                System.Linq.Enumerable.Select(e.MelhoriasDoCargo(), m => m.Id));
            e.Estado.cargo = 1;
            Assert.AreEqual(4, System.Linq.Enumerable.Count(e.MelhoriasDoCargo()));
        }

        [Test]
        public void Servidor1UPrecisaDoRack()
        {
            var e = Nova(1_000_000);
            e.Estado.cargo = 1;
            Assert.IsFalse(e.Comprar(Catalogo.Servidor1U));
            Assert.IsTrue(e.Comprar(Catalogo.Rack));
            Assert.IsTrue(e.Comprar(Catalogo.Servidor1U));
            Assert.AreEqual(2, e.TotalServidores);
            Assert.AreEqual(1 + 9, e.ReceitaPorSegundo, 1e-9);
        }

        // ---------- Energia e temperatura ----------

        [Test]
        public void SobrecargaDeEnergiaReduzAReceita()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);    // 3 torres = 1.2 kW
            DefinirNivel(e, Catalogo.Rack, 1);
            DefinirNivel(e, Catalogo.Servidor1U, 2);  // +0.8 kW = 2.0 kW > 1.5 kW
            DefinirNivel(e, Catalogo.ArCondicionado, 3); // sem calor, para isolar a energia
            Assert.IsTrue(e.Sobrecarga);
            Assert.AreEqual(1.5 / 2.0, e.FatorEnergia, 1e-9);
            Assert.AreEqual((3 * 1 + 2 * 9) * 1.5 / 2.0, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void NoBreakResolveASobrecarga()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            DefinirNivel(e, Catalogo.Rack, 1);
            DefinirNivel(e, Catalogo.Servidor1U, 2);
            DefinirNivel(e, Catalogo.NoBreak, 1);
            Assert.IsFalse(e.Sobrecarga);
            Assert.AreEqual(1, e.FatorEnergia, 1e-9);
        }

        [Test]
        public void CalorAcimaDe32GrausCortaODesempenho()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            DefinirNivel(e, Catalogo.Rack, 1);
            DefinirNivel(e, Catalogo.Servidor1U, 4);  // 2.8 kW -> 22 + 14 = 36 graus
            DefinirNivel(e, Catalogo.NoBreak, 1);
            Assert.AreEqual(36, e.Temperatura, 1e-9);
            Assert.IsTrue(e.Quente);
            Assert.AreEqual(0.6, e.FatorTemperatura, 1e-9);
            DefinirNivel(e, Catalogo.ArCondicionado, 1);
            Assert.IsFalse(e.Quente);
        }

        // ---------- Incidentes ----------

        [Test]
        public void ServidorTravadoNaoRende()
        {
            var e = Nova();
            e.Travar(0);
            Assert.IsTrue(e.Travado(0));
            Assert.AreEqual(0, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CliqueNoServidorTravadoReinicia()
        {
            var e = Nova();
            bool? peloTecnico = null;
            e.Voltou += (s, t) => peloTecnico = t;
            e.Travar(0);
            Assert.AreEqual(0, e.Clicar(0), 1e-9, "reiniciar não rende bônus");
            Assert.IsFalse(e.Travado(0));
            Assert.AreEqual(1, e.Estado.incidentesResolvidos);
            Assert.AreEqual(false, peloTecnico);
        }

        [Test]
        public void TecnicoConsertaSozinhoDepoisDe30Segundos()
        {
            var e = Nova();
            bool? peloTecnico = null;
            e.Voltou += (s, t) => peloTecnico = t;
            e.Travar(0);
            e.Avancar(29);
            Assert.IsTrue(e.Travado(0));
            e.Avancar(1.5);
            Assert.IsFalse(e.Travado(0));
            Assert.AreEqual(true, peloTecnico);
        }

        [Test]
        public void EstagiarioFazOConsertoSairEm15Segundos()
        {
            var e = Nova(1_000_000);
            Assert.IsTrue(e.Comprar(Catalogo.Estagiario));
            e.Travar(0);
            e.Avancar(14);
            Assert.IsTrue(e.Travado(0));
            e.Avancar(1.5);
            Assert.IsFalse(e.Travado(0));
        }

        [Test]
        public void SorteioTravaServidores()
        {
            var e = Nova(sorteio: 0);
            int travadas = 0;
            e.Travou += _ => travadas++;
            e.Avancar(1);
            Assert.AreEqual(1, travadas);
            e.Avancar(1);
            Assert.AreEqual(1, travadas, "servidor já travado não trava de novo");
        }

        // ---------- Carreira ----------

        [Test]
        public void PromocaoExigeTodasAsMetas()
        {
            var e = Nova();
            Assert.IsFalse(e.PodePromover);
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.Estado.totalGanho = 20000;
            Assert.IsFalse(e.PodePromover, "faltam os incidentes");
            e.Estado.incidentesResolvidos = 8;
            Assert.IsTrue(e.PodePromover);

            int novoCargo = -1;
            e.Promoveu += c => novoCargo = c;
            Assert.IsTrue(e.Promover());
            Assert.AreEqual(1, e.Cargo);
            Assert.AreEqual(1, novoCargo);
            Assert.IsFalse(e.PodePromover, "o Sysadmin tem metas próprias");
        }

        [Test]
        public void SysadminViraAnalistaComORackCheio()
        {
            var e = Nova();
            e.Estado.cargo = 1;
            DefinirNivel(e, Catalogo.Rack, 1);
            DefinirNivel(e, Catalogo.Servidor1U, 4);
            e.Estado.totalGanho = 600000;
            e.Estado.incidentesResolvidos = 60;
            Assert.IsFalse(e.PodePromover, "falta um 1U para encher o rack");
            e.Estado.melhorias.Find(m => m.id == Catalogo.Servidor1U).nivel = 5;
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("Analista de Infra", e.CargoAtual.Nome);
            Assert.IsFalse(e.TemProximoCargo, "Analista é o último cargo por enquanto");
        }

        // ---------- Analista de Infra ----------

        [Test]
        public void SalaDeRacksTrazEnergiaERefrigeracao()
        {
            var e = Nova();
            e.Estado.cargo = 1;
            double kw = e.CapacidadeKw;
            e.Estado.cargo = 2;
            Assert.AreEqual(kw + Catalogo.CapacidadeSalaDeRacksKw, e.CapacidadeKw, 1e-9);
            double esperada = Catalogo.TemperaturaAmbiente + e.ConsumoKw * Catalogo.GrausPorKw * Catalogo.FatorCalorSalaDeRacks - Catalogo.GrausArDePrecisao;
            Assert.AreEqual(esperada, e.Temperatura, 1e-9);
        }

        [Test]
        public void RackCheioRendeEContaOitoServidores()
        {
            var e = Nova();
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.RackCheio, 1);
            Assert.AreEqual(1 + Catalogo.ServidoresPorRackCheio, e.ContagemServidores);
            Assert.AreEqual(1, e.TotalServidores, "rack cheio não entra na lista de servidores que travam");
            Assert.AreEqual(1 + Catalogo.ReceitaRackCheio, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void LinkSaturadoReduzAReceitaELinkDeFibraResolve()
        {
            var e = Nova();
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.RackCheio, 1);   // 10 + 160 = 170 Mbps, cabe nos 200
            Assert.IsFalse(e.LinkSaturado);
            e.Estado.melhorias.Find(m => m.id == Catalogo.RackCheio).nivel = 2; // 330 Mbps
            Assert.IsTrue(e.LinkSaturado);
            Assert.AreEqual(200.0 / 330.0, e.FatorBanda, 1e-9);
            DefinirNivel(e, Catalogo.Link, 1);
            Assert.IsFalse(e.LinkSaturado);
            Assert.AreEqual(1, e.FatorBanda, 1e-9);
        }

        [Test]
        public void StorageAumentaAReceitaEDiscoQueimadoTiraOBonus()
        {
            var e = Nova();
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.Storage, 2);
            Assert.AreEqual(1.5, e.ReceitaPorSegundo, 1e-9);
            e.QueimarDisco();
            Assert.IsTrue(e.DiscoQueimado);
            Assert.AreEqual(1, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void DiscoSemBackupCustaReembolso()
        {
            var e = Nova(1000);
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.Storage, 1);
            e.QueimarDisco();
            double perda = e.TrocarDisco(porTecnico: false);
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup * 1.25, perda, 1e-9);
            Assert.AreEqual(1000 - perda, e.Dinheiro, 1e-9);
            Assert.AreEqual(0, e.Estado.backupsRestaurados);
            Assert.AreEqual(1, e.Estado.incidentesResolvidos);
        }

        [Test]
        public void ComBackupOTecnicoRestauraSozinho()
        {
            var e = Nova(1000);
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.Storage, 1);
            DefinirNivel(e, Catalogo.Backup, 1);
            bool restaurou = false;
            e.DiscoTrocado += (backup, perda, tecnico) => restaurou = backup && perda == 0 && tecnico;
            e.QueimarDisco();
            e.Avancar(Catalogo.TempoConsertoTecnico);
            Assert.IsFalse(e.DiscoQueimado);
            Assert.IsTrue(restaurou);
            Assert.AreEqual(1, e.Estado.backupsRestaurados);
        }

        [Test]
        public void SorteioQueimaDiscoSoComStorage()
        {
            var e = Nova(sorteio: 0);
            e.Estado.cargo = 2;
            e.Avancar(1);
            Assert.IsFalse(e.DiscoQueimado, "sem storage não há disco");
            DefinirNivel(e, Catalogo.Storage, 1);
            e.Avancar(1);
            Assert.IsTrue(e.DiscoQueimado);
        }

        // ---------- Offline ----------

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
            Assert.AreEqual(0, Nova().AplicarOffline(1_000_000), 1e-9);
        }

        [Test]
        public void AoVoltarOTecnicoJaConsertouTudo()
        {
            var e = Nova();
            e.Travar(0);
            e.Estado.ultimoSalvamentoUnix = 1000;
            double ganho = e.AplicarOffline(1000 + 3600);
            Assert.IsFalse(e.Travado(0));
            Assert.AreEqual(1800, ganho, 1e-9);
            Assert.AreEqual(1, e.ConsertadosFora);
            Assert.AreEqual(3600, e.SegundosFora, 1e-9);
            Assert.IsFalse(e.PassouDoLimite);
        }
    }
}
