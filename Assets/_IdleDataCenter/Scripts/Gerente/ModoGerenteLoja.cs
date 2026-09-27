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
                case "Compute": return new[] { Catalogo.Servidor, Catalogo.Ssd, Catalogo.Ventoinha, Catalogo.Rack, Catalogo.Servidor1U, Catalogo.RackCheio,
                                               Catalogo.Hypervisor, Catalogo.Containers, Catalogo.ServidorCi, Catalogo.NoKubernetes, Catalogo.Balanceador };
                case "Energia": return new[] { Catalogo.NoBreak };
                case "Refrigeracao": return new[] { Catalogo.ArCondicionado };
                case "Storage": return new[] { Catalogo.Storage, Catalogo.Backup };
                case "Rede": return new[] { Catalogo.Link, Catalogo.Link10G };
                case "NOC": return new[] { Catalogo.Observabilidade };
                case "Equipe": return new[] { Catalogo.Estagiario };
                case "Campus": return new[] { Catalogo.Datacenter, Catalogo.Fibra, Catalogo.Cdn, Catalogo.Gerador };
                case "Mundo": return new[] { Catalogo.Regiao, Catalogo.CaboSubmarino, Catalogo.Renovavel, Catalogo.Gpu };
                default:
                    return Catalogo.Melhorias.Select(m => m.Id).ToArray();   // "Melhorias" (aba TUDO): todos os setores
            }
        }

        static readonly string[] Subtitulos =
        {
            "Compute", "SERVIDORES, RACKS, VIRTUALIZACAO, CONTAINERS E KUBERNETES",
            "Energia", "NO-BREAKS: MAIS CAPACIDADE, MENOS SOBRECARGA",
            "Refrigeracao", "AR-CONDICIONADO: SALA FRIA RENDE MAIS E TRAVA MENOS",
            "Storage", "DISCOS EM RAID E BACKUP",
            "Rede", "BANDA: LINK SATURADO DEIXA TUDO LENTO",
            "NOC", "CENTRO DE OPERACOES: INCIDENTES, PICOS E SLA",
            "Equipe", "QUEM CONSERTA AS COISAS QUANDO VOCE NAO ESTA",
            "Automacao", "O TECNICO ESCREVE UM SCRIPT POR VEZ",
            "Melhorias", "TUDO O QUE DA PARA COMPRAR, DE TODOS OS SETORES",
            "Carreira", "METAS PARA A PROXIMA PROMOCAO",
            "Prestigio", "CERTIFICACOES: BONUS QUE PASSAM DE UMA EMPRESA PARA A OUTRA",
            "Vender", "A EMPRESA E VENDIDA E UMA NOVA COMECA",
            "Campus", "PREDIOS NOVOS, FIBRA ENTRE ELES, CDN E GERADORES",
            "Mundo", "REGIOES, CABOS SUBMARINOS, ENERGIA VERDE E NUVEM DE IA",
        };

        static string Subtitulo(string janela)
        {
            for (int i = 0; i < Subtitulos.Length; i += 2) if (Subtitulos[i] == janela) return Subtitulos[i + 1];
            return "";
        }

        /// <summary>As abas da LOJA: um setor por aba (as que o cargo ainda não tem ficam de fora) e "TUDO" no fim.</summary>
        static readonly string[] AbasDaLoja = { "Compute", "Energia", "Refrigeracao", "Storage", "Rede", "NOC", "Equipe", "Campus", "Mundo", "Melhorias" };
        static readonly string[] AbasDaCarreira = { "Carreira", "Prestigio" };
        string ultimaAbaLoja = "Compute";

        static string NomeDaAba(string id)
        {
            switch (id)
            {
                case "Refrigeracao": return "REFRIG.";
                case "Melhorias": return "TUDO";
                case "Prestigio": return "PRESTIGIO";
                default: return id.ToUpperInvariant();
            }
        }

        void Loja()
        {
            ui.Ret(new Rect(0, 0, W, H), new Color(.02f, .04f, .08f, .55f));
            var modal = new Rect(240, 136, 960, 660);
            bool automacao = janela == "Automacao", loja = EmLoja;
            if (loja) ultimaAbaLoja = janela;
            ui.Caixa(modal, IsoGui.Cor("14243a"), automacao ? IsoGui.Verde : loja ? IsoGui.Cyan : IsoGui.Roxo);
            string titulo = janela == "Vender" ? "VENDER A EMPRESA" : automacao ? "AUTOMACAO" : loja ? "LOJA" : "CARREIRA";
            ui.Texto(titulo, modal.x + 28, modal.y + 22, IsoGui.Branco, 5);
            string saldo = Dinheiro(E.Dinheiro);
            ui.Texto(saldo, modal.xMax - 76 - PixelCanvas.LarguraTexto(saldo) * 4, modal.y + 26, Ouro, 4);
            if (ui.Botao(new Rect(modal.xMax - 58, modal.y + 18, 40, 40), "X", Vermelho)) { Abrir("Visao"); return; }

            // abas (a loja por setor, a carreira com o prestígio)
            var abas = loja ? AbasDaLoja.Where(a => a == "Melhorias" || SalaIso.CargoDoSetor(a) <= E.Cargo).ToArray()
                     : !automacao && janela != "Vender" ? AbasDaCarreira : new string[0];
            float ax = modal.x + 28;
            foreach (var aba in abas)
            {
                string nome = NomeDaAba(aba);
                float largura = PixelCanvas.LarguraTexto(nome) * 2 + 32;
                if (ui.Botao(new Rect(ax, modal.y + 76, largura, 38), nome, janela == aba ? IsoGui.Cyan : IsoGui.Borda, true)) Abrir(aba);
                ax += largura + 8;
            }
            if (abas.Length == 0) ui.Texto(Subtitulo(janela), modal.x + 28, modal.y + 80, IsoGui.Muted, 2);
            var conteudo = new Rect(modal.x, modal.y + 34, modal.width, modal.height - 34);   // o que antes começava logo abaixo do título

            if (janela == "Carreira") { Carreira(conteudo); return; }
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
                if (ui.Botao(new Rect(modal.x + 28, modal.yMax - 56, 150, 38), "< ANTERIOR", IsoGui.Borda, pagina > 0)) pagina--;
                ui.Texto((pagina + 1) + " / " + paginas, modal.center.x, modal.yMax - 43, IsoGui.Muted, 2, true);
                if (ui.Botao(new Rect(modal.xMax - 178, modal.yMax - 56, 150, 38), "PROXIMA >", IsoGui.Cyan, pagina + 1 < paginas)) pagina++;
            }
            else ui.Texto(loja ? "AS COMPRAS VALEM NA HORA, TAMBEM NA FAIXA  /  ESC FECHA" : "ESC FECHA", modal.center.x, modal.yMax - 36, IsoGui.Muted, 2, true);
        }

        /// <summary>Texto em até duas linhas, quebrando nos espaços.</summary>
        void TextoQuebrado(string s, float x, float y, int maximo, Color cor, int escala = 2)
        {
            if (s.Length <= maximo) { ui.Texto(s, x, y, cor, escala); return; }
            int corte = s.LastIndexOf(' ', maximo);
            if (corte <= 0) corte = maximo;
            ui.Texto(s.Substring(0, corte), x, y, cor, escala);
            ui.Texto(Cortar(s.Substring(corte).Trim(), maximo), x, y + escala * 5 + 6, cor, escala);
        }

        void CardMelhoria(Rect r, string id)
        {
            var def = Catalogo.Buscar(id);
            bool maximo = E.NoMaximo(id), cedo = def.Cargo > E.Cargo, req = E.RequisitoOk(id);
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, maximo ? IsoGui.Cor("2f6a4a") : E.PodeComprar(id) ? IsoGui.Cyan : IsoGui.Borda);
            string nome = NomeLongo(id).ToUpperInvariant();
            ui.Texto(nome, r.x + 16, r.y + 16, cedo ? IsoGui.Muted : IsoGui.Branco, PixelCanvas.LarguraTexto(nome) * 3 <= r.width - 32 ? 3 : 2);
            if (!cedo && req && def.NivelMaximo > 1) ui.Texto(E.Nivel(id) + "/" + def.NivelMaximo, r.x + 16, r.y + 44, IsoGui.Cyan, 2);
            else ui.Texto(cedo ? "LIBERA NO " + Catalogo.Cargos[def.Cargo].Nome.ToUpperInvariant() : !req ? "PRECISA: " + NomeLongo(def.Requisito).ToUpperInvariant() : "",
                r.x + 16, r.y + 44, IsoGui.Laranja, 2);
            TextoQuebrado(def.Efeito.ToUpperInvariant(), r.x + 16, r.y + 70, 33, IsoGui.Muted);
            if (!maximo && !cedo) ui.Texto(Dinheiro(E.Custo(id)), r.x + 16, r.y + 118, E.PodeComprar(id) ? IsoGui.Verde : IsoGui.Laranja, 3);
            string texto = maximo ? "COMPLETO" : cedo ? "BLOQUEADO" : !req ? "FALTA REQUISITO" : E.PodeComprar(id) ? "COMPRAR" : "SEM DINHEIRO";
            if (ui.Botao(new Rect(r.x + 14, r.yMax - 50, r.width - 28, 38), texto, IsoGui.Verde, PodeComprarAqui(id), 3)) Comprar(id);
        }

        void CardAutomacao(Rect r, AutomacaoDef a)
        {
            bool pronta = E.TemAutomacao(a.Id), escrevendo = E.Estado.escrevendo == a.Id, cedo = a.Cargo > E.Cargo;
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, pronta ? IsoGui.Verde : escrevendo ? IsoGui.Roxo : IsoGui.Borda);
            string nome = a.Nome.ToUpperInvariant();
            ui.Texto(nome, r.x + 16, r.y + 16, cedo ? IsoGui.Muted : IsoGui.Branco, PixelCanvas.LarguraTexto(nome) * 3 <= r.width - 32 ? 3 : 2);
            string linha2 = cedo ? "LIBERA NO " + Catalogo.Cargos[a.Cargo].Nome.ToUpperInvariant()
                          : Math.Ceiling(a.Segundos / 60) + " MIN" + (!E.RequisitoAutomacaoOk(a) ? " / PRECISA " + NomeLongo(a.Requisito).ToUpperInvariant() : " PARA ESCREVER");
            ui.Texto(Cortar(linha2, 33), r.x + 16, r.y + 44, cedo ? IsoGui.Laranja : IsoGui.Roxo, 2);
            TextoQuebrado(a.Descricao.ToUpperInvariant(), r.x + 16, r.y + 70, 33, IsoGui.Muted);
            if (escrevendo) ui.Barra(new Rect(r.x + 16, r.y + 120, r.width - 32, 18), E.ProgressoEscrita, IsoGui.Roxo);
            else if (!cedo) ui.Texto(pronta ? "ATIVA" : Dinheiro(a.Custo), r.x + 16, r.y + 118, pronta ? IsoGui.Verde : IsoGui.Roxo, 3);
            string texto = pronta ? "ATIVA" : escrevendo ? "ESCREVENDO " + Numero(Math.Floor(E.ProgressoEscrita * 100)) + "%"
                : cedo ? "BLOQUEADA" : E.Escrevendo ? "TECNICO OCUPADO" : !E.RequisitoAutomacaoOk(a) ? "FALTA REQUISITO"
                : E.Dinheiro < a.Custo ? "SEM DINHEIRO" : "ESCREVER";
            if (ui.Botao(new Rect(r.x + 14, r.yMax - 50, r.width - 28, 38), texto, IsoGui.Verde, E.PodeEscrever(a.Id), 3)) Escrever(a);
        }

        /// <summary>Topo da tela do NOC: incidentes e pico de tráfego, com os botões de ação.</summary>
        void NocAcoes(Rect modal)
        {
            var r = new Rect(modal.x + 20, modal.y + 101, modal.width - 40, 175);
            ui.Caixa(r, IsoGui.Painel, Incidentes > 0 || (E.EmPico && !E.PicoFoiEscalado) ? IsoGui.Laranja : IsoGui.Verde);
            ui.Texto("INCIDENTES AGORA", r.x + 16, r.y + 16, IsoGui.Branco, 2);
            int y = (int)r.y + 42;
            foreach (var t in E.Travamentos.Take(3)) { ui.Texto("SERVIDOR TRAVADO / VOLTA EM " + Numero(Math.Ceiling(E.TempoConserto - t.segundos)) + "S", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.DiscoQueimado) { ui.Texto("DISCO QUEIMADO NO STORAGE", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.DeployQuebrado) { ui.Texto("DEPLOY QUEBRADO / APPS FORA", r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.TemQuedaDeEnergia) { ui.Texto("QUEDA DE ENERGIA NO DC-0" + (E.DatacenterSemEnergia + 1), r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (E.TemPaneRegional) { ui.Texto("PANE REGIONAL: " + Catalogo.NomesRegioes[E.RegiaoEmPane].ToUpperInvariant(), r.x + 16, y, IsoGui.Laranja, 2); y += 20; }
            if (Incidentes == 0) ui.Texto("NENHUM. " + E.Estado.incidentesResolvidos + " RESOLVIDOS ATE HOJE.", r.x + 16, y, IsoGui.Verde, 2);
            if (ui.Botao(new Rect(r.xMax - 250, r.y + 16, 234, 34), "RESOLVER TUDO", IsoGui.Laranja, Incidentes > 0)) Resolver();

            string pico = !E.NoDataCenter ? "PICOS DE TRAFEGO CHEGAM NO SRE"
                : E.EmPico ? "PICO: " + E.NomeDoPico.ToUpperInvariant() + (E.PicoFoiEscalado ? " / ESCALADO, RENDE 2X" : E.PicoViolado ? " / SLA VIOLADO" : "")
                : E.NosKubernetes == 0 ? "SEM CLUSTER: SEM PICOS" : "PROXIMO PICO EM " + Numero(Math.Ceiling(E.SegundosAteProximoPico / 60)) + " MIN";
            ui.Texto(pico, r.x + 16, r.yMax - 30, E.EmPico ? IsoGui.Laranja : IsoGui.Muted, 2);
            ui.Texto("PICOS SUPERADOS: " + E.Estado.picosSobrevividos + " DE " + E.Estado.picosTotal, r.x + 16, r.yMax - 12, IsoGui.Muted, 1);
            if (ui.Botao(new Rect(r.xMax - 250, r.yMax - 50, 234, 34), "ESCALAR CLUSTER", IsoGui.Laranja, E.EmPico && !E.PicoFoiEscalado)) Escalar();
        }

        void Carreira(Rect modal)
        {
            var metas = E.CargoAtual.MetasParaPromocao;
            ui.Texto("CARGO ATUAL: " + E.CargoAtual.Nome.ToUpperInvariant(), modal.x + 29, modal.y + 109, IsoGui.Cyan, 3);
            if (E.IpoFeito)
            {
                ui.Texto("A EMPRESA ESTA NA BOLSA. VOCE CHEGOU AO TOPO DA CARREIRA.", modal.x + 29, modal.y + 150, IsoGui.Cor("ffd65c"), 2);
                ui.Texto("EM PRESTIGIO, VENDA A EMPRESA E RECOMECE COM BONUS PERMANENTES.", modal.x + 29, modal.y + 176, IsoGui.Muted, 2);
                return;
            }
            bool ipo = !E.TemProximoCargo;
            ui.Texto(ipo ? "PROXIMO: ABRIR O CAPITAL (IPO)" : "PROXIMO: " + Catalogo.Cargos[E.Cargo + 1].Nome.ToUpperInvariant(), modal.x + 29, modal.y + 140, IsoGui.Laranja, 2);
            for (int i = 0; i < metas.Length; i++)
            {
                var m = metas[i];
                float y = modal.y + 180 + i * 80;
                bool ok = E.Cumprida(m);
                ui.Texto(m.Texto.ToUpperInvariant(), modal.x + 29, y, ok ? IsoGui.Verde : IsoGui.Branco, 3);
                string prog = m.Tipo == TipoMeta.TotalGanho ? Dinheiro(Math.Min(E.Progresso(m), m.Alvo)) + " / " + Dinheiro(m.Alvo)
                    : Numero(Math.Min(E.Progresso(m), m.Alvo)) + " / " + Numero(m.Alvo);
                ui.Texto(prog, modal.xMax - 40 - PixelCanvas.LarguraTexto(prog) * 2, y + 4, IsoGui.Muted, 2);
                ui.Barra(new Rect(modal.x + 29, y + 26, modal.width - 62, 16), E.Progresso(m) / m.Alvo, ok ? IsoGui.Verde : IsoGui.Cyan);
            }
            // números da empresa (antes ficavam na coluna da direita)
            float ye = modal.yMax - 128;
            ui.Texto("RECEITA +" + Dinheiro(E.ReceitaPorSegundo) + "/S   /   BONUS DO STORAGE +" + Numero((E.FatorStorage - 1) * 100) + "%   /   "
                + E.Estado.incidentesResolvidos + " INCIDENTES RESOLVIDOS", modal.x + 29, ye, IsoGui.Muted, 2);
            ui.Texto("COM O JOGO FECHADO: " + Numero(E.TaxaOffline * 100) + "% DA RECEITA, ATE " + Numero(E.HorasMaximasOffline) + " H", modal.x + 29, ye + 22, IsoGui.Muted, 2);
            bool pode = ipo ? E.PodeFazerIpo : E.PodePromover;
            if (ui.Botao(new Rect(modal.center.x - 160, modal.yMax - 70, 320, 44), pode ? (ipo ? "ABRIR O CAPITAL!" : "SER PROMOVIDO!") : "CUMPRA AS METAS", IsoGui.Laranja, pode, 3))
            {
                if (ipo) faixa.FazerIpo(); else faixa.Promover();
            }
        }
    }
}
