// ============================================================
// MissionPanelUI.cs — Bailiff & Co  V2
// NOUVEAU — Extrait de HubUI V1 (éclatement du God Object).
// Gère UNIQUEMENT l'affichage de la fiche mission détaillée.
//
// RESPONSABILITÉS :
//   - Afficher les détails d'une MissionData
//   - Bouton "Retour" vers la liste des missions
//   - Aucune logique métier (pas de sélection, HubManager s'en charge)
//
// SETUP UNITY :
//   Attacher sur le GameObject "PanelFicheMission" (enfant de HubUI Canvas).
//   Assigner toutes les refs TMP dans l'Inspector.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class MissionPanelUI : MonoBehaviour
    {
        // ================================================================
        // RÉFÉRENCES UI
        // ================================================================

        [Header("Fiche Mission")]
        [SerializeField] private GameObject      _panelFicheMission;
        [SerializeField] private TextMeshProUGUI _txtNomMission;
        [SerializeField] private TextMeshProUGUI _txtNomProprio;
        [SerializeField] private TextMeshProUGUI _txtTrait;
        [SerializeField] private TextMeshProUGUI _txtSecurite;
        [SerializeField] private TextMeshProUGUI _txtCitation;
        [SerializeField] private TextMeshProUGUI _txtQuota;
        [SerializeField] private Image           _imgPortrait;
        [SerializeField] private Button          _btnRetour;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            // Cache le panel au démarrage
            _panelFicheMission?.SetActive(false);

            // Branche le bouton retour
            _btnRetour?.onClick.AddListener(Fermer);
        }

        private void OnDestroy()
        {
            _btnRetour?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // API PUBLIQUE — appelée par HubManager
        // ================================================================

        /// <summary>
        /// Affiche la fiche détaillée d'une mission.
        /// Appelé depuis HubManager.SelectionnerMission().
        /// </summary>
        public void AfficherFiche(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogWarning("[MissionPanelUI] AfficherFiche : mission null ignorée.");
                return;
            }

            // Active le panel
            _panelFicheMission?.SetActive(true);

            // Remplit les infos mission
            if (_txtNomMission != null)
                _txtNomMission.text = mission.MissionName;

            if (_txtQuota != null)
                _txtQuota.text = $"Quota minimum : {mission.MinimumQuotaValue:N0} €";

            // Propriétaire (Owner)
            var owner = mission.Owner;
            if (owner == null)
            {
                Debug.LogWarning($"[MissionPanelUI] Mission '{mission.MissionName}' sans Owner !");
                return;
            }

            if (_txtNomProprio != null)
                _txtNomProprio.text = owner.OwnerName;

            if (_txtTrait != null)
                _txtTrait.text = owner.PersonalityTrait;

            if (_txtCitation != null)
                _txtCitation.text = $"« {owner.ClueQuote} »";

            // Portrait
            if (_imgPortrait != null && owner.CartoonPortrait != null)
                _imgPortrait.sprite = owner.CartoonPortrait;

            // Niveau de sécurité (étoiles)
            if (_txtSecurite != null)
            {
                int niveau = owner.SecurityLevel;
                _txtSecurite.text = new string('★', niveau) + new string('☆', 5 - niveau);
            }
        }

        /// <summary>Ferme la fiche et retourne à la liste des missions.</summary>
        public void Fermer()
        {
            _panelFicheMission?.SetActive(false);
        }

        // ================================================================
        // HANDLERS BOUTONS — branchés dans l'Inspector si besoin
        // ================================================================

        /// <summary>Bouton "Retour" → On Click () dans l'Inspector.</summary>
        public void OnRetour()
        {
            Fermer();
        }
    }
}