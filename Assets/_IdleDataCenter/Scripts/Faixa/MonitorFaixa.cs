using System;
using System.Collections.Generic;
using IdleDataCenter.Gerente;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// A faixa acima da barra de tarefas: um monitor discreto do data center, no estilo do modo gerente.
    /// À esquerda o dinheiro e a meta; no meio uma janela para a sala isométrica (a câmera passeia devagar);
    /// à direita o que pede atenção agora (incidente, chamado, promoção) ou a próxima compra; e os ícones.
    /// Para jogar de verdade, clicar na sala (ou no ◇) abre o modo gerente.
    ///
    /// Desenha em OnGUI numa grade de "unidades": a faixa tem 128 unidades de altura (64 pixels de arte),
    /// então cada unidade vale Escala / 2 pixels de tela. Os cliques não usam o GUI.Button: a janela
    /// atravessa cliques nas áreas vazias, então a faixa descobre sozinha o que está sob o cursor.
    /// </summary>
    public class MonitorFaixa : MonoBehaviour
    {
        const float Altura = 128, Margem = 16;
        const float LarguraHud = 216, LarguraLado = 196, LarguraIcones = 26, JanelaPadrao = 520, JanelaMinima = 160;
        const float IntervaloSala = 1 / 15f;

        static readonly Color Ouro = IsoGui.Cor("ffd65c"), Vermelho = IsoGui.Cor("ff5a6a"), Energia = IsoGui.Cor("ffbf3f"),
                              Frio = IsoGui.Cor("5aa9ff"), FundoSala = IsoGui.Cor("0c1424");

        static readonly string[] Moeda = { ".###.", "##.##", "#.#.#", "##.##", ".###." },
            Raio = { "..#..", ".##..", "#####", "..##.", "..#.." }, Termometro = { ".#.", ".#.", ".#.", "###", "###" },
            Rede = { "....#", "...##", "..###", ".####", "#####" }, Losango = { "..#..", ".#.#.", "#.#.#", ".#.#.", "..#.." },
            Triangulo = { "..#..", ".###.", "#####", ".....", "#####" }, Baixo = { ".....", "#...#", ".#.#.", "..#..", "....." },
            Xis = { "#...#", ".#.#.", "..#..", ".#.#.", "#...#" };

        Faixa faixa;
        Economia E;
        JanelaDesktop janela;
        IsoGui ui;
        SalaIso sala;
        float proximaSala;

        string aviso = "";
        Color corAviso;
        float avisoAte;

        // Alvos de clique montados no último desenho (em unidades), e o cursor na mesma grade
        readonly List<(Rect area, Action acao)> alvos = new List<(Rect, Action)>();
        Rect areaTotal;
        Vector2 cursor = new Vector2(-1, -1);
        float larguraJanela = JanelaPadrao;

        public bool Visivel { get; set; } = true;

        /// <summary>Escala para testes de tela estreita (pixels de arte da tela); 0 = a tela de verdade.</summary>
        public float LarguraSimulada { get; set; }

        public void Iniciar(Faixa faixa, Economia economia, JanelaDesktop janela)
        {
            this.faixa = faixa;
            E = economia;
            this.janela = janela;
            ui = new IsoGui();
            sala = new SalaIso(E) { MostrarExpansao = false };
        }

        void OnDestroy() => ui?.Dispose();

        public void Avisar(string texto, float segundos, Color? cor = null)
        {
            aviso = texto;
            corAviso = cor ?? Ouro;
            avisoAte = Time.unscaledTime + segundos;
        }

        float Unidade => janela.Escala / 2f;   // pixels de tela por unidade

        /// <summary>Largura da tela em unidades.</summary>
        float LarguraTela => LarguraSimulada > 0 ? LarguraSimulada * 2 : Screen.width / Unidade;

        /// <summary>Canto de cima à esquerda do monitor, em pixels de tela (coordenadas do GUI).</summary>
        Vector2 Origem => new Vector2(
            Ajustes.Direita ? Mathf.Floor(Screen.width - (areaTotal.width + Margem) * Unidade) : Margem * Unidade,
            Screen.height - Altura * Unidade);

        /// <summary>O cursor (coordenadas de tela do Unity, origem embaixo) está sobre alguma parte do monitor?</summary>
        public bool Contem(Vector2 posicaoTela)
        {
            if (!Visivel) return false;
            var o = Origem;
            var u = new Vector2((posicaoTela.x - o.x) / Unidade, (Screen.height - posicaoTela.y - o.y) / Unidade);
            return areaTotal.Contains(u);
        }

        /// <summary>Atualiza o cursor e, num clique, executa o alvo sob ele. Retorna se o clique foi do monitor.</summary>
        public bool Processar(Vector2 posicaoTela, bool sobreAJanela, bool clicou)
        {
            if (!Visivel || !sobreAJanela) { cursor = new Vector2(-1, -1); return false; }
            var o = Origem;
            cursor = new Vector2((posicaoTela.x - o.x) / Unidade, (Screen.height - posicaoTela.y - o.y) / Unidade);
            if (!clicou) return false;
            for (int i = alvos.Count - 1; i >= 0; i--)
                if (alvos[i].area.Contains(cursor))
                {
                    Sons.Tique();
                    alvos[i].acao();
                    return true;
                }
            return areaTotal.Contains(cursor);
        }

        void Update()
        {
            if (!Visivel) return;
            if (Time.unscaledTime >= proximaSala)
            {
                proximaSala = Time.unscaledTime + IntervaloSala;
                sala.Desenhar(Time.unscaledTime);
            }
        }

        // ---------------- Desenho ----------------

        void OnGUI()
        {
            if (!Visivel || ui == null || Event.current.type != EventType.Repaint) return;
            alvos.Clear();

            // largura: a janela da sala encolhe em telas estreitas e some se nem assim couber
            float fixo = 4 + LarguraHud + 12 + 10 + LarguraLado + 8 + LarguraIcones + 4;
            float livre = LarguraTela - Margem * 2 - fixo;
            larguraJanela = livre >= JanelaMinima ? Mathf.Min(JanelaPadrao, livre) : 0;
            float largura = fixo + larguraJanela - (larguraJanela > 0 ? 0 : 12);
            areaTotal = new Rect(0, 0, largura, Altura);

            var anterior = GUI.matrix;
            var o = Origem;
            GUI.matrix = Matrix4x4.TRS(new Vector3(o.x, o.y, 0), Quaternion.identity, new Vector3(Unidade, Unidade, 1));

            float x = 4;
            Hud(x);
            x += LarguraHud + 12;
            if (larguraJanela > 0) { Sala(x, larguraJanela); x += larguraJanela + 10; }
            Lado(x);
            x += LarguraLado + 8;
            Icones(x);

            GUI.matrix = anterior;
        }

        bool Pisca => Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0;

        void Hud(float x)
        {
            ui.Caixa(new Rect(x, 6, LarguraHud, 62));
            ui.Texto(E.CafeAtivo ? "Dinheiro · café ×2" : "Dinheiro", x + 12, 14, E.CafeAtivo ? Ouro : IsoGui.Muted, 1);
            Mapa(Moeda, x + 12, 26, Ouro, 3);
            ui.Texto("R$ " + Faixa.Formatar(E.Dinheiro), x + 34, 26, IsoGui.Branco, 3);
            string receita = "+R$ " + Faixa.Formatar(E.ReceitaPorSegundo) + "/s";
            ui.Texto(receita, x + LarguraHud - 12 - ui.Largura(receita, 2), 52, IsoGui.Verde, 2);

            // meta: a mais perto de ser cumprida; com tudo pronto, a caixa vira o botão da promoção
            var r = new Rect(x, 74, LarguraHud, 48);
            var metas = E.CargoAtual.MetasParaPromocao;
            if (E.PodePromover || E.PodeFazerIpo)
            {
                var cor = Pisca ? Ouro : IsoGui.Laranja;
                Botao(r, "", cor, E.PodePromover ? (Action)faixa.Promover : faixa.FazerIpo);
                ui.Texto(E.PodePromover ? "Metas cumpridas" : "Tudo pronto", x + 12, 82, IsoGui.Muted, 1);
                ui.Texto(E.PodePromover ? "Ser promovido!" : "Abrir o capital!", x + LarguraHud / 2, 98, IsoGui.Branco, 2, true);
                return;
            }
            ui.Caixa(r);
            if (metas.Length == 0)
            {
                ui.Texto(E.IpoFeito ? "Empresa na bolsa" : "Carreira", x + 12, 82, IsoGui.Muted, 1);
                ui.Texto(E.CargoAtual.Nome, x + 12, 94, Ouro, 2);
                return;
            }
            int feitas = 0;
            MetaDef proxima = null;
            foreach (var m in metas)
            {
                if (E.Cumprida(m)) { feitas++; continue; }
                if (proxima == null || E.Progresso(m) / m.Alvo > E.Progresso(proxima) / proxima.Alvo) proxima = m;
            }
            if (proxima == null)
            {
                ui.Texto("Metas cumpridas", x + 12, 82, IsoGui.Muted, 1);
                ui.Texto(E.IpoFeito ? "Empresa na bolsa" : E.CargoAtual.Nome, x + 12, 94, Ouro, 2);
                return;
            }
            ui.Texto(E.TemProximoCargo ? "Meta para " + Catalogo.Cargos[E.Cargo + 1].Nome : "Meta para o IPO", x + 12, 82, IsoGui.Muted, 1);
            ui.Texto(Cortar(proxima.Texto, 20), x + 12, 92, IsoGui.Branco, 2);
            string fracao = feitas + "/" + metas.Length;
            ui.Texto(fracao, x + LarguraHud - 12 - ui.Largura(fracao, 2), 92, IsoGui.Cyan, 2);
            ui.Barra(new Rect(x + 12, 106, LarguraHud - 24, 10), E.Progresso(proxima) / proxima.Alvo, IsoGui.Cyan);
        }

        void Sala(float x, float largura)
        {
            var r = new Rect(x, 6, largura, 116);
            ui.Caixa(r, FundoSala);
            var tex = sala.Textura;
            if (tex != null)
            {
                // a câmera vai e volta pela sala, devagar; salas menores que a janela ficam centralizadas
                var area = sala.AreaDaSala;
                float vw = largura - 4, vh = 112;
                float folgaX = area.width - vw, folgaY = area.height - vh;
                float vai = (Mathf.Sin(Time.unscaledTime * 0.2f) + 1) / 2;
                float sx = folgaX > 0 ? area.x + Mathf.Round(folgaX * vai) : area.x + folgaX / 2;
                float sy = folgaY > 0 ? area.y + Mathf.Round(folgaY * 0.55f) : area.y + folgaY / 2;
                // recorte da textura (origem em cima) que cabe na janela
                float x0 = Mathf.Max(sx, 0), y0 = Mathf.Max(sy, 0);
                float x1 = Mathf.Min(sx + vw, tex.width), y1 = Mathf.Min(sy + vh, tex.height);
                if (x1 > x0 && y1 > y0)
                {
                    var destino = new Rect(r.x + 2 + (x0 - sx), r.y + 2 + (y0 - sy), x1 - x0, y1 - y0);
                    var uv = new Rect(x0 / tex.width, 1 - y1 / tex.height, (x1 - x0) / tex.width, (y1 - y0) / tex.height);
                    GUI.DrawTextureWithTexCoords(destino, tex, uv);
                }
            }

            // medidores no canto de cima, sobre uma tarja escura
            ui.Ret(new Rect(r.x + 2, r.y + 2, E.NaSalaDeRacks ? 186 : 126, 18), new Color(0.05f, 0.08f, 0.14f, 0.8f));
            float mx = r.x + 10;
            Medidor(ref mx, r.y + 6, Raio, Energia, E.Sobrecarga, Mathf.RoundToInt((float)(E.ConsumoKw / Math.Max(0.001, E.CapacidadeKw) * 100)) + "%");
            Medidor(ref mx, r.y + 6, Termometro, Frio, E.Quente, Mathf.RoundToInt((float)E.Temperatura) + "°C");
            if (E.NaSalaDeRacks)
                Medidor(ref mx, r.y + 6, Rede, IsoGui.Roxo, E.LinkSaturado, Mathf.RoundToInt((float)(E.TrafegoMbps / Math.Max(0.001, E.BandaMbps) * 100)) + "%");
            // uptime da última hora no canto (verde quando já paga SLA)
            string uptime = "Uptime " + Economia.FormatarUptime(E.Uptime);
            ui.Texto(uptime, r.xMax - 10 - ui.Largura(uptime, 1), r.y + 8, E.Sla.HasValue ? IsoGui.Verde : IsoGui.Cyan, 1);

            // aviso na parte de baixo da janela (ou a dica de que clicar abre o gerente)
            bool sobre = r.Contains(cursor);
            if (Time.unscaledTime < avisoAte || sobre)
            {
                string texto = Time.unscaledTime < avisoAte ? aviso : "Abrir o modo gerente";
                var faixaAviso = new Rect(r.x + 2, r.yMax - 22, r.width - 4, 20);
                ui.Ret(faixaAviso, new Color(0.05f, 0.08f, 0.14f, 0.88f));
                ui.Texto(Cortar(texto, (int)((r.width - 20) / 8)), faixaAviso.center.x, faixaAviso.y + 5, Time.unscaledTime < avisoAte ? corAviso : IsoGui.Cyan, 2, true);
            }
            alvos.Add((r, faixa.AbrirGerente));
        }

        void Medidor(ref float x, float y, string[] icone, Color cor, bool alerta, string texto)
        {
            var c = alerta ? (Pisca ? Vermelho : IsoGui.Laranja) : cor;
            Mapa(icone, x, y, c, 2);
            ui.Texto(texto, x + 14, y + 2, alerta ? c : IsoGui.Branco, 1);
            x += 58;
        }

        /// <summary>O que pede atenção agora; sem nada urgente, a próxima compra.</summary>
        void Lado(float x)
        {
            var r = new Rect(x, 6, LarguraLado, 116);
            if (E.EmPico && !E.PicoFoiEscalado)
                Alerta(r, "Pico: " + E.NomeDoPico, E.PicoViolado ? "SLA violado!" : "Escale em " + Mathf.Max(0, Mathf.CeilToInt((float)(E.LimiteParaEscalar - E.SegundosDePico))) + "s", "Escalar", faixa.Escalar);
            else if (E.TemEvento && E.BotaoDoEvento != null) AlertaDoEvento(r);
            else if (E.TemQuedaDeEnergia) Alerta(r, "Queda de energia", "DC-0" + (E.Estado.quedaDc + 1) + " apagado", "Religar", faixa.Religar);
            else if (E.TemPaneRegional) Alerta(r, "Pane regional", Catalogo.NomesRegioes[E.Estado.paneRegiao], "Redirecionar", faixa.Redirecionar);
            else if (E.DeployQuebrado) Alerta(r, "Deploy quebrou", "Apps fora do ar", "Rollback", faixa.FazerRollback);
            else if (E.DiscoQueimado) Alerta(r, "Disco queimou", E.NivelStorage > 0 ? "No storage" : "HD da torre", "Trocar disco", faixa.TrocarDisco);
            else if (E.Travamentos.Count > 0)
            {
                int servidor = E.Travamentos[0].servidor;
                Alerta(r, E.Travamentos.Count == 1 ? "Servidor travou" : E.Travamentos.Count + " servidores travados", "O técnico vai consertar", "Reiniciar", () => faixa.Reiniciar(servidor));
            }
            else if (E.TemEvento) AlertaDoEvento(r);
            else if (E.ForaDaGarantia && (E.PodeFazerRefresh || E.FimDaVida))
                Alerta(r, E.FimDaVida ? "Fim de vida" : "Fora da garantia", "Travam " + E.FatorIdade + "x mais", "Refresh R$ " + Faixa.Formatar(E.CustoDoRefresh), () => { if (E.FazerRefresh()) Sons.Promocao(); });
            else if (E.TemChamado)
                Alerta(r, "Chamado urgente", E.TextoDoChamado, "Atender " + Mathf.CeilToInt((float)E.SegundosDoChamado) + "s", () => faixa.AtenderChamado(), Ouro);
            else ProximaCompra(r);
        }

        void Alerta(Rect r, string titulo, string detalhe, string botao, Action acao, Color? cor = null)
        {
            var c = cor ?? (Pisca ? IsoGui.Laranja : IsoGui.Cor("ff8a3a"));
            ui.Caixa(r, IsoGui.Cor("3a2410"), c);
            ui.Texto("!", r.center.x, r.y + 10, c, 4, true);
            ui.Texto(Cortar(titulo, 22), r.center.x, r.y + 38, c, 2, true);
            ui.Texto(Cortar(detalhe, 30), r.center.x, r.y + 58, IsoGui.Branco, 1, true);
            if (botao != null) Botao(new Rect(r.x + 14, r.y + 80, r.width - 28, 26), botao, c, acao);
        }

        void AlertaDoEvento(Rect r)
        {
            bool bom = E.Evento.Id == Catalogo.EventoCliente || E.Evento.Id == Catalogo.EventoBlackFriday;
            Alerta(r, E.Evento.Nome, E.TextoDoEvento, E.BotaoDoEvento, () => { if (E.AgirNoEvento()) Sons.Tique(); }, bom ? Ouro : (Color?)null);
        }

        void ProximaCompra(Rect r)
        {
            var def = E.MelhoriaSugerida();
            if (def == null)
            {
                ui.Caixa(r);
                ui.Texto("Tudo comprado", r.x + 14, r.y + 12, IsoGui.Muted, 1);
                ui.Texto("Neste cargo", r.x + 14, r.y + 28, IsoGui.Branco, 2);
                Botao(new Rect(r.x + 14, r.y + 80, r.width - 28, 26), "Ver metas", IsoGui.Cyan, faixa.AbrirGerente);
                return;
            }
            bool pode = E.PodeComprar(def.Id);
            ui.Caixa(r, pode ? IsoGui.Cor("173a2e") : IsoGui.Painel, pode ? IsoGui.Verde : IsoGui.Borda);
            ui.Texto("Próxima compra", r.x + 14, r.y + 12, IsoGui.Muted, 1);
            ui.Texto(Cortar(def.Nome + (def.Gerador ? " nº " + (E.UnidadesDoGerador(def.Id) + 1) : def.NivelMaximo > 1 ? " " + (E.Nivel(def.Id) + 1) + "/" + def.NivelMaximo : ""), 21), r.x + 14, r.y + 26, pode ? IsoGui.Branco : IsoGui.Muted, 2);
            ui.Texto(Cortar(def.Efeito, 30), r.x + 14, r.y + 42, IsoGui.Muted, 1);
            ui.Texto("R$ " + Faixa.Formatar(E.Custo(def.Id)), r.x + 14, r.y + 56, pode ? IsoGui.Verde : IsoGui.Muted, 2);
            var botao = new Rect(r.x + 14, r.y + 80, r.width - 28, 26);
            if (pode) Botao(botao, "Comprar", IsoGui.Verde, () => faixa.TentarComprar(def));
            else
            {
                // barra do quanto já juntou
                ui.Barra(botao, E.Dinheiro / Math.Max(1, E.Custo(def.Id)), IsoGui.Borda);
                ui.Texto("Juntando...", botao.center.x, botao.y + 9, IsoGui.Muted, 2, true);
            }
        }

        void Icones(float x)
        {
            Icone(x, 0, Losango, IsoGui.Cyan, faixa.AbrirGerente);
            Icone(x, 1, Triangulo, IsoGui.Muted, faixa.AbrirLoja);
            Icone(x, 2, Baixo, IsoGui.Muted, faixa.Ocultar);
            Icone(x, 3, Xis, IsoGui.Cor("ff7a8a"), Application.Quit);
        }

        void Icone(float x, int i, string[] mapa, Color cor, Action acao)
        {
            var r = new Rect(x, 6 + i * 30, LarguraIcones, 26);
            bool sobre = r.Contains(cursor);
            ui.Caixa(r, sobre ? Color.Lerp(cor, IsoGui.Painel, 0.6f) : IsoGui.Painel, sobre ? cor : IsoGui.Borda);
            Mapa(mapa, r.x + 8, r.y + 8, sobre ? IsoGui.Branco : cor, 2);
            alvos.Add((r, acao));
        }

        void Botao(Rect r, string titulo, Color cor, Action acao)
        {
            bool sobre = r.Contains(cursor);
            ui.Caixa(r, Color.Lerp(cor, IsoGui.Painel, sobre ? 0.45f : 0.72f), cor);
            if (titulo.Length > 0) ui.Texto(titulo, r.center.x, r.center.y - 5, IsoGui.Branco, 2, true);
            alvos.Add((r, acao));
        }

        void Mapa(string[] linhas, float x, float y, Color cor, int escala)
        {
            for (int j = 0; j < linhas.Length; j++)
                for (int i = 0; i < linhas[j].Length; i++)
                    if (linhas[j][i] == '#') ui.Ret(new Rect(x + i * escala, y + j * escala, escala, escala), cor);
        }

        static string Cortar(string s, int maximo) => s.Length <= maximo ? s : s.Substring(0, Mathf.Max(1, maximo - 1)) + ".";
    }
}
