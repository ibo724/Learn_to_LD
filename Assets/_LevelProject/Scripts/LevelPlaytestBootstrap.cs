using UnityEngine;

/// <summary>
/// Uses the scale-reference capsule as a spawn marker when Play starts.
/// All setup happens at runtime; the saved scene and its design stay intact.
/// </summary>
public static class LevelPlaytestBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePlayer()
    {
        if (Object.FindFirstObjectByType<LevelPlaytestController>() != null)
            return;

        GameObject reference = GameObject.Find("ScaleRef")
            ?? GameObject.Find("ScaleReference");
        if (reference == null)
            return;

        Vector3 feet = reference.transform.position;
        Collider referenceCollider = reference.GetComponent<Collider>();
        Renderer referenceRenderer = reference.GetComponent<Renderer>();
        if (referenceCollider != null)
            feet.y = referenceCollider.bounds.min.y;
        else if (referenceRenderer != null)
            feet.y = referenceRenderer.bounds.min.y;

        // The reference remains visible in edit mode, but cannot block its player in Play.
        foreach (Collider collider in reference.GetComponentsInChildren<Collider>())
            collider.enabled = false;
        foreach (Renderer renderer in reference.GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        Physics.SyncTransforms();
        // Probe just above the feet, below doorway lintels, to find the actual edited floor.
        if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down,
                out RaycastHit ground, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && ground.normal.y > 0.5f)
        {
            feet.y = ground.point.y;
        }
        feet.y += 0.05f;

        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
        {
            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraToUse = cameraObject.AddComponent<Camera>();
        }

        // Reusing the current camera preserves its URP renderer and Volume settings.
        AudioListener listener = cameraToUse.GetComponent<AudioListener>();
        if (listener == null)
            listener = cameraToUse.gameObject.AddComponent<AudioListener>();
        foreach (AudioListener otherListener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            otherListener.enabled = otherListener == listener;

        GameObject player = new GameObject("Player_Playtest");
        GameObject gameplay = GameObject.Find("Gameplay");
        if (gameplay != null)
            player.transform.SetParent(gameplay.transform, true);
        player.transform.localScale = Vector3.one;

        LevelPlaytestController controller = player.AddComponent<LevelPlaytestController>();
        controller.Initialize(cameraToUse, feet, reference.transform.rotation);
        Debug.Log("Level playtest ready: WASD move, mouse look, Shift sprint, Space jump, R reset, Esc release cursor.", player);
    }
}
