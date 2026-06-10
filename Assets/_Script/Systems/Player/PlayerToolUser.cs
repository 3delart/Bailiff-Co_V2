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
    [Tooltip("Ancre de tenue (PointPortage, sous un os du torse) — l'objet en main y est monté ; les mains le rejoignent en IK.")]
    [SerializeField] private Transform        _pointDePort;
    [Tooltip("Fallback si PointPortage absent : os de la main droite.")]
    [SerializeField] private Transform        _handBone;
    [SerializeField] private PlayerAnimator   _playerAnimator;
    [SerializeField] private HoldIK           _holdIK;

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
        if (_inventaire == null) _inventaire = GetComponentInChildren<InventaireSystem>();
        if (_playerAnimator == null) _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        if (_holdIK == null) _holdIK = GetComponentInChildren<HoldIK>();

        // Réfs centralisées : remplit depuis le component Player si non assignées.
        var player = GetComponent<Player>();
        if (player != null)
        {
            if (_camera      == null) _camera      = player.Camera;
            if (_handBone    == null) _handBone    = player.HandBone;
            if (_pointDePort == null) _pointDePort = player.PointDePort;
        }
        if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
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

        // Modèle 3D monté sur l'ancre de tenue (PointPortage, sous le torse) ; les mains le rejoignent en IK.
        Transform ancre = _pointDePort != null ? _pointDePort : _handBone;
        if (item.HandPrefab != null && ancre != null)
        {
            _modeleEnMain = Instantiate(item.HandPrefab, ancre);
            _modeleEnMain.transform.localPosition = item.HoldOffset;
            _modeleEnMain.transform.localRotation = Quaternion.Euler(item.HoldEuler);
        }
        else if (ancre == null)
        {
            Debug.LogWarning("[PlayerToolUser] PointPortage/_handBone non assignés — l'item est " +
                             "pris en main logiquement mais aucun modèle visible.");
        }

        // Pose de tenue + clip d'usage sur le haut du corps.
        _playerAnimator?.TenirOutil(item.HandCount, item.UseAnimation, item.HoldPose);

        // Hand IK : mains collées aux grips de l'objet (offsets relus en live → réglables en playmode).
        if (_modeleEnMain != null)
            _holdIK?.Hold(_modeleEnMain.transform, item.HandCount, () => item.GripRight, () => item.GripLeft);

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
            Animator     = _playerAnimator,
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

        _playerAnimator?.RangerMains();
        _holdIK?.Release();
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
        // Offset de tenue appliqué EN LIVE (réglable en playmode, comme pour les objets portés).
        if (_modeleEnMain != null && _itemActif != null)
        {
            _modeleEnMain.transform.localPosition = _itemActif.HoldOffset;
            _modeleEnMain.transform.localRotation = Quaternion.Euler(_itemActif.HoldEuler);
        }

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
