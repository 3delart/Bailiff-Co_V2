// ============================================================
// ProprietaireAI.cs — Bailiff & Co  V2
// State machine principale du proprio (8 états selon GDD §4.3).
// Ne frappe JAMAIS le joueur. La tension est sociale et juridique.
// Émet des events, ne modifie rien directement dans les autres systèmes.
//
// CHANGEMENTS V2 :
//   - ProprietaireDef → ProprietaireData (propriétés calculées)
//   - FindObjectOfType supprimés → injection [SerializeField]
//   - OnBruitEmis → OnNoiseEmitted
//   - OnSeuilAtteint → OnThresholdReached
//   - OnProprietaireStateChanged → OnOwnerStateChanged
//   - OnProprietaireSortDeLaMaison → OnOwnerLeftHouse
//   - OnProprietaireRecupereObjet → OnOwnerRetrievedObject
//   - Toutes les valeurs comportementales viennent des propriétés
//     calculées de ProprietaireData (NormalSpeed, PanicSpeed, etc.)
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class ProprietaireAI : MonoBehaviour
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Configuration")]
    [Tooltip("Données du propriétaire — toutes les valeurs comportementales sont calculées automatiquement")]
    [SerializeField] private ProprietaireData _data;

    [Header("Références injectées")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _vehicle;

    [Header("Patrouille (optionnel)")]
    [Tooltip("Points de patrouille — si vide, reste sur place en Idle")]
    [SerializeField] private Transform[] _patrolPoints;

    // ================================================================
    // COMPOSANTS
    // ================================================================

    private NavMeshAgent _agent;
    private Animator     _animator;
    private AudioSource  _audio;

    // ================================================================
    // ÉTAT
    // ================================================================

    [Header("État (lecture seule en jeu)")]
    [SerializeField] private ProprietaireState _currentState = ProprietaireState.Idle;
    [SerializeField] private float _currentParanoia = 0f;
    [SerializeField] private int   _currentTier     = 0;

    private int       _patrolIndex = 0;
    private Vector3   _lastNoisePosition;
    private bool      _isLocked = false;
    private Coroutine _stateCoroutine;

    // Coyote time pour la vision — évite les flickers
    private const float VISION_COYOTE = 0.2f;
    private float _lastPlayerSeenTime = -999f;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _audio    = GetComponent<AudioSource>();

        if (_data == null)
        {
            Debug.LogError("[ProprietaireAI] ProprietaireData manquant !");
            return;
        }

        // Configuration NavMeshAgent depuis les propriétés calculées
        _agent.speed = _data.NormalSpeed;
    }

    private void Start()
    {
        EnterState(ProprietaireState.Idle);
    }

    private void OnEnable()
    {
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnNoiseEmitted>.Subscribe(OnNoiseEmitted);
        EventBus<OnThresholdReached>.Subscribe(OnThresholdReached);
        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
    }

    private void OnDisable()
    {
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnNoiseEmitted>.Unsubscribe(OnNoiseEmitted);
        EventBus<OnThresholdReached>.Unsubscribe(OnThresholdReached);
        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
    }

    private void Update()
    {
        if (_isLocked) return;
        CheckPlayerVision();
    }

    // ================================================================
    // STATE MACHINE — TRANSITIONS
    // ================================================================

    public void EnterState(ProprietaireState newState)
    {
        // Permet le re-déclenchement de Idle (pour la patrouille)
        if (_currentState == newState && newState != ProprietaireState.Idle) 
            return;

        var oldState = _currentState;
        _currentState = newState;

        if (_stateCoroutine != null) 
            StopCoroutine(_stateCoroutine);

        EventBus<OnOwnerStateChanged>.Raise(new OnOwnerStateChanged
        {
            OldState = oldState,
            NewState = newState
        });

        _stateCoroutine = StartCoroutine(newState switch
        {
            ProprietaireState.Idle        => StateIdle(),
            ProprietaireState.Alert       => StateAlert(),
            ProprietaireState.Investigate => StateInvestigate(),
            ProprietaireState.Confront    => StateConfront(),
            ProprietaireState.Panic       => StatePanic(),
            ProprietaireState.Outdoor     => StateOutdoor(),
            ProprietaireState.Locked      => StateLocked(),
            ProprietaireState.Furious     => StateFurious(),
            _                             => StateIdle()
        });
    }

    // ================================================================
    // STATE COROUTINES
    // ================================================================

    // IDLE — patrouille ou reste sur place
    private IEnumerator StateIdle()
    {
        _agent.speed = _data.NormalSpeed;
        _animator?.SetTrigger("Idle");

        while (_currentState == ProprietaireState.Idle)
        {
            if (_patrolPoints.Length > 0)
            {
                Transform target = _patrolPoints[_patrolIndex % _patrolPoints.Length];
                _agent.SetDestination(target.position);

                yield return new WaitUntil(() =>
                    !_agent.pathPending && _agent.remainingDistance < 0.5f);

                _patrolIndex++;
                yield return new WaitForSeconds(Random.Range(1f, 3f));
            }
            else
            {
                // Pas de points de patrouille — reste sur place
                yield return new WaitForSeconds(1f);
            }
        }
    }

    // ALERT — s'arrête, regarde autour, cherche l'origine du bruit
    private IEnumerator StateAlert()
    {
        _agent.SetDestination(transform.position); // s'arrête net
        _animator?.SetTrigger("Alert");
        PlaySound(_data.SpotPlayerSounds);

        yield return new WaitForSeconds(1.5f);

        // Transition selon le palier de paranoïa
        if (_currentTier >= 2)
            EnterState(ProprietaireState.Investigate);
        else
            EnterState(ProprietaireState.Idle);
    }

    // INVESTIGATE — se dirige vers la source de bruit
    private IEnumerator StateInvestigate()
    {
        _animator?.SetTrigger("Investigate");
        _agent.SetDestination(_lastNoisePosition);

        yield return new WaitUntil(() =>
            !_agent.pathPending && _agent.remainingDistance < 1.5f);

        yield return new WaitForSeconds(2f);

        // Revient à Idle si rien trouvé
        if (_currentState == ProprietaireState.Investigate)
            EnterState(ProprietaireState.Idle);
    }

    // CONFRONT — approche le joueur, dialogue, exige le mandat
    // NE TOUCHE PAS physiquement le joueur
    private IEnumerator StateConfront()
    {
        _agent.speed = _data.NormalSpeed;
        _animator?.SetTrigger("Confront");
        PlaySound(_data.ConfrontSounds);

        // S'approche du joueur à distance de conversation (2.5m)
        while (_currentState == ProprietaireState.Confront)
        {
            if (_player != null)
            {
                float dist = Vector3.Distance(transform.position, _player.position);
                if (dist > 2.5f)
                    _agent.SetDestination(_player.position);
                else
                    _agent.SetDestination(transform.position); // s'arrête
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // PANIC — court, appelle des renforts (amis/avocat selon Sociability)
    private IEnumerator StatePanic()
    {
        _agent.speed = _data.PanicSpeed;
        _animator?.SetTrigger("Panic");
        PlaySound(_data.PanicSounds);

        // Appel automatique avocat si Sociability >= 7
        if (_data.AutoCallsLawyer)
        {
            yield return new WaitForSeconds(3f);
            // TODO V2 : EventBus<OnLawyerCalled>.Raise()
            Debug.Log($"[ProprietaireAI] Appel avocat automatique — seuil : {_data.LawyerCallThreshold}");
        }

        // TODO V2 : Spawn amis selon FriendsCount après FriendsArrivalDelay
        if (_data.FriendsCount > 0)
        {
            Debug.Log($"[ProprietaireAI] {_data.FriendsCount} ami(s) arriveront dans {_data.FriendsArrivalDelay}s");
        }

        yield break;
    }

    // OUTDOOR — sort vers le véhicule pour récupérer un objet
    private IEnumerator StateOutdoor()
    {
        _animator?.SetTrigger("Outdoor");
        _agent.speed = _data.NormalSpeed;

        EventBus<OnOwnerLeftHouse>.Raise(new OnOwnerLeftHouse());

        if (_vehicle != null)
        {
            _agent.SetDestination(_vehicle.position);

            yield return new WaitUntil(() =>
                !_agent.pathPending && _agent.remainingDistance < 2f);

            // Arrive au véhicule — notifie VehicleRuntime
            EventBus<OnVehicleAttacked>.Raise(new OnVehicleAttacked
            {
                Attacker = gameObject,
                IsOwner  = true
            });

            // Forçage du coffre — durée selon Method
            float forceDuration = _data.VehicleForceDuration;
            yield return new WaitForSeconds(forceDuration);

            // TODO V2 : récupération aléatoire d'un objet
            // EventBus<OnOwnerRetrievedObject>.Raise(...)

            // Retour panique après avoir récupéré
            EnterState(ProprietaireState.Panic);
        }
    }

    // LOCKED — immobilisé (menottes, enfermé)
    private IEnumerator StateLocked()
    {
        _isLocked = true;
        _agent.enabled = false;
        _animator?.SetTrigger("Locked");

        // Durée d'immobilisation fixe en V2 (60 sec)
        // TODO : lire depuis un OutilData si on veut des niveaux d'upgrade
        yield return new WaitForSeconds(60f);

        _isLocked = false;
        _agent.enabled = true;
        EnterState(ProprietaireState.Panic);
    }

    // FURIOUS — actions multiples simultanées (palier max)
    private IEnumerator StateFurious()
    {
        _agent.speed = _data.PanicSpeed;
        _animator?.SetTrigger("Furious");

        // En V2 : pose pièges dynamiques selon MaxDynamicTrapsCount
        int trapsToPose = _data.MaxDynamicTrapsCount;
        Debug.Log($"[ProprietaireAI] Mode Furieux — pose {trapsToPose} pièges dynamiques");

        // TODO V2 : logique pose pièges en urgence
        // for (int i = 0; i < trapsToPose; i++) { ... }

        yield break;
    }

    // ================================================================
    // EVENT HANDLERS
    // ================================================================

    private void OnParanoiaChanged(OnParanoiaChanged e)
    {
        _currentParanoia = e.NewValue;
        _currentTier     = e.NewTier;

        // Transitions automatiques selon le palier
        switch (_currentState)
        {
            case ProprietaireState.Idle when e.NewTier >= 1:
                EnterState(ProprietaireState.Alert);
                break;

            case ProprietaireState.Alert when e.NewTier >= 2:
                EnterState(ProprietaireState.Investigate);
                break;

            case ProprietaireState.Investigate when e.NewTier >= 3:
                EnterState(ProprietaireState.Confront);
                break;

            case ProprietaireState.Confront when e.NewTier >= 4:
                EnterState(ProprietaireState.Panic);
                break;

            case ProprietaireState.Panic when e.NewTier >= 5:
                EnterState(ProprietaireState.Furious);
                break;
        }

        // Appel avocat si seuil franchi (Sociability >= 7 seulement)
        if (_data.AutoCallsLawyer && 
            e.NewValue >= _data.LawyerCallThreshold && 
            e.OldValue < _data.LawyerCallThreshold)
        {
            Debug.Log($"[ProprietaireAI] Seuil avocat franchi : {e.NewValue:F0}");
            // TODO V2 : EventBus<OnLawyerCalled>.Raise()
        }
    }

    private void OnNoiseEmitted(OnNoiseEmitted e)
    {
        // Ignore les bruits silencieux
        if (e.Level == NiveauBruit.Silencieux) return;

        float dist = Vector3.Distance(transform.position, e.Position);
        float hearingRange = e.Range + _data.HearingBonus;

        if (dist <= hearingRange)
        {
            _lastNoisePosition = e.Position;
            
            if (_currentState == ProprietaireState.Idle)
                EnterState(ProprietaireState.Alert);
        }
    }

    private void OnThresholdReached(OnThresholdReached e)
    {
        // À 20% du quota : peut sortir vers le véhicule (palier 3+ requis)
        if (e.Percentage >= 0.20f && _currentTier >= 3)
        {
            if (_currentState != ProprietaireState.Outdoor && 
                _currentState != ProprietaireState.Locked)
            {
                EnterState(ProprietaireState.Outdoor);
            }
        }

        // À 80% : devient Furieux automatiquement
        if (e.Percentage >= 0.80f)
        {
            EnterState(ProprietaireState.Furious);
        }
    }

    private void OnMissionStarted(OnMissionStarted e)
    {
        _data              = e.Mission.Owner;
        _currentParanoia   = _data.StartingParanoia;
        _currentTier       = CalculateParanoiaTier(_currentParanoia);
        
        // Applique les vitesses calculées
        _agent.speed = _data.NormalSpeed;
        
        EnterState(ProprietaireState.Idle);
    }

    // ================================================================
    // VISION DÉTECTION
    // ================================================================

    private void CheckPlayerVision()
    {
        if (_player == null) return;

        Vector3 dirToPlayer = (_player.position - transform.position).normalized;
        float   dist        = Vector3.Distance(transform.position, _player.position);

        // Hors de portée — ignore
        if (dist > _data.VisionRange) 
        {
            _lastPlayerSeenTime = -999f;
            return;
        }

        // Calcule l'angle entre forward et direction joueur
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        
        // Hors du cône de vision — ignore
        if (angle > _data.VisionAngle) 
        {
            _lastPlayerSeenTime = -999f;
            return;
        }

        // Raycast pour obstacles
        Vector3 origin = transform.position + Vector3.up;
        if (Physics.Raycast(origin, dirToPlayer, out RaycastHit hit, dist))
        {
            if (hit.transform != _player)
            {
                _lastPlayerSeenTime = -999f;
                return;
            }
        }

        // Joueur visible — marque le temps
        _lastPlayerSeenTime = Time.time;

        // Transition si vue confirmée depuis VISION_COYOTE secondes
        if (Time.time - _lastPlayerSeenTime >= VISION_COYOTE)
        {
            if (_currentState == ProprietaireState.Idle || 
                _currentState == ProprietaireState.Investigate)
            {
                EnterState(ProprietaireState.Confront);
            }
        }
    }

    // ================================================================
    // AUDIO
    // ================================================================

    private void PlaySound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || _audio == null) return;
        
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            _audio.PlayOneShot(clip);
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private int CalculateParanoiaTier(float paranoia)
    {
        if (paranoia >= ProprietaireData.PALIER_OBSESSION) return 5;
        if (paranoia >= ProprietaireData.PALIER_FURIEUX)   return 4;
        if (paranoia >= ProprietaireData.PALIER_PANIQUE)   return 3;
        if (paranoia >= ProprietaireData.PALIER_INQUIET)   return 2;
        if (paranoia >= ProprietaireData.PALIER_MEFIANT)   return 1;
        return 0;
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    /// <summary>Force l'immobilisation (menottes, enfermement)</summary>
    public void Immobilize() => EnterState(ProprietaireState.Locked);

    /// <summary>Injecte les références player/vehicle après spawn</summary>
    public void SetReferences(Transform player, Transform vehicle)
    {
        _player  = player;
        _vehicle = vehicle;
    }

    // ================================================================
    // PROPERTIES
    // ================================================================

    public ProprietaireState CurrentState => _currentState;
    public float CurrentParanoia          => _currentParanoia;
    public int   CurrentTier              => _currentTier;
}