using System;
using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Painel de gestão que abre acima da faixa. É desenhado quadro a quadro numa PixelCanvas
    /// (12 quadros por segundo) e tem três abas: Visão geral (cena da era + metas, incidentes,
    /// servidores e recursos), Melhorias e Carreira. Os botões são retângulos registrados a cada desenho.
    /// </summary>
    public class Painel : MonoBehaviour, IClicavel
    {
        public const int Largura = 560, Altura = 236, Espaco = 4;
        const float Fps = 12f;

        enum Aba { VisaoGeral, Melhorias, Carreira, Ajustes, Automacao }

        static readonly string[] Escada =
        {
            "Técnico de TI", "Sysadmin", "Analista de Infra", "Engenheiro DevOps", "SRE", "Arquiteto", "CTO / Fundador",
        };

        static readonly Dictionary<int, string[]> Novidades = new Dictionary<int, string[]>
        {
            [1] = new[] { "O armário vira uma salinha.", "Rack 42U com servidores 1U.", "Energia e temperatura", "passam a importar." },
            [2] = new[] { "A salinha vira sala de racks,", "com energia e ar de precisão.", "Racks cheios, storage e backup.", "A banda do link passa a importar." },
            [3] = new[] { "A sala escurece: vira", "sala virtualizada.", "Hypervisor, containers e CI.", "Deploys às vezes quebram." },
        };

        Faixa faixa;
        Economia economia;
        PixelCanvas tela;
        SpriteRenderer sr;
        BoxCollider2D colisor;
        Aba aba;
        float t, proximoDesenho, proximaAmostra;
        Vector2Int cursor = new Vector2Int(-1, -1);
        readonly List<(RectInt area, Action acao)> botoes = new List<(RectInt, Action)>();
        readonly float[] historico = new float[240];
        int cabeca;

        public bool Aberto { get; private set; }
        public int Ordem => 20;

        public void Iniciar(Faixa faixa, Economia economia, Vector2 posicao)
        {
            this.faixa = faixa;
            this.economia = economia;
            transform.position = posicao;
            tela = new PixelCanvas(Largura, Altura);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = tela.Sprite;
            sr.sortingOrder = 30;
            colisor = gameObject.AddComponent<BoxCollider2D>();
            colisor.size = new Vector2(Largura, Altura);
            colisor.offset = new Vector2(Largura / 2f, Altura / 2f);
            Mostrar(false);
        }

        void Mostrar(bool sim)
        {
            Aberto = sim;
            sr.enabled = sim;
            colisor.enabled = sim;
            if (sim) proximoDesenho = 0;
        }

        public void Abrir() => Mostrar(true);
        public void AbrirCarreira() { aba = Aba.Carreira; Mostrar(true); }

        /// <summary>Abre numa aba pelo nome ("visao", "melhorias", "carreira", "ajustes" ou "automacao"); usado nos testes.</summary>
        public void AbrirAba(string nome)
        {
            aba = nome == "melhorias" ? Aba.Melhorias : nome == "carreira" ? Aba.Carreira : nome == "ajustes" ? Aba.Ajustes
                : nome == "automacao" && economia.AutomacoesLiberadas ? Aba.Automacao : Aba.VisaoGeral;
            Mostrar(true);
        }
        public void Fechar() => Mostrar(false);

        // ---------------- Entrada ----------------

        // o painel pode estar ampliado (escala própria), então converte do mundo para pixels do painel
        Vector2Int ParaLocal(Vector2 mundo)
        {
            float s = transform.localScale.x;
            return new Vector2Int(Mathf.FloorToInt((mundo.x - transform.position.x) / s), Altura - 1 - Mathf.FloorToInt((mundo.y - transform.position.y) / s));
        }

        public void DefinirCursor(Vector2 mundo) => cursor = ParaLocal(mundo);

        public void ClicarEm(Vector2 mundo)
        {
            var p = ParaLocal(mundo);
            for (int i = botoes.Count - 1; i >= 0; i--)
                if (botoes[i].area.Contains(p)) { Sons.Tique(); botoes[i].acao(); proximoDesenho = 0; return; }
        }

        public void Clicar() { }
        public void DefinirDestaque(bool ligado) { if (!ligado) cursor = new Vector2Int(-1, -1); }

        // ---------------- Laço ----------------

        void Update()
        {
            t = Time.time;
            if (t >= proximaAmostra)
            {
                proximaAmostra = t + 0.5f;
                historico[cabeca] = (float)economia.ReceitaPorSegundo;
                cabeca = (cabeca + 1) % historico.Length;
            }
            if (!Aberto || t < proximoDesenho) return;
            proximoDesenho = t + 1f / Fps;
            Desenhar();
        }

        void Desenhar()
        {
            botoes.Clear();
            tela.SemRecorte();
            tela.Limpar(C("#1b1a2e"));
            R(1, 1, Largura - 2, Altura - 2, "#171a2e");
            BarraSuperior();
            Menu();
            switch (aba)
            {
                case Aba.VisaoGeral:
                    Cena(76, 20);
                    ColunaDireita(424);
                    LinhaDeBaixo(174);
                    break;
                case Aba.Melhorias: AbaMelhorias(); break;
                case Aba.Carreira: AbaCarreira(); break;
                case Aba.Ajustes: AbaAjustes(); break;
                case Aba.Automacao: AbaAutomacao(); break;
            }
            tela.Aplicar();
        }

        // ---------------- Atalhos de desenho ----------------

        static Color32 C(string hex) => PixelCanvas.C(hex);
        void R(int x, int y, int w, int h, string c) => tela.Ret(x, y, w, h, C(c));
        void P(int x, int y, string c) => tela.Pixel(x, y, C(c));
        void T(string s, int x, int y, string c, bool sombra = true, int escala = 1) => tela.Texto(s, x, y, C(c), sombra, escala);
        static int L(string s, int escala = 1) => PixelCanvas.LarguraTexto(s, escala);
        static string F(double v) => Faixa.Formatar(v);

        void Caixa(int x, int y, int w, int h, string titulo)
        {
            R(x, y, w, h, "#1b1a2e");
            R(x + 1, y + 1, w - 2, h - 2, "#20233d");
            if (titulo == null) return;
            R(x + 1, y + 1, w - 2, 9, "#282c4f");
            T(titulo, x + 4, y + 3, "#a9c7ff");
        }

        bool Sobre(RectInt r) => r.Contains(cursor);

        /// <summary>Botão com texto centralizado. Só registra o clique se estiver ativo.</summary>
        void Botao(RectInt r, string texto, string cor, string fundo, Action acao, bool ativo = true, int escala = 1)
        {
            bool sobre = ativo && Sobre(r);
            R(r.x, r.y, r.width, r.height, "#1b1a2e");
            R(r.x + 1, r.y + 1, r.width - 2, r.height - 2, ativo ? (sobre ? "#5258a8" : fundo) : "#2a2d48");
            if (ativo) R(r.x + 1, r.y + r.height - 2, r.width - 2, 1, "#1b1a2e");
            // sombra só em texto claro: em texto escuro sobre fundo claro ela embola as letras
            var c = C(cor);
            bool claro = (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) > 128;
            T(texto, r.x + (r.width - L(texto, escala)) / 2, r.y + (r.height - 5 * escala) / 2, ativo ? cor : "#6c7099", ativo && claro, escala);
            if (ativo) botoes.Add((r, acao));
        }

        void Barra(int x, int y, int w, int h, float valor, string cor)
        {
            R(x, y, w, h, "#171a2e");
            R(x, y, Mathf.RoundToInt(w * Mathf.Clamp01(valor)), h, cor);
        }

        // ---------------- Moldura ----------------

        void BarraSuperior()
        {
            R(1, 1, Largura - 2, 16, "#232748");
            tela.Mapa(new[] { "..ccc...", ".cyyyc..", "cyyyyycc", "cccccccc" }, 5, 6);
            T("Idle Data Center", 16, 6, "#fdf6e3");

            var chips = new List<(string valor, string rotulo, string cor)>
            {
                ("R$ " + F(economia.Dinheiro), "+" + F(economia.ReceitaPorSegundo) + "/s", "#ffd65c"),
                ($"{economia.ConsumoKw:0.0}/{economia.CapacidadeKw:0.0} kW", "Energia", economia.Sobrecarga ? "#ff3b4e" : "#ffbf3f"),
                (economia.ContagemServidores.ToString(), "Servidores", "#5cc8ff"),
                ($"{economia.Temperatura:0} C", "Temp", economia.Quente ? "#ff3b4e" : "#ff9fae"),
            };
            if (economia.NaSalaDeRacks)
                chips.Add(($"{economia.TrafegoMbps:0}/{economia.BandaMbps:0} Mb", "Banda", economia.LinkSaturado ? "#ff3b4e" : "#b48cff"));
            int x = 90;
            foreach (var (valor, rotulo, cor) in chips)
            {
                int w = Mathf.Max(L(valor), L(rotulo)) + 10;
                R(x, 3, w, 12, "#1b1d35");
                P(x + 2, 5, cor); P(x + 2, 6, cor);
                T(valor, x + 5, 4, cor, false);
                T(rotulo, x + 5, 10, "#7d82ad", false);
                x += w + 4;
            }

            // Cargo e progresso das metas
            var metas = economia.CargoAtual.MetasParaPromocao;
            int feitas = metas.Count(m => economia.Cumprida(m));
            T(economia.CargoAtual.Nome, Largura - 158, 4, "#fdf6e3", false);
            Barra(Largura - 158, 11, 100, 3, metas.Length == 0 ? 1f : feitas / (float)metas.Length, economia.PodePromover ? "#ffd65c" : "#5aa9ff");
            if (metas.Length > 0) T($"{feitas}/{metas.Length} metas", Largura - 54, 10, "#7d82ad", false);

            Botao(new RectInt(Largura - 14, 4, 10, 10), "x", "#ff7a8a", "#2a2d58", Fechar);
        }

        void Menu()
        {
            var itens = new (string nome, Aba? aba)[]
            {
                ("Visão geral", Aba.VisaoGeral), ("Melhorias", Aba.Melhorias), ("Carreira", Aba.Carreira),
                ("Ajustes", Aba.Ajustes), ("Automação", economia.AutomacoesLiberadas ? Aba.Automacao : (Aba?)null), ("Pesquisa", null),
            };
            string[][] icones =
            {
                new[] { ".#.", "###", "#.#" }, new[] { ".#.", "###", ".#." }, new[] { "###", ".#.", ".#." },
                new[] { "#.#", ".#.", "#.#" }, new[] { "#.#", ".#.", "#.#" }, new[] { ".#.", ".#.", "###" },
            };
            for (int i = 0; i < itens.Length; i++)
            {
                var r = new RectInt(3, 20 + i * 16, 70, 14);
                var (nome, alvo) = itens[i];
                bool ativo = alvo == aba;
                bool travado = alvo == null;
                R(r.x, r.y, r.width, r.height, ativo ? "#2f3a7a" : !travado && Sobre(r) ? "#2a2f55" : "#1d2038");
                if (ativo) R(r.x, r.y, 2, r.height, "#ffd65c");
                tela.Mapa(icones[i].Select(l => l.Replace('#', travado ? 'W' : 'Y')).ToArray(), 8, r.y + 5);
                T(nome, 15, r.y + 5, ativo ? "#fdf6e3" : travado ? "#4d5170" : "#a3a0bd", false);
                if (!travado) botoes.Add((r, () => aba = alvo.Value));
            }
        }

        // ---------------- Aba: Visão geral ----------------

        void ColunaDireita(int x)
        {
            // Metas da próxima promoção
            var metas = economia.CargoAtual.MetasParaPromocao;
            bool ultimo = !economia.TemProximoCargo;
            Caixa(x, 20, 132, 84, ultimo ? "Cargo máximo (por ora)" : "Promoção: " + Catalogo.Cargos[economia.Cargo + 1].Nome);
            if (ultimo)
            {
                T("Novos cargos chegam", x + 5, 36, "#a3a0bd", false);
                T("nas próximas versões.", x + 5, 44, "#a3a0bd", false);
            }
            for (int i = 0; i < metas.Length; i++)
            {
                var m = metas[i];
                int y = 34 + i * 13;
                bool ok = economia.Cumprida(m);
                R(x + 5, y, 7, 7, "#171a2e");
                if (ok) R(x + 6, y + 1, 5, 5, "#5cff8a");
                T(m.Texto, x + 16, y + 1, ok ? "#6fd36f" : "#fdf6e3", false);
                string prog = m.Tipo == TipoMeta.TotalGanho ? F(Math.Min(economia.Progresso(m), m.Alvo)) : $"{Math.Min(economia.Progresso(m), m.Alvo):0}/{m.Alvo:0}";
                if (!ok) T(prog, x + 127 - L(prog), y + 1, "#7d82ad", false);
            }
            if (!ultimo)
                Botao(new RectInt(x + 5, 76, 122, 22), economia.PodePromover ? "Ser promovido!" : "Cumpra as metas",
                    "#1b1a2e", "#ffd65c", faixa.Promover, economia.PodePromover);

            // Incidentes
            Caixa(x, 108, 132, 62, "Incidentes");
            var lista = economia.Travamentos;
            if (lista.Count == 0 && !economia.DiscoQueimado && !economia.DeployQuebrado)
            {
                T("Tudo funcionando", x + 5, 124, "#6fd36f", false);
                T($"{economia.Estado.incidentesResolvidos} resolvidos", x + 5, 134, "#7d82ad", false);
            }
            for (int i = 0; i < Math.Min(3, lista.Count); i++)
            {
                int s = lista[i].servidor, y = 121 + i * 16;
                bool piscar = Mathf.FloorToInt(t * 3) % 2 == 0;
                R(x + 5, y + 2, 3, 3, piscar ? "#ff3b4e" : "#6b1a24");
                T(NomeServidor(s), x + 11, y + 1, "#fdf6e3", false);
                T($"{economia.TempoConserto - lista[i].segundos:0}s", x + 11, y + 8, "#7d82ad", false);
                int idx = s;
                Botao(new RectInt(x + 70, y, 57, 14), "Reiniciar", "#fdf6e3", "#7a2a3a", () => faixa.Reiniciar(idx));
            }
            int linha = Math.Min(3, lista.Count);
            if (economia.DeployQuebrado && linha < 3)
            {
                int y = 121 + linha * 16;
                linha++;
                bool piscar = Mathf.FloorToInt(t * 3) % 2 == 0;
                R(x + 5, y + 2, 3, 3, piscar ? "#ff8cc6" : "#5a2a3a");
                T("Deploy quebrado", x + 11, y + 1, "#fdf6e3", false);
                T($"{economia.TempoRollback - economia.SegundosDeployQuebrado:0}s, apps fora", x + 11, y + 8, "#ff7a8a", false);
                Botao(new RectInt(x + 70, y, 57, 14), "Rollback", "#fdf6e3", "#7a2a5a", faixa.FazerRollback);
            }
            if (economia.DiscoQueimado && linha < 3)
            {
                int y = 121 + linha * 16;
                bool piscar = Mathf.FloorToInt(t * 3) % 2 == 0;
                R(x + 5, y + 2, 3, 3, piscar ? "#b48cff" : "#3a2a5a");
                T("Disco queimado", x + 11, y + 1, "#fdf6e3", false);
                T(economia.TemBackup ? $"{economia.TempoConserto - economia.SegundosDiscoQueimado:0}s, com backup" : $"{economia.TempoConserto - economia.SegundosDiscoQueimado:0}s, SEM backup",
                    x + 11, y + 8, economia.TemBackup ? "#7d82ad" : "#ff7a8a", false);
                Botao(new RectInt(x + 70, y, 57, 14), "Trocar", "#fdf6e3", "#4a2a7a", faixa.TrocarDisco);
            }
        }

        string NomeServidor(int s) => economia.EhTorre(s) ? $"Torre-{s + 1}" : $"1U-{s - economia.Torres + 1:00}";

        void LinhaDeBaixo(int y)
        {
            // Servidores (travados primeiro)
            int total = economia.TotalServidores;
            Caixa(76, y, 150, 58, $"Servidores ({economia.ContagemServidores})");
            var ordem = Enumerable.Range(0, total).OrderBy(s => economia.Travado(s) ? 0 : 1).Take(3).ToList();
            for (int i = 0; i < ordem.Count; i++)
            {
                int s = ordem[i], yy = y + 14 + i * 12;
                bool trav = economia.Travado(s);
                R(81, yy + 1, 3, 3, trav ? "#ff3b4e" : "#5cff8a");
                T(NomeServidor(s), 88, yy, "#fdf6e3", false);
                string r = trav ? "Travado" : "+" + F(economia.ReceitaDoServidor(s)) + "/s";
                T(r, 221 - L(r), yy, trav ? "#ff7a8a" : "#7d82ad", false);
            }
            int outros = economia.ContagemServidores - ordem.Count;
            if (outros > 0) T($"+{outros} outros", 88, y + 50, "#4d5170", false);

            // Recursos: receita ao longo do tempo, energia, temperatura e (na sala de racks) banda
            bool banda = economia.NaSalaDeRacks;
            int passo = banda ? 10 : 12;
            Caixa(230, y, 326, 58, "Recursos");
            T("Receita", 235, y + 14, "#a3a0bd", false);
            float max = Mathf.Max(1f, historico.Max());
            for (int i = 0; i < 238; i++)
            {
                float v = historico[(cabeca + 2 + i) % historico.Length] / max;
                P(284 + i, y + 20 - Mathf.RoundToInt(v * 8), "#5cff8a");
            }
            int yy2 = y + (banda ? 25 : 28);
            T("Energia", 235, yy2, "#a3a0bd", false);
            Barra(284, yy2 + 1, 238, 4, (float)(economia.ConsumoKw / economia.CapacidadeKw), economia.Sobrecarga ? "#ff3b4e" : "#ffbf3f");
            T($"{economia.ConsumoKw:0.0} kW", 524, yy2, "#7d82ad", false);
            yy2 += passo;
            T("Temp", 235, yy2, "#a3a0bd", false);
            Barra(284, yy2 + 1, 238, 4, (float)((economia.Temperatura - 15) / 30), economia.Quente ? "#ff3b4e" : "#5aa9ff");
            T($"{economia.Temperatura:0} C", 524, yy2, "#7d82ad", false);
            if (banda)
            {
                yy2 += passo;
                T("Banda", 235, yy2, "#a3a0bd", false);
                Barra(284, yy2 + 1, 238, 4, (float)(economia.TrafegoMbps / economia.BandaMbps), economia.LinkSaturado ? "#ff3b4e" : "#b48cff");
                T($"{economia.TrafegoMbps:0} Mb", 524, yy2, "#7d82ad", false);
            }
        }

        // ---------------- Aba: Melhorias ----------------

        void AbaMelhorias()
        {
            Caixa(76, 20, 480, 212, "Melhorias de " + economia.CargoAtual.Nome);
            var lista = economia.MelhoriasDoCargo().ToList();
            for (int i = 0; i < lista.Count; i++)
            {
                var m = lista[i];
                int y = 34 + i * 48;
                R(80, y, 472, 44, "#252947");
                int nivel = economia.Nivel(m.Id);
                T(m.Nome, 86, y + 6, "#fdf6e3", true, 2);
                T($"Nível {nivel}/{m.NivelMaximo}", 86, y + 20, "#7d82ad", false);
                T(m.Efeito, 86, y + 30, "#6fd36f", false);

                bool max = economia.NoMaximo(m.Id), req = economia.RequisitoOk(m.Id);
                if (!max)
                {
                    string custo = "R$ " + F(economia.Custo(m.Id));
                    T(custo, 450 - L(custo, 2), y + 14, economia.PodeComprar(m.Id) ? "#ffd65c" : "#7d82ad", true, 2);
                }
                string rotulo = max ? "Máximo" : !req ? "Precisa: " + Catalogo.Buscar(m.Requisito).Nome : "Comprar";
                var def = m;
                Botao(new RectInt(462, y + 10, 84, 24), rotulo, "#1b1a2e", "#6fd36f",
                    () => faixa.Loja.TentarComprar(def), !max && economia.PodeComprar(m.Id));
            }
        }

        // ---------------- Aba: Automação ----------------

        void AbaAutomacao()
        {
            Caixa(76, 20, 480, 212, $"Automação  ({economia.AutomacoesAtivas}/{Catalogo.Automacoes.Count} ativas)");

            // o que está sendo escrito agora
            R(80, 32, 472, 20, "#1d2140");
            var emEscrita = economia.AutomacaoEmEscrita;
            if (emEscrita != null)
            {
                double resta = emEscrita.Segundos * (1 - economia.ProgressoEscrita);
                T("Escrevendo: " + emEscrita.Nome, 86, 35, "#a9c7ff", false);
                string tempo = Duracao(resta) + " restantes";
                T(tempo, 546 - L(tempo), 35, "#7d82ad", false);
                Barra(86, 44, 460, 4, (float)economia.ProgressoEscrita, "#5cc8ff");
            }
            else
            {
                T("O técnico escreve um script por vez, e cada um trabalha sozinho para sempre.", 86, 35, "#7d82ad", false);
                T("Continua sendo escrito com o jogo fechado.", 86, 43, "#4d5170", false);
            }

            for (int i = 0; i < Catalogo.Automacoes.Count; i++)
            {
                var a = Catalogo.Automacoes[i];
                int y = 55 + i * 22;   // linhas compactas: cabem as 8 automações
                bool ativa = economia.TemAutomacao(a.Id), escrevendo = emEscrita == a, req = economia.RequisitoAutomacaoOk(a);
                bool cedo = economia.Cargo < a.Cargo;   // ainda não chegou no cargo que libera
                R(80, y, 472, 20, ativa ? "#223a36" : cedo ? "#1f2238" : "#252947");
                // ícone de terminal ">_"
                R(86, y + 4, 14, 12, "#171a2e");
                T(">_", 87, y + 8, ativa ? "#5cff8a" : escrevendo && Mathf.FloorToInt(t * 2) % 2 == 0 ? "#5cc8ff" : "#4d5170", false);
                T(a.Nome, 106, y + 3, ativa ? "#6fd36f" : cedo ? "#6c7099" : "#fdf6e3", true, 1);
                T(a.Descricao, 106, y + 12, cedo ? "#4d5170" : "#a3a0bd", false);

                if (ativa)
                {
                    T("Ativa", 546 - L("Ativa"), y + 8, "#5cff8a", false);
                    continue;
                }
                if (escrevendo)
                {
                    string pct = $"{economia.ProgressoEscrita * 100:0}%";
                    Barra(444, y + 9, 70, 4, (float)economia.ProgressoEscrita, "#5cc8ff");
                    T(pct, 546 - L(pct), y + 8, "#5cc8ff", false);
                    continue;
                }
                string custo = "R$ " + F(a.Custo);
                T(custo, 432 - L(custo), y + 8, !cedo && economia.Dinheiro >= a.Custo ? "#ffd65c" : "#7d82ad", false);
                string rotulo = cedo ? "Libera no " + Catalogo.Cargos[a.Cargo].Nome.Split(' ').Last()
                              : !req ? "Precisa: " + Catalogo.Buscar(a.Requisito).Nome
                              : economia.Escrevendo ? "Aguarde" : "Escrever";
                string id = a.Id;
                Botao(new RectInt(440, y + 2, 106, 16), rotulo, "#1b1a2e", "#5cc8ff", () => faixa.EscreverAutomacao(id), economia.PodeEscrever(id));
            }
        }

        static string Duracao(double s)
        {
            int total = Mathf.CeilToInt((float)s);
            return total >= 60 ? $"{total / 60} min {total % 60:00} s" : $"{total} s";
        }

        // ---------------- Aba: Ajustes ----------------

        void AbaAjustes()
        {
            Caixa(76, 20, 480, 212, "Ajustes");
            int n = Mathf.Max(1, JanelaDesktop.QuantidadeMonitores);
            var linhas = new (string rotulo, string valor, Action acao, bool ativo)[]
            {
                ("Som", Ajustes.Som ? "Ligado" : "Desligado", () => Ajustes.Som = !Ajustes.Som, true),
                ("Volume", Ajustes.NomesVolume[Ajustes.Volume], () => Ajustes.Volume = (Ajustes.Volume + 1) % Ajustes.NomesVolume.Length, Ajustes.Som),
                ("Zumbido de ventoinha", Ajustes.Zumbido ? "Ligado" : "Desligado", () => Ajustes.Zumbido = !Ajustes.Zumbido, Ajustes.Som),
                ("Monitor", $"{(Ajustes.Monitor % n) + 1} de {n}", () => Ajustes.Monitor = (Ajustes.Monitor + 1) % n, n > 1),
                ("Lado da tela", Ajustes.Direita ? "Direita" : "Esquerda", () => Ajustes.Direita = !Ajustes.Direita, true),
                ("Esconder em tela cheia", Ajustes.EsconderEmTelaCheia ? "Sim" : "Não", () => Ajustes.EsconderEmTelaCheia = !Ajustes.EsconderEmTelaCheia, true),
            };
            for (int i = 0; i < linhas.Length; i++)
            {
                var (rotulo, valor, acao, ativo) = linhas[i];
                int y = 36 + i * 26;
                R(80, y - 4, 472, 22, i % 2 == 0 ? "#232747" : "#20233d");
                T(rotulo, 90, y + 3, ativo ? "#fdf6e3" : "#6c7099", true, 1);
                Botao(new RectInt(420, y - 2, 124, 18), valor, "#1b1a2e", "#a9c7ff", acao, ativo);
            }
            T("Esconder e mostrar a faixa: " + JanelaDesktop.Atalho, 90, 200, "#7d82ad", false);
            T("O jogo continua rendendo com a faixa escondida.", 90, 210, "#4d5170", false);
        }

        // ---------------- Aba: Carreira ----------------

        void AbaCarreira()
        {
            Caixa(76, 20, 186, 212, "Carreira");
            for (int i = 0; i < Escada.Length; i++)
            {
                int y = 34 + i * 27;
                bool passado = i < economia.Cargo, atual = i == economia.Cargo;
                R(84, y, 12, 12, "#1b1a2e");
                R(85, y + 1, 10, 10, passado ? "#3f9b54" : atual ? "#ffd65c" : "#2a2d48");
                if (passado) tela.Mapa(new[] { "....y", "...y.", "y.y..", ".y..." }, 88, y + 4); // check
                if (atual) T(">", 88, y + 4, "#1b1a2e", false);
                if (i < Escada.Length - 1) R(89, y + 12, 2, 15, passado ? "#3f9b54" : "#2a2d48");
                T(Escada[i], 102, y + 4, atual ? "#fdf6e3" : passado ? "#6fd36f" : "#4d5170", atual);
                if (atual) T(Catalogo.Cargos[i].Lugar, 102, y + 12, "#7d82ad", false);
            }

            int x = 266;
            if (!economia.TemProximoCargo)
            {
                Caixa(x, 20, 290, 212, "Próximo cargo");
                T("Parabéns, " + economia.CargoAtual.Nome + "!", x + 10, 40, "#ffd65c", true, 2);
                T("Os próximos cargos chegam nas próximas versões.", x + 10, 64, "#a3a0bd", false);
                return;
            }
            var proximo = Catalogo.Cargos[economia.Cargo + 1];
            Caixa(x, 20, 290, 212, "Próximo cargo");
            T(proximo.Nome, x + 10, 36, "#ffd65c", true, 2);
            if (Novidades.TryGetValue(economia.Cargo + 1, out var linhas))
                for (int i = 0; i < linhas.Length; i++) T(linhas[i], x + 10, 54 + i * 8, "#a3a0bd", false);

            var metas = economia.CargoAtual.MetasParaPromocao;
            for (int i = 0; i < metas.Length; i++)
            {
                var m = metas[i];
                int y = 100 + i * 24;
                bool ok = economia.Cumprida(m);
                T(m.Texto, x + 10, y, ok ? "#6fd36f" : "#fdf6e3", false);
                string prog = m.Tipo == TipoMeta.TotalGanho
                    ? $"{F(Math.Min(economia.Progresso(m), m.Alvo))} / {F(m.Alvo)}"
                    : $"{Math.Min(economia.Progresso(m), m.Alvo):0} / {m.Alvo:0}";
                T(prog, x + 280 - L(prog), y, "#7d82ad", false);
                Barra(x + 10, y + 8, 270, 5, (float)(economia.Progresso(m) / m.Alvo), ok ? "#5cff8a" : "#5aa9ff");
            }
            Botao(new RectInt(x + 10, 184, 270, 38), economia.PodePromover ? "Ser promovido!" : "Cumpra as metas",
                "#1b1a2e", "#ffd65c", faixa.Promover, economia.PodePromover, 2);
        }

        // ---------------- Cena da era (arte do PixelLab) com animações por cima ----------------

        /// <summary>Onde procurar o que animar em cada cena (frações da imagem, origem em cima à esquerda).</summary>
        class ConfigCena
        {
            public string Arte;
            public Rect Terminal, Leds, Luzes, Pele;   // Terminal vazio = a cena não tem CRT
            public bool TemGato;
        }

        static readonly ConfigCena[] Cenas =
        {
            new ConfigCena { Arte = "cena_homelab", Terminal = new Rect(0, 0, 0.35f, 1), Leds = new Rect(0.72f, 0.25f, 0.28f, 0.75f), Luzes = new Rect(0.39f, 0, 0.30f, 0.47f), Pele = new Rect(0.33f, 0, 0.12f, 1), TemGato = true },
            new ConfigCena { Arte = "cena_sysadmin", Terminal = Rect.zero, Leds = new Rect(0.42f, 0.2f, 0.58f, 0.8f), Luzes = new Rect(0, 0, 0.15f, 0.6f), Pele = new Rect(0.36f, 0, 0.12f, 0.35f) },
            new ConfigCena { Arte = "cena_infra", Terminal = Rect.zero, Leds = new Rect(0.33f, 0.1f, 0.67f, 0.8f), Luzes = new Rect(0, 0.4f, 0.17f, 0.3f), Pele = new Rect(0.18f, 0.1f, 0.14f, 0.35f) },
            new ConfigCena { Arte = "cena_devops", Terminal = Rect.zero, Leds = new Rect(0, 0.1f, 0.55f, 0.4f), Luzes = new Rect(0.6f, 0.05f, 0.4f, 0.6f), Pele = new Rect(0.37f, 0.35f, 0.1f, 0.35f) },
        };

        Color32[] cenaPx;
        int cenaW, cenaH, cenaCargo = -1;
        readonly List<Vector2Int> ledsCena = new List<Vector2Int>(), luzesCena = new List<Vector2Int>();
        RectInt telaCrt;
        Color32 fundoCrt;
        Vector2Int cabecaDev, gato;
        bool cenaTemGato;

        /// <summary>
        /// Carrega a cena e acha, pela cor, onde animar: o texto verde do CRT (esquerda), os LEDs do rack
        /// (direita) e as janelas acesas da cidade (janela do meio). Assim a imagem pode ser trocada sem mexer no código.
        /// </summary>
        void CarregarCena(int cargo)
        {
            var cfg = Cenas[Mathf.Clamp(cargo, 0, Cenas.Length - 1)];
            cenaCargo = cargo;
            ledsCena.Clear();
            luzesCena.Clear();
            var t = ArteGerada.Textura(cfg.Arte);
            cenaW = t.width; cenaH = t.height;
            cenaPx = ArteGerada.PixelsDeCimaParaBaixo(t);
            PreencherBordaEscura();
            int tx0 = cenaW, ty0 = cenaH, tx1 = -1, ty1 = -1, peleY = cenaH, peleX = 0, peleN = 0;
            for (int y = 0; y < cenaH; y++)
            for (int x = 0; x < cenaW; x++)
            {
                var c = cenaPx[y * cenaW + x];
                int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float fx = x / (float)cenaW, fy = y / (float)cenaH;
                if (cfg.Terminal.Contains(new Vector2(fx, fy)) && c.g > 140 && c.r < 120 && c.b < 140)
                { tx0 = Mathf.Min(tx0, x); ty0 = Mathf.Min(ty0, y); tx1 = Mathf.Max(tx1, x); ty1 = Mathf.Max(ty1, y); }
                else if (cfg.Leds.Contains(new Vector2(fx, fy)) && max - min > 110 && (c.g > 170 || c.b > 200))
                    ledsCena.Add(new Vector2Int(x, y));
                else if (cfg.Luzes.Contains(new Vector2(fx, fy)) && ((c.r > 150 && c.g > 130 && c.b < 110) || (c.g > 150 && c.r < 120)))
                    luzesCena.Add(new Vector2Int(x, y));
                if (cfg.Pele.Contains(new Vector2(fx, fy)) && c.r > 200 && c.g > 140 && c.g < 200 && c.b > 100 && c.b < 170)
                { peleY = Mathf.Min(peleY, y); peleX += x; peleN++; }
            }
            telaCrt = tx1 < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(tx0 - 1, ty0 - 1, tx1 - tx0 + 3, ty1 - ty0 + 3);
            var contagem = new Dictionary<Color32, int>();
            for (int y = telaCrt.yMin; y < telaCrt.yMax; y++)
            for (int x = telaCrt.xMin; x < telaCrt.xMax; x++)
            {
                var c = cenaPx[y * cenaW + x];
                if (c.g > 140 || c.r + c.g + c.b > 200) continue;
                contagem[c] = contagem.TryGetValue(c, out int n) ? n + 1 : 1;
            }
            fundoCrt = contagem.Count > 0 ? contagem.OrderByDescending(k => k.Value).First().Key : C("#0b1a14");
            cabecaDev = new Vector2Int(peleN > 0 ? peleX / peleN : cenaW / 2, Mathf.Max(2, peleY - 22));
            gato = new Vector2Int(Mathf.RoundToInt(cenaW * 0.9f), 12);
            cenaTemGato = cfg.TemGato;
        }

        /// <summary>
        /// Algumas cenas geradas vêm com uma faixa quase preta na borda direita. Ela é coberta com o espelho
        /// das colunas logo ao lado (onde costumam estar os racks), para não sobrar um vão escuro no painel.
        /// </summary>
        void PreencherBordaEscura()
        {
            int faixa = 0;
            for (int x = cenaW - 1; x > cenaW / 2; x--)
            {
                long soma = 0;
                for (int y = 0; y < cenaH; y++) { var c = cenaPx[y * cenaW + x]; soma += c.r + c.g + c.b; }
                if (soma / (cenaH * 3) > 16) break;
                faixa++;
            }
            if (faixa == 0 || faixa > cenaW / 4) return;
            int borda = cenaW - faixa;
            for (int x = borda; x < cenaW; x++)
            {
                int origem = 2 * borda - 1 - x;
                for (int y = 0; y < cenaH; y++) cenaPx[y * cenaW + x] = cenaPx[y * cenaW + origem];
            }
        }

        static int Sorteio(int a, int b) => Mathf.Abs(a * 7919 + b * 104729) % 11;

        void Cena(int x0, int y0)
        {
            const int SW = 344, SH = 150;
            if (cenaPx == null || cenaCargo != economia.Cargo) CarregarCena(economia.Cargo);
            bool alerta = economia.Travamentos.Count > 0 || economia.DiscoQueimado || economia.DeployQuebrado;
            tela.Recortar(x0, y0, SW, SH);
            tela.Imagem(cenaPx, cenaW, cenaH, x0, y0);

            // luzes da cidade: algumas janelas apagam e acendem devagar
            int bloco = Mathf.FloorToInt(t / 3f);
            for (int i = 0; i < luzesCena.Count; i++)
                if (Sorteio(i, bloco) == 0)
                {
                    var p = luzesCena[i];
                    var c = cenaPx[p.y * cenaW + p.x];
                    tela.Pixel(x0 + p.x, y0 + p.y, new Color32((byte)(c.r * 0.3f), (byte)(c.g * 0.3f), (byte)(c.b * 0.4f), 255));
                }

            // LEDs do rack piscando; com incidente, uma parte fica vermelha
            bool piscar = Mathf.FloorToInt(t * 4) % 2 == 0;
            for (int i = 0; i < ledsCena.Count; i++)
            {
                var p = ledsCena[i];
                var c = cenaPx[p.y * cenaW + p.x];
                Color32 cor = c;
                if (alerta && i % 3 == 0) cor = piscar ? C("#ff3b4e") : C("#3a1018");
                else if (Mathf.FloorToInt(t * 6 + i * 1.7f) % 5 <= 1) cor = new Color32((byte)(c.r * 0.25f), (byte)(c.g * 0.25f), (byte)(c.b * 0.25f), 255);
                tela.Pixel(x0 + p.x, y0 + p.y, cor);
            }

            // terminal do CRT: linhas sendo digitadas (vermelhas quando algo trava)
            if (telaCrt.width > 0)
            {
                tela.Ret(x0 + telaCrt.x, y0 + telaCrt.y, telaCrt.width, telaCrt.height, fundoCrt);
                int linhas = Mathf.Max(1, (telaCrt.height - 2) / 3);
                int passo = Mathf.FloorToInt(t * (alerta ? 14 : 8));
                for (int l = 0; l < linhas; l++)
                {
                    int tamanho = 3 + Sorteio(l, passo / 20) % Mathf.Max(1, telaCrt.width - 5);
                    int visivel = l == linhas - 1 ? Mathf.Min(tamanho, passo % 20) : tamanho;
                    string cor = alerta ? (l == linhas - 1 ? "#ff3b4e" : "#a83240") : (l == linhas - 1 ? "#5cff8a" : "#3fbf6a");
                    tela.Ret(x0 + telaCrt.x + 2, y0 + telaCrt.y + 2 + l * 3, visivel, 1, C(cor));
                }
                if (Mathf.FloorToInt(t * 2) % 2 == 1)
                    tela.Ret(x0 + telaCrt.x + 2, y0 + telaCrt.yMax - 3, 2, 1, C(alerta ? "#ff3b4e" : "#5cff8a"));
            }

            // o dev se assusta quando algo trava
            if (alerta && piscar) T("!", x0 + cabecaDev.x, y0 + cabecaDev.y, "#ff3b4e", true, 2);

            // o gato sonhando (só na cena que tem gato)
            float zz = (t * 0.6f) % 1f;
            if (cenaTemGato) tela.Texto("Z", x0 + gato.x + Mathf.RoundToInt(zz * 4), y0 + gato.y - Mathf.RoundToInt(zz * 10), new Color32(201, 212, 255, (byte)((1 - zz) * 255)), false);

            tela.SemRecorte();
            R(x0 - 1, y0 - 1, SW + 2, 1, "#1b1a2e");
            R(x0 - 1, y0 + SH, SW + 2, 1, "#1b1a2e");
        }
    }
}
