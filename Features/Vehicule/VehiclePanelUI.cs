// ============================================================
// VehiclePanelUI.cs — Bailiff & Co  V2
// Gère UNIQUEMENT le popup de confirmation de location véhicule.
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
    public class VehiclePanelUI : MonoBehaviour
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
        [SerializeField] private GameObject      _panelVehicule;
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
            _panelVehicule?.SetActive(false);
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

        public void AfficherPopup(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[VehiclePanelUI] AfficherPopup : vehicule null.");
                return;
            }

            gameObject.SetActive(true);
            _panelVehicule?.SetActive(true);

            // Bloque caméra + déplacements, libère le curseur
            UIManager.Instance?.SetPanelOpen(true);

            float solde     = GameManager.Instance?.Argent ?? 0f;
            bool  peutLouer = solde >= prixLocation;

            if (_txtNomVehicule != null)
                _txtNomVehicule.text = vehicule.VehicleName;

            if (_txtCapacite != null)
                _txtCapacite.text = $"Capacité : {vehicule.ObjectCapacity} objets";

            if (_txtAvantage != null)
                _txtAvantage.text = $"✓ {vehicule.AdvantageDescription}";

            if (_txtInconvenient != null)
                _txtInconvenient.text = $"✗ {vehicule.DisadvantageDescription}";

            if (_txtAstuce != null)
                _txtAstuce.text = $"💡 {vehicule.TipDescription}";

            if (_txtPrixLocation != null)
            {
                _txtPrixLocation.text = prixLocation <= 0f
                    ? "Gratuit"
                    : $"Location : {prixLocation:N0} € / mission";
            }

            if (_txtSoldeActuel != null)
            {
                _txtSoldeActuel.text = $"Ton solde : {solde:N0} €"
                                     + (peutLouer ? "" : "  ⚠ Fonds insuffisants");
            }

            if (_imgVehicule != null && vehicule.UIIllustration != null)
                _imgVehicule.sprite = vehicule.UIIllustration;

            if (_btnLouer != null)
            {
                _btnLouer.interactable = peutLouer;

                var txtBouton = _btnLouer.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBouton != null)
                {
                    txtBouton.text = prixLocation <= 0f
                        ? "Partir (Gratuit)"
                        : "Louer & Partir";
                }
            }
        }

        public void FermerPopup()
        {
            _panelVehicule?.SetActive(false);

            // Débloque caméra + déplacements
            UIManager.Instance?.SetPanelOpen(false);
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
