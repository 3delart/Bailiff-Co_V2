// ============================================================
// VehiculeData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un véhicule (coffre, cage, sons,
// ambiance, capacité). Référencé par MissionData et HubManager.
// Créer via : clic droit → Create → BailiffCo/VehiculeData
//
// COLOCALISATION RECOMMANDÉE :
//   Models/Vehicles/Fourgon/
//   ├── fourgon.fbx
//   ├── Fourgon.prefab
//   └── FourgonData.asset     ← cet asset
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/VehiculeData")]
public class VehiculeData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string VehicleName;
    public TypeVehicule VehicleType;
    [TextArea(2, 4)]
    public string Description;
    public Sprite UIIllustration;
    public GameObject Prefab;

    // ── HUB — RENTAL ─────────────────────────────────────────
    [Header("Hub — Rental")]
    [Tooltip("Rental price per mission. 0 = free (e.g. cargo bike).")]
    public float RentalPrice          = 0f;
    [Tooltip("Mission number after which this vehicle becomes available. 0 = always.")]
    public int   UnlocksAfterMission  = 0;

    // ── HUB — UI DESCRIPTIONS ────────────────────────────────
    [Header("Hub — UI Descriptions")]
    [TextArea(1, 3)]
    public string AdvantageDescription;
    [TextArea(1, 3)]
    public string DisadvantageDescription;
    [TextArea(1, 2)]
    public string TipDescription;

    // ── TRUNK CAPACITY ───────────────────────────────────────
    [Header("Trunk Capacity")]
    [Tooltip("Maximum number of objects that can be loaded into this vehicle.")]
    public int   ObjectCapacity        = 6;
    [Tooltip("Duration (seconds) to close the trunk door. 0 = instant.")]
    public float TrunkCloseDuration    = 0.5f;
    [Tooltip("Time (seconds) the owner needs to force the trunk open. 0 = instant.")]
    public float OwnerForceTrunkTime   = 12f;

    // ── ANIMAL CAGE ──────────────────────────────────────────
    [Header("Animal Cage")]
    [Tooltip("Whether this vehicle has an animal cage.")]
    public bool  HasAnimalCage         = false;
    [Tooltip("Maximum number of animals the cage can hold.")]
    public int   CageCapacity          = 1;

    // ── NOISE & VISIBILITY ───────────────────────────────────
    [Header("Noise & Visibility")]
    [Tooltip("Whether this vehicle is highly visible / suspicious (affects owner and neighbour behaviour).")]
    public bool  IsConspicuous         = false;
    [Tooltip("Noise level emitted when the vehicle engine is running (heard by neighbours).")]
    public NiveauBruit EngineNoiseLevel = NiveauBruit.Leger;
    [Tooltip("Engine noise range (metres).")]
    public float EngineNoiseRange      = 15f;

    // ── AUDIO — TRUNK ─────────────────────────────────────────
    [Header("Audio — Trunk")]
    public AudioClip TrunkOpenSound;
    public AudioClip TrunkCloseSound;

    // ── AUDIO — AMBIENT SPECIAL SOUNDS ───────────────────────
    [Header("Audio — Ambient Special Sounds")]
    [Tooltip("Random special sounds played during the mission (bray, ice-cream jingle, rotor…). Leave empty for none.")]
    public AudioClip[] SpecialSounds;
    [Tooltip("Minimum interval (seconds) between special sounds.")]
    public float SpecialSoundIntervalMin = 90f;
    [Tooltip("Maximum interval (seconds) between special sounds.")]
    public float SpecialSoundIntervalMax = 150f;
    [Tooltip("Noise range (metres) emitted when a special sound plays.")]
    public float SpecialSoundNoiseRange  = 12f;
}
