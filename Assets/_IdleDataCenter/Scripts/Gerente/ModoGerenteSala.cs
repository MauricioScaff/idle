using System;
using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    public partial class ModoGerente
    {
        SalaIso salaIso;
        float proximoQuadroSala, proximaRenda;
        Rect retSala;
        float zoomSala = 2;   // pixels do GUI por pixel da arte (na tela é sempre um número inteiro)
        /// <summary>Vista escolhida; se ainda não existe no cargo, cai para a próxima mais de perto.</summary>
        SalaIso.Vista vistaEscolhida = SalaIso.Vista.Mundo;
        SalaIso.Vista VistaAtual =>
            vistaEscolhida == SalaIso.Vista.Mundo && E.NoMundo ? SalaIso.Vista.Mundo
            : vistaEscolhida != SalaIso.Vista.Sala && E.NoCampus ? SalaIso.Vista.Campus : SalaIso.Vista.Sala;
        bool MostrandoCampus => VistaAtual == SalaIso.Vista.Campus;

        static readonly Color Ouro = IsoGui.Cor("ffd65c"), Vermelho = IsoGui.Cor("ff5a6a"), Energia = IsoGui.Cor("ffbf3f"), Frio = IsoGui.Cor("5aa9ff");

        static readonly string[] Moeda = { ".###.", "##.##", "#.#.#", "##.##", ".###." },
            Raio = { "..#..", ".##..", "#####", "..##.", "..#.." }, Termometro = { ".#.", ".#.", ".#.", "###", "###" },
            Rede = { "....#", "...##", "..###", ".####", "#####" }, Sacola = { "#####", "#...#", "#####", "#.#.#", "#####" },
            Engrenagem = { ".#.#.", "#####", ".#.#.", "#####", ".#.#." }, Subir = { "..#..", ".###.", "#.#.#", "..#..", "..#.." },
            IconeFaixa = { ".....", ".....", ".....", "#####", "#####" }, Cadeado = { ".###.", ".#.#.", "#####", "##.##", "#####" },
            Foguete = { "..#..", ".###.", ".###.", "#####", "#.#.#" };

        /// <summary>Textos "+R$" que sobem de onde se clicou (ou de onde o dinheiro está sendo feito).</summary>
        readonly List<(string texto, Vector2 pos, float nasceu, Color cor)> flutuantes = new List<(string, Vector2, float, Color)>();

        bool Livre => string.IsNullOrEmpty(janela);
        bool Pisca => Mathf.FloorToInt(Time.unscaledTime * 3) % 2 == 0;

        /// <summary>O que o marcador "+ ..." do piso compra em cada cargo (o equipamento principal da fase).</summary>
        string ItemPrincipal()
        {
            switch (E.Cargo)
            {
                case 0: return Catalogo.Servidor;
                case 1: return E.TemRack ? Catalogo.Servidor1U : Catalogo.Rack;
                case 2: return Catalogo.RackCheio;
                case 3: return Catalogo.Containers;
                case 4: return Catalogo.NoKubernetes;
                default: return VistaAtual == SalaIso.Vista.Mundo ? Catalogo.Regiao : MostrandoCampus ? Catalogo.Datacenter : Catalogo.NoKubernetes;
            }
        }

        /// <summary>Redesenha a sala (chamado no Update, uns 12 quadros por segundo).</summary>
        void AtualizarSala()
        {
            if (salaIso == null) salaIso = new SalaIso(E) { MostrarExpansao = false };
            if (Time.unscaledTime < proximoQuadroSala) return;
            proximoQuadroSala = Time.unscaledTime + 1f / 12f;
            salaIso.Desenhar(Time.unscaledTime, VistaAtual);
        }

        Vector2 NaTela(Vector2Int p) => new Vector2(retSala.x + p.x * zoomSala, retSala.y + p.y * zoomSala);

        Rect NaTela(RectInt r) => new Rect(NaTela(new Vector2Int(r.x, r.y)), new Vector2(r.width * zoomSala, r.height * zoomSala));

        void Flutuar(string texto, Vector2 pos, Color cor) => flutuantes.Add((texto, pos, Time.unscaledTime, cor));

        // ---------------- Sala ----------------

        void DesenharSala()
        {
            if (salaIso == null || salaIso.Textura == null) AtualizarSala();
            // Pixel-perfect: a interface inteira é escalada para caber na janela (fator quebrado, ex. 1,15), então o zoom
            // da sala é escolhido em pixels de TELA (inteiro) e a textura é desenhada fora da matriz do GUI.
            // Assim cada pixel da arte vira sempre o mesmo quadrado na tela, sem pixels de tamanhos diferentes.
            var area = VistaAtual == SalaIso.Vista.Sala ? salaIso.AreaDaSala : new RectInt(0, 0, salaIso.Largura, salaIso.Altura);
            var matriz = GUI.matrix;
            float escalaGui = matriz.m00;
            int zoomTela = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(areaSala.width * escalaGui / area.width, areaSala.height * escalaGui / area.height)));
            zoomSala = zoomTela / escalaGui;
            Vector3 centro = matriz.MultiplyPoint3x4(new Vector3(areaSala.center.x, areaSala.center.y, 0));
            float sx = Mathf.Round(centro.x - (area.x + area.width / 2f) * zoomTela), sy = Mathf.Round(centro.y - (area.y + area.height / 2f) * zoomTela);
            GUI.matrix = Matrix4x4.identity;
            GUI.DrawTexture(new Rect(sx, sy, salaIso.Largura * zoomTela, salaIso.Altura * zoomTela), salaIso.Textura, ScaleMode.StretchToFill);
            GUI.matrix = matriz;
            // o mesmo retângulo nas coordenadas do GUI (para cliques, placas e efeitos)
            var inv = matriz.inverse;
            Vector3 canto = inv.MultiplyPoint3x4(new Vector3(sx, sy, 0));
            retSala = new Rect(canto.x, canto.y, salaIso.Largura * zoomSala, salaIso.Altura * zoomSala);

            Vida();

            // equipamentos clicáveis: só o que está sob o cursor (pelo desenho do objeto, o da frente ganha). Objetos com
            // pixels ganham um contorno no próprio desenho; os outros, o retângulo
            var mouse = Event.current.mousePosition;
            int sob = Livre && retSala.Contains(mouse) ? salaIso.AlvoEm((mouse - retSala.position) / zoomSala) : -1;
            salaIso.DestaqueTipo = null;
            if (sob >= 0)
            {
                var alvo = salaIso.Alvos[sob];
                var r = NaTela(alvo.Area);
                if (alvo.Px != null) { salaIso.DestaqueTipo = alvo.Tipo; salaIso.DestaquePos = alvo.Area.position; }
                else
                {
                    ui.Ret(new Rect(r.x, r.y, r.width, 2), IsoGui.Cyan); ui.Ret(new Rect(r.x, r.yMax - 2, r.width, 2), IsoGui.Cyan);
                    ui.Ret(new Rect(r.x, r.y, 2, r.height), IsoGui.Cyan); ui.Ret(new Rect(r.xMax - 2, r.y, 2, r.height), IsoGui.Cyan);
                }
                if (GUI.Button(new Rect(mouse.x - 1, mouse.y - 1, 2, 2), GUIContent.none, GUIStyle.none)) ClicarNaSala(alvo.Tipo, new Vector2(r.center.x, r.y));
            }

            // placas dos setores: abrem a loja na aba do setor
            foreach (var p in salaIso.Placas)
            {
                var pos = NaTela(p.Pos);
                bool alerta = (p.Setor == "Mundo" && E.TemPaneRegional) || (p.Setor == "Campus" && E.TemQuedaDeEnergia) || (p.Setor == "Storage" && E.DiscoQueimado) || (p.Setor == "NOC" && Incidentes > 0)
                              || (p.Setor == "Energia" && E.Sobrecarga) || (p.Setor == "Refrigeracao" && E.Quente) || (p.Setor == "Rede" && E.LinkSaturado);
                var cor = alerta && Pisca ? IsoGui.Laranja : p.Cor;
                float largura = ui.Largura(p.Nome, 2) + 26;
                if (ui.Botao(new Rect(pos.x - largura / 2, pos.y - 30, largura, 28), p.Nome, cor, Livre)) Abrir(p.Setor);
            }

            // marcador de construção: compra o equipamento principal do cargo
            string item = ItemPrincipal();
            if (salaIso.Marcador.HasValue && !E.NoMaximo(item))
            {
                var pos = NaTela(salaIso.Marcador.Value);
                string rotulo = "+ " + NomeLongo(item);
                float largura = ui.Largura(rotulo, 2) + 28;
                var cor = flashCompra > 0 ? IsoGui.Verde : IsoGui.Laranja;
                if (ui.Botao(new Rect(pos.x - largura / 2, pos.y + 12, largura, 30), rotulo, cor, Livre)) Comprar(item);   // embaixo do lugar: não cobre os equipamentos
                ui.Texto(Dinheiro(E.Custo(item)), pos.x, pos.y + 48, PodeComprarAqui(item) ? IsoGui.Verde : IsoGui.Branco, 2, true);
            }

            // texto do chamado ao lado do papel
            if (salaIso.Chamado.HasValue && E.TemChamado)
            {
                var pos = NaTela(salaIso.Chamado.Value);
                string texto = "Chamado: " + E.TextoDoChamado + "  (" + Numero(Mathf.Ceil((float)E.SegundosDoChamado)) + "s)";
                float largura = ui.Largura(texto, 2) + 20;
                var r = new Rect(pos.x + 24, pos.y - 30, largura, 26);
                ui.Caixa(r, IsoGui.Cor("2a1f10"), Ouro);
                ui.Texto(texto, r.x + 10, r.y + 8, Ouro, 2);
                if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) AtenderChamado(new Vector2(pos.x, pos.y - 20));
            }

            // "+R$" subindo e sumindo
            for (int i = flutuantes.Count - 1; i >= 0; i--)
            {
                var (texto, pos, nasceu, cor) = flutuantes[i];
                float idade = Time.unscaledTime - nasceu;
                if (idade > 1.2f) { flutuantes.RemoveAt(i); continue; }
                cor.a = Mathf.Clamp01(1.4f - idade);
                ui.Texto(texto, pos.x, pos.y - 10 - idade * 36, cor, 2, true);
            }
        }

        /// <summary>
        /// Sala viva: o dinheiro sobe dos equipamentos que estão rendendo, o que quebrou solta faíscas
        /// e a caneca fumega enquanto o café faz efeito.
        /// </summary>
        void Vida()
        {
            var renda = new List<Rect>();
            foreach (var a in salaIso.Alvos)
            {
                var r = NaTela(a.Area);
                bool quebrado = (a.Tipo.StartsWith("servidor:") && E.Travado(int.Parse(a.Tipo.Substring(9))))
                             || (a.Tipo == "rack" && E.Travamentos.Any(t => t.servidor >= E.Torres))
                             || (a.Tipo == "storage" && E.DiscoQueimado) || (a.Tipo == "containers" && E.DeployQuebrado)
                             || (a.Tipo == "dc:" + E.DatacenterSemEnergia && E.TemQuedaDeEnergia) || (a.Tipo == "regiao:" + E.RegiaoEmPane && E.TemPaneRegional);
                if (quebrado) Faiscas(r);
                else if (a.Tipo.StartsWith("servidor:") || a.Tipo == "rack" || a.Tipo == "equipamento" || a.Tipo == "containers" || a.Tipo == "k8s" || a.Tipo.StartsWith("dc:") || a.Tipo.StartsWith("regiao:"))
                    renda.Add(r);
                if (a.Tipo == "cafe" && E.CafeAtivo) Fumaca(r);
            }
            if (renda.Count > 0 && Time.unscaledTime >= proximaRenda && E.ReceitaPorSegundo > 0)
            {
                const float intervalo = 1.1f;
                proximaRenda = Time.unscaledTime + intervalo;
                var r = renda[UnityEngine.Random.Range(0, renda.Count)];
                Flutuar("+" + Dinheiro(E.ReceitaPorSegundo * intervalo), new Vector2(r.center.x, r.y + 6), Ouro);
            }
        }

        void Faiscas(Rect r)
        {
            float t = Time.unscaledTime;
            for (int k = 0; k < 5; k++)
            {
                float a = t * 7 + k * 1.3f;
                if (Mathf.Sin(a * 3) <= 0) continue;
                ui.Ret(new Rect(r.center.x + Mathf.Cos(a) * r.width * 0.45f, r.y + r.height * 0.3f + Mathf.Sin(a * 1.7f) * r.height * 0.25f, 4, 4), k % 2 == 0 ? Ouro : Color.white);
            }
        }

        void Fumaca(Rect r)
        {
            float t = Time.unscaledTime;
            for (int k = 0; k < 3; k++)
            {
                float f = (t * 0.6f + k / 3f) % 1f;
                ui.Ret(new Rect(r.center.x + Mathf.Sin(f * 6 + k) * 3 * zoomSala, r.y - f * 12 * zoomSala, zoomSala * 2, zoomSala * 2), new Color(0.91f, 0.93f, 0.97f, 0.6f * (1 - f)));
            }
        }

        /// <summary>Clique num equipamento da sala: conserta o que estiver quebrado ou rende um clique.</summary>
        void ClicarNaSala(string tipo, Vector2 pos)
        {
            if (tipo.StartsWith("servidor:"))
            {
                int s = int.Parse(tipo.Substring(9));
                if (E.Travado(s)) { faixa.Reiniciar(s); Flutuar("Reiniciado", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "rack")
            {
                for (int s = E.Torres; s < E.TotalServidores; s++)
                    if (E.Travado(s)) { faixa.Reiniciar(s); Flutuar("Reiniciado", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "storage" && E.DiscoQueimado) { faixa.TrocarDisco(); Flutuar("Disco trocado", pos, IsoGui.Verde); return; }
            else if (tipo == "containers" && E.DeployQuebrado) { faixa.FazerRollback(); Flutuar("Rollback", pos, IsoGui.Verde); return; }
            else if (tipo == "k8s" && E.EmPico && !E.PicoFoiEscalado) { Escalar(); Flutuar("Escalado", pos, IsoGui.Cyan); return; }
            else if (tipo == "noc") { Abrir("NOC"); return; }
            else if (tipo == "cafe") { TomarCafe(pos); return; }
            else if (tipo.StartsWith("regiao:"))
            {
                int r = int.Parse(tipo.Substring(7));
                if (r == 0) { vistaEscolhida = SalaIso.Vista.Campus; Notificar("Campus da sede, na América do Sul."); return; }
                if (E.RegiaoEmPane == r) { faixa.Redirecionar(); Flutuar("Tráfego redirecionado", pos, IsoGui.Verde); return; }
            }
            else if (tipo.StartsWith("dc:"))
            {
                int dc = int.Parse(tipo.Substring(3));
                if (dc == 0) { vistaEscolhida = SalaIso.Vista.Sala; Notificar("Dentro do DC-01. O botão CAMPUS volta para o quarteirão."); return; }
                if (E.DatacenterSemEnergia == dc) { faixa.Religar(); Flutuar("Energia de volta", pos, IsoGui.Verde); return; }
            }
            else if (tipo == "chamado") { AtenderChamado(pos); return; }

            double valor = E.ClicarEquipamento();
            Sons.Moeda();
            Flutuar("+" + Dinheiro(valor), pos, Ouro);
        }

        // ---------------- Topo: dinheiro, meta e cargo ----------------

        void Hud()
        {
            // dinheiro
            ui.Caixa(new Rect(20, 20, 330, 104));
            ui.Texto(E.CafeAtivo ? "Dinheiro  ·  café ×2: " + Numero(Mathf.Ceil((float)E.SegundosDeCafe)) + "s" : "Dinheiro", 40, 34, E.CafeAtivo ? Ouro : IsoGui.Muted, 2);
            Mapa(Moeda, 40, 58, Ouro, 5);
            string dinheiro = Dinheiro(E.Dinheiro);
            ui.Texto(dinheiro, 76, 58, IsoGui.Branco, ui.Largura(dinheiro, 5) <= 260 ? 5 : 4);
            ui.Texto("+" + Dinheiro(E.ReceitaPorSegundo) + "/s", 40, 98, IsoGui.Verde, 2);

            Meta(new Rect(460, 20, 520, 104));

            // cargo e medidores
            ui.Caixa(new Rect(1090, 20, 330, 104));
            string cargo = E.CargoAtual.Nome;
            ui.Texto(cargo, 1110, 34, Ouro, ui.Largura(cargo, 3) <= 290 ? 3 : 2);
            ui.Texto(E.CargoAtual.Lugar, 1110, 60, IsoGui.Muted, 2);
            float x = 1110;
            Medidor(ref x, 86, Raio, Energia, E.Sobrecarga, Numero(Mathf.Round((float)(E.ConsumoKw / Math.Max(0.001, E.CapacidadeKw) * 100))) + "%");
            Medidor(ref x, 86, Termometro, Frio, E.Quente, Numero(Mathf.Round((float)E.Temperatura)) + "°C");
            if (E.NaSalaDeRacks) Medidor(ref x, 86, Rede, IsoGui.Roxo, E.LinkSaturado, Numero(Mathf.Round((float)(E.TrafegoMbps / Math.Max(0.001, E.BandaMbps) * 100))) + "%");
        }

        void Medidor(ref float x, float y, string[] icone, Color cor, bool alerta, string texto)
        {
            var c = alerta ? (Pisca ? Vermelho : IsoGui.Laranja) : cor;
            Mapa(icone, x, y, c, 3);
            ui.Texto(texto, x + 22, y + 3, alerta ? c : IsoGui.Branco, 2);
            x += 100;
        }

        /// <summary>A meta mais perto de ser cumprida; com tudo pronto, vira o botão da promoção (ou do IPO). Clicar abre a carreira.</summary>
        void Meta(Rect r)
        {
            var metas = E.CargoAtual.MetasParaPromocao;
            if (E.PodePromover || E.PodeFazerIpo)
            {
                if (ui.Botao(r, E.PodePromover ? "Ser promovido!" : "Abrir o capital!", Pisca ? Ouro : IsoGui.Laranja, Livre, 4))
                {
                    if (E.PodePromover) faixa.Promover(); else faixa.FazerIpo();
                }
                return;
            }
            ui.Caixa(r);
            MetaDef proxima = metas.Where(m => !E.Cumprida(m)).OrderByDescending(m => E.Progresso(m) / m.Alvo).FirstOrDefault();
            if (proxima == null)
            {
                ui.Texto(E.IpoFeito ? "Empresa na bolsa" : "Carreira", r.x + 20, r.y + 14, IsoGui.Muted, 2);
                ui.Texto(E.IpoFeito ? "Você chegou ao topo" : "Cargo máximo por ora", r.x + 20, r.y + 40, Ouro, 3);
                ui.Texto(E.IpoFeito ? "Prestígio: venda e recomece com bônus" : "", r.x + 20, r.y + 74, IsoGui.Muted, 2);
            }
            else
            {
                int feitas = metas.Count(m => E.Cumprida(m));
                ui.Texto(E.TemProximoCargo ? "Meta para " + Catalogo.Cargos[E.Cargo + 1].Nome : "Meta para o IPO", r.x + 20, r.y + 14, IsoGui.Muted, 2);
                string fracao = feitas + "/" + metas.Length;
                ui.Texto(fracao, r.xMax - 20 - ui.Largura(fracao, 2), r.y + 14, IsoGui.Cyan, 2);
                ui.Texto(proxima.Texto, r.x + 20, r.y + 36, IsoGui.Branco, 3);
                ui.Barra(new Rect(r.x + 20, r.y + 64, r.width - 40, 22), E.Progresso(proxima) / proxima.Alvo, IsoGui.Cyan);
            }
            if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir("Carreira");
        }

        // ---------------- Direita: o que pede atenção agora (ou a próxima compra) ----------------

        void CartaoAtencao()
        {
            var r = new Rect(1140, 140, 280, 190);
            if (E.EmPico && !E.PicoFoiEscalado)
                Alerta(r, E.NomeDoPico, E.PicoViolado ? "SLA violado!" : "Escale em " + Numero(Mathf.Max(0, Mathf.Ceil((float)(E.LimiteParaEscalar - E.SegundosDePico)))) + "s", "Escalar", Escalar);
            else if (Incidentes > 1) Alerta(r, Incidentes + " incidentes", "O NOC está piscando", "Resolver tudo", Resolver);
            else if (E.TemQuedaDeEnergia) Alerta(r, "Queda de energia", "DC-0" + (E.DatacenterSemEnergia + 1) + " apagado", "Religar", () => { faixa.Religar(); Notificar("Energia de volta no DC-0" + (E.DatacenterSemEnergia + 1) + "."); });
            else if (E.TemPaneRegional) Alerta(r, "Pane regional", Catalogo.NomesRegioes[E.RegiaoEmPane], "Redirecionar", faixa.Redirecionar);
            else if (E.DeployQuebrado) Alerta(r, "Deploy quebrou", "Apps fora do ar", "Rollback", faixa.FazerRollback);
            else if (E.DiscoQueimado) Alerta(r, "Disco queimou", "No storage", "Trocar disco", faixa.TrocarDisco);
            else if (E.Travamentos.Count > 0) Alerta(r, "Servidor travou", "O técnico vai consertar", "Reiniciar", Resolver);
            else if (E.TemChamado)
                Alerta(r, "Chamado urgente", Cortar(E.TextoDoChamado, 30), "Atender " + Numero(Mathf.Ceil((float)E.SegundosDoChamado)) + "s", () => AtenderChamado(new Vector2(r.center.x, r.y)), Ouro);
            else ProximaCompra(r);
        }

        void Alerta(Rect r, string titulo, string detalhe, string botao, Action acao, Color? cor = null)
        {
            var c = cor ?? (Pisca ? IsoGui.Laranja : IsoGui.Cor("ff8a3a"));
            ui.Caixa(r, IsoGui.Cor("3a2410"), c);
            ui.Texto("!", r.center.x, r.y + 14, c, 6, true);
            ui.Texto(Cortar(titulo, 18), r.center.x, r.y + 60, c, 3, true);
            ui.Texto(detalhe, r.center.x, r.y + 92, IsoGui.Branco, 2, true);
            if (ui.Botao(new Rect(r.x + 20, r.y + 124, r.width - 40, 46), botao, c, Livre, 3)) acao();
        }

        void ProximaCompra(Rect r)
        {
            var def = E.MelhoriaSugerida();
            if (def == null)
            {
                ui.Caixa(r);
                ui.Texto("Tudo comprado", r.x + 20, r.y + 16, IsoGui.Muted, 2);
                ui.Texto("Neste cargo", r.x + 20, r.y + 40, IsoGui.Branco, 3);
                if (ui.Botao(new Rect(r.x + 20, r.y + 124, r.width - 40, 46), "Ver metas", IsoGui.Cyan, Livre, 3)) Abrir("Carreira");
                return;
            }
            bool pode = E.PodeComprar(def.Id);
            ui.Caixa(r, pode ? IsoGui.Cor("173a2e") : IsoGui.Painel, pode ? IsoGui.Verde : IsoGui.Borda);
            ui.Texto("Próxima compra", r.x + 20, r.y + 16, IsoGui.Muted, 2);
            string nome = NomeLongo(def.Id) + (def.Gerador ? " nº " + (E.UnidadesDoGerador(def.Id) + 1) : def.NivelMaximo > 1 ? " " + (E.Nivel(def.Id) + 1) + "/" + def.NivelMaximo : "");
            ui.Texto(nome, r.x + 20, r.y + 40, pode ? IsoGui.Branco : IsoGui.Muted, ui.Largura(nome, 3) <= r.width - 40 ? 3 : 2);
            ui.Texto(Cortar(def.Efeito, 29), r.x + 20, r.y + 68, IsoGui.Muted, 2);
            ui.Texto(Dinheiro(E.Custo(def.Id)), r.x + 20, r.y + 92, pode ? IsoGui.Verde : IsoGui.Laranja, 3);
            var botao = new Rect(r.x + 20, r.y + 124, r.width - 40, 46);
            if (pode) { if (ui.Botao(botao, "Comprar", IsoGui.Verde, Livre, 3)) Comprar(def.Id); }
            else
            {
                ui.Barra(botao, E.Dinheiro / Math.Max(1, E.Custo(def.Id)), IsoGui.Borda);
                ui.Texto("Juntando...", botao.center.x, botao.y + 18, IsoGui.Muted, 2, true);
            }
        }

        // ---------------- Embaixo: os botões ----------------

        void Barra()
        {
            bool deploy = E.HostsContainers > 0;
            int n = deploy ? 4 : 3;
            const float largura = 230, altura = 64, espaco = 18;
            float x = W / 2 - (n * largura + (n - 1) * espaco) / 2, y = 812;
            if (BotaoIcone(new Rect(x, y, largura, altura), "Loja", IsoGui.Cyan, Sacola, 4, EmLoja)) Abrir(ultimaAbaLoja);
            x += largura + espaco;
            bool automacao = E.AutomacoesLiberadas;
            var rAuto = new Rect(x, y, largura, altura);
            if (BotaoIcone(rAuto, "Automação", automacao ? IsoGui.Verde : IsoGui.Borda, automacao ? Engrenagem : Cadeado, 3, janela == "Automacao"))
            {
                if (automacao) Abrir("Automacao");
                else Notificar("Automações chegam no cargo Analista de Infra.");
            }
            if (E.Escrevendo) ui.Barra(new Rect(rAuto.x + 10, rAuto.yMax - 14, rAuto.width - 20, 10), E.ProgressoEscrita, IsoGui.Roxo);
            x += largura + espaco;
            if (BotaoIcone(new Rect(x, y, largura, altura), "Carreira", IsoGui.Roxo, Subir, 4, janela == "Carreira" || janela == "Prestigio" || janela == "Vender")) Abrir("Carreira");
            if (deploy)
            {
                x += largura + espaco;
                if (BotaoIcone(new Rect(x, y, largura, altura), "Deploy", IsoGui.Cor("5899ff"), Foguete, 4, false)) Deploy();
            }

            // encolhe o jogo para a faixa acima da barra de tarefas (o ◇ da faixa traz de volta)
            if (BotaoIcone(new Rect(20, 828, 190, 48), "Faixa", IsoGui.Borda, IconeFaixa, 3, false)) faixa.FecharGerente();

            // sala, campus e mundo (Arquiteto em diante), embaixo do cartão da direita
            if (E.NoCampus)
            {
                var vistas = E.NoMundo ? new[] { SalaIso.Vista.Sala, SalaIso.Vista.Campus, SalaIso.Vista.Mundo } : new[] { SalaIso.Vista.Sala, SalaIso.Vista.Campus };
                float larguraVista = (280 - (vistas.Length - 1) * 8f) / vistas.Length, vx = 1140;
                foreach (var v in vistas)
                {
                    bool atual = VistaAtual == v;
                    string nome = v == SalaIso.Vista.Sala ? "DC-01" : v == SalaIso.Vista.Campus ? "Campus" : "Mundo";
                    if (ui.Botao(new Rect(vx, 344, larguraVista, 40), nome, atual ? IsoGui.Cyan : IsoGui.Borda, Livre)) vistaEscolhida = v;
                    vx += larguraVista + 8;
                }
            }
        }

        bool EmLoja => Array.IndexOf(AbasDaLoja, janela) >= 0;

        /// <summary>Botão grande com ícone (o IsoGui.Botao só tem texto).</summary>
        bool BotaoIcone(Rect r, string titulo, Color cor, string[] icone, int escala, bool ativo)
        {
            bool sobre = r.Contains(Event.current.mousePosition);
            ui.Caixa(r, Color.Lerp(cor, IsoGui.Painel, ativo || sobre ? 0.45f : 0.72f), cor);
            ui.Ret(new Rect(r.x + 2, r.yMax - 6, r.width - 4, 4), new Color(0, 0, 0, .22f));
            float largura = ui.Largura(titulo, escala) + 15 + 12;
            float x = r.center.x - largura / 2;
            Mapa(icone, x, r.center.y - 8, IsoGui.Branco, 3);
            ui.Texto(titulo, x + 27, r.center.y - escala * 2.5f - 1, IsoGui.Branco, escala);
            bool clicou = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (clicou) Sons.Tique();
            return clicou;
        }

        /// <summary>Aviso curto embaixo da meta (a barra de notícias).</summary>
        void Aviso(string texto)
        {
            texto = CaberEm(texto, 1080 - 48, 2);
            float largura = ui.Largura(texto, 2) + 48;
            var r = new Rect(W / 2 - largura / 2, 138, largura, 36);
            ui.Caixa(r, IsoGui.Cor("162c40"), IsoGui.Cyan);
            ui.Texto(texto, r.center.x, r.y + 13, IsoGui.Branco, 2, true);
        }

        /// <summary>Corta o texto (com reticências) até caber na largura.</summary>
        string CaberEm(string texto, float largura, int escala)
        {
            if (ui.Largura(texto, escala) <= largura) return texto;
            while (texto.Length > 1 && ui.Largura(texto + "...", escala) > largura) texto = texto.Substring(0, texto.Length - 1);
            return texto.TrimEnd() + "...";
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
