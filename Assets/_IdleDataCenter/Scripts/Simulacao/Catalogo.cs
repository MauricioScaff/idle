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

        public double Custo(int nivelAtual) => Math.Round(CustoBase * Math.Pow(FatorCusto, nivelAtual));
    }

    /// <summary>Automação: um script que o técnico escreve (leva tempo real) e depois trabalha sozinho para sempre.</summary>
    public class AutomacaoDef
    {
        public const int CargoAnalista = 2;
        public string Id;
        public string Nome;
        public string Descricao;    // o que ela faz, em uma linha
        public int Cargo = CargoAnalista;   // cargo a partir do qual pode ser escrita
        public string Requisito;    // id de melhoria que precisa estar comprada (ou null)
        public double Custo;
        public double Segundos;     // tempo para escrever
    }

    public enum TipoMeta { Servidores, TotalGanho, IncidentesResolvidos, ServidoresRack, BackupsRestaurados, AutomacoesAtivas, HostsContainers, PicosSobrevividos }

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
    public static class Catalogo
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
        public const string Link = "link";
        public const string Hypervisor = "hypervisor";   // cargo 4 (DevOps)
        public const string Containers = "containers";
        public const string ServidorCi = "ci";
        public const string Link10G = "link10g";
        public const string NoKubernetes = "k8s";        // cargo 5 (SRE)
        public const string Balanceador = "balanceador";
        public const string Observabilidade = "observabilidade";

        // --- Receita ---
        public const double ReceitaBaseServidor = 1;   // servidor torre sem melhorias (R$/s)
        public const double BonusSsd = 1.0;            // +100%
        public const double BonusVentoinha = 0.5;      // +50% por ventoinha
        public const double ReceitaServidor1U = 9;     // servidor de rack (R$/s)
        public const int VagasNoRack = 5;              // o rack da arte tem 5 unidades
        public const int ServidoresPorRackCheio = 8;
        public const double ReceitaRackCheio = 72;     // 8 servidores 1U novos
        public const double BonusStorage = 0.25;       // banco de dados gerenciado: +25% da receita por nível
        public const double BonusVirtualizacao = 0.4;  // VMs: servidores +40% por nível de hypervisor
        public const double ReceitaHostContainers = 250; // apps por host
        public const double BonusCi = 0.5;             // deploys contínuos: apps +50%
        public const double ReceitaNoKubernetes = 400;  // Kubernetes gerenciado, por nó
        public const double BonusBalanceador = 0.25;    // K8s +25%
        public const double BonusObservabilidade = 0.15; // SLA premium: receita +15% por nível

        /// <summary>Clique no servidor vale isto + ValorCliqueReceita × receita por segundo.</summary>
        public const double ValorCliqueBase = 2;
        public const double ValorCliqueReceita = 2;

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
        public const double GrausPorKw = 5;
        public const double GrausPorArCondicionado = 6;
        public const double TemperaturaQuente = 32;    // acima: desempenho cai e travam mais
        public const double TemperaturaCritica = 40;
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
        /// <summary>Sem backup, um disco queimado custa esta quantidade de segundos de receita em reembolsos.</summary>
        public const double SegundosPerdidosSemBackup = 120;
        public const double MtbfDeploy = 1200;          // por host de containers

        // --- Café e chamados urgentes (para quem está jogando) ---
        public const double DuracaoCafe = 30, RecargaCafe = 180, MultiplicadorCafe = 2;
        public const double PrimeiroChamado = 90, IntervaloChamadoMin = 180, IntervaloChamadoMax = 360;
        public const double TempoParaAtender = 20;
        public const double SegundosDeBonusDoChamado = 60;   // o chamado paga 60 s de receita (mínimo R)
        public static readonly string[] Chamados =
        {
            "Impressora não imprime", "É sempre o DNS", "Senha do Wi-Fi", "Mouse sem pilha", "Cliente quer o backup de ontem",
            "Servidor fazendo barulho", "Esqueci a senha", "A internet caiu (não caiu)", "Planilha travou", "Certificado expirou",
        };
        public const int PassosTutorial = 6;

        // --- Automações ---
        public const int CargoDasAutomacoes = 2;         // liberam no Analista de Infra
        public const double TempoWatchdog = 5;           // reinicia servidor travado sozinho
        public const double TempoHotSpare = 5;           // troca de disco automática
        public const double FatorMtbfMonitoramento = 2;  // incidentes pela metade
        public const double TempoRollbackAutomatico = 5;
        public const double FatorPipeline = 4;           // testes no pipeline: 4x menos deploys quebrados
        public const double DescontoIac = 0.15;          // infra como código: melhorias 15% mais baratas

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

        public static readonly IReadOnlyList<CargoDef> Cargos = new[]
        {
            new CargoDef
            {
                Nome = "Técnico de TI", Lugar = "Armário",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.Servidores, Alvo = 3, Texto = "Ter 3 servidores" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 20000, Texto = "Faturar R$ 20K" },
                    new MetaDef { Tipo = TipoMeta.IncidentesResolvidos, Alvo = 8, Texto = "Resolver 8 incidentes" },
                },
            },
            new CargoDef
            {
                Nome = "Sysadmin", Lugar = "Salinha",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.ServidoresRack, Alvo = VagasNoRack, Texto = "Encher o rack" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 600000, Texto = "Faturar R$ 600K" },
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
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 5000000, Texto = "Faturar R$ 5M" },
                },
            },
            new CargoDef
            {
                Nome = "Engenheiro DevOps", Lugar = "Sala virtualizada",
                MetasParaPromocao = new[]
                {
                    new MetaDef { Tipo = TipoMeta.AutomacoesAtivas, Alvo = 7, Texto = "7 automações ativas" },
                    new MetaDef { Tipo = TipoMeta.HostsContainers, Alvo = 4, Texto = "4 hosts de containers" },
                    new MetaDef { Tipo = TipoMeta.TotalGanho, Alvo = 40000000, Texto = "Faturar R$ 40M" },
                },
            },
            new CargoDef { Nome = "SRE", Lugar = "Data center pequeno", MetasParaPromocao = new MetaDef[0] },
        };

        public static readonly IReadOnlyList<MelhoriaDef> Melhorias = new[]
        {
            new MelhoriaDef { Id = Ssd, Nome = "SSD", Efeito = "Torres ×2", Cargo = 0, NivelMaximo = 1, CustoBase = 100, FatorCusto = 1 },
            new MelhoriaDef { Id = Ventoinha, Nome = "Ventoinha", Efeito = "Torres +50%", Cargo = 0, NivelMaximo = 3, CustoBase = 250, FatorCusto = 2.2 },
            new MelhoriaDef { Id = Servidor, Nome = "Servidor", Efeito = "+1 torre", Cargo = 0, NivelMaximo = 2, CustoBase = 1000, FatorCusto = 3 },
            new MelhoriaDef { Id = Estagiario, Nome = "Estagiário", Efeito = "Conserto em 15s", Cargo = 0, NivelMaximo = 1, CustoBase = 2500, FatorCusto = 1 },

            new MelhoriaDef { Id = Rack, Nome = "Rack 42U", Efeito = "5 vagas 1U", Cargo = 1, NivelMaximo = 1, CustoBase = 8000, FatorCusto = 1 },
            new MelhoriaDef { Id = Servidor1U, Nome = "Servidor 1U", Efeito = "+9/s, 0.4 kW", Cargo = 1, Requisito = Rack, NivelMaximo = VagasNoRack, CustoBase = 7000, FatorCusto = 1.5 },
            new MelhoriaDef { Id = NoBreak, Nome = "No-break", Efeito = "+1.5 kW", Cargo = 1, NivelMaximo = 3, CustoBase = 6000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = ArCondicionado, Nome = "Ar-cond.", Efeito = "-6 graus", Cargo = 1, NivelMaximo = 3, CustoBase = 5000, FatorCusto = 2.5 },

            new MelhoriaDef { Id = RackCheio, Nome = "Rack cheio", Efeito = "+72/s, 1.6 kW, 160 Mb", Cargo = 2, NivelMaximo = 4, CustoBase = 90000, FatorCusto = 2 },
            new MelhoriaDef { Id = Storage, Nome = "Storage", Efeito = "RAID: receita +25%", Cargo = 2, NivelMaximo = 3, CustoBase = 150000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Backup, Nome = "Backup fita", Efeito = "Salva os dados", Cargo = 2, Requisito = Storage, NivelMaximo = 1, CustoBase = 100000, FatorCusto = 1 },
            new MelhoriaDef { Id = Link, Nome = "Link fibra", Efeito = "+300 Mbps", Cargo = 2, NivelMaximo = 2, CustoBase = 50000, FatorCusto = 3 },

            new MelhoriaDef { Id = Hypervisor, Nome = "Hypervisor", Efeito = "VMs: servidores +40%", Cargo = 3, NivelMaximo = 3, CustoBase = 1200000, FatorCusto = 2.5 },
            new MelhoriaDef { Id = Containers, Nome = "Containers", Efeito = "+250/s em apps, 1 kW, 120 Mb", Cargo = 3, NivelMaximo = 4, CustoBase = 800000, FatorCusto = 1.8 },
            new MelhoriaDef { Id = ServidorCi, Nome = "Servidor CI", Efeito = "Deploy contínuo: apps +50%", Cargo = 3, Requisito = Containers, NivelMaximo = 1, CustoBase = 1500000, FatorCusto = 1 },
            new MelhoriaDef { Id = Link10G, Nome = "Link 10G", Efeito = "+1500 Mbps", Cargo = 3, NivelMaximo = 1, CustoBase = 700000, FatorCusto = 1 },

            new MelhoriaDef { Id = NoKubernetes, Nome = "Nó K8s", Efeito = "+400/s, 0.8 kW, 150 Mb", Cargo = 4, NivelMaximo = 6, CustoBase = 4000000, FatorCusto = 1.6 },
            new MelhoriaDef { Id = Balanceador, Nome = "Balanceador", Efeito = "K8s +25%, +10 s p/ escalar", Cargo = 4, Requisito = NoKubernetes, NivelMaximo = 1, CustoBase = 6000000, FatorCusto = 1 },
            new MelhoriaDef { Id = Observabilidade, Nome = "Observab.", Efeito = "Observabilidade: +15% (SLA)", Cargo = 4, NivelMaximo = 3, CustoBase = 5000000, FatorCusto = 2.5 },
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

        public static readonly IReadOnlyList<AutomacaoDef> Automacoes = new[]
        {
            new AutomacaoDef { Id = Watchdog, Nome = "Watchdog", Descricao = "Reinicia servidor travado em 5 s", Custo = 150000, Segundos = 180 },
            new AutomacaoDef { Id = HotSpare, Nome = "Troca de disco", Descricao = "Disco reserva entra sozinho em 5 s", Requisito = Storage, Custo = 250000, Segundos = 300 },
            new AutomacaoDef { Id = Monitoramento, Nome = "Monitoramento", Descricao = "Alerta antes da falha: metade dos incidentes", Custo = 400000, Segundos = 480 },
            new AutomacaoDef { Id = CronFaturamento, Nome = "Cron de faturamento", Descricao = "Receita offline sobe para 75%", Custo = 300000, Segundos = 360 },
            new AutomacaoDef { Id = Plantao, Nome = "Plantão 24h", Descricao = "Offline a 100%, até 24 h", Requisito = null, Custo = 600000, Segundos = 600 },
            new AutomacaoDef { Id = Pipeline, Nome = "Pipeline com testes", Descricao = "4x menos deploys quebrados", Cargo = 3, Requisito = ServidorCi, Custo = 1000000, Segundos = 600 },
            new AutomacaoDef { Id = RollbackAutomatico, Nome = "Rollback automático", Descricao = "Deploy quebrado volta em 5 s", Cargo = 3, Requisito = Containers, Custo = 800000, Segundos = 480 },
            new AutomacaoDef { Id = InfraComoCodigo, Nome = "Infra como código", Descricao = "Melhorias 15% mais baratas", Cargo = 3, Custo = 1500000, Segundos = 720 },
            new AutomacaoDef { Id = Autoscaling, Nome = "Autoscaling", Descricao = "Escala o cluster sozinho nos picos", Cargo = 4, Requisito = NoKubernetes, Custo = 8000000, Segundos = 900 },
            new AutomacaoDef { Id = Chaos, Nome = "Chaos engineering", Descricao = "Falhas testadas antes: metade dos incidentes", Cargo = 4, Custo = 6000000, Segundos = 720 },
            new AutomacaoDef { Id = Runbooks, Nome = "Runbooks automáticos", Descricao = "Todo conserto automático em 3 s", Cargo = 4, Custo = 10000000, Segundos = 1080 },
        };

        public static AutomacaoDef BuscarAutomacao(string id)
        {
            foreach (var a in Automacoes) if (a.Id == id) return a;
            throw new ArgumentException("Automação desconhecida: " + id);
        }

        public static MelhoriaDef Buscar(string id)
        {
            foreach (var m in Melhorias) if (m.Id == id) return m;
            throw new ArgumentException("Melhoria desconhecida: " + id);
        }
    }
}
