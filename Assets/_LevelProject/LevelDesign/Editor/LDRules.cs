using System.Collections.Generic;
using UnityEngine;

/// <summary>Level design rules over role-tagged pieces. Pure geometry; no scene access.</summary>
public static class LDRules
{
    public const float Epsilon = 0.005f;
    public const float LowCoverMin = 0.9f;
    public const float LowCoverMax = 1.2f;
    public const float TallMin = 1.9f;
    public const float DoorWidthError = 1.2f;
    public const float DoorWidthWarn = 1.5f;
    public const float DoorHeightMin = 2.2f;
    public const float GapTouch = 0.05f;
    public const float GapMin = 1.2f;
    public const float ChainMin = 3f;
    public const float ChainMax = 6f;
    public const float EntranceMinDistance = 10f;
    public const float EyeHeight = 1.6f;
    public const float NavSampleRadius = 2f;

    /// <summary>Distance between two boxes seen from above; 0 when they touch or overlap.</summary>
    public static float HorizontalGap(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>Top of the highest floor under this box that the box still rises above; 0 when there is none.</summary>
    public static float FloorTopUnder(IReadOnlyList<LDPiece> pieces, Bounds b)
    {
        bool found = false;
        float top = 0f;
        foreach (LDPiece piece in pieces)
        {
            if (piece.Role != LDRoleType.Zemin || HorizontalGap(piece.Bounds, b) > 0f) continue;
            float floorTop = piece.Bounds.max.y;
            // A floor above the piece's top is an upper storey, not what it stands on.
            if (floorTop > b.max.y - Epsilon) continue;
            if (!found || floorTop > top) top = floorTop;
            found = true;
        }
        return top;
    }

    public static float HeightAboveFloor(IReadOnlyList<LDPiece> pieces, Bounds b) => b.max.y - FloorTopUnder(pieces, b);

    public static bool IsObstacle(LDRoleType r) => r == LDRoleType.Duvar || IsCover(r);
    public static bool IsCover(LDRoleType r) => r == LDRoleType.AlcakSiper || r == LDRoleType.YuksekSiper || r == LDRoleType.Blok;

    /// <summary>Each cover piece linked to its nearest other cover piece (edge to edge, seen from above).</summary>
    public static List<LDChainLink> ChainLinks(IReadOnlyList<LDPiece> pieces)
    {
        var links = new List<LDChainLink>();
        for (int i = 0; i < pieces.Count; i++)
        {
            if (!IsCover(pieces[i].Role)) continue;
            int nearest = -1;
            float best = float.MaxValue;
            for (int j = 0; j < pieces.Count; j++)
            {
                if (i == j || !IsCover(pieces[j].Role)) continue;
                float gap = HorizontalGap(pieces[i].Bounds, pieces[j].Bounds);
                if (gap < best) { best = gap; nearest = j; }
            }
            if (nearest >= 0) links.Add(new LDChainLink(pieces[i], pieces[nearest], best));
        }
        return links;
    }

    /// <summary>Rules 1–8 and 11. Rules 9–10 need physics and the NavMesh, so the scene scanner adds them.</summary>
    public static List<LDFinding> Evaluate(IReadOnlyList<LDPiece> pieces, IReadOnlyList<LDPiece> unassigned)
    {
        var findings = new List<LDFinding>();
        int players = 0, enemies = 0, covers = 0;

        foreach (LDPiece p in pieces)
        {
            Bounds b = p.Bounds;
            switch (p.Role)
            {
                case LDRoleType.AlcakSiper:
                {
                    float h = HeightAboveFloor(pieces, b);
                    if (h < LowCoverMin - Epsilon || h > LowCoverMax + Epsilon)
                        findings.Add(new LDFinding(LDSeverity.Hata, 1, $"Alçak siper {M(h)} m: {M(LowCoverMin)}–{M(LowCoverMax)} m olmalı ({p.Name})", b.center, p.Source));
                    break;
                }
                case LDRoleType.YuksekSiper:
                case LDRoleType.Blok:
                {
                    float h = HeightAboveFloor(pieces, b);
                    if (h < TallMin - Epsilon)
                        findings.Add(new LDFinding(LDSeverity.Hata, 2, $"{RoleLabel(p.Role)} {M(h)} m: en az {M(TallMin)} m olmalı ({p.Name})", b.center, p.Source));
                    break;
                }
                case LDRoleType.Kapi:
                {
                    float width = Mathf.Max(b.size.x, b.size.z);
                    if (width < DoorWidthError - Epsilon)
                        findings.Add(new LDFinding(LDSeverity.Hata, 3, $"Kapı {M(width)} m genişliğinde: en az {M(DoorWidthError)} m olmalı, önerilen {M(DoorWidthWarn)} m ({p.Name})", b.center, p.Source));
                    else if (width < DoorWidthWarn - Epsilon)
                        findings.Add(new LDFinding(LDSeverity.Uyari, 3, $"Kapı {M(width)} m genişliğinde: çatışma için {M(DoorWidthWarn)} m önerilir ({p.Name})", b.center, p.Source));
                    float clearance = b.min.y - FloorTopUnder(pieces, b);
                    if (clearance < DoorHeightMin - Epsilon)
                        findings.Add(new LDFinding(LDSeverity.Hata, 3, $"Kapı {M(clearance)} m yüksekliğinde: lentonun altı en az {M(DoorHeightMin)} m olmalı ({p.Name})", b.center, p.Source));
                    break;
                }
                case LDRoleType.OyuncuGirisi: players++; break;
                case LDRoleType.DusmanGirisi: enemies++; break;
            }
            if (IsCover(p.Role)) covers++;
        }

        for (int i = 0; i < pieces.Count; i++)
        for (int j = i + 1; j < pieces.Count; j++)
        {
            LDPiece a = pieces[i], c = pieces[j];
            float gap = HorizontalGap(a.Bounds, c.Bounds);
            Vector3 mid = (a.Bounds.center + c.Bounds.center) * 0.5f;
            if (IsObstacle(a.Role) && IsObstacle(c.Role) && gap > GapTouch && gap < GapMin - Epsilon)
                findings.Add(new LDFinding(LDSeverity.Uyari, 4, $"Dar geçit {M(gap)} m: {a.Name} ile {c.Name} arası en az {M(GapMin)} m olmalı ya da kapatılmalı", mid, a.Source));
            if (IsCover(a.Role) && IsCover(c.Role) && gap < ChainMin - Epsilon)
                findings.Add(new LDFinding(LDSeverity.Bilgi, 5, $"{a.Name} ile {c.Name} arası {M(gap)} m: bu kadar yakın siperler tek siper gibi davranır", mid, a.Source));
        }

        if (covers == 1)
        {
            LDPiece only = FirstCover(pieces);
            findings.Add(new LDFinding(LDSeverity.Uyari, 5, $"Tek siper var ({only.Name}): düşmanın siperden sipere ilerleyebilmesi için zincir gerekir", only.Bounds.center, only.Source));
        }
        foreach (LDChainLink link in ChainLinks(pieces))
            if (link.Distance > ChainMax + Epsilon)
                findings.Add(new LDFinding(LDSeverity.Uyari, 5, $"{link.From.Name} yalnız: en yakın siper {M(link.Distance)} m uzakta, en fazla {M(ChainMax)} m olmalı", link.From.Bounds.center, link.From.Source));

        if (players == 0)
            findings.Add(new LDFinding(LDSeverity.Hata, 6, "Oyuncu girişi yok: ScaleRef'e Oyuncu girişi rolü ver", Vector3.zero));
        if (enemies == 0)
            findings.Add(new LDFinding(LDSeverity.Hata, 6, "Düşman girişi yok: düşmanın geleceği yere boş bir nesne koyup Düşman girişi rolü ver", Vector3.zero));
        if (enemies == 1)
            findings.Add(new LDFinding(LDSeverity.Uyari, 7, "Tek düşman girişi var: flank için düşmanın ikinci bir yoldan gelebilmesi gerekir", Vector3.zero));

        foreach (LDPiece player in pieces)
        {
            if (player.Role != LDRoleType.OyuncuGirisi) continue;
            float nearest = float.MaxValue;
            LDPiece closest = default;
            foreach (LDPiece enemy in pieces)
            {
                if (enemy.Role != LDRoleType.DusmanGirisi) continue;
                Vector3 d = enemy.Bounds.center - player.Bounds.center;
                float dist = new Vector2(d.x, d.z).magnitude;
                if (dist < nearest) { nearest = dist; closest = enemy; }
            }
            if (nearest < EntranceMinDistance - Epsilon)
                findings.Add(new LDFinding(LDSeverity.Uyari, 8, $"{closest.Name} oyuncu girişine {M(nearest)} m: en az {M(EntranceMinDistance)} m olmalı", player.Bounds.center, player.Source));
        }

        foreach (LDPiece p in unassigned)
            findings.Add(new LDFinding(LDSeverity.Bilgi, 11, $"Rolü yok: {p.Name}", p.Bounds.center, p.Source));

        return findings;
    }

    public static string RoleLabel(LDRoleType r)
    {
        switch (r)
        {
            case LDRoleType.Zemin: return "Zemin";
            case LDRoleType.Duvar: return "Duvar";
            case LDRoleType.AlcakSiper: return "Alçak siper";
            case LDRoleType.YuksekSiper: return "Yüksek siper";
            case LDRoleType.Blok: return "Blok / kolon";
            case LDRoleType.Kapi: return "Kapı (lento)";
            case LDRoleType.OyuncuGirisi: return "Oyuncu girişi";
            default: return "Düşman girişi";
        }
    }

    /// <summary>Metres in Turkish notation: 0,75.</summary>
    public static string M(float metres) => metres.ToString("0.##", Turkish);

    private static readonly System.Globalization.CultureInfo Turkish = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    private static LDPiece FirstCover(IReadOnlyList<LDPiece> pieces)
    {
        foreach (LDPiece p in pieces)
            if (IsCover(p.Role)) return p;
        return default;
    }
}

public readonly struct LDChainLink
{
    public readonly LDPiece From;
    public readonly LDPiece To;
    public readonly float Distance;

    public LDChainLink(LDPiece from, LDPiece to, float distance)
    {
        From = from;
        To = to;
        Distance = distance;
    }
}
