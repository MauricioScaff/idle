using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    public partial class ModoGerente
    {
        SalaIso salaIso;
        float proximoQuadroSala;
        Rect retSala;
        int zoomSala = 2;
        bool verCampus = true;   // no Arquiteto: campus (true) ou a sala de dentro do DC-01 (false)
        bool MostrandoCampus => E.NoCampus && verCampus;

        /// <summary>Textos "+R$" que sobem de onde se clicou.</summary>
        readonly List<(string texto, Vector2 pos, float nasceu, Color cor)> flutuantes = new List<(string, Vector2, float, Color)>();

        bool Livre => string.IsNullOrEmpty(janela);

        /// <summary>O que o marcador "CONSTRUIR" do piso compra em cada cargo (o equipamento principal da fase).</summary>
        string ItemPrincipal()
        {
            switch (E.Cargo)
            {
                case 0: return Catalogo.Servidor;
                case 1: return E.TemRack ? Catalogo.Servidor1U : Catalogo.Rack;
                case 2: return Catalogo.RackCheio;
                case 3: return Catalogo.Containers;
                case 4: return Catalogo.NoKubernetes;
                default: return MostrandoCampus ? Catalogo.Datacenter : Catalogo.NoKubernetes;
            }
        }

        /// <summary>Redesenha a sala (chamado no Update, uns 12 quadros por segundo).</summary>
        void AtualizarSala()
        {
            if (salaIso == null) salaIso = new SalaIso(E);
            if (Time.unscaledTime < proximoQuadroSala) return;
            proximoQuadroSala = Time.unscaledTime + 1f / 12f;
            salaIso.Desenhar(Time.unscaledTime, MostrandoCampus);
        }

        Vector2 NaTela(Vector2Int p) => new Vector2(retSala.x + p.x * zoomSala, retSala.y + p.y * zoomSala);

        void Flutuar(string texto, Vector2 pos, Color cor) => flutuantes.Add((texto, pos, Time.unscaledTime, cor));

        void DesenharSala()
        {
            ui.Ret(areaCentral, IsoGui.Cor("0c1626"));
            if (salaIso == null || salaIso.Textura == null) AtualizarSala();
            // zoom inteiro: pixels nítidos; salas pequenas (começo do jogo) ficam mais perto
            zoomSala = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min((areaCentral.width - 8) / salaIso.Largura, (areaCentral.height - 30) / salaIso.Altura)));
            float w = salaIso.Largura * zoomSala, h = salaIso.Altura * zoomSala;
            retSala = new Rect(Mathf.Round(areaCentral.center.x - w / 2), Mathf.Round(areaCentral.y + 24 + (areaCentral.height - 24 - h) / 2), w, h);
            GUI.DrawTexture(retSala, salaIso.Textura, ScaleMode.StretchToFill);

            ui.Ret(new Rect(areaCentral.x, areaCentral.y, areaCentral.width, 23), new Color(.04f, .1f, .18f, .9f));
            ui.Texto(MostrandoCampus ? "CAMPUS  /  " + E.TotalDatacenters + (E.TotalDatacenters == 1 ? " DATACENTER" : " DATACENTERS") : "DC-01  /  " + (E.NoCampus ? "SALA DE RACKS" : E.CargoAtual.Lugar.ToUpperInvariant()),
                areaCentral.x + 12, areaCentral.y + 7, IsoGui.Cyan);
            if (E.NoCampus && ui.Botao(new Rect(areaCentral.x + 300, areaCentral.y + 1, 230, 21), verCampus ? "ENTRAR NO DC-01" : "VER O CAMPUS", IsoGui.Cyan, Livre, 2))
                verCampus = !verCampus;
            const string dica = "CLIQUE NOS EQUIPAMENTOS E NAS PLACAS";
            ui.Texto(dica, areaCentral.xMax - 12 - PixelCanvas.LarguraTexto(dica) * 2, areaCentral.y + 7, IsoGui.Muted);

            // equipamentos clicáveis (destaque sob o cursor)
            var mouse = Event.current.mousePosition;
            foreach (var alvo in salaIso.Alvos)
            {
                var r = new Rect(NaTela(new Vector2Int(alvo.Area.x, alvo.Area.y)), new Vector2(alvo.Area.width * zoomSala, alvo.Area.height * zoomSala));
                if (Livre && r.Contains(mouse))
                {
                    ui.Ret(new Rect(r.x, r.y, r.width, 2), IsoGui.Cyan); ui.Ret(new Rect(r.x, r.yMax - 2, r.width, 2), IsoGui.Cyan);
                    ui.Ret(new Rect(r.x, r.y, 2, r.height), IsoGui.Cyan); ui.Ret(new Rect(r.xMax - 2, r.y, 2, r.height), IsoGui.Cyan);
                }
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) ClicarNaSala(alvo.Tipo, new Vector2(r.center.x, r.y));
            }

            // placas dos setores
            foreach (var p in salaIso.Placas)
            {
                var pos = NaTela(p.Pos);
                bool alerta = (p.Setor == "Campus" && E.TemQuedaDeEnergia) || (p.Setor == "Storage" && E.DiscoQueimado) || (p.Setor == "NOC" && Incidentes > 0)
                              || (p.Setor == "Energia" && E.Sobrecarga) || (p.Setor == "Refrigeracao" && E.Quente) || (p.Setor == "Rede" && E.LinkSaturado);
                var cor = alerta && Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0 ? IsoGui.Laranja : p.Cor;
                int largura = PixelCanvas.LarguraTexto(p.Nome) * 2 + 22;
                if (ui.Botao(new Rect(pos.x - largura / 2, pos.y - 26, largura, 24), p.Nome, cor, Livre)) Abrir(p.Setor);
            }

            // próxima expansão da sala
            if (salaIso.Expansao.HasValue && E.TemProximoCargo)
            {
                var pos = NaTela(salaIso.Expansao.Value);
                string texto = "EXPANSAO: " + Catalogo.Cargos[E.Cargo + 1].Nome.ToUpperInvariant();
                int largura = PixelCanvas.LarguraTexto(texto) * 2 + 24;
                var r = new Rect(pos.x - largura / 2, pos.y - 14, largura, 28);
                ui.Caixa(r, IsoGui.Cor("2a1f10"), IsoGui.Laranja);
                ui.Texto(texto, r.center.x, r.y + 9, IsoGui.Laranja, 2, true);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir("Carreira");
            }

            // marcador de construção: compra o equipamento principal do cargo
            string item = ItemPrincipal();
            if (salaIso.Marcador.HasValue && !E.NoMaximo(item))
            {
                var pos = NaTela(salaIso.Marcador.Value);
                string rotulo = "+ " + NomeLongo(item).ToUpperInvariant();
                float largura = PixelCanvas.LarguraTexto(rotulo) * 2 + 24;
                var cor = flashCompra > 0 ? IsoGui.Verde : IsoGui.Laranja;
                if (ui.Botao(new Rect(pos.x - largura / 2, pos.y + 12, largura, 26), rotulo, cor, Livre)) Comprar(item);   // embaixo do lugar: não cobre os equipamentos
                ui.Texto(Dinheiro(E.Custo(item)), pos.x, pos.y + 42, PodeComprarAqui(item) ? IsoGui.Verde : IsoGui.Branco, 2, true);
            }

            // pico de tráfego: faixa de alerta embaixo da sala
            if (E.EmPico && !E.PicoFoiEscalado)
            {
                bool piscar = Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0;
                var r = new Rect(areaCentral.x + 150, areaCentral.yMax - 76, areaCentral.width - 300, 34);
                ui.Caixa(r, piscar ? IsoGui.Cor("5a2a10") : IsoGui.Cor("3a1a08"), IsoGui.Laranja);
                string texto = E.PicoViolado ? "SLA VIOLADO! ESCALE PARA RECUPERAR"
                    : "PICO: " + E.NomeDoPico.ToUpperInvariant() + "  /  ESCALE EM " + Numero(Mathf.Ceil((float)(E.LimiteParaEscalar - E.SegundosDePico))) + "S";
                ui.Texto(texto, r.center.x, r.y + 12, IsoGui.Laranja, 2, true);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) Escalar();
            }

            // texto do chamado ao lado do papel
            if (salaIso.Chamado.HasValue && E.TemChamado)
            {
                var pos = NaTela(salaIso.Chamado.Value);
                string texto = "CHAMADO: " + E.TextoDoChamado.ToUpperInvariant() + "  (" + Numero(Mathf.Ceil((float)E.SegundosDoChamado)) + "S)";
                float largura = PixelCanvas.LarguraTexto(texto) * 2 + 20;
                var r = new Rect(pos.x + 24, pos.y - 30, largura, 24);
                ui.Caixa(r, IsoGui.Cor("2a1f10"), IsoGui.Cor("ffd65c"));
                ui.Texto(texto, r.x + 10, r.y + 8, IsoGui.Cor("ffd65c"), 2);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) AtenderChamado(new Vector2(pos.x, pos.y - 20));
            }

            // "+R$" subindo
            for (int i = flutuantes.Count - 1; i >= 0; i--)
            {
                var (texto, pos, nasceu, cor) = flutuantes[i];
                float idade = Time.unscaledTime - nasceu;
                if (idade > 1f) { flutuantes.RemoveAt(i); continue; }
                ui.Texto(texto, pos.x, pos.y - 10 - idade * 30, cor, 2, true);
            }
        }

        /// <summary>Clique num equipamento da sala: conserta o que estiver quebrado ou rende um clique.</summary>
        void ClicarNaSala(string tipo, Vector2 pos)
        {
            if (tipo.StartsWith("servidor:"))
            {
                int s = int.Parse(tipo.Substring(9));
                if (E.Travado(s)) { faixa.Reiniciar(s); Flutuar("REINICIADO", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "rack")
            {
                for (int s = E.Torres; s < E.TotalServidores; s++)
                    if (E.Travado(s)) { faixa.Reiniciar(s); Flutuar("REINICIADO", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "storage" && E.DiscoQueimado) { faixa.TrocarDisco(); Flutuar("DISCO TROCADO", pos, IsoGui.Verde); return; }
            else if (tipo == "containers" && E.DeployQuebrado) { faixa.FazerRollback(); Flutuar("ROLLBACK", pos, IsoGui.Verde); return; }
            else if (tipo == "k8s" && E.EmPico && !E.PicoFoiEscalado) { Escalar(); Flutuar("ESCALADO", pos, IsoGui.Cyan); return; }
            else if (tipo == "noc") { Abrir("NOC"); return; }
            else if (tipo == "cafe") { TomarCafe(pos); return; }
            else if (tipo.StartsWith("dc:"))
            {
                int dc = int.Parse(tipo.Substring(3));
                if (dc == 0) { verCampus = false; Notificar("Dentro do DC-01. \"Ver o campus\" volta para o quarteirão."); return; }
                if (E.DatacenterSemEnergia == dc) { faixa.Religar(); Flutuar("ENERGIA DE VOLTA", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "chamado") { AtenderChamado(pos); return; }

            double valor = E.ClicarEquipamento();
            Sons.Moeda();
            Flutuar("+" + Dinheiro(valor), pos, IsoGui.Cor("ffd65c"));
        }

        // ---------------- Barra superior ----------------

        void Topo()
        {
            ui.Ret(new Rect(0, 0, W, 84), IsoGui.Cor("0b1627"));
            ui.Ret(new Rect(0, 81, W, 3), IsoGui.Borda);
            ui.Icone(0, 20, 19, IsoGui.Cyan, 5);
            ui.Texto("IDLE DATA CENTER", 65, 17, IsoGui.Branco, 3);
            ui.Texto("/ MODO GERENTE", 65, 43, IsoGui.Cyan, 2);
            ui.Texto("O SEU DATA CENTER", 22, 66, IsoGui.Muted, 1);
            Recurso(275, 184, "DINHEIRO", Dinheiro(E.Dinheiro), E.CafeAtivo ? "CAFE X2: " + Numero(Mathf.Ceil((float)E.SegundosDeCafe)) + "S" : "+" + Dinheiro(E.ReceitaPorSegundo) + "/S", E.CafeAtivo ? IsoGui.Cor("ffd65c") : IsoGui.Verde);
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
            ("Campus", "CAMPUS"), ("Melhorias", "MELHORIAS"), ("Carreira", "CARREIRA"),
        };

        void Navegacao()
        {
            ui.Ret(new Rect(0, 86, 176, 814), IsoGui.Cor("101e32"));
            ui.Texto("CENTRAL", 15, 104, IsoGui.Muted, 2);
            for (int i = 0; i < Menus.Length; i++)
            {
                var (id, rotulo) = Menus[i];
                bool travado = SalaIso.CargoDoSetor(id) > E.Cargo;
                var r = new Rect(10, 128 + i * 42, 156, 36);
                bool ativo = selecionado == id;
                ui.Caixa(r, ativo ? IsoGui.Cor("165d81") : IsoGui.Painel, ativo ? IsoGui.Cyan : IsoGui.Borda);
                ui.Icone(i, r.x + 10, r.y + 11, travado ? IsoGui.Borda : ativo ? IsoGui.Cyan : IsoGui.Muted);
                ui.Texto(rotulo, r.x + 32, r.y + 14, travado ? IsoGui.Borda : ativo ? IsoGui.Branco : IsoGui.Muted, 2);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir(id);
            }
            // encolhe o jogo para a faixa acima da barra de tarefas (o ◇ da faixa traz de volta)
            if (ui.Botao(new Rect(10, 786, 156, 88), "IR PARA A FAIXA", IsoGui.Cyan, true, 2)) faixa.FecharGerente();
            ui.Texto("JOGAR TRABALHANDO", 88, 852, IsoGui.Muted, 1, true);
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
