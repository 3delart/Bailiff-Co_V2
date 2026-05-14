// ============================================================
// PlayerCarry.cs — Bailiff & Co  V2
// Saisir, porter, poser délicatement ou lancer un objet.
// Clic gauche = poser | Clic droit = lancer
// La masse de l'objet influence la vitesse de lancer.
//
// CHANGEMENTS V2 :
//   - Valeurs de lancer viennent de PlayerConfigData
//   - ObjetValeur → ValueObject
//   - OnBruitEmis → OnNoiseEmitted
// ============================================================
using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfigData _config;

    [Header("Références")]
    [SerializeField] private Transform          _pointDePort;
    [SerializeField] private PlayerNoiseEmitter _noise;

    private ValueObject _objetPorte;
    private Rigidbody   _rbPorte;
    private int         _layerOriginal;
    private Collider[]  _collidersPortes;

    private const string LAYER_PORTE = "ObjetPorte";

    // ================================================================
    // UPDATE / FIXED UPDATE
    // ================================================================

    private void Update()
    {
        if (_objetPorte == null) return;

        if (Input.GetMouseButtonDown(0))
            Poser(doux: true);

        if (Input.GetMouseButtonDown(1))
            Lancer();
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

        _objetPorte.ReleaseCarrier();
        _objetPorte = null;
        _rbPorte    = null;
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