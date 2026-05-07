// ============================================================
// MissionPanelUI.cs — Bailiff & Co  V2
// Gère UNIQUEMENT l'affichage de la fiche mission détaillée.
// panelType = Blocking dans l'Inspector.
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class MissionPanelUI : UIPanel
    {
        [Header("En-tête")]
        [SerializeField] private GameObject      _panelFicheMission;
        [SerializeField] private Image           _imgPortrait;
        [SerializeField] private TextMeshProUGUI _txtMissionName;
        [SerializeField] private TextMeshProUGUI _txtProprioInfo;
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

        /// <summary>
        /// Affiche la fiche de la mission et ouvre le panel.
        /// Remplace l'ancien OnEnable(MissionData) qui masquait le lifecycle Unity.
        /// </summary>
        public void Ouvrir(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogWarning("[MissionPanelUI] Ouvrir : mission null ignorée.");
                return;
            }

            PopulerFiche(mission);
            base.Ouvrir();                       // SetActive(true) → peut déclencher Start() qui cache _panelFicheMission
            _panelFicheMission?.SetActive(true); // toujours montré après, même si Start() venait de le cacher
        }
 
        public override void Fermer()
        {
            _panelFicheMission?.SetActive(false); // ← CORRECTION : cacher le panel visuel
            base.Fermer(); // → SetActive(false) → OnDisable → UnregisterPanel → UIManager restaure input
        }


        // ================================================================
        // POPULATION DE LA FICHE
        // ================================================================

        private void PopulerFiche(MissionData mission)
        {
            Debug.Log($"[MissionPanelUI] Affichage de la fiche mission : {mission.MissionName}");

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

            RefreshMenaces(mission.KnownThreats);
        }

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