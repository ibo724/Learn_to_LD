using UnityEditor;
using UnityEngine;

/// <summary>Draws roles, the 1 m cover line, cover chain distances, narrow gaps and sightlines from the last scan.</summary>
[InitializeOnLoad]
public static class LDSceneDrawer
{
    private static readonly Color Good = new Color(0.3f, 0.85f, 0.4f);
    private static readonly Color Warn = new Color(0.95f, 0.8f, 0.2f);
    private static readonly Color Bad = new Color(0.95f, 0.25f, 0.2f);

    static LDSceneDrawer()
    {
        SceneView.duringSceneGui += Draw;
        LDScanService.Changed += SceneView.RepaintAll;
    }

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(LDProgress.Key("overlay"), true);
        set
        {
            EditorPrefs.SetBool(LDProgress.Key("overlay"), value);
            SceneView.RepaintAll();
        }
    }

    public static Color RoleColor(LDRoleType r)
    {
        switch (r)
        {
            case LDRoleType.Zemin: return new Color(0.5f, 0.5f, 0.5f);
            case LDRoleType.Duvar: return new Color(0.35f, 0.35f, 0.35f);
            case LDRoleType.AlcakSiper: return new Color(0.95f, 0.75f, 0.2f);
            case LDRoleType.YuksekSiper: return new Color(0.95f, 0.5f, 0.15f);
            case LDRoleType.Blok: return new Color(0.45f, 0.55f, 0.7f);
            case LDRoleType.Kapi: return new Color(0.2f, 0.8f, 0.9f);
            case LDRoleType.OyuncuGirisi: return new Color(0.2f, 0.8f, 0.4f);
            default: return new Color(0.9f, 0.25f, 0.2f);
        }
    }

    private static void Draw(SceneView view)
    {
        if (!Enabled || Event.current.type != EventType.Repaint) return;
        LDScanResult scan = LDScanService.Last;

        foreach (LDPiece piece in scan.Pieces)
        {
            Bounds b = piece.Bounds;
            Handles.color = RoleColor(piece.Role);
            if (b.size == Vector3.zero) Handles.DrawWireDisc(b.center, Vector3.up, 0.5f, 2f);
            else Handles.DrawWireCube(b.center, b.size);
            Handles.Label(new Vector3(b.center.x, b.max.y + 0.2f, b.center.z), LDRules.RoleLabel(piece.Role));

            if (piece.Role == LDRoleType.AlcakSiper)
            {
                float y = LDRules.FloorTopUnder(scan.Pieces, b) + 1f;
                Handles.color = Good;
                bool alongX = b.size.x >= b.size.z;
                Vector3 a = alongX ? new Vector3(b.min.x, y, b.center.z) : new Vector3(b.center.x, y, b.min.z);
                Vector3 c = alongX ? new Vector3(b.max.x, y, b.center.z) : new Vector3(b.center.x, y, b.max.z);
                Handles.DrawLine(a, c, 3f);
            }
        }

        foreach (LDChainLink link in scan.Links)
        {
            Vector3 from = FacingPoint(link.From.Bounds, link.To.Bounds), to = FacingPoint(link.To.Bounds, link.From.Bounds);
            bool ok = link.Distance >= LDRules.ChainMin - LDRules.Epsilon && link.Distance <= LDRules.ChainMax + LDRules.Epsilon;
            Handles.color = ok ? Good : Warn;
            Handles.DrawLine(from, to, 2f);
            Handles.Label((from + to) * 0.5f, LDRules.M(link.Distance) + " m");
        }

        Handles.color = Bad;
        foreach (LDFinding f in scan.Findings)
            if (f.Rule == 4) Handles.DrawWireDisc(f.Position, Vector3.up, 0.6f, 4f);

        foreach (LDSightLine line in scan.SightLines)
        {
            Handles.color = line.Blocked ? Good : Warn;
            Handles.DrawDottedLine(line.From, line.To, 4f);
        }
    }

    /// <summary>The point on a's outline, just above its base, that faces b.</summary>
    private static Vector3 FacingPoint(Bounds a, Bounds b)
    {
        return new Vector3(Axis(a.min.x, a.max.x, b.min.x, b.max.x), a.min.y + 0.05f, Axis(a.min.z, a.max.z, b.min.z, b.max.z));
    }

    private static float Axis(float aMin, float aMax, float bMin, float bMax)
    {
        if (aMax < bMin) return aMax;
        if (bMax < aMin) return aMin;
        return (Mathf.Max(aMin, bMin) + Mathf.Min(aMax, bMax)) * 0.5f;
    }
}
