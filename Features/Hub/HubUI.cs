// ============================================================
// HubUI.cs — Bailiff & Co  V2
// Coordinateur de navigation Hub.
//
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
        [SerializeField] private MissionListUI _missionListUI;  // ← TYPE CORRIGÉ
        [SerializeField] private GameObject _panelBoutique;
        [SerializeField] private GameObject _panelInventaire;
        [SerializeField] private GameObject _panelGarage;

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
            // Fermer tous les panels au démarrage
            if (_missionListUI   != null) _missionListUI.gameObject.SetActive(false);
            if (_panelBoutique   != null) _panelBoutique.SetActive(false);
            if (_panelInventaire != null) _panelInventaire.SetActive(false);
            if (_panelGarage     != null) _panelGarage.SetActive(false);
            if (_popupErreur     != null) _popupErreur.SetActive(false);

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
            Debug.Log($"[HubUI] OuvrirPanelMissions appelé. _missionListUI = {_missionListUI != null}");
            
            if (_missionListUI == null)
            {
                Debug.LogError("[HubUI] _missionListUI est NULL ! Vérifier l'Inspector.");
                return;
            }
            
            _missionListUI.Ouvrir();  // ← APPEL CORRIGÉ
        }

        public void OuvrirPanelBoutique()
        {
            OuvrirPanel(_panelBoutique);
        }

        public void OuvrirPanelInventaire()
        {
            OuvrirPanel(_panelInventaire);
        }

        public void OuvrirPanelGarage()
        {
            OuvrirPanel(_panelGarage);
        }

        private void OuvrirPanel(GameObject panel)
        {
            panel?.GetComponent<UIPanel>()?.Ouvrir();
        }

        public void FermerTousLesPanneaux()
        {
            _missionListUI?.Fermer();
            _panelBoutique?.GetComponent<UIPanel>()?.Fermer();
            _panelInventaire?.GetComponent<UIPanel>()?.Fermer();
            _panelGarage?.GetComponent<UIPanel>()?.Fermer();
            _popupErreur?.SetActive(false); // popup erreur reste SetActive (pas de UIPanel dessus)
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
            (_missionListUI   != null && _missionListUI.EstOuvert)   ||
            (_panelBoutique   != null && _panelBoutique.activeSelf)   ||
            (_panelInventaire != null && _panelInventaire.activeSelf) ||
            (_panelGarage     != null && _panelGarage.activeSelf)     ||
            (_popupErreur     != null && _popupErreur.activeSelf);
    }
}