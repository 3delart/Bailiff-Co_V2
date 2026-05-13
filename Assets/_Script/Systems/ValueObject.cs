// ============================================================
// ValueObject.cs — Bailiff & Co  V2
// Objet saisissable avec valeur monétaire et état cassable.
// IInteractable : saisir, scanner, dégrader.
//
// CHANGEMENTS V2 :
//   - ObjetValeur → ValueObject (anglais cohérent)
//   - ObjetDef → ObjetData (nouveau nom SO)
//   - OnBruitEmis → OnNoiseEmitted
//   - OnObjetCharge → OnObjectLoaded
//   - OnObjetEndommage → OnObjectDamaged
//   - PlayerCarry injecté via interaction, plus de cache
//   - ✅ IsBreakable = propriété de l'asset (peut casser ?)
//   - ✅ IsBroken = état runtime (est cassé ?)
//     → Cache les impacts répétés une fois cassé
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ValueObject : MonoBehaviour, IInteractable
{
    [Header("Données")]
    [SerializeField] private ObjetData _data;
    [SerializeField] private float     _actualValue;
    [SerializeField] private bool      _isScanned = false;
    [SerializeField] private bool      _isBroken = false;  // ✅ Runtime state

    private Rigidbody   _rb;
    private PlayerCarry _carrier;
    private float       _impactVelocity;

    // ================================================================
    // INITIALIZATION
    // ================================================================

    /// <summary>
    /// Initialise l'objet avec ses données et sa valeur.
    /// Appelé par MissionBuilder lors du spawn procédural.
    /// </summary>
    public void Initialize(ObjetData data, float value)
    {
        _data        = data;
        _actualValue = value;
        _isBroken    = false;  // ✅ Toujours false au spawn
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Si pas initialisé par MissionBuilder, tire une valeur aléatoire
        if (_actualValue == 0f && _data != null)
            _actualValue = Random.Range(_data.ValueMin, _data.ValueMax);

        // ✅ Toujours false au démarrage
        _isBroken = false;
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
        if (_carrier != null) 
            return;

        // ✅ Si pas cassable, ignore complètement
        if (_data == null || !_data.IsBreakable) 
            return;

        // ✅ Si déjà cassé, ne peut pas casser 2 fois
        if (_isBroken) 
            return;

        _impactVelocity = col.relativeVelocity.magnitude;
        
        // Impact insuffisant
        if (_impactVelocity < _data.DamageImpactThreshold) 
            return;

        // ✅ MÉMORISER la valeur AVANT les dégâts
        float valueBefore = _actualValue;

        // Calcul des dégâts selon le seuil d'impact
        float lostValue;
        if (_impactVelocity >= _data.MajorDamageThreshold)
        {
            // Dégâts majeurs : perte massive (ex: 80% si MajorDamageMultiplier=0.2)
            lostValue = _actualValue * (1f - _data.MajorDamageMultiplier);
        }
        else
        {
            // Dégâts mineurs : perte modérée (ex: 50% si MinorDamageMultiplier=0.5)
            lostValue = _actualValue * (1f - _data.MinorDamageMultiplier);
        }

        // Applique la perte de valeur
        _actualValue -= lostValue;
        _actualValue = Mathf.Max(0f, _actualValue);

        // ✅ Marquer comme cassé (empêche les dégâts répétés)
        _isBroken = true;

        // ✅ Émettre l'event avec valueBefore pour le UI
        EventBus<OnObjectDamaged>.Raise(new OnObjectDamaged
        {
            Object       = _data,
            ValueBefore  = valueBefore,  // ✅ Valeur avant dégâts (pour UI)
            ValueLost    = lostValue,
            Position     = transform.position,
            IsBroken     = _isBroken      // ✅ Marque comme cassé
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

        Debug.Log($"[ValueObject] {_data.ObjectName} CASSÉ ! " +
                  $"Avant: {valueBefore:N0}€ | Perdu: {lostValue:N0}€ | Après: {_actualValue:N0}€");
    }

    // ================================================================
    // LOADING INTO VEHICLE
    // ================================================================

    public void LoadIntoVehicle()
    {
        EventBus<OnObjectLoaded>.Raise(new OnObjectLoaded
        {
            Object      = _data,
            Value       = _actualValue,
            IsBreakable = _data?.IsBreakable ?? false,
            IsBroken    = _isBroken  // ✅ Indique si l'objet est cassé
        });
        
        Destroy(gameObject);
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public ObjetData Data        => _data;
    public float     ActualValue => _actualValue;
    public bool      IsScanned   => _isScanned;
    public bool      IsBroken    => _isBroken;  // ✅ État runtime (est cassé ?)
    
    public void ReleaseCarrier() => _carrier = null;
}