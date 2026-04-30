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
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIManager] Instance en double détectée — destruction.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
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

    [Header("UI Mission Uniquement")]
    [SerializeField] private GameObject _hudSystem;        // quota, paranoïa, urgence

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarree);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargee);
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
        // Déterminer le contexte selon le nom de la scène
        switch (e.NomScene)
        {
            case SceneNames.MENU:
            case SceneNames.CHARACTER_CUSTOMIZATION:
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

        Debug.Log("[UIManager] Contexte Menu activé.");
    }

    public void ActiverContexteHub()
    {
        // Hub : Label + Inventaire actifs, mais pas le HUD mission
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        false);

        Debug.Log("[UIManager] Contexte Hub activé.");
    }

    public void ActiverContexteMission()
    {
        // Mission : tout actif
        SetActif(_labelInteraction, true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem,        true);

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
}