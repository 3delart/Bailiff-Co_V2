// ============================================================
// ParanoiaConfig.cs — Bailiff & Co  V2
// ScriptableObject optionnel pour tweaker les valeurs de paranoïa
// sans recompiler. Si absent, ParanoiaSystem utilise des constantes.
//
// Créer via : clic droit → Create → BailiffCo/ParanoiaConfig
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/ParanoiaConfig")]
public class ParanoiaConfig : ScriptableObject
{
    [Header("Décroissance passive")]
    [Tooltip("Temps (secondes) sans action avant que la paranoïa ne commence à décroître")]
    public float DelayBeforeDecay = 10f;

    [Tooltip("Vitesse de décroissance par seconde (ex: 5/120 = 5 points en 2 minutes)")]
    public float DecayRatePerSecond = 5f / 120f;

    [Header("Bruit — Delta paranoïa")]
    [Tooltip("Paranoïa ajoutée par un bruit léger (pas, porte qui grince)")]
    public float NoiseLight = 3f;

    [Tooltip("Paranoïa ajoutée par un bruit fort (sprint, objet lâché)")]
    public float NoiseLoud = 12f;

    [Tooltip("Paranoïa ajoutée par un bruit très fort (objet cassé, porte forcée)")]
    public float NoiseVeryLoud = 25f;

}
