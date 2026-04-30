// ============================================================
// FurnitureInteractable.cs — Bailiff & Co  V2
// Classe de base ABSTRAITE pour tous les meubles interactables.
// Gère le déplacement/poussée commun à tous les meubles.
//
// HIÉRARCHIE :
//   FurnitureInteractable (cette classe)
//   ├── OpenableInteractable (portes, fenêtres)
//   └── DrawerInteractable (tiroirs)
//
// CHANGEMENTS V2 :
//   - Extrait de MeubleInteractable.cs (V1)
//   - Classe abstraite avec template method pattern
//   - Valeurs numériques viennent de FurnitureConfig (SO)
//   - OnBruitEmis → OnNoiseEmitted
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class FurnitureInteractable : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Configuration (optionnelle — sinon valeurs par défaut)")]
    [SerializeField] protected FurnitureConfig _config;

    [Header("Masse & Résistance")]
    [Tooltip("Masse simulée du meuble en kg — réduit la vitesse du joueur")]
    [SerializeField] protected float _massKg = 30f;

    // ================================================================
    // COMPOSANTS
    // ================================================================

    protected Rigidbody          _rb;
    protected PlayerNoiseEmitter _noise;

    // ================================================================
    // ÉTAT POUSSÉE
    // ================================================================

    protected bool     _isBeingPushed = false;
    protected GameObject _pusher;
    protected Vector3  _previousPusherPosition;
    protected float    _lastNoiseTime = 0f;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation
                            | RigidbodyConstraints.FreezePositionY;
        }
    }

    protected virtual void Update()
    {
        if (!_isBeingPushed || _pusher == null) return;

        Vector3 currentPos = _pusher.transform.position;
        Vector3 delta      = currentPos - _previousPusherPosition;
        delta.y            = 0f;

        if (delta.sqrMagnitude > 0.00001f)
        {
            transform.position += delta;
            EmitSlidingNoise();
        }

        _previousPusherPosition = currentPos;
    }

    // ================================================================
    // IINTERACTABLE — implémentation par défaut
    // Les classes dérivées peuvent override si besoin
    // ================================================================

    public virtual bool CanInteract(GameObject interactor) => true;

    public virtual void Interact(GameObject interactor)
    {
        if (!_isBeingPushed)
            StartPushing(interactor);
    }

    public abstract string GetInteractionLabel();

    // ================================================================
    // PUSHING LOGIC
    // ================================================================

    public virtual void StartPushing(GameObject pusher)
    {
        _isBeingPushed          = true;
        _pusher                 = pusher;
        _noise                  = pusher.GetComponent<PlayerNoiseEmitter>();
        _previousPusherPosition = pusher.transform.position;
        
        if (_rb != null) 
            _rb.isKinematic = true;
    }

    public virtual void StopPushing()
    {
        _isBeingPushed = false;
        _pusher        = null;
    }

    // ================================================================
    // NOISE EMISSION
    // ================================================================

    protected virtual void EmitSlidingNoise()
    {
        if (_noise == null) return;

        float noiseInterval = _config != null 
            ? _config.SlidingNoiseInterval 
            : 0.3f;

        if (Time.time - _lastNoiseTime < noiseInterval) return;
        _lastNoiseTime = Time.time;

        string floorTag = DetectFloorTag();
        float range = GetNoiseRangeForFloor(floorTag);

        _noise.EmettreBruit(NiveauBruit.Fort, range);
    }

    protected virtual string DetectFloorTag()
    {
        if (Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down, 
            out RaycastHit hit, 
            0.5f,
            Physics.AllLayers, 
            QueryTriggerInteraction.Ignore))
        {
            return hit.collider.tag;
        }
        return "";
    }

    protected virtual float GetNoiseRangeForFloor(string floorTag)
    {
        if (_config == null)
        {
            // Valeurs par défaut si pas de config
            return floorTag switch
            {
                "Parquet"   => 10f,
                "Moquette"  => 4f,
                "Carrelage" => 12f,
                _           => 8f
            };
        }

        return floorTag switch
        {
            "Parquet"   => _config.NoiseRangeParquet,
            "Moquette"  => _config.NoiseRangeCarpet,
            "Carrelage" => _config.NoiseRangeTile,
            _           => _config.NoiseRangeDefault
        };
    }

    // ================================================================
    // SPEED MULTIPLIER — exposé au PlayerController
    // ================================================================

    public virtual float SpeedMultiplier
    {
        get
        {
            if (!_isBeingPushed) return 1f;

            float minMultiplier = _config != null 
                ? _config.MinSpeedMultiplier 
                : 0.25f;

            // Formule : 1 / (1 + masse/20)
            // 10kg → 0.67 | 30kg → 0.4 | 60kg → 0.25
            return Mathf.Max(minMultiplier, 1f / (1f + _massKg / 20f));
        }
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public bool IsBeingPushed => _isBeingPushed;
}