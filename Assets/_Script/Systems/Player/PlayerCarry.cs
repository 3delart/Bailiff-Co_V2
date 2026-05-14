// ============================================================
// PlayerCarry.cs — Bailiff & Co  V4
// Saisir, porter, poser délicatement ou lancer un objet.
// Nouveau système de pose précise avec prévisualisation ghost.
//
// CHANGEMENTS V4 :
//   - ✅ State machine: Idle / Holding / Previewing
//   - ✅ Clic gauche maintenu = ghost preview, relâchage = pose précise
//   - ✅ Clic droit pendant preview = annuler, pendant holding = lancer
//   - ✅ Rotation via scroll wheel (PlacementPreview)
//   - ✅ Event OnObjectPlaced levé au placement
// ============================================================
using System.Collections;
using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    private enum CarryState { Idle, Holding, Previewing }

    [Header("Configuration")]
    [SerializeField] private PlayerConfigData _config;

    [Header("Références")]
    [SerializeField] private Transform          _pointDePort;
    [SerializeField] private Transform          _camera;
    [SerializeField] private PlayerNoiseEmitter _noise;
    [SerializeField] private PlacementPreview   _placementPreview;

    [Header("Protection Drop (optionnel)")]
    [Tooltip("Durée pendant laquelle les dégâts sont désactivés après une pose douce")]
    [SerializeField] private float _dropProtectionDuration = 0.5f;

    private ValueObject _objetPorte;
    private Rigidbody   _rbPorte;
    private int         _layerOriginal;
    private Collider[]  _collidersPortes;
    private CarryState  _state = CarryState.Idle;

    private const string LAYER_PORTE = "ObjetPorte";

    // ================================================================
    // UPDATE / FIXED UPDATE
    // ================================================================

    private void Update()
    {
        if (_objetPorte == null) return;

        // Right click always throws (regardless of state)
        if (Input.GetMouseButtonDown(1) && _state == CarryState.Holding)
        {
            Lancer();
            return;
        }

        // Left click: maintain preview while held
        bool leftClickHeld = Input.GetMouseButton(0);

        if (_state == CarryState.Holding && leftClickHeld)
        {
            // Enter preview mode
            _state = CarryState.Previewing;

            // Auto-create PlacementPreview if missing
            if (_placementPreview == null)
            {
                GameObject previewGO = new GameObject("PlacementPreview");
                previewGO.transform.SetParent(transform);
                _placementPreview = previewGO.AddComponent<PlacementPreview>();
            }

            if (_placementPreview != null && _config != null)
                _placementPreview.BeginPreview(_objetPorte, _camera, _config);
        }

        if (_state == CarryState.Previewing)
        {
            if (_placementPreview != null)
            {
                _placementPreview.UpdatePreview();

                if (Input.GetMouseButtonDown(1))  // Right click cancels
                {
                    _placementPreview.EndPreview();
                    _state = CarryState.Holding;
                }
            }

            // Release left click: place or cancel
            if (!leftClickHeld)
            {
                if (_placementPreview != null && _placementPreview.IsValid)
                {
                    Vector3    pos = _placementPreview.PreviewPosition;
                    Quaternion rot = _placementPreview.PreviewRotation;
                    _placementPreview.EndPreview();
                    PoserAPosition(pos, rot);
                }
                else
                {
                    if (_placementPreview != null)
                        _placementPreview.EndPreview();
                    _state = CarryState.Holding;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (_objetPorte == null || _rbPorte == null || _pointDePort == null) return;

        _rbPorte.MovePosition(_pointDePort.position);
        _rbPorte.MoveRotation(_pointDePort.rotation);
    }

    // ================================================================
    // SAISIR
    // ================================================================

    public void Saisir(ValueObject objet)
    {
        if (_objetPorte != null) return;

        _objetPorte = objet;
        _rbPorte    = objet.GetComponent<Rigidbody>();
        _state      = CarryState.Holding;

        // Dé-parente l'objet de son conteneur (tiroir, meuble…)
        objet.transform.SetParent(null);

        if (_rbPorte != null)
        {
            _rbPorte.isKinematic = true;
            _rbPorte.useGravity  = false;
        }

        // Désactive tous les colliders pendant le transport — évite la collision avec le CharacterController
        _collidersPortes = objet.GetComponentsInChildren<Collider>();
        foreach (var col in _collidersPortes)
            col.enabled = false;

        int layerPorte = LayerMask.NameToLayer(LAYER_PORTE);
        if (layerPorte == -1)
        {
            Debug.LogError("Layer 'ObjetPorte' introuvable — crée-le dans Project Settings → Tags & Layers");
            return;
        }
        _layerOriginal         = objet.gameObject.layer;
        objet.gameObject.layer = layerPorte;
    }

    // ================================================================
    // POSER
    // ================================================================

    public void Poser(bool doux)
    {
        if (_objetPorte == null) return;

        _state = CarryState.Idle;
        _placementPreview?.EndPreview();

        _objetPorte.gameObject.layer = _layerOriginal;

        // Réactive les colliders avant de rendre l'objet dynamique (nécessaire pour OnTriggerEnter coffre)
        if (_collidersPortes != null)
        {
            foreach (var col in _collidersPortes)
                if (col != null) col.enabled = true;
            _collidersPortes = null;
        }

        if (_rbPorte != null)
        {
            _rbPorte.isKinematic = false;
            _rbPorte.useGravity  = true;
            _rbPorte.constraints = RigidbodyConstraints.None;

            if (doux) _rbPorte.linearVelocity = Vector3.zero;
        }

        if (!doux)
            _noise?.EmettreBruit(NiveauBruit.Leger, 3f);

        // Activer la protection sur ValueObject directement
        if (doux && _objetPorte != null)
        {
            _objetPorte.ActivateDamageProtection(_dropProtectionDuration);
        }

        _objetPorte.ReleaseCarrier();
        _objetPorte = null;
        _rbPorte    = null;
    }

    // ================================================================
    // POSER À POSITION (Placement Preview)
    // ================================================================

    private void PoserAPosition(Vector3 worldPos, Quaternion worldRot)
    {
        if (_objetPorte == null || _rbPorte == null) return;

        // Teleport object to preview position/rotation
        _objetPorte.transform.position = worldPos;
        _objetPorte.transform.rotation = worldRot;

        // Re-enable colliders
        _objetPorte.gameObject.layer = _layerOriginal;
        if (_collidersPortes != null)
        {
            foreach (var col in _collidersPortes)
                if (col != null) col.enabled = true;
            _collidersPortes = null;
        }

        // Make dynamic
        if (_rbPorte != null)
        {
            _rbPorte.isKinematic = false;
            _rbPorte.useGravity  = true;
            _rbPorte.constraints = RigidbodyConstraints.None;
            _rbPorte.linearVelocity = Vector3.zero;
        }

        // Activate damage protection for gentle placement
        if (_objetPorte != null)
        {
            _objetPorte.ActivateDamageProtection(_dropProtectionDuration);
        }

        // Raise event
        EventBus<OnObjectPlaced>.Raise(new OnObjectPlaced
        {
            Object = _objetPorte.Data,
            Position = worldPos,
            Rotation = worldRot,
            InTrunk = false  // Could check if position is inside VehicleTrunkZone
        });

        _objetPorte.ReleaseCarrier();
        _objetPorte = null;
        _rbPorte = null;
        _state = CarryState.Idle;
    }

    // ================================================================
    // LANCER
    // La vitesse = vitesseBase / masse, clampée entre min et max.
    // Un objet de 1kg   → vitesseBase      (ex: 10 m/s)
    // Un objet de 3.5kg → vitesseBase / 3.5 (ex: ~2.9 m/s)
    // Un objet de 0.5kg → vitesseBase / 0.5 (ex: 20 m/s, clampé à max)
    // ================================================================

    private void Lancer()
    {
        if (_objetPorte == null || _rbPorte == null) return;
        if (_config == null)
        {
            Debug.LogError("[PlayerCarry] PlayerConfigData manquant !");
            return;
        }

        Rigidbody rb    = _rbPorte;
        float     masse = rb.mass;

        Poser(doux: false); // → isKinematic = false ici

        Transform cam       = Camera.main != null ? Camera.main.transform : transform;
        Vector3   direction = (cam.forward + Vector3.up * 0.1f).normalized;

        float vitesse = Mathf.Clamp(_config.BaseThrowVelocity / masse,
                                     _config.MinThrowVelocity,
                                     _config.MaxThrowVelocity);

        rb.linearVelocity = direction * vitesse;

        // Bruit proportionnel à la vitesse de lancer
        NiveauBruit niveau = vitesse > 8f ? NiveauBruit.Fort : NiveauBruit.Leger;
        _noise?.EmettreBruit(niveau, vitesse * 0.6f);
    }

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    public bool        EstEnTrain  => _objetPorte != null;
    public ValueObject ObjetEnMain => _objetPorte;
}