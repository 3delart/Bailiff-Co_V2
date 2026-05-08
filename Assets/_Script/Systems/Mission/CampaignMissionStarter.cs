// ============================================================
// CampaignMissionStarter.cs — Bailiff & Co  V2
// Démarre une scène mission hand-crafted (campagne).
// Remplace MissionBuilder pour les scènes Mission_XX.
//
// SETUP UNITY (chaque Mission_XX.unity) :
//   ├── MissionSetup (GameObject)
//   │   └── CampaignMissionStarter (ce script)
//   ├── SpawnPlayer  (Empty — positionner devant la porte)
//   └── SpawnVehicle (Empty — positionner sur la rue)
// ============================================================
using UnityEngine;

public class CampaignMissionStarter : MonoBehaviour
{
    [Header("Systèmes")]
    [SerializeField] private MissionSystem   _missionSystem;
    [SerializeField] private QuotaSystem     _quotaSystem;

    [Header("Spawn Points — GameObjects vides placés dans la scène")]
    [SerializeField] private Transform       _playerSpawnPoint;
    [SerializeField] private Transform       _vehicleSpawnPoint;

    [Header("Scène — refs hand-crafted")]
    [Tooltip("ProprietaireAI présent dans la scène (glissé depuis la hiérarchie).")]
    [SerializeField] private ProprietaireAI  _proprietaireAI;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        var mission = GameManager.Instance?.MissionSelectionnee;
        if (mission == null)
        {
            Debug.LogError("[CampaignMissionStarter] Aucune MissionData en cours — " +
                           "lancer la mission depuis le Hub.");
            return;
        }

        Build(mission);
    }

    // ================================================================
    // CONSTRUCTION
    // ================================================================

    private void Build(MissionData mission)
    {
        if (_missionSystem == null)
        {
            Debug.LogError("[CampaignMissionStarter] _missionSystem non assigné dans l'Inspector !");
            return;
        }

        SpawnPlayer();
        var vehiculeSpawne = SpawnVehicle();
        InjecterRefsDansVehicule(vehiculeSpawne);
        InjecterRefsProprietaire(vehiculeSpawne);
        _missionSystem.StartMission(mission);

        Debug.Log($"[CampaignMissionStarter] Scène prête : {mission.MissionName}");
    }

    private void SpawnPlayer()
    {
        Transform pt = _playerSpawnPoint != null ? _playerSpawnPoint : transform;
        GameManager.Instance?.SpawnerPlayerSiNecessaire(pt.position, pt.rotation);
    }

    private GameObject SpawnVehicle()
    {
        var vehicule = GameManager.Instance?.VehiculeSelectionne;
        if (vehicule?.Prefab == null || _vehicleSpawnPoint == null)
        {
            Debug.LogWarning("[CampaignMissionStarter] Pas de véhicule ou SpawnVehicle non assigné.");
            return null;
        }

        var spawned = Instantiate(vehicule.Prefab,
            _vehicleSpawnPoint.position,
            _vehicleSpawnPoint.rotation);
        spawned.name = $"Vehicle_{vehicule.VehicleName}";
        return spawned;
    }

    private void InjecterRefsDansVehicule(GameObject vehicule)
    {
        if (vehicule == null) return;

        var runtime = vehicule.GetComponent<VehicleRuntime>();
        if (runtime == null)
        {
            Debug.LogWarning("[CampaignMissionStarter] VehicleRuntime introuvable sur le véhicule spawné.");
            return;
        }

        var player = GameManager.Instance?.Player;
        var carry  = player != null ? player.GetComponent<PlayerCarry>() : null;

        if (carry == null)
            Debug.LogWarning("[CampaignMissionStarter] PlayerCarry introuvable sur le joueur — label coffre dégradé.");

        runtime.InjectDependencies(_missionSystem, carry, _quotaSystem);
    }

    private void InjecterRefsProprietaire(GameObject vehicule)
    {
        if (_proprietaireAI == null) return;
        var player = GameManager.Instance?.Player;
        if (player == null)
        {
            Debug.LogWarning("[CampaignMissionStarter] Player non disponible — refs proprio non injectées.");
            return;
        }
        _proprietaireAI.SetReferences(player.transform, vehicule?.transform);
    }
}
