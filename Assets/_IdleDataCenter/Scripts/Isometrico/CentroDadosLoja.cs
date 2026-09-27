using System;
using System.Collections.Generic;
using UnityEngine;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter.Isometrico
{
    public partial class CentroDados
    {
        static string NomeMelhoria(string id)
        {
            switch (id)
            {
                case Catalogo.RackCheio: return "Compute rack";
                case Catalogo.Rack: return "42U rack";
                case Catalogo.Servidor1U: return "1U server";
                case Catalogo.Ssd: return "SSD upgrade";
                case Catalogo.Ventoinha: return "Tower cooling";
                case Catalogo.Servidor: return "Tower server";
                case Catalogo.NoBreak: return "UPS capacity";
                case Catalogo.ArCondicionado: return "Precision cooling";
                case Catalogo.Estagiario: return "Junior engineer";
                case Catalogo.Storage: return "Managed storage";
                case Catalogo.Backup: return "Tape backup";
                case Catalogo.Link: return "Fiber uplink";
                case Catalogo.Hypervisor: return "Virtualization";
                case Catalogo.Containers: return "Container host";
                case Catalogo.ServidorCi: return "CI/CD server";
                case Catalogo.Link10G: return "10G uplink";
                default: return id;
            }
        }

        static string NomeAutomacao(string id)
        {
            switch (id)
            {
                case Catalogo.Watchdog: return "Watchdog + drone";
                case Catalogo.HotSpare: return "Hot spare";
                case Catalogo.Monitoramento: return "Monitoring";
                case Catalogo.CronFaturamento: return "Billing cron";
                case Catalogo.Plantao: return "24H operations";
                case Catalogo.Pipeline: return "Pipeline tests";
                case Catalogo.RollbackAutomatico: return "Auto rollback";
                case Catalogo.InfraComoCodigo: return "Infrastructure as code";
                default: return id;
            }
        }

        static string[] Descricao(string id)
        {
            switch (id)
            {
                case Catalogo.RackCheio: return new[] { "8 SERVERS / +$72 BASE/S", "+1.6 KW / +160 MBPS" };
                case Catalogo.Servidor1U: return new[] { "+$9 BASE/S", "+0.4 KW / 1 RACK SLOT" };
                case Catalogo.Rack: return new[] { "5 SLOTS FOR 1U SERVERS", "UNLOCKS RACK SERVERS" };
                case Catalogo.Ssd: return new[] { "DOUBLE TOWER INCOME", "SOLID STATE STORAGE" };
                case Catalogo.Ventoinha: return new[] { "+50% TOWER INCOME", "PER UPGRADE LEVEL" };
                case Catalogo.Servidor: return new[] { "+1 TOWER SERVER", "+0.4 KW POWER USE" };
                case Catalogo.NoBreak: return new[] { "+1.5 KW POWER CAPACITY", "PREVENT POWER THROTTLING" };
                case Catalogo.ArCondicionado: return new[] { "REDUCE TEMPERATURE BY 6 C", "KEEP PRODUCTION EFFICIENT" };
                case Catalogo.Estagiario: return new[] { "+1 OPERATIONS ENGINEER", "REPAIR TIME: 30S TO 15S" };
                case Catalogo.Storage: return new[] { "+25% TOTAL INCOME / LEVEL", "MANAGED DATABASE SERVICES" };
                case Catalogo.Backup: return new[] { "PROTECT AGAINST DATA LOSS", "RESTORE FAILED DISKS" };
                case Catalogo.Link: return new[] { "+300 MBPS BANDWIDTH", "AVOID NETWORK BOTTLENECKS" };
                case Catalogo.Hypervisor: return new[] { "+40% SERVER INCOME", "PER VIRTUALIZATION LEVEL" };
                case Catalogo.Containers: return new[] { "+$250 BASE/S IN APPS", "+1 KW / +120 MBPS" };
                case Catalogo.ServidorCi: return new[] { "+50% APPLICATION INCOME", "UNLOCKS PIPELINE TESTING" };
                default: return new[] { "+1500 MBPS BANDWIDTH", "HIGH CAPACITY FIBER" };
            }
        }

        static string[] DescricaoAutomacao(string id)
        {
            switch (id)
            {
                case Catalogo.Watchdog: return new[] { "AUTO REPAIR IN 5 SECONDS", "ACTIVATES AUTOMATION DRONE" };
                case Catalogo.HotSpare: return new[] { "REPLACE FAILED DISKS IN 5S", "KEEP STORAGE ONLINE" };
                case Catalogo.Monitoramento: return new[] { "HALF AS MANY INCIDENTS", "PROACTIVE OBSERVABILITY" };
                case Catalogo.CronFaturamento: return new[] { "OFFLINE INCOME: +25%", "AUTOMATED BILLING JOBS" };
                case Catalogo.Plantao: return new[] { "OFFLINE INCOME: +25%", "OFFLINE LIMIT: 24 HOURS" };
                case Catalogo.Pipeline: return new[] { "4X FEWER FAILED DEPLOYS", "AUTOMATED TEST COVERAGE" };
                case Catalogo.RollbackAutomatico: return new[] { "ROLL BACK BAD DEPLOYS IN 5S", "RESTORE APPLICATION INCOME" };
                default: return new[] { "ALL UPGRADES COST 15% LESS", "REPEATABLE INFRASTRUCTURE" };
            }
        }

        string[] ItensLoja()
        {
            switch (janela)
            {
                case "Racks": return new[] { Catalogo.RackCheio, Catalogo.Servidor1U, Catalogo.Servidor };
                case "Compute": return new[] { Catalogo.Containers, Catalogo.Hypervisor, Catalogo.ServidorCi };
                case "Storage": return new[] { Catalogo.Storage, Catalogo.Backup };
                case "Network": return new[] { Catalogo.Link, Catalogo.Link10G };
                case "Power": return new[] { Catalogo.NoBreak };
                case "Cooling": return new[] { Catalogo.ArCondicionado, Catalogo.Ventoinha };
                case "Staff": return new[] { Catalogo.Estagiario };
                default:
                    var ids = new List<string>();
                    foreach (var item in Catalogo.Melhorias) if (item.Id != Catalogo.Rack) ids.Add(item.Id);
                    return ids.ToArray();
            }
        }

        void Loja()
        {
            ui.Ret(areaCentral, new Color(.015f, .04f, .08f, .82f));
            Rect modal = new Rect(210, 155, 928, 548);
            ui.Caixa(modal, IsoGui.Fundo, janela == "Research" ? IsoGui.Roxo : IsoGui.Cyan);
            ui.Texto(janela, modal.x + 24, modal.y + 23, IsoGui.Branco, 4);
            ui.Texto("DC-01 / " + (janela == "Research" ? "AUTOMATION LAB" : "INFRASTRUCTURE MANAGEMENT"), modal.x + 24, modal.y + 57, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(modal.xMax - 57, modal.y + 15, 40, 32), "X", IsoGui.Borda)) { janela = ""; return; }
            ui.Ret(new Rect(modal.x + 18, modal.y + 82, modal.width - 36, 2), IsoGui.Borda);

            if (janela == "Achievements") { Conquistas(modal); return; }
            bool pesquisa = janela == "Research";
            string[] itens = pesquisa ? null : ItensLoja();
            int quantidade = pesquisa ? Catalogo.Automacoes.Count : itens.Length;
            int paginas = (quantidade + 5) / 6;
            pagina = Mathf.Clamp(pagina, 0, paginas - 1);
            for (int k = 0; k < 6; k++)
            {
                int indice = pagina * 6 + k;
                if (indice >= quantidade) break;
                var r = new Rect(modal.x + 20 + k % 3 * 298, modal.y + 101 + k / 3 * 190, 284, 175);
                if (pesquisa) CardPesquisa(r, Catalogo.Automacoes[indice]);
                else CardMelhoria(r, itens[indice]);
            }
            if (paginas > 1)
            {
                if (ui.Botao(new Rect(modal.x + 20, modal.yMax - 48, 110, 30), "< PREV", IsoGui.Borda, pagina > 0)) pagina--;
                ui.Texto((pagina + 1) + " / " + paginas, modal.center.x, modal.yMax - 37, IsoGui.Muted, 2, true);
                if (ui.Botao(new Rect(modal.xMax - 130, modal.yMax - 48, 110, 30), "NEXT >", IsoGui.Cyan, pagina + 1 < paginas)) pagina++;
            }
            else ui.Texto("UPGRADES APPLY IMMEDIATELY / ESC TO CLOSE", modal.center.x, modal.yMax - 28, IsoGui.Muted, 2, true);
        }

        void CardMelhoria(Rect r, string id)
        {
            var def = Catalogo.Buscar(id);
            bool maximo = E.NoMaximo(id);
            ui.Caixa(r, IsoGui.Painel, maximo ? IsoGui.Verde : IsoGui.Borda);
            ui.Texto(NomeMelhoria(id), r.x + 12, r.y + 15, IsoGui.Branco, 2);
            ui.Texto(!E.RequisitoOk(id) ? "NEEDS " + NomeMelhoria(def.Requisito) : "LEVEL " + E.Nivel(id) + " / " + def.NivelMaximo,
                r.x + 12, r.y + 39, IsoGui.Cyan, 1);
            var desc = Descricao(id);
            ui.Texto(desc[0], r.x + 12, r.y + 62, IsoGui.Muted, 2);
            ui.Texto(desc[1], r.x + 12, r.y + 81, IsoGui.Muted, 2);
            if (!maximo) ui.Texto(Dinheiro(E.Custo(id)), r.x + 12, r.y + 109, E.PodeComprar(id) ? IsoGui.Verde : IsoGui.Laranja, 3);
            else ui.Texto("FULLY UPGRADED", r.x + 12, r.y + 111, IsoGui.Verde, 2);
            string texto = maximo ? "COMPLETE" : !E.RequisitoOk(id) ? "PREREQUISITE REQUIRED" : E.PodeComprar(id) ? "INSTALL" : "INSUFFICIENT FUNDS";
            if (ui.Botao(new Rect(r.x + 10, r.yMax - 37, r.width - 20, 28), texto, IsoGui.Cyan, E.PodeComprar(id))) Comprar(id);
        }

        void CardPesquisa(Rect r, AutomacaoDef def)
        {
            bool pronta = E.TemAutomacao(def.Id), escrevendo = E.Estado.escrevendo == def.Id;
            ui.Caixa(r, IsoGui.Painel, pronta ? IsoGui.Verde : escrevendo ? IsoGui.Roxo : IsoGui.Borda);
            ui.Texto(NomeAutomacao(def.Id), r.x + 12, r.y + 15, IsoGui.Branco, 2);
            ui.Texto(Math.Ceiling(def.Segundos / 60) + " MIN" + (!E.RequisitoAutomacaoOk(def) ? " / NEEDS " + NomeMelhoria(def.Requisito) : " RESEARCH"),
                r.x + 12, r.y + 39, IsoGui.Roxo, 1);
            var desc = DescricaoAutomacao(def.Id);
            ui.Texto(desc[0], r.x + 12, r.y + 62, IsoGui.Muted, 2);
            ui.Texto(desc[1], r.x + 12, r.y + 81, IsoGui.Muted, 2);
            if (escrevendo) ui.Barra(new Rect(r.x + 12, r.y + 111, r.width - 24, 14), E.ProgressoEscrita, IsoGui.Roxo);
            else ui.Texto(pronta ? "AUTOMATION ACTIVE" : Dinheiro(def.Custo), r.x + 12, r.y + 108, pronta ? IsoGui.Verde : IsoGui.Roxo, pronta ? 2 : 3);
            string texto = pronta ? "ACTIVE" : escrevendo ? "RESEARCHING " + Numero(E.ProgressoEscrita * 100) + "%" :
                E.Escrevendo ? "LAB BUSY" : !E.RequisitoAutomacaoOk(def) ? "PREREQUISITE REQUIRED" : E.Dinheiro < def.Custo ? "INSUFFICIENT FUNDS" : "START RESEARCH";
            if (ui.Botao(new Rect(r.x + 10, r.yMax - 37, r.width - 20, 28), texto, IsoGui.Roxo, E.PodeEscrever(def.Id)))
            {
                E.EscreverAutomacao(def.Id);
                Notificar("Research started: " + NomeAutomacao(def.Id));
                if (som) Sons.Compra();
                Salvar(false);
            }
        }

        void Conquistas(Rect r)
        {
            string[] titulos = { "COMPUTE EXPANSION", "FULL CAPACITY", "AUTONOMOUS OPERATIONS", "INCIDENT RESPONSE" };
            string[] detalhes = { "4 RACKS / REWARD $50K", "5 RACKS / REWARD $100K", "COMPLETE ALL 8 AUTOMATIONS", "RESOLVE 10 INCIDENTS" };
            double[] progressos = { jogo.Racks / 4.0, jogo.Racks / 5.0, E.AutomacoesAtivas / 8.0, E.Estado.incidentesResolvidos / 10.0 };
            for (int i = 0; i < 4; i++)
            {
                float y = r.y + 109 + i * 94;
                ui.Texto(titulos[i], r.x + 29, y, progressos[i] >= 1 ? IsoGui.Verde : IsoGui.Branco, 3);
                ui.Texto(detalhes[i], r.x + 29, y + 29, IsoGui.Muted, 2);
                ui.Barra(new Rect(r.x + 29, y + 49, r.width - 62, 14), progressos[i], IsoGui.Verde);
            }
            ui.Texto("RACK REWARDS ARE CREDITED AUTOMATICALLY", r.center.x, r.yMax - 32, IsoGui.Muted, 2, true);
        }
    }
}
