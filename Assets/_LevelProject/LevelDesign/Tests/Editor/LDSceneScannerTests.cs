using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LDSceneScannerTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Piece(string name, LDRoleType role, Vector3 position, Vector3 size, bool collider)
    {
        var go = collider ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject();
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = size;
        go.AddComponent<LDRole>().role = role;
        created.Add(go);
        return go;
    }

    private Scene previous, scratch;

    // The scanner reads the active scene, so tests run in an empty extra scene and leave a saved user scene alone.
    // Unity can't add a scene next to an unsaved untitled one (as in batch runs); that scene is used as is.
    [SetUp] public void SetUp()
    {
        previous = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(previous.path)) return;
        scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scratch);
    }

    [TearDown] public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
        if (!scratch.IsValid()) return;
        SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scratch, true);
    }

    private void Entrances()
    {
        Piece("Oyuncu", LDRoleType.OyuncuGirisi, new Vector3(0, 1, 0), new Vector3(1, 2, 1), true);
        Piece("Dusman", LDRoleType.DusmanGirisi, new Vector3(0, 0, 20), Vector3.one, false);
    }

    [Test] public void Kural9_AcikHatUyarir()
    {
        Entrances();
        LDScanResult scan = LDSceneScanner.Scan();
        Assert.IsFalse(scan.SightLines.Single().Blocked);
        Assert.AreEqual(1, scan.Findings.Count(f => f.Rule == 9));
    }

    [Test] public void Kural9_DuvarHattiKeser()
    {
        Entrances();
        Piece("Duvar", LDRoleType.Duvar, new Vector3(0, 2, 10), new Vector3(6, 4, 0.5f), true);
        LDScanResult scan = LDSceneScanner.Scan();
        Assert.IsTrue(scan.SightLines.Single().Blocked);
        Assert.IsFalse(scan.Findings.Any(f => f.Rule == 9));
    }

    [Test] public void RolsuzSahnedeNavMeshSorgulanmaz()
        => Assert.IsFalse(LDSceneScanner.Scan().Findings.Any(f => f.Rule == 10));

    [Test] public void Kural10_NavMeshYoksaHata()
    {
        Entrances();
        Assert.IsTrue(LDSceneScanner.Scan().Findings.Any(f => f.Rule == 10 && f.Severity == LDSeverity.Hata && f.Message.Contains("bake")));
    }
}
