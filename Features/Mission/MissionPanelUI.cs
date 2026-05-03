// ============================================================
// MissionPanelUI.cs — Bailiff & Co  V2
// Gère UNIQUEMENT l'affichage de la fiche mission détaillée.
// Pas de blocage caméra/déplacements — c'est un élément UI
// intégré au Hub, pas un panel modal.
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class MissionPanelUI : MonoBehaviour
    {
        [Header("En-tête")]
        [SerializeField] private GameObject      _panelFicheMission;
        [SerializeField] private Image           _imgPortrait;
        [SerializeField] private TextMeshProUGUI _txtMissionName;
        [SerializeField] private TextMeshProUGUI _txtProprioInfo;   // "Name, Age\nProfession"
        [SerializeField] private TextMeshProUGUI _txtCitation;

        [Header("Habitation")]
        [SerializeField] private TextMeshProUGUI _txtHabitationType;
        [SerializeField] private TextMeshProUGUI _txtHabitationSurface;
        [SerializeField] private TextMeshProUGUI _txtHabitationEtage;
        [SerializeField] private TextMeshProUGUI _txtHabitationAcces;

        [Header("Propriétaire")]
        [SerializeField] private TextMeshProUGUI _txtReactivite;
        [SerializeField] private TextMeshProUGUI _txtMethode;
        [SerializeField] private TextMeshProUGUI _txtSociabilite;
        [SerializeField] private TextMeshProUGUI _txtSecurite;

        [Header("Menaces Connues")]
        [SerializeField] private Transform  _menaceContent;
        [SerializeField] private GameObject _prefabMenaceTag;

        [Header("Objectif / Quota")]
        [SerializeField] private TextMeshProUGUI _txtObjectif;
        [SerializeField] private TextMeshProUGUI _txtQuotaValue;

        [Header("Navigation")]
        [SerializeField] private Button _btnRetour;

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
            Debug.Log($"[MissionPanelUI] Affichage de la fiche mission : {mission.MissionName}");

            // En-tête
            if (_txtMissionName != null) _txtMissionName.text = mission.MissionName;
            if (_txtQuotaValue  != null) _txtQuotaValue.text  = $"{mission.MinimumQuotaValue:N0} €";
            if (_txtObjectif    != null) _txtObjectif.text    = mission.ObjectiveDescription;

            var owner = mission.Owner;
            if (owner == null)
            {
                Debug.LogWarning($"[MissionPanelUI] Mission '{mission.MissionName}' sans Owner !");
                return;
            }

            if (_imgPortrait != null && owner.CartoonPortrait != null)
                _imgPortrait.sprite = owner.CartoonPortrait;

            if (_txtProprioInfo != null)
                _txtProprioInfo.text = $"{owner.OwnerName}, {owner.Age}\n{owner.Profession}";

            if (_txtCitation != null)
                _txtCitation.text = $"« {owner.ClueQuote} »";

            // Habitation
            var hab = mission.Habitation;
            if (hab != null)
            {
                if (_txtHabitationType    != null) _txtHabitationType.text    = hab.Type;
                if (_txtHabitationSurface != null) _txtHabitationSurface.text = $"{hab.Surface} m²";
                if (_txtHabitationEtage   != null) _txtHabitationEtage.text   = hab.Etage;
                if (_txtHabitationAcces   != null) _txtHabitationAcces.text   = hab.Acces;
            }

            // Propriétaire
            if (_txtReactivite  != null) _txtReactivite.text  = $"{owner.Reactivity}/10";
            if (_txtMethode     != null) _txtMethode.text     = $"{owner.Method}/10";
            if (_txtSociabilite != null) _txtSociabilite.text = $"{owner.Sociability}/10";

            if (_txtSecurite != null)
            {
                int n = owner.SecurityLevel;
                _txtSecurite.text = new string('★', n) + new string('☆', 5 - n);
            }

            // Menaces
            RefreshMenaces(mission.KnownThreats);
        }

        public void Fermer()
        {
            _panelFicheMission?.SetActive(false);
        }

        public void OnRetour() => Fermer();

        // ================================================================
        // HELPERS
        // ================================================================

        private void RefreshMenaces(List<string> threats)
        {
            if (_menaceContent == null || _prefabMenaceTag == null) return;

            foreach (Transform child in _menaceContent)
                Destroy(child.gameObject);

            if (threats == null || threats.Count == 0) return;

            foreach (string threat in threats)
            {
                var tag = Instantiate(_prefabMenaceTag, _menaceContent);
                var lbl = tag.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = threat;
            }
        }
    }
}