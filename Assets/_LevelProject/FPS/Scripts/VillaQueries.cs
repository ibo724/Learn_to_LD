using UnityEngine;

public static class VillaQueries
{
    public const int PlayerLayer = 8, GuardLayer = 9, GlassLayer = 10, WeaponLayer = 11;
    private static readonly RaycastHit[] Hits = new RaycastHit[128];
    private static int synchronizedFrame = -1;
    private static bool transformsDirty = true;

    /// <summary>Synchronize native collider poses once before FPS movement and frame queries.</summary>
    public static void SyncTransforms()
    {
        // Trigger callbacks run against the physics step's current collider data. Do not let
        // an early FixedUpdate pickup query consume the later Update synchronization.
        if (Time.inFixedTimeStep) return;
        if (!transformsDirty && synchronizedFrame == Time.frameCount) return;
        Physics.SyncTransforms();
        synchronizedFrame = Time.frameCount;
        transformsDirty = false;
    }

    /// <summary>Call after a collider-bearing actor is warped or activated within the current frame.</summary>
    public static void InvalidateTransforms() => transformsDirty = true;

    /// <summary>Whether the nearest supporting surface is terrain rather than a placed floor.</summary>
    public static bool TerrainUnderfoot(Vector3 feet)
    {
        if (!IsFinite(feet)) return false;
        SyncTransforms();
        int mask = ~((1 << 2) | (1 << PlayerLayer) | (1 << GuardLayer) | (1 << WeaponLayer));
        return Physics.Raycast(feet + Vector3.up * .25f, Vector3.down, out RaycastHit hit,
            .6f, mask, QueryTriggerInteraction.Ignore) && hit.collider is TerrainCollider;
    }

    public static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    public static bool Cast(Vector3 origin, Vector3 direction, float distance,
        Transform ignore, out RaycastHit nearest, bool vision = false)
    {
        nearest = default;
        if (!IsFinite(origin) || !IsFinite(direction) || direction.sqrMagnitude < .000001f
            || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0f) return false;

        SyncTransforms();

        int mask = ~((1 << 2) | (1 << WeaponLayer));
        if (vision) mask &= ~(1 << GlassLayer);
        direction.Normalize();
        int count = Physics.RaycastNonAlloc(origin, direction, Hits, distance, mask, QueryTriggerInteraction.Ignore);
        RaycastHit[] candidates = Hits;
        // NonAlloc results are unordered. A full buffer may omit the nearest wall, so never
        // treat it as a complete set in a densely furnished room.
        if (count == Hits.Length)
        {
            candidates = Physics.RaycastAll(origin, direction, distance, mask, QueryTriggerInteraction.Ignore);
            count = candidates.Length;
        }

        float closest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider c = candidates[i].collider;
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy
                || (ignore != null && c.transform.IsChildOf(ignore))) continue;
            float candidateDistance = candidates[i].distance;
            if (candidateDistance > closest) continue;
            if (candidateDistance == closest && nearest.collider != null
                && c.GetInstanceID() >= nearest.collider.GetInstanceID()) continue;
            nearest = candidates[i]; closest = candidateDistance;
        }
        return closest < float.PositiveInfinity;
    }
}
