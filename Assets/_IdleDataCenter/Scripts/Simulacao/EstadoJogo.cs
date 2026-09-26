using System;
using System.Collections.Generic;

namespace IdleDataCenter.Simulacao
{
    /// <summary>Tudo o que é salvo. Só dados, sem lógica (compatível com JsonUtility).</summary>
    [Serializable]
    public class EstadoJogo
    {
        public const int VersaoAtual = 1;

        public int versao = VersaoAtual;
        public double dinheiro;
        public double totalGanho;
        public List<NivelMelhoria> melhorias = new List<NivelMelhoria>();
        public long ultimoSalvamentoUnix;   // segundos UTC; base do progresso offline
        public bool jaClicouNoServidor;     // esconde a dica do primeiro clique
    }

    [Serializable]
    public class NivelMelhoria
    {
        public string id;
        public int nivel;
    }
}
