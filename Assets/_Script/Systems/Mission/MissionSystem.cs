// ============================================================
// MissionSystem.cs — Bailiff & Co  V2
// Orchestrateur : démarre la mission, écoute les événements,
// collecte les stats, calcule le bulletin de paie complet.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BailiffCo;

public class MissionSystem : MonoBehaviour
{
    // ================================================================
    // CONSTANTES
    // ================================================================

    private const float QUOTA_RATIO_LIMIT = 1.25f;  // Max ratio to qualify for highest stars
    private const float DEFAULT_QUOTA = 5000f;      // Default target if MissionData.MinimumQuotaValue is 0
    private const int MAX_RANDOM_SEED = 999999;     // Upper bound for random seed generation

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
    private readonly Dictionary<string, (int qty, float prix)> _consommablesMap = new();

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnQuotaReached>.Subscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Subscribe(OnTrapTriggered);
        EventBus<OnConsommableUsed>.Subscribe(OnConsommableUsed);
        EventBus<OnUrgencyTimerStarted>.Subscribe(OnUrgencyTimer);
        EventBus<OnMissionEndRequested>.Subscribe(OnMissionEndRequested);
    }

    private void OnDisable()
    {
        EventBus<OnQuotaReached>.Unsubscribe(OnQuotaReached);
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnTrapTriggered>.Unsubscribe(OnTrapTriggered);
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
            : Random.Range(1, MAX_RANDOM_SEED);
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

    public void EndMission(bool voluntaryDeparture)
    {
        if (!_missionActive) return;
        _missionActive = false;

        var tracker = FindFirstObjectByType<MissionTracker>();
        if (tracker == null)
        {
            Debug.LogError("[MissionSystem] MissionTracker introuvable !");
            return;
        }

        float elapsedTime = Time.time - _startTime;
        var loadedObjects = tracker.GetObjetsCharges();
        var stolenObjects = tracker.GetObjetsVoles();
        var consumablesUsed = tracker.GetConsommablesUtilises();
        float vehicleDamages = tracker.GetTotalDegatsVehicule();
        float infractions = tracker.GetTotalAmendesInfractions();

        float recovered = CalculateRecoveredValue(loadedObjects, stolenObjects);
        float target = CalculateQuotaTarget();
        int brokenCount = CountBrokenObjects(loadedObjects);

        int stars = CalculateStars(recovered, target, brokenCount, _trapsTriggered, elapsedTime, _maxParanoiaReached);
        bool bonusTemps = ApplyTimeBonus(ref stars, elapsedTime);

        (float commission, float bonus) = CalculateCommission(recovered, target, stars);

        (float penaliteObjets, var objetsEndommagesList) = CalculateDamagesPenalties(loadedObjects);

        float locationVehicule = GameManager.Instance?.VehiculeSelectionne?.RentalPrice ?? 0f;
        var optionsLouees = GameManager.Instance?.OptionsSelectionnees;
        if (optionsLouees == null) optionsLouees = new List<VehicleOption>();
        float coutOptionsVehicule = optionsLouees.Sum(o => o.Price);

        (float amendeExces, bool suspendu) = CalculateExcessiveSeizurePenalty(recovered, target, ref stars);

        // === TOTAL RETENUES ===
        float totalRetenues = penaliteObjets + locationVehicule + coutOptionsVehicule + vehicleDamages + amendeExces + infractions;
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
            OptionsLouees = optionsLouees,
            CoutOptionsVehicule = coutOptionsVehicule,
            DegatsVehicule = vehicleDamages,
            AmendesSaisieExcessive = amendeExces,
            Suspendu = suspendu,
            BonusTempsApplique = bonusTemps,
            AmendesInfractions = infractions,
            SalaireNet = salaireNet,
            ObjetsRecuperes = BuildObjetsRecuperes(loadedObjects),
            ConsommablesUtilises = BuildConsommablesUtilises(consumablesUsed)
        };

        EventBus<OnMissionEnded>.Raise(new OnMissionEnded { Result = result });

        Debug.Log($"[MissionSystem] Mission terminée — *{stars} | " +
                $"Récupéré: {recovered:N0}€ | Net: {salaireNet:N0}€" +
                (suspendu ? " | SUSPENDU" : ""));
    }

    // ✅ Construit ObjetsRecuperes depuis instances individuelles
    private List<MissionResult.ObjetRecupere> BuildObjetsRecuperes(
        List<(ObjetData obj, int instanceId, float basePrice, float currentPrice, float damagePercent, bool isBroken)> loadedObjects)
    {
        var list = new List<MissionResult.ObjetRecupere>();
        
        // Regroupe par asset pour l'affichage
        // ✅ On utilise basePrice (prix de base) toujours
        var grouped = new Dictionary<string, (int qty, float totalBasePrice, float totalCurrentPrice, float avgDamagePercent)>();
        
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
        {
            string key = obj.ObjectName;
            
            if (grouped.TryGetValue(key, out var entry))
            {
                grouped[key] = (entry.qty + 1, 
                                entry.totalBasePrice + basePrice,
                                entry.totalCurrentPrice + currentPrice,
                                (entry.avgDamagePercent + damagePercent) / 2f);
            }
            else
            {
                grouped[key] = (1, basePrice, currentPrice, damagePercent);
            }
        }
        
        foreach (var kv in grouped)
        {
            var (qty, totalBasePrice, totalCurrentPrice, avgDamage) = kv.Value;
            list.Add(new MissionResult.ObjetRecupere
            {
                Nom = kv.Key,
                Quantite = qty,
                ValeurUnitaire = qty > 0 ? totalBasePrice / qty : 0f,  // ✅ Prix de base
                ValeurTotale = totalBasePrice,
                ValeurActuelle = totalCurrentPrice,                      // ✅ NOUVEAU: prix après dégâts
                DamagePercent = avgDamage                                // ✅ NOUVEAU: % moyen de dégâts
            });
        }
        
        return list;
    }

    private List<MissionResult.ConsommableUtilise> BuildConsommablesUtilises(
        List<(string nom, int qty, float coutUnitaire)> consumablesUsed)
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

    private float CalculateRecoveredValue(
        IReadOnlyList<(ObjetData, int, float, float, float, bool)> loadedObjects,
        IReadOnlyList<(ObjetData, float)> stolenObjects)
    {
        float recovered = 0f;
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
            recovered += basePrice;
        foreach (var (obj, valeur) in stolenObjects)
            recovered -= valeur;
        return recovered;
    }

    private float CalculateQuotaTarget() => _currentMission.MinimumQuotaValue > 0
        ? _currentMission.MinimumQuotaValue
        : DEFAULT_QUOTA;

    private int CountBrokenObjects(IReadOnlyList<(ObjetData, int, float, float, float, bool)> loadedObjects)
    {
        int count = 0;
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
            if (damagePercent >= 100f) count++;
        return count;
    }

    private bool ApplyTimeBonus(ref int stars, float elapsedTime)
    {
        bool bonusApplied = stars > 0 && elapsedTime < _currentMission.BonusTimeThresholdSeconds;
        if (bonusApplied)
            stars = Mathf.Min(stars + 1, 3);
        return bonusApplied;
    }

    private (float commission, float bonus) CalculateCommission(float recovered, float target, int stars)
    {
        float commissionTaux = (recovered >= target)
            ? _currentMission.CommissionTaux
            : _currentMission.CommissionEchecTaux;
        float commission = recovered * commissionTaux;
        float bonus = CalculateBonus(stars, recovered);
        return (commission, bonus);
    }

    private (float penalite, List<MissionResult.ObjetEndommage>) CalculateDamagesPenalties(
        IReadOnlyList<(ObjetData, int, float, float, float, bool)> loadedObjects)
    {
        float totalPenalite = 0f;
        var damagesList = new List<MissionResult.ObjetEndommage>();
        var processedNames = new HashSet<string>();

        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
        {
            if (damagePercent > 0f && damagePercent <= 100f)
            {
                float penalite = basePrice - currentPrice;
                totalPenalite += penalite;
                damagesList.Add(new MissionResult.ObjetEndommage
                {
                    Nom = obj.ObjectName,
                    ValeurUnitaire = basePrice,
                    ValeurActuelle = currentPrice,
                    DamagePercent = damagePercent,
                    Penalite = penalite
                });
                processedNames.Add(obj.ObjectName);
                Debug.Log($"[MissionSystem] EMBARQUÉ: {obj.ObjectName}: {basePrice:N0}€ → {currentPrice:N0}€ ({damagePercent:F1}%) | Pénalité: {penalite:N0}€");
            }
        }

        var loadedIds = new HashSet<int>();
        foreach (var (_, instanceId, _, _, _, _) in loadedObjects)
            loadedIds.Add(instanceId);

        var sceneObjects = FindObjectsByType<ValueObject>(FindObjectsSortMode.None);
        foreach (var vo in sceneObjects)
        {
            if (vo.DamagePercent <= 0f || loadedIds.Contains(vo.GetInstanceID())) continue;

            float basePrice = vo.Data != null ? vo.Data.Value : 0f;
            float actualValue = vo.ActualValue;
            float penalite = basePrice - actualValue;

            totalPenalite += penalite;
            damagesList.Add(new MissionResult.ObjetEndommage
            {
                Nom = vo.Data != null ? vo.Data.ObjectName : vo.name,
                ValeurUnitaire = basePrice,
                ValeurActuelle = actualValue,
                DamagePercent = vo.DamagePercent,
                Penalite = penalite
            });
            Debug.Log($"[MissionSystem] NON-EMBARQUÉ: {vo.name} | {basePrice:N0}€ → {actualValue:N0}€ ({vo.DamagePercent:F1}%) | Pénalité: {penalite:N0}€");
        }

        return (totalPenalite, damagesList);
    }

    private (float penalite, bool suspendu) CalculateExcessiveSeizurePenalty(float recovered, float target, ref int stars)
    {
        float penalite = 0f;
        bool suspendu = false;

        if (target > 0f && recovered > target)
        {
            float exces = recovered - target;
            float excesRatio = exces / target;
            var m = _currentMission;

            if (excesRatio > m.SeuilExcesAbusif)
            {
                penalite = exces * m.TauxPenaliteExcesAbusif;
                suspendu = true;
            }
            else if (excesRatio > m.SeuilExcesModere)
            {
                penalite = exces * m.TauxPenaliteExcesModere;
            }
            else if (excesRatio > m.SeuilExcesLeger)
            {
                penalite = exces * m.TauxPenaliteExcesLeger;
            }

            if (suspendu && stars > 1)
                stars = 1;
        }

        return (penalite, suspendu);
    }

    private float CalculateBonus(int stars, float recovered)
    {
        return stars switch
        {
            3 => recovered * _currentMission.BonusEtoile3,
            2 => recovered * _currentMission.BonusEtoile2,
            _ => 0f
        };
    }

    private int CalculateStars(float recovered, float target, int broken, int traps, float time, float paranoia)
    {
        if (recovered < target) return 0;

        float ratio = recovered / target;
        bool quotaLegal = ratio <= QUOTA_RATIO_LIMIT;

        bool star3 = quotaLegal
                  && broken == 0
                  && traps == 0
                  && paranoia < _currentMission.ParanoiaSeuilStar3;
        if (star3) return 3;

        bool star2 = quotaLegal
                  && broken <= _currentMission.MaxBrokenObjectsFor2Stars
                  && paranoia < _currentMission.ParanoiaSeuilStar2;
        if (star2) return 2;

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