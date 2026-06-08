// ============================================================
// ShopPanel.cs — Boutique Hub (2 onglets : Outils / Consommables)
// Master/détail UNIFIÉ : les 2 onglets sont des listes qui alimentent
// la même FicheDetails. Outil → table niveaux + Acheter/Upgrader.
// Conso → prix/max/possédés + stepper quantité (1-10) + Acheter.
// Panel Blocking, contexte Hub. Ouvert par HubPNJ type Boutique.
// ============================================================
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
    [SerializeField] private Button _boutonFermer;          // ferme la boutique

    [Header("Listes (lignes via _ligneOutilPrefab)")]
    [SerializeField] private GameObject _paneOutils;        // wrapper onglet Outils (ex: PanelOutils)
    [SerializeField] private GameObject _paneConso;         // wrapper onglet Conso  (ex: PanelConso)
    [SerializeField] private Transform  _listeOutilsRoot;   // ContentOutils
    [SerializeField] private Transform  _grilleConsoRoot;   // ContentConso (désormais une LISTE)
    [SerializeField] private GameObject _ligneOutilPrefab;  // Button + TMP_Text (réutilisé outils ET conso)

    [Header("Fiche partagée (outils + conso)")]
    [SerializeField] private GameObject _ficheOutil;
    [SerializeField] private TMP_Text   _ficheNom, _ficheDesc, _ficheNiveaux;
    [SerializeField] private Button     _ficheBouton;
    [SerializeField] private TMP_Text   _ficheBoutonLabel;

    [Header("Fiche — stepper conso (montré pour les consommables)")]
    [SerializeField] private GameObject _ficheStepper;
    [SerializeField] private Button     _ficheStepMoins, _ficheStepPlus;
    [SerializeField] private TMP_Text   _ficheStepQty;

    [Header("Argent")]
    [SerializeField] private TMP_Text _argentLabel;

    private InventaireSystem _inv;
    private OutilData _selection;   // outil OU conso (OutilData.IsConsumable distingue)
    private int _qtyConso = 1;

    protected override void OnEnable()
    {
        base.OnEnable();
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        if (_ongletOutils) { _ongletOutils.onClick.RemoveAllListeners(); _ongletOutils.onClick.AddListener(() => AfficherOnglet(true)); }
        if (_ongletConso)  { _ongletConso.onClick.RemoveAllListeners();  _ongletConso.onClick.AddListener(() => AfficherOnglet(false)); }
        if (_boutonFermer) { _boutonFermer.onClick.RemoveAllListeners();  _boutonFermer.onClick.AddListener(Fermer); }
        AfficherOnglet(true);
        MajArgent();
    }

    protected override void OnDisable()
    {
        if (_ongletOutils) _ongletOutils.onClick.RemoveAllListeners();
        if (_ongletConso)  _ongletConso.onClick.RemoveAllListeners();
        if (_boutonFermer) _boutonFermer.onClick.RemoveAllListeners();
        base.OnDisable();
    }

    private void MajArgent()
    {
        if (_argentLabel) _argentLabel.text = (GameManager.Instance?.Argent ?? 0f).ToString("N0") + " €";
    }

    private bool EstVerrouille(OutilData o)
        => o.UnlocksAfterMission > (GameManager.Instance?.DerniereMissionCompletee ?? 0);

    // ================================================================
    // ONGLETS + LISTES
    // ================================================================

    private void AfficherOnglet(bool outils)
    {
        // Masquer le PANE entier (pas juste le viewport) — sinon le pane caché
        // recouvre/grise/bloque l'autre liste.
        if (_paneOutils) _paneOutils.SetActive(outils);
        if (_paneConso)  _paneConso.SetActive(!outils);
        if (_ficheOutil) _ficheOutil.SetActive(true); // fiche TOUJOURS visible (partagée)

        _selection = null;
        _qtyConso  = 1;
        if (outils) RemplirListeOutils();
        else        RemplirListeConso();
    }

    private void RemplirListeOutils()
    {
        if (_listeOutilsRoot == null || _ligneOutilPrefab == null || _catalogue == null) return;
        Vider(_listeOutilsRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || o.IsConsumable) continue;
            if (_selection == null) _selection = o;
            var captured = o;
            CreerLigne(_listeOutilsRoot, LibelleOutil(o), () => { _selection = captured; RemplirFiche(); });
        }
        RemplirFiche();
    }

    private void RemplirListeConso()
    {
        if (_grilleConsoRoot == null || _ligneOutilPrefab == null || _catalogue == null) return;
        Vider(_grilleConsoRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || !o.IsConsumable) continue;
            if (_selection == null) _selection = o;
            var captured = o;
            CreerLigne(_grilleConsoRoot, LibelleConso(o), () => { _selection = captured; _qtyConso = 1; RemplirFiche(); });
        }
        RemplirFiche();
    }

    private void CreerLigne(Transform root, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = Instantiate(_ligneOutilPrefab, root);
        var txt = go.GetComponentInChildren<TMP_Text>(); if (txt) txt.text = label;
        var btn = go.GetComponentInChildren<Button>();   if (btn) btn.onClick.AddListener(onClick);
    }

    private string LibelleOutil(OutilData o)
    {
        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        if (niv >= 0)          return o.ToolName + "  " + Pips(niv, o.Levels.Length);
        if (EstVerrouille(o))  return o.ToolName + "  🔒 M" + o.UnlocksAfterMission;
        return o.ToolName + "  " + o.PurchasePrice + " €";
    }

    private string LibelleConso(OutilData o)
    {
        if (EstVerrouille(o)) return o.ToolName + "  🔒 M" + o.UnlocksAfterMission;
        int q = _inv != null ? _inv.QuantiteConsommable(o.ToolName) : 0;
        return o.ToolName + "  x" + q;
    }

    // ================================================================
    // FICHE PARTAGÉE
    // ================================================================

    private void RemplirFiche()
    {
        if (_ficheOutil == null || _selection == null) return;
        if (_selection.IsConsumable) RemplirFicheConso(_selection);
        else                          RemplirFicheOutil(_selection);
    }

    private void RemplirFicheOutil(OutilData o)
    {
        if (_ficheStepper) _ficheStepper.SetActive(false);

        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        bool possede = niv >= 0;
        bool verrou  = EstVerrouille(o);

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
            _ficheBouton.onClick.AddListener(() => AcheterOutil(o));
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
            _ficheBouton.onClick.AddListener(() => UpgraderOutil(o));
        }
    }

    private void RemplirFicheConso(OutilData o)
    {
        bool verrou = EstVerrouille(o);
        int owned = _inv != null ? _inv.QuantiteConsommable(o.ToolName) : 0;

        if (_ficheNom)  _ficheNom.text  = o.ToolName;
        if (_ficheDesc) _ficheDesc.text = o.Description;
        if (_ficheNiveaux) _ficheNiveaux.text =
            $"Prix : {o.PurchasePrice} € / unité\nMax emport : {o.MaxCarryPerMission} / mission\nPossédés : {owned}";

        // Stepper quantité (caché si verrouillé)
        if (_ficheStepper) _ficheStepper.SetActive(!verrou);
        _qtyConso = Mathf.Clamp(_qtyConso, 1, 10);
        if (_ficheStepQty) _ficheStepQty.text = _qtyConso.ToString();
        if (_ficheStepMoins)
        {
            _ficheStepMoins.onClick.RemoveAllListeners();
            _ficheStepMoins.interactable = _qtyConso > 1;
            _ficheStepMoins.onClick.AddListener(() => { _qtyConso = Mathf.Max(1, _qtyConso - 1); RemplirFiche(); });
        }
        if (_ficheStepPlus)
        {
            _ficheStepPlus.onClick.RemoveAllListeners();
            _ficheStepPlus.interactable = _qtyConso < 10;
            _ficheStepPlus.onClick.AddListener(() => { _qtyConso = Mathf.Min(10, _qtyConso + 1); RemplirFiche(); });
        }

        if (_ficheBouton == null || _ficheBoutonLabel == null) return;
        _ficheBouton.onClick.RemoveAllListeners();
        if (verrou)
        {
            _ficheBoutonLabel.text = "🔒 Mission " + o.UnlocksAfterMission;
            _ficheBouton.interactable = false;
        }
        else
        {
            int total = o.PurchasePrice * _qtyConso;
            bool ok = (GameManager.Instance?.Argent ?? 0) >= total;
            _ficheBoutonLabel.text = ok ? $"Acheter ×{_qtyConso} · {total} €" : "Fonds insuffisants";
            _ficheBouton.interactable = ok;
            _ficheBouton.onClick.AddListener(() => AcheterConso(o, _qtyConso));
        }
    }

    // ================================================================
    // TRANSACTIONS
    // ================================================================

    private void AcheterOutil(OutilData o)
    {
        if (_inv == null || !(GameManager.Instance?.PeutPayer(o.PurchasePrice) ?? false)) return;
        GameManager.Instance.Debiter(o.PurchasePrice);
        _inv.AjouterOutil(o);
        MajArgent(); RemplirListeOutils();
    }

    private void UpgraderOutil(OutilData o)
    {
        if (_inv == null) return;
        int niv = _inv.NiveauOutil(o);
        if (niv < 0 || niv >= o.Levels.Length - 1) return;
        int cost = o.Levels[niv + 1].UpgradeCost;
        if (!(GameManager.Instance?.PeutPayer(cost) ?? false)) return;
        GameManager.Instance.Debiter(cost);
        _inv.UpgraderOutil(o);
        MajArgent(); RemplirListeOutils();
    }

    private void AcheterConso(OutilData o, int qty)
    {
        if (_inv == null) return;
        int total = o.PurchasePrice * qty;
        if (!(GameManager.Instance?.PeutPayer(total) ?? false)) return;
        GameManager.Instance.Debiter(total);
        _inv.AjouterConsommable(o, qty, o.PurchasePrice);
        _qtyConso = 1;
        MajArgent(); RemplirListeConso();
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private static string Pips(int level, int total)
    { var s = ""; for (int i = 0; i < total; i++) s += i <= level ? "●" : "○"; return s; }

    private static void Vider(Transform t)
    { if (t == null) return; for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }
}
