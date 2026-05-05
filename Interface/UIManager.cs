using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ════════════════════════════════════════════════════════
    // RÉFÉRENCES AUX CANVAS
    // ════════════════════════════════════════════════════════
    [Header("Canvas — Toujours Actifs")]
    [SerializeField] private GameObject _canvasPersistent;
    [SerializeField] private GameObject _canvasFonduNoir;

    [Header("Canvas — Contexte Hub")]
    [SerializeField] private GameObject _canvasHudPlayerHub;
    [SerializeField] private GameObject _canvasHub;

    [Header("Canvas — Contexte Mission")]
    [SerializeField] private GameObject _canvasHudPlayerMission;

    // ════════════════════════════════════════════════════════
    // RÉFÉRENCES AUX PANELS INDIVIDUELS
    // ════════════════════════════════════════════════════════
    [Header("Persistent Panels")]
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _optionsUI;
    [SerializeField] private GameObject _inventaireWheel;
    [SerializeField] private GameObject _labelInteraction;
    [SerializeField] private GameObject _crosshair;

    [Header("Hub Panels")]
    [SerializeField] private MissionListUI  _missionListUI;
    [SerializeField] private MissionPanelUI _missionPanelUI;
    [SerializeField] private VehiclePanelUI _vehiclePanelUI;

    [Header("Hub HUD")]
    [SerializeField] private TextMeshProUGUI _txtArgent;
    [SerializeField] private TextMeshProUGUI _txtMissionChoisie;

    [Header("Mission HUD")]
    [SerializeField] private HUDSystem _hudSystem;

    // ════════════════════════════════════════════════════════
    // GESTION DES PANELS ACTIFS (logique actuelle conservée)
    // ════════════════════════════════════════════════════════
    private HashSet<UIPanel> _panelsActifs = new HashSet<UIPanel>();

    public void RegisterPanel(UIPanel panel) { /* ... */ }
    public void UnregisterPanel(UIPanel panel) { /* ... */ }
    private void UpdateUIState() { /* ... */ }

    // ════════════════════════════════════════════════════════
    // ACTIVATION CONTEXTES (REFONTE)
    // ════════════════════════════════════════════════════════
    public void ActiverContexteMenu()
    {
        // Cache TOUS les Canvas sauf Persistent
        SetActif(_canvasHudPlayerHub,     false);
        SetActif(_canvasHudPlayerMission, false);
        SetActif(_canvasHub,              false);
        
        // Garde Persistent (pause, options, etc.)
        SetActif(_canvasPersistent, true);
        
        _panelsActifs.Clear();
        UpdateUIState();
    }

    public void ActiverContexteHub()
    {
        SetActif(_canvasHudPlayerHub,     true);
        SetActif(_canvasHudPlayerMission, false);
        SetActif(_canvasHub,              true); // popups Hub dispo
        SetActif(_canvasPersistent,       true);
        
        // Active gameplay UI Hub
        SetActif(_labelInteraction, true);
        SetActif(_crosshair,        true);
        SetActif(_inventaireWheel,  true);
        
        _panelsActifs.Clear();
        UpdateUIState();
    }

    public void ActiverContexteMission()
    {
        SetActif(_canvasHudPlayerHub,     false);
        SetActif(_canvasHudPlayerMission, true);
        SetActif(_canvasHub,              false); // pas de popups Hub en mission
        SetActif(_canvasPersistent,       true);
        
        // Active gameplay UI Mission
        SetActif(_labelInteraction, true);
        SetActif(_crosshair,        true);
        SetActif(_inventaireWheel,  true);
        SetActif(_hudSystem.gameObject, true);
        
        _panelsActifs.Clear();
        UpdateUIState();
    }

    // ════════════════════════════════════════════════════════
    // API PUBLIQUE — HUB (ex-HubUI)
    // ════════════════════════════════════════════════════════
    public void MettreAJourArgent(float montant)
    {
        if (_txtArgent != null)
            _txtArgent.text = $"{montant:N0} €";
    }

    public void MettreAJourMissionChoisie(string nomMission)
    {
        if (_txtMissionChoisie != null)
            _txtMissionChoisie.text = $"Mission : {nomMission}";
    }

    public void OuvrirPanelMissions()    => _missionListUI?.Ouvrir();
    public void OuvrirPanelBoutique()    { /* futur */ }
    public void OuvrirPanelInventaire()  { /* futur */ }
    public void OuvrirPanelGarage()      { /* futur */ }

    public void AfficherErreur(string message)
    {
        // TODO: popup erreur globale
        Debug.LogWarning($"[UIManager] Erreur : {message}");
    }

    // ════════════════════════════════════════════════════════
    // UTILITAIRES
    // ════════════════════════════════════════════════════════
    private void SetActif(GameObject go, bool actif)
    {
        if (go != null && go.activeSelf != actif)
            go.SetActive(actif);
    }
}