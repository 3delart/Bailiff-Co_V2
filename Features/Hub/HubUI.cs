// ============================================================
// HubUI.cs — Bailiff & Co  V2
// Coordinateur de navigation Hub.
//
// CHANGEMENTS :
//   - Appel UIManager.SetPanelOpen(true/false) à l'ouverture
//     et fermeture de chaque panel (Boutique, Inventaire, Garage)
//   - FermerTousLesPanneaux() reset le compteur via SetPanelOpen
//     si un panel était ouvert
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class HubUI : MonoBehaviour
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
        [SerializeField] private GameObject _panelMissions;
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
        // ÉTAT — suivi du panel actuellement ouvert
        // ================================================================

        private GameObject _panelActuel = null;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            if (_panelMissions   != null) _panelMissions.SetActive(false);
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
            OuvrirPanel(_panelMissions);
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

        /// <summary>
        /// Ouvre un panel en fermant l'éventuel panel précédent.
        /// Gère SetPanelOpen pour ne pas faire +1 à chaque switch de panel.
        /// </summary>
        private void OuvrirPanel(GameObject panel)
        {
            if (panel == null) return;

            // Si un panel différent était ouvert, on le ferme sans décrémenter
            // le compteur UIManager (on va en ouvrir un autre juste après)
            if (_panelActuel != null && _panelActuel != panel)
                _panelActuel.SetActive(false);

            bool estait_ferme = _panelActuel == null || !_panelActuel.activeSelf;

            _panelActuel = panel;
            panel.SetActive(true);

            // N'incrémente que si aucun panel n'était ouvert avant
            if (estait_ferme)
                UIManager.Instance?.SetPanelOpen(true);
        }

        public void FermerTousLesPanneaux()
        {
            bool unPanelEtaitOuvert = _panelActuel != null && _panelActuel.activeSelf;

            if (_panelMissions   != null) _panelMissions.SetActive(false);
            if (_panelBoutique   != null) _panelBoutique.SetActive(false);
            if (_panelInventaire != null) _panelInventaire.SetActive(false);
            if (_panelGarage     != null) _panelGarage.SetActive(false);
            if (_popupErreur     != null) _popupErreur.SetActive(false);

            _panelActuel = null;

            // Décrémente uniquement si un panel était ouvert
            if (unPanelEtaitOuvert)
                UIManager.Instance?.SetPanelOpen(false);
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
            (_panelMissions   != null && _panelMissions.activeSelf)   ||
            (_panelBoutique   != null && _panelBoutique.activeSelf)   ||
            (_panelInventaire != null && _panelInventaire.activeSelf) ||
            (_panelGarage     != null && _panelGarage.activeSelf)     ||
            (_popupErreur     != null && _popupErreur.activeSelf);
    }
}
