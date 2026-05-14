// ============================================================
// TiroirInteractable.cs — Bailiff & Co  V2
// Tiroir qui s'ouvre/ferme en translation sur son axe local.
// Les ValueObject placés comme enfants deviennent saisissables
// quand le tiroir est ouvert.
//
// HIÉRARCHIE RECOMMANDÉE :
//   Commode (root — MeubleInteractable optionnel)
//   └── Tiroir (ce script + BoxCollider Layer "Interactable")
//       └── ValueObject_01   ← placé comme enfant dans la scène
//       └── ValueObject_02
//
// WORKFLOW :
//   1. Placer les ValueObject comme enfants du Tiroir dans la scène
//   2. Leurs Rigidbody.isKinematic doit être TRUE au départ
//   3. À l'ouverture : libère les Rigidbody → objets saisissables
//   4. À la fermeture : re-kinematise les objets encore présents
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class TiroirInteractable : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Paramètres")]
    [Tooltip("Distance d'ouverture en mètres le long de l'axe local Z.")]
    [SerializeField] private float _openDistance = 0.4f;

    [Tooltip("Vitesse de glissement (m/s).")]
    [SerializeField] private float _slideSpeed = 3f;

    // ================================================================
    // ÉTAT
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
        _isOpen    = !_isOpen;
        _targetPos = _isOpen ? _openPos : _closedPos;
        _isMoving  = true;

        // Re-kinematise immédiatement à la fermeture pour que les objets suivent
        if (!_isOpen) LockContents();

        // Bruit d'ouverture/fermeture (grincement aléatoire)
        if (Random.value < 0.35f)
        {
            EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
            {
                Position = transform.position,
                Range    = 7f,
                Level    = NiveauBruit.Leger,
                Source   = gameObject
            });
        }
    }

    public string GetInteractionLabel()
    {
        if (_isMoving) return "...";

        int count = GetPresentContents().Count;

        if (_isOpen && count > 0)
            return $"Fermer le tiroir ({count} objet{(count > 1 ? "s" : "")})";

        return _isOpen ? "Fermer le tiroir" : "Ouvrir le tiroir";
    }

    // ================================================================
    // GESTION DU CONTENU
    // ================================================================

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
    // PROPRIÉTÉS
    // ================================================================

    public bool IsOpen      => _isOpen;
    public int  ObjectCount => GetPresentContents().Count;
}
