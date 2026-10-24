using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

/// <summary>Collects role-tagged pieces from the active scene and adds the rules that need physics and the NavMesh.</summary>
public static class LDSceneScanner
{
    public static LDScanResult Scan()
    {
        Scene scene = SceneManager.GetActiveScene();
        var pieces = new List<LDPiece>();
        foreach (LDRole marker in Object.FindObjectsByType<LDRole>(FindObjectsSortMode.None))
        {
            if (!marker.gameObject.activeInHierarchy || marker.gameObject.scene != scene) continue;
            pieces.Add(new LDPiece(marker.name, marker.role, MeasureBounds(marker), marker.gameObject));
        }
        pieces.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        var unassigned = new List<LDPiece>();
        foreach (ProBuilderMesh mesh in Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None))
        {
            if (!mesh.gameObject.activeInHierarchy || mesh.gameObject.scene != scene) continue;
            if (mesh.GetComponentInParent<LDRole>(true) != null) continue;
            Renderer r = mesh.GetComponent<Renderer>();
            unassigned.Add(new LDPiece(mesh.name, LDRoleType.Zemin, r != null ? r.bounds : new Bounds(mesh.transform.position, Vector3.zero), mesh.gameObject));
        }

        LDScanResult result = LDScanResult.FromPieces(scene.path, pieces, unassigned);
        // Scenes that don't use the tool (the Villa scenes) skip the costly physics and NavMesh queries.
        if (pieces.Count == 0) return result;
        AddSightLines(result);
        AddNavigation(result);
        return result;
    }

    /// <summary>Renderers of the piece and its children (stopping at children with their own role), else colliders, else a point.</summary>
    private static Bounds MeasureBounds(LDRole marker)
    {
        bool found = false;
        Bounds total = default;
        foreach (Renderer r in marker.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r.GetComponentInParent<LDRole>() != marker) continue;
            if (found) total.Encapsulate(r.bounds); else total = r.bounds;
            found = true;
        }
        if (found) return total;
        foreach (Collider c in marker.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled || c.GetComponentInParent<LDRole>() != marker) continue;
            if (found) total.Encapsulate(c.bounds); else total = c.bounds;
            found = true;
        }
        return found ? total : new Bounds(marker.transform.position, Vector3.zero);
    }

    // Rule 9: from the player's eye at the entrance, can the enemy entrance be seen directly?
    private static void AddSightLines(LDScanResult result)
    {
        Physics.SyncTransforms();
        foreach (LDPiece player in result.OfRole(LDRoleType.OyuncuGirisi))
        foreach (LDPiece enemy in result.OfRole(LDRoleType.DusmanGirisi))
        {
            Vector3 from = new Vector3(player.Bounds.center.x, player.Bounds.min.y + LDRules.EyeHeight, player.Bounds.center.z);
            Vector3 to = new Vector3(enemy.Bounds.center.x, LDRules.FloorTopUnder(result.Pieces, enemy.Bounds) + LDRules.EyeHeight, enemy.Bounds.center.z);
            bool blocked = IsBlocked(from, to, player.Source as GameObject, enemy.Source as GameObject);
            result.SightLines.Add(new LDSightLine(from, to, blocked));
            if (!blocked)
                result.Findings.Add(new LDFinding(LDSeverity.Uyari, 9, $"Oyuncu girişinden düşman girişi doğrudan görünüyor ({enemy.Name}): arayı bir blok ya da duvarla kes", (from + to) * 0.5f, enemy.Source));
        }
    }

    private static bool IsBlocked(Vector3 from, Vector3 to, GameObject self, GameObject target)
    {
        Vector3 dir = to - from;
        RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dir.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        return hits.Any(h => !BelongsTo(h.collider, self) && !BelongsTo(h.collider, target));
    }

    private static bool BelongsTo(Collider c, GameObject root) => root != null && c.transform.IsChildOf(root.transform);

    // Rule 10: the NavMesh exists and every enemy entrance can walk to the player entrance.
    private static void AddNavigation(LDScanResult result)
    {
        if (NavMesh.CalculateTriangulation().vertices.Length == 0)
        {
            result.Findings.Add(new LDFinding(LDSeverity.Hata, 10, "NavMesh bake edilmemiş: NavMesh Surface ekleyip Bake'e bas", Vector3.zero));
            return;
        }
        foreach (LDPiece player in result.OfRole(LDRoleType.OyuncuGirisi))
        {
            if (!NavMesh.SamplePosition(player.Bounds.center, out NavMeshHit start, LDRules.NavSampleRadius, NavMesh.AllAreas))
            {
                result.Findings.Add(new LDFinding(LDSeverity.Hata, 10, $"{player.Name} NavMesh üstünde değil", player.Bounds.center, player.Source));
                continue;
            }
            foreach (LDPiece enemy in result.OfRole(LDRoleType.DusmanGirisi))
            {
                if (!NavMesh.SamplePosition(enemy.Bounds.center, out NavMeshHit end, LDRules.NavSampleRadius, NavMesh.AllAreas))
                {
                    result.Findings.Add(new LDFinding(LDSeverity.Hata, 10, $"{enemy.Name} NavMesh üstünde değil", enemy.Bounds.center, enemy.Source));
                    continue;
                }
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(end.position, start.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    result.Findings.Add(new LDFinding(LDSeverity.Hata, 10, $"{enemy.Name} oyuncu girişine yürüyemiyor", enemy.Bounds.center, enemy.Source));
            }
        }
    }
}
