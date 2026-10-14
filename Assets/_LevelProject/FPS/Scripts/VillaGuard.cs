using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent), typeof(VillaHealth))]
public sealed class VillaGuard : MonoBehaviour
{
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int AccentStrengthId = Shader.PropertyToID("_AccentStrength");

    private sealed class ArmorFlash
    {
        public MeshRenderer renderer;
        public MaterialPropertyBlock original, flash;
    }

    public Vector3[] patrol = new Vector3[0];
    public Transform visual, muzzle;
    public AudioClip shotClip, alertClip, footstepClip, grassFootstepClip;
    public Material tracerMaterial;
    private NavMeshAgent agent;
    private VillaHealth health;
    private VillaBalance balance;
    private AudioSource speaker;
    private LineRenderer trace;
    private MeshRenderer muzzleFlare;
    private ArmorFlash[] armorFlashes;
    private Vector3 lastSeen, visualHome;
    private Quaternion visualRotation;
    private float reactionUntil, searchUntil, nextShot, nextRepath, nextNavCheck, patrolWaitUntil, traceUntil, nextFootstep;
    private float flashUntil, armorFlashUntil;
    private int waypoint, shotVisualCounter;
    private bool alerted, searching, hadSight, hasPatrolDestination, dead, armorFlashing;
    public bool Alive => !dead && health != null && health.Alive && isActiveAndEnabled;
    // Engaged means able to line up an actual shot, rather than merely seeing through glass.
    public bool Engaged { get; private set; }
    private bool CanNavigate => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<VillaHealth>();
        balance = VillaMission.Instance != null ? VillaMission.Instance.Balance : VillaBalance.Default;
        health.maximum = balance.GuardHealth;
        health.Restore();
        agent.speed = balance.GuardSpeed;
        speaker = GetComponent<AudioSource>();
        nextFootstep = Time.time + Mathf.Abs(GetInstanceID() % 10) * .047f;
        health.Damaged += OnDamaged;
        health.Died += Die;
        if (visual != null)
        {
            visualHome = visual.localPosition;
            visualRotation = visual.localRotation;
            CacheArmorFlashes();
        }
        if (tracerMaterial != null)
        {
            trace = new GameObject(name + " shot streak").AddComponent<LineRenderer>();
            trace.sharedMaterial = tracerMaterial;
            trace.useWorldSpace = true;
            trace.shadowCastingMode = ShadowCastingMode.Off;
            trace.receiveShadows = false;
            trace.startWidth = .025f;
            trace.endWidth = .008f;
            trace.positionCount = 2;
            trace.enabled = false;
        }
    }

    private void Start()
    {
        VillaImpactEffects.Prepare(VillaMission.Instance, tracerMaterial);
        muzzleFlare = VillaImpactEffects.CreateMuzzleVisual(muzzle, tracerMaterial, VillaQueries.GuardLayer, 1.15f);
    }

    private void CacheArmorFlashes()
    {
        MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
        armorFlashes = new ArmorFlash[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            if (material == null || !material.HasProperty(AccentColorId) || !material.HasProperty(AccentStrengthId)) continue;
            ArmorFlash target = new ArmorFlash { renderer = renderers[i],
                original = new MaterialPropertyBlock(), flash = new MaterialPropertyBlock() };
            renderers[i].GetPropertyBlock(target.original);
            renderers[i].GetPropertyBlock(target.flash);
            target.flash.SetColor(AccentColorId, new Color(.95f, .79f, .48f));
            target.flash.SetFloat(AccentStrengthId, .45f);
            armorFlashes[i] = target;
        }
    }

    private void UpdateArmorFlash(bool visible)
    {
        bool active = visible && Time.time < armorFlashUntil;
        if (active == armorFlashing) return;
        armorFlashing = active;
        if (armorFlashes == null) return;
        for (int i = 0; i < armorFlashes.Length; i++)
        {
            ArmorFlash target = armorFlashes[i];
            if (target != null && target.renderer != null)
                target.renderer.SetPropertyBlock(active ? target.flash : target.original);
        }
    }

    private void OnEnable()
    {
        // Reserve guards join during Mission.Update, after this frame's first physics sync.
        VillaQueries.InvalidateTransforms();
    }

    private void Update()
    {
        VillaMission mission = VillaMission.Instance;
        Engaged = false;
        if (!Alive || mission == null || !mission.Playing || mission.Player == null
            || mission.Health == null || !mission.Health.Alive)
        {
            StopNavigation();
            HideTransientEffects();
            return;
        }

        TryBindNavigation();
        Vector3 eye = transform.position + Vector3.up * 1.5f;
        Vector3 target = mission.Player.transform.position + Vector3.up * 1.15f;
        Vector3 toPlayer = target - eye;
        Vector3 horizontalToPlayer = toPlayer;
        horizontalToPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        bool sees = distance <= balance.GuardViewDistance
            && (alerted || Vector3.Angle(transform.forward, horizontalToPlayer) <= balance.GuardVisionAngle * .5f)
            && VillaQueries.Cast(eye, toPlayer, distance + .1f, transform, out RaycastHit sight, true)
            && IsPlayer(sight.collider, mission);

        if (sees)
        {
            if (!alerted) Alert(mission.Player.transform.position);
            else if (!hadSight) reactionUntil = Mathf.Max(reactionUntil, Time.time + balance.GuardReactionSeconds * .5f);
            lastSeen = mission.Player.transform.position;
            searchUntil = Time.time + balance.GuardSearchSeconds;
            searching = true;
            hasPatrolDestination = false;

            bool inRange = distance <= balance.GuardAttackDistance;
            bool clearShot = inRange && HasClearShot(eye, target, mission);
            if (clearShot)
            {
                StopNavigation(true);
                Vector3 flatDirection = horizontalToPlayer;
                if (flatDirection.sqrMagnitude > .001f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(flatDirection), Time.deltaTime * 240f);
                }
                bool facing = flatDirection.sqrMagnitude <= .001f
                    || Vector3.Angle(transform.forward, flatDirection) <= 12f;
                Engaged = facing;
                if (facing && Time.time >= reactionUntil && Time.time >= nextShot
                    && mission.CanGuardFire(this)) Fire(mission);
            }
            else
            {
                // Glass blocks a bullet but not perception. A guard may approach an open route,
                // and never operates a door itself.
                GoTo(lastSeen);
            }
        }
        else if (searching)
        {
            if (Time.time >= searchUntil)
            {
                searching = false;
                alerted = false;
                hasPatrolDestination = false;
                StopNavigation(true);
            }
            else GoTo(lastSeen);
        }
        else Patrol();

        hadSight = sees;
        if (visual != null)
        {
            float walk = CanNavigate ? Mathf.Min(1f, agent.velocity.magnitude) : 0f;
            visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(Time.time * 10f) * .018f * walk);
        }
        if (mission.Playing && CanNavigate && !agent.isStopped && agent.velocity.sqrMagnitude > .0625f
            && Time.time >= nextFootstep && speaker != null && footstepClip != null)
        {
            nextFootstep = Time.time + .48f;
            AudioClip step = grassFootstepClip != null && VillaQueries.TerrainUnderfoot(transform.position)
                ? grassFootstepClip : footstepClip;
            speaker.PlayOneShot(step, .18f);
        }
        if (trace != null) trace.enabled = mission.Playing && Time.time < traceUntil;
        if (muzzleFlare != null) muzzleFlare.enabled = mission.Playing && Time.time < flashUntil;
        UpdateArmorFlash(mission.Playing);
    }

    private void TryBindNavigation()
    {
        if (CanNavigate || agent == null || !agent.isActiveAndEnabled || Time.time < nextNavCheck) return;
        nextNavCheck = Time.time + 1f;
        // A stationary guard still perceives and shoots if a bake is missing. Retry the local
        // surface instead of touching agent navigation properties while it is unbound.
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.2f, agent.areaMask))
        {
            Vector3 from = transform.position + Vector3.up * .9f;
            Vector3 to = hit.position + Vector3.up * .9f;
            Vector3 displacement = to - from;
            // A closing door may carve away the polygon under this actor. Recovery must not
            // jump across that door to the nearest surviving polygon. Check both directions
            // because a forward ray alone misses a collider containing its origin.
            if (VillaQueries.Cast(from, displacement, displacement.magnitude, transform, out _)
                || VillaQueries.Cast(to, -displacement, displacement.magnitude, transform, out _)) return;
            if (agent.Warp(hit.position)) VillaQueries.InvalidateTransforms();
        }
    }

    private void StopNavigation(bool clearPath = false)
    {
        if (!CanNavigate) return;
        if (!agent.isStopped) agent.isStopped = true;
        if (clearPath && agent.hasPath) agent.ResetPath();
    }

    private bool GoTo(Vector3 point)
    {
        if (!CanNavigate || !VillaQueries.IsFinite(point)) return false;
        if (Time.time < nextRepath)
        {
            bool continuing = agent.hasPath || agent.pathPending;
            if (continuing && agent.isStopped) agent.isStopped = false;
            return continuing;
        }
        nextRepath = Time.time + .3f;
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 1f, agent.areaMask))
        {
            StopNavigation(true);
            return false;
        }
        bool accepted = agent.SetDestination(hit.position);
        if (accepted && agent.isStopped) agent.isStopped = false;
        return accepted;
    }

    private void Patrol()
    {
        if (!CanNavigate || patrol == null || patrol.Length == 0)
        {
            StopNavigation();
            return;
        }
        waypoint = Mathf.Clamp(waypoint, 0, patrol.Length - 1);
        if (Time.time < patrolWaitUntil)
        {
            StopNavigation();
            return;
        }
        if (hasPatrolDestination && !agent.pathPending
            && agent.pathStatus == NavMeshPathStatus.PathComplete
            && agent.remainingDistance <= Mathf.Max(.45f, agent.stoppingDistance + .1f))
        {
            waypoint = (waypoint + 1) % patrol.Length;
            patrolWaitUntil = Time.time + .65f;
            hasPatrolDestination = false;
            StopNavigation();
            return;
        }
        if (GoTo(patrol[waypoint])) hasPatrolDestination = true;
    }

    private void Alert(Vector3 source)
    {
        alerted = searching = true;
        lastSeen = source;
        reactionUntil = Time.time + balance.GuardReactionSeconds;
        searchUntil = Time.time + balance.GuardSearchSeconds;
        nextRepath = 0f;
        hasPatrolDestination = false;
        if (speaker != null && alertClip != null) speaker.PlayOneShot(alertClip, .35f);
    }

    private void OnDamaged(float amount, Vector3 source)
    {
        if (dead || health == null || !health.Alive) return;
        armorFlashUntil = Time.time + .065f;
        UpdateArmorFlash(true);
        VillaMission mission = VillaMission.Instance;
        // Damage sources are muzzle/camera positions, which can be above the nav sampling radius.
        Vector3 searchPoint = mission != null && mission.Player != null
            ? mission.Player.transform.position : source;
        if (!alerted) Alert(searchPoint);
        else
        {
            lastSeen = searchPoint;
            searchUntil = Time.time + balance.GuardSearchSeconds;
            searching = true;
        }
    }

    private bool HasClearShot(Vector3 eye, Vector3 target, VillaMission mission)
    {
        Vector3 origin = ShotOrigin(target);
        Vector3 eyeToMuzzle = origin - eye;
        if (VillaQueries.Cast(eye, eyeToMuzzle, eyeToMuzzle.magnitude, transform, out RaycastHit cover)
            && !IsPlayer(cover.collider, mission)) return false;
        Vector3 direction = target - origin;
        return VillaQueries.Cast(origin, direction, direction.magnitude + .1f,
            transform, out RaycastHit hit) && IsPlayer(hit.collider, mission);
    }

    private Vector3 ShotOrigin(Vector3 target)
    {
        Vector3 eye = transform.position + Vector3.up * 1.4f;
        if (muzzle == null || !VillaQueries.IsFinite(muzzle.position)) return eye;
        Vector3 toTarget = target - muzzle.position;
        // At contact distance the decorative barrel may already be inside the player's
        // controller. Raycasts originating there cannot hit its containing collider.
        if (toTarget.sqrMagnitude < .16f || Vector3.Dot(toTarget, target - eye) <= 0f) return eye;
        return muzzle.position;
    }

    private static bool IsPlayer(Collider collider, VillaMission mission)
    {
        return collider != null && collider.GetComponentInParent<LevelPlaytestController>() == mission.Player;
    }

    private void Fire(VillaMission mission)
    {
        Vector3 target = mission.Player.transform.position + Vector3.up * 1.15f;
        if (!HasClearShot(transform.position + Vector3.up * 1.5f, target, mission))
        {
            Engaged = false;
            return;
        }
        nextShot = Time.time + balance.GuardShotInterval;
        Vector3 origin = ShotOrigin(target);
        Vector3 forward = (target - origin).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < .001f) right = transform.right;
        Vector3 up = Vector3.Cross(forward, right).normalized;
        Vector2 spread = Random.insideUnitCircle * Mathf.Tan(3f * Mathf.Deg2Rad);
        Vector3 direction = (forward + right * spread.x + up * spread.y).normalized;
        bool struck = VillaQueries.Cast(origin, direction, 30f, transform, out RaycastHit hit);
        if (struck && IsPlayer(hit.collider, mission)) mission.Health.Damage(balance.GuardDamage, origin);
        else if (struck) VillaImpactEffects.Emit(hit);
        flashUntil = Time.time + .055f;
        if (muzzleFlare != null)
            muzzleFlare.transform.localRotation = Quaternion.Euler(0f, 0f, ++shotVisualCounter * 47f % 360f);
        if (speaker != null && shotClip != null) speaker.PlayOneShot(shotClip, .35f);
        if (trace != null)
        {
            trace.SetPosition(0, origin);
            trace.SetPosition(1, struck ? hit.point : origin + direction * 20f);
            traceUntil = Time.time + .08f;
            trace.enabled = true;
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        Engaged = false;
        StopNavigation(true);
        if (agent != null) agent.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        VillaQueries.InvalidateTransforms();
        if (visual != null)
        {
            visual.localRotation = visualRotation * Quaternion.Euler(-85f, 0f, 18f);
            visual.localPosition = visualHome + Vector3.up * .15f;
        }
        HideTransientEffects();
        VillaMission.Instance?.GuardDown();
    }

    private void HideTransientEffects()
    {
        traceUntil = flashUntil = armorFlashUntil = 0f;
        if (trace != null) trace.enabled = false;
        if (muzzleFlare != null) muzzleFlare.enabled = false;
        UpdateArmorFlash(false);
    }

    private void OnDisable()
    {
        Engaged = false;
        hadSight = false;
        VillaQueries.InvalidateTransforms();
        StopNavigation();
        HideTransientEffects();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= Die;
        }
        if (trace != null) Destroy(trace.gameObject);
    }
}
