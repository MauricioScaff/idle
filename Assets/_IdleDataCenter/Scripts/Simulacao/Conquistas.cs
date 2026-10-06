using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    public class ConquistaDef
    {
        public string Id, Nome, Descricao;
        /// <summary>Conquistada quando isto fica verdadeiro (null: vem de um acontecimento, ver Economia.Conquistar).</summary>
        public Func<Economia, bool> Condicao;
    }

    public static partial class Catalogo
    {
        /// <summary>Cada conquista dá +0,5% de renda para sempre (passa de uma empresa para a outra).</summary>
        public const double BonusPorConquista = 0.005;
        public const int CafesParaConquista = 10, ChamadosParaConquista = 100, IncidentesParaConquista = 500, AutomacoesParaConquista = 5;

        public const string ConquistaDns = "dns", ConquistaFaxineira = "faxineira", ConquistaRato = "rato", ConquistaReuniao = "reuniao",
                            ConquistaSsl = "ssl", ConquistaDeploySexta = "deploysexta", ConquistaBloqueou = "bloqueou",
                            ConquistaRansomware = "ransomware", ConquistaRefresh = "refresh", ConquistaExit = "exit";

        public static readonly IReadOnlyList<ConquistaDef> Conquistas = new[]
        {
            new ConquistaDef { Id = "hello", Nome = "Hello, world", Descricao = "Ter 2 servidores", Condicao = e => e.TotalServidores >= 2 },
            new ConquistaDef { Id = "fazenda", Nome = "Fazenda de torres", Descricao = "10 servidores torre", Condicao = e => e.Torres >= 10 },
            new ConquistaDef { Id = "rack", Nome = "Agora é sério", Descricao = "Comprar o primeiro rack", Condicao = e => e.TemRack },
            new ConquistaDef { Id = "cafe", Nome = "Movido a café", Descricao = "Tomar " + CafesParaConquista + " cafés", Condicao = e => e.Estado.cafesTomados >= CafesParaConquista },
            new ConquistaDef { Id = "p1", Nome = "Apagando incêndio", Descricao = "Atender um chamado P1", Condicao = e => e.Estado.chamadosP1 > 0 },
            new ConquistaDef { Id = "chamados", Nome = "Já tentou reiniciar?", Descricao = "Fechar " + ChamadosParaConquista + " chamados", Condicao = e => e.Estado.chamadosAtendidos >= ChamadosParaConquista },
            new ConquistaDef { Id = ConquistaDns, Nome = "É sempre o DNS", Descricao = "Passar por uma pane de DNS" },
            new ConquistaDef { Id = ConquistaFaxineira, Nome = "A tomada era do aspirador", Descricao = "A faxineira desligou o rack" },
            new ConquistaDef { Id = ConquistaRato, Nome = "Roeu o cabo", Descricao = "Um rato roeu o cabo de rede" },
            new ConquistaDef { Id = ConquistaReuniao, Nome = "Podia ser um e-mail", Descricao = "Sobreviver a uma reunião" },
            new ConquistaDef { Id = ConquistaSsl, Nome = "Venceu ontem", Descricao = "O certificado SSL expirou" },
            new ConquistaDef { Id = ConquistaDeploySexta, Nome = "Deploy na sexta", Descricao = "Fazer e dar certo" },
            new ConquistaDef { Id = ConquistaBloqueou, Nome = "Aqui não, hacker", Descricao = "Bloquear um ataque" },
            new ConquistaDef { Id = ConquistaRansomware, Nome = "Backup salva", Descricao = "Restaurar o backup num ransomware" },
            new ConquistaDef { Id = "tresnoves", Nome = "Três noves", Descricao = "Uptime de 99,9%", Condicao = e => e.Uptime >= 0.999 },
            new ConquistaDef { Id = "quatronoves", Nome = "Quatro noves", Descricao = "Uptime de 99,99%", Condicao = e => e.Uptime >= 0.9999 },
            new ConquistaDef { Id = "cinconoves", Nome = "Cinco noves", Descricao = "Uptime de 99,999%", Condicao = e => e.Uptime >= 0.99999 },
            new ConquistaDef { Id = ConquistaRefresh, Nome = "Cheiro de novo", Descricao = "Fazer o refresh do hardware" },
            new ConquistaDef { Id = "promovido", Nome = "Crachá novo", Descricao = "Ser contratado por uma empresa", Condicao = e => e.Cargo >= Catalogo.CargoTecnico },
            new ConquistaDef { Id = "sre", Nome = "Pager no bolso", Descricao = "Chegar a SRE", Condicao = e => e.Cargo >= Catalogo.CargoSre },
            new ConquistaDef { Id = "cto", Nome = "De estagiário a CTO", Descricao = "Chegar a CTO", Condicao = e => e.Cargo >= Catalogo.CargoCto },
            new ConquistaDef { Id = "milhao", Nome = "Primeiro milhão", Descricao = "Faturar R$ 1 milhão", Condicao = e => e.Estado.totalGanho >= 1e6 },
            new ConquistaDef { Id = "bilhao", Nome = "Unicórnio", Descricao = "Faturar R$ 1 bilhão", Condicao = e => e.Estado.totalGanho >= 1e9 },
            new ConquistaDef { Id = "incidentes", Nome = "Bombeiro de plantão", Descricao = "Resolver " + IncidentesParaConquista + " incidentes", Condicao = e => e.Estado.incidentesResolvidos >= IncidentesParaConquista },
            new ConquistaDef { Id = "automacoes", Nome = "Automatize tudo", Descricao = AutomacoesParaConquista + " automações rodando", Condicao = e => e.AutomacoesAtivas >= AutomacoesParaConquista },
            new ConquistaDef { Id = "zerotrust", Nome = "Confiar em ninguém", Descricao = "Comprar o Zero Trust", Condicao = e => e.Nivel(ZeroTrust) > 0 },
            new ConquistaDef { Id = "contrato", Nome = "Assinado e entregue", Descricao = "Atender um pedido de cliente", Condicao = e => e.Estado.pedidosAtendidos > 0 || e.Estado.contratosCumpridos > 0 },
            new ConquistaDef { Id = "gigante", Nome = "Cresceu com a gente", Descricao = "Um cliente chegou a Gigante", Condicao = e => e.Clientes.Exists(c => c.porte >= PorteMaximo) },
            new ConquistaDef { Id = "ipo", Nome = "Toca o sino", Descricao = "Abrir o capital e virar CEO", Condicao = e => e.IpoFeito },
            new ConquistaDef { Id = "terminal", Nome = "Linha de comando", Descricao = "Resolver " + ComandosParaConquista + " problemas no terminal", Condicao = e => e.Estado.comandosCertos >= ComandosParaConquista },
            new ConquistaDef { Id = "terminal100", Nome = "Quem precisa de mouse?", Descricao = "Resolver " + ComandosParaConquista2 + " problemas no terminal", Condicao = e => e.Estado.comandosCertos >= ComandosParaConquista2 },
            new ConquistaDef { Id = ConquistaExit, Nome = "Exit", Descricao = "Vender uma empresa" },
        };

        public static ConquistaDef BuscarConquista(string id)
        {
            foreach (var c in Conquistas) if (c.Id == id) return c;
            return null;
        }
    }

    /// <summary>
    /// Conquistas: marcos com humor de TI. Cada uma dá +0,5% de renda para sempre; ficam no prestígio, então valem para as
    /// próximas empresas também.
    /// </summary>
    public partial class Economia
    {
        public event Action<ConquistaDef> Conquistou;

        public bool TemConquista(string id) => Prestigio.conquistas.Contains(id);
        public int NumeroDeConquistas => Prestigio.conquistas.Count;
        public double FatorConquistas => 1 + NumeroDeConquistas * Catalogo.BonusPorConquista;

        /// <summary>Marca a conquista (uma vez só) e avisa. Retorna se era nova.</summary>
        public bool Conquistar(string id)
        {
            var def = Catalogo.BuscarConquista(id);
            if (def == null || TemConquista(id)) return false;
            Prestigio.conquistas.Add(id);
            Conquistou?.Invoke(def);
            return true;
        }

        /// <summary>As conquistas que dependem do estado do jogo (as de acontecimento são marcadas na hora).</summary>
        void ChecarConquistas()
        {
            foreach (var c in Catalogo.Conquistas)
                if (c.Condicao != null && !TemConquista(c.Id) && c.Condicao(this)) Conquistar(c.Id);
        }
    }
}
