// ============================================================
// HubPNJ.cs — Bailiff & Co  V2
// À mettre sur chaque PNJ de l'agence dans le Hub.
// Implémente IInteractable — le joueur appuie E pour interagir.
//
// CHANGEMENTS V2 :
//   - Plus de FindObjectOfType → injection [SerializeField]
//   - Appelle HubUI via _hubUI au lieu de FindObjectOfType
// ============================================================
using TMPro;
using UnityEngine;

namespace BailiffCo.Hub
{
    public class HubPNJ : MonoBehaviour, IInteractable
    {
        // ================================================================
        // ENUMS
        // ================================================================

        public enum TypePanneau
        {
            Missions,
            Boutique,
            Inventaire,
            Garage,
            Archiviste,
        }

        // ================================================================
        // CONFIGURATION
        // ================================================================

        [Header("Identité")]
        [SerializeField] private string      _nomPnj             = "Chef";
        [SerializeField] private string      _actionLabel        = "Parler";
        [SerializeField] private TypePanneau _typePanneau        = TypePanneau.Missions;

        [Header("Déblocage")]
        [SerializeField] private bool        _debloque           = true;
        [SerializeField] private string      _conditionDeblocage = "Terminer la campagne";

        [Header("Label flottant (optionnel)")]
        [SerializeField] private TextMeshPro _labelTexte;
        [SerializeField] private float       _hauteurLabel       = 2.2f;

        // ================================================================
        // LIFECYCLE
        // ================================================================

        private void Awake()
        {
            if (_labelTexte != null)
            {
                _labelTexte.transform.localPosition = Vector3.up * _hauteurLabel;
                MettreAJourLabel();
            }
        }

        private void Update()
        {
            // Billboard — label face à la caméra
            if (_labelTexte != null && Camera.main != null)
            {
                _labelTexte.transform.LookAt(
                    _labelTexte.transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up
                );
            }
        }

        // ================================================================
        // IINTERACTABLE
        // ================================================================

        public bool CanInteract(GameObject interacteur) => true; // toujours visible

        public void Interact(GameObject interacteur)
        {
            if (!_debloque)
            {
                HubManager.Instance?.AfficherErreur($"{_nomPnj} — Verrouillé\n{_conditionDeblocage}");
                return;
            }

            if (HubManager.Instance == null)
            {
                Debug.LogWarning($"[HubPNJ] {_nomPnj} : HubManager introuvable, impossible d'ouvrir le panel.");
                return;
            }

            switch (_typePanneau)
            {
                case TypePanneau.Missions:   HubManager.Instance.OuvrirPanelMissions(); break;
                case TypePanneau.Boutique:   HubManager.Instance.OuvrirPanelShop();     break;
                case TypePanneau.Inventaire: Debug.LogWarning("[HubPNJ] Panel Inventaire pas encore implémenté."); break;
                case TypePanneau.Garage:     Debug.LogWarning("[HubPNJ] Panel Garage pas encore implémenté.");     break;
                case TypePanneau.Archiviste: HubManager.Instance.OuvrirPanelMissions(); break;
            }
        }

        public string GetInteractionLabel()
        {
            if (!_debloque)
                return $"{_nomPnj} — 🔒 {_conditionDeblocage}";

            return $"{_nomPnj} — [E] {_actionLabel}";
        }

        // ================================================================
        // API PUBLIQUE — appelé par SaveSystem au chargement (futur)
        // ================================================================

        public void Debloquer()
        {
            _debloque = true;
            MettreAJourLabel();
        }

        // ================================================================
        // UTILITAIRES
        // ================================================================

        private void MettreAJourLabel()
        {
            if (_labelTexte == null) return;

            _labelTexte.text = _debloque
                ? $"{_nomPnj}\n<size=70%>[E] {_actionLabel}</size>"
                : $"{_nomPnj}\n<size=70%>🔒 {_conditionDeblocage}</size>";

            _labelTexte.color = _debloque
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        // ================================================================
        // PROPRIÉTÉS
        // ================================================================

        public bool   EstDebloque => _debloque;
        public string NomPnj      => _nomPnj;
    }
}