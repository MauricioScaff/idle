using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Um evento aleatório: algo que acontece de vez em quando e às vezes pede uma decisão rápida.</summary>
    public class EventoDef
    {
        public string Id, Nome;
        public int CargoMinimo, CargoMaximo = 99;
        public double Duracao;   // segundos (na oferta do cliente grande: o tempo para aceitar)
    }

    public static partial class Catalogo
    {
        public const string EventoCliente = "cliente", EventoAuditoria = "auditoria", EventoInternet = "internet",
                            EventoBlackFriday = "blackfriday", EventoCafeAcabou = "cafeacabou";

        public static readonly IReadOnlyList<EventoDef> Eventos = new[]
        {
            new EventoDef { Id = EventoCliente, Nome = "Cliente grande", CargoMinimo = 1, Duracao = 20 },
            new EventoDef { Id = EventoAuditoria, Nome = "Auditoria", CargoMinimo = 2, Duracao = 60 },
            new EventoDef { Id = EventoInternet, Nome = "Internet caiu", CargoMaximo = 2, Duracao = 60 },
            new EventoDef { Id = EventoBlackFriday, Nome = "Black Friday", CargoMinimo = 3, Duracao = 120 },
            new EventoDef { Id = EventoCafeAcabou, Nome = "O café acabou", Duracao = 180 },
        };

        public static EventoDef BuscarEvento(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var e in Eventos) if (e.Id == id) return e;
            return null;
        }

        public const double PrimeiroEvento = 240, IntervaloEventoMin = 300, IntervaloEventoMax = 480;
        public const double DuracaoClienteAceito = 90, MultiplicadorCliente = 2, SegundosDeMultaDoCliente = 30;
        public const double SegundosDeBonusDaAuditoria = 60, SegundosDeMultaDaAuditoria = 30;
        public const double FatorInternetCaida = 0.5, FatorInternet4G = 0.8;
        public const int LinksQueEvitamAQuedaDaInternet = 2;   // com dois links de fibra a internet tem redundância
        public const double MultiplicadorBlackFriday = 1.5;
        public const double FatorConsertoSemCafe = 2, SegundosDoPrecoDoCafe = 10;
    }

    /// <summary>
    /// Eventos aleatórios, um de cada vez, a cada 5 a 8 minutos de jogo (só com o jogo aberto: o progresso offline não
    /// sorteia eventos):
    /// - Cliente grande (Sysadmin em diante): paga o dobro por 90 s se aceitar; se algo travar nesse tempo, cancela e multa.
    /// - Auditoria (Analista em diante): avisa 60 s antes; tudo funcionando no fim dá bônus, algo quebrado dá multa.
    /// - Internet caiu (até o Analista): metade da receita por 60 s; o 4G do celular segura 80%; dois links de fibra evitam.
    /// - Black Friday (DevOps em diante): receita ×1,5 por 2 minutos.
    /// - O café acabou: o técnico conserta na metade da velocidade até comprarem café.
    /// </summary>
    public partial class Economia
    {
        public event Action<EventoDef> EventoComecou;
        /// <summary>O evento acabou: (qual, valor ganho; negativo se foi multa).</summary>
        public event Action<EventoDef, double> EventoTerminou;

        public EventoDef Evento => Catalogo.BuscarEvento(Estado.evento);
        public bool TemEvento => Evento != null;
        public double SegundosDoEvento => Estado.eventoSegundos;
        /// <summary>0: começou (oferta do cliente, internet sem 4G); 1: aceito / 4G ligado.</summary>
        public int FaseDoEvento => Estado.eventoFase;

        /// <summary>Quanto o evento mexe na receita agora.</summary>
        public double FatorEvento
        {
            get
            {
                switch (Estado.evento)
                {
                    case Catalogo.EventoCliente: return Estado.eventoFase == 1 ? Catalogo.MultiplicadorCliente : 1;
                    case Catalogo.EventoInternet: return Estado.eventoFase == 1 ? Catalogo.FatorInternet4G : Catalogo.FatorInternetCaida;
                    case Catalogo.EventoBlackFriday: return Catalogo.MultiplicadorBlackFriday;
                    default: return 1;
                }
            }
        }

        /// <summary>Sem café, o técnico demora mais para consertar.</summary>
        public double FatorConsertoDoEvento => Estado.evento == Catalogo.EventoCafeAcabou ? Catalogo.FatorConsertoSemCafe : 1;

        public double PrecoDoCafe => Math.Max(10, ReceitaPorSegundo * Catalogo.SegundosDoPrecoDoCafe);

        /// <summary>O que o evento diz agora (muda com a fase e o tempo).</summary>
        public string TextoDoEvento
        {
            get
            {
                int s = (int)Math.Ceiling(Estado.eventoSegundos);
                switch (Estado.evento)
                {
                    case Catalogo.EventoCliente: return Estado.eventoFase == 0 ? "Paga o dobro por 90 s" : "Renda x2: " + s + " s, nada pode travar";
                    case Catalogo.EventoAuditoria: return "Em " + s + " s: tudo funcionando";
                    case Catalogo.EventoInternet: return Estado.eventoFase == 0 ? "Renda pela metade: " + s + " s" : "No 4G: " + s + " s";
                    case Catalogo.EventoBlackFriday: return "Renda x1,5: " + s + " s";
                    case Catalogo.EventoCafeAcabou: return "Técnico 2x mais lento";
                    default: return "";
                }
            }
        }

        /// <summary>O botão do evento agora (null quando não há o que fazer além de esperar).</summary>
        public string BotaoDoEvento
        {
            get
            {
                switch (Estado.evento)
                {
                    case Catalogo.EventoCliente: return Estado.eventoFase == 0 ? "Aceitar " + (int)Math.Ceiling(Estado.eventoSegundos) + "s" : null;
                    case Catalogo.EventoInternet: return Estado.eventoFase == 0 ? "Ligar o 4G" : null;
                    case Catalogo.EventoCafeAcabou: return "Café R$ " + Formatar(PrecoDoCafe);
                    default: return null;
                }
            }
        }

        static string Formatar(double v) =>
            v >= 1e9 ? (v / 1e9).ToString("0.#") + " bi" : v >= 1e6 ? (v / 1e6).ToString("0.#") + " mi" : v >= 1e3 ? (v / 1e3).ToString("0.#") + " mil" : v.ToString("0");

        void AvancarEvento(double segundos)
        {
            if (TemEvento)
            {
                Estado.eventoSegundos -= segundos;
                if (Estado.eventoSegundos <= 0) FimDoTempoDoEvento();
                return;
            }
            if (Estado.proximoEvento < 0) Estado.proximoEvento = Catalogo.PrimeiroEvento;
            Estado.proximoEvento -= segundos;
            if (Estado.proximoEvento <= 0) ComecarEvento();
        }

        /// <summary>Sorteia um evento que faça sentido no cargo atual e começa.</summary>
        public void ComecarEvento(string id = null)
        {
            if (TemEvento) return;
            var possiveis = new List<EventoDef>();
            foreach (var e in Catalogo.Eventos)
            {
                if (Cargo < e.CargoMinimo || Cargo > e.CargoMaximo) continue;
                if (e.Id == Catalogo.EventoInternet && Nivel(Catalogo.Link) >= Catalogo.LinksQueEvitamAQuedaDaInternet) continue;
                if (id == null || e.Id == id) possiveis.Add(e);
            }
            if (possiveis.Count == 0) { AgendarProximoEvento(); return; }
            var def = possiveis[sorteio.Next(possiveis.Count)];
            Estado.evento = def.Id;
            Estado.eventoFase = 0;
            Estado.eventoSegundos = def.Duracao;
            EventoComecou?.Invoke(def);
        }

        /// <summary>O botão do evento: aceita o cliente, liga o 4G ou compra o café. Retorna se fez algo.</summary>
        public bool AgirNoEvento()
        {
            switch (Estado.evento)
            {
                case Catalogo.EventoCliente when Estado.eventoFase == 0:
                    Estado.eventoFase = 1;
                    Estado.eventoSegundos = Catalogo.DuracaoClienteAceito;
                    return true;
                case Catalogo.EventoInternet when Estado.eventoFase == 0:
                    Estado.eventoFase = 1;
                    return true;
                case Catalogo.EventoCafeAcabou:
                    double preco = PrecoDoCafe;
                    if (Estado.dinheiro < preco) return false;
                    Estado.dinheiro -= preco;
                    EncerrarEvento(0);
                    return true;
                default:
                    return false;
            }
        }

        void FimDoTempoDoEvento()
        {
            switch (Estado.evento)
            {
                case Catalogo.EventoCliente:
                    // na oferta: o cliente foi embora; aceito: contrato cumprido (o dobro já entrou na receita)
                    EncerrarEvento(0);
                    break;
                case Catalogo.EventoAuditoria:
                    bool tudoOk = Estado.travamentos.Count == 0 && !DiscoQueimado && !DeployQuebrado && !TemQuedaDeEnergia && !TemPaneRegional;
                    double valor = ReceitaPorSegundo * (tudoOk ? Catalogo.SegundosDeBonusDaAuditoria : -Catalogo.SegundosDeMultaDaAuditoria);
                    EncerrarEvento(valor);
                    break;
                default:
                    EncerrarEvento(0);
                    break;
            }
        }

        /// <summary>Algo travou: com o cliente grande aceito, ele cancela e cobra multa.</summary>
        void QuebrouDuranteEvento()
        {
            if (Estado.evento == Catalogo.EventoCliente && Estado.eventoFase == 1)
                EncerrarEvento(-ReceitaPorSegundo / Catalogo.MultiplicadorCliente * Catalogo.SegundosDeMultaDoCliente);
        }

        void EncerrarEvento(double valor)
        {
            var def = Evento;
            if (valor > 0) Ganhar(valor);
            else if (valor < 0) { valor = -Math.Min(Estado.dinheiro, -valor); Estado.dinheiro += valor; }
            Estado.evento = "";
            Estado.eventoFase = 0;
            Estado.eventoSegundos = 0;
            AgendarProximoEvento();
            if (def != null) EventoTerminou?.Invoke(def, valor);
        }

        void AgendarProximoEvento() =>
            Estado.proximoEvento = Catalogo.IntervaloEventoMin + sorteio.NextDouble() * (Catalogo.IntervaloEventoMax - Catalogo.IntervaloEventoMin);
    }
}
