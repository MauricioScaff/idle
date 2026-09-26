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

        [MenuItem("Idle Data Center/Configurar projeto e cena")]
        public static void ProjetoECena()
        {
            ConfigurarPlayer();
            CriarCena();
        }

        /// <summary>A transparência da faixa só aparece no build (o editor não fica transparente).</summary>
        [MenuItem("Idle Data Center/Gerar build de Windows")]
        public static void GerarBuild()
        {
            ProjetoECena();
            var r = BuildPipeline.BuildPlayer(new[] { CaminhoCena }, CaminhoBuild, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Build: {r.summary.result} ({r.summary.totalErrors} erros) em {Path.GetFullPath(CaminhoBuild)}");
        }

        static void ConfigurarPlayer()
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
