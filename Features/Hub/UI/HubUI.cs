// ============================================================
// HubUI.cs — Bailiff & Co  V2
// Coordinateur de navigation Hub — NE GÈRE PLUS LE CONTENU.
// 
// CHANGEMENTS V2 (ÉCLATEMENT DU GOD OBJECT) :
//   - Ne garde QUE : navigation panels, affichage argent, erreur
//   - EXTRAIT vers MissionPanelUI : tout le code fiche mission
//   - EXTRAIT vers VehiclePanelUI : tout le code popup véhicule
//   - Plus de logique métier ici, juste coordination UI
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class HubUI : MonoBehaviour
    {
        // ================================================================
        // HUD PERSISTANT (toujours visible)
        // ================================================================

        [Header("HUD Persistant")]
        [SerializeField] private TextMeshProUGUI _txtArgent;
        [SerializeField] private TextMeshProUGUI _txtMissionChoisie;

        // ================================================================
        // PANNEAUX PRINCIPAUX (navigation)
        // ================================================================

        [Header("Panneaux principaux")]
        [SerializeField] private GameObject _panelMissions;
        [SerializeField] private GameObject _panelBoutique;
        [SerializeField] private GameObject _panelInventaire;
        [SerializeField] private GameObject _panelGarage;

        // ================================================================
        // POPUP ERREUR (partagé)
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
            FermerTousLesPanneaux();

            if (_btnFermerErreur != null)
                _btnFermerErreur.onClick.AddListener(FermerErreur);
        }

        private void OnDestroy()
        {
            _btnFermerErreur?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // NAVIGATION PANNEAUX — appelé par HubPNJ
        // ================================================================

        public void OuvrirPanelMissions()
        {
            FermerTousLesPanneaux();
            _panelMissions?.SetActive(true);
        }

        public void OuvrirPanelBoutique()
        {
            FermerTousLesPanneaux();
            _panelBoutique?.SetActive(true);
        }

        public void OuvrirPanelInventaire()
        {
            FermerTousLesPanneaux();
            _panelInventaire?.SetActive(true);
        }

        public void OuvrirPanelGarage()
        {
            FermerTousLesPanneaux();
            _panelGarage?.SetActive(true);
        }

        public void FermerTousLesPanneaux()
        {
            _panelMissions?.SetActive(false);
            _panelBoutique?.SetActive(false);
            _panelInventaire?.SetActive(false);
            _panelGarage?.SetActive(false);
            _popupErreur?.SetActive(false);

            // NOTE : Les panels MissionPanelUI et VehiclePanelUI
            // gèrent leur propre fermeture via leurs scripts dédiés
        }

        // ================================================================
        // HUD PERSISTANT — mis à jour par HubManager
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
        // POPUP ERREUR — appelé par HubManager, VehiclePanelUI, etc.
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
        // PROPRIÉTÉ — utilisée par PlayerController pour bloquer mouvement
        // ================================================================

        public bool UnPanneauEstOuvert =>
            (_panelMissions   != null && _panelMissions.activeSelf)   ||
            (_panelBoutique   != null && _panelBoutique.activeSelf)   ||
            (_panelInventaire != null && _panelInventaire.activeSelf) ||
            (_panelGarage     != null && _panelGarage.activeSelf)     ||
            (_popupErreur     != null && _popupErreur.activeSelf);
    }
}