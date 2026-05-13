// ============================================================
// MissionSystem.cs — Bailiff & Co  V2
// Orchestrateur : démarre la mission, écoute les événements,
// collecte les stats, calcule le bulletin de paie complet.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionSystem : MonoBehaviour
{
    // ================================================================
    // RÉFÉRENCES INJECTÉES
    // ================================================================

    [Header("Références injectées")]
    [SerializeField] private QuotaSystem     _quotaSystem;
    [SerializeField] private ParanoiaSystem  _paranoiaSystem;

    // ================================================================
    // ÉTAT
    // ================================================================

    [Header("État (lecture seule en jeu)")]
    [SerializeField] private MissionData _currentMission;
    [SerializeField] private bool        _missionActive  = false;
    [SerializeField] private bool        _quotaValid     = false;
    [SerializeField] private float       _startTime      = 0f;

    // Suivi stats
    private float _maxParanoiaReached = 0f;
    private int   _trapsTriggered     = 0;

    // Suivi détaillé pour le bulletin
    private readonly List<MissionResult.ObjetEndommage>    _objetsEndommages    = new();
    private readonly Dictionary<string, (int qty, float prix)> _consommablesMap = new();

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnQuotaReached>.Subscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Subscribe(OnTrapTriggered);
        EventBus<OnObjectDamaged>.Subscribe(OnObjectDamaged);
        EventBus<OnConsommableUsed>.Subscribe(OnConsommableUsed);
        EventBus<OnUrgencyTimerStarted>.Subscribe(OnUrgencyTimer);
        EventBus<OnMissionEndRequested>.Subscribe(OnMissionEndRequested);
    }

    private void OnDisable()
    {
        EventBus<OnQuotaReached>.Unsubscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Unsubscribe(OnTrapTriggered);
        EventBus<OnObjectDamaged>.Unsubscribe(OnObjectDamaged);
        EventBus<OnConsommableUsed>.Unsubscribe(OnConsommableUsed);
        EventBus<OnUrgencyTimerStarted>.Unsubscribe(OnUrgencyTimer);
        EventBus<OnMissionEndRequested>.Unsubscribe(OnMissionEndRequested);
    }

    // ================================================================
    // API PUBLIQUE — DÉMARRAGE
    // ================================================================

    public void StartMission(MissionData mission)
    {
        if (mission == null)
        {
            Debug.LogError("[MissionSystem] MissionData est null !");
            return;
        }

        _currentMission = mission;

        int seed = mission.FixedSeed != 0
            ? mission.FixedSeed
            : Random.Range(1, 999999);
        Random.InitState(seed);

        EventBus<OnMissionStarted>.Raise(new OnMissionStarted
        {
            Mission = mission,
            Seed    = seed
        });

        _missionActive        = true;
        _quotaValid           = false;
        _maxParanoiaReached   = 0f;
        _trapsTriggered       = 0;
        _objetsEndommages.Clear();
        _consommablesMap.Clear();
        _startTime            = Time.time;

        Debug.Log($"[MissionSystem] Mission démarrée : {mission.MissionName} (seed: {seed})");
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnQuotaReached(OnQuotaReached e)
    {
        _quotaValid = true;
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
        if (!_missionActive) return;
        string nom      = e.Object != null ? (e.Object.ObjectName ?? e.Object.name) : "Objet inconnu";
        float  penalite = e.ValueLost * 0.5f;
        _objetsEndommages.Add(new MissionResult.ObjetEndommage
        {
            Nom           = nom,
            ValeurUnitaire = e.ValueLost,
            Penalite       = penalite
        });
    }

    private void OnConsommableUsed(OnConsommableUsed e)
    {
        if (!_missionActive) return;
        if (_consommablesMap.TryGetValue(e.Nom, out var existing))
            _consommablesMap[e.Nom] = (existing.qty + e.Quantite, e.CoutUnitaire);
        else
            _consommablesMap[e.Nom] = (e.Quantite, e.CoutUnitaire);
    }

    private void OnUrgencyTimer(OnUrgencyTimerStarted e)
    {
        StartCoroutine(ExpulsionTimerCoroutine(e.DurationSeconds));
    }

    private void OnMissionEndRequested(OnMissionEndRequested e)
    {
        Debug.Log("[MissionSystem] Fin de mission demandée — attente confirmation");
    }

    // ================================================================
    // FIN DE MISSION
    // ================================================================

    /// <summary>
    /// Termine la mission et calcule le bulletin de paie.
    /// Appelé directement par VehicleRuntime après ConvertObjectsToQuota().
    /// </summary>
    public void EndMission(bool voluntaryDeparture)
    {
        if (!_missionActive) return;
        _missionActive = false;

        // === RÉCUPÈRE LES DONNÉES TRACKÉES ===
        var tracker = FindObjectOfType<MissionTracker>();
        if (tracker == null)
        {
            Debug.LogError("[MissionSystem] MissionTracker introuvable !");
            return;
        }

        // Données brutes
        float elapsedTime = Time.time - _startTime;
        var loadedObjects = tracker.GetObjetsCharges();  // NEW
        var damagedObjects = tracker.GetObjetsEndommages();  // NEW
        var stolenObjects = tracker.GetObjetsVoles();  // NEW
        var consumablesUsed = tracker.GetConsommablesUtilises();  // NEW
        float vehicleDamages = tracker.GetTotalDegatsVehicule();  // NEW
        float infractions = tracker.GetTotalAmendesInfractions();  // NEW

        // Calcul valeur récupérée (chargée - volée)
        float recovered = 0f;
        foreach (var (obj, qty, totalVal) in loadedObjects)
            recovered += totalVal;

        foreach (var (obj, qty, totalVal) in stolenObjects)
            recovered -= totalVal;  // Compensation si vol

        float target = _currentMission.MinimumQuotaValue > 0
            ? _currentMission.MinimumQuotaValue
            : 5000f;  // fallback

        // === CALCUL STARS ===
        int stars = CalculateStars(recovered, target, damagedObjects.Count, _trapsTriggered, elapsedTime);

        // === HONORAIRES ===
        float commissionTaux = (recovered >= target)
            ? _currentMission.CommissionTaux
            : _currentMission.CommissionEchecTaux;
        float commission = recovered * commissionTaux;
        float bonus = CalculateBonus(stars, recovered);

        // === RETENUES A : OBJETS CASSÉS ===
        float penaliteObjets = 0f;
        var objetsEndommagesList = new List<MissionResult.ObjetEndommage>();
        foreach (var (nom, qty, valeurPerdue) in damagedObjects)
        {
            float penalite = valeurPerdue * 0.5f;
            penaliteObjets += penalite;
            objetsEndommagesList.Add(new MissionResult.ObjetEndommage
            {
                Nom = nom,
                ValeurUnitaire = valeurPerdue / qty,  // valeur unitaire perdue
                Penalite = penalite
            });
        }

        // === RETENUES B : LOCATION VÉHICULE ===
        float locationVehicule = GameManager.Instance?.VehiculeSelectionne?.RentalPrice ?? 0f;

        // === RETENUES C : SAISIE EXCESSIVE ===
        float amendeExces = 0f;
        bool suspendu = false;
        if (target > 0f && recovered > target)
        {
            float exces = recovered - target;
            float excesRatio = exces / target;
            var m = _currentMission;

            if (excesRatio > m.SeuilExcesAbusif)
            {
                amendeExces = exces * m.TauxPenaliteExcesAbusif;
                suspendu = true;
            }
            else if (excesRatio > m.SeuilExcesModere)
            {
                amendeExces = exces * m.TauxPenaliteExcesModere;
            }
            else if (excesRatio > m.SeuilExcesLeger)
            {
                amendeExces = exces * m.TauxPenaliteExcesLeger;
            }
        }

        // === TOTAL RETENUES ===
        float totalRetenues = penaliteObjets + locationVehicule + vehicleDamages + amendeExces + infractions;
        float salaireNet = commission + bonus - totalRetenues;

        // === BUILD RÉSULTAT ===
        var result = new MissionResult
        {
            Mission = _currentMission,
            MissionReussie = (recovered >= target),
            Etoiles = stars,
            TempsSecondes = elapsedTime,
            ParanoiaMaxAtteinte = _maxParanoiaReached,
            NombrePiegesDeclenches = _trapsTriggered,
            ValeurTotaleRecuperee = recovered,
            ValeurQuotaCible = target,
            NombreObjetsRecuperes = loadedObjects.Count,
            CommissionBase = commission,
            BonusPerformance = bonus,
            ObjetsEndommages = objetsEndommagesList,
            CoutLocationVehicule = locationVehicule,
            DegatsVehicule = vehicleDamages,
            AmendesSaisieExcessive = amendeExces,
            Suspendu = suspendu,
            AmendesInfractions = infractions,
            SalaireNet = salaireNet,
            ObjetsRecuperes = BuildObjetsRecuperes(loadedObjects),
            ConsommablesUtilises = BuildConsommablesUtilises(consumablesUsed)
        };

        EventBus<OnMissionEnded>.Raise(new OnMissionEnded { Result = result });

        Debug.Log($"[MissionSystem] Mission terminée — ★{stars} | " +
                $"Récupéré: {recovered:N0}€ | Net: {salaireNet:N0}€" +
                (suspendu ? " | SUSPENDU" : ""));
    }

    // ================================================================
    // HELPERS PRIVÉS
    // ================================================================

    private float CalculateBonus(int stars, float recovered)
    {
        return stars switch
        {
            3 => recovered * _currentMission.BonusEtoile3,
            2 => recovered * _currentMission.BonusEtoile2,
            _ => 0f
        };
    }

    private List<MissionResult.ObjetRecupere> BuildObjetsRecuperes(
        List<(ObjetData obj, int qty, float totalVal)> loadedObjects)
    {
        var list = new List<MissionResult.ObjetRecupere>();
        foreach (var (obj, qty, totalVal) in loadedObjects)
        {
            list.Add(new MissionResult.ObjetRecupere
            {
                Nom = obj.ObjectName,
                Quantite = qty,
                ValeurUnitaire = qty > 0 ? totalVal / qty : 0f,
                ValeurTotale = totalVal
            });
        }
        return list;
    }

    private List<MissionResult.ConsommableUtilise> BuildConsommablesUtilises(List<(string nom, int qty, float coutUnitaire)> consumablesUsed)
    {
        var list = new List<MissionResult.ConsommableUtilise>();
        foreach (var (nom, qty, coutUnitaire) in consumablesUsed)
        {
            list.Add(new MissionResult.ConsommableUtilise
            {
                Nom = nom,
                Quantite = qty,
                CoutUnitaire = coutUnitaire,
                CoutTotal = qty * coutUnitaire
            });
        }
        return list;
    }

    private int CalculateStars(float recovered, float target, int broken, int traps, float time)
    {
        if (recovered < target)
            return 0;

        bool perfectRun = recovered >= target * _currentMission.ValueMultiplierFor3Stars
                       && broken == 0
                       && traps == 0;
        if (perfectRun) return 3;

        bool goodRun = recovered >= target * 1.5f
                    && broken <= _currentMission.MaxBrokenObjectsFor2Stars;
        if (goodRun) return 2;

        return 1;
    }

    // ================================================================
    // TIMER D'EXPULSION
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
