using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class LDRulesTests
{
    private static readonly List<LDPiece> None = new List<LDPiece>();

    private static LDPiece P(LDRoleType r, Vector3 center, Vector3 size) => new LDPiece(r.ToString(), r, new Bounds(center, size));
    private static LDPiece Zemin() => P(LDRoleType.Zemin, new Vector3(0, -0.1f, 0), new Vector3(40, 0.2f, 40));
    private static LDPiece Siper(float x, float z, float h) => P(LDRoleType.AlcakSiper, new Vector3(x, h / 2f, z), new Vector3(3, h, 0.5f));
    private static LDPiece Kapi(float width, float bottom) => P(LDRoleType.Kapi, new Vector3(0, bottom + 0.75f, 0), new Vector3(width, 1.5f, 0.5f));
    private static LDPiece Oyuncu() => P(LDRoleType.OyuncuGirisi, new Vector3(0, 1, 0), new Vector3(1, 2, 1));
    private static LDPiece Dusman(float x) => P(LDRoleType.DusmanGirisi, new Vector3(x, 0, 0), Vector3.zero);
    private static List<LDPiece> L(params LDPiece[] pieces) => pieces.ToList();
    private static int Count(List<LDFinding> list, int rule, LDSeverity sev) => list.Count(f => f.Rule == rule && f.Severity == sev);
    private static bool Has(List<LDFinding> list, int rule, LDSeverity sev) => Count(list, rule, sev) > 0;

    [TestCase(0.9f, true)] [TestCase(1.2f, true)] [TestCase(0.89f, false)] [TestCase(1.21f, false)]
    public void Kural1_SinirDegerleriGecer(float h, bool ok)
        => Assert.AreEqual(!ok, Has(LDRules.Evaluate(L(Zemin(), Siper(0, 0, h)), None), 1, LDSeverity.Hata));

    [TestCase(1.89f, true)] [TestCase(1.9f, false)]
    public void Kural2_YuksekSiper(float h, bool hata)
        => Assert.AreEqual(hata, Has(LDRules.Evaluate(L(Zemin(), P(LDRoleType.YuksekSiper, new Vector3(0, h / 2f, 0), new Vector3(1, h, 1))), None), 2, LDSeverity.Hata));

    [TestCase(1.19f, LDSeverity.Hata)] [TestCase(1.2f, LDSeverity.Uyari)] [TestCase(1.49f, LDSeverity.Uyari)]
    public void Kural3_KapiGenisligi(float w, LDSeverity sev)
        => Assert.AreEqual(1, Count(LDRules.Evaluate(L(Zemin(), Kapi(w, 2.5f)), None), 3, sev));

    [Test] public void Kural3_KapiGenisligi15Temiz()
        => Assert.IsFalse(LDRules.Evaluate(L(Zemin(), Kapi(1.5f, 2.5f)), None).Any(f => f.Rule == 3));

    [Test] public void Kural3_AlcakLentoHata()
        => Assert.AreEqual(1, Count(LDRules.Evaluate(L(Zemin(), Kapi(2f, 2.1f)), None), 3, LDSeverity.Hata));

    [TestCase(0.04f, 0)] [TestCase(1.19f, 1)] [TestCase(1.2f, 0)]
    public void Kural4_DarGecit(float gap, int beklenen)
    {
        var walls = L(P(LDRoleType.Duvar, new Vector3(0, 2, 0), new Vector3(1, 4, 1)), P(LDRoleType.Duvar, new Vector3(1 + gap, 2, 0), new Vector3(1, 4, 1)));
        Assert.AreEqual(beklenen, Count(LDRules.Evaluate(walls, None), 4, LDSeverity.Uyari));
    }

    [Test] public void Kural4_KapiVeGirisEngelSayilmaz()
    {
        var pieces = L(P(LDRoleType.Duvar, new Vector3(0, 2, 0), new Vector3(1, 4, 1)),
                       P(LDRoleType.Kapi, new Vector3(2, 3.25f, 0), new Vector3(2, 1.5f, 0.5f)),
                       P(LDRoleType.DusmanGirisi, new Vector3(1, 0, 0), Vector3.zero));
        Assert.IsFalse(LDRules.Evaluate(pieces, None).Any(f => f.Rule == 4));
    }

    [Test] public void Kural5_YalnizSiper()
        => Assert.AreEqual(2, Count(LDRules.Evaluate(L(Zemin(), Siper(0, 0, 1), Siper(9.01f, 0, 1)), None), 5, LDSeverity.Uyari));

    [Test] public void Kural5_YakinIkiliTekBilgi()
        => Assert.AreEqual(1, Count(LDRules.Evaluate(L(Zemin(), Siper(0, 0, 1), Siper(5, 0, 1)), None), 5, LDSeverity.Bilgi));

    [Test] public void Kural5_TekSiperUyari()
        => Assert.AreEqual(1, Count(LDRules.Evaluate(L(Zemin(), Siper(0, 0, 1)), None), 5, LDSeverity.Uyari));

    [Test] public void Kural6_GirisYoksaHata()
        => Assert.AreEqual(2, Count(LDRules.Evaluate(L(Zemin()), None), 6, LDSeverity.Hata));

    [Test] public void Kural7_TekDusmanGirisi()
    {
        Assert.AreEqual(1, Count(LDRules.Evaluate(L(Oyuncu(), Dusman(20)), None), 7, LDSeverity.Uyari));
        Assert.AreEqual(0, Count(LDRules.Evaluate(L(Oyuncu(), Dusman(20), Dusman(-20)), None), 7, LDSeverity.Uyari));
    }

    [Test] public void Kural8_NoktaIsaretleriyleMesafe()
    {
        Assert.AreEqual(1, Count(LDRules.Evaluate(L(Oyuncu(), Dusman(9.99f)), None), 8, LDSeverity.Uyari));
        Assert.AreEqual(0, Count(LDRules.Evaluate(L(Oyuncu(), Dusman(10f)), None), 8, LDSeverity.Uyari));
    }

    [Test] public void Kural11_RolsuzParca()
    {
        var unassigned = L(P(LDRoleType.Zemin, Vector3.zero, Vector3.one), P(LDRoleType.Zemin, Vector3.one * 5, Vector3.one));
        Assert.AreEqual(2, Count(LDRules.Evaluate(None, unassigned), 11, LDSeverity.Bilgi));
    }

    [Test] public void ZincirBaglantilari_EnYakiniSecer()
    {
        LDChainLink link = LDRules.ChainLinks(L(Siper(0, 0, 1), Siper(5, 0, 1), Siper(20, 0, 1))).Single(l => l.From.Bounds.center.x == 20f);
        Assert.AreEqual(5f, link.To.Bounds.center.x, 1e-4f);
        Assert.AreEqual(12f, link.Distance, 1e-4f);
    }
}
