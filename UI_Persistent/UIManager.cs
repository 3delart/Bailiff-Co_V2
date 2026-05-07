// ============================================================
// UIManager.cs — Bailiff & Co  V2
// Gestionnaire central de l'UI persistante.
//
// Au démarrage, découvre tous les UIPanel de la scène via
// FindObjectsOfType (y compris les panels inactifs).
// À chaque OnContextChanged, appelle EvaluerContexte() sur
// tous les panels — chacun s'ouvre ou se ferme seul.
//
// PLUS DE : TryResolveRefs, ActiverContexteXxx, [SerializeField]
// panels individuels, abonnements OnMissionDemarree/Terminee.
// ============================================================
using System.Collections.Generic;
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
    // PANELS
    // ================================================================

    private readonly List<UIPanel>    _tousLesPanels = new();
    private readonly HashSet<UIPanel> _panelsActifs  = new();

    private void Start()
    {
        // Découverte unique de tous les panels (y compris inactifs) dans UI_Persistent
        _tousLesPanels.AddRange(FindObjectsOfType<UIPanel>(includeInactive: true));

        // Évalue le contexte initial (Menu au démarrage)
        if (GameManager.Instance != null)
            EvaluerTousLesPanels(GameManager.Instance.ContexteActuel);
    }

    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        _panelsActifs.Add(panel);
        UpdateUIState();
    }

    public void UnregisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        _panelsActifs.Remove(panel);
        UpdateUIState();
    }

    // ================================================================
    // ÉVALUATION CONTEXTE
    // ================================================================

    private void EvaluerTousLesPanels(ContexteJeu contexte)
    {
        foreach (var panel in _tousLesPanels)
            if (panel != null)
                panel.EvaluerContexte(contexte);
    }

    // ================================================================
    // EVENTS
    // ================================================================

    private void OnEnable()
        => EventBus<OnContextChanged>.Subscribe(OnContextChanged);

    private void OnDisable()
        => EventBus<OnContextChanged>.Unsubscribe(OnContextChanged);

    private void OnContextChanged(OnContextChanged e)
        => EvaluerTousLesPanels(e.Context);

    // ================================================================
    // UI STATE — source unique de vérité pour input joueur + curseur
    // ================================================================

    private void UpdateUIState()
    {
        if (GameManager.Instance == null) return;

        bool hasBlocking = false;
        bool hasOverlay  = false;

        foreach (var p in _panelsActifs)
        {
            if (p.PanelType == UIPanelType.Blocking) hasBlocking = true;
            if (p.PanelType == UIPanelType.Overlay)  hasOverlay  = true;
        }

        bool inputActif =
            (GameManager.Instance.ContexteActuel == ContexteJeu.Mission ||
             GameManager.Instance.ContexteActuel == ContexteJeu.Hub) &&
            !hasBlocking;

        GameManager.Instance.SetInputJoueurActif(inputActif);

        bool cursorVisible = !inputActif || hasBlocking || hasOverlay;
        Cursor.visible   = cursorVisible;
        Cursor.lockState = cursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ================================================================
    // RE-SUBSCRIBE — appelé par SceneLoader après EventBusHelper.ClearAll()
    // ================================================================

    public void ReSubscribe()
    {
        EventBus<OnContextChanged>.Unsubscribe(OnContextChanged);
        EventBus<OnContextChanged>.Subscribe(OnContextChanged);
    }

    // ================================================================
    // INJECTION — appelé par MissionBuilder après spawn joueur
    // ================================================================

    public void OnJoueurSpawne(InventaireSystem inventaire, PlayerCarry carry)
    {
        var wheel = FindObjectOfType<InventaireWheel>(includeInactive: true);
        if (wheel != null)
            wheel.SetRefs(inventaire, carry);
        else
            Debug.LogWarning("[UIManager] InventaireWheel introuvable — SetRefs ignoré.");
    }

    public HUDSystem GetHUDSystem()
        => FindObjectOfType<HUDSystem>(includeInactive: true);

    public void OuvrirOptions()
    {
        var options = FindObjectOfType<OptionsUI>(includeInactive: true);
        options?.Ouvrir();
    }
}
