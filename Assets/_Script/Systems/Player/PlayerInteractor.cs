// ============================================================
// PlayerInteractor.cs — Bailiff & Co  V2
// Raycast to IInteractable, display contextual label,
// trigger Interact() on E press.
// Also handles held E for FurnitureInteractable pushing.
// Detects which child collider is aimed (e.g. vehicle doors).
//
// V2 CHANGES:
//   - InteractionRange from PlayerConfigData
//   - FurnitureInteractable (standalone concrete class)
//   - Vehicle → VehicleRuntime
//   - EventBus broadcast of label (OnInteractionLabelChanged)
//     so LabelInteractionUI no longer needs FindObjectOfType
// ============================================================
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfigData _config;

    [Header("References")]
    [SerializeField] private LayerMask _layerInteractable;
    [SerializeField] private Transform _camera;

    private IInteractable        _currentTarget;
    private Collider             _aimedCollider;

    // Reference to furniture being pushed
    private FurnitureInteractable _furniture;

    // Cache of last label sent to avoid unnecessary broadcasts
    private string _lastLabel = string.Empty;

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
        // Release stuck furniture push when input is locked
        if (!e.Actif && _furniture != null)
        {
            _furniture.StopPushing();
            _furniture = null;
            Debug.Log("[PlayerInteractor] Furniture push released due to input lock.");
        }
    }

    private void Update()
    {
        DetectTarget();
        BroadcastLabel();
        HandleInteraction();
        HandlePush();
    }

    // ================================================================
    // TARGET DETECTION
    // ================================================================

    private void DetectTarget()
    {
        // If pushing furniture, no need to search for another target
        if (_furniture != null) return;

        if (_config == null)
        {
            Debug.LogError("[PlayerInteractor] PlayerConfigData missing!");
            return;
        }

        Transform origin = _camera != null ? _camera : transform;

        #if UNITY_EDITOR
        Debug.DrawRay(origin.position, origin.forward * _config.InteractionRange, Color.red);
        #endif

        if (Physics.Raycast(origin.position, origin.forward,
            out RaycastHit hit, _config.InteractionRange, _layerInteractable))
        {
            _aimedCollider = hit.collider;

            IInteractable interactable = null;
            foreach (var candidate in hit.collider.GetComponentsInParent<IInteractable>())
            {
                if (candidate.CanInteract(gameObject)) { interactable = candidate; break; }
            }

            if (interactable != null)
            {
                _currentTarget = interactable;
                return;
            }
        }

        _currentTarget = null;
        _aimedCollider = null;
    }

    // ================================================================
    // LABEL BROADCAST (V2)
    // ================================================================

    private void BroadcastLabel()
    {
        string label = GetCurrentLabel();

        if (label == _lastLabel) return;
        _lastLabel = label;
        EventBus<OnInteractionLabelChanged>.Raise(new OnInteractionLabelChanged { Label = label });
    }

    /// <summary>Reset cache to force broadcast on next frame.</summary>
    public void ForceBroadcast() => _lastLabel = null;

    // ================================================================
    // NORMAL INTERACTION (E pressed)
    // ================================================================

    private void HandleInteraction()
    {
        if (_furniture != null) return;

        KeyCode interactKey = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Interagir)
            : KeyCode.E;

        if (_currentTarget != null && Input.GetKeyDown(interactKey))
        {
            if (_currentTarget.CanInteract(gameObject))
            {
                if (_currentTarget is FurnitureInteractable furniture)
                {
                    _furniture = furniture;
                    _furniture.StartPushing(gameObject);
                    return;
                }

                _currentTarget.Interact(gameObject);
            }
        }
    }

    // ================================================================
    // FURNITURE PUSH (E held → continue, E released → stop)
    // ================================================================

    private void HandlePush()
    {
        if (_furniture == null) return;

        KeyCode interactKey = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Interagir)
            : KeyCode.E;

        if (Input.GetKeyUp(interactKey))
        {
            _furniture.StopPushing();
            _furniture = null;
        }
    }

    // ================================================================
    // HUD LABEL
    // ================================================================

    /// <summary>Returned to PlayerController to reduce player speed.</summary>
    public float FurnitureSpeedMultiplier
        => _furniture != null ? _furniture.SpeedMultiplier : 1f;

    public string GetCurrentLabel()
    {
        if (_currentTarget == null) return string.Empty;
        // Special label during push
        if (_furniture != null)
            return _furniture.GetInteractionLabel();

        return _currentTarget.GetInteractionLabel();
    }
}