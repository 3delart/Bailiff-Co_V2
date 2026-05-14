// ============================================================
// PlayerInteractor.cs — Bailiff & Co  V2
// Raycast vers IInteractable, affiche le label contextuel,
// déclenche Interact() sur pression de E.
// Gère aussi le E maintenu pour MeubleInteractable.
// Détecte quel collider enfant est visé (ex: portes du véhicule).
//
// CHANGEMENTS V2 :
//   - PorteeInteraction vient de PlayerConfigData
//   - FurnitureInteractable → MeubleInteractable (standalone concrete class)
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
    private MeubleInteractable _meubleInteractable;

    // Cache du dernier label envoyé pour éviter les broadcasts inutiles
    private string _dernierLabel = string.Empty;

    private void OnEnable()
    {
        EventBus<OnInputStateChanged>.Subscribe(OnInputStateChanged);
    }

    private void OnDisable()
    {
        EventBus<OnInputStateChanged>.Unsubscribe(OnInputStateChanged);
    }

    private void OnInputStateChanged(OnInputStateChanged e)
    {
        // Release stuck meuble push when input is locked
        if (!e.Actif && _meubleInteractable != null)
        {
            _meubleInteractable.StopPushing();
            _meubleInteractable = null;
            Debug.Log("[PlayerInteractor] Meuble push released due to input lock.");
        }
    }

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

        #if UNITY_EDITOR
        Debug.DrawRay(origine.position, origine.forward * _config.InteractionRange, Color.red);
        #endif

        if (Physics.Raycast(origine.position, origine.forward,
            out RaycastHit hit, _config.InteractionRange, _layerInteractable))
        {
            _colliderVise = hit.collider;

            IInteractable interactable = null;
            foreach (var candidate in hit.collider.GetComponentsInParent<IInteractable>())
            {
                if (candidate.CanInteract(gameObject)) { interactable = candidate; break; }
            }

            if (interactable != null)
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

        if (label == _dernierLabel) return;
        _dernierLabel = label;
        EventBus<OnInteractionLabelChanged>.Raise(new OnInteractionLabelChanged { Label = label });
    }

    /// <summary>Réinitialise le cache pour forcer un broadcast au prochain frame.</summary>
    public void ForcerBroadcast() => _dernierLabel = null;

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
                if (_cibleCourante is MeubleInteractable meuble)
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
        if (_cibleCourante == null) return string.Empty;
        // Label spécial pendant la pousse
        if (_meubleInteractable != null)
            return _meubleInteractable.GetInteractionLabel();

        return _cibleCourante.GetInteractionLabel();
    }
}