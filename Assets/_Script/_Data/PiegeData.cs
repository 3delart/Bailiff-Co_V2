// ============================================================
// PiegeData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un piège posable par le proprio.
// Créer via : clic droit → Create → BailiffCo/PiegeData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/PiegeData")]
public class PiegeData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string TrapName;
    public TypePiege TrapType;
    public Sprite UIIcon;
    public GameObject Prefab;
    [TextArea(1, 3)]
    public string Description;

    // ── DETECTION ────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Tag of the object that triggers this trap (e.g. 'Player').")]
    public string TriggerTag          = "Player";
    [Tooltip("Detection radius (metres) for proximity traps.")]
    public float  DetectionRadius     = 0.5f;
    [Tooltip("Whether the trap uses a trigger volume (true) or a raycast beam (false).")]
    public bool   UsesTriggerVolume   = true;

    // ── EFFECTS ON TRIGGER ───────────────────────────────────
    [Header("Effects on Trigger")]
    [Tooltip("Duration (seconds) the trap effect is active after triggering.")]
    public float  EffectDuration      = 5f;
    [Tooltip("Paranoia added to the owner when this trap is triggered.")]
    public float  ParanoiaBonusOnTrigger = 10f;
    [Tooltip("Whether triggering this trap alerts nearby neighbours.")]
    public bool   AlertsNeighbours    = false;
    [Tooltip("Whether triggering this trap calls the police (starts urgency timer).")]
    public bool   AlertsPolice        = false;
    [Tooltip("If AlertsPolice is true, seconds before the police arrive.")]
    public float  PoliceArrivalTime   = 120f;

    // ── PLAYER EFFECTS ───────────────────────────────────────
    [Header("Player Effects")]
    [Tooltip("Movement speed multiplier applied to the player while affected (1 = no effect).")]
    [Range(0f, 1f)]
    public float  PlayerSpeedMultiplier = 1f;
    [Tooltip("Whether the player is fully immobilised (override speed multiplier).")]
    public bool   ImmobilisesPlayer   = false;
    [Tooltip("Whether the trap makes the player drop their carried object.")]
    public bool   ForcesDrop          = false;
    [Tooltip("Whether the trap obscures the player's vision (smoke, gas, etc.).")]
    public bool   ObscuresVision      = false;

    // ── VISUAL / AUDIO ───────────────────────────────────────
    [Header("Visual / Audio")]
    [Tooltip("Description of the visual indicator shown in UV scan mode.")]
    [TextArea(1, 2)]
    public string UVIndicatorDescription;
    [Tooltip("Prefab for the UV indicator object placed in the scene.")]
    public GameObject UVIndicatorPrefab;
    [Tooltip("Sound played when the trap triggers.")]
    public AudioClip TriggerSound;
    [Tooltip("Sound played while the trap effect is active (loop).")]
    public AudioClip ActiveLoopSound;

    // ── COUNTERS ─────────────────────────────────────────────
    [Header("Counter-measures")]
    [Tooltip("Tool name required to disarm this trap (empty = cannot be disarmed).")]
    public string DisarmToolName;
    [Tooltip("Time (seconds) required to disarm the trap.")]
    public float  DisarmDuration      = 3f;
}
