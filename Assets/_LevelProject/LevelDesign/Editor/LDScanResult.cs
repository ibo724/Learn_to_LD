using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public readonly struct LDSightLine
{
    public readonly Vector3 From, To;
    public readonly bool Blocked;

    public LDSightLine(Vector3 from, Vector3 to, bool blocked)
    {
        From = from;
        To = to;
        Blocked = blocked;
    }
}

/// <summary>Everything one scan found; the lesson window and the scene drawer both read this.</summary>
public sealed class LDScanResult
{
    public string ScenePath = "";
    public List<LDPiece> Pieces = new List<LDPiece>();
    public List<LDFinding> Findings = new List<LDFinding>();
    public List<LDChainLink> Links = new List<LDChainLink>();
    public List<LDSightLine> SightLines = new List<LDSightLine>();

    public static readonly LDScanResult Empty = new LDScanResult();

    public static LDScanResult FromPieces(string scenePath, List<LDPiece> pieces, List<LDPiece> unassigned) => new LDScanResult
    {
        ScenePath = scenePath ?? "",
        Pieces = pieces,
        Findings = LDRules.Evaluate(pieces, unassigned),
        Links = LDRules.ChainLinks(pieces)
    };

    public int Count(LDRoleType r) => Pieces.Count(p => p.Role == r);
    public IEnumerable<LDPiece> OfRole(LDRoleType r) => Pieces.Where(p => p.Role == r);
    public IEnumerable<LDFinding> ForRules(int[] rules) => rules == null ? Enumerable.Empty<LDFinding>() : Findings.Where(f => rules.Contains(f.Rule));
    public bool HasError(params int[] rules) => ForRules(rules).Any(f => f.Severity == LDSeverity.Hata);
}
