// ============================================================
// ValueObject.cs — Bailiff & Co  V4 (ULTRA-SIMPLIFIÉ)
// Objet saisissable avec valeur monétaire et état cassable.
// IInteractable : saisir, scanner, dégrader.
//
// CHANGEMENTS V4 (FINAL) :
//   - ✅ Plus de _actualValue du tout !
//   - ✅ ActualValue = _data.Value - _damageLoss (calculé)
//   - Stocke seulement la perte de valeur en cas de dégâts
//   - Une seule source de vérité : ObjetData.Value
//   - Formatage automatique avec PriceFormatter
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
    [SerializeField] private bool _isScanned = false;
    [SerializeField] private bool _isBroken = false;

    private Rigidbody   _rb;
    private PlayerCarry _carrier;
    private float       _impactVelocity;

    // ================================================================
    // INITIALIZATION
    // ================================================================

    /// <summary>
    /// Initialise l'objet avec ses données.
    /// Appelé par MissionBuilder lors du spawn procédural.
    /// </summary>
    public void Initialize(ObjetData data)
    {
        _data     = data;
        _isBroken = false;    // ✅ Toujours false au spawn
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Si pas initialisé par MissionBuilder, initialise depuis ObjetData
        if (_data == null)
            return;

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
        
        // ✅ Formatage avec PriceFormatter
        return $"Saisir — {PriceFormatter.Format(ActualValue)}";
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
        float valueBefore = ActualValue;

        // ✅ CASSER L'OBJET (prix divisé par 2)
        _isBroken = true;
        float valueAfter = ActualValue;
        float lostValue = valueBefore - valueAfter;

        // ✅ Émettre l'event
        EventBus<OnObjectDamaged>.Raise(new OnObjectDamaged
        {
            Object       = _data,
            ValueBefore  = valueBefore,      // ✅ Valeur avant dégâts
            ValueLost    = lostValue,
            Position     = transform.position,
            IsBroken     = _isBroken         // ✅ Marque comme cassé
        });

        // Émettre le bruit
        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range    = _data.DropNoiseRange,
            Level    = _data.DropNoiseLevel,
            Source   = gameObject
        });

        Debug.Log($"[ValueObject] {_data.ObjectName} CASSÉ ! " +
                  $"Avant: {PriceFormatter.Format(valueBefore)} | " +
                  $"Après: {PriceFormatter.Format(valueAfter)}");
    }

    // ================================================================
    // LOADING INTO VEHICLE
    // ================================================================

    public void LoadIntoVehicle()
    {
        EventBus<OnObjectLoaded>.Raise(new OnObjectLoaded
        {
            Object       = _data,
            BasePrice    = _data != null ? _data.Value : 0f,      // ✅ Prix de base (original)
            CurrentPrice = ActualValue,                            // ✅ Prix actuel (peut être réduit)
            IsBreakable  = _data?.IsBreakable ?? false,
            IsBroken     = _isBroken                               // ✅ État: est-il cassé ?
        });
        
        Destroy(gameObject);
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public ObjetData Data => _data;
    
    /// <summary>
    /// ✅ Valeur actuelle = Valeur de base si intact, sinon divisée par 2
    /// Si cassé : prix / 2
    /// Si pas cassé : prix normal
    /// </summary>
    public float ActualValue => _isBroken 
        ? (_data != null ? _data.Value / 2f : 0f) 
        : (_data != null ? _data.Value : 0f);
    
    public bool IsScanned => _isScanned;
    public bool IsBroken => _isBroken;  // ✅ État runtime (est cassé ?)
    
    public void ReleaseCarrier() => _carrier = null;
}