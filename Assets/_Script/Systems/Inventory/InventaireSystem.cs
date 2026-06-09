// ============================================================
// InventaireSystem.cs — Bailiff & Co
// Outils permanents + consommables du joueur.
// Totalement indépendant de la mission en cours.
//
// IDENTITÉ : outils keyés par ref `ToolData` (stable) ; consommables
// keyés par `Id` stable (plus de matching par nom d'affichage).
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class InventaireSystem : MonoBehaviour
{
    [Header("Outils de départ (donnés au joueur)")]
    [SerializeField] private ToolData _badgeOfficiel;
    [SerializeField] private ToolData _telephoneHuissier;

    // Outils possédés + leur niveau (0=niv1, 1=niv2, 2=niv3)
    private readonly Dictionary<ToolData, int> _outils = new();

    // Consommables : Id → quantité
    private readonly Dictionary<string, int>   _consommables     = new();
    // Prix unitaire par Id (enregistré à l'achat)
    private readonly Dictionary<string, float> _consommablesPrix = new();

    // Loadout équipé (ce qui va dans la roue en mission)
    public const int MAX_OUTILS_EQUIPES = 3;
    public const int MAX_CONSOS_EQUIPES = 3;
    private readonly ToolData[]     _outilsEquipes = new ToolData[MAX_OUTILS_EQUIPES];     // null = slot vide
    private readonly ConsoEquipe?[] _consosEquipes = new ConsoEquipe?[MAX_CONSOS_EQUIPES];  // null = slot vide
    // Registre Id→ConsumableData pour les consommables (retrouver MaxCarryPerMission/icône/def)
    private readonly Dictionary<string, ConsumableData> _consoDefs = new();

    private void Start()
    {
        if (_badgeOfficiel     != null) _outils[_badgeOfficiel]     = 0;
        if (_telephoneHuissier != null) _outils[_telephoneHuissier] = 0;

        // Auto-équipe les outils possédés aux premiers slots pour que la roue ne soit pas vide en slice.
        int slotAuto = 0;
        foreach (var kv in _outils)
        {
            if (slotAuto >= MAX_OUTILS_EQUIPES) break;
            _outilsEquipes[slotAuto++] = kv.Key;
        }
    }

    // ----------------------------------------------------------------
    // OUTILS
    // ----------------------------------------------------------------

    public bool PossedeOutil(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var kv in _outils)
            if (kv.Key != null && kv.Key.Id == id) return true;
        return false;
    }

    public int NiveauOutil(ToolData def)
    {
        return def != null && _outils.TryGetValue(def, out int niv) ? niv : -1;
    }

    public void AjouterOutil(ToolData def)
    {
        if (def == null) return;
        if (!_outils.ContainsKey(def))
            _outils[def] = 0;
    }

    public void UpgraderOutil(ToolData def)
    {
        if (def == null) return;
        if (_outils.TryGetValue(def, out int niv) && niv < 2)
            _outils[def] = niv + 1;
    }

    // ----------------------------------------------------------------
    // CONSOMMABLES (keyés par Id)
    // ----------------------------------------------------------------

    public void AjouterConsommable(string id, int quantite = 1, float prixUnitaire = 0f)
    {
        if (string.IsNullOrEmpty(id)) return;
        _consommables.TryGetValue(id, out int actuel);
        _consommables[id] = actuel + quantite;
        if (prixUnitaire > 0f)
            _consommablesPrix[id] = prixUnitaire;
    }

    /// <summary>Ajoute un consommable depuis sa définition (enregistre la def pour le loadout).</summary>
    public void AjouterConsommable(ConsumableData def, int quantite, float prixUnitaire = 0f)
    {
        if (def == null) return;
        _consoDefs[def.Id] = def;
        AjouterConsommable(def.Id, quantite, prixUnitaire);
    }

    public bool UtiliserConsommable(string id)
    {
        if (!string.IsNullOrEmpty(id) && _consommables.TryGetValue(id, out int q) && q > 0)
        {
            _consommables[id] = q - 1;
            _consommablesPrix.TryGetValue(id, out float prix);
            EventBus<OnConsommableUsed>.Raise(new OnConsommableUsed
            {
                Nom          = id,
                CoutUnitaire = prix,
                Quantite     = 1
            });
            return true;
        }
        return false;
    }

    public int QuantiteConsommable(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        _consommables.TryGetValue(id, out int q);
        return q;
    }

    public ConsumableData ConsoDef(string id)
        => !string.IsNullOrEmpty(id) && _consoDefs.TryGetValue(id, out var d) ? d : null;

    // ----------------------------------------------------------------
    // LOADOUT ÉQUIPÉ
    // ----------------------------------------------------------------

    // Positionnel : index = slot, null = vide.
    public IReadOnlyList<ToolData>     OutilsEquipes => _outilsEquipes;
    public IReadOnlyList<ConsoEquipe?> ConsosEquipes => _consosEquipes;

    public ToolData     OutilAuSlot(int i) => (i >= 0 && i < _outilsEquipes.Length) ? _outilsEquipes[i] : null;
    public ConsoEquipe? ConsoAuSlot(int i) => (i >= 0 && i < _consosEquipes.Length) ? _consosEquipes[i] : null;

    public bool OutilEstEquipe(ToolData def)
    {
        if (def == null) return false;
        foreach (var o in _outilsEquipes) if (o == def) return true;
        return false;
    }

    public bool ConsoEstEquipe(string id)
    {
        foreach (var c in _consosEquipes) if (c.HasValue && c.Value.Type == id) return true;
        return false;
    }

    /// <summary>Équipe un outil au slot donné (déplace s'il était dans un autre slot).</summary>
    public bool EquiperOutilSlot(ToolData def, int slot)
    {
        if (def == null || !_outils.ContainsKey(def)) return false;      // doit être possédé
        if (slot < 0 || slot >= _outilsEquipes.Length) return false;
        for (int i = 0; i < _outilsEquipes.Length; i++)                  // retire d'un autre slot (déplacement)
            if (_outilsEquipes[i] == def) _outilsEquipes[i] = null;
        _outilsEquipes[slot] = def;
        RaiseLoadoutChanged();
        return true;
    }

    public void DesequiperOutilSlot(int slot)
    {
        if (slot >= 0 && slot < _outilsEquipes.Length) { _outilsEquipes[slot] = null; RaiseLoadoutChanged(); }
    }

    public void DesequiperOutil(ToolData def)
    {
        for (int i = 0; i < _outilsEquipes.Length; i++)
            if (_outilsEquipes[i] == def) _outilsEquipes[i] = null;
        RaiseLoadoutChanged();
    }

    public int MaxCarryConso(string id)
    {
        if (_consoDefs.TryGetValue(id, out var def) && def.MaxCarryPerMission > 0)
            return def.MaxCarryPerMission;
        return 99;
    }

    /// <summary>Équipe un consommable au slot donné (déplace s'il était ailleurs ; clamp qté).</summary>
    public bool EquiperConsoSlot(string id, int quantite, int slot)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (slot < 0 || slot >= _consosEquipes.Length) return false;
        if (!_consommables.TryGetValue(id, out int stock) || stock <= 0) return false;

        int max = Mathf.Min(MaxCarryConso(id), stock);
        int q   = Mathf.Clamp(quantite, 1, max);

        for (int i = 0; i < _consosEquipes.Length; i++)                  // retire le type d'un autre slot
            if (_consosEquipes[i].HasValue && _consosEquipes[i].Value.Type == id) _consosEquipes[i] = null;

        _consosEquipes[slot] = new ConsoEquipe(id, q);
        RaiseLoadoutChanged();
        return true;
    }

    public void DesequiperConsoSlot(int slot)
    {
        if (slot >= 0 && slot < _consosEquipes.Length) { _consosEquipes[slot] = null; RaiseLoadoutChanged(); }
    }

    public void DesequiperConso(string id)
    {
        for (int i = 0; i < _consosEquipes.Length; i++)
            if (_consosEquipes[i].HasValue && _consosEquipes[i].Value.Type == id) _consosEquipes[i] = null;
        RaiseLoadoutChanged();
    }

    private static void RaiseLoadoutChanged()
        => EventBus<OnLoadoutChanged>.Raise(new OnLoadoutChanged());

    // ----------------------------------------------------------------
    // DONNÉES (pour la boutique et l'UI)
    // ----------------------------------------------------------------

    public IReadOnlyDictionary<ToolData, int> Outils       => _outils;
    public IReadOnlyDictionary<string, int>   Consommables => _consommables;
}

[System.Serializable]
public struct ConsoEquipe
{
    public string Type;     // = ConsumableData.Id
    public int    Quantite;
    public ConsoEquipe(string type, int quantite) { Type = type; Quantite = quantite; }
}
