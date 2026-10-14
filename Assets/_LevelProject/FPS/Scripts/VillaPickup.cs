using UnityEngine;

[DisallowMultipleComponent]
public sealed class VillaPickup : MonoBehaviour
{
    public const float HealthAmount = 35f;
    public const int AmmoAmount = 30;
    public bool health;
    public Transform visual;
    private Vector3 basePosition;
    private bool collected;
    private void Awake() { if (visual != null) basePosition = visual.localPosition; }
    private void OnEnable() { collected = false; VillaQueries.InvalidateTransforms(); }
    private void OnDisable() { VillaQueries.InvalidateTransforms(); }
    private void Update()
    {
        if (visual == null || VillaMission.Instance == null || !VillaMission.Instance.Playing) return;
        visual.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * .06f);
        visual.Rotate(0f, 25f * Time.deltaTime, 0f);
    }
    private void OnTriggerEnter(Collider other) => TryCollect(other);
    private void OnTriggerStay(Collider other) => TryCollect(other);
    private void TryCollect(Collider other)
    {
        VillaMission m = VillaMission.Instance;
        if (collected || other == null || m == null || !m.Playing || m.Player == null
            || m.Health == null || !m.Health.Alive || m.Weapon == null
            || other.GetComponentInParent<LevelPlaytestController>() != m.Player) return;
        Vector3 eye = m.sceneCamera != null ? m.sceneCamera.transform.position
            : m.Player.transform.position + Vector3.up * 1.2f;
        Vector3 toItem = transform.position - eye;
        if (VillaQueries.Cast(eye, toItem, toItem.magnitude, m.Player.transform, out RaycastHit obstruction)
            && !obstruction.collider.transform.IsChildOf(transform)) return;
        float beforeHealth = m.Health.Current;
        int beforeAmmo = m.Weapon.Reserve;
        bool accepted = health ? m.Health.Heal(m.Balance.Healing) : m.Weapon.AddAmmo(m.Balance.AmmoPerPack);
        if (!accepted) return;
        collected = true;
        int added = health ? Mathf.CeilToInt(m.Health.Current - beforeHealth) : m.Weapon.Reserve - beforeAmmo;
        m.Notify(health ? "+" + added + " CAN" : "+" + added + " MERMİ");
        m.PlayCue(4);
        gameObject.SetActive(false);
    }
}
