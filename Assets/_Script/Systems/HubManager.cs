// ============================================================
// HubManager.cs — Bailiff & Co  V2
// ============================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BailiffCo;

namespace BailiffCo.Hub
{
    public class HubManager : MonoBehaviour
    {
        public static HubManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ================================================================
        // CONFIGURATION
        // ================================================================

        [Header("Player Spawn Points")]
        [SerializeField] private Transform _spawnPointHub;
        [SerializeField] private Transform _spawnPointRetour;

        [Header("Dev/Test Only")]
        [SerializeField] private MissionData _missionTest;
        [SerializeField] private float       _argentTest = 0f;

        // ================================================================
        // ÉTAT SESSION
        // ================================================================

        private HubUI        _hubUI;
        private VehiculeData _vehiculeSelectionne;
        private float        _prixLocationVehicule;
        private bool         _retourDeMission = false;
        private List<VehicleOption> _optionsSelectionnees = new();

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            _retourDeMission = GameManager.Instance != null &&
                               GameManager.Instance.DerniereMissionCompletee > 0;

            StartCoroutine(InitialiserHub());
        }

        private IEnumerator InitialiserHub()
        {
            yield return null;
            yield return null;

            // HubUI vit dans UI_Persistent — disponible dès le premier frame
            _hubUI = UIManager.Instance?.GetPanel<HubUI>();
            if (_hubUI == null)
                Debug.LogWarning("[HubManager] HubUI introuvable via UIManager.GetPanel<HubUI>() !");

            SpawnerPlayer();

            if (_argentTest > 0f && GameManager.Instance != null)
            {
                GameManager.Instance.Crediter(_argentTest);
                Debug.Log($"[HubManager] Argent test injecté : {_argentTest:N0} €");
            }

            _hubUI?.MettreAJourArgent(GameManager.Instance?.Argent ?? 0f);

            if (_missionTest != null)
            {
                GameManager.Instance?.SetMissionSelectionnee(_missionTest);
                Debug.Log($"[HubManager] Mission test auto : {_missionTest.MissionName}");
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ================================================================
        // SPAWN PLAYER
        // ================================================================

        private void SpawnerPlayer()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[HubManager] GameManager.Instance est null !");
                return;
            }

            Transform spawnPoint = (_retourDeMission && _spawnPointRetour != null)
                ? _spawnPointRetour
                : (_spawnPointHub != null ? _spawnPointHub : transform);

            GameManager.Instance.SpawnerPlayerSiNecessaire(
                spawnPoint.position,
                spawnPoint.rotation
            );
        }

        // ================================================================
        // SÉLECTION MISSION
        // ================================================================

        public void SelectionnerMission(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogWarning("[HubManager] SelectionnerMission : mission null ignorée.");
                return;
            }

            GameManager.Instance?.SetMissionSelectionnee(mission);
            Debug.Log($"[HubManager] Mission sélectionnée : {mission.MissionName}");

            _hubUI?.OuvrirPanelMissionDetail(mission);
            _hubUI?.MettreAJourMissionChoisie(mission.MissionName);
        }

        // ================================================================
        // LOCATION VÉHICULE
        // ================================================================

        public void DemanderLocationVehicule(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[HubManager] DemanderLocationVehicule : vehicule null.");
                return;
            }

            _vehiculeSelectionne  = vehicule;
            _prixLocationVehicule = prixLocation;
            _optionsSelectionnees.Clear();

            _hubUI?.OuvrirPanelVehicule(vehicule, prixLocation);
        }

        public void ConfirmerLocationEtPartir()
        {
            var mission = GameManager.Instance?.MissionSelectionnee;
            if (mission == null)
            {
                _hubUI?.AfficherErreur("Aucune mission sélectionnée !\nParle au Chef d'abord.");
                return;
            }

            if (_vehiculeSelectionne == null)
            {
                _hubUI?.AfficherErreur("Aucun véhicule sélectionné.");
                return;
            }

            float totalPrice = GetTotalPrice();
            float solde = GameManager.Instance?.Argent ?? 0f;
            if (solde < totalPrice)
            {
                _hubUI?.AfficherErreur(
                    $"Fonds insuffisants.\n" +
                    $"Location : {totalPrice:N0} €\n" +
                    $"Ton solde : {solde:N0} €");
                return;
            }

            GameManager.Instance?.SetOptionsSelectionnees(_optionsSelectionnees, totalPrice);

            Debug.Log($"[HubManager] Départ → {mission.MissionName} " +
                      $"avec {_vehiculeSelectionne.VehicleName} ({_prixLocationVehicule:N0} €)");

            GameManager.Instance?.LancerMission(mission, _vehiculeSelectionne);
        }

        public void AnnulerLocationVehicule()
        {
            _vehiculeSelectionne  = null;
            _prixLocationVehicule = 0f;
            _hubUI?.FermerPanelVehicule();
        }

        public void ToggleOption(VehicleOption option)
        {
            if (_optionsSelectionnees.Contains(option))
            {
                _optionsSelectionnees.Remove(option);
            }
            else
            {
                _optionsSelectionnees.Add(option);
            }
        }

        public float GetTotalPrice()
        {
            float optionsTotal = _optionsSelectionnees.Sum(o => o.Price);
            return _prixLocationVehicule + optionsTotal;
        }

        // ================================================================
        // UTILITAIRES
        // ================================================================

        public void OuvrirPanelMissions()  => _hubUI?.OuvrirPanelMissions();
        public void OuvrirPanelShop()      => _hubUI?.OuvrirPanelShop();
        public void OuvrirMissionLibreConfig()
            => UIManager.Instance?.GetPanel<MissionLibreConfigUI>()?.Ouvrir();
        public void AfficherErreur(string message) => _hubUI?.AfficherErreur(message);

        public void MettreAJourAffichageArgent()
        {
            _hubUI?.MettreAJourArgent(GameManager.Instance?.Argent ?? 0f);
        }

        // ================================================================
        // PROPRIÉTÉS
        // ================================================================

        public MissionData  MissionSelectionnee => GameManager.Instance?.MissionSelectionnee;
        public VehiculeData VehiculeSelectionne => _vehiculeSelectionne;
        public bool         MissionChoisie      => MissionSelectionnee != null;
    }
}
