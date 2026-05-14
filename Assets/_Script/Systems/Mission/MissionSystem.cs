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
        if (e.Object == null) return;

        string nom = e.Object.ObjectName ?? e.Object.name;
        float valeurUnitaire = e.Object.Value;
        float valeurActuelle = e.ValueAfter;
        float valeurPerdue = valeurUnitaire - valeurActuelle;
        float penalite = valeurPerdue * 0.5f;

        _objetsEndommages.Add(new MissionResult.ObjetEndommage
        {
            Nom           = nom,
            ValeurUnitaire = valeurUnitaire,
            ValeurActuelle = valeurActuelle,
            DamagePercent = e.DamagePercent,
            Penalite = penalite
        });

        Debug.Log($"[MissionSystem] Objet endommagé non-embarqué: {nom} | {valeurUnitaire:N0}€ → {valeurActuelle:N0}€ ({e.DamagePercent:F1}%) | Pénalité: {penalite:N0}€");
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
        var damagedObjects = tracker.GetObjetsEndommages();     
        var stolenObjects = tracker.GetObjetsVoles();
        var consumablesUsed = tracker.GetConsommablesUtilises();
        float vehicleDamages = tracker.GetTotalDegatsVehicule();
        float infractions = tracker.GetTotalAmendesInfractions();

        // === CALCUL VALEUR RÉCUPÉRÉE ===
        // ✅ Utiliser basePrice (prix original) pour le quota
        // Les retenues de cassure sont appliquées séparément en tant que pénalités
        float recovered = 0f;
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
            recovered += basePrice;  // ✅ Prix original, pas réduit

        foreach (var (obj, valeur) in stolenObjects)
            recovered -= valeur;

        float target = _currentMission.MinimumQuotaValue > 0
            ? _currentMission.MinimumQuotaValue
            : 5000f;

        // === CALCUL STARS ===
        int brokenCount = 0;
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
        {
            if (damagePercent >= 100f) brokenCount++;
        }
        int stars = CalculateStars(recovered, target, brokenCount, _trapsTriggered, elapsedTime);

        // === HONORAIRES ===
        float commissionTaux = (recovered >= target)
            ? _currentMission.CommissionTaux
            : _currentMission.CommissionEchecTaux;
        float commission = recovered * commissionTaux;
        float bonus = CalculateBonus(stars, recovered);

        // === RETENUES A : OBJETS CASSÉS (embarqués + non-embarqués) ===
        float penaliteObjets = 0f;
        var objetsEndommagesList = new List<MissionResult.ObjetEndommage>();
        var processedNames = new HashSet<string>();

        // 1️⃣ Objets EMBARQUÉS cassés (utilise données de LoadedObjects avec BasePrice/CurrentPrice)
        foreach (var (obj, instanceId, basePrice, currentPrice, damagePercent, isBroken) in loadedObjects)
        {
            if (damagePercent > 0f && damagePercent <= 100f)
            {
                float prixActuel = currentPrice;
                float valeurPerdue = basePrice - prixActuel;
                float penalite = valeurPerdue * 0.5f;
                penaliteObjets += penalite;

                objetsEndommagesList.Add(new MissionResult.ObjetEndommage
                {
                    Nom = obj.ObjectName,
                    ValeurUnitaire = basePrice,
                    ValeurActuelle = prixActuel,
                    DamagePercent = damagePercent,
                    Penalite = penalite
                });
                processedNames.Add(obj.ObjectName);

                Debug.Log($"[MissionSystem] EMBARQUÉ: {obj.ObjectName}: {basePrice:N0}€ → {prixActuel:N0}€ ({damagePercent:F1}%) | Pénalité: {penalite:N0}€");
            }
        }

        // 2️⃣ Objets NON-EMBARQUÉS cassés (scan scène — état final, un objet = une entrée)
        // Les objets embarqués ont été Destroy()-és par LoadIntoVehicle(), donc
        // seuls les non-embarqués sont encore présents en scène.
        var loadedIds = new HashSet<int>();
        foreach (var (_, instanceId, _, _, _, _) in loadedObjects)
            loadedIds.Add(instanceId);

        var sceneObjects = FindObjectsByType<ValueObject>(FindObjectsSortMode.None);
        foreach (var vo in sceneObjects)
        {
            if (vo.DamagePercent <= 0f) continue;
            if (loadedIds.Contains(vo.GetInstanceID())) continue; // filet sécurité (race condition)

            float basePrice    = vo.Data != null ? vo.Data.Value : 0f;
            float actualValue  = vo.ActualValue;
            float valeurPerdue = basePrice - actualValue;
            float penalite     = valeurPerdue * 0.5f;

            penaliteObjets += penalite;
            objetsEndommagesList.Add(new MissionResult.ObjetEndommage
            {
                Nom            = vo.Data != null ? vo.Data.ObjectName : vo.name,
                ValeurUnitaire = basePrice,
                ValeurActuelle = actualValue,
                DamagePercent  = vo.DamagePercent,
                Penalite       = penalite
            });

            Debug.Log($"[MissionSystem] NON-EMBARQUÉ (scène): {vo.name} | " +
                      $"{basePrice:N0}€ → {actualValue:N0}€ ({vo.DamagePercent:F1}%) | " +
                      $"Pénalité: {penalite:N0}€");
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

    private float CalculateBonus(int stars, float recovered)
    {
        return stars switch
        {
            3 => recovered * _currentMission.BonusEtoile3,
            2 => recovered * _currentMission.BonusEtoile2,
            _ => 0f
        };
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