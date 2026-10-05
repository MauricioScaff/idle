using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>A luta contra o chefe do cargo, em andamento (id vazio: nenhuma). Só dados, para o save.</summary>
    [Serializable]
    public class LutaChefe
    {
        public string id = "";
        public double carga;                 // quanto da "vida" do chefe já foi vencido (em R$ de renda)
        public double estabilidade = 100;    // a vida do jogador: zero é derrota
        public double decorrido;
        public double proximoAtaque;
        public int ataques;                  // quantos ataques já vieram (o próximo é Ataques[ataques % n])
        public int decisao = -1;             // decisão aberta agora (-1 = nenhuma)
        public int decisoesFeitas;           // quantas decisões já passaram
        public double decisaoRestante;       // segundos para escolher (depois vale a primeira opção)
        public double dps = 1, dpsRestante;  // bônus de dano das opções escolhidas
        public double escudo = 1, escudoRestante;
        public string ultimoAtaque = "";
        public double ultimoDano;
        public string resultado = "";        // o que aconteceu com a última escolha
    }

    /// <summary>O ponto que um ataque do chefe testa (e a compra que defende).</summary>
    public enum Defesa { Energia, Calor, Seguranca, Dados, Trafego, Hardware }

    public class AtaqueDef
    {
        public string Texto;
        public Defesa Tipo;
        public double Dano;   // estabilidade perdida sem nenhuma defesa
    }

    /// <summary>
    /// Uma opção de uma decisão. Efeitos: Curar (estabilidade), Dps (multiplica o dano por Segundos), Escudo (multiplica o
    /// dano recebido por Segundos). Custo em segundos de renda. Com Chance &lt; 1 pode dar errado: aí tira DanoSeFalhar.
    /// </summary>
    public class OpcaoDef
    {
        public string Texto, Efeito;
        public string Requer;                // ver Economia.RequisitoDaOpcao (null = nenhum)
        public double Curar, Dps = 1, Escudo = 1, Segundos;
        public double Custo;                 // segundos de renda
        public double Chance = 1, DanoSeFalhar;
        public string Sucesso, Falha;
    }

    public class DecisaoDef
    {
        public string Pergunta;
        public OpcaoDef[] Opcoes;            // a primeira é a segura: vale se o tempo acabar
    }

    public class ChefeDef
    {
        public string Id, Nome, Fala;
        public int Cargo;
        /// <summary>Renda por segundo esperada de quem acabou de cumprir as metas do cargo (medida no teste de ritmo).</summary>
        public double RendaAlvo;
        public AtaqueDef[] Ataques;
        public DecisaoDef[] Decisoes;        // abrem quando a vida do chefe passa de 2/3 e de 1/3
        public double Vida => RendaAlvo * Catalogo.SegundosDeVidaDoChefe;
    }

    public static partial class Catalogo
    {
        // --- Chefes: o fim de cada cargo (ver Economia.EnfrentarChefe) ---
        public const double SegundosDeVidaDoChefe = 75;   // com a renda alvo, a luta leva uns 75 s
        public const double TempoDaLuta = 150;            // passou disso, o chefe venceu
        public const double IntervaloDeAtaque = 8, PrimeiroAtaque = 5;
        public const double TempoParaDecidir = 15;
        public const double RecargaDoChefe = 120;         // depois de perder, espera para tentar de novo
        public const double RegeneracaoPorSegundo = 0.4;  // a equipe vai consertando
        public const double EstabilidadeFraca = 40;       // abaixo disso, a capacidade cai
        public const double FatorDpsFraco = 0.6;
        public const double DefesaMaxima = 0.85;
        public const double SegundosDoPremioDoChefe = 120;
        public const double BonusPorDerrota = 0.2;          // experiência: +20% de capacidade contra o mesmo chefe a cada derrota
        public static readonly double[] VidaDasDecisoes = { 2 / 3.0, 1 / 3.0 };

        public static readonly IReadOnlyList<ChefeDef> Chefes = new[]
        {
            new ChefeDef
            {
                Id = "fechamento", Cargo = 0, Nome = "O Fechamento do Mês", RendaAlvo = 27,
                Fala = "O financeiro abriu a planilha de 2 GB. Todo mundo ao mesmo tempo.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "A torre travou com a planilha", Tipo = Defesa.Hardware, Dano = 31 },
                    new AtaqueDef { Texto = "O armário virou uma sauna", Tipo = Defesa.Calor, Dano = 25 },
                    new AtaqueDef { Texto = "Ligaram a cafeteira na mesma tomada", Tipo = Defesa.Energia, Dano = 28 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "A planilha está travando tudo. E agora?",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Reiniciar o servidor", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Voltou. Por enquanto." },
                            new OpcaoDef { Texto = "Pedir para salvar em partes", Efeito = "metade do dano por 20 s", Escudo = 0.5, Segundos = 20, Sucesso = "O financeiro reclamou, mas salvou." },
                            new OpcaoDef { Texto = "Virar a noite no café", Efeito = "custa 30 s de renda: dano x1,5 por 20 s", Custo = 30, Dps = 1.5, Segundos = 20, Sucesso = "Cafeína na veia." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "Faltam poucos lançamentos.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Segurar firme", Efeito = "dano recebido x0,7 por 15 s", Escudo = 0.7, Segundos = 15, Sucesso = "Segurando." },
                            new OpcaoDef { Texto = "Chamar o estagiário", Requer = "estagiario", Efeito = "+30 estabilidade", Curar = 30, Sucesso = "O estagiário reiniciou a impressora. Ajudou." },
                            new OpcaoDef { Texto = "Desligar a impressora", Efeito = "60%: dano x2 por 15 s; senão -15", Chance = 0.6, Dps = 2, Segundos = 15, DanoSeFalhar = 15,
                                           Sucesso = "Sobrou energia para o servidor!", Falha = "A impressora era do diretor." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "ransomware", Cargo = 1, Nome = "Ransomware PagaOuChora", RendaAlvo = 350,
                Fala = "Os arquivos viraram .chora. Um bilhete na tela pede 3 bitcoins.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "Criptografou a pasta do RH", Tipo = Defesa.Seguranca, Dano = 35 },
                    new AtaqueDef { Texto = "Apagou as cópias de sombra", Tipo = Defesa.Dados, Dano = 31 },
                    new AtaqueDef { Texto = "Derrubou o servidor de arquivos", Tipo = Defesa.Hardware, Dano = 25 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "O malware está se espalhando pela rede.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Tirar o cabo de rede", Efeito = "metade do dano por 20 s", Escudo = 0.5, Segundos = 20, Sucesso = "Isolado. Ninguém tem internet, mas isolado." },
                            new OpcaoDef { Texto = "Restaurar do backup", Requer = "backup", Efeito = "+35 estabilidade", Curar = 35, Sucesso = "O backup salvou o dia." },
                            new OpcaoDef { Texto = "Pagar o resgate", Efeito = "custa 60 s de renda; 50%: +40", Custo = 60, Chance = 0.5, Curar = 40,
                                           Sucesso = "Mandaram a chave. Que sorte.", Falha = "Pegaram o dinheiro e sumiram." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "O ransomware achou o último compartilhamento.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Isolar a máquina infectada", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Era o PC do estagiário. Claro." },
                            new OpcaoDef { Texto = "Varredura completa", Requer = "seguranca2", Efeito = "dano x1,8 por 20 s", Dps = 1.8, Segundos = 20, Sucesso = "O antivírus achou tudo." },
                            new OpcaoDef { Texto = "Formatar tudo na coragem", Efeito = "50%: dano x2,5 por 20 s; senão -25", Chance = 0.5, Dps = 2.5, Segundos = 20, DanoSeFalhar = 25,
                                           Sucesso = "Limpo como novo!", Falha = "Formatou o disco errado." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "blackfriday", Cargo = 2, Nome = "A Black Friday", RendaAlvo = 8700,
                Fala = "Metade do Brasil quer a mesma TV de 50 polegadas. Agora.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "Fila de 2 milhões no carrinho", Tipo = Defesa.Trafego, Dano = 36 },
                    new AtaqueDef { Texto = "Os no-breaks estão no limite", Tipo = Defesa.Energia, Dano = 28 },
                    new AtaqueDef { Texto = "Os racks estão fervendo", Tipo = Defesa.Calor, Dano = 25 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "O site começou a ficar lento.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Página de \"aguarde na fila\"", Efeito = "metade do dano por 20 s", Escudo = 0.5, Segundos = 20, Sucesso = "A fila andou." },
                            new OpcaoDef { Texto = "Desligar as recomendações", Efeito = "dano x1,4 por 25 s", Dps = 1.4, Segundos = 25, Sucesso = "Ninguém sentiu falta." },
                            new OpcaoDef { Texto = "Comprar banda às pressas", Efeito = "custa 45 s de renda: dano x2 por 20 s", Custo = 45, Dps = 2, Segundos = 20, Sucesso = "Banda nova no ar." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "Bots estão comprando todo o estoque.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Captcha em tudo", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Os bots erraram os semáforos." },
                            new OpcaoDef { Texto = "Rodar o script de escala", Requer = "automacao", Efeito = "dano x2 por 20 s", Dps = 2, Segundos = 20, Sucesso = "O script subiu mais servidores." },
                            new OpcaoDef { Texto = "Cupom para quem esperar", Efeito = "custa 30 s de renda: +35", Custo = 30, Curar = 35, Sucesso = "A fila ficou feliz." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "deploysexta", Cargo = 3, Nome = "O Deploy de Sexta-feira", RendaAlvo = 98000,
                Fala = "18h de sexta. Alguém fez deploy direto na produção. Sem teste.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "Os containers entraram em loop", Tipo = Defesa.Hardware, Dano = 34 },
                    new AtaqueDef { Texto = "A migração apagou uma coluna", Tipo = Defesa.Dados, Dano = 34 },
                    new AtaqueDef { Texto = "Os clientes não param de dar F5", Tipo = Defesa.Trafego, Dano = 28 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "Tudo quebrou depois do deploy.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Fazer rollback", Efeito = "+20 estabilidade", Curar = 20, Sucesso = "Voltou para a versão de ontem." },
                            new OpcaoDef { Texto = "Corrigir em produção", Efeito = "50%: dano x2,2 por 20 s; senão -25", Chance = 0.5, Dps = 2.2, Segundos = 20, DanoSeFalhar = 25,
                                           Sucesso = "Hotfix de primeira!", Falha = "O hotfix quebrou outra coisa." },
                            new OpcaoDef { Texto = "Restaurar o banco do backup", Requer = "backup", Efeito = "+35 estabilidade", Curar = 35, Sucesso = "A coluna voltou." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "O dono do deploy foi para o happy hour.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Ligar para ele", Efeito = "dano recebido x0,6 por 20 s", Escudo = 0.6, Segundos = 20, Sucesso = "Ele atendeu. Do bar." },
                            new OpcaoDef { Texto = "Pipeline com testes", Requer = "automacao", Efeito = "dano x1,8 por 25 s", Dps = 1.8, Segundos = 25, Sucesso = "Os testes acharam o bug." },
                            new OpcaoDef { Texto = "Pizza para o time", Efeito = "custa 40 s de renda: dano x1,6 por 30 s", Custo = 40, Dps = 1.6, Segundos = 30, Sucesso = "Com pizza, ninguém reclama." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "apagao", Cargo = 4, Nome = "O Apagão", RendaAlvo = 277000,
                Fala = "A concessionária cortou a luz do bairro. O gerador está tossindo.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "O gerador engasgou", Tipo = Defesa.Energia, Dano = 36 },
                    new AtaqueDef { Texto = "O ar-condicionado desligou", Tipo = Defesa.Calor, Dano = 22 },
                    new AtaqueDef { Texto = "Todo o tráfego caiu no seu DC", Tipo = Defesa.Trafego, Dano = 31 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "Os no-breaks aguentam pouco tempo.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Desligar o ambiente de teste", Efeito = "metade do dano por 20 s", Escudo = 0.5, Segundos = 20, Sucesso = "Sobrou energia para a produção." },
                            new OpcaoDef { Texto = "Escalar o Kubernetes", Requer = "k8s", Efeito = "dano x1,8 por 25 s", Dps = 1.8, Segundos = 25, Sucesso = "O cluster redistribuiu tudo." },
                            new OpcaoDef { Texto = "Alugar um gerador", Efeito = "custa 60 s de renda: +40", Custo = 60, Curar = 40, Sucesso = "Chegou o caminhão do gerador." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "A temperatura está subindo rápido.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Abrir as portas", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Entrou um vento. E um pombo." },
                            new OpcaoDef { Texto = "Failover automático", Requer = "automacao", Efeito = "dano recebido x0,3 por 25 s", Escudo = 0.3, Segundos = 25, Sucesso = "O tráfego foi para outro lugar." },
                            new OpcaoDef { Texto = "Tudo no talo e torcer", Efeito = "50%: dano x2,5 por 15 s; senão -30", Chance = 0.5, Dps = 2.5, Segundos = 15, DanoSeFalhar = 30,
                                           Sucesso = "Aguentou!", Falha = "Desarmou o disjuntor geral." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "migracao", Cargo = 5, Nome = "A Migração para a Nuvem", RendaAlvo = 870000,
                Fala = "O conselho quer tudo na nuvem até segunda. São 400 TB.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "Um lote de 40 TB corrompeu", Tipo = Defesa.Dados, Dano = 36 },
                    new AtaqueDef { Texto = "O link de saída engasgou", Tipo = Defesa.Trafego, Dano = 31 },
                    new AtaqueDef { Texto = "Um bucket ficou público", Tipo = Defesa.Seguranca, Dano = 34 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "A cópia está lenta demais.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Copiar em lotes menores", Efeito = "dano recebido x0,6 por 25 s", Escudo = 0.6, Segundos = 25, Sucesso = "Devagar e sempre." },
                            new OpcaoDef { Texto = "Usar a fibra entre os DCs", Requer = "datacenter", Efeito = "dano x1,8 por 25 s", Dps = 1.8, Segundos = 25, Sucesso = "A fibra voou." },
                            new OpcaoDef { Texto = "Mandar os discos de caminhão", Efeito = "custa 60 s de renda: dano x2,5 por 15 s", Custo = 60, Dps = 2.5, Segundos = 15, Sucesso = "Nada vence um caminhão cheio de discos." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "Dados sensíveis apareceram na internet.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Fechar todos os buckets", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Trancado." },
                            new OpcaoDef { Texto = "Chamar o time de segurança", Requer = "seguranca4", Efeito = "+35 estabilidade", Curar = 35, Sucesso = "O SOC resolveu em minutos." },
                            new OpcaoDef { Texto = "Fingir que não viu", Efeito = "40%: dano x2 por 20 s; senão -35", Chance = 0.4, Dps = 2, Segundos = 20, DanoSeFalhar = 35,
                                           Sucesso = "Ninguém percebeu.", Falha = "Saiu no jornal." },
                        },
                    },
                },
            },
            new ChefeDef
            {
                Id = "auditoria", Cargo = 6, Nome = "A Auditoria do IPO", RendaAlvo = 45000000,
                Fala = "Os auditores querem ver tudo: logs, backups, contratos e senhas.",
                Ataques = new[]
                {
                    new AtaqueDef { Texto = "Acharam uma senha num post-it", Tipo = Defesa.Seguranca, Dano = 39 },
                    new AtaqueDef { Texto = "Pediram o backup de 2019", Tipo = Defesa.Dados, Dano = 36 },
                    new AtaqueDef { Texto = "Um servidor está sem garantia", Tipo = Defesa.Hardware, Dano = 34 },
                },
                Decisoes = new[]
                {
                    new DecisaoDef
                    {
                        Pergunta = "Os auditores acharam uma falha.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Explicar com calma", Efeito = "dano recebido x0,6 por 20 s", Escudo = 0.6, Segundos = 20, Sucesso = "Eles anotaram e seguiram." },
                            new OpcaoDef { Texto = "Mostrar os runbooks", Requer = "automacao", Efeito = "+30 estabilidade", Curar = 30, Sucesso = "Processo documentado. Os auditores sorriram." },
                            new OpcaoDef { Texto = "Contratar uma consultoria", Efeito = "custa 90 s de renda: dano x2 por 25 s", Custo = 90, Dps = 2, Segundos = 25, Sucesso = "Slides bonitos resolvem muita coisa." },
                        },
                    },
                    new DecisaoDef
                    {
                        Pergunta = "Última reunião antes do sino da bolsa.",
                        Opcoes = new[]
                        {
                            new OpcaoDef { Texto = "Apresentar o uptime", Efeito = "+15 estabilidade", Curar = 15, Sucesso = "Os noves impressionaram." },
                            new OpcaoDef { Texto = "Mostrar as regiões redundantes", Requer = "regiao", Efeito = "dano x2,2 por 20 s", Dps = 2.2, Segundos = 20, Sucesso = "Redundância em quatro continentes." },
                            new OpcaoDef { Texto = "Prometer 100% de uptime", Efeito = "50%: dano x3 por 15 s; senão -35", Chance = 0.5, Dps = 3, Segundos = 15, DanoSeFalhar = 35,
                                           Sucesso = "Eles acreditaram!", Falha = "Ninguém acredita em 100%." },
                        },
                    },
                },
            },
        };

        public static ChefeDef ChefeDoCargo(int cargo)
        {
            foreach (var c in Chefes) if (c.Cargo == cargo) return c;
            return null;
        }

        public static ChefeDef BuscarChefe(string id)
        {
            foreach (var c in Chefes) if (c.Id == id) return c;
            return null;
        }

        /// <summary>Nome curto do ponto de defesa e o que comprar para melhorá-lo.</summary>
        public static string NomeDaDefesa(Defesa d) => d switch
        {
            Defesa.Energia => "Energia", Defesa.Calor => "Refrigeração", Defesa.Seguranca => "Segurança",
            Defesa.Dados => "Backup", Defesa.Trafego => "Rede", _ => "Hardware",
        };

        public static string DicaDaDefesa(Defesa d) => d switch
        {
            Defesa.Energia => "compre no-break ou gerador para sobrar energia",
            Defesa.Calor => "compre ar-condicionado para baixar a temperatura",
            Defesa.Seguranca => "suba a linha de segurança",
            Defesa.Dados => "suba a linha de backup",
            Defesa.Trafego => "compre link para sobrar banda",
            _ => "estagiário e automações de conserto deixam o reparo mais rápido",
        };
    }

    /// <summary>
    /// Chefes: com as metas do cargo cumpridas, a promoção (ou o IPO) depende de vencer o chefe do cargo. A luta é
    /// automática: a renda por segundo derruba a vida do chefe; ele ataca a cada tanto e cada ataque testa uma defesa
    /// (energia, refrigeração, segurança, backup, rede ou hardware); a estabilidade zerada é derrota. Em 2/3 e 1/3 da vida
    /// abre uma decisão com três opções (sem resposta, vale a primeira). Perder não tira nada: só espera para tentar de novo.
    /// </summary>
    public partial class Economia
    {
        public event Action<ChefeDef> ChefeComecou;
        public event Action<ChefeDef, AtaqueDef, double> ChefeAtacou;
        public event Action<ChefeDef, DecisaoDef> DecisaoAberta;
        /// <summary>A luta acabou: (chefe, venceu?, prêmio).</summary>
        public event Action<ChefeDef, bool, double> ChefeTerminou;

        LutaChefe Luta => Estado.luta ?? (Estado.luta = new LutaChefe());
        public LutaChefe LutaAtual => EmLuta ? Luta : null;
        public bool EmLuta => !string.IsNullOrEmpty(Luta.id);
        public ChefeDef ChefeDaLuta => EmLuta ? Catalogo.BuscarChefe(Luta.id) : null;
        public ChefeDef ChefeDoCargo => Catalogo.ChefeDoCargo(Estado.cargo);
        public double RecargaDoChefe => Estado.recargaChefe;

        /// <summary>As metas estão cumpridas (promoção ou IPO), mas falta vencer o chefe.</summary>
        public bool ChefeLiberado => (PodePromover || PodeFazerIpo) && ChefeDoCargo != null;
        public bool PodeEnfrentarChefe => ChefeLiberado && !EmLuta && Estado.recargaChefe <= 0;

        /// <summary>Quanto um ponto de defesa segura de cada ataque (0 a DefesaMaxima).</summary>
        public double ValorDefesa(Defesa d)
        {
            double v;
            switch (d)
            {
                case Defesa.Energia:
                    v = Sobrecarga ? 0 : 0.2 + Math.Min(0.55, (CapacidadeKw / Math.Max(0.1, ConsumoKw) - 1) * 1.5) + (Nivel(Catalogo.Gerador) > 0 ? 0.1 : 0);
                    break;
                case Defesa.Calor:
                    v = Math.Max(0, Math.Min(1, (Catalogo.TemperaturaQuente - Temperatura) / (Catalogo.TemperaturaQuente - Catalogo.TemperaturaMinima))) * 0.8;
                    break;
                case Defesa.Seguranca: v = ProtecaoSeguranca * 0.85; break;
                case Defesa.Dados: v = ProtecaoBackup * 0.85; break;
                case Defesa.Trafego:
                    v = LinkSaturado ? 0.1 : 0.25 + Math.Min(0.5, (BandaMbps / Math.Max(1, TrafegoMbps) - 1) * 0.5) + (TemBalanceador ? 0.1 : 0);
                    break;
                default: v = 0.75 * Math.Sqrt(3 / Math.Max(3, TempoConserto)); break;
            }
            return Math.Max(0, Math.Min(Catalogo.DefesaMaxima, v));
        }

        /// <summary>A defesa mais fraca contra os ataques de um chefe (a dica depois de uma derrota).</summary>
        public Defesa PontoFraco(ChefeDef c)
        {
            var pior = c.Ataques[0].Tipo;
            foreach (var a in c.Ataques) if (ValorDefesa(a.Tipo) < ValorDefesa(pior)) pior = a.Tipo;
            return pior;
        }

        /// <summary>
        /// A melhoria que reforça um ponto de defesa agora (a mais barata que dá para comprar no cargo), ou null. É a dica
        /// da derrota: "ponto fraco: refrigeração; compre ar-condicionado".
        /// </summary>
        public MelhoriaDef MelhoriaParaDefesa(Defesa d)
        {
            MelhoriaDef melhor = null;
            foreach (var m in Catalogo.Melhorias)
            {
                if (m.Cargo > Estado.cargo || NoMaximo(m.Id) || !RequisitoOk(m.Id) || !ReforcaDefesa(m, d)) continue;
                if (melhor == null || Custo(m.Id) < Custo(melhor.Id)) melhor = m;
            }
            return melhor;
        }

        static bool ReforcaDefesa(MelhoriaDef m, Defesa d)
        {
            switch (d)
            {
                case Defesa.Energia: return m.Id == Catalogo.NoBreak || m.Id == Catalogo.Gerador || m.Alvo == Catalogo.AlvoKw;
                case Defesa.Calor: return m.Id == Catalogo.ArCondicionado || m.Alvo == Catalogo.AlvoGraus;
                case Defesa.Trafego: return m.Id == Catalogo.Link || m.Id == Catalogo.Link10G || m.Id == Catalogo.Cdn || m.Id == Catalogo.Balanceador;
                case Defesa.Seguranca: return Array.IndexOf(Catalogo.LinhaDeSeguranca, m.Id) >= 0;
                case Defesa.Dados: return Array.IndexOf(Catalogo.LinhaDeBackup, m.Id) >= 0;
                default: return m.Id == Catalogo.Estagiario;
            }
        }

        public bool RequisitoDaOpcao(OpcaoDef o)
        {
            switch (o.Requer)
            {
                case null: case "": return true;
                case "estagiario": return TemEstagiario;
                case "backup": return TemBackup;
                case "seguranca2": return NivelSeguranca >= 2;
                case "seguranca4": return NivelSeguranca >= 4;
                case "automacao": return AutomacoesAtivas > 0;
                case "k8s": return NosKubernetes > 0;
                case "datacenter": return DatacentersExtras > 0;
                case "regiao": return RegioesExtras > 0;
                default: return false;
            }
        }

        /// <summary>O que falta para uma opção (texto curto), ou null se dá para escolher.</summary>
        public string FaltaParaOpcao(OpcaoDef o)
        {
            if (!RequisitoDaOpcao(o))
                switch (o.Requer)
                {
                    case "estagiario": return "precisa de estagiário";
                    case "backup": return "precisa de backup";
                    case "seguranca2": return "precisa de 2 níveis de segurança";
                    case "seguranca4": return "precisa de 4 níveis de segurança";
                    case "automacao": return "precisa de uma automação";
                    case "k8s": return "precisa de Kubernetes";
                    case "datacenter": return "precisa de outro datacenter";
                    case "regiao": return "precisa de outra região";
                    default: return "indisponível";
                }
            if (o.Custo > 0 && Estado.dinheiro < ReceitaPorSegundo * o.Custo) return "sem dinheiro";
            return null;
        }

        public DecisaoDef DecisaoAtual => EmLuta && Luta.decisao >= 0 ? ChefeDaLuta.Decisoes[Luta.decisao] : null;
        public double VidaDoChefe => EmLuta ? Math.Max(0, 1 - Luta.carga / ChefeDaLuta.Vida) : 1;

        /// <summary>Dano por segundo na luta: a renda, com os bônus das escolhas e a queda quando a estabilidade está baixa.</summary>
        /// <summary>A experiência contra o chefe de agora: cada derrota soma BonusPorDerrota.</summary>
        public double FatorExperiencia => 1 + Estado.derrotasNoChefe * Catalogo.BonusPorDerrota;

        public double DanoPorSegundo => ReceitaPorSegundo * (Luta.dpsRestante > 0 ? Luta.dps : 1) * FatorExperiencia
                                        * (Luta.estabilidade < Catalogo.EstabilidadeFraca ? Catalogo.FatorDpsFraco : 1);

        public bool EnfrentarChefe()
        {
            if (!PodeEnfrentarChefe) return false;
            var c = ChefeDoCargo;
            Estado.luta = new LutaChefe { id = c.Id, proximoAtaque = Catalogo.PrimeiroAtaque };
            ChefeComecou?.Invoke(c);
            return true;
        }

        public bool EscolherOpcao(int indice)
        {
            var d = DecisaoAtual;
            if (d == null || indice < 0 || indice >= d.Opcoes.Length) return false;
            var o = d.Opcoes[indice];
            if (FaltaParaOpcao(o) != null) return false;
            if (o.Custo > 0) Estado.dinheiro -= ReceitaPorSegundo * o.Custo;
            var l = Luta;
            bool deuCerto = o.Chance >= 1 || sorteio.NextDouble() < o.Chance;
            if (deuCerto)
            {
                l.estabilidade = Math.Min(100, l.estabilidade + o.Curar);
                if (o.Dps > 1) { l.dps = o.Dps; l.dpsRestante = o.Segundos; }
                if (o.Escudo < 1) { l.escudo = o.Escudo; l.escudoRestante = o.Segundos; }
                l.resultado = o.Sucesso ?? "";
            }
            else
            {
                l.estabilidade -= o.DanoSeFalhar;
                l.resultado = o.Falha ?? "";
            }
            l.decisao = -1;
            l.decisoesFeitas++;
            if (l.estabilidade <= 0) TerminarLuta(false);
            return true;
        }

        void AvancarChefe(double segundos)
        {
            if (Estado.recargaChefe > 0) Estado.recargaChefe = Math.Max(0, Estado.recargaChefe - segundos);
            if (!EmLuta) return;
            var c = ChefeDaLuta;
            var l = Luta;
            if (c == null) { Estado.luta = new LutaChefe(); return; }

            l.decorrido += segundos;
            l.carga += DanoPorSegundo * segundos;
            if (l.dpsRestante > 0) l.dpsRestante -= segundos;
            if (l.escudoRestante > 0) l.escudoRestante -= segundos;
            l.estabilidade = Math.Min(100, l.estabilidade + Catalogo.RegeneracaoPorSegundo * segundos);

            if (l.carga >= c.Vida) { TerminarLuta(true); return; }

            // as decisões abrem quando a vida do chefe passa das marcas; sem resposta, vale a primeira opção
            if (l.decisao >= 0)
            {
                l.decisaoRestante -= segundos;
                if (l.decisaoRestante <= 0) EscolherOpcao(0);
                if (!EmLuta) return;
            }
            else if (l.decisoesFeitas < c.Decisoes.Length && l.decisoesFeitas < Catalogo.VidaDasDecisoes.Length
                     && VidaDoChefe <= Catalogo.VidaDasDecisoes[l.decisoesFeitas])
            {
                l.decisao = l.decisoesFeitas;
                l.decisaoRestante = Catalogo.TempoParaDecidir;
                DecisaoAberta?.Invoke(c, c.Decisoes[l.decisao]);
            }

            l.proximoAtaque -= segundos;
            if (l.proximoAtaque <= 0)
            {
                var a = c.Ataques[l.ataques % c.Ataques.Length];
                double dano = a.Dano * (1 - ValorDefesa(a.Tipo)) * (l.escudoRestante > 0 ? l.escudo : 1);
                l.estabilidade -= dano;
                l.ataques++;
                l.ultimoAtaque = a.Texto;
                l.ultimoDano = dano;
                l.proximoAtaque += Catalogo.IntervaloDeAtaque;
                ChefeAtacou?.Invoke(c, a, dano);
            }

            if (l.estabilidade <= 0 || l.decorrido >= Catalogo.TempoDaLuta) TerminarLuta(false);
        }

        void TerminarLuta(bool venceu)
        {
            var c = ChefeDaLuta;
            Estado.luta = new LutaChefe();
            double premio = 0;
            if (venceu)
            {
                premio = ReceitaPorSegundo * Catalogo.SegundosDoPremioDoChefe;
                Ganhar(premio);
                Estado.chefesVencidos++;
                Estado.derrotasNoChefe = 0;
            }
            else
            {
                Estado.recargaChefe = Catalogo.RecargaDoChefe;
                Estado.derrotasChefe++;
                Estado.derrotasNoChefe++;
            }
            ChefeTerminou?.Invoke(c, venceu, premio);
            if (!venceu) return;
            if (PodeFazerIpo) FazerIpo(); else Promover();
        }

        /// <summary>Com o jogo fechado ninguém luta: a luta é cancelada (sem perder nada) e a espera acaba.</summary>
        void CancelarLutaOffline()
        {
            Estado.luta = new LutaChefe();
            Estado.recargaChefe = 0;
        }
    }
}
