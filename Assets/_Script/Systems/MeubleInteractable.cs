// ============================================================
// MeubleInteractable.cs — Bailiff & Co  V2
// Meuble déplaçable dans la pièce (commode, frigo, armoire…).
// E maintenu → le joueur pousse ou tire le meuble.
// Le meuble suit exactement le déplacement XZ du joueur.
//
// SETUP UNITY :
//   GameObject meuble :
//   ├── MeubleInteractable (ce script)
//   ├── BoxCollider (Layer "Interactable" — face avant)
//   └── Rigidbody  (ajouté automatiquement)
//         Is Kinematic = true (géré par le script)
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeubleInteractable : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Identité")]
    [SerializeField] private string _nomMeuble = "meuble";

    [Header("Physique")]
    [Tooltip("Masse simulée en kg — réduit la vitesse du joueur pendant la poussée.")]
    [SerializeField] private float _massKg = 30f;

    // ================================================================
    // ÉTAT
    // ================================================================

    private Rigidbody  _rb;
    private bool       _estPoussé  = false;
    private GameObject _pousseur;
    private Vector3    _dernierePos;

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
        if (!_estPoussé || _rb == null || _pousseur == null) return;

        Vector3 currentPos = _pousseur.transform.position;
        Vector3 delta      = currentPos - _dernierePos;
        delta.y            = 0f;
        _dernierePos       = currentPos;

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
        if (_estPoussé) return $"Déplacer la {_nomMeuble} (relâcher E)";
        return $"Déplacer la {_nomMeuble}";
    }

    // ================================================================
    // API POUSSÉE — appelée par PlayerInteractor
    // ================================================================

    public void StartPushing(GameObject pousseur)
    {
        _estPoussé   = true;
        _pousseur    = pousseur;
        _dernierePos = pousseur.transform.position;
    }

    public void StopPushing()
    {
        _estPoussé = false;
        _pousseur  = null;
    }

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    /// <summary>Ralentit le joueur selon la masse. Exposé à PlayerController.</summary>
    public float SpeedMultiplier
    {
        get
        {
            if (!_estPoussé) return 1f;
            return Mathf.Max(0.25f, 1f / (1f + _massKg / 20f));
        }
    }

    public bool EstPoussé => _estPoussé;
}
