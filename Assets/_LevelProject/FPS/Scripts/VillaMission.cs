using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-50)]
public sealed class VillaMission : MonoBehaviour
{
    public enum Phase { Briefing, Playing, Paused, Won, Lost }
    public static VillaMission Instance { get; private set; }
    private static bool restartImmediately;
    private static bool hasSessionSettings;
    private static float sessionSensitivity = .1f, sessionVolume = .7f;
    private static bool sessionShake = true;
    private const string PreferencePrefix = "VillaFPS.v1.";
    public Camera sceneCamera;
    public UniversalRenderPipelineAsset renderPipeline;
    public Transform spawn, objective, extraction;
    public GameObject weaponPrefab;
    public VillaGuard[] guards;
    public VillaGuard reserveGuard;
    public AudioClip[] cues;
    public Color accent = new Color(.35f, .8f, .7f);
    public Phase State { get; private set; }
    public bool Playing => State == Phase.Playing;
    public bool HasData { get; private set; }
    public LevelPlaytestController Player { get; private set; }
    public VillaHealth Health { get; private set; }
    public VillaWeapon Weapon { get; private set; }
    public int Shots { get; set; }
    public int Hits { get; set; }
    public int Kills { get; private set; }
    public float Elapsed { get; private set; }
    public string Notice { get; private set; }
    public float NoticeUntil { get; private set; }
    public float HitUntil { get; private set; }
    public float HurtUntil { get; private set; }
    public Vector3 HurtSource { get; private set; }
    public string Interaction { get; private set; }
    public float Sensitivity = .1f, Volume = .7f;
    public bool CameraShake = true;
    private AudioSource speaker;
    private LevelDoor targetDoor;
    private bool targetData, focused = true;
    private float suppressFireUntil, nextStep;
    private VillaHUD hud;
    private readonly VillaGuard[] shootingGuards = new VillaGuard[2];
    private readonly float[] shootingLeaseUntil = new float[2];
    private RenderPipelineAsset previousPipeline;
    private float previousListenerVolume;
    private bool runtimeStarted;

    private void Awake()
    {
        if (!Application.isPlaying) return;
        runtimeStarted=true;
        Instance = this;
        previousPipeline = QualitySettings.renderPipeline;
        previousListenerVolume = AudioListener.volume;
        if (renderPipeline != null) QualitySettings.renderPipeline = renderPipeline;
    }
    private void Start()
    {
        LevelDoor.PrepareSceneDoors();
        LoadSettings();
        if (spawn == null || sceneCamera == null || weaponPrefab == null || objective == null || extraction == null)
        { Debug.LogError("Villa mission is missing scene references. Rebuild with Tools > Villa FPS > Prepare game scene.", this); enabled = false; return; }
        GameObject player = new GameObject("Player FPS");
        player.layer = VillaQueries.PlayerLayer;
        Player = player.AddComponent<LevelPlaytestController>();
        Player.ExternalControl = true;
        Player.Initialize(sceneCamera, spawn.position, spawn.rotation);
        player.GetComponent<LevelDoorInteractor>().enabled = false;
        Health = player.AddComponent<VillaHealth>();
        Health.Damaged += OnDamaged;
        Health.Died += Lose;
        speaker = player.AddComponent<AudioSource>(); speaker.spatialBlend = 0f;
        AudioListener.volume = Volume;
        Camera gunCamera = new GameObject("Weapon camera").AddComponent<Camera>();
        gunCamera.transform.SetParent(sceneCamera.transform, false);
        gunCamera.clearFlags = CameraClearFlags.Depth; gunCamera.nearClipPlane = .01f; gunCamera.farClipPlane = 3f;
        gunCamera.fieldOfView = 60f; gunCamera.cullingMask = 1 << VillaQueries.WeaponLayer;
        sceneCamera.cullingMask &= ~(1 << VillaQueries.WeaponLayer);
        UniversalAdditionalCameraData mainData = sceneCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData weaponData = gunCamera.GetUniversalAdditionalCameraData();
        weaponData.renderType = CameraRenderType.Overlay;
        weaponData.renderShadows = false;
        mainData.renderType = CameraRenderType.Base;
        mainData.SetRenderer(0);
        StartCoroutine(AttachWeaponCamera(mainData, gunCamera));
        GameObject gun = Instantiate(weaponPrefab, gunCamera.transform, false);
        Weapon = gun.GetComponent<VillaWeapon>(); Weapon.Initialize(this, sceneCamera);
        hud = gameObject.AddComponent<VillaHUD>(); hud.mission = this;
        ChangePhase(Phase.Briefing);
        if (restartImmediately) { restartImmediately = false; Begin(); }
    }
    public void Begin()
    {
        if (State != Phase.Briefing) return;
        ChangePhase(Phase.Playing);
        Notify("VİLLAYA GİR / ÜST KATTAKİ VERİYİ BUL");
    }
    private IEnumerator AttachWeaponCamera(UniversalAdditionalCameraData mainData,Camera gunCamera)
    {
        // Pipeline changes are applied by Unity's render loop, including while the briefing is paused.
        for (int attempt=0;attempt<8;attempt++)
        {
            yield return null;
            if (mainData == null || gunCamera == null) yield break;
            if (renderPipeline != null && UniversalRenderPipeline.asset != renderPipeline) continue;
            var stack = mainData.cameraStack;
            if (stack == null) continue;
            if (!stack.Contains(gunCamera)) stack.Add(gunCamera);
            yield break;
        }
        Debug.LogError("FPS weapon camera could not join the Forward renderer. Recreate the game scene with Tools > Villa FPS.",this);
    }
    public void Resume() { if (State == Phase.Paused) ChangePhase(Phase.Playing); }
    public void Restart()
    {
        restartImmediately = true; Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }
    public void Briefing()
    {
        restartImmediately = false; Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }
    public void Quit()
    {
        SaveSettings();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    private void ChangePhase(Phase next)
    {
        State = next; Time.timeScale = Playing ? 1f : 0f;
        if (Player != null) Player.SetCursorCaptured(Playing);
        suppressFireUntil = Time.unscaledTime + .2f;
        if (!Playing && Weapon != null && Weapon.muzzleLight != null) Weapon.muzzleLight.enabled = false;
        Interaction = null;
    }
    private void Update()
    {
        if (Player == null) return;
        Keyboard keyboard = Keyboard.current; Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (hud != null && hud.TryHandleEscape()) return;
            if (Playing) ChangePhase(Phase.Paused); else if (State == Phase.Paused) Resume();
            return;
        }
        if (!Playing) return;
        if (!focused || Cursor.lockState != CursorLockMode.Locked) { ChangePhase(Phase.Paused); return; }
        Elapsed += Time.deltaTime;
        Player.LookSensitivity = Sensitivity;
        Player.ApplyLook(Time.unscaledTime > suppressFireUntil && mouse != null ? mouse.delta.ReadValue() : Vector2.zero);
        Vector2 move = Vector2.zero;
        bool sprint = false, jump = false, reload = false;
        if (keyboard != null)
        {
            move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            sprint = keyboard.leftShiftKey.isPressed && move.sqrMagnitude > 0f;
            jump = keyboard.spaceKey.wasPressedThisFrame; reload = keyboard.rKey.wasPressedThisFrame;
        }
        LevelDoor.SyncMovedColliders();
        VillaQueries.SyncTransforms();
        Player.MoveCharacter(move * (Weapon.Aiming ? .65f : 1f), sprint, jump, Mathf.Min(Time.deltaTime, .1f));
        Weapon.Tick(mouse != null && mouse.leftButton.isPressed && Time.unscaledTime > suppressFireUntil,
            mouse != null && mouse.rightButton.isPressed, sprint, reload, Mathf.Clamp01(move.magnitude));
        UpdateInteraction(keyboard != null && keyboard.eKey.wasPressedThisFrame);
        if (move.sqrMagnitude > .1f && Player.Grounded && Time.time > nextStep)
        { nextStep = Time.time + (sprint ? .28f : .43f); PlayCue(6, .2f); }
        Vector3 toExit = Player.transform.position - extraction.position;
        if (HasData && new Vector2(toExit.x, toExit.z).magnitude < 1.8f && Mathf.Abs(toExit.y) < 2f)
        { ChangePhase(Phase.Won); PlayCue(8); }
        if (Player.transform.position.y < spawn.position.y - 10f) Health.Damage(100f, Player.transform.position);
    }
    private void UpdateInteraction(bool use)
    {
        targetDoor = null; targetData = false; Interaction = null;
        if (VillaQueries.Cast(sceneCamera.transform.position, sceneCamera.transform.forward, 2.5f, Player.transform, out RaycastHit hit))
        {
            targetDoor = LevelDoor.FindForCollider(hit.collider);
            targetData = !HasData && hit.collider.transform.IsChildOf(objective);
        }
        if (targetData) Interaction = "[E] VERİ CİHAZINI AL";
        else if (targetDoor != null) Interaction = targetDoor.IsOpen ? "[E] KAPIYI KAPAT" : "[E] KAPIYI AÇ";
        if (!use) return;
        if (targetData)
        {
            HasData = true; objective.gameObject.SetActive(false);
            if (reserveGuard != null) reserveGuard.gameObject.SetActive(true);
            Notify("VERİ ALINDI / BAHÇEDEKİ ÇIKIŞA DÖN"); PlayCue(3);
        }
        else if (targetDoor != null) targetDoor.Toggle();
    }
    public bool CanGuardFire(VillaGuard guard)
    {
        if (!Playing || guard == null || !guard.Alive) return false;
        int freeSlot = -1;
        for (int i=0;i<shootingGuards.Length;i++)
        {
            VillaGuard owner = shootingGuards[i];
            if (owner == null || !owner.Alive || !owner.Engaged || shootingLeaseUntil[i] <= Time.time)
                shootingGuards[i] = null;
            if (shootingGuards[i] == guard)
            {
                shootingLeaseUntil[i] = Time.time + 1.05f;
                return true;
            }
            if (shootingGuards[i] == null) freeSlot = i;
        }
        if (freeSlot < 0) return false;
        shootingGuards[freeSlot] = guard;
        shootingLeaseUntil[freeSlot] = Time.time + 1.05f;
        return true;
    }
    private void OnDamaged(float amount, Vector3 source)
    { HurtSource = source; HurtUntil = Time.unscaledTime + .3f; PlayCue(2); }
    private void Lose() { ChangePhase(Phase.Lost); PlayCue(9); }
    public void GuardDown() { Kills++; }
    public void ShowHit() { HitUntil = Time.unscaledTime + .15f; PlayCue(10,.3f); }
    public void Notify(string message) { Notice = message; NoticeUntil = Time.unscaledTime + 3f; }
    public void PlayCue(int index, float gain = 1f)
    { if (speaker != null && cues != null && index >= 0 && index < cues.Length && cues[index] != null) speaker.PlayOneShot(cues[index], gain); }
    private void OnApplicationFocus(bool focus)
    { focused = focus; if (!focus && Playing) ChangePhase(Phase.Paused); }
    private void OnDestroy()
    {
        if (!runtimeStarted) return;
        SaveSettings();
        if (Instance == this) Instance = null;
        if (renderPipeline != null && QualitySettings.renderPipeline == renderPipeline) QualitySettings.renderPipeline = previousPipeline;
        AudioListener.volume = previousListenerVolume;
        Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    private void LoadSettings()
    {
        if (hasSessionSettings)
        { Sensitivity=sessionSensitivity; Volume=sessionVolume; CameraShake=sessionShake; return; }
        Sensitivity=ValidSetting(PlayerPrefs.GetFloat(PreferencePrefix+"Sensitivity",Sensitivity),.03f,.3f,.1f);
        Volume=ValidSetting(PlayerPrefs.GetFloat(PreferencePrefix+"Volume",Volume),0f,1f,.7f);
        CameraShake=PlayerPrefs.GetInt(PreferencePrefix+"CameraReaction",CameraShake ? 1 : 0) != 0;
    }

    public void SaveSettings()
    {
        Sensitivity=ValidSetting(Sensitivity,.03f,.3f,.1f); Volume=ValidSetting(Volume,0f,1f,.7f);
        hasSessionSettings=true; sessionSensitivity=Sensitivity; sessionVolume=Volume; sessionShake=CameraShake;
        PlayerPrefs.SetFloat(PreferencePrefix+"Sensitivity",Sensitivity);
        PlayerPrefs.SetFloat(PreferencePrefix+"Volume",Volume);
        PlayerPrefs.SetInt(PreferencePrefix+"CameraReaction",CameraShake ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static float ValidSetting(float value,float min,float max,float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value,min,max);
}
