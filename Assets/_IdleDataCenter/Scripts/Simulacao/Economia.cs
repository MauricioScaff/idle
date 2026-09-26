using System;

namespace IdleDataCenter.Simulacao
{
    /// <summary>
    /// Regras da economia: receita, cliques, compras e progresso offline.
    /// C# puro (sem Unity), para ser testável e para o mesmo código rodar online e offline.
    /// </summary>
    public class Economia
    {
        public EstadoJogo Estado { get; }

        /// <summary>Disparado depois de cada compra, com o id da melhoria (para atualizar o visual).</summary>
        public event Action<string> Comprou;

        public Economia(EstadoJogo estado) => Estado = estado ?? new EstadoJogo();

        public double Dinheiro => Estado.dinheiro;
        public int Servidores => 1 + Nivel(Catalogo.Servidor);
        public bool TemSsd => Nivel(Catalogo.Ssd) > 0;
        public int Ventoinhas => Nivel(Catalogo.Ventoinha);

        public double ReceitaPorServidor =>
            Catalogo.ReceitaBaseServidor
            * (1 + (TemSsd ? Catalogo.BonusSsd : 0))
            * (1 + Ventoinhas * Catalogo.BonusVentoinha);

        public double ReceitaPorSegundo => Servidores * ReceitaPorServidor;
        public double ValorClique => Catalogo.ValorCliqueBase + Catalogo.ValorCliqueReceita * ReceitaPorSegundo;

        public int Nivel(string id)
        {
            foreach (var m in Estado.melhorias) if (m.id == id) return m.nivel;
            return 0;
        }

        public bool NoMaximo(string id) => Nivel(id) >= Catalogo.Buscar(id).NivelMaximo;
        public double Custo(string id) => Catalogo.Buscar(id).Custo(Nivel(id));
        public bool PodeComprar(string id) => !NoMaximo(id) && Estado.dinheiro >= Custo(id);

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

        public void Ganhar(double valor)
        {
            Estado.dinheiro += valor;
            Estado.totalGanho += valor;
        }

        /// <summary>Avança o tempo com o jogo aberto.</summary>
        public void Avancar(double segundos) => Ganhar(ReceitaPorSegundo * segundos);

        /// <summary>Clique no servidor. Retorna quanto rendeu.</summary>
        public double Clicar()
        {
            Estado.jaClicouNoServidor = true;
            double valor = ValorClique;
            Ganhar(valor);
            return valor;
        }

        /// <summary>Quanto o jogador ganharia por ficar fora esse tempo (com taxa reduzida e limite de horas).</summary>
        public double CalcularGanhoOffline(double segundosFora)
        {
            if (segundosFora < Catalogo.SegundosMinimosOffline) return 0;
            double segundos = Math.Min(segundosFora, Catalogo.HorasMaximasOffline * 3600);
            return ReceitaPorSegundo * segundos * Catalogo.TaxaOffline;
        }

        /// <summary>Aplica o progresso offline desde o último save. Retorna o valor ganho.</summary>
        public double AplicarOffline(long agoraUnix)
        {
            if (Estado.ultimoSalvamentoUnix <= 0) return 0; // primeiro jogo
            double ganho = CalcularGanhoOffline(agoraUnix - Estado.ultimoSalvamentoUnix);
            Ganhar(ganho);
            return ganho;
        }
    }
}
