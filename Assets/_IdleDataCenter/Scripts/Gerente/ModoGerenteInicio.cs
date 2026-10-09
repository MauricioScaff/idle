using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A tela inicial: a cidade à noite ao fundo, o emblema do jogo (PixelLab) e o nome, com Continuar (mostra o cargo e o
    /// dinheiro do save), Novo jogo (pede confirmação e guarda uma cópia do save), Configurações e Sair. Aparece quando o
    /// jogo abre no modo gerente; no meio do jogo, o Esc (sem janela aberta) ou o botão de menu a trazem de volta.
    /// </summary>
    public partial class ModoGerente
    {
        enum TelaDoMenu { Nenhuma, Inicio, ConfirmarNovo, Configuracoes, Dicionario, Creditos }
        TelaDoMenu menu = TelaDoMenu.Nenhuma;
        /// <summary>A versão no canto da tela inicial (a mesma do itch e da release do GitHub).</summary>
        const string Versao = "v0.9.6";

        bool menuDoInicio;          // aberta ao abrir o jogo (o botão diz "Continuar"; no meio do jogo, "Voltar ao jogo")
        double ganhoAoAbrir;
        Texture2D emblema;

        public bool NaTelaInicial => menu != TelaDoMenu.Nenhuma;

        /// <summary>Abre a tela inicial (ganho: quanto rendeu com o jogo fechado, para mostrar no Continuar).</summary>
        public void MostrarTelaInicial(double ganho = 0, bool depoisVaiParaAFaixa = false)
        {
            menu = TelaDoMenu.Inicio;
            menuDoInicio = true;
            ganhoAoAbrir = ganho;
            voltarParaAFaixa = depoisVaiParaAFaixa;
        }

        bool voltarParaAFaixa;

        /// <summary>Continuar: fecha o menu e, se o jogador usava a faixa, volta para ela.</summary>
        void Continuar()
        {
            FecharMenu();
            if (voltarParaAFaixa) { voltarParaAFaixa = false; faixa.FecharGerente(); }
        }

        void AbrirMenu() { menu = TelaDoMenu.Inicio; menuDoInicio = false; }

        void FecharMenu() { menu = TelaDoMenu.Nenhuma; }

        /// <summary>Esc: com o menu aberto, volta um passo (ou fecha); sem janela aberta no jogo, abre o menu.</summary>
        bool EscNoMenu()
        {
            if (!Input.GetKeyDown(KeyCode.Escape) || terminalAberto) return false;
            if (menu == TelaDoMenu.ConfirmarNovo || menu == TelaDoMenu.Configuracoes || menu == TelaDoMenu.Dicionario || menu == TelaDoMenu.Creditos) { menu = TelaDoMenu.Inicio; return true; }
            if (menu == TelaDoMenu.Inicio) { Continuar(); return true; }
            if (string.IsNullOrEmpty(janela)) { AbrirMenu(); return true; }
            return false;
        }

        /// <summary>Desenha o menu por cima de tudo. Retorna true se ele está aberto (o resto da tela não recebe cliques).</summary>
        bool TelaInicial()
        {
            if (menu == TelaDoMenu.Nenhuma) return false;
            ui.Dicionario = false;   // no menu nada fica sublinhado
            // no começo do jogo, só a cidade ao fundo; no meio do jogo, a sala aparece escurecida atrás
            ui.Ret(new Rect(-200, -200, W + 400, H + 400), new Color(.02f, .03f, .07f, menuDoInicio ? .35f : .7f));
            if (menu == TelaDoMenu.Dicionario) { Dicionario(); return true; }   // a tela inteira (sem o emblema)
            if (menu == TelaDoMenu.Creditos) { Creditos(() => menu = TelaDoMenu.Inicio); return true; }
            Emblema(new Vector2(W / 2, 138));
            ui.Texto("IDLE DATA CENTER", W / 2, 282, Ouro, 7, true);
            ui.Texto("De freelancer no quarto a CEO de uma nuvem global", W / 2, 340, IsoGui.Muted, 2, true);

            switch (menu)
            {
                case TelaDoMenu.Inicio: MenuPrincipal(); break;
                case TelaDoMenu.ConfirmarNovo: ConfirmarNovoJogo(); break;
                case TelaDoMenu.Configuracoes: Configuracoes(); break;
            }
            ui.Texto(Versao, W - 24 - ui.Largura(Versao, 2), H - 24, IsoGui.Borda, 2);
            return true;
        }

        void Emblema(Vector2 centro)
        {
            if (emblema == null) emblema = Resources.Load<Texture2D>("Arte/logo");
            if (emblema == null) return;
            float lado = 2 * emblema.width;   // pixel art em escala inteira
            float bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 3;
            GUI.DrawTexture(new Rect(Mathf.Round(centro.x - lado / 2), Mathf.Round(centro.y - lado / 2 + bob), lado, lado), emblema);
        }

        void MenuPrincipal()
        {
            const float L = 380, A = 56;
            float x = W / 2 - L / 2, y = 392;
            bool temJogo = E.Estado.totalGanho > 0 || E.Cargo > Catalogo.CargoFreelancer;

            if (ui.Botao(new Rect(x, y, L, A), menuDoInicio ? "Continuar" : "Voltar ao jogo", IsoGui.Verde, true, 3)) Continuar();
            if (temJogo)
            {
                string resumo = E.NomeDoCargo + " · " + Dinheiro(E.Dinheiro);
                if (menuDoInicio && ganhoAoAbrir > 0) resumo += " · +" + Dinheiro(ganhoAoAbrir) + " enquanto fora";
                ui.Texto(resumo, W / 2, y + A + 8, IsoGui.Muted, 2, true);
            }
            y += A + 36;
            if (ui.Botao(new Rect(x, y, L, A), "Novo jogo", IsoGui.Cyan, true, 3))
            {
                if (temJogo) menu = TelaDoMenu.ConfirmarNovo; else FecharMenu();
            }
            y += A + 18;
            if (ui.Botao(new Rect(x, y, L, A), "Configurações", IsoGui.Roxo, true, 3)) menu = TelaDoMenu.Configuracoes;
            y += A + 18;
            if (ui.Botao(new Rect(x, y, L, A), "Dicionário", IsoGui.Cor("5899ff"), true, 3)) AbrirDicionario();
            y += A + 18;
            if (ui.Botao(new Rect(x, y, L, A), "Créditos", IsoGui.Borda, true, 3)) menu = TelaDoMenu.Creditos;
            y += A + 18;
            if (ui.Botao(new Rect(x, y, L, A), "Sair", IsoGui.Borda, true, 3))
            {
                Salvamento.Salvar(E.Estado);
                Application.Quit();
            }
        }

        void ConfirmarNovoJogo()
        {
            var caixa = new Rect(W / 2 - 330, 390, 660, 250);
            ui.Caixa(caixa, IsoGui.Cor("1d1420"), Vermelho);
            ui.Texto("Começar do zero?", W / 2, caixa.y + 28, IsoGui.Branco, 4, true);
            ui.Texto("Você volta a ser freelancer, sem dinheiro nem certificações.", W / 2, caixa.y + 82, IsoGui.Muted, 2, true);
            ui.Texto("O jogo atual (" + E.NomeDoCargo + ", " + Dinheiro(E.Dinheiro) + ") fica guardado numa cópia.", W / 2, caixa.y + 108, IsoGui.Muted, 2, true);
            if (ui.Botao(new Rect(caixa.x + 40, caixa.yMax - 84, 270, 52), "Começar do zero", Vermelho, true, 3))
            {
                string copia = faixa.NovoJogo();
                vistaEscolhida = SalaIso.Vista.Sala;
                janela = "";
                FecharMenu();
                Notificar(copia != null ? "Novo jogo! O anterior ficou guardado em " + copia + "." : "Novo jogo!", 8);
            }
            if (ui.Botao(new Rect(caixa.xMax - 310, caixa.yMax - 84, 270, 52), "Voltar", IsoGui.Borda, true, 3)) menu = TelaDoMenu.Inicio;
        }

        void Configuracoes()
        {
            var caixa = new Rect(W / 2 - 360, 360, 720, 470);
            ui.Caixa(caixa, IsoGui.Painel, IsoGui.Roxo);
            ui.Texto("Configurações", caixa.x + 28, caixa.y + 22, IsoGui.Branco, 4);
            int monitores = Mathf.Max(1, JanelaDesktop.QuantidadeMonitores);
            var linhas = new (string nome, string valor, System.Action trocar, bool ativa)[]
            {
                ("Idioma", Ajustes.Idioma == 0 ? "Português" : "English", () => Ajustes.Idioma = 1 - Ajustes.Idioma, true),
                ("Som", Ajustes.Som ? "Ligado" : "Desligado", () => Ajustes.Som = !Ajustes.Som, true),
                ("Volume", Ajustes.NomesVolume[Ajustes.Volume], () => Ajustes.Volume = (Ajustes.Volume + 1) % Ajustes.NomesVolume.Length, Ajustes.Som),
                ("Som ambiente", Ajustes.Zumbido ? "Ligado" : "Desligado", () => Ajustes.Zumbido = !Ajustes.Zumbido, Ajustes.Som),
                ("Abrir o jogo", Ajustes.AbrirNoGerente ? "Na sala" : "Na faixa", () => Ajustes.AbrirNoGerente = !Ajustes.AbrirNoGerente, true),
                ("Monitor da faixa", (Ajustes.Monitor % monitores + 1) + " de " + monitores, () => Ajustes.Monitor = (Ajustes.Monitor + 1) % monitores, monitores > 1),
                ("Lado da faixa", Ajustes.Direita ? "Direita" : "Esquerda", () => Ajustes.Direita = !Ajustes.Direita, true),
                ("Esconder em tela cheia", Ajustes.EsconderEmTelaCheia ? "Sim" : "Não", () => Ajustes.EsconderEmTelaCheia = !Ajustes.EsconderEmTelaCheia, true),
            };
            float y = caixa.y + 78;
            foreach (var l in linhas)
            {
                ui.Texto(l.nome, caixa.x + 32, y + 12, l.ativa ? IsoGui.Branco : IsoGui.Borda, 2);
                if (ui.Botao(new Rect(caixa.xMax - 252, y, 220, 36), l.valor, IsoGui.Cyan, l.ativa, 2)) l.trocar();
                y += 42;
            }
            ui.Texto("A faixa se move arrastando pela alça (o pontilhado na ponta dela).", caixa.x + 32, y + 8, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(caixa.x + 32, caixa.yMax - 66, 330, 44), "Faixa de volta à barra", IsoGui.Borda, Ajustes.FaixaMovida, 2)) Ajustes.DevolverFaixa();
            if (ui.Botao(new Rect(caixa.xMax - 252, caixa.yMax - 66, 220, 44), "Voltar", IsoGui.Verde, true, 2)) menu = TelaDoMenu.Inicio;
        }

        /// <summary>O botão de menu no canto de cima, à esquerda do dinheiro (três tracinhos).</summary>
        void BotaoDoMenu()
        {
            var r = new Rect(222, 828, 48, 48);   // embaixo, ao lado do botão Faixa
            bool sobre = r.Contains(Event.current.mousePosition);
            ui.Caixa(r, sobre ? IsoGui.Cor("23314f") : IsoGui.Painel, IsoGui.Borda);
            for (int i = 0; i < 3; i++) ui.Ret(new Rect(r.x + 13, r.y + 15 + i * 8, 22, 4), sobre ? IsoGui.Branco : IsoGui.Muted);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { Sons.Tique(); AbrirMenu(); }
        }
    }
}
