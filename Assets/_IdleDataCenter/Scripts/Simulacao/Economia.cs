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

        /// <summary>Quantos servidores existem de verdade (os racks cheios contam cada servidor).</summary>
        public int ContagemServidores => TotalServidores + RacksCheios * Catalogo.ServidoresPorRackCheio;

        /// <summary>Quanto tempo o técnico leva para consertar sozinho (com estagiário, metade).</summary>
        public double TempoConserto => TemEstagiario ? Catalogo.TempoConsertoComEstagiario : Catalogo.TempoConsertoTecnico;

        public double ReceitaTorre =>
            Catalogo.ReceitaBaseServidor
            * (1 + (TemSsd ? Catalogo.BonusSsd : 0))
            * (1 + Ventoinhas * Catalogo.BonusVentoinha);

        // Energia
        public double ConsumoKw => Torres * Catalogo.ConsumoServidorTorre + ServidoresRack * Catalogo.ConsumoServidor1U
                                 + RacksCheios * Catalogo.ConsumoRackCheio + NivelStorage * Catalogo.ConsumoStorage;
        public double CapacidadeKw => Catalogo.CapacidadeBaseKw + Nivel(Catalogo.NoBreak) * Catalogo.CapacidadePorNoBreak
                                    + (NaSalaDeRacks ? Catalogo.CapacidadeSalaDeRacksKw : 0);
        public bool Sobrecarga => ConsumoKw > CapacidadeKw + 1e-9;
        /// <summary>Com sobrecarga, a receita cai na proporção da energia que falta.</summary>
        public double FatorEnergia => Sobrecarga ? CapacidadeKw / ConsumoKw : 1;

        // Temperatura
        public double Temperatura =>
            Catalogo.TemperaturaAmbiente + ConsumoKw * Catalogo.GrausPorKw * (NaSalaDeRacks ? Catalogo.FatorCalorSalaDeRacks : 1)
            - Nivel(Catalogo.ArCondicionado) * Catalogo.GrausPorArCondicionado
            - (NaSalaDeRacks ? Catalogo.GrausArDePrecisao : 0);
        public bool Quente => Temperatura > Catalogo.TemperaturaQuente;
        public double FatorTemperatura =>
            Temperatura > Catalogo.TemperaturaCritica ? 0.3 : Quente ? 0.6 : 1;

        // Banda: com o link saturado, todo mundo fica lento e a receita cai na proporção
        public double TrafegoMbps => Torres * Catalogo.TrafegoTorre + ServidoresRack * Catalogo.TrafegoServidor1U
                                   + RacksCheios * Catalogo.TrafegoRackCheio;
        public double BandaMbps => Catalogo.BandaBase + Nivel(Catalogo.Link) * Catalogo.BandaPorLink;
        public bool LinkSaturado => TrafegoMbps > BandaMbps + 1e-9;
        public double FatorBanda => LinkSaturado ? BandaMbps / TrafegoMbps : 1;

        /// <summary>Storage vende banco de dados gerenciado; com um disco queimado o RAID fica degradado e esse bônus some.</summary>
        public double FatorStorage => DiscoQueimado ? 1 : 1 + NivelStorage * Catalogo.BonusStorage;

        /// <summary>Tudo que multiplica a receita de todos os servidores.</summary>
        public double FatorGeral => FatorEnergia * FatorTemperatura * FatorBanda * FatorStorage;

        public bool Travado(int servidor) => Estado.travamentos.Exists(t => t.servidor == servidor);
        public IReadOnlyList<Travamento> Travamentos => Estado.travamentos;

        /// <summary>Receita "de placa" de um servidor, sem os fatores de energia e temperatura.</summary>
        public double ReceitaBruta(int servidor) => EhTorre(servidor) ? ReceitaTorre : Catalogo.ReceitaServidor1U;

        public double ReceitaDoServidor(int servidor) =>
            Travado(servidor) ? 0 : ReceitaBruta(servidor) * FatorGeral;

        public double ReceitaDosRacksCheios => RacksCheios * Catalogo.ReceitaRackCheio * FatorGeral;

        public double ReceitaPorSegundo
        {
            get
            {
                double soma = 0;
                for (int i = 0; i < TotalServidores; i++) soma += ReceitaDoServidor(i);
                return soma + ReceitaDosRacksCheios;
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
        public double Custo(string id) => Catalogo.Buscar(id).Custo(Nivel(id));
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

        /// <summary>Avança o tempo com o jogo aberto: rende, conserta com o técnico e sorteia travadas.</summary>
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

            // Novas travadas: cada servidor tem uma chance por segundo (maior se estiver quente)
            double mult = Quente ? Catalogo.MultiplicadorQuente : 1;
            for (int s = 0; s < TotalServidores; s++)
            {
                if (Travado(s)) continue;
                double mtbf = EhTorre(s) ? Catalogo.MtbfServidorTorre : Catalogo.MtbfServidor1U;
                if (sorteio.NextDouble() < segundos * mult / mtbf) Travar(s);
            }

            // Storage: um disco queimado de cada vez; o técnico troca sozinho no mesmo tempo de um conserto
            if (DiscoQueimado)
            {
                Estado.discoSegundos += segundos;
                if (Estado.discoSegundos >= TempoConserto) TrocarDisco(porTecnico: true);
            }
            else if (NivelStorage > 0 && sorteio.NextDouble() < segundos * mult * NivelStorage / Catalogo.MtbfDisco)
                QueimarDisco();
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

        void Resolver(int servidor, bool porTecnico)
        {
            if (Estado.travamentos.RemoveAll(t => t.servidor == servidor) == 0) return;
            Estado.incidentesResolvidos++;
            Voltou?.Invoke(servidor, porTecnico);
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

        /// <summary>Quanto o jogador ganharia por ficar fora esse tempo (com taxa reduzida e limite de horas).</summary>
        public double CalcularGanhoOffline(double segundosFora)
        {
            if (segundosFora < Catalogo.SegundosMinimosOffline) return 0;
            double segundos = Math.Min(segundosFora, Catalogo.HorasMaximasOffline * 3600);
            return ReceitaPorSegundo * segundos * Catalogo.TaxaOffline;
        }

        /// <summary>Resumo da última volta: quanto tempo ficou fora (s), quanto rendeu e quantos servidores o técnico consertou.</summary>
        public double SegundosFora { get; private set; }
        public double GanhoFora { get; private set; }
        public int ConsertadosFora { get; private set; }
        /// <summary>Ficou fora mais que o limite de horas (o que passou disso não rendeu).</summary>
        public bool PassouDoLimite => SegundosFora > Catalogo.HorasMaximasOffline * 3600;

        /// <summary>
        /// Aplica o progresso offline desde o último save. Enquanto você estava fora, o técnico
        /// consertou o que tinha travado. Retorna o valor ganho.
        /// </summary>
        public double AplicarOffline(long agoraUnix)
        {
            if (Estado.ultimoSalvamentoUnix <= 0) return 0; // primeiro jogo
            double fora = agoraUnix - Estado.ultimoSalvamentoUnix;
            SegundosFora = fora;
            if (fora >= Catalogo.SegundosMinimosOffline)
            {
                ConsertadosFora = Estado.travamentos.Count;
                Estado.incidentesResolvidos += ConsertadosFora;
                Estado.travamentos.Clear();
                if (DiscoQueimado) { TrocarDisco(porTecnico: true); ConsertadosFora++; }
            }
            double ganho = CalcularGanhoOffline(fora);
            GanhoFora = ganho;
            Ganhar(ganho);
            return ganho;
        }
    }
}
