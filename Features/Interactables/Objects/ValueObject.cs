// ============================================================
// ValueObject.cs — Bailiff & Co  V2
// Objet saisissable avec valeur monétaire.
// IInteractable : saisir, scanner, dégrader.
//
// CHANGEMENTS V2 :
//   - ObjetValeur → ValueObject (anglais cohérent)
//   - ObjetDef → ObjetData (nouveau nom SO)
//   - OnBruitEmis → OnNoiseEmitted
//   - OnObjetCharge → OnObjectLoaded
//   - OnObjetEndommage → OnObjectDamaged
//   - PlayerCarry injecté via interaction, plus de cache
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ValueObject : MonoBehaviour, IInteractable
{
    [Header("Données")]
    [SerializeField] private ObjetData _data;
    [SerializeField] private float     _actualValue;
    [SerializeField] private bool      _isScanned = false;

    private Rigidbody   _rb;
    private PlayerCarry _carrier;
    private float       _impactVelocity;

    // ================================================================
    // INITIALIZATION
    // ================================================================

    public void Initialize(ObjetData data, float value)
    {
        _data        = data;
        _actualValue = value;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Si pas initialisé par MissionBuilder, tire une valeur aléatoire
        if (_actualValue == 0f && _data != null)
            _actualValue = Random.Range(_data.ValueMin, _data.ValueMax);
    }

    // ================================================================
    // IINTERACTABLE
    // ================================================================

    public bool CanInteract(GameObject interactor) => _carrier == null;

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<PlayerCarry>(out var carry))
        {
            carry.Saisir(this);
            _carrier = carry;
        }
    }

    public string GetInteractionLabel()
    {
        if (!_isScanned)
            return $"Saisir ({(_data != null ? _data.ObjectName : "Objet")})";
        
        return $"Saisir — {_actualValue:N0} €";
    }

    // ================================================================
    // SCAN
    // ================================================================

    public void Scan()
    {
        _isScanned = true;
    }

    // ================================================================
    // DAMAGE
    // ================================================================

    private void OnCollisionEnter(Collision col)
    {
        // Ne pas traiter les collisions quand l'objet est porté
        if (_carrier != null) return;

        _impactVelocity = col.relativeVelocity.magnitude;
        
        if (_data == null || !_data.IsFragile) return;
        if (_impactVelocity < _data.DamageImpactThreshold) return;

        // Calcul des dégâts selon le seuil
        float lostValue;
        if (_impactVelocity >= _data.MajorDamageThreshold)
        {
            // Dégâts majeurs
            lostValue = _actualValue * (1f - _data.MajorDamageMultiplier);
        }
        else
        {
            // Dégâts mineurs
            lostValue = _actualValue * (1f - _data.MinorDamageMultiplier);
        }

        _actualValue -= lostValue;
        _actualValue  = Mathf.Max(0f, _actualValue);

        EventBus<OnObjectDamaged>.Raise(new OnObjectDamaged
        {
            Object      = _data,
            ValueLost   = lostValue,
            Position    = transform.position
        });

        // Bruit fort si impact violent
        if (_impactVelocity >= _data.MajorDamageThreshold)
        {
            EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
            {
                Position = transform.position,
                Range    = _data.DropNoiseRange,
                Level    = _data.DropNoiseLevel,
                Source   = gameObject
            });
        }
    }

    // ================================================================
    // LOADING INTO VEHICLE
    // ================================================================

    public void LoadIntoVehicle()
    {
        EventBus<OnObjectLoaded>.Raise(new OnObjectLoaded
        {
            Object     = _data,
            Value      = _actualValue,
            IsFragile  = _data?.IsFragile ?? false
        });
        
        Destroy(gameObject);
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public ObjetData Data        => _data;
    public float     ActualValue => _actualValue;
    public bool      IsScanned   => _isScanned;
    
    public void ReleaseCarrier() => _carrier = null;
}