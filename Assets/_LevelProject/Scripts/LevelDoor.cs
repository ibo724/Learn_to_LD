using UnityEngine;

/// <summary>Moves a door's children by rotating a hinge or sliding a carriage.</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[AddComponentMenu("Level Design/Level Door")]
public sealed class LevelDoor : MonoBehaviour
{
    [Header("Door / Kapi")]
    [Tooltip("Rotation relative to the saved closed pose. Use -90 to open the other way.")]
    [SerializeField, Range(-180f, 180f)] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float angularSpeed = 180f;

    [Header("Sliding / Surgulu")]
    [SerializeField] private bool slidingDoor;
    [Tooltip("Open-position offset in the moving group's parent space.")]
    [SerializeField] private Vector3 slidingOffset = new Vector3(2.4f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float slidingSpeed = 1.8f;
    [Tooltip("Optional pair of carriages: they slide apart while this controller stays still.")]
    [SerializeField] private Transform slidingLeftLeaf = null;
    [SerializeField] private Transform slidingRightLeaf = null;

    private Quaternion closedLocalRotation;
    private Vector3 closedLocalPosition;
    private Vector3 closedLeftPosition;
    private Vector3 closedRightPosition;
    private Vector3 leftSlidingOffset;
    private Vector3 rightSlidingOffset;
    private float currentAngle;
    private static bool collidersNeedSync;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (slidingLeftLeaf != null || slidingRightLeaf != null)
        {
            if (slidingLeftLeaf != null && slidingRightLeaf != null
                && LevelGlassDoorLayout.Prepare(slidingLeftLeaf, out Vector3 leftAxis, out float leftWidth)
                && LevelGlassDoorLayout.Prepare(slidingRightLeaf, out Vector3 rightAxis, out float rightWidth))
            {
                slidingDoor = true;
                closedLeftPosition = slidingLeftLeaf.localPosition;
                closedRightPosition = slidingRightLeaf.localPosition;
                leftSlidingOffset = slidingLeftLeaf.localRotation * leftAxis * -(leftWidth + 0.1f);
                rightSlidingOffset = slidingRightLeaf.localRotation * rightAxis * (rightWidth + 0.1f);
                collidersNeedSync = true;
            }
            else
            {
                // An incomplete pair must not translate the whole casing.
                enabled = false;
                Debug.LogWarning("Sliding door needs a glass pane and four frame bars in each carriage.", this);
            }
        }

        // Named glass-door frames opt in to sliding; ordinary doors keep their hinge.
        if (LevelGlassDoorLayout.Prepare(transform, out Vector3 slideAxis, out float leafWidth))
        {
            slidingDoor = true;
            float direction = Vector3.Dot(slidingOffset, slideAxis) < 0f ? -1f : 1f;
            slidingOffset = slideAxis * ((leafWidth + 0.1f) * direction);
            collidersNeedSync = true;
        }

        // Preserve the saved closed pose, including rotated door instances.
        closedLocalRotation = transform.localRotation;
        closedLocalPosition = transform.localPosition;
    }

    public void Toggle()
    {
        if (isActiveAndEnabled)
            IsOpen = !IsOpen;
    }

    private void Update()
    {
        if (slidingDoor && slidingLeftLeaf != null && slidingRightLeaf != null)
        {
            bool leftMoved = SlideLeaf(slidingLeftLeaf, closedLeftPosition, leftSlidingOffset);
            bool rightMoved = SlideLeaf(slidingRightLeaf, closedRightPosition, rightSlidingOffset);
            collidersNeedSync |= leftMoved || rightMoved;
            return;
        }

        if (slidingDoor)
        {
            Vector3 targetPosition = closedLocalPosition
                + (IsOpen ? slidingOffset : Vector3.zero);
            Vector3 nextPosition = Vector3.MoveTowards(transform.localPosition,
                targetPosition, slidingSpeed * Time.deltaTime);
            if (nextPosition == transform.localPosition)
                return;

            transform.localPosition = nextPosition;
            collidersNeedSync = true;
            return;
        }

        float targetAngle = IsOpen ? openAngle : 0f;
        float nextAngle = Mathf.MoveTowards(currentAngle, targetAngle,
            angularSpeed * Time.deltaTime);
        if (nextAngle == currentAngle)
            return;

        currentAngle = nextAngle;
        transform.localRotation = closedLocalRotation
            * Quaternion.AngleAxis(currentAngle, Vector3.up);
        collidersNeedSync = true;
    }

    private bool SlideLeaf(Transform leaf, Vector3 closedPosition, Vector3 offset)
    {
        Vector3 target = closedPosition + (IsOpen ? offset : Vector3.zero);
        Vector3 next = Vector3.MoveTowards(leaf.localPosition, target, slidingSpeed * Time.deltaTime);
        if (next == leaf.localPosition)
            return false;

        leaf.localPosition = next;
        return true;
    }

    /// <summary>The fixed casing can close a sliding panel after it leaves the opening.</summary>
    public static LevelDoor FindForCollider(Collider collider)
    {
        if (collider == null)
            return null;

        LevelDoor movingDoor = collider.GetComponentInParent<LevelDoor>();
        if (movingDoor != null)
            return movingDoor;

        Transform doorRoot = collider.transform.parent;
        if (doorRoot == null)
            return null;

        Transform carriage = doorRoot.Find("DoorHinge") ?? doorRoot.Find("Door_Hinge");
        LevelDoor sliding = carriage != null ? carriage.GetComponent<LevelDoor>() : null;
        return sliding != null && sliding.slidingDoor ? sliding : null;
    }

    /// <summary>Sync moving leaves once before the player's movement and raycast queries.</summary>
    public static void SyncMovedColliders()
    {
        if (!collidersNeedSync)
            return;

        Physics.SyncTransforms();
        collidersNeedSync = false;
    }

    /// <summary>Configure the user's named hinges in Play without editing scene/prefab assets.</summary>
    public static void PrepareSceneDoors()
    {
        foreach (Transform candidate in Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.name != "DoorHinge" && candidate.name != "Door_Hinge")
                continue;

            // Only opt in to the door hierarchy used in the lesson.
            if (candidate.Find("Door_Leaf") == null)
                continue;

            if (candidate.GetComponent<LevelDoor>() == null)
                candidate.gameObject.AddComponent<LevelDoor>();
        }
    }
}
