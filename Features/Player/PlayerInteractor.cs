// ============================================================
// PlayerInteractor.cs — Bailiff & Co  V2
// Raycast vers IInteractable, affiche le label contextuel,
// déclenche Interact() sur pression de E.
// Gère aussi le E maintenu pour FurnitureInteractable.
// Détecte quel collider enfant est visé (ex: portes du véhicule).
//
// CHANGEMENTS V2 :
//   - PorteeInteraction vient de PlayerConfigData
//   - MeubleInteractable → FurnitureInteractable
//   - Vehicule → VehicleRuntime
//   - NOUVEAU : broadcast du label via EventBus (OnInteractionLabelChanged)
//     pour que LabelInteractionUI n'ait plus besoin de FindObjectOfType
// ============================================================
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfigData _config;

    [Header("Références")]
    [SerializeField] private LayerMask _layerInteractable;
    [SerializeField] private Transform _camera;

    private IInteractable _cibleCourante;
    private Collider      _colliderVise;

    // Référence au meuble en cours de pousse
    private FurnitureInteractable _meubleInteractable;

    // Cache du dernier label envoyé pour éviter les broadcasts inutiles
    private string _dernierLabel = string.Empty;

    private void Update()
    {
        DetecterCible();
        BroadcastLabel();
        GererInteraction();
        GererPousse();
    }

    // ================================================================
    // DÉTECTION CIBLE
    // ================================================================

    private void DetecterCible()
    {
        // Si on pousse un meuble, pas besoin de chercher une autre cible
        if (_meubleInteractable != null) return;

        if (_config == null)
        {
            Debug.LogError("[PlayerInteractor] PlayerConfigData manquant !");
            return;
        }

        Transform origine = _camera != null ? _camera : transform;

        if (Physics.Raycast(origine.position, origine.forward,
            out RaycastHit hit, _config.InteractionRange, _layerInteractable))
        {
            _colliderVise = hit.collider;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                _cibleCourante = interactable;
                return;
            }
        }

        _cibleCourante = null;
        _colliderVise  = null;
    }

    // ================================================================
    // BROADCAST LABEL (NOUVEAU V2)
    // ================================================================

    private void BroadcastLabel()
    {
        string label = GetLabelCourant();

        // Éviter de broadcaster si le label n'a pas changé
        if (label == _dernierLabel) return;

        _dernierLabel = label;
        EventBus<OnInteractionLabelChanged>.Raise(new OnInteractionLabelChanged { Label = label });
    }

    // ================================================================
    // INTERACTION NORMALE (E pressé)
    // ================================================================

    private void GererInteraction()
    {
        if (_meubleInteractable != null) return;

        KeyCode toucheInteragir = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Interagir)
            : KeyCode.E;

        if (_cibleCourante != null && Input.GetKeyDown(toucheInteragir))
        {
            if (_cibleCourante.CanInteract(gameObject))
            {
                if (_cibleCourante is VehicleRuntime vehicule)
                    vehicule.SetTargetCollider(_colliderVise);

                if (_cibleCourante is FurnitureInteractable meuble)
                {
                    _meubleInteractable = meuble;
                    _meubleInteractable.StartPushing(gameObject);
                    return;
                }

                _cibleCourante.Interact(gameObject);
            }
        }
    }

    // ================================================================
    // POUSSE MEUBLE (E maintenu → continue, E relâché → stop)
    // ================================================================

    private void GererPousse()
    {
        if (_meubleInteractable == null) return;

        KeyCode toucheInteragir = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Interagir)
            : KeyCode.E;

        if (Input.GetKeyUp(toucheInteragir))
        {
            _meubleInteractable.StopPushing();
            _meubleInteractable = null;
        }
    }

    // ================================================================
    // LABEL HUD
    // ================================================================

    /// <summary>Retourné au PlayerController pour réduire la vitesse du joueur.</summary>
    public float MultiplicateurVitesseMeuble
        => _meubleInteractable != null ? _meubleInteractable.SpeedMultiplier : 1f;  // ← nom anglais V2

    public string GetLabelCourant()
    {
        // Label spécial pendant la pousse
        if (_meubleInteractable != null)
            return _meubleInteractable.GetInteractionLabel();

        if (_cibleCourante == null) return string.Empty;

        if (_cibleCourante is VehicleRuntime vehicule)
            vehicule.SetTargetCollider(_colliderVise);

        return _cibleCourante.GetInteractionLabel();
    }
}