// ============================================================
// MissionLibreConfigUI.cs — Bailiff & Co  V2
// Panel Hub : configuration d'une mission libre procédurale.
// Hérite de UIPanel.
//
// SETUP UNITY (Inspector) :
//   _habitationsDisponibles   → glisser tous les HabitationData SO
//   _proprietairesDisponibles → glisser tous les OwnerData SO
//   _objetsSaisissables       → SeizableObjectEntry[] par défaut
//   _sliderReactivite         → Slider UI (min 1, max 10)
//   _sliderMethode            → Slider UI (min 1, max 10)
//   _sliderSociabilite        → Slider UI (min 1, max 10)
//   _btnConfirmer             → Button UI
//   panelType                 → Blocking (Inspector, base UIPanel)
//   _contexteVisibles         → Hub (Inspector, base UIPanel)
//   _autoAfficher             → false (Inspector, base UIPanel)
// ============================================================
using BailiffCo.Hub;
using UnityEngine;
using UnityEngine.UI;

public class MissionLibreConfigUI : UIPanel
{
    // ================================================================
    // CONFIGURATION — assignée dans l'Inspector
    // ================================================================

    [Header("Pool d'assets disponibles")]
    [SerializeField] private HabitationData[]     _habitationsDisponibles;
    [SerializeField] private OwnerData[]   _proprietairesDisponibles;
    [SerializeField] private SeizableObjectEntry[] _objetsSaisissables;

    [Header("Sliders — comportement proprio (1–10)")]
    [SerializeField] private Slider _sliderReactivite;
    [SerializeField] private Slider _sliderMethode;
    [SerializeField] private Slider _sliderSociabilite;

    [Header("UI")]
    [SerializeField] private Button _btnConfirmer;

    // ================================================================
    // ÉTAT
    // ================================================================

    private int _habitationIndex   = 0;
    private int _proprietaireIndex = -1;  // -1 = aléatoire

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();
        _btnConfirmer?.onClick.AddListener(ConfirmerEtLancer);
        InitialiserSliders();
    }

    private void OnDestroy()
    {
        _btnConfirmer?.onClick.RemoveAllListeners();
    }

    // ================================================================
    // INITIALISATION
    // ================================================================

    private void InitialiserSliders()
    {
        ConfigurerSlider(_sliderReactivite);
        ConfigurerSlider(_sliderMethode);
        ConfigurerSlider(_sliderSociabilite);
    }

    public override void Ouvrir()
    {
        _habitationIndex   = 0;
        _proprietaireIndex = -1;
        InitialiserSliders();
        base.Ouvrir();
    }

    private static void ConfigurerSlider(Slider s)
    {
        if (s == null) return;
        s.minValue = 1f;
        s.maxValue = 10f;
        s.wholeNumbers = true;
        s.value = 5f;
    }

    // ================================================================
    // SÉLECTION — appelé par les boutons de la liste UI
    // ================================================================

    /// <summary>Sélectionne une habitation par index dans _habitationsDisponibles.</summary>
    public void SelectionnerHabitation(int index)
    {
        if (_habitationsDisponibles == null || index < 0 || index >= _habitationsDisponibles.Length)
            return;
        _habitationIndex = index;
    }

    /// <summary>Sélectionne un proprio par index. Passer -1 pour aléatoire.</summary>
    public void SelectionnerProprietaire(int index)
    {
        _proprietaireIndex = index;
    }

    // ================================================================
    // CONFIRMATION ET LANCEMENT
    // ================================================================

    private void ConfirmerEtLancer()
    {
        if (!ValiderSelection()) return;

        var runtimeMission = ConstruireMissionData();
        var vehicule       = HubManager.Instance?.VehiculeSelectionne;

        GameManager.Instance.LancerMission(runtimeMission, vehicule);
    }

    private bool ValiderSelection()
    {
        if (_habitationsDisponibles == null || _habitationsDisponibles.Length == 0)
        {
            Debug.LogError("[MissionLibreConfigUI] Aucune habitation configurée dans l'Inspector !");
            return false;
        }
        if (_habitationIndex >= _habitationsDisponibles.Length)
        {
            _habitationIndex = 0;
        }
        if (HubManager.Instance?.VehiculeSelectionne == null)
        {
            Debug.LogWarning("[MissionLibreConfigUI] Aucun véhicule sélectionné — louer un véhicule d'abord.");
            return false;
        }
        return true;
    }

    private MissionData ConstruireMissionData()
    {
        var habitation = _habitationsDisponibles[_habitationIndex];
        var proprio    = ConstruireProprietaire();

        var runtime             = ScriptableObject.CreateInstance<MissionData>();
        runtime.MissionName     = "Mission Libre";
        runtime.SceneName       = SceneNames.MISSION_LIBRE;
        runtime.Habitation      = habitation;
        runtime.HousePrefab     = habitation.Prefab;
        runtime.Owner           = proprio;
        runtime.OwnerPrefab     = proprio?.OwnerPrefab;
        runtime.SeizableObjects = _objetsSaisissables;

        return runtime;
    }

    private OwnerData ConstruireProprietaire()
    {
        if (_proprietairesDisponibles == null || _proprietairesDisponibles.Length == 0)
        {
            Debug.LogWarning("[MissionLibreConfigUI] Aucun OwnerData configuré.");
            return null;
        }

        int idx = (_proprietaireIndex >= 0 && _proprietaireIndex < _proprietairesDisponibles.Length)
            ? _proprietaireIndex
            : Random.Range(0, _proprietairesDisponibles.Length);

        // Copie runtime — ne modifie pas l'asset source
        var runtime         = Instantiate(_proprietairesDisponibles[idx]);
        runtime.Reactivity  = Mathf.RoundToInt(_sliderReactivite?.value  ?? 5f);
        runtime.Method      = Mathf.RoundToInt(_sliderMethode?.value     ?? 5f);
        runtime.Sociability = Mathf.RoundToInt(_sliderSociabilite?.value ?? 5f);

        return runtime;
    }
}
