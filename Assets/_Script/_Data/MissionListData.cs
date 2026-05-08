// ============================================================
// MissionListData.cs — Bailiff & Co  V2
// ScriptableObject contenant la liste ordonnée de toutes les
// missions du jeu. Référencé par MissionListUI.
//
// Créer via : clic droit → Create → BailiffCo/MissionListData
//
// SETUP :
//   1. Créer un asset MissionListData dans Features/Mission/MissionsData/
//   2. Glisser les MissionData dans le tableau Missions dans l'ordre
//   3. Vérifier que chaque MissionData a MissionNumber rempli (1, 2, 3…)
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/MissionListData")]
public class MissionListData : ScriptableObject
{
    [Tooltip("Toutes les missions du jeu dans l'ordre. " +
             "Chaque MissionData doit avoir MissionNumber rempli (1, 2, 3…).")]
    public MissionData[] Missions;

    // ================================================================
    // API
    // ================================================================

    /// <summary>
    /// Retourne les missions disponibles selon la progression.
    /// Une mission est disponible si MissionNumber <= dernierComplété + 1.
    /// </summary>
    public MissionData[] GetMissionsDisponibles(int derniereMissionCompletee)
    {
        var disponibles = new System.Collections.Generic.List<MissionData>();

        if (Missions == null) return disponibles.ToArray();

        foreach (var mission in Missions)
        {
            if (mission == null) continue;
            if (mission.MissionNumber <= derniereMissionCompletee + 1)
                disponibles.Add(mission);
        }

        return disponibles.ToArray();
    }

    /// <summary>
    /// Retourne la prochaine mission non complétée.
    /// </summary>
    public MissionData GetProchaineMission(int derniereMissionCompletee)
    {
        if (Missions == null) return null;

        foreach (var mission in Missions)
        {
            if (mission != null && mission.MissionNumber == derniereMissionCompletee + 1)
                return mission;
        }

        return null; // Toutes les missions complétées
    }

    /// <summary>
    /// Retourne true si toutes les missions sont complétées.
    /// </summary>
    public bool ToutesCompletees(int derniereMissionCompletee)
    {
        if (Missions == null || Missions.Length == 0) return false;

        foreach (var mission in Missions)
        {
            if (mission != null && mission.MissionNumber > derniereMissionCompletee)
                return false;
        }

        return true;
    }
}
