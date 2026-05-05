// ============================================================
// UIManager.cs — Bailiff & Co  V2
// Gestionnaire central de l'UI persistante.
// Active/désactive les panels selon le contexte de jeu.
//
// CHANGEMENTS V2 :
//   + RegisterPanel/UnregisterPanel : gestion automatique des panels via HashSet
//   + UpdateUIState() : source unique de vérité pour input joueur ET curseur
//   + Abonnement à OnContextChanged : recalcule l'état quand le contexte change
//   - IsInputBlocked : SUPPRIMÉ (remplacé par GameManager.InputJoueurActif)
//   - Override curseur dans ActiverContexte* : SUPPRIMÉ (tout passe par UpdateUIState)
// ============================================================
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    // ================================================================
    // RÉFÉRENCES AUX PANELS
    // ================================================================

    [Header("UI Toujours Actives")]
    [SerializeField] private GameObject _fonduNoir;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _optionsUI;

    [Header("UI Hub + Mission")]
    [SerializeField] private GameObject _labelInteraction;
    [SerializeField] private GameObject _inventaireWheel;
    [SerializeField] private GameObject _crosshair;

    [Header("UI Mission Uniquement")]
    [SerializeField] private GameObject _hudSystem;

    // ================================================================
    // ÉTAT — PANELS ACTIFS
    // ================================================================

    /// <summary>
    /// Collection de tous les panels actuellement actifs (OnEnable).
    /// </summary>
    private HashSet<UIPanel> _panelsActifs = new HashSet<UIPanel>();

    // ================================================================
    // GESTION DES PANELS
    // ================================================================

    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIManager] RegisterPanel : panel null !");
            return;
        }
        
        _panelsActifs.Add(panel);
        Debug.Log($"[UIManager] RegisterPanel: {panel.GetType().Name} ({panel.PanelType}) - Total panels: {_panelsActifs.Count}");
        UpdateUIState();
    }

    public void UnregisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        _panelsActifs.Remove(panel);
        Debug.Log($"[UIManager] UnregisterPanel: {panel.GetType().Name}");
        UpdateUIState();
    }

    /// <summary>
    /// Source unique de vérité : synchronise input joueur et curseur.
    /// Appelé à chaque changement de panel OU de contexte.
    /// Ne jamais overrider Cursor en dehors de cette méthode.
    /// </summary>
    private void UpdateUIState()
    {
        if (GameManager.Instance == null) return;

        bool hasBlocking = _panelsActifs.Any(p => p.PanelType == UIPanelType.Blocking);
        bool hasOverlay  = _panelsActifs.Any(p => p.PanelType == UIPanelType.Overlay);

    
        Debug.Log($"[UIManager] UpdateUIState: Blocking={hasBlocking}, Panels={_panelsActifs.Count}");

        // =====================================================
        // INPUT JOUEUR
        // =====================================================
        // Actif si contexte Hub/Mission ET aucun panel bloquant ouvert
        bool inputActif =
            (GameManager.Instance.ContexteActuel == ContexteJeu.Mission ||
            GameManager.Instance.ContexteActuel == ContexteJeu.Hub) &&
            !hasBlocking;

        GameManager.Instance.SetInputJoueurActif(inputActif);

        // =====================================================
        // CURSEUR
        // =====================================================
        // Visible si panel bloquant OU overlay actif
        // En contexte Menu/Personnalisation : toujours visible (inputActif = false donc hasBlocking n'importe pas)
        bool cursorVisible = !inputActif || hasBlocking || hasOverlay;

        Cursor.visible   = cursorVisible;
        Cursor.lockState = cursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
        EventBus<OnContextChanged>.Subscribe(OnContextChanged);
    }

    private void OnDisable()
    {
        EventBus<OnMissionDemarree>.Unsubscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Unsubscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Unsubscribe(OnSceneChargee);
        EventBus<OnContextChanged>.Unsubscribe(OnContextChanged);
    }

    private void Start()
    {
        ActiverContexteMenu();
    }

    /// <summary>
    /// Tente de résoudre les références UI qui peuvent être perdues après un changement de scène.
    /// </summary>
    private void TryResolveRefs()
    {
        if (_labelInteraction == null)
            _labelInteraction = GameObject.Find("Canvas_LabelInteraction");
        if (_crosshair == null)
            _crosshair = GameObject.Find("Canvas_Crosshair");
        if (_inventaireWheel == null)
            _inventaireWheel = GameObject.Find("Canvas_InventaireWheel");
        if (_hudSystem == null)
            _hudSystem = GameObject.Find("CanvasHUD");
    }

    // ================================================================
    // HANDLERS EVENTS
    // ================================================================

    private void OnSceneChargee(OnSceneChargee e)
    {
        TryResolveRefs();

        // Reset de la collection des panels à chaque changement de scène
        _panelsActifs.Clear();

        Debug.Log($"[UIManager] OnSceneChargee : {e.NomScene}");
        switch (e.NomScene)
        {
            case SceneNames.MENU:
            case SceneNames.PERSONNALISATION:
                ActiverContexteMenu();
                break;

            case SceneNames.HUB:
                ActiverContexteHub();
                break;

            case SceneNames.MISSION:
                // L'activation se fait via OnMissionDemarree
                break;
        }
    }

    private void OnMissionDemarree(OnMissionDemarree e)
    {
        ActiverContexteMission();
    }

    private void OnMissionTerminee(OnMissionTerminee e)
    {
        // Retour géré par SceneLoader → OnSceneChargee s'occupera du contexte
    }

    /// <summary>
    /// Reçoit les changements de contexte depuis GameManager.
    /// Recalcule immédiatement l'état UI (input + curseur).
    /// </summary>
    private void OnContextChanged(OnContextChanged e)
    {
        UpdateUIState();
    }

    // ================================================================
    // ACTIVATION CONTEXTES
    // ================================================================

    /// <summary>
    /// Active l'UI pour le contexte Menu/Personnalisation.
    /// Aucune UI de gameplay. Curseur géré par UpdateUIState().
    /// </summary>
    public void ActiverContexteMenu()
    {
        SetActif(_labelInteraction, false);
        SetActif(_inventaireWheel,  false);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        false);

        // Les SetActive(false) déclenchent OnDisable → UnregisterPanel automatiquement.
        // On force un clear + recalcul pour être sûr (panels déjà détruits entre scènes).
        _panelsActifs.Clear();
        UpdateUIState();

        Debug.Log("[UIManager] Contexte Menu activé.");
    }

    /// <summary>
    /// Active l'UI pour le contexte Hub.
    /// Label interaction, roue inventaire et crosshair actifs.
    /// Curseur géré par UpdateUIState().
    /// </summary>
    public void ActiverContexteHub()
    {
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        true);

        // Les SetActive(true) déclenchent OnEnable → RegisterPanel automatiquement.
        // Un clear préalable évite des doublons si des refs traînent d'une scène précédente.
        _panelsActifs.Clear();
        UpdateUIState();

        Debug.Log("[UIManager] Contexte Hub activé.");
    }

    /// <summary>
    /// Active l'UI pour le contexte Mission.
    /// Toutes les UI de gameplay actives. Curseur géré par UpdateUIState().
    /// </summary>
    public void ActiverContexteMission()
    {
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        true);
        SetActif(_crosshair,        true);

        _panelsActifs.Clear();
        UpdateUIState();

        Debug.Log("[UIManager] Contexte Mission activé.");
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    /// <summary>
    /// Active/désactive un GameObject de manière sécurisée.
    /// </summary>
    private void SetActif(GameObject go, bool actif)
    {
        if (go != null && go.activeSelf != actif)
            go.SetActive(actif);
    }

    /// <summary>
    /// Appelé par MissionBuilder après spawn du joueur.
    /// Injecte les références dans la roue d'inventaire.
    /// </summary>
    public void OnJoueurSpawne(InventaireSystem inventaire, PlayerCarry carry)
    {
        var wheel = _inventaireWheel?.GetComponent<InventaireWheel>();
        if (wheel != null)
            wheel.SetRefs(inventaire, carry);
        else
            Debug.LogWarning("[UIManager] InventaireWheel introuvable — SetRefs ignoré.");
    }

    /// <summary>
    /// Réabonne les événements (utile après un hot-reload).
    /// </summary>
    public void ReSubscribe()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
        EventBus<OnContextChanged>.Subscribe(OnContextChanged);
    }

    /// <summary>
    /// Récupère le composant HUDSystem.
    /// </summary>
    public HUDSystem GetHUDSystem() => _hudSystem?.GetComponent<HUDSystem>();

    /// <summary>
    /// Ouvre le menu des options.
    /// </summary>
    public void OuvrirOptions() => _optionsUI?.SetActive(true);
}