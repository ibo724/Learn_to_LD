using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillaAmbience : MonoBehaviour
{
    public VillaMission mission;
    public AudioClip wind, water;
    public Vector3 poolPosition;
    public Bounds interiorBounds;

    private const float GardenVolume = .24f;
    private const float PoolVolume = .12f;
    private const float IndoorWindMultiplier = .22f;
    private const float VolumeResponse = 3f;
    private AudioSource windSource, waterSource;
    private bool initialized, audible, windStarted, waterStarted;

    private void Start()
    {
        if (!Application.isPlaying) return;
        if (mission == null) mission = VillaMission.Instance;
        windSource = CreateSource("Garden wind loop", wind, false);
        waterSource = CreateSource("Pool water loop", water, true);
        if (waterSource != null && VillaQueries.IsFinite(poolPosition)) waterSource.transform.position = poolPosition;
        initialized = true;
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip, bool spatial)
    {
        if (clip == null) return null;
        GameObject owner = new GameObject(sourceName);
        owner.transform.SetParent(transform, false);
        AudioSource source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.clip = clip;
        source.volume = 0f;
        source.priority = 230; // Gameplay cues retain voice priority over the background loops.
        source.spatialBlend = spatial ? 1f : 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 10f;
        return source;
    }

    private void Update()
    {
        if (!initialized) return;
        bool playing = mission != null && mission.Playing && Application.isFocused;
        if (playing != audible) SetPlaying(playing);
        if (!playing) return;

        bool indoors = mission.Player != null && interiorBounds.size.sqrMagnitude > .001f &&
            interiorBounds.Contains(mission.Player.transform.position);
        float windTarget = GardenVolume * (indoors ? IndoorWindMultiplier : 1f);
        float blend = 1f - Mathf.Exp(-VolumeResponse * Mathf.Max(0f, Time.unscaledDeltaTime));
        if (windSource != null) windSource.volume = Mathf.Lerp(windSource.volume, windTarget, blend);
        if (waterSource != null)
        {
            waterSource.volume = Mathf.Lerp(waterSource.volume, PoolVolume, blend);
            if (VillaQueries.IsFinite(poolPosition)) waterSource.transform.position = poolPosition;
        }
    }

    private void SetPlaying(bool playing)
    {
        audible = playing;
        SetSourcePlaying(windSource, playing, ref windStarted);
        SetSourcePlaying(waterSource, playing, ref waterStarted);
    }

    private static void SetSourcePlaying(AudioSource source, bool playing, ref bool started)
    {
        if (source == null) return;
        if (playing)
        {
            source.volume = 0f; // Fade back in without restarting the loop's playback position.
            if (started) source.UnPause();
            else { source.Play(); started = true; }
        }
        else if (started) source.Pause();
    }

    private void OnDisable()
    {
        if (initialized) SetPlaying(false);
    }

    private void OnDestroy()
    {
        if (windSource != null) Destroy(windSource.gameObject);
        if (waterSource != null) Destroy(waterSource.gameObject);
    }
}
