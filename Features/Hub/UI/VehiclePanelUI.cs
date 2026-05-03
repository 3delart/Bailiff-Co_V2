// ============================================================
// VehiclePanelUI.cs — Bailiff & Co  V2
// NOUVEAU — Extrait de HubUI V1 (éclatement du God Object).
// Gère UNIQUEMENT le popup de confirmation de location véhicule.
//
// RESPONSABILITÉS :
//   - Afficher les détails du véhicule (nom, prix, capacité, avantages)
//   - Afficher le solde du joueur
//   - Boutons Louer / Annuler → appelle HubManager
//   - Aucune logique métier (vérification fonds = HubManager)
//
// SETUP UNITY :
//   Attacher sur le GameObject "PanelVehicule" (enfant de HubUI Canvas).
//   Assigner toutes les refs TMP + boutons dans l'Inspector.
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
            // Cache le panel au démarrage
            _panelVehicule?.SetActive(false);

            // Branche les boutons
            _btnLouer?.onClick.AddListener(OnLouer);
            _btnAnnuler?.onClick.AddListener(OnAnnuler);

            // Validation injection
            if (_hubManager == null)
            {
                Debug.LogWarning("[VehiclePanelUI] HubManager non injecté !");
            }
        }

        private void OnDestroy()
        {
            _btnLouer?.onClick.RemoveAllListeners();
            _btnAnnuler?.onClick.RemoveAllListeners();
        }

        // ================================================================
        // API PUBLIQUE — appelée par HubManager
        // ================================================================

        /// <summary>
        /// Affiche le popup de location véhicule.
        /// Appelé depuis HubManager.DemanderLocationVehicule().
        /// </summary>
        public void AfficherPopup(VehiculeData vehicule, float prixLocation)
        {
            if (vehicule == null)
            {
                Debug.LogWarning("[VehiclePanelUI] AfficherPopup : vehicule null.");
                return;
            }

            // Active le panel
            gameObject.SetActive(true);
            _panelVehicule?.SetActive(true);

            EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Hub });

            // Récupère le solde depuis GameManager
            float solde     = GameManager.Instance?.Argent ?? 0f;
            bool  peutLouer = solde >= prixLocation;

            // Remplit les infos véhicule
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

            // Prix
            if (_txtPrixLocation != null)
            {
                _txtPrixLocation.text = prixLocation <= 0f
                    ? "Gratuit"
                    : $"Location : {prixLocation:N0} € / mission";
            }

            // Solde avec warning si insuffisant
            if (_txtSoldeActuel != null)
            {
                _txtSoldeActuel.text = $"Ton solde : {solde:N0} €"
                                     + (peutLouer ? "" : "  ⚠ Fonds insuffisants");
            }

            // Illustration
            if (_imgVehicule != null && vehicule.UIIllustration != null)
                _imgVehicule.sprite = vehicule.UIIllustration;

            // Bouton Louer (actif uniquement si fonds suffisants)
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

        /// <summary>Ferme le popup véhicule sans sauvegarder.</summary>
        public void FermerPopup()
        {
            _panelVehicule?.SetActive(false);
            EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Mission });
        }

        // ================================================================
        // HANDLERS BOUTONS — branchés via onClick.AddListener
        // ================================================================

        private void OnLouer()
        {
            if (_hubManager == null)
            {
                Debug.LogWarning("[VehiclePanelUI] OnLouer : HubManager manquant !");
                return;
            }

            // Délègue la logique métier à HubManager
            // (vérification fonds, débit, lancement mission)
            _hubManager.ConfirmerLocationEtPartir();
        }

        private void OnAnnuler()
        {
            if (_hubManager == null)
            {
                Debug.LogWarning("[VehiclePanelUI] OnAnnuler : HubManager manquant !");
                return;
            }

            // Annule la sélection véhicule
            _hubManager.AnnulerLocationVehicule();
        }
    }
}