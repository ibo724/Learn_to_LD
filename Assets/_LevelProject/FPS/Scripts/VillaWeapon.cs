using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class VillaWeapon : MonoBehaviour
{
    public const int MagazineCapacity = 30;
    public const int ReserveCapacity = 150;
    public const float DamagePerShot = 25f;
    public const float ReloadDuration = 2f;
    private const float ShotInterval = 1f / 6f;
    private const float ShotRange = 50f;

    public Transform muzzle, movingModel;
    public Light muzzleLight;
    public Material tracerMaterial;
    private VillaMission mission;
    private Camera cameraToUse, viewModelCamera;
    private Vector3 home;
    private Quaternion homeRotation;
    private float nextShot, reloadUntil, recoil, flashUntil, traceUntil;
    private bool reloading, initialized, traceFollowsMuzzle;
    private LineRenderer trace;
    public int Magazine { get; private set; } = MagazineCapacity;
    public int Reserve { get; private set; } = 90;
    public bool Reloading => reloading;
    public bool Aiming { get; private set; }
    public float ReloadProgress => reloading
        ? 1f - Mathf.Clamp01((reloadUntil - Time.time) / ReloadDuration) : 0f;

    private bool CanOperate => isActiveAndEnabled && initialized && mission != null && mission.Playing
        && mission.Player != null && mission.Health != null && mission.Health.Alive && cameraToUse != null;

    public void Initialize(VillaMission owner, Camera view)
    {
        mission = owner;
        cameraToUse = view;
        viewModelCamera = GetComponentInParent<Camera>();
        if (movingModel == null) movingModel = transform;
        home = movingModel.localPosition;
        homeRotation = movingModel.localRotation;
        initialized = owner != null && view != null;
        if (muzzleLight != null) muzzleLight.enabled = false;
        if (trace != null) Destroy(trace.gameObject);
        trace = null;
        if (tracerMaterial != null)
        {
            trace = new GameObject("Player shot streak").AddComponent<LineRenderer>();
            trace.sharedMaterial = tracerMaterial;
            trace.useWorldSpace = true;
            trace.shadowCastingMode = ShadowCastingMode.Off;
            trace.receiveShadows = false;
            trace.startWidth = .018f;
            trace.endWidth = .006f;
            trace.positionCount = 2;
            trace.enabled = false;
        }
    }

    public bool AddAmmo(int amount)
    {
        if (amount <= 0 || Reserve >= ReserveCapacity) return false;
        // Subtract before adding, so even an out-of-range pickup cannot overflow the reserve.
        Reserve += Mathf.Min(amount, ReserveCapacity - Reserve);
        return true;
    }

    public void Reload()
    {
        if (!CanOperate || reloading || Magazine >= MagazineCapacity || Reserve <= 0) return;
        reloading = true;
        reloadUntil = Time.time + ReloadDuration;
        Aiming = false;
        mission.PlayCue(1);
    }

    public void Tick(bool fire, bool aim, bool sprint, bool reload, float movement)
    {
        if (!CanOperate)
        {
            Aiming = false;
            HideFlash();
            return;
        }

        if (reload) Reload();
        if (reloading && Time.time >= reloadUntil)
        {
            int amount = Mathf.Min(MagazineCapacity - Magazine, Reserve);
            Magazine += amount;
            Reserve -= amount;
            reloading = false;
        }

        Aiming = aim && !sprint && !reloading;
        cameraToUse.fieldOfView = Mathf.Lerp(cameraToUse.fieldOfView, Aiming ? 55f : 70f,
            1f - Mathf.Exp(-12f * Time.deltaTime));
        if (fire && !sprint && !reloading && Time.time >= nextShot)
        {
            nextShot = Time.time + ShotInterval;
            if (Magazine > 0) Shoot();
            else
            {
                mission.PlayCue(5);
                nextShot = Time.time + .4f;
            }
        }

        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 7f);
        float bobWeight = Mathf.Clamp01(movement) * (Aiming ? .25f : 1f);
        Vector3 bob = new Vector3(Mathf.Sin(Time.time * 9f) * .006f,
            Mathf.Sin(Time.time * 18f) * .004f, 0f) * bobWeight;
        // The factory rifle's sight tips sit .145 m above its origin; the model starts at -.25 m.
        Vector3 target = home + (Aiming ? new Vector3(-home.x, .105f, .09f) : Vector3.zero)
            + bob - Vector3.forward * recoil * .06f;
        movingModel.localPosition = Vector3.Lerp(movingModel.localPosition, target,
            1f - Mathf.Exp(-16f * Time.deltaTime));
        movingModel.localRotation = homeRotation * Quaternion.Euler(
            reloading ? Mathf.Sin(ReloadProgress * Mathf.PI) * 28f : -recoil * 5f, 0f, 0f);
    }

    private void Shoot()
    {
        Magazine--;
        mission.Shots++;
        recoil = 1f;
        flashUntil = Time.time + .045f;
        mission.PlayCue(0);

        Vector3 origin = cameraToUse.transform.position;
        Vector3 direction = cameraToUse.transform.forward;
        bool hitSomething = VillaQueries.Cast(origin, direction, ShotRange,
            mission.Player.transform, out RaycastHit hit);
        Vector3 end = hitSomething ? hit.point : origin + direction * ShotRange;
        Vector3 traceOrigin = origin + direction * .35f;
        traceFollowsMuzzle = false;

        if (muzzle != null && VillaQueries.IsFinite(muzzle.position))
        {
            traceOrigin = muzzle.position;
            traceFollowsMuzzle = true;
            Vector3 toMuzzle = muzzle.position - origin;
            // The virtual weapon can extend into a nearby wall. Cast from the eye first so
            // a barrel already inside that wall cannot fire through its far side.
            if (VillaQueries.Cast(origin, toMuzzle, toMuzzle.magnitude,
                mission.Player.transform, out RaycastHit obstruction))
            {
                hit = obstruction;
                hitSomething = true;
                end = obstruction.point;
                traceOrigin = origin;
                traceFollowsMuzzle = false;
            }
            else
            {
                Vector3 fromMuzzle = end - muzzle.position;
                if (VillaQueries.Cast(muzzle.position, fromMuzzle, fromMuzzle.magnitude + .015f,
                    mission.Player.transform, out obstruction))
                {
                    hit = obstruction;
                    hitSomething = true;
                    end = hit.point;
                }
            }
        }

        if (hitSomething)
        {
            VillaHealth victim = hit.collider.GetComponentInParent<VillaHealth>();
            if (victim != null && victim != mission.Health && victim.Alive)
            {
                victim.Damage(DamagePerShot, origin);
                mission.Hits++;
                mission.ShowHit();
            }
        }

        if (mission.CameraShake) mission.Player.AddRecoil(Aiming ? .45f : .8f);
        if (trace != null)
        {
            trace.SetPosition(0, traceFollowsMuzzle ? VisibleMuzzlePosition(traceOrigin) : traceOrigin);
            trace.SetPosition(1, end);
            traceUntil = Time.time + .055f;
            trace.enabled = true;
        }
    }

    private void LateUpdate()
    {
        bool visible = CanOperate;
        if (!visible)
        {
            Aiming = false;
            HideFlash();
            return;
        }
        if (muzzleLight != null) muzzleLight.enabled = visible && Time.time < flashUntil;
        if (trace != null) trace.enabled = visible && Time.time < traceUntil;
        if (trace != null && trace.enabled && traceFollowsMuzzle && muzzle != null)
            trace.SetPosition(0, VisibleMuzzlePosition(muzzle.position));
    }

    private Vector3 VisibleMuzzlePosition(Vector3 worldPosition)
    {
        if (viewModelCamera == null || viewModelCamera == cameraToUse) return worldPosition;
        // The overlay uses its own FOV. Reproject only the streak's visual origin into the
        // world camera so it meets the rendered barrel; collision still uses the real muzzle.
        Vector3 viewport = viewModelCamera.WorldToViewportPoint(worldPosition);
        return viewport.z > 0f ? cameraToUse.ViewportToWorldPoint(viewport) : worldPosition;
    }

    private void HideFlash()
    {
        flashUntil = traceUntil = 0f;
        traceFollowsMuzzle = false;
        if (muzzleLight != null) muzzleLight.enabled = false;
        if (trace != null) trace.enabled = false;
    }

    private void OnDisable()
    {
        Aiming = false;
        HideFlash();
    }

    private void OnDestroy()
    {
        if (trace != null) Destroy(trace.gameObject);
    }
}
