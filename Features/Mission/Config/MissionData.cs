// ============================================================
// MissionData.cs — Bailiff & Co  V2
// ScriptableObject central d'une mission. Référencé par
// GameManager et MissionBuilder pour construire la scène.
// Créer via : clic droit → Create → BailiffCo/MissionData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/MissionData")]
public class MissionData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string MissionName;
    [Tooltip("Sequential mission number used for progression unlocks.")]
    public int    MissionNumber;
    [TextArea(2, 4)]
    public string BriefingText;
    public Sprite MissionThumbnail;

    // ── OWNER ────────────────────────────────────────────────
    [Header("Owner")]
    public ProprietaireData Owner;

    // ── LEVEL CONSTRUCTION (MissionBuilder) ──────────────────
    [Header("Level Construction")]
    [Tooltip("Prefab of the building to instantiate at the MaisonAnchor point.")]
    public GameObject HousePrefab;
    [Tooltip("Prefab of the owner character (skin + AI config).")]
    public GameObject OwnerPrefab;

    // ── OBJECTS TO SEIZE ─────────────────────────────────────
    [Header("Objects to Seize")]
    [Tooltip("List of objects that can be seized in this mission.")]
    public SeizableObjectEntry[] SeizableObjects;

    // ── QUOTA ────────────────────────────────────────────────
    [Header("Quota")]
    [Tooltip("Minimum total value (€) required to validate the mission. 0 = auto (50% of max possible).")]
    public float MinimumQuotaValue    = 0f;

    // ── SEED ─────────────────────────────────────────────────
    [Header("Seed")]
    [Tooltip("Fixed seed for deterministic object placement. 0 = random each run.")]
    public int FixedSeed              = 0;

    // ── TRAPS & HIDING SPOTS ─────────────────────────────────
    [Header("Traps & Hiding Spots")]
    [Tooltip("Traps the owner may place during the mission (randomly selected with seed).")]
    public PiegeData[]    PossibleTraps;
    [Tooltip("Maximum number of traps active simultaneously.")]
    public int            MaxActiveTraps = 3;
    [Tooltip("Hiding spots available in this mission layout.")]
    public CachetteData[] PossibleHidingSpots;

    // ── SCORE CONDITIONS ─────────────────────────────────────
    [Header("Score Conditions")]
    [Tooltip("Time bonus threshold (seconds) — finishing under this time contributes to star rating.")]
    public float BonusTimeThresholdSeconds = 600f;
    [Tooltip("Max broken objects still allowing 2 stars.")]
    public int   MaxBrokenObjectsFor2Stars = 3;
    [Tooltip("Value multiplier required for 3 stars (e.g. 2 = recover twice the quota).")]
    public float ValueMultiplierFor3Stars  = 2f;

    // ── AMBIANCE ─────────────────────────────────────────────
    [Header("Ambiance")]
    [Tooltip("Skybox material applied at mission start. Null = default.")]
    public Material SkyboxOverride;
    [Tooltip("Fog colour applied at mission start.")]
    public Color    FogColor           = Color.gray;
    [Tooltip("Fog density applied at mission start.")]
    [Range(0f, 0.1f)]
    public float    FogDensity         = 0.01f;
    [Tooltip("Music track played during the mission.")]
    public AudioClip MissionMusic;
    [Tooltip("Volume of the mission music (0–1).")]
    [Range(0f, 1f)]
    public float     MusicVolume       = 0.7f;
}

// ============================================================
// SEIZABLE OBJECT ENTRY
// Wraps ObjetData with spawn-specific overrides.
// ============================================================
[System.Serializable]
public class SeizableObjectEntry
{
    [Tooltip("The object definition.")]
    public ObjetData ObjectData;

    [Tooltip("How many of this object can appear in the mission (chosen randomly with seed).")]
    public int   MinCount             = 1;
    public int   MaxCount             = 1;

    [Tooltip("Override minimum value for this mission. 0 = use ObjectData.ValueMin.")]
    public float ValueMinOverride     = 0f;
    [Tooltip("Override maximum value for this mission. 0 = use ObjectData.ValueMax.")]
    public float ValueMaxOverride     = 0f;

    [Tooltip("Spawn weight relative to other entries (higher = more likely to appear when count is limited).")]
    [Range(1, 10)]
    public int   SpawnWeight          = 5;

    [Tooltip("If true, this object is guaranteed to spawn regardless of seed.")]
    public bool  IsGuaranteed         = false;
}
