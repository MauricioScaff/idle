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

    public enum TipoMeta { Servidores, TotalGanho, IncidentesResolvidos }

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

        // --- Receita ---
        public const double ReceitaBaseServidor = 1;   // servidor torre sem melhorias (R$/s)
        public const double BonusSsd = 1.0;            // +100%
        public const double BonusVentoinha = 0.5;      // +50% por ventoinha
        public const double ReceitaServidor1U = 9;     // servidor de rack (R$/s)
        public const int VagasNoRack = 5;              // o rack da arte tem 5 unidades

        /// <summary>Clique no servidor vale isto + ValorCliqueReceita × receita por segundo.</summary>
        public const double ValorCliqueBase = 2;
        public const double ValorCliqueReceita = 2;

        // --- Energia (kW) ---
        public const double ConsumoServidorTorre = 0.4;
        public const double ConsumoServidor1U = 0.4;
        public const double CapacidadeBaseKw = 1.5;
        public const double CapacidadePorNoBreak = 1.5;

        // --- Temperatura (°C) ---
        public const double TemperaturaAmbiente = 22;
        public const double GrausPorKw = 5;
        public const double GrausPorArCondicionado = 6;
        public const double TemperaturaQuente = 32;    // acima: desempenho cai e travam mais
        public const double TemperaturaCritica = 40;

        // --- Incidentes ---
        public const double MtbfServidorTorre = 300;   // segundos, em média, entre travadas
        public const double MtbfServidor1U = 900;
        public const double MultiplicadorQuente = 3;
        public const double TempoConsertoTecnico = 30; // o técnico resolve sozinho depois disso
        public const double TempoConsertoComEstagiario = 15;

        // --- Progresso offline ---
        public const double TaxaOffline = 0.5;
        public const double HorasMaximasOffline = 12;
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
            new CargoDef { Nome = "Sysadmin", Lugar = "Salinha", MetasParaPromocao = new MetaDef[0] },
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
        };

        public static MelhoriaDef Buscar(string id)
        {
            foreach (var m in Melhorias) if (m.Id == id) return m;
            throw new ArgumentException("Melhoria desconhecida: " + id);
        }
    }
}
