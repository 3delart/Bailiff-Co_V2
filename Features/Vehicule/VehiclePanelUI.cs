// ============================================================
// VehiclePanelUI.cs — Bailiff & Co  V2
// Popup de confirmation de location véhicule.
// panelType = Blocking dans l'Inspector.
// Toute gestion input/curseur déléguée à UIManager via UIPanel.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BailiffCo.Hub
{
    public class VehiclePanelUI : UIPanel
    {
        // ================================================================
        // INJECTION DE DÉPENDANCES
        // ================================================================

        [Header("References")]
        [SerializeField] private HubManager _hubManager;

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

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Start()
        {
            _btnLouer?.onClick.AddListener(OnLouer);
            _btnAnnuler?.onClick.AddListener(OnAnnuler);

            if (_hubManager == null)
                Debug.LogWarning("[VehiclePanelUI] HubManager non injecté !");
        }

        private void OnDestroy()
        {
            _btnLouer?.onClick.RemoveAllListeners();
            _btnAnnuler?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // API PUBLIQUE
        // ================================================================

        /// <summary>
        /// Affiche le popup avec les données du véhicule et ouvre le panel.
        /// Remplace l'ancien OnEnable(VehiculeData, float) qui masquait le lifecycle Unity.
        /// </summary>
        public void Ouvrir(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[VehiclePanelUI] Ouvrir : vehicule null.");
                return;
            }

            PopulerFiche(vehicule, prixLocation);
            base.Ouvrir(); // → SetActive(true) → OnEnable → RegisterPanel
        }

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

        private void OnLouer()
        {
            if (_hubManager == null)
            {
                Debug.LogWarning("[VehiclePanelUI] OnLouer : HubManager manquant !");
                return;
            }
            _hubManager.ConfirmerLocationEtPartir();
        }

        private void OnAnnuler()
        {
            if (_hubManager == null)
            {
                Debug.LogWarning("[VehiclePanelUI] OnAnnuler : HubManager manquant !");
                return;
            }
            _hubManager.AnnulerLocationVehicule();
        }
    }
}
