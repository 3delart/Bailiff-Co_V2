// ============================================================
// FurnitureConfig.cs — Bailiff & Co  V2
// ScriptableObject optionnel pour tweaker les valeurs de bruit
// des meubles sans recompiler.
// Créer via : clic droit → Create → BailiffCo/FurnitureConfig
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/FurnitureConfig")]
public class FurnitureConfig : ScriptableObject
{
    [Header("Noise Ranges — Opening")]
    [Tooltip("Noise range (metres) for normal door/drawer opening")]
    public float NormalOpenNoiseRange = 4f;

    [Tooltip("Noise range (metres) when a door/drawer squeaks")]
    public float SqueakNoiseRange = 7f;

    [Tooltip("Noise range (metres) when forcing a door open")]
    public float ForcedOpenNoiseRange = 20f;

    [Header("Noise Ranges — Sliding (furniture push)")]
    [Tooltip("Noise range (metres) when sliding on parquet floor")]
    public float NoiseRangeParquet = 10f;

    [Tooltip("Noise range (metres) when sliding on carpet")]
    public float NoiseRangeCarpet = 4f;

    [Tooltip("Noise range (metres) when sliding on tile")]
    public float NoiseRangeTile = 12f;

    [Tooltip("Noise range (metres) for default/unknown floor types")]
    public float NoiseRangeDefault = 8f;

    [Header("Sliding")]
    [Tooltip("Interval (seconds) between noise emissions while sliding")]
    public float SlidingNoiseInterval = 0.3f;

    [Tooltip("Minimum speed multiplier when pushing heavy furniture")]
    [Range(0.1f, 1f)]
    public float MinSpeedMultiplier = 0.25f;
}