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
        string festaCargo = "", festaTitulo = "PROMOVIDO!", festaTexto = "";

        static readonly string[] Dicas =
        {
            "BEM-VINDO! CLIQUE NUM SERVIDOR PARA GANHAR DINHEIRO.",
            "COMPRE O SSD: ELE DOBRA A RECEITA DAS TORRES. ESTA NA LOJA.",
            "HORA DO CAFE: CLIQUE NA CANECA DA MESA. A RECEITA DOBRA POR 30 S.",
            "CONSTRUA MAIS UM SERVIDOR NO MARCADOR LARANJA DA SALA.",
            "A META DA PROMOCAO FICA NO TOPO. CUMPRA AS TRES PARA VIRAR SYSADMIN.",
            "VAI TRABALHAR? O BOTAO FAIXA DEIXA O JOGO DISCRETO ACIMA DA BARRA DE TAREFAS.",
        };

        void IniciarPrimeiraHora()
        {
            E.Comprou += id => salaIso?.Comprou(id, Time.unscaledTime);
            E.Promoveu += c => { vistaEscolhida = SalaIso.Vista.Mundo; festaDesde = Time.unscaledTime; festaTitulo = "PROMOVIDO!"; festaCargo = E.CargoAtual.Nome.ToUpperInvariant(); festaTexto = "A SALA CRESCEU. NOVOS SETORES E EQUIPAMENTOS NA LOJA."; };
            E.Ipo += () => { festaDesde = Time.unscaledTime; festaTitulo = "IPO!"; festaCargo = "A EMPRESA ESTA NA BOLSA"; festaTexto = "DE TECNICO DE TI NUM ARMARIO A CTO DE UMA NUVEM GLOBAL."; };
            E.Vendeu += c => { vistaEscolhida = SalaIso.Vista.Mundo; festaDesde = Time.unscaledTime; festaTitulo = "VENDIDA!"; festaCargo = "+" + c + " CERTIFICACOES"; festaTexto = "UMA EMPRESA NOVA COMECA NO ARMARIO, COM OS BONUS."; };
            E.ChamadoApareceu += texto => Notificar("Chamado urgente: " + texto + "! Clique no papel sobre a mesa.", 8);
            E.ChamadoEncerrado += bonus => { if (bonus <= 0) Notificar("O chamado foi embora sem resposta."); };
        }

        void TomarCafe(Vector2 pos)
        {
            if (faixa.TomarCafe()) Flutuar("CAFE! RECEITA X2", pos, IsoGui.Cor("ffd65c"));
            else Flutuar(E.CafeAtivo ? "O CAFE AINDA FAZ EFEITO" : "CAFE EM " + Numero(Mathf.Ceil((float)E.RecargaDoCafe)) + "S", pos, IsoGui.Muted);
        }

        void AtenderChamado(Vector2 pos)
        {
            double bonus = faixa.AtenderChamado();
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
            ui.Texto("DICA " + (passo + 1) + "/" + Catalogo.PassosTutorial, caixa.x + 14, caixa.y + 8, Ouro, 2);
            ui.Texto(Dicas[passo], caixa.x + 14, caixa.y + 28, IsoGui.Branco, 2);
            bool informativa = passo >= 4;
            if (informativa && ui.Botao(new Rect(caixa.xMax - 96, caixa.y + 10, 84, 32), "OK", IsoGui.Verde)) E.AvancarTutorial(passo + 1);
            if (!informativa && ui.Botao(new Rect(caixa.xMax - 96, caixa.y + 10, 84, 32), "PULAR", IsoGui.Borda)) E.AvancarTutorial(Catalogo.PassosTutorial);

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
