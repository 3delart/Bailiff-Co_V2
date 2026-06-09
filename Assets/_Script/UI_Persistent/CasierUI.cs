// ============================================================
// CasierUI.cs — Casier Hub : équiper le loadout (roue radiale, drag-drop).
// 2 prefabs typés (chip outil / chip conso, via CasierChipUI) réutilisés
// dans les colonnes possédés ET dans les slots de la roue.
// Les slots de la roue sont de simples cases vides (drop targets).
// Panel Blocking, contexte Hub. Ouvert par HubPNJ type Inventaire.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class CasierUI : UIPanel
{
    public static CasierUI Current { get; private set; }

    [Header("Colonnes possédés")]
    [SerializeField] private Transform  _poolOutils;
    [SerializeField] private Transform  _poolConso;

    [Header("Prefabs chips (CasierDragItem + CasierChipUI à la racine)")]
    [SerializeField] private GameObject _chipOutilPrefab;   // Icon + Nom + Extra(=niveau)
    [SerializeField] private GameObject _chipConsoPrefab;   // Icon + Nom + Extra(=qté) + Remove/Moins/Plus

    [Header("Roue — slots (cases VIDES avec CasierDropSlot)")]
    [SerializeField] private Transform[] _slotsOutils = new Transform[3];
    [SerializeField] private Transform[] _slotsConso  = new Transform[3];

    [Header("Fermer")]
    [SerializeField] private Button _boutonFermer;

    private InventaireSystem _inv;
    private CasierDragItem _drag;

    protected override void OnEnable()
    {
        base.OnEnable();
        Current = this;
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        if (_boutonFermer) { _boutonFermer.onClick.RemoveAllListeners(); _boutonFermer.onClick.AddListener(Fermer); }
        Render();
    }

    protected override void OnDisable()
    {
        if (_boutonFermer) _boutonFermer.onClick.RemoveAllListeners();
        if (Current == this) Current = null;
        base.OnDisable();
    }

    public void SetDrag(CasierDragItem d) => _drag = d;
    public void EndDrag() => _drag = null;

    public void HandleDrop(CasierDropSlot slot)
    {
        if (_drag == null || _inv == null) return;
        switch (slot.SlotZone)
        {
            case CasierDropSlot.Zone.ToolSlot:
                if (_drag.ItemKind == CasierDragItem.Kind.Tool) _inv.EquiperOutilSlot(ResoudreOutil(_drag.Id), slot.Index);
                break;
            case CasierDropSlot.Zone.ConsoSlot:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso) _inv.EquiperConsoSlot(_drag.Id, _inv.MaxCarryConso(_drag.Id), slot.Index);
                break;
            case CasierDropSlot.Zone.ToolPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Tool) _inv.DesequiperOutil(ResoudreOutil(_drag.Id));
                break;
            case CasierDropSlot.Zone.ConsoPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso) _inv.DesequiperConso(_drag.Id);
                break;
        }
        Render();
    }

    private ToolData ResoudreOutil(string id)
    {
        foreach (var kv in _inv.Outils) if (kv.Key != null && kv.Key.Id == id) return kv.Key;
        return null;
    }

    // ================================================================
    // RENDU
    // ================================================================

    private void Render()
    {
        if (_inv == null) return;

        Vider(_poolOutils);
        if (_poolOutils && _chipOutilPrefab)
            foreach (var kv in _inv.Outils)
                if (!_inv.OutilEstEquipe(kv.Key)) ChipOutil(_poolOutils, kv.Key, kv.Value, slot: false);

        Vider(_poolConso);
        if (_poolConso && _chipConsoPrefab)
            foreach (var kv in _inv.Consommables)
                if (!_inv.ConsoEstEquipe(kv.Key) && kv.Value > 0) ChipConso(_poolConso, kv.Key, slotEquipe: null);

        for (int i = 0; i < _slotsOutils.Length; i++)
        {
            Vider(_slotsOutils[i]);
            var o = _inv.OutilAuSlot(i);
            if (o != null) ChipOutil(_slotsOutils[i], o, _inv.NiveauOutil(o), slot: true);
        }

        for (int i = 0; i < _slotsConso.Length; i++)
        {
            Vider(_slotsConso[i]);
            var ce = _inv.ConsoAuSlot(i);
            if (ce.HasValue) ChipConso(_slotsConso[i], ce.Value.Type, slotEquipe: ce.Value);
        }
    }

    // ================================================================
    // CHIPS (outil / conso ; en pool OU dans un slot)
    // ================================================================

    private void ChipOutil(Transform parent, ToolData o, int niv, bool slot)
    {
        var go = Instantiate(_chipOutilPrefab, parent);
        var d  = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Tool; d.Id = o.Id; }
        var c  = go.GetComponent<CasierChipUI>();   if (c == null) return;
        c.SetIcon(o.Icon);
        if (c.Nom)   c.Nom.text   = o.DisplayName;
        if (c.Extra) c.Extra.text = Pips(niv, o.Levels.Length);
        if (slot) c.ModeSlotIcone();                       // slot = icône seule (déséquiper en glissant)
        else { c.SetVisible(true, true); c.ShowBoutons(false, false); }
    }

    private void ChipConso(Transform parent, string type, ConsoEquipe? slotEquipe)
    {
        var go = Instantiate(_chipConsoPrefab, parent);
        var d  = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Conso; d.Id = type; }
        var c  = go.GetComponent<CasierChipUI>();   if (c == null) return;

        var def = _inv.ConsoDef(type);
        c.SetIcon(def != null ? def.Icon : null);
        if (c.Nom) c.Nom.text = def != null ? def.DisplayName : type;

        if (slotEquipe == null)
        {
            // En pool : stock + max
            int stock = _inv.QuantiteConsommable(type);
            if (c.Extra) c.Extra.text = "x" + stock + " · max " + _inv.MaxCarryConso(type);
            c.ShowBoutons(remove: false, stepper: false);
            c.SetVisible(nom: true, extra: true);   // pool = complet
        }
        else
        {
            // Équipé : qté/max + retirer + stepper
            c.ModeSlotIcone();   // slot = icône seule (déséquiper en glissant vers la colonne)
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private static string Pips(int level, int total)
    { var s = ""; for (int i = 0; i < total; i++) s += i <= level ? "●" : "○"; return s; }

    private static void Vider(Transform t)
    { if (t == null) return; for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }
}
