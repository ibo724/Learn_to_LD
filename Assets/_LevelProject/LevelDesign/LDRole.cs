using UnityEngine;

/// <summary>What a level piece is, so the design tools and later the AI can read the layout.</summary>
public enum LDRoleType
{
    [InspectorName("Zemin")] Zemin,
    [InspectorName("Duvar")] Duvar,
    [InspectorName("Alçak siper")] AlcakSiper,
    [InspectorName("Yüksek siper")] YuksekSiper,
    [InspectorName("Blok / kolon")] Blok,
    [InspectorName("Kapı (lento)")] Kapi,
    [InspectorName("Oyuncu girişi")] OyuncuGirisi,
    [InspectorName("Düşman girişi")] DusmanGirisi
}

/// <summary>Marks a greybox piece with its level design role. Data only; it has no runtime behaviour.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Level Design/LD Rol")]
public sealed class LDRole : MonoBehaviour
{
    [Tooltip("Bu parçanın level'daki görevi. Kapı rolü lentoya, oyuncu girişi ScaleRef'e verilir.")]
    public LDRoleType role;
}
