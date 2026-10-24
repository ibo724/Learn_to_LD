using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LDRoleAssignerTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Make(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }

    [TearDown] public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test] public void Assign_BilesenEklerVeRolVerir()
    {
        GameObject a = Make("a"), b = Make("b");
        Assert.AreEqual(2, LDRoleAssigner.Assign(new[] { a, b }, LDRoleType.AlcakSiper));
        Assert.AreEqual(LDRoleType.AlcakSiper, a.GetComponent<LDRole>().role);
        Assert.AreEqual(LDRoleType.AlcakSiper, b.GetComponent<LDRole>().role);
    }

    [Test] public void Assign_VarolanRoluGunceller()
    {
        GameObject a = Make("a");
        a.AddComponent<LDRole>().role = LDRoleType.Duvar;
        Assert.AreEqual(1, LDRoleAssigner.Assign(new[] { a }, LDRoleType.Blok));
        Assert.AreEqual(1, a.GetComponents<LDRole>().Length);
        Assert.AreEqual(LDRoleType.Blok, a.GetComponent<LDRole>().role);
    }

    [Test] public void Assign_BosDiziSifir() => Assert.AreEqual(0, LDRoleAssigner.Assign(new GameObject[0], LDRoleType.Zemin));
}
