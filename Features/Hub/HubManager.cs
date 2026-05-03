// ============================================================
// HubManager.cs — Bailiff & Co  V2
// Orchestrateur du Hub. Source de vérité locale pour la session.
// 
// CHANGEMENTS V2 :
//   - Plus de FindObjectOfType → injection [SerializeField]
//   - VehiculeData transmis à GameManager (pas juste la mission)
//   - UI éclatée : HubUI ne gère que navigation, panels dédiés
//   - Argent test via Inspector (dev only)
// ============================================================
using UnityEngine;
using System.Collections;

namespace BailiffCo.Hub
{
    public class HubManager : MonoBehaviour
    {
        // ================================================================
        // SINGLETON LOCAL
        // ================================================================

        public static HubManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ================================================================
        // INJECTION DE DÉPENDANCES
        // ================================================================

        [Header("UI References")]
        [SerializeField] private HubUI _hubUI;
        [SerializeField] private MissionPanelUI _missionPanelUI;
        [SerializeField] private VehiclePanelUI _vehiclePanelUI;

        [Header("Player Spawn Points")]
        [Tooltip("Point de spawn initial du Player dans le Hub")]
        [SerializeField] private Transform _spawnPointHub;

        [Tooltip("Point de spawn quand le joueur revient de mission (près du parking)")]
        [SerializeField] private Transform _spawnPointRetour;

        [Header("Dev/Test Only")]
        [Tooltip("Mission auto-sélectionnée au démarrage (dev only).")]
        [SerializeField] private MissionData _missionTest;
        
        [Tooltip("Argent injecté au démarrage (dev only). 0 = désactivé.")]
        [SerializeField] private float _argentTest = 0f;

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
            // Détermine si on revient d'une mission
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

            Transform spawnPoint;

            if (_retourDeMission && _spawnPointRetour != null)
            {
                // Retour de mission → spawn au parking
                spawnPoint = _spawnPointRetour;
            }
            else
            {
                // Première visite → spawn à l'entrée du Hub
                spawnPoint = _spawnPointHub != null ? _spawnPointHub : transform;
            }

            GameManager.Instance.SpawnerPlayerSiNecessaire(
                spawnPoint.position,
                spawnPoint.rotation
            );
        }

        // ================================================================
        // SÉLECTION MISSION — appelé par MissionPanelUI ou HubPNJ
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

            // Affiche la fiche mission
            _missionPanelUI?.AfficherFiche(mission);

            // Met à jour le label HUD
            _hubUI?.MettreAJourMissionChoisie(mission.MissionName);
        }

        // ================================================================
        // LOCATION VÉHICULE — appelé par VehicleHubSlot
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

            // Affiche le panel véhicule (popup confirmation)
            _vehiclePanelUI?.AfficherPopup(vehicule, prixLocation);
        }

        public void ConfirmerLocationEtPartir()
        {
            // Validation mission
            if (_missionSelectionnee == null)
            {
                _hubUI?.AfficherErreur("Aucune mission sélectionnée !\nParle au Chef d'abord.");
                return;
            }

            // Validation véhicule
            if (_vehiculeSelectionne == null)
            {
                _hubUI?.AfficherErreur("Aucun véhicule sélectionné.");
                return;
            }

            // Validation fonds
            float solde = GameManager.Instance?.Argent ?? 0f;
            if (solde < _prixLocationVehicule)
            {
                _hubUI?.AfficherErreur(
                    $"Fonds insuffisants.\n" +
                    $"Location : {_prixLocationVehicule:N0} €\n" +
                    $"Ton solde : {solde:N0} €");
                return;
            }

            // Débit de l'argent
            GameManager.Instance?.Debiter(_prixLocationVehicule);

            Debug.Log($"[HubManager] Départ → {_missionSelectionnee.MissionName} " +
                      $"avec {_vehiculeSelectionne.VehicleName} ({_prixLocationVehicule:N0} €)");

            // Lancement mission (V2 : transmet aussi le véhicule)
            GameManager.Instance?.LancerMission(_missionSelectionnee, _vehiculeSelectionne);
        }

        public void AnnulerLocationVehicule()
        {
            _vehiculeSelectionne  = null;
            _prixLocationVehicule = 0f;
            
            // Ferme le panel véhicule
            _vehiclePanelUI?.FermerPopup();
        }

        // ================================================================
        // MISE À JOUR AFFICHAGE ARGENT — appelé après achat boutique
        // ================================================================

        public void MettreAJourAffichageArgent()
        {
            _hubUI?.MettreAJourArgent(GameManager.Instance?.Argent ?? 0f);
        }

        // ================================================================
        // PROPRIÉTÉS PUBLIQUES
        // ================================================================

        public MissionData  MissionSelectionnee => _missionSelectionnee;
        public VehiculeData VehiculeSelectionne => _vehiculeSelectionne;
        public bool         MissionChoisie      => _missionSelectionnee != null;
    }
}