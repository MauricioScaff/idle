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
    public partial class Economia
    {
        readonly Random sorteio;

        public EstadoJogo Estado { get; private set; }

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
        /// <summary>Queda de energia num datacenter extra (índice 1..3, como DC-02..DC-04).</summary>
        public event Action<int> QuedaDeEnergia;
        /// <summary>O datacenter voltou (true se foi sozinho: técnico, gerador ou failover).</summary>
        public event Action<int, bool> EnergiaVoltou;
        /// <summary>Pane regional (índice da região: 1 = América do Norte, 2 = Europa, 3 = Ásia).</summary>
        public event Action<int> PaneRegional;
        public event Action<int, bool> RegiaoVoltou;
        /// <summary>A empresa abriu o capital (fim da carreira).</summary>
        public event Action Ipo;
        /// <summary>A empresa foi vendida e uma nova começou (certificações ganhas).</summary>
        public event Action<int> Vendeu;

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
        /// <summary>Quantos níveis da linha de backup (HD externo, NAS, fita, sala, DC de recuperação, outra região) já foram comprados.</summary>
        public int NivelBackup { get { int n = 0; foreach (var id in Catalogo.LinhaDeBackup) if (Nivel(id) > 0) n++; return n; } }
        public bool TemBackup => NivelBackup > 0;
        /// <summary>Quanto da perda de um disco queimado o backup evita: metade por nível; com a linha toda, tudo.</summary>
        public double ProtecaoBackup => NivelBackup >= Catalogo.LinhaDeBackup.Length ? 1 : 1 - Math.Pow(0.5, NivelBackup);
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
            (TemAutomacao(Catalogo.Runbooks) ? Catalogo.TempoRunbook
            : TemAutomacao(Catalogo.Watchdog) ? Catalogo.TempoWatchdog
            : TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico) * FatorConsertoDoEvento;

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
            * (1 + Ventoinhas * Catalogo.BonusVentoinha)
            * BonusDoAlvo(Catalogo.AlvoTorres) * FatorMarcos(Torres);

        /// <summary>Renda de um servidor 1U (cabos organizados, firmware e marcos).</summary>
        public double Receita1U => Catalogo.ReceitaServidor1U * BonusDoAlvo(Catalogo.Alvo1U) * FatorMarcos(ServidoresRack);

        // ---------------- Geradores sem teto e melhorias simples ----------------

        /// <summary>Soma dos bônus das melhorias simples de um alvo (1 = nenhum bônus).</summary>
        public double BonusDoAlvo(string alvo) => 1 + SomaDoAlvo(alvo);

        /// <summary>Soma de nível × bônus das melhorias simples de um alvo (kW, graus ou fração de renda).</summary>
        public double SomaDoAlvo(string alvo)
        {
            double soma = 0;
            foreach (var m in Estado.melhorias)
            {
                if (m.nivel <= 0) continue;
                var def = Catalogo.Achar(m.id);
                if (def != null && def.Alvo == alvo) soma += m.nivel * def.BonusPorNivel;
            }
            return soma;
        }

        /// <summary>Marcos atingidos por um gerador com esta quantidade de unidades.</summary>
        public static int MarcosAtingidos(int unidades)
        {
            int n = 0;
            foreach (var m in Catalogo.Marcos) if (unidades >= m) n++;
            return n;
        }

        /// <summary>A renda de um gerador dobra a cada marco (10, 25, 50, 100 unidades).</summary>
        public static double FatorMarcos(int unidades) => Math.Pow(2, MarcosAtingidos(unidades));

        /// <summary>Unidades que contam para os marcos de um gerador (a primeira torre já vem com o armário).</summary>
        public int UnidadesDoGerador(string id) => id == Catalogo.Servidor ? Torres : Nivel(id);

        /// <summary>A última compra deste gerador bateu exatamente num marco (hora da festa)?</summary>
        public bool AtingiuMarco(string id)
        {
            var def = Catalogo.Achar(id);
            return def != null && def.Gerador && Array.IndexOf(Catalogo.Marcos, UnidadesDoGerador(id)) >= 0;
        }

        /// <summary>Próximo marco de um gerador, ou 0 se já passou de todos.</summary>
        public int ProximoMarco(string id)
        {
            int unidades = UnidadesDoGerador(id);
            foreach (var m in Catalogo.Marcos) if (unidades < m) return m;
            return 0;
        }

        // Energia
        public double ConsumoKw => Torres * Catalogo.ConsumoServidorTorre + ServidoresRack * Catalogo.ConsumoServidor1U
                                 + RacksCheios * Catalogo.ConsumoRackCheio + NivelStorage * Catalogo.ConsumoStorage
                                 + HostsContainers * Catalogo.ConsumoHostContainers + (TemCi ? Catalogo.ConsumoServidorCi : 0)
                                 + NosKubernetes * Catalogo.ConsumoNoKubernetes;
        public double CapacidadeKw => Catalogo.CapacidadeBaseKw + Nivel(Catalogo.NoBreak) * Catalogo.CapacidadePorNoBreak
                                    + (NaSalaDeRacks ? Catalogo.CapacidadeSalaDeRacksKw : 0)
                                    + (NaSalaVirtualizada ? Catalogo.CapacidadeSalaVirtualizadaKw : 0)
                                    + (NoDataCenter ? Catalogo.CapacidadeDataCenterKw : 0)
                                    + SomaDoAlvo(Catalogo.AlvoKw);
        public bool Sobrecarga => ConsumoKw > CapacidadeKw + 1e-9;
        /// <summary>Com sobrecarga, a receita cai na proporção da energia que falta.</summary>
        public double FatorEnergia => Sobrecarga ? CapacidadeKw / ConsumoKw : 1;

        // Temperatura
        /// <summary>O ar-condicionado esfria até um limite: a sala nunca fica abaixo de TemperaturaMinima.</summary>
        public double Temperatura => Math.Max(Catalogo.TemperaturaMinima,
            Catalogo.TemperaturaAmbiente + ConsumoKw * Catalogo.GrausPorKw * (NaSalaDeRacks ? Catalogo.FatorCalorSalaDeRacks : 1)
            - Nivel(Catalogo.ArCondicionado) * Catalogo.GrausPorArCondicionado
            - (NaSalaDeRacks ? Catalogo.GrausArDePrecisao : 0)
            - (NaSalaVirtualizada ? Catalogo.GrausSalaVirtualizada : 0)
            - (NoDataCenter ? Catalogo.GrausDataCenter : 0)
            - SomaDoAlvo(Catalogo.AlvoGraus));
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
                                   * (1 + NivelBackup * Catalogo.BonusPorBackup)
                                   * (1 + Nivel(Catalogo.Observabilidade) * Catalogo.BonusObservabilidade)
                                   * (CafeAtivo ? Catalogo.MultiplicadorCafe : 1)
                                   * FatorEvento
                                   * FatorSla
                                   * FatorConquistas;

        // ---------------- Prestígio: certificações e desafio ----------------

        public Prestigio Prestigio => Estado.prestigio ?? (Estado.prestigio = new Prestigio());
        public int NivelCertificacao(string id)
        {
            foreach (var b in Prestigio.bonus) if (b.id == id) return b.nivel;
            return 0;
        }
        public bool Desafio(string id) => Estado.desafio == id;
        /// <summary>Uptime Wizard: vale para tudo.</summary>
        public double FatorCertificacoes => 1 + NivelCertificacao(Catalogo.UptimeWizard) * 0.1;

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
            ((RegioesExtras - (TemPaneRegional ? 1 : 0)) * Catalogo.ReceitaRegiao * (1 + RegioesLigadas * Catalogo.BonusCabo) * BonusDoAlvo(Catalogo.AlvoRegioes)
             + Nivel(Catalogo.Gpu) * Catalogo.ReceitaGpu * BonusDoAlvo(Catalogo.AlvoGpu) * FatorMarcos(Nivel(Catalogo.Gpu))) * FatorEmpresa;

        /// <summary>Receita dos datacenters novos (o que estiver sem energia não rende).</summary>
        public double ReceitaDatacenters =>
            (DatacentersExtras - (TemQuedaDeEnergia ? 1 : 0)) * Catalogo.ReceitaDatacenter * BonusDoAlvo(Catalogo.AlvoDatacenters) * FatorEmpresa;

        /// <summary>Hypervisor: cada servidor físico vira várias VMs vendidas como VPS.</summary>
        public double FatorVirtualizacao => 1 + NivelHypervisor * Catalogo.BonusVirtualizacao;

        /// <summary>Durante um pico: escalado rende o dobro, violado quase nada.</summary>
        public double MultiplicadorDoPico => !EmPico ? 1
            : Estado.picoEscalado ? Catalogo.MultiplicadorPicoEscalado
            : Estado.picoViolado ? Catalogo.MultiplicadorPicoViolado : 1;

        /// <summary>Receita do Kubernetes gerenciado (sente os picos de tráfego).</summary>
        public double ReceitaKubernetes =>
            NosKubernetes * Catalogo.ReceitaNoKubernetes * (TemBalanceador ? 1 + Catalogo.BonusBalanceador : 1) * BonusDoAlvo(Catalogo.AlvoK8s) * FatorMarcos(NosKubernetes)
            * FatorGeral * MultiplicadorDoPico;

        /// <summary>Receita dos apps nos containers (zero com deploy quebrado).</summary>
        public double ReceitaApps => DeployQuebrado ? 0
            : HostsContainers * Catalogo.ReceitaHostContainers * (TemCi ? 1 + Catalogo.BonusCi : 1) * BonusDoAlvo(Catalogo.AlvoApps) * FatorMarcos(HostsContainers) * FatorGeral;

        public bool Travado(int servidor) => Estado.travamentos.Exists(t => t.servidor == servidor);
        public IReadOnlyList<Travamento> Travamentos => Estado.travamentos;

        /// <summary>Receita "de placa" de um servidor, sem os fatores de energia e temperatura.</summary>
        public double ReceitaBruta(int servidor) => EhTorre(servidor) ? ReceitaTorre : Receita1U;

        public double ReceitaDoServidor(int servidor) =>
            Travado(servidor) ? 0 : ReceitaBruta(servidor) * FatorGeral * FatorVirtualizacao;

        public double ReceitaDosRacksCheios => RacksCheios * Catalogo.ReceitaRackCheio * BonusDoAlvo(Catalogo.AlvoRackCheio) * FatorMarcos(RacksCheios) * FatorGeral * FatorVirtualizacao;

        public double ReceitaPorSegundo
        {
            get
            {
                // torres e 1U rendem igual entre si: conta quantos estão de pé em vez de somar um por um
                int torresTravadas = 0, u1Travados = 0;
                foreach (var t in Estado.travamentos) if (EhTorre(t.servidor)) torresTravadas++; else u1Travados++;
                double soma = ((Torres - torresTravadas) * ReceitaTorre + (ServidoresRack - u1Travados) * Receita1U) * FatorGeral * FatorVirtualizacao;
                return ((soma + ReceitaDosRacksCheios + ReceitaApps + ReceitaKubernetes) * FatorCampus + ReceitaDatacenters + ReceitaMundial) * FatorGlobal * FatorCertificacoes;
            }
        }

        public double ValorClique => Desafio(Catalogo.SoAutomacao) ? 0 : Catalogo.ValorCliqueBase + Catalogo.ValorCliqueReceita * ReceitaPorSegundo;

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
        public bool PodeComprar(string id) => !NoMaximo(id) && RequisitoOk(id) && Estado.dinheiro >= Custo(id)
                                           && !(id == Catalogo.Estagiario && Desafio(Catalogo.SemEstagiario));

        /// <summary>
        /// O que o jogo sugere comprar agora: sem energia, o mais barato que dá energia; quente, o que esfria;
        /// link saturado, o que dá banda. Sem gargalo, a melhoria mais barata do cargo atual (e depois a de qualquer cargo).
        /// </summary>
        public MelhoriaDef MelhoriaSugerida()
        {
            string[] opcoes = Sobrecarga ? new[] { Catalogo.FiltroDeLinha, Catalogo.NoBreak }
                            : Quente ? new[] { Catalogo.Ventilador, Catalogo.ArCondicionado }
                            : LinkSaturado ? new[] { Catalogo.Link, Catalogo.Link10G } : null;
            if (opcoes != null)
            {
                MelhoriaDef melhor = null;
                foreach (var id in opcoes)
                {
                    var m = Catalogo.Buscar(id);
                    if (m.Cargo > Estado.cargo || NoMaximo(id) || !RequisitoOk(id)) continue;
                    if (melhor == null || Custo(id) < Custo(melhor.Id)) melhor = m;
                }
                if (melhor != null) return melhor;
            }
            // sem gargalo: primeiro o que é do cargo atual; quando ele acabar, o que ficou para trás
            MelhoriaDef doCargo = null;
            foreach (var m in MelhoriasDoCargo())
            {
                if (NoMaximo(m.Id) || !RequisitoOk(m.Id) || (m.Id == Catalogo.Estagiario && Desafio(Catalogo.SemEstagiario))) continue;
                if (doCargo == null || Custo(m.Id) < Custo(doCargo.Id)) doCargo = m;
            }
            return doCargo ?? MelhoriaMaisBarata();
        }

        /// <summary>A melhoria mais barata que já dá para comprar (inclui as que ficaram para trás nos cargos anteriores); null se não sobrou nada.</summary>
        public MelhoriaDef MelhoriaMaisBarata()
        {
            MelhoriaDef melhor = null;
            foreach (var m in Catalogo.Melhorias)
            {
                if (m.Cargo > Estado.cargo || NoMaximo(m.Id) || !RequisitoOk(m.Id)) continue;
                if (m.Id == Catalogo.Estagiario && Desafio(Catalogo.SemEstagiario)) continue;
                if (melhor == null || Custo(m.Id) < Custo(melhor.Id)) melhor = m;
            }
            return melhor;
        }

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
            AvancarCafe(segundos);
            AvancarChamados(segundos);
            ChecarConquistas();
            AvancarEvento(segundos);
            AvancarUptime(segundos);
            AvancarIdade(segundos);
            AvancarQuedaDeEnergia(segundos);
            AvancarPaneRegional(segundos);

            // Novas travadas: cada servidor tem uma chance por segundo (maior com calor, menor com monitoramento)
            double mult = (Quente ? Catalogo.MultiplicadorQuente : 1) / (TemAutomacao(Catalogo.Monitoramento) ? Catalogo.FatorMtbfMonitoramento : 1)
                          / (TemAutomacao(Catalogo.Chaos) ? Catalogo.FatorChaos : 1);
            mult *= Math.Max(0.3, 1 - NivelCertificacao(Catalogo.ItilGambiarra) * 0.1);   // ITIL da Gambiarra
            for (int s = 0; s < TotalServidores; s++)
            {
                if (Travado(s)) continue;
                if ((EhTorre(s) ? s : s - Torres) >= Catalogo.ServidoresQueTravam) continue;
                double mtbf = EhTorre(s) ? Catalogo.MtbfServidorTorre : Catalogo.MtbfServidor1U;
                if (sorteio.NextDouble() < segundos * mult * FatorIdade / mtbf) Travar(s);   // hardware velho trava mais
            }

            // Disco: um queimado de cada vez; o técnico troca sozinho (ou o hot-spare entra). Antes do storage, quem queima é o
            // HD da torre; depois, os discos do storage (mais níveis, mais discos, mais falhas)
            if (DiscoQueimado)
            {
                Estado.discoSegundos += segundos;
                if (Estado.discoSegundos >= TempoTrocaDisco) TrocarDisco(porTecnico: true);
            }
            else if (sorteio.NextDouble() < segundos * mult * (NivelStorage > 0 ? NivelStorage / Catalogo.MtbfDisco : 1 / Catalogo.MtbfDiscoSemStorage))
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
            QuebrouDuranteEvento();   // antes de travar: a multa sai da receita de antes da queda
            Estado.travamentos.Add(new Travamento { servidor = servidor });
            Travou?.Invoke(servidor);
        }

        public void QueimarDisco()
        {
            if (DiscoQueimado) return;
            Estado.discoQueimado = true;
            Estado.discoSegundos = 0;
            DiscoQueimou?.Invoke();
        }

        /// <summary>
        /// Troca o disco queimado. Com backup, os dados voltam (conta como backup restaurado); o que o backup não cobre
        /// (metade por nível da linha de backup) vira reembolso aos clientes. Retorna o valor perdido.
        /// </summary>
        public double TrocarDisco(bool porTecnico)
        {
            if (!DiscoQueimado) return 0;
            Estado.discoQueimado = false;
            Estado.discoSegundos = 0;
            Estado.incidentesResolvidos++;
            double perda = 0;
            if (TemBackup) Estado.backupsRestaurados++;
            // o que o backup não salvou vira reembolso aos clientes
            perda = Math.Min(Estado.dinheiro, ReceitaPorSegundo * Catalogo.SegundosPerdidosSemBackup * (1 - ProtecaoBackup));
            Estado.dinheiro -= perda;
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

        // ---------------- Vender a empresa (prestígio) ----------------

        public bool PodeVender => Estado.cargo >= Catalogo.CargoParaVender;

        /// <summary>Quantas certificações a venda daria agora: raiz do faturamento em milhões, em dobro depois do IPO, vezes o desafio.</summary>
        public int CertificacoesDaVenda =>
            !PodeVender ? 0 : (int)Math.Floor(Math.Sqrt(Estado.totalGanho / 1e6) * (Estado.ipoFeito ? Catalogo.MultiplicadorIpo : 1)
                                              * Catalogo.BuscarDesafio(Estado.desafio).Multiplicador);

        /// <summary>
        /// Vende a empresa: ganha as certificações e um troféu, e recomeça no armário com um desafio (opcional).
        /// O prestígio e as preferências de tutorial passam para a empresa nova.
        /// </summary>
        public bool VenderEmpresa(string novoDesafio = "")
        {
            if (!PodeVender) return false;
            Conquistar(Catalogo.ConquistaExit);
            int ganhas = CertificacoesDaVenda;
            var p = Prestigio;
            p.certificacoes += ganhas;
            p.certificacoesGanhas += ganhas;
            p.empresasVendidas++;
            p.trofeus.Add(Estado.ipoFeito ? "IPO" : CargoAtual.Nome);

            var nova = new EstadoJogo
            {
                prestigio = p,
                desafio = novoDesafio ?? "",
                tutorial = Catalogo.PassosTutorial,
                jaClicouNoServidor = true,
                ultimoSalvamentoUnix = Estado.ultimoSalvamentoUnix,
            };
            nova.dinheiro = 1000 * NivelCertificacaoDe(p, Catalogo.AwsEstagiario);   // AWS Certified Estagiário
            Estado = nova;
            Vendeu?.Invoke(ganhas);
            return true;
        }

        static int NivelCertificacaoDe(Prestigio p, string id)
        {
            foreach (var b in p.bonus) if (b.id == id) return b.nivel;
            return 0;
        }

        public bool PodeComprarCertificacao(string id)
        {
            var c = Catalogo.BuscarCertificacao(id);
            int nivel = NivelCertificacao(id);
            return nivel < c.NivelMaximo && Prestigio.certificacoes >= c.Custo(nivel);
        }

        public bool ComprarCertificacao(string id)
        {
            if (!PodeComprarCertificacao(id)) return false;
            var c = Catalogo.BuscarCertificacao(id);
            Prestigio.certificacoes -= c.Custo(NivelCertificacao(id));
            var registro = Prestigio.bonus.Find(b => b.id == id);
            if (registro == null) Prestigio.bonus.Add(registro = new NivelMelhoria { id = id });
            registro.nivel++;
            return true;
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
        public bool PodeTomarCafe => Estado.cafeRecarga <= 0 && !Desafio(Catalogo.SemCafe);

        /// <summary>O técnico toma um café: a receita dobra por 30 s; o próximo só depois de 3 min.</summary>
        public bool TomarCafe()
        {
            if (!PodeTomarCafe) return false;
            Estado.cafeRestante = Catalogo.DuracaoCafe + NivelCertificacao(Catalogo.ScrumCafe) * 10;
            Estado.cafeRecarga = Catalogo.RecargaCafe;
            Estado.cafesTomados++;
            return true;
        }

        void AvancarCafe(double segundos)
        {
            if (Estado.cafeRestante > 0) Estado.cafeRestante = Math.Max(0, Estado.cafeRestante - segundos);
            if (Estado.cafeRecarga > 0) Estado.cafeRecarga -= segundos;
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
            // Kubernetes Whisperer: cada nível escreve 15% mais rápido
            Estado.segundosEscritos += segundos / Math.Max(0.4, 1 - NivelCertificacao(Catalogo.K8sWhisperer) * 0.15);
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

        public double HorasMaximasOffline => (TemAutomacao(Catalogo.Plantao) ? Catalogo.HorasOfflineComPlantao : Catalogo.HorasMaximasOffline)
                                             + NivelCertificacao(Catalogo.LinuxPlantao) * 4;

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
                LimparChamadosOffline();
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
