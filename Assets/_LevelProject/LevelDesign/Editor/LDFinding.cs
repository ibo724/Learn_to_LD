using UnityEngine;

public enum LDSeverity { Hata, Uyari, Bilgi }

/// <summary>One rule result, with where to look in the scene.</summary>
public readonly struct LDFinding
{
    public readonly LDSeverity Severity;
    public readonly int Rule;
    public readonly string Message;
    public readonly Vector3 Position;
    public readonly Object Target;

    public LDFinding(LDSeverity severity, int rule, string message, Vector3 position, Object target = null)
    {
        Severity = severity;
        Rule = rule;
        Message = message;
        Position = position;
        Target = target;
    }
}
