using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Small reusable geometric impact effects. All geometry is created once per scene.</summary>
[DisallowMultipleComponent]
public sealed class VillaImpactEffects : MonoBehaviour
{
    private const int PoolSize = 32;
    private const int ChipsPerImpact = 3;
    private const float MarkLifetime = 5f;
    private const float ChipLifetime = .22f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static VillaImpactEffects current;

    private sealed class Slot
    {
        public MeshFilter filter;
        public MeshRenderer mark;
        public Transform attached;
        public Vector3 localPoint, localNormal;
        public readonly Transform[] chips = new Transform[ChipsPerImpact];
        public readonly MeshRenderer[] chipRenderers = new MeshRenderer[ChipsPerImpact];
        public readonly Vector3[] chipOrigins = new Vector3[ChipsPerImpact];
        public readonly Vector3[] chipVelocities = new Vector3[ChipsPerImpact];
        public readonly Quaternion[] chipRotations = new Quaternion[ChipsPerImpact];
        public float born, radius, twist;
        public bool active, chipsExpired;
    }

    private VillaMission owner;
    private Material material;
    private Mesh solidMarkMesh, glassMarkMesh, chipMesh, muzzleMesh;
    private readonly Slot[] slots = new Slot[PoolSize];
    private readonly MaterialPropertyBlock colors = new MaterialPropertyBlock();
    private int nextSlot;
    private uint variant = 0x7193u;

    public static void Prepare(VillaMission mission, Material unlitMaterial)
    {
        if (mission == null || unlitMaterial == null) return;
        if (current != null && current.owner == mission) return;
        GameObject root = new GameObject("Combat impact pool");
        root.transform.SetParent(mission.transform, false);
        current = root.AddComponent<VillaImpactEffects>();
        current.Initialize(mission, unlitMaterial);
    }

    private void Initialize(VillaMission mission, Material unlitMaterial)
    {
        owner = mission;
        material = unlitMaterial;
        solidMarkMesh = Disc(false);
        glassMarkMesh = Disc(true);
        chipMesh = Chip();
        muzzleMesh = Muzzle();
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = new Slot();
            slot.mark = Renderer("Impact mark " + i, solidMarkMesh, transform);
            slot.filter = slot.mark.GetComponent<MeshFilter>();
            for (int j = 0; j < ChipsPerImpact; j++)
            {
                slot.chipRenderers[j] = Renderer("Impact chip " + i + "/" + j, chipMesh, transform);
                slot.chips[j] = slot.chipRenderers[j].transform;
            }
            slots[i] = slot;
        }
    }

    public static MeshRenderer CreateMuzzleVisual(Transform muzzle, Material unlitMaterial, int layer, float scale = 1f)
    {
        if (current == null || muzzle == null || unlitMaterial == null || current.muzzleMesh == null) return null;
        MeshRenderer renderer = current.Renderer("Muzzle flare", current.muzzleMesh, muzzle);
        renderer.sharedMaterial = unlitMaterial;
        renderer.gameObject.layer = layer;
        renderer.transform.localScale = Vector3.one * scale;
        return renderer;
    }

    public static void Emit(RaycastHit hit)
    {
        if (current == null || current.owner == null || !current.owner.Playing || hit.collider == null
            || !VillaQueries.IsFinite(hit.point) || !VillaQueries.IsFinite(hit.normal)
            || hit.normal.sqrMagnitude < .001f || hit.collider.GetComponentInParent<VillaHealth>() != null) return;
        VillaBevelSurface surface = hit.collider.GetComponent<VillaBevelSurface>();
        if (surface != null)
        {
            if (!surface.TryProject(hit.point, hit.normal, out Vector3 point, out Vector3 normal)
                || !VillaQueries.IsFinite(point) || !VillaQueries.IsFinite(normal)
                || normal.sqrMagnitude < .001f) return;
            // Projection only changes this visual hit copy; ballistic collision stays unchanged.
            hit.point = point;
            hit.normal = normal;
        }
        current.Show(hit);
    }

    private void Show(RaycastHit hit)
    {
        Slot slot = slots[nextSlot];
        nextSlot = (nextSlot + 1) % slots.Length;
        bool isGlass = hit.collider.gameObject.layer == VillaQueries.GlassLayer;
        Vector3 normal = hit.normal.normalized;
        slot.attached = hit.collider.transform;
        slot.localPoint = slot.attached.InverseTransformPoint(hit.point);
        slot.localNormal = slot.attached.InverseTransformDirection(normal);
        slot.born = Time.time;
        slot.twist = Signed() * 180f;
        slot.radius = isGlass ? .035f : .019f;
        slot.active = true;
        slot.chipsExpired = false;
        slot.filter.sharedMesh = isGlass ? glassMarkMesh : solidMarkMesh;
        SetColor(slot.mark, isGlass ? new Color(.4f, .78f, .75f) : new Color(.13f, .20f, .22f));
        PoseMark(slot, 1f);
        slot.mark.enabled = true;

        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        if (tangent.sqrMagnitude < .001f) tangent = Vector3.right;
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
        for (int i = 0; i < ChipsPerImpact; i++)
        {
            slot.chipOrigins[i] = hit.point + normal * .012f;
            slot.chipVelocities[i] = normal * (.5f + .12f * i)
                + tangent * (Signed() * .6f) + bitangent * (Signed() * .6f);
            slot.chipRotations[i] = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 0, Signed() * 180f);
            slot.chips[i].position = slot.chipOrigins[i];
            slot.chips[i].rotation = slot.chipRotations[i];
            slot.chips[i].localScale = Vector3.one * .045f;
            SetColor(slot.chipRenderers[i], isGlass ? new Color(.63f, .88f, .84f) : new Color(.91f, .79f, .57f));
            slot.chipRenderers[i].enabled = true;
        }
    }

    private void LateUpdate()
    {
        bool visible = owner != null && owner.Playing;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (slot == null || !slot.active) continue;
            float age = Time.time - slot.born;
            if (age >= MarkLifetime)
            {
                slot.active = false;
                slot.mark.enabled = false;
                HideChips(slot);
                continue;
            }
            if (!visible)
            {
                slot.mark.enabled = false;
                slot.chipsExpired = true;
                HideChips(slot);
                continue;
            }
            slot.mark.enabled = slot.attached != null && slot.attached.gameObject.activeInHierarchy;
            if (slot.mark.enabled) PoseMark(slot, Mathf.Clamp01((MarkLifetime - age) / .5f));
            if (slot.chipsExpired || age >= ChipLifetime)
            {
                slot.chipsExpired = true;
                HideChips(slot);
                continue;
            }
            float size = .045f * (1f - age / ChipLifetime);
            for (int j = 0; j < ChipsPerImpact; j++)
            {
                slot.chips[j].position = slot.chipOrigins[j] + slot.chipVelocities[j] * age + Vector3.down * (2f * age * age);
                slot.chips[j].rotation = slot.chipRotations[j] * Quaternion.Euler(180f * age, 90f * age, 0);
                slot.chips[j].localScale = Vector3.one * size;
                slot.chipRenderers[j].enabled = true;
            }
        }
    }

    private static void HideChips(Slot slot)
    {
        for (int i = 0; i < ChipsPerImpact; i++) slot.chipRenderers[i].enabled = false;
    }

    private static void PoseMark(Slot slot, float shrink)
    {
        if (slot.attached == null) return;
        Vector3 normal = slot.attached.TransformDirection(slot.localNormal).normalized;
        // Follow the struck transform without inheriting its nonuniform glass/door scale.
        slot.mark.transform.position = slot.attached.TransformPoint(slot.localPoint) + normal * .004f;
        slot.mark.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 0, slot.twist);
        slot.mark.transform.localScale = Vector3.one * (slot.radius * shrink);
    }

    private MeshRenderer Renderer(string name, Mesh mesh, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;
        return renderer;
    }

    private void SetColor(MeshRenderer renderer, Color color)
    {
        colors.Clear();
        colors.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(colors);
    }

    private float Signed()
    {
        // Cosmetic variation has its own generator and never changes guard shot randomness.
        variant = unchecked(variant * 1664525u + 1013904223u);
        return ((variant >> 8) & 65535u) / 32767.5f - 1f;
    }

    private static Mesh Disc(bool ring)
    {
        int count = ring ? 16 : 9;
        Vector3[] vertices = new Vector3[count];
        int[] triangles = new int[ring ? 48 : 24];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            if (ring)
            {
                vertices[i] = point;
                vertices[i + 8] = point * .48f;
                int next = (i + 1) % 8;
                int start = i * 6;
                triangles[start] = i;
                triangles[start + 1] = next;
                triangles[start + 2] = i + 8;
                triangles[start + 3] = next;
                triangles[start + 4] = next + 8;
                triangles[start + 5] = i + 8;
            }
            else
            {
                vertices[i + 1] = point;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % 8 + 1;
            }
        }
        return Mesh(ring ? "Glass impact ring" : "Surface impact octagon", vertices, triangles);
    }

    private static Mesh Chip()
    {
        return Mesh("Impact chip", new[] { new Vector3(0, .6f, 0), new Vector3(-.4f, -.4f, 0),
            new Vector3(.4f, -.4f, 0), new Vector3(0, 0, .5f) },
            new[] { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 });
    }

    private static Mesh Muzzle()
    {
        Vector3[] vertices = new Vector3[18];
        int[] triangles = new int[48];
        for (int plane = 0; plane < 2; plane++)
        {
            int center = plane * 9;
            vertices[center] = new Vector3(0, 0, .06f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                float radius = i % 2 == 0 ? .08f : .025f;
                float x = Mathf.Cos(angle) * radius, y = Mathf.Sin(angle) * radius;
                vertices[center + i + 1] = plane == 0
                    ? new Vector3(x, y, .06f) : new Vector3(0, y, .06f + x * 1.5f);
                int start = (plane * 8 + i) * 3;
                triangles[start] = center;
                triangles[start + 1] = center + i + 1;
                triangles[start + 2] = center + (i + 1) % 8 + 1;
            }
        }
        return Mesh("Faceted muzzle flare", vertices, triangles);
    }

    private static Mesh Mesh(string name, Vector3[] vertices, int[] front)
    {
        int[] bothSides = new int[front.Length * 2];
        for (int i = 0; i < front.Length; i += 3)
        {
            bothSides[i] = front[i]; bothSides[i + 1] = front[i + 1]; bothSides[i + 2] = front[i + 2];
            int back = i + front.Length;
            bothSides[back] = front[i]; bothSides[back + 1] = front[i + 2]; bothSides[back + 2] = front[i + 1];
        }
        Mesh mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.triangles = bothSides;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        if (solidMarkMesh != null) Destroy(solidMarkMesh);
        if (glassMarkMesh != null) Destroy(glassMarkMesh);
        if (chipMesh != null) Destroy(chipMesh);
        if (muzzleMesh != null) Destroy(muzzleMesh);
    }
}
