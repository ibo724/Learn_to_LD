using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class Ders01ArenaTests
{
    private const string LessonScene = "Assets/_LevelProject/LevelDesign/Scenes/LD_Ders01_Arena.unity";

    private static List<LDPiece> TablePieces(System.Func<LDTableRow, Vector3> size = null)
        => Ders01Arena.Table.Select(r => new LDPiece(r.Name, r.Role, new Bounds(r.Position,
            size != null ? size(r) : r.Name == "ScaleRef" ? new Vector3(1, 2, 1) : r.Scale))).ToList();

    private static LDScanResult Scan(List<LDPiece> pieces, string scenePath = LessonScene)
        => LDScanResult.FromPieces(scenePath, pieces, new List<LDPiece>());

    [Test] public void Tablo_Kural1den8eHataVeUyariUretmez()
        => CollectionAssert.IsEmpty(Scan(TablePieces()).Findings.Where(f => f.Severity != LDSeverity.Bilgi).Select(f => f.Message));

    [Test] public void Tablo_Adim1den7yeKadarGecer()
    {
        LDLesson lesson = Ders01Arena.Create(null);
        LDScanResult scan = Scan(TablePieces());
        for (int i = 0; i <= 7; i++)
            Assert.IsTrue(lesson.Steps[i].IsPassed(scan, _ => true), lesson.Steps[i].Title);
    }

    [Test] public void Adim0_KaydedilmemisSahneGecmez()
    {
        LDStepResult result = Ders01Arena.Create(null).Steps[0].Check(Scan(TablePieces(), ""));
        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Problems.Any(p => p.Contains("Sahneyi kaydet")));
    }

    [Test] public void Adim0_VillaSahnesindeGecmez()
        => Assert.IsFalse(Ders01Arena.Create(null).Steps[0].IsPassed(Scan(TablePieces(), "Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity"), _ => true));

    [Test] public void Adim5_DortSiperleGecmez()
        => Assert.IsFalse(Ders01Arena.Create(null).Steps[5].IsPassed(Scan(TablePieces().Where(p => p.Name != "Siper_E").ToList()), _ => true));

    [Test] public void Adim2_AlcakDuvarlaGecmez()
    {
        // Scale.y 2 around the table's centre y=2 puts the top at 3 m, below the 3.5 m wall minimum.
        List<LDPiece> pieces = TablePieces(r => r.Name == "Duvar_B" ? new Vector3(r.Scale.x, 2, r.Scale.z) : r.Name == "ScaleRef" ? new Vector3(1, 2, 1) : r.Scale);
        Assert.IsFalse(Ders01Arena.Create(null).Steps[2].IsPassed(Scan(pieces), _ => true));
    }

    [Test] public void Adim8_ElleIsaretlenmedenGecmez()
    {
        LDStep step = Ders01Arena.Create(null).Steps[8];
        LDScanResult scan = Scan(TablePieces());
        Assert.IsFalse(step.IsPassed(scan, _ => false));
        Assert.IsTrue(step.IsPassed(scan, _ => true));
    }

    [Test] public void DersKaydi_Ders01Icerir() => Assert.AreEqual(Ders01Arena.Id, LDLessons.All[0].Id);
}
