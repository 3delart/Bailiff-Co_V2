// ============================================================
// DrawerInteractable.cs — Bailiff & Co  V2
// Tiroir qui s'ouvre/ferme en translation sur son axe local.
// Contient des ValueObject placés comme enfants en scène.
//
// CHANGEMENTS V2 :
//   - TiroirInteractable → DrawerInteractable
//   - FUSION de ZoneTiroirTrigger.cs (micro-script supprimé)
//   - Gestion du collider zone directement via [SerializeField]
//   - ObjetValeur → ValueObject
//   - OnBruitEmis → OnNoiseEmitted
//   - Valeurs numériques viennent de FurnitureConfig (optionnel)
//
// HIÉRARCHIE DANS LA SCÈNE :
//   Commode (MeshRenderer, pas de script)
//   └── Drawer (ce script + BoxCollider sur Layer Interactable)
//       └── DrawerZone (BoxCollider IsTrigger pour détecter objets)
//       └── ValueObject_01  ← placé comme enfant
//       └── ValueObject_02  ← placé comme enfant
//
// WORKFLOW :
//   1. Placer les ValueObject comme enfants du Drawer dans la scène
//   2. Leurs Rigidbody.isKinematic doit être TRUE au départ
//   3. À l'ouverture : libère les Rigidbody → objets saisissables
//   4. À la fermeture : re-kinematise les objets encore présents
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class DrawerInteractable : FurnitureInteractable
{
    // ================================================================
    // SERIALIZATION
    // ================================================================

    [Header("Paramètres tiroir")]
    [SerializeField] private float _openDistance   = 0.4f;  // mètres
    [SerializeField] private float _slideSpeed     = 3f;
    [SerializeField] private float _squeakChance   = 0.35f; // 35%

    [Header("Zone de détection des objets")]
    [Tooltip("BoxCollider IsTrigger sur l'enfant DrawerZone. Actif uniquement quand le tiroir est ouvert.")]
    [SerializeField] private Collider _zoneCollider;

    // ================================================================
    // PRIVATE STATE
    // ================================================================

    private bool    _isOpen     = false;
    private bool    _isMoving   = false;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;

    // Cache des objets présents dans le tiroir à l'ouverture
    private readonly List<ValueObject> _initialObjects = new();

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();

        _closedPosition = transform.localPosition;
        _openPosition   = _closedPosition 
                        + transform.localRotation * Vector3.forward * _openDistance;
        _targetPosition = _closedPosition;

        // Désactive la zone trigger au départ (tiroir fermé)
        if (_zoneCollider != null) 
            _zoneCollider.enabled = false;

        // Inventorie les ValueObject enfants et kinematise-les
        InitializeContents();
    }

    protected override void Update()
    {
        base.Update();

        if (!_isMoving) return;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition, 
            _targetPosition, 
            _slideSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.localPosition, _targetPosition) < 0.001f)
        {
            transform.localPosition = _targetPosition;
            _isMoving = false;

            // Callbacks de fin d'animation
            if (_isOpen)
                OnOpeningFinished();
            else
                OnClosingFinished();
        }
    }

    // ================================================================
    // INITIALIZATION
    // ================================================================

    private void InitializeContents()
    {
        foreach (var obj in GetComponentsInChildren<ValueObject>())
        {
            _initialObjects.Add(obj);
            
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
            }
        }
    }

    // ================================================================
    // CALLBACKS FIN D'ANIMATION
    // ================================================================

    private void OnOpeningFinished()
    {
        // Active le trigger — les objets peuvent maintenant sortir
        if (_zoneCollider != null)
            _zoneCollider.enabled = true;

        // Libère les Rigidbody des objets encore présents
        foreach (var obj in GetPresentObjects())
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity  = true;
            }
        }
    }

    private void OnClosingFinished()
    {
        // Désactive le trigger
        if (_zoneCollider != null)
            _zoneCollider.enabled = false;

        // Re-kinematise les objets restants pour qu'ils suivent le tiroir
        foreach (var obj in GetPresentObjects())
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
                rb.velocity    = Vector3.zero;
            }
        }
    }

    // ================================================================
    // HELPER — retourne uniquement les objets encore enfants
    // (les objets pris par le joueur ont été dé-parentés)
    // ================================================================

    private List<ValueObject> GetPresentObjects()
    {
        var present = new List<ValueObject>();
        
        foreach (var obj in _initialObjects)
        {
            if (obj != null && obj.transform.IsChildOf(transform))
                present.Add(obj);
        }
        
        return present;
    }

    // ================================================================
    // IINTERACTABLE OVERRIDE
    // ================================================================

    public override bool CanInteract(GameObject interactor) => !_isMoving;

    public override void Interact(GameObject interactor)
    {
        _isOpen         = !_isOpen;
        _targetPosition = _isOpen ? _openPosition : _closedPosition;
        _isMoving       = true;

        // Si on ferme, re-kinematise immédiatement les objets
        // pour qu'ils suivent le tiroir sans glisser
        if (!_isOpen)
        {
            if (_zoneCollider != null)
                _zoneCollider.enabled = false;

            foreach (var obj in GetPresentObjects())
            {
                var rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity  = false;
                    rb.velocity    = Vector3.zero;
                }
            }
        }

        // Récupère le NoiseEmitter du joueur
        if (_noise == null)
            _noise = interactor.GetComponent<PlayerNoiseEmitter>();

        // Grincement aléatoire
        bool squeaks = Random.value < _squeakChance;
        float range  = squeaks ? GetSqueakRange() : GetNormalRange();
        NiveauBruit level = squeaks ? NiveauBruit.Leger : NiveauBruit.Silencieux;

        if (squeaks)
            _noise?.EmettreBruit(level, range);
    }

    public override string GetInteractionLabel()
    {
        if (_isMoving) return "...";

        int count = GetPresentObjects().Count;
        
        if (_isOpen && count > 0)
            return $"Fermer le tiroir ({count} objet{(count > 1 ? "s" : "")})";
        
        return _isOpen ? "Fermer le tiroir" : "Ouvrir le tiroir";
    }

    // ================================================================
    // NOISE RANGES
    // ================================================================

    private float GetNormalRange()
    {
        return _config != null 
            ? _config.NormalOpenNoiseRange 
            : 4f;
    }

    private float GetSqueakRange()
    {
        return _config != null 
            ? _config.SqueakNoiseRange 
            : 7f;
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public bool IsOpen      => _isOpen;
    public int  ObjectCount => GetPresentObjects().Count;
}