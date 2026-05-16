// ============================================================
// ValueObject.cs — Bailiff & Co  V5 (AVEC PROTECTION DROP)
// Objet saisissable avec valeur monétaire et état cassable.
// IInteractable : saisir, scanner, dégrader.
//
// CHANGEMENTS V5 :
//   - ✅ Protection contre les dégâts du drop (collider désactivé)
//   - ✅ Seuil de dégâts plus haut (3 m/s au lieu de 2)
//   - ✅ Sistema de dégâts cumulatifs (_damagePercentage)
//   - Valeur = lerp entre prix de base et 0€ selon % dégâts
// ============================================================
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ValueObject : MonoBehaviour, IInteractable
{
    [Header("Données")]
    [SerializeField] private ObjetData _data;
    [SerializeField] private bool _isScanned = false;

    private Rigidbody   _rb;
    private Collider    _collider;
    private PlayerCarry _carrier;
    private float       _impactVelocity;
    
    // ✅ NOUVEAU : Système de dégâts cumulatifs
    private float _damagePercentage = 0f;
    
    // ✅ NOUVEAU : ID unique pour tracker cette instance à travers la mission
    private int _instanceId = 0;
    
    // ✅ Protection contre les dégâts du drop (après pose douce)
    private bool _damageProtected = false;

    // ================================================================
    // INITIALIZATION
    // ================================================================

    public void Initialize(ObjetData data)
    {
        _data = data;
        _damagePercentage = 0f;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        if (_data == null)
            return;

        _damagePercentage = 0f;
        
        // ✅ NOUVEAU : Assigner un ID unique (utiliser GetInstanceID de Unity)
        _instanceId = GetInstanceID();
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
    // DAMAGE — Système cumulatif
    // ================================================================

    private void OnCollisionEnter(Collision col)
    {
        // ✅ Si protégé contre les dégâts (après pose douce), ignore
        if (_damageProtected)
            return;
        
        // Ne pas traiter les collisions quand l'objet est porté
        if (_carrier != null) 
            return;

        // ✅ Si pas cassable, ignore complètement
        if (_data == null || !_data.IsBreakable) 
            return;

        // ✅ Si déjà 100% cassé, stop
        if (_damagePercentage >= 100f) 
            return;

        _impactVelocity = col.relativeVelocity.magnitude;
        
        // ✅ Seuil augmenté à 3 m/s (au lieu de 2) pour éviter les faux positifs du drop
        if (_impactVelocity < _data.DamageImpactThreshold) 
            return;

        // ✅ Calculer les dégâts de cet impact
        float damageDelta = CalculateDamageFromImpact(_impactVelocity);
        float damagePercentBefore = _damagePercentage;
        _damagePercentage = Mathf.Min(_damagePercentage + damageDelta, 100f);

        float valueBefore = GetValueAtDamagePercent(damagePercentBefore);
        float valueAfter = ActualValue;
        float lostValue = valueBefore - valueAfter;

        // ✅ Émettre l'event
        EventBus<OnObjectDamaged>.Raise(new OnObjectDamaged
        {
            Object       = _data,
            ValueBefore  = valueBefore,
            ValueAfter   = valueAfter,
            ValueLost    = lostValue,
            DamagePercent = _damagePercentage,  // ✅ Nouveau
            IsBroken     = (_damagePercentage >= 100f),
            Position     = transform.position
        });

        // Émettre le bruit
        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range    = _data.DropNoiseRange,
            Level    = _data.DropNoiseLevel,
            Source   = gameObject
        });

        //Debug.Log($"[ValueObject] Impact sur {_data.ObjectName}: +{damageDelta:F1}% dégâts " +
        //          $"(total: {_damagePercentage:F1}%) | {valueBefore:F2}€ → {valueAfter:F2}€ | " +
        //          $"Velocity: {_impactVelocity:F2} m/s");
    }

    // ✅ Calcule les dégâts basés sur la vélocité d'impact
    private float CalculateDamageFromImpact(float velocity)
    {
        // Gérer les cas spéciaux (NaN, infini, négatif)
        if (float.IsNaN(velocity) || float.IsInfinity(velocity) || velocity < 0f)
            return 0f;
        
        return velocity switch
        {
            < 4f   => 5f,
            < 6f   => 15f,
            < 9f   => 30f,
            < 13f  => 50f,
            _ => 75f      
        };
    }

    // ✅ Calcule le prix basé sur le % de dégâts
    private float GetValueAtDamagePercent(float damagePercent)
    {
        if (_data == null) return 0f;
        
        // Interpolation linéaire : chaque % de dégâts = % de perte de valeur
        float healthPercent = Mathf.Max(0f, 100f - damagePercent);
        return _data.Value * (healthPercent / 100f);
    }

    // ================================================================
    // LOADING INTO VEHICLE
    // ================================================================

    public void LoadIntoVehicle()
    {
        EventBus<OnObjectLoaded>.Raise(new OnObjectLoaded
        {
            Object       = _data,
            BasePrice    = _data != null ? _data.Value : 0f,
            CurrentPrice = ActualValue,
            IsBreakable  = _data?.IsBreakable ?? false,
            IsBroken     = (_damagePercentage >= 100f),
            DamagePercent = _damagePercentage,    // ✅ NOUVEAU
            InstanceId    = _instanceId             // ✅ NOUVEAU
        });
        
        Destroy(gameObject);
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public ObjetData Data => _data;
    
    /// <summary>
    /// ✅ Valeur actuelle basée sur % de dégâts cumulatifs
    /// 0% dégâts = prix complet
    /// 100% dégâts = 0€
    /// </summary>
    public float ActualValue => GetValueAtDamagePercent(_damagePercentage);
    
    public bool IsScanned => _isScanned;
    public bool IsBroken => _damagePercentage >= 100f;
    public float DamagePercent => _damagePercentage;
    
    public void ReleaseCarrier() => _carrier = null;
    
    /// <summary>Active la protection contre les dégâts pour une durée donnée (pose douce)</summary>
    public void ActivateDamageProtection(float duration)
    {
        StartCoroutine(DamageProtectionCoroutine(duration));
    }
    
    private IEnumerator DamageProtectionCoroutine(float duration)
    {
        _damageProtected = true;
        //Debug.Log($"[ValueObject] Protection dégâts activée pour {_data.ObjectName} ({duration}s)");
        yield return new WaitForSeconds(duration);
        _damageProtected = false;
        //Debug.Log($"[ValueObject] Protection dégâts désactivée pour {_data.ObjectName}");
    }
}