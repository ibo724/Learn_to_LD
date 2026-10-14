using UnityEngine;

/// <summary>Authoring values for the one-mission prototype. Existing GDD numbers are the defaults.</summary>
[CreateAssetMenu(fileName="Villa Balance",menuName="Villa FPS/Game balance")]
public sealed class VillaBalance : ScriptableObject
{
    [Header("Oyuncu")]
    [SerializeField,Min(1f)] private float playerHealth=100f;
    [SerializeField,Min(.1f)] private float walkSpeed=3.5f;
    [SerializeField,Min(.1f)] private float sprintSpeed=6f;
    [SerializeField,Min(0f)] private float jumpHeight=.9f;
    [SerializeField,Range(-50f,-1f)] private float gravity=-22f;

    [Header("Karabina")]
    [SerializeField,Range(1,100)] private int magazineSize=30;
    [SerializeField,Min(0)] private int startingReserve=90;
    [SerializeField,Min(0)] private int maxReserve=150;
    [SerializeField,Min(.1f)] private float shotDamage=25f;
    [SerializeField,Range(1f,20f)] private float shotsPerSecond=6f;
    [SerializeField,Range(.2f,8f)] private float reloadSeconds=2f;
    [SerializeField,Range(5f,150f)] private float shotRange=50f;
    [SerializeField,Range(0f,3f)] private float hipRecoil=.8f;
    [SerializeField,Range(0f,3f)] private float aimRecoil=.45f;

    [Header("Muhafiz")]
    [SerializeField,Min(1f)] private float guardHealth=100f;
    [SerializeField,Range(.1f,6f)] private float guardSpeed=2.2f;
    [SerializeField,Range(2f,30f)] private float guardViewDistance=12f;
    [SerializeField,Range(1f,25f)] private float guardAttackDistance=7f;
    [SerializeField,Range(30f,180f)] private float guardVisionAngle=100f;
    [SerializeField,Range(.1f,3f)] private float guardReactionSeconds=.7f;
    [SerializeField,Range(1f,12f)] private float guardSearchSeconds=4f;
    [SerializeField,Range(.2f,4f)] private float guardShotInterval=1f;
    [SerializeField,Min(.1f)] private float guardDamage=10f;

    [Header("Destek paketleri")]
    [SerializeField,Min(1)] private int healing=35;
    [SerializeField,Min(1)] private int ammoPerPack=30;

    private static VillaBalance fallback;
    public static VillaBalance Default
    {
        get
        {
            if (fallback != null) return fallback;
            fallback=CreateInstance<VillaBalance>();
            fallback.hideFlags=HideFlags.HideAndDontSave;
            return fallback;
        }
    }

    public float PlayerHealth=>Safe(playerHealth,1f,1000f,100f);
    public float WalkSpeed=>Safe(walkSpeed,.1f,12f,3.5f);
    public float SprintSpeed=>Mathf.Max(WalkSpeed,Safe(sprintSpeed,.1f,16f,6f));
    public float JumpHeight=>Safe(jumpHeight,0f,2f,.9f);
    public float Gravity=>Safe(gravity,-50f,-1f,-22f);
    public int MagazineSize=>Mathf.Clamp(magazineSize,1,100);
    public int StartingReserve=>Mathf.Clamp(startingReserve,0,MaxReserve);
    public int MaxReserve=>Mathf.Clamp(maxReserve,0,1000);
    public float ShotDamage=>Safe(shotDamage,.1f,1000f,25f);
    public float ShotsPerSecond=>Safe(shotsPerSecond,1f,20f,6f);
    public float ReloadSeconds=>Safe(reloadSeconds,.2f,8f,2f);
    public float ShotRange=>Safe(shotRange,5f,150f,50f);
    public float HipRecoil=>Safe(hipRecoil,0f,3f,.8f);
    public float AimRecoil=>Safe(aimRecoil,0f,3f,.45f);
    public float GuardHealth=>Safe(guardHealth,1f,1000f,100f);
    public float GuardSpeed=>Safe(guardSpeed,.1f,6f,2.2f);
    public float GuardViewDistance=>Safe(guardViewDistance,2f,30f,12f);
    public float GuardAttackDistance=>Mathf.Min(GuardViewDistance,Safe(guardAttackDistance,1f,25f,7f));
    public float GuardVisionAngle=>Safe(guardVisionAngle,30f,180f,100f);
    public float GuardReactionSeconds=>Safe(guardReactionSeconds,.1f,3f,.7f);
    public float GuardSearchSeconds=>Safe(guardSearchSeconds,1f,12f,4f);
    public float GuardShotInterval=>Safe(guardShotInterval,.2f,4f,1f);
    public float GuardDamage=>Safe(guardDamage,.1f,1000f,10f);
    public int Healing=>Mathf.Clamp(healing,1,1000);
    public int AmmoPerPack=>Mathf.Clamp(ammoPerPack,1,1000);

    private void OnValidate()
    {
        playerHealth=PlayerHealth; walkSpeed=WalkSpeed; sprintSpeed=SprintSpeed;
        jumpHeight=JumpHeight; gravity=Gravity;
        magazineSize=MagazineSize; maxReserve=MaxReserve; startingReserve=StartingReserve;
        shotDamage=ShotDamage; shotsPerSecond=ShotsPerSecond; reloadSeconds=ReloadSeconds;
        shotRange=ShotRange; hipRecoil=HipRecoil; aimRecoil=AimRecoil;
        guardHealth=GuardHealth; guardSpeed=GuardSpeed; guardViewDistance=GuardViewDistance;
        guardAttackDistance=GuardAttackDistance; guardVisionAngle=GuardVisionAngle;
        guardReactionSeconds=GuardReactionSeconds; guardSearchSeconds=GuardSearchSeconds;
        guardShotInterval=GuardShotInterval; guardDamage=GuardDamage;
        healing=Healing; ammoPerPack=AmmoPerPack;
    }

    private static float Safe(float value,float min,float max,float normal)
        => float.IsNaN(value) || float.IsInfinity(value) ? normal : Mathf.Clamp(value,min,max);
}
