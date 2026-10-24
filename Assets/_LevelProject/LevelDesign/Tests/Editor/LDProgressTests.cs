using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LDProgressTests
{
    private string id;

    [SetUp] public void SetUp() => id = "test-" + Guid.NewGuid();

    [TearDown] public void TearDown()
    {
        EditorPrefs.DeleteKey(LDProgress.Key(id + ".step"));
        for (int step = 0; step < 10; step++)
        for (int i = 0; i < 5; i++)
            EditorPrefs.DeleteKey(LDProgress.Key($"{id}.{step}.manual.{i}"));
    }

    [Test] public void Adim_KaydedilirVeOkunur()
    {
        LDProgress.SetStep(id, 4);
        Assert.AreEqual(4, LDProgress.GetStep(id));
    }

    [Test] public void Adim_VarsayilanSifir() => Assert.AreEqual(0, LDProgress.GetStep(id));

    [Test] public void ElleKontrol_AdimVeSiraylaAyrilir()
    {
        LDProgress.SetManual(id, 8, 1, true);
        Assert.IsTrue(LDProgress.GetManual(id, 8, 1));
        Assert.IsFalse(LDProgress.GetManual(id, 8, 0));
        Assert.IsFalse(LDProgress.GetManual(id, 7, 1));
    }

    [Test] public void Anahtar_ProjeyeOzgu()
        => StringAssert.StartsWith("LD." + Hash128.Compute(Application.dataPath), LDProgress.Key("x"));
}
