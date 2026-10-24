using UnityEngine;

/// <summary>A role-tagged piece as the rules see it: plain data, so rules run without a scene.</summary>
public readonly struct LDPiece
{
    public readonly string Name;
    public readonly LDRoleType Role;
    public readonly Bounds Bounds;
    public readonly Object Source;

    public LDPiece(string name, LDRoleType role, Bounds bounds, Object source = null)
    {
        Name = name;
        Role = role;
        Bounds = bounds;
        Source = source;
    }
}
