using UnityEngine;

/// <summary>Fits the lesson's glass-door parts at runtime, without changing saved assets.</summary>
internal static class LevelGlassDoorLayout
{
    private const float FrameWidth = 0.05f;
    private const float FrameDepth = 0.04f;
    private const float JointCollisionPadding = 0.012f;

    public static bool Prepare(Transform hinge, out Vector3 slideAxis, out float leafWidth)
    {
        slideAxis = Vector3.right;
        leafWidth = 0f;
        Transform leaf = hinge.Find("Door_Leaf");
        Transform left = hinge.Find("Leaf_Frame_Left");
        Transform right = hinge.Find("Leaf_Frame_Right");
        Transform top = hinge.Find("Leaf_Frame_Top");
        Transform bottom = hinge.Find("Leaf_Frame_Bottom");

        // Ordinary doors have no named moving frame, so their setup is preserved.
        if (!TryGetBounds(leaf, out Bounds glassBounds)
            || !TryGetBounds(left, out Bounds leftBounds)
            || !TryGetBounds(right, out Bounds rightBounds)
            || !TryGetBounds(top, out Bounds topBounds)
            || !TryGetBounds(bottom, out Bounds bottomBounds))
            return false;

        Vector3 scale = leaf.localScale;
        Vector3 size = Vector3.Scale(glassBounds.size,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        if (size.x <= 0.001f || size.y <= 0.001f)
            return false;

        // Cover the narrow centre joint so looking straight at it still hits a closed pane.
        BoxCollider glassCollider = leaf.GetComponent<BoxCollider>();
        if (glassCollider != null)
        {
            glassCollider.center = glassBounds.center;
            Vector3 colliderSize = glassBounds.size;
            colliderSize.x += JointCollisionPadding / Mathf.Abs(scale.x);
            glassCollider.size = colliderSize;
        }

        Quaternion rotation = leaf.localRotation;
        Vector3 center = leaf.localPosition
            + rotation * Vector3.Scale(glassBounds.center, scale);
        float width = Mathf.Min(FrameWidth, Mathf.Min(size.x, size.y) * 0.25f);
        float depth = Mathf.Max(FrameDepth, size.z);

        // Keep the user's left/right naming when viewing the door from either side.
        float leftSide = (Quaternion.Inverse(rotation) * (left.localPosition - center)).x
            < 0f ? -1f : 1f;
        Vector3 uprightSize = new Vector3(width, size.y, depth);
        Vector3 railSize = new Vector3(size.x - 2f * width, width, depth);
        float uprightX = (size.x - width) * 0.5f;
        float railY = (size.y - width) * 0.5f;

        // All bars sit inside the existing leaf rectangle, avoiding the fixed casing.
        FitBar(left, leftBounds, center, rotation,
            new Vector3(leftSide * uprightX, 0f, 0f), uprightSize);
        FitBar(right, rightBounds, center, rotation,
            new Vector3(-leftSide * uprightX, 0f, 0f), uprightSize);
        FitBar(top, topBounds, center, rotation,
            new Vector3(0f, railY, 0f), railSize);
        FitBar(bottom, bottomBounds, center, rotation,
            new Vector3(0f, -railY, 0f), railSize);

        // Translation follows the panel's width axis, without relocating a hinge or rotating it.
        slideAxis = rotation * Vector3.right;
        leafWidth = size.x;
        return true;
    }

    private static bool TryGetBounds(Transform part, out Bounds bounds)
    {
        bounds = default;
        if (part == null)
            return false;

        MeshFilter filter = part.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return false;

        bounds = filter.sharedMesh.bounds;
        Vector3 size = bounds.size;
        return size.x > 0.00001f && size.y > 0.00001f && size.z > 0.00001f;
    }

    private static void FitBar(Transform bar, Bounds meshBounds, Vector3 center,
        Quaternion rotation, Vector3 offset, Vector3 size)
    {
        Vector3 meshSize = meshBounds.size;
        bar.localRotation = rotation;
        bar.localScale = new Vector3(size.x / meshSize.x,
            size.y / meshSize.y, size.z / meshSize.z);
        bar.localPosition = center + rotation * offset
            - rotation * Vector3.Scale(meshBounds.center, bar.localScale);

        // Edit Collider changes should not leave invisible blocks beside the glass.
        BoxCollider collider = bar.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.center = meshBounds.center;
            collider.size = meshBounds.size;
        }
    }
}
