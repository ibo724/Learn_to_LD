using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Keeps the latest scan and refreshes it shortly after the scene changes.</summary>
[InitializeOnLoad]
public static class LDScanService
{
    private const double Delay = 0.5;
    private static double scanAt = -1;

    public static LDScanResult Last { get; private set; } = LDScanResult.Empty;
    public static event Action Changed;

    static LDScanService()
    {
        if (Application.isBatchMode) return;
        EditorApplication.hierarchyChanged += RequestScan;
        Undo.undoRedoPerformed += RequestScan;
        ObjectChangeEvents.changesPublished += OnChangesPublished;
        EditorSceneManager.activeSceneChangedInEditMode += (_, __) => RequestScan();
        EditorApplication.playModeStateChanged += _ => RequestScan();
        EditorApplication.update += Update;
        RequestScan();
    }

    public static void RequestScan() => scanAt = EditorApplication.timeSinceStartup + Delay;

    public static void ScanNow()
    {
        scanAt = -1;
        Last = LDSceneScanner.Scan();
        Changed?.Invoke();
    }

    private static void OnChangesPublished(ref ObjectChangeEventStream stream) => RequestScan();

    private static void Update()
    {
        if (scanAt >= 0 && EditorApplication.timeSinceStartup >= scanAt) ScanNow();
    }
}
