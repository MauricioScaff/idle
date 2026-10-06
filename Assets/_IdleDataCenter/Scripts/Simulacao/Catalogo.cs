using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Definição de uma melhoria comprável (dados fixos; o nível comprado fica no EstadoJogo).</summary>
    public class MelhoriaDef
    {
        public string Id;
        public string Nome;
        public string Efeito;       // texto curto para a interface
        public int Cargo;           // cargo em que aparece na loja
        public string Requisito;    // id de outra melhoria que precisa estar comprada (ou null)
        public int NivelMaximo;
        public double CustoBase;
        public double FatorCusto;   // custo do nível n = CustoBase × FatorCusto^n
        /// <summary>Gerador: vai até 100 e a renda dele dobra a cada marco (10, 25, 50, 100 unidades).</summary>
        public bool Gerador;
        /// <summary>Melhoria simples: aumenta um alvo (ver Catalogo.AlvoTorres...) em BonusPorNivel por nível.</summary>
        public string Alvo;
        public double BonusPorNivel;

        public double Custo(int nivelAtual) => Math.Round(CustoBase * Math.Pow(FatorCusto, nivelAtual));
    }

    /// <summary>Automação: um script que o técnico escreve (leva tempo real) e depois trabalha sozinho para sempre.</summary>
    public class AutomacaoDef
    {
        public string Id;
        public string Nome;
        public string Descricao;    // o que ela faz, em uma linha
        public int Cargo = Catalogo.CargoAnalista;   // cargo a partir do qual pode ser escrita
        public string Requisito;    // id de melhoria que precisa estar comprada (ou null)
        public double Custo;
        public double Segundos;     // tempo para escrever
    }

    /// <summary>Certificação comprada com a moeda de prestígio: bônus permanente, com níveis.</summary>
    public class CertificacaoDef
    {
        public string Id, Nome, Efeito;
        public int NivelMaximo;
        public int CustoBase;                  // nível n custa CustoBase × (n + 1)
        public int Custo(int nivelAtual) => CustoBase * (nivelAtual + 1);
    }

    /// <summary>Desafio opcional de uma empresa nova: mais difícil, mais certificações.</summary>
    public class DesafioDef
    {
        public string Id, Nome, Descricao;
        public double Multiplicador;
    }

    public enum TipoMeta { Servidores, TotalGanho, IncidentesResolvidos, ServidoresRack, BackupsRestaurados, AutomacoesAtivas, HostsContainers, PicosSobrevividos, Datacenters, Regioes }

    public class MetaDef
    {
        public TipoMeta Tipo;
        public double Alvo;
        public string Texto;
    }

    public class CargoDef
    {
        public string Nome;
        public string Lugar;                 // onde o técnico trabalha nesse cargo
        public MetaDef[] MetasParaPromocao;  // vazio = último cargo disponível por enquanto
    }

    /// <summary>
    /// Números de balanceamento. Tudo que mexe no ritmo do jogo fica aqui, num lugar só.
    /// </summary>
    public static partial class Catalogo
    {
        // --- Ids das melhorias ---
        public const string Ssd = "ssd";
        public const string Ventoinha = "ventoinha";
        public const string Servidor = "servidor";      // servidor torre extra (cargo 1)
        public const string Rack = "rack";
        public const string Servidor1U = "servidor1u";
        public const string NoBreak = "nobreak";
        public const string ArCondicionado = "arcond";
        public const string Estagiario = "estagiario";
        public const string RackCheio = "rackcheio";   // rack inteiro de servidores (cargo 3)
        public const string Storage = "storage";
        public const string Backup = "backup";
        /// <summary>A linha de backup, do mais barato ao mais caro: HD externo (Técnico), NAS (Sysadmin), fita (Analista),
        /// sala de backup (DevOps), DC de recuperação (Arquiteto) e backup em outra região (CTO).</summary>
        public const string HdExterno = "hdexterno", Nas = "nas", SalaBackup = "salabackup", DcRecuperacao = "dcrecuperacao", BackupRegiao = "backupregiao";
        public static readonly string[] LinhaDeBackup = { HdExterno, Nas, Backup, SalaBackup, DcRecuperacao, BackupRegiao };
        /// <summary>A linha de segurança: cada nível bloqueia metade dos ataques que ainda passavam.</summary>
        public const string Antivirus = "antivirus", Firewall = "firewall", VpnMfa = "vpnmfa", ScannerVulnerabilidades = "scanner",
                            Waf = "waf", Soc = "soc", ZeroTrust = "zerotrust";
        public static readonly string[] LinhaDeSeguranca = { Antivirus, Firewall, VpnMfa, ScannerVulnerabilidades, Waf, Soc, ZeroTrust };
        public const string Link = "link";
        public const string Hypervisor = "hypervisor";   // cargo 4 (DevOps)
        public const string Containers = "containers";
        public const string ServidorCi = "ci";
        public const string Link10G = "link10g";
        public const string NoKubernetes = "k8s";        // cargo 5 (SRE)
        public const string Balanceador = "balanceador";
        public const string Observabilidade = "observabilidade";
        public const string Datacenter = "datacenter";      // cargo 6 (Arquiteto)
        public const string Fibra = "fibra";
        public const string Cdn = "cdn";
        public const string Gerador = "gerador";
        public const string Regiao = "regiao";              // cargo 7 (CTO)
        public const string CaboSubmarino = "cabo";
        public const string Renovavel = "renovavel";
        public const string Gpu = "gpu";
        // melhorias simples (multiplicam a renda de um gerador, ou dão energia/frio)
        public const string FiltroDeLinha = "filtro", Ventilador = "ventilador", PastaTermica = "pasta";
        public const string CabosOrganizados = "cabos", Firmware = "firmware", PisoElevado = "piso";
        public const string ImagensEnxutas = "imagens", CacheRedis = "redis", ServiceMesh = "mesh";
        public const string ResfriamentoLiquido = "liquido", ContratoDeEnergia = "ppa", Edge = "edge", ChipsProprios = "chips";

        // alvos das melhorias simples
        public const string AlvoTorres = "torres", Alvo1U = "1u", AlvoRackCheio = "rackcheio", AlvoApps = "apps", AlvoK8s = "k8s",
                            AlvoDatacenters = "datacenters", AlvoRegioes = "regioes", AlvoGpu = "gpu", AlvoKw = "kw", AlvoGraus = "graus";

        /// <summary>Marcos dos geradores: ao chegar a cada um, a renda daquele gerador dobra.</summary>
        public static readonly int[] Marcos = { 10, 25, 50, 100 };
        public const int MaximoGerador = 100;
        /// <summary>Só os primeiros servidores de cada tipo travam (senão, com dezenas, os alertas não parariam).</summary>
        public const int ServidoresQueTravam = 8;

        // --- Receita ---
        public const double ReceitaBaseServidor = 2;   // servidor torre sem melhorias (R$/s)
        public const double BonusSsd = 1.0;            // +100%
        public const double BonusVentoinha = 0.5;      // +50% por ventoinha
        public const double ReceitaServidor1U = 18;     // servidor de rack (R$/s)
        public const int VagasNoRack = 5;              // o rack da arte tem 5 unidades
        public const int ServidoresPorRackCheio = 8;
        public const double ReceitaRackCheio = 144;     // 8 servidores 1U novos
        public const double BonusStorage = 0.25;       // banco de dados gerenciado: +25% da receita por nível
        public const double BonusVirtualizacao = 0.4;  // VMs: servidores +40% por nível de hypervisor
        public const double ReceitaHostContainers = 500; // apps por host
        public const double BonusCi = 0.5;             // deploys contínuos: apps +50%
        public const double ReceitaNoKubernetes = 800;  // Kubernetes gerenciado, por nó
        public const double BonusBalanceador = 0.25;    // K8s +25%
        public const double BonusObservabilidade = 0.15; // SLA premium: receita +15% por nível
        public const double ReceitaDatacenter = 10000;   // cada datacenter novo (tem energia, refrigeração e link próprios)
        public const double BonusFibra = 0.2;           // rede global: +20% por datacenter interligado
        public const double BonusCdn = 0.3;             // CDN: +30% por nível
        public const double ReducaoTrafegoCdn = 0.3;    // e 30% menos tráfego no link do DC-01 por nível
        public const double ReceitaRegiao = 200000;     // cada região nova (vários campi)
        public const double BonusCabo = 0.25;            // cabo submarino: +25% por região ligada
        public const double BonusRenovavel = 0.15;       // contratos "verdes": +15% por nível
        public const double ReceitaGpu = 120000;          // nuvem de IA, por cluster de GPU
        public const double BonusAiops = 0.2;
        public static readonly string[] NomesRegioes = { "América do Sul", "América do Norte", "Europa", "Ásia" };

        /// <summary>Clique no servidor vale isto + ValorCliqueReceita × receita por segundo.</summary>
        public const double ValorCliqueBase = 4;
        public const double ValorCliqueReceita = 1;

        // --- Energia (kW) ---
        public const double ConsumoServidorTorre = 0.4;
        public const double ConsumoServidor1U = 0.4;
        public const double CapacidadeBaseKw = 1.5;
        public const double CapacidadePorNoBreak = 1.5;
        public const double ConsumoRackCheio = 1.6;     // servidores novos gastam metade
        public const double ConsumoStorage = 0.3;
        /// <summary>A sala de racks (cargo 3) já vem com quadro de energia próprio.</summary>
        public const double CapacidadeSalaDeRacksKw = 10;
        /// <summary>Na sala virtualizada (DevOps), mais um quadro de energia e contenção de corredor.</summary>
        public const double CapacidadeSalaVirtualizadaKw = 4;
        public const double GrausSalaVirtualizada = 8;
        public const double ConsumoHostContainers = 1.0;
        public const double ConsumoServidorCi = 0.5;
        /// <summary>No SRE o data center pequeno ganha mais um quadro de energia e refrigeração.</summary>
        public const double CapacidadeDataCenterKw = 6;
        public const double GrausDataCenter = 6;
        public const double ConsumoNoKubernetes = 0.8;

        // --- Temperatura (°C) ---
        public const double TemperaturaAmbiente = 22;
        public const double TemperaturaMinima = 18;   // o ar-condicionado não deixa a sala mais fria que isso
        public const double GrausPorKw = 5;
        public const double GrausPorArCondicionado = 6;
        public const double TemperaturaQuente = 32;    // acima: desempenho cai e travam mais
        public const double TemperaturaCritica = 40;
        public const double TemperaturaMaxima = 60;    // teto: acima disso a sala já estaria desligada (antes chegava a 274 °C)
        /// <summary>Ar de precisão da sala de racks.</summary>
        public const double GrausArDePrecisao = 12;
        /// <summary>Com piso técnico e corredor frio, só metade do calor dos equipamentos fica na sala.</summary>
        public const double FatorCalorSalaDeRacks = 0.5;

        // --- Banda (Mbps) ---
        public const double TrafegoTorre = 10;
        public const double TrafegoServidor1U = 20;
        public const double TrafegoRackCheio = 160;
        public const double BandaBase = 200;
        public const double BandaPorLink = 300;
        public const double TrafegoHostContainers = 120;
        public const double BandaLink10G = 1500;
        public const double TrafegoNoKubernetes = 150;

        // --- Incidentes ---
        public const double MtbfServidorTorre = 300;   // segundos, em média, entre travadas
        public const double MtbfServidor1U = 900;
        public const double MultiplicadorQuente = 3;
        public const double TempoConsertoTecnico = 30; // o técnico resolve sozinho depois disso
        public const double TempoConsertoComEstagiario = 15;
        public const double MtbfDisco = 1200;          // por nível de storage (mais discos, mais falhas)
        public const double MtbfDiscoSemStorage = 1800;  // antes do storage, o disco que queima é o HD da torre
        /// <summary>Cada nível da linha de backup dá esta fração a mais de receita (os clientes confiam).</summary>
        public const double BonusPorBackup = 0.02;
        /// <summary>Sem backup, um disco queimado custa esta quantidade de segundos de receita em reembolsos.</summary>
        public const double SegundosPerdidosSemBackup = 120;
        public const double MtbfDeploy = 1200;          // por host de containers

        // --- Café e chamados urgentes (para quem está jogando) ---
        public const double DuracaoCafe = 30, RecargaCafe = 180, MultiplicadorCafe = 2;
        public const int PassosTutorial = 6;

        // --- Automações ---
        public const int CargoDasAutomacoes = CargoAnalista;         // liberam no Analista de Infra
        public const double TempoWatchdog = 5;           // reinicia servidor travado sozinho
        public const double TempoHotSpare = 5;           // troca de disco automática
        public const double FatorMtbfMonitoramento = 2;  // incidentes pela metade
        public const double TempoRollbackAutomatico = 5;
        public const double FatorPipeline = 4;           // testes no pipeline: 4x menos deploys quebrados
        public const double DescontoIac = 0.15;          // infra como código: melhorias 15% mais baratas

        // --- Queda de energia nos datacenters (Arquiteto) ---
        public const double MtbfQuedaDeEnergia = 1500;   // por datacenter extra
        public const double TempoGerador = 5, TempoFailover = 2;
        public const double MtbfPaneRegional = 2400;     // por região extra
        public const double TempoFailoverMultirregiao = 2;

        // --- Picos de tráfego (SRE) ---
        public const double PrimeiroPico = 600;          // segundos depois de virar SRE
        public const double IntervaloPicoMin = 900, IntervaloPicoMax = 1500;
        public const double DuracaoPico = 60;
        public const double TempoParaEscalar = 15;       // sem escalar até aqui, o SLA é violado
        public const double TempoExtraBalanceador = 10;
        public const double MultiplicadorPicoEscalado = 2;  // pico atendido: K8s rende o dobro
        public const double MultiplicadorPicoViolado = 0.3;
        public const double SegundosDeMultaSla = 20;      // multa: 20 s da receita total
        public const double TempoAutoscaling = 2;
        public const double FatorChaos = 2;               // chaos engineering: metade das falhas
        public const double TempoRunbook = 3;             // runbooks: todo conserto automático em 3 s
        public static readonly string[] NomesDePico = { "Black Friday", "Final da copa", "Lançamento", "Live famosa", "Promoção" };  // curtos: cabem ao lado do botão no painel

        // --- Progresso offline ---
        public const double TaxaOffline = 0.5;
        public const double HorasMaximasOffline = 12;
        public const double BonusOfflinePorAutomacao = 0.25;   // cron e plantão: 50% → 75% → 100%
        public const double HorasOfflineComPlantao = 24;
        public const double SegundosMinimosOffline = 60;

        /// <summary>Os cargos pelo nome (índice em Cargos): o código nunca usa o número solto, para dar para inserir um cargo.</summary>
        public const int CargoTecnico = 0, CargoSysadmin = 1, CargoAnalista = 2, CargoDevOps = 3, CargoSre = 4, CargoArquiteto = 5, CargoCto = 6;

        public static readonly IReadOnlyList<CargoDef> Cargos = new[]
        {
            new CargoDef
            {
                Nome = "Técnico de TI", Lugar = "Armário",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.Servidores, Alvo = 3, Texto = "Ter 3 servidores" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 120000, Texto = "Faturar R$ 120 mil" },
                    new MetaDef { Tipo = TipoMeta.IncidentesResolvidos, Alvo = 8, Texto = "Resolver 8 incidentes" },
                },
            },
            new CargoDef
            {
                Nome = "Sysadmin", Lugar = "Salinha",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.ServidoresRack, Alvo = VagasNoRack, Texto = "Encher o rack" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 7200000, Texto = "Faturar R$ 7,2 mi" },
                    new MetaDef { Tipo = TipoMeta.IncidentesResolvidos, Alvo = 60, Texto = "Resolver 60 incidentes" },
                },
            },
            new CargoDef
            {
                Nome = "Analista de Infra", Lugar = "Sala de racks",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.BackupsRestaurados, Alvo = 1, Texto = "Restaurar um backup" },
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 3, Texto = "3 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 180000000, Texto = "Faturar R$ 180 mi" },
                },
            },
            new CargoDef
            {
                Nome = "Engenheiro DevOps", Lugar = "Sala virtualizada",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 7, Texto = "7 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.HostsContainers, Alvo = 4, Texto = "4 hosts de containers" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 6000000000, Texto = "Faturar R$ 6 bi" },
                },
            },
            new CargoDef
            {
                Nome = "SRE", Lugar = "Data center pequeno",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.PicosSobrevividos, Alvo = 5, Texto = "Superar 5 picos" },
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 10, Texto = "10 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 24000000000, Texto = "Faturar R$ 24 bi" },
                },
            },
            new CargoDef
            {
                Nome = "Arquiteto", Lugar = "Campus de datacenters",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.Datacenters, Alvo = 4, Texto = "Ter 4 datacenters" },
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 12, Texto = "12 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 240000000000, Texto = "Faturar R$ 240 bi" },
                },
            },
            // o último cargo: as "metas" do CTO são as do IPO (abrir o capital), o fim da carreira
            new CargoDef
            {
                Nome = "CTO", Lugar = "Mapa-múndi",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.Regioes, Alvo = 4, Texto = "Estar em 4 regiões" },
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 15, Texto = "15 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 3600000000000, Texto = "Faturar R$ 3,6 tri" },
                },
            },
        };

        public static readonly IReadOnlyList<MelhoriaDef> Melhorias = new[]
        {
            // Técnico: o armário. As torres vão longe, mas pedem energia (filtro de linha) e frio (ventilador).
            new MelhoriaDef { Id = Ssd, Nome = "SSD", Efeito = "Torres ×2", Cargo = Catalogo.CargoTecnico, NivelMaximo = 1, CustoBase = 600, FatorCusto = 1 },
            new MelhoriaDef { Id = Ventoinha, Nome = "Ventoinha", Efeito = "Torres +50%", Cargo = Catalogo.CargoTecnico, NivelMaximo = 3, CustoBase = 1500, FatorCusto = 2.2 },
            new MelhoriaDef { Id = Servidor, Nome = "Servidor", Efeito = "+1 torre", Cargo = Catalogo.CargoTecnico, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 6000, FatorCusto = 1.4 },
            new MelhoriaDef { Id = FiltroDeLinha, Nome = "Filtro de linha", Efeito = "+0.8 kW", Cargo = Catalogo.CargoTecnico, NivelMaximo = 12, Alvo = AlvoKw, BonusPorNivel = 0.8, CustoBase = 3600, FatorCusto = 1.8 },
            new MelhoriaDef { Id = Ventilador, Nome = "Ventilador", Efeito = "-3 graus", Cargo = Catalogo.CargoTecnico, NivelMaximo = 12, Alvo = AlvoGraus, BonusPorNivel = 3, CustoBase = 3000, FatorCusto = 1.8 },
            new MelhoriaDef { Id = PastaTermica, Nome = "Pasta térmica", Efeito = "Torres +30%", Cargo = Catalogo.CargoTecnico, NivelMaximo = 3, Alvo = AlvoTorres, BonusPorNivel = 0.3, CustoBase = 10800, FatorCusto = 2.5 },
            new MelhoriaDef { Id = HdExterno, Nome = "HD externo", Efeito = "Backup: disco queimado perde metade, +2% renda", Cargo = Catalogo.CargoTecnico, NivelMaximo = 1, CustoBase = 9000, FatorCusto = 1 },
            new MelhoriaDef { Id = Antivirus, Nome = "Antivírus", Efeito = "Segurança: bloqueia metade dos ataques", Cargo = Catalogo.CargoTecnico, NivelMaximo = 1, CustoBase = 4800, FatorCusto = 1 },
            new MelhoriaDef { Id = Estagiario, Nome = "Estagiário", Efeito = "Conserto em 15s", Cargo = Catalogo.CargoTecnico, NivelMaximo = 1, CustoBase = 15000, FatorCusto = 1 },

            // Sysadmin: a salinha com o primeiro rack.
            new MelhoriaDef { Id = Rack, Nome = "Rack 42U", Efeito = "Vagas para servidores 1U", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 1, CustoBase = 48000, FatorCusto = 1 },
            new MelhoriaDef { Id = Servidor1U, Nome = "Servidor 1U", Efeito = "+18/s, 0.4 kW", Cargo = Catalogo.CargoSysadmin, Requisito = Rack, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 42000, FatorCusto = 1.3 },
            new MelhoriaDef { Id = NoBreak, Nome = "No-break", Efeito = "+1.5 kW", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 60, CustoBase = 36000, FatorCusto = 1.35 },
            new MelhoriaDef { Id = ArCondicionado, Nome = "Ar-cond.", Efeito = "-6 graus", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 60, CustoBase = 30000, FatorCusto = 1.35 },
            new MelhoriaDef { Id = CabosOrganizados, Nome = "Cabos organizados", Efeito = "Servidores 1U +25%", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 3, Alvo = Alvo1U, BonusPorNivel = 0.25, CustoBase = 90000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Firmware, Nome = "Firmware novo", Efeito = "Servidores 1U +40%", Cargo = Catalogo.CargoSysadmin, Requisito = Servidor1U, NivelMaximo = 2, Alvo = Alvo1U, BonusPorNivel = 0.4, CustoBase = 240000, FatorCusto = 3 },
            new MelhoriaDef { Id = Nas, Nome = "NAS", Efeito = "Backup: perde só 1/4, +2% renda", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 1, CustoBase = 150000, FatorCusto = 1 },
            new MelhoriaDef { Id = Firewall, Nome = "Firewall", Efeito = "Segurança: bloqueia 3/4 dos ataques", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 1, CustoBase = 120000, FatorCusto = 1 },
            new MelhoriaDef { Id = HelpDesk, Nome = "Analista de help desk", Efeito = "Fecha os chamados P3 e P4 sozinho", Cargo = Catalogo.CargoSysadmin, NivelMaximo = 1, CustoBase = 180000, FatorCusto = 1 },

            // Analista: a sala de racks.
            new MelhoriaDef { Id = RackCheio, Nome = "Rack cheio", Efeito = "+144/s, 1.6 kW, 160 Mb", Cargo = Catalogo.CargoAnalista, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 540000, FatorCusto = 1.3 },
            new MelhoriaDef { Id = Storage, Nome = "Storage", Efeito = "RAID: receita +25%", Cargo = Catalogo.CargoAnalista, NivelMaximo = 3, CustoBase = 900000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Backup, Nome = "Backup fita", Efeito = "Backup: perde só 1/8, +2% renda", Cargo = Catalogo.CargoAnalista, NivelMaximo = 1, CustoBase = 600000, FatorCusto = 1 },
            new MelhoriaDef { Id = VpnMfa, Nome = "VPN e MFA", Efeito = "Segurança: bloqueia 7/8 dos ataques", Cargo = Catalogo.CargoAnalista, NivelMaximo = 1, CustoBase = 720000, FatorCusto = 1 },
            new MelhoriaDef { Id = Link, Nome = "Link fibra", Efeito = "+300 Mbps", Cargo = Catalogo.CargoAnalista, NivelMaximo = 40, CustoBase = 300000, FatorCusto = 1.45 },
            new MelhoriaDef { Id = PisoElevado, Nome = "Piso elevado", Efeito = "Racks cheios +30%", Cargo = Catalogo.CargoAnalista, Requisito = RackCheio, NivelMaximo = 3, Alvo = AlvoRackCheio, BonusPorNivel = 0.3, CustoBase = 1500000, FatorCusto = 2.5 },

            // DevOps: a sala virtualizada.
            new MelhoriaDef { Id = Hypervisor, Nome = "Hypervisor", Efeito = "VMs: servidores +40%", Cargo = Catalogo.CargoDevOps, NivelMaximo = 3, CustoBase = 7200000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Containers, Nome = "Containers", Efeito = "+500/s em apps, 1 kW, 120 Mb", Cargo = Catalogo.CargoDevOps, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 4800000, FatorCusto = 1.3 },
            new MelhoriaDef { Id = ServidorCi, Nome = "Servidor CI", Efeito = "Deploy contínuo: apps +50%", Cargo = Catalogo.CargoDevOps, Requisito = Containers, NivelMaximo = 1, CustoBase = 9000000, FatorCusto = 1 },
            new MelhoriaDef { Id = Link10G, Nome = "Link 10G", Efeito = "+1500 Mbps", Cargo = Catalogo.CargoDevOps, NivelMaximo = 30, CustoBase = 4200000, FatorCusto = 1.5 },
            new MelhoriaDef { Id = ImagensEnxutas, Nome = "Imagens enxutas", Efeito = "Apps +30%", Cargo = Catalogo.CargoDevOps, Requisito = Containers, NivelMaximo = 3, Alvo = AlvoApps, BonusPorNivel = 0.3, CustoBase = 12000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = CacheRedis, Nome = "Cache Redis", Efeito = "Apps +50%", Cargo = Catalogo.CargoDevOps, Requisito = Containers, NivelMaximo = 1, Alvo = AlvoApps, BonusPorNivel = 0.5, CustoBase = 30000000, FatorCusto = 1 },
            new MelhoriaDef { Id = SalaBackup, Nome = "Sala de backup", Efeito = "Backup separado: perde só 1/16, +2% renda", Cargo = Catalogo.CargoDevOps, NivelMaximo = 1, CustoBase = 18000000, FatorCusto = 1 },
            new MelhoriaDef { Id = ScannerVulnerabilidades, Nome = "Scanner de vulnerabilidades", Efeito = "Segurança no pipeline: bloqueia 15/16", Cargo = Catalogo.CargoDevOps, NivelMaximo = 1, CustoBase = 15000000, FatorCusto = 1 },
            new MelhoriaDef { Id = ServiceDesk, Nome = "Service desk 24h", Efeito = "Fecha também os chamados P2", Cargo = Catalogo.CargoDevOps, Requisito = HelpDesk, NivelMaximo = 1, CustoBase = 18000000, FatorCusto = 1 },

            // SRE: o data center pequeno e o cluster.
            new MelhoriaDef { Id = NoKubernetes, Nome = "Nó K8s", Efeito = "+800/s, 0.8 kW, 150 Mb", Cargo = Catalogo.CargoSre, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 24000000, FatorCusto = 1.28 },
            new MelhoriaDef { Id = Balanceador, Nome = "Balanceador", Efeito = "K8s +25%, +10 s p/ escalar", Cargo = Catalogo.CargoSre, Requisito = NoKubernetes, NivelMaximo = 1, CustoBase = 36000000, FatorCusto = 1 },
            new MelhoriaDef { Id = Observabilidade, Nome = "Observab.", Efeito = "Observabilidade: +15% (SLA)", Cargo = Catalogo.CargoSre, NivelMaximo = 3, CustoBase = 30000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = ServiceMesh, Nome = "Service mesh", Efeito = "K8s +30%", Cargo = Catalogo.CargoSre, Requisito = NoKubernetes, NivelMaximo = 3, Alvo = AlvoK8s, BonusPorNivel = 0.3, CustoBase = 60000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Waf, Nome = "WAF anti-DDoS", Efeito = "Segurança: bloqueia 31/32 dos ataques", Cargo = Catalogo.CargoSre, NivelMaximo = 1, CustoBase = 48000000, FatorCusto = 1 },

            // Arquiteto: o campus.
            new MelhoriaDef { Id = Datacenter, Nome = "Datacenter", Efeito = "Novo prédio: +10 mil/s", Cargo = Catalogo.CargoArquiteto, NivelMaximo = 3, CustoBase = 300000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Fibra, Nome = "Fibra", Efeito = "Liga um DC: receita +20%", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, NivelMaximo = 3, CustoBase = 180000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Cdn, Nome = "CDN", Efeito = "+30% e -30% de tráfego", Cargo = Catalogo.CargoArquiteto, NivelMaximo = 3, CustoBase = 240000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Gerador, Nome = "Gerador", Efeito = "Queda de luz: volta em 5 s", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, NivelMaximo = 1, CustoBase = 120000000, FatorCusto = 1 },
            new MelhoriaDef { Id = ResfriamentoLiquido, Nome = "Resfriamento líquido", Efeito = "Datacenters +30%", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, NivelMaximo = 3, Alvo = AlvoDatacenters, BonusPorNivel = 0.3, CustoBase = 480000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = ContratoDeEnergia, Nome = "Contrato de energia", Efeito = "Datacenters +50%", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, NivelMaximo = 1, Alvo = AlvoDatacenters, BonusPorNivel = 0.5, CustoBase = 900000000, FatorCusto = 1 },
            new MelhoriaDef { Id = DcRecuperacao, Nome = "DC de recuperação", Efeito = "Backup em outro prédio: perde só 1/32, +2% renda", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, NivelMaximo = 1, CustoBase = 360000000, FatorCusto = 1 },
            new MelhoriaDef { Id = Soc, Nome = "SOC 24h", Efeito = "Centro de segurança: bloqueia 63/64", Cargo = Catalogo.CargoArquiteto, NivelMaximo = 1, CustoBase = 480000000, FatorCusto = 1 },

            // CTO: o mundo.
            new MelhoriaDef { Id = Regiao, Nome = "Região", Efeito = "Nova região: +200 mil/s", Cargo = Catalogo.CargoCto, NivelMaximo = 3, CustoBase = 9000000000, FatorCusto = 3 },
            new MelhoriaDef { Id = CaboSubmarino, Nome = "Cabo sub.", Efeito = "Liga uma região: +25%", Cargo = Catalogo.CargoCto, Requisito = Regiao, NivelMaximo = 3, CustoBase = 4800000000, FatorCusto = 3 },
            new MelhoriaDef { Id = Renovavel, Nome = "Renovável", Efeito = "Energia verde: +15%", Cargo = Catalogo.CargoCto, NivelMaximo = 3, CustoBase = 6000000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Gpu, Nome = "GPU / IA", Efeito = "Nuvem de IA: +120 mil/s", Cargo = Catalogo.CargoCto, NivelMaximo = MaximoGerador, Gerador = true, CustoBase = 7200000000, FatorCusto = 1.35 },
            new MelhoriaDef { Id = Edge, Nome = "Edge computing", Efeito = "Regiões +30%", Cargo = Catalogo.CargoCto, Requisito = Regiao, NivelMaximo = 3, Alvo = AlvoRegioes, BonusPorNivel = 0.3, CustoBase = 12000000000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = ChipsProprios, Nome = "Chips próprios", Efeito = "GPU +50%", Cargo = Catalogo.CargoCto, Requisito = Gpu, NivelMaximo = 2, Alvo = AlvoGpu, BonusPorNivel = 0.5, CustoBase = 30000000000, FatorCusto = 3 },
            new MelhoriaDef { Id = BackupRegiao, Nome = "Backup em outra região", Efeito = "Backup do outro lado do mundo: não perde nada, +2% renda", Cargo = Catalogo.CargoCto, Requisito = Regiao, NivelMaximo = 1, CustoBase = 12000000000, FatorCusto = 1 },
            new MelhoriaDef { Id = ZeroTrust, Nome = "Zero Trust", Efeito = "Ninguém é confiável: bloqueia quase todos", Cargo = Catalogo.CargoCto, NivelMaximo = 1, CustoBase = 18000000000, FatorCusto = 1 },
        };

        // --- Ids das automações ---
        public const string Watchdog = "watchdog";
        public const string HotSpare = "hotspare";
        public const string Monitoramento = "monitoramento";
        public const string CronFaturamento = "cron";
        public const string Plantao = "plantao";
        public const string Pipeline = "pipeline";
        public const string RollbackAutomatico = "rollback";
        public const string InfraComoCodigo = "iac";
        public const string Autoscaling = "autoscaling";
        public const string Chaos = "chaos";
        public const string Runbooks = "runbooks";
        public const string Failover = "failover";
        public const string BalanceamentoGlobal = "global";
        public const string Multirregiao = "multirregiao";
        public const string Aiops = "aiops";

        public static readonly IReadOnlyList<AutomacaoDef> Automacoes = new[]
        {
            new AutomacaoDef { Id = Watchdog, Nome = "Watchdog", Descricao = "Reinicia servidor travado em 5 s", Custo = 900000, Segundos = 180 },
            new AutomacaoDef { Id = HotSpare, Nome = "Troca de disco", Descricao = "Disco reserva entra sozinho em 5 s", Requisito = Storage, Custo = 1500000, Segundos = 300 },
            new AutomacaoDef { Id = Monitoramento, Nome = "Monitoramento", Descricao = "Alerta antes da falha: metade dos incidentes", Custo = 2400000, Segundos = 480 },
            new AutomacaoDef { Id = CronFaturamento, Nome = "Cron de faturamento", Descricao = "Receita offline sobe para 75%", Custo = 1800000, Segundos = 360 },
            new AutomacaoDef { Id = Plantao, Nome = "Plantão 24h", Descricao = "Offline a 100%, até 24 h", Requisito = null, Custo = 3600000, Segundos = 600 },
            new AutomacaoDef { Id = Pipeline, Nome = "Pipeline com testes", Descricao = "4x menos deploys quebrados", Cargo = Catalogo.CargoDevOps, Requisito = ServidorCi, Custo = 6000000, Segundos = 600 },
            new AutomacaoDef { Id = RollbackAutomatico, Nome = "Rollback automático", Descricao = "Deploy quebrado volta em 5 s", Cargo = Catalogo.CargoDevOps, Requisito = Containers, Custo = 4800000, Segundos = 480 },
            new AutomacaoDef { Id = InfraComoCodigo, Nome = "Infra como código", Descricao = "Melhorias 15% mais baratas", Cargo = Catalogo.CargoDevOps, Custo = 9000000, Segundos = 720 },
            new AutomacaoDef { Id = Autoscaling, Nome = "Autoscaling", Descricao = "Escala o cluster sozinho nos picos", Cargo = Catalogo.CargoSre, Requisito = NoKubernetes, Custo = 48000000, Segundos = 900 },
            new AutomacaoDef { Id = Chaos, Nome = "Chaos engineering", Descricao = "Falhas testadas antes: metade dos incidentes", Cargo = Catalogo.CargoSre, Custo = 36000000, Segundos = 720 },
            new AutomacaoDef { Id = Runbooks, Nome = "Runbooks automáticos", Descricao = "Todo conserto automático em 3 s", Cargo = Catalogo.CargoSre, Custo = 60000000, Segundos = 1080 },
            new AutomacaoDef { Id = Failover, Nome = "Failover entre DCs", Descricao = "Queda de energia volta em 2 s", Cargo = Catalogo.CargoArquiteto, Requisito = Datacenter, Custo = 360000000, Segundos = 1200 },
            new AutomacaoDef { Id = BalanceamentoGlobal, Nome = "Balanceamento global", Descricao = "Tráfego no DC certo: receita +15%", Cargo = Catalogo.CargoArquiteto, Requisito = Fibra, Custo = 540000000, Segundos = 1500 },
            new AutomacaoDef { Id = Multirregiao, Nome = "Failover multirregião", Descricao = "Pane regional volta em 2 s", Cargo = Catalogo.CargoCto, Requisito = Regiao, Custo = 5400000000, Segundos = 1800 },
            new AutomacaoDef { Id = Aiops, Nome = "AIOps", Descricao = "IA cuidando da operação: +20%", Cargo = Catalogo.CargoCto, Requisito = Gpu, Custo = 9000000000, Segundos = 2400 },
        };

        public static AutomacaoDef BuscarAutomacao(string id)
        {
            foreach (var a in Automacoes) if (a.Id == id) return a;
            throw new ArgumentException("Automação desconhecida: " + id);
        }

        // --- Prestígio ---
        public const int CargoParaVender = 4;          // a partir do SRE dá para vender a empresa
        public const double MultiplicadorIpo = 2;       // depois do IPO a venda vale o dobro
        public const string UptimeWizard = "uptime", ItilGambiarra = "itil", AwsEstagiario = "aws",
                            ScrumCafe = "scrum", K8sWhisperer = "whisperer", LinuxPlantao = "linux";

        public static readonly IReadOnlyList<CertificacaoDef> Certificacoes = new[]
        {
            new CertificacaoDef { Id = UptimeWizard, Nome = "Certified Uptime Wizard", Efeito = "+10% de receita por nível", NivelMaximo = 10, CustoBase = 3 },
            new CertificacaoDef { Id = ItilGambiarra, Nome = "ITIL da Gambiarra", Efeito = "10% menos incidentes por nível", NivelMaximo = 5, CustoBase = 4 },
            new CertificacaoDef { Id = AwsEstagiario, Nome = "AWS Certified Estagiário", Efeito = "Começa com R$ 1K por nível", NivelMaximo = 5, CustoBase = 2 },
            new CertificacaoDef { Id = ScrumCafe, Nome = "Scrum Master do Café", Efeito = "Café dura +10 s por nível", NivelMaximo = 5, CustoBase = 2 },
            new CertificacaoDef { Id = K8sWhisperer, Nome = "Kubernetes Whisperer", Efeito = "Scripts 15% mais rápidos por nível", NivelMaximo = 4, CustoBase = 5 },
            new CertificacaoDef { Id = LinuxPlantao, Nome = "Linux+ do Plantão", Efeito = "+4 h de limite offline por nível", NivelMaximo = 3, CustoBase = 4 },
        };

        public const string SemEstagiario = "semestagiario", SemCafe = "semcafe", SoAutomacao = "soautomacao";
        public static readonly IReadOnlyList<DesafioDef> Desafios = new[]
        {
            new DesafioDef { Id = "", Nome = "Sem desafio", Descricao = "Uma empresa normal", Multiplicador = 1 },
            new DesafioDef { Id = SemEstagiario, Nome = "Sem estagiário", Descricao = "Não dá para contratar o estagiário", Multiplicador = 1.25 },
            new DesafioDef { Id = SemCafe, Nome = "Sem café", Descricao = "A cafeteira quebrou de vez", Multiplicador = 1.25 },
            new DesafioDef { Id = SoAutomacao, Nome = "Só automação", Descricao = "Cliques não rendem (consertar ainda vale)", Multiplicador = 1.5 },
        };

        public static CertificacaoDef BuscarCertificacao(string id)
        {
            foreach (var c in Certificacoes) if (c.Id == id) return c;
            throw new ArgumentException("Certificação desconhecida: " + id);
        }

        public static DesafioDef BuscarDesafio(string id)
        {
            foreach (var d in Desafios) if (d.Id == (id ?? "")) return d;
            return Desafios[0];
        }

        public static MelhoriaDef Buscar(string id)
        {
            foreach (var m in Melhorias) if (m.Id == id) return m;
            throw new ArgumentException("Melhoria desconhecida: " + id);
        }

        /// <summary>Como Buscar, mas devolve null para ids que não existem mais (saves antigos).</summary>
        public static MelhoriaDef Achar(string id)
        {
            foreach (var m in Melhorias) if (m.Id == id) return m;
            return null;
        }
    }
}
