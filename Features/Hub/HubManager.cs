// ============================================================
// HubManager.cs — Bailiff & Co  V2
// ============================================================
using UnityEngine;
using System.Collections;

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
        // INJECTION DE DÉPENDANCES
        // ================================================================

        [Header("UI References")]
        [SerializeField] private HubUI          _hubUI;
        [SerializeField] private MissionPanelUI _missionPanelUI;
        [SerializeField] private VehiclePanelUI _vehiclePanelUI;

        [Header("Player Spawn Points")]
        [SerializeField] private Transform _spawnPointHub;
        [SerializeField] private Transform _spawnPointRetour;

        [Header("Dev/Test Only")]
        [SerializeField] private MissionData _missionTest;
        [SerializeField] private float       _argentTest = 0f;

        // ================================================================
        // ÉTAT SESSION
        // ================================================================

        private MissionData  _missionSelectionnee;
        private VehiculeData _vehiculeSelectionne;
        private float        _prixLocationVehicule;
        private bool         _retourDeMission = false;

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

            SpawnerPlayer();

            if (_argentTest > 0f && GameManager.Instance != null)
            {
                GameManager.Instance.Crediter(_argentTest);
                Debug.Log($"[HubManager] Argent test injecté : {_argentTest:N0} €");
            }

            _hubUI?.MettreAJourArgent(GameManager.Instance?.Argent ?? 0f);

            if (_missionTest != null)
            {
                _missionSelectionnee = _missionTest;
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

            _missionSelectionnee = mission;
            Debug.Log($"[HubManager] Mission sélectionnée : {mission.MissionName}");

            // ← NOM CORRIGÉ
            _missionPanelUI?.AfficherFiche(mission);

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

            Debug.Log($"[HubManager] _vehiclePanelUI: {_vehiclePanelUI != null}");

            // ← NOM CORRIGÉ
            _vehiclePanelUI?.AfficherPopup(vehicule, prixLocation);
        }

        public void ConfirmerLocationEtPartir()
        {
            if (_missionSelectionnee == null)
            {
                _hubUI?.AfficherErreur("Aucune mission sélectionnée !\nParle au Chef d'abord.");
                return;
            }

            if (_vehiculeSelectionne == null)
            {
                _hubUI?.AfficherErreur("Aucun véhicule sélectionné.");
                return;
            }

            float solde = GameManager.Instance?.Argent ?? 0f;
            if (solde < _prixLocationVehicule)
            {
                _hubUI?.AfficherErreur(
                    $"Fonds insuffisants.\n" +
                    $"Location : {_prixLocationVehicule:N0} €\n" +
                    $"Ton solde : {solde:N0} €");
                return;
            }

            GameManager.Instance?.Debiter(_prixLocationVehicule);

            Debug.Log($"[HubManager] Départ → {_missionSelectionnee.MissionName} " +
                      $"avec {_vehiculeSelectionne.VehicleName} ({_prixLocationVehicule:N0} €)");

            GameManager.Instance?.LancerMission(_missionSelectionnee, _vehiculeSelectionne);
        }

        public void AnnulerLocationVehicule()
        {
            _vehiculeSelectionne  = null;
            _prixLocationVehicule = 0f;

            // ← NOM CORRIGÉ
            _vehiclePanelUI?.FermerPopup();
        }

        // ================================================================
        // UTILITAIRES
        // ================================================================

        public void MettreAJourAffichageArgent()
        {
            _hubUI?.MettreAJourArgent(GameManager.Instance?.Argent ?? 0f);
        }

        // ================================================================
        // PROPRIÉTÉS
        // ================================================================

        public MissionData  MissionSelectionnee => _missionSelectionnee;
        public VehiculeData VehiculeSelectionne => _vehiculeSelectionne;
        public bool         MissionChoisie      => _missionSelectionnee != null;
    }
}