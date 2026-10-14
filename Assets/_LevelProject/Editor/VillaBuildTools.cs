#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VillaFPS.Editor;

/// <summary>Editor-only preparation and Windows compilation. Never starts a player.</summary>
public static class VillaBuildTools
{
    [MenuItem("Tools/Villa FPS/Build Windows prototype")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before building.");
        if (!File.Exists(VillaSceneBuilder.GameScene)) throw new InvalidOperationException("Prepare the FPS scene first.");
        string folder = "Builds/Villa_Operation";
        Directory.CreateDirectory(folder);
        // Building can unload the original assets. Keep paths so restoration does not use destroyed Unity objects.
        string previousDefaultPath = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
        string previousQualityPath = AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
        int previousQualityLevel = QualitySettings.GetQualityLevel();
        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        string previousProduct = PlayerSettings.productName;
        int previousWidth=PlayerSettings.defaultScreenWidth, previousHeight=PlayerSettings.defaultScreenHeight;
        FullScreenMode previousFullscreen=PlayerSettings.fullScreenMode;
        bool previousResizable=PlayerSettings.resizableWindow;
        try
        {
            // Make the runtime Forward pipeline visible to URP's build-time shader variant collector.
            var pipeline = VillaRendering.CreatePipeline();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.productName = "Villa Operasyonu";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true;
            AssetDatabase.SaveAssets();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { VillaSceneBuilder.GameScene },
                locationPathName = folder + "/Villa_Operation.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Directory.CreateDirectory("Logs/VillaFPS");
            File.WriteAllText("Logs/VillaFPS/windows-build.txt",
                "Result: " + report.summary.result + "\nErrors: " + report.summary.totalErrors
                + "\nWarnings: " + report.summary.totalWarnings + "\nSize: " + report.summary.totalSize
                + "\nDuration: " + report.summary.totalTime + "\nPlayer was not launched. Manual testing is pending.\n");
            using (var messages = new StreamWriter("Logs/VillaFPS/build-messages.txt",false))
                foreach (BuildStep step in report.steps)
                    foreach (BuildStepMessage message in step.messages)
                        if (message.type == LogType.Warning || message.type == LogType.Error || message.type == LogType.Exception)
                            messages.WriteLine(step.name + ": " + message.type + ": " + message.content);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed; inspect Logs/VillaFPS-build.log.");
            File.WriteAllText(folder + "/BASLAMADAN_ONCE.txt",
                "VILLA OPERASYONU\r\n\r\nVilla_Operation.exe dosyasini calistirin. Data klasoru ve DLL dosyalari ayni klasorde kalmali.\r\n"
                + "WASD hareket, fare bakis, sol tik ates, sag tik nisan, Shift kosu, Space ziplama, R doldurma, E etkilesim, Esc duraklatma.\r\n"
                + "Ust kattaki veri cihazini alip bahcedeki CIKIS bolgesine donun.\r\n"
                + "Bu ilk prototiptir. Derleme hazirlandi; oynanis ve gorsel kontroller kullanici tarafindan yapilacak.\r\n");
            Debug.Log("VILLA_FPS_WINDOWS_BUILD_READY " + Path.GetFullPath(folder));
        }
        finally
        {
            try
            {
                if (QualitySettings.GetQualityLevel() != previousQualityLevel) QualitySettings.SetQualityLevel(previousQualityLevel,false);
                GraphicsSettings.defaultRenderPipeline = LoadPipeline(previousDefaultPath);
                QualitySettings.renderPipeline = LoadPipeline(previousQualityPath);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,previousBackend);
                PlayerSettings.productName = previousProduct;
                PlayerSettings.defaultScreenWidth=previousWidth; PlayerSettings.defaultScreenHeight=previousHeight;
                PlayerSettings.fullScreenMode=previousFullscreen; PlayerSettings.resizableWindow=previousResizable;
                AssetDatabase.SaveAssets();
            }
        }
    }
    private static RenderPipelineAsset LoadPipeline(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
        if (pipeline == null) throw new InvalidOperationException("The original render pipeline could not be restored: " + path);
        return pipeline;
    }
    public static void PrepareAndBuild()
    {
        VillaSceneBuilder.Prepare();
        BuildWindows();
    }
}
#endif
