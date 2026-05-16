// ============================================================
// DrawerInteractable.cs — Bailiff & Co  V2
// Drawer that slides open/closed along its local axis.
// ValueObjects placed as children become seizable when drawer opens.
//
// RECOMMENDED HIERARCHY:
//   Dresser (root — FurnitureInteractable optional)
//   └── Drawer (this script + BoxCollider Layer "Interactable")
//       └── ValueObject_01   ← placed as child in scene
//       └── ValueObject_02
//
// WORKFLOW:
//   1. Place ValueObjects as children of Drawer in scene
//   2. Their Rigidbody.isKinematic must be TRUE initially
//   3. On open: free Rigidbody → objects become seizable
//   4. On close: re-kinematize remaining objects
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class DrawerInteractable : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Parameters")]
    [Tooltip("Opening distance in meters along local Z axis.")]
    [SerializeField] private float _openDistance = 0.4f;

    [Tooltip("Slide speed (m/s).")]
    [SerializeField] private float _slideSpeed = 3f;

    [Tooltip("Maximum object height (in meters) that can fit when drawer is closed.")]
    [SerializeField] private float _interiorClearance = 0.15f;

    // ================================================================
    // STATE
    // ================================================================

    private bool    _isOpen   = false;
    private bool    _isMoving = false;
    private Vector3 _closedPos;
    private Vector3 _openPos;
    private Vector3 _targetPos;

    private readonly List<ValueObject> _contents = new();

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _closedPos = transform.localPosition;
        _openPos   = _closedPos + transform.localRotation * Vector3.forward * _openDistance;
        _targetPos = _closedPos;

        foreach (var obj in GetComponentsInChildren<ValueObject>())
        {
            _contents.Add(obj);
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        }
    }

    private void Update()
    {
        if (!_isMoving) return;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition, _targetPos, _slideSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, _targetPos) < 0.001f)
        {
            transform.localPosition = _targetPos;
            _isMoving = false;

            if (_isOpen) LiberateContents();
            else         LockContents();
        }
    }

    // ================================================================
    // IINTERACTABLE
    // ================================================================

    public bool CanInteract(GameObject interactor) => !_isMoving;

    public void Interact(GameObject interactor)
    {
        // Check if close is blocked by oversized contents
        if (_isOpen && !CanClose()) return;

        _isOpen    = !_isOpen;
        _targetPos = _isOpen ? _openPos : _closedPos;
        _isMoving  = true;

        // Re-kinematize immediately on close so objects follow
        if (!_isOpen) LockContents();

        // Open/close noise (random squeaking)
        if (Random.value < 0.35f)
        {
            EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
            {
                Position = transform.position,
                Range    = 7f,
                Level    = NoiseLevel.Light,
                Source   = gameObject
            });
        }
    }

    public string GetInteractionLabel()
    {
        if (_isMoving) return "...";

        int count = GetPresentContents().Count;

        if (_isOpen && count > 0)
            return $"Close drawer ({count} item{(count > 1 ? "s" : "")})";

        return _isOpen ? "Close drawer" : "Open drawer";
    }

    // ================================================================
    // CONTENTS MANAGEMENT
    // ================================================================

    private bool CanClose()
    {
        foreach (var obj in GetPresentContents())
        {
            var col = obj.GetComponentInChildren<Collider>();
            if (col == null) continue;

            if (col.bounds.size.y > _interiorClearance)
                return false;
        }
        return true;
    }

    private void LiberateContents()
    {
        foreach (var obj in GetPresentContents())
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
        }
    }

    private void LockContents()
    {
        foreach (var obj in GetPresentContents())
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
                rb.linearVelocity    = Vector3.zero;
            }
        }
    }

    private List<ValueObject> GetPresentContents()
    {
        var list = new List<ValueObject>();
        foreach (var obj in _contents)
            if (obj != null && obj.transform.IsChildOf(transform))
                list.Add(obj);
        return list;
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public bool IsOpen      => _isOpen;
    public int  ObjectCount => GetPresentContents().Count;
}
