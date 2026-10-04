using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Primeira hora no modo gerente: tutorial em 6 dicas com uma seta apontando onde clicar,
    /// o café e o chamado urgente na sala, e a festa da promoção.
    /// </summary>
    public partial class ModoGerente
    {
        float festaDesde = -10;
        string festaCargo = "", festaTitulo = "Promovido!", festaTexto = "";

        static readonly string[] Dicas =
        {
            "Bem-vindo! Clique num servidor para ganhar dinheiro.",
            "Compre o SSD: ele dobra a receita das torres. Está na loja.",
            "Hora do café: clique na caneca da mesa. A receita dobra por 30 s.",
            "Construa mais um servidor no marcador laranja da sala.",
            "A meta da promoção fica no topo. Cumpra as três para virar Sysadmin.",
            "Vai trabalhar? O botão Faixa deixa o jogo discreto acima da barra de tarefas.",
        };

        void IniciarPrimeiraHora()
        {
            E.Comprou += id =>
            {
                salaIso?.Comprou(id, Time.unscaledTime);
                if (!E.AtingiuMarco(id)) return;
                // marco de um gerador: festa curta
                festaDesde = Time.unscaledTime; festaTitulo = "Marco!";
                festaCargo = E.UnidadesDoGerador(id) + "× " + NomeLongo(id);
                festaTexto = "A renda deles dobrou." + (E.ProximoMarco(id) > 0 ? " Próximo marco: " + E.ProximoMarco(id) + "." : " Todos os marcos!");
            };
            E.Promoveu += c => { vistaEscolhida = SalaIso.Vista.Mundo; festaDesde = Time.unscaledTime; festaTitulo = "Promovido!"; festaCargo = E.CargoAtual.Nome; festaTexto = "A sala cresceu. Novos setores e equipamentos na loja."; };
            E.Ipo += () => { festaDesde = Time.unscaledTime; festaTitulo = "IPO!"; festaCargo = "A empresa está na bolsa"; festaTexto = "De técnico de TI num armário a CTO de uma nuvem global."; };
            E.Vendeu += c => { vistaEscolhida = SalaIso.Vista.Mundo; festaDesde = Time.unscaledTime; festaTitulo = "Vendida!"; festaCargo = "+" + c + " certificações"; festaTexto = "Uma empresa nova começa no armário, com os bônus."; };
            E.ChamadoApareceu += c => { if (c.prioridade <= 2) { Notificar("Chamado P" + c.prioridade + ": " + c.texto + "! Atenda no painel do help desk, à esquerda.", 6); if (c.prioridade == 1) Sons.Alerta(); } };
            E.EventoComecou += AoComecarEvento;
            E.EventoTerminou += AoTerminarEvento;
            E.AtaqueBloqueado += (def, quem) => Notificar(quem + " bloqueou um ataque de " + def.Nome.ToLower() + ".", 4);
            E.HardwareEnvelheceu += garantia => Notificar(garantia ? "A garantia dos servidores venceu: vão travar 2x mais. Faça o refresh no cartão à direita." : "Servidores no fim da vida: travam 3x mais. Hora do refresh!", 8);
            E.ChamadoEncerrado += (c, bonus, equipe) =>
            {
                if (bonus > 0) return;
                if (c.prioridade == 1) Notificar("O P1 \"" + c.texto + "\" estourou o prazo: o uptime sentiu.", 6);
                else if (c.prioridade == 2) Notificar("Um P2 foi embora sem resposta.");
            };
        }

        void TomarCafe(Vector2 pos)
        {
            if (faixa.TomarCafe()) Flutuar("Café! Receita ×2", pos, IsoGui.Cor("ffd65c"));
            else Flutuar(E.CafeAtivo ? "O café ainda faz efeito" : "Café em " + Numero(Mathf.Ceil((float)E.RecargaDoCafe)) + "s", pos, IsoGui.Muted);
        }

        void AoComecarEvento(EventoDef def)
        {
            switch (def.Id)
            {
                case Catalogo.EventoCliente: Notificar("Um cliente grande quer pagar o dobro por 90 s! Aceite no cartão à direita, mas nada pode travar.", 7); break;
                case Catalogo.EventoAuditoria: Notificar("Auditoria em 60 s! Deixe tudo funcionando para ganhar um bônus.", 7); break;
                case Catalogo.EventoInternet: Notificar("A internet do bairro caiu: renda pela metade por 60 s. Ligue o 4G do celular!", 7); break;
                case Catalogo.EventoBlackFriday: Notificar("Um app de cliente viralizou! Renda x1,5 por 2 minutos.", 6); break;
                case Catalogo.EventoCafeAcabou: Notificar("O café acabou e o técnico ficou lento. Compre café no cartão à direita.", 7); break;
                case Catalogo.AtaquePhishing: Notificar((E.TemEstagiario ? "O estagiário" : "Alguém") + " clicou num link de phishing: servidores travaram! Compre segurança na loja.", 7); break;
                case Catalogo.AtaqueMalware: Notificar("Malware na rede: renda a 70%. Clique em Limpar no cartão à direita.", 7); break;
                case Catalogo.AtaqueDdos: Notificar("Ataque DDoS: tráfego falso derrubando a renda. Bloqueie os IPs!", 7); break;
                case Catalogo.AtaqueRansomware: Notificar(E.RestauraRansomware ? "Ransomware criptografou os dados! Restaure do backup no cartão à direita." : "Ransomware criptografou os dados! Sem backup em fita, é pagar o resgate ou esperar.", 8); break;
                case Catalogo.EventoSsl: Notificar("O certificado SSL expirou: o site está com cadeado vermelho e os clientes fugindo. Renove!", 7); break;
                case Catalogo.EventoDns: Notificar("Ninguém acha o site. Não é a rede, não é o servidor... é sempre o DNS.", 7); break;
                case Catalogo.EventoFaxineira: Notificar("A faxineira desligou o rack da tomada para ligar o aspirador. Religue!", 7); break;
                case Catalogo.EventoRato: Notificar("Um rato roeu o cabo de rede: um servidor ficou fora do ar.", 6); break;
                case Catalogo.EventoDeploySexta: Notificar("Sexta-feira, 18h, alguém quer subir uma versão nova. Arrisca o deploy?", 7); break;
                case Catalogo.EventoReuniao: Notificar("O time está numa reunião que podia ser um e-mail: tudo conserta mais devagar.", 7); break;
            }
            Sons.Alerta();
        }

        void AoTerminarEvento(EventoDef def, double valor)
        {
            if (def.Id == Catalogo.EventoAuditoria)
                Notificar(valor > 0 ? "Auditoria aprovada: +" + Dinheiro(valor) : "Auditoria achou problemas: multa de " + Dinheiro(-valor), 6);
            else if (def.Id == Catalogo.EventoCliente && valor < 0)
                Notificar("Algo travou: o cliente cancelou e cobrou " + Dinheiro(-valor) + " de multa.", 6);
            else if (def.Id == Catalogo.EventoCafeAcabou) Notificar("Café novo na mesa: o técnico voltou ao normal.", 4);
            else if (def.Id == Catalogo.AtaqueRansomware) Notificar(valor < 0 ? "Resgate pago: " + Dinheiro(-valor) + ". Backup em fita evita isso." : "Dados de volta: o ransomware acabou.", 6);
            else if (def.Id == Catalogo.EventoDeploySexta)
                Notificar(valor > 0 ? "O deploy de sexta deu certo: +" + Dinheiro(valor) + ". Desta vez." : E.DeployQuebrado ? "Deu ruim: o deploy de sexta quebrou. Rollback!" : "O deploy ficou para segunda. Sábado em paz.", 6);
            if (valor > 0) Sons.Promocao();
        }

        void AtenderChamado(Vector2 pos, int indice = -1)
        {
            double bonus = faixa.AtenderChamado(indice);
            if (bonus > 0) Flutuar("+" + Dinheiro(bonus), pos, IsoGui.Cor("ffd65c"));
        }

        /// <summary>Confere se a dica atual já foi cumprida (algumas avançam sozinhas).</summary>
        void AvancarDicas()
        {
            if (E.TutorialConcluido) return;
            if (E.Cargo > 0) { E.AvancarTutorial(Catalogo.PassosTutorial); return; }
            switch (E.PassoTutorial)
            {
                case 0: if (E.Estado.jaClicouNoServidor) E.AvancarTutorial(1); break;
                case 1: if (E.Nivel(Catalogo.Ssd) > 0) E.AvancarTutorial(2); break;
                case 2: if (E.CafeAtivo || !E.PodeTomarCafe) E.AvancarTutorial(3); break;
                case 3: if (E.Torres >= 2) E.AvancarTutorial(4); break;
            }
        }

        /// <summary>Caixa da dica embaixo da meta, com uma seta pulando sobre o que deve ser clicado. Retorna se está aparecendo.</summary>
        bool Tutorial()
        {
            AvancarDicas();
            if (E.TutorialConcluido || !Livre) return false;
            int passo = E.PassoTutorial;

            var caixa = new Rect(230, 138, 890, 52);
            ui.Caixa(caixa, IsoGui.Cor("1d2a14"), IsoGui.Cor("ffd65c"));
            ui.Texto("Dica " + (passo + 1) + "/" + Catalogo.PassosTutorial, caixa.x + 14, caixa.y + 8, Ouro, 2);
            ui.Texto(Dicas[passo], caixa.x + 14, caixa.y + 28, IsoGui.Branco, 2);
            bool informativa = passo >= 4;
            if (informativa && ui.Botao(new Rect(caixa.xMax - 96, caixa.y + 10, 84, 32), "OK", IsoGui.Verde)) E.AvancarTutorial(passo + 1);
            if (!informativa && ui.Botao(new Rect(caixa.xMax - 96, caixa.y + 10, 84, 32), "Pular", IsoGui.Borda)) E.AvancarTutorial(Catalogo.PassosTutorial);

            // alvo da seta
            Vector2? alvo = null;
            switch (passo)
            {
                case 0: alvo = TopoDoAlvo("servidor:0"); break;
                case 1: alvo = new Vector2(W / 2 - (E.HostsContainers > 0 ? 2 : 1.5f) * 230 - (E.HostsContainers > 0 ? 27 : 18) + 115, 812); break;   // LOJA
                case 2: alvo = TopoDoAlvo("cafe"); break;
                case 3: if (salaIso.Marcador.HasValue) alvo = NaTela(salaIso.Marcador.Value) + new Vector2(0, 10); break;
                case 4: alvo = new Vector2(W / 2 + 200, 56); break;     // meta no topo
                case 5: alvo = new Vector2(115, 828); break;      // botão FAIXA
            }
            if (alvo.HasValue) Seta(alvo.Value);
            return true;
        }

        Vector2? TopoDoAlvo(string tipo)
        {
            foreach (var a in salaIso.Alvos)
                if (a.Tipo == tipo) return NaTela(new Vector2Int(a.Area.x + a.Area.width / 2, a.Area.y));
            return null;
        }

        /// <summary>Seta amarela apontando para baixo, pulando sobre o ponto.</summary>
        void Seta(Vector2 ponta)
        {
            float pulo = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5)) * 8;
            float y = ponta.y - 8 - pulo;
            var cor = IsoGui.Cor("ffd65c");
            for (int i = 0; i < 8; i++)
                ui.Ret(new Rect(ponta.x - 8 + i, y - 8 + i, 16 - i * 2, 2), cor);
            ui.Ret(new Rect(ponta.x - 3, y - 22, 6, 14), cor);
        }

        /// <summary>Promoção: faixa grande no meio da sala e confete caindo por 4 segundos.</summary>
        void Festa()
        {
            float idade = Time.unscaledTime - festaDesde;
            if (idade > 4f) return;
            string[] cores = { "ffd65c", "5cff8a", "5cc8ff", "ff8cc6", "b48cff", "ffa53c" };
            for (int i = 0; i < 90; i++)
            {
                float x = ((i * 97) % 100) / 100f * W;
                float y = ((idade * (90 + (i * 37) % 120)) + (i * 53) % 300) % H;
                ui.Ret(new Rect(x + Mathf.Sin(idade * 3 + i) * 6, y, 4, 4), IsoGui.Cor(cores[i % cores.Length]));
            }
            var r = new Rect(W / 2 - 320, 400, 640, 130);
            ui.Caixa(r, IsoGui.Cor("2a1f10"), IsoGui.Cor("ffd65c"));
            ui.Texto(festaTitulo, r.center.x, r.y + 22, IsoGui.Cor("ffd65c"), 6, true);
            ui.Texto(festaCargo, r.center.x, r.y + 64, IsoGui.Branco, 3, true);
            ui.Texto(festaTexto, r.center.x, r.y + 94, IsoGui.Muted, 2, true);
        }
    }
}
