using System;
using System.IO;
using UnityEngine;

namespace IdleDataCenter.Simulacao
{
    /// <summary>
    /// Save em JSON na pasta de dados do jogo, com cópia do save anterior (.bak) para o caso
    /// de o arquivo principal corromper (ex.: PC desligou no meio da gravação).
    /// </summary>
    public static class Salvamento
    {
        static string Pasta => Application.persistentDataPath;
        static string Arquivo => Path.Combine(Pasta, "save.json");
        static string Reserva => Path.Combine(Pasta, "save.bak");
        static string Temporario => Path.Combine(Pasta, "save.tmp");

        public static long AgoraUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static EstadoJogo Carregar()
        {
            foreach (var caminho in new[] { Arquivo, Reserva })
            {
                try
                {
                    if (!File.Exists(caminho)) continue;
                    var estado = JsonUtility.FromJson<EstadoJogo>(File.ReadAllText(caminho));
                    if (estado != null) return estado;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Save ilegível em {caminho}: {e.Message}");
                }
            }
            return new EstadoJogo();
        }

        public static void Salvar(EstadoJogo estado)
        {
            try
            {
                estado.ultimoSalvamentoUnix = AgoraUnix;
                Directory.CreateDirectory(Pasta);
                // grava num temporário e só então troca, para nunca deixar um save pela metade
                File.WriteAllText(Temporario, JsonUtility.ToJson(estado, true));
                if (File.Exists(Arquivo)) File.Copy(Arquivo, Reserva, true);
                File.Copy(Temporario, Arquivo, true);
                File.Delete(Temporario);
            }
            catch (Exception e)
            {
                Debug.LogError("Falha ao salvar: " + e.Message);
            }
        }
    }
}
