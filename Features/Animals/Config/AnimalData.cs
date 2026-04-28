// ============================================================
// AnimalData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un animal présent chez le proprio.
// Certains champs ne s'appliquent qu'à certaines espèces —
// ils sont groupés par section dans l'Inspector.
// Créer via : clic droit → Create → BailiffCo/AnimalData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/AnimalData")]
public class AnimalData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string AnimalName;
    public AnimalEspece Species;
    public Sprite UIIcon;
    public GameObject Prefab;
    [TextArea(1, 3)]
    public string FlavorDescription;

    // ── DETECTION ────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Detection radius (metres) — animal reacts if player enters this range.")]
    public float DetectionRange        = 5f;
    [Tooltip("Whether the animal detects the player visually (line of sight).")]
    public bool  UseLineOfSight        = false;
    [Tooltip("Detection cone half-angle in degrees (if UseLineOfSight = true).")]
    [Range(10f, 180f)]
    public float VisionAngle           = 90f;
    [Tooltip("Whether the animal reacts to sounds emitted by OnBruitEmis.")]
    public bool  ReactsToSound         = true;
    [Tooltip("Minimum noise level the animal reacts to.")]
    public NiveauBruit MinNoiseLevel   = NiveauBruit.Leger;
    [Tooltip("Delay (seconds) before the animal reacts after detecting the player.")]
    public float ReactionDelay         = 0.5f;

    // ── ALERT BEHAVIOUR ──────────────────────────────────────
    [Header("Alert Behaviour")]
    [Tooltip("Whether this animal emits an alert noise (bark, meow, squawk…).")]
    public bool  CanMakeNoise           = true;
    [Tooltip("Noise level of the alert cry.")]
    public NiveauBruit AlertNoiseLevel  = NiveauBruit.Fort;
    [Tooltip("Alert noise range (metres).")]
    public float AlertNoiseRange        = 10f;
    [Tooltip("How much paranoia is added to the owner per alert bark/cry.")]
    public float ParanoiaBonusPerAlert  = 5f;
    [Tooltip("Intensity used in OnAnimalAboie (0 = chihuahua, 1 = guard dog).")]
    [Range(0f, 1f)]
    public float AlertIntensity         = 0.5f;
    [Tooltip("Minimum seconds between two consecutive alert cries.")]
    public float AlertCooldown          = 3f;

    // ── AUDIO ────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("Clips played when the animal alerts (bark, meow, etc.).")]
    public AudioClip[] AlertSounds;
    [Tooltip("Clips played when idle / ambient.")]
    public AudioClip[] IdleSounds;
    [Tooltip("Minimum interval (seconds) between idle sounds.")]
    public float IdleSoundIntervalMin   = 20f;
    [Tooltip("Maximum interval (seconds) between idle sounds.")]
    public float IdleSoundIntervalMax   = 60f;

    // ── MOVEMENT (active animals) ────────────────────────────
    [Header("Movement")]
    [Tooltip("Whether the animal can move / patrol (false = fish, tortoise, etc.).")]
    public bool  CanMove                = false;
    [Tooltip("Normal patrol speed (m/s).")]
    public float PatrolSpeed            = 1.5f;
    [Tooltip("Chase speed when following player (m/s). 0 = does not chase.")]
    public float ChaseSpeed             = 0f;
    [Tooltip("Whether the animal follows/chases the player when alerted.")]
    public bool  ChasesPlayer           = false;

    // ── CAGE / CONTAINMENT ───────────────────────────────────
    [Header("Cage / Containment")]
    [Tooltip("Can this animal be placed in the vehicle cage by the player?")]
    public bool  CanBeCaged             = false;
    [Tooltip("Bonus paranoia modifier when the owner notices the caged animal.")]
    public float OwnerNoticeCagedParanoia = 10f;

    // ── PARROT SPECIFIC ──────────────────────────────────────
    [Header("Parrot / Talking Bird (ignored for other species)")]
    [Tooltip("Whether this animal can speak phrases (OnPerroquetParle).")]
    public bool  CanSpeak               = false;
    [Tooltip("Phrases the parrot may randomly say. Some may be clues.")]
    public ParrotPhrase[] Phrases;
    [Tooltip("Minimum interval (seconds) between two random phrases.")]
    public float PhraseIntervalMin      = 30f;
    [Tooltip("Maximum interval (seconds) between two random phrases.")]
    public float PhraseIntervalMax      = 90f;

    // ── GUARD DOG SPECIFIC ───────────────────────────────────
    [Header("Guard Dog (ignored for other species)")]
    [Tooltip("If true, can be released by the owner to roam the level.")]
    public bool  CanBeReleasedByOwner   = false;
    [Tooltip("Once released, how long before the dog is re-leashed (seconds). 0 = never.")]
    public float ReleaseMaxDuration     = 60f;
    [Tooltip("Paranoia added to the owner when the guard dog is alerted.")]
    public float GuardDogOwnerParanoia  = 15f;

    // ── LITTER BOX (cat) ─────────────────────────────────────
    [Header("Litter Box (Cat only — ignored for other species)")]
    [Tooltip("Whether this cat uses a litter box that can be used as a hiding spot (CachetteDef).")]
    public bool  HasLitterBox           = false;
}

// ============================================================
// PARROT PHRASE — inline struct for Inspector
// ============================================================
[System.Serializable]
public class ParrotPhrase
{
    [Tooltip("The phrase displayed on screen and sent via OnPerroquetParle.")]
    public string Text;
    [Tooltip("If true, this phrase is a gameplay clue (e.g. hints at a hiding spot).")]
    public bool   IsClue;
    [Tooltip("Probability weight relative to other phrases (higher = more frequent).")]
    [Range(1, 10)]
    public int    Weight = 1;
}
