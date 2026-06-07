// ============================================================
// InventaireSystem.cs — Bailiff & Co
// Outils permanents + consommables du joueur.
// Totalement indépendant de la mission en cours.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class InventaireSystem : MonoBehaviour
{
    [Header("Outils de départ (donnés au joueur)")]
    [SerializeField] private OutilData _badgeOfficiel;        // ← CORRECTION
    [SerializeField] private OutilData _telephoneHuissier;    // ← CORRECTION

    // Outils achetés + leur niveau (0=niv1, 1=niv2, 2=niv3)
    private readonly Dictionary<OutilData, int> _outils = new();  // ← CORRECTION

    // Consommables : type → quantité
    private readonly Dictionary<string, int>   _consommables     = new();
    // Prix unitaire par type (enregistré à l'achat)
    private readonly Dictionary<string, float> _consommablesPrix = new();

    // Loadout équipé (ce qui va dans la roue en mission)
    public const int MAX_OUTILS_EQUIPES = 3;
    public const int MAX_CONSOS_EQUIPES = 3;
    private readonly List<OutilData>   _outilsEquipes = new();
    private readonly List<ConsoEquipe> _consosEquipes = new();
    // Registre type→OutilData pour les consommables (retrouver MaxCarryPerMission/icône)
    private readonly Dictionary<string, OutilData> _consoDefs = new();

    private void Start()
    {
        if (_badgeOfficiel     != null) _outils[_badgeOfficiel]     = 0;
        if (_telephoneHuissier != null) _outils[_telephoneHuissier] = 0;

        // Auto-équipe les outils possédés (≤3) pour que la roue ne soit pas vide en slice.
        foreach (var kv in _outils)
        {
            if (_outilsEquipes.Count >= MAX_OUTILS_EQUIPES) break;
            _outilsEquipes.Add(kv.Key);
        }
    }

    // ----------------------------------------------------------------
    // OUTILS
    // ----------------------------------------------------------------

    public bool PossedePiedDeBiche()
    {
        foreach (var kv in _outils)
            if (kv.Key.ToolName.Contains("Pied-de-biche")) return true;  // ← CORRECTION (NomOutil → ToolName)
        return false;
    }

    public bool PossedeOutil(string nomOutil)
    {
        foreach (var kv in _outils)
            if (kv.Key.ToolName == nomOutil) return true;  // ← CORRECTION
        return false;
    }

    public int NiveauOutil(OutilData def)  // ← CORRECTION
    {
        return _outils.TryGetValue(def, out int niv) ? niv : -1;
    }

    public void AjouterOutil(OutilData def)  // ← CORRECTION
    {
        if (!_outils.ContainsKey(def))
            _outils[def] = 0;
    }

    public void UpgraderOutil(OutilData def)  // ← CORRECTION
    {
        if (_outils.TryGetValue(def, out int niv) && niv < 2)
            _outils[def] = niv + 1;
    }

    // ----------------------------------------------------------------
    // CONSOMMABLES
    // ----------------------------------------------------------------

    public void AjouterConsommable(string type, int quantite = 1, float prixUnitaire = 0f)
    {
        _consommables.TryGetValue(type, out int actuel);
        _consommables[type] = actuel + quantite;
        if (prixUnitaire > 0f)
            _consommablesPrix[type] = prixUnitaire;
    }

    public bool UtiliserConsommable(string type)
    {
        if (_consommables.TryGetValue(type, out int q) && q > 0)
        {
            _consommables[type] = q - 1;
            _consommablesPrix.TryGetValue(type, out float prix);
            EventBus<OnConsommableUsed>.Raise(new OnConsommableUsed
            {
                Nom          = type,
                CoutUnitaire = prix,
                Quantite     = 1
            });
            return true;
        }
        return false;
    }

    public int QuantiteConsommable(string type)
    {
        _consommables.TryGetValue(type, out int q);
        return q;
    }

    /// <summary>Ajoute un consommable depuis sa définition (enregistre la def pour le loadout).</summary>
    public void AjouterConsommable(OutilData def, int quantite, float prixUnitaire = 0f)
    {
        if (def == null) return;
        _consoDefs[def.ToolName] = def;
        AjouterConsommable(def.ToolName, quantite, prixUnitaire);
    }

    public OutilData ConsoDef(string type) => _consoDefs.TryGetValue(type, out var d) ? d : null;

    // ----------------------------------------------------------------
    // LOADOUT ÉQUIPÉ
    // ----------------------------------------------------------------

    public IReadOnlyList<OutilData> OutilsEquipes => _outilsEquipes;

    public bool OutilEstEquipe(OutilData def) => def != null && _outilsEquipes.Contains(def);

    public bool EquiperOutil(OutilData def)
    {
        if (def == null || !_outils.ContainsKey(def)) return false; // doit être possédé
        if (_outilsEquipes.Contains(def)) return true;
        if (_outilsEquipes.Count >= MAX_OUTILS_EQUIPES) return false;
        _outilsEquipes.Add(def);
        return true;
    }

    public void DesequiperOutil(OutilData def) => _outilsEquipes.Remove(def);

    public IReadOnlyList<ConsoEquipe> ConsosEquipes => _consosEquipes;

    public int MaxCarryConso(string type)
    {
        if (_consoDefs.TryGetValue(type, out var def) && def.MaxCarryPerMission > 0)
            return def.MaxCarryPerMission;
        return 99;
    }

    public bool ConsoEstEquipe(string type) => _consosEquipes.Exists(c => c.Type == type);

    public bool EquiperConso(string type, int quantite)
    {
        if (string.IsNullOrEmpty(type)) return false;
        if (!_consommables.TryGetValue(type, out int stock) || stock <= 0) return false;

        int max = Mathf.Min(MaxCarryConso(type), stock);
        int q   = Mathf.Clamp(quantite, 1, max);

        int idx = _consosEquipes.FindIndex(c => c.Type == type);
        if (idx >= 0) { _consosEquipes[idx] = new ConsoEquipe(type, q); return true; }
        if (_consosEquipes.Count >= MAX_CONSOS_EQUIPES) return false;
        _consosEquipes.Add(new ConsoEquipe(type, q));
        return true;
    }

    public void DesequiperConso(string type) => _consosEquipes.RemoveAll(c => c.Type == type);

    // ----------------------------------------------------------------
    // DONNÉES (pour la boutique et l'UI)
    // ----------------------------------------------------------------

    public IReadOnlyDictionary<OutilData, int> Outils       => _outils;  // ← CORRECTION
    public IReadOnlyDictionary<string, int>    Consommables => _consommables;
}

[System.Serializable]
public struct ConsoEquipe
{
    public string Type;
    public int    Quantite;
    public ConsoEquipe(string type, int quantite) { Type = type; Quantite = quantite; }
}