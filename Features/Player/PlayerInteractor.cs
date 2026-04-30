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

    private void Update()
    {
        DetecterCible();
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
                    vehicule.SetColliderVise(_colliderVise);

                if (_cibleCourante is FurnitureInteractable meuble)
                {
                    _meubleInteractable = meuble;
                    _meubleInteractable.CommencerPousse(gameObject);
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
            _meubleInteractable.StopperPousse();
            _meubleInteractable = null;
        }
    }

    // ================================================================
    // LABEL HUD
    // ================================================================

    /// <summary>Retourné au PlayerController pour réduire la vitesse du joueur.</summary>
    public float MultiplicateurVitesseMeuble
        => _meubleInteractable != null ? _meubleInteractable.MultiplicateurVitesse : 1f;

    public string GetLabelCourant()
    {
        // Label spécial pendant la pousse
        if (_meubleInteractable != null)
            return _meubleInteractable.GetInteractionLabel();

        if (_cibleCourante == null) return string.Empty;

        if (_cibleCourante is VehicleRuntime vehicule)
            vehicule.SetColliderVise(_colliderVise);

        return _cibleCourante.GetInteractionLabel();
    }
}