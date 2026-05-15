// ============================================================
// FurnitureInteractable.cs — Bailiff & Co  V2
// Pushable furniture in room (dresser, fridge, cabinet…).
// Hold E → player pushes or pulls furniture.
// Furniture follows player XZ movement exactly.
//
// SETUP UNITY:
//   GameObject furniture:
//   ├── FurnitureInteractable (this script)
//   ├── BoxCollider (Layer "Interactable" — front face)
//   └── Rigidbody  (added automatically)
//         Is Kinematic = true (managed by script)
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FurnitureInteractable : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Identity")]
    [SerializeField] private string _furntureName = "furniture";

    [Header("Physics")]
    [Tooltip("Simulated mass in kg — reduces player speed while pushing.")]
    [SerializeField] private float _massKg = 30f;

    // ================================================================
    // STATE
    // ================================================================

    private Rigidbody  _rb;
    private bool       _isPushing   = false;
    private GameObject _pusher;
    private Vector3    _lastPos;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void FixedUpdate()
    {
        if (!_isPushing || _rb == null || _pusher == null) return;

        Vector3 currentPos = _pusher.transform.position;
        Vector3 delta      = currentPos - _lastPos;
        delta.y            = 0f;
        _lastPos           = currentPos;

        if (delta.sqrMagnitude < 0.00001f) return;

        _rb.MovePosition(_rb.position + delta);
    }

    // ================================================================
    // IINTERACTABLE
    // ================================================================

    public bool CanInteract(GameObject interactor) => true;

    public void Interact(GameObject interactor) { }

    public string GetInteractionLabel()
    {
        if (_isPushing) return $"Push {_furntureName} (release E)";
        return $"Push {_furntureName}";
    }

    // ================================================================
    // PUSHING API — called by PlayerInteractor
    // ================================================================

    public void StartPushing(GameObject pusher)
    {
        _isPushing = true;
        _pusher    = pusher;
        _lastPos   = pusher.transform.position;
    }

    public void StopPushing()
    {
        _isPushing = false;
        _pusher    = null;
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    /// <summary>Slows player based on mass. Exposed to PlayerController.</summary>
    public float SpeedMultiplier
    {
        get
        {
            if (!_isPushing) return 1f;
            return Mathf.Max(0.25f, 1f / (1f + _massKg / 20f));
        }
    }

    public bool IsPushing => _isPushing;
}
