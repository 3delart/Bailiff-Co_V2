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
using System.Collections;
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
    [Tooltip("OwnerAI présent dans la scène (glissé depuis la hiérarchie).")]
    [SerializeField] private OwnerAI  _proprietaireAI;

    [Header("Debug — test direct sans Hub")]
    [Tooltip("MissionData utilisée si GameManager.MissionSelectionnee est null (play direct depuis l'éditeur).")]
    [SerializeField] private MissionData _fallbackMissionData;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        var mission = GameManager.Instance?.MissionSelectionnee ?? _fallbackMissionData;
        if (mission == null)
        {
            Debug.LogError("[CampaignMissionStarter] Aucune MissionData en cours — " +
                           "lancer depuis le Hub ou assigner _fallbackMissionData dans l'Inspector.");
            return;
        }

        StartCoroutine(BuildApresInit(mission));
    }

    // ================================================================
    // CONSTRUCTION
    // ================================================================

    // Délai d'une frame pour laisser UIManager activer les panels (HUDSystem)
    // avant que StartMission n'émette OnMissionStarted → OnQuotaChanged.
    private IEnumerator BuildApresInit(MissionData mission)
    {
        yield return null;
        Build(mission);
    }

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

        //Debug.Log($"[CampaignMissionStarter] Scène prête : {mission.MissionName}");
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
        // Si aucun véhicule spawné (play direct sans Hub), cherche dans la scène
        var vehicle = vehicule?.GetComponent<Vehicle>()
                   ?? FindFirstObjectByType<Vehicle>();

        if (vehicle == null)
        {
            Debug.LogWarning("[CampaignMissionStarter] Vehicle introuvable — refs non injectées.");
            return;
        }

        var player = GameManager.Instance?.Player;
        var carry  = player != null ? player.GetComponent<PlayerCarry>() : null;

        if (carry == null)
            Debug.LogWarning("[CampaignMissionStarter] PlayerCarry introuvable sur le joueur — label coffre dégradé.");

        vehicle.InjectDependencies(_missionSystem, carry, _quotaSystem);
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
