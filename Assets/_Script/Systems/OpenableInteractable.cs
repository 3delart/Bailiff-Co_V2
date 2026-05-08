// ============================================================
// OpenableInteractable.cs — Bailiff & Co  V2
// Remplace Porte.cs et OuvrableInteractable.cs (V1).
// Gère tout objet ouvrable/fermable :
//   • Porte              → Mode Rotation (axe Y, 90°)
//   • Fenêtre guillotine → Mode TranslationVerticale
//   • Fenêtre coulissante → Mode TranslationHorizontale
//
// CHANGEMENTS V2 :
//   - Hérite de FurnitureInteractable
//   - OuvrableInteractable → OpenableInteractable
//   - Valeurs de bruit viennent de FurnitureConfig (SO optionnel)
//   - OnBruitEmis → OnNoiseEmitted
//   - Nettoyage du code redondant
//
// CONFIGURATION INSPECTOR :
//   _openMode         → choisir le mode
//   _objectName       → "porte", "fenêtre", "volet"… (pour les labels)
//   _openAngle        → (Rotation uniquement) angle en degrés
//   _rotationAxis     → (Rotation uniquement) Vector3.up par défaut
//   _moveDistance     → (Translation uniquement) distance en mètres
//   _moveAxis         → (Translation uniquement) Vector3.up = vertical, right = horizontal
// ============================================================
using System.Collections;
using UnityEngine;

public class OpenableInteractable : FurnitureInteractable
{
    // ================================================================
    // ENUMS
    // ================================================================

    public enum OpenMode
    {
        Rotation,               // porte classique sur charnière
        TranslationVertical,    // fenêtre guillotine (monte/descend)
        TranslationHorizontal   // fenêtre coulissante (glisse sur le côté)
    }

    public enum OpenableState 
    { 
        Closed, 
        Open, 
        Locked, 
        Blocked 
    }

    // ================================================================
    // SERIALIZATION
    // ================================================================

    [Header("Identité")]
    [Tooltip("Utilisé dans les labels : 'Ouvrir la [fenêtre]', 'Forcer la [porte]'…")]
    [SerializeField] private string _objectName = "porte";

    [Header("Mode & État")]
    [SerializeField] private OpenMode        _openMode = OpenMode.Rotation;
    [SerializeField] private OpenableState   _state    = OpenableState.Closed;
    [SerializeField] private bool            _squeaks  = false;

    [Header("Rotation (porte classique)")]
    [Tooltip("Axe de rotation en espace local. Vector3.up = axe Y (porte standard).")]
    [SerializeField] private Vector3 _rotationAxis    = Vector3.up;
    [SerializeField] private float   _openAngle       = 90f;
    [SerializeField] private float   _animationDuration = 0.4f;

    [Header("Translation (fenêtre)")]
    [Tooltip("Axe de déplacement en espace LOCAL.\nVector3.up = monte (guillotine).\nVector3.right = glisse (coulissant).")]
    [SerializeField] private Vector3 _moveAxis     = Vector3.up;
    [SerializeField] private float   _moveDistance = 0.6f;
    [SerializeField] private float   _moveSpeed    = 3f;

    // ================================================================
    // PRIVATE STATE
    // ================================================================

    private bool       _isMoving = false;
    private Quaternion _closedRotation;
    private Vector3    _closedPosition;
    private Vector3    _openPosition;
    private Vector3    _targetPosition;
    private bool       _translationActive = false;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();

        _closedRotation = transform.localRotation;
        _closedPosition = transform.localPosition;
        _openPosition   = _closedPosition 
                        + transform.localRotation * _moveAxis.normalized * _moveDistance;
        _targetPosition = _closedPosition;
    }

    protected override void Update()
    {
        base.Update();

        if (!_translationActive) return;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition, 
            _targetPosition, 
            _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, _targetPosition) < 0.001f)
        {
            transform.localPosition = _targetPosition;
            _translationActive = false;
            _isMoving          = false;
        }
    }

    // ================================================================
    // IINTERACTABLE OVERRIDE
    // ================================================================

    public override bool CanInteract(GameObject interactor) =>
        _state != OpenableState.Blocked && !_isMoving;

    public override void Interact(GameObject interactor)
    {
        switch (_state)
        {
            case OpenableState.Closed:
                Open();
                break;
            
            case OpenableState.Open:
                Close();
                break;
            
            case OpenableState.Locked:
                AttemptForce(interactor);
                break;
        }
    }

    public override string GetInteractionLabel()
    {
        string name = char.ToUpper(_objectName[0]) + _objectName.Substring(1);
        
        return _state switch
        {
            OpenableState.Closed  => $"Ouvrir la {_objectName}",
            OpenableState.Open    => $"Fermer la {_objectName}",
            OpenableState.Locked  => $"Forcer la {_objectName} (pied-de-biche) / Crocheter",
            OpenableState.Blocked => $"{name} bloquée",
            _                     => name
        };
    }

    // ================================================================
    // ACTIONS
    // ================================================================

    private void Open()
    {
        _state = OpenableState.Open;
        StartAnimation(opening: true);

        if (_squeaks && Random.value < 0.4f)
            EmitNoise(GetNoiseRange(squeaking: true), NiveauBruit.Leger);
    }

    private void Close()
    {
        _state = OpenableState.Closed;
        StartAnimation(opening: false);
    }

    private void AttemptForce(GameObject interactor)
    {
        // Vérifier si le joueur possède un pied-de-biche
        var inv = interactor.GetComponent<InventaireSystem>();
        if (inv != null && inv.PossedePiedDeBiche())
            ForceOpen();
    }

    public void ForceOpen()
    {
        _state = OpenableState.Open;
        StartAnimation(opening: true);
        EmitNoise(GetNoiseRange(forced: true), NiveauBruit.Tresfort);
    }

    // ================================================================
    // ANIMATION DISPATCH
    // ================================================================

    private void StartAnimation(bool opening)
    {
        if (_isMoving) return;
        _isMoving = true;

        switch (_openMode)
        {
            case OpenMode.Rotation:
                StartCoroutine(AnimateRotation(opening));
                break;

            case OpenMode.TranslationVertical:
            case OpenMode.TranslationHorizontal:
                _targetPosition    = opening ? _openPosition : _closedPosition;
                _translationActive = true;
                // _isMoving sera remis à false dans Update()
                break;
        }
    }

    // ================================================================
    // ROTATION COROUTINE
    // ================================================================

    private IEnumerator AnimateRotation(bool opening)
    {
        Quaternion start = transform.localRotation;
        Quaternion end   = opening
            ? _closedRotation * Quaternion.AngleAxis(_openAngle, _rotationAxis)
            : _closedRotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _animationDuration;
            transform.localRotation = Quaternion.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }
        
        transform.localRotation = end;
        _isMoving = false;
    }

    // ================================================================
    // NOISE
    // ================================================================

    private void EmitNoise(float range, NiveauBruit level)
    {
        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range    = range,
            Level    = level,
            Source   = gameObject
        });
    }

    private float GetNoiseRange(bool squeaking = false, bool forced = false)
    {
        if (_config == null)
        {
            // Valeurs par défaut si pas de config
            if (forced)     return 20f;
            if (squeaking)  return 7f;
            return 6f;
        }

        if (forced)     return _config.ForcedOpenNoiseRange;
        if (squeaking)  return _config.SqueakNoiseRange;
        return _config.NormalOpenNoiseRange;
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    public void Lock()   => _state = OpenableState.Locked;
    public void Unlock() => _state = OpenableState.Closed;
    public void Block()  => _state = OpenableState.Blocked;

    public bool IsOpen   => _state == OpenableState.Open;
    public bool IsLocked => _state == OpenableState.Locked;
    public OpenableState State => _state;
}