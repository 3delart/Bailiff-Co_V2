// ============================================================
// UIManager.cs — Bailiff & Co  V2
// Gestionnaire central de l'UI persistante.
// Active/désactive les panels selon le contexte de jeu :
//   - Menu : seulement OptionsUI + PauseMenu
//   - Hub  : + LabelInteraction + InventaireWheel
//   - Mission : + HUDSystem (quota, paranoïa, urgence)
//
// RÈGLE : ce script ne contient AUCUNE logique métier.
// Il ne fait qu'écouter les events de changement de contexte
// et activer/désactiver les bons GameObjects.
//
// SETUP UNITY :
//   Placer ce script sur le GameObject racine "UIManager" 
//   dans la scène UI_Persistent.
//   Assigner toutes les références aux panels enfants.
// ============================================================
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // ================================================================
    // SINGLETON (optionnel, mais pratique pour accès direct)
    // ================================================================

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
    [SerializeField] private GameObject _fonduNoir;        // géré par SceneLoader
    [SerializeField] private GameObject _pauseMenu;        // actif partout
    [SerializeField] private GameObject _optionsUI;        // actif partout

    [Header("UI Hub + Mission")]
    [SerializeField] private GameObject _labelInteraction; // actif Hub + Mission
    [SerializeField] private GameObject _inventaireWheel;  // actif Hub + Mission
    [SerializeField] private GameObject _crosshair;

    [Header("UI Mission Uniquement")]
    [SerializeField] private GameObject _hudSystem;        // quota, paranoïa, urgence

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        // Les refs viennent de l'Inspector — pas de Find() ici
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
    }

    private void TryResolveRefs()
    {
        // Seulement si pas assigné dans l'Inspector
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
        // Au démarrage, on est dans le Menu (chargé par Bootstrap)
        ActiverContexteMenu();
    }

    // ================================================================
    // HANDLERS EVENTS
    // ================================================================

    private void OnSceneChargee(OnSceneChargee e)
    {

        TryResolveRefs();

        // Déterminer le contexte selon le nom de la scène
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
                // La mission se démarre via OnMissionDemarree, pas ici
                // (le MissionSystem raise l'event après le build)
                break;
        }
    }

    private void OnMissionDemarree(OnMissionDemarree e)
    {
        ActiverContexteMission();
    }

    private void OnMissionTerminee(OnMissionTerminee e)
    {
        // Retour au Hub après la mission
        // (SceneLoader charge le Hub, OnSceneChargee va activer le bon contexte)
    }

    // ================================================================
    // ACTIVATION CONTEXTES
    // ================================================================

    public void ActiverContexteMenu()
    {
        // Menu : seulement Options + Pause (pas de label, pas d'inventaire, pas de HUD)
        SetActif(_labelInteraction, false);
        SetActif(_inventaireWheel,  false);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        false);

        Debug.Log("[UIManager] Contexte Menu activé.");
    }

    public void ActiverContexteHub()
    {
        // Hub : Label + Inventaire actifs, mais pas le HUD mission
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        false);
        SetActif(_crosshair,        true);

        Debug.Log("[UIManager] Contexte Hub activé.");
    }

    public void ActiverContexteMission()
    {
        // Mission : tout actif
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        true);
        SetActif(_crosshair,        true);

        Debug.Log("[UIManager] Contexte Mission activé.");
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void SetActif(GameObject go, bool actif)
    {
        if (go != null && go.activeSelf != actif)
        {
            go.SetActive(actif);
        }
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

    // ================================================================
    // API PUBLIQUE (pour accès direct si besoin)
    // ================================================================

    /// <summary>
    /// Retourne la référence au HUDSystem pour afficher des notifications
    /// manuelles (ex: argent gagné dans le Hub).
    /// </summary>
    public HUDSystem GetHUDSystem()
    {
        return _hudSystem?.GetComponent<HUDSystem>();
    }

    public void OuvrirOptions()
    {
        _optionsUI?.SetActive(true);
    }


}