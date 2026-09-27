using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleDataCenter.Isometrico;

namespace IdleDataCenter.Ferramentas
{
    public static class ConfigurarIsometrico
    {
        public const string Cena = "Assets/_IdleDataCenter/Scenes/DataCenter.unity";
        public const string Executavel = "Builds/Isometrico/IdleDevOps.exe";

        [MenuItem("Idle Data Center/Isometrico/Abrir cena")]
        public static void Abrir()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepararCena();
            EditorSceneManager.OpenScene(Cena);
        }

        public static void PrepararCena()
        {
            if (File.Exists(Cena)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            var cam = new GameObject("Main Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam, scene);
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 0, -10);
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = IsoGui.Fundo;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            cam.AddComponent<AudioListener>();
            var jogo = new GameObject("Data Center - Isometric Management");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(jogo, scene);
            jogo.AddComponent<CentroDados>();
            EditorSceneManager.SaveScene(scene, Cena);
            if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Idle Data Center/Isometrico/Gerar build de Windows")]
        public static void GerarBuild()
        {
            PrepararCena();
            string nome = PlayerSettings.productName;
            int w = PlayerSettings.defaultScreenWidth, h = PlayerSettings.defaultScreenHeight;
            bool resize = PlayerSettings.resizableWindow, troca = PlayerSettings.allowFullscreenSwitch;
            var modo = PlayerSettings.fullScreenMode;
            try
            {
                PlayerSettings.productName = "Idle DevOps - Data Center";
                PlayerSettings.defaultScreenWidth = 1440;
                PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.allowFullscreenSwitch = true;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                Directory.CreateDirectory(Path.GetDirectoryName(Executavel));
                var report = BuildPipeline.BuildPlayer(new[] { Cena }, Executavel, BuildTarget.StandaloneWindows64, BuildOptions.None);
                Debug.Log("ISOMETRIC_BUILD " + report.summary.result + "; errors=" + report.summary.totalErrors + "; " + Path.GetFullPath(Executavel));
                if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build isométrico falhou.");
            }
            finally
            {
                PlayerSettings.productName = nome;
                PlayerSettings.defaultScreenWidth = w;
                PlayerSettings.defaultScreenHeight = h;
                PlayerSettings.resizableWindow = resize;
                PlayerSettings.allowFullscreenSwitch = troca;
                PlayerSettings.fullScreenMode = modo;
                AssetDatabase.SaveAssets();
            }
        }
    }

    public class ImportadorIsometrico : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/_IdleDataCenter/Resources/Isometrico/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.filterMode = FilterMode.Point;
            t.mipmapEnabled = false;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.npotScale = TextureImporterNPOTScale.None;
            t.alphaIsTransparency = true;
            t.wrapMode = TextureWrapMode.Clamp;
            t.maxTextureSize = 2048;
        }
    }
}
