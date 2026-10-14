using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillaHealth : MonoBehaviour
{
    [Min(1f)] public float maximum = 100f;
    public float Current { get; private set; }
    public bool Alive => Current > 0f;
    public float Normalized => Mathf.Clamp01(Current / Mathf.Max(1f, maximum));
    public event Action<float, Vector3> Damaged;
    public event Action Died;
    private bool deathReported;

    private void Awake() => Restore();

    private void OnValidate()
    {
        if (float.IsNaN(maximum) || float.IsInfinity(maximum)) maximum = 100f;
        maximum = Mathf.Max(1f, maximum);
    }

    public void Restore()
    {
        OnValidate();
        Current = maximum;
        deathReported = false;
    }

    public bool Heal(float amount)
    {
        if (!Alive || Current >= maximum || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
            return false;
        Current = Mathf.Min(maximum, Current + amount);
        return true;
    }

    public void Damage(float amount, Vector3 source)
    {
        if (!Alive || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        float applied = Mathf.Min(Current, amount);
        Current = Mathf.Max(0f, Current - amount);
        bool fatal = !Alive && !deathReported;
        // Mark the transition before callbacks so a second hit cannot report the same death twice.
        if (fatal) deathReported = true;
        if (!VillaQueries.IsFinite(source)) source = transform.position;
        Damaged?.Invoke(applied, source);
        if (fatal) Died?.Invoke();
    }
}
