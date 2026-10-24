using UnityEditor;
using UnityEngine;

public static class LDRoleAssigner
{
    /// <summary>Gives every target an LDRole with this role, adding the component when missing. Undoable.</summary>
    public static int Assign(GameObject[] targets, LDRoleType role)
    {
        int count = 0;
        foreach (GameObject go in targets)
        {
            if (go == null) continue;
            LDRole marker = go.GetComponent<LDRole>();
            if (marker == null) marker = Undo.AddComponent<LDRole>(go);
            else Undo.RecordObject(marker, "LD rol ata");
            marker.role = role;
            EditorUtility.SetDirty(marker);
            count++;
        }
        return count;
    }
}
