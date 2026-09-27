using UnityEngine;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter.Isometrico
{
    public partial class CentroDados
    {
        Vector2 Ponto(float x, float y) => new Vector2(sala.x + sala.width * x, sala.y + sala.height * y);

        void DesenharSala()
        {
            sala = areaCentral;
            if (ambiente != null)
            {
                float escala = Mathf.Min(areaCentral.width / ambiente.width, areaCentral.height / ambiente.height);
                sala = new Rect(areaCentral.center.x - ambiente.width * escala / 2, areaCentral.center.y - ambiente.height * escala / 2,
                    ambiente.width * escala, ambiente.height * escala);
                GUI.DrawTexture(sala, ambiente, ScaleMode.StretchToFill);
            }
            ui.Ret(new Rect(areaCentral.x, areaCentral.y, areaCentral.width, 23), new Color(.04f, .1f, .18f, .9f));
            ui.Texto("DC-01  /  LIVE OPERATIONS", areaCentral.x + 12, areaCentral.y + 7, IsoGui.Cyan);
            ui.Texto("CLICK A SECTOR TO MANAGE", areaCentral.xMax - 220, areaCentral.y + 7, IsoGui.Muted);

            bool livre = string.IsNullOrEmpty(janela);
            for (int i = 0; i < racks.Length; i++)
            {
                Vector2 p = Ponto(racks[i].x, racks[i].y);
                float width = sala.width * .067f, height = width * 1.68f;
                if (i < jogo.Racks)
                {
                    var r = new Rect(p.x - width / 2, p.y - height, width, height);
                    GUI.DrawTexture(r, rack, ScaleMode.StretchToFill);
                    bool falha = i == 0 && E.Travamentos.Count > 0;
                    for (int led = 0; led < 4; led++)
                        if (Mathf.Sin(Time.unscaledTime * (3 + led) + i * 2 + led) > -.25f)
                            ui.Ret(new Rect(p.x - width * .23f, p.y - height * (.24f + led * .1f), 2, 2), falha ? IsoGui.Laranja : IsoGui.Verde);
                    if (falha) ui.Texto("!", p.x, r.y - 10, IsoGui.Laranja, 3, true);
                    if (livre && GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        if (falha) Reparar();
                        else { double valor = E.ClicarEquipamento(); Notificar("Compute job delivered: +" + Dinheiro(valor)); if (som) Sons.Moeda(); }
                    }
                }
                else
                {
                    Lote(p, 28, IsoGui.Cyan);
                    if (livre && new Rect(p.x - 30, p.y - 16, 60, 32).Contains(Event.current.mousePosition))
                        ui.Texto(Dinheiro(E.Custo(Catalogo.RackCheio)), p.x, p.y - 30, IsoGui.Branco, 2, true);
                    if (livre && GUI.Button(new Rect(p.x - 30, p.y - 16, 60, 32), GUIContent.none, GUIStyle.none)) Comprar(Catalogo.RackCheio);
                }
            }

            // Os hosts aparecem fisicamente no piso de expansão quando comprados.
            for (int i = 0; i < E.HostsContainers; i++)
            {
                Vector2 p = Ponto(.43f + i * .043f, .77f + i * .027f);
                GUI.color = new Color(.8f, .94f, 1);
                GUI.DrawTexture(new Rect(p.x - 23, p.y - 78, 46, 78), rack);
                GUI.color = Color.white;
            }

            if (!E.NoMaximo(Catalogo.RackCheio))
            {
                var p = Ponto(.50f, .78f);
                Lote(p, 74, flashCompra > 0 ? IsoGui.Verde : IsoGui.Laranja);
                ui.Caixa(new Rect(p.x - 18, p.y - 50, 36, 32), IsoGui.Cor("d49335"), IsoGui.Laranja);
                ui.Texto("+", p.x, p.y - 42, Color.white, 3, true);
                if (ui.Botao(new Rect(p.x - 68, p.y - 8, 136, 28), "BUILD RACK", IsoGui.Laranja, livre)) Comprar(Catalogo.RackCheio);
                ui.Texto(Dinheiro(E.Custo(Catalogo.RackCheio)), p.x, p.y + 28, IsoGui.Branco, 2, true);
            }

            for (int i = 0; i < 2 + (E.TemEstagiario ? 1 : 0); i++)
            {
                float t = Time.unscaledTime * .035f + i * .31f;
                float x = Mathf.Lerp(.32f, .68f, Mathf.PingPong(t, 1));
                float y = .69f + i * .047f + (x - .4f) * .23f;
                var p = Ponto(x, y);
                int frame = (Mathf.FloorToInt(Time.unscaledTime * 5) + i) % passos.Length;
                var tex = passos[frame];
                float largura = 40 * passosAspecto[frame];
                Rect uv = passosUv[frame];
                if (Mathf.FloorToInt(t) % 2 == 1) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
                ui.Ret(new Rect(p.x - 8, p.y - 2, 16, 3), new Color(.1f, .2f, .3f, .25f));
                if (tex != null) GUI.DrawTextureWithTexCoords(new Rect(p.x - largura / 2, p.y - 40, largura, 40), tex, uv);
            }

            var d = Ponto(jogo.DroneAtivo ? .57f + Mathf.Sin(Time.unscaledTime * .25f) * .14f : .73f,
                jogo.DroneAtivo ? .64f + Mathf.Cos(Time.unscaledTime * .25f) * .06f : .74f);
            GUI.color = jogo.DroneAtivo ? Color.white : new Color(.62f, .72f, .79f, 1);
            GUI.DrawTexture(new Rect(d.x - 27, d.y - 38 - Mathf.Sin(Time.unscaledTime * 2) * 3, 54, 46), drone);
            GUI.color = Color.white;
            if (!jogo.DroneAtivo) ui.Texto("PROTOTYPE", d.x, d.y + 11, IsoGui.Cyan, 1, true);

            Placa("NOC", .155f, .205f, "Overview", IsoGui.Cyan);
            Placa("COMPUTE", .46f, .10f, "Compute", IsoGui.Cyan);
            Placa("STORAGE", .645f, .225f, "Storage", IsoGui.Cyan);
            Placa("NETWORK", .49f, .475f, "Network", IsoGui.Cyan);
            Placa("POWER", .235f, .51f, "Power", IsoGui.Laranja);
            Placa("COOLING", .80f, .515f, "Cooling", IsoGui.Cyan);
            Placa("RESEARCH", .84f, .28f, "Research", IsoGui.Roxo);
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

        void Placa(string nome, float x, float y, string secao, Color accent)
        {
            var p = Ponto(x, y);
            int largura = PixelCanvas.LarguraTexto(nome) * 2 + 22;
            if (ui.Botao(new Rect(p.x - largura / 2, p.y, largura, 26), nome, accent, string.IsNullOrEmpty(janela)))
            {
                if (nome == "NOC") Reparar();
                else if (nome == "STORAGE" && E.DiscoQueimado) Reparar();
                else Abrir(secao);
            }
        }

        void Topo()
        {
            ui.Ret(new Rect(0, 0, W, 84), IsoGui.Cor("0b1627"));
            ui.Ret(new Rect(0, 81, W, 3), IsoGui.Borda);
            ui.Icone(0, 20, 19, IsoGui.Cyan, 5);
            ui.Texto("IDLE DEVOPS", 65, 17, IsoGui.Branco, 3);
            ui.Texto("/ DATA CENTER", 65, 43, IsoGui.Cyan, 2);
            ui.Texto("BUILD. DEPLOY. GROW.", 22, 66, IsoGui.Muted, 1);
            Recurso(275, 184, "FUNDS", Dinheiro(E.Dinheiro), "+" + Dinheiro(E.ReceitaPorSegundo) + "/S", IsoGui.Verde);
            Recurso(469, 174, "POWER", Numero(E.ConsumoKw) + " KW", "/ " + Numero(E.CapacidadeKw) + " KW", E.Sobrecarga ? IsoGui.Laranja : IsoGui.Laranja);
            Recurso(653, 174, "COOLING", Numero(E.FatorTemperatura * 100) + "%", Numero(E.Temperatura) + " C", E.Quente ? IsoGui.Laranja : IsoGui.Cyan);
            Recurso(837, 150, "RACKS", jogo.Racks + " / 5", E.ContagemServidores + " SERVERS", IsoGui.Cyan);
            Recurso(997, 154, "ENGINEERS", jogo.Engenheiros.ToString(), "OPERATIONS TEAM", IsoGui.Roxo);
            Recurso(1161, 264, "DATACENTER LEVEL", "LV " + jogo.Nivel, "DEVOPS ENGINEER", IsoGui.Laranja);
        }

        void Recurso(int x, int largura, string nome, string valor, string extra, Color c)
        {
            ui.Caixa(new Rect(x, 10, largura, 63));
            ui.Ret(new Rect(x + 2, 12, 3, 59), c);
            ui.Texto(nome, x + 13, 19, IsoGui.Muted, 1);
            ui.Texto(valor, x + 13, 33, c, 3);
            ui.Texto(extra, x + 13, 57, IsoGui.Muted, 1);
        }

        void Navegacao()
        {
            ui.Ret(new Rect(0, 86, 176, 814), IsoGui.Cor("101e32"));
            ui.Texto("CONTROL CENTER", 15, 104, IsoGui.Muted, 2);
            for (int i = 0; i < menus.Length; i++)
            {
                var r = new Rect(10, 135 + i * 45, 156, 38);
                bool ativo = selecionado == menus[i];
                ui.Caixa(r, ativo ? IsoGui.Cor("165d81") : IsoGui.Painel, ativo ? IsoGui.Cyan : IsoGui.Borda);
                ui.Icone(i, r.x + 10, r.y + 11, ativo ? IsoGui.Cyan : IsoGui.Muted);
                ui.Texto(menus[i], r.x + 32, r.y + 14, ativo ? IsoGui.Branco : IsoGui.Muted, 2);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir(menus[i]);
            }
            ui.Caixa(new Rect(10, 651, 156, 116));
            ui.Icone(3, 25, 665, IsoGui.Verde, 3);
            ui.Texto("SYSTEM ONLINE", 20, 699, IsoGui.Verde, 2);
            ui.Texto("AUTOSAVE EVERY 20S", 20, 723, IsoGui.Muted, 1);
            ui.Texto("B BUILD / R RESEARCH", 20, 743, IsoGui.Muted, 1);
            if (ui.Botao(new Rect(10, 786, 156, 38), "SAVE", IsoGui.Cyan)) Salvar(true);
            if (ui.Botao(new Rect(10, 838, 156, 36), som ? "SOUND ON" : "SOUND OFF", IsoGui.Borda))
            {
                som = !som;
                Notificar(som ? "Sound enabled." : "Sound muted.");
            }
        }

        void Objetivos()
        {
            const int x = 1174, width = 252;
            ui.Caixa(new Rect(x, 94, width, 242));
            ui.Texto("OBJECTIVES", x + 17, 113, IsoGui.Branco, 3);
            ui.Ret(new Rect(x + 12, 141, width - 24, 2), IsoGui.Borda);
            ui.Texto(jogo.MetasConcluidas ? "COMPUTE EXPANDED" : "REACH " + jogo.AlvoRacks + " RACKS", x + 17, 158, IsoGui.Branco, 2);
            ui.Texto(jogo.Racks + " / " + jogo.AlvoRacks, x + 17, 180, IsoGui.Cyan, 2);
            ui.Barra(new Rect(x + 16, 202, 219, 14), (double)jogo.Racks / jogo.AlvoRacks, IsoGui.Verde);
            ui.Texto("AUTOMATION DRONE", x + 17, 237, IsoGui.Branco, 2);
            ui.Texto(jogo.DroneAtivo ? "ONLINE / WATCHDOG" : "RESEARCH WATCHDOG", x + 17, 259, IsoGui.Cyan, 2);
            double progresso = jogo.DroneAtivo ? 1 : E.Estado.escrevendo == Catalogo.Watchdog ? E.ProgressoEscrita : 0;
            ui.Barra(new Rect(x + 16, 281, 219, 14), progresso, IsoGui.Cyan);
            if (GUI.Button(new Rect(x + 12, 228, 226, 94), GUIContent.none, GUIStyle.none)) Abrir("Research");
            ui.Texto(jogo.DroneAtivo ? "AUTO REPAIR ENABLED" : "CLICK TO RESEARCH", x + 17, 310, IsoGui.Muted, 1);

            ui.Caixa(new Rect(x, 348, width, 184));
            ui.Texto("ALERTS", x + 17, 369, IsoGui.Branco, 3);
            int incidentes = E.Travamentos.Count + (E.DiscoQueimado ? 1 : 0) + (E.DeployQuebrado ? 1 : 0);
            Alerta(x, 404, E.Sobrecarga, "POWER " + Numero(E.ConsumoKw / E.CapacidadeKw * 100) + "%");
            Alerta(x, 432, E.Quente, E.Quente ? "COOLING OVERLOAD" : "COOLING STABLE");
            Alerta(x, 460, E.LinkSaturado, "NETWORK " + Numero(E.TrafegoMbps / E.BandaMbps * 100) + "%");
            if (ui.Botao(new Rect(x + 14, 488, 224, 28), incidentes > 0 ? "REPAIR " + incidentes + " INCIDENTS" : "ALL SERVICES HEALTHY", incidentes > 0 ? IsoGui.Laranja : IsoGui.Verde)) Reparar();

            ui.Caixa(new Rect(x, 546, width, 221));
            ui.Texto("PRODUCTION", x + 17, 567, IsoGui.Branco, 3);
            ui.Texto("INCOME / SECOND", x + 17, 600, IsoGui.Muted, 2);
            ui.Texto(Dinheiro(E.ReceitaPorSegundo), x + 17, 624, IsoGui.Verde, 4);
            ui.Texto("STORAGE BONUS", x + 17, 661, IsoGui.Muted, 2);
            ui.Texto("+" + Numero((E.FatorStorage - 1) * 100) + "%", x + 171, 661, IsoGui.Cyan, 2);
            ui.Texto("AUTOMATIONS", x + 17, 689, IsoGui.Muted, 2);
            ui.Texto(E.AutomacoesAtivas + " / 8", x + 171, 689, IsoGui.Roxo, 2);
            ui.Texto("OFFLINE " + Numero(E.TaxaOffline * 100) + "% / " + E.HorasMaximasOffline + "H", x + 17, 728, IsoGui.Muted, 2);
        }

        void Alerta(int x, int y, bool alerta, string texto)
        {
            ui.Ret(new Rect(x + 17, y + 1, 8, 8), alerta ? IsoGui.Laranja : IsoGui.Verde);
            ui.Texto(texto, x + 36, y, alerta ? IsoGui.Laranja : IsoGui.Muted, 2);
        }

        void Rodape()
        {
            ui.Ret(new Rect(177, 777, W - 177, 123), IsoGui.Cor("0c1729"));
            ui.Ret(new Rect(177, 777, W - 177, 2), IsoGui.Borda);
            string[] nomes = { "BUILD", "DEPLOY", "AUTOMATE", "UPGRADE", "RESEARCH" };
            Color[] cores = { IsoGui.Cyan, IsoGui.Cor("5899ff"), IsoGui.Verde, IsoGui.Laranja, IsoGui.Roxo };
            for (int i = 0; i < 5; i++)
            {
                if (ui.Botao(new Rect(194 + i * 194, 790, 180, 52), nomes[i], cores[i], true, 3))
                {
                    if (i == 0) Abrir("Racks");
                    if (i == 1) Deploy();
                    if (i == 2 || i == 4) Abrir("Research");
                    if (i == 3) Abrir("Upgrades");
                }
            }
            ui.Texto(jogo.MetasConcluidas ? "GOAL COMPLETE: COMPUTE EXPANSION" : "GOAL: BUILD " + jogo.AlvoRacks + " RACKS", 199, 862, IsoGui.Branco, 2);
            ui.Barra(new Rect(529, 857, 293, 18), (double)jogo.Racks / jogo.AlvoRacks, IsoGui.Verde);
            ui.Texto(jogo.MetasConcluidas ? "NEXT: COMPLETE RESEARCH" : "REWARD: " + (jogo.Estado.metaQuatroRacks ? "$100K" : "$50K"), 845, 862, IsoGui.Laranja, 2);
            ui.Caixa(new Rect(1174, 788, 252, 90));
            ui.Texto(E.Escrevendo ? "RESEARCH IN PROGRESS" : "RESEARCH LAB", 1190, 804, IsoGui.Roxo, 2);
            if (E.Escrevendo)
            {
                ui.Texto(NomeAutomacao(E.Estado.escrevendo), 1190, 828, IsoGui.Branco, 2);
                ui.Barra(new Rect(1190, 852, 218, 10), E.ProgressoEscrita, IsoGui.Roxo);
            }
            else ui.Texto(E.AutomacoesAtivas == 8 ? "ALL SYSTEMS AUTOMATED" : "READY FOR NEXT PROJECT", 1190, 838, IsoGui.Muted, 2);
        }
    }
}
