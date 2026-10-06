using System;
using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// O terminal do modo gerente: um console que desce do topo (F1, a tecla ' ou o botão Terminal) com o log do data center
    /// rodando. O que quebra aparece em vermelho; digitar um comando certo resolve na hora (ver Simulacao/Terminal.cs).
    /// Tab completa, as setas trazem o histórico, Esc fecha.
    /// </summary>
    public partial class ModoGerente
    {
        const float AlturaTerminal = 430, LinhaDoTerminal = 21;
        const int LinhasGuardadas = 300;

        static readonly string[] IconeTerminal = { "#....", ".#...", "..#..", ".#...", "#.###" };
        static readonly Color CorDoTerminal = IsoGui.Cor("0b1420");

        bool terminalAberto, ignorarCaractere;
        float terminalAnim, proximoLogAmbiente, proximaChecagemDoTerminal, acertoNoTerminalEm = -10;
        string entrada = "";
        string comandoDeTeste;
        int posHistorico = -1;
        bool dicaDoTerminalDada;
        readonly List<LinhaTerminal> linhasDoTerminal = new List<LinhaTerminal>();
        readonly List<string> historicoDoTerminal = new List<string>();
        readonly HashSet<string> problemasNoLog = new HashSet<string>();
        readonly System.Random sorteioDoLog = new System.Random();
        bool atenderDeTeste;

        void IniciarTerminal()
        {
            E.ResolvidoNoTerminal += (p, bonus) => { acertoNoTerminalEm = Time.unscaledTime; Sons.Moeda(); };
            E.SiteCaiu += c => Escrever(Hora() + Catalogo.TipoDoCliente(c.nome).Dominio + ": 502 Bad Gateway (o site de " + c.nome + " caiu junto)", TipoLinha.Alerta);
            Escrever("Idle Data Center · terminal. Quando algo quebrar, o sintoma aparece aqui em vermelho.", TipoLinha.Info);
            Escrever("Digite help para ver o comando que resolve. Acertos seguidos rendem combo.", TipoLinha.Info);
            // teste: -terminal abre; -terminal=<comando> abre e digita o comando 4 s depois (dá tempo do sintoma aparecer)
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-atender") atenderDeTeste = true;   // teste: atende o primeiro chamado 3 s depois (o técnico vai até lá)
                if (!arg.StartsWith("-terminal")) continue;
                AbrirTerminal();
                if (arg.StartsWith("-terminal=")) comandoDeTeste = arg.Substring(10).Replace('_', ' ');   // _ no lugar dos espaços
            }
        }

        string Prompt => E.UsuarioDoTerminal + "@" + E.MaquinaDoTerminal + ":~$ ";

        void AbrirTerminal()
        {
            terminalAberto = true;
            ignorarCaractere = true;   // a tecla que abriu não vira texto
        }

        void FecharTerminal() => terminalAberto = false;

        /// <summary>Roda no Update: escreve no log o que quebrou e as linhas do dia a dia (mesmo fechado, para o histórico).</summary>
        void AtualizarTerminal()
        {
            float agora = Time.unscaledTime;
            if (comandoDeTeste != null && agora > 4) { entrada = comandoDeTeste; comandoDeTeste = null; Enviar(); }
            if (atenderDeTeste && agora > 3 && E.Chamados.Count > 0) { atenderDeTeste = false; salaIso.IrAtender(0); AtenderChamado(new Vector2(W / 2, H / 2), 0); }
            if (agora >= proximaChecagemDoTerminal)
            {
                proximaChecagemDoTerminal = agora + 0.25f;
                var abertos = new HashSet<string>();
                foreach (var p in E.ProblemasNoTerminal())
                {
                    abertos.Add(p.Id);
                    if (!problemasNoLog.Add(p.Id)) continue;
                    Escrever(Hora() + p.Log, TipoLinha.Alerta);
                    if (!dicaDoTerminalDada && E.Estado.comandosCertos == 0 && !terminalAberto)
                    {
                        dicaDoTerminalDada = true;
                        Notificar("Dica: aperte F1 (ou o botão Terminal) e digite help para resolver pelo teclado, com bônus.", 8);
                    }
                }
                problemasNoLog.IntersectWith(abertos);   // se voltar a quebrar, aparece de novo
            }
            if (agora >= proximoLogAmbiente)
            {
                proximoLogAmbiente = agora + 1.5f + (float)sorteioDoLog.NextDouble() * 3.5f;
                Escrever(Hora() + LinhaDoDiaADia(), TipoLinha.Normal);
            }
        }

        static string Hora() => DateTime.Now.ToString("HH:mm:ss") + "  ";

        string LinhaDoDiaADia()
        {
            int n = sorteioDoLog.Next(1000, 9999);
            var opcoes = new List<string>
            {
                "CRON[" + n + "]: (root) CMD (/usr/local/bin/backup.sh)",
                "sshd[" + n + "]: Accepted publickey for " + E.UsuarioDoTerminal + " from 10.0.0." + sorteioDoLog.Next(2, 250),
                "nginx: GET /api/pedidos 200 " + sorteioDoLog.Next(4, 90) + "ms",
                "nginx: POST /login 200 " + sorteioDoLog.Next(20, 140) + "ms",
                "systemd[1]: Started Daily apt upgrade and clean activities.",
                "kernel: [UFW BLOCK] IN=eth0 SRC=185.220.101." + sorteioDoLog.Next(2, 250) + " DPT=22",
                "smartd: Device /dev/sda, SMART Usage Attribute: 194 Temperature " + Mathf.RoundToInt((float)E.Temperatura + 8) + " C",
            };
            if (E.Cargo >= Catalogo.CargoDevOps)
            {
                opcoes.Add("dockerd: container api-" + n.ToString("x") + " health_status: healthy");
                opcoes.Add("gitlab-runner: job #" + n + " succeeded (deploy)");
            }
            if (E.Cargo >= Catalogo.CargoSre)
            {
                opcoes.Add("kubelet: Started container web (pod web-" + n.ToString("x") + ")");
                opcoes.Add("prometheus: alert HighLatency resolved");
            }
            if (E.Cargo >= Catalogo.CargoArquiteto) opcoes.Add("bgpd: neighbor 200.160.0." + sorteioDoLog.Next(1, 250) + " Up");
            return opcoes[sorteioDoLog.Next(opcoes.Count)];
        }

        void Escrever(string texto, TipoLinha tipo)
        {
            if (ui == null) { linhasDoTerminal.Add(new LinhaTerminal(texto, tipo)); return; }
            foreach (var parte in QuebrarLinha(texto, W - 60))
                linhasDoTerminal.Add(new LinhaTerminal(parte, tipo));
            if (linhasDoTerminal.Count > LinhasGuardadas) linhasDoTerminal.RemoveRange(0, linhasDoTerminal.Count - LinhasGuardadas);
        }

        /// <summary>Quebra por palavras para caber na largura (palavras maiores que a linha ficam inteiras).</summary>
        List<string> QuebrarLinha(string texto, float largura, int escala = 2)
        {
            var linhas = new List<string>();
            string atual = "";
            foreach (var palavra in (texto ?? "").Split(' '))
            {
                string tentativa = atual.Length == 0 ? palavra : atual + " " + palavra;
                if (atual.Length == 0 || ui.Largura(tentativa, escala) <= largura) atual = tentativa;
                else { linhas.Add(atual); atual = "  " + palavra; }
            }
            linhas.Add(atual);
            return linhas;
        }

        void Enviar()
        {
            string linha = entrada.Trim();
            entrada = "";
            posHistorico = -1;
            Escrever(Prompt + linha, TipoLinha.Comando);
            if (linha.Length == 0) return;
            if (historicoDoTerminal.Count == 0 || historicoDoTerminal[historicoDoTerminal.Count - 1] != linha) historicoDoTerminal.Add(linha);
            if (linha == "history")
            {
                for (int i = 0; i < historicoDoTerminal.Count; i++) Escrever("  " + (i + 1) + "  " + historicoDoTerminal[i], TipoLinha.Normal);
                return;
            }
            foreach (var s in E.Executar(linha))
            {
                if (s.Tipo == TipoLinha.Limpar) { linhasDoTerminal.Clear(); continue; }
                if (s.Tipo == TipoLinha.Sair) { FecharTerminal(); continue; }
                Escrever(s.Texto, s.Tipo);
            }
        }

        /// <summary>
        /// Teclado e mouse do terminal: roda no começo do OnGUI (com a matriz já posta). Aberto, ele fica com o teclado e
        /// com os cliques na área dele, para não clicar no que está por baixo.
        /// </summary>
        void EventosDoTerminal()
        {
            var ev = Event.current;
            if (terminalAnim > 0.01f && (ev.isMouse || ev.type == EventType.ScrollWheel) && ev.mousePosition.y < AlturaTerminal * Suavizar(terminalAnim))
            {
                // o botão de fechar fica no canto do console
                if (ev.type == EventType.MouseDown && BotaoFecharTerminal.Contains(ev.mousePosition)) FecharTerminal();
                ev.Use();
                return;
            }
            if (ev.type != EventType.KeyDown) return;
            if (!terminalAberto)
            {
                if (ev.keyCode == KeyCode.F1 || ((ev.keyCode == KeyCode.BackQuote || ev.keyCode == KeyCode.Quote) && Livre))
                {
                    AbrirTerminal();
                    ev.Use();
                }
                return;
            }
            switch (ev.keyCode)
            {
                case KeyCode.Return: case KeyCode.KeypadEnter: Enviar(); break;
                case KeyCode.Backspace: if (entrada.Length > 0) entrada = entrada.Substring(0, entrada.Length - 1); break;
                case KeyCode.Escape: case KeyCode.F1: FecharTerminal(); break;
                case KeyCode.Tab: entrada = E.Completar(entrada); break;
                case KeyCode.UpArrow:
                    if (historicoDoTerminal.Count == 0) break;
                    posHistorico = posHistorico < 0 ? historicoDoTerminal.Count - 1 : Mathf.Max(0, posHistorico - 1);
                    entrada = historicoDoTerminal[posHistorico];
                    break;
                case KeyCode.DownArrow:
                    if (posHistorico < 0) break;
                    posHistorico++;
                    if (posHistorico >= historicoDoTerminal.Count) { posHistorico = -1; entrada = ""; }
                    else entrada = historicoDoTerminal[posHistorico];
                    break;
                default:
                    char c = ev.character;
                    if (c != '\0' && c != '\t' && c != '\n' && c != '\r' && !char.IsControl(c))
                    {
                        if (ignorarCaractere && (c == '\'' || c == '`' || c == '´')) { ignorarCaractere = false; break; }
                        ignorarCaractere = false;
                        if (entrada.Length < 120) entrada += c;
                    }
                    break;
            }
            ev.Use();
        }

        static float Suavizar(float t) => 1 - (1 - t) * (1 - t) * (1 - t);

        Rect BotaoFecharTerminal => new Rect(W - 60, AlturaTerminal * Suavizar(terminalAnim) - AlturaTerminal + 8, 44, 30);

        /// <summary>O console: desce do topo, com o log por cima e a linha de comando embaixo.</summary>
        void Terminal()
        {
            if (Event.current.type == EventType.Repaint)
                terminalAnim = Mathf.MoveTowards(terminalAnim, terminalAberto ? 1 : 0, Time.unscaledDeltaTime * 5);
            if (terminalAnim <= 0.001f) return;
            float h = AlturaTerminal, y = h * Suavizar(terminalAnim) - h;
            // fundo (passa das bordas da tela de 1440 x 900 em monitores mais largos)
            ui.Ret(new Rect(-W, y - H, W * 3, h + H), new Color(CorDoTerminal.r, CorDoTerminal.g, CorDoTerminal.b, 0.96f));
            bool acertou = Time.unscaledTime - acertoNoTerminalEm < 0.5f;
            ui.Ret(new Rect(-W, y + h - 3, W * 3, 3), acertou ? IsoGui.Verde : IsoGui.Cyan);

            // barra de título
            ui.Texto(E.UsuarioDoTerminal + "@" + E.MaquinaDoTerminal + "  ·  terminal  ·  help: comandos  ·  Tab completa  ·  Esc fecha", 24, y + 14, IsoGui.Muted, 2);
            if (E.ComboDoTerminal > 1)
            {
                string combo = "combo x" + E.MultiplicadorDoCombo.ToString("0.#");
                ui.Texto(combo, W - 80 - ui.Largura(combo, 3), y + 12, Ouro, 3);
            }
            var bf = BotaoFecharTerminal;
            ui.Caixa(bf, IsoGui.Painel, IsoGui.Borda);
            ui.Texto("x", bf.center.x, bf.y + 9, IsoGui.Branco, 2, true);
            ui.Ret(new Rect(16, y + 38, W - 32, 1), IsoGui.Borda);

            // o log, de baixo para cima
            float baseY = y + h - 60;
            int cabem = Mathf.FloorToInt((h - 110) / LinhaDoTerminal);
            for (int i = 0; i < cabem && i < linhasDoTerminal.Count; i++)
            {
                var l = linhasDoTerminal[linhasDoTerminal.Count - 1 - i];
                ui.Texto(l.Texto, 24, baseY - (i + 1) * LinhaDoTerminal, CorDaLinha(l.Tipo), 2);
            }

            // linha de comando
            ui.Ret(new Rect(16, y + h - 46, W - 32, 34), IsoGui.Cor("101e30"));
            string prompt = Prompt;
            ui.Texto(prompt, 24, y + h - 36, IsoGui.Verde, 2);
            float px = 24 + ui.Largura(prompt, 2) + 4;
            ui.Texto(entrada, px, y + h - 36, IsoGui.Branco, 2);
            if (terminalAberto && Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 2 == 0)
                ui.Ret(new Rect(px + ui.Largura(entrada, 2) + 3, y + h - 38, 10, 18), IsoGui.Branco);
        }

        static Color CorDaLinha(TipoLinha t) => t switch
        {
            TipoLinha.Comando => IsoGui.Branco,
            TipoLinha.Ok => IsoGui.Verde,
            TipoLinha.Erro => Vermelho,
            TipoLinha.Info => IsoGui.Cyan,
            TipoLinha.Alerta => Vermelho,
            _ => IsoGui.Muted,
        };

        /// <summary>O botão do canto de baixo à direita; com problema aberto, mostra quantos.</summary>
        void BotaoDoTerminal()
        {
            var r = new Rect(W - 210, 828, 190, 48);
            int abertos = problemasNoLog.Count;
            var cor = abertos > 0 ? (Pisca ? Vermelho : IsoGui.Laranja) : IsoGui.Borda;
            if (BotaoIcone(r, abertos > 0 ? "Terminal " + abertos : "Terminal", cor, IconeTerminal, 3, terminalAberto))
            {
                if (terminalAberto) FecharTerminal(); else AbrirTerminal();
                ignorarCaractere = false;
            }
        }
    }
}
