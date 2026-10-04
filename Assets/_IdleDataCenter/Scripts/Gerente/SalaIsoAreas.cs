using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// As áreas atrás das portas (da sala de racks em diante). A porta da direita leva a Dados e backup (storage, a
    /// biblioteca de fitas, o cofre da sala de backup e as telas do DC de recuperação e da outra região); a da esquerda,
    /// a Rede e segurança (os racks dos links, os de segurança, que acendem conforme a linha do antivírus ao Zero Trust,
    /// e a mesa do SOC). Problema lá dentro (disco queimado, ataque, link saturado) faz a porta piscar na sala.
    /// </summary>
    public partial class SalaIso
    {
        static readonly Color32 LedLink = new Color32(70, 230, 230, 255), LedSeguranca = new Color32(255, 140, 60, 255);

        /// <summary>Ponto na parede da esquerda ('E', u ao longo de gy) ou da direita ('D', u ao longo de gx), z acima do piso.</summary>
        Vector2Int NaParede(char parede, float u, float z = 0) => parede == 'E' ? IP(0, u, z) : IP(u, 0, z);

        public bool ProblemaNosDados => E.DiscoQueimado && E.NivelStorage > 0;
        public bool ProblemaNaRede => (E.TemEvento && E.Evento.Ataque) || E.LinkSaturado;

        // ---------------- Portas ----------------

        /// <summary>Portas das áreas na sala de racks e no data center (as mesmas que o montador desenhou no fundo).</summary>
        void PortasDasAreas()
        {
            bool dc = E.Cargo >= 4;
            int altura = dc ? 100 : 98;
            Porta('E', dc ? 3.2f : 3.0f, dc ? 4.7f : 4.3f, altura, "porta:rede", "REDE", ProblemaNaRede ? "Rede e segurança: problema lá dentro!" : "Rede e segurança: entrar", ProblemaNaRede);
            Porta('D', dc ? 2.5f : 2.0f, dc ? 4.0f : 3.3f, altura, "porta:dados", "DADOS", ProblemaNosDados ? "Dados e backup: disco queimou lá dentro!" : "Dados e backup: entrar", ProblemaNosDados);
            // as compras de dados soltam as faíscas na porta
            pontoDoStorage = pontoDaFita = NaParede('D', dc ? 3.25f : 2.65f, altura / 2);
        }

        /// <summary>
        /// Uma porta clicável: o alvo tem o formato da porta (para o contorno), a placa em cima e, com problema do outro
        /// lado, um "!" piscando.
        /// </summary>
        void Porta(char parede, float u0, float u1, int altura, string tipo, string placa, string nome, bool problema)
        {
            var s = Sala;
            Vector2 passo = parede == 'E' ? (s.esquerda - s.fundo) / s.casas : (s.direita - s.fundo) / s.casas;
            Vector2Int a = NaParede(parede, u0), b = NaParede(parede, u1), c = NaParede(parede, u0, altura), e = NaParede(parede, u1, altura);
            int x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x), y0 = Mathf.Min(c.y, e.y), y1 = Mathf.Max(a.y, b.y);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x0 + x + 0.5f - s.fundo.x) / passo.x;
                    float z = s.fundo.y + u * passo.y - (y0 + y + 0.5f);
                    if (u >= u0 && u <= u1 && z >= 0 && z <= altura) px[y * w + x] = new Color32(255, 255, 255, 255);
                }
            Alvos.Add(new Alvo { Area = new RectInt(x0, y0, w, h), Tipo = tipo, Px = px, Prof = -30, Nome = nome });

            // placa em cima da porta
            var topo = NaParede(parede, (u0 + u1) / 2, altura + 12);
            int largura = PixelCanvas.LarguraTexto(placa) + 6;
            var corPlaca = problema && Piscar() ? IsoDesenho.C("ff3b4e") : IsoDesenho.C("41d7f5");
            fila.Add((-29, () =>
            {
                tela.Ret(topo.x - largura / 2 - 1, topo.y - 5, largura + 2, 11, IsoDesenho.C("0d1426"));
                tela.Ret(topo.x - largura / 2, topo.y - 4, largura, 9, IsoDesenho.C("1b2440"));
                tela.Texto(placa, topo.x - largura / 2 + 3, topo.y - 2, corPlaca, false);
                if (problema && Piscar()) tela.Texto("!", topo.x - 2, topo.y - 30, IsoDesenho.C("ff3b4e"), true, 3);
            }));
        }

        /// <summary>Tela pendurada na parede (status de algo que fica longe): fundo escuro, texto e um LED.</summary>
        void TelaNaParede(char parede, float u0, float u1, int z0, int z1, string texto, bool ok, string nome)
        {
            var s = Sala;
            Vector2 passo = parede == 'E' ? (s.esquerda - s.fundo) / s.casas : (s.direita - s.fundo) / s.casas;
            Vector2Int a = NaParede(parede, u0, z1), b = NaParede(parede, u1, z0);
            int x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            var cor = ok ? IsoDesenho.C("6ee07a") : IsoDesenho.C("ff3b4e");
            fila.Add((-20, () =>
            {
                for (int x = x0; x <= x1; x++)
                {
                    float u = (x + 0.5f - s.fundo.x) / passo.x;
                    int pe = Mathf.RoundToInt(s.fundo.y + u * passo.y);
                    tela.Ret(x, pe - z1, 1, z1 - z0, IsoDesenho.C(x == x0 || x == x1 ? "0b0d14" : "141b2e"));
                    tela.Pixel(x, pe - z1, IsoDesenho.C("0b0d14"));
                    tela.Pixel(x, pe - z0 - 1, IsoDesenho.C("0b0d14"));
                }
                var meio = NaParede(parede, (u0 + u1) / 2, (z0 + z1) / 2);
                tela.Texto(texto, meio.x - PixelCanvas.LarguraTexto(texto) / 2, meio.y - 3, cor, false);
                if (Piscar(0.6f)) tela.Ret(meio.x + PixelCanvas.LarguraTexto(texto) / 2 + 2, meio.y - 2, 2, 2, cor);
            }));
            Alvos.Add(new Alvo { Area = new RectInt(x0, Mathf.Min(a.y, b.y) - 2, x1 - x0 + 1, Mathf.Abs(b.y - a.y) + (z1 - z0) + 4), Tipo = "loja:Storage", Prof = -19, Nome = nome });
        }

        /// <summary>Fila de racks encostados numa parede, do fundo (u0) para a frente. Retorna o canto de cima de cada um.</summary>
        Vector2Int[] RacksNaParede(char parede, float u0, int quantos)
        {
            var rack = CarregarPixelLab("rack" + Sala.equip, parede == 'E');
            var lugares = new Vector2Int[quantos];
            if (parede == 'E')
            {
                // NaParedeEsquerda alinha o canto esquerdo da base (o da frente): o de trás fica LarguraNaEsquerda depois
                int passo = LarguraNaEsquerda(rack) + 2;
                float x = NaParede('E', u0).x - LarguraNaEsquerda(rack);
                for (int i = 0; i < quantos; i++) lugares[i] = NaParedeEsquerda(rack, x - i * passo);
            }
            else
            {
                int passo = LarguraNaDireita(rack) + 2;
                float x = NaParede('D', u0).x;
                for (int i = 0; i < quantos; i++) lugares[i] = NaParedeDireita(rack, x + i * passo);
            }
            return lugares;
        }

        /// <summary>Um rack de área: LEDs na cor do que ele faz, aceso até 'cheio', clicável.</summary>
        void RackDeArea(char parede, Vector2Int l, Color32 led, float cheio, string tipo, string nome, bool quebrado, int ordem)
        {
            var s = ComLedsEspelhado("rack" + Sala.equip, quebrado ? LedQuebrado : led, parede == 'E');
            float prof = -5 + ordem * 0.01f;
            fila.Add((prof, () => DesenharRack(s, l, cheio, !quebrado)));
            Alvos.Add(new Alvo { Area = new RectInt(l.x, l.y, s.w, s.h), Tipo = tipo, Px = s.px, Prof = prof, Nome = nome });
            var b = BaseDe(s);
            if (quebrado) fila.Add((5, () => { if (Piscar()) tela.Texto("!", l.x + b.frente.x - 3, l.y - 4, IsoDesenho.C("ff3b4e"), true, 3); }));
            Ronda(l + b.frente, parede == 'E');
        }

        static SpriteIso ComLedsEspelhado(string nome, Color32 cor, bool espelhar)
        {
            string chave = "pl/" + nome + (espelhar ? "|espelhado" : "") + "|leds" + cor.r + "," + cor.g + "," + cor.b;
            if (sprites.TryGetValue(chave, out var s)) return s;
            var o = CarregarPixelLab(nome, espelhar);
            var px = (Color32[])o.px.Clone();
            foreach (int i in o.leds) px[i] = cor;
            s = new SpriteIso { px = px, w = o.w, h = o.h, frente = o.frente };
            s.leds.AddRange(o.leds);
            return sprites[chave] = s;
        }

        /// <summary>Contorno no lugar da próxima compra da área e o item que o "+" compra.</summary>
        void MarcarNaArea(char parede, Vector2Int lugar, string item)
        {
            if (item == null) return;
            ContornoDaBase(CarregarPixelLab("rack" + Sala.equip, parede == 'E'), lugar);
            ItemDoMarcador = item;
        }

        /// <summary>O próximo da lista que já está liberado no cargo e ainda não foi comprado (ou null).</summary>
        string ProximoDe(params string[] ids)
        {
            foreach (var id in ids)
                if (Catalogo.Buscar(id).Cargo <= E.Cargo && E.Nivel(id) == 0) return id;
            return null;
        }

        // ---------------- Dados e backup ----------------

        void SalaDeDados()
        {
            // parede da esquerda, do fundo para a frente: os storages (o primeiro fica vermelho com o disco queimado)
            int storages = Mathf.Min(3, E.NivelStorage);
            var esq = RacksNaParede('E', 0.5f, 3);
            for (int i = 0; i < storages; i++)
            {
                bool queimado = i == 0 && E.DiscoQueimado;
                RackDeArea('E', esq[i], LedStorage, 1, "storage", queimado ? "Storage: disco queimou, clique para trocar" : "Storage (RAID)", queimado, i);
            }
            if (storages > 0) pontoDoStorage = esq[0] + new Vector2Int(20, 30);
            if (storages < 3 && Catalogo.Buscar(Catalogo.Storage).Cargo <= E.Cargo) MarcarNaArea('E', esq[storages], Catalogo.Storage);

            // parede da direita: a biblioteca de fitas e, com a sala de backup, o cofre (mais duas)
            int fitas = E.Nivel(Catalogo.Backup) > 0 ? (E.Nivel(Catalogo.SalaBackup) > 0 ? 3 : 1) : 0;
            var dir = RacksNaParede('D', 1.3f, 3);
            for (int i = 0; i < fitas; i++)
                RackDeArea('D', dir[i], LedFita, 1, "loja:Storage", i == 0 ? "Biblioteca de fitas" : "Cofre de fitas (sala de backup)", false, 10 + i);
            if (fitas > 0) pontoDaFita = dir[0] + new Vector2Int(20, 30);
            if (ItemDoMarcador == null)
            {
                string proximo = ProximoDe(Catalogo.Backup, Catalogo.SalaBackup);
                if (proximo != null) MarcarNaArea('D', dir[fitas], proximo);
            }

            // longe daqui: telas com o estado do DC de recuperação e do backup em outra região
            if (E.Nivel(Catalogo.DcRecuperacao) > 0) TelaNaParede('D', 4.0f, 5.0f, 52, 78, "DR OK", !E.TemQuedaDeEnergia, "DC de recuperação: pronto para o failover");
            if (E.Nivel(Catalogo.BackupRegiao) > 0) TelaNaParede('D', 4.0f, 5.0f, 18, 44, "REGIAO OK", !E.TemPaneRegional, "Backup em outra região");
            if (ItemDoMarcador == null) ItemDoMarcador = ProximoDe(Catalogo.DcRecuperacao, Catalogo.BackupRegiao);
            if (ItemDoMarcador != null && Marcador == null) Marcador = NaParede('D', 4.5f, 0) + new Vector2Int(-20, 10);

            Porta('E', 5.2f, 6.5f, 98, "porta:sala", "SALA", "Voltar para a sala", false);
            pontoDoChamado = NaParede('E', 2.0f, 120);
        }

        // ---------------- Rede e segurança ----------------

        void SalaDeRede()
        {
            // parede da esquerda, do fundo: os links (fibra, 10G, a fibra do campus e o cabo submarino). Laranja com o link
            // saturado; a mesa do SOC fica na frente, na ilustração
            int links = 0;
            foreach (var id in new[] { Catalogo.Link, Catalogo.Link10G, Catalogo.Fibra, Catalogo.CaboSubmarino }) if (E.Nivel(id) > 0) links++;
            links = Mathf.Min(links, 3);
            var esq = RacksNaParede('E', 0.5f, 3);
            for (int i = 0; i < links; i++)
                RackDeArea('E', esq[i], E.LinkSaturado ? LedPico : LedLink, 1, "loja:Rede", E.LinkSaturado ? "Rede: link saturado!" : "Rack de rede (links)", false, i);
            string proximoLink = links < 3 ? ProximoDe(Catalogo.Link, Catalogo.Link10G, Catalogo.Fibra, Catalogo.CaboSubmarino) : null;

            // parede da direita: segurança, um rack a cada três itens da linha; os LEDs acendem conforme ela cresce.
            // Num ataque, o rack da frente pisca em vermelho
            int nivel = E.NivelSeguranca, racks = Mathf.Min(3, (nivel + 2) / 3);
            bool ataque = E.TemEvento && E.Evento.Ataque;
            var dir = RacksNaParede('D', 1.3f, 3);
            for (int i = 0; i < racks; i++)
            {
                float cheio = Mathf.Clamp01((nivel - i * 3) / 3f);
                RackDeArea('D', dir[i], LedSeguranca, cheio, "loja:Seguranca", ataque ? "Segurança: ataque em andamento!" : "Rack de segurança: " + nivel + " de " + Catalogo.LinhaDeSeguranca.Length, ataque && i == racks - 1, 10 + i);
            }
            string proximaSeguranca = ProximoDe(Catalogo.LinhaDeSeguranca);
            if (proximaSeguranca != null) MarcarNaArea('D', dir[Mathf.Min(racks, 2)], proximaSeguranca);
            else if (proximoLink != null) MarcarNaArea('E', esq[links], proximoLink);

            bool soc = E.Nivel(Catalogo.Soc) > 0;
            Alvos.Add(new Alvo { Area = new RectInt(Mathf.RoundToInt(NaParede('E', 6.6f).x), NaParede('E', 4.0f, 90).y, 120, 120), Tipo = "loja:Seguranca", Prof = -20, Nome = soc ? "SOC 24h" : "Mesa de monitoramento" });
            Porta('D', 5.2f, 6.5f, 98, "porta:sala", "SALA", "Voltar para a sala", false);
            pontoDoChamado = NaParede('D', 2.0f, 120);
        }
    }
}
