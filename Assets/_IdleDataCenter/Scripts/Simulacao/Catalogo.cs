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
        public int NivelMaximo;
        public double CustoBase;
        public double FatorCusto;   // custo do nível n = CustoBase × FatorCusto^n

        public double Custo(int nivelAtual) => Math.Round(CustoBase * Math.Pow(FatorCusto, nivelAtual));
    }

    /// <summary>
    /// Números de balanceamento. Tudo que mexe no ritmo do jogo fica aqui, num lugar só.
    /// </summary>
    public static class Catalogo
    {
        public const string Ssd = "ssd";
        public const string Ventoinha = "ventoinha";
        public const string Servidor = "servidor";

        /// <summary>Receita de um servidor velho, sem melhorias (R$/s).</summary>
        public const double ReceitaBaseServidor = 1;
        public const double BonusSsd = 1.0;          // +100%
        public const double BonusVentoinha = 0.5;    // +50% por ventoinha

        /// <summary>Clique no servidor vale isto + ValorCliqueReceita × receita por segundo.</summary>
        public const double ValorCliqueBase = 2;
        public const double ValorCliqueReceita = 2;

        /// <summary>Progresso offline: fração da receita normal e limite de horas acumuladas.</summary>
        public const double TaxaOffline = 0.5;
        public const double HorasMaximasOffline = 12;
        public const double SegundosMinimosOffline = 60;

        public static readonly IReadOnlyList<MelhoriaDef> Melhorias = new[]
        {
            new MelhoriaDef { Id = Ssd, Nome = "SSD", Efeito = "Receita x2", NivelMaximo = 1, CustoBase = 25, FatorCusto = 1 },
            new MelhoriaDef { Id = Ventoinha, Nome = "Ventoinha", Efeito = "+50% receita", NivelMaximo = 3, CustoBase = 60, FatorCusto = 2.2 },
            new MelhoriaDef { Id = Servidor, Nome = "Servidor", Efeito = "+1 servidor", NivelMaximo = 2, CustoBase = 250, FatorCusto = 3 },
        };

        public static MelhoriaDef Buscar(string id)
        {
            foreach (var m in Melhorias) if (m.Id == id) return m;
            throw new ArgumentException("Melhoria desconhecida: " + id);
        }
    }
}
