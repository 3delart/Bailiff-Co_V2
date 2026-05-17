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
    private Renderer    _renderer;
    private SkinnedMeshRenderer _skinnedMesh;
    
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
        _renderer = GetComponent<Renderer>();
        _skinnedMesh = GetComponent<SkinnedMeshRenderer>();

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

        // Trigger topple si applicable
        if (_data.CanTopple && _impactVelocity >= _data.ToppleVelocityThreshold && _rb != null && _rb.isKinematic)
        {
            _rb.isKinematic = false;
        }

        // Mettre à jour visuals (déformation, texture)
        OnDamageVisualUpdate();

        // Vérifier si cassé + déclencher break visuals
        if (_damagePercentage >= 100f)
        {
            OnBreak();
        }

        //Debug.Log($"[ValueObject] Impact sur {_data.ObjectName}: +{damageDelta:F1}% dégâts " +
        //          $"(total: {_damagePercentage:F1}%) | {valueBefore:F2}€ → {valueAfter:F2}€ | " +
        //          $"Velocity: {_impactVelocity:F2} m/s");
    }

    // ✅ Calcule les dégâts basés sur la vélocité d'impact
    // Applique DamageMultiplier et DurabilityVariance
    private float CalculateDamageFromImpact(float velocity)
    {
        // Gérer les cas spéciaux (NaN, infini, négatif)
        if (float.IsNaN(velocity) || float.IsInfinity(velocity) || velocity < 0f)
            return 0f;

        // Step table de base
        float baseDamage = velocity switch
        {
            < 4f   => 5f,
            < 6f   => 15f,
            < 9f   => 30f,
            < 13f  => 50f,
            _ => 75f
        };

        // Appliquer le multiplicateur de matériau
        float damageAfterMultiplier = baseDamage * _data.DamageMultiplier;

        // Appliquer la variance aléatoire si configurée
        if (_data.DurabilityVariance > 0f)
        {
            float varianceFactor = Random.Range(
                1f - _data.DurabilityVariance * 0.5f,
                1f + _data.DurabilityVariance * 0.5f
            );
            damageAfterMultiplier *= varianceFactor;
        }

        return damageAfterMultiplier;
    }

    // ✅ Calcule le prix basé sur le % de dégâts
    private float GetValueAtDamagePercent(float damagePercent)
    {
        if (_data == null) return 0f;

        // Interpolation linéaire : chaque % de dégâts = % de perte de valeur
        float healthPercent = Mathf.Max(0f, 100f - damagePercent);
        return _data.Value * (healthPercent / 100f);
    }

    // ✅ Applique les dégâts d'un impact direct (appelé par PlayerCarry lors wall check)
    public void ApplyImpactDamage(float velocity)
    {
        if (_damageProtected || _data == null || !_data.IsBreakable || _damagePercentage >= 100f)
            return;

        _impactVelocity = velocity;

        if (_impactVelocity < _data.DamageImpactThreshold)
            return;

        // Même logique que OnCollisionEnter
        float damageDelta = CalculateDamageFromImpact(_impactVelocity);
        float damagePercentBefore = _damagePercentage;
        _damagePercentage = Mathf.Min(_damagePercentage + damageDelta, 100f);

        float valueBefore = GetValueAtDamagePercent(damagePercentBefore);
        float valueAfter = ActualValue;
        float lostValue = valueBefore - valueAfter;

        EventBus<OnObjectDamaged>.Raise(new OnObjectDamaged
        {
            Object       = _data,
            ValueBefore  = valueBefore,
            ValueAfter   = valueAfter,
            ValueLost    = lostValue,
            DamagePercent = _damagePercentage,
            IsBroken     = (_damagePercentage >= 100f),
            Position     = transform.position
        });

        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range    = _data.DropNoiseRange,
            Level    = _data.DropNoiseLevel,
            Source   = gameObject
        });

        OnDamageVisualUpdate();

        if (_damagePercentage >= 100f)
        {
            OnBreak();
        }
    }

    // ✅ Déclenche les effets visuels de casse
    private void OnBreak()
    {
        if (_data == null || !_data.IsDestroyable) return;

        if (_data.BreakType == BreakType.Shatters && _data.BrokenVariant != null)
        {
            var shattered = Instantiate(_data.BrokenVariant, transform.position, transform.rotation);
            var rbs  = shattered.GetComponentsInChildren<Rigidbody>();
            var cols = shattered.GetComponentsInChildren<Collider>();

            // If not pickupable, disable colliders on fragments so they can't be carried
            if (!_data.IsPickupableAfterBreak)
            {
                foreach (var col in cols)
                    col.enabled = false;
            }

            // Prevent depenetration explosion: fragments that overlap at spawn
            // should not push each other apart via physics solver
            for (int i = 0; i < cols.Length; i++)
                for (int j = i + 1; j < cols.Length; j++)
                    Physics.IgnoreCollision(cols[i], cols[j]);

            foreach (var rb in rbs)
            {
                rb.linearDamping = _data.FragmentDrag;

                Vector3 dir = (rb.transform.position - transform.position).normalized
                              + Random.insideUnitSphere * 0.5f;
                dir.Normalize();
                float speed = Random.Range(_data.ShatterForceMin, _data.ShatterForceMax);
                rb.AddForce(dir * speed, ForceMode.VelocityChange);
            }

            Destroy(shattered, 180f);
            Destroy(gameObject);
        }
    }

    // ✅ Met à jour les visuals en fonction du pourcentage de dégâts
    private void OnDamageVisualUpdate()
    {
        if (_data == null || _data.BreakType != BreakType.Deforms) return;

        float t = _damagePercentage / 100f;

        // Appliquer la déformation via blend shape
        if (_data.DamagedBlendShapeWeight > 0 && _skinnedMesh != null)
        {
            float targetWeight = t * _data.DamagedBlendShapeWeight;
            _skinnedMesh.SetBlendShapeWeight(0, targetWeight);
        }

        // Swapper le matériau au seuil 50% dégâts
        if (_damagePercentage >= 50f && _data.DamagedMaterial != null && _renderer != null)
        {
            _renderer.material = _data.DamagedMaterial;
        }
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