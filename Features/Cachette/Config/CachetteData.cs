// ============================================================
// CachetteData.cs — Bailiff & Co  V2
// ScriptableObject décrivant une cachette d'objet dans le niveau.
// Référencé par MissionData pour le spawn procédural.
// Créer via : clic droit → Create → BailiffCo/CachetteData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/CachetteData")]
public class CachetteData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string HidingSpotName;
    public TypeCachette HidingType;
    public Sprite UIIcon;
    public GameObject Prefab;
    [TextArea(1, 3)]
    public string Description;

    // ── SPAWN RULES ──────────────────────────────────────────
    [Header("Spawn Rules")]
    [Tooltip("Tags of scene locations where this hiding spot can be placed (e.g. 'WallSlot', 'FloorSlot').")]
    public string[] ValidSpawnTags;
    [Tooltip("Whether the player can move this hiding spot (e.g. a garden gnome).")]
    public bool IsMovable             = false;
    [Tooltip("Maximum number of objects that can be hidden inside.")]
    public int  MaxObjectCapacity     = 1;

    // ── DETECTION ────────────────────────────────────────────
    [Header("Detection")]
    [Tooltip("Base difficulty for the owner to detect this hiding spot (0 = never noticed, 1 = always noticed).")]
    [Range(0f, 1f)]
    public float OwnerDetectionChance = 0.1f;
    [Tooltip("Whether this hiding spot is revealed by UV scan tool.")]
    public bool  RevealedByUVScan     = true;
    [Tooltip("Whether this hiding spot requires the X-Ray scanner to be located.")]
    public bool  RequiresXRayScan     = false;

    // ── INTERACTION ──────────────────────────────────────────
    [Header("Interaction")]
    [Tooltip("Sound played when the hiding spot is opened.")]
    public AudioClip OpenSound;
    [Tooltip("Sound played when the hiding spot is closed.")]
    public AudioClip CloseSound;
    [Tooltip("Whether opening this spot makes noise that the owner can hear.")]
    public bool  MakesNoiseOnOpen     = false;
    [Tooltip("Noise level emitted when opened (if MakesNoiseOnOpen = true).")]
    public NiveauBruit OpenNoiseLevel = NiveauBruit.Leger;
    [Tooltip("Noise range (metres) when opened.")]
    public float OpenNoiseRange       = 4f;

    // ── UV INDICATOR ─────────────────────────────────────────
    [Header("UV Indicator")]
    [Tooltip("Prefab placed at the hiding spot location, only visible in UV scan mode.")]
    public GameObject UVIndicatorPrefab;
    [TextArea(1, 2)]
    public string UVIndicatorDescription;
}
