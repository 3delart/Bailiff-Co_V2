// ============================================================
// ToolData.cs — Bailiff & Co  V2
// Outil PERMANENT : possédé en 1 exemplaire, upgradable (niveaux 0-2).
// Chaque niveau porte son propre EffectStats (params tunables).
// Le niveau ACTUEL du joueur est géré à runtime dans InventaireSystem.
//
// Créer via : clic droit → Create → BailiffCo/Tool
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/Tool")]
public class ToolData : ItemData
{
    [Header("Catégorie (cosmétique — tri boutique)")]
    public ToolCategory Category;

    [Header("Effet")]
    [Tooltip("Effet concret — utilisé par le sous-dispatch des behaviours de canalisation.")]
    public ToolEffectType EffectType = ToolEffectType.None;

    [Header("Niveaux (max 3 — laisser les niveaux inutilisés vides)")]
    [Tooltip("Stats + coût d'upgrade par niveau. Index 0 = Niv.1, Index 2 = Niv.3 (max).")]
    public ToolLevel[] Levels = new ToolLevel[3];

    /// <summary>Stats résolus pour un niveau (clampé aux bornes du tableau). Defaut si vide.</summary>
    public EffectStats StatsForLevel(int niveau)
    {
        if (Levels == null || Levels.Length == 0) return default;
        int i = Mathf.Clamp(niveau, 0, Levels.Length - 1);
        return Levels[i] != null ? Levels[i].Stats : default;
    }

    /// <summary>Nombre de niveaux réellement définis (non nuls).</summary>
    public int LevelCount
    {
        get
        {
            if (Levels == null) return 0;
            int n = 0;
            foreach (var lv in Levels) if (lv != null) n++;
            return n;
        }
    }
}

// ============================================================
// TOOL LEVEL — données d'un palier d'upgrade
// ============================================================
[System.Serializable]
public class ToolLevel
{
    [Tooltip("Nom affiché du niveau (ex: 'Pied-de-biche Niv.2').")]
    public string LevelName;

    [TextArea(1, 2)]
    [Tooltip("Description courte de ce qui change à ce niveau.")]
    public string EffectDescription;

    [Tooltip("Coût (€) pour passer du niveau précédent à celui-ci. 0 = gratuit / niveau de base.")]
    public int UpgradeCost = 0;

    [Tooltip("Paramètres d'effet pour ce niveau (durées, portées, puissance…).")]
    public EffectStats Stats;
}
