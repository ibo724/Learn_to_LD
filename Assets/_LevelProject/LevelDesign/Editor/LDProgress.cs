using UnityEditor;
using UnityEngine;

/// <summary>Remembers the chosen lesson, step and ticked boxes per project, across editor restarts.</summary>
public static class LDProgress
{
    public static string LessonId
    {
        get => EditorPrefs.GetString(Key("lesson"), "");
        set => EditorPrefs.SetString(Key("lesson"), value ?? "");
    }

    public static int GetStep(string lessonId) => EditorPrefs.GetInt(Key(lessonId + ".step"), 0);
    public static void SetStep(string lessonId, int step) => EditorPrefs.SetInt(Key(lessonId + ".step"), step);

    public static bool GetManual(string lessonId, int step, int index) => EditorPrefs.GetBool(Key($"{lessonId}.{step}.manual.{index}"), false);
    public static void SetManual(string lessonId, int step, int index, bool value) => EditorPrefs.SetBool(Key($"{lessonId}.{step}.manual.{index}"), value);

    /// <summary>EditorPrefs are machine-wide, so keys carry a hash of this project's path.</summary>
    public static string Key(string suffix) => "LD." + Hash128.Compute(Application.dataPath) + "." + suffix;
}
