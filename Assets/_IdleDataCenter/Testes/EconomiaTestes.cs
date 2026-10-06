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

        /// <summary>Renda de uma torre sem melhorias (as contas dos testes partem dela).</summary>
        const double R = Catalogo.ReceitaBaseServidor;

        /// <summary>Empresa nova já no Técnico (quase todos os testes são das salas); o Freelancer tem os testes dele.</summary>
        static Economia Nova(double dinheiro = 0, double sorteio = 0.9999) =>
            new Economia(new EstadoJogo { dinheiro = dinheiro, cargo = Catalogo.CargoTecnico }, new SorteioFixo(sorteio));

        static void DefinirNivel(Economia e, string id, int nivel) =>
            e.Estado.melhorias.Add(new NivelMelhoria { id = id, nivel = nivel });

        // ---------- Receita e compras ----------

        [Test]
        public void ComecaComUmServidorRendendoUmPorSegundo()
        {
            var e = Nova();
            Assert.AreEqual(1, e.TotalServidores);
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void AvancarAcumulaReceita()
        {
            var e = Nova();
            e.Avancar(10);
            Assert.AreEqual(10 * R, e.Dinheiro, 1e-9);
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
            var e = Nova(Catalogo.Buscar(Catalogo.Ssd).CustoBase + 50);
            Assert.IsTrue(e.Comprar(Catalogo.Ssd));
            Assert.AreEqual(50, e.Dinheiro, 1e-9);
            Assert.AreEqual(2 * R, e.ReceitaPorSegundo, 1e-9);
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
            Assert.AreEqual(Catalogo.Buscar(Catalogo.Ventoinha).CustoBase, e.Custo(Catalogo.Ventoinha), 1e-9);
            e.Comprar(Catalogo.Ventoinha);
            Assert.AreEqual(Math.Round(Catalogo.Buscar(Catalogo.Ventoinha).CustoBase * 2.2), e.Custo(Catalogo.Ventoinha), 1e-9);
        }

        [Test]
        public void TorresExtrasMultiplicamAReceitaComMelhorias()
        {
            var e = Nova(1_000_000);
            e.Comprar(Catalogo.Ssd);        // 2 por torre
            e.Comprar(Catalogo.Ventoinha);  // 3 por torre
            e.Comprar(Catalogo.Servidor);   // 2 torres
            Assert.AreEqual(6 * R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CliqueRendeBaseMaisDobroDaReceita()
        {
            var e = Nova();
            Assert.AreEqual(Catalogo.ValorCliqueBase + Catalogo.ValorCliqueReceita * R, e.Clicar(0), 1e-9);
            Assert.IsTrue(e.Estado.jaClicouNoServidor);
        }

        [Test]
        public void LojaMostraSoAsMelhoriasDoCargo()
        {
            var e = Nova();
            CollectionAssert.AreEquivalent(new[] { Catalogo.Ssd, Catalogo.Ventoinha, Catalogo.Servidor, Catalogo.FiltroDeLinha, Catalogo.Ventilador, Catalogo.PastaTermica, Catalogo.HdExterno, Catalogo.Antivirus, Catalogo.Estagiario },
                System.Linq.Enumerable.Select(e.MelhoriasDoCargo(), m => m.Id));
            e.Estado.cargo = Catalogo.CargoSysadmin;
            Assert.AreEqual(9, System.Linq.Enumerable.Count(e.MelhoriasDoCargo()), "o analista de help desk entra no Sysadmin");
        }

        [Test]
        public void Servidor1UPrecisaDoRack()
        {
            var e = Nova(1_000_000);
            e.Estado.cargo = Catalogo.CargoSysadmin;
            Assert.IsFalse(e.Comprar(Catalogo.Servidor1U));
            Assert.IsTrue(e.Comprar(Catalogo.Rack));
            Assert.IsTrue(e.Comprar(Catalogo.Servidor1U));
            Assert.AreEqual(2, e.TotalServidores);
            Assert.AreEqual(R + Catalogo.ReceitaServidor1U, e.ReceitaPorSegundo, 1e-9);
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
            Assert.AreEqual((3 * R + 2 * Catalogo.ReceitaServidor1U) * 1.5 / 2.0, e.ReceitaPorSegundo, 1e-9);
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
            e.Estado.totalGanho = 200000;
            Assert.IsFalse(e.PodePromover, "faltam os incidentes");
            e.Estado.incidentesResolvidos = 8;
            Assert.IsTrue(e.PodePromover);

            int novoCargo = -1;
            e.Promoveu += c => novoCargo = c;
            Assert.IsTrue(e.Promover());
            Assert.AreEqual(Catalogo.CargoSysadmin, e.Cargo);
            Assert.AreEqual(Catalogo.CargoSysadmin, novoCargo);
            Assert.IsFalse(e.PodePromover, "o Sysadmin tem metas próprias");
        }

        [Test]
        public void SysadminViraAnalistaComORackCheio()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoSysadmin;
            DefinirNivel(e, Catalogo.Rack, 1);
            DefinirNivel(e, Catalogo.Servidor1U, 4);
            e.Estado.totalGanho = 7200000;
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
            e.Estado.cargo = Catalogo.CargoSysadmin;
            double kw = e.CapacidadeKw;
            e.Estado.cargo = Catalogo.CargoAnalista;
            Assert.AreEqual(kw + Catalogo.CapacidadeSalaDeRacksKw, e.CapacidadeKw, 1e-9);
            double esperada = System.Math.Max(Catalogo.TemperaturaMinima,
                Catalogo.TemperaturaAmbiente + e.ConsumoKw * Catalogo.GrausPorKw * Catalogo.FatorCalorSalaDeRacks - Catalogo.GrausArDePrecisao);
            Assert.AreEqual(esperada, e.Temperatura, 1e-9);
        }

        [Test]
        public void RackCheioRendeEContaOitoServidores()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.RackCheio, 1);
            Assert.AreEqual(1 + Catalogo.ServidoresPorRackCheio, e.ContagemServidores);
            Assert.AreEqual(1, e.TotalServidores, "rack cheio não entra na lista de servidores que travam");
            Assert.AreEqual(R + Catalogo.ReceitaRackCheio, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void LinkSaturadoReduzAReceitaELinkDeFibraResolve()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
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
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.Storage, 2);
            Assert.AreEqual(1.5 * R, e.ReceitaPorSegundo, 1e-9);
            e.QueimarDisco();
            Assert.IsTrue(e.DiscoQueimado);
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void DiscoSemBackupCustaReembolso()
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.Storage, 1);
            e.QueimarDisco();
            double perda = e.TrocarDisco(porTecnico: false);
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup * 1.25 * R, perda, 1e-9);
            Assert.AreEqual(1000 - perda, e.Dinheiro, 1e-9);
            Assert.AreEqual(0, e.Estado.backupsRestaurados);
            Assert.AreEqual(1, e.Estado.incidentesResolvidos);
        }

        [Test]
        public void ComALinhaDeBackupInteiraOTecnicoRestauraSemPerder()
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.Storage, 1);
            foreach (var id in Catalogo.LinhaDeBackup) DefinirNivel(e, id, 1);
            bool restaurou = false;
            e.DiscoTrocado += (backup, perda, tecnico) => restaurou = backup && perda == 0 && tecnico;
            e.QueimarDisco();
            e.Avancar(Catalogo.TempoConsertoTecnico);
            Assert.IsFalse(e.DiscoQueimado);
            Assert.IsTrue(restaurou);
            Assert.AreEqual(1, e.Estado.backupsRestaurados);
        }

        [Test]
        public void SemStorageQuemQueimaEOHdDaTorre()
        {
            var e = Nova(sorteio: 0);
            e.Avancar(1);
            Assert.IsTrue(e.DiscoQueimado, "o HD da torre também queima");
        }

        [Test]
        public void CadaNivelDeBackupCortaAPerdaPelaMetade()
        {
            double PerdaCom(int niveis)
            {
                var e = Nova(1000000);
                for (int i = 0; i < niveis; i++) DefinirNivel(e, Catalogo.LinhaDeBackup[i], 1);
                e.QueimarDisco();
                return e.TrocarDisco(porTecnico: false) / e.ReceitaPorSegundo;   // em segundos de receita
            }
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup, PerdaCom(0), 1e-9);
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup / 2, PerdaCom(1), 1e-9, "HD externo");
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup / 4, PerdaCom(2), 1e-9, "NAS");
            Assert.AreEqual(Catalogo.SegundosPerdidosSemBackup / 8, PerdaCom(3), 1e-9, "fita");
            Assert.AreEqual(0, PerdaCom(Catalogo.LinhaDeBackup.Length), 1e-9, "linha inteira");
        }

        [Test]
        public void CadaNivelDeBackupDaCincoPorCentoDeReceita()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.HdExterno, 1);
            DefinirNivel(e, Catalogo.Nas, 1);
            Assert.AreEqual(R * (1 + 2 * Catalogo.BonusPorBackup), e.ReceitaPorSegundo, 1e-9);
        }

        // ---------- Engenheiro DevOps ----------

        static Economia NoDevOps(double dinheiro = 0, double sorteio = 0.9999)
        {
            var e = Nova(dinheiro, sorteio);
            e.Estado.cargo = Catalogo.CargoDevOps;
            return e;
        }

        [Test]
        public void AnalistaViraDevOpsComBackupEAutomacoes()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
            e.Estado.totalGanho = 180000000;
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
            Assert.AreEqual(1.8 * R, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Containers, 1);
            Assert.AreEqual(1.8 * R + Catalogo.ReceitaHostContainers, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void CiAumentaOsAppsEDeployQuebradoDerrubaOsApps()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Containers, 1);
            DefinirNivel(e, Catalogo.ServidorCi, 1);
            Assert.AreEqual(R + Catalogo.ReceitaHostContainers * 1.5, e.ReceitaPorSegundo, 1e-9);
            e.QuebrarDeploy();
            Assert.IsTrue(e.DeployQuebrado);
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9);
            e.FazerRollback(porTecnico: false);
            Assert.AreEqual(R + Catalogo.ReceitaHostContainers * 1.5, e.ReceitaPorSegundo, 1e-9);
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
            e.Estado.cargo = Catalogo.CargoAnalista;
            Assert.IsFalse(e.PodeEscrever(Catalogo.InfraComoCodigo));
            e.Estado.cargo = Catalogo.CargoDevOps;
            Assert.IsTrue(e.PodeEscrever(Catalogo.InfraComoCodigo));
        }

        // ---------- SRE ----------

        static Economia NoSre(double dinheiro = 0, int nos = 1)
        {
            var e = Nova(dinheiro);
            e.Estado.cargo = Catalogo.CargoSre;
            DefinirNivel(e, Catalogo.NoKubernetes, nos);
            DefinirNivel(e, Catalogo.Link10G, 1);   // banda de sobra: o teste é sobre o cluster
            return e;
        }

        [Test]
        public void DevOpsViraSre()
        {
            var e = NoDevOps();
            DefinirNivel(e, Catalogo.Containers, 4);
            e.Estado.totalGanho = 6000000000;
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
            Assert.AreEqual(R + 2 * Catalogo.ReceitaNoKubernetes, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Balanceador, 1);
            Assert.AreEqual(R + 2 * Catalogo.ReceitaNoKubernetes * 1.25, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Observabilidade, 2);
            Assert.AreEqual((R + 2 * Catalogo.ReceitaNoKubernetes * 1.25) * 1.3, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void PicosSoComeçamNoSre()
        {
            var e = NoSre();
            e.Estado.cargo = Catalogo.CargoDevOps;
            e.Avancar(Catalogo.PrimeiroPico + 10);
            Assert.IsFalse(e.EmPico);
            e.Estado.cargo = Catalogo.CargoSre;
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
            e.Avancar(0);   // as conquistas do cargo entram antes de medir a receita
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
            Assert.AreEqual(R + Catalogo.ReceitaNoKubernetes * Catalogo.MultiplicadorPicoEscalado, e.ReceitaPorSegundo, 1e-9);
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
            e.Estado.cargo = Catalogo.CargoArquiteto;
            return e;
        }

        [Test]
        public void SreViraArquiteto()
        {
            var e = NoSre();
            e.Estado.picosSobrevividos = 5;
            e.Estado.totalGanho = 24000000000;
            e.Estado.automacoes.AddRange(new[] { Catalogo.Watchdog, Catalogo.HotSpare, Catalogo.Monitoramento, Catalogo.CronFaturamento, Catalogo.Plantao,
                                                 Catalogo.Pipeline, Catalogo.RollbackAutomatico, Catalogo.InfraComoCodigo, Catalogo.Autoscaling });
            Assert.IsFalse(e.PodePromover, "faltam automações");
            e.Estado.automacoes.Add(Catalogo.Chaos);
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("Arquiteto", e.CargoAtual.Nome);
            Assert.IsFalse(e.PodePromover, "o Arquiteto tem metas próprias");
        }

        [Test]
        public void DatacenterNovoRendeComFibraECdn()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.Datacenter, 1);
            Assert.AreEqual(R + Catalogo.ReceitaDatacenter, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Fibra, 1);
            Assert.AreEqual((R + Catalogo.ReceitaDatacenter) * 1.2, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Cdn, 1);
            Assert.AreEqual((R + Catalogo.ReceitaDatacenter) * 1.2 * 1.3, e.ReceitaPorSegundo, 1e-6);
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
            Assert.AreEqual(R + Catalogo.ReceitaDatacenter, e.ReceitaPorSegundo, 1e-6, "um dos dois DCs parado");
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

        // ---------- CTO e IPO ----------

        static Economia NoMundo(double dinheiro = 0)
        {
            var e = Nova(dinheiro);
            e.Estado.cargo = Catalogo.CargoCto;
            return e;
        }

        [Test]
        public void ArquitetoViraCto()
        {
            var e = NoCampus();
            DefinirNivel(e, Catalogo.Datacenter, 3);
            e.Estado.totalGanho = 240000000000;
            for (int i = 0; i < 12; i++) e.Estado.automacoes.Add(Catalogo.Automacoes[i].Id);
            Assert.IsTrue(e.Promover());
            Assert.AreEqual("CTO", e.CargoAtual.Nome);
            Assert.IsFalse(e.TemProximoCargo, "CTO é o último cargo");
            Assert.IsFalse(e.PodePromover);
        }

        [Test]
        public void RegiaoCaboRenovavelEGpuRendem()
        {
            var e = NoMundo();
            DefinirNivel(e, Catalogo.Regiao, 1);
            Assert.AreEqual(R + Catalogo.ReceitaRegiao, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.CaboSubmarino, 1);
            Assert.AreEqual(R + Catalogo.ReceitaRegiao * 1.25, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Gpu, 1);
            Assert.AreEqual(R + Catalogo.ReceitaRegiao * 1.25 + Catalogo.ReceitaGpu, e.ReceitaPorSegundo, 1e-6);
            DefinirNivel(e, Catalogo.Renovavel, 2);
            Assert.AreEqual((R + Catalogo.ReceitaRegiao * 1.25 + Catalogo.ReceitaGpu) * 1.3, e.ReceitaPorSegundo, 1e-6);
        }

        [Test]
        public void PaneRegionalDerrubaUmaRegiaoEFailoverRedireciona()
        {
            var e = NoMundo();
            DefinirNivel(e, Catalogo.Regiao, 2);
            e.DerrubarRegiao(1);
            Assert.IsTrue(e.TemPaneRegional);
            Assert.AreEqual(R + Catalogo.ReceitaRegiao, e.ReceitaPorSegundo, 1e-6);
            e.Estado.automacoes.Add(Catalogo.Multirregiao);
            e.Avancar(Catalogo.TempoFailoverMultirregiao);
            Assert.IsFalse(e.TemPaneRegional);
        }

        [Test]
        public void IpoFechaACarreira()
        {
            var e = NoMundo();
            Assert.IsFalse(e.PodeFazerIpo);
            DefinirNivel(e, Catalogo.Regiao, 3);
            e.Estado.totalGanho = 3600000000000;
            for (int i = 0; i < 15; i++) e.Estado.automacoes.Add(Catalogo.Automacoes[i].Id);
            Assert.IsTrue(e.PodeFazerIpo);
            bool festa = false;
            e.Ipo += () => festa = true;
            Assert.IsTrue(e.FazerIpo());
            Assert.IsTrue(festa && e.IpoFeito);
            Assert.IsFalse(e.PodeFazerIpo, "só uma vez");
        }

        // ---------- Prestígio ----------

        [Test]
        public void SoDaParaVenderAPartirDoSre()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoDevOps;
            e.Estado.totalGanho = 1e9;
            Assert.IsFalse(e.PodeVender);
            Assert.AreEqual(0, e.CertificacoesDaVenda);
            e.Estado.cargo = Catalogo.CargoSre;
            Assert.AreEqual(31, e.CertificacoesDaVenda, "raiz de 1000 milhões");
        }

        [Test]
        public void VenderReiniciaMasGuardaCertificacoesETrofeu()
        {
            var e = NoSre(5000);
            e.Estado.totalGanho = 400e6;
            e.Estado.prestigio.bonus.Add(new NivelMelhoria { id = Catalogo.AwsEstagiario, nivel = 2 });
            int ganhas = -1;
            e.Vendeu += c => ganhas = c;
            Assert.IsTrue(e.VenderEmpresa(Catalogo.SemCafe));
            Assert.AreEqual(20, ganhas);
            Assert.AreEqual(0, e.Cargo);
            Assert.AreEqual(1, e.TotalServidores, "começa de novo no armário");
            Assert.AreEqual(2000, e.Dinheiro, 1e-9, "AWS Certified Estagiário nível 2");
            Assert.AreEqual(20, e.Prestigio.certificacoes);
            Assert.AreEqual(1, e.Prestigio.empresasVendidas);
            CollectionAssert.AreEqual(new[] { "SRE" }, e.Prestigio.trofeus);
            Assert.IsFalse(e.TomarCafe(), "desafio sem café");
            Assert.IsTrue(e.TutorialConcluido);
        }

        [Test]
        public void IpoEDesafioMultiplicamAsCertificacoes()
        {
            var e = NoMundo();
            e.Estado.totalGanho = 100e6;          // raiz = 10
            e.Estado.ipoFeito = true;             // x2
            e.Estado.desafio = Catalogo.SoAutomacao;   // x1,5
            Assert.AreEqual(30, e.CertificacoesDaVenda);
        }

        [Test]
        public void CertificacoesCompramBonusPermanentes()
        {
            var e = Nova();
            e.Prestigio.certificacoes = 9;
            Assert.IsTrue(e.ComprarCertificacao(Catalogo.UptimeWizard));   // custa 3
            Assert.IsTrue(e.ComprarCertificacao(Catalogo.UptimeWizard));   // custa 6
            Assert.AreEqual(0, e.Prestigio.certificacoes);
            Assert.IsFalse(e.ComprarCertificacao(Catalogo.UptimeWizard));
            Assert.AreEqual(1.2 * R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void DesafiosTravamOQuePrometem()
        {
            var e = Nova(1e6);
            e.Estado.desafio = Catalogo.SemEstagiario;
            Assert.IsFalse(e.PodeComprar(Catalogo.Estagiario));
            e.Estado.desafio = Catalogo.SoAutomacao;
            Assert.AreEqual(0, e.ValorClique, 1e-9);
        }

        [Test]
        public void PrestigioSobreviveAoSave()
        {
            var e = Nova();
            e.Prestigio.certificacoes = 7;
            e.Prestigio.trofeus.Add("IPO");
            var json = UnityEngine.JsonUtility.ToJson(e.Estado);
            var volta = UnityEngine.JsonUtility.FromJson<EstadoJogo>(json);
            Assert.AreEqual(7, volta.prestigio.certificacoes);
            CollectionAssert.AreEqual(new[] { "IPO" }, volta.prestigio.trofeus);
        }

        // ---------- Compatibilidade de save ----------

        [Test]
        public void SaveAntigoCarregaSemSustos()
        {
            // formato do começo do projeto: nada de café, chamados, picos, quedas, prestígio...
            const string antigo = "{\"versao\":2,\"dinheiro\":3400,\"totalGanho\":9000,\"cargo\":0,\"incidentesResolvidos\":4,"
                                + "\"melhorias\":[{\"id\":\"ssd\",\"nivel\":1}],\"travamentos\":[],\"ultimoSalvamentoUnix\":1000,\"jaClicouNoServidor\":true}";
            var estado = UnityEngine.JsonUtility.FromJson<EstadoJogo>(antigo);
            var e = new Economia(estado, new SorteioFixo(0.9999));
            Assert.AreEqual(Catalogo.CargoTecnico, e.Cargo, "save de antes do Freelancer: quem era Técnico continua Técnico");
            Assert.AreEqual(EstadoJogo.VersaoAtual, e.Estado.versao);
            Assert.AreEqual(2 * R, e.ReceitaPorSegundo, 1e-9);
            Assert.IsFalse(e.TemQuedaDeEnergia);
            Assert.IsFalse(e.TemPaneRegional);
            Assert.IsNotNull(e.Prestigio);
            e.Avancar(1);
            Assert.IsFalse(e.TemChamado, "o primeiro chamado espera o tempo normal");
            Assert.IsFalse(e.EmPico);
        }

        // ---------- Freelancer ----------

        static Economia Freelancer(double dinheiro = 0) => new Economia(new EstadoJogo { dinheiro = dinheiro }, new SorteioFixo(0.9999));

        [Test]
        public void JogoNovoComecaComoFreelancer()
        {
            var e = Freelancer();
            Assert.AreEqual(Catalogo.CargoFreelancer, e.Cargo);
            CollectionAssert.AreEquivalent(new[] { Catalogo.SiteCliente, Catalogo.KitFerramentas, Catalogo.CartaoDeVisita, Catalogo.Hospedagem },
                System.Linq.Enumerable.Select(e.MelhoriasDoCargo(), m => m.Id));
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9, "só a torre velha");
        }

        [Test]
        public void SitesRendemNaTorreVelhaAteOLimite()
        {
            var e = Freelancer();
            DefinirNivel(e, Catalogo.SiteCliente, 4);
            Assert.AreEqual(R + 4 * Catalogo.ReceitaSite, e.ReceitaPorSegundo, 1e-9);
            DefinirNivel(e, Catalogo.Hospedagem, 1);
            Assert.AreEqual(R + 4 * Catalogo.ReceitaSite * 1.5, e.ReceitaPorSegundo, 1e-9, "hospedagem caprichada: +50%");
            e.Estado.melhorias.Clear();
            DefinirNivel(e, Catalogo.SiteCliente, Catalogo.SitesNaTorre);
            Assert.IsTrue(e.NoMaximo(Catalogo.SiteCliente), "a torre velha não aguenta mais sites");
        }

        [Test]
        public void ConsertoDoFreelancerTemPrecoDeBairro()
        {
            var e = Freelancer();
            e.Estado.proximoChamado = 1e9;
            var c = e.AbrirChamado(3);
            CollectionAssert.Contains(Catalogo.TextosDosConsertos[2], c.texto, "chega um PC na bancada, não um chamado de empresa");
            Assert.AreEqual(Catalogo.PrecoDoConserto[2], e.AtenderChamado(), 1e-9);
            DefinirNivel(e, Catalogo.KitFerramentas, 2);
            e.AbrirChamado(4);
            Assert.AreEqual(Catalogo.PrecoDoConserto[3] * 2, e.AtenderChamado(), 1e-9, "kit nível 2: +100%");
        }

        [Test]
        public void FreelancerConsertaSozinhoDevagar()
        {
            var e = Freelancer();
            e.Estado.proximoChamado = 1e9;
            e.AbrirChamado(3);
            e.AbrirChamado(2);
            double pago = 0;
            e.ChamadoEncerrado += (c, b, equipe) => { if (equipe) pago = b; };
            for (int i = 0; i < Catalogo.TempoDoFreelancerNoConserto - 1; i++) e.Avancar(1);
            Assert.AreEqual(2, e.Chamados.Count, "ainda no meio do conserto");
            e.Avancar(1);
            Assert.AreEqual(1, e.Chamados.Count);
            Assert.AreEqual(2, e.Chamados[0].prioridade, "o P2 é urgente: espera você");
            Assert.AreEqual(Catalogo.PrecoDoConserto[2] * Catalogo.FracaoDaEquipe, pago, 1e-9);
            Assert.AreEqual(1, e.Estado.chamadosAtendidos);
        }

        [Test]
        public void CartaoDeVisitaTrazMaisPcs()
        {
            var e = Freelancer();
            DefinirNivel(e, Catalogo.CartaoDeVisita, 2);
            e.Estado.proximoChamado = 0.5;
            e.Avancar(1);
            Assert.IsTrue(e.TemChamado);
            double normal = Catalogo.IntervaloChamadoMin + 0.9999 * (Catalogo.IntervaloChamadoMax - Catalogo.IntervaloChamadoMin);
            Assert.AreEqual(normal * (1 - 2 * Catalogo.MaisPcsPorCartao), e.Estado.proximoChamado, 1e-6);
        }

        [Test]
        public void MetasDoFreelancerTrazemAContratacao()
        {
            var e = Freelancer();
            DefinirNivel(e, Catalogo.SiteCliente, 5);
            e.Estado.totalGanho = 15000;
            Assert.IsFalse(e.PodePromover, "faltam os consertos");
            e.Estado.chamadosAtendidos = 10;
            Assert.IsTrue(e.Promover());
            Assert.AreEqual(Catalogo.CargoTecnico, e.Cargo);
            Assert.AreEqual(R + 5 * Catalogo.ReceitaSite, e.ReceitaPorSegundo, 1e-9, "os sites vêm junto para a empresa");
        }

        // ---------- Automações ----------

        [Test]
        public void AutomacoesLiberamNoAnalista()
        {
            var e = Nova(1e7);
            e.Estado.cargo = Catalogo.CargoSysadmin;
            Assert.IsFalse(e.PodeEscrever(Catalogo.Watchdog));
            e.Estado.cargo = Catalogo.CargoAnalista;
            Assert.IsTrue(e.PodeEscrever(Catalogo.Watchdog));
            Assert.IsFalse(e.PodeEscrever(Catalogo.HotSpare), "troca de disco precisa de storage");
        }

        [Test]
        public void AutomacaoLevaTempoParaEscreverEUmaPorVez()
        {
            var e = Nova(1e7);
            e.Estado.cargo = Catalogo.CargoAnalista;
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
            e.Estado.cargo = Catalogo.CargoAnalista;
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
            Assert.AreEqual(24 * 3600 * R, e.CalcularGanhoOffline(48 * 3600), 1e-9);
        }

        [Test]
        public void AutomacaoContinuaSendoEscritaOffline()
        {
            var e = Nova(1e7);
            e.Estado.cargo = Catalogo.CargoAnalista;
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
            Assert.AreEqual(2 * R, e.ReceitaPorSegundo, 1e-9);
            Assert.IsFalse(e.TomarCafe(), "ainda recarregando");
            e.Avancar(Catalogo.DuracaoCafe);
            Assert.IsFalse(e.CafeAtivo);
            Assert.AreEqual(R * e.FatorConquistas, e.ReceitaPorSegundo, 1e-9);   // depois do Avancar: o crachá novo (contratado) já valeu
            e.Avancar(Catalogo.RecargaCafe - Catalogo.DuracaoCafe);
            Assert.IsTrue(e.PodeTomarCafe);
        }

        [Test]
        public void ChamadoChegaNaFilaEPagaPelaPrioridade()
        {
            var e = Nova();
            ChamadoAberto chegou = null;
            e.ChamadoApareceu += c => chegou = c;
            for (int i = 0; i < Catalogo.PrimeiroChamado; i++) e.Avancar(1);
            Assert.IsTrue(e.TemChamado);
            Assert.IsNotNull(chegou);
            Assert.AreEqual(4, chegou.prioridade, "no armário, o sorteio alto dá P4");
            double antes = e.Dinheiro;
            Assert.AreEqual(25, e.AtenderChamado(), 1e-9, "P4 paga 6 s de R$ 1/s, mas no mínimo R$ 25");
            Assert.AreEqual(antes + 25, e.Dinheiro, 1e-9);
            Assert.IsFalse(e.TemChamado);
            e.AbrirChamado(1);
            Assert.AreEqual(Catalogo.BonusDoChamadoEmSegundos[0] * R * e.FatorConquistas, e.AtenderChamado(), 1e-9, "P1 paga 1 min de receita");
        }

        [Test]
        public void FilaTemLimiteEOMaisUrgenteVemPrimeiro()
        {
            var e = Nova();
            e.AbrirChamado(4); e.AbrirChamado(3); e.AbrirChamado(1); e.AbrirChamado(2); e.AbrirChamado(4);
            Assert.IsNull(e.AbrirChamado(1), "fila cheia: o sexto não entra");
            Assert.AreEqual(Catalogo.ChamadosNaFila, e.Chamados.Count);
            Assert.AreEqual(1, e.Chamados[0].prioridade);
            Assert.AreEqual(2, e.Chamados[1].prioridade);
            Assert.AreEqual(1, e.PrioridadeDoChamado);
            Assert.IsTrue(e.TemChamadoUrgente);
            e.AtenderChamado(2);   // o terceiro da fila: o P3
            Assert.AreEqual(0, System.Linq.Enumerable.Count(e.Chamados, c => c.prioridade == 3));
        }

        [Test]
        public void ChamadoIgnoradoSomeSemPagar()
        {
            var e = Nova();
            e.Estado.proximoChamado = 1e9;
            e.AbrirChamado(3);
            double pago = -1;
            e.ChamadoEncerrado += (c, b, equipe) => pago = b;
            for (int i = 0; i < Catalogo.PrazoDoChamado[2]; i++) e.Avancar(1);
            Assert.IsFalse(e.TemChamado);
            Assert.AreEqual(0, pago, 1e-9);
            Assert.AreEqual(0, e.AtenderChamado(), 1e-9);
        }

        [Test]
        public void P1EstouradoDerrubaOUptime()
        {
            var e = Nova();
            e.Estado.proximoChamado = 1e9;
            e.Estado.uptime = 0.9995;
            e.AbrirChamado(1);
            for (int i = 0; i < Catalogo.PrazoDoChamado[0]; i++) e.Avancar(1);
            Assert.IsFalse(e.TemChamado);
            Assert.Less(e.Uptime, 0.9995 - 0.001, "5 s fora do ar na média da última hora");
        }

        [Test]
        public void EquipeFechaOQueEDaAlcadaDela()
        {
            var e = Nova();
            e.Estado.proximoChamado = 1e9;
            DefinirNivel(e, Catalogo.Estagiario, 1);
            e.AbrirChamado(4); e.AbrirChamado(3);
            double pago = 0; bool porEquipe = false;
            e.ChamadoEncerrado += (c, b, equipe) => { pago = b; porEquipe = equipe; };
            for (int i = 0; i < Catalogo.TempoDaEquipeNoChamado; i++) e.Avancar(1);
            Assert.AreEqual(1, e.Chamados.Count, "o estagiário fechou o P4");
            Assert.AreEqual(3, e.Chamados[0].prioridade, "o P3 não é com ele");
            Assert.IsTrue(porEquipe);
            Assert.AreEqual(25 * Catalogo.FracaoDaEquipe, pago, 1e-9, "a equipe recebe metade");
            for (int i = 0; i < Catalogo.TempoDaEquipeNoChamado; i++) e.Avancar(1);
            Assert.AreEqual(1, e.Chamados.Count);
            DefinirNivel(e, Catalogo.HelpDesk, 1);
            for (int i = 0; i < Catalogo.TempoDaEquipeNoChamado; i++) e.Avancar(1);
            Assert.IsFalse(e.TemChamado, "o analista de help desk fecha o P3");
            e.AbrirChamado(1);
            DefinirNivel(e, Catalogo.ServiceDesk, 1);
            for (int i = 0; i < Catalogo.TempoDaEquipeNoChamado; i++) e.Avancar(1);
            Assert.IsTrue(e.TemChamado, "P1 é sempre com o jogador");
        }

        [Test]
        public void ChamadoDeSaveAntigoViraP3NaFila()
        {
            var e = Nova();
            e.Estado.chamadoRestante = 12;
            e.Estado.chamadoTexto = "Impressora não imprime";
            Assert.AreEqual(1, e.Chamados.Count);
            Assert.AreEqual(3, e.Chamados[0].prioridade);
            Assert.AreEqual("Impressora não imprime", e.TextoDoChamado);
            Assert.AreEqual(0, e.Estado.chamadoRestante, 1e-9);
        }

        [Test]
        public void CafeEChamadoNaoValemOffline()
        {
            var e = Nova();
            e.TomarCafe();
            e.AbrirChamado();
            e.Estado.ultimoSalvamentoUnix = 1000;
            double ganho = e.AplicarOffline(1000 + 3600);
            Assert.AreEqual(1800 * R, ganho, 1e-9, "offline sem o café");
            Assert.IsFalse(e.TemChamado);
        }

        // ---------- Conquistas ----------

        [Test]
        public void ConquistaDaBonusEPassaParaAProximaEmpresa()
        {
            var e = Nova();
            int avisos = 0;
            e.Conquistou += c => avisos++;
            DefinirNivel(e, Catalogo.Servidor, 1);
            e.Avancar(0);
            Assert.IsTrue(e.TemConquista("hello"), "duas torres");
            Assert.AreEqual(1 + Catalogo.BonusPorConquista * e.NumeroDeConquistas, e.FatorConquistas, 1e-9);
            Assert.IsFalse(e.Conquistar("hello"), "só uma vez");
            Assert.AreEqual(e.NumeroDeConquistas, avisos);

            e.Estado.cargo = Catalogo.CargoParaVender;
            e.Estado.totalGanho = 4e6;
            int antes = e.NumeroDeConquistas;
            Assert.IsTrue(e.VenderEmpresa());
            Assert.IsTrue(e.TemConquista("hello"), "conquista fica no prestígio");
            Assert.IsTrue(e.TemConquista(Catalogo.ConquistaExit));
            Assert.Greater(e.NumeroDeConquistas, antes);
        }

        [Test]
        public void EventoDeHumorViraConquista()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoDevOps;
            e.ComecarEvento(Catalogo.EventoDns);
            Assert.IsTrue(e.TemConquista(Catalogo.ConquistaDns));
        }

        // ---------- Clientes ----------

        static Economia ComClientes(double uptime)
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoSysadmin;
            e.Estado.proximoEvento = 1e9;
            e.Estado.proximoChamado = 1e9;
            e.Estado.proximaProposta = 1e9;
            e.Estado.uptime = uptime;
            e.Avancar(0);
            return e;
        }

        static Cliente ComUmCliente(Economia e)
        {
            e.ProporCliente();
            Assert.IsTrue(e.AceitarCliente());
            var c = e.Clientes[0];
            c.proximoPedido = 1e9;
            return c;
        }

        [Test]
        public void ClienteSoPedeOSlaQueOUptimeEntrega()
        {
            var e = ComClientes(0.9962);
            var p = e.ProporCliente();
            Assert.IsNotNull(p);
            Assert.AreEqual(0.995, p.sla, 1e-9, "o maior SLA abaixo do uptime de agora");
            Assert.IsTrue(e.TemPropostaDeCliente);
            e.Estado.uptime = 0.97;
            e.RecusarCliente();
            Assert.IsNull(e.ProporCliente(), "abaixo de 98% ninguém quer hospedar aqui");
        }

        [Test]
        public void ClienteAceitoRendeConformeOPorteEASatisfacao()
        {
            var e = ComClientes(0.9999);
            double antes = e.ReceitaPorSegundo;
            var c = ComUmCliente(e);
            Assert.IsFalse(e.TemPropostaDeCliente);
            Assert.AreEqual(1, c.porte);
            double bonus = Catalogo.BonusPorPorte[1] * (0.5 + 0.5 * Catalogo.SatisfacaoInicial / 100);
            Assert.AreEqual(antes * (1 + bonus), e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void SiteCaiQuandoOServidorDeleTravaEOClienteSeIrrita()
        {
            var e = ComClientes(0.9999);
            var c = ComUmCliente(e);
            Assert.IsFalse(e.SiteFora(c));
            Cliente caiu = null;
            e.SiteCaiu += x => caiu = x;
            e.Travar(c.servidor);
            Assert.IsTrue(e.SiteFora(c));
            e.Avancar(5);
            Assert.AreEqual(c, caiu);
            Assert.Less(c.satisfacao, Catalogo.SatisfacaoInicial - 5);
            Assert.Greater(c.foraDoAr, 0);
        }

        [Test]
        public void ClienteSatisfeitoCresceDePorte()
        {
            var e = ComClientes(0.9999);
            var c = ComUmCliente(e);
            c.satisfacao = 100;
            Cliente cresceu = null;
            e.ClienteCresceu += x => cresceu = x;
            for (int i = 0; i <= Catalogo.MinutosParaCrescer[1] * 60; i++) e.Avancar(1);
            Assert.AreEqual(2, c.porte);
            Assert.AreEqual(c, cresceu);
            Assert.AreEqual("Loja virtual", Catalogo.PortesDoSite[c.porte]);
        }

        [Test]
        public void ClienteInsatisfeitoVaiEmbora()
        {
            var e = ComClientes(0.9999);
            var c = ComUmCliente(e);
            Cliente saiu = null;
            e.ClienteSaiu += x => saiu = x;
            c.satisfacao = 1;
            e.Travar(c.servidor);
            e.Avancar(2);
            Assert.AreEqual(0, e.Clientes.Count);
            Assert.AreEqual(c, saiu);
        }

        [Test]
        public void PedidoCumpridoPagaEAceleraOCrescimento()
        {
            var e = ComClientes(0.9999);
            var c = ComUmCliente(e);
            Assert.IsTrue(e.FazerPedido(c, Catalogo.PedidoServidores));
            Assert.AreEqual(e.ContagemServidores + 1 + c.porte, c.pedidoAlvo, 1e-9);
            StringAssert.Contains("servidores", e.TextoDoPedido(c));
            bool? cumpriu = null;
            e.PedidoTerminou += (x, ok, premio) => cumpriu = ok;
            DefinirNivel(e, Catalogo.Servidor, 5);
            double antes = e.Dinheiro;
            e.Avancar(0.1);
            Assert.AreEqual(true, cumpriu);
            Assert.AreEqual("", c.pedido);
            Assert.AreEqual(1, e.Estado.pedidosAtendidos);
            Assert.AreEqual(Catalogo.CrescimentoDoPedido, c.crescimento, 0.01);
            Assert.Greater(e.Dinheiro, antes);
        }

        [Test]
        public void PedidoPerdidoNoPrazoIrrita()
        {
            var e = ComClientes(0.9999);
            var c = ComUmCliente(e);
            Assert.IsTrue(e.FazerPedido(c, Catalogo.PedidoServidores));
            c.proximoPedido = 1e9;
            bool? cumpriu = null;
            e.PedidoTerminou += (x, ok, premio) => cumpriu = ok;
            c.pedidoRestante = 1;
            c.satisfacao = 90;
            e.Avancar(1.5);
            Assert.AreEqual(false, cumpriu);
            Assert.Less(c.satisfacao, 90 - Catalogo.SatisfacaoDoPedidoPerdido + 2);
            Assert.AreEqual(0, e.Estado.pedidosAtendidos);
        }

        [Test]
        public void ContratosDosSavesAntigosViramClientes()
        {
            var e = ComClientes(0.9999);
            e.Estado.contratos.Add(new Contrato { cliente = "Banco Pixel", sla = 0.99 });
            e.Avancar(0);
            Assert.AreEqual(1, e.Clientes.Count);
            Assert.AreEqual("Banco Pixel", e.Clientes[0].nome);
            Assert.AreEqual(0.99, e.Clientes[0].sla, 1e-9);
            Assert.AreEqual(0, e.Estado.contratos.Count);
        }

        [Test]
        public void PropostaSemRespostaVenceEOCargoLimitaOsClientes()
        {
            var e = ComClientes(0.9999);
            e.ProporCliente();
            for (int i = 0; i < Catalogo.TempoParaAceitar; i++) e.Avancar(1);
            Assert.IsFalse(e.TemPropostaDeCliente, "venceu");
            for (int i = 0; i < Catalogo.CapacidadeDeClientes(Catalogo.CargoSysadmin); i++) { e.ProporCliente(); Assert.IsTrue(e.AceitarCliente()); }
            e.ProporCliente();
            Assert.IsFalse(e.PodeAceitarCliente, "o Sysadmin cuida de 3 clientes");
            Assert.IsFalse(e.AceitarCliente());
            Assert.AreEqual(Catalogo.CapacidadeDeClientes(Catalogo.CargoSysadmin), e.Clientes.Count);
            Assert.AreEqual(Catalogo.CapacidadeDeClientes(Catalogo.CargoSysadmin), new System.Collections.Generic.HashSet<string>(e.Clientes.ConvertAll(c => c.nome)).Count, "nomes diferentes");
        }

        // ---------- Offline ----------

        [Test]
        public void OfflineRendeMetadeComLimiteDeDozeHoras()
        {
            var e = Nova();
            Assert.AreEqual(0, e.CalcularGanhoOffline(30), 1e-9, "menos de 1 minuto não conta");
            Assert.AreEqual(1800 * R, e.CalcularGanhoOffline(3600), 1e-9);
            Assert.AreEqual(12 * 3600 * 0.5 * R, e.CalcularGanhoOffline(48 * 3600), 1e-9);
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
            Assert.AreEqual(1800 * R, ganho, 1e-9);
            Assert.AreEqual(1, e.ConsertadosFora);
            Assert.AreEqual(3600, e.SegundosFora, 1e-9);
            Assert.IsFalse(e.PassouDoLimite);
        }

        // ---------- Geradores sem teto, marcos e melhorias simples ----------

        [Test]
        public void MarcosDobramARendaDoGerador()
        {
            Assert.AreEqual(1, Economia.FatorMarcos(9));
            Assert.AreEqual(2, Economia.FatorMarcos(10));
            Assert.AreEqual(4, Economia.FatorMarcos(25));
            Assert.AreEqual(16, Economia.FatorMarcos(100));

            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.RackCheio, 9);
            // sem os fatores de energia, calor e banda (10 racks estouram a sala)
            double Placa() => e.ReceitaDosRacksCheios / (e.FatorGeral * e.FatorVirtualizacao);
            Assert.AreEqual(9 * Catalogo.ReceitaRackCheio, Placa(), 1e-6);
            e.Estado.melhorias.Find(m => m.id == Catalogo.RackCheio).nivel = 10;
            Assert.AreEqual(10 * Catalogo.ReceitaRackCheio * 2, Placa(), 1e-6);   // o marco dobrou tudo
            Assert.AreEqual(25, e.ProximoMarco(Catalogo.RackCheio));
        }

        [Test]
        public void AsTorresContamAPrimeiraParaOsMarcos()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 9);   // 1 + 9 = 10 torres
            Assert.AreEqual(10, e.Torres);
            Assert.AreEqual(25, e.ProximoMarco(Catalogo.Servidor));
            Assert.AreEqual(Catalogo.ReceitaBaseServidor * 2, e.ReceitaTorre, 1e-9);
        }

        [Test]
        public void MelhoriasSimplesAumentamOAlvo()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.PastaTermica, 2);
            Assert.AreEqual(Catalogo.ReceitaBaseServidor * 1.6, e.ReceitaTorre, 1e-9);

            e.Estado.cargo = Catalogo.CargoSysadmin;
            DefinirNivel(e, Catalogo.CabosOrganizados, 1);
            DefinirNivel(e, Catalogo.Firmware, 1);
            Assert.AreEqual(Catalogo.ReceitaServidor1U * (1 + 0.25 + 0.4), e.Receita1U, 1e-9);
        }

        [Test]
        public void FiltroDeLinhaDaEnergiaEVentiladorEsfria()
        {
            var e = Nova();
            double kw = e.CapacidadeKw, graus = e.Temperatura;
            DefinirNivel(e, Catalogo.FiltroDeLinha, 2);
            DefinirNivel(e, Catalogo.Ventilador, 1);
            Assert.AreEqual(kw + 1.6, e.CapacidadeKw, 1e-9);
            Assert.AreEqual(graus - 3, e.Temperatura, 1e-9);
        }

        [Test]
        public void SugestaoResolveOGargaloAntes()
        {
            var e = Nova(1_000_000);
            DefinirNivel(e, Catalogo.Servidor, 5);   // 6 torres: 2.4 kW para 1.5 kW
            Assert.IsTrue(e.Sobrecarga);
            Assert.AreEqual(Catalogo.FiltroDeLinha, e.MelhoriaSugerida().Id);
            DefinirNivel(e, Catalogo.FiltroDeLinha, 2);
            Assert.IsFalse(e.Sobrecarga);
            Assert.AreNotEqual(Catalogo.FiltroDeLinha, e.MelhoriaSugerida().Id);
        }

        [Test]
        public void SoOsPrimeirosServidoresTravam()
        {
            var e = Nova(0, 0);   // o sorteio sempre "acerta": todo servidor que puder travar, trava
            DefinirNivel(e, Catalogo.Servidor, 19);   // 20 torres
            e.Avancar(1);
            Assert.AreEqual(Catalogo.ServidoresQueTravam, e.Travamentos.Count);
        }

        [Test]
        public void ArCondicionadoNaoDeixaASalaAbaixoDoMinimo()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.ArCondicionado, 20);
            DefinirNivel(e, Catalogo.Ventilador, 12);
            Assert.AreEqual(Catalogo.TemperaturaMinima, e.Temperatura, 1e-9);
        }

        [Test]
        public void SaveComMelhoriaQueNaoExisteMaisNaoQuebra()
        {
            var e = Nova();
            DefinirNivel(e, "melhoria-antiga", 3);
            Assert.DoesNotThrow(() => { var _ = e.ReceitaPorSegundo; });
        }

        // ---------- Eventos aleatórios ----------

        [Test]
        public void ClienteGrandeAceitoDobraARendaEMultaSeAlgoTravar()
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoSysadmin;
            e.ComecarEvento(Catalogo.EventoCliente);
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9, "na oferta ainda não dobra");
            Assert.IsTrue(e.AgirNoEvento());
            Assert.AreEqual(2 * R, e.ReceitaPorSegundo, 1e-9);
            double multa = 0;
            e.EventoTerminou += (def, valor) => multa = valor;
            e.Travar(0);
            Assert.IsFalse(e.TemEvento, "o cliente cancela");
            Assert.AreEqual(-Catalogo.SegundosDeMultaDoCliente * R, multa, 1e-9);
        }

        [Test]
        public void ClienteGrandeIgnoradoVaiEmboraSemCustar()
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoSysadmin;
            e.ComecarEvento(Catalogo.EventoCliente);
            e.Avancar(Catalogo.Eventos[0].Duracao + 1);
            Assert.IsFalse(e.TemEvento);
            Assert.Greater(e.Dinheiro, 1000);
        }

        [Test]
        public void AuditoriaDaBonusComTudoFuncionandoEMultaComIncidente()
        {
            var e = Nova(1000);
            e.Estado.cargo = Catalogo.CargoAnalista;
            e.Avancar(0);   // as conquistas do cargo entram antes de medir a receita
            double resultado = 0;
            e.EventoTerminou += (def, valor) => resultado = valor;
            e.ComecarEvento(Catalogo.EventoAuditoria);
            e.Avancar(61);
            Assert.AreEqual(Catalogo.SegundosDeBonusDaAuditoria * R * e.FatorConquistas, resultado, 1e-9);

            DefinirNivel(e, Catalogo.Servidor, 1);   // duas torres: com uma travada ainda há receita para a multa
            e.ComecarEvento(Catalogo.EventoAuditoria);
            e.Avancar(55);
            e.Travar(0);
            e.Avancar(6);
            Assert.Less(resultado, 0, "servidor travado na hora da auditoria");
        }

        [Test]
        public void InternetCaidaCortaARendaEO4GSeguraUmPouco()
        {
            var e = Nova();
            e.ComecarEvento(Catalogo.EventoInternet);
            Assert.AreEqual(Catalogo.FatorInternetCaida * R, e.ReceitaPorSegundo, 1e-9);
            e.AgirNoEvento();
            Assert.AreEqual(Catalogo.FatorInternet4G * R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void DoisLinksDeFibraEvitamAQuedaDaInternet()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoAnalista;
            DefinirNivel(e, Catalogo.Link, Catalogo.LinksQueEvitamAQuedaDaInternet);
            e.ComecarEvento(Catalogo.EventoInternet);
            Assert.IsFalse(e.TemEvento);
        }

        [Test]
        public void SemCafeOTecnicoDemoraODobroAteComprarem()
        {
            var e = Nova(1000);
            double normal = e.TempoConserto;
            e.ComecarEvento(Catalogo.EventoCafeAcabou);
            Assert.AreEqual(normal * Catalogo.FatorConsertoSemCafe, e.TempoConserto, 1e-9);
            Assert.IsTrue(e.AgirNoEvento(), "comprar café");
            Assert.IsFalse(e.TemEvento);
            Assert.AreEqual(normal, e.TempoConserto, 1e-9);
        }

        [Test]
        public void EventosSoComOJogoAberto()
        {
            var e = Nova();
            e.Avancar(Catalogo.PrimeiroEvento + 1);
            Assert.IsTrue(e.TemEvento, "jogando, o primeiro evento chega");
        }

        // ---------- Segurança ----------

        [Test]
        public void SemSegurancaOAtaqueAcontece()
        {
            var e = Nova();
            e.ComecarEvento(Catalogo.AtaqueMalware);
            Assert.IsTrue(e.TemEvento);
            Assert.AreEqual(Catalogo.FatorMalware * R, e.ReceitaPorSegundo, 1e-9);
            Assert.IsTrue(e.AgirNoEvento(), "limpar");
            Assert.IsFalse(e.TemEvento);
        }

        [Test]
        public void SegurancaBloqueiaOAtaqueEAvisaQuemBloqueou()
        {
            var e = Nova(sorteio: 0);
            DefinirNivel(e, Catalogo.Antivirus, 1);
            DefinirNivel(e, Catalogo.Firewall, 1);
            string quem = null;
            e.AtaqueBloqueado += (def, nome) => quem = nome;
            e.ComecarEvento(Catalogo.AtaqueMalware);
            Assert.IsFalse(e.TemEvento);
            Assert.AreEqual("Firewall", quem);
            Assert.AreEqual(0.75, e.ProtecaoSeguranca, 1e-9);
        }

        [Test]
        public void PhishingTravaDoisServidores()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 3);
            e.ComecarEvento(Catalogo.AtaquePhishing);
            Assert.AreEqual(Catalogo.ServidoresDoPhishing, e.Travamentos.Count);
        }

        [Test]
        public void RansomwareComFitaRestauraESemBackupCobraResgate()
        {
            var e = Nova(1000000);
            e.Estado.cargo = Catalogo.CargoAnalista;
            e.ComecarEvento(Catalogo.AtaqueRansomware);
            Assert.AreEqual(Catalogo.FatorRansomware * R, e.ReceitaPorSegundo, 1e-9);
            double antes = e.Dinheiro;
            Assert.IsTrue(e.AgirNoEvento(), "pagar o resgate");
            Assert.AreEqual(antes - Catalogo.SegundosDoResgate * R, e.Dinheiro, 1e-6);

            DefinirNivel(e, Catalogo.HdExterno, 1); DefinirNivel(e, Catalogo.Nas, 1); DefinirNivel(e, Catalogo.Backup, 1);
            e.ComecarEvento(Catalogo.AtaqueRansomware);
            antes = e.Dinheiro;
            Assert.IsTrue(e.RestauraRansomware);
            Assert.IsTrue(e.AgirNoEvento(), "restaurar do backup");
            Assert.AreEqual(antes, e.Dinheiro, 1e-9, "restaurar não custa nada");
            Assert.IsFalse(e.TemEvento);
        }

        [Test]
        public void DdosBloquearIpsSeguraQuaseTudo()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoSre;
            e.ComecarEvento(Catalogo.AtaqueDdos);
            Assert.AreEqual(Catalogo.FatorDdos, e.FatorEvento, 1e-9);
            e.AgirNoEvento();
            Assert.AreEqual(Catalogo.FatorDdosBloqueado, e.FatorEvento, 1e-9);
        }

        // ---------- Uptime e SLA ----------

        [Test]
        public void UptimeComecaEm99EUmServidorForaOBaixa()
        {
            var e = Nova();
            Assert.AreEqual(0.99, e.Uptime, 1e-12, "sem histórico, nada de SLA");
            Assert.IsNull(e.Sla);
            e.Estado.uptime = 1;
            e.Travar(0);
            Assert.AreEqual(1, e.Indisponibilidade, 1e-9, "o único servidor caiu: tudo fora");
            for (int s = 0; s <= Catalogo.TempoConsertoTecnico; s++) e.Avancar(1);   // o jogo avança em passos curtos
            Assert.Less(e.Uptime, 0.999, "trinta segundos fora já tiram os três noves");
        }

        [Test]
        public void TresNovesOuMaisPagamSla()
        {
            var e = Nova();
            e.Estado.uptime = 0.9995;
            Assert.AreEqual(R * 1.025, e.ReceitaPorSegundo, 1e-9);
            e.Estado.uptime = 0.99995;
            Assert.AreEqual(R * 1.05, e.ReceitaPorSegundo, 1e-9);
            e.Estado.uptime = 0.995;
            Assert.IsNull(e.Sla);
            Assert.AreEqual(R, e.ReceitaPorSegundo, 1e-9);
        }

        [Test]
        public void UptimeEscritoComoEmTI()
        {
            Assert.AreEqual("99,95%", Economia.FormatarUptime(0.99954));
            Assert.AreEqual("99,995%", Economia.FormatarUptime(0.99995));
            Assert.AreEqual("97,3%", Economia.FormatarUptime(0.973));
        }

        // ---------- Eventos de humor de TI ----------

        [Test]
        public void SslExpiradoCortaARendaAteRenovar()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoSysadmin;
            e.ComecarEvento(Catalogo.EventoSsl);
            Assert.AreEqual(Catalogo.FatorSslExpirado, e.FatorEvento, 1e-9);
            Assert.IsTrue(e.AgirNoEvento(), "renovar");
            Assert.IsFalse(e.TemEvento);
        }

        [Test]
        public void DeployNaSextaAsVezesPagaAsVezesQuebra()
        {
            var sorte = Nova(1000);
            sorte.Estado.cargo = Catalogo.CargoDevOps;
            DefinirNivel(sorte, Catalogo.Containers, 1);
            double ganho = 0;
            sorte.EventoTerminou += (def, valor) => ganho = valor;
            sorte.ComecarEvento(Catalogo.EventoDeploySexta);
            sorte.AgirNoEvento();
            Assert.Greater(ganho, 0, "deu certo");
            Assert.IsFalse(sorte.DeployQuebrado);

            var azar = Nova(1000, sorteio: 0);
            azar.Estado.cargo = Catalogo.CargoDevOps;
            DefinirNivel(azar, Catalogo.Containers, 1);
            azar.ComecarEvento(Catalogo.EventoDeploySexta);
            azar.AgirNoEvento();
            Assert.IsTrue(azar.DeployQuebrado, "quebrou na sexta");
        }

        [Test]
        public void RatoNoCaboDerrubaUmServidor()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.ComecarEvento(Catalogo.EventoRato);
            Assert.AreEqual(1, e.Travamentos.Count);
        }

        // ---------- Ciclo de vida do hardware ----------

        [Test]
        public void ServidoresEnvelhecemETravamMaisForaDaGarantia()
        {
            var e = Nova();
            Assert.AreEqual(1, e.FatorIdade, 1e-9);
            bool avisou = false;
            e.HardwareEnvelheceu += garantia => avisou = garantia;
            e.Estado.idadeServidores = Catalogo.FimDaGarantia - 1;
            e.Avancar(2);
            Assert.IsTrue(e.ForaDaGarantia);
            Assert.IsTrue(avisou);
            Assert.AreEqual(Catalogo.FalhasForaDaGarantia, e.FatorIdade, 1e-9);
            e.Estado.idadeServidores = Catalogo.FimDaVida;
            Assert.AreEqual(Catalogo.FalhasNoFimDaVida, e.FatorIdade, 1e-9);
        }

        [Test]
        public void RefreshZeraAIdadeECustaMinutosDeReceita()
        {
            var e = Nova(10000);
            Assert.IsFalse(e.FazerRefresh(), "na garantia não precisa");
            e.Estado.idadeServidores = Catalogo.FimDaGarantia;
            double custo = e.CustoDoRefresh;
            Assert.AreEqual(Catalogo.RefreshMinimo, custo, 1e-9, "receita pequena: preço mínimo");
            Assert.IsTrue(e.FazerRefresh());
            Assert.AreEqual(0, e.IdadeDosServidores, 1e-9);
            Assert.AreEqual(10000 - custo, e.Dinheiro, 1e-9);
        }
        // ---------- Terminal ----------

        [Test]
        public void ServidorTravadoApareceNoTerminalEORebootResolveComBonus()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.Travar(1);
            var problemas = e.ProblemasNoTerminal();
            Assert.AreEqual(1, problemas.Count);
            StringAssert.Contains("srv02", problemas[0].Log);
            e.Avancar(0);   // as conquistas de agora já contam na renda
            double antes = e.Dinheiro;
            var saida = e.Executar("ssh srv-02 sudo reboot");
            Assert.AreEqual(TipoLinha.Ok, saida[0].Tipo);
            Assert.IsFalse(e.Travado(1));
            Assert.AreEqual(antes + e.ReceitaPorSegundo * Catalogo.SegundosDeBonusDoTerminal, e.Dinheiro, 1e-6);
            Assert.AreEqual(1, e.Estado.comandosCertos);
            Assert.AreEqual(1, e.ComboDoTerminal);
        }

        [Test]
        public void ComandoErradoNaoResolveEZeraOCombo()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.Travar(0);
            e.Travar(2);
            e.Executar("reboot srv01");
            Assert.AreEqual(1, e.ComboDoTerminal);
            var saida = e.Executar("reboot srv09");   // servidor que não travou
            Assert.AreEqual(TipoLinha.Erro, saida[0].Tipo);
            Assert.IsTrue(e.Travado(2));
            Assert.AreEqual(0, e.ComboDoTerminal);
            Assert.AreEqual(TipoLinha.Erro, e.Executar("rebot srv03")[0].Tipo, "erro de digitação");
            Assert.IsTrue(e.Travado(2));
        }

        [Test]
        public void ComboAumentaOBonusAteOMaximo()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 5);
            for (int s = 0; s < 3; s++) e.Travar(s);
            e.Avancar(0);
            e.Executar("reboot srv01");
            e.Executar("reboot srv02");
            Assert.AreEqual(1 + 2 * Catalogo.BonusPorCombo, e.MultiplicadorDoCombo, 1e-9);
            double antes = e.Dinheiro;
            e.Executar("reboot srv03");
            Assert.AreEqual(antes + e.ReceitaPorSegundo * Catalogo.SegundosDeBonusDoTerminal * (1 + 2 * Catalogo.BonusPorCombo), e.Dinheiro, 1e-6);
        }

        [Test]
        public void DiscoEDeployTemComandosProprios()
        {
            var e = Nova();
            e.QueimarDisco();
            Assert.AreEqual("fsck -y /dev/sda", e.ProblemasNoTerminal()[0].Exemplo);
            e.Executar("fsck /dev/sda");
            Assert.IsFalse(e.DiscoQueimado);

            DefinirNivel(e, Catalogo.Storage, 1);
            e.QueimarDisco();
            Assert.AreEqual(TipoLinha.Erro, e.Executar("fsck /dev/sda")[0].Tipo, "com RAID, fsck não troca disco");
            e.Executar("mdadm /dev/md0 --add /dev/sdd");
            Assert.IsFalse(e.DiscoQueimado);

            DefinirNivel(e, Catalogo.Containers, 1);
            e.QuebrarDeploy();
            e.Executar("kubectl rollout undo deployment/api");
            Assert.IsFalse(e.DeployQuebrado);
        }

        [Test]
        public void EventoDeTiResolvidoNoTerminal()
        {
            var e = Nova();
            e.Estado.cargo = Catalogo.CargoSysadmin;
            e.ComecarEvento(Catalogo.EventoDns);
            Assert.AreEqual(Catalogo.EventoDns, e.Estado.evento);
            Assert.AreEqual(TipoLinha.Ok, e.Executar("systemctl restart named")[0].Tipo);
            Assert.IsFalse(e.TemEvento);
        }

        [Test]
        public void ComandosDoDiaADiaNaoZeramOCombo()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.Travar(0);
            e.Executar("reboot srv01");
            Assert.AreEqual(TipoLinha.Limpar, e.Executar("clear")[0].Tipo);
            Assert.AreEqual(TipoLinha.Sair, e.Executar("exit")[0].Tipo);
            Assert.Greater(e.Executar("help").Count, 0);
            e.Executar("whoami");
            Assert.AreEqual(1, e.ComboDoTerminal);
        }

        [Test]
        public void TabCompletaOComandoDoProblema()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            e.Travar(1);
            Assert.AreEqual("ssh srv02 sudo reboot", e.Completar("ss"));
            Assert.AreEqual("fortune", e.Completar("fo"));
            Assert.AreEqual("xyz", e.Completar("xyz"));
        }

        [Test]
        public void DezAcertosNoTerminalDaoConquista()
        {
            var e = Nova();
            DefinirNivel(e, Catalogo.Servidor, 2);
            for (int i = 0; i < Catalogo.ComandosParaConquista; i++)
            {
                e.Travar(0);
                e.Executar("reboot srv01");
            }
            Assert.IsTrue(e.TemConquista("terminal"));
        }
        [Test]
        public void DinheiroEmPortuguesComTodosOsDigitosAteUmMilhao()
        {
            Assert.AreEqual("0", Economia.FormatarDinheiro(0));
            Assert.AreEqual("2,5", Economia.FormatarDinheiro(2.56));
            Assert.AreEqual("16.342", Economia.FormatarDinheiro(16342.9));
            Assert.AreEqual("999.999", Economia.FormatarDinheiro(999999.99));
            Assert.AreEqual("1,23 mi", Economia.FormatarDinheiro(1239000));
            Assert.AreEqual("45,6 bi", Economia.FormatarDinheiro(45.69e9));
            Assert.AreEqual("600 bi", Economia.FormatarDinheiro(600e9));
            Assert.AreEqual("-1.500", Economia.FormatarDinheiro(-1500));
        }

    }
}
