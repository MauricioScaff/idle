using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleDataCenter.Ferramentas
{
    /// <summary>Menu "Idle Data Center": configura o projeto, monta a cena e gera o build de Windows.</summary>
    public static class Configurar
    {
        const string CaminhoCena = "Assets/_IdleDataCenter/Scenes/Faixa.unity";
        const string CaminhoBuild = "Builds/IdleDataCenter.exe";

        /// <summary>Aplica as configurações de janela e gráficos que a faixa precisa. Seguro de rodar sempre.</summary>
        [MenuItem("Idle Data Center/Configurar projeto")]
        public static void ConfigurarPlayer()
        {
            PlayerSettings.productName = "Idle Data Center";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 144;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;       // continua rodando com o foco em outro programa
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.useFlipModelSwapchain = false; // necessário para a janela transparente
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SplashScreen.show = false;
            DefinirIcone();
        }

        /// <summary>
        /// O ícone do .exe (barra de tarefas e bandeja): a torre desenhada pixel a pixel em 16/32/48 px e o emblema
        /// nos tamanhos grandes. As imagens saem de itch/gerar_icone.ps1.
        /// </summary>
        static void DefinirIcone()
        {
            const string Pasta = "Assets/_IdleDataCenter/Icone/icone_";
            Texture2D Carregar(int lado) => AssetDatabase.LoadAssetAtPath<Texture2D>(Pasta + lado + ".png");
            int[] disponiveis = { 16, 32, 48, 128, 256, 512, 1024 };
            var standalone = UnityEditor.Build.NamedBuildTarget.Standalone;
            var tamanhos = PlayerSettings.GetIconSizes(standalone, IconKind.Any);
            var icones = new Texture2D[tamanhos.Length];
            // cada tamanho pedido pelo Windows recebe o desenho daquele tamanho (ou o maior mais próximo)
            for (int i = 0; i < tamanhos.Length; i++)
            {
                int pedido = tamanhos[i], lado = System.Array.Find(disponiveis, t => t >= pedido);
                icones[i] = Carregar(lado > 0 ? lado : 1024);
            }
            if (System.Array.Exists(icones, t => t == null)) { Debug.LogWarning("Ícone: faltam imagens em " + Pasta + "*.png"); return; }
            PlayerSettings.SetIcons(standalone, icones, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { Carregar(256) }, IconKind.Any);
        }

        /// <summary>
        /// A transparência da faixa só aparece no build (o editor não fica transparente).
        /// Nunca mexe na cena: só cria uma se ela ainda não existir.
        /// </summary>
        [MenuItem("Idle Data Center/Gerar build de Windows")]
        public static void GerarBuild()
        {
            ConfigurarPlayer();
            if (!File.Exists(CaminhoCena)) CriarCena();
            var r = BuildPipeline.BuildPlayer(new[] { CaminhoCena }, CaminhoBuild, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Build: {r.summary.result} ({r.summary.totalErrors} erros) em {Path.GetFullPath(CaminhoBuild)}");
        }

        /// <summary>Recria a cena do zero. Pede confirmação, porque apaga qualquer edição feita nela.</summary>
        [MenuItem("Idle Data Center/Recriar cena do zero")]
        public static void RecriarCena()
        {
            if (File.Exists(CaminhoCena) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Recriar cena", "Isso apaga todas as mudanças feitas na cena Faixa. Continuar?", "Recriar", "Cancelar"))
                return;
            CriarCena();
        }

        static void CriarCena()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CaminhoCena));
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.allowMSAA = false;
            camera.allowHDR = false;

            new GameObject("Faixa").AddComponent<Faixa>();

            EditorSceneManager.SaveScene(cena, CaminhoCena);
            var cenas = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(CaminhoCena, true) };
            EditorBuildSettings.scenes = cenas.ToArray();
        }
    }
}
