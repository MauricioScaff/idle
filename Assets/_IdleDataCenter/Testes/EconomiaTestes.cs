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
            Assert.IsFalse(e.PodePromover, "o Analista tem metas próprias");
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

        // ---------- Engenheiro DevOps ----------

        static Economia NoDevOps(double dinheiro = 0, double sorteio = 0.9999)
        {
            var e = Nova(dinheiro, sorteio);
            e.Estado.cargo = 3;
            return e;
        }

        [Test]
        public void AnalistaViraDevOpsComBackupEAutomacoes()
        {
            var e = Nova();
            e.Estado.cargo = 2;
            e.Estado.totalGanho = 5000000;
            e.Estado.automacoes.AddRange(new[] { Catalogo.Watchdog, Catalogo.HotSpare, Catalogo.CronFaturamento });
            Assert.IsFalse(e.PodePromover, "falta restaurar um backup");
            e.Estado.backupsRestaurados = 1;
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("Engenheiro DevOps", e.CargoAtual.Nome);
            Assert.IsFalse(e.PodePromover, "o DevOps tem metas próprias");
        }

        [Test]
        public void HypervisorAumentaSoOsServidores()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Hypervisor, 2);
            Assert.AreEqual(1.8, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Containers, 1);
            Assert.AreEqual(1.8 + Catalogo.ReceitaHostContainers, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CiAumentaOsAppsEDeployQuebradoDerrubaOsApps()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Containers, 1);
            DefinirNivel(e, Catalogo.ServidorCi, 1);
            Assert.AreEqual(1 + 375, e.ReceitaPorSegundo, 1e-9);
            e.QuebrarDeploy();
            Assert.IsTrue(e.DeployQuebrado);
            Assert.AreEqual(1, e.ReceitaPorSegundo, 1e-9);
            e.FazerRollback(porTecnico: false);
            Assert.AreEqual(1 + 375, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void RollbackAutomaticoEmCincoSegundos()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Containers, 1);
            e.QuebrarDeploy();
            e.Avancar(Catalogo.TempoRollbackAutomatico);
            Assert.IsTrue(e.DeployQuebrado, "sem a automação, o técnico leva 30 s");
            e.Estado.automacoes.Add(Catalogo.RollbackAutomatico);
            e.Avancar(0.01);
            Assert.IsFalse(e.DeployQuebrado);
        }

        [Test]
        public void PipelineComTestesQuebraMenosDeploys()
        {
            var sem = NoDevOps(sorteio: 0.5);
            DefinirNivel(sem, Catalogo.Containers, 1);
            sem.Avancar(1000);   // chance 1000/1200 > 0.5
            Assert.IsTrue(sem.DeployQuebrado);

            var com = NoDevOps(sorteio: 0.5);
            DefinirNivel(com, Catalogo.Containers, 1);
            com.Estado.automacoes.Add(Catalogo.Pipeline);
            com.Avancar(1000);   // chance 1000/4800 < 0.5
            Assert.IsFalse(com.DeployQuebrado);
        }

        [Test]
        public void InfraComoCodigoBarateiaAsMelhorias()
        {
            var e = NoDevOps();
            double antes = e.Custo(Catalogo.Hypervisor);
            e.Estado.automacoes.Add(Catalogo.InfraComoCodigo);
            Assert.AreEqual(Math.Round(antes * 0.85), e.Custo(Catalogo.Hypervisor), 1e-9);
        }

        [Test]
        public void AutomacoesDoDevOpsEsperamOCargo()
        {
            var e = Nova(1e8);
            e.Estado.cargo = 2;
            Assert.IsFalse(e.PodeEscrever(Catalogo.InfraComoCodigo));
            e.Estado.cargo = 3;
            Assert.IsTrue(e.PodeEscrever(Catalogo.InfraComoCodigo));
        }

        // ---------- SRE ----------

        static Economia NoSre(double dinheiro = 0, int nos = 1)
        {
            var e = Nova(dinheiro);
            e.Estado.cargo = 4;
            DefinirNivel(e, Catalogo.NoKubernetes, nos);
            DefinirNivel(e, Catalogo.Link10G, 1);   // banda de sobra: o teste é sobre o cluster
            return e;
        }

        [Test]
        public void DevOpsViraSre()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Containers, 4);
            e.Estado.totalGanho = 40000000;
            e.Estado.automacoes.AddRange(new[] { Catalogo.Watchdog, Catalogo.HotSpare, Catalogo.Monitoramento, Catalogo.CronFaturamento, Catalogo.Plantao, Catalogo.Pipeline });
            Assert.IsFalse(e.PodePromover, "faltam automações");
            e.Estado.automacoes.Add(Catalogo.RollbackAutomatico);
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("SRE", e.CargoAtual.Nome);
            Assert.IsFalse(e.PodePromover, "o SRE tem metas próprias");
        }

        [Test]
        public void NosKubernetesRendemComBalanceadorEObservabilidade()
        {
            var e = NoSre(nos: 2);
            Assert.AreEqual(1 + 800, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Balanceador, 1);
            Assert.AreEqual(1 + 1000, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Observabilidade, 2);
            Assert.AreEqual((1 + 1000) * 1.3, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void PicosSoComeçamNoSre()
        {
            var e = NoSre();
            e.Estado.cargo = 3;
            e.Avancar(Catalogo.PrimeiroPico + 10);
            Assert.IsFalse(e.EmPico);
            e.Estado.cargo = 4;
            string nome = null;
            e.PicoComecou += n => nome = n;
            for (int i = 0; i <= Catalogo.PrimeiroPico; i++) e.Avancar(1);
            Assert.IsTrue(e.EmPico);
            Assert.IsNotNull(nome);
        }

        [Test]
        public void PicoSemEscalarViolaOSlaECobraMulta()
        {
            var e = NoSre(1000000);
            e.ComecarPico();
            double receita = e.ReceitaPorSegundo;
            double multa = -1;
            bool? sobreviveu = null;
            e.SlaViolado += m => multa = m;
            e.PicoTerminou += s => sobreviveu = s;
            for (int i = 0; i < Catalogo.TempoParaEscalar; i++) e.Avancar(1);
            Assert.IsTrue(e.PicoViolado);
            Assert.AreEqual(receita * Catalogo.SegundosDeMultaSla, multa, 1e-6);
            Assert.AreEqual(Catalogo.MultiplicadorPicoViolado, e.MultiplicadorDoPico, 1e-9);
            for (int i = 0; i < Catalogo.DuracaoPico; i++) e.Avancar(1);
            Assert.IsFalse(e.EmPico);
            Assert.AreEqual(false, sobreviveu);
            Assert.AreEqual(0, e.Estado.picosSobrevividos);
        }

        [Test]
        public void PicoEscaladoRendeODobroEContaComoSobrevivido()
        {
            var e = NoSre();
            e.ComecarPico();
            Assert.IsTrue(e.Escalar());
            Assert.AreEqual(1 + 400 * Catalogo.MultiplicadorPicoEscalado, e.ReceitaPorSegundo, 1e-9);
            for (int i = 0; i <= Catalogo.DuracaoPico; i++) e.Avancar(1);
            Assert.AreEqual(1, e.Estado.picosSobrevividos);
            Assert.AreEqual(1, e.Progresso(new MetaDef { Tipo = TipoMeta.PicosSobrevividos }), 1e-9);
        }

        [Test]
        public void BalanceadorDaMaisTempoParaEscalar()
        {
            var e = NoSre();
            DefinirNivel(e, Catalogo.Balanceador, 1);
            e.ComecarPico();
            for (int i = 0; i < Catalogo.TempoParaEscalar + 2; i++) e.Avancar(1);
            Assert.IsFalse(e.PicoViolado);
            Assert.IsTrue(e.Escalar());
        }

        [Test]
        public void AutoscalingEscalaSozinho()
        {
            var e = NoSre();
            e.Estado.automacoes.Add(Catalogo.Autoscaling);
            e.ComecarPico();
            for (int i = 0; i < Catalogo.TempoAutoscaling; i++) e.Avancar(1);
            Assert.IsTrue(e.PicoFoiEscalado);
        }

        [Test]
        public void RunbooksConsertamTudoEmTresSegundos()
        {
            var e = NoSre();
            e.Estado.automacoes.AddRange(new[] { Catalogo.Watchdog, Catalogo.Runbooks });
            Assert.AreEqual(Catalogo.TempoRunbook, e.TempoConserto, 1e-9);
            Assert.AreEqual(Catalogo.TempoRunbook, e.TempoTrocaDisco, 1e-9);
            Assert.AreEqual(Catalogo.TempoRunbook, e.TempoRollback, 1e-9);
        }

        // ---------- Arquiteto ----------

        static Economia NoCampus(double dinheiro = 0, double sorteio = 0.9999)
        {
            var e = Nova(dinheiro, sorteio);
            e.Estado.cargo = 5;
            return e;
        }

        [Test]
        public void SreViraArquiteto()
        {
            var e = NoSre();
            e.Estado.picosSobrevividos = 5;
            e.Estado.totalGanho = 250000000;
            e.Estado.automacoes.AddRange(new[] { Catalogo.Watchdog, Catalogo.HotSpare, Catalogo.Monitoramento, Catalogo.CronFaturamento, Catalogo.Plantao,
                                                 Catalogo.Pipeline, Catalogo.RollbackAutomatico, Catalogo.InfraComoCodigo, Catalogo.Autoscaling });
            Assert.IsFalse(e.PodePromover, "faltam automações");
            e.Estado.automacoes.Add(Catalogo.Chaos);
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("Arquiteto", e.CargoAtual.Nome);
            Assert.IsFalse(e.TemProximoCargo, "Arquiteto é o último cargo por enquanto");
        }

        [Test]
        public void DatacenterNovoRendeComFibraECdn()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.Datacenter, 1);
            Assert.AreEqual(1 + Catalogo.ReceitaDatacenter, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Fibra, 1);
            Assert.AreEqual((1 + Catalogo.ReceitaDatacenter) * 1.2, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Cdn, 1);
            Assert.AreEqual((1 + Catalogo.ReceitaDatacenter) * 1.2 * 1.3, e.ReceitaPorSegundo, 1e-6);
        }

        [Test]
        public void FibraSoContaParaDatacentersQueExistem()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.Datacenter, 1);
            DefinirNivel(e, Catalogo.Fibra, 3);
            Assert.AreEqual(1, e.DatacentersInterligados);
        }

        [Test]
        public void QuedaDeEnergiaDerrubaUmDatacenterEGeradorReligaRapido()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.Datacenter, 2);
            e.DerrubarEnergia(2);
            Assert.IsTrue(e.TemQuedaDeEnergia);
            Assert.AreEqual(1 + Catalogo.ReceitaDatacenter, e.ReceitaPorSegundo, 1e-6, "um dos dois DCs parado");
            e.Avancar(Catalogo.TempoGerador);
            Assert.IsTrue(e.TemQuedaDeEnergia, "sem gerador o técnico leva 30 s");
            DefinirNivel(e, Catalogo.Gerador, 1);
            e.Avancar(0.01);
            Assert.IsFalse(e.TemQuedaDeEnergia);
        }

        [Test]
        public void CdnAliviaOLinkDoDc01()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.RackCheio, 2);   // 10 + 320 Mbps no link de 200
            Assert.IsTrue(e.LinkSaturado);
            DefinirNivel(e, Catalogo.Cdn, 2);          // 40% do tráfego: 132 Mbps
            Assert.IsFalse(e.LinkSaturado);
        }

        // ---------- Automações ----------

        [Test]
        public void AutomacoesLiberamNoAnalista()
        {
            var e = Nova(1e7);
            e.Estado.cargo = 1;
            Assert.IsFalse(e.PodeEscrever(Catalogo.Watchdog));
            e.Estado.cargo = 2;
            Assert.IsTrue(e.PodeEscrever(Catalogo.Watchdog));
            Assert.IsFalse(e.PodeEscrever(Catalogo.HotSpare), "troca de disco precisa de storage");
        }

        [Test]
        public void AutomacaoLevaTempoParaEscreverEUmaPorVez()
        {
            var e = Nova(1e7);
            e.Estado.cargo = 2;
            string pronta = null;
            e.AutomacaoPronta += id => pronta = id;
            Assert.IsTrue(e.EscreverAutomacao(Catalogo.Watchdog));
            Assert.AreEqual(1e7 - Catalogo.BuscarAutomacao(Catalogo.Watchdog).Custo, e.Dinheiro, 1e-6);
            Assert.IsFalse(e.PodeEscrever(Catalogo.CronFaturamento), "uma por vez");
            e.Avancar(Catalogo.BuscarAutomacao(Catalogo.Watchdog).Segundos - 1);
            Assert.IsFalse(e.TemAutomacao(Catalogo.Watchdog));
            Assert.AreEqual(Catalogo.TempoConsertoTecnico, e.TempoConserto, 1e-9);
            e.Avancar(1);
            Assert.IsTrue(e.TemAutomacao(Catalogo.Watchdog));
            Assert.AreEqual(Catalogo.Watchdog, pronta);
            Assert.AreEqual(Catalogo.TempoWatchdog, e.TempoConserto, 1e-9);
            Assert.IsTrue(e.PodeEscrever(Catalogo.CronFaturamento));
        }

        [Test]
        public void HotSpareTrocaODiscoSozinho()
        {
            var e = Nova();
            e.Estado.cargo = 2;
            DefinirNivel(e, Catalogo.Storage, 1);
            e.Estado.automacoes.Add(Catalogo.HotSpare);
            e.QueimarDisco();
            e.Avancar(Catalogo.TempoHotSpare);
            Assert.IsFalse(e.DiscoQueimado);
        }

        [Test]
        public void CronEPlantaoLevamOOfflineA100PorCento()
        {
            var e = Nova();
            Assert.AreEqual(0.5, e.TaxaOffline, 1e-9);
            e.Estado.automacoes.Add(Catalogo.CronFaturamento);
            Assert.AreEqual(0.75, e.TaxaOffline, 1e-9);
            e.Estado.automacoes.Add(Catalogo.Plantao);
            Assert.AreEqual(1.0, e.TaxaOffline, 1e-9);
            Assert.AreEqual(24 * 3600, e.CalcularGanhoOffline(48 * 3600), 1e-9);
        }

        [Test]
        public void AutomacaoContinuaSendoEscritaOffline()
        {
            var e = Nova(1e7);
            e.Estado.cargo = 2;
            e.EscreverAutomacao(Catalogo.Plantao);
            e.Estado.ultimoSalvamentoUnix = 1000;
            e.AplicarOffline(1000 + 3600);
            Assert.IsTrue(e.TemAutomacao(Catalogo.Plantao));
            Assert.IsFalse(e.Escrevendo);
        }

        // ---------- Café e chamados ----------

        [Test]
        public void CafeDobraAReceitaPorTrintaSegundosEDepoisRecarrega()
        {
            var e = Nova();
            Assert.IsTrue(e.TomarCafe());
            Assert.AreEqual(2, e.ReceitaPorSegundo, 1e-9);
            Assert.IsFalse(e.TomarCafe(), "ainda recarregando");
            e.Avancar(Catalogo.DuracaoCafe);
            Assert.IsFalse(e.CafeAtivo);
            Assert.AreEqual(1, e.ReceitaPorSegundo, 1e-9);
            e.Avancar(Catalogo.RecargaCafe - Catalogo.DuracaoCafe);
            Assert.IsTrue(e.PodeTomarCafe);
        }

        [Test]
        public void ChamadoApareceEPagaSeAtendidoATempo()
        {
            var e = Nova();
            string texto = null;
            e.ChamadoApareceu += s => texto = s;
            for (int i = 0; i < Catalogo.PrimeiroChamado; i++) e.Avancar(1);
            Assert.IsTrue(e.TemChamado);
            Assert.IsNotNull(texto);
            double antes = e.Dinheiro;
            Assert.AreEqual(Catalogo.SegundosDeBonusDoChamado, e.AtenderChamado(), 1e-9, "60 s da receita de R$ 1/s");
            Assert.AreEqual(antes + Catalogo.SegundosDeBonusDoChamado, e.Dinheiro, 1e-9);
            Assert.IsFalse(e.TemChamado);
        }

        [Test]
        public void ChamadoIgnoradoSomeSemPagar()
        {
            var e = Nova();
            e.AbrirChamado();
            double pago = -1;
            e.ChamadoEncerrado += b => pago = b;
            for (int i = 0; i < Catalogo.TempoParaAtender; i++) e.Avancar(1);
            Assert.IsFalse(e.TemChamado);
            Assert.AreEqual(0, pago, 1e-9);
            Assert.AreEqual(0, e.AtenderChamado(), 1e-9);
        }

        [Test]
        public void CafeEChamadoNaoValemOffline()
        {
            var e = Nova();
            e.TomarCafe();
            e.AbrirChamado();
            e.Estado.ultimoSalvamentoUnix = 1000;
            double ganho = e.AplicarOffline(1000 + 3600);
            Assert.AreEqual(1800, ganho, 1e-9, "offline sem o café");
            Assert.IsFalse(e.TemChamado);
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
