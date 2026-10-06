using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Clientes na tela do gerente: a lista à esquerda (embaixo do help desk), a proposta de cliente novo no cartão da
    /// direita e a janela Clientes com o site de cada um (que muda com o porte e mostra 502 quando cai), a satisfação, o
    /// crescimento e o pedido em andamento, com o botão de comprar o que ele pediu.
    /// </summary>
    public partial class ModoGerente
    {
        void IniciarClientes()
        {
            E.ClienteProposto += c => Notificar("Novo cliente: " + c.nome + " quer hospedar o site aqui (exige " + Economia.FormatarUptime(c.sla) + " de uptime).", 6);
            E.ClienteCresceu += c =>
            {
                novidadeTitulo = "Cresceu!";
                novidadeNome = c.nome + " virou " + Catalogo.PortesDoSite[c.porte];
                novidadeDesde = Time.unscaledTime;
                Sons.Promocao();
                if (Aberto) Estourar(new Vector2(185, RetClientes.y + 20), 30, 1.1f);
            };
            E.ClienteSaiu += c => { Notificar(c.nome + " foi embora: o site caiu demais.", 8); Sons.Alerta(); };
            E.PedidoFeito += c => Notificar(c.nome + " pede: " + E.TextoDoPedido(c) + ".", 7);
            E.PedidoTerminou += (c, ok, premio) =>
            {
                if (ok) { Notificar("Pedido de " + c.nome + " atendido: +" + Dinheiro(premio) + ". Ele cresce mais rápido.", 6); Sons.Promocao(); }
                else Notificar(c.nome + " ficou bravo: o pedido venceu sem resposta.", 6);
            };
        }

        Rect RetClientes
        {
            get
            {
                int n = E.Clientes.Count;
                if (n == 0) return Rect.zero;
                var hd = RetHelpDesk;
                float y = hd.height > 0 ? hd.yMax + 10 : TopoDoHelpDesk;
                return new Rect(20, y, 330, 38 + n * LinhaDoChamado + 6);
            }
        }

        static string Porcentagem(double sla) => Economia.FormatarUptime(sla);

        /// <summary>A lista compacta: no ar ou fora, nome, porte e satisfação; com pedido, um "!" laranja. Clicar abre a janela.</summary>
        void PainelClientes()
        {
            var clientes = E.Clientes;
            if (clientes.Count == 0) return;
            var r = RetClientes;
            bool algumFora = clientes.Exists(c => c.foraDoAr > 0);
            ui.Caixa(r, IsoGui.Painel, algumFora && Pisca ? Vermelho : IsoGui.Borda);
            ui.Texto("Clientes " + clientes.Count + "/" + E.CapacidadeDeClientes, r.x + 14, r.y + 12, IsoGui.Muted, 2);
            string bonus = "+" + Numero(Mathf.Round((float)((E.FatorClientes - 1) * 100))) + "%";
            ui.Texto(bonus, r.xMax - 14 - ui.Largura(bonus, 2), r.y + 12, IsoGui.Verde, 2);
            for (int i = 0; i < clientes.Count; i++)
            {
                var c = clientes[i];
                var linha = new Rect(r.x + 8, r.y + 34 + i * LinhaDoChamado, r.width - 16, LinhaDoChamado - 4);
                bool fora = c.foraDoAr > 0, pedido = !string.IsNullOrEmpty(c.pedido);
                ui.Ret(linha, IsoGui.Cor("141d33"));
                ui.Ret(new Rect(linha.x, linha.y, 4, linha.height), fora ? (Pisca ? Vermelho : IsoGui.Laranja) : IsoGui.Verde);
                float x = linha.x + 12;
                if (pedido) { ui.Texto("!", x, linha.y + 8, Pisca ? Ouro : IsoGui.Laranja, 2); x += 14; }
                const float larguraBarra = 54;
                string porte = fora ? "502" : Catalogo.PortesDoSite[c.porte];
                float larguraPorte = ui.Largura(porte, 2);
                ui.Texto(CaberEm(c.nome, linha.xMax - x - larguraBarra - larguraPorte - 26, 2), x, linha.y + 8, IsoGui.Cor("c8d0de"), 2);
                ui.Texto(porte, linha.xMax - larguraBarra - 14 - larguraPorte, linha.y + 8, fora ? Vermelho : Ouro, 2);
                ui.Barra(new Rect(linha.xMax - larguraBarra - 6, linha.y + 8, larguraBarra, 10), c.satisfacao / 100, CorDaSatisfacao(c.satisfacao));
            }
            if (Livre && GUI.Button(r, GUIContent.none, GUIStyle.none)) Abrir("Clientes");
        }

        static Color CorDaSatisfacao(double s) => s >= Catalogo.SatisfacaoParaCrescer ? IsoGui.Verde : s >= 40 ? IsoGui.Laranja : Vermelho;

        /// <summary>A proposta no cartão da direita: quem é, o SLA que exige; aceitar ou recusar.</summary>
        void CartaoProposta(Rect r)
        {
            var p = E.PropostaDeCliente;
            var tipo = Catalogo.TipoDoCliente(p.nome);
            ui.Caixa(r, IsoGui.Cor("2a2410"), Ouro);
            ui.Texto("Novo cliente", r.x + 16, r.y + 14, IsoGui.Muted, 2);
            ui.Texto(CaberEm(p.nome, r.width - 32, 3), r.x + 16, r.y + 34, Ouro, 3);
            ui.Texto(CaberEm(tipo.Dominio, r.width - 32, 2), r.x + 16, r.y + 60, IsoGui.Muted, 2);
            ui.Texto("Exige uptime de " + Porcentagem(p.sla), r.x + 16, r.y + 82, IsoGui.Branco, 2);
            ui.Barra(new Rect(r.x + 16, r.y + 106, r.width - 32, 6), (float)(p.restante / Catalogo.TempoParaAceitar), Ouro);
            float meia = (r.width - 48) / 2;
            if (E.PodeAceitarCliente)
            {
                if (ui.Botao(new Rect(r.x + 16, r.y + 128, meia, 44), "Aceitar", IsoGui.Verde, Livre, 3))
                {
                    E.AceitarCliente();
                    Sons.Compra();
                    Notificar(p.nome + " agora hospeda aqui. Site no ar e uptime acima de " + Porcentagem(p.sla) + " deixam ele feliz.");
                }
            }
            else ui.Texto("Sem vaga (" + E.Clientes.Count + "/" + E.CapacidadeDeClientes + ")", r.x + 16, r.y + 144, IsoGui.Muted, 2);
            if (ui.Botao(new Rect(r.x + 32 + meia, r.y + 128, meia, 44), "Recusar", IsoGui.Borda, Livre, 3)) E.RecusarCliente();
        }

        // ---------------- Janela Clientes ----------------

        void TelaClientes(Rect modal)
        {
            var clientes = E.Clientes;
            ui.Texto(clientes.Count + " de " + E.CapacidadeDeClientes + " clientes", modal.x + 28, modal.y + 82, IsoGui.Branco, 3);
            string bonus = "+" + Numero(Mathf.Round((float)((E.FatorClientes - 1) * 100))) + "% de renda";
            ui.Texto(bonus, modal.x + 48 + ui.Largura(clientes.Count + " de " + E.CapacidadeDeClientes + " clientes", 3), modal.y + 82, IsoGui.Verde, 3);
            ui.Texto("Site no ar e uptime dentro do SLA: o cliente fica feliz e cresce. Site fora: ele se irrita e pode ir embora.",
                modal.x + 28, modal.y + 110, IsoGui.Muted, 2);

            const int colunas = 3, porPagina = 6;
            float largura = (modal.width - 56 - (colunas - 1) * 12) / colunas, altura = 250;
            int vagas = Mathf.Max(clientes.Count, Mathf.Min(E.CapacidadeDeClientes, porPagina));
            int paginas = Mathf.Max(1, (vagas + porPagina - 1) / porPagina);
            pagina = Mathf.Clamp(pagina, 0, paginas - 1);
            for (int k = 0; k < porPagina; k++)
            {
                int i = pagina * porPagina + k;
                if (i >= vagas) break;
                var r = new Rect(modal.x + 28 + k % colunas * (largura + 12), modal.y + 136 + k / colunas * (altura + 12), largura, altura);
                if (i < clientes.Count) CartaoDoCliente(r, clientes[i]);
                else
                {
                    ui.Caixa(r, IsoGui.Cor("111b2c"), IsoGui.Cor("23344c"));
                    ui.Texto("Vaga livre", r.center.x, r.center.y - 20, IsoGui.Muted, 3, true);
                    ui.Texto("Clientes novos chegam", r.center.x, r.center.y + 10, IsoGui.Muted, 2, true);
                    ui.Texto("quando o uptime está alto", r.center.x, r.center.y + 30, IsoGui.Muted, 2, true);
                }
            }
            if (paginas > 1)
            {
                if (ui.Botao(new Rect(modal.xMax - 290, modal.y + 74, 120, 32), "< Antes", IsoGui.Borda, pagina > 0)) pagina--;
                if (ui.Botao(new Rect(modal.xMax - 160, modal.y + 74, 120, 32), "Depois >", IsoGui.Borda, pagina < paginas - 1)) pagina++;
            }
        }

        void CartaoDoCliente(Rect r, Cliente c)
        {
            bool fora = c.foraDoAr > 0, pedido = !string.IsNullOrEmpty(c.pedido);
            ui.Caixa(r, IsoGui.Painel, fora ? (Pisca ? Vermelho : IsoGui.Laranja) : pedido ? IsoGui.Laranja : IsoGui.Borda);
            ui.Texto(CaberEm(c.nome, r.width - 24, 2), r.x + 12, r.y + 12, IsoGui.Branco, 2);
            DesenharSite(new Rect(r.x + 12, r.y + 30, r.width - 24, 96), c, fora);

            string porte = Catalogo.PortesDoSite[c.porte];
            ui.Texto(porte, r.x + 12, r.y + 134, Ouro, 2);
            string bonus = "+" + Numero(Mathf.Round((float)(E.BonusDoCliente(c) * 100))) + "%";
            ui.Texto(bonus, r.xMax - 12 - ui.Largura(bonus, 2), r.y + 134, IsoGui.Verde, 2);
            ui.Barra(new Rect(r.x + 12, r.y + 152, r.width - 24, 14), c.satisfacao / 100, CorDaSatisfacao(c.satisfacao));
            if (c.porte < Catalogo.PorteMaximo) ui.Barra(new Rect(r.x + 12, r.y + 170, r.width - 24, 8), c.crescimento, IsoGui.Cyan);

            float y = r.y + 184;
            if (pedido)
            {
                var linhas = QuebrarLinha(E.TextoDoPedido(c), r.width - 24, 2);
                for (int k = 0; k < linhas.Count && k < 2; k++) ui.Texto(linhas[k].Trim(), r.x + 12, y + k * 18, IsoGui.Laranja, 2);
                var b = new Rect(r.x + 12, r.yMax - 28, r.width - 24, 22);
                var item = string.IsNullOrEmpty(c.pedidoItem) ? null : Catalogo.Buscar(c.pedidoItem);
                string prazo = Relogio(c.pedidoRestante);
                if (item != null && !E.NoMaximo(item.Id))
                {
                    if (ui.Botao(b, CaberEm(item.Nome + " " + Dinheiro(E.Custo(item.Id)) + " · " + prazo, b.width - 12, 2), IsoGui.Verde, E.PodeComprar(item.Id), 2))
                        Comprar(item.Id);
                }
                else
                {
                    ui.Barra(new Rect(b.x, b.y + 6, b.width - 50, 10), E.ProgressoDoPedido(c), IsoGui.Laranja);
                    ui.Texto(prazo, b.xMax - 44, b.y + 6, IsoGui.Muted, 2);
                }
            }
            else if (fora) ui.Texto("Site fora do ar!", r.x + 12, y + 8, Vermelho, 2);
            else ui.Texto(CaberEm("Exige " + Porcentagem(c.sla) + " de uptime", r.width - 24, 2), r.x + 12, y + 8, IsoGui.Muted, 2);
        }

        static string Relogio(double segundos)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt((float)segundos));
            return s / 60 + ":" + (s % 60).ToString("00");
        }

        // ---------------- O site do cliente (desenhado, muda com o porte) ----------------

        /// <summary>Miniatura do site num navegador: em construção, loja, e-commerce, plataforma ou gigante; fora do ar, o 502.</summary>
        void DesenharSite(Rect r, Cliente c, bool fora)
        {
            var tipo = Catalogo.TipoDoCliente(c.nome);
            var marca = IsoGui.Cor(tipo.Cor);
            var clara = Color.Lerp(marca, Color.white, 0.55f);
            var escura = Color.Lerp(marca, Color.black, 0.45f);
            // navegador
            ui.Ret(r, IsoGui.Cor("2a2f3a"));
            ui.Ret(new Rect(r.x, r.y, r.width, 14), IsoGui.Cor("3a4150"));
            ui.Ret(new Rect(r.x + 5, r.y + 5, 4, 4), IsoGui.Cor("ff5f57"));
            ui.Ret(new Rect(r.x + 11, r.y + 5, 4, 4), IsoGui.Cor("febc2e"));
            ui.Ret(new Rect(r.x + 17, r.y + 5, 4, 4), IsoGui.Cor("28c840"));
            ui.Ret(new Rect(r.x + 28, r.y + 3, r.width - 34, 8), IsoGui.Cor("1e232c"));
            ui.Texto(tipo.Dominio, r.x + 32, r.y + 5, IsoGui.Muted, 1);
            var a = new Rect(r.x + 2, r.y + 16, r.width - 4, r.height - 18);

            if (fora)
            {
                ui.Ret(a, IsoGui.Cor("f4f4f0"));
                ui.Texto("502 Bad Gateway", a.center.x, a.center.y - 14, IsoGui.Cor("222222"), 2, true);
                ui.Ret(new Rect(a.x + 20, a.center.y + 6, a.width - 40, 1), IsoGui.Cor("cccccc"));
                ui.Texto("nginx", a.center.x, a.center.y + 12, IsoGui.Cor("777777"), 1, true);
                return;
            }
            var cinza = IsoGui.Cor("c9ccd2");
            switch (c.porte)
            {
                case 1:   // em construção
                    ui.Ret(a, IsoGui.Cor("f4f4f0"));
                    ui.Ret(new Rect(a.x + 8, a.y + 8, 22, 22), marca);
                    ui.Texto(tipo.Nome, a.x + 36, a.y + 12, IsoGui.Cor("333333"), 1);
                    ui.Texto("Site em construção", a.center.x, a.y + 44, IsoGui.Cor("555555"), 2, true);
                    for (int i = 0; i * 12 < a.width; i++)
                        ui.Ret(new Rect(a.x + i * 12, a.yMax - 10, 6, 10), i % 2 == 0 ? IsoGui.Cor("f5c518") : IsoGui.Cor("222222"));
                    break;
                case 2:   // loja virtual: cabeçalho, uma vitrine e texto
                    ui.Ret(a, IsoGui.Cor("f7f7f5"));
                    ui.Ret(new Rect(a.x, a.y, a.width, 14), marca);
                    ui.Ret(new Rect(a.x + 4, a.y + 3, 8, 8), Color.white);
                    ui.Ret(new Rect(a.x + 8, a.y + 22, a.width * 0.42f, a.height - 32), clara);
                    for (int i = 0; i < 4; i++) ui.Ret(new Rect(a.x + a.width * 0.5f + 4, a.y + 24 + i * 12, a.width * (i == 3 ? 0.25f : 0.4f), 5), cinza);
                    ui.Ret(new Rect(a.x + a.width * 0.5f + 4, a.yMax - 18, 40, 10), IsoGui.Cor("3ab86a"));
                    break;
                case 3:   // e-commerce: menu e três produtos
                    ui.Ret(a, IsoGui.Cor("f7f7f5"));
                    ui.Ret(new Rect(a.x, a.y, a.width, 14), marca);
                    ui.Ret(new Rect(a.x, a.y + 14, a.width, 7), escura);
                    for (int i = 0; i < 4; i++) ui.Ret(new Rect(a.x + 6 + i * 30, a.y + 16, 20, 3), clara);
                    float w3 = (a.width - 24) / 3;
                    for (int i = 0; i < 3; i++)
                    {
                        float px = a.x + 6 + i * (w3 + 6);
                        ui.Ret(new Rect(px, a.y + 26, w3, a.height - 54), clara);
                        ui.Ret(new Rect(px, a.yMax - 24, w3 * 0.8f, 4), cinza);
                        ui.Ret(new Rect(px, a.yMax - 16, w3 * 0.45f, 6), IsoGui.Cor("3ab86a"));
                    }
                    break;
                case 4:   // plataforma: banner de promoção e grade de produtos
                    ui.Ret(a, IsoGui.Cor("f7f7f5"));
                    ui.Ret(new Rect(a.x, a.y, a.width, 14), marca);
                    ui.Ret(new Rect(a.xMax - 14, a.y + 3, 9, 8), Color.white);   // carrinho
                    ui.Ret(new Rect(a.x + 4, a.y + 18, a.width - 8, 24), escura);
                    ui.Texto("-50%", a.x + 12, a.y + 26, Ouro, 2);
                    float w4 = (a.width - 8 - 3 * 4) / 4;
                    for (int j = 0; j < 2; j++)
                        for (int i = 0; i < 4; i++)
                        {
                            float px = a.x + 4 + i * (w4 + 4), py = a.y + 46 + j * 24;
                            ui.Ret(new Rect(px, py, w4, 16), clara);
                            ui.Ret(new Rect(px, py + 18, w4 * 0.6f, 3), cinza);
                        }
                    break;
                default:  // gigante: o app com painel lateral, números e gráfico se mexendo
                    ui.Ret(a, IsoGui.Cor("1b1f2a"));
                    ui.Ret(new Rect(a.x, a.y, 30, a.height), escura);
                    for (int i = 0; i < 5; i++) ui.Ret(new Rect(a.x + 6, a.y + 8 + i * 12, 18, 4), i == 0 ? clara : IsoGui.Cor("5a6070"));
                    float wk = (a.width - 46) / 3;
                    for (int i = 0; i < 3; i++)
                    {
                        ui.Ret(new Rect(a.x + 36 + i * (wk + 4), a.y + 6, wk, 18), IsoGui.Cor("262b38"));
                        ui.Ret(new Rect(a.x + 40 + i * (wk + 4), a.y + 12, wk * 0.5f, 6), i == 1 ? IsoGui.Verde : clara);
                    }
                    int barras = 12;
                    float wb = (a.width - 46) / barras;
                    for (int i = 0; i < barras; i++)
                    {
                        float h = (0.3f + 0.6f * Mathf.PerlinNoise(i * 0.5f, Time.unscaledTime * 0.4f)) * (a.height - 40);
                        ui.Ret(new Rect(a.x + 36 + i * wb, a.yMax - 6 - h, wb - 2, h), marca);
                    }
                    break;
            }
        }
    }
}
