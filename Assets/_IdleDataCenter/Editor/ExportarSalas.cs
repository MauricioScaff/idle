using System.IO;
using IdleDataCenter.Gerente;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Ferramentas
{
    /// <summary>
    /// Desenha a sala isométrica de cada save de uma pasta em PNG (para mockups e revisão de arte).
    /// Uso: Unity -batchmode -quit -executeMethod IdleDataCenter.Ferramentas.ExportarSalas.Exportar -pasta=&lt;dir com *.json&gt;
    /// </summary>
    public static class ExportarSalas
    {
        public static void Exportar()
        {
            string pasta = null;
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a.StartsWith("-pasta=")) pasta = a.Substring(7);
            if (pasta == null) { Debug.LogError("Falta -pasta=<dir>"); return; }

            foreach (var arquivo in Directory.GetFiles(pasta, "*.json"))
            {
                var estado = JsonUtility.FromJson<EstadoJogo>(File.ReadAllText(arquivo));
                var economia = new Economia(estado);
                var sala = new SalaIso(economia);
                sala.MostrarObstaculos = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-obstaculos");   // teste: piso bloqueado e caminhos
                var nome = Path.Combine(pasta, Path.GetFileNameWithoutExtension(arquivo));
                Salvar(sala, 1.3f, SalaIso.Vista.Sala, nome + "_sala.png");
                // teste: -filme desenha a sala em 24 momentos seguidos (as pessoas andando), para ver se alguém passa por cima de algo
                if (System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-filme"))
                {
                    sala.MostrarObstaculos = false;
                    for (int k = 0; k < 24; k++) Salvar(sala, 2f + k * 2.5f, SalaIso.Vista.Sala, nome + "_q" + k.ToString("00") + ".png");
                }
                if (economia.NoCampus) Salvar(sala, 1.3f, SalaIso.Vista.Campus, nome + "_campus.png");
                if (economia.NoMundo) Salvar(sala, 1.3f, SalaIso.Vista.Mundo, nome + "_mundo.png");
                if (economia.Cargo >= Catalogo.CargoAnalista)
                {
                    Salvar(sala, 1.3f, SalaIso.Vista.Dados, nome + "_dados.png");
                    Salvar(sala, 1.3f, SalaIso.Vista.Rede, nome + "_rede.png");
                }
            }
        }

        static void Salvar(SalaIso sala, float tempo, SalaIso.Vista vista, string arquivo)
        {
            sala.Desenhar(tempo, vista);
            File.WriteAllBytes(arquivo, sala.Textura.EncodeToPNG());
            Debug.Log("Exportado: " + arquivo);
        }
    }
}
