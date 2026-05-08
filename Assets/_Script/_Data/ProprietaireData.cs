// ============================================================
// ProprietaireData.cs — Bailiff & Co  V2 REFONTE
// ScriptableObject décrivant le propriétaire avec les 3 sliders
// du GDD (Réactivité, Méthode, Sociabilité).
//
// TOUTES les valeurs comportementales sont calculées automatiquement
// à partir de ces 3 sliders via des propriétés.
//
// Créer via : clic droit → Create → BailiffCo/ProprietaireData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/ProprietaireData")]
public class ProprietaireData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string OwnerName;
    public int Age;
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

    // ════════════════════════════════════════════════════════════
    // ══ 3 SLIDERS — SOURCE DE VÉRITÉ (1–10) ═══════════════════
    // ════════════════════════════════════════════════════════════

    [Header("Personality Core Traits (1–10)")]
    [Tooltip("Réactivité (1–10): How quickly the owner reacts to stimuli.\n" +
             "1 = slow, forgiving, takes time to notice.\n" +
             "10 = hypervigilant, instant reaction.")]
    [Range(1, 10)]
    public int Reactivity = 5;

    [Tooltip("Méthode (1–10): How methodical and prepared the owner is.\n" +
             "1 = improvises, disorganized, few traps.\n" +
             "10 = meticulous, many traps, everything planned.")]
    [Range(1, 10)]
    public int Method = 5;

    [Tooltip("Sociabilité (1–10): How social/connected the owner is.\n" +
             "1 = isolated, no reinforcements.\n" +
             "10 = calls friends, lawyer, neighbours quickly.")]
    [Range(1, 10)]
    public int Sociability = 5;

    // ════════════════════════════════════════════════════════════
    // ══ CALCULATED PROPERTIES — READ-ONLY ═════════════════════
    // ════════════════════════════════════════════════════════════

    // ── PARANOIA START ───────────────────────────────────────
    /// <summary>Starting paranoia (0–100). Higher Reactivity = higher start.</summary>
    public float StartingParanoia => Mathf.Lerp(0f, 30f, (Reactivity - 1) / 9f);

    // ── AI SPEEDS ────────────────────────────────────────────
    /// <summary>Normal patrol speed (m/s). Higher Reactivity = slightly faster.</summary>
    public float NormalSpeed => Mathf.Lerp(2.0f, 3.0f, (Reactivity - 1) / 9f);

    /// <summary>Panic speed (m/s). Higher Reactivity = much faster.</summary>
    public float PanicSpeed => Mathf.Lerp(3.5f, 5.5f, (Reactivity - 1) / 9f);

    // ── VISION & HEARING ─────────────────────────────────────
    /// <summary>Vision range (metres). Higher Reactivity = better sight.</summary>
    public float VisionRange => Mathf.Lerp(6f, 12f, (Reactivity - 1) / 9f);

    /// <summary>Hearing bonus range (metres). Higher Reactivity = better hearing.</summary>
    public float HearingBonus => Mathf.Lerp(3f, 8f, (Reactivity - 1) / 9f);

    /// <summary>Vision cone half-angle (degrees). Higher Reactivity = wider cone.</summary>
    public float VisionAngle => Mathf.Lerp(70f, 110f, (Reactivity - 1) / 9f);

    // ── TRAPS ────────────────────────────────────────────────
    /// <summary>Number of traps prepared before mission start. Higher Method = more traps.</summary>
    public int PreplacedTrapsCount => Mathf.RoundToInt(Mathf.Lerp(0f, 5f, (Method - 1) / 9f));

    /// <summary>Maximum number of traps the owner can place during mission. Higher Method = more dynamic traps.</summary>
    public int MaxDynamicTrapsCount => Mathf.RoundToInt(Mathf.Lerp(0f, 3f, (Method - 1) / 9f));

    // ── VEHICLE FORCING ──────────────────────────────────────
    /// <summary>Time (seconds) the owner needs to force the vehicle trunk open. Higher Method = faster.</summary>
    public float VehicleForceDuration => Mathf.Lerp(20f, 8f, (Method - 1) / 9f);

    // ── REINFORCEMENTS ───────────────────────────────────────
    /// <summary>Whether the owner automatically calls their lawyer. Higher Sociability = yes.</summary>
    public bool AutoCallsLawyer => Sociability >= 7;

    /// <summary>Paranoia threshold at which lawyer is called. Higher Sociability = earlier call.</summary>
    public float LawyerCallThreshold => Mathf.Lerp(90f, 50f, (Sociability - 1) / 9f);

    /// <summary>Number of friends called for backup. Higher Sociability = more friends.</summary>
    public int FriendsCount
    {
        get
        {
            if (Sociability <= 3) return 0;
            if (Sociability <= 6) return 1;
            return Mathf.RoundToInt(Mathf.Lerp(2f, 3f, (Sociability - 7) / 3f));
        }
    }

    /// <summary>Delay (seconds) before friends arrive after being called. Higher Sociability = faster arrival.</summary>
    public float FriendsArrivalDelay => Mathf.Lerp(120f, 30f, (Sociability - 1) / 9f);

    // ── PARANOIA THRESHOLDS (GDD §4.2) ───────────────────────
    // These are constant across all owners, but documented here for reference
    public const float PALIER_CALM      = 0f;
    public const float PALIER_MEFIANT   = 11f;
    public const float PALIER_INQUIET   = 26f;
    public const float PALIER_PANIQUE   = 51f;
    public const float PALIER_FURIEUX   = 76f;
    public const float PALIER_OBSESSION = 91f;

    // ── AUDIO ────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Clips played when the owner spots the player.")]
    public AudioClip[] SpotPlayerSounds;
    [Tooltip("Clips played when the owner is in Confront state.")]
    public AudioClip[] ConfrontSounds;
    [Tooltip("Clips played when the owner is panicking.")]
    public AudioClip[] PanicSounds;
}