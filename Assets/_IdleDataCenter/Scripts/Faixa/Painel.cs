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

        enum Aba { VisaoGeral, Melhorias, Carreira }

        static readonly string[] Escada =
        {
            "Técnico de TI", "Sysadmin", "Analista de Infra", "Engenheiro DevOps", "SRE", "Arquiteto", "CTO / Fundador",
        };

        static readonly Dictionary<int, string[]> Novidades = new Dictionary<int, string[]>
        {
            [1] = new[] { "O armário vira uma salinha.", "Rack 42U com servidores 1U.", "Energia e temperatura", "passam a importar." },
        };

        static readonly string[] Gato =
        {
            "..k.k.............",
            ".kyky......kk.....",
            "kyyyykkkkkkyyk....",
            "kyekeyyyyyyyyyk...",
            "kyyyyyyvyyvyyyk...",
            ".kyyyyvyyvyyyyk...",
            "..kkkkkkkkkkkk....",
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

        /// <summary>Abre numa aba pelo nome ("visao", "melhorias" ou "carreira"); usado nos testes.</summary>
        public void AbrirAba(string nome)
        {
            aba = nome == "melhorias" ? Aba.Melhorias : nome == "carreira" ? Aba.Carreira : Aba.VisaoGeral;
            Mostrar(true);
        }
        public void Fechar() => Mostrar(false);

        // ---------------- Entrada ----------------

        Vector2Int ParaLocal(Vector2 mundo) =>
            new Vector2Int(Mathf.FloorToInt(mundo.x - transform.position.x), Altura - 1 - Mathf.FloorToInt(mundo.y - transform.position.y));

        public void DefinirCursor(Vector2 mundo) => cursor = ParaLocal(mundo);

        public void ClicarEm(Vector2 mundo)
        {
            var p = ParaLocal(mundo);
            for (int i = botoes.Count - 1; i >= 0; i--)
                if (botoes[i].area.Contains(p)) { botoes[i].acao(); proximoDesenho = 0; return; }
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
                (economia.TotalServidores.ToString(), "Servidores", "#5cc8ff"),
                ($"{economia.Temperatura:0} C", "Temp", economia.Quente ? "#ff3b4e" : "#ff9fae"),
            };
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
                ("Automação", null), ("Pesquisa", null), ("Conquistas", null),
            };
            string[][] icones =
            {
                new[] { ".#.", "###", "#.#" }, new[] { ".#.", "###", ".#." }, new[] { "###", ".#.", ".#." },
                new[] { "#.#", ".#.", "#.#" }, new[] { ".#.", ".#.", "###" }, new[] { "###", "#.#", ".#." },
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
            if (lista.Count == 0)
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
                T($"{Catalogo.TempoConsertoTecnico - lista[i].segundos:0}s", x + 11, y + 8, "#7d82ad", false);
                int idx = s;
                Botao(new RectInt(x + 70, y, 57, 14), "Reiniciar", "#fdf6e3", "#7a2a3a", () => faixa.Reiniciar(idx));
            }
        }

        string NomeServidor(int s) => economia.EhTorre(s) ? $"Torre-{s + 1}" : $"1U-{s - economia.Torres + 1:00}";

        void LinhaDeBaixo(int y)
        {
            // Servidores (travados primeiro)
            int total = economia.TotalServidores;
            Caixa(76, y, 150, 58, $"Servidores ({total})");
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
            if (total > 3) T($"+{total - 3} outros", 88, y + 50, "#4d5170", false);

            // Recursos: receita ao longo do tempo, energia e temperatura
            Caixa(230, y, 326, 58, "Recursos");
            T("Receita", 235, y + 14, "#a3a0bd", false);
            float max = Mathf.Max(1f, historico.Max());
            for (int i = 0; i < 238; i++)
            {
                float v = historico[(cabeca + 2 + i) % historico.Length] / max;
                P(284 + i, y + 20 - Mathf.RoundToInt(v * 8), "#5cff8a");
            }
            T("Energia", 235, y + 28, "#a3a0bd", false);
            Barra(284, y + 29, 238, 4, (float)(economia.ConsumoKw / economia.CapacidadeKw), economia.Sobrecarga ? "#ff3b4e" : "#ffbf3f");
            T($"{economia.ConsumoKw:0.0} kW", 524, y + 28, "#7d82ad", false);
            T("Temp", 235, y + 40, "#a3a0bd", false);
            Barra(284, y + 41, 238, 4, (float)((economia.Temperatura - 15) / 30), economia.Quente ? "#ff3b4e" : "#5aa9ff");
            T($"{economia.Temperatura:0} C", 524, y + 40, "#7d82ad", false);
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

        // ---------------- Cena da Era 1: homelab à noite ----------------

        void Cena(int x0, int y0)
        {
            const int SW = 344, SH = 150;
            bool alerta = economia.Travamentos.Count > 0;
            tela.Recortar(x0, y0, SW, SH);

            // parede, piso e tapete
            R(x0, y0, SW, SH, "#2b2238");
            for (int x = 0; x < SW; x += 12) R(x0 + x, y0, 1, 128, "#30273f");
            R(x0, y0 + 127, SW, 2, "#1f1a2a");
            R(x0, y0 + 129, SW, 21, "#4a3530");
            for (int y = 0; y < 21; y += 6) R(x0, y0 + 129 + y, SW, 1, "#3d2b27");
            for (int i = 0; i < 12; i++) R(x0 + (i * 41) % SW, y0 + 129 + (i % 3) * 6, 1, 6, "#3d2b27");
            R(x0 + 30, y0 + 138, 110, 9, "#7a3b44"); R(x0 + 32, y0 + 140, 106, 5, "#8f4a52");
            for (int x = 36; x < 136; x += 8) R(x0 + x, y0 + 142, 4, 1, "#c07a5a");

            // janela com a cidade à noite
            int jx = x0 + 8, jy = y0 + 10;
            R(jx, jy, 70, 64, "#1b1a2e");
            string[] ceu = { "#10163a", "#141c45", "#19224f", "#1e2859", "#232e63" };
            for (int i = 0; i < ceu.Length; i++) R(jx + 3, jy + 3 + i * 12, 64, 12, ceu[i]);
            for (int i = 0; i < 26; i++)
                if ((Mathf.FloorToInt(t * 2) + i) % 5 != 0) P(jx + 3 + (i * 37) % 64, jy + 3 + (i * 23) % 26, "#c9d4ff");
            for (int dy = -5; dy <= 5; dy++)
            for (int dx = -5; dx <= 5; dx++)
                if (dx * dx + dy * dy <= 25 && (dx - 3) * (dx - 3) + (dy + 1) * (dy + 1) > 18) P(jx + 54 + dx, jy + 14 + dy, "#fdf6e3");
            int[] px0 = { 0, 9, 17, 24, 33, 41, 50, 57 }, alt = { 22, 30, 18, 34, 26, 38, 24, 30 };
            for (int i = 0; i < px0.Length; i++) R(jx + 3 + px0[i], jy + 61 - alt[i], 8, alt[i], "#0c0f24");
            for (int i = 0; i < 40; i++)
                if ((i * 7 + Mathf.FloorToInt(t / 3)) % 4 != 0) P(jx + 4 + (i * 29) % 62, jy + 48 + (i * 17) % 14, i % 6 != 0 ? "#ffd65c" : "#ff9f5c");
            R(jx + 34, jy + 3, 2, 58, "#1b1a2e"); R(jx + 3, jy + 31, 64, 2, "#1b1a2e");
            R(jx - 2, jy + 64, 74, 3, "#5a4a60");
            tela.Mapa(Arte.MapaPlanta, jx + 4, jy + 55);

            // pôster
            int px = x0 + 90, py = y0 + 12;
            R(px, py, 56, 44, "#1b1a2e"); R(px + 2, py + 2, 52, 40, "#23305e");
            string[] poster = { "Funciona", "na minha", "máquina", ":)" };
            for (int i = 0; i < poster.Length; i++)
                T(poster[i], px + 28 - L(poster[i]) / 2, py + 7 + i * 8, i == 3 ? "#ffd65c" : "#c9d4ff", false);

            // lâmpada pendurada com brilho
            int lx = x0 + 176;
            R(lx, y0, 1, 14, "#1b1a2e"); R(lx - 1, y0 + 14, 3, 2, "#3a3b4a"); R(lx - 1, y0 + 16, 3, 3, "#ffe9a8");
            byte brilho = (byte)((0.10f + 0.03f * Mathf.Sin(t * 3)) * 255);
            for (int r = 26; r > 4; r -= 7) tela.Circulo(lx, y0 + 18, r, new Color32(255, 214, 120, brilho));

            // prateleira: gato dormindo, planta e livros
            int ex = x0 + 196, ey = y0 + 30;
            R(ex, ey, 66, 3, "#7d5238"); R(ex, ey, 66, 1, "#b07a52");
            tela.Mapa(Gato, ex + 4, ey - 7);
            int rabo = Mathf.FloorToInt(t * 1.5f) % 2;
            R(ex + 18, ey - 3 - rabo, 1, 2, "#1b1a2e"); R(ex + 19, ey - 4 - rabo, 2, 1, "#1b1a2e");
            float zz = (t * 0.6f) % 1f;
            tela.Texto("Z", ex + 12 + Mathf.RoundToInt(zz * 4), ey - 12 - Mathf.RoundToInt(zz * 8), new Color32(201, 212, 255, (byte)((1 - zz) * 255)), false);
            tela.Mapa(Arte.MapaPlanta, ex + 42, ey - 9);
            R(ex + 54, ey - 7, 2, 7, "#e0805a"); R(ex + 57, ey - 8, 2, 8, "#5aa9ff"); R(ex + 60, ey - 6, 2, 6, "#6fd36f");
            PostIt("Mais", "racks", x0 + 196, y0 + 40);
            PostIt("Dominar", "o mundo", x0 + 232, y0 + 40);

            // mesa com gavetas
            R(x0 + 58, y0 + 96, 144, 4, "#8a5a3c"); R(x0 + 58, y0 + 96, 144, 1, "#b07a52");
            R(x0 + 62, y0 + 100, 4, 27, "#6b442e"); R(x0 + 196, y0 + 100, 4, 27, "#6b442e");
            R(x0 + 168, y0 + 100, 28, 27, "#7d5238");
            for (int i = 0; i < 3; i++) { R(x0 + 170, y0 + 102 + i * 8, 24, 7, "#8a5a3c"); R(x0 + 180, y0 + 105 + i * 8, 4, 1, "#d4b08c"); }

            // luminária com cone de luz quente
            R(x0 + 68, y0 + 93, 10, 3, "#3a3b4a");
            for (int i = 0; i < 16; i++) P(x0 + 72 + i / 2, y0 + 92 - i, "#3a3b4a");
            R(x0 + 78, y0 + 72, 10, 5, "#3a3b4a"); R(x0 + 80, y0 + 77, 6, 1, "#ffe9a8");
            var luz = new Color32(255, 200, 110, 26);
            for (int y = 78; y < 96; y++)
            {
                float k = (y - 78) / 18f;
                int esq = Mathf.RoundToInt(Mathf.Lerp(80, 60, k)), dir = Mathf.RoundToInt(Mathf.Lerp(86, 112, k));
                tela.Ret(x0 + esq, y0 + y, dir - esq, 1, luz);
            }

            // monitor: status real do "datacenter"
            int mx = x0 + 112, my = y0 + 56;
            R(mx, my, 52, 36, "#1b1a2e"); R(mx + 1, my + 1, 50, 34, "#2e3040"); R(mx + 3, my + 3, 46, 28, alerta ? "#1a0b10" : "#0b1a14");
            string[] linhasT = alerta
                ? new[] { ">ping...", ">timeout!", ">reinicia", ">ALERTA!" }
                : new[] { ">serv   ok", ">monit  ok", ">backup ok", ">tudo certo" };
            int escritos = Mathf.FloorToInt(t * 8) % 60;
            for (int i = 0; i < linhasT.Length; i++)
            {
                int n = Mathf.Clamp(escritos - i * 12, 0, linhasT[i].Length);
                string cor = alerta ? (i == 3 ? "#ff3b4e" : "#bf3f4f") : (i == 3 ? "#5cff8a" : "#3fbf6a");
                T(linhasT[i].Substring(0, n), mx + 5, my + 5 + i * 6, cor, false);
            }
            if (Mathf.FloorToInt(t * 2) % 2 == 1) R(mx + 5, my + 29, 3, 1, alerta ? "#ff3b4e" : "#5cff8a");
            R(mx + 22, my + 36, 8, 4, "#2e3040"); R(mx + 16, my + 39, 20, 1, "#2e3040");

            // teclado, mouse e caneca fumegante
            R(x0 + 116, y0 + 93, 36, 3, "#3a3b4a");
            for (int i = 0; i < 8; i++) P(x0 + 118 + i * 4, y0 + 94, "#6c6f86");
            R(x0 + 156, y0 + 94, 4, 2, "#3a3b4a");
            R(x0 + 96, y0 + 86, 8, 10, "#fdf6e3"); R(x0 + 104, y0 + 88, 2, 5, "#fdf6e3"); R(x0 + 97, y0 + 89, 6, 2, "#ff9fae");
            float vapor = (t * 0.8f) % 1f;
            int balanco = Mathf.RoundToInt(Mathf.Sin(t * 4));
            tela.Pixel(x0 + 98 + balanco, y0 + 83 - Mathf.RoundToInt(vapor * 8), new Color32(255, 255, 255, (byte)(180 * (1 - vapor))));
            tela.Pixel(x0 + 101 - balanco, y0 + 80 - Mathf.RoundToInt(vapor * 8), new Color32(255, 255, 255, (byte)(130 * (1 - vapor))));

            // cadeira e o dev de moletom e pijama
            R(x0 + 64, y0 + 76, 5, 38, "#26273a"); R(x0 + 64, y0 + 108, 30, 5, "#26273a"); R(x0 + 76, y0 + 113, 3, 12, "#1b1a2e");
            R(x0 + 68, y0 + 125, 20, 2, "#1b1a2e");
            Dev(x0 + 70, y0 + 62, alerta);

            // gabinete velho embaixo da mesa, com adesivo de pinguim
            tela.Mapa(Arte.MapaServidor, x0 + 146, y0 + 109);
            P(x0 + 149, y0 + 123, "#5cff8a");
            P(x0 + 151, y0 + 123, UnityEngine.Random.value < 0.5f ? "#ffbf3f" : "#4a3f30");
            tela.Mapa(new[] { ".k.", "kyk", "kyk", "Y.Y" }, x0 + 153, y0 + 116);

            Rack(x0 + 272, y0 + 36, alerta);

            // caixa de backups
            R(x0 + 300, y0 + 108, 40, 20, "#c8955f"); R(x0 + 300, y0 + 108, 40, 2, "#a8784a"); R(x0 + 318, y0 + 108, 4, 20, "#e0b27a");
            T("Backup", x0 + 309, y0 + 115, "#6b4a1f", false);

            tela.SemRecorte();
            R(x0 - 1, y0 - 1, SW + 2, 1, "#1b1a2e");
            R(x0 - 1, y0 + SH, SW + 2, 1, "#1b1a2e");
        }

        void PostIt(string a, string b, int x, int y)
        {
            R(x, y, 32, 17, "#ffe27a"); R(x, y, 32, 2, "#f5cf52");
            T(a, x + 2, y + 3, "#6b4a1f", false);
            T(b, x + 2, y + 10, "#6b4a1f", false);
        }

        /// <summary>O dev cansado. Com incidente, arregala os olhos e para de digitar.</summary>
        void Dev(int dx, int dy, bool alerta)
        {
            R(dx + 2, dy + 20, 22, 26, "#5b5f73"); R(dx + 2, dy + 20, 5, 26, "#474a5c");   // moletom
            R(dx, dy + 16, 8, 12, "#474a5c");                                                 // capuz
            R(dx + 6, dy + 4, 16, 17, "#f2c29a");                                             // rosto
            R(dx + 4, dy, 18, 7, "#3b2620"); R(dx + 4, dy + 6, 4, 8, "#3b2620");             // cabelo
            int[,] mechas = { { 5, -2 }, { 9, -3 }, { 13, -2 }, { 17, -3 }, { 20, -1 }, { 2, 2 } };
            for (int i = 0; i < mechas.GetLength(0); i++) R(dx + mechas[i, 0], dy + mechas[i, 1], 3, 3, "#3b2620");
            R(dx + 10, dy + 15, 13, 6, "#4a2f25"); R(dx + 12, dy + 20, 9, 2, "#4a2f25");       // barba
            if (alerta)
            {
                R(dx + 16, dy + 8, 3, 3, "#fdf6e3"); P(dx + 17, dy + 9, "#2a2238");             // olho arregalado
                if (Mathf.FloorToInt(t * 3) % 2 == 0) T("!", dx + 12, dy - 12, "#ff3b4e", false);
            }
            else
            {
                R(dx + 16, dy + 9, 3, 1, "#2a2238"); R(dx + 16, dy + 10, 3, 1, "#d9a07e");      // olho cansado + olheira
            }
            P(dx + 22, dy + 11, "#e0a882"); P(dx + 18, dy + 13, "#ff9fae");                    // nariz, bochecha
            int mao = alerta ? 0 : Mathf.FloorToInt(t * 7) % 2;
            R(dx + 18, dy + 28, 26, 4, "#5b5f73"); R(dx + 44, dy + 29 - mao, 4, 3, "#f2c29a");
            R(dx + 20, dy + 44, 24, 7, "#3b5aa8");                                            // pijama
            R(dx + 40, dy + 50, 6, 14, "#3b5aa8");
            P(dx + 24, dy + 46, "#fdf6e3"); P(dx + 32, dy + 48, "#fdf6e3"); P(dx + 42, dy + 55, "#fdf6e3"); P(dx + 38, dy + 45, "#fdf6e3");
            R(dx + 38, dy + 63, 12, 3, "#f2d0d8"); P(dx + 47, dy + 61, "#ff9fae"); P(dx + 49, dy + 61, "#ff9fae"); // pantufa
        }

        void Rack(int rkx, int rky, bool alerta)
        {
            // roteador com wi-fi
            R(rkx + 4, rky - 10, 26, 6, "#e8e8ef"); R(rkx + 8, rky - 18, 1, 8, "#c9c9d6"); R(rkx + 25, rky - 18, 1, 8, "#c9c9d6");
            for (int i = 0; i < 4; i++) P(rkx + 8 + i * 4, rky - 8, (Mathf.FloorToInt(t * 4) + i) % 3 != 0 ? "#5cff8a" : "#2f8a4f");
            int wifi = Mathf.FloorToInt(t * 2) % 3;
            for (int a = 0; a <= wifi; a++) R(rkx + 15 - a * 2, rky - 22 - a * 2, 3 + a * 4, 1, "#5cc8ff");

            R(rkx, rky, 58, 92, "#1b1a2e"); R(rkx + 2, rky + 2, 54, 88, "#2a2c3d");
            bool piscar = Mathf.FloorToInt(t * 4) % 2 == 0;
            for (int u = 0; u < 7; u++)
            {
                int uy = rky + 4 + u * 12;
                R(rkx + 4, uy, 50, 10, u == 2 ? "#1f2a4a" : "#3a3d52"); R(rkx + 4, uy, 50, 1, "#50546c");
                if (u == 2) T("NAS", rkx + 7, uy + 3, "#5cc8ff", false);
                for (int l = 0; l < 8; l++)
                {
                    bool aceso = Mathf.FloorToInt(t * 6 + u * 3 + l * 1.7f) % 5 > 1;
                    string cor = alerta && u == 4 ? (piscar ? "#ff3b4e" : "#1f2130")
                               : aceso ? (u % 3 == 0 ? "#5cc8ff" : l % 4 == 0 ? "#ffbf3f" : "#5cff8a") : "#1f2130";
                    P(rkx + 24 + l * 3, uy + 4, cor);
                }
            }
            // cabos pendurados
            for (int y = 0; y < 90; y++)
            {
                P(rkx + 58 + Mathf.RoundToInt(2 * Mathf.Sin(y / 9f)), rky + 2 + y, "#3d7bd8");
                P(rkx + 61 + Mathf.RoundToInt(2 * Mathf.Sin(y / 11f + 1)), rky + 6 + y, "#e0805a");
            }
        }
    }
}
