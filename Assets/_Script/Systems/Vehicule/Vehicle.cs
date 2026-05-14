// ============================================================
// Vehicle.cs — Bailiff & Co  V2
// Fusion de VehicleRuntime + VehicleHubSlot + VehicleAmbiance.
// Gère : zones (coffre, sièges, remorques), départ, hub, ambiance.
// Les portes sont des OpenableInteractable séparés — pas de gestion ici.
//
// ARCHITECTURE :
//   - Zones auto-découvertes via GetComponentsInChildren<VehicleTrunkZone>.
//   - Zones TOUJOURS actives — joueur peut lancer objet sans ouvrir.
//   - PorteConducteur.OnOpened → Vehicle.RequestDeparture()
//   - PorteCoffre.Lock() appelé si anti-theft activé.
//   - OpenTrunk() appelé par ProprietaireAI avant TakeRandom().
// ============================================================
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using BailiffCo;
using BailiffCo.Hub;

[RequireComponent(typeof(AudioSource))]
public class Vehicle : MonoBehaviour, IInteractable
{
    // ================================================================
    // CONFIGURATION
    // ================================================================

    [Header("Data")]
    [SerializeField] private VehiculeData _data;

    [Header("Hub")]
    [SerializeField] private bool        _available           = true;
    [SerializeField] private TextMeshPro _labelText;
    [SerializeField] private float       _labelHeight         = 2f;
    [SerializeField] private Renderer    _unavailableRenderer;
    [SerializeField] private Material    _unavailableMaterial;

    [Header("Remorque")]
    [SerializeField] private Transform _trailerAnchor;

    [Header("Anti-theft — coffre (optionnel)")]
    [Tooltip("OpenableInteractable du coffre — sera Lock() quand anti-theft actif")]
    [SerializeField] private OpenableInteractable _trunkDoor;

    // ================================================================
    // ÉTAT PRIVÉ
    // ================================================================

    private AudioSource                     _audioSource;
    private List<VehicleTrunkZone>          _zones           = new();
    private readonly List<VehicleTrunkZone> _attachedTrailers = new();
    private bool                            _antiTheft;
    private bool                            _confirmationPending;
    private int                             _loadedCount;
    private bool                            _animalInCage;
    private ValueObject                     _animalInCage_ref;

    // Injectés par MissionBuilder — pas dans l'Inspector
    private MissionSystem _missionSystem;
    private PlayerCarry   _playerCarry;
    private QuotaSystem   _quotaSystem;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _zones = new List<VehicleTrunkZone>(GetComponentsInChildren<VehicleTrunkZone>(true));
        UpdateLabel();
    }

    private void Start()
    {
        if (_data?.SpecialSounds != null && _data.SpecialSounds.Length > 0)
            StartCoroutine(AmbianceLoop());
    }

    private void Update()
    {
        if (_labelText != null && Camera.main != null)
        {
            _labelText.transform.LookAt(
                _labelText.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
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
    // IINTERACTABLE — Hub + Mission
    // ================================================================

    public bool CanInteract(GameObject interactor)
    {
        var ctx = GameManager.Instance?.ContexteActuel;
        if (ctx == ContexteJeu.Hub)     return _data != null && _available;
        if (ctx == ContexteJeu.Mission) return true;
        return false;
    }

    public void Interact(GameObject interactor)
    {
        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Hub)
            HubManager.Instance?.DemanderLocationVehicule(_data, _data?.RentalPrice ?? 0f);
        else
            RequestDeparture();
    }

    public string GetInteractionLabel()
    {
        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Mission)
            return $"[E] Quitter la mission";

        if (!_available) return $"{_data?.VehicleName ?? "Véhicule"} — Indisponible";
        float balance = GameManager.Instance?.Argent ?? 0f;
        float price   = _data?.RentalPrice ?? 0f;
        string priceStr = price <= 0f ? "Gratuit" : $"{price:N0} €/mission";
        if (balance < price && price > 0f)
            return $"{_data.VehicleName} ({priceStr}) — [E] Louer  ⚠ Fonds insuffisants";
        return $"{_data.VehicleName} ({priceStr}) — [E] Louer";
    }

    // ================================================================
    // DÉPART — appelé via OpenableInteractable.OnOpened de PorteConducteur
    // ================================================================

    public void RequestDeparture()
    {
        if (_confirmationPending) return;
        _confirmationPending = true;

        float preview = AllZones.SelectMany(z => z.ObjectsInZone).Sum(o => o.ActualValue);
        float target  = _quotaSystem != null ? _quotaSystem.TargetValue : 0f;
        EventBus<OnQuotaChanged>.Raise(new OnQuotaChanged
        {
            TotalValue  = preview,
            TargetValue = target,
            Percentage  = target > 0f ? preview / target : 0f
        });
        EventBus<OnMissionEndRequested>.Raise(new OnMissionEndRequested());
    }

    private void OnDepartureConfirmed(OnDepartureConfirmed e)
    {
        _confirmationPending = false;
        if (!e.Confirmed) return;
        ConvertObjectsToQuota();
        _missionSystem?.EndMission(true);
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        EventBus<OnFadeToBlack>.Raise(new OnFadeToBlack { DurationSeconds = 1f });
        yield return new WaitForSeconds(1f);
    }

    // ================================================================
    // ZONES
    // ================================================================

    private IEnumerable<VehicleTrunkZone> AllZones => _zones.Concat(_attachedTrailers);

    private void ConvertObjectsToQuota()
    {
        _loadedCount = 0;
        float remaining = AllZones.Sum(z => z.SurfaceM2);
        foreach (var zone in AllZones)
        {
            foreach (var obj in zone.ObjectsInZone.ToList())
            {
                if (obj.Data.SurfaceM2 <= remaining)
                {
                    obj.LoadIntoVehicle();
                    remaining -= obj.Data.SurfaceM2;
                    _loadedCount++;
                }
            }
        }
    }

    // ================================================================
    // CAGE À ANIMAUX
    // ================================================================

    public void PlaceAnimalInCage(PlayerCarry carry)
    {
        if (_animalInCage) return;
        _animalInCage_ref = carry.ObjetEnMain;
        carry.Poser(doux: true);
        _animalInCage = true;
    }

    // ================================================================
    // OPTIONS VÉHICULE
    // ================================================================

    private void ApplyVehicleOptions()
    {
        var options = GameManager.Instance?.OptionsSelectionnees;
        if (options == null) return;
        foreach (var option in options)
        {
            switch (option.Type)
            {
                case VehicleOptionType.Remorque:
                    AttachTrailer(option);
                    break;
                case VehicleOptionType.AlarmeAntivol:
                    _antiTheft = true;
                    _trunkDoor?.Lock();
                    break;
            }
        }
    }

    public void OpenTrunk()
    {
        if (_antiTheft) return;
        _trunkDoor?.ForceOpen();
    }

    private void AttachTrailer(VehicleOption option)
    {
        if (option.TrailerPrefab == null) return;
        var anchor = _trailerAnchor != null ? _trailerAnchor : transform;
        var trailerObj = Instantiate(option.TrailerPrefab, anchor.position, anchor.rotation, transform);
        _attachedTrailers.AddRange(trailerObj.GetComponentsInChildren<VehicleTrunkZone>(true));
    }

    // ================================================================
    // ANTIVOL
    // ================================================================

    public void ActivateAntiTheft(float durationSeconds = 0f)
    {
        _antiTheft = true;
        if (durationSeconds > 0f) StartCoroutine(DeactivateAfter(durationSeconds));
    }

    private IEnumerator DeactivateAfter(float d) { yield return new WaitForSeconds(d); _antiTheft = false; }

    // ================================================================
    // AMBIANCE
    // ================================================================

    private IEnumerator AmbianceLoop()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(
            _data.SpecialSoundIntervalMin * 0.5f,
            _data.SpecialSoundIntervalMax * 0.5f));

        while (true)
        {
            var clip = _data.SpecialSounds[UnityEngine.Random.Range(0, _data.SpecialSounds.Length)];
            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
                {
                    Position = transform.position,
                    Range    = _data.SpecialSoundNoiseRange,
                    Level    = NiveauBruit.Fort,
                    Source   = gameObject
                });
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(
                _data.SpecialSoundIntervalMin,
                _data.SpecialSoundIntervalMax));
        }
    }

    public void StopAmbiance()
    {
        StopAllCoroutines();
        if (_audioSource.isPlaying) _audioSource.Stop();
    }

    // ================================================================
    // HUB — label flottant
    // ================================================================

    private void UpdateLabel()
    {
        if (_labelText == null || _data == null) return;
        _labelText.transform.localPosition = Vector3.up * _labelHeight;
        float price = _data.RentalPrice;
        string priceStr = price <= 0f ? "Gratuit" : $"{price:N0} €/mission";
        _labelText.text  = $"{_data.VehicleName}\n<size=70%>{priceStr}</size>";
        _labelText.color = _available ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    public void SetAvailable(bool available)
    {
        _available = available;
        if (!available && _unavailableRenderer != null && _unavailableMaterial != null)
            _unavailableRenderer.material = _unavailableMaterial;
        UpdateLabel();
    }

    // ================================================================
    // EVENTS
    // ================================================================

    private void OnVehicleAttackedEvent(OnVehicleAttacked e) { }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    public void InjectDependencies(MissionSystem missionSys, PlayerCarry playerCarry, QuotaSystem quotaSys)
    {
        _missionSystem = missionSys;
        _playerCarry   = playerCarry;
        _quotaSystem   = quotaSys;
        ApplyVehicleOptions();
    }

    public ValueObject TakeRandom()
    {
        if (_antiTheft) return null;
        var all = AllZones.SelectMany(z => z.ObjectsInZone).ToList();
        if (all.Count == 0) return null;
        var obj = all[UnityEngine.Random.Range(0, all.Count)];
        foreach (var zone in AllZones) zone.RemoveObject(obj);
        EventBus<OnOwnerRetrievedObject>.Raise(new OnOwnerRetrievedObject { Object = obj.Data, Value = obj.ActualValue });
        return obj;
    }

    public bool  IsFull         => AllZones.Sum(z => z.UsedSurface) >= AllZones.Sum(z => z.SurfaceM2);
    public bool  AntiTheft      => _antiTheft;
    public bool  AnimalInCage   => _animalInCage;
    public int   ObjectsInTrunk => AllZones.Sum(z => z.ObjectsInZone.Count());
    public IEnumerable<ValueObject> ObjectsInTrunkZone => AllZones.SelectMany(z => z.ObjectsInZone);
    public bool  TrunkAccessible => AllZones.Any(z => z.ObjectsInZone.Any()) && !_antiTheft;

    public VehiculeData Data        => _data;
    public float        RentalPrice => _data?.RentalPrice ?? 0f;
    public bool         Available   => _available;
    public bool         IsFree      => (_data?.RentalPrice ?? 0f) <= 0f;
}
