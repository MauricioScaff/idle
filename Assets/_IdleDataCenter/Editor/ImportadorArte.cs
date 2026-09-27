using UnityEditor;
using UnityEngine;

namespace IdleDataCenter.Ferramentas
{
    /// <summary>
    /// Toda imagem em Resources/Arte entra como pixel art: sem suavização, sem compressão, sem mipmaps
    /// e legível pelo código (o jogo recorta, recolore e acha os LEDs lendo os pixels).
    /// </summary>
    public class ImportadorArte : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string caminho = assetPath.Replace('\\', '/');
            if (caminho.Contains("/_IdleDataCenter/Resources/Isometrico/"))
            {
                // modo gerente: a ilustração é lida pixel a pixel para escurecer os setores bloqueados
                var iso = (TextureImporter)assetImporter;
                iso.isReadable = true;
                iso.mipmapEnabled = false;
                iso.textureCompression = TextureImporterCompression.Uncompressed;
                iso.npotScale = TextureImporterNPOTScale.None;
                iso.wrapMode = TextureWrapMode.Clamp;
                return;
            }
            if (!caminho.Contains("/_IdleDataCenter/Resources/Arte/")) return;
            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Default;
            importador.isReadable = true;
            importador.filterMode = FilterMode.Point;
            importador.mipmapEnabled = false;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
