// ============================================================
// VoisinData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un voisin/renfort appelé par le
// proprio ou spawné par MissionBuilder.
// Créer via : clic droit → Create → BailiffCo/NeighbourData
//
// RÔLES :
//   - Ami : renforce la surveillance, patrouille avec le proprio
//   - Avocat : fait obstruction légale, ralentit le joueur
//   - Curieux : observe depuis la rue, alerte le proprio si suspect
//   - Voleur : force le coffre du véhicule et vole un objet
//   - Livreur : sonne à la porte, interpelle le proprio
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/NeighbourData")]
public class NeighbourData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string NeighbourName;
    public NeighbourRole Role;
    public Sprite CartoonPortrait;
    public GameObject NeighbourPrefab;
    [TextArea(1, 3)]
    public string Description;

    // ── APPEARANCE TIMING ────────────────────────────────────
    [Header("Appearance Timing")]
    [Tooltip("When this neighbour can be summoned. E.g. Friend = called by owner at Paranoia 50+.")]
    public NeighbourTrigger AppearanceTrigger;
    [Tooltip("Delay (seconds) before this neighbour arrives after being called.")]
    public float ArrivalDelay         = 30f;
    [Tooltip("How long (seconds) this neighbour stays active. 0 = until mission end.")]
    public float StayDuration         = 0f;

    // ── MOVEMENT ─────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("Walking speed (m/s).")]
    public float NormalSpeed          = 2.5f;
    [Tooltip("Whether this neighbour can patrol or just stays in one spot.")]
    public bool CanPatrol             = false;
    [Tooltip("Detection range (metres) for spotting the player.")]
    public float VisionRange          = 8f;
    [Tooltip("Vision cone half-angle (degrees).")]
    [Range(10f, 180f)]
    public float VisionAngle          = 90f;

    // ── ROLE-SPECIFIC: FRIEND ────────────────────────────────
    [Header("Friend-Specific")]
    [Tooltip("Paranoia bonus given to the owner when this friend spots the player.")]
    public float FriendSpottedBonus   = 10f;

    // ── ROLE-SPECIFIC: LAWYER ────────────────────────────────
    [Header("Lawyer-Specific")]
    [Tooltip("Duration (seconds) the lawyer can legally block the player.")]
    public float LawyerBlockDuration  = 15f;
    [Tooltip("Whether the lawyer follows the owner or stays near the vehicle.")]
    public bool LawyerFollowsOwner    = true;

    // ── ROLE-SPECIFIC: CURIOUS ───────────────────────────────
    [Header("Curious-Specific")]
    [Tooltip("Whether the curious neighbour alerts the owner immediately or after observing.")]
    public bool CuriousAlertsOwner    = true;
    [Tooltip("Time (seconds) the curious neighbour observes before alerting.")]
    public float CuriousObserveDuration = 5f;

    // ── ROLE-SPECIFIC: THIEF ─────────────────────────────────
    [Header("Thief-Specific")]
    [Tooltip("Time (seconds) required to force the vehicle trunk open.")]
    public float ThiefForceTrunkTime  = 8f;
    [Tooltip("Whether the thief takes only one object or all accessible objects.")]
    public bool ThiefTakesAll         = false;

    // ── ROLE-SPECIFIC: DELIVERY ──────────────────────────────
    [Header("Delivery-Specific")]
    [Tooltip("Time (seconds) the delivery person waits at the door before leaving.")]
    public float DeliveryWaitTime     = 10f;
    [Tooltip("Whether ringing the doorbell alerts the owner immediately.")]
    public bool DeliveryRingsOwner    = true;

    // ── AUDIO ────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Clips played when this neighbour spots the player.")]
    public AudioClip[] SpotPlayerSounds;
    [Tooltip("Clips played when this neighbour interacts with the owner.")]
    public AudioClip[] InteractionSounds;
}

// ============================================================
// ENUMS — kept here to avoid polluting Enums.cs
// ============================================================

public enum NeighbourRole
{
    Friend,     // Renforcement: patrols with owner
    Lawyer,     // Legal obstruction: blocks player actions
    Curious,    // Observer: alerts owner from the street
    Thief,      // Opportunist: forces vehicle trunk
    Delivery    // Neutral: rings doorbell, alerts owner
}

public enum NeighbourTrigger
{
    OnMissionStart,      // Spawned at mission start (e.g. curious neighbour)
    OnOwnerCall,         // Summoned by owner (e.g. friend, lawyer)
    OnQuotaThreshold,    // Spawned when quota reaches X% (e.g. thief)
    OnTimerExpiry        // Spawned after X minutes (e.g. delivery)
}