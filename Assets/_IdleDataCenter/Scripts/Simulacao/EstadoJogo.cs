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
