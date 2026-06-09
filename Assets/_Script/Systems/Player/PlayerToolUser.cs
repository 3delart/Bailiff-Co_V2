// ============================================================
// PlayerToolUser.cs — Bailiff & Co
// ORCHESTRATEUR du système "objet en main".
//   - Prend un item en main (instancie le HandPrefab sur _pointDePort).
//   - Crée le ToolBehaviour correspondant (via la factory).
//   - Route les inputs (clic G tap/maintien/relâché, clic D) vers le behaviour.
// Mutuellement exclusif avec PlayerCarry (porter un objet de valeur).
// ============================================================
using UnityEngine;

[RequireComponent(typeof(PlayerCarry))]
public class PlayerToolUser : MonoBehaviour
{
    [SerializeField] private PlayerConfigData _config;
    [SerializeField] private Transform        _camera;
    [SerializeField] private PlayerCarry      _carry;
    [Tooltip("Ancre du modèle en main — assigner le MÊME transform que PlayerCarry._pointDePort.")]
    [SerializeField] private Transform        _pointDePort;

    private InventaireSystem _inventaire;

    private ItemData       _itemActif;
    private int            _niveauActif;
    private GameObject     _modeleEnMain;
    private ToolBehaviour  _behaviour;
    private ToolUseContext _ctx;

    public ItemData ItemActif   => _itemActif;
    public bool     ARienEnMain => _itemActif == null;

    private void Awake()
    {
        if (_carry == null)  _carry  = GetComponent<PlayerCarry>();
        if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
        if (_inventaire == null) _inventaire = GetComponentInChildren<InventaireSystem>();
    }

    // ================================================================
    // PRISE EN MAIN
    // ================================================================

    /// <summary>Wrapper compat (roue — slot outil).</summary>
    public void PrendreOutil(ToolData outil, int niveau) => PrendreObjet(outil, niveau);

    /// <summary>Met un item (outil OU consommable) en main. Refusé si un objet de valeur est porté.</summary>
    public void PrendreObjet(ItemData item, int niveau)
    {
        if (item == null) return;
        if (_carry != null && _carry.EstEnTrain) return;   // exclusion : objet porté

        RangerOutil();                                     // nettoie l'ancien

        _itemActif   = item;
        _niveauActif = Mathf.Max(0, niveau);

        // Modèle 3D en main.
        if (item.HandPrefab != null && _pointDePort != null)
        {
            _modeleEnMain = Instantiate(item.HandPrefab, _pointDePort);
            _modeleEnMain.transform.localPosition = Vector3.zero;
            _modeleEnMain.transform.localRotation = Quaternion.identity;
        }
        else if (_pointDePort == null)
        {
            Debug.LogWarning("[PlayerToolUser] _pointDePort non assigné — l'item est pris en main " +
                             "logiquement mais aucun modèle visible. Assigne-le dans l'Inspector.");
        }

        // Contexte + behaviour.
        _ctx = new ToolUseContext
        {
            Camera       = _camera,
            Config       = _config,
            Item         = item,
            Niveau       = _niveauActif,
            Stats        = ResoudreStats(item, _niveauActif),
            ConsoId      = item is ConsumableData c ? c.Id : null,
            ModeleEnMain = _modeleEnMain != null ? _modeleEnMain.transform : null,
            Inventaire   = _inventaire,
            User         = this
        };
        _behaviour = ToolBehaviourFactory.Create(item);
        _behaviour.OnEquip(_ctx);
    }

    public void RangerOutil()
    {
        _behaviour?.OnUnequip();
        _behaviour = null;
        _ctx       = null;

        if (_modeleEnMain != null) Destroy(_modeleEnMain);
        _modeleEnMain = null;
        _itemActif    = null;
    }

    private static EffectStats ResoudreStats(ItemData item, int niveau)
    {
        if (item is ToolData t)       return t.StatsForLevel(niveau);
        if (item is ConsumableData c) return c.Stats;
        return default;
    }

    // ================================================================
    // ROUTAGE INPUT
    // ================================================================

    private void Update()
    {
        if (_behaviour == null) return;

        if (GameManager.Instance != null && !GameManager.Instance.InputJoueurActif)
        {
            _behaviour.OnPrimaryUp();   // annule toute canalisation en cours
            return;
        }

        if (Input.GetMouseButtonDown(0))      _behaviour.OnPrimaryDown();
        if (Input.GetMouseButton(0))          _behaviour.OnPrimaryHold(Time.deltaTime);
        else if (Input.GetMouseButtonUp(0))   _behaviour.OnPrimaryUp();

        if (Input.GetMouseButtonDown(1))      _behaviour.OnSecondaryDown();

        _behaviour.Tick(Time.deltaTime);
    }
}
