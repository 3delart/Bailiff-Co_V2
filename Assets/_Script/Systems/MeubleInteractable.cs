// ============================================================
// MeubleInteractable.cs — Bailiff & Co  V2
// Meuble déplaçable dans la pièce (commode, frigo, armoire…).
// E maintenu → le joueur pousse ou tire le meuble.
// Ground-snap : le meuble suit le sol (rampes, petites marches).
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

    [Tooltip("Layers considérés comme sol pour le ground-snap (cocher Default, Ground…).")]
    [SerializeField] private LayerMask _groundMask = ~0;

    // ================================================================
    // ÉTAT
    // ================================================================

    private Rigidbody  _rb;
    private bool       _estPoussé  = false;
    private GameObject _pousseur;
    private Vector3    _dernierePos;
    private Vector3    _pendingDelta;

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

    private void Update()
    {
        if (!_estPoussé || _pousseur == null) return;

        Vector3 currentPos = _pousseur.transform.position;
        Vector3 delta      = currentPos - _dernierePos;
        delta.y            = 0f;
        _pendingDelta      = delta;
        _dernierePos       = currentPos;
    }

    private void FixedUpdate()
    {
        if (!_estPoussé || _rb == null) return;
        if (_pendingDelta.sqrMagnitude < 0.00001f) return;

        Vector3 newPos = _rb.position + _pendingDelta;
        _pendingDelta  = Vector3.zero;

        // Ground-snap : suit le sol pour gérer marches et dénivelés
        if (Physics.Raycast(_rb.position + Vector3.up * 0.5f, Vector3.down,
                            out RaycastHit hit, 2f, _groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            newPos.y = Mathf.Lerp(_rb.position.y, hit.point.y, Time.fixedDeltaTime * 15f);
        }

        _rb.MovePosition(newPos);
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
        _pendingDelta = Vector3.zero;
    }

    public void StopPushing()
    {
        _estPoussé    = false;
        _pousseur     = null;
        _pendingDelta = Vector3.zero;
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
