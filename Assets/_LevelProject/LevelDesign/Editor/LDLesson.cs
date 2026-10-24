using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One line of a lesson's build table: what to type into Position and Scale.</summary>
public sealed class LDTableRow
{
    public string Name;
    public LDRoleType Role;
    public Vector3 Position;
    public Vector3 Scale;
}

public sealed class LDStepResult
{
    public bool Passed => Problems.Count == 0;
    public readonly List<string> Problems = new List<string>();

    public LDStepResult Require(bool condition, string problem)
    {
        if (!condition) Problems.Add(problem);
        return this;
    }
}

public sealed class LDStep
{
    public string Title, What, Why;
    public LDTableRow[] Table = new LDTableRow[0];
    public (string Term, string Meaning)[] Terms = new (string, string)[0];
    public int[] Rules = new int[0];
    public Func<LDScanResult, LDStepResult> Check;
    public string[] ManualChecks = new string[0];
    public string ActionLabel;
    public Action Action;

    /// <summary>Passes when the automatic check (if any) passes and every manual box is ticked.</summary>
    public bool IsPassed(LDScanResult scan, Func<int, bool> manualChecked)
    {
        if (Check != null && !Check(scan).Passed) return false;
        for (int i = 0; i < ManualChecks.Length; i++)
            if (!manualChecked(i)) return false;
        return true;
    }
}

public sealed class LDLesson
{
    public string Id, Title;
    public LDStep[] Steps;
}
