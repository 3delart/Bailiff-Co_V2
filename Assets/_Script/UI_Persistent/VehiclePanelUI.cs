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
        // RÉFÉRENCES UI — HEADER
        // ================================================================

        [Header("Header")]
        [SerializeField] private Image           _imgIcon;
        [SerializeField] private TextMeshProUGUI _txtVehicleName;
        [SerializeField] private TextMeshProUGUI _txtVehicleDesc;

        // ================================================================
        // RÉFÉRENCES UI — INFOS VÉHICULE
        // ================================================================

        [Header("Infos Véhicule")]
        [SerializeField] private TextMeshProUGUI _txtCapacityValue;
        [SerializeField] private TextMeshProUGUI _txtLocationValue;
        [SerializeField] private TextMeshProUGUI _txtAdvantageDesc;
        [SerializeField] private TextMeshProUGUI _txtDisadvantageDesc;
        [SerializeField] private TextMeshProUGUI _txtTipDesc;

        // ================================================================
        // RÉFÉRENCES UI — OPTIONS
        // ================================================================

        [Header("Options")]
        [SerializeField] private Transform       _containerOptions;
        [SerializeField] private GameObject      _prefabOptionVehicle;
        private List<Toggle> _optionToggles = new();

        // ================================================================
        // RÉFÉRENCES UI — PRICE SUMMARY CARD
        // ================================================================

        [Header("Price Summary Card")]
        [SerializeField] private TextMeshProUGUI _basePriceValue;
        [SerializeField] private TextMeshProUGUI _optionsPriceValue;
        [SerializeField] private TextMeshProUGUI _totalPriceValue;

        // ================================================================
        // RÉFÉRENCES UI — PLAYER BALANCE CARD
        // ================================================================

        [Header("Player Balance Card")]
        [SerializeField] private TextMeshProUGUI _balanceValue;
        [SerializeField] private Image           _fundsStatusIcon;

        // ================================================================
        // RÉFÉRENCES UI — BUTTONS
        // ================================================================

        [Header("Buttons")]
        [SerializeField] private Button          _btnLouer;
        [SerializeField] private Button          _btnAnnuler;

        // ================================================================
        // STATE
        // ================================================================

        private float _baseRentalPrice = 0f;

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
            _baseRentalPrice = prixLocation;
            float solde = GameManager.Instance?.Argent ?? 0f;
            float totalPrice = GetTotalPrice();
            bool peutLouer = solde >= totalPrice;

            if (_txtVehicleName != null) _txtVehicleName.text = vehicule.VehicleName;
            if (_txtVehicleDesc != null) _txtVehicleDesc.text = vehicule.Description;
            if (_imgIcon != null && vehicule.UIIllustration != null) _imgIcon.sprite = vehicule.UIIllustration;

            if (_txtCapacityValue != null) _txtCapacityValue.text = $"{vehicule.TrunkSurfaceM2:F1} m²";
            if (_txtAdvantageDesc != null) _txtAdvantageDesc.text = vehicule.AdvantageDescription;
            if (_txtDisadvantageDesc != null) _txtDisadvantageDesc.text = vehicule.DisadvantageDescription;
            if (_txtTipDesc != null) _txtTipDesc.text = vehicule.TipDescription;

            RefreshPriceSummary();
            RefreshBalance(solde, peutLouer);

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
        // RAFRAÎCHISSEMENT PRIX & SOLDE
        // ================================================================

        private float GetTotalPrice()
        {
            return HubManager.Instance?.GetTotalPrice() ?? _baseRentalPrice;
        }

        private void RefreshPriceSummary()
        {
            float basePrice = _baseRentalPrice;
            float optionsPrice = (GetTotalPrice() - basePrice);

            if (_basePriceValue != null)
                _basePriceValue.text = basePrice <= 0f ? "Gratuit" : $"{basePrice:N0} €";

            if (_optionsPriceValue != null)
                _optionsPriceValue.text = optionsPrice <= 0f ? "Aucune" : $"{optionsPrice:N0} €";

            if (_totalPriceValue != null)
            {
                float total = GetTotalPrice();
                _totalPriceValue.text = total <= 0f ? "Gratuit" : $"{total:N0} €";
            }
        }

        private void RefreshBalance(float solde, bool peutLouer)
        {
            if (_balanceValue != null)
                _balanceValue.text = $"{solde:N0} €";

            if (_fundsStatusIcon != null)
                _fundsStatusIcon.gameObject.SetActive(!peutLouer);
        }

        public void RefreshTotalPrice()
        {
            float solde = GameManager.Instance?.Argent ?? 0f;
            float totalPrice = GetTotalPrice();
            bool peutLouer = solde >= totalPrice;

            RefreshPriceSummary();
            RefreshBalance(solde, peutLouer);

            if (_btnLouer != null)
                _btnLouer.interactable = peutLouer;
        }

        // ================================================================
        // POPULATION AVEC OPTIONS
        // ================================================================

        public void Ouvrir(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[VehiclePanelUI] Ouvrir : vehicule null.");
                return;
            }

            PopulerFiche(vehicule, prixLocation);
            CreateOptionRows(vehicule);
            RefreshTotalPrice();

            base.Ouvrir();
        }

        private void CreateOptionRows(VehiculeData vehicule)
        {
            ClearOptionRows();

            if (vehicule.AvailableOptions == null || vehicule.AvailableOptions.Count == 0)
                return;

            foreach (var option in vehicule.AvailableOptions)
            {
                if (_prefabOptionVehicle == null || _containerOptions == null)
                    continue;

                var row = Instantiate(_prefabOptionVehicle, _containerOptions);
                var toggle = row.GetComponentInChildren<Toggle>();
                if (toggle == null)
                {
                    Debug.LogWarning($"[VehiclePanelUI] PrefabOptionVehicle n'a pas de Toggle enfant!");
                    continue;
                }

                var optionLabel = row.transform.Find("OptionLabel")?.GetComponent<TextMeshProUGUI>();
                var optionPrice = row.transform.Find("OptionPrice")?.GetComponent<TextMeshProUGUI>();

                if (optionLabel != null) optionLabel.text = option.OptionName;
                if (optionPrice != null) optionPrice.text = $"{option.Price}€";

                toggle.onValueChanged.AddListener(isOn => OnOptionToggled(option));
                _optionToggles.Add(toggle);
            }
        }

        private void ClearOptionRows()
        {
            if (_containerOptions == null)
                return;

            foreach (Transform child in _containerOptions)
                Destroy(child.gameObject);

            _optionToggles.Clear();
        }

        private void OnOptionToggled(VehicleOption option)
        {
            HubManager.Instance?.ToggleOption(option);
            RefreshTotalPrice();
        }
    }
}
