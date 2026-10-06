using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Tudo o que é salvo. Só dados, sem lógica (compatível com JsonUtility).</summary>
    [Serializable]
    public class EstadoJogo
    {
        public const int VersaoAtual = 3;   // 3: o Freelancer entrou antes do Técnico

        public int versao = VersaoAtual;
        public double dinheiro;
        public double totalGanho;
        public int cargo;                   // índice em Catalogo.Cargos
        public int incidentesResolvidos;
        public List<NivelMelhoria> melhorias = new List<NivelMelhoria>();
        public List<Travamento> travamentos = new List<Travamento>();
        public long ultimoSalvamentoUnix;   // segundos UTC; base do progresso offline
        public bool jaClicouNoServidor;     // esconde a dica do primeiro clique

        // Storage (cargo 3): um disco queimado de cada vez
        public bool discoQueimado;
        public double discoSegundos;         // há quanto tempo está queimado
        public int backupsRestaurados;

        // Automações
        public List<string> automacoes = new List<string>();   // prontas e ativas
        public string escrevendo = "";       // id da que o técnico está escrevendo (vazio = nenhuma)
        public double segundosEscritos;

        // DevOps: deploy quebrado derruba os apps até o rollback
        public bool deployQuebrado;
        public double deploySegundos;

        // SRE: picos de tráfego
        public double proximoPico = -1;       // segundos até o próximo (-1 = ainda não agendado)
        public string picoNome = "";          // vazio = sem pico agora
        public double picoDecorrido;
        public bool picoEscalado, picoViolado;
        public int picosSobrevividos, picosTotal;

        // Café (receita em dobro por um tempo) e chamados urgentes (bônus se atendidos a tempo)
        public double cafeRestante, cafeRecarga;
        public double proximoChamado = -1;    // segundos até o próximo (-1 = ainda não agendado)
        public List<ChamadoAberto> chamados = new List<ChamadoAberto>();   // a fila do help desk (ver HelpDesk.cs)
        public double helpDeskTrabalho;        // quanto a equipe já trabalhou no chamado da vez
        public int chamadosAtendidos, chamadosP1;
        public int cafesTomados;
        public int comandosCertos;             // problemas resolvidos digitando no terminal (ver Terminal.cs)

        // Clientes (ver Clientes.cs): os que hospedam aqui e a proposta na mesa (nome vazio = nenhuma)
        public List<Cliente> clientes = new List<Cliente>();
        public Cliente propostaCliente = new Cliente();
        public int pedidosAtendidos;
        // saves antigos: os contratos viram clientes (Economia.MigrarContratos)
        public Contrato proposta = new Contrato();
        public List<Contrato> contratos = new List<Contrato>();
        public double proximaProposta = -1;
        public int contratosCumpridos;
        public double chamadoRestante;        // saves antigos: o chamado único (vira um P3 na fila)
        public string chamadoTexto = "";

        // Evento aleatório em andamento (vazio = nenhum): ver Eventos.cs
        public string evento = "";
        public int eventoFase;
        public double eventoSegundos;
        public double proximoEvento = -1;     // segundos até o próximo (-1 = ainda não agendado)

        // Uptime da última hora (0 a 1): ver Uptime.cs. Começa em 99%: os noves do SLA se conquistam
        public double uptime = 0.99;

        // Idade das torres e servidores 1U em segundos de jogo (ver Hardware.cs); o refresh zera
        public double idadeServidores;

        // Arquiteto: queda de energia num datacenter (índice do DC extra, -1 = nenhuma)
        public int quedaDc = -1;
        public double quedaSegundos;

        // CTO: pane regional (índice da região extra, -1 = nenhuma) e o IPO
        public int paneRegiao = -1;
        public double paneSegundos;
        public bool ipoFeito;

        // Desafio escolhido para esta empresa (vazio = nenhum); multiplica as certificações na venda
        public string desafio = "";

        // Prestígio: o que sobrevive à venda da empresa
        public Prestigio prestigio = new Prestigio();

        // Tutorial do modo gerente: passo atual (Catalogo.PassosTutorial = concluído)
        public int tutorial;
    }

    /// <summary>O que o jogador leva de uma empresa para a outra.</summary>
    [Serializable]
    public class Prestigio
    {
        public int certificacoes;                 // para gastar
        public int certificacoesGanhas;           // total da carreira
        public int empresasVendidas;
        public List<NivelMelhoria> bonus = new List<NivelMelhoria>();
        public List<string> trofeus = new List<string>();   // um por empresa vendida (cargo em que vendeu, ou "IPO")
        public List<string> conquistas = new List<string>();   // ids das conquistas (ver Conquistas.cs): +0,5% de renda cada
    }

    [Serializable]
    public class NivelMelhoria
    {
        public string id;
        public int nivel;
    }

    /// <summary>Um servidor travado: qual (índice) e há quanto tempo.</summary>
    [Serializable]
    public class Travamento
    {
        public int servidor;
        public double segundos;
    }
}
