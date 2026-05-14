// ============================================================
// MissionTracker.cs — Bailiff & Co  V2 (CORRIGÉ)
// Tracke CHAQUE INSTANCE individuellement, pas par asset!
//
// CORRECTION CLÉE :
//   - Avant: regroupait par ObjetData asset (fusion!)
//   - Maintenant: chaque objet = separate entry (instance)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class MissionTracker : MonoBehaviour
{
    // ================================================================
    // DONNÉES TRACKÉES — PAR INSTANCE
    // ================================================================

    /// <summary>Chaque objet chargé individuellement avec ses deux prix (base + actuel) ET ses dégâts</summary>
    private List<(ObjetData obj, int instanceId, float basePrice, float currentPrice, float damagePercent, bool isBroken)> _objetsCharges = new();

    /// <summary>Chaque objet endommagé individuellement.</summary>
    private List<(ObjetData obj, float valeurBefore, float valeurPerdue)> _objetsEndommages = new();

    /// <summary>Objets volés par proprio/voisins.</summary>
    private List<(ObjetData obj, float valeur)> _objetsVoles = new();

    /// <summary>Consommables utilisés (nom + qty + coût unitaire).</summary>
    private List<(string nom, int qty, float coutUnitaire)> _consommablesUtilises = new();

    /// <summary>Dégâts véhicule accumulés.</summary>
    private float _totalDegatsVehicule = 0f;

    /// <summary>Amendes infractions accumulées.</summary>
    private float _totalAmendesInfractions = 0f;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnObjectLoaded>.Subscribe(OnObjectLoaded);
        EventBus<OnObjectDamaged>.Subscribe(OnObjectDamaged);
        EventBus<OnConsommableUsed>.Subscribe(OnConsommableUsed);
        EventBus<OnOwnerRetrievedObject>.Subscribe(OnOwnerRetrievedObject);
        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
    }

    private void OnDisable()
    {
        EventBus<OnObjectLoaded>.Unsubscribe(OnObjectLoaded);
        EventBus<OnObjectDamaged>.Unsubscribe(OnObjectDamaged);
        EventBus<OnConsommableUsed>.Unsubscribe(OnConsommableUsed);
        EventBus<OnOwnerRetrievedObject>.Unsubscribe(OnOwnerRetrievedObject);
        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnMissionStarted(OnMissionStarted e)
    {
        _objetsCharges.Clear();
        _objetsEndommages.Clear();
        _objetsVoles.Clear();
        _consommablesUtilises.Clear();
        _totalDegatsVehicule = 0f;
        _totalAmendesInfractions = 0f;

        Debug.Log("[MissionTracker] Tracking démarré");
    }

    /// <summary>✅ CHAQUE objet = une entrée séparée avec BasePrice, CurrentPrice ET DamagePercent</summary>
    private void OnObjectLoaded(OnObjectLoaded e)
    {
        _objetsCharges.Add((e.Object, e.InstanceId, e.BasePrice, e.CurrentPrice, e.DamagePercent, e.IsBroken));
        Debug.Log($"[MissionTracker] Objet chargé: {e.Object.ObjectName} [#{e.InstanceId}] | Base: {e.BasePrice:N0}€ | Actuel: {e.CurrentPrice:N0}€ | Dégâts: {e.DamagePercent:F1}% | Cassé: {e.IsBroken}");
    }

    /// <summary>✅ CHAQUE objet cassé = une entrée séparée</summary>
    private void OnObjectDamaged(OnObjectDamaged e)
    {
        string nom = e.Object != null ? e.Object.ObjectName : "Objet inconnu";
        _objetsEndommages.Add((e.Object, e.ValueBefore, e.ValueLost));
        Debug.Log($"[MissionTracker] Objet endommagé: {nom} | Avant: {e.ValueBefore:N0}€ | Perdu: {e.ValueLost:N0}€");
    }

    private void OnConsommableUsed(OnConsommableUsed e)
    {
        for (int i = 0; i < _consommablesUtilises.Count; i++)
        {
            if (_consommablesUtilises[i].nom == e.Nom)
            {
                var entry = _consommablesUtilises[i];
                _consommablesUtilises[i] = (entry.nom, entry.qty + e.Quantite, entry.coutUnitaire);
                Debug.Log($"[MissionTracker] +{e.Quantite} {e.Nom}");
                return;
            }
        }

        _consommablesUtilises.Add((e.Nom, e.Quantite, e.CoutUnitaire));
        Debug.Log($"[MissionTracker] Consommable utilisé: {e.Nom} x{e.Quantite} @ {e.CoutUnitaire:N0}€");
    }

    private void OnOwnerRetrievedObject(OnOwnerRetrievedObject e)
    {
        string nom = e.Object != null ? e.Object.ObjectName : "Objet inconnu";
        _objetsVoles.Add((e.Object, e.Value));
        Debug.Log($"[MissionTracker] Proprio a volé: {nom} ({e.Value:N0}€)");
    }

    // ================================================================
    // API PUBLIQUE — lecture par MissionSystem
    // ================================================================

    /// <summary>Liste de CHAQUE objet chargé individuellement avec BasePrice, CurrentPrice, DamagePercent et InstanceId</summary>
    public List<(ObjetData obj, int instanceId, float basePrice, float currentPrice, float damagePercent, bool isBroken)> GetObjetsCharges()
        => new List<(ObjetData, int, float, float, float, bool)>(_objetsCharges);

    /// <summary>Liste de CHAQUE objet endommagé individuellement</summary>
    public List<(ObjetData obj, float valeurBefore, float valeurPerdue)> GetObjetsEndommages()
        => new List<(ObjetData, float, float)>(_objetsEndommages);

    public List<(ObjetData obj, float valeur)> GetObjetsVoles()
        => new List<(ObjetData, float)>(_objetsVoles);

    public List<(string nom, int qty, float coutUnitaire)> GetConsommablesUtilises()
        => new List<(string, int, float)>(_consommablesUtilises);

    public float GetTotalDegatsVehicule() => _totalDegatsVehicule;
    public float GetTotalAmendesInfractions() => _totalAmendesInfractions;

    // ================================================================
    // API PUBLIQUE — ajout manuel
    // ================================================================

    public void AddDegatsVehicule(float montant)
    {
        _totalDegatsVehicule += montant;
        Debug.Log($"[MissionTracker] Dégâts véhicule: +{montant:N0}€ (total: {_totalDegatsVehicule:N0}€)");
    }

    public void AddInfraction(string description, float amende)
    {
        _totalAmendesInfractions += amende;
        Debug.Log($"[MissionTracker] Infraction: {description} (-{amende:N0}€)");
    }
}