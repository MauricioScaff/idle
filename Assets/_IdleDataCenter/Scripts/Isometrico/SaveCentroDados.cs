using System;
using System.IO;
using UnityEngine;

namespace IdleDataCenter.Isometrico
{
    public static class SaveCentroDados
    {
        public static string Caminho => Path.Combine(Application.persistentDataPath, "save-isometrico.json");

        public static CentroDadosEstado Carregar(string caminho = null)
        {
            caminho = caminho ?? Caminho;
            foreach (var arquivo in new[] { caminho, caminho + ".bak" })
            {
                try
                {
                    if (!File.Exists(arquivo)) continue;
                    var estado = JsonUtility.FromJson<CentroDadosEstado>(File.ReadAllText(arquivo));
                    if (estado == null || estado.versao != 1 || estado.economia == null ||
                        estado.economia.melhorias == null || estado.economia.automacoes == null ||
                        estado.economia.travamentos == null || double.IsNaN(estado.economia.dinheiro) ||
                        double.IsInfinity(estado.economia.dinheiro) || estado.economia.cargo != 3)
                        throw new InvalidDataException("Campanha inválida.");
                    return estado;
                }
                catch (Exception e) { Debug.LogWarning("Não foi possível carregar " + arquivo + ": " + e.Message); }
            }
            return CentroDadosSimulacao.NovoEstado();
        }

        public static bool Salvar(CentroDadosEstado estado, string caminho = null)
        {
            caminho = caminho ?? Caminho;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(caminho)));
                estado.economia.ultimoSalvamentoUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(caminho + ".tmp", JsonUtility.ToJson(estado, true));
                if (File.Exists(caminho)) File.Replace(caminho + ".tmp", caminho, caminho + ".bak");
                else File.Move(caminho + ".tmp", caminho);
                return true;
            }
            catch (Exception e) { Debug.LogError("Falha ao salvar a campanha isométrica: " + e.Message); return false; }
        }
    }
}
