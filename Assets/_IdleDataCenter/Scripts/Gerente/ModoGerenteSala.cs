using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    public partial class ModoGerente
    {
        // piso do compute onde os racks aparecem (frações da ilustração), como no protótipo do Codex
        static readonly Vector2[] PisoRacks = { new Vector2(.47f, .235f), new Vector2(.418f, .269f),
            new Vector2(.366f, .303f), new Vector2(.526f, .303f), new Vector2(.474f, .337f) };

        Rect retSala;
        Vector2 Ponto(float x, float y) => new Vector2(retSala.x + retSala.width * x, retSala.y + retSala.height * y);
        bool Livre => string.IsNullOrEmpty(janela);

        int RacksNoPiso => Mathf.Min(PisoRacks.Length, (E.TemRack ? 1 : 0) + E.RacksCheios);

        /// <summary>O que o marcador "CONSTRUIR" do piso compra em cada cargo (o equipamento principal da fase).</summary>
        string ItemPrincipal()
        {
            switch (E.Cargo)
            {
                case 0: return Catalogo.Servidor;
                case 1: return E.TemRack ? Catalogo.Servidor1U : Catalogo.Rack;
                case 2: return Catalogo.RackCheio;
                case 3: return Catalogo.Containers;
                default: return Catalogo.NoKubernetes;
            }
        }

        void DesenharSala()
        {
            if (cargoDaSala != E.Cargo) { sala = SetoresIso.Escurecida(ambiente, E.Cargo); cargoDaSala = E.Cargo; }
            retSala = areaCentral;
            if (sala != null)
            {
                float esc = Mathf.Min(areaCentral.width / sala.width, areaCentral.height / sala.height);
                retSala = new Rect(areaCentral.center.x - sala.width * esc / 2, areaCentral.center.y - sala.height * esc / 2,
                    sala.width * esc, sala.height * esc);
                GUI.DrawTexture(retSala, sala, ScaleMode.StretchToFill);
            }
            ui.Ret(new Rect(areaCentral.x, areaCentral.y, areaCentral.width, 23), new Color(.04f, .1f, .18f, .9f));
            ui.Texto("DC-01  /  " + E.CargoAtual.Lugar.ToUpperInvariant(), areaCentral.x + 12, areaCentral.y + 7, IsoGui.Cyan);
            ui.Texto("CLIQUE NUM SETOR PARA GERENCIAR", areaCentral.xMax - 12 - PixelCanvas.LarguraTexto("CLIQUE NUM SETOR PARA GERENCIAR") * 2, areaCentral.y + 7, IsoGui.Muted);

            // racks comprados, com LEDs; o primeiro fica em alerta quando há servidor travado
            for (int i = 0; i < RacksNoPiso; i++)
            {
                Vector2 p = Ponto(PisoRacks[i].x, PisoRacks[i].y);
                float largura = retSala.width * .067f, altura = largura * 1.68f;
                var r = new Rect(p.x - largura / 2, p.y - altura, largura, altura);
                GUI.DrawTexture(r, rack, ScaleMode.StretchToFill);
                bool falha = i == 0 && E.Travamentos.Count > 0;
                for (int led = 0; led < 4; led++)
                    if (Mathf.Sin(Time.unscaledTime * (3 + led) + i * 2 + led) > -.25f)
                        ui.Ret(new Rect(p.x - largura * .23f, p.y - altura * (.24f + led * .1f), 2, 2), falha ? IsoGui.Laranja : IsoGui.Verde);
                if (falha) ui.Texto("!", p.x, r.y - 10, IsoGui.Laranja, 3, true);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (falha) Resolver();
                    else { double v = E.ClicarEquipamento(); Sons.Moeda(); Notificar("Job de compute entregue: +" + Dinheiro(v)); }
                }
            }

            // hosts de containers no piso de expansão
            for (int i = 0; i < E.HostsContainers; i++)
            {
                Vector2 p = Ponto(.60f + i * .035f, .64f + i * .02f);   // piso livre entre o marcador e a refrigeração
                GUI.color = E.DeployQuebrado && Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0 ? new Color(1, .55f, .55f) : new Color(.8f, .94f, 1);
                GUI.DrawTexture(new Rect(p.x - 23, p.y - 78, 46, 78), rack);
                GUI.color = Color.white;
            }

            // marcador de construção: compra o equipamento principal do cargo
            string item = ItemPrincipal();
            if (!E.NoMaximo(item))
            {
                var p = Ponto(.50f, .78f);
                Lote(p, 74, flashCompra > 0 ? IsoGui.Verde : IsoGui.Laranja);
                ui.Caixa(new Rect(p.x - 18, p.y - 50, 36, 32), IsoGui.Cor("d49335"), IsoGui.Laranja);
                ui.Texto("+", p.x, p.y - 42, Color.white, 3, true);
                string rotulo = "CONSTRUIR " + NomeLongo(item).ToUpperInvariant();
                float w = Mathf.Max(136, PixelCanvas.LarguraTexto(rotulo) * 2 + 24);
                if (ui.Botao(new Rect(p.x - w / 2, p.y - 8, w, 28), rotulo, IsoGui.Laranja, Livre)) Comprar(item);
                ui.Texto(Dinheiro(E.Custo(item)), p.x, p.y + 28, PodeComprarAqui(item) ? IsoGui.Verde : IsoGui.Branco, 2, true);
            }

            // equipe andando pelo piso (o estagiário entra quando é contratado)
            for (int i = 0; i < 2 + (E.TemEstagiario ? 1 : 0); i++)
            {
                float t = Time.unscaledTime * .035f + i * .31f;
                float x = Mathf.Lerp(.32f, .68f, Mathf.PingPong(t, 1));
                float y = .69f + i * .047f + (x - .4f) * .23f;
                var p = Ponto(x, y);
                int quadro = (Mathf.FloorToInt(Time.unscaledTime * 5) + i) % passos.Length;
                float largura = 40 * passosAspecto[quadro];
                Rect uv = passosUv[quadro];
                if (Mathf.FloorToInt(t) % 2 == 1) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
                ui.Ret(new Rect(p.x - 8, p.y - 2, 16, 3), new Color(.1f, .2f, .3f, .25f));
                GUI.DrawTextureWithTexCoords(new Rect(p.x - largura / 2, p.y - 40, largura, 40), passos[quadro], uv);
            }

            // drone: parado como protótipo até o Watchdog ficar pronto; depois circula fazendo reparos
            bool droneAtivo = E.TemAutomacao(Catalogo.Watchdog);
            var d = Ponto(droneAtivo ? .57f + Mathf.Sin(Time.unscaledTime * .25f) * .14f : .73f,
                          droneAtivo ? .64f + Mathf.Cos(Time.unscaledTime * .25f) * .06f : .74f);
            GUI.color = droneAtivo ? Color.white : new Color(.62f, .72f, .79f, 1);
            GUI.DrawTexture(new Rect(d.x - 27, d.y - 38 - Mathf.Sin(Time.unscaledTime * 2) * 3, 54, 46), drone);
            GUI.color = Color.white;
            if (!droneAtivo) ui.Texto("PROTOTIPO", d.x, d.y + 11, IsoGui.Cyan, 1, true);

            // placas dos setores (as bloqueadas dizem em que cargo liberam)
            foreach (var s in SetoresIso.Todos)
                if (s.Nome != null) Placa(s);

            // pico de tráfego: faixa de alerta no topo da sala
            if (E.EmPico && !E.PicoFoiEscalado)
            {
                bool piscar = Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0;
                var r = new Rect(areaCentral.x + 150, areaCentral.yMax - 76, areaCentral.width - 300, 34);   // embaixo: não cobre as placas
                ui.Caixa(r, piscar ? IsoGui.Cor("5a2a10") : IsoGui.Cor("3a1a08"), IsoGui.Laranja);
                string texto = E.PicoViolado ? "SLA VIOLADO! ESCALE PARA RECUPERAR"
                    : "PICO: " + E.NomeDoPico.ToUpperInvariant() + "  /  ESCALE EM " + Numero(Mathf.Ceil((float)(E.LimiteParaEscalar - E.SegundosDePico))) + "S";
                ui.Texto(texto, r.center.x, r.y + 12, IsoGui.Laranja, 2, true);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) Escalar();
            }
        }

        void Lote(Vector2 p, int raio, Color c)
        {
            var pontos = new[] { new Vector2(p.x, p.y - raio / 2), new Vector2(p.x + raio, p.y),
                new Vector2(p.x, p.y + raio / 2), new Vector2(p.x - raio, p.y) };
            for (int i = 0; i < 4; i++)
            {
                var a = pontos[i]; var b = pontos[(i + 1) % 4];
                for (int j = 0; j < raio; j += 4)
                {
                    var q = Vector2.Lerp(a, b, (float)j / raio);
                    ui.Ret(new Rect(Mathf.Round(q.x), Mathf.Round(q.y), 3, 2), c);
                }
            }
        }

        void Placa(SetoresIso.Setor s)
        {
            var p = Ponto(s.Placa.x, s.Placa.y);
            bool liberado = SetoresIso.Liberado(s, E.Cargo);
            int largura = PixelCanvas.LarguraTexto(s.Nome) * 2 + 22;
            if (liberado)
            {
                // alerta no setor: storage com disco queimado, NOC com incidentes
                bool alerta = (s.Id == "Storage" && E.DiscoQueimado) || (s.Id == "NOC" && Incidentes > 0);
                var cor = alerta && Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0 ? IsoGui.Laranja : s.Cor;
                if (ui.Botao(new Rect(p.x - largura / 2, p.y, largura, 26), s.Nome, cor, Livre))
                {
                    if (s.Id == "Storage" && E.DiscoQueimado) { faixa.TrocarDisco(); Notificar("Disco trocado."); }
                    else Abrir(s.Id);
                }
                return;
            }
            string libera = "LIBERA NO " + SetoresIso.CargoCurto(s.Cargo);
            int w = Mathf.Max(largura, PixelCanvas.LarguraTexto(libera) + 22);
            var r = new Rect(p.x - w / 2, p.y, w, 36);
            ui.Caixa(r, IsoGui.Cor("141d2c"), IsoGui.Borda);
            ui.Texto(s.Nome, r.center.x, r.y + 7, IsoGui.Muted, 2, true);
            ui.Texto(libera, r.center.x, r.y + 24, IsoGui.Laranja, 1, true);
        }

        // ---------------- Barra superior ----------------

        void Topo()
        {
            ui.Ret(new Rect(0, 0, W, 84), IsoGui.Cor("0b1627"));
            ui.Ret(new Rect(0, 81, W, 3), IsoGui.Borda);
            ui.Icone(0, 20, 19, IsoGui.Cyan, 5);
            ui.Texto("IDLE DATA CENTER", 65, 17, IsoGui.Branco, 3);
            ui.Texto("/ MODO GERENTE", 65, 43, IsoGui.Cyan, 2);
            ui.Texto("ESC VOLTA PARA A FAIXA", 22, 66, IsoGui.Muted, 1);
            Recurso(275, 184, "DINHEIRO", Dinheiro(E.Dinheiro), "+" + Dinheiro(E.ReceitaPorSegundo) + "/S", IsoGui.Verde);
            Recurso(469, 174, "ENERGIA", Numero(E.ConsumoKw) + " KW", "DE " + Numero(E.CapacidadeKw) + " KW", E.Sobrecarga ? IsoGui.Laranja : IsoGui.Laranja);
            Recurso(653, 174, "TEMPERATURA", Numero(Mathf.Round((float)E.Temperatura)) + " C", E.Quente ? "QUENTE: RENDE MENOS" : "ESTAVEL", E.Quente ? IsoGui.Laranja : IsoGui.Cyan);
            Recurso(837, 150, "SERVIDORES", E.ContagemServidores.ToString(), E.NaSalaDeRacks ? Numero(E.TrafegoMbps) + " / " + Numero(E.BandaMbps) + " MB" : "SEM LIMITE DE BANDA", IsoGui.Cyan);
            Recurso(997, 154, "AUTOMACOES", E.AutomacoesAtivas + " / " + Catalogo.Automacoes.Count, E.Escrevendo ? "ESCREVENDO..." : E.AutomacoesLiberadas ? "LABORATORIO LIVRE" : "LIBERA NO ANALISTA", IsoGui.Roxo);
            Recurso(1161, 264, "CARGO", E.CargoAtual.Nome.ToUpperInvariant(), E.CargoAtual.Lugar.ToUpperInvariant(), IsoGui.Laranja);
        }

        void Recurso(int x, int largura, string nome, string valor, string extra, Color c)
        {
            ui.Caixa(new Rect(x, 10, largura, 63));
            ui.Ret(new Rect(x + 2, 12, 3, 59), c);
            ui.Texto(nome, x + 13, 19, IsoGui.Muted, 1);
            ui.Texto(valor, x + 13, 33, c, PixelCanvas.LarguraTexto(valor) * 3 > largura - 20 ? 2 : 3);
            ui.Texto(extra, x + 13, 57, IsoGui.Muted, 1);
        }

        // ---------------- Menu lateral ----------------

        static readonly (string id, string rotulo)[] Menus =
        {
            ("Visao", "VISAO GERAL"), ("Compute", "COMPUTE"), ("Energia", "ENERGIA"), ("Refrigeracao", "REFRIGERACAO"),
            ("Storage", "STORAGE"), ("Rede", "REDE"), ("NOC", "NOC"), ("Equipe", "EQUIPE"), ("Automacao", "AUTOMACAO"),
            ("Melhorias", "MELHORIAS"), ("Carreira", "CARREIRA"),
        };

        void Navegacao()
        {
            ui.Ret(new Rect(0, 86, 176, 814), IsoGui.Cor("101e32"));
            ui.Texto("CENTRAL", 15, 104, IsoGui.Muted, 2);
            for (int i = 0; i < Menus.Length; i++)
            {
                var (id, rotulo) = Menus[i];
                var setor = SetoresIso.Buscar(id);
                bool travado = setor != null && !SetoresIso.Liberado(setor, E.Cargo);
                var r = new Rect(10, 130 + i * 44, 156, 38);
                bool ativo = selecionado == id;
                ui.Caixa(r, ativo ? IsoGui.Cor("165d81") : IsoGui.Painel, ativo ? IsoGui.Cyan : IsoGui.Borda);
                ui.Icone(i, r.x + 10, r.y + 11, travado ? IsoGui.Borda : ativo ? IsoGui.Cyan : IsoGui.Muted);
                ui.Texto(rotulo, r.x + 32, r.y + 14, travado ? IsoGui.Borda : ativo ? IsoGui.Branco : IsoGui.Muted, 2);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir(id);
            }
            if (ui.Botao(new Rect(10, 786, 156, 88), "VOLTAR", IsoGui.Cyan, true, 3)) faixa.FecharGerente();
            ui.Texto("PARA A FAIXA", 88, 850, IsoGui.Muted, 1, true);
        }

        // ---------------- Coluna da direita ----------------

        void Objetivos()
        {
            const int x = 1174, largura = 252;
            ui.Caixa(new Rect(x, 94, largura, 242));
            var metas = E.CargoAtual.MetasParaPromocao;
            ui.Texto(E.TemProximoCargo ? "PROMOCAO" : "CARREIRA", x + 17, 113, IsoGui.Branco, 3);
            ui.Ret(new Rect(x + 12, 141, largura - 24, 2), IsoGui.Borda);
            if (!E.TemProximoCargo)
            {
                ui.Texto("CARGO MAXIMO POR ORA", x + 17, 160, IsoGui.Verde, 2);
                ui.Texto("NOVOS CARGOS NAS", x + 17, 186, IsoGui.Muted, 2);
                ui.Texto("PROXIMAS VERSOES", x + 17, 204, IsoGui.Muted, 2);
            }
            else
            {
                ui.Texto("PARA " + Catalogo.Cargos[E.Cargo + 1].Nome.ToUpperInvariant(), x + 17, 152, IsoGui.Cyan, 1);
                for (int i = 0; i < metas.Length && i < 3; i++)
                {
                    var m = metas[i];
                    int y = 168 + i * 40;
                    bool ok = E.Cumprida(m);
                    ui.Texto(m.Texto.ToUpperInvariant(), x + 17, y, ok ? IsoGui.Verde : IsoGui.Branco, 2);
                    ui.Barra(new Rect(x + 16, y + 16, 219, 12), E.Progresso(m) / m.Alvo, ok ? IsoGui.Verde : IsoGui.Cyan);
                }
                if (ui.Botao(new Rect(x + 14, 290, 224, 34), E.PodePromover ? "SER PROMOVIDO!" : "CUMPRA AS METAS", IsoGui.Laranja, E.PodePromover))
                    faixa.Promover();
            }

            ui.Caixa(new Rect(x, 348, largura, 184));
            ui.Texto("ALERTAS", x + 17, 369, IsoGui.Branco, 3);
            Alerta(x, 404, E.Sobrecarga, E.Sobrecarga ? "ENERGIA NO LIMITE" : "ENERGIA " + Numero(Mathf.Round((float)(E.ConsumoKw / E.CapacidadeKw * 100))) + "%");
            Alerta(x, 432, E.Quente, E.Quente ? "SALA QUENTE" : "REFRIGERACAO OK");
            Alerta(x, 460, E.LinkSaturado, E.LinkSaturado ? "LINK SATURADO" : "REDE " + Numero(Mathf.Round((float)(E.TrafegoMbps / E.BandaMbps * 100))) + "%");
            if (E.EmPico && !E.PicoFoiEscalado)
            {
                if (ui.Botao(new Rect(x + 14, 488, 224, 28), "ESCALAR CLUSTER", IsoGui.Laranja)) Escalar();
            }
            else if (ui.Botao(new Rect(x + 14, 488, 224, 28), Incidentes > 0 ? "RESOLVER " + Incidentes + (Incidentes == 1 ? " INCIDENTE" : " INCIDENTES") : "TUDO FUNCIONANDO",
                         Incidentes > 0 ? IsoGui.Laranja : IsoGui.Verde)) Resolver();

            ui.Caixa(new Rect(x, 546, largura, 221));
            ui.Texto("PRODUCAO", x + 17, 567, IsoGui.Branco, 3);
            ui.Texto("RECEITA POR SEGUNDO", x + 17, 600, IsoGui.Muted, 2);
            ui.Texto(Dinheiro(E.ReceitaPorSegundo), x + 17, 624, IsoGui.Verde, 4);
            ui.Texto("BONUS DO STORAGE", x + 17, 661, IsoGui.Muted, 2);
            ui.Texto("+" + Numero((E.FatorStorage - 1) * 100) + "%", x + 190, 661, IsoGui.Cyan, 2);
            ui.Texto("INCIDENTES", x + 17, 689, IsoGui.Muted, 2);
            ui.Texto(E.Estado.incidentesResolvidos.ToString(), x + 190, 689, IsoGui.Roxo, 2);
            ui.Texto("OFFLINE " + Numero(E.TaxaOffline * 100) + "% ATE " + Numero(E.HorasMaximasOffline) + " H", x + 17, 728, IsoGui.Muted, 2);
        }

        void Alerta(int x, int y, bool alerta, string texto)
        {
            ui.Ret(new Rect(x + 17, y + 1, 8, 8), alerta ? IsoGui.Laranja : IsoGui.Verde);
            ui.Texto(texto, x + 36, y, alerta ? IsoGui.Laranja : IsoGui.Muted, 2);
        }

        // ---------------- Rodapé ----------------

        void Rodape()
        {
            ui.Ret(new Rect(177, 777, W - 177, 123), IsoGui.Cor("0c1729"));
            ui.Ret(new Rect(177, 777, W - 177, 2), IsoGui.Borda);
            string[] nomes = { "CONSTRUIR", "DEPLOY", "AUTOMATIZAR", "MELHORIAS", "CARREIRA" };
            Color[] cores = { IsoGui.Cyan, IsoGui.Cor("5899ff"), IsoGui.Verde, IsoGui.Laranja, IsoGui.Roxo };
            for (int i = 0; i < nomes.Length; i++)
            {
                bool ativo = i != 2 || E.AutomacoesLiberadas;
                if (ui.Botao(new Rect(194 + i * 194, 790, 180, 52), nomes[i], cores[i], ativo, 3))
                {
                    if (i == 0) Abrir("Compute");
                    if (i == 1) Deploy();
                    if (i == 2) Abrir("Automacao");
                    if (i == 3) Abrir("Melhorias");
                    if (i == 4) Abrir("Carreira");
                }
            }

            // meta mais próxima de ser cumprida
            var metas = E.CargoAtual.MetasParaPromocao;
            var proxima = metas.Where(m => !E.Cumprida(m)).OrderByDescending(m => E.Progresso(m) / m.Alvo).FirstOrDefault();
            if (proxima != null)
            {
                ui.Texto("META: " + proxima.Texto.ToUpperInvariant(), 199, 862, IsoGui.Branco, 2);
                ui.Barra(new Rect(609, 857, 233, 18), E.Progresso(proxima) / proxima.Alvo, IsoGui.Verde);
                ui.Texto("PREMIO: PROMOCAO", 858, 862, IsoGui.Laranja, 2);
            }
            else ui.Texto(E.PodePromover ? "TODAS AS METAS CUMPRIDAS: SEJA PROMOVIDO!" : "CARGO MAXIMO POR ORA", 199, 862, IsoGui.Verde, 2);

            ui.Caixa(new Rect(1174, 788, 252, 90));
            ui.Texto(E.Escrevendo ? "ESCREVENDO SCRIPT" : "LABORATORIO", 1190, 804, IsoGui.Roxo, 2);
            if (E.Escrevendo)
            {
                ui.Texto(E.AutomacaoEmEscrita.Nome.ToUpperInvariant(), 1190, 828, IsoGui.Branco, 2);
                ui.Barra(new Rect(1190, 852, 218, 10), E.ProgressoEscrita, IsoGui.Roxo);
            }
            else ui.Texto(!E.AutomacoesLiberadas ? "LIBERA NO ANALISTA" : E.AutomacoesAtivas == Catalogo.Automacoes.Count ? "TUDO AUTOMATIZADO" : "PRONTO PARA O PROXIMO",
                1190, 838, IsoGui.Muted, 2);
        }
    }
}
