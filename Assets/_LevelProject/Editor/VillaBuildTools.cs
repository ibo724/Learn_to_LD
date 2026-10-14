#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
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
    private const string PrototypeVersion="0.2.0";
    [MenuItem("Tools/Villa FPS/Build Windows prototype")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before building.");
        if (!File.Exists(VillaSceneBuilder.GameScene)) throw new InvalidOperationException("Prepare the FPS scene first.");
        string buildRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds"));
        string destination=Path.Combine(buildRoot,"Villa_Operation");
        string folder=Path.Combine(buildRoot,"Villa_Operation_Staging_"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        AssertBuildPath(buildRoot,folder);
        Directory.CreateDirectory(folder);
        // Building can unload the original assets. Keep paths so restoration does not use destroyed Unity objects.
        string previousDefaultPath = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
        string previousQualityPath = AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
        int previousQualityLevel = QualitySettings.GetQualityLevel();
        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        string previousProduct = PlayerSettings.productName;
        string previousVersion=PlayerSettings.bundleVersion;
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
            PlayerSettings.bundleVersion=PrototypeVersion;
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
                "VILLA OPERASYONU "+PrototypeVersion+"\r\n\r\nVilla_Operation.exe dosyasini calistirin. Data klasoru ve DLL dosyalari ayni klasorde kalmali.\r\n"
                + "WASD hareket, fare bakis, sol tik ates, sag tik nisan, Shift kosu, Space ziplama, R doldurma, E etkilesim, Esc duraklatma.\r\n"
                + "Ust kattaki veri cihazini alip bahcedeki CIKIS bolgesine donun.\r\n"
                + "Bu ilk prototiptir. Derleme hazirlandi; oynanis ve gorsel kontroller kullanici tarafindan yapilacak.\r\n");
            File.WriteAllText(Path.Combine(folder,"BUILD_INFO.json"),JsonUtility.ToJson(new BuildReceipt
            {
                version=PrototypeVersion,engine=Application.unityVersion,builtUtc=DateTime.UtcNow.ToString("O"),
                scene=VillaSceneBuilder.GameScene,sourceSha256=HashFile(VillaSceneBuilder.SourceScene),
                sceneSha256=HashFile(VillaSceneBuilder.GameScene),balanceSha256=HashFile("Assets/_LevelProject/FPS/Settings/Villa Balance.asset"),
                buildResult=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,
                sizeBytes=report.summary.totalSize,manualVerification="NOT RUN"
            },true));
            PublishBuild(buildRoot,folder,destination);
            Debug.Log("VILLA_FPS_WINDOWS_BUILD_READY " + destination);
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
                PlayerSettings.bundleVersion=previousVersion;
                PlayerSettings.defaultScreenWidth=previousWidth; PlayerSettings.defaultScreenHeight=previousHeight;
                PlayerSettings.fullScreenMode=previousFullscreen; PlayerSettings.resizableWindow=previousResizable;
                AssetDatabase.SaveAssets();
            }
        }
    }

    [Serializable] private class BuildReceipt
    {
        public string version,engine,builtUtc,scene,sourceSha256,sceneSha256,balanceSha256,buildResult,manualVerification;
        public int errors,warnings;
        public ulong sizeBytes;
    }
    private static string HashFile(string path)
    {
        if (!File.Exists(path)) return "MISSING";
        using (SHA256 hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","");
    }

    private static void AssertBuildPath(string buildRoot,string path)
    {
        string prefix=Path.GetFullPath(buildRoot).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Build path escaped the project's Builds directory: "+path);
    }

    private static void PublishBuild(string buildRoot,string staged,string destination)
    {
        AssertBuildPath(buildRoot,staged); AssertBuildPath(buildRoot,destination);
        string previous=null;
        if (Directory.Exists(destination))
        {
            string history=Path.Combine(buildRoot,"History");
            AssertBuildPath(buildRoot,history); Directory.CreateDirectory(history);
            previous=Path.Combine(history,"Villa_Operation-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            AssertBuildPath(buildRoot,previous);
            Directory.Move(destination,previous);
        }
        try { Directory.Move(staged,destination); }
        catch
        {
            if (previous!=null && Directory.Exists(previous) && !Directory.Exists(destination)) Directory.Move(previous,destination);
            throw;
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
