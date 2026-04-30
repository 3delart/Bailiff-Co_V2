// ============================================================
// MissionBuilder.cs — Bailiff & Co  V2
// NOUVEAU — Construit la scène Mission de manière procédurale.
// Remplace les 50+ scènes Mission individuelles par un système
// unique qui spawn le contenu selon la MissionData.
//
// RESPONSABILITÉS :
//   - Instancier le prefab Maison au bon emplacement
//   - Instancier le véhicule sélectionné au parking
//   - Instancier le prefab Propriétaire
//   - Spawner les objets saisissables aux points définis
//   - Appliquer l'ambiance (skybox, fog, musique)
//   - Démarrer MissionSystem une fois tout en place
//
// SETUP UNITY :
//   Scène Mission.unity contient uniquement :
//   ├── MissionBuilder (ce script)
//   ├── MissionSystem
//   ├── QuotaSystem
//   ├── ParanoiaSystem
//   ├── Ancrages (Transforms vides)
//   │   ├── AncrageMaison
//   │   ├── AncrageVehicule
//   │   └── AncrageProprietaire
//   └── PlayerRoot (spawne au SpawnPoint du prefab Maison)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class MissionBuilder : MonoBehaviour
{
    // ================================================================
    // RÉFÉRENCES INJECTÉES
    // ================================================================

    [Header("Ancrages — positions de spawn")]
    [Tooltip("Transform vide où instancier le prefab Maison")]
    [SerializeField] private Transform _anchorHouse;

    [Tooltip("Transform vide où instancier le véhicule")]
    [SerializeField] private Transform _anchorVehicle;

    [Tooltip("Transform vide où instancier le proprio (optionnel si défini dans Maison)")]
    [SerializeField] private Transform _anchorOwner;

    [Header("Systèmes")]
    [SerializeField] private MissionSystem   _missionSystem;
    [SerializeField] private QuotaSystem     _quotaSystem;
    [SerializeField] private ParanoiaSystem  _paranoiaSystem;

    [Header("Joueur")]
    [Tooltip("Prefab du joueur — spawné au SpawnPoint trouvé dans le prefab Maison")]
    [SerializeField] private GameObject _playerPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource _musicSource;

    // ================================================================
    // ÉTAT
    // ================================================================

    private MissionData  _mission;
    private VehiculeData _vehicle;
    private GameObject   _spawnedHouse;
    private GameObject   _spawnedVehicle;
    private GameObject   _spawnedOwner;
    private GameObject   _spawnedPlayer;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        // Récupère la mission et le véhicule depuis GameManager
        _mission = GameManager.Instance?.MissionSelectionnee;
        _vehicle = GameManager.Instance?.VehiculeSelectionne;

        if (_mission == null)
        {
            Debug.LogError("[MissionBuilder] Pas de MissionData sélectionnée !");
            return;
        }

        Build();
    }

    // ================================================================
    // CONSTRUCTION PROCÉDURALE
    // ================================================================

    private void Build()
    {
        Debug.Log($"[MissionBuilder] Construction de la mission : {_mission.MissionName}");

        // 1. Spawner la maison
        SpawnHouse();

        // 2. Spawner le véhicule
        SpawnVehicle();

        // 3. Spawner le propriétaire
        SpawnOwner();

        // 4. Spawner les objets saisissables
        SpawnObjects();

        // 5. Spawner le joueur au bon endroit
        SpawnPlayer();

        // 6. Appliquer l'ambiance (skybox, fog, musique)
        ApplyAmbiance();

        // 7. Démarrer la mission
        _missionSystem.StartMission(_mission);

        Debug.Log("[MissionBuilder] Construction terminée");
    }

    // ================================================================
    // 1. MAISON
    // ================================================================

    private void SpawnHouse()
    {
        if (_mission.HousePrefab == null)
        {
            Debug.LogWarning("[MissionBuilder] Pas de HousePrefab défini dans MissionData");
            return;
        }

        _spawnedHouse = Instantiate(
            _mission.HousePrefab,
            _anchorHouse.position,
            _anchorHouse.rotation
        );

        _spawnedHouse.name = $"House_{_mission.MissionName}";
        Debug.Log($"[MissionBuilder] Maison spawnée : {_spawnedHouse.name}");
    }

    // ================================================================
    // 2. VÉHICULE
    // ================================================================

    private void SpawnVehicle()
    {
        if (_vehicle?.Prefab == null)
        {
            Debug.LogWarning("[MissionBuilder] Pas de véhicule sélectionné ou Prefab manquant");
            return;
        }

        _spawnedVehicle = Instantiate(
            _vehicle.Prefab,
            _anchorVehicle.position,
            _anchorVehicle.rotation
        );

        _spawnedVehicle.name = $"Vehicle_{_vehicle.VehicleName}";
        Debug.Log($"[MissionBuilder] Véhicule spawné : {_spawnedVehicle.name}");
    }

    // ================================================================
    // 3. PROPRIÉTAIRE
    // ================================================================

    private void SpawnOwner()
    {
        // Le prefab proprio peut venir soit de MissionData.OwnerPrefab
        // soit de MissionData.Owner.OwnerPrefab
        GameObject ownerPrefab = _mission.OwnerPrefab;
        if (ownerPrefab == null && _mission.Owner != null)
            ownerPrefab = _mission.Owner.OwnerPrefab;

        if (ownerPrefab == null)
        {
            Debug.LogWarning("[MissionBuilder] Pas de OwnerPrefab défini");
            return;
        }

        // Cherche un point de spawn "OwnerSpawn" dans la maison
        Transform ownerSpawn = FindSpawnPoint("OwnerSpawn");
        Vector3   spawnPos   = ownerSpawn != null ? ownerSpawn.position : _anchorOwner.position;
        Quaternion spawnRot  = ownerSpawn != null ? ownerSpawn.rotation : _anchorOwner.rotation;

        _spawnedOwner = Instantiate(ownerPrefab, spawnPos, spawnRot);
        _spawnedOwner.name = $"Owner_{_mission.Owner?.OwnerName ?? "Unknown"}";

        // Injecte les références player/vehicle dans ProprietaireAI
        if (_spawnedOwner.TryGetComponent<ProprietaireAI>(out var ai))
        {
            // On les injectera après avoir spawné le joueur
            Debug.Log($"[MissionBuilder] Propriétaire spawné : {_spawnedOwner.name}");
        }
    }

    // ================================================================
    // 4. OBJETS SAISISSABLES
    // ================================================================

    private void SpawnObjects()
    {
        if (_mission.SeizableObjects == null || _mission.SeizableObjects.Length == 0)
        {
            Debug.LogWarning("[MissionBuilder] Pas d'objets saisissables définis");
            return;
        }

        // Récupère tous les SpawnPoints_Objets dans la maison
        List<Transform> spawnPoints = FindAllSpawnPoints("ObjectSpawn");
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[MissionBuilder] Aucun ObjectSpawn trouvé dans le prefab Maison");
            return;
        }

        // Shuffle des spawn points avec le seed
        int seed = _mission.FixedSeed != 0 ? _mission.FixedSeed : Random.Range(1, 999999);
        ShuffleList(spawnPoints, seed);

        int spawnIndex = 0;

        // Pour chaque entrée d'objet saisissable
        foreach (var entry in _mission.SeizableObjects)
        {
            if (entry.ObjectData?.Prefab == null) continue;

            // Détermine combien spawner (entre MinCount et MaxCount)
            int count = Random.Range(entry.MinCount, entry.MaxCount + 1);

            for (int i = 0; i < count; i++)
            {
                if (spawnIndex >= spawnPoints.Count)
                {
                    Debug.LogWarning("[MissionBuilder] Plus de spawn points disponibles");
                    break;
                }

                Transform spawnPoint = spawnPoints[spawnIndex];
                spawnIndex++;

                // Instancie l'objet
                GameObject obj = Instantiate(
                    entry.ObjectData.Prefab,
                    spawnPoint.position,
                    spawnPoint.rotation
                );

                // Initialise sa valeur aléatoire
                float valueMin = entry.ValueMinOverride > 0 ? entry.ValueMinOverride : entry.ObjectData.ValueMin;
                float valueMax = entry.ValueMaxOverride > 0 ? entry.ValueMaxOverride : entry.ObjectData.ValueMax;
                float value    = Random.Range(valueMin, valueMax);

                if (obj.TryGetComponent<ValueObject>(out var valueObj))
                {
                    valueObj.Initialize(entry.ObjectData, value);
                }

                obj.name = $"{entry.ObjectData.ObjectName}_{i + 1}";
            }
        }

        Debug.Log($"[MissionBuilder] {spawnIndex} objets spawnés");
    }

    // ================================================================
    // 5. JOUEUR
    // ================================================================

    private void SpawnPlayer()
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[MissionBuilder] PlayerPrefab manquant !");
            return;
        }

        // Cherche un point de spawn "PlayerSpawn" dans la maison
        Transform playerSpawn = FindSpawnPoint("PlayerSpawn");
        if (playerSpawn == null)
        {
            Debug.LogWarning("[MissionBuilder] Aucun PlayerSpawn trouvé — spawn à l'origine");
            playerSpawn = transform;
        }

        _spawnedPlayer = Instantiate(
            _playerPrefab,
            playerSpawn.position,
            playerSpawn.rotation
        );

        _spawnedPlayer.name = "Player";

        // Injecte maintenant les références dans ProprietaireAI
        if (_spawnedOwner != null && _spawnedOwner.TryGetComponent<ProprietaireAI>(out var ai))
        {
            ai.SetReferences(_spawnedPlayer.transform, _spawnedVehicle?.transform);
        }

        Debug.Log($"[MissionBuilder] Joueur spawné à {playerSpawn.position}");
    }

    // ================================================================
    // 6. AMBIANCE
    // ================================================================

    private void ApplyAmbiance()
    {
        // Skybox
        if (_mission.SkyboxOverride != null)
            RenderSettings.skybox = _mission.SkyboxOverride;

        // Fog
        RenderSettings.fogColor   = _mission.FogColor;
        RenderSettings.fogDensity = _mission.FogDensity;

        // Musique
        if (_mission.MissionMusic != null && _musicSource != null)
        {
            _musicSource.clip   = _mission.MissionMusic;
            _musicSource.volume = _mission.MusicVolume;
            _musicSource.loop   = true;
            _musicSource.Play();
        }

        Debug.Log("[MissionBuilder] Ambiance appliquée");
    }

    // ================================================================
    // HELPERS — RECHERCHE DE SPAWN POINTS
    // ================================================================

    /// <summary>Trouve le premier Transform avec le tag spécifié dans la maison spawnée</summary>
    private Transform FindSpawnPoint(string tag)
    {
        if (_spawnedHouse == null) return null;

        foreach (Transform child in _spawnedHouse.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
                return child;
        }

        return null;
    }

    /// <summary>Trouve tous les Transforms avec le tag spécifié dans la maison</summary>
    private List<Transform> FindAllSpawnPoints(string tag)
    {
        List<Transform> points = new List<Transform>();
        if (_spawnedHouse == null) return points;

        foreach (Transform child in _spawnedHouse.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
                points.Add(child);
        }

        return points;
    }

    // ================================================================
    // HELPERS — SHUFFLE
    // ================================================================

    private void ShuffleList<T>(List<T> list, int seed)
    {
        System.Random rng = new System.Random(seed);
        int n = list.Count;
        
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}