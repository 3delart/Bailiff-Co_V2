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
    [SerializeField] private OutilDef _badgeOfficiel;
    [SerializeField] private OutilDef _telephoneHuissier;

    // Outils achetés + leur niveau (0=niv1, 1=niv2, 2=niv3)
    private readonly Dictionary<OutilDef, int> _outils = new();

    // Consommables : type → quantité
    private readonly Dictionary<string, int> _consommables = new();

    private void Start()
    {
        if (_badgeOfficiel     != null) _outils[_badgeOfficiel]     = 0;
        if (_telephoneHuissier != null) _outils[_telephoneHuissier] = 0;
    }

    // ----------------------------------------------------------------
    // OUTILS
    // ----------------------------------------------------------------

    public bool PossedePiedDeBiche()
    {
        foreach (var kv in _outils)
            if (kv.Key.NomOutil.Contains("Pied-de-biche")) return true;
        return false;
    }

    public bool PossedeOutil(string nomOutil)
    {
        foreach (var kv in _outils)
            if (kv.Key.NomOutil == nomOutil) return true;
        return false;
    }

    public int NiveauOutil(OutilDef def)
    {
        return _outils.TryGetValue(def, out int niv) ? niv : -1;
    }

    public void AjouterOutil(OutilDef def)
    {
        if (!_outils.ContainsKey(def))
            _outils[def] = 0;
    }

    public void UpgraderOutil(OutilDef def)
    {
        if (_outils.TryGetValue(def, out int niv) && niv < 2)
            _outils[def] = niv + 1;
    }

    // ----------------------------------------------------------------
    // CONSOMMABLES
    // ----------------------------------------------------------------

    public void AjouterConsommable(string type, int quantite = 1)
    {
        _consommables.TryGetValue(type, out int actuel);
        _consommables[type] = actuel + quantite;
    }

    public bool UtiliserConsommable(string type)
    {
        if (_consommables.TryGetValue(type, out int q) && q > 0)
        {
            _consommables[type] = q - 1;
            return true;
        }
        return false;
    }

    public int QuantiteConsommable(string type)
    {
        _consommables.TryGetValue(type, out int q);
        return q;
    }

    // ----------------------------------------------------------------
    // DONNÉES (pour la boutique et l'UI)
    // ----------------------------------------------------------------

    public IReadOnlyDictionary<OutilDef, int> Outils       => _outils;
    public IReadOnlyDictionary<string, int>   Consommables => _consommables;
}
