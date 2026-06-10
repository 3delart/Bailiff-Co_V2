// ============================================================
// ItemData.cs — Bailiff & Co  V2
// Base commune à tous les items équipables (outils & consommables).
// Hiérarchie typée (remplace l'ex-OutilData fourre-tout) :
//   ItemData (abstrait)
//     ├─ ToolData        (permanent, upgradable — Levels[])
//     └─ ConsumableData  (consommable — stock + EffectStats à plat)
//
// IDENTITÉ : chaque item a un `Id` STABLE (ex "crowbar") utilisé comme
// clé partout (inventaire, loadout, casier). Le DisplayName ne sert qu'à
// l'affichage et peut changer/être localisé sans rien casser.
// ============================================================
using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    [Tooltip("Clé stable et unique (ex: 'crowbar'). NE JAMAIS renommer après création — c'est la clé d'inventaire.")]
    public string Id;
    [Tooltip("Nom affiché (UI). Peut être renommé/localisé librement.")]
    public string DisplayName;
    [TextArea(1, 3)]
    public string Description;
    public Sprite Icon;
    [Tooltip("Modèle 3D tenu en main quand l'item est sélectionné dans la roue.")]
    public GameObject HandPrefab;

    // ── ACQUISITION ──────────────────────────────────────────
    [Header("Acquisition")]
    [Tooltip("Prix d'achat (boutique). Pour un outil = prix niveau 1 ; pour un conso = prix unitaire.")]
    public int  PurchasePrice       = 0;
    [Tooltip("Numéro de mission après lequel l'item devient disponible. 0 = toujours dispo.")]
    public int  UnlocksAfterMission = 0;
    [Tooltip("Donné gratuitement au joueur au démarrage.")]
    public bool IsStartingItem      = false;

    // ── USAGE ────────────────────────────────────────────────
    [Header("Usage")]
    [Tooltip("Comment l'item s'utilise une fois en main (route le comportement runtime).")]
    public ToolUsageMode UsageMode  = ToolUsageMode.None;

    // ── ANIMATION ────────────────────────────────────────────
    [Header("Animation")]
    [Tooltip("Nombre de mains pour tenir l'item — choisit la pose de tenue générique du haut du corps.")]
    public HandCount HandCount = HandCount.OneHand;
    [Tooltip("Clip d'usage joué sur le haut du corps (forçage, scan, poser…). Null = tenue seule (ex: lampe).")]
    public AnimationClip UseAnimation;
    [Tooltip("Pose de tenue spécifique (optionnel) — sinon pose générique selon HandCount.")]
    public AnimationClip HoldPose;

    [Header("Tenue — placement de l'objet (relatif à l'ancre PointPortage)")]
    [Tooltip("Décalage de position de l'objet tenu (hauteur/profondeur/latéral).")]
    public Vector3 HoldOffset;
    [Tooltip("Rotation (euler) de l'objet tenu — inclure la correction d'orientation du mesh.")]
    public Vector3 HoldEuler;

    [Header("Tenue — offset des mains (relatif aux grips)")]
    [Tooltip("Ajuste la main DROITE par rapport au point GripRight (sans toucher au prefab).")]
    public GripPose GripRight;
    [Tooltip("Ajuste la main GAUCHE par rapport au point GripLeft (objets 2 mains).")]
    public GripPose GripLeft;
}

/// <summary>Décalage d'une main par rapport à son point de grip (pour régler l'IK via la data).</summary>
[System.Serializable]
public struct GripPose
{
    [Tooltip("Décalage de position du poignet par rapport au grip (espace local du grip).")]
    public Vector3 PositionOffset;
    [Tooltip("Décalage de rotation (euler) de la main par rapport au grip.")]
    public Vector3 EulerOffset;
}

// ============================================================
// EFFECT STATS — jeu de paramètres tunables PARTAGÉ (DRY)
// Embarqué PAR NIVEAU dans ToolLevel (outils upgradables)
// ET À PLAT dans ConsumableData (conso non-upgradables).
// Chaque item ne remplit que les champs qui le concernent.
// Les behaviours lisent ces valeurs (jamais de constante codée en dur).
// ============================================================
[System.Serializable]
public struct EffectStats
{
    // ── Timing ───────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Temps d'action : durée de canalisation OU temps de charge max. (canalisation, marteau)")]
    public float ActionDuration;
    [Tooltip("Durée pendant laquelle l'effet persiste : fumée, sommeil, stun, immobilisation, reveal, résistance antivol…")]
    public float EffectDuration;

    // ── Spatial ──────────────────────────────────────────────
    [Header("Spatial")]
    [Tooltip("Portée : raycast d'usage, portée de lancer, portée de la lumière/scan.")]
    public float Range;
    [Tooltip("Rayon / zone d'effet : nuage, révélation, zone de lumière.")]
    public float Radius;
    [Tooltip("Angle du cône (lampe de poche / spot).")]
    public float ConeAngle;

    // ── Power ────────────────────────────────────────────────
    [Header("Power")]
    [Tooltip("Force / dégâts / puissance : cassure marteau, intensité.")]
    public float Power;
    [Tooltip("Quantité générique : soin, réduction paranoïa, % réduction de dégâts.")]
    public float Magnitude;

    // ── Reliability ──────────────────────────────────────────
    [Header("Reliability")]
    [Range(0f, 1f)]
    [Tooltip("Proba de réussite à la fin d'une canalisation (1 = sûr). 0 = mécanique désactivée (toujours réussir).")]
    public float SuccessChance;

    // ── Modifiers ────────────────────────────────────────────
    [Header("Modifiers")]
    [Tooltip("Multiplicateur de vitesse du joueur pendant l'usage (1 = aucun effet).")]
    public float PlayerSpeedMultiplier;
    [Tooltip("Modificateur de bruit émis par l'usage (négatif = plus discret).")]
    public float NoiseModifier;
    [Tooltip("Nombre de charges / usages. 0 = illimité.")]
    public int   Charges;
    [Tooltip("Cooldown (secondes) entre deux usages.")]
    public float Cooldown;
}

// ============================================================
// ENUMS PARTAGÉS (items)
// ============================================================

/// <summary>Comment un item s'utilise une fois en main — route le ToolBehaviour.</summary>
public enum ToolUsageMode
{
    None,       // pas d'usage actif (badge en main, etc.)
    Channel,    // maintien clic → jauge → effet (pied-de-biche, crochetage, scan, marteau)
    Light,      // lampe : cône de lumière en main, pas d'effet au clic
    Place,      // pose un prefab dans le monde (antivol, appât, leurre)
    Throw,      // lance / lâche un prefab (fumigène)
    Passive     // équipé sans modèle actif (gants, gilet)
}

/// <summary>Effet concret d'un outil — utilisé par le sous-dispatch des behaviours de canalisation.</summary>
public enum ToolEffectType
{
    None,
    ForceDoor,          // force une ouverture verrouillée (bruyant)
    Lockpick,           // crochetage silencieux
    ScanValue,          // révèle nom + valeur d'un objet (téléphone d'huissier)
    ReduceParanoia,     // badge, mandat — désamorce le proprio
    ImmobiliseOwner,    // menottes, fléchette
    ScanUV,             // révèle traces/indicateurs UV
    ScanXRay,           // révèle objets cachés à travers les murs
    SprayAnimal,        // calme / distrait un animal
    SpraySilence,       // réduit l'émission de bruit d'une pièce
    ExpandCarryCapacity // sac, chariot
}

/// <summary>Nombre de mains pour tenir l'item — sélectionne la pose de tenue du haut du corps.</summary>
public enum HandCount { OneHand, TwoHand }

/// <summary>Catégorie cosmétique (tri boutique / filtres). Sans effet runtime.</summary>
public enum ToolCategory
{
    ForceEntry,
    Stealth,
    Legal,
    Scanner,
    Immobiliser,
    Consumable,
    Utility
}
