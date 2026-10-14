using UnityEngine;
using UnityEngine.AI;

/// <summary>Small rigid-part walk cycle; aiming and collision remain controlled by the guard.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(VillaGuard), typeof(NavMeshAgent), typeof(VillaHealth))]
public sealed class VillaGuardAnimation : MonoBehaviour
{
    public Transform leftHip, rightHip, leftKnee, rightKnee;
    [Range(0f, 30f)] public float hipSwing = 16f;
    [Range(0f, 40f)] public float kneeLift = 22f;
    private VillaGuard guard;
    private VillaHealth health;
    private NavMeshAgent agent;
    private Quaternion leftHipHome, rightHipHome, leftKneeHome, rightKneeHome;
    private float phase, weight;
    private bool captured;

    private void Awake()
    {
        guard = GetComponent<VillaGuard>();
        health = GetComponent<VillaHealth>();
        agent = GetComponent<NavMeshAgent>();
        if (leftHip == null || rightHip == null || leftKnee == null || rightKnee == null)
        {
            enabled = false;
            return;
        }
        leftHipHome = leftHip.localRotation;
        rightHipHome = rightHip.localRotation;
        leftKneeHome = leftKnee.localRotation;
        rightKneeHome = rightKnee.localRotation;
        phase = Mathf.Abs(GetInstanceID() % 11) * (Mathf.PI * 2f / 11f);
        captured = true;
        health.Died += ResetPose;
    }

    private void LateUpdate()
    {
        if (!captured) return;
        // Death can also occur during a phase transition. Reset before checking the pause state,
        // without touching the existing grounded body-collapse rotation on Model.
        if (health == null || !health.Alive || guard == null || !guard.Alive)
        {
            ResetPose();
            return;
        }
        VillaMission mission = VillaMission.Instance;
        if (mission == null || !mission.Playing || Time.deltaTime <= 0f) return;

        float speed = 0f;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
        {
            Vector3 velocity = agent.velocity;
            if (VillaQueries.IsFinite(velocity)) speed = new Vector2(velocity.x, velocity.z).magnitude;
        }
        float target = speed > .08f ? Mathf.Clamp01(speed / 1.8f) : 0f;
        weight = Mathf.MoveTowards(weight, target, Time.deltaTime * 5f);
        if (weight <= .001f)
        {
            ResetPose();
            return;
        }
        // A full cycle covers roughly two metres; stops ease to neutral without stepping in place.
        if (speed > .08f) phase = Mathf.Repeat(phase + speed * Mathf.PI * Time.deltaTime, Mathf.PI * 2f);
        float swing = Mathf.Sin(phase) * hipSwing * weight;
        float leftBend = Mathf.Max(0f, -Mathf.Cos(phase)) * kneeLift * weight;
        float rightBend = Mathf.Max(0f, Mathf.Cos(phase)) * kneeLift * weight;
        leftHip.localRotation = leftHipHome * Quaternion.Euler(swing, 0f, 0f);
        rightHip.localRotation = rightHipHome * Quaternion.Euler(-swing, 0f, 0f);
        leftKnee.localRotation = leftKneeHome * Quaternion.Euler(leftBend, 0f, 0f);
        rightKnee.localRotation = rightKneeHome * Quaternion.Euler(rightBend, 0f, 0f);
    }

    private void ResetPose()
    {
        weight = 0f;
        if (!captured) return;
        if (leftHip != null) leftHip.localRotation = leftHipHome;
        if (rightHip != null) rightHip.localRotation = rightHipHome;
        if (leftKnee != null) leftKnee.localRotation = leftKneeHome;
        if (rightKnee != null) rightKnee.localRotation = rightKneeHome;
    }

    private void OnDisable() => ResetPose();

    private void OnDestroy()
    {
        if (health != null) health.Died -= ResetPose;
    }
}
