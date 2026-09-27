using System;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter.Isometrico
{
    [Serializable]
    public class CentroDadosEstado
    {
        public int versao = 1;
        public EstadoJogo economia;
        public bool metaQuatroRacks;
        public bool metaCincoRacks;
    }

    /// <summary>Campanha isométrica: usa as regras existentes com uma instalação inicial própria.</summary>
    public sealed class CentroDadosSimulacao
    {
        public CentroDadosEstado Estado { get; }
        public Economia Economia { get; }
        public int Racks => (Economia.TemRack ? 1 : 0) + Economia.RacksCheios;
        public int Engenheiros => 4 + (Economia.TemEstagiario ? 1 : 0);
        public bool DroneAtivo => Economia.TemAutomacao(Catalogo.Watchdog);
        public int Nivel => 1 + Racks + Economia.AutomacoesAtivas + Economia.NivelHypervisor;
        public int AlvoRacks => Estado.metaQuatroRacks ? 5 : 4;
        public bool MetasConcluidas => Estado.metaCincoRacks;

        public CentroDadosSimulacao(CentroDadosEstado estado = null, Random random = null)
        {
            Estado = estado ?? NovoEstado();
            if (Estado.economia == null) throw new ArgumentException("Campanha sem economia.");
            Economia = new Economia(Estado.economia, random);
        }

        public static CentroDadosEstado NovoEstado()
        {
            var e = new EstadoJogo { cargo = 3, dinheiro = 350000 };
            void Nivel(string id, int nivel) => e.melhorias.Add(new NivelMelhoria { id = id, nivel = nivel });
            Nivel(Catalogo.Ssd, 1);
            Nivel(Catalogo.Rack, 1);
            Nivel(Catalogo.Servidor1U, 5);
            Nivel(Catalogo.RackCheio, 1);
            Nivel(Catalogo.Storage, 1);
            Nivel(Catalogo.Link, 1);
            Nivel(Catalogo.NoBreak, 1);
            Nivel(Catalogo.ArCondicionado, 1);
            return new CentroDadosEstado { economia = e };
        }

        public bool Comprar(string id)
        {
            if (!Economia.Comprar(id)) return false;
            RecompensarMetas();
            return true;
        }

        public double RecompensarMetas()
        {
            double premio = 0;
            if (Racks >= 4 && !Estado.metaQuatroRacks) { Estado.metaQuatroRacks = true; premio += 50000; }
            if (Racks >= 5 && !Estado.metaCincoRacks) { Estado.metaCincoRacks = true; premio += 100000; }
            if (premio > 0) Economia.Ganhar(premio);
            return premio;
        }

        public void Avancar(double segundos)
        {
            if (double.IsNaN(segundos) || double.IsInfinity(segundos) || segundos <= 0) return;
            // As probabilidades de incidentes da economia pressupõem passos curtos.
            while (segundos > 0) { double passo = Math.Min(1, segundos); Economia.Avancar(passo); segundos -= passo; }
        }
    }
}
