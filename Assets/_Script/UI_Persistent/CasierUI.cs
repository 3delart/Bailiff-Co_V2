// ============================================================
// CasierUI.cs — Casier Hub : équiper le loadout (roue radiale, drag-drop).
// 3 colonnes : outils possédés (gauche) / roue (centre) / conso possédés (droite).
// Panel Blocking, contexte Hub. Ouvert par HubPNJ type Inventaire.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasierUI : UIPanel
{
    public static CasierUI Current { get; private set; }

    [Header("Colonnes possédés")]
    [SerializeField] private Transform  _poolOutils;
    [SerializeField] private Transform  _poolConso;
    [SerializeField] private GameObject _chipPrefab;      // CasierDragItem + TMP_Text

    [Header("Roue — slots (avec CasierDropSlot)")]
    [SerializeField] private Transform[] _slotsOutils = new Transform[3];
    [SerializeField] private Transform[] _slotsConso  = new Transform[3];
    [SerializeField] private GameObject  _slotItemPrefab; // CasierDragItem + TMP + boutons Remove/Moins/Plus

    private InventaireSystem _inv;
    private CasierDragItem _drag;

    protected override void OnEnable()
    {
        base.OnEnable();
        Current = this;
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        Render();
    }

    protected override void OnDisable()
    {
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
                if (_drag.ItemKind == CasierDragItem.Kind.Tool)
                    _inv.EquiperOutil(ResoudreOutil(_drag.Id));
                break;
            case CasierDropSlot.Zone.ConsoSlot:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso)
                    _inv.EquiperConso(_drag.Id, _inv.MaxCarryConso(_drag.Id));
                break;
            case CasierDropSlot.Zone.ToolPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Tool)
                    _inv.DesequiperOutil(ResoudreOutil(_drag.Id));
                break;
            case CasierDropSlot.Zone.ConsoPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso)
                    _inv.DesequiperConso(_drag.Id);
                break;
        }
        Render();
    }

    private OutilData ResoudreOutil(string toolName)
    {
        foreach (var kv in _inv.Outils) if (kv.Key.ToolName == toolName) return kv.Key;
        return null;
    }

    private void Render()
    {
        if (_inv == null) return;

        // Pools (non équipés)
        Vider(_poolOutils);
        if (_poolOutils != null && _chipPrefab != null)
            foreach (var kv in _inv.Outils)
                if (!_inv.OutilEstEquipe(kv.Key))
                    ChipOutil(_poolOutils, kv.Key, kv.Value);

        Vider(_poolConso);
        if (_poolConso != null && _chipPrefab != null)
            foreach (var kv in _inv.Consommables)
                if (!_inv.ConsoEstEquipe(kv.Key) && kv.Value > 0)
                    ChipConso(_poolConso, kv.Key, kv.Value);

        // Slots roue
        var outils = _inv.OutilsEquipes;
        for (int i = 0; i < _slotsOutils.Length; i++)
        {
            Vider(_slotsOutils[i]);
            if (i < outils.Count) SlotOutil(_slotsOutils[i], outils[i], _inv.NiveauOutil(outils[i]));
        }
        var consos = _inv.ConsosEquipes;
        for (int i = 0; i < _slotsConso.Length; i++)
        {
            Vider(_slotsConso[i]);
            if (i < consos.Count) SlotConso(_slotsConso[i], consos[i]);
        }
    }

    // --- chips de pool ---
    private void ChipOutil(Transform parent, OutilData o, int niv)
    {
        var go = Instantiate(_chipPrefab, parent);
        var d = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Tool; d.Id = o.ToolName; }
        var t = go.GetComponentInChildren<TMP_Text>(); if (t) t.text = o.ToolName + "  " + Pips(niv, o.Levels.Length);
    }
    private void ChipConso(Transform parent, string type, int stock)
    {
        var go = Instantiate(_chipPrefab, parent);
        var d = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Conso; d.Id = type; }
        var t = go.GetComponentInChildren<TMP_Text>(); if (t) t.text = type + "  x" + stock + " · max " + _inv.MaxCarryConso(type);
    }

    // --- items dans les slots de la roue ---
    private void SlotOutil(Transform slot, OutilData o, int niv)
    {
        if (slot == null || _slotItemPrefab == null) return;
        var go = Instantiate(_slotItemPrefab, slot);
        var d = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Tool; d.Id = o.ToolName; }
        var t = go.GetComponentInChildren<TMP_Text>(); if (t) t.text = o.ToolName + "\n" + Pips(niv, o.Levels.Length);
        var x = TrouverBouton(go, "Remove"); if (x) x.onClick.AddListener(() => { _inv.DesequiperOutil(o); Render(); });
    }
    private void SlotConso(Transform slot, ConsoEquipe c)
    {
        if (slot == null || _slotItemPrefab == null) return;
        var go = Instantiate(_slotItemPrefab, slot);
        var d = go.GetComponent<CasierDragItem>(); if (d) { d.ItemKind = CasierDragItem.Kind.Conso; d.Id = c.Type; }
        int mq = Mathf.Min(_inv.MaxCarryConso(c.Type), _inv.QuantiteConsommable(c.Type));
        var t = go.GetComponentInChildren<TMP_Text>(); if (t) t.text = c.Type + "\n" + c.Quantite + "/" + mq;
        var moins = TrouverBouton(go, "Moins"); if (moins) moins.onClick.AddListener(() => { _inv.EquiperConso(c.Type, c.Quantite - 1); Render(); });
        var plus  = TrouverBouton(go, "Plus");  if (plus)  plus.onClick.AddListener(()  => { _inv.EquiperConso(c.Type, c.Quantite + 1); Render(); });
        var x     = TrouverBouton(go, "Remove");if (x)     x.onClick.AddListener(()     => { _inv.DesequiperConso(c.Type); Render(); });
    }

    private static Button TrouverBouton(GameObject go, string nom)
    {
        var t = go.transform.Find(nom);
        return t != null ? t.GetComponent<Button>() : null;
    }
    private static string Pips(int level, int total)
    { var s = ""; for (int i = 0; i < total; i++) s += i <= level ? "●" : "○"; return s; }
    private static void Vider(Transform t)
    { if (t == null) return; for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }
}
