// ============================================================
// MissionTracker.cs — Bailiff & Co  V2
// Tracke TOUS les événements de la mission pour le bulletin.
// Écoute : OnObjectLoaded, OnObjectDamaged, OnConsommableUsed,
// OnOwnerRetrievedObject, et futures OnVehicleDamaged, OnInfraction.
//
// MissionSystem lit ces données au EndMission().
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class MissionTracker : MonoBehaviour
{
    // ================================================================
    // DONNÉES TRACKÉES
    // ================================================================

    /// <summary>Objets chargés dans le coffre (qty + valeur réelle).</summary>
    private List<(ObjetData obj, int qty, float valeurTotale)> _objetsCharges = new();

    /// <summary>Objets endommagés (qty + valeur perdue).</summary>
    private List<(string nom, int qty, float valeurPerdue)> _objetsEndommages = new();

    /// <summary>Objets volés par proprio/voisins (qty + valeur).</summary>
    private List<(ObjetData obj, int qty, float valeurTotale)> _objetsVoles = new();

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
        // Reset au démarrage de mission
        _objetsCharges.Clear();
        _objetsEndommages.Clear();
        _objetsVoles.Clear();
        _consommablesUtilises.Clear();
        _totalDegatsVehicule = 0f;
        _totalAmendesInfractions = 0f;

        Debug.Log("[MissionTracker] Tracking démarré");
    }

    private void OnObjectLoaded(OnObjectLoaded e)
    {
        // Cherche si on a déjà cet objet dans la liste
        for (int i = 0; i < _objetsCharges.Count; i++)
        {
            if (_objetsCharges[i].obj == e.Object)
            {
                var entry = _objetsCharges[i];
                _objetsCharges[i] = (entry.obj, entry.qty + 1, entry.valeurTotale + e.Value);
                Debug.Log($"[MissionTracker] +1 {e.Object.ObjectName} (total: {_objetsCharges[i].qty})");
                return;
            }
        }

        // Nouvel objet
        _objetsCharges.Add((e.Object, 1, e.Value));
        Debug.Log($"[MissionTracker] Premier {e.Object.ObjectName} chargé");
    }

    private void OnObjectDamaged(OnObjectDamaged e)
    {
        // Un objet a perdu de la valeur
        string nom = e.Object != null ? e.Object.ObjectName : "Objet inconnu";

        for (int i = 0; i < _objetsEndommages.Count; i++)
        {
            if (_objetsEndommages[i].nom == nom)
            {
                var entry = _objetsEndommages[i];
                _objetsEndommages[i] = (entry.nom, entry.qty + 1, entry.valeurPerdue + e.ValueLost);
                Debug.Log($"[MissionTracker] Objet endommagé: {nom} (-{e.ValueLost:N0} €)");
                return;
            }
        }

        _objetsEndommages.Add((nom, 1, e.ValueLost));
        Debug.Log($"[MissionTracker] Premier {nom} endommagé (-{e.ValueLost:N0} €)");
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
        Debug.Log($"[MissionTracker] Consommable utilisé: {e.Nom} x{e.Quantite}");
    }

    private void OnOwnerRetrievedObject(OnOwnerRetrievedObject e)
    {
        // Le proprio/voisin a volé un objet du coffre
        string nom = e.Object != null ? e.Object.ObjectName : "Objet inconnu";

        for (int i = 0; i < _objetsVoles.Count; i++)
        {
            if (_objetsVoles[i].obj == e.Object)
            {
                var entry = _objetsVoles[i];
                _objetsVoles[i] = (entry.obj, entry.qty + 1, entry.valeurTotale + e.Value);
                Debug.Log($"[MissionTracker] Propriétaire a volé: +1 {nom}");
                return;
            }
        }

        _objetsVoles.Add((e.Object, 1, e.Value));
        Debug.Log($"[MissionTracker] Propriétaire a volé: {nom}");
    }

    // ================================================================
    // API PUBLIQUE — lecture par MissionSystem
    // ================================================================

    public List<(ObjetData obj, int qty, float valeurTotale)> GetObjetsCharges()
        => new List<(ObjetData, int, float)>(_objetsCharges);

    public List<(string nom, int qty, float valeurPerdue)> GetObjetsEndommages()
        => new List<(string, int, float)>(_objetsEndommages);

    public List<(ObjetData obj, int qty, float valeurTotale)> GetObjetsVoles()
        => new List<(ObjetData, int, float)>(_objetsVoles);

    public List<(string nom, int qty, float coutUnitaire)> GetConsommablesUtilises()
        => new List<(string, int, float)>(_consommablesUtilises);

    public float GetTotalDegatsVehicule() => _totalDegatsVehicule;
    public float GetTotalAmendesInfractions() => _totalAmendesInfractions;

    // ================================================================
    // API PUBLIQUE — ajout manuel (pour propriétaire/voisins/infra)
    // ================================================================

    public void AddDegatsVehicule(float montant)
    {
        _totalDegatsVehicule += montant;
        Debug.Log($"[MissionTracker] Dégâts véhicule: +{montant:N0} € (total: {_totalDegatsVehicule:N0} €)");
    }

    public void AddInfraction(string description, float amende)
    {
        _totalAmendesInfractions += amende;
        Debug.Log($"[MissionTracker] Infraction: {description} (-{amende:N0} €)");
    }
}