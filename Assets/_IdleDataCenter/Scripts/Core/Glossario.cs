using System;
using System.Collections.Generic;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter
{
    /// <summary>
    /// O dicionário do jogo: cada termo de TI explicado para quem não é da área (o que é na vida real, em linguagem de
    /// leigo, e o que faz no jogo). Aparece no menu (por cargo: os de cargos que ainda não chegaram ficam bloqueados) e
    /// como dica ao passar o mouse nos termos sublinhados do modo gerente. Os textos são em português; o inglês vem do
    /// Idiomas como o resto do jogo.
    /// </summary>
    public static class Glossario
    {
        public sealed class Termo
        {
            public string Nome;          // como aparece no dicionário
            public int Cargo;            // a partir de qual cargo aparece no jogo
            public string OQueE;         // na vida real
            public string NoJogo;        // o que faz aqui
            public string[] Pt;          // como ele aparece nos textos em português (sem diferenciar maiúsculas)
            public string[] En;          // e em inglês
            public bool Sublinhar = true;   // palavras comuns demais (servidor, chamado) ficam só no dicionário
        }

        static Termo T(int cargo, string nome, string oQueE, string noJogo, string[] pt, string[] en, bool sublinhar = true) =>
            new Termo { Cargo = cargo, Nome = nome, OQueE = oQueE, NoJogo = noJogo, Pt = pt, En = en, Sublinhar = sublinhar };

        static string[] L(params string[] s) => s;

        public static readonly Termo[] Termos =
        {
            // Freelancer: o quarto
            T(Catalogo.CargoFreelancer, "Servidor", "Um computador que fica ligado o dia todo servindo sites e programas para outras pessoas.",
                "É o que gera a renda: mais servidores, mais dinheiro por segundo.", L("servidor", "servidores", "torre", "torres"), L("server", "servers", "tower", "towers"), false),
            T(Catalogo.CargoFreelancer, "Hospedagem", "Guardar o site de alguém num computador que fica sempre ligado, para ele abrir na internet.",
                "Cada site de cliente na sua torre rende um pouco por segundo.", L("hospedagem"), L("hosting")),
            T(Catalogo.CargoFreelancer, "Terminal", "Uma tela de texto onde você dá ordens ao computador digitando comandos, sem mouse.",
                "Aperte F1: os incidentes que aparecem lá se resolvem com comandos de verdade (o 'help' lista todos).", L("terminal"), L("terminal")),

            // Técnico: o escritório
            T(Catalogo.CargoTecnico, "Chamado", "Um pedido de ajuda aberto para a TI: 'a impressora travou', 'esqueci a senha'.",
                "Clique no balão (ou no help desk) para o técnico ir resolver. Cada um tem um prazo.", L("chamado", "chamados"), L("ticket", "tickets"), false),
            T(Catalogo.CargoTecnico, "P1 a P4 (prioridade)", "O quanto um chamado é urgente: P1 é tudo parado, P4 pode esperar.",
                "P1 e P2 pagam mais e têm menos tempo para resolver.", L("P1", "P2", "P3", "P4", "prioridade"), L("P1", "P2", "P3", "P4", "priority")),
            T(Catalogo.CargoTecnico, "Help desk", "O balcão de atendimento da TI: quem recebe os chamados das pessoas.",
                "O painel da esquerda lista os chamados abertos; quem você contrata ajuda a fechar.", L("help desk", "service desk"), L("help desk", "service desk")),
            T(Catalogo.CargoTecnico, "Uptime", "Quanto tempo os sites ficaram no ar sem cair. 99,9% quer dizer menos de 9 horas fora por ano.",
                "Quanto mais perto de 100%, mais os clientes crescem.", L("uptime"), L("uptime")),
            T(Catalogo.CargoTecnico, "SLA", "Acordo de nível de serviço: a promessa, por contrato, de deixar o site no ar (por exemplo, 99,9% do tempo).",
                "Com uptime alto o bastante, você cumpre o SLA e os clientes pagam mais.", L("SLA"), L("SLA")),
            T(Catalogo.CargoTecnico, "SSD", "Um disco sem peças que giram, muitas vezes mais rápido que o HD antigo.",
                "Deixa as torres mais rápidas: elas rendem o dobro.", L("SSD"), L("SSD")),
            T(Catalogo.CargoTecnico, "kW (energia)", "Quilowatt: quanta energia os equipamentos puxam da tomada.",
                "Se o consumo passar do que a sala aguenta, ela entra em sobrecarga e rende menos.", L("kW"), L("kW")),
            T(Catalogo.CargoTecnico, "Backup", "Uma cópia dos dados guardada em outro lugar, para quando um disco queimar ou alguém apagar algo.",
                "Quando um disco queima, o backup diminui quanto você perde.", L("backup", "backups"), L("backup", "backups")),
            T(Catalogo.CargoTecnico, "DNS", "A 'agenda de contatos' da internet: transforma nomes de sites em endereços que os computadores entendem.",
                "Quando ele cai, tudo parece quebrado. No terminal, a culpa é sempre do DNS.", L("DNS"), L("DNS")),

            // Sysadmin: a salinha
            T(Catalogo.CargoSysadmin, "Sysadmin", "Administrador de sistemas: quem instala, atualiza e cuida dos servidores da empresa.",
                "Seu primeiro cargo com uma sala só de servidores.", L("sysadmin"), L("sysadmin")),
            T(Catalogo.CargoSysadmin, "Rack (42U)", "Um armário de metal padronizado onde os servidores ficam empilhados. 42U é a altura: cabem 42 servidores finos.",
                "Abre vagas para os servidores 1U, que rendem bem mais que as torres.", L("rack", "racks", "42U"), L("rack", "racks", "42U")),
            T(Catalogo.CargoSysadmin, "Servidor 1U", "Um servidor fininho, da altura de uma 'unidade' do rack (uns 4,5 cm), feito para ficar empilhado.",
                "Cada um rende mais e gasta energia: precisa de rack, no-break e ar-condicionado.", L("1U"), L("1U")),
            T(Catalogo.CargoSysadmin, "No-break", "Uma bateria grande que segura a energia quando a luz pisca ou cai.",
                "Aumenta quanta energia (kW) a sala aguenta ligada.", L("no-break", "no-breaks"), L("UPS", "UPSs")),
            T(Catalogo.CargoSysadmin, "Firewall", "Um porteiro da rede: deixa passar o que é permitido e barra o resto.",
                "Bloqueia parte dos ataques, que derrubam a receita por um tempo.", L("firewall"), L("firewall")),
            T(Catalogo.CargoSysadmin, "NAS", "Uma caixinha com vários discos ligada à rede, para guardar arquivos e backups.",
                "Mais um degrau do backup: perde menos quando um disco queima.", L("NAS"), L("NAS")),
            T(Catalogo.CargoSysadmin, "Firmware", "O programa que vem gravado dentro do próprio aparelho. Atualizar corrige falhas.",
                "Servidores com firmware novo rendem mais.", L("firmware"), L("firmware")),

            // Analista de Infra: a sala de racks
            T(Catalogo.CargoAnalista, "Storage e RAID", "Storage: um equipamento só de discos. RAID: juntar vários discos para que, se um queimar, nada se perca.",
                "Aumenta a receita e segura os dados quando um disco queima.", L("storage", "RAID"), L("storage", "RAID")),
            T(Catalogo.CargoAnalista, "Fita de backup", "Fita magnética: um jeito barato e antigo (e ainda muito usado) de guardar backup por anos.",
                "Mais um degrau do backup.", L("fita"), L("tape")),
            T(Catalogo.CargoAnalista, "Link e Mbps", "Link: a conexão da empresa com a internet. Mbps: quantos megabits passam por segundo, a 'grossura do cano'.",
                "Se o tráfego passar da banda, o link satura e tudo fica lento.", L("link", "Mbps", "Mb", "banda", "10G"), L("link", "Mbps", "Mb", "bandwidth", "10G")),
            T(Catalogo.CargoAnalista, "Tráfego", "Quanta gente está usando os sites ao mesmo tempo, e quantos dados isso faz passar pela rede.",
                "Cresce com os clientes; precisa caber no link.", L("tráfego"), L("traffic")),
            T(Catalogo.CargoAnalista, "VPN e MFA", "VPN: um túnel seguro para entrar na rede da empresa de fora. MFA: pedir um segundo código além da senha.",
                "Bloqueiam mais ataques.", L("VPN", "MFA"), L("VPN", "MFA")),
            T(Catalogo.CargoTecnico, "Incidente", "Quando algo quebra e atrapalha alguém: servidor travado, disco queimado, site fora do ar, o PC lento de um colega.",
                "Resolva rápido (clicando ou pelo terminal): enquanto dura, a receita cai. Cada chamado atendido também conta como incidente.", L("incidente", "incidentes"), L("incident", "incidents")),
            T(Catalogo.CargoAnalista, "Automação (script)", "Um programinha que faz sozinho uma tarefa repetitiva, para ninguém precisar fazer na mão.",
                "O técnico escreve uma por vez; cada uma resolve um tipo de problema para sempre.", L("automação", "automações", "script", "scripts"), L("automation", "automations", "script", "scripts")),
            T(Catalogo.CargoAnalista, "Watchdog", "Um 'cão de guarda' que percebe quando um programa travou e reinicia sozinho.",
                "Servidor travado volta sozinho em poucos segundos.", L("watchdog"), L("watchdog")),
            T(Catalogo.CargoAnalista, "Cron", "O agendador do Linux: roda uma tarefa no horário marcado, todo dia.",
                "O cron de faturamento faz o jogo render mais com ele fechado.", L("cron"), L("cron")),

            // DevOps
            T(Catalogo.CargoDevOps, "DevOps", "Um jeito de trabalhar em que quem programa e quem cuida dos servidores trabalham juntos para entregar mais rápido.",
                "Chegam os apps: containers, deploys e o NOC.", L("DevOps"), L("DevOps")),
            T(Catalogo.CargoDevOps, "Hypervisor e VM", "Hypervisor: o programa que divide um servidor em vários computadores 'de mentira' (VMs), cada um com seu sistema.",
                "Aproveita melhor as máquinas: servidores rendem mais.", L("hypervisor", "hypervisors", "VM", "VMs"), L("hypervisor", "hypervisors", "VM", "VMs")),
            T(Catalogo.CargoDevOps, "Container", "Um 'pacote' com o programa e tudo de que ele precisa, que roda igual em qualquer servidor.",
                "Os hosts de containers rodam os apps, que rendem muito.", L("container", "containers"), L("container", "containers")),
            T(Catalogo.CargoDevOps, "Deploy", "Colocar no ar uma versão nova de um programa.",
                "O botão Deploy entrega uma versão e rende na hora. Às vezes ela quebra.", L("deploy", "deploys"), L("deploy", "deploys")),
            T(Catalogo.CargoDevOps, "Rollback", "Voltar para a versão anterior quando a nova deu problema.",
                "Conserta um deploy quebrado.", L("rollback"), L("rollback")),
            T(Catalogo.CargoDevOps, "CI e pipeline", "Uma esteira automática que testa e entrega cada mudança no código.",
                "Menos deploys quebrados e apps rendendo mais.", L("CI", "pipeline"), L("CI", "pipeline")),
            T(Catalogo.CargoDevOps, "Cache (Redis)", "Guardar por perto o que é pedido toda hora, para responder mais rápido. Redis é um dos mais usados.",
                "Apps mais rápidos, mais receita.", L("cache", "Redis"), L("cache", "Redis")),
            T(Catalogo.CargoDevOps, "NOC", "Centro de operações de rede: a sala com telões onde se vigia se tudo está no ar.",
                "Lista os incidentes e resolve todos de uma vez.", L("NOC"), L("NOC")),
            T(Catalogo.CargoDevOps, "Infra como código", "Descrever os servidores em arquivos de texto, para recriar tudo igualzinho com um comando.",
                "Melhorias ficam mais baratas.", L("infra como código"), L("infrastructure as code")),

            // SRE: o data center
            T(Catalogo.CargoSre, "SRE", "Engenharia de confiabilidade: quem garante que os sistemas não caiam, usando código e medições.",
                "Chegam o data center, o Kubernetes e os picos de tráfego.", L("SRE"), L("SRE")),
            T(Catalogo.CargoSre, "Kubernetes (K8s)", "Um sistema que liga, desliga e espalha cópias dos programas sozinho, conforme a procura.",
                "Os nós do cluster rodam os apps e aguentam os picos.", L("Kubernetes", "K8s", "cluster"), L("Kubernetes", "K8s", "cluster")),
            T(Catalogo.CargoSre, "Escalar", "Colocar mais máquinas (ou mais cópias do programa) para aguentar mais gente ao mesmo tempo.",
                "Num pico de tráfego, escale a tempo e o pico rende o dobro.", L("escalar", "escale", "escalado", "pico", "picos"), L("scale", "scaled", "peak", "peaks", "spike", "spikes")),
            T(Catalogo.CargoSre, "Balanceador", "Distribui os acessos entre vários servidores, para nenhum ficar sobrecarregado.",
                "O cluster rende mais.", L("balanceador", "balanceamento"), L("load balancer", "balancer", "balancing")),
            T(Catalogo.CargoSre, "Observabilidade", "Gráficos, registros (logs) e alertas que mostram o que está acontecendo dentro dos sistemas.",
                "Ajuda a cumprir o SLA.", L("observabilidade", "observab."), L("observability")),
            T(Catalogo.CargoSre, "DDoS e WAF", "DDoS: um ataque com milhares de acessos falsos ao mesmo tempo para derrubar o site. WAF: um filtro que barra isso.",
                "Bloqueia mais ataques.", L("DDoS", "WAF"), L("DDoS", "WAF")),
            T(Catalogo.CargoSre, "Chaos engineering", "Quebrar coisas de propósito, com cuidado, para descobrir se o sistema aguenta antes que quebre sozinho.",
                "Menos incidentes.", L("chaos engineering"), L("chaos engineering")),
            T(Catalogo.CargoSre, "Runbook", "O passo a passo de como resolver um problema que já aconteceu antes.",
                "Automático: os consertos acontecem sozinhos.", L("runbook", "runbooks"), L("runbook", "runbooks")),

            // Arquiteto: o campus
            T(Catalogo.CargoArquiteto, "Datacenter (DC)", "Um prédio inteiro feito para servidores, com energia, internet e refrigeração reservas.",
                "Cada prédio novo no campus soma muita receita.", L("datacenter", "datacenters", "DC", "DCs"), L("datacenter", "datacenters", "data center", "DC", "DCs")),
            T(Catalogo.CargoArquiteto, "CDN", "Cópias do site espalhadas pelo mundo, perto de quem acessa, para abrir mais rápido.",
                "Mais receita e menos tráfego no seu link.", L("CDN"), L("CDN")),
            T(Catalogo.CargoArquiteto, "Failover", "Passar sozinho para um sistema reserva quando o principal cai.",
                "Quedas voltam em segundos.", L("failover"), L("failover")),
            T(Catalogo.CargoArquiteto, "SOC", "Centro de operações de segurança: uma equipe vigiando ataques 24 horas por dia.",
                "Bloqueia quase todos os ataques.", L("SOC"), L("SOC")),

            // CTO: o mundo
            T(Catalogo.CargoCto, "CTO", "Diretor de tecnologia: quem decide os rumos da tecnologia da empresa.",
                "O último cargo antes do IPO.", L("CTO"), L("CTO")),
            T(Catalogo.CargoCto, "Região", "Um grupo de datacenters numa parte do mundo, como São Paulo ou Virgínia.",
                "Cada região nova no mapa soma muita receita.", L("região", "regiões", "regional"), L("region", "regions", "regional")),
            T(Catalogo.CargoCto, "Edge computing", "Processar os dados perto de quem usa (na antena, na loja), em vez de longe, no datacenter.",
                "Mais receita das regiões.", L("edge computing", "edge"), L("edge computing", "edge")),
            T(Catalogo.CargoCto, "GPU", "Uma placa feita para fazer muitos cálculos ao mesmo tempo. É o que roda as IAs.",
                "A nuvem de IA rende muito.", L("GPU"), L("GPU")),
            T(Catalogo.CargoCto, "Zero Trust", "Ninguém é confiável por padrão: todo acesso é conferido, até de quem está dentro da empresa.",
                "Bloqueia quase todos os ataques.", L("zero trust"), L("zero trust")),
            T(Catalogo.CargoCto, "IPO", "Quando a empresa passa a vender ações na bolsa de valores.",
                "O fim da carreira: você vira CEO. Depois, dá para vender a empresa e recomeçar com bônus.", L("IPO"), L("IPO")),
        };

        /// <summary>Os termos que aparecem a partir de um cargo (para as abas do dicionário).</summary>
        public static List<Termo> DoCargo(int cargo) => new List<Termo>(Array.FindAll(Termos, t => t.Cargo == cargo));

        // ---------------- Termos num texto ----------------

        static List<(string forma, Termo termo)> formas;
        static bool formasEmIngles;
        static readonly Dictionary<string, List<(int inicio, int comprimento, Termo termo)>> cache = new Dictionary<string, List<(int, int, Termo)>>();
        static readonly List<(int, int, Termo)> nenhum = new List<(int, int, Termo)>();

        /// <summary>
        /// Onde estão os termos sublinháveis num texto já no idioma da tela (palavra inteira, sem diferenciar maiúsculas;
        /// a forma mais comprida ganha: "service desk" antes de "desk"). Guardado por texto, porque é chamado a cada quadro.
        /// </summary>
        public static List<(int inicio, int comprimento, Termo termo)> Encontrar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return nenhum;
            bool ingles = Idiomas.Ingles;
            if (formas == null || formasEmIngles != ingles)
            {
                formasEmIngles = ingles;
                cache.Clear();
                formas = new List<(string, Termo)>();
                foreach (var t in Termos)
                    if (t.Sublinhar)
                        foreach (var f in ingles ? t.En : t.Pt) formas.Add((f, t));
                formas.Sort((a, b) => b.forma.Length.CompareTo(a.forma.Length));
            }
            if (cache.TryGetValue(texto, out var achados)) return achados;
            if (cache.Count > 4000) cache.Clear();   // textos com números mudam a cada quadro

            achados = null;
            for (int i = 0; i < texto.Length; i++)
            {
                if (i > 0 && char.IsLetterOrDigit(texto[i - 1])) continue;   // só no começo de uma palavra
                foreach (var (forma, termo) in formas)
                {
                    int n = forma.Length;
                    if (i + n > texto.Length || string.Compare(texto, i, forma, 0, n, StringComparison.OrdinalIgnoreCase) != 0) continue;
                    if (i + n < texto.Length && char.IsLetterOrDigit(texto[i + n])) continue;
                    (achados ??= new List<(int, int, Termo)>()).Add((i, n, termo));
                    i += n - 1;
                    break;
                }
            }
            return cache[texto] = achados ?? nenhum;
        }
    }
}
