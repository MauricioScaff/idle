using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>
    /// Um cliente do data center (ou a proposta de um, enquanto não foi aceito; nome vazio: nenhuma). O site dele roda num
    /// dos seus servidores: cresce de porte com a satisfação e cai junto quando algo quebra.
    /// </summary>
    [Serializable]
    public class Cliente
    {
        public string nome = "";
        public double sla;                  // o uptime que ele exige
        public int porte = 1;               // 1 (página) a 5 (gigante)
        public double satisfacao = Catalogo.SatisfacaoInicial;
        public double crescimento;          // 0 a 1 até o próximo porte
        public int servidor;                // onde o site roda (se esse servidor travar, o site cai)
        public double restante;             // proposta: segundos para aceitar
        public double foraDoAr;             // há quanto tempo o site está fora (0 = no ar)

        // pedido em andamento (vazio = nenhum): o tipo, o alvo, a melhoria que resolve e o prazo
        public string pedido = "";
        public double pedidoAlvo, pedidoRestante;
        public string pedidoItem = "";
        public double proximoPedido = -1;
    }

    /// <summary>Contrato dos saves antigos: vira cliente na primeira vez que o jogo roda (ver MigrarContratos).</summary>
    [Serializable]
    public class Contrato
    {
        public string cliente = "";
        public double sla, bonus, duracao, restante;
    }

    public class TipoDeCliente
    {
        public string Nome, Dominio, Cor;   // cor da marca (hex), usada no site
    }

    public static partial class Catalogo
    {
        // --- Clientes (Sysadmin em diante) ---
        public const int CargoDosClientes = 1;
        public static int CapacidadeDeClientes(int cargo) => 2 + cargo - Catalogo.CargoTecnico;
        public const double PrimeiraProposta = 300, IntervaloPropostaMin = 300, IntervaloPropostaMax = 540;
        public const double TempoParaAceitar = 90;
        /// <summary>Os SLAs que um cliente pode pedir (o mais alto que o uptime de agora entrega, às vezes um abaixo).</summary>
        public static readonly double[] SlasDosClientes = { 0.98, 0.99, 0.995, 0.999, 0.9999 };

        public const double SatisfacaoInicial = 70, SatisfacaoParaCrescer = 80;
        public const double SatisfacaoSobePorSegundo = 0.4;         // site no ar e uptime dentro do SLA
        public const double SatisfacaoCaiForaDoAr = 1.2;            // por segundo com o site fora (mais, quanto maior o SLA)
        public const double SatisfacaoCaiAbaixoDoSla = 0.15;        // por segundo com o uptime abaixo do SLA
        public static readonly string[] PortesDoSite = { "", "Página", "Loja virtual", "E-commerce", "Plataforma", "Gigante" };
        public const int PorteMaximo = 5;
        /// <summary>Renda a mais por cliente, pelo porte (com a satisfação cheia; pela metade com ela zerada).</summary>
        public static readonly double[] BonusPorPorte = { 0, 0.015, 0.03, 0.05, 0.075, 0.11 };
        /// <summary>Minutos com o cliente satisfeito para subir do porte p para o p+1.</summary>
        public static readonly double[] MinutosParaCrescer = { 0, 8, 15, 30, 60 };

        // pedidos: de vez em quando um cliente pede algo com prazo
        public const double PrimeiroPedidoMin = 240, IntervaloPedidoMin = 420, IntervaloPedidoMax = 780;
        public const double PrazoDoPedido = 480;
        public const double SegundosDoPremioDoPedido = 30, SatisfacaoDoPedido = 20, CrescimentoDoPedido = 0.4, SatisfacaoDoPedidoPerdido = 25;
        public const string PedidoServidores = "servidores", PedidoBackup = "backup", PedidoSeguranca = "seguranca",
                            PedidoEnergia = "energia", PedidoFrio = "frio", PedidoUptime = "uptime";
        public const double FolgaDeEnergiaDoPedido = 1.25;

        public static readonly TipoDeCliente[] TiposDeCliente =
        {
            new TipoDeCliente { Nome = "Loja do Seu Zé", Dominio = "lojadoseuze.com.br", Cor = "e8743a" },
            new TipoDeCliente { Nome = "Banco Pixel", Dominio = "bancopixel.com.br", Cor = "3a6fe8" },
            new TipoDeCliente { Nome = "Mercadinho Online", Dominio = "mercadinho.com", Cor = "3ab86a" },
            new TipoDeCliente { Nome = "Hospital São Byte", Dominio = "saobyte.org.br", Cor = "3ac0c8" },
            new TipoDeCliente { Nome = "Streaming Pipoca", Dominio = "pipoca.tv", Cor = "e83a5a" },
            new TipoDeCliente { Nome = "Prefeitura de Bitópolis", Dominio = "bitopolis.gov.br", Cor = "4a8a3a" },
            new TipoDeCliente { Nome = "Fintech Cripto Feliz", Dominio = "criptofeliz.io", Cor = "9a5ae8" },
            new TipoDeCliente { Nome = "Delivery Turbo", Dominio = "deliveryturbo.app", Cor = "e8b83a" },
            new TipoDeCliente { Nome = "Escola Ctrl+Z", Dominio = "ctrlz.edu.br", Cor = "5a7ae8" },
            new TipoDeCliente { Nome = "Rede Social Bolha", Dominio = "bolha.social", Cor = "e85ab8" },
            new TipoDeCliente { Nome = "Jogo de Fazendinha", Dominio = "fazendinha.game", Cor = "7ac83a" },
            new TipoDeCliente { Nome = "Cartório Digital", Dominio = "cartorio.digital", Cor = "8a7a6a" },
        };

        public static TipoDeCliente TipoDoCliente(string nome)
        {
            foreach (var t in TiposDeCliente) if (t.Nome == nome) return t;
            return TiposDeCliente[0];
        }

        /// <summary>Eventos que derrubam o site de todos os clientes enquanto duram.</summary>
        public static readonly string[] EventosQueDerrubamSites = { EventoDns, EventoSsl, EventoFaxineira, AtaqueRansomware };
    }

    /// <summary>
    /// Clientes: de tempos em tempos um cliente quer hospedar o site no seu data center (com o SLA que ele exige). Aceito, ele
    /// fica: paga renda a mais conforme o porte e a satisfação. A satisfação sobe com o site no ar e o uptime dentro do SLA,
    /// e cai quando o site cai (o servidor dele travou, o deploy quebrou, faltou energia, deu DNS...). Satisfeito por um
    /// tempo, ele cresce de porte (a página vira loja, e-commerce, plataforma, gigante); zerada, ele vai embora.
    /// Às vezes ele faz um pedido com prazo (mais capacidade, backup, segurança, folga de energia, sala mais fria, uptime):
    /// cumprir paga e acelera o crescimento.
    /// </summary>
    public partial class Economia
    {
        public event Action<Cliente> ClienteProposto, ClienteCresceu, ClienteSaiu, SiteCaiu, PedidoFeito;
        /// <summary>Pedido encerrado: (cliente, cumpriu?, prêmio).</summary>
        public event Action<Cliente, bool, double> PedidoTerminou;

        public List<Cliente> Clientes => Estado.clientes;
        public bool TemPropostaDeCliente => !string.IsNullOrEmpty(Estado.propostaCliente?.nome);
        public Cliente PropostaDeCliente => TemPropostaDeCliente ? Estado.propostaCliente : null;
        public int CapacidadeDeClientes => Catalogo.CapacidadeDeClientes(Estado.cargo);
        public bool PodeAceitarCliente => TemPropostaDeCliente && Estado.clientes.Count < CapacidadeDeClientes;

        /// <summary>Quanto um cliente soma na renda: o bônus do porte, inteiro com a satisfação cheia e pela metade com ela zerada.</summary>
        public double BonusDoCliente(Cliente c) =>
            Catalogo.BonusPorPorte[Math.Max(0, Math.Min(Catalogo.PorteMaximo, c.porte))] * (0.5 + 0.5 * c.satisfacao / 100);

        public double FatorClientes
        {
            get
            {
                double f = 1;
                foreach (var c in Estado.clientes) f += BonusDoCliente(c);
                return f;
            }
        }

        /// <summary>O site está fora: o servidor dele travou, ou algo derrubou todos (deploy, energia, pane, DNS, SSL...).</summary>
        public bool SiteFora(Cliente c) =>
            Travado(c.servidor) || DeployQuebrado || TemQuedaDeEnergia || TemPaneRegional
            || Array.IndexOf(Catalogo.EventosQueDerrubamSites, Estado.evento) >= 0
            || (Estado.evento == Catalogo.AtaqueDdos && Estado.eventoFase == 0)
            || (Estado.evento == Catalogo.EventoInternet && Estado.eventoFase == 0);

        void AvancarClientes(double segundos)
        {
            MigrarContratos();
            if (Estado.propostaCliente == null) Estado.propostaCliente = new Cliente();

            for (int i = Estado.clientes.Count - 1; i >= 0; i--)
            {
                var c = Estado.clientes[i];
                if (c.servidor >= TotalServidores) c.servidor = SorteiaServidor();
                bool fora = SiteFora(c);
                if (fora)
                {
                    if (c.foraDoAr == 0) SiteCaiu?.Invoke(c);
                    c.foraDoAr += segundos;
                    // quem exige mais SLA se irrita mais rápido (98%: 1x; 99,99%: 2x)
                    double sensibilidade = 1 + Math.Max(0, c.sla - 0.98) * 50;
                    c.satisfacao -= Catalogo.SatisfacaoCaiForaDoAr * sensibilidade * segundos;
                }
                else
                {
                    c.foraDoAr = 0;
                    if (Uptime >= c.sla) c.satisfacao += Catalogo.SatisfacaoSobePorSegundo * segundos;
                    else c.satisfacao -= Catalogo.SatisfacaoCaiAbaixoDoSla * segundos;
                }
                c.satisfacao = Math.Min(100, c.satisfacao);
                if (c.satisfacao <= 0)
                {
                    Estado.clientes.RemoveAt(i);
                    ClienteSaiu?.Invoke(c);
                    continue;
                }
                // satisfeito e no ar: cresce
                if (!fora && c.satisfacao >= Catalogo.SatisfacaoParaCrescer && c.porte < Catalogo.PorteMaximo)
                    Crescer(c, segundos / (Catalogo.MinutosParaCrescer[c.porte] * 60));
                AvancarPedido(c, segundos);
            }

            // a proposta na mesa vence se ninguém responde
            if (TemPropostaDeCliente)
            {
                Estado.propostaCliente.restante -= segundos;
                if (Estado.propostaCliente.restante <= 0) RecusarCliente();
                return;
            }
            if (Estado.cargo < Catalogo.CargoDosClientes || Estado.clientes.Count >= CapacidadeDeClientes) return;
            if (Estado.proximaProposta < 0) Estado.proximaProposta = Catalogo.PrimeiraProposta;
            Estado.proximaProposta -= segundos;
            if (Estado.proximaProposta <= 0) ProporCliente();
        }

        void Crescer(Cliente c, double quanto)
        {
            c.crescimento += quanto;
            if (c.crescimento < 1) return;
            c.crescimento = 0;
            c.porte++;
            ClienteCresceu?.Invoke(c);
        }

        int SorteiaServidor()
        {
            // só os servidores que podem travar (os primeiros de cada tipo)
            var possiveis = new List<int>();
            for (int s = 0; s < TotalServidores; s++)
                if ((EhTorre(s) ? s : s - Torres) < Catalogo.ServidoresQueTravam) possiveis.Add(s);
            return possiveis.Count == 0 ? 0 : possiveis[sorteio.Next(possiveis.Count)];
        }

        /// <summary>Um cliente novo propõe hospedar o site com o SLA que o uptime de agora entrega.</summary>
        public Cliente ProporCliente()
        {
            Estado.proximaProposta = Catalogo.IntervaloPropostaMin + sorteio.NextDouble() * (Catalogo.IntervaloPropostaMax - Catalogo.IntervaloPropostaMin);
            int maior = -1;
            for (int i = 0; i < Catalogo.SlasDosClientes.Length; i++)
                if (Uptime >= Catalogo.SlasDosClientes[i] + 0.0005) maior = i;
            if (maior < 0) return null;   // nem 98%: ninguém quer hospedar aqui agora
            var livres = new List<TipoDeCliente>();
            foreach (var t in Catalogo.TiposDeCliente)
                if (!Estado.clientes.Exists(c => c.nome == t.Nome)) livres.Add(t);
            if (livres.Count == 0) return null;
            int nivel = Math.Max(0, maior - (sorteio.NextDouble() < 0.4 ? 1 : 0));
            Estado.propostaCliente = new Cliente
            {
                nome = livres[sorteio.Next(livres.Count)].Nome,
                sla = Catalogo.SlasDosClientes[nivel],
                restante = Catalogo.TempoParaAceitar,
            };
            ClienteProposto?.Invoke(Estado.propostaCliente);
            return Estado.propostaCliente;
        }

        public bool AceitarCliente()
        {
            if (!PodeAceitarCliente) return false;
            var p = Estado.propostaCliente;
            Estado.clientes.Add(new Cliente
            {
                nome = p.nome, sla = p.sla, servidor = SorteiaServidor(),
                proximoPedido = Catalogo.PrimeiroPedidoMin + sorteio.NextDouble() * Catalogo.PrimeiroPedidoMin,
            });
            Estado.propostaCliente = new Cliente();
            return true;
        }

        public void RecusarCliente()
        {
            Estado.propostaCliente = new Cliente();
            if (Estado.proximaProposta <= 0) Estado.proximaProposta = Catalogo.IntervaloPropostaMin;
        }

        /// <summary>Com o jogo fechado ninguém responde proposta: ela some. Os clientes ficam como estavam.</summary>
        void LimparPropostaOffline() => Estado.propostaCliente = new Cliente();

        /// <summary>Saves antigos: cada contrato ativo vira um cliente, com o mesmo SLA.</summary>
        void MigrarContratos()
        {
            if (Estado.contratos == null || Estado.contratos.Count == 0) return;
            foreach (var k in Estado.contratos)
                if (!string.IsNullOrEmpty(k.cliente) && !Estado.clientes.Exists(c => c.nome == k.cliente))
                    Estado.clientes.Add(new Cliente { nome = k.cliente, sla = k.sla, servidor = SorteiaServidor(), proximoPedido = Catalogo.PrimeiroPedidoMin });
            Estado.contratos.Clear();
        }

        // ---------------- Pedidos ----------------

        void AvancarPedido(Cliente c, double segundos)
        {
            if (string.IsNullOrEmpty(c.pedido))
            {
                if (c.proximoPedido < 0) c.proximoPedido = Catalogo.PrimeiroPedidoMin;
                c.proximoPedido -= segundos;
                if (c.proximoPedido <= 0) FazerPedido(c);
                return;
            }
            c.pedidoRestante -= segundos;
            // o de uptime só conta no fim do prazo; os outros, assim que cumpridos
            if (c.pedido != Catalogo.PedidoUptime && PedidoCumprido(c)) { EncerrarPedido(c, true); return; }
            if (c.pedidoRestante <= 0) EncerrarPedido(c, PedidoCumprido(c));
        }

        /// <summary>Sorteia um pedido que dá para cumprir no cargo de agora (há o que comprar para isso).</summary>
        public bool FazerPedido(Cliente c, string tipo = null)
        {
            c.proximoPedido = Catalogo.IntervaloPedidoMin + sorteio.NextDouble() * (Catalogo.IntervaloPedidoMax - Catalogo.IntervaloPedidoMin);
            var possiveis = new List<(string tipo, double alvo, string item)>();
            var gerador = GeradorMaisBarato();
            if (gerador != null) possiveis.Add((Catalogo.PedidoServidores, ContagemServidores + 1 + c.porte, gerador.Id));
            var bkp = ProximoDaLinha(Catalogo.LinhaDeBackup);
            if (bkp != null) possiveis.Add((Catalogo.PedidoBackup, NivelBackup + 1, bkp.Id));
            var seg = ProximoDaLinha(Catalogo.LinhaDeSeguranca);
            if (seg != null) possiveis.Add((Catalogo.PedidoSeguranca, NivelSeguranca + 1, seg.Id));
            var energia = MaisBaratoQue(m => m.Id == Catalogo.NoBreak || m.Alvo == Catalogo.AlvoKw);
            if (energia != null && CapacidadeKw < ConsumoKw * Catalogo.FolgaDeEnergiaDoPedido)
                possiveis.Add((Catalogo.PedidoEnergia, Catalogo.FolgaDeEnergiaDoPedido, energia.Id));
            var frio = MaisBaratoQue(m => m.Id == Catalogo.ArCondicionado || m.Alvo == Catalogo.AlvoGraus);
            double alvoFrio = Math.Max(Catalogo.TemperaturaMinima + 2, Math.Floor(Temperatura) - 4);
            if (frio != null && Temperatura > alvoFrio) possiveis.Add((Catalogo.PedidoFrio, alvoFrio, frio.Id));
            if (Uptime >= c.sla) possiveis.Add((Catalogo.PedidoUptime, c.sla, ""));
            if (tipo != null) possiveis.RemoveAll(p => p.tipo != tipo);
            if (possiveis.Count == 0) return false;
            var escolhido = possiveis[sorteio.Next(possiveis.Count)];
            c.pedido = escolhido.tipo;
            c.pedidoAlvo = escolhido.alvo;
            c.pedidoItem = escolhido.item;
            c.pedidoRestante = Catalogo.PrazoDoPedido;
            PedidoFeito?.Invoke(c);
            return true;
        }

        MelhoriaDef GeradorMaisBarato() => MaisBaratoQue(m => m.Gerador);

        MelhoriaDef MaisBaratoQue(Predicate<MelhoriaDef> filtro)
        {
            MelhoriaDef melhor = null;
            foreach (var m in Catalogo.Melhorias)
            {
                if (m.Cargo > Estado.cargo || NoMaximo(m.Id) || !RequisitoOk(m.Id) || !filtro(m)) continue;
                if (melhor == null || Custo(m.Id) < Custo(melhor.Id)) melhor = m;
            }
            return melhor;
        }

        /// <summary>O próximo item ainda não comprado de uma linha (backup, segurança), se já dá para comprar no cargo.</summary>
        MelhoriaDef ProximoDaLinha(string[] linha)
        {
            foreach (var id in linha)
            {
                if (Nivel(id) > 0) continue;
                var m = Catalogo.Buscar(id);
                return m != null && m.Cargo <= Estado.cargo && RequisitoOk(id) ? m : null;
            }
            return null;
        }

        public bool PedidoCumprido(Cliente c)
        {
            switch (c.pedido)
            {
                case Catalogo.PedidoServidores: return ContagemServidores >= c.pedidoAlvo;
                case Catalogo.PedidoBackup: return NivelBackup >= c.pedidoAlvo;
                case Catalogo.PedidoSeguranca: return NivelSeguranca >= c.pedidoAlvo;
                case Catalogo.PedidoEnergia: return CapacidadeKw >= ConsumoKw * c.pedidoAlvo;
                case Catalogo.PedidoFrio: return Temperatura <= c.pedidoAlvo + 1e-9;
                case Catalogo.PedidoUptime: return Uptime >= c.pedidoAlvo;
                default: return false;
            }
        }

        /// <summary>O que o cliente pediu, em uma frase.</summary>
        public string TextoDoPedido(Cliente c)
        {
            switch (c.pedido)
            {
                case Catalogo.PedidoServidores: return "Mais capacidade: chegue a " + c.pedidoAlvo.ToString("0") + " servidores";
                case Catalogo.PedidoBackup: return "Quer backup melhor: compre " + NomeDoItem(c.pedidoItem);
                case Catalogo.PedidoSeguranca: return "Quer mais segurança: compre " + NomeDoItem(c.pedidoItem);
                case Catalogo.PedidoEnergia: return "Sistema pesado: deixe 25% de folga de energia";
                case Catalogo.PedidoFrio: return "O servidor esquenta: baixe a sala para " + c.pedidoAlvo.ToString("0") + " °C";
                case Catalogo.PedidoUptime: return "Campanha no ar: uptime acima de " + FormatarUptime(c.pedidoAlvo) + " até o prazo";
                default: return "";
            }
        }

        static string NomeDoItem(string id) => Catalogo.Buscar(id)?.Nome ?? id;

        /// <summary>Quanto do pedido já está feito (0 a 1).</summary>
        public double ProgressoDoPedido(Cliente c)
        {
            switch (c.pedido)
            {
                case Catalogo.PedidoServidores: return Math.Min(1, ContagemServidores / Math.Max(1, c.pedidoAlvo));
                case Catalogo.PedidoEnergia: return Math.Min(1, CapacidadeKw / Math.Max(0.001, ConsumoKw * c.pedidoAlvo));
                case Catalogo.PedidoFrio: return Math.Min(1, Math.Max(0, (Catalogo.TemperaturaCritica - Temperatura) / Math.Max(0.1, Catalogo.TemperaturaCritica - c.pedidoAlvo)));
                case Catalogo.PedidoUptime: return 1 - c.pedidoRestante / Catalogo.PrazoDoPedido;
                default: return PedidoCumprido(c) ? 1 : 0;
            }
        }

        void EncerrarPedido(Cliente c, bool cumpriu)
        {
            double premio = 0;
            if (cumpriu)
            {
                premio = ReceitaPorSegundo * Catalogo.SegundosDoPremioDoPedido;
                Ganhar(premio);
                c.satisfacao = Math.Min(100, c.satisfacao + Catalogo.SatisfacaoDoPedido);
                Estado.pedidosAtendidos++;
                if (c.porte < Catalogo.PorteMaximo) Crescer(c, Catalogo.CrescimentoDoPedido);
            }
            else c.satisfacao -= Catalogo.SatisfacaoDoPedidoPerdido;
            c.pedido = "";
            c.pedidoItem = "";
            PedidoTerminou?.Invoke(c, cumpriu, premio);
        }
    }
}
