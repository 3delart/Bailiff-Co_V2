// ============================================================
// VehicleRuntime.cs — Bailiff & Co  V2
// Remplace Vehicule.cs. Coffre (porte animée + zone trigger),
// porte conducteur, cage animaux. Sur le root du prefab véhicule.
//
// CHANGEMENTS V2 :
//   - Vehicule → VehicleRuntime (anglais cohérent)
//   - VehiculeDef → VehiculeData (nouveau nom SO)
//   - FUSION de ZoneCoffreTrigger.cs (micro-script supprimé) :
//     la zone trigger est gérée directement via [SerializeField]
//     _trunkZoneCollider + OnTriggerEnter/Exit locaux.
//   - Suppression des 3 FindObjectOfType dans Awake() →
//     systèmes injectés par [SerializeField] ou via EventBus
//   - OnVehiculeAttaque → OnVehicleAttacked
//   - OnDemandeFinMission → OnMissionEndRequested
//   - OnConfirmationDepart → OnDepartureConfirmed
//   - OnObjetCharge → OnObjectLoaded (via ValueObject.LoadIntoVehicle)
//   - OnFondNoir → OnFadeToBlack
//   - ObjetValeur → ValueObject
//
// ARCHITECTURE :
//   - La zone trigger coffre est sur un enfant "TrunkZone"
//     (BoxCollider IsTrigger). Ce script écoute directement ses
//     OnTriggerEnter/Exit via un composant VehicleTrunkZone dédié
//     qui relaie les appels (voir VehicleTrunkZone.cs).
//   - Les objets dans _objectsInZone sont "dans le coffre"
//     visuellement ; ils sont convertis en quota UNIQUEMENT
//     au moment où le joueur confirme le départ.
//   - ProprietaireAI / VoisinSystem appellent TakeRandom()
//     pour voler un objet.
//   - La porte conducteur émet OnMissionEndRequested ;
//     HUDSystem affiche le popup de confirmation.
//
// SETUP UNITY :
//   Prefab véhicule (root) :
//   ├── VehicleRuntime.cs
//   ├── VehicleAmbiance.cs
//   ├── [MeshRenderer / children]
//   ├── TrunkDoor       ← Transform animé → _trunkDoor
//   ├── TrunkZone       ← BoxCollider IsTrigger + VehicleTrunkZone.cs
//   │   └── assigner _trunkZoneCollider ici
//   ├── CageDoor        ← Transform animé cage (optionnel)
//   ├── ColliderTrunkDoor   ← Collider cliquable coffre
//   ├── ColliderDriverDoor  ← Collider cliquable porte conducteur
//   └── ColliderCage        ← Collider cliquable cage (optionnel)
// ============================================================
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BailiffCo;

public class VehicleRuntime : MonoBehaviour, IInteractable
{
    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Configuration")]
    [SerializeField] private VehiculeData _data;

    [Header("Porte conducteur")]
    [SerializeField] private Collider _driverDoorCollider;

    [Header("Coffre — porte animée")]
    [SerializeField] private Transform _trunkDoor;
    [SerializeField] private Vector3   _trunkRotationAxis    = Vector3.right;
    [SerializeField] private float     _trunkOpenAngle       = 90f;
    [SerializeField] private float     _trunkAnimDuration    = 0.4f;

    [Header("Coffre — multi-zone (trunk + trailers)")]
    [SerializeField] private List<TrunkZone> _zones = new();
    [SerializeField] private Transform _trailerAnchor;
    [SerializeField] private Collider _trunkZoneCollider;
    [SerializeField] private Collider _trunkDoorCollider;

    [Header("Cage à animaux (optionnelle)")]
    [SerializeField] private Transform _cageDoor;
    [SerializeField] private Collider  _cageCollider;
    [SerializeField] private float     _cageAnimDuration = 0.3f;

    [Header("Références systèmes (injectées — plus de FindObjectOfType)")]
    [SerializeField] private MissionSystem _missionSystem;
    [SerializeField] private PlayerCarry   _playerCarry;
    [SerializeField] private QuotaSystem   _quotaSystem;

    [Header("État (lecture seule dans l'Inspector)")]
    [SerializeField] private bool _trunkOpen    = false;
    [SerializeField] private bool _cageOpen     = false;
    [SerializeField] private bool _antiTheft    = false;
    [SerializeField] private int  _loadedCount  = 0;  // compteur définitif après départ

    // ================================================================
    // ÉTAT PRIVÉ
    // ================================================================

    // Trailers attachées dynamiquement
    private readonly List<TrunkZone> _attachedTrailers = new();

    // Objets actuellement dans la zone trigger du coffre
    private readonly HashSet<ValueObject> _objectsInZone  = new();

    // Removals en attente de debounce (rebonds physiques)
    private readonly HashSet<ValueObject> _pendingRemoval = new();

    // Flags d'animation — un par porte pour éviter les conflits
    private bool _trunkMoving = false;
    private bool _cageMoving  = false;

    // Cage
    private bool        _animalInCage     = false;
    private ValueObject _animalInCage_ref = null;

    // Rotation initiale des portes (mémorisée en Awake)
    private Quaternion _trunkClosedRotation;
    private Quaternion _cageClosedRotation;

    // Collider actuellement visé par le rayon du joueur
    private Collider _targetCollider;

    // État du popup de confirmation départ
    private bool _confirmationPending = false;


    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        AutoFindRefs();

        if (_driverDoorCollider == null)
            Debug.LogWarning("[VehicleRuntime] Porte conducteur introuvable — renomme l'enfant 'ColliderDriverDoor' ou assigne-le dans l'Inspector.");
        if (_trunkDoorCollider == null)
            Debug.LogWarning("[VehicleRuntime] Collider coffre introuvable — renomme l'enfant 'ColliderTrunkDoor' ou assigne-le dans l'Inspector.");

        // Zone trigger désactivée par défaut (coffre fermé)
        if (_trunkZoneCollider != null)
            _trunkZoneCollider.enabled = false;

        // Mémorise les rotations initiales
        if (_trunkDoor != null) _trunkClosedRotation = _trunkDoor.localRotation;
        if (_cageDoor  != null) _cageClosedRotation  = _cageDoor.localRotation;
    }

    private void AutoFindRefs()
    {
        // Porte conducteur — fallback sur ancien nom français si nécessaire
        if (_driverDoorCollider == null)
            _driverDoorCollider = FindChildCollider("ColliderDriverDoor")
                               ?? FindChildCollider("PorteConducteur");
        if (_trunkDoorCollider  == null) _trunkDoorCollider  = FindChildCollider("ColliderTrunkDoor");
        if (_cageCollider       == null) _cageCollider       = FindChildCollider("ColliderCage");
        if (_trunkZoneCollider  == null) _trunkZoneCollider  = FindChildCollider("TrunkZone");
        if (_trunkDoor          == null) _trunkDoor          = transform.Find("TrunkDoor");

        // Partage VehiculeData depuis VehicleHubSlot si non assigné dans l'Inspector
        if (_data == null)
        {
            var slot = GetComponent<VehicleHubSlot>();
            if (slot != null) _data = slot.Data;
        }
    }

    private Collider FindChildCollider(string childName)
    {
        var t = transform.Find(childName);
        return t != null ? t.GetComponent<Collider>() : null;
    }

#if UNITY_EDITOR
    private void Reset()      => AutoFindRefs();
    private void OnValidate() => AutoFindRefs();
#endif

    // ================================================================
    // PROPRIÉTÉS — ZONES MULTI-COFFRE
    // ================================================================

    /// <summary>All zones (trunk + attached trailers).</summary>
    private IEnumerable<TrunkZone> AllZones => _zones.Concat(_attachedTrailers);

    private void OnEnable()
    {
        EventBus<OnVehicleAttacked>.Subscribe(OnVehicleAttackedEvent);
        EventBus<OnDepartureConfirmed>.Subscribe(OnDepartureConfirmed);
    }

    private void OnDisable()
    {
        EventBus<OnVehicleAttacked>.Unsubscribe(OnVehicleAttackedEvent);
        EventBus<OnDepartureConfirmed>.Unsubscribe(OnDepartureConfirmed);
    }

    // ================================================================
    // ZONE TRIGGER COFFRE — fusionné depuis ZoneCoffreTrigger.cs
    // Le collider "TrunkZone" a ce GameObject comme parent ou utilise
    // VehicleTrunkZone.cs pour relayer. Les deux patterns fonctionnent.
    // ================================================================

    /// <summary>
    /// Appelé par VehicleTrunkZone (enfant) quand un ValueObject entre dans la zone.
    /// Peut aussi être appelé directement si le collider est sur ce même GameObject.
    /// </summary>
    public void OnObjectEnteredTrunk(ValueObject obj)
    {
        if (!_trunkOpen) return;
        _pendingRemoval.Remove(obj); // Annule un removal en attente (rebond physique)
        if (_objectsInZone.Add(obj))
            EmitTrunkPreview();
    }

    /// <summary>
    /// Appelé par VehicleTrunkZone (enfant) quand un ValueObject quitte la zone.
    /// Utilise un debounce de 2 FixedUpdates pour ignorer les rebonds physiques
    /// transitoires (ex: objet déposé qui heurte un autre et sort brièvement).
    /// </summary>
    public void OnObjectLeftTrunk(ValueObject obj)
    {
        // Ignorer les exits causés par la désactivation du collider (CloseTrunk).
        if (!_trunkOpen) return;
        if (!_objectsInZone.Contains(obj)) return;
        _pendingRemoval.Add(obj);
        StartCoroutine(DelayedRemove(obj));
    }

    private IEnumerator DelayedRemove(ValueObject obj)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        // Si l'objet est re-entré entre-temps, _pendingRemoval.Remove retourne false → annulé
        if (!_pendingRemoval.Remove(obj)) yield break;
        if (_objectsInZone.Remove(obj))
            EmitTrunkPreview();
    }

    private void EmitTrunkPreview()
    {
        float preview = 0f;
        foreach (var o in _objectsInZone) preview += o.ActualValue;

        float target = _quotaSystem != null ? _quotaSystem.TargetValue : 0f;
        EventBus<OnQuotaChanged>.Raise(new OnQuotaChanged
        {
            TotalValue  = preview,
            TargetValue = target,
            Percentage  = target > 0f ? preview / target : 0f
        });
    }

    // ================================================================
    // IINTERACTABLE
    // ================================================================

    /// <summary>
    /// Appelé par PlayerInteractor pour indiquer quel collider enfant est visé.
    /// Nécessaire car le véhicule a plusieurs zones d'interaction.
    /// </summary>
    public void SetTargetCollider(Collider col) => _targetCollider = col;

    public bool CanInteract(GameObject interactor)
    {
        if (_targetCollider == _driverDoorCollider)
            return true;

        if (_targetCollider == _cageCollider && _cageDoor != null)
            return !_cageMoving;

        if (_targetCollider == _trunkDoorCollider)
        {
            if (_trunkMoving) return false;
            if (interactor.TryGetComponent<PlayerCarry>(out var carry) && carry.EstEnTrain)
                return false;
            return true;
        }

        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (_targetCollider == _trunkDoorCollider)
        {
            if (_trunkOpen) CloseTrunk();
            else
            {
                // Alarm check before opening trunk
                if (_antiTheft)
                {
                    PlayAlarmSound();
                    EventBus<OnVehicleAlarmTriggered>.Raise(new OnVehicleAlarmTriggered { Vehicle = this });
                    return; // Block access
                }
                OpenTrunk();
            }
        }
        else if (_targetCollider == _driverDoorCollider)
        {
            RequestDepartureConfirmation();
        }
        else if (_targetCollider == _cageCollider && _cageDoor != null)
        {
            if (interactor.TryGetComponent<PlayerCarry>(out var carry) && carry.EstEnTrain)
                PlaceAnimalInCage(carry);
            else if (_cageOpen) CloseCage();
            else                OpenCage();
        }
    }

    public string GetInteractionLabel()
    {
        bool handsOccupied = _playerCarry != null && _playerCarry.EstEnTrain;

        if (_targetCollider == _trunkDoorCollider)
        {
            if (handsOccupied) return "Pose l'objet d'abord";
            return GetTrunkLabel();
        }

        if (_targetCollider == _driverDoorCollider)
            return GetDriverDoorLabel();

        if (_targetCollider == _cageCollider && _cageDoor != null)
            return GetCageLabel(depositPossible: handsOccupied);

        return "";
    }

    // ================================================================
    // ALARM SYSTEM
    // ================================================================

    private void PlayAlarmSound()
    {
        if (_data?.AlarmSound != null)
        {
            // Assume there's an AudioSource on this GameObject or parent
            var audioSource = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(_data.AlarmSound);
            }
        }
    }

    // ================================================================
    // COFFRE
    // ================================================================

    public void OpenTrunk()
    {
        if (_trunkOpen || _trunkMoving) return;
        _trunkOpen = true;

        // Active la zone trigger : les ValueObject peuvent entrer
        if (_trunkZoneCollider != null)
            _trunkZoneCollider.enabled = true;

        if (_trunkDoor != null)
            StartCoroutine(AnimateTrunkDoor(opening: true));
    }

    public void CloseTrunk()
    {
        if (!_trunkOpen || _trunkMoving) return;
        _trunkOpen = false;

        if (_trunkZoneCollider != null)
            _trunkZoneCollider.enabled = false;

        if (_trunkDoor != null)
            StartCoroutine(AnimateTrunkDoor(opening: false));
    }

    // ================================================================
    // CAGE À ANIMAUX
    // ================================================================

    private void OpenCage()
    {
        if (_cageOpen || _cageMoving) return;
        _cageOpen = true;
        StartCoroutine(AnimateCageDoor(opening: true));
    }

    private void CloseCage()
    {
        if (!_cageOpen || _cageMoving) return;
        _cageOpen = false;
        StartCoroutine(AnimateCageDoor(opening: false));
    }

    private void PlaceAnimalInCage(PlayerCarry carry)
    {
        if (_animalInCage)
        {
            Debug.Log("[VehicleRuntime] La cage est déjà occupée.");
            return;
        }

        if (!_cageOpen) OpenCage();

        _animalInCage_ref = carry.ObjetEnMain;
        carry.Poser(doux: true);
        _animalInCage = true;

        StartCoroutine(CloseCageAfterDelay(0.5f));
    }

    private IEnumerator CloseCageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseCage();
    }

    // ================================================================
    // DÉPART — popup de confirmation via EventBus
    // ================================================================

    private void RequestDepartureConfirmation()
    {
        if (_confirmationPending) return;
        _confirmationPending = true;

        // Rafraîchit les valeurs quota AVANT d'ouvrir le panel de confirmation
        EmitTrunkPreview();
        EventBus<OnMissionEndRequested>.Raise(new OnMissionEndRequested());
    }

    private void OnDepartureConfirmed(OnDepartureConfirmed e)
    {
        _confirmationPending = false;
        if (!e.Confirmed) return;

        // Ordre garanti : charger les objets AVANT qu'EndMission lise QuotaSystem.TotalValue
        ConvertObjectsToQuota();           // 1. Synchrone → QuotaSystem à jour
        _missionSystem?.EndMission(true);  // 2. Calcule MissionResult avec les bonnes valeurs
        StartCoroutine(FadeCoroutine());   // 3. Fondu noir (async)
    }

    private IEnumerator FadeCoroutine()
    {
        EventBus<OnFadeToBlack>.Raise(new OnFadeToBlack { DurationSeconds = 1f });
        yield return new WaitForSeconds(1f);
        // SceneLoader (re-abonné après ClearAll) gère le retour Hub via OnMissionEnded
    }

    // ================================================================
    // CONVERSION DES OBJETS AU DÉPART
    // ================================================================

    private void ConvertObjectsToQuota()
    {
        _loadedCount = 0;

        // Calculate total available surface
        float totalSurface = AllZones.Sum(z => z.SurfaceM2);
        float remainingSurface = totalSurface;

        // Load objects from all zones
        foreach (var zone in AllZones)
        {
            var objectsInZone = zone.ObjectsInZone.ToList();
            foreach (var obj in objectsInZone)
            {
                // Check if object fits
                if (obj.Data.SurfaceM2 <= remainingSurface)
                {
                    obj.LoadIntoVehicle();
                    remainingSurface -= obj.Data.SurfaceM2;
                    _loadedCount++;
                }
            }
        }
    }

    // ================================================================
    // VOLER UN OBJET — appelé par ProprietaireAI ou VoisinSystem
    // ================================================================

    /// <summary>
    /// Retire et retourne un objet aléatoire du coffre.
    /// Retourne null si coffre vide ou protégé par antivol.
    /// </summary>
    public ValueObject TakeRandom()
    {
        if (_antiTheft) return null;
        if (_objectsInZone.Count == 0) return null;

        int index  = Random.Range(0, _objectsInZone.Count);
        ValueObject target = null;
        int i = 0;
        foreach (var obj in _objectsInZone)
        {
            if (i == index) { target = obj; break; }
            i++;
        }

        if (target != null)
        {
            _objectsInZone.Remove(target);
            EventBus<OnOwnerRetrievedObject>.Raise(new OnOwnerRetrievedObject
            {
                Object = target.Data,
                Value  = target.ActualValue
            });
        }

        return target;
    }

    // ================================================================
    // ANTIVOL CONSOMMABLE
    // ================================================================

    public void ActivateAntiTheft(float durationSeconds = 0f)
    {
        _antiTheft = true;
        if (durationSeconds > 0f)
            StartCoroutine(DeactivateAntiTheftAfter(durationSeconds));
    }

    private IEnumerator DeactivateAntiTheftAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        _antiTheft = false;
    }

    // ================================================================
    // ANIMATIONS — flags séparés par porte
    // ================================================================

    private IEnumerator AnimateTrunkDoor(bool opening)
    {
        _trunkMoving = true;

        Quaternion start = _trunkDoor.localRotation;
        Quaternion end   = opening
            ? _trunkClosedRotation * Quaternion.AngleAxis(_trunkOpenAngle, _trunkRotationAxis)
            : _trunkClosedRotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _trunkAnimDuration;
            _trunkDoor.localRotation = Quaternion.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        _trunkDoor.localRotation = end;
        _trunkMoving = false;
    }

    private IEnumerator AnimateCageDoor(bool opening)
    {
        _cageMoving = true;

        Quaternion start = _cageDoor.localRotation;
        Quaternion end   = opening
            ? _cageClosedRotation * Quaternion.AngleAxis(90f, Vector3.up)
            : _cageClosedRotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _cageAnimDuration;
            _cageDoor.localRotation = Quaternion.Lerp(start, end, Mathf.Clamp01(t));
            yield return null;
        }

        _cageDoor.localRotation = end;
        _cageMoving = false;
    }

    // ================================================================
    // LABELS
    // ================================================================

    private string GetTrunkLabel()
    {
        if (_trunkMoving) return "...";
        return _trunkOpen
            ? $"Fermer le coffre ({_objectsInZone.Count} objet(s))"
            : "Ouvrir le coffre";
    }

    private string GetDriverDoorLabel()
    {
        float preview = GetTrunkPreviewValue();
        float target  = _quotaSystem != null ? _quotaSystem.TargetValue : 0f;
        bool  reached = target > 0f && preview >= target;

        return reached
            ? $"Partir ✓ — {preview:N0} €"
            : $"Partir — {preview:N0} / {target:N0} €";
    }

    private float GetTrunkPreviewValue()
    {
        float total = 0f;
        foreach (var o in _objectsInZone) total += o.ActualValue;
        return total;
    }

    private string GetCageLabel(bool depositPossible)
    {
        if (_animalInCage)
            return $"Cage occupée — {_animalInCage_ref?.Data?.ObjectName ?? "Animal"}";
        if (depositPossible)
            return "Déposer l'animal dans la cage";
        return _cageOpen ? "Fermer la cage" : "Ouvrir la cage";
    }

    // ================================================================
    // EVENTS
    // ================================================================

    private void OnVehicleAttackedEvent(OnVehicleAttacked e)
    {
        // Notification HUD future.
        // ProprietaireAI / VoisinSystem appellent TakeRandom() directement.
    }

    // ================================================================
    // VEHICLE OPTIONS — TRAILERS & ANTI-THEFT
    // ================================================================

    private void ApplyVehicleOptions()
    {
        var options = GameManager.Instance?.OptionsSelectionnees;
        if (options == null || options.Count == 0) return;

        foreach (var option in options)
        {
            switch (option.Type)
            {
                case VehicleOptionType.Remorque:
                    AttachTrailer(option);
                    break;
                case VehicleOptionType.AlarmeAntivol:
                    _antiTheft = true;
                    break;
            }
        }
    }

    private void AttachTrailer(VehicleOption option)
    {
        if (option.TrailerPrefab == null) return;

        // Instantiate trailer
        var trailerObj = Instantiate(option.TrailerPrefab, _trailerAnchor ?? transform);
        trailerObj.transform.SetParent(transform);
        trailerObj.transform.localPosition = Vector3.zero;

        // Get TrunkZone component and register
        if (trailerObj.TryGetComponent<TrunkZone>(out var trailerZone))
        {
            _attachedTrailers.Add(trailerZone);
        }
    }

    // ================================================================
    // API PUBLIQUE — injection tardive (appelée par MissionBuilder)
    // ================================================================

    /// <summary>
    /// Injecte les références systèmes depuis MissionBuilder après le spawn.
    /// Évite les FindObjectOfType au démarrage.
    /// </summary>
    public void InjectDependencies(MissionSystem missionSys,
                                   PlayerCarry   playerCarry,
                                   QuotaSystem   quotaSys)
    {
        _missionSystem = missionSys;
        _playerCarry   = playerCarry;
        _quotaSystem   = quotaSys;

        // Apply vehicle options after dependencies are injected
        ApplyVehicleOptions();
    }

    // ================================================================
    // PROPRIÉTÉS PUBLIQUES
    // ================================================================

    public bool IsFull        => AllZones.Sum(z => z.UsedSurface) >= AllZones.Sum(z => z.SurfaceM2);
    public bool TrunkOpen     => _trunkOpen;
    public bool CageOpen      => _cageOpen;
    public bool AnimalInCage  => _animalInCage;
    public bool AntiTheft     => _antiTheft;
    public int  ObjectsInTrunk => _objectsInZone.Count;

    /// <summary>Objets physiquement présents dans la zone coffre (lecture seule).</summary>
    public IReadOnlyCollection<ValueObject> ObjectsInTrunkZone => _objectsInZone;

    /// <summary>Coffre ouvert, sans antivol, et avec des objets accessibles.</summary>
    public bool TrunkAccessible => _trunkOpen && !_antiTheft && _objectsInZone.Count > 0;
}