// ============================================================
// HubUI.cs — Bailiff & Co  V2
// Coordinateur de navigation Hub.
// _contexteVisibles = [Hub], _autoAfficher = true (Inspector)
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class HubUI : UIPanel
    {
        // ================================================================
        // HUD PERSISTANT
        // ================================================================

        [Header("HUD Persistant")]
        [SerializeField] private TextMeshProUGUI _txtArgent;
        [SerializeField] private TextMeshProUGUI _txtMissionChoisie;

        // ================================================================
        // PANNEAUX PRINCIPAUX
        // ================================================================

        [Header("Panneaux principaux")]
        [SerializeField] private MissionListUI   _missionListUI;
        [SerializeField] private MissionPanelUI  _missionPanelUI;
        [SerializeField] private VehiclePanelUI  _vehiclePanelUI;
        [SerializeField] private UIPanel         _shopPanel;          // ShopPanelUI — à venir
        [SerializeField] private UIPanel         _missionBuilderPanel; // MissionBuilderUI — à venir

        // ================================================================
        // POPUP ERREUR
        // ================================================================

        [Header("Popup Erreur")]
        [SerializeField] private GameObject      _popupErreur;
        [SerializeField] private TextMeshProUGUI _txtErreur;
        [SerializeField] private Button          _btnFermerErreur;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            if (_popupErreur != null) _popupErreur.SetActive(false);

            if (_btnFermerErreur != null)
                _btnFermerErreur.onClick.AddListener(FermerErreur);
        }

        private void OnDestroy()
        {
            _btnFermerErreur?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // NAVIGATION PANNEAUX
        // ================================================================

        public void OuvrirPanelMissions()
        {
            if (_missionListUI == null)
            {
                Debug.LogError("[HubUI] _missionListUI est NULL ! Vérifier l'Inspector.");
                return;
            }
            _missionListUI.Ouvrir();
        }

        public void OuvrirPanelMissionDetail(MissionData mission)
        {
            _missionPanelUI?.Ouvrir(mission);
        }

        public void OuvrirPanelVehicule(VehiculeData vehicule, float prixLocation)
        {
            _vehiclePanelUI?.Ouvrir(vehicule, prixLocation);
        }

        public void FermerPanelVehicule()
        {
            _vehiclePanelUI?.Fermer();
        }

        public void OuvrirPanelShop()
        {
            _shopPanel?.Ouvrir();
        }

        public void OuvrirPanelMissionBuilder()
        {
            _missionBuilderPanel?.Ouvrir();
        }

        public void FermerTousLesPanneaux()
        {
            _missionListUI?.Fermer();
            _missionPanelUI?.Fermer();
            _vehiclePanelUI?.Fermer();
            _shopPanel?.Fermer();
            _missionBuilderPanel?.Fermer();
            _popupErreur?.SetActive(false);
        }

        // ================================================================
        // HUD PERSISTANT
        // ================================================================

        public void MettreAJourArgent(float montant)
        {
            if (_txtArgent != null)
                _txtArgent.text = $"{montant:N0} €";
        }

        public void MettreAJourMissionChoisie(string nomMission)
        {
            if (_txtMissionChoisie != null)
                _txtMissionChoisie.text = $"Mission : {nomMission}";
        }

        // ================================================================
        // POPUP ERREUR
        // ================================================================

        public void AfficherErreur(string message)
        {
            if (_popupErreur == null)
            {
                Debug.LogWarning($"[HubUI] Erreur : {message}");
                return;
            }

            _popupErreur.SetActive(true);

            if (_txtErreur != null)
                _txtErreur.text = message;
        }

        public void FermerErreur()
        {
            _popupErreur?.SetActive(false);
        }

        // ================================================================
        // PROPRIÉTÉ
        // ================================================================

        public bool UnPanneauEstOuvert =>
            (_missionListUI    != null && _missionListUI.EstOuvert)    ||
            (_missionPanelUI   != null && _missionPanelUI.EstOuvert)   ||
            (_vehiclePanelUI   != null && _vehiclePanelUI.EstOuvert)   ||
            (_shopPanel        != null && _shopPanel.EstOuvert)        ||
            (_missionBuilderPanel != null && _missionBuilderPanel.EstOuvert) ||
            (_popupErreur      != null && _popupErreur.activeSelf);
    }
}
