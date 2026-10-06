using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    public enum TipoLinha { Normal, Comando, Ok, Erro, Info, Alerta, Limpar, Sair }

    public struct LinhaTerminal
    {
        public string Texto;
        public TipoLinha Tipo;
        public LinhaTerminal(string texto, TipoLinha tipo = TipoLinha.Normal) { Texto = texto; Tipo = tipo; }
    }

    /// <summary>
    /// Um problema que dá para resolver digitando: o sintoma como aparece no log, um comando de exemplo (o help mostra) e
    /// as soluções aceitas (cada uma é um conjunto de palavras que precisam aparecer na linha digitada).
    /// </summary>
    public class ProblemaTerminal
    {
        public string Id;            // estável enquanto o problema existe
        public string Log;
        public string Exemplo;
        public string[][] Solucoes;
        internal Func<bool> Resolver;
    }

    public static partial class Catalogo
    {
        // --- Terminal: resolver digitando dá bônus (e combo para acertos seguidos) ---
        public const double SegundosDeBonusDoTerminal = 8;
        public const double BonusPorCombo = 0.5;        // cada acerto seguido soma +50% no próximo, até ComboMaximo
        public const int ComboMaximo = 2;
        public const int ComandosParaConquista = 10, ComandosParaConquista2 = 100;
        public static readonly string[] HostsDasRegioes = { "sa-east", "us-east", "eu-west", "ap-south" };
        public static readonly string[] UsuariosDosCargos = { "freela", "tecnico", "sysadmin", "analista", "devops", "sre", "arquiteto", "cto" };

        public static readonly string[] Piadas =
        {
            "Funciona na minha máquina.",
            "Já tentou desligar e ligar de novo?",
            "Não existe nuvem: é só o computador de outra pessoa.",
            "Teste em produção é teste com plateia.",
            "Há 10 tipos de pessoas: as que entendem binário e as que não.",
            "O estagiário tem acesso root. Durma bem.",
            "99 bugs no código. Corrige um, compila: 127 bugs no código.",
            "Senha forte: Senha123! (anotada no post-it do monitor).",
        };
    }

    /// <summary>
    /// O terminal do modo gerente: quando algo quebra, o sintoma aparece no log e quem digita um comando certo resolve na
    /// hora, com bônus de renda (maior a cada acerto seguido). Os comandos são de verdade (reboot, fsck, mdadm, kubectl,
    /// certbot, iptables...), mas a conferência é tolerante: basta ter as palavras certas. Erro de digitação só zera o combo.
    /// </summary>
    public partial class Economia
    {
        public event Action<ProblemaTerminal, double> ResolvidoNoTerminal;

        int comboDoTerminal;
        /// <summary>Acertos seguidos no terminal (zera num comando que não existe ou não resolve nada).</summary>
        public int ComboDoTerminal => comboDoTerminal;
        public double MultiplicadorDoCombo => 1 + Math.Min(comboDoTerminal, Catalogo.ComboMaximo) * Catalogo.BonusPorCombo;

        public static string HostDoServidor(int servidor) => "srv" + (servidor + 1).ToString("00");
        public string UsuarioDoTerminal => Catalogo.UsuariosDosCargos[Math.Min(Estado.cargo, Catalogo.UsuariosDosCargos.Length - 1)];
        public string MaquinaDoTerminal => Estado.cargo == Catalogo.CargoFreelancer ? "casa" : Estado.cargo == Catalogo.CargoTecnico ? "armario" : Estado.cargo == Catalogo.CargoSysadmin ? "salinha" : "dc01";

        static string[][] S(params string[][] s) => s;
        static string[] P(params string[] p) => p;

        /// <summary>O que está quebrado agora e dá para resolver pelo terminal.</summary>
        public List<ProblemaTerminal> ProblemasNoTerminal()
        {
            var lista = new List<ProblemaTerminal>();
            foreach (var t in Estado.travamentos)
            {
                int s = t.servidor;
                string host = HostDoServidor(s);
                lista.Add(new ProblemaTerminal
                {
                    Id = "trava:" + s, Log = host + " kernel: watchdog: BUG: soft lockup - CPU#" + (s % 4) + " stuck for 23s!",
                    Exemplo = "ssh " + host + " sudo reboot",
                    Solucoes = S(P("reboot", host), P("restart", host), P("reset", host)),
                    Resolver = () => { Clicar(s); return true; },
                });
            }
            if (DiscoQueimado)
                lista.Add(NivelStorage == 0
                    ? new ProblemaTerminal
                    {
                        Id = "disco", Log = "sda: I/O error, dev sda, sector 4821733 · EXT4-fs: remontado só leitura",
                        Exemplo = "fsck -y /dev/sda", Solucoes = S(P("fsck", "sda")),
                        Resolver = () => { TrocarDisco(porTecnico: false); return true; },
                    }
                    : new ProblemaTerminal
                    {
                        Id = "disco", Log = "md0: Disk failure on sdc, disabling device · RAID degradado",
                        Exemplo = "mdadm /dev/md0 --add /dev/sdd", Solucoes = S(P("mdadm", "add"), P("mdadm", "replace")),
                        Resolver = () => { TrocarDisco(porTecnico: false); return true; },
                    });
            if (DeployQuebrado)
                lista.Add(new ProblemaTerminal
                {
                    Id = "deploy", Log = "api-7f9c4d   0/1   CrashLoopBackOff   (deploy v2.4.1)",
                    Exemplo = NosKubernetes > 0 ? "kubectl rollout undo deployment/api" : "docker service rollback api",
                    Solucoes = S(P("rollout", "undo"), P("rollback")),
                    Resolver = () => { FazerRollback(porTecnico: false); return true; },
                });
            if (EmPico && !PicoFoiEscalado)
                lista.Add(new ProblemaTerminal
                {
                    Id = "pico", Log = "ALERTA " + NomeDoPico + ": 12k req/s · capacidade 6k req/s",
                    Exemplo = "kubectl scale deployment/web --replicas=30",
                    Solucoes = S(P("scale", "replicas"), P("autoscale"), P("scale", "up")),
                    Resolver = () => Escalar(),
                });
            if (TemQuedaDeEnergia)
            {
                string dc = "dc" + (DatacenterSemEnergia + 1).ToString("00");
                lista.Add(new ProblemaTerminal
                {
                    Id = "energia", Log = dc + " UPS: on battery · utility power lost",
                    Exemplo = "ipmitool -H " + dc + " chassis power on",
                    Solucoes = S(P("power", "on", dc), P("poweron", dc)),
                    Resolver = () => { Religar(); return true; },
                });
            }
            if (TemPaneRegional)
            {
                string r = Catalogo.HostsDasRegioes[Math.Min(RegiaoEmPane, Catalogo.HostsDasRegioes.Length - 1)];
                lista.Add(new ProblemaTerminal
                {
                    Id = "pane", Log = "healthcheck " + r + ": 0/12 healthy · região fora do ar",
                    Exemplo = "dns failover " + r + " --to sa-east", Solucoes = S(P("failover")),
                    Resolver = () => { Redirecionar(); return true; },
                });
            }
            var ev = ProblemaDoEvento();
            if (ev != null) lista.Add(ev);
            return lista;
        }

        ProblemaTerminal ProblemaDoEvento()
        {
            if (!TemEvento) return null;
            ProblemaTerminal P1(string log, string exemplo, params string[][] solucoes) =>
                new ProblemaTerminal { Id = "evento:" + Estado.evento, Log = log, Exemplo = exemplo, Solucoes = solucoes, Resolver = AgirNoEvento };
            switch (Estado.evento)
            {
                case Catalogo.EventoSsl: return P1("nginx: SSL_ERROR certificate has expired (loja.exemplo.com.br)", "certbot renew", P("certbot", "renew"));
                case Catalogo.EventoDns: return P1("named: SERVFAIL resolvendo api.interno · é sempre o DNS", "systemctl restart named",
                    P("restart", "named"), P("restart", "bind"), P("restart", "dns"), P("flush"));
                case Catalogo.EventoFaxineira: return P1("rack02-pdu: outlet 1 sem energia (tiraram da tomada?)", "pdu rack02 outlet 1 on",
                    P("pdu", "on"), P("outlet", "on"), P("power", "on", "rack"));
                case Catalogo.AtaqueMalware: return P1("clamd: Win.Trojan.Agent FOUND /srv/compartilhado/fatura.pdf.exe", "clamscan -r --remove /srv/compartilhado",
                    P("clamscan"), P("rm", "fatura"));
                case Catalogo.EventoReuniao: return P1("calendar: \"Alinhamento rápido\" em 2 min (1h30, 14 pessoas)", "meeting decline --motivo 'podia ser um e-mail'",
                    P("decline"), P("recusar"));
                case Catalogo.AtaqueDdos when Estado.eventoFase == 0: return P1("nginx: 48.000 conexões/s de 203.0.113.0/24", "iptables -A INPUT -s 203.0.113.0/24 -j DROP",
                    P("iptables", "drop"), P("ufw", "deny"), P("block", "203.0.113"));
                case Catalogo.AtaqueRansomware when RestauraRansomware: return P1("smbd: 9.214 arquivos renomeados para *.chora em /srv/financeiro", "restic restore latest --target /srv/financeiro",
                    P("restore"));
                case Catalogo.EventoInternet when Estado.eventoFase == 0: return P1("ping 8.8.8.8: Destination Host Unreachable · o link de fibra caiu", "nmcli connection up celular-4g",
                    P("nmcli", "up"), P("4g"));
                case Catalogo.EventoCafeAcabou: return P1("cafeteira: reservatório vazio · produtividade caindo", "sudo make coffee", P("coffee"), P("cafe"));
                default: return null;
            }
        }

        /// <summary>Minúsculas e sem hífen (srv-02 = srv02), para a conferência tolerante.</summary>
        static string Normalizar(string s) => (s ?? "").Trim().ToLowerInvariant().Replace("-", "").Replace("_", "");

        static bool Bate(string linha, string[] palavras)
        {
            foreach (var p in palavras) if (!linha.Contains(Normalizar(p))) return false;
            return true;
        }

        /// <summary>Roda uma linha digitada. Devolve a saída (inclusive as linhas especiais Limpar e Sair).</summary>
        public List<LinhaTerminal> Executar(string linha)
        {
            var saida = new List<LinhaTerminal>();
            string texto = (linha ?? "").Trim();
            if (texto.Length == 0) return saida;
            string n = Normalizar(texto);

            // primeiro: resolve algum problema aberto?
            foreach (var p in ProblemasNoTerminal())
            {
                bool bate = false;
                foreach (var sol in p.Solucoes) if (Bate(n, sol)) { bate = true; break; }
                if (!bate) continue;
                if (!p.Resolver())
                {
                    comboDoTerminal = 0;
                    saida.Add(new LinhaTerminal("Não deu: falta dinheiro para isso.", TipoLinha.Erro));
                    return saida;
                }
                double bonus = ReceitaPorSegundo * Catalogo.SegundosDeBonusDoTerminal * MultiplicadorDoCombo;
                Ganhar(bonus);
                comboDoTerminal++;
                Estado.comandosCertos++;
                ChecarConquistas();
                saida.Add(new LinhaTerminal("OK · resolvido · +R$ " + Formatar(bonus) + (comboDoTerminal > 1 ? "  (combo x" + MultiplicadorDoCombo.ToString("0.#") + ")" : ""), TipoLinha.Ok));
                ResolvidoNoTerminal?.Invoke(p, bonus);
                return saida;
            }

            // depois: comandos do dia a dia (não resolvem nada, mas também não zeram o combo)
            string cmd = n.StartsWith("sudo ") ? n.Substring(5).Trim() : n;
            string primeiro = cmd.Split(' ')[0];
            switch (primeiro)
            {
                case "help": case "ajuda": case "?":
                    Ajuda(saida);
                    return saida;
                case "clear": case "cls":
                    saida.Add(new LinhaTerminal("", TipoLinha.Limpar));
                    return saida;
                case "exit": case "logout": case "quit":
                    saida.Add(new LinhaTerminal("", TipoLinha.Sair));
                    return saida;
                case "whoami":
                    saida.Add(new LinhaTerminal(UsuarioDoTerminal + " (" + CargoAtual.Nome + ")"));
                    return saida;
                case "uptime":
                    saida.Add(new LinhaTerminal("uptime da última hora: " + FormatarUptime(Uptime) + " · " + ContagemServidores + " servidores"));
                    return saida;
                case "top": case "htop":
                    saida.Add(new LinhaTerminal(ContagemServidores + " servidores · " + Estado.travamentos.Count + " travados · renda R$ " + Formatar(ReceitaPorSegundo) + "/s"));
                    saida.Add(new LinhaTerminal("energia " + ConsumoKw.ToString("0.#") + "/" + CapacidadeKw.ToString("0.#") + " kW · " + Temperatura.ToString("0") + " °C"));
                    return saida;
                case "df":
                    saida.Add(new LinhaTerminal(NivelStorage > 0 ? "/dev/md0   " + (NivelStorage * 12) + "T   RAID " + (DiscoQueimado ? "degradado" : "ok") : "/dev/sda   1T   " + (DiscoQueimado ? "só leitura" : "ok")));
                    return saida;
                case "ls":
                    saida.Add(new LinhaTerminal("backup.sh   deploy.yaml   runbooks/   senhas.txt"));
                    return saida;
                case "cat":
                    saida.Add(new LinhaTerminal(cmd.Contains("senhas") ? "Boa tentativa. O auditor anotou o seu nome." : "Arquivo sem graça. Tente o senhas.txt."));
                    return saida;
                case "ping":
                    bool semInternet = Estado.evento == Catalogo.EventoInternet && Estado.eventoFase == 0;
                    string alvo = cmd.Split(' ').Length > 1 ? cmd.Split(' ')[1] : "8.8.8.8";
                    saida.Add(new LinhaTerminal(semInternet ? "ping " + alvo + ": Destination Host Unreachable" : "64 bytes from " + alvo + ": icmp_seq=1 ttl=57 time=12 ms",
                        semInternet ? TipoLinha.Erro : TipoLinha.Normal));
                    return saida;
                case "rm":
                    saida.Add(new LinhaTerminal(cmd.Contains("rf") ? "Calma. O estagiário já tentou isso uma vez." : "rm: nada removido (ainda bem).", TipoLinha.Info));
                    return saida;
                case "coffee": case "cafe": case "make":
                    if (TomarCafe()) saida.Add(new LinhaTerminal("Café pronto! Renda x" + Catalogo.MultiplicadorCafe.ToString("0.#") + " por um tempo.", TipoLinha.Ok));
                    else saida.Add(new LinhaTerminal("A cafeteira ainda está esquentando.", TipoLinha.Info));
                    return saida;
                case "fortune":
                    saida.Add(new LinhaTerminal(Catalogo.Piadas[sorteio.Next(Catalogo.Piadas.Length)], TipoLinha.Info));
                    return saida;
                case "echo":
                    saida.Add(new LinhaTerminal(texto.Length > 5 ? texto.Substring(texto.IndexOf(' ') + 1) : ""));
                    return saida;
                case "vim": case "vi": case "nano":
                    saida.Add(new LinhaTerminal(primeiro == "nano" ? "nano? Aqui é terminal de respeito. (brincadeira)" : "Você entrou no vim. Ninguém sabe sair. Tente :q!", TipoLinha.Info));
                    return saida;
                case ":q!": case ":q": case ":wq":
                    saida.Add(new LinhaTerminal("Você saiu do vim. Coloque isso no currículo.", TipoLinha.Ok));
                    return saida;
                case "neofetch":
                    saida.Add(new LinhaTerminal(UsuarioDoTerminal + "@" + MaquinaDoTerminal + " · " + CargoAtual.Nome + " · " + CargoAtual.Lugar, TipoLinha.Info));
                    saida.Add(new LinhaTerminal("Servidores: " + ContagemServidores + " · Uptime: " + FormatarUptime(Uptime) + " · Automações: " + AutomacoesAtivas, TipoLinha.Info));
                    return saida;
            }

            // um comando de conserto que não bate com nada: zera o combo
            comboDoTerminal = 0;
            bool conhecido = Array.IndexOf(ComandosConhecidos, primeiro) >= 0;
            saida.Add(new LinhaTerminal(conhecido ? primeiro + ": nada para consertar com isso agora (digite help)" : "bash: " + texto.Split(' ')[0] + ": command not found",
                TipoLinha.Erro));
            return saida;
        }

        static readonly string[] ComandosConhecidos =
        {
            "reboot", "ssh", "systemctl", "fsck", "mdadm", "kubectl", "docker", "helm", "ipmitool", "dns", "certbot", "pdu", "clamscan",
            "meeting", "iptables", "ufw", "restic", "nmcli",
        };

        static readonly string[] ComandosDoDiaADia = { "help", "clear", "exit", "whoami", "uptime", "top", "df -h", "ls", "ping 8.8.8.8", "coffee", "fortune", "neofetch" };

        void Ajuda(List<LinhaTerminal> saida)
        {
            var problemas = ProblemasNoTerminal();
            if (problemas.Count == 0) saida.Add(new LinhaTerminal("Tudo funcionando. Quando algo quebrar, o sintoma aparece aqui em vermelho.", TipoLinha.Info));
            else
            {
                saida.Add(new LinhaTerminal("Quebrado agora (um comando que resolve):", TipoLinha.Info));
                foreach (var p in problemas) saida.Add(new LinhaTerminal("  " + p.Exemplo, TipoLinha.Ok));
            }
            saida.Add(new LinhaTerminal("Outros: " + string.Join(", ", ComandosDoDiaADia) + ". Tab completa, setas trazem o histórico.", TipoLinha.Normal));
        }

        /// <summary>Tab: completa a linha com um comando conhecido (os exemplos dos problemas abertos vêm primeiro).</summary>
        public string Completar(string parcial)
        {
            if (string.IsNullOrEmpty(parcial)) return parcial;
            var candidatos = new List<string>();
            foreach (var p in ProblemasNoTerminal()) candidatos.Add(p.Exemplo);
            candidatos.AddRange(ComandosDoDiaADia);
            var bons = candidatos.FindAll(c => c.StartsWith(parcial, StringComparison.OrdinalIgnoreCase));
            if (bons.Count == 0) return parcial;
            if (bons.Count == 1) return bons[0];
            // o pedaço que todos têm em comum
            string comum = bons[0];
            foreach (var b in bons)
            {
                int i = 0;
                while (i < comum.Length && i < b.Length && char.ToLowerInvariant(comum[i]) == char.ToLowerInvariant(b[i])) i++;
                comum = comum.Substring(0, i);
            }
            return comum.Length > parcial.Length ? comum : parcial;
        }
    }
}
