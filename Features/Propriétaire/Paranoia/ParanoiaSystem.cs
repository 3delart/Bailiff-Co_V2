// ============================================================
// ParanoiaSystem.cs — Bailiff & Co  V2
// SOURCE DE VÉRITÉ UNIQUE pour la paranoïa (0–100).
// Ne modifie rien directement — reçoit des events, calcule,
// émet OnParanoiaChanged et les transitions de palier.
//
// CHANGEMENTS V2 :
//   - OnBruitEmis → OnNoiseEmitted
//   - OnObjetCharge → OnObjectLoaded
//   - OnPiegeDeclenche → OnTrapTriggered
//   - OnAnimalAboie → OnAnimalAlerted
//   - OnMissionDemarree → OnMissionStarted
//   - OnMissionTerminee → OnMissionEnded
//   - Optionnel : ParanoiaConfig.asset pour tweaker les valeurs
//   - Paliers constants copiés de ProprietaireData
// ============================================================
using UnityEngine;

public class ParanoiaSystem : MonoBehaviour
{
    // ================================================================
    // CONFIGURATION (optionnelle)
    // ================================================================

    [Header("Configuration (optionnelle)")]
    [Tooltip("Si null, utilise les constantes par défaut")]
    [SerializeField] private ParanoiaConfig _config;

    // ================================================================
    // PALIERS — identiques à ProprietaireData
    // ================================================================

    public const float PALIER_CALM      = 0f;
    public const float PALIER_MEFIANT   = 11f;
    public const float PALIER_INQUIET   = 26f;
    public const float PALIER_PANIQUE   = 51f;
    public const float PALIER_FURIEUX   = 76f;
    public const float PALIER_OBSESSION = 91f;

    // ================================================================
    // ÉTAT
    // ================================================================

    [Header("État (lecture seule en jeu)")]
    [SerializeField] private float _paranoia = 0f;
    [SerializeField] private int   _tier     = 0;

    private float _timeSinceLastAction = 0f;
    private bool  _playerVisible       = false;
    private bool  _missionActive       = false;

    // ================================================================
    // PROPERTIES
    // ================================================================

    public float Value => _paranoia;
    public int   Tier  => _tier;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
        EventBus<OnMissionEnded>.Subscribe(OnMissionEnded);
        EventBus<OnNoiseEmitted>.Subscribe(OnNoiseEmitted);
        EventBus<OnObjectLoaded>.Subscribe(OnObjectLoaded);
        EventBus<OnTrapTriggered>.Subscribe(OnTrapTriggered);
        EventBus<OnAnimalAlerted>.Subscribe(OnAnimalAlerted);
    }

    private void OnDisable()
    {
        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
        EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
        EventBus<OnNoiseEmitted>.Unsubscribe(OnNoiseEmitted);
        EventBus<OnObjectLoaded>.Unsubscribe(OnObjectLoaded);
        EventBus<OnTrapTriggered>.Unsubscribe(OnTrapTriggered);
        EventBus<OnAnimalAlerted>.Unsubscribe(OnAnimalAlerted);
    }

    private void Update()
    {
        if (!_missionActive) return;

        // Décroissance passive si joueur discret
        if (!_playerVisible)
        {
            _timeSinceLastAction += Time.deltaTime;

            float delayBeforeDecay = _config != null 
                ? _config.DelayBeforeDecay 
                : 10f;

            if (_timeSinceLastAction > delayBeforeDecay)
            {
                float decayRate = _config != null 
                    ? _config.DecayRatePerSecond 
                    : 5f / 120f; // 5 pts / 2 min par défaut

                Modify(-decayRate * Time.deltaTime);
            }
        }
    }

    // ================================================================
    // HANDLERS D'EVENTS
    // ================================================================

    private void OnMissionStarted(OnMissionStarted e)
    {
        _paranoia = e.Mission.Owner?.StartingParanoia ?? 0f;
        _tier     = CalculateTier(_paranoia);
        _missionActive = true;
        _timeSinceLastAction = 0f;

        Debug.Log($"[ParanoiaSystem] Démarrage — paranoïa initiale : {_paranoia:F0} (palier {_tier})");
    }

    private void OnMissionEnded(OnMissionEnded e)
    {
        _missionActive = false;
    }

    private void OnNoiseEmitted(OnNoiseEmitted e)
    {
        float delta = e.Level switch
        {
            NiveauBruit.Leger    => _config != null ? _config.NoiseLight : 3f,
            NiveauBruit.Fort     => _config != null ? _config.NoiseLoud  : 12f,
            NiveauBruit.Tresfort => _config != null ? _config.NoiseVeryLoud : 25f,
            _                    => 0f
        };

        Modify(delta);
        ResetDecayTimer();
    }

    private void OnObjectLoaded(OnObjectLoaded e)
    {
        // Chaque objet chargé augmente la paranoïa
        // Formule : valeur / 5000, clampé entre 3 et 15
        float delta = Mathf.Clamp(e.Value / 5000f, 3f, 15f);
        Modify(delta);
        ResetDecayTimer();
    }

    private void OnTrapTriggered(OnTrapTriggered e)
    {
        float delta = e.Trap?.ParanoiaBonusOnTrigger ?? 10f;
        Modify(delta);
        ResetDecayTimer();
    }

    private void OnAnimalAlerted(OnAnimalAlerted e)
    {
        // Intensité 0-1 → 3-8 points de paranoïa
        float delta = Mathf.Lerp(3f, 8f, e.Intensity);
        Modify(delta);
        ResetDecayTimer();
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Modifie manuellement la paranoïa (ex: badge présenté = -5, spray = +15).
    /// Utilisé par les outils et actions spéciales.
    /// </summary>
    public void Modify(float delta)
    {
        if (!_missionActive) return;

        float oldValue = _paranoia;
        int   oldTier  = _tier;

        _paranoia = Mathf.Clamp(_paranoia + delta, 0f, 100f);
        _tier     = CalculateTier(_paranoia);

        EventBus<OnParanoiaChanged>.Raise(new OnParanoiaChanged
        {
            NewValue = _paranoia,
            OldValue = oldValue,
            NewTier  = _tier
        });

        if (_tier != oldTier)
        {
            Debug.Log($"[ParanoiaSystem] Changement de palier : {GetTierName(oldTier)} → {GetTierName(_tier)} ({_paranoia:F0})");
        }
    }

    /// <summary>
    /// Appelé par ProprietaireAI pour signaler que le joueur est visible.
    /// Stoppe la décroissance passive.
    /// </summary>
    public void SetPlayerVisible(bool visible)
    {
        _playerVisible = visible;
        if (visible) 
            ResetDecayTimer();
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private void ResetDecayTimer()
    {
        _timeSinceLastAction = 0f;
    }

    public static int CalculateTier(float value)
    {
        if (value >= PALIER_OBSESSION) return 5;
        if (value >= PALIER_FURIEUX)   return 4;
        if (value >= PALIER_PANIQUE)   return 3;
        if (value >= PALIER_INQUIET)   return 2;
        if (value >= PALIER_MEFIANT)   return 1;
        return 0;
    }

    public static string GetTierName(int tier) => tier switch
    {
        0 => "Calme",
        1 => "Méfiant",
        2 => "Inquiet",
        3 => "Paniqué",
        4 => "Furieux",
        5 => "Obsessionnel",
        _ => "?"
    };
}