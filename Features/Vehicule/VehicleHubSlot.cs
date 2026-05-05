// ============================================================
// VehicleHubSlot.cs — Bailiff & Co  V2
// À mettre sur chaque véhicule dans le parking du Hub.
// Le joueur s'approche de la PORTE du véhicule et appuie E
// → popup de détail avec prix de location + boutons.
//
// CHANGEMENTS V2 :
//   - HubVehicule → VehicleHubSlot (anglais cohérent)
//   - VehiculeDef → VehiculeData (nouveau nom SO)
//   - Suppression du FindObjectOfType<HubCoordinator> → injection via [SerializeField]
//     ou accès via HubManager.Instance si nécessaire
//   - Commentaires en anglais pour cohérence
//
// SETUP UNITY :
//   Prefab véhicule dans le parking :
//   ├── Root (VehicleHubSlot.cs)
//   │   └── Mesh du véhicule
//   └── DoorInteraction (BoxCollider — Layer Interactable)
//       └── Ce collider est ce que le joueur vise avec E
//
//   Dans l'Inspector :
//   - _data          : VehiculeData ScriptableObject
//   - _rentalPrice   : prix de location pour cette mission (€)
//                      0 = gratuit (vélo cargo)
//   - _doorCollider  : le BoxCollider sur la porte
//
// FONCTIONNEMENT :
//   Joueur vise la porte → label contextuel → E
//   → HubManager.RequestVehicleRental()
//   → HubCoordinator.ShowVehiclePanel() avec :
//       Nom | Prix | Capacité | Avantage | Inconvénient | Solde
//       [Louer & Partir] [Annuler]
// ============================================================
using TMPro;
using UnityEngine;
using BailiffCo.Hub;

public class VehicleHubSlot : MonoBehaviour, IInteractable
{
    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Données")]
    [SerializeField] private VehiculeData _data;

    [Header("Location")]
    [Tooltip("Prix de location pour une mission. 0 = gratuit (vélo).")]
    [SerializeField] private float _rentalPrice = 0f;

    [Header("Visuel verrouillé")]
    [Tooltip("Matériau grisé si le véhicule n'est pas disponible (optionnel)")]
    [SerializeField] private Material _unavailableMaterial;
    [SerializeField] private Renderer _renderer;

    [Header("Label flottant (optionnel)")]
    [SerializeField] private TextMeshPro _labelText;
    [SerializeField] private float       _labelHeight = 2f;

    [Header("Références injectées (évite FindObjectOfType)")]


    // ================================================================
    // ÉTAT
    // ================================================================

    // Un véhicule peut être temporairement indisponible
    // (ex : déjà loué par quelqu'un d'autre en coop — futur)
    private bool _available = true;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponentInChildren<Renderer>();

        UpdateLabel();
    }

    private void Update()
    {
        // Billboard label — toujours face à la caméra
        if (_labelText != null && Camera.main != null)
        {
            _labelText.transform.LookAt(
                _labelText.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }
    }

    // ================================================================
    // IINTERACTABLE
    // ================================================================

    public bool CanInteract(GameObject interactor) => _data != null;

    public void Interact(GameObject interactor)
    {
        Debug.Log($"[VehicleHubSlot] Interact appelé — HubManager: {HubManager.Instance != null}");
        Debug.Log($"[VehicleHubSlot] _data: {_data != null} — _available: {_available}");
        
        if (_data == null) return;

        if (!_available)
        {
            ShowError("Ce véhicule n'est pas disponible.");
            return;
        }

        // Délègue au HubManager qui vérifie le solde et affiche le panel
        HubManager.Instance?.DemanderLocationVehicule(_data, _rentalPrice);
    }

    public string GetInteractionLabel()
    {
        if (_data == null) return "Véhicule";

        if (!_available)
            return $"{_data.VehicleName} — Indisponible";

        float balance = GameManager.Instance?.Argent ?? 0f;
        bool  canRent = balance >= _rentalPrice;

        string price = _rentalPrice <= 0f ? "Gratuit" : $"{_rentalPrice:N0} €/mission";

        if (!canRent && _rentalPrice > 0f)
            return $"{_data.VehicleName} ({price}) — [E] Voir détails  ⚠ Fonds insuffisants";

        return $"{_data.VehicleName} ({price}) — [E] Voir détails";
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void UpdateLabel()
    {
        if (_labelText == null || _data == null) return;

        _labelText.transform.localPosition = Vector3.up * _labelHeight;

        string price = _rentalPrice <= 0f ? "Gratuit" : $"{_rentalPrice:N0} €/mission";
        _labelText.text = $"{_data.VehicleName}\n<size=70%>{price} · {_data.ObjectCapacity} objets</size>";

        _labelText.color = _available ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

        // Applique le matériau indisponible si nécessaire
        if (_renderer != null && _unavailableMaterial != null && !_available)
            _renderer.material = _unavailableMaterial;
    }

    private void ShowError(string message)
    {
        // Si UIManager est injecté, l'utilise directement
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AfficherErreur(message);
            return;
        }

        // Sinon cherche via FindObjectOfType (fallback)
        var HubCoordinator = FindObjectOfType<HubCoordinator>();
        if (HubCoordinator != null)
            HubCoordinator.AfficherErreur(message);
        else
            Debug.LogWarning($"[VehicleHubSlot] Erreur : {message}");
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>Rend le véhicule indisponible (ex: coop futur).</summary>
    public void SetAvailable(bool available)
    {
        _available = available;
        UpdateLabel();
    }

    /// <summary>Injection UIManager depuis HubManager au Start() si nécessaire.</summary>
    public void InjectUIManager(UIManager UIManager)
    {
        UIManager.Instance = UIManager;
    }

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    public VehiculeData Data        => _data;
    public float        RentalPrice => _rentalPrice;
    public bool         Available   => _available;
    public bool         IsFree      => _rentalPrice <= 0f;
}