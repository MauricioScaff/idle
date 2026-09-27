using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Tudo o que é salvo. Só dados, sem lógica (compatível com JsonUtility).</summary>
    [Serializable]
    public class EstadoJogo
    {
        public const int VersaoAtual = 2;

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
        public double chamadoRestante;        // > 0: tem chamado esperando
        public string chamadoTexto = "";
        public int chamadosAtendidos;

        // Arquiteto: queda de energia num datacenter (índice do DC extra, -1 = nenhuma)
        public int quedaDc = -1;
        public double quedaSegundos;

        // CTO: pane regional (índice da região extra, -1 = nenhuma) e o IPO
        public int paneRegiao = -1;
        public double paneSegundos;
        public bool ipoFeito;

        // Tutorial do modo gerente: passo atual (Catalogo.PassosTutorial = concluído)
        public int tutorial;
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
