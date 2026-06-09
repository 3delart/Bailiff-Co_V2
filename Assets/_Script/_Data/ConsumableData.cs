// ============================================================
// ConsumableData.cs — Bailiff & Co  V2
// Consommable : possédé en STOCK (acheté à l'unité), non upgradable.
// Un seul jeu d'EffectStats (à plat — pas de niveaux).
// Usage en mission décrémente le stock.
//
// Créer via : clic droit → Create → BailiffCo/Consumable
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Emport")]
    [Tooltip("Nombre max emporté par mission (loadout casier). 0 = illimité (à éviter).")]
    public int MaxCarryPerMission = 3;

    [Header("Effet (à plat — pas de niveaux)")]
    [Tooltip("Paramètres d'effet (durée, rayon, puissance…).")]
    public EffectStats Stats;

    [Header("Monde")]
    [Tooltip("Prefab laissé / lancé dans le monde à l'usage (peut différer du HandPrefab tenu en main).")]
    public GameObject WorldPrefab;

    [Tooltip("Pose CIBLÉE : valide uniquement en visant le coffre du véhicule (antivol). Sinon pose libre.")]
    public bool TargetVehicleTrunkOnly = false;
}
