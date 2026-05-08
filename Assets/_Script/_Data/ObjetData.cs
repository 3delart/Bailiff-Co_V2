// ============================================================
// ObjetData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un objet saisissable (valeur, physique, scan).
// Créer via : clic droit → Create → BailiffCo/ObjetData
//
// COLOCALISATION RECOMMANDÉE :
//   Models/Props/Vase/
//   ├── vase.fbx
//   ├── Vase.prefab       ← ValueObject.cs référence cet asset
//   └── VaseData.asset    ← cet asset, colocalisé avec le prefab
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/ObjetData")]
public class ObjetData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string ObjectName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite UIIcon;
    public GameObject Prefab;

    // ── VALUE ────────────────────────────────────────────────
    [Header("Value")]
    [Tooltip("Minimum value when spawned (randomised with seed).")]
    public float ValueMin = 500f;
    [Tooltip("Maximum value when spawned (randomised with seed).")]
    public float ValueMax = 5000f;

    // ── PHYSICS ──────────────────────────────────────────────
    [Header("Physics")]
    [Tooltip("Mass in kg — affects carry speed and throw velocity.")]
    public float WeightKg = 1f;
    [Tooltip("If true, collisions above threshold damage the value.")]
    public bool IsFragile = false;
    [Tooltip("If true, requires two players to carry (coop).")]
    public bool RequiresTwoPlayers = false;
    [Tooltip("If true, cannot fit through standard doorways — blocks certain hiding spots.")]
    public bool IsOversized = false;

    // ── DAMAGE THRESHOLDS ────────────────────────────────────
    [Header("Damage Thresholds (Fragile only)")]
    [Tooltip("Impact speed (m/s) below which no damage occurs.")]
    public float DamageImpactThreshold = 2f;
    [Tooltip("Impact speed above which major damage (80% value loss) occurs.")]
    public float MajorDamageThreshold  = 6f;
    [Tooltip("Value multiplier kept after a minor impact (e.g. 0.5 = 50% lost).")]
    [Range(0f, 1f)]
    public float MinorDamageMultiplier = 0.5f;
    [Tooltip("Value multiplier kept after a major impact (e.g. 0.2 = 80% lost).")]
    [Range(0f, 1f)]
    public float MajorDamageMultiplier = 0.2f;

    // ── SCAN ─────────────────────────────────────────────────
    [Header("Scan / X-Ray")]
    [Tooltip("Full name revealed after scanning (e.g. 'Vase Ming Dynasty, ca. 1402').")]
    public string FullNameAfterScan;
    [Tooltip("Edition / year string shown in scan result.")]
    public string EditionYear;

    // ── NOISE ON DROP ────────────────────────────────────────
    [Header("Noise on Drop")]
    [Tooltip("Noise level emitted when this object hits the floor at speed.")]
    public NiveauBruit DropNoiseLevel   = NiveauBruit.Fort;
    [Tooltip("Noise range (metres) when dropped hard.")]
    public float DropNoiseRange        = 8f;
}
