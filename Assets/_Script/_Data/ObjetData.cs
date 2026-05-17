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
    public NoiseLevel DropNoiseLevel = NoiseLevel.Loud;
    [Tooltip("Noise range (metres) when dropped hard.")]
    public float DropNoiseRange = 8f;

    // ── BREAK PROFILE ────────────────────────────────────────
    [Header("Break Profile")]
    [Tooltip("How this object breaks: Shatters (fragments), Deforms (deformation+texture), or Scratches (barely visible).")]
    public BreakType BreakType = BreakType.Deforms;

    [Tooltip("Multiplies the damage from the velocity step table. Fragile=5-10, Normal=1, Solid=0.1-0.2")]
    [Range(0.05f, 15f)]
    public float DamageMultiplier = 1f;

    [Tooltip("Randomness factor for durability. 0=deterministic, 1=max variance (±50% damage).")]
    [Range(0f, 1f)]
    public float DurabilityVariance = 0.2f;

    [Tooltip("Can this object be toppled/knocked over by player collision?")]
    public bool CanTopple = false;

    [Tooltip("Minimum impact velocity (m/s) to trigger topple behavior.")]
    [Range(0.5f, 5f)]
    public float ToppleVelocityThreshold = 1.5f;

    // ── SHATTER PROFILE (BreakType == Shatters) ──────────
    [Header("Shatter Profile (if BreakType=Shatters)")]
    [Tooltip("Prefab of shattered/broken fragments. Instantiated when object breaks.")]
    public GameObject BrokenVariant;

    [Tooltip("Minimum scatter speed (m/s) applied to each fragment. ForceMode.VelocityChange — mass-independent.")]
    [Range(0f, 5f)]
    public float ShatterForceMin = 2f;

    [Tooltip("Maximum scatter speed (m/s) applied to each fragment. ForceMode.VelocityChange — mass-independent.")]
    [Range(0f, 10f)]
    public float ShatterForceMax = 4f;

    [Tooltip("Linear drag applied to fragments on spawn. 0=Unity default (slides forever). 1.5=realistic deceleration.")]
    [Range(0f, 5f)]
    public float FragmentDrag = 1.5f;

    // ── DEFORM PROFILE (BreakType == Deforms) ──────────
    [Header("Deform Profile (if BreakType=Deforms)")]
    [Tooltip("Material applied when object is damaged (cracked screen, dented, etc.).")]
    public Material DamagedMaterial;

    [Tooltip("Target blend shape weight when fully damaged (0-100). 0=no shape key animation.")]
    [Range(0f, 100f)]
    public float DamagedBlendShapeWeight = 0f;

    // ── AUDIO ────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Material type for audio feedback (assigned during audio implementation phase).")]
    public SoundMaterial SoundMaterial = SoundMaterial.Ceramic;

    [Tooltip("Audio clip played when object breaks (assigned during audio phase).")]
    public AudioClip BreakSound;
}