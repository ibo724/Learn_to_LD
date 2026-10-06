using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>First-person movement for checking level scale and collision.</summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class LevelPlaytestController : MonoBehaviour
{
    [Header("Movement / Hareket")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 6f;
    [SerializeField, Min(0f)] private float jumpHeight = 0.9f;
    [SerializeField] private float gravity = -22f;

    [Header("Camera / Kamera")]
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(40f, 100f)] private float fieldOfView = 70f;
    [SerializeField, Min(0.1f)] private float eyeHeight = 1.65f;

    private CharacterController character;
    private Camera playerCamera;
    private LevelDoorInteractor doorInteractor;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float verticalSpeed;
    private float pitch;
    private bool hasFocus = true;
    private bool skipMouseDelta;

    private void Awake()
    {
        ConfigureCharacter();
    }

    private void ConfigureCharacter()
    {
        character = GetComponent<CharacterController>();
        character.height = 1.8f;
        character.radius = 0.3f;
        character.center = new Vector3(0f, 0.9f, 0f);
        character.stepOffset = 0.3f;
        character.slopeLimit = 45f;
        character.skinWidth = 0.03f;
        character.minMoveDistance = 0f;
    }

    public void Initialize(Camera cameraToUse, Vector3 feetPosition, Quaternion facing)
    {
        ConfigureCharacter();
        playerCamera = cameraToUse;
        spawnPosition = feetPosition;
        spawnRotation = Quaternion.Euler(0f, facing.eulerAngles.y, 0f);

        playerCamera.transform.SetParent(transform, false);
        playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        playerCamera.transform.localRotation = Quaternion.identity;
        playerCamera.transform.localScale = Vector3.one;
        playerCamera.orthographic = false;
        playerCamera.fieldOfView = fieldOfView;
        playerCamera.nearClipPlane = 0.05f;
        playerCamera.enabled = true;

        doorInteractor = GetComponent<LevelDoorInteractor>();
        if (doorInteractor == null)
            doorInteractor = gameObject.AddComponent<LevelDoorInteractor>();
        doorInteractor.Initialize(playerCamera);

        ResetToSpawn();
        if (Application.isPlaying)
            CaptureCursor();
    }

    private void Update()
    {
        if (playerCamera == null)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            ReleaseCursor();

        bool controlsActive = hasFocus && Cursor.lockState == CursorLockMode.Locked;
        if (hasFocus && !controlsActive && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            CaptureCursor();
            controlsActive = true;
        }

        Vector2 movement = Vector2.zero;
        bool sprint = false;
        bool jump = false;

        if (controlsActive)
        {
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    ResetToSpawn();

                movement.x = (keyboard.dKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed ? 1f : 0f);
                movement.y = (keyboard.wKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed ? 1f : 0f);
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                jump = keyboard.spaceKey.wasPressedThisFrame;
            }

            if (mouse != null && !skipMouseDelta)
                ApplyLook(mouse.delta.ReadValue());
        }

        skipMouseDelta = false;
        LevelDoor.SyncMovedColliders();
        MoveCharacter(movement, sprint, jump, Mathf.Min(Time.deltaTime, 0.1f));
        if (doorInteractor != null && doorInteractor.isActiveAndEnabled)
            doorInteractor.Tick(controlsActive);
    }

    /// <summary>Mouse delta is already per frame, so it is not multiplied by deltaTime.</summary>
    public void ApplyLook(Vector2 mouseDelta)
    {
        transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f, Space.World);
        pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void MoveCharacter(Vector2 input, bool sprint, bool jump, float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (character.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (jump && character.isGrounded)
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

        input = Vector2.ClampMagnitude(input, 1f);
        float speed = sprint ? sprintSpeed : walkSpeed;
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;
        verticalSpeed += gravity * deltaTime;
        velocity.y = verticalSpeed;

        CollisionFlags collisions = character.Move(velocity * deltaTime);
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;
        if ((collisions & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (transform.position.y < spawnPosition.y - 15f)
            ResetToSpawn();
    }

    public void ResetToSpawn()
    {
        character.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        character.enabled = true;
        verticalSpeed = 0f;
        pitch = 0f;
        skipMouseDelta = true;
        playerCamera.transform.localRotation = Quaternion.identity;
    }

    private void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        skipMouseDelta = true;
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        skipMouseDelta = true;
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused)
            ReleaseCursor();
    }

    private void OnDisable()
    {
        if (doorInteractor != null)
            doorInteractor.Tick(false);
        if (Application.isPlaying)
            ReleaseCursor();
    }
}
