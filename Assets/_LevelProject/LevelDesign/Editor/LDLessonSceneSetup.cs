using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Creates (or reopens) the lesson scene and the three greybox materials.</summary>
public static class LDLessonSceneSetup
{
    public const string ScenePath = "Assets/_LevelProject/LevelDesign/Scenes/LD_Ders01_Arena.unity";
    private const string Root = "Assets/_LevelProject/LevelDesign";
    private const string MaterialFolder = Root + "/Materials";

    public static void CreateOrOpen()
    {
        if (Application.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureMaterials();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        EnsureFolder(Root, "Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("Arena");
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void EnsureMaterials()
    {
        EnsureFolder(Root, "Materials");
        EnsureMaterial("LD_Gri", new Color(0.6f, 0.6f, 0.6f));
        EnsureMaterial("LD_Sari", new Color(0.95f, 0.75f, 0.2f));
        EnsureMaterial("LD_Kirmizi", new Color(0.85f, 0.25f, 0.2f));
    }

    private static void EnsureMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.defaultShader : Shader.Find("Standard");
            Debug.LogWarning($"URP Lit shader bulunamadı; {name} malzemesi {shader?.name} ile oluşturuldu.");
        }
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        AssetDatabase.CreateAsset(material, path);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child);
    }
}
