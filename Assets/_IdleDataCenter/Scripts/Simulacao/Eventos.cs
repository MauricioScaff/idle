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
        public bool Ataque;      // ataque: a linha de segurança pode bloquear antes de acontecer
    }

    public static partial class Catalogo
    {
        public const string EventoCliente = "cliente", EventoAuditoria = "auditoria", EventoInternet = "internet",
                            EventoBlackFriday = "blackfriday", EventoCafeAcabou = "cafeacabou",
                            AtaquePhishing = "phishing", AtaqueMalware = "malware", AtaqueDdos = "ddos", AtaqueRansomware = "ransomware",
                            EventoSsl = "ssl", EventoDns = "dns", EventoFaxineira = "faxineira", EventoRato = "rato", EventoDeploySexta = "deploysexta",
                            EventoReuniao = "reuniao";

        public static readonly IReadOnlyList<EventoDef> Eventos = new[]
        {
            new EventoDef { Id = EventoCliente, Nome = "Cliente grande", CargoMinimo = 1, Duracao = 20 },
            new EventoDef { Id = EventoAuditoria, Nome = "Auditoria", CargoMinimo = 2, Duracao = 60 },
            new EventoDef { Id = EventoInternet, Nome = "Internet caiu", CargoMaximo = 2, Duracao = 60 },
            new EventoDef { Id = EventoBlackFriday, Nome = "Viralizou", CargoMinimo = 3, Duracao = 120 },   // (o pico "Black Friday" é do SRE)
            new EventoDef { Id = EventoCafeAcabou, Nome = "O café acabou", Duracao = 180 },
            new EventoDef { Id = AtaquePhishing, Nome = "Phishing", Duracao = 1, Ataque = true },
            new EventoDef { Id = AtaqueMalware, Nome = "Malware", CargoMaximo = 3, Duracao = 90, Ataque = true },
            new EventoDef { Id = AtaqueRansomware, Nome = "Ransomware", CargoMinimo = 2, Duracao = 180, Ataque = true },
            new EventoDef { Id = AtaqueDdos, Nome = "DDoS", CargoMinimo = 4, Duracao = 90, Ataque = true },
            new EventoDef { Id = EventoSsl, Nome = "SSL expirou", CargoMinimo = 1, Duracao = 120 },
            new EventoDef { Id = EventoDns, Nome = "É sempre o DNS", CargoMinimo = 1, Duracao = 90 },
            new EventoDef { Id = EventoFaxineira, Nome = "Rack desligado", CargoMinimo = 1, CargoMaximo = 3, Duracao = 90 },
            new EventoDef { Id = EventoRato, Nome = "Rato no cabo", CargoMaximo = 2, Duracao = 1 },
            new EventoDef { Id = EventoDeploySexta, Nome = "Deploy na sexta", CargoMinimo = 3, Duracao = 20 },
            new EventoDef { Id = EventoReuniao, Nome = "Reunião", CargoMinimo = 3, Duracao = 120 },
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
        public const int ServidoresDoPhishing = 2;
        public const double FatorMalware = 0.7, FatorDdos = 0.5, FatorDdosBloqueado = 0.85, FatorRansomware = 0.2;
        public const double SegundosDoResgate = 300;   // o resgate custa 5 minutos de receita (sem o ataque)
        public const int BackupQueRestauraRansomware = 3;   // fita (offline): restaura na hora
        public const double FatorSslExpirado = 0.6, FatorDnsFora = 0.5, FatorRackDesligado = 0.7;
        public const double ChanceDoDeploySextaQuebrar = 0.5, SegundosDeBonusDoDeploySexta = 60;
        public static readonly string[] EventosQueORunbookResolve = { EventoSsl, EventoDns, EventoFaxineira, AtaqueMalware, EventoReuniao };
        public const double SegundosDoRunbookNoEvento = 5;
    }

    /// <summary>
    /// Eventos aleatórios, um de cada vez, a cada 5 a 8 minutos de jogo (só com o jogo aberto: o progresso offline não
    /// sorteia eventos):
    /// - Cliente grande (Sysadmin em diante): paga o dobro por 90 s se aceitar; se algo travar nesse tempo, cancela e multa.
    /// - Auditoria (Analista em diante): avisa 60 s antes; tudo funcionando no fim dá bônus, algo quebrado dá multa.
    /// - Internet caiu (até o Analista): metade da receita por 60 s; o 4G do celular segura 80%; dois links de fibra evitam.
    /// - Black Friday (DevOps em diante): receita ×1,5 por 2 minutos.
    /// - O café acabou: o técnico conserta na metade da velocidade até comprarem café.
    /// Ataques (a linha de segurança bloqueia metade dos que ainda passavam a cada nível):
    /// - Phishing: alguém clicou no link e dois servidores travam.
    /// - Malware (até o DevOps): receita a 70% até clicarem em limpar.
    /// - Ransomware (Analista em diante): receita a 20%; com backup até a fita, restaura na hora; senão paga o resgate ou espera.
    /// - DDoS (SRE em diante): metade da receita; bloquear os IPs segura 85%.
    /// </summary>
    public partial class Economia
    {
        public event Action<EventoDef> EventoComecou;
        /// <summary>O evento acabou: (qual, valor ganho; negativo se foi multa).</summary>
        public event Action<EventoDef, double> EventoTerminou;
        /// <summary>Um ataque foi bloqueado antes de acontecer: (qual, o nome do que bloqueou).</summary>
        public event Action<EventoDef, string> AtaqueBloqueado;

        /// <summary>Quantos níveis da linha de segurança já foram comprados.</summary>
        public int NivelSeguranca { get { int n = 0; foreach (var id in Catalogo.LinhaDeSeguranca) if (Nivel(id) > 0) n++; return n; } }
        /// <summary>Chance de um ataque ser bloqueado: metade a cada nível.</summary>
        public double ProtecaoSeguranca => 1 - Math.Pow(0.5, NivelSeguranca);
        /// <summary>Com backup até a fita (offline), o ransomware se resolve restaurando.</summary>
        public bool RestauraRansomware => NivelBackup >= Catalogo.BackupQueRestauraRansomware;
        public double PrecoDoResgate => ReceitaPorSegundo / Catalogo.FatorRansomware * Catalogo.SegundosDoResgate;

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
                    case Catalogo.AtaqueMalware: return Catalogo.FatorMalware;
                    case Catalogo.AtaqueDdos: return Estado.eventoFase == 1 ? Catalogo.FatorDdosBloqueado : Catalogo.FatorDdos;
                    case Catalogo.AtaqueRansomware: return Catalogo.FatorRansomware;
                    case Catalogo.EventoSsl: return Catalogo.FatorSslExpirado;
                    case Catalogo.EventoDns: return Catalogo.FatorDnsFora;
                    case Catalogo.EventoFaxineira: return Catalogo.FatorRackDesligado;
                    default: return 1;
                }
            }
        }

        /// <summary>Sem café, o técnico demora mais para consertar.</summary>
        public double FatorConsertoDoEvento => Estado.evento == Catalogo.EventoCafeAcabou || Estado.evento == Catalogo.EventoReuniao ? Catalogo.FatorConsertoSemCafe : 1;

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
                    case Catalogo.AtaquePhishing: return "Clicaram no link: servidores travaram";
                    case Catalogo.AtaqueMalware: return "Renda a 70% até limpar";
                    case Catalogo.AtaqueDdos: return Estado.eventoFase == 0 ? "Tráfego falso: renda pela metade" : "IPs bloqueados: " + s + " s";
                    case Catalogo.AtaqueRansomware: return "Dados criptografados: " + s + " s";
                    case Catalogo.EventoSsl: return "Cadeado vermelho: renda a 60%";
                    case Catalogo.EventoDns: return "Ninguém acha o site";
                    case Catalogo.EventoFaxineira: return "Precisava ligar o aspirador";
                    case Catalogo.EventoRato: return "Um servidor ficou sem rede";
                    case Catalogo.EventoDeploySexta: return "Sexta, 18h. Vai arriscar?";
                    case Catalogo.EventoReuniao: return "Time consertando 2x mais devagar";
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
                    case Catalogo.AtaqueMalware: return "Limpar";
                    case Catalogo.AtaqueDdos: return Estado.eventoFase == 0 ? "Bloquear IPs" : null;
                    case Catalogo.AtaqueRansomware: return RestauraRansomware ? "Restaurar backup" : "Resgate R$ " + Formatar(PrecoDoResgate);
                    case Catalogo.EventoSsl: return "Renovar";
                    case Catalogo.EventoDns: return "Reiniciar o DNS";
                    case Catalogo.EventoFaxineira: return "Religar na tomada";
                    case Catalogo.EventoDeploySexta: return "Fazer o deploy";
                    case Catalogo.EventoReuniao: return "Podia ser um e-mail";
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
                // com runbooks automáticos, os problemas de rotina (SSL, DNS, rack, malware, reunião) se resolvem sozinhos
                else if (TemAutomacao(Catalogo.Runbooks) && Evento.Duracao - Estado.eventoSegundos >= Catalogo.SegundosDoRunbookNoEvento
                         && Array.IndexOf(Catalogo.EventosQueORunbookResolve, Estado.evento) >= 0)
                    EncerrarEvento(0);
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
            if (def.Ataque && sorteio.NextDouble() < ProtecaoSeguranca)
            {
                // bloqueado: avisa quem bloqueou (o item de segurança mais alto)
                string quem = "A segurança";
                foreach (var idSeg in Catalogo.LinhaDeSeguranca) if (Nivel(idSeg) > 0) quem = Catalogo.Buscar(idSeg).Nome;
                Conquistar(Catalogo.ConquistaBloqueou);
                AgendarProximoEvento();
                AtaqueBloqueado?.Invoke(def, quem);
                return;
            }
            Estado.evento = def.Id;
            Estado.eventoFase = 0;
            // os eventos de humor de TI viram conquista na primeira vez
            if (def.Id == Catalogo.EventoDns || def.Id == Catalogo.EventoFaxineira || def.Id == Catalogo.EventoRato || def.Id == Catalogo.EventoReuniao || def.Id == Catalogo.EventoSsl) Conquistar(def.Id);
            Estado.eventoSegundos = def.Duracao;
            EventoComecou?.Invoke(def);
            if (def.Id == Catalogo.AtaquePhishing || def.Id == Catalogo.EventoRato)
            {
                // trava os primeiros servidores que estiverem de pé (os que podem travar)
                int travados = 0;
                for (int s = 0; s < TotalServidores && travados < (def.Id == Catalogo.EventoRato ? 1 : Catalogo.ServidoresDoPhishing); s++)
                    if (!Travado(s) && (EhTorre(s) ? s : s - Torres) < Catalogo.ServidoresQueTravam) { Travar(s); travados++; }
            }
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
                case Catalogo.AtaqueMalware: case Catalogo.EventoSsl: case Catalogo.EventoDns: case Catalogo.EventoFaxineira: case Catalogo.EventoReuniao:
                    EncerrarEvento(0);
                    return true;
                case Catalogo.EventoDeploySexta:
                    // a aposta: às vezes dá certo e paga, às vezes quebra o deploy no fim de semana
                    if (sorteio.NextDouble() < Catalogo.ChanceDoDeploySextaQuebrar) { if (HostsContainers > 0) QuebrarDeploy(); EncerrarEvento(0); }
                    else { Conquistar(Catalogo.ConquistaDeploySexta); EncerrarEvento(ReceitaPorSegundo * Catalogo.SegundosDeBonusDoDeploySexta); }
                    return true;
                case Catalogo.AtaqueDdos when Estado.eventoFase == 0:
                    Estado.eventoFase = 1;
                    return true;
                case Catalogo.AtaqueRansomware:
                    if (RestauraRansomware) { Estado.backupsRestaurados++; Conquistar(Catalogo.ConquistaRansomware); EncerrarEvento(0); return true; }
                    double resgate = PrecoDoResgate;
                    if (Estado.dinheiro < resgate) return false;
                    EncerrarEvento(-resgate);   // EncerrarEvento desconta o valor negativo
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
