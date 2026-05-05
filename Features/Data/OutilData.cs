// ============================================================
// OutilData.cs — Bailiff & Co  V2
// ScriptableObject décrivant un outil (permanent ou consommable).
// Contient les stats de TOUS les niveaux et les coûts d'upgrade.
// Le niveau ACTUEL du joueur est géré à runtime dans InventaireSystem.
//
// Exemple — HammerData :
//   Level 1 : BreakForce = 10,  UpgradeCost = 500
//   Level 2 : BreakForce = 20,  UpgradeCost = 1500
//   Level 3 : BreakForce = 35,  UpgradeCost = 0 (max)
//
// Créer via : clic droit → Create → BailiffCo/OutilData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/OutilData")]
public class OutilData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string ToolName;
    [TextArea(1, 3)]
    public string Description;
    public Sprite UIIcon;
    public GameObject Prefab;
    public ToolCategory Category;

    // ── ACQUISITION ──────────────────────────────────────────
    [Header("Acquisition")]
    [Tooltip("Purchase price in the Hub shop (Level 1).")]
    public int  PurchasePrice         = 0;
    [Tooltip("If true, the tool is given to the player for free at the start.")]
    public bool IsStartingTool        = false;
    [Tooltip("Mission number after which this tool becomes available in the shop. 0 = always available.")]
    public int  UnlocksAfterMission   = 0;

    // ── LEVELS ───────────────────────────────────────────────
    [Header("Levels (max 3 — leave unused levels empty)")]
    [Tooltip("Stats and upgrade cost for each level. Index 0 = Level 1, Index 2 = Level 3 (max).")]
    public ToolLevel[] Levels = new ToolLevel[3];

    // ── USAGE ────────────────────────────────────────────────
    [Header("Usage")]
    [Tooltip("Whether this tool is consumed on use (false = reusable permanently).")]
    public bool IsConsumable          = false;
    [Tooltip("For consumables: stack size sold in shop.")]
    public int  ShopStackSize         = 1;
    [Tooltip("Key action that triggers this tool (mapped in OptionsData).")]
    public ActionJeu UseAction        = ActionJeu.Interagir;

    // ── EFFECTS (shared — actual values come from current ToolLevel) ──
    [Header("Effect Type")]
    [Tooltip("What this tool does — used by the runtime system to dispatch the correct logic.")]
    public ToolEffectType EffectType  = ToolEffectType.None;
}

// ============================================================
// TOOL LEVEL — data for one upgrade tier
// ============================================================
[System.Serializable]
public class ToolLevel
{
    [Tooltip("Display name for this level (e.g. 'Crowbar Lv.2').")]
    public string LevelName;

    [TextArea(1, 2)]
    [Tooltip("Short description of what changes at this level.")]
    public string EffectDescription;

    [Tooltip("Cost (€) to upgrade FROM the previous level to this one. 0 = free / max level.")]
    public int UpgradeCost            = 0;

    // ── GENERIC NUMERIC VALUES ───────────────────────────────
    // These fields are shared across all tool types.
    // Fill only the ones relevant to your tool; leave others at 0.

    [Header("Numeric Values")]
    [Tooltip("General effect radius / zone of effect (metres). E.g. spray range, scan radius.")]
    public float ZoneEffect           = 0f;

    [Tooltip("Duration of the effect (seconds). E.g. how long a spray lasts, a lock hold.")]
    public float EffectDuration       = 0f;

    [Tooltip("Force / power value. E.g. break force of a crowbar, tranq dart strength.")]
    public float Power                = 0f;

    [Tooltip("Paranoia modifier applied to the owner when this tool is used (negative = reduces paranoia).")]
    public float ParanoiaModifier     = 0f;

    [Tooltip("Speed multiplier applied to the player while using this tool (1 = no effect).")]
    [Range(0f, 2f)]
    public float PlayerSpeedMultiplier = 1f;

    [Tooltip("Number of charges / uses for this level. 0 = unlimited.")]
    public int   Charges              = 0;

    [Tooltip("Cooldown (seconds) between uses.")]
    public float Cooldown             = 0f;
}

// ============================================================
// ENUMS (tool-specific — kept here to avoid polluting Enums.cs)
// ============================================================

public enum ToolCategory
{
    ForceEntry,     // Crowbar, lockpick, battering ram
    Stealth,        // Spray, silencer, soft shoes
    Legal,          // Badge, warrant, phone
    Scanner,        // UV lamp, X-ray
    Immobiliser,    // Handcuffs, tranq dart, cage
    Consumable,     // Spray cans, zip ties
    Utility         // Backpack, trolley, etc.
}

public enum ToolEffectType
{
    None,
    ForceDoor,          // Forces open locked doors/drawers
    Lockpick,           // Silent forced entry, skill-based
    ReduceParanoia,     // Badge, warrant — legally reduces paranoia
    ImmobiliseOwner,    // Handcuffs, tranq dart
    ScanUV,             // Reveals UV indicators
    ScanXRay,           // Reveals hidden objects through walls
    SprayAnimal,        // Calms or distracts an animal
    SpraySilence,       // Silences a room (reduces noise emission)
    ExpandCarryCapacity // Backpack, trolley
}
