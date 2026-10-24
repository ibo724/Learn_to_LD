using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LDMeasureTests
{
    private static LDPiece P(LDRoleType r, Vector3 center, Vector3 size) => new LDPiece(r.ToString(), r, new Bounds(center, size));

    [Test] public void Bosluk_DegenKutulardaSifir()
        => Assert.AreEqual(0f, LDRules.HorizontalGap(new Bounds(new Vector3(0, 1, 0), new Vector3(2, 2, 2)), new Bounds(new Vector3(2, 1, 0), new Vector3(2, 2, 2))), 1e-4f);

    [Test] public void Bosluk_KosegendeOklidMesafesi() // dx=3, dz=4
        => Assert.AreEqual(5f, LDRules.HorizontalGap(new Bounds(Vector3.zero, Vector3.one * 2), new Bounds(new Vector3(5, 0, 6), Vector3.one * 2)), 1e-4f);

    [Test] public void Bosluk_YukseklikFarkiniYoksayar()
        => Assert.AreEqual(1f, LDRules.HorizontalGap(new Bounds(Vector3.zero, Vector3.one), new Bounds(new Vector3(2, 10, 0), Vector3.one)), 1e-4f);

    [Test] public void Yukseklik_ZemineGomuluSiper() // zemin üstü y=0, siper -0.2..1.0
    {
        var list = new List<LDPiece> { P(LDRoleType.Zemin, new Vector3(0, -0.1f, 0), new Vector3(20, 0.2f, 20)) };
        Assert.AreEqual(1f, LDRules.HeightAboveFloor(list, new Bounds(new Vector3(0, 0.4f, 0), new Vector3(3, 1.2f, 0.5f))), 1e-4f);
    }

    [Test] public void Yukseklik_YuksekZemineGomuluSiper() // zemin üstü y=2, siper 1.8..3.0
    {
        var list = new List<LDPiece> { P(LDRoleType.Zemin, new Vector3(0, 1.9f, 0), new Vector3(20, 0.2f, 20)) };
        Assert.AreEqual(1f, LDRules.HeightAboveFloor(list, new Bounds(new Vector3(0, 2.4f, 0), new Vector3(3, 1.2f, 0.5f))), 1e-4f);
    }

    [Test] public void Yukseklik_ZeminYoksaSifirdanOlculur()
        => Assert.AreEqual(1f, LDRules.HeightAboveFloor(new List<LDPiece>(), new Bounds(new Vector3(0, 0.5f, 0), new Vector3(1, 1, 1))), 1e-4f);

    [Test] public void Yukseklik_UstPlatformaGoreDegilAltZemineGore()
    {
        var list = new List<LDPiece>
        {
            P(LDRoleType.Zemin, new Vector3(0, -0.1f, 0), new Vector3(20, 0.2f, 20)),
            P(LDRoleType.Zemin, new Vector3(0, 3.9f, 0), new Vector3(20, 0.2f, 20))
        };
        Assert.AreEqual(1f, LDRules.HeightAboveFloor(list, new Bounds(new Vector3(0, 0.5f, 0), new Vector3(3, 1, 0.5f))), 1e-4f);
    }
}
