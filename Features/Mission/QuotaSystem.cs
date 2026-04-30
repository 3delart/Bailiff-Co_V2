// ============================================================
// QuotaSystem.cs — Bailiff & Co  V2
// Calcule la valeur totale chargée dans le véhicule.
// Ne connaît pas le proprio. Ne connaît pas le joueur.
// Émet OnQuotaChanged, OnQuotaReached, OnThresholdReached.
//
// CHANGEMENTS V2 :
//   - OnQuotaChanged → identique
//   - OnQuotaAtteint → OnQuotaReached
//   - OnSeuilAtteint → OnThresholdReached
//   - OnMissionDemarree → OnMissionStarted
//   - OnObjetCharge → OnObjectLoaded
//   - OnProprietaireRecupereObjet → OnOwnerRetrievedObject
//   - ObjetDef → ObjetData
//   - Suppression du cache FindObjectOfType
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class QuotaSystem : MonoBehaviour
{
    // ================================================================
    // ÉTAT
    // ================================================================

    [Header("État (lecture seule)")]
    [SerializeField] private float _totalValue   = 0f;
    [SerializeField] private float _targetValue  = 0f;
    [SerializeField] private bool  _quotaReached = false;

    // Seuils à surveiller (selon GDD : 20% = proprio peut sortir)
    private static readonly float[] MONITORED_THRESHOLDS = { 0.20f, 0.50f, 0.60f, 0.80f, 1.00f };
    private readonly HashSet<float> _triggeredThresholds = new();

    // Détail par objet pour l'écran de résultat
    private readonly List<(ObjetData obj, float value)> _loadedObjects = new();

    // ================================================================
    // PROPERTIES
    // ================================================================

    public float TotalValue   => _totalValue;
    public float TargetValue  => _targetValue;
    public float Percentage   => _targetValue > 0 ? _totalValue / _targetValue : 0f;
    public bool  QuotaReached => _quotaReached;
    public IReadOnlyList<(ObjetData, float)> LoadedObjects => _loadedObjects;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
        EventBus<OnObjectLoaded>.Subscribe(OnObjectLoaded);
        EventBus<OnOwnerRetrievedObject>.Subscribe(OnOwnerRetrievedObject);
    }

    private void OnDisable()
    {
        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
        EventBus<OnObjectLoaded>.Unsubscribe(OnObjectLoaded);
        EventBus<OnOwnerRetrievedObject>.Unsubscribe(OnOwnerRetrievedObject);
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnMissionStarted(OnMissionStarted e)
    {
        _totalValue = 0f;
        _quotaReached = false;
        _loadedObjects.Clear();
        _triggeredThresholds.Clear();

        // Calcule la valeur cible (quota minimum)
        if (e.Mission.MinimumQuotaValue > 0)
        {
            _targetValue = e.Mission.MinimumQuotaValue;
        }
        else
        {
            // Fallback : 50% de la valeur max possible des objets
            float maxPossible = 0f;
            foreach (var entry in e.Mission.SeizableObjects)
            {
                if (entry.ObjectData == null) continue;

                float valueMax = entry.ValueMaxOverride > 0 
                    ? entry.ValueMaxOverride 
                    : entry.ObjectData.ValueMax;
                
                maxPossible += valueMax * entry.MaxCount;
            }
            _targetValue = maxPossible * 0.5f;
        }

        PublishChange();
        Debug.Log($"[QuotaSystem] Quota cible : {_targetValue:N0} €");
    }

    private void OnObjectLoaded(OnObjectLoaded e)
    {
        _totalValue += e.Value;
        _loadedObjects.Add((e.Object, e.Value));

        PublishChange();
        CheckThresholds();

        if (!_quotaReached && _totalValue >= _targetValue)
        {
            _quotaReached = true;
            EventBus<OnQuotaReached>.Raise(new OnQuotaReached());
            Debug.Log($"[QuotaSystem] Quota atteint : {_totalValue:N0} € / {_targetValue:N0} €");
        }
    }

    private void OnOwnerRetrievedObject(OnOwnerRetrievedObject e)
    {
        // Le proprio a vidé un objet du coffre — on recalcule
        bool found = false;
        
        for (int i = _loadedObjects.Count - 1; i >= 0; i--)
        {
            if (_loadedObjects[i].obj == e.Object)
            {
                _totalValue -= _loadedObjects[i].value;
                _loadedObjects.RemoveAt(i);
                found = true;
                break;
            }
        }

        if (found)
        {
            _totalValue = Mathf.Max(0f, _totalValue);
            _quotaReached = _totalValue >= _targetValue;
            PublishChange();
            
            Debug.Log($"[QuotaSystem] Objet récupéré par proprio — nouveau total : {_totalValue:N0} €");
        }
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void PublishChange()
    {
        EventBus<OnQuotaChanged>.Raise(new OnQuotaChanged
        {
            TotalValue = _totalValue,
            TargetValue = _targetValue,
            Percentage = Percentage
        });
    }

    private void CheckThresholds()
    {
        float pct = Percentage;
        
        foreach (float threshold in MONITORED_THRESHOLDS)
        {
            if (pct >= threshold && !_triggeredThresholds.Contains(threshold))
            {
                _triggeredThresholds.Add(threshold);
                EventBus<OnThresholdReached>.Raise(new OnThresholdReached 
                { 
                    Percentage = threshold 
                });
                
                Debug.Log($"[QuotaSystem] Seuil atteint : {threshold * 100:F0}%");
            }
        }
    }
}