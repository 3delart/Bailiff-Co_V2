// ============================================================
// ShopPanel.cs — Boutique Hub (2 onglets : Outils / Consommables)
// Master/détail UNIFIÉ : les 2 onglets sont des listes qui alimentent
// la même FicheDetails. Outil → table niveaux + Acheter/Upgrader.
// Conso → prix/max/possédés + stepper quantité (1-10) + Acheter.
// Panel Blocking, contexte Hub. Ouvert par HubPNJ type Boutique.
// ============================================================
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : UIPanel
{
    [Header("Catalogue")]
    [Tooltip("Si coché, charge automatiquement TOUS les ItemData du dossier Resources ci-dessous (ignore la liste manuelle).")]
    [SerializeField] private bool       _autoChargerDepuisResources = true;
    [Tooltip("Sous-dossier sous un dossier 'Resources/' (ex: 'Items'). Tous les .asset ItemData dedans sont vendus.")]
    [SerializeField] private string     _resourcesPath = "Items";
    [Tooltip("Catalogue manuel — utilisé seulement si l'auto-chargement est décoché.")]
    [SerializeField] private ItemData[] _catalogue;

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
    [SerializeField] private Image      _ficheIcon;
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
    private ItemData _selection;   // ToolData OU ConsumableData
    private int _qtyConso = 1;

    protected override void OnEnable()
    {
        base.OnEnable();
        ChargerCatalogue();
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

    /// <summary>Charge le catalogue depuis Resources (si activé). Tri : outils d'abord, puis déblocage/prix.</summary>
    private void ChargerCatalogue()
    {
        if (!_autoChargerDepuisResources) return;

        var all = Resources.LoadAll<ItemData>(_resourcesPath);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning($"[ShopPanel] Aucun ItemData dans Resources/{_resourcesPath}. " +
                             $"Soit place les .asset sous 'Resources/{_resourcesPath}', " +
                             $"soit décoche l'auto-chargement et clique 'Remplir le catalogue' (éditeur).");
            return;
        }

        _catalogue = Trier(all);
    }

    private static ItemData[] Trier(ItemData[] items)
        => items
            .Where(i => i != null)
            .OrderBy(i => i is ConsumableData)     // outils avant consommables
            .ThenBy(i => i.UnlocksAfterMission)
            .ThenBy(i => i.PurchasePrice)
            .ToArray();

#if UNITY_EDITOR
    /// <summary>Éditeur : scanne TOUT le projet et remplit le catalogue (garde ta structure de dossiers).
    /// Clic droit sur le composant → 'Remplir le catalogue (projet)'. À relancer après ajout d'un item.</summary>
    [ContextMenu("Remplir le catalogue (projet)")]
    private void RemplirCatalogueDepuisProjet()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        var all = guids
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
            .ToArray();

        _autoChargerDepuisResources = false;   // on utilise désormais la liste remplie
        _catalogue = Trier(all);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ShopPanel] Catalogue rempli : {_catalogue.Length} item(s).");
    }
#endif

    private bool EstVerrouille(ItemData o)
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
        foreach (var item in _catalogue)
        {
            if (item is not ToolData o) continue;
            if (_selection == null) _selection = o;
            var captured = o;
            CreerLigne(_listeOutilsRoot, o.Icon, LibelleOutil(o), () => { _selection = captured; RemplirFiche(); });
        }
        RemplirFiche();
    }

    private void RemplirListeConso()
    {
        if (_grilleConsoRoot == null || _ligneOutilPrefab == null || _catalogue == null) return;
        Vider(_grilleConsoRoot);
        foreach (var item in _catalogue)
        {
            if (item is not ConsumableData o) continue;
            if (_selection == null) _selection = o;
            var captured = o;
            CreerLigne(_grilleConsoRoot, o.Icon, LibelleConso(o), () => { _selection = captured; _qtyConso = 1; RemplirFiche(); });
        }
        RemplirFiche();
    }

    private void CreerLigne(Transform root, Sprite icon, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = Instantiate(_ligneOutilPrefab, root);

        var txt = go.GetComponentInChildren<TMP_Text>(); if (txt) txt.text = label;

        // Icône : cherche un enfant nommé "Icon" (Image) et y pose le sprite.
        var iconImg = TrouverImageParNom(go.transform, "Icon");
        if (iconImg) { iconImg.sprite = icon; iconImg.enabled = icon != null; }

        var btn = go.GetComponentInChildren<Button>(); if (btn) btn.onClick.AddListener(onClick);
    }

    private static Image TrouverImageParNom(Transform t, string nom)
    {
        foreach (var img in t.GetComponentsInChildren<Image>(true))
            if (img.gameObject.name == nom) return img;
        return null;
    }

    private string LibelleOutil(ToolData o)
    {
        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        if (niv >= 0)          return o.DisplayName + "  " + Pips(niv, o.Levels.Length);
        if (EstVerrouille(o))  return o.DisplayName + "  🔒 M" + o.UnlocksAfterMission;
        return o.DisplayName + "  " + o.PurchasePrice + " €";
    }

    private string LibelleConso(ConsumableData o)
    {
        if (EstVerrouille(o)) return o.DisplayName + "  🔒 M" + o.UnlocksAfterMission;
        int q = _inv != null ? _inv.QuantiteConsommable(o.Id) : 0;
        return o.DisplayName + "  x" + q;
    }

    // ================================================================
    // FICHE PARTAGÉE
    // ================================================================

    private void RemplirFiche()
    {
        if (_ficheOutil == null || _selection == null) return;
        if (_selection is ConsumableData c) RemplirFicheConso(c);
        else if (_selection is ToolData t)  RemplirFicheOutil(t);
    }

    private void RemplirFicheOutil(ToolData o)
    {
        if (_ficheStepper) _ficheStepper.SetActive(false);

        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        bool possede = niv >= 0;
        bool verrou  = EstVerrouille(o);

        if (_ficheIcon) { _ficheIcon.sprite = o.Icon; _ficheIcon.enabled = o.Icon != null; }
        if (_ficheNom)  _ficheNom.text  = o.DisplayName + (possede ? "  " + Pips(niv, o.Levels.Length) : "");
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

    private void RemplirFicheConso(ConsumableData o)
    {
        bool verrou = EstVerrouille(o);
        int owned = _inv != null ? _inv.QuantiteConsommable(o.Id) : 0;

        if (_ficheIcon) { _ficheIcon.sprite = o.Icon; _ficheIcon.enabled = o.Icon != null; }
        if (_ficheNom)  _ficheNom.text  = o.DisplayName;
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

    private void AcheterOutil(ToolData o)
    {
        if (_inv == null || !(GameManager.Instance?.PeutPayer(o.PurchasePrice) ?? false)) return;
        GameManager.Instance.Debiter(o.PurchasePrice);
        _inv.AjouterOutil(o);
        MajArgent(); RemplirListeOutils();
    }

    private void UpgraderOutil(ToolData o)
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

    private void AcheterConso(ConsumableData o, int qty)
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
