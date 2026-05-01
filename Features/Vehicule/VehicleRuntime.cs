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
using UnityEngine;

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
    [SerializeField] private Collider  _trunkDoorCollider;
    [SerializeField] private Vector3   _trunkRotationAxis    = Vector3.right;
    [SerializeField] private float     _trunkOpenAngle       = 90f;
    [SerializeField] private float     _trunkAnimDuration    = 0.4f;

    [Header("Coffre — zone trigger (fusionné depuis ZoneCoffreTrigger)")]
    [Tooltip("BoxCollider IsTrigger sur l'enfant TrunkZone. " +
             "Activer/désactiver selon l'état du coffre géré ici directement.")]
    [SerializeField] private Collider _trunkZoneCollider;

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

    // Objets actuellement dans la zone trigger du coffre
    private readonly HashSet<ValueObject> _objectsInZone = new();

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
        // Plus de FindObjectOfType — systèmes injectés par [SerializeField]
        // ou retrouvés depuis la scène via MissionBuilder au spawn

        // Zone trigger désactivée par défaut (coffre fermé)
        if (_trunkZoneCollider != null)
            _trunkZoneCollider.enabled = false;

        // Mémorise les rotations initiales
        if (_trunkDoor != null) _trunkClosedRotation = _trunkDoor.localRotation;
        if (_cageDoor  != null) _cageClosedRotation  = _cageDoor.localRotation;
    }

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
        _objectsInZone.Add(obj);
    }

    /// <summary>
    /// Appelé par VehicleTrunkZone (enfant) quand un ValueObject quitte la zone.
    /// </summary>
    public void OnObjectLeftTrunk(ValueObject obj)
    {
        _objectsInZone.Remove(obj);
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
            else            OpenTrunk();
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

        // Désactive la zone trigger
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

        // HUDSystem écoute et affiche le popup Oui/Non
        EventBus<OnMissionEndRequested>.Raise(new OnMissionEndRequested());
    }

    private void OnDepartureConfirmed(OnDepartureConfirmed e)
    {
        _confirmationPending = false;

        if (e.Confirmed)
            StartCoroutine(DepartureCoroutine());
        // Si refusé : rien à faire, le jeu continue
    }

    private IEnumerator DepartureCoroutine()
    {
        // Convertit les objets présents en quota au moment du départ
        ConvertObjectsToQuota();

        // Fondu noir via EventBus → géré par SceneLoader
        EventBus<OnFadeToBlack>.Raise(new OnFadeToBlack { DurationSeconds = 1f });

        yield return new WaitForSeconds(1f);
        EventBus<OnMissionEndRequested>.Raise(new OnMissionEndRequested());
    }

    // ================================================================
    // CONVERSION DES OBJETS AU DÉPART
    // ================================================================

    private void ConvertObjectsToQuota()
    {
        if (_data == null) return;

        var toLoad = new List<ValueObject>(_objectsInZone);
        foreach (var obj in toLoad)
        {
            if (_loadedCount >= _data.ObjectCapacity) break;
            obj.LoadIntoVehicle(); // → émet OnObjectLoaded, Destroy(gameObject)
            _loadedCount++;
        }
        _objectsInZone.Clear();
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
        if (_quotaSystem == null) return "Partir";
        return _quotaSystem.QuotaReached
            ? $"Partir ✓ — {_quotaSystem.TotalValue:N0} €"
            : $"Partir — {_quotaSystem.TotalValue:N0} / {_quotaSystem.TargetValue:N0} €";
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
    }

    // ================================================================
    // PROPRIÉTÉS PUBLIQUES
    // ================================================================

    public bool IsFull        => _data != null && _loadedCount >= _data.ObjectCapacity;
    public bool TrunkOpen     => _trunkOpen;
    public bool CageOpen      => _cageOpen;
    public bool AnimalInCage  => _animalInCage;
    public bool AntiTheft     => _antiTheft;
    public int  ObjectsInTrunk => _objectsInZone.Count;

    /// <summary>Coffre ouvert, sans antivol, et avec des objets accessibles.</summary>
    public bool TrunkAccessible => _trunkOpen && !_antiTheft && _objectsInZone.Count > 0;
}