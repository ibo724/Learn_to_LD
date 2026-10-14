using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VillaSceneReport
{
    [System.Serializable] private class Entry { public string name, path; public Vector3 position, min, max; }
    [System.Serializable] private class Report { public Entry[] renderers, landmarks; }
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    public static void Export()
    {
        EditorSceneManager.OpenScene("Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity");
        Report report = new Report
        {
            renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Select(r => new Entry
            { name = r.name, path = PathOf(r.transform), position = r.transform.position, min = r.bounds.min, max = r.bounds.max }).ToArray(),
            landmarks = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name.Contains("Door") || t.name.Contains("Floor") || t.name.Contains("Pool") || t.name == "ScaleRef" || t.name == "Garden_Terrain")
                .Select(t => new Entry { name = t.name, path = PathOf(t), position = t.position }).ToArray()
        };
        Directory.CreateDirectory("Logs/VillaFPS");
        File.WriteAllText("Logs/VillaFPS/scene-report.json", JsonUtility.ToJson(report, true));
        Debug.Log("VILLA_SCENE_REPORT_READY");
    }
}
