// ============================================================
// ProprietaireData.cs — Bailiff & Co  V2
// ScriptableObject décrivant le propriétaire (IA, dossier joueur,
// comportement, animaux). Référencé par MissionData.
// Créer via : clic droit → Create → BailiffCo/ProprietaireData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/ProprietaireData")]
public class ProprietaireData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string OwnerName;
    public int    Age;
    public string Profession;
    public Sprite CartoonPortrait;
    public ProprietaireArchetypeType Archetype;
    public GameObject OwnerPrefab;

    // ── PLAYER DOSSIER ───────────────────────────────────────
    [Header("Player Dossier (shown in Hub)")]
    [TextArea(2, 4)]
    public string Hobbies;
    [TextArea(2, 5)]
    public string Backstory;
    public string PersonalityTrait;
    [Tooltip("A quote that hints at a hiding spot or behaviour.")]
    [TextArea(1, 3)]
    public string ClueQuote;
    [Tooltip("Security level 1–5, shown as stars in the Hub UI.")]
    [Range(1, 5)]
    public int SecurityLevel = 1;

    // ── ANIMALS ──────────────────────────────────────────────
    [Header("Animals")]
    [Tooltip("All animals present in this owner's home. Can be empty.")]
    public AnimalData[] Animals;

    // ── AI BEHAVIOUR ─────────────────────────────────────────
    [Header("AI Behaviour")]
    [Tooltip("Starting paranoia value (0–100) at mission start.")]
    [Range(0f, 100f)]
    public float StartingParanoia     = 0f;
    [Tooltip("Normal patrol/walk speed (m/s).")]
    public float NormalSpeed          = 2.5f;
    [Tooltip("Panic/chase speed (m/s).")]
    public float PanicSpeed           = 4.5f;
    [Tooltip("Vision range (metres) for detecting the player.")]
    public float VisionRange          = 8f;
    [Tooltip("Hearing range (metres) added on top of each noise event's own range.")]
    public float HearingBonus         = 5f;
    [Tooltip("Vision cone half-angle (degrees).")]
    [Range(10f, 180f)]
    public float VisionAngle          = 90f;

    // ── LEGAL / ESCALATION ───────────────────────────────────
    [Header("Legal / Escalation")]
    [Tooltip("If true, the owner automatically calls their lawyer when paranoia reaches LawyerCallThreshold.")]
    public bool  AutoCallsLawyer      = false;
    [Tooltip("Paranoia value at which the owner calls their lawyer.")]
    [Range(0f, 100f)]
    public float LawyerCallThreshold  = 76f;
    [Tooltip("Delay (seconds) between the owner arriving at the vehicle and forcing it open.")]
    public float VehicleForceDuration = 12f;

    // ── AUDIO ────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Clips played when the owner spots the player.")]
    public AudioClip[] SpotPlayerSounds;
    [Tooltip("Clips played when the owner is in Confront state.")]
    public AudioClip[] ConfrontSounds;
    [Tooltip("Clips played when the owner is panicking.")]
    public AudioClip[] PanicSounds;
}
