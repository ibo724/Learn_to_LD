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
}
