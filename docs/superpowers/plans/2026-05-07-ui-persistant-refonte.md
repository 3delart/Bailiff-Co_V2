# UI_Persistent Refonte Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centraliser tous les Canvas dans la scène `UI_Persistent`, remplacer le système manuel d'activation par un système déclaratif (chaque `UIPanel` déclare ses contextes autorisés), et consolider les événements en anglais uniquement.

**Architecture:** `UIPanel` gagne `_contexteVisibles[]` et `_autoAfficher`. `UIManager` utilise `FindObjectsOfType<UIPanel>` au démarrage pour construire `_tousLesPanels`, puis appelle `EvaluerContexte()` sur chaque panel à chaque changement de contexte. Aucun `SetActive` n'est appelé depuis l'extérieur des panels — tout passe par `Ouvrir()`/`Fermer()`.

**Tech Stack:** Unity 2022 LTS, C# 9, URP, TextMeshPro, EventBus maison

**Spec de référence:** `docs/superpowers/specs/2026-05-07-ui-persistant-refonte-design.md`

---

## Carte des fichiers

| Fichier | Action | Rôle |
|---|---|---|
| `Assets/_Script/UI_Persistent/UIPanel.cs` | Modifier | Base de tous les panels — ajout contexte + EvaluerContexte |
| `Assets/_Script/UI_Persistent/UIManager.cs` | Modifier | Simplification majeure — FindObjectsOfType + EvaluerTousLesPanels |
| `Assets/_Script/_Core/Events/GameEvents.cs` | Modifier | Supprimer les 6 alias français |
| `Assets/_Script/_Core/Events/EventBus.cs` | Modifier | Mettre à jour les commentaires |
| `Assets/_Script/_Core/Services/SceneLoader.cs` | Modifier | OnFondNoir→OnFadeToBlack, OnMissionTerminee→OnMissionEnded |
| `Assets/_Script/UI_Persistent/HUDSystem.cs` | Modifier | 3 migrations d'events français→anglais |
| `Assets/_Script/_Core/SharedData/MissionResult.cs` | Modifier | Commentaire uniquement |
| `Assets/_Script/Features/Menu/MenuUI.cs` | Modifier | MonoBehaviour → UIPanel |
| `Assets/_Script/UI_Persistent/InventaireWheel.cs` | Modifier | Fix bug OnEnable + adaptation nouveau système |
| `Assets/_Script/Features/Mission/MissionListUI.cs` | Modifier | FindObjectOfType → HubManager.Instance |
| `Assets/_Script/Features/Options/UI/OptionsUI.cs` | Modifier | SetActive(false) → Fermer() |
| `Assets/_Script/Features/Hub/HubUI.cs` | Modifier | Nettoyage fallback SetActive |
| Scène Unity `UI_Persistent` | Créer + configurer | Accueillir tous les canvases |

---

## Task 1: UIPanel.cs — Nouveau système de contexte

**Files:**
- Modify: `Assets/_Script/UI_Persistent/UIPanel.cs`

- [ ] **Step 1 : Remplacer le contenu complet de UIPanel.cs**

```csharp
// ============================================================
// UIPanel.cs — Bailiff & Co  V2
// Classe de base pour tous les panels UI.
//
// Chaque panel déclare dans l'Inspector :
//   _contexteVisibles : les contextes où il a le droit d'exister
//   _autoAfficher     : s'ouvre automatiquement à l'entrée du contexte
//
// UIManager appelle EvaluerContexte() sur tous les panels
// à chaque changement de contexte (OnContextChanged).
//
// RÈGLE ABSOLUE : aucun SetActive() direct depuis l'extérieur.
// Tout passe par Ouvrir() / Fermer(). Override autorisé —
// doit toujours appeler base.Ouvrir() / base.Fermer().
// ============================================================
using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [Header("Type de panel")]
    [SerializeField] protected UIPanelType panelType;
    public UIPanelType PanelType => panelType;

    [Header("Contexte de visibilité")]
    [Tooltip("Contextes dans lesquels ce panel a le droit d'exister.")]
    [SerializeField] private ContexteJeu[] _contexteVisibles;

    [Tooltip("Si coché, s'ouvre automatiquement dès l'entrée dans un contexte autorisé.\n" +
             "Si non coché, attend un déclencheur explicite (Ouvrir() ou input joueur).")]
    [SerializeField] private bool _autoAfficher;

    // ================================================================
    // LIFECYCLE — ENREGISTREMENT ACTIF
    // ================================================================

    protected virtual void OnEnable()
        => UIManager.Instance?.RegisterPanel(this);

    protected virtual void OnDisable()
        => UIManager.Instance?.UnregisterPanel(this);

    // ================================================================
    // ÉVALUATION CONTEXTE — appelé par UIManager sur OnContextChanged
    // ================================================================

    /// <summary>
    /// Évalue si ce panel doit être visible dans le contexte donné.
    /// Appelé par UIManager à chaque changement de ContexteJeu.
    /// </summary>
    public void EvaluerContexte(ContexteJeu contexte)
    {
        bool autorise = _contexteVisibles != null
            && System.Array.IndexOf(_contexteVisibles, contexte) >= 0;

        if (!autorise)
        {
            Fermer();
        }
        else if (_autoAfficher)
        {
            Ouvrir();
        }
        // contexte autorisé + _autoAfficher = false → ne rien faire
    }

    // ================================================================
    // API UNIFIÉE — OUVRIR / FERMER
    // ================================================================

    public virtual void Ouvrir()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        else
            UIManager.Instance?.RegisterPanel(this);
    }

    public virtual void Fermer() => gameObject.SetActive(false);

    public bool EstOuvert => gameObject.activeSelf;
}
```

- [ ] **Step 2 : Vérifier en Play Mode que les panels existants compilent sans erreur**

Ouvrir Unity → attendre la compilation → vérifier la console (aucune erreur sur UIPanel, PauseMenu, OptionsUI, HUDSystem, LabelInteractionUI, InventaireWheel, MissionListUI, MissionPanelUI, VehiclePanelUI, HubUI).

- [ ] **Step 3 : Commit**

```bash
git add Assets/_Script/UI_Persistent/UIPanel.cs
git commit -m "refactor(ui): UIPanel - ajout _contexteVisibles, _autoAfficher, EvaluerContexte"
```

---

## Task 2: UIManager.cs — Simplification majeure

**Files:**
- Modify: `Assets/_Script/UI_Persistent/UIManager.cs`

- [ ] **Step 1 : Remplacer le contenu complet de UIManager.cs**

```csharp
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
```

- [ ] **Step 2 : Vérifier la compilation dans Unity**

Ouvrir Unity → attendre la recompilation → console sans erreur liée à UIManager.

- [ ] **Step 3 : Vérifier en Play Mode — contexte Menu**

Lancer le jeu depuis Bootstrap. Vérifier dans la console :
- `[UIManager]` ne logue plus `ActiverContexteMenu` (méthode supprimée)
- Aucune `NullReferenceException` sur UIManager

- [ ] **Step 4 : Commit**

```bash
git add Assets/_Script/UI_Persistent/UIManager.cs
git commit -m "refactor(ui): UIManager - FindObjectsOfType, EvaluerTousLesPanels, suppression ActiverContexteXxx"
```

---

## Task 3: Consolidation des événements — Anglais uniquement

**Files:**
- Modify: `Assets/_Script/_Core/Events/GameEvents.cs`
- Modify: `Assets/_Script/_Core/Events/EventBus.cs`
- Modify: `Assets/_Script/_Core/Services/SceneLoader.cs`
- Modify: `Assets/_Script/UI_Persistent/HUDSystem.cs`
- Modify: `Assets/_Script/_Core/SharedData/MissionResult.cs`

### 3a — GameEvents.cs : supprimer les alias français

- [ ] **Step 1 : Supprimer les 6 structs alias et le bloc de commentaires associé**

Dans `GameEvents.cs`, supprimer le bloc suivant (lignes ~136–267) :

```csharp
// Alias français — utilisés par PauseMenu, UIManager, InventaireWheel, etc.
// OnMissionDemarree transporte optionnellement les refs joueur pour
// que UIManager puisse appeler InventaireWheel.SetRefs() sans passer
// par MissionBuilder.OnJoueurSpawne().

/// <summary>Mission démarrée — alias français de OnMissionStarted.</summary>
public struct OnMissionDemarree
{
    public MissionData      Mission;
    public InventaireSystem Inventaire; // nullable — injecter si disponible
    public PlayerCarry      Carry;      // nullable — injecter si disponible
}

/// <summary>Mission terminée — alias français de OnMissionEnded.</summary>
public struct OnMissionTerminee
{
    public MissionResult Resultat;
}
```

Et aussi :

```csharp
/// <summary>Objet chargé dans le véhicule — alias français de OnObjectLoaded</summary>
public struct OnObjetCharge
{
    public ObjetData Objet;
    public float     Valeur;
    public bool      EstFragile;
}

/// <summary>Timer d'urgence déclenché — alias français de OnUrgencyTimerStarted</summary>
public struct OnTimerUrgenceDéclenche
{
    public float DureeSecondes;
}

/// <summary>Le perroquet a parlé — alias français (mais garder OnParrotSpoke aussi)</summary>
public struct OnPerroquetParle
{
    public string Phrase;
    public bool   EstIndice;
}
```

Et :

```csharp
/// <summary>Fondu vers le noir — alias français de OnFadeToBlack.</summary>
public struct OnFondNoir
{
    public float DureeSecondes;
}
```

- [ ] **Step 2 : Vérifier que Unity signale des erreurs de compilation** (normal — les abonnés utilisent encore ces types)

### 3b — SceneLoader.cs : migrer OnFondNoir et OnMissionTerminee

- [ ] **Step 3 : Remplacer les abonnements et handlers dans SceneLoader.cs**

Changer `OnEnable` / `OnDisable` :

```csharp
private void OnEnable()
{
    EventBus<OnFadeToBlack>.Subscribe(OnFadeToBlackEvent);
    EventBus<OnMissionEnded>.Subscribe(OnMissionEnded);
}

private void OnDisable()
{
    EventBus<OnFadeToBlack>.Unsubscribe(OnFadeToBlackEvent);
    EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
}
```

Changer les handlers :

```csharp
private void OnFadeToBlackEvent(OnFadeToBlack e) => FondNoir(e.DurationSeconds);

private void OnMissionEnded(OnMissionEnded e)
{
    StartCoroutine(RetourHubApresDelai(e.Result, 3f));
}
```

Supprimer l'appel à `UIManager.Instance?.ReSubscribe()` dans `TransitionAvecFondu` **n'est pas nécessaire** — ReSubscribe() est conservé dans UIManager simplifié. Laisser la ligne telle quelle.

### 3c — HUDSystem.cs : migrer les 3 events français

- [ ] **Step 4 : Mettre à jour les abonnements dans HUDSystem.cs**

Dans `OnEnable` et `OnDisable`, remplacer :

```csharp
// AVANT
EventBus<OnObjetCharge>.Subscribe(OnObjetCharge);
EventBus<OnTimerUrgenceDéclenche>.Subscribe(OnTimerUrgence);
EventBus<OnPerroquetParle>.Subscribe(OnPerroquetParle);

// APRÈS
EventBus<OnObjectLoaded>.Subscribe(OnObjectLoaded);
EventBus<OnUrgencyTimerStarted>.Subscribe(OnUrgencyTimerStarted);
EventBus<OnParrotSpoke>.Subscribe(OnParrotSpoke);
```

- [ ] **Step 5 : Renommer les méthodes handler et adapter les champs**

Remplacer :

```csharp
private void OnObjetCharge(OnObjetCharge e)
{
    if (_notificationChargement == null) return;
    _notificationChargement.text = $"+{e.Valeur:N0} €";
    CancelInvoke(nameof(CacherNotification));
    _notificationChargement.gameObject.SetActive(true);
    Invoke(nameof(CacherNotification), 2f);
}

private void OnTimerUrgence(OnTimerUrgenceDéclenche e)
{
    _urgenceActive = true;
    _timerUrgence  = e.DureeSecondes;
    if (_panneauUrgence) _panneauUrgence.SetActive(true);
}

private void OnPerroquetParle(OnPerroquetParle e)
{
    if (_notificationChargement == null) return;
    _notificationChargement.text = $"🦜 \"{e.Phrase}\"";
    _notificationChargement.gameObject.SetActive(true);
    CancelInvoke(nameof(CacherNotification));
    Invoke(nameof(CacherNotification), 4f);
}
```

Par :

```csharp
private void OnObjectLoaded(OnObjectLoaded e)
{
    if (_notificationChargement == null) return;
    _notificationChargement.text = $"+{e.Value:N0} €";
    CancelInvoke(nameof(CacherNotification));
    _notificationChargement.gameObject.SetActive(true);
    Invoke(nameof(CacherNotification), 2f);
}

private void OnUrgencyTimerStarted(OnUrgencyTimerStarted e)
{
    _urgenceActive = true;
    _timerUrgence  = e.DurationSeconds;
    if (_panneauUrgence) _panneauUrgence.SetActive(true);
}

private void OnParrotSpoke(OnParrotSpoke e)
{
    if (_notificationChargement == null) return;
    _notificationChargement.text = $"🦜 \"{e.Phrase}\"";
    _notificationChargement.gameObject.SetActive(true);
    CancelInvoke(nameof(CacherNotification));
    Invoke(nameof(CacherNotification), 4f);
}
```

### 3d — EventBus.cs et MissionResult.cs : commentaires

- [ ] **Step 6 : Mettre à jour les commentaires dans EventBus.cs**

Remplacer dans le header de EventBus.cs :

```csharp
//   EventBus<OnObjetCharge>.Raise(new OnObjetCharge { ... });
//   EventBus<OnObjetCharge>.Subscribe(MonHandler);
//   EventBus<OnObjetCharge>.Unsubscribe(MonHandler);
```

Par :

```csharp
//   EventBus<OnObjectLoaded>.Raise(new OnObjectLoaded { ... });
//   EventBus<OnObjectLoaded>.Subscribe(MonHandler);
//   EventBus<OnObjectLoaded>.Unsubscribe(MonHandler);
```

- [ ] **Step 7 : Mettre à jour le commentaire dans MissionResult.cs**

Remplacer :

```csharp
// Transportée par OnMissionTerminee vers GameManager,
```

Par :

```csharp
// Transportée par OnMissionEnded vers GameManager,
```

- [ ] **Step 8 : Vérifier que Unity compile sans erreur**

Attendre la recompilation. Aucune erreur attendue sur les types `OnMissionDemarree`, `OnMissionTerminee`, `OnObjetCharge`, `OnFondNoir`, `OnTimerUrgenceDéclenche`, `OnPerroquetParle`.

- [ ] **Step 9 : Commit**

```bash
git add Assets/_Script/_Core/Events/GameEvents.cs \
        Assets/_Script/_Core/Events/EventBus.cs \
        Assets/_Script/_Core/Services/SceneLoader.cs \
        Assets/_Script/UI_Persistent/HUDSystem.cs \
        Assets/_Script/_Core/SharedData/MissionResult.cs
git commit -m "refactor(events): supprimer alias français, migrer abonnés vers OnObjectLoaded/OnMissionEnded/OnFadeToBlack"
```

---

## Task 4: MenuUI.cs — MonoBehaviour → UIPanel

**Files:**
- Modify: `Assets/_Script/Features/Menu/MenuUI.cs`

- [ ] **Step 1 : Remplacer le contenu de MenuUI.cs**

`MenuUI` hérite maintenant de `UIPanel`. Les listeners sont câblés dans `Awake` (pour fonctionner même si le GO démarre inactif). `_contexteVisibles = [Menu]`, `_autoAfficher = true` — configuré dans l'Inspector.

```csharp
// ============================================================
// MenuUI.cs — Bailiff & Co  V2
// Panel du menu principal. Hérite de UIPanel.
// _contexteVisibles = [Menu], _autoAfficher = true (Inspector)
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : UIPanel
{
    [Header("Boutons")]
    [SerializeField] private Button _boutonJouer;
    [SerializeField] private Button _boutonCoop;
    [SerializeField] private Button _boutonPersonnalisation;
    [SerializeField] private Button _boutonOptions;
    [SerializeField] private Button _boutonQuitter;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake(); // requis si UIPanel.Awake() est utilisé dans le futur

        _boutonJouer?.onClick.AddListener(OnJouer);
        _boutonCoop?.onClick.AddListener(OnCoop);
        _boutonPersonnalisation?.onClick.AddListener(OnPersonnalisation);
        _boutonOptions?.onClick.AddListener(OnOptions);
        _boutonQuitter?.onClick.AddListener(OnQuitter);

        if (_boutonCoop != null)
            _boutonCoop.interactable = false;
        if (_boutonPersonnalisation != null)
            _boutonPersonnalisation.interactable = false;
    }

    private void OnDestroy()
    {
        _boutonJouer?.onClick.RemoveAllListeners();
        _boutonCoop?.onClick.RemoveAllListeners();
        _boutonPersonnalisation?.onClick.RemoveAllListeners();
        _boutonOptions?.onClick.RemoveAllListeners();
        _boutonQuitter?.onClick.RemoveAllListeners();
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnJouer()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.EnTransition) return;
        GameManager.Instance?.AllerAuHub();
    }

    private void OnCoop()
    {
        Debug.Log("[Menu] Coop — non implémenté en V2");
    }

    private void OnPersonnalisation()
    {
        SceneLoader.Instance?.ChargerScene(SceneNames.PERSONNALISATION, avecFondu: true);
    }

    private void OnOptions()
    {
        UIManager.Instance?.OuvrirOptions();
    }

    private void OnQuitter()
    {
        GameManager.Instance?.QuitterJeu();
    }
}
```

- [ ] **Step 2 : Compiler et vérifier**

Attendre recompilation Unity — aucune erreur sur MenuUI.

- [ ] **Step 3 : Commit**

```bash
git add Assets/_Script/Features/Menu/MenuUI.cs
git commit -m "refactor(ui): MenuUI - MonoBehaviour → UIPanel"
```

---

## Task 5: InventaireWheel.cs — Correction bug + adaptation

**Files:**
- Modify: `Assets/_Script/UI_Persistent/InventaireWheel.cs`

**Contexte du bug :** `OnEnable` appelle `_wheelRoot.SetActive(true)`, ce qui fait apparaître la roue immédiatement à chaque fois que UIManager active le panel (entrée en Hub après une mission). La roue ne doit s'afficher que sur pression de la touche inventaire.

- [ ] **Step 1 : Corriger OnEnable — ne plus afficher _wheelRoot automatiquement**

Remplacer la méthode `OnEnable` :

```csharp
protected override void OnEnable()
{
    base.OnEnable(); // RegisterPanel

    if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false); // caché jusqu'au Tab
    RafraichirSlots();

    _centreEcran             = new Vector2(Screen.width / 2f, Screen.height / 2f);
    _positionSourisVirtuelle = _centreEcran;
}
```

- [ ] **Step 2 : Corriger Update — gérer _wheelRoot directement sans Ouvrir()/Fermer()**

Remplacer la méthode `Update` :

```csharp
private void Update()
{
    KeyCode toucheInv = OptionsManager.Instance != null
        ? OptionsManager.Instance.GetTouche(ActionJeu.Inventaire)
        : KeyCode.Tab;

    bool wheelVisible = _wheelRoot != null && _wheelRoot.activeSelf;

    if (Input.GetKeyDown(toucheInv) && !wheelVisible)
    {
        _wheelRoot?.SetActive(true);
        RafraichirSlots();
        _centreEcran             = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _positionSourisVirtuelle = _centreEcran;
    }

    if (Input.GetKeyUp(toucheInv) && wheelVisible)
    {
        SelectionnerSlot(_slotSelectionne);
        _wheelRoot?.SetActive(false);
    }

    if (_wheelRoot != null && _wheelRoot.activeSelf)
        MettreAJourSelection();
}
```

- [ ] **Step 3 : Override Fermer() — cacher _wheelRoot sur sortie de contexte**

Ajouter après `OnDisable` :

```csharp
protected override void OnDisable()
{
    if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false);
    base.OnDisable(); // UnregisterPanel
}

public override void Fermer()
{
    if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false);
    base.Fermer(); // SetActive(false) sur le panel GO
}
```

(Supprimer l'ancien `OnDisable` qui existait déjà et le remplacer par cette version.)

- [ ] **Step 4 : Supprimer l'ancien `OnEnable` dans `Start` (artefact du bug original)**

Dans `Start`, la ligne suivante est un vestige du bug original (elle cachait _wheelRoot après OnEnable pour masquer le problème). La supprimer :

```csharp
// SUPPRIMER cette ligne dans Start() :
if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false);
```

- [ ] **Step 5 : Vérifier en Play Mode**

1. Lancer le jeu, aller au Hub
2. La roue d'inventaire ne doit PAS s'afficher automatiquement
3. Appuyer et maintenir Tab → la roue apparaît
4. Relâcher Tab → la roue disparaît
5. Faire une mission, revenir au Hub — même comportement (pas de roue auto)

- [ ] **Step 6 : Commit**

```bash
git add Assets/_Script/UI_Persistent/InventaireWheel.cs
git commit -m "fix(ui): InventaireWheel - roue ne s'affiche plus automatiquement au changement de contexte"
```

---

## Task 6: MissionListUI.cs — Supprimer FindObjectOfType

**Files:**
- Modify: `Assets/_Script/Features/Mission/MissionListUI.cs`

**Problème :** `Awake` utilise `FindObjectOfType<HubManager>()`. Depuis `UI_Persistent`, au moment où MissionListUI s'éveille (si le panel est actif au lancement), la scène Hub n'est pas encore chargée — HubManager n'existe pas.

- [ ] **Step 1 : Remplacer FindObjectOfType par HubManager.Instance dans OnMissionSelectionnee**

Remplacer la méthode `Awake` et le champ `_hubManager` :

```csharp
// SUPPRIMER le champ :
// private HubManager _hubManager;

// REMPLACER la méthode Awake :
protected override void Awake()
{
    base.Awake(); // appel éventuel UIPanel
    _btnFermer?.onClick.AddListener(Fermer);
}
```

Et remplacer `OnMissionSelectionnee` :

```csharp
private void OnMissionSelectionnee(MissionData mission)
{
    var hub = BailiffCo.Hub.HubManager.Instance;
    if (hub == null)
    {
        Debug.LogWarning("[MissionListUI] HubManager.Instance introuvable !");
        return;
    }
    hub.SelectionnerMission(mission);
}
```

- [ ] **Step 2 : Conserver OnDestroy et vérifier la suppression de `_hubManager`**

S'assurer qu'`OnDestroy` est bien présent (retire le listener du bouton Fermer) :

```csharp
private void OnDestroy()
{
    _btnFermer?.onClick.RemoveAllListeners();
}
```

S'assurer qu'aucune référence à `_hubManager` ne reste dans le fichier.

- [ ] **Step 3 : Compiler et vérifier**

Attendre recompilation — aucune erreur sur MissionListUI.

- [ ] **Step 4 : Commit**

```bash
git add Assets/_Script/Features/Mission/MissionListUI.cs
git commit -m "fix(ui): MissionListUI - FindObjectOfType → HubManager.Instance"
```

---

## Task 7: OptionsUI.cs — Remplacer SetActive(false) par Fermer()

**Files:**
- Modify: `Assets/_Script/Features/Options/UI/OptionsUI.cs`

**Problème :** `OnAppliquer` et `OnAnnuler` appellent `gameObject.SetActive(false)` directement, contournant l'API UIPanel et donc `UnregisterPanel`.

- [ ] **Step 1 : Remplacer les deux appels directs**

Dans `OnAppliquer`, remplacer la dernière ligne :

```csharp
// AVANT
gameObject.SetActive(false); // → OnDisable → UnregisterPanel

// APRÈS
Fermer();
```

Dans `OnAnnuler`, remplacer la dernière ligne :

```csharp
// AVANT
gameObject.SetActive(false); // → OnDisable → UnregisterPanel

// APRÈS
Fermer();
```

- [ ] **Step 2 : Compiler et vérifier**

Attendre recompilation — aucune erreur sur OptionsUI.

- [ ] **Step 3 : Commit**

```bash
git add Assets/_Script/Features/Options/UI/OptionsUI.cs
git commit -m "fix(ui): OptionsUI - SetActive(false) → Fermer() pour respecter l'API UIPanel"
```

---

## Task 8: HubUI.cs — Nettoyage SetActive fallback

**Files:**
- Modify: `Assets/_Script/Features/Hub/HubUI.cs`

**Problème :** `OuvrirPanel(GameObject)` utilise `SetActive(true)` comme fallback si le GO n'a pas de composant UIPanel. Avec le nouveau système, tous les panels héritent de UIPanel — ce fallback ne doit plus exister. `FermerTousLesPanneaux` utilise aussi `SetActive(false)` directement.

- [ ] **Step 1 : Remplacer la méthode OuvrirPanel**

```csharp
private void OuvrirPanel(GameObject panel)
{
    panel?.GetComponent<UIPanel>()?.Ouvrir();
}
```

- [ ] **Step 2 : Remplacer FermerTousLesPanneaux**

```csharp
public void FermerTousLesPanneaux()
{
    _missionListUI?.Fermer();
    _panelBoutique?.GetComponent<UIPanel>()?.Fermer();
    _panelInventaire?.GetComponent<UIPanel>()?.Fermer();
    _panelGarage?.GetComponent<UIPanel>()?.Fermer();
    _popupErreur?.SetActive(false); // popup erreur reste SetActive (pas de UIPanel dessus)
}
```

- [ ] **Step 3 : Compiler et vérifier**

Attendre recompilation — aucune erreur sur HubUI.

- [ ] **Step 4 : Commit**

```bash
git add Assets/_Script/Features/Hub/HubUI.cs
git commit -m "refactor(ui): HubUI - supprimer fallback SetActive, tout passe par UIPanel.Ouvrir/Fermer"
```

---

## Task 9: Unity Editor — Scène UI_Persistent + configuration Inspector

**Outils :** Unity Editor uniquement. Pas de code C#.

### 9a — Créer la scène UI_Persistent

- [ ] **Step 1 : Créer la scène**

`File → New Scene → Empty` → Save as `Assets/Scenes/UI_Persistent.unity` (ou dans le dossier existant des scènes du projet).

- [ ] **Step 2 : Ajouter dans Build Settings**

`File → Build Settings → Add Open Scenes`. S'assurer que `UI_Persistent` est présente dans la liste (l'index n'importe pas — Bootstrap la charge par nom via `SceneNames.UI_PERSISTENT`).

### 9b — Déplacer les canvases

- [ ] **Step 3 : Identifier tous les canvases dans les scènes existantes**

Ouvrir chaque scène (Menu, Hub, Mission) et noter tous les GameObjects Canvas racine.

- [ ] **Step 4 : Déplacer les canvases vers UI_Persistent**

Pour chaque canvas :
1. Ouvrir la scène source en additif (`File → Open Scene → Additive`)
2. Ouvrir `UI_Persistent` en additif
3. Glisser le GO Canvas racine depuis la Hierarchy de la scène source vers la Hierarchy de `UI_Persistent`
4. Sauvegarder `UI_Persistent`
5. Sauvegarder la scène source (le canvas ne doit plus y être)

Canvases à déplacer :
- Depuis **Menu** : `Canvas_MenuUI` (ou équivalent)
- Depuis **Hub** : `Canvas_HubUI`, `Canvas_MissionList`, `Canvas_MissionPanel`, `Canvas_Vehicle`
- Depuis **Mission** : `Canvas_HUD`
- Depuis toute scène contenant : `Canvas_PauseMenu`, `Canvas_OptionsUI`, `Canvas_LabelInteraction`, `Canvas_InventaireWheel`, `Canvas_Crosshair`, `Canvas_FadeToBlack` (si pas déjà dans UI_Persistent)

- [ ] **Step 5 : Vérifier qu'aucune scène gameplay ne contient de canvas**

Ouvrir Menu, Hub, Mission séparément — la Hierarchy ne doit contenir aucun Canvas.

### 9c — Ajouter UIManager dans UI_Persistent

- [ ] **Step 6 : Créer ou déplacer le GameObject UIManager**

Si UIManager est actuellement dans Bootstrap ou une autre scène, le déplacer dans `UI_Persistent`. Créer un GameObject `UIManager` dans `UI_Persistent` et attacher `UIManager.cs`.

### 9d — Configurer l'Inspector sur chaque UIPanel

Pour chaque panel, sélectionner le GO dans `UI_Persistent`, puis configurer les champs `Panel Type`, `Contexte Visibles` et `Auto Afficher` selon la table ci-dessous.

- [ ] **Step 7 : Configurer chaque panel**

| Panel GameObject | Panel Type | Contexte Visibles | Auto Afficher |
|---|---|---|---|
| Canvas_MenuUI (MenuUI) | Blocking | Menu | ✓ |
| Canvas_HubUI (HubUI) | GameUI | Hub | ✓ |
| Canvas_MissionList (MissionListUI) | Blocking | Hub | ☐ |
| Canvas_MissionPanel (MissionPanelUI) | Blocking | Hub | ☐ |
| Canvas_Vehicle (VehiclePanelUI) | Blocking | Hub | ☐ |
| Canvas_HUD (HUDSystem) | GameUI | Mission | ✓ |
| Canvas_LabelInteraction (LabelInteractionUI) | GameUI | Hub, Mission | ✓ |
| Canvas_Crosshair | GameUI | Hub, Mission | ✓ |
| Canvas_InventaireWheel (InventaireWheel) | Overlay | Hub, Mission | ✓ |
| Canvas_PauseMenu (PauseMenu) | Blocking | Hub, Mission | ☐ |
| Canvas_OptionsUI (OptionsUI) | Blocking | Menu, Hub, Mission | ☐ |
| Canvas_FadeToBlack | GameUI | Menu, Hub, Mission | ☐ |

> **Note LabelInteractionUI :** `_autoAfficher = true` car le GO doit rester actif pour recevoir `OnInteractionLabelChanged`. Le panel gère lui-même l'affichage vide quand aucun interactable n'est proche.

> **Note InventaireWheel :** `_autoAfficher = true` car le GO doit rester actif pour que `Update()` puisse détecter la touche Tab. Le `_wheelRoot` (visuel de la roue) est géré séparément.

### 9e — Test de fumée complet

- [ ] **Step 8 : Test contexte Menu**

Lancer depuis Bootstrap. Vérifier :
- MenuUI visible ✓
- HUD masqué ✓
- PauseMenu masqué ✓
- Curseur visible ✓

- [ ] **Step 9 : Test contexte Hub**

Cliquer sur Jouer → Hub. Vérifier :
- MenuUI disparaît ✓
- HubUI visible ✓
- HUD masqué ✓
- PauseMenu masqué (apparaît sur ESC seulement) ✓
- InventaireWheel inactif visuellement (roue cachée) ✓
- Tab → roue apparaît ✓

- [ ] **Step 10 : Test contexte Mission**

Lancer une mission. Vérifier :
- HubUI disparaît ✓
- HUD visible ✓
- PauseMenu masqué (apparaît sur ESC seulement) ✓
- Aucun panel Hub visible ✓

- [ ] **Step 11 : Test retour au Menu**

Depuis PauseMenu en mission → Retour au menu. Vérifier :
- HUD masqué ✓
- PauseMenu masqué ✓
- MenuUI visible ✓
- Curseur visible ✓

- [ ] **Step 12 : Commit final**

```bash
git add Assets/Scenes/UI_Persistent.unity
git commit -m "feat(scene): UI_Persistent - tous les canvases centralisés, Inspector configuré"
```

---

## Résumé des dépendances entre tasks

```
Task 1 (UIPanel) → Task 2 (UIManager) → Task 9 (Unity)
Task 1           → Task 4 (MenuUI)
Task 1           → Task 5 (InventaireWheel)
Task 1           → Task 6 (MissionListUI)
Task 1           → Task 7 (OptionsUI)
Task 1           → Task 8 (HubUI)
Task 3 (Events)  → indépendant, peut être fait en parallèle de 4-8
Task 9           → dépend de 1-8 (tous les scripts doivent compiler)
```
