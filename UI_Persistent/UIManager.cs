// ============================================================
// UIManager.cs — Bailiff & Co  V2
// Gestionnaire central de l'UI persistante.
// Active/désactive les panels selon le contexte de jeu.
//
// CHANGEMENTS :
//   + SetPanelOpen(bool) : appelé par chaque panel à l'ouverture/fermeture
//   + IsInputBlocked     : lu par PlayerController dans Update()
//     → true si contexte Menu/Personnalisation OU si un panel est ouvert
// ============================================================
using UnityEngine;

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
    // ÉTAT — BLOCAGE INPUT
    // ================================================================

    /// <summary>
    /// Contexte courant de la scène (Menu, Hub, Mission, Personnalisation).
    /// Mis à jour par OnSceneChargee et OnMissionDemarree.
    /// </summary>
    private ContexteJeu _contexteCourant = ContexteJeu.Menu;

    /// <summary>
    /// Nombre de panels ouverts simultanément.
    /// Un compteur (plutôt qu'un bool) gère les cas où plusieurs panels
    /// s'ouvrent/ferment en cascade sans se désynchroniser.
    /// </summary>
    private int _panelsOuverts = 0;

    /// <summary>
    /// Lu par PlayerController dans Update().
    /// Bloque caméra + déplacements si true.
    /// </summary>
    public bool IsInputBlocked =>
        _contexteCourant != ContexteJeu.Mission && _contexteCourant != ContexteJeu.Hub
        || _panelsOuverts > 0;

    /// <summary>
    /// Appelé par chaque panel à l'ouverture (open=true) et à la fermeture (open=false).
    /// Met aussi à jour le curseur automatiquement.
    /// </summary>
    public void SetPanelOpen(bool open)
    {
        _panelsOuverts = Mathf.Max(0, _panelsOuverts + (open ? 1 : -1));

        // Curseur libre si un panel est ouvert, verrouillé sinon (en Mission/Hub)
        bool panelOuvert = _panelsOuverts > 0;
        Cursor.lockState = panelOuvert ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = panelOuvert;
    }

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
    }

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

    private void OnDisable()
    {
        EventBus<OnMissionDemarree>.Unsubscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Unsubscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Unsubscribe(OnSceneChargee);
    }

    private void Start()
    {
        ActiverContexteMenu();
    }

    // ================================================================
    // HANDLERS EVENTS
    // ================================================================

    private void OnSceneChargee(OnSceneChargee e)
    {
        TryResolveRefs();

        // Reset du compteur de panels à chaque changement de scène
        _panelsOuverts = 0;

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

    // ================================================================
    // ACTIVATION CONTEXTES
    // ================================================================

    public void ActiverContexteMenu()
    {
        _contexteCourant = ContexteJeu.Menu;
        _panelsOuverts   = 0;

        SetActif(_labelInteraction, false);
        SetActif(_inventaireWheel,  false);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[UIManager] Contexte Menu activé.");
    }

    public void ActiverContexteHub()
    {
        _contexteCourant = ContexteJeu.Hub;
        _panelsOuverts   = 0;

        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        true);

        // En Hub, le curseur est libre par défaut (pas de panels ouverts)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[UIManager] Contexte Hub activé.");
    }

    public void ActiverContexteMission()
    {
        _contexteCourant = ContexteJeu.Mission;
        _panelsOuverts   = 0;

        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        true);
        SetActif(_crosshair,        true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        Debug.Log("[UIManager] Contexte Mission activé.");
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void SetActif(GameObject go, bool actif)
    {
        if (go != null && go.activeSelf != actif)
            go.SetActive(actif);
    }

    /// <summary>Appelé par MissionBuilder après spawn du joueur.</summary>
    public void OnJoueurSpawne(InventaireSystem inventaire, PlayerCarry carry)
    {
        var wheel = _inventaireWheel?.GetComponent<InventaireWheel>();
        if (wheel != null)
            wheel.SetRefs(inventaire, carry);
        else
            Debug.LogWarning("[UIManager] InventaireWheel introuvable — SetRefs ignoré.");
    }

    public void ReSubscribe()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
    }

    public HUDSystem GetHUDSystem() => _hudSystem?.GetComponent<HUDSystem>();

    public void OuvrirOptions() => _optionsUI?.SetActive(true);
}
