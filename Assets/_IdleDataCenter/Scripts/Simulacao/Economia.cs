using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>
    /// Regras do jogo: receita, energia, temperatura, incidentes, compras, promoções e progresso offline.
    /// C# puro (sem Unity), para ser testável e para o mesmo código rodar online e offline.
    ///
    /// Servidores são numerados assim: primeiro as torres (0 .. Torres-1), depois os de rack.
    /// </summary>
    public class Economia
    {
        readonly Random sorteio;

        public EstadoJogo Estado { get; }

        /// <summary>Depois de cada compra, com o id da melhoria.</summary>
        public event Action<string> Comprou;
        /// <summary>Um servidor travou (índice).</summary>
        public event Action<int> Travou;
        /// <summary>Um servidor voltou (índice; true se foi o técnico sozinho, false se foi clique).</summary>
        public event Action<int, bool> Voltou;
        /// <summary>Promoção para um novo cargo (índice do cargo novo).</summary>
        public event Action<int> Promoveu;
        /// <summary>Um disco do storage queimou.</summary>
        public event Action DiscoQueimou;
        /// <summary>O disco foi trocado: (restaurou do backup?, quanto se perdeu em reembolsos, foi o técnico sozinho?).</summary>
        public event Action<bool, double, bool> DiscoTrocado;
        /// <summary>Uma automação terminou de ser escrita e já está trabalhando (id).</summary>
        public event Action<string> AutomacaoPronta;
        /// <summary>Um deploy quebrou (apps fora do ar).</summary>
        public event Action DeployQuebrou;
        /// <summary>Rollback feito (true se foi o técnico ou a automação, false se foi clique).</summary>
        public event Action<bool> DeployVoltou;
        /// <summary>Começou um pico de tráfego (nome do evento).</summary>
        public event Action<string> PicoComecou;
        /// <summary>O cluster foi escalado para o pico (true = pelo autoscaling).</summary>
        public event Action<bool> PicoEscalado;
        /// <summary>O SLA foi violado (valor da multa).</summary>
        public event Action<double> SlaViolado;
        /// <summary>O pico acabou (true = sobreviveu sem violar o SLA).</summary>
        public event Action<bool> PicoTerminou;
        /// <summary>Apareceu um chamado urgente (texto).</summary>
        public event Action<string> ChamadoApareceu;
        /// <summary>Chamado atendido (bônus) ou perdido (0).</summary>
        public event Action<double> ChamadoEncerrado;
        /// <summary>Queda de energia num datacenter extra (índice 1..3, como DC-02..DC-04).</summary>
        public event Action<int> QuedaDeEnergia;
        /// <summary>O datacenter voltou (true se foi sozinho: técnico, gerador ou failover).</summary>
        public event Action<int, bool> EnergiaVoltou;
        /// <summary>Pane regional (índice da região: 1 = América do Norte, 2 = Europa, 3 = Ásia).</summary>
        public event Action<int> PaneRegional;
        public event Action<int, bool> RegiaoVoltou;
        /// <summary>A empresa abriu o capital (fim da carreira).</summary>
        public event Action Ipo;

        public Economia(EstadoJogo estado, Random sorteio = null)
        {
            Estado = estado ?? new EstadoJogo();
            this.sorteio = sorteio ?? new Random();
        }

        // ---------------- Consultas ----------------

        public double Dinheiro => Estado.dinheiro;
        public int Cargo => Estado.cargo;
        public CargoDef CargoAtual => Catalogo.Cargos[Estado.cargo];

        public int Torres => 1 + Nivel(Catalogo.Servidor);
        public int ServidoresRack => Nivel(Catalogo.Servidor1U);
        public int TotalServidores => Torres + ServidoresRack;
        public bool EhTorre(int servidor) => servidor < Torres;

        public bool TemSsd => Nivel(Catalogo.Ssd) > 0;
        public int Ventoinhas => Nivel(Catalogo.Ventoinha);
        public bool TemRack => Nivel(Catalogo.Rack) > 0;
        public bool TemEstagiario => Nivel(Catalogo.Estagiario) > 0;
        public int RacksCheios => Nivel(Catalogo.RackCheio);
        public int NivelStorage => Nivel(Catalogo.Storage);
        public bool TemBackup => Nivel(Catalogo.Backup) > 0;
        public bool NaSalaDeRacks => Estado.cargo >= 2;
        public bool DiscoQueimado => Estado.discoQueimado;
        public double SegundosDiscoQueimado => Estado.discoSegundos;
        public int NivelHypervisor => Nivel(Catalogo.Hypervisor);
        public int HostsContainers => Nivel(Catalogo.Containers);
        public bool TemCi => Nivel(Catalogo.ServidorCi) > 0;
        public bool NaSalaVirtualizada => Estado.cargo >= 3;
        public bool DeployQuebrado => Estado.deployQuebrado;
        public double SegundosDeployQuebrado => Estado.deploySegundos;
        public int NosKubernetes => Nivel(Catalogo.NoKubernetes);
        public bool TemBalanceador => Nivel(Catalogo.Balanceador) > 0;
        public bool NoDataCenter => Estado.cargo >= 4;
        public bool EmPico => !string.IsNullOrEmpty(Estado.picoNome);
        public string NomeDoPico => Estado.picoNome;
        public bool PicoFoiEscalado => Estado.picoEscalado;
        public bool PicoViolado => Estado.picoViolado;
        public double SegundosDePico => Estado.picoDecorrido;
        public double SegundosAteProximoPico => Estado.proximoPico;
        public int RegioesExtras => Nivel(Catalogo.Regiao);
        public int TotalRegioes => 1 + RegioesExtras;
        public bool NoMundo => Estado.cargo >= 6;
        public int RegioesLigadas => Math.Min(Nivel(Catalogo.CaboSubmarino), RegioesExtras);
        public int RegiaoEmPane => Estado.paneRegiao;
        public bool TemPaneRegional => Estado.paneRegiao > 0;
        public double SegundosDePane => Estado.paneSegundos;
        public double TempoRedirecionar =>
            TemAutomacao(Catalogo.Multirregiao) ? Catalogo.TempoFailoverMultirregiao
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;
        public bool IpoFeito => Estado.ipoFeito;
        /// <summary>No CTO, cumprir as metas libera o IPO (não há próximo cargo).</summary>
        public bool PodeFazerIpo => Estado.cargo == Catalogo.Cargos.Count - 1 && !Estado.ipoFeito
                                    && Array.TrueForAll(CargoAtual.MetasParaPromocao, Cumprida);

        public int DatacentersExtras => Nivel(Catalogo.Datacenter);
        public int TotalDatacenters => 1 + DatacentersExtras;
        public bool NoCampus => Estado.cargo >= 5;
        /// <summary>Datacenters extras ligados por fibra ao DC-01 (cada link vale +20%).</summary>
        public int DatacentersInterligados => Math.Min(Nivel(Catalogo.Fibra), DatacentersExtras);
        /// <summary>Qual DC está sem energia (1..3), ou 0 se nenhum.</summary>
        public int DatacenterSemEnergia => Estado.quedaDc;
        public bool TemQuedaDeEnergia => Estado.quedaDc > 0;
        public double SegundosSemEnergia => Estado.quedaSegundos;
        public double TempoReligar =>
            TemAutomacao(Catalogo.Failover) ? Catalogo.TempoFailover
            : Nivel(Catalogo.Gerador) > 0 ? Catalogo.TempoGerador
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;
        /// <summary>Até quando dá para escalar sem violar o SLA (segundos desde o começo do pico).</summary>
        public double LimiteParaEscalar => Catalogo.TempoParaEscalar + (TemBalanceador ? Catalogo.TempoExtraBalanceador : 0);

        /// <summary>Quantos servidores existem de verdade (os racks cheios contam cada servidor).</summary>
        public int ContagemServidores => TotalServidores + RacksCheios * Catalogo.ServidoresPorRackCheio + NosKubernetes;

        /// <summary>Quanto tempo o técnico leva para consertar sozinho (com estagiário, metade).</summary>
        public double TempoConserto =>
            TemAutomacao(Catalogo.Runbooks) ? Catalogo.TempoRunbook
            : TemAutomacao(Catalogo.Watchdog) ? Catalogo.TempoWatchdog
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;

        /// <summary>Quanto tempo um deploy quebrado fica fora até o rollback sem clique.</summary>
        public double TempoRollback =>
            TemAutomacao(Catalogo.Runbooks) ? Catalogo.TempoRunbook
            : TemAutomacao(Catalogo.RollbackAutomatico) ? Catalogo.TempoRollbackAutomatico
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;

        /// <summary>Quanto tempo um disco queimado fica até ser trocado sem clique (com hot-spare, quase nada).</summary>
        public double TempoTrocaDisco =>
            TemAutomacao(Catalogo.Runbooks) ? Catalogo.TempoRunbook
            : TemAutomacao(Catalogo.HotSpare) ? Catalogo.TempoHotSpare
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;

        public double ReceitaTorre =>
            Catalogo.ReceitaBaseServidor
            * (1 + (TemSsd ? Catalogo.BonusSsd : 0))
            * (1 + Ventoinhas * Catalogo.BonusVentoinha);

        // Energia
        public double ConsumoKw => Torres * Catalogo.ConsumoServidorTorre + ServidoresRack * Catalogo.ConsumoServidor1U
                                 + RacksCheios * Catalogo.ConsumoRackCheio + NivelStorage * Catalogo.ConsumoStorage
                                 + HostsContainers * Catalogo.ConsumoHostContainers + (TemCi ? Catalogo.ConsumoServidorCi : 0)
                                 + NosKubernetes * Catalogo.ConsumoNoKubernetes;
        public double CapacidadeKw => Catalogo.CapacidadeBaseKw + Nivel(Catalogo.NoBreak) * Catalogo.CapacidadePorNoBreak
                                    + (NaSalaDeRacks ? Catalogo.CapacidadeSalaDeRacksKw : 0)
                                    + (NaSalaVirtualizada ? Catalogo.CapacidadeSalaVirtualizadaKw : 0)
                                    + (NoDataCenter ? Catalogo.CapacidadeDataCenterKw : 0);
        public bool Sobrecarga => ConsumoKw > CapacidadeKw + 1e-9;
        /// <summary>Com sobrecarga, a receita cai na proporção da energia que falta.</summary>
        public double FatorEnergia => Sobrecarga ? CapacidadeKw / ConsumoKw : 1;

        // Temperatura
        public double Temperatura =>
            Catalogo.TemperaturaAmbiente + ConsumoKw * Catalogo.GrausPorKw * (NaSalaDeRacks ? Catalogo.FatorCalorSalaDeRacks : 1)
            - Nivel(Catalogo.ArCondicionado) * Catalogo.GrausPorArCondicionado
            - (NaSalaDeRacks ? Catalogo.GrausArDePrecisao : 0)
            - (NaSalaVirtualizada ? Catalogo.GrausSalaVirtualizada : 0)
            - (NoDataCenter ? Catalogo.GrausDataCenter : 0);
        public bool Quente => Temperatura > Catalogo.TemperaturaQuente;
        public double FatorTemperatura =>
            Temperatura > Catalogo.TemperaturaCritica ? 0.3 : Quente ? 0.6 : 1;

        // Banda: com o link saturado, todo mundo fica lento e a receita cai na proporção
        public double TrafegoMbps => Torres * Catalogo.TrafegoTorre + ServidoresRack * Catalogo.TrafegoServidor1U
                                   + RacksCheios * Catalogo.TrafegoRackCheio + HostsContainers * Catalogo.TrafegoHostContainers + NosKubernetes * Catalogo.TrafegoNoKubernetes;
        public double BandaMbps => Catalogo.BandaBase + Nivel(Catalogo.Link) * Catalogo.BandaPorLink + Nivel(Catalogo.Link10G) * Catalogo.BandaLink10G;
        /// <summary>A CDN entrega parte do conteúdo de fora: sobra banda no link do DC-01.</summary>
        double FatorTrafegoCdn => Math.Max(0.1, 1 - Nivel(Catalogo.Cdn) * Catalogo.ReducaoTrafegoCdn);
        public bool LinkSaturado => TrafegoMbps * FatorTrafegoCdn > BandaMbps + 1e-9;
        public double FatorBanda => LinkSaturado ? BandaMbps / (TrafegoMbps * FatorTrafegoCdn) : 1;

        /// <summary>Storage vende banco de dados gerenciado; com um disco queimado o RAID fica degradado e esse bônus some.</summary>
        public double FatorStorage => DiscoQueimado ? 1 : 1 + NivelStorage * Catalogo.BonusStorage;

        /// <summary>Tudo que multiplica a receita de todos os servidores.</summary>
        public double FatorGeral => FatorEnergia * FatorTemperatura * FatorBanda * FatorStorage
                                   * (1 + Nivel(Catalogo.Observabilidade) * Catalogo.BonusObservabilidade)
                                   * (CafeAtivo ? Catalogo.MultiplicadorCafe : 1);

        /// <summary>Tudo o que vale para a empresa inteira (todos os datacenters): café, observabilidade, fibra, CDN, balanceamento global.</summary>
        public double FatorEmpresa => (CafeAtivo ? Catalogo.MultiplicadorCafe : 1)
            * (1 + Nivel(Catalogo.Observabilidade) * Catalogo.BonusObservabilidade)
            * (1 + DatacentersInterligados * Catalogo.BonusFibra)
            * (1 + Nivel(Catalogo.Cdn) * Catalogo.BonusCdn)
            * (TemAutomacao(Catalogo.BalanceamentoGlobal) ? 1.15 : 1);

        /// <summary>No campus, a fibra, a CDN e o balanceamento global também turbinam o DC-01.</summary>
        double FatorCampus => (1 + DatacentersInterligados * Catalogo.BonusFibra) * (1 + Nivel(Catalogo.Cdn) * Catalogo.BonusCdn)
                            * (TemAutomacao(Catalogo.BalanceamentoGlobal) ? 1.15 : 1);

        /// <summary>Vale para tudo no mundo: energia renovável e AIOps.</summary>
        public double FatorGlobal => (1 + Nivel(Catalogo.Renovavel) * Catalogo.BonusRenovavel) * (TemAutomacao(Catalogo.Aiops) ? 1 + Catalogo.BonusAiops : 1);

        /// <summary>Receita das regiões novas (a que estiver em pane não rende) e dos clusters de GPU.</summary>
        public double ReceitaMundial =>
            ((RegioesExtras - (TemPaneRegional ? 1 : 0)) * Catalogo.ReceitaRegiao * (1 + RegioesLigadas * Catalogo.BonusCabo)
             + Nivel(Catalogo.Gpu) * Catalogo.ReceitaGpu) * FatorEmpresa;

        /// <summary>Receita dos datacenters novos (o que estiver sem energia não rende).</summary>
        public double ReceitaDatacenters =>
            (DatacentersExtras - (TemQuedaDeEnergia ? 1 : 0)) * Catalogo.ReceitaDatacenter * FatorEmpresa;

        /// <summary>Hypervisor: cada servidor físico vira várias VMs vendidas como VPS.</summary>
        public double FatorVirtualizacao => 1 + NivelHypervisor * Catalogo.BonusVirtualizacao;

        /// <summary>Durante um pico: escalado rende o dobro, violado quase nada.</summary>
        public double MultiplicadorDoPico => !EmPico ? 1
            : Estado.picoEscalado ? Catalogo.MultiplicadorPicoEscalado
            : Estado.picoViolado ? Catalogo.MultiplicadorPicoViolado : 1;

        /// <summary>Receita do Kubernetes gerenciado (sente os picos de tráfego).</summary>
        public double ReceitaKubernetes =>
            NosKubernetes * Catalogo.ReceitaNoKubernetes * (TemBalanceador ? 1 + Catalogo.BonusBalanceador : 1) * FatorGeral * MultiplicadorDoPico;

        /// <summary>Receita dos apps nos containers (zero com deploy quebrado).</summary>
        public double ReceitaApps => DeployQuebrado ? 0
            : HostsContainers * Catalogo.ReceitaHostContainers * (TemCi ? 1 + Catalogo.BonusCi : 1) * FatorGeral;

        public bool Travado(int servidor) => Estado.travamentos.Exists(t => t.servidor == servidor);
        public IReadOnlyList<Travamento> Travamentos => Estado.travamentos;

        /// <summary>Receita "de placa" de um servidor, sem os fatores de energia e temperatura.</summary>
        public double ReceitaBruta(int servidor) => EhTorre(servidor) ? ReceitaTorre : Catalogo.ReceitaServidor1U;

        public double ReceitaDoServidor(int servidor) =>
            Travado(servidor) ? 0 : ReceitaBruta(servidor) * FatorGeral * FatorVirtualizacao;

        public double ReceitaDosRacksCheios => RacksCheios * Catalogo.ReceitaRackCheio * FatorGeral * FatorVirtualizacao;

        public double ReceitaPorSegundo
        {
            get
            {
                double soma = 0;
                for (int i = 0; i < TotalServidores; i++) soma += ReceitaDoServidor(i);
                return ((soma + ReceitaDosRacksCheios + ReceitaApps + ReceitaKubernetes) * FatorCampus + ReceitaDatacenters + ReceitaMundial) * FatorGlobal;
            }
        }

        public double ValorClique => Catalogo.ValorCliqueBase + Catalogo.ValorCliqueReceita * ReceitaPorSegundo;

        // ---------------- Melhorias ----------------

        public int Nivel(string id)
        {
            foreach (var m in Estado.melhorias) if (m.id == id) return m.nivel;
            return 0;
        }

        public IEnumerable<MelhoriaDef> MelhoriasDoCargo()
        {
            foreach (var m in Catalogo.Melhorias) if (m.Cargo == Estado.cargo) yield return m;
        }

        public bool NoMaximo(string id) => Nivel(id) >= Catalogo.Buscar(id).NivelMaximo;
        public double Custo(string id) =>
            Math.Round(Catalogo.Buscar(id).Custo(Nivel(id)) * (TemAutomacao(Catalogo.InfraComoCodigo) ? 1 - Catalogo.DescontoIac : 1));
        public bool RequisitoOk(string id)
        {
            var req = Catalogo.Buscar(id).Requisito;
            return req == null || Nivel(req) > 0;
        }
        public bool PodeComprar(string id) => !NoMaximo(id) && RequisitoOk(id) && Estado.dinheiro >= Custo(id);

        public bool Comprar(string id)
        {
            if (!PodeComprar(id)) return false;
            Estado.dinheiro -= Custo(id);
            var registro = Estado.melhorias.Find(m => m.id == id);
            if (registro == null) Estado.melhorias.Add(registro = new NivelMelhoria { id = id });
            registro.nivel++;
            Comprou?.Invoke(id);
            return true;
        }

        // ---------------- Dinheiro e tempo ----------------

        public void Ganhar(double valor)
        {
            Estado.dinheiro += valor;
            Estado.totalGanho += valor;
        }

        /// <summary>Avança o tempo com o jogo aberto: rende, conserta, escreve automações e sorteia falhas.</summary>
        public void Avancar(double segundos)
        {
            Ganhar(ReceitaPorSegundo * segundos);

            // O técnico resolve sozinho, só que devagar
            for (int i = Estado.travamentos.Count - 1; i >= 0; i--)
            {
                var t = Estado.travamentos[i];
                t.segundos += segundos;
                if (t.segundos >= TempoConserto) Resolver(t.servidor, porTecnico: true);
            }

            // A automação em escrita avança
            AvancarEscrita(segundos);
            AvancarPico(segundos);
            AvancarCafeEChamados(segundos);
            AvancarQuedaDeEnergia(segundos);
            AvancarPaneRegional(segundos);

            // Novas travadas: cada servidor tem uma chance por segundo (maior com calor, menor com monitoramento)
            double mult = (Quente ? Catalogo.MultiplicadorQuente : 1) / (TemAutomacao(Catalogo.Monitoramento) ? Catalogo.FatorMtbfMonitoramento : 1)
                          / (TemAutomacao(Catalogo.Chaos) ? Catalogo.FatorChaos : 1);
            for (int s = 0; s < TotalServidores; s++)
            {
                if (Travado(s)) continue;
                double mtbf = EhTorre(s) ? Catalogo.MtbfServidorTorre : Catalogo.MtbfServidor1U;
                if (sorteio.NextDouble() < segundos * mult / mtbf) Travar(s);
            }

            // Storage: um disco queimado de cada vez; o técnico troca sozinho (ou o hot-spare entra)
            if (DiscoQueimado)
            {
                Estado.discoSegundos += segundos;
                if (Estado.discoSegundos >= TempoTrocaDisco) TrocarDisco(porTecnico: true);
            }
            else if (NivelStorage > 0 && sorteio.NextDouble() < segundos * mult * NivelStorage / Catalogo.MtbfDisco)
                QueimarDisco();

            // Deploys: cada host de containers recebe versões novas; às vezes uma quebra
            if (DeployQuebrado)
            {
                Estado.deploySegundos += segundos;
                if (Estado.deploySegundos >= TempoRollback) FazerRollback(porTecnico: true);
            }
            else if (HostsContainers > 0 && sorteio.NextDouble() < segundos * HostsContainers
                     / (Catalogo.MtbfDeploy * (TemAutomacao(Catalogo.Pipeline) ? Catalogo.FatorPipeline : 1)))
                QuebrarDeploy();
        }

        /// <summary>Clique no servidor. Se estiver travado, reinicia; senão rende um bônus. Retorna o valor ganho.</summary>
        public double Clicar(int servidor)
        {
            Estado.jaClicouNoServidor = true;
            if (Travado(servidor))
            {
                Resolver(servidor, porTecnico: false);
                return 0;
            }
            double valor = ValorClique;
            Ganhar(valor);
            return valor;
        }

        /// <summary>Clique num equipamento que não trava (rack cheio, storage saudável): rende como um clique no servidor.</summary>
        public double ClicarEquipamento()
        {
            Estado.jaClicouNoServidor = true;
            double valor = ValorClique;
            Ganhar(valor);
            return valor;
        }

        public void Travar(int servidor)
        {
            if (servidor < 0 || servidor >= TotalServidores || Travado(servidor)) return;
            Estado.travamentos.Add(new Travamento { servidor = servidor });
            Travou?.Invoke(servidor);
        }

        public void QueimarDisco()
        {
            if (NivelStorage == 0 || DiscoQueimado) return;
            Estado.discoQueimado = true;
            Estado.discoSegundos = 0;
            DiscoQueimou?.Invoke();
        }

        /// <summary>
        /// Troca o disco queimado. Com backup, os dados voltam (conta como backup restaurado);
        /// sem backup, os clientes são reembolsados. Retorna o valor perdido.
        /// </summary>
        public double TrocarDisco(bool porTecnico)
        {
            if (!DiscoQueimado) return 0;
            Estado.discoQueimado = false;
            Estado.discoSegundos = 0;
            Estado.incidentesResolvidos++;
            double perda = 0;
            if (TemBackup) Estado.backupsRestaurados++;
            else
            {
                perda = Math.Min(Estado.dinheiro, ReceitaPorSegundo * Catalogo.SegundosPerdidosSemBackup);
                Estado.dinheiro -= perda;
            }
            DiscoTrocado?.Invoke(TemBackup, perda, porTecnico);
            return perda;
        }

        public void QuebrarDeploy()
        {
            if (HostsContainers == 0 || DeployQuebrado) return;
            Estado.deployQuebrado = true;
            Estado.deploySegundos = 0;
            DeployQuebrou?.Invoke();
        }

        /// <summary>Volta a versão anterior: os apps voltam a rodar.</summary>
        public void FazerRollback(bool porTecnico)
        {
            if (!DeployQuebrado) return;
            Estado.deployQuebrado = false;
            Estado.deploySegundos = 0;
            Estado.incidentesResolvidos++;
            DeployVoltou?.Invoke(porTecnico);
        }

        void Resolver(int servidor, bool porTecnico)
        {
            if (Estado.travamentos.RemoveAll(t => t.servidor == servidor) == 0) return;
            Estado.incidentesResolvidos++;
            Voltou?.Invoke(servidor, porTecnico);
        }

        // ---------------- Pane regional e IPO (CTO) ----------------

        void AvancarPaneRegional(double segundos)
        {
            if (TemPaneRegional)
            {
                Estado.paneSegundos += segundos;
                if (Estado.paneSegundos >= TempoRedirecionar) Redirecionar(sozinho: true);
                return;
            }
            if (RegioesExtras > 0 && sorteio.NextDouble() < segundos * RegioesExtras / Catalogo.MtbfPaneRegional)
                DerrubarRegiao(1 + sorteio.Next(RegioesExtras));
        }

        public void DerrubarRegiao(int regiao)
        {
            if (TemPaneRegional || regiao < 1 || regiao > RegioesExtras) return;
            Estado.paneRegiao = regiao;
            Estado.paneSegundos = 0;
            PaneRegional?.Invoke(regiao);
        }

        /// <summary>Redireciona o tráfego da região em pane para as outras (clique ou sozinho).</summary>
        public void Redirecionar(bool sozinho = false)
        {
            if (!TemPaneRegional) return;
            int regiao = Estado.paneRegiao;
            Estado.paneRegiao = -1;
            Estado.paneSegundos = 0;
            Estado.incidentesResolvidos++;
            RegiaoVoltou?.Invoke(regiao, sozinho);
        }

        /// <summary>Abre o capital: o fim da carreira (o jogo continua).</summary>
        public bool FazerIpo()
        {
            if (!PodeFazerIpo) return false;
            Estado.ipoFeito = true;
            Ipo?.Invoke();
            return true;
        }

        // ---------------- Queda de energia (Arquiteto) ----------------

        void AvancarQuedaDeEnergia(double segundos)
        {
            if (TemQuedaDeEnergia)
            {
                Estado.quedaSegundos += segundos;
                if (Estado.quedaSegundos >= TempoReligar) Religar(sozinho: true);
                return;
            }
            if (DatacentersExtras > 0 && sorteio.NextDouble() < segundos * DatacentersExtras / Catalogo.MtbfQuedaDeEnergia)
                DerrubarEnergia(1 + sorteio.Next(DatacentersExtras));
        }

        public void DerrubarEnergia(int dc)
        {
            if (TemQuedaDeEnergia || dc < 1 || dc > DatacentersExtras) return;
            Estado.quedaDc = dc;
            Estado.quedaSegundos = 0;
            QuedaDeEnergia?.Invoke(dc);
        }

        /// <summary>Religa o datacenter que caiu (clique do jogador ou sozinho).</summary>
        public void Religar(bool sozinho = false)
        {
            if (!TemQuedaDeEnergia) return;
            int dc = Estado.quedaDc;
            Estado.quedaDc = -1;
            Estado.quedaSegundos = 0;
            Estado.incidentesResolvidos++;
            EnergiaVoltou?.Invoke(dc, sozinho);
        }

        // ---------------- Café e chamados urgentes ----------------

        public bool CafeAtivo => Estado.cafeRestante > 0;
        public double SegundosDeCafe => Estado.cafeRestante;
        public double RecargaDoCafe => Math.Max(0, Estado.cafeRecarga);
        public bool PodeTomarCafe => Estado.cafeRecarga <= 0;

        /// <summary>O técnico toma um café: a receita dobra por 30 s; o próximo só depois de 3 min.</summary>
        public bool TomarCafe()
        {
            if (!PodeTomarCafe) return false;
            Estado.cafeRestante = Catalogo.DuracaoCafe;
            Estado.cafeRecarga = Catalogo.RecargaCafe;
            return true;
        }

        public bool TemChamado => Estado.chamadoRestante > 0;
        public string TextoDoChamado => Estado.chamadoTexto;
        public double SegundosDoChamado => Estado.chamadoRestante;
        public double BonusDoChamado => Math.Max(25, ReceitaPorSegundo * Catalogo.SegundosDeBonusDoChamado);

        void AvancarCafeEChamados(double segundos)
        {
            if (Estado.cafeRestante > 0) Estado.cafeRestante = Math.Max(0, Estado.cafeRestante - segundos);
            if (Estado.cafeRecarga > 0) Estado.cafeRecarga -= segundos;

            if (TemChamado)
            {
                Estado.chamadoRestante -= segundos;
                if (Estado.chamadoRestante <= 0) EncerrarChamado(0);
                return;
            }
            if (Estado.proximoChamado < 0) Estado.proximoChamado = Catalogo.PrimeiroChamado;
            Estado.proximoChamado -= segundos;
            if (Estado.proximoChamado <= 0) AbrirChamado();
        }

        public void AbrirChamado()
        {
            if (TemChamado) return;
            Estado.chamadoTexto = Catalogo.Chamados[sorteio.Next(Catalogo.Chamados.Length)];
            Estado.chamadoRestante = Catalogo.TempoParaAtender;
            ChamadoApareceu?.Invoke(Estado.chamadoTexto);
        }

        /// <summary>Atende o chamado aberto e recebe o bônus. Retorna o valor (0 se não havia chamado).</summary>
        public double AtenderChamado()
        {
            if (!TemChamado) return 0;
            double bonus = BonusDoChamado;
            Ganhar(bonus);
            Estado.chamadosAtendidos++;
            EncerrarChamado(bonus);
            return bonus;
        }

        void EncerrarChamado(double bonus)
        {
            Estado.chamadoRestante = 0;
            Estado.chamadoTexto = "";
            Estado.proximoChamado = Catalogo.IntervaloChamadoMin + sorteio.NextDouble() * (Catalogo.IntervaloChamadoMax - Catalogo.IntervaloChamadoMin);
            ChamadoEncerrado?.Invoke(bonus);
        }

        // ---------------- Tutorial ----------------

        public int PassoTutorial => Estado.tutorial;
        public bool TutorialConcluido => Estado.tutorial >= Catalogo.PassosTutorial;
        public void AvancarTutorial(int ate) { if (ate > Estado.tutorial) Estado.tutorial = Math.Min(ate, Catalogo.PassosTutorial); }

        // ---------------- Picos de tráfego (SRE) ----------------

        void AvancarPico(double segundos)
        {
            if (!NoDataCenter || NosKubernetes == 0) return;
            if (Estado.proximoPico < 0) Estado.proximoPico = Catalogo.PrimeiroPico;
            if (!EmPico)
            {
                Estado.proximoPico -= segundos;
                if (Estado.proximoPico <= 0) ComecarPico();
                return;
            }
            Estado.picoDecorrido += segundos;
            if (!Estado.picoEscalado && TemAutomacao(Catalogo.Autoscaling) && Estado.picoDecorrido >= Catalogo.TempoAutoscaling)
                Escalar(automatico: true);
            if (!Estado.picoEscalado && !Estado.picoViolado && Estado.picoDecorrido >= LimiteParaEscalar)
                ViolarSla();
            if (Estado.picoDecorrido >= Catalogo.DuracaoPico) TerminarPico();
        }

        public void ComecarPico()
        {
            if (EmPico || NosKubernetes == 0) return;
            Estado.picoNome = Catalogo.NomesDePico[sorteio.Next(Catalogo.NomesDePico.Length)];
            Estado.picoDecorrido = 0;
            Estado.picoEscalado = Estado.picoViolado = false;
            Estado.picosTotal++;
            PicoComecou?.Invoke(Estado.picoNome);
        }

        /// <summary>Sobe réplicas para aguentar o pico (clique do jogador ou autoscaling). Pico escalado rende o dobro.</summary>
        public bool Escalar(bool automatico = false)
        {
            if (!EmPico || Estado.picoEscalado) return false;
            Estado.picoEscalado = true;
            PicoEscalado?.Invoke(automatico);
            return true;
        }

        void ViolarSla()
        {
            double multa = Math.Min(Estado.dinheiro, ReceitaPorSegundo * Catalogo.SegundosDeMultaSla);
            Estado.dinheiro -= multa;
            Estado.picoViolado = true;
            SlaViolado?.Invoke(multa);
        }

        void TerminarPico()
        {
            bool sobreviveu = !Estado.picoViolado;
            if (sobreviveu) Estado.picosSobrevividos++;
            Estado.picoNome = "";
            Estado.picoDecorrido = 0;
            Estado.picoEscalado = Estado.picoViolado = false;
            Estado.proximoPico = Catalogo.IntervaloPicoMin + sorteio.NextDouble() * (Catalogo.IntervaloPicoMax - Catalogo.IntervaloPicoMin);
            PicoTerminou?.Invoke(sobreviveu);
        }

        // ---------------- Automações ----------------

        public bool AutomacoesLiberadas => Estado.cargo >= Catalogo.CargoDasAutomacoes;
        public bool TemAutomacao(string id) => Estado.automacoes.Contains(id);
        public int AutomacoesAtivas => Estado.automacoes.Count;
        public bool Escrevendo => !string.IsNullOrEmpty(Estado.escrevendo);
        public AutomacaoDef AutomacaoEmEscrita => Escrevendo ? Catalogo.BuscarAutomacao(Estado.escrevendo) : null;
        /// <summary>De 0 a 1.</summary>
        public double ProgressoEscrita => Escrevendo ? Math.Min(1, Estado.segundosEscritos / AutomacaoEmEscrita.Segundos) : 0;

        public bool RequisitoAutomacaoOk(AutomacaoDef a) => a.Requisito == null || Nivel(a.Requisito) > 0;

        public bool PodeEscrever(string id)
        {
            var a = Catalogo.BuscarAutomacao(id);
            return Estado.cargo >= a.Cargo && !Escrevendo && !TemAutomacao(id) && RequisitoAutomacaoOk(a) && Estado.dinheiro >= a.Custo;
        }

        /// <summary>Paga e começa a escrever (uma por vez; o técnico fica na mesa digitando).</summary>
        public bool EscreverAutomacao(string id)
        {
            if (!PodeEscrever(id)) return false;
            Estado.dinheiro -= Catalogo.BuscarAutomacao(id).Custo;
            Estado.escrevendo = id;
            Estado.segundosEscritos = 0;
            return true;
        }

        void AvancarEscrita(double segundos)
        {
            if (!Escrevendo) return;
            Estado.segundosEscritos += segundos;
            if (Estado.segundosEscritos < AutomacaoEmEscrita.Segundos) return;
            string id = Estado.escrevendo;
            Estado.escrevendo = "";
            Estado.segundosEscritos = 0;
            Estado.automacoes.Add(id);
            AutomacaoPronta?.Invoke(id);
        }

        // ---------------- Carreira ----------------

        public double Progresso(MetaDef meta)
        {
            switch (meta.Tipo)
            {
                case TipoMeta.Servidores: return TotalServidores;
                case TipoMeta.TotalGanho: return Estado.totalGanho;
                case TipoMeta.ServidoresRack: return ServidoresRack;
                case TipoMeta.BackupsRestaurados: return Estado.backupsRestaurados;
                case TipoMeta.AutomacoesAtivas: return AutomacoesAtivas;
                case TipoMeta.HostsContainers: return HostsContainers;
                case TipoMeta.PicosSobrevividos: return Estado.picosSobrevividos;
                case TipoMeta.Datacenters: return TotalDatacenters;
                case TipoMeta.Regioes: return TotalRegioes;
                default: return Estado.incidentesResolvidos;
            }
        }

        public bool Cumprida(MetaDef meta) => Progresso(meta) >= meta.Alvo;

        public bool TemProximoCargo => Estado.cargo + 1 < Catalogo.Cargos.Count && CargoAtual.MetasParaPromocao.Length > 0;

        public bool PodePromover
        {
            get
            {
                if (!TemProximoCargo) return false;
                foreach (var m in CargoAtual.MetasParaPromocao) if (!Cumprida(m)) return false;
                return true;
            }
        }

        public bool Promover()
        {
            if (!PodePromover) return false;
            Estado.cargo++;
            Promoveu?.Invoke(Estado.cargo);
            return true;
        }

        // ---------------- Offline ----------------

        /// <summary>Fração da receita normal que rende com o jogo fechado (automações sobem até 100%).</summary>
        public double TaxaOffline => Catalogo.TaxaOffline
            + (TemAutomacao(Catalogo.CronFaturamento) ? Catalogo.BonusOfflinePorAutomacao : 0)
            + (TemAutomacao(Catalogo.Plantao) ? Catalogo.BonusOfflinePorAutomacao : 0);

        public double HorasMaximasOffline => TemAutomacao(Catalogo.Plantao) ? Catalogo.HorasOfflineComPlantao : Catalogo.HorasMaximasOffline;

        /// <summary>Quanto o jogador ganharia por ficar fora esse tempo (com taxa reduzida e limite de horas).</summary>
        public double CalcularGanhoOffline(double segundosFora)
        {
            if (segundosFora < Catalogo.SegundosMinimosOffline) return 0;
            double segundos = Math.Min(segundosFora, HorasMaximasOffline * 3600);
            return ReceitaPorSegundo * segundos * TaxaOffline;
        }

        /// <summary>Resumo da última volta: quanto tempo ficou fora (s), quanto rendeu e quantos servidores o técnico consertou.</summary>
        public double SegundosFora { get; private set; }
        public double GanhoFora { get; private set; }
        public int ConsertadosFora { get; private set; }
        /// <summary>Ficou fora mais que o limite de horas (o que passou disso não rendeu).</summary>
        public bool PassouDoLimite => SegundosFora > HorasMaximasOffline * 3600;

        /// <summary>
        /// Aplica o progresso offline desde o último save. Enquanto você estava fora, o técnico
        /// consertou o que tinha travado. Retorna o valor ganho.
        /// </summary>
        public double AplicarOffline(long agoraUnix)
        {
            if (Estado.ultimoSalvamentoUnix <= 0) return 0; // primeiro jogo
            double fora = agoraUnix - Estado.ultimoSalvamentoUnix;
            SegundosFora = fora;
            // café e chamado são coisas de quem está jogando: não valem com o jogo fechado
            if (fora >= Catalogo.SegundosMinimosOffline)
            {
                Estado.cafeRestante = 0;
                Estado.cafeRecarga = 0;
                if (TemChamado) { Estado.chamadoRestante = 0; Estado.chamadoTexto = ""; Estado.proximoChamado = Catalogo.IntervaloChamadoMin; }
            }
            if (fora >= Catalogo.SegundosMinimosOffline)
            {
                ConsertadosFora = Estado.travamentos.Count;
                Estado.incidentesResolvidos += ConsertadosFora;
                Estado.travamentos.Clear();
                if (DiscoQueimado) { TrocarDisco(porTecnico: true); ConsertadosFora++; }
                if (DeployQuebrado) { FazerRollback(porTecnico: true); ConsertadosFora++; }
                // pico em andamento quando o jogo fechou: acaba sem contar nem multar
                if (EmPico) { Estado.picoNome = ""; Estado.picoDecorrido = 0; Estado.picoEscalado = Estado.picoViolado = false; }
                if (TemQuedaDeEnergia) { Religar(sozinho: true); ConsertadosFora++; }
                if (TemPaneRegional) { Redirecionar(sozinho: true); ConsertadosFora++; }
            }
            if (fora >= Catalogo.SegundosMinimosOffline) AvancarEscrita(fora); // o script continua sendo escrito
            double ganho = CalcularGanhoOffline(fora);
            GanhoFora = ganho;
            Ganhar(ganho);
            return ganho;
        }
    }
}
