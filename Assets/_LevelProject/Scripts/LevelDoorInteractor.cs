using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Camera-centred, line-of-sight door interaction for the level playtest player.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Level Design/Level Door Interactor")]
public sealed class LevelDoorInteractor : MonoBehaviour
{
    [Header("Interaction / Etkilesim")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;
    [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;

    private Camera viewCamera;
    private LevelDoor targetDoor;
    private bool showHud;
    private GUIStyle crosshairStyle;
    private GUIStyle promptStyle;

    public void Initialize(Camera cameraToUse)
    {
        viewCamera = cameraToUse;
        targetDoor = null;
        showHud = false;
    }

    /// <summary>Called after movement/look, with the same focus and cursor rules as the player.</summary>
    public void Tick(bool controlsActive)
    {
        targetDoor = null;
        showHud = controlsActive && Time.timeScale > 0f
            && viewCamera != null && viewCamera.isActiveAndEnabled;
        if (!showHud)
            return;

        // Only the nearest solid hit can be used: walls and other objects block interaction.
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance,
            interactionLayers, QueryTriggerInteraction.Ignore))
        {
            LevelDoor door = LevelDoor.FindForCollider(hit.collider);
            if (door != null && door.isActiveAndEnabled)
                targetDoor = door;
        }

        Keyboard keyboard = Keyboard.current;
        if (targetDoor != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            targetDoor.Toggle();
    }

    private void OnGUI()
    {
        if (!showHud || viewCamera == null || !viewCamera.isActiveAndEnabled)
            return;

        if (crosshairStyle == null)
        {
            crosshairStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            crosshairStyle.normal.textColor = Color.white;
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };
            promptStyle.normal.textColor = Color.white;
        }

        Rect viewport = viewCamera.pixelRect;
        float centerX = viewport.center.x;
        float centerY = Screen.height - viewport.center.y;
        GUI.Label(new Rect(centerX - 12f, centerY - 12f, 24f, 24f), "+", crosshairStyle);

        if (targetDoor != null && targetDoor.isActiveAndEnabled)
        {
            string prompt = targetDoor.IsOpen ? "[E] Kapat" : "[E] Aç";
            GUI.Box(new Rect(centerX - 90f, centerY + 35f, 180f, 34f), prompt, promptStyle);
        }
    }

    private void OnDisable()
    {
        targetDoor = null;
        showHud = false;
    }
}
