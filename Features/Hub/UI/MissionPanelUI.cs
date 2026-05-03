// ============================================================
// MissionPanelUI.cs — Bailiff & Co  V2
// Gère UNIQUEMENT l'affichage de la fiche mission détaillée.
//
// CHANGEMENTS :
//   - Suppression des raises OnContextChanged (plus nécessaires)
//   - Appel UIManager.SetPanelOpen(true/false) à l'ouverture/fermeture
//     → bloque automatiquement caméra + déplacements
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
            _panelFicheMission?.SetActive(false);
            _btnRetour?.onClick.AddListener(Fermer);
        }

        private void OnDestroy()
        {
            _btnRetour?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // API PUBLIQUE
        // ================================================================

        public void AfficherFiche(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogWarning("[MissionPanelUI] AfficherFiche : mission null ignorée.");
                return;
            }

            gameObject.SetActive(true);
            _panelFicheMission?.SetActive(true);

            // Bloque caméra + déplacements, libère le curseur
            UIManager.Instance?.SetPanelOpen(true);

            // Remplit les infos mission
            if (_txtNomMission != null)
                _txtNomMission.text = mission.MissionName;

            if (_txtQuota != null)
                _txtQuota.text = $"Quota minimum : {mission.MinimumQuotaValue:N0} €";

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

            if (_imgPortrait != null && owner.CartoonPortrait != null)
                _imgPortrait.sprite = owner.CartoonPortrait;

            if (_txtSecurite != null)
            {
                int niveau = owner.SecurityLevel;
                _txtSecurite.text = new string('★', niveau) + new string('☆', 5 - niveau);
            }
        }

        public void Fermer()
        {
            _panelFicheMission?.SetActive(false);

            // Débloque caméra + déplacements, verrouille le curseur
            UIManager.Instance?.SetPanelOpen(false);
        }

        public void OnRetour() => Fermer();
    }
}
