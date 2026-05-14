// ============================================================
// VehiclePanelUI.cs — Bailiff & Co  V2
// Popup de confirmation de location véhicule.
// panelType = Blocking dans l'Inspector.
// Toute gestion input/curseur déléguée à UIManager via UIPanel.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BailiffCo.Hub
{
    public class VehiclePanelUI : UIPanel
    {
        // ================================================================
        // RÉFÉRENCES UI
        // ================================================================

        [Header("Panel Véhicule (popup location)")]
        [SerializeField] private TextMeshProUGUI _txtNomVehicule;
        [SerializeField] private TextMeshProUGUI _txtPrixLocation;
        [SerializeField] private TextMeshProUGUI _txtCapacite;
        [SerializeField] private TextMeshProUGUI _txtAvantage;
        [SerializeField] private TextMeshProUGUI _txtInconvenient;
        [SerializeField] private TextMeshProUGUI _txtAstuce;
        [SerializeField] private TextMeshProUGUI _txtSoldeActuel;
        [SerializeField] private Image           _imgVehicule;
        [SerializeField] private Button          _btnLouer;
        [SerializeField] private Button          _btnAnnuler;
        [SerializeField] private Transform       _containerOptions;
        [SerializeField] private GameObject      _prefabOptionRow;
        private List<Toggle> _optionToggles = new();

        // ================================================================
        // LIFECYCLE
        // ================================================================

        protected override void Awake()
        {
            base.Awake();
            // Initialisation dans Awake, pas Start
            _btnLouer?.onClick.AddListener(OnLouer);
            _btnAnnuler?.onClick.AddListener(OnAnnuler);
        }

        private void OnDestroy()
        {
            _btnLouer?.onClick.RemoveAllListeners();
            _btnAnnuler?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // API PUBLIQUE
        // ================================================================

        public override void Fermer()
        {
            base.Fermer(); // → SetActive(false) → OnDisable → UnregisterPanel → UIManager restaure input
        }

        // ================================================================
        // POPULATION DE LA FICHE
        // ================================================================

        private void PopulerFiche(VehiculeData vehicule, float prixLocation)
        {
            float solde     = GameManager.Instance?.Argent ?? 0f;
            bool  peutLouer = solde >= prixLocation;

            if (_txtNomVehicule  != null) _txtNomVehicule.text  = vehicule.VehicleName;
            if (_txtCapacite     != null) _txtCapacite.text     = $"Capacité : {vehicule.ObjectCapacity} objets";
            if (_txtAvantage     != null) _txtAvantage.text     = $"✓ {vehicule.AdvantageDescription}";
            if (_txtInconvenient != null) _txtInconvenient.text = $"✗ {vehicule.DisadvantageDescription}";
            if (_txtAstuce       != null) _txtAstuce.text       = $"💡 {vehicule.TipDescription}";

            if (_txtPrixLocation != null)
                _txtPrixLocation.text = prixLocation <= 0f
                    ? "Gratuit"
                    : $"Location : {prixLocation:N0} € / mission";

            if (_txtSoldeActuel != null)
                _txtSoldeActuel.text = $"Ton solde : {solde:N0} €"
                                     + (peutLouer ? "" : "  ⚠ Fonds insuffisants");

            if (_imgVehicule != null && vehicule.UIIllustration != null)
                _imgVehicule.sprite = vehicule.UIIllustration;

            if (_btnLouer != null)
            {
                _btnLouer.interactable = peutLouer;
                var txtBouton = _btnLouer.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBouton != null)
                    txtBouton.text = prixLocation <= 0f ? "Partir (Gratuit)" : "Louer & Partir";
            }
        }

        // ================================================================
        // HANDLERS BOUTONS
        // ================================================================

        private void OnLouer()   => HubManager.Instance?.ConfirmerLocationEtPartir();
        private void OnAnnuler() => HubManager.Instance?.AnnulerLocationVehicule();

        // ================================================================
        // RAFRAÎCHISSEMENT PRIX
        // ================================================================

        public void RefreshTotalPrice()
        {
            float totalPrice = HubManager.Instance?.GetTotalPrice() ?? 0f;
            if (_txtPrixLocation != null)
            {
                _txtPrixLocation.text = totalPrice <= 0f
                    ? "Gratuit"
                    : $"Location : {totalPrice:N0} € / mission";
            }
        }

        // ================================================================
        // POPULATION AVEC OPTIONS
        // ================================================================

        /// <summary>
        /// Affiche le popup avec les données du véhicule, options, et ouvre le panel.
        /// </summary>
        public void Ouvrir(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[VehiclePanelUI] Ouvrir : vehicule null.");
                return;
            }

            PopulerFiche(vehicule, prixLocation);

            // Clear old option rows
            if (_containerOptions != null)
            {
                foreach (Transform child in _containerOptions)
                {
                    Destroy(child.gameObject);
                }
            }
            _optionToggles.Clear();

            // Create toggle row for each available option
            if (vehicule.AvailableOptions != null && vehicule.AvailableOptions.Count > 0)
            {
                foreach (var option in vehicule.AvailableOptions)
                {
                    if (_prefabOptionRow == null || _containerOptions == null) continue;

                    var row = Instantiate(_prefabOptionRow, _containerOptions);
                    var toggle = row.GetComponent<Toggle>();

                    // Find TMP_Text children for label and price
                    var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 2)
                    {
                        texts[0].text = option.OptionName;
                        texts[1].text = $"{option.Price}€";
                    }

                    // Wire toggle to HubManager
                    if (toggle != null)
                    {
                        toggle.onValueChanged.AddListener((isOn) =>
                        {
                            HubManager.Instance?.ToggleOption(option);
                            RefreshTotalPrice();
                        });

                        _optionToggles.Add(toggle);
                    }
                }
            }

            // Refresh total to include base price
            RefreshTotalPrice();

            base.Ouvrir(); // → SetActive(true) → OnEnable → RegisterPanel
        }
    }
}
