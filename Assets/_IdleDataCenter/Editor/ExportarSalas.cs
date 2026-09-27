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
                var nome = Path.Combine(pasta, Path.GetFileNameWithoutExtension(arquivo));
                Salvar(sala, 1.3f, SalaIso.Vista.Sala, nome + "_sala.png");
                if (economia.NoCampus) Salvar(sala, 1.3f, SalaIso.Vista.Campus, nome + "_campus.png");
                if (economia.NoMundo) Salvar(sala, 1.3f, SalaIso.Vista.Mundo, nome + "_mundo.png");
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
