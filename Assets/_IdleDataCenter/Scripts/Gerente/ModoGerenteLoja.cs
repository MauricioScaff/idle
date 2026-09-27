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
                case Catalogo.ArCondicionado: return "Ar-condicionado";
                default: return Catalogo.Buscar(id).Nome;
            }
        }

        /// <summary>O que cada setor vende (em ordem de cargo).</summary>
        string[] ItensDoSetor(string setor)
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
                default:
                    // "Melhorias": tudo o que já dá para comprar primeiro, depois o que falta liberar, e o completo no fim
                    return Catalogo.Melhorias
                        .OrderBy(m => E.NoMaximo(m.Id) ? 3 : m.Cargo > E.Cargo ? 2 : E.PodeComprar(m.Id) ? 0 : 1)
                        .Select(m => m.Id).ToArray();
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
            "Campus", "PREDIOS NOVOS, FIBRA ENTRE ELES, CDN E GERADORES",
        };

        static string Subtitulo(string janela)
        {
            for (int i = 0; i < Subtitulos.Length; i += 2) if (Subtitulos[i] == janela) return Subtitulos[i + 1];
            return "";
        }

        void Loja()
        {
            ui.Ret(areaCentral, new Color(.015f, .04f, .08f, .82f));
            var modal = new Rect(210, 155, 928, 548);
            bool automacao = janela == "Automacao";
            ui.Caixa(modal, IsoGui.Fundo, automacao ? IsoGui.Roxo : IsoGui.Cyan);
            string titulo = Menus.First(m => m.id == janela).rotulo;
            ui.Texto(titulo, modal.x + 24, modal.y + 23, IsoGui.Branco, 4);
            ui.Texto("DC-01 / " + Subtitulo(janela), modal.x + 24, modal.y + 57, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(modal.xMax - 57, modal.y + 15, 40, 32), "X", IsoGui.Borda)) { Abrir("Visao"); return; }
            ui.Ret(new Rect(modal.x + 18, modal.y + 82, modal.width - 36, 2), IsoGui.Borda);

            int cargoDoSetor = SalaIso.CargoDoSetor(janela);
            if (cargoDoSetor > E.Cargo)
            {
                ui.Texto("SETOR AINDA NAO CONSTRUIDO", modal.center.x, modal.y + 220, IsoGui.Laranja, 4, true);
                ui.Texto("A SALA CRESCE ATE ELE NO CARGO " + Catalogo.Cargos[cargoDoSetor].Nome.ToUpperInvariant(), modal.center.x, modal.y + 270, IsoGui.Muted, 2, true);
                return;
            }
            if (janela == "Carreira") { Carreira(modal); return; }
            if (janela == "NOC") NocAcoes(modal);

            int quantidade = automacao ? Catalogo.Automacoes.Count : ItensDoSetor(janela).Length;
            string[] itens = automacao ? null : ItensDoSetor(janela);
            var automacoes = automacao
                ? Catalogo.Automacoes.OrderBy(a => E.TemAutomacao(a.Id) ? 2 : E.Cargo < a.Cargo ? 1 : 0).ToArray() : null;
            int porPagina = 6, primeiraLinha = janela == "NOC" ? 1 : 0;
            int paginas = Mathf.Max(1, (quantidade + porPagina - 1) / porPagina);
            pagina = Mathf.Clamp(pagina, 0, paginas - 1);
            for (int k = 0; k < porPagina; k++)
            {
                int indice = pagina * porPagina + k;
                if (indice >= quantidade) break;
                int linha = k / 3 + primeiraLinha;
                if (linha > 1) break;
                var r = new Rect(modal.x + 20 + k % 3 * 298, modal.y + 101 + linha * 190, 284, 175);
                if (automacao) CardAutomacao(r, automacoes[indice]);
                else CardMelhoria(r, itens[indice]);
            }
            if (paginas > 1)
            {
                if (ui.Botao(new Rect(modal.x + 20, modal.yMax - 48, 120, 30), "< ANTERIOR", IsoGui.Borda, pagina > 0)) pagina--;
                ui.Texto((pagina + 1) + " / " + paginas, modal.center.x, modal.yMax - 37, IsoGui.Muted, 2, true);
                if (ui.Botao(new Rect(modal.xMax - 140, modal.yMax - 48, 120, 30), "PROXIMA >", IsoGui.Cyan, pagina + 1 < paginas)) pagina++;
            }
            else ui.Texto("AS COMPRAS VALEM NA HORA, TAMBEM NA FAIXA  /  ESC FECHA", modal.center.x, modal.yMax - 28, IsoGui.Muted, 2, true);
        }

        void CardMelhoria(Rect r, string id)
        {
            var def = Catalogo.Buscar(id);
            bool maximo = E.NoMaximo(id), cedo = def.Cargo > E.Cargo, req = E.RequisitoOk(id);
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, maximo ? IsoGui.Verde : IsoGui.Borda);
            ui.Texto(NomeLongo(id).ToUpperInvariant(), r.x + 12, r.y + 15, cedo ? IsoGui.Muted : IsoGui.Branco, 2);
            ui.Texto(cedo ? "LIBERA NO CARGO " + Catalogo.Cargos[def.Cargo].Nome.ToUpperInvariant()
                     : !req ? "PRECISA: " + NomeLongo(def.Requisito).ToUpperInvariant()
                     : "NIVEL " + E.Nivel(id) + " / " + def.NivelMaximo, r.x + 12, r.y + 39, cedo ? IsoGui.Laranja : IsoGui.Cyan, 1);
            ui.Texto(def.Efeito.ToUpperInvariant(), r.x + 12, r.y + 62, IsoGui.Muted, 2);
            if (maximo) ui.Texto("COMPLETO", r.x + 12, r.y + 104, IsoGui.Verde, 2);
            else if (!cedo) ui.Texto(Dinheiro(E.Custo(id)), r.x + 12, r.y + 100, E.PodeComprar(id) ? IsoGui.Verde : IsoGui.Laranja, 3);
            string texto = maximo ? "COMPLETO" : cedo ? "BLOQUEADO" : !req ? "FALTA REQUISITO" : E.PodeComprar(id) ? "COMPRAR" : "SEM DINHEIRO";
            if (ui.Botao(new Rect(r.x + 10, r.yMax - 37, r.width - 20, 28), texto, IsoGui.Cyan, PodeComprarAqui(id))) Comprar(id);
        }

        void CardAutomacao(Rect r, AutomacaoDef a)
        {
            bool pronta = E.TemAutomacao(a.Id), escrevendo = E.Estado.escrevendo == a.Id, cedo = a.Cargo > E.Cargo;
            ui.Caixa(r, cedo ? IsoGui.Cor("121e30") : IsoGui.Painel, pronta ? IsoGui.Verde : escrevendo ? IsoGui.Roxo : IsoGui.Borda);
            ui.Texto(a.Nome.ToUpperInvariant(), r.x + 12, r.y + 15, cedo ? IsoGui.Muted : IsoGui.Branco, 2);
            string linha2 = cedo ? "LIBERA NO CARGO " + Catalogo.Cargos[a.Cargo].Nome.ToUpperInvariant()
                          : Math.Ceiling(a.Segundos / 60) + " MIN PARA ESCREVER" + (!E.RequisitoAutomacaoOk(a) ? " / PRECISA " + NomeLongo(a.Requisito).ToUpperInvariant() : "");
            ui.Texto(linha2, r.x + 12, r.y + 39, cedo ? IsoGui.Laranja : IsoGui.Roxo, 1);
            ui.Texto(a.Descricao.ToUpperInvariant(), r.x + 12, r.y + 62, IsoGui.Muted, 1);
            if (escrevendo) ui.Barra(new Rect(r.x + 12, r.y + 100, r.width - 24, 14), E.ProgressoEscrita, IsoGui.Roxo);
            else if (!cedo) ui.Texto(pronta ? "ATIVA" : Dinheiro(a.Custo), r.x + 12, r.y + 100, pronta ? IsoGui.Verde : IsoGui.Roxo, pronta ? 2 : 3);
            string texto = pronta ? "ATIVA" : escrevendo ? "ESCREVENDO " + Numero(Math.Floor(E.ProgressoEscrita * 100)) + "%"
                : cedo ? "BLOQUEADA" : E.Escrevendo ? "TECNICO OCUPADO" : !E.RequisitoAutomacaoOk(a) ? "FALTA REQUISITO"
                : E.Dinheiro < a.Custo ? "SEM DINHEIRO" : "ESCREVER";
            if (ui.Botao(new Rect(r.x + 10, r.yMax - 37, r.width - 20, 28), texto, IsoGui.Roxo, E.PodeEscrever(a.Id))) Escrever(a);
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
            if (!E.TemProximoCargo)
            {
                ui.Texto("CARGO MAXIMO POR ORA. OS PROXIMOS CHEGAM NAS NOVAS VERSOES.", modal.x + 29, modal.y + 150, IsoGui.Muted, 2);
                return;
            }
            ui.Texto("PROXIMO: " + Catalogo.Cargos[E.Cargo + 1].Nome.ToUpperInvariant(), modal.x + 29, modal.y + 140, IsoGui.Laranja, 2);
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
            if (ui.Botao(new Rect(modal.center.x - 160, modal.yMax - 70, 320, 44), E.PodePromover ? "SER PROMOVIDO!" : "CUMPRA AS METAS", IsoGui.Laranja, E.PodePromover, 3))
                faixa.Promover();
        }
    }
}
