// ============================================================
// MissionSystem.cs — Bailiff & Co  V2
// Orchestrateur : gère le seed, démarre la mission, écoute
// OnQuotaAtteint, collecte les stats, calcule les étoiles.
// C'est le seul système qui connaît la MissionData en cours.
//
// CHANGEMENTS V2 :
//   - MissionDef → MissionData
//   - Suppression de RetourHubCoroutine (géré par SceneLoader)
//   - FindObjectOfType supprimés → injection [SerializeField]
//   - OnMissionDemarree → OnMissionStarted
//   - OnMissionTerminee → OnMissionEnded
//   - OnQuotaAtteint → OnQuotaReached
//   - OnPiegeDeclenche → OnTrapTriggered
//   - OnObjetEndommage → OnObjectDamaged
//   - OnTimerUrgenceDéclenche → OnUrgencyTimerStarted
//   - Spawn des objets délégué à MissionBuilder
// ============================================================
using System.Collections;
using UnityEngine;

public class MissionSystem : MonoBehaviour
{
    // ================================================================
    // RÉFÉRENCES INJECTÉES
    // ================================================================

    [Header("Références injectées")]
    [Tooltip("Système de quota — injecté dans l'Inspector")]
    [SerializeField] private QuotaSystem _quotaSystem;

    [Tooltip("Système de paranoïa — injecté dans l'Inspector")]
    [SerializeField] private ParanoiaSystem _paranoiaSystem;

    // ================================================================
    // ÉTAT
    // ================================================================

    [Header("État (lecture seule en jeu)")]
    [SerializeField] private MissionData _currentMission;
    [SerializeField] private bool        _missionActive   = false;
    [SerializeField] private bool        _quotaValid      = false;
    [SerializeField] private float       _startTime       = 0f;

    // Suivi pour le résultat
    private float _maxParanoiaReached = 0f;
    private int   _trapsTriggered     = 0;
    private int   _objectsBroken      = 0;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnQuotaReached>.Subscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Subscribe(OnTrapTriggered);
        EventBus<OnObjectDamaged>.Subscribe(OnObjectDamaged);
        EventBus<OnUrgencyTimerStarted>.Subscribe(OnUrgencyTimer);
        EventBus<OnMissionEndRequested>.Subscribe(OnMissionEndRequested);
        EventBus<OnDepartureConfirmed>.Subscribe(OnDepartureConfirmed);
    }

    private void OnDisable()
    {
        EventBus<OnQuotaReached>.Unsubscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Unsubscribe(OnTrapTriggered);
        EventBus<OnObjectDamaged>.Unsubscribe(OnObjectDamaged);
        EventBus<OnUrgencyTimerStarted>.Unsubscribe(OnUrgencyTimer);
        EventBus<OnMissionEndRequested>.Unsubscribe(OnMissionEndRequested);
        EventBus<OnDepartureConfirmed>.Unsubscribe(OnDepartureConfirmed);
    }

    // ================================================================
    // API PUBLIQUE — DÉMARRAGE MISSION
    // ================================================================

    /// <summary>
    /// Démarre une mission. Appelé par MissionBuilder après construction.
    /// </summary>
    public void StartMission(MissionData mission)
    {
        if (mission == null)
        {
            Debug.LogError("[MissionSystem] MissionData est null !");
            return;
        }

        _currentMission = mission;

        // Seed reproductible (même seed = même mission)
        int seed = mission.FixedSeed != 0 
            ? mission.FixedSeed 
            : Random.Range(1, 999999);
        Random.InitState(seed);

        EventBus<OnMissionStarted>.Raise(new OnMissionStarted
        {
            Mission = mission,
            Seed    = seed
        });

        _missionActive       = true;
        _quotaValid          = false;
        _maxParanoiaReached  = 0f;
        _trapsTriggered      = 0;
        _objectsBroken       = 0;
        _startTime           = Time.time;

        Debug.Log($"[MissionSystem] Mission démarrée : {mission.MissionName} (seed: {seed})");
    }

    // ================================================================
    // HANDLERS D'EVENTS
    // ================================================================

    private void OnQuotaReached(OnQuotaReached e)
    {
        _quotaValid = true;
        Debug.Log("[MissionSystem] Quota atteint — mission validable");
    }

    private void OnParanoiaChanged(OnParanoiaChanged e)
    {
        if (e.NewValue > _maxParanoiaReached)
            _maxParanoiaReached = e.NewValue;
    }

    private void OnTrapTriggered(OnTrapTriggered e)
    {
        _trapsTriggered++;
    }

    private void OnObjectDamaged(OnObjectDamaged e)
    {
        _objectsBroken++;
    }

    private void OnUrgencyTimer(OnUrgencyTimerStarted e)
    {
        StartCoroutine(ExpulsionTimerCoroutine(e.DurationSeconds));
    }

    private void OnMissionEndRequested(OnMissionEndRequested e)
    {
        // Le joueur a interagi avec la porte conducteur
        // On ne termine pas immédiatement — on attend sa confirmation
        Debug.Log("[MissionSystem] Fin de mission demandée — attente confirmation");
    }

    private void OnDepartureConfirmed(OnDepartureConfirmed e)
    {
        if (e.Confirmed)
            EndMission(voluntaryDeparture: true);
    }

    // ================================================================
    // FIN DE MISSION
    // ================================================================

    /// <summary>
    /// Termine la mission et calcule le résultat.
    /// </summary>
    private void EndMission(bool voluntaryDeparture)
    {
        if (!_missionActive) return;
        _missionActive = false;

        float elapsedTime = Time.time - _startTime;

        // Récupère les données finales depuis QuotaSystem
        float recovered = _quotaSystem != null ? _quotaSystem.TotalValue : 0f;
        float target    = _quotaSystem != null ? _quotaSystem.TargetValue : 1f;
        int   objCount  = _quotaSystem != null ? _quotaSystem.LoadedObjects.Count : 0;

        // Calcul des étoiles
        int stars = CalculateStars(recovered, target, _objectsBroken, _trapsTriggered, elapsedTime);

        // Construction du résultat
        var result = new MissionResult
        {
            Mission                  = _currentMission,
            ValeurTotaleRecuperee    = recovered,
            ValeurQuotaCible         = target,
            NombreObjetsRecuperes    = objCount,
            NombreObjetsCasses       = _objectsBroken,
            NombrePiegesDeclenches   = _trapsTriggered,
            TempsSecondes            = elapsedTime,
            ParanoiaMaxAtteinte      = _maxParanoiaReached,
            MissionReussie           = _quotaValid,
            Etoiles                  = stars,
            ArgentGagne              = recovered * 0.85f  // 15% de frais d'agence
        };

        EventBus<OnMissionEnded>.Raise(new OnMissionEnded
        {
            Result = result
        });

        Debug.Log($"[MissionSystem] Mission terminée — Étoiles: {stars} | Argent: {result.ArgentGagne:N0} €");

        // CORRECTION V2 : ne plus faire RetourHubCoroutine ici
        // SceneLoader écoute OnMissionEnded et gère le retour Hub automatiquement
    }

    // ================================================================
    // CALCUL DES ÉTOILES
    // ================================================================

    private int CalculateStars(float recovered, float target, int broken, int traps, float time)
    {
        if (recovered < target) 
            return 0;

        // 3 étoiles : récupéré >= 2× quota, 0 cassés, 0 pièges
        bool perfectRun = recovered >= target * _currentMission.ValueMultiplierFor3Stars
                       && broken == 0
                       && traps == 0;
        if (perfectRun) 
            return 3;

        // 2 étoiles : récupéré >= 1.5× quota, ≤ MaxBrokenObjectsFor2Stars cassés
        bool goodRun = recovered >= target * 1.5f
                    && broken <= _currentMission.MaxBrokenObjectsFor2Stars;
        if (goodRun) 
            return 2;

        // 1 étoile : quota atteint mais pas les conditions ci-dessus
        return 1;
    }

    // ================================================================
    // TIMER D'EXPULSION (police appelée)
    // ================================================================

    private IEnumerator ExpulsionTimerCoroutine(float duration)
    {
        Debug.Log($"[MissionSystem] Timer urgence : {duration}s avant expulsion");
        yield return new WaitForSeconds(duration);

        if (_missionActive)
        {
            Debug.Log("[MissionSystem] Temps écoulé — expulsion forcée");
            EndMission(voluntaryDeparture: false);
        }
    }

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    public MissionData CurrentMission => _currentMission;
    public bool        IsActive       => _missionActive;
    public bool        QuotaValid     => _quotaValid;
    public float       ElapsedTime    => _missionActive ? Time.time - _startTime : 0f;
}