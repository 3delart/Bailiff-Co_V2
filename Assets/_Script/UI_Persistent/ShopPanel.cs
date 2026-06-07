// ============================================================
// ShopPanel.cs — Boutique Hub (2 onglets : Outils / Consommables)
// Outils = maître/détail (liste + fiche niveaux). Conso = grille (achat 1-10).
// Panel Blocking, contexte Hub. Ouvert par HubPNJ type Boutique.
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : UIPanel
{
    [Header("Catalogue (assigné en Inspector)")]
    [SerializeField] private OutilData[] _catalogue;

    [Header("Onglets")]
    [SerializeField] private Button _ongletOutils;
    [SerializeField] private Button _ongletConso;

    [Header("Outils — maître/détail")]
    [SerializeField] private Transform  _listeOutilsRoot;
    [SerializeField] private GameObject _ligneOutilPrefab; // Button + TMP_Text
    [SerializeField] private GameObject _ficheOutil;
    [SerializeField] private TMP_Text   _ficheNom, _ficheDesc, _ficheNiveaux;
    [SerializeField] private Button     _ficheBouton;
    [SerializeField] private TMP_Text   _ficheBoutonLabel;

    [Header("Conso — grille")]
    [SerializeField] private Transform  _grilleConsoRoot;
    [SerializeField] private GameObject _carteConsoPrefab; // ShopConsoCard

    [Header("Argent")]
    [SerializeField] private TMP_Text _argentLabel;

    private InventaireSystem _inv;
    private OutilData _selection;
    private readonly Dictionary<string,int> _qtyAchat = new();

    protected override void OnEnable()
    {
        base.OnEnable();
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        if (_ongletOutils) _ongletOutils.onClick.AddListener(() => AfficherOnglet(true));
        if (_ongletConso)  _ongletConso.onClick.AddListener(() => AfficherOnglet(false));
        AfficherOnglet(true);
        MajArgent();
    }

    protected override void OnDisable()
    {
        if (_ongletOutils) _ongletOutils.onClick.RemoveAllListeners();
        if (_ongletConso)  _ongletConso.onClick.RemoveAllListeners();
        base.OnDisable();
    }

    private void MajArgent()
    {
        if (_argentLabel) _argentLabel.text = (GameManager.Instance?.Argent ?? 0f).ToString("N0") + " €";
    }

    private void AfficherOnglet(bool outils)
    {
        if (_listeOutilsRoot && _listeOutilsRoot.parent) _listeOutilsRoot.parent.gameObject.SetActive(outils);
        if (_ficheOutil) _ficheOutil.SetActive(outils);
        if (_grilleConsoRoot && _grilleConsoRoot.parent) _grilleConsoRoot.parent.gameObject.SetActive(!outils);
        if (outils) RemplirOutils(); else RemplirConso();
    }

    private bool EstVerrouille(OutilData o)
        => o.UnlocksAfterMission > (GameManager.Instance?.DerniereMissionCompletee ?? 0);

    // ---- OUTILS ----
    private void RemplirOutils()
    {
        if (_listeOutilsRoot == null || _ligneOutilPrefab == null || _catalogue == null) return;
        Vider(_listeOutilsRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || o.IsConsumable) continue;
            var go = Instantiate(_ligneOutilPrefab, _listeOutilsRoot);
            var label = go.GetComponentInChildren<TMP_Text>();
            int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
            if (label) label.text = o.ToolName +
                (niv >= 0 ? "  " + Pips(niv, o.Levels.Length)
                          : (EstVerrouille(o) ? "  🔒 M" + o.UnlocksAfterMission : ""));
            var btn = go.GetComponent<Button>();
            var captured = o;
            if (btn) btn.onClick.AddListener(() => { _selection = captured; RemplirFiche(); });
        }
        if (_selection == null) _selection = System.Array.Find(_catalogue, x => x != null && !x.IsConsumable);
        RemplirFiche();
    }

    private void RemplirFiche()
    {
        if (_selection == null || _ficheOutil == null) return;
        var o = _selection;
        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        bool possede = niv >= 0;
        bool verrou = EstVerrouille(o);

        if (_ficheNom)  _ficheNom.text  = o.ToolName + (possede ? "  " + Pips(niv, o.Levels.Length) : "");
        if (_ficheDesc) _ficheDesc.text = o.Description;

        if (_ficheNiveaux)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < o.Levels.Length; i++)
            {
                var lv = o.Levels[i]; if (lv == null) continue;
                string cout = i == 0 ? (possede ? "acquis" : o.PurchasePrice + " €") : lv.UpgradeCost + " €";
                string mark = (possede && i == niv) ? "  ◄" : "";
                sb.AppendLine($"{lv.LevelName}  —  {lv.EffectDescription}  ({cout}){mark}");
            }
            _ficheNiveaux.text = sb.ToString();
        }

        if (_ficheBouton == null || _ficheBoutonLabel == null) return;
        _ficheBouton.onClick.RemoveAllListeners();
        if (verrou && !possede)
        {
            _ficheBoutonLabel.text = "🔒 Mission " + o.UnlocksAfterMission;
            _ficheBouton.interactable = false;
        }
        else if (!possede)
        {
            bool ok = (GameManager.Instance?.Argent ?? 0) >= o.PurchasePrice;
            _ficheBoutonLabel.text = ok ? $"Acheter · {o.PurchasePrice} €" : "Fonds insuffisants";
            _ficheBouton.interactable = ok;
            _ficheBouton.onClick.AddListener(() => Acheter(o));
        }
        else if (niv >= o.Levels.Length - 1)
        {
            _ficheBoutonLabel.text = "Niveau max";
            _ficheBouton.interactable = false;
        }
        else
        {
            int cost = o.Levels[niv + 1].UpgradeCost;
            bool ok = (GameManager.Instance?.Argent ?? 0) >= cost;
            _ficheBoutonLabel.text = ok ? $"Upgrader · {cost} €" : "Fonds insuffisants";
            _ficheBouton.interactable = ok;
            _ficheBouton.onClick.AddListener(() => Upgrader(o));
        }
    }

    private void Acheter(OutilData o)
    {
        if (_inv == null || !(GameManager.Instance?.PeutPayer(o.PurchasePrice) ?? false)) return;
        GameManager.Instance.Debiter(o.PurchasePrice);
        _inv.AjouterOutil(o);
        MajArgent(); RemplirOutils();
    }

    private void Upgrader(OutilData o)
    {
        if (_inv == null) return;
        int niv = _inv.NiveauOutil(o);
        if (niv < 0 || niv >= o.Levels.Length - 1) return;
        int cost = o.Levels[niv + 1].UpgradeCost;
        if (!(GameManager.Instance?.PeutPayer(cost) ?? false)) return;
        GameManager.Instance.Debiter(cost);
        _inv.UpgraderOutil(o);
        MajArgent(); RemplirOutils();
    }

    // ---- CONSO ----
    private void RemplirConso()
    {
        if (_grilleConsoRoot == null || _carteConsoPrefab == null || _catalogue == null) return;
        Vider(_grilleConsoRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || !o.IsConsumable) continue;
            var go = Instantiate(_carteConsoPrefab, _grilleConsoRoot);
            var card = go.GetComponent<ShopConsoCard>();
            if (card == null) continue;
            int qty = _qtyAchat.TryGetValue(o.ToolName, out var q) ? q : 1;
            var captured = o;
            card.Bind(captured, qty, EstVerrouille(captured), _inv,
                onStep: (d) => { int nq = Mathf.Clamp(qty + d, 1, 10); _qtyAchat[captured.ToolName] = nq; RemplirConso(); },
                onBuy:  () => AcheterConso(captured, qty));
        }
    }

    private void AcheterConso(OutilData o, int qty)
    {
        if (_inv == null) return;
        int total = o.PurchasePrice * qty;
        if (!(GameManager.Instance?.PeutPayer(total) ?? false)) return;
        GameManager.Instance.Debiter(total);
        _inv.AjouterConsommable(o, qty, o.PurchasePrice);
        _qtyAchat[o.ToolName] = 1;
        MajArgent(); RemplirConso();
    }

    private static string Pips(int level, int total)
    { var s = ""; for (int i = 0; i < total; i++) s += i <= level ? "●" : "○"; return s; }

    private static void Vider(Transform t)
    { if (t == null) return; for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }
}
