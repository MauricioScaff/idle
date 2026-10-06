using System;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    public partial class ModoGerente
    {
        /// <summary>Nome por extenso (a loja da faixa abrevia alguns para caber).</summary>
        static string NomeLongo(string id)
        {
            switch (id)
            {
                case Catalogo.Servidor: return "Servidor torre";
                case Catalogo.Rack: return "Rack 42U";
                case Catalogo.RackCheio: return "Rack cheio";
                case Catalogo.Storage: return "Storage RAID";
                case Catalogo.Backup: return "Backup em fita";
                case Catalogo.HdExterno: return "HD externo de backup";
                case Catalogo.Nas: return "NAS de backup";
                case Catalogo.Link: return "Link de fibra";
                case Catalogo.ServidorCi: return "Servidor de CI";
                case Catalogo.Containers: return "Host de containers";
                case Catalogo.NoKubernetes: return "Nó Kubernetes";
                case Catalogo.Balanceador: return "Balanceador";
                case Catalogo.Observabilidade: return "Observabilidade";
                case Catalogo.Datacenter: return "Novo datacenter";
                case Catalogo.Fibra: return "Fibra entre DCs";
                case Catalogo.Gerador: return "Gerador diesel";
                case Catalogo.Regiao: return "Nova região";
                case Catalogo.CaboSubmarino: return "Cabo submarino";
                case Catalogo.Renovavel: return "Energia renovável";
                case Catalogo.Gpu: return "Cluster de GPU";
                case Catalogo.ArCondicionado: return "Ar-condicionado";
                default: return Catalogo.Buscar(id).Nome;
            }
        }

        /// <summary>O que cada setor vende: primeiro o que dá para comprar, depois o que falta juntar, o bloqueado e o completo no fim.</summary>
        string[] ItensDoSetor(string setor) =>
            ItensDoSetorNaOrdem(setor).OrderBy(id => E.NoMaximo(id) ? 3 : Catalogo.Buscar(id).Cargo > E.Cargo ? 2 : E.PodeComprar(id) ? 0 : 1).ToArray();

        /// <summary>O que cada setor vende, em ordem de cargo.</summary>
        string[] ItensDoSetorNaOrdem(string setor)
        {
            switch (setor)
            {
                case "Compute": return new[] { Catalogo.SiteCliente, Catalogo.Hospedagem, Catalogo.Servidor, Catalogo.Ssd, Catalogo.Ventoinha, Catalogo.Rack, Catalogo.Servidor1U, Catalogo.RackCheio,
                                               Catalogo.Hypervisor, Catalogo.Containers, Catalogo.ServidorCi, Catalogo.NoKubernetes, Catalogo.Balanceador };
                case "Bancada": return new[] { Catalogo.KitFerramentas, Catalogo.CartaoDeVisita };
                case "Energia": return new[] { Catalogo.NoBreak };
                case "Refrigeracao": return new[] { Catalogo.ArCondicionado };
                case "Storage": return new[] { Catalogo.Storage, Catalogo.HdExterno, Catalogo.Nas, Catalogo.Backup, Catalogo.SalaBackup, Catalogo.DcRecuperacao, Catalogo.BackupRegiao };
                case "Seguranca": return Catalogo.LinhaDeSeguranca;
                case "Rede": return new[] { Catalogo.Link, Catalogo.Link10G };
                case "NOC": return new[] { Catalogo.Observabilidade };
                case "Equipe": return new[] { Catalogo.Funcionario, Catalogo.Estagiario, Catalogo.HelpDesk, Catalogo.ServiceDesk };
                case "Campus": return new[] { Catalogo.Datacenter, Catalogo.Fibra, Catalogo.Cdn, Catalogo.Gerador };
                case "Mundo": return new[] { Catalogo.Regiao, Catalogo.CaboSubmarino, Catalogo.Renovavel, Catalogo.Gpu };
                default:
                    return Catalogo.Melhorias.Select(m => m.Id).ToArray();   // "Melhorias" (aba TUDO): todos os setores
            }
        }

        static readonly string[] Subtitulos =
        {
            "Compute", "Sites, servidores, racks, virtualização, containers e Kubernetes",
            "Bancada", "Consertos: ferramentas e clientes do bairro",
            "Energia", "No-breaks: mais capacidade, menos sobrecarga",
            "Refrigeracao", "Ar-condicionado: sala fria rende mais e trava menos",
            "Storage", "Discos em RAID e backup",
            "Rede", "Banda: link saturado deixa tudo lento",
            "NOC", "Centro de operações: incidentes, picos e SLA",
            "Equipe", "Quem conserta as coisas quando você não está",
            "Automacao", "O técnico escreve um script por vez",
            "Melhorias", "Tudo o que dá para comprar, de todos os setores",
            "Carreira", "Metas para a próxima promoção",
            "Prestigio", "Certificações: bônus que passam de uma empresa para a outra",
            "Conquistas", "Cada uma dá +0,5% de renda, para sempre",
            "Vender", "A empresa é vendida e uma nova começa",
            "Campus", "Prédios novos, fibra entre eles, CDN e geradores",
            "Mundo", "Regiões, cabos submarinos, energia verde e nuvem de IA",
        };

        static string Subtitulo(string janela)
        {
            for (int i = 0; i < Subtitulos.Length; i += 2) if (Subtitulos[i] == janela) return Subtitulos[i + 1];
            return "";
        }

        /// <summary>As abas da LOJA: um setor por aba (as que o cargo ainda não tem ficam de fora) e "Tudo" no fim.</summary>
        static readonly string[] AbasDaLoja = { "Compute", "Bancada", "Energia", "Refrigeracao", "Storage", "Seguranca", "Rede", "NOC", "Equipe", "Campus", "Mundo", "Melhorias" };
        static readonly string[] AbasDaCarreira = { "Carreira", "Conquistas", "Prestigio" };
        string ultimaAbaLoja = "Compute";

        static string NomeDaAba(string id)
        {
            switch (id)
            {
                case "Refrigeracao": return "Refrig.";
                case "Storage": return "Dados";
                case "Seguranca": return "Segurança";
                case "Melhorias": return "Tudo";
                case "Prestigio": return "Prestígio";
                default: return id;
            }
        }

        void Loja()
        {
            ui.Ret(new Rect(0, 0, W, H), new Color(.02f, .04f, .08f, .55f));
            var modal = new Rect(240, 136, 960, 660);
            bool automacao = janela == "Automacao", loja = EmLoja;
            if (loja) ultimaAbaLoja = janela;
            ui.Caixa(modal, IsoGui.Cor("14243a"), automacao ? IsoGui.Verde : loja ? IsoGui.Cyan : IsoGui.Roxo);
            string titulo = janela == "Clientes" ? "Clientes" : janela == "Vender" ? "Vender a empresa" : automacao ? "Automação" : loja ? "Loja" : "Carreira";
            ui.Texto(titulo, modal.x + 28, modal.y + 22, IsoGui.Branco, 5);
            string saldo = Dinheiro(E.Dinheiro);
            ui.Texto(saldo, modal.xMax - 76 - ui.Largura(saldo, 4), modal.y + 26, Ouro, 4);
            if (ui.Botao(new Rect(modal.xMax - 58, modal.y + 18, 40, 40), "X", Vermelho)) { Abrir("Visao"); return; }

            // abas (a loja por setor, a carreira com o prestígio)
            var abas = loja ? AbasDaLoja.Where(a => a == "Melhorias" || SalaIso.CargoDoSetor(a) <= E.Cargo).ToArray()
                     : !automacao && janela != "Vender" && janela != "Clientes" ? AbasDaCarreira : new string[0];
            float ax = modal.x + 28;
            foreach (var aba in abas)
            {
                string nome = NomeDaAba(aba);
                float largura = ui.Largura(nome, 2) + 32;
                if (ui.Botao(new Rect(ax, modal.y + 76, largura, 38), nome, janela == aba ? IsoGui.Cyan : IsoGui.Borda, true)) Abrir(aba);
                ax += largura + 8;
            }
            if (abas.Length == 0) ui.Texto(Subtitulo(janela), modal.x + 28, modal.y + 80, IsoGui.Muted, 2);
            var conteudo = new Rect(modal.x, modal.y + 34, modal.width, modal.height - 34);   // o que antes começava logo abaixo do título

            if (janela == "Clientes") { TelaClientes(modal); return; }
            if (janela == "Carreira") { Carreira(conteudo); return; }
            if (janela == "Conquistas") { TelaConquistas(conteudo); return; }
            if (janela == "Prestigio") { TelaPrestigio(conteudo); return; }
            if (janela == "Vender") { TelaVender(conteudo); return; }
            if (janela == "NOC") NocAcoes(conteudo);

            int quantidade = automacao ? Catalogo.Automacoes.Count : ItensDoSetor(janela).Length;
            string[] itens = automacao ? null : ItensDoSetor(janela);
            var automacoes = automacao
                ? Catalogo.Automacoes.OrderBy(a => E.TemAutomacao(a.Id) ? 2 : E.Cargo < a.Cargo ? 1 : 0).ToArray() : null;
            int primeiraLinha = janela == "NOC" ? 1 : 0, porPagina = 6 - primeiraLinha * 3;
            int paginas = Mathf.Max(1, (quantidade + porPagina - 1) / porPagina);
            pagina = Mathf.Clamp(pagina, 0, paginas - 1);
            for (int k = 0; k < porPagina; k++)
            {
                int indice = pagina * porPagina + k;
                if (indice >= quantidade) break;
                int linha = k / 3 + primeiraLinha;
                var r = new Rect(modal.x + 28 + k % 3 * 306, modal.y + 130 + linha * 222, 294, 210);
                if (automacao) CardAutomacao(r, automacoes[indice]);
                else CardMelhoria(r, itens[indice]);
            }
            if (paginas > 1)
            {
                if (ui.Botao(new Rect(modal.x + 28, modal.yMax - 56, 150, 38), "< Anterior", IsoGui.Borda, pagina > 0)) pagina--;
                ui.Texto((pagina + 1) + " / " + paginas, modal.center.x, modal.yMax - 43, IsoGui.Muted, 2, true);
                if (ui.Botao(new Rect(modal.xMax - 178, modal.yMax - 56, 150, 38), "Próxima >", IsoGui.Cyan, pagina + 1 < paginas)) pagina++;
            }
            else ui.Texto(loja ? "As compras valem na hora, também na faixa  ·  Esc fecha" : "Esc fecha", modal.center.x, modal.yMax - 36, IsoGui.Muted, 2, true);
        }

        /// <summary>Texto em até duas linhas, quebrando nos espaços.</summary>
        /// <summary>Até duas linhas que cabem na largura (medida na letra de verdade; a segunda corta com reticências).</summary>
        void TextoQuebrado(string s, float x, float y, float largura, Color cor, int escala = 2)
        {
            if (ui.Largura(s, escala) <= largura) { ui.Texto(s, x, y, cor, escala); return; }
            int corte = s.Length;
            while (corte > 0) { corte = s.LastIndexOf(' ', corte - 1); if (corte <= 0 || ui.Largura(s.Substring(0, corte), escala) <= largura) break; }
            if (corte <= 0) corte = s.Length / 2;
            ui.Texto(s.Substring(0, corte), x, y, cor, escala);
            ui.Texto(CaberEm(s.Substring(corte).Trim(), largura, escala), x, y + escala * 5 + 6, cor, escala);
        }

        void CardMelhoria(Rect r, string id)
        {
            var def = Catalogo.Buscar(id);
            bool maximo = E.NoMaximo(id), cedo = def.Cargo > E.Cargo, req = E.RequisitoOk(id);
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, maximo ? IsoGui.Cor("2f6a4a") : E.PodeComprar(id) ? IsoGui.Cyan : IsoGui.Borda);
            string nome = NomeLongo(id);
            ui.Texto(nome, r.x + 16, r.y + 16, cedo ? IsoGui.Muted : IsoGui.Branco, ui.Largura(nome, 3) <= r.width - 32 ? 3 : 2);
            if (!cedo && req && def.Gerador)
            {
                int marco = E.ProximoMarco(id);
                ui.Texto(CaberEm(E.UnidadesDoGerador(id) + (marco > 0 ? "  ·  marco em " + marco + " (renda ×2)" : "  ·  todos os marcos"), r.width - 32, 2), r.x + 16, r.y + 44, IsoGui.Cyan, 2);
            }
            else if (!cedo && req && def.NivelMaximo > 1) ui.Texto(E.Nivel(id) + "/" + def.NivelMaximo, r.x + 16, r.y + 44, IsoGui.Cyan, 2);
            else ui.Texto(cedo ? "Libera no " + Catalogo.Cargos[def.Cargo].Nome : !req ? "Precisa: " + NomeLongo(def.Requisito) : "",
                r.x + 16, r.y + 44, IsoGui.Laranja, 2);
            TextoQuebrado(def.Efeito, r.x + 16, r.y + 70, r.width - 32, IsoGui.Muted);
            if (!maximo && !cedo) ui.Texto(Dinheiro(E.Custo(id)), r.x + 16, r.y + 118, E.PodeComprar(id) ? IsoGui.Verde : IsoGui.Laranja, 3);
            string texto = maximo ? "Completo" : cedo ? "Bloqueado" : !req ? "Falta requisito" : E.PodeComprar(id) ? "Comprar" : "Sem dinheiro";
            if (ui.Botao(new Rect(r.x + 14, r.yMax - 50, r.width - 28, 38), texto, IsoGui.Verde, PodeComprarAqui(id), 3)) Comprar(id);
        }

        void CardAutomacao(Rect r, AutomacaoDef a)
        {
            bool pronta = E.TemAutomacao(a.Id), escrevendo = E.Estado.escrevendo == a.Id, cedo = a.Cargo > E.Cargo;
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, pronta ? IsoGui.Verde : escrevendo ? IsoGui.Roxo : IsoGui.Borda);
            string nome = a.Nome;
            ui.Texto(nome, r.x + 16, r.y + 16, cedo ? IsoGui.Muted : IsoGui.Branco, ui.Largura(nome, 3) <= r.width - 32 ? 3 : 2);
            string linha2 = cedo ? "Libera no " + Catalogo.Cargos[a.Cargo].Nome
                          : Math.Ceiling(a.Segundos / 60) + " min" + (!E.RequisitoAutomacaoOk(a) ? " / precisa " + NomeLongo(a.Requisito) : " para escrever");
            ui.Texto(Cortar(linha2, 33), r.x + 16, r.y + 44, cedo ? IsoGui.Laranja : IsoGui.Roxo, 2);
            TextoQuebrado(a.Descricao, r.x + 16, r.y + 70, r.width - 32, IsoGui.Muted);
            if (escrevendo) ui.Barra(new Rect(r.x + 16, r.y + 120, r.width - 32, 18), E.ProgressoEscrita, IsoGui.Roxo);
            else if (!cedo) ui.Texto(pronta ? "Ativa" : Dinheiro(a.Custo), r.x + 16, r.y + 118, pronta ? IsoGui.Verde : IsoGui.Roxo, 3);
            string texto = pronta ? "Ativa" : escrevendo ? "Escrevendo " + Numero(Math.Floor(E.ProgressoEscrita * 100)) + "%"
                : cedo ? "Bloqueada" : E.Escrevendo ? "Técnico ocupado" : !E.RequisitoAutomacaoOk(a) ? "Falta requisito"
                : E.Dinheiro < a.Custo ? "Sem dinheiro" : "Escrever";
            if (ui.Botao(new Rect(r.x + 14, r.yMax - 50, r.width - 28, 38), texto, IsoGui.Verde, E.PodeEscrever(a.Id), 3)) Escrever(a);
        }

        /// <summary>Topo da tela do NOC: incidentes e pico de tráfego, com os botões de ação.</summary>
        void NocAcoes(Rect modal)
        {
            var r = new Rect(modal.x + 20, modal.y + 101, modal.width - 40, 175);
            ui.Caixa(r, IsoGui.Painel, Incidentes > 0 || (E.EmPico && !E.PicoFoiEscalado) ? IsoGui.Laranja : IsoGui.Verde);
            ui.Texto("Incidentes agora", r.x + 16, r.y + 16, IsoGui.Branco, 2);
            int y = (int)r.y + 42;
            foreach (var t in E.Travamentos.Take(3)) { ui.Texto("Servidor travado / volta em " + Numero(Math.Ceiling(E.TempoConserto - t.segundos)) + "s", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.DiscoQueimado) { ui.Texto("Disco queimado no storage", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.DeployQuebrado) { ui.Texto("Deploy quebrado / apps fora", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.TemQuedaDeEnergia) { ui.Texto("Queda de energia no DC-0" + (E.DatacenterSemEnergia + 1), r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.TemPaneRegional) { ui.Texto("Pane regional: " + Catalogo.NomesRegioes[E.RegiaoEmPane], r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (Incidentes == 0) ui.Texto("Nenhum. " + E.Estado.incidentesResolvidos + " resolvidos até hoje.", r.x + 16, y, IsoGui.Verde, 2);
            if (ui.Botao(new Rect(r.xMax - 250, r.y + 16, 234, 34), "Resolver tudo", IsoGui.Laranja, Incidentes > 0)) Resolver();

            string pico = !E.NoDataCenter ? "Picos de tráfego chegam no SRE"
                : E.EmPico ? "Pico: " + E.NomeDoPico + (E.PicoFoiEscalado ? " / escalado, rende 2×" : E.PicoViolado ? " / SLA violado" : "")
                : E.NosKubernetes == 0 ? "Sem cluster: sem picos" : "Próximo pico em " + Numero(Math.Ceiling(E.SegundosAteProximoPico / 60)) + " min";
            ui.Texto(pico, r.x + 16, r.yMax - 30, E.EmPico ? IsoGui.Laranja : IsoGui.Muted, 2);
            if (E.NoDataCenter) ui.Texto("Picos superados: " + E.Estado.picosSobrevividos + " de " + E.Estado.picosTotal, r.x + 16, r.yMax - 52, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(r.xMax - 250, r.yMax - 50, 234, 34), "Escalar cluster", IsoGui.Laranja, E.EmPico && !E.PicoFoiEscalado)) Escalar();
        }

        void Carreira(Rect modal)
        {
            var metas = E.CargoAtual.MetasParaPromocao;
            ui.Texto("Cargo atual: " + E.NomeDoCargo, modal.x + 29, modal.y + 109, IsoGui.Cyan, 3);
            if (E.IpoFeito)
            {
                ui.Texto("A empresa está na bolsa. Você chegou ao topo da carreira.", modal.x + 29, modal.y + 150, IsoGui.Cor("ffd65c"), 2);
                ui.Texto("Em prestígio, venda a empresa e recomece com bônus permanentes.", modal.x + 29, modal.y + 176, IsoGui.Muted, 2);
                return;
            }
            bool ipo = !E.TemProximoCargo;
            ui.Texto(ipo ? "Próximo: abrir o capital (IPO)" : "Próximo: " + Catalogo.Cargos[E.Cargo + 1].Nome, modal.x + 29, modal.y + 140, IsoGui.Laranja, 2);
            for (int i = 0; i < metas.Length; i++)
            {
                var m = metas[i];
                float y = modal.y + 180 + i * 80;
                bool ok = E.Cumprida(m);
                ui.Texto(m.Texto, modal.x + 29, y, ok ? IsoGui.Verde : IsoGui.Branco, 3);
                string prog = m.Tipo == TipoMeta.TotalGanho ? Dinheiro(Math.Min(E.Progresso(m), m.Alvo)) + " / " + Dinheiro(m.Alvo)
                    : Numero(Math.Min(E.Progresso(m), m.Alvo)) + " / " + Numero(m.Alvo);
                ui.Texto(prog, modal.xMax - 40 - ui.Largura(prog, 2), y + 4, IsoGui.Muted, 2);
                ui.Barra(new Rect(modal.x + 29, y + 26, modal.width - 62, 16), E.Progresso(m) / m.Alvo, ok ? IsoGui.Verde : IsoGui.Cyan);
            }
            // números da empresa (antes ficavam na coluna da direita)
            float ye = modal.yMax - 128;
            ui.Texto("Receita +" + Dinheiro(E.ReceitaPorSegundo) + "/s   ·   bônus do storage +" + Numero((E.FatorStorage - 1) * 100) + "%   /   "
                + E.Estado.incidentesResolvidos + " incidentes resolvidos", modal.x + 29, ye, IsoGui.Muted, 2);
            ui.Texto("Com o jogo fechado: " + Numero(E.TaxaOffline * 100) + "% da receita, até " + Numero(E.HorasMaximasOffline) + " h", modal.x + 29, ye + 22, IsoGui.Muted, 2);
            bool pode = ipo ? E.PodeFazerIpo : E.PodePromover;
            if (ui.Botao(new Rect(modal.center.x - 160, modal.yMax - 70, 320, 44), pode ? (ipo ? "Abrir o capital!" : "Ser promovido!") : "Cumpra as metas", IsoGui.Laranja, pode, 3))
            {
                if (ipo) faixa.FazerIpo(); else faixa.Promover();
            }
        }
    }
}
