// ============================================================
// ObjetData.cs — Bailiff & Co  V2 (SIMPLIFIÉ)
// ScriptableObject décrivant un objet saisissable (valeur, physique, scan).
// Créer via : clic droit → Create → BailiffCo/ObjetData
//
// COLOCALISATION RECOMMANDÉE :
//   Models/Props/Vase/
//   ├── vase.fbx
//   ├── Vase.prefab       ← ValueObject.cs référence cet asset
//   └── VaseData.asset    ← cet asset, colocalisé avec le prefab
//
// CHANGEMENTS V3 (SIMPLIFIÉ) :
//   - ValueMin/ValueMax SUPPRIMÉS → une seule Value
//   - Plus simple, une source de vérité unique
//   - IsFragile supprimé (redondant avec IsBreakable)
//   - IsBreakable = propriété (peut-il casser ?)
//   - IsBroken = runtime state (est-il cassé ?) → sur ValueObject, pas ici
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
    [Tooltip("✅ Valeur fixe de cet objet (euros).")]
    public float Value = 1000f;

    // ── PHYSICS ──────────────────────────────────────────────
    [Header("Physics")]
    [Tooltip("Mass in kg — affects carry speed and throw velocity.")]
    public float WeightKg = 1f;
    [Min(0.1f)]
    public float SurfaceM2 = 0.25f;

    [Tooltip("If true, this object can be damaged/broken by impacts. If false, never breaks.")]
    public bool IsBreakable = true;

    [Tooltip("If true, requires two players to carry (coop).")]
    public bool RequiresTwoPlayers = false;

    [Tooltip("If true, cannot fit through standard doorways — blocks certain hiding spots.")]
    public bool IsOversized = false;

    // ── DAMAGE THRESHOLDS (only if IsBreakable = true) ───────
    [Header("Damage Thresholds (IsBreakable only)")]
    [Tooltip("Impact speed (m/s) below which no damage occurs.")]
    public float DamageImpactThreshold = 2f;

    // ── SCAN ─────────────────────────────────────────────────
    [Header("Scan / X-Ray")]
    [Tooltip("Full name revealed after scanning (e.g. 'Vase Ming Dynasty, ca. 1402').")]
    public string FullNameAfterScan;
    [Tooltip("Edition / year string shown in scan result.")]
    public string EditionYear;

    // ── NOISE ON DROP ────────────────────────────────────────
    [Header("Noise on Drop")]
    [Tooltip("Noise level emitted when this object hits the floor at speed.")]
    public NiveauBruit DropNoiseLevel = NiveauBruit.Fort;
    [Tooltip("Noise range (metres) when dropped hard.")]
    public float DropNoiseRange = 8f;
}