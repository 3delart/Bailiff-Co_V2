# Audit Refactoring Complet — Plan d'Implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Réorganiser l'architecture du projet (dossiers `_Script/` et `Assets/`), corriger les anti-patterns dans GameManager/HubManager/UIManager, et ajouter `CampaignMissionStarter` + `MissionLibreConfigUI`.

**Architecture:** 8 tâches C# (modifiables par agent, séquentielles) + 3 tâches manuelles Unity Editor. Les tâches Unity Editor sont regroupées à la fin — elles doivent être exécutées dans l'ordre car elles dépendent des scripts des tâches 1–8.

**Tech Stack:** Unity 2022+, C#, UnityEngine.UI (Slider, Button), ScriptableObject, EventBus générique, UIPanel system (spec `2026-05-07-ui-persistant-refonte-design.md`)

---

## Fichiers impactés

| Fichier | Action | Tâche |
|---|---|---|
| `_Core/Services/SceneNames.cs` | Modifier | 1 |
| `Features/Habitation/HabitationData.cs` | Modifier | 2 |
| `Features/Mission/Config/MissionData.cs` | Modifier | 3 |
| `_Core/Services/GameManager.cs` | Modifier | 4 |
| `UI_Persistent/UIManager.cs` | Modifier | 5 |
| `Features/Hub/HubManager.cs` | Modifier | 6 |
| `Systems/Mission/CampaignMissionStarter.cs` | Créer | 7 |
| `UI_Persistent/MissionLibreConfigUI.cs` | Créer | 8 |
| 23 scripts — déplacements dossiers | Déplacer (Unity Editor) | 9 |
| `Assets/Content/` + `Assets/Missions/` | Créer (Unity Editor) | 10 |
| `Mission_Libre.unity`, `Mission_01.unity` | Créer (Unity Editor) | 11 |

---

### Task 1: SceneNames.cs — Constantes MISSION_LIBRE + PLAYER_PREFAB_PATH

**Files:**
- Modify: `Assets/_Script/_Core/Services/SceneNames.cs`

- [ ] **Step 1: Remplacer le contenu complet**

```csharp
// ============================================================
// SceneNames.cs — Bailiff & Co  V2
// ============================================================
public static class SceneNames
{
    public const string BOOTSTRAP          = "Bootstrap";
    public const string UI_PERSISTENT      = "UI_Persistent";
    public const string MENU               = "Menu";
    public const string HUB                = "Hub";
    public const string MISSION_LIBRE      = "Mission_Libre";
    public const string PERSONNALISATION   = "CharacterCustomization";

    public const string PLAYER_PREFAB_PATH = "Prefabs/Player/PlayerRoot";
}
```

Note : `MISSION` (ancienne constante) est supprimée — elle est remplacée par `MissionData.SceneName` dynamique (Task 3) et `MISSION_LIBRE` pour le mode procédural.

- [ ] **Step 2: Vérifier la compilation**

Passer dans Unity → onglet Console → confirmer qu'aucune erreur de compilation n'apparaît. Si une erreur mentionne `SceneNames.MISSION`, passer à la Task 4 qui la corrige dans `GameManager`.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Script/_Core/Services/SceneNames.cs
git commit -m "refactor(core): SceneNames — MISSION_LIBRE + PLAYER_PREFAB_PATH, suppression MISSION hardcodé"
```

---

### Task 2: HabitationData.cs — Champ Prefab

**Context:** `Content/Habitations/[NomMaison]/` regroupe le `.prefab` et le `_Data.asset`. `HabitationData` doit référencer son prefab pour que `MissionLibreConfigUI` (Task 8) puisse construire un `MissionData` runtime avec le bon prefab de maison.

**Files:**
- Modify: `Assets/_Script/Features/Habitation/HabitationData.cs`

- [ ] **Step 1: Ajouter le champ Prefab**

Remplacer le contenu complet :

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/HabitationData")]
public class HabitationData : ScriptableObject
{
    [Header("Logement")]
    [Tooltip("Ex : Individuelle, Appartement, Loft…")]
    public string Type    = "Individuelle";

    [Tooltip("Surface en m²")]
    public int    Surface = 100;

    [Tooltip("Ex : Plain-pied, 1er étage, 2 + sous-sol…")]
    public string Etage   = "Plain-pied";

    [Tooltip("Ex : Porte, Porte + Garage, Digicode…")]
    public string Acces   = "Porte";

    [Header("Prefab")]
    [Tooltip("Prefab de la maison à instancier dans Mission_Libre (procédurale).")]
    public GameObject Prefab;
}
```

- [ ] **Step 2: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Script/Features/Habitation/HabitationData.cs"
git commit -m "feat(data): HabitationData — champ Prefab pour Mission_Libre"
```

---

### Task 3: MissionData.cs — Champ SceneName

**Context:** `GameManager.LancerMission()` hardcode `SceneNames.MISSION`. Le champ `SceneName` permet à chaque `MissionData` de déclarer sa scène cible. Une valeur vide signifie Mission_Libre (procédurale).

**Files:**
- Modify: `Assets/_Script/Features/Mission/Config/MissionData.cs`

- [ ] **Step 1: Ajouter le bloc Scene après la section Identity**

Ouvrir le fichier. Après le champ `MissionThumbnail` (ligne ~21) et avant `[Header("Owner")]`, insérer :

```csharp
    // ── SCÈNE ──────────────────────────────────────────────────
    [Header("Scene")]
    [Tooltip("Nom de la scène Unity pour cette mission (ex : 'Mission_01').\n" +
             "Laisser vide = Mission_Libre (procédurale via MissionBuilder).")]
    public string SceneName;
```

Le début du fichier doit ressembler à :

```csharp
[CreateAssetMenu(menuName = "BailiffCo/MissionData")]
public class MissionData : ScriptableObject
{
    // ── IDENTITY ─────────────────────────────────────────────
    [Header("Identity")]
    public string MissionName;
    public int    MissionNumber;
    [TextArea(2, 4)]
    public string BriefingText;
    public Sprite MissionThumbnail;

    // ── SCÈNE ──────────────────────────────────────────────────
    [Header("Scene")]
    [Tooltip("Nom de la scène Unity pour cette mission (ex : 'Mission_01').\n" +
             "Laisser vide = Mission_Libre (procédurale via MissionBuilder).")]
    public string SceneName;

    // ── OWNER ────────────────────────────────────────────────
    [Header("Owner")]
    public ProprietaireData Owner;
    // ... (reste inchangé)
```

- [ ] **Step 2: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Script/Features/Mission/Config/MissionData.cs"
git commit -m "feat(data): MissionData — champ SceneName pour chargement de scène dynamique"
```

---

### Task 4: GameManager.cs — Chargement dynamique + SetMissionSelectionnee()

**Context:** Trois changements : (1) `LancerMission()` lit `mission.SceneName` au lieu de `SceneNames.MISSION`. (2) `SetMissionSelectionnee()` expose un setter public sans déclencher de transition. (3) Le chemin Resources hardcodé utilise la constante `SceneNames.PLAYER_PREFAB_PATH`.

**Files:**
- Modify: `Assets/_Script/_Core/Services/GameManager.cs`

- [ ] **Step 1: Remplacer LancerMission()**

Trouver la méthode `LancerMission()` (vers la ligne 177). Remplacer son corps complet par :

```csharp
public void LancerMission(MissionData mission, VehiculeData vehicule)
{
    if (mission == null)
    {
        Debug.LogError("[GameManager] LancerMission : MissionData est null !");
        return;
    }

    MissionSelectionnee = mission;
    VehiculeSelectionne = vehicule;

    string scene = string.IsNullOrEmpty(mission.SceneName)
        ? SceneNames.MISSION_LIBRE
        : mission.SceneName;

    Debug.Log($"[GameManager] Mission : {mission.MissionName} | Scène : {scene} | " +
              $"Véhicule : {vehicule?.VehicleName ?? "aucun"}");

    SetContexte(ContexteJeu.Mission);
    SceneLoader.Instance.ChargerScene(scene);
}
```

- [ ] **Step 2: Ajouter SetMissionSelectionnee() après LancerMission()**

```csharp
/// <summary>
/// Stocke la mission sans déclencher de transition.
/// Appelé par HubManager lors de la sélection dans la liste.
/// </summary>
public void SetMissionSelectionnee(MissionData mission) => MissionSelectionnee = mission;
```

- [ ] **Step 3: Remplacer le chemin Resources hardcodé dans SpawnerPlayerSiNecessaire()**

Trouver la ligne :
```csharp
GameObject prefab = Resources.Load<GameObject>("Prefabs/Player/PlayerRoot");
if (prefab == null)
{
    Debug.LogError("[GameManager] PlayerRoot prefab introuvable dans Resources/Prefabs/Player/");
    return;
}
```

Remplacer par :
```csharp
GameObject prefab = Resources.Load<GameObject>(SceneNames.PLAYER_PREFAB_PATH);
if (prefab == null)
{
    Debug.LogError($"[GameManager] PlayerRoot prefab introuvable : {SceneNames.PLAYER_PREFAB_PATH}");
    return;
}
```

- [ ] **Step 4: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Script/_Core/Services/GameManager.cs
git commit -m "refactor(core): GameManager — chargement scène dynamique, SetMissionSelectionnee(), PLAYER_PREFAB_PATH"
```

---

### Task 5: UIManager.cs — GetPanel\<T\>() + OuvrirOptions()

**Context:** `HubManager` a besoin d'une API propre pour obtenir `HubUI` sans `FindObjectOfType`. `UIManager._tousLesPanels` contient déjà tous les panels enregistrés — un lookup générique suffit. `OuvrirOptions()` bénéficie aussi de cette API.

**Files:**
- Modify: `Assets/_Script/UI_Persistent/UIManager.cs`

- [ ] **Step 1: Ajouter GetPanel\<T\>() après GetHUDSystem()**

Trouver `GetHUDSystem()` (vers la ligne 148). Ajouter immédiatement après :

```csharp
/// <summary>
/// Retourne le premier panel de type T enregistré dans _tousLesPanels.
/// Retourne null si aucun panel de ce type n'est trouvé.
/// </summary>
public T GetPanel<T>() where T : UIPanel
{
    foreach (var panel in _tousLesPanels)
        if (panel is T typed) return typed;
    return null;
}
```

- [ ] **Step 2: Mettre à jour OuvrirOptions()**

Trouver la méthode `OuvrirOptions()`. Remplacer par :

```csharp
public void OuvrirOptions()
{
    GetPanel<OptionsUI>()?.Ouvrir();
}
```

- [ ] **Step 3: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Script/UI_Persistent/UIManager.cs
git commit -m "feat(ui): UIManager — GetPanel<T>() générique, OuvrirOptions() sans FindObjectOfType"
```

---

### Task 6: HubManager.cs — Suppression état dupliqué + FindObjectOfType

**Context:** `_missionSelectionnee` dans `HubManager` dupliquait `GameManager.MissionSelectionnee` — les deux pouvaient diverger. `FindObjectOfType<HubUI>()` est remplacé par `UIManager.Instance.GetPanel<HubUI>()`. `HubManager` délègue maintenant le stockage à `GameManager` via `SetMissionSelectionnee()`.

**Files:**
- Modify: `Assets/_Script/Features/Hub/HubManager.cs`

- [ ] **Step 1: Supprimer _missionSelectionnee de la section ÉTAT SESSION**

Trouver et supprimer la ligne :
```csharp
private MissionData  _missionSelectionnee;
```

- [ ] **Step 2: Mettre à jour InitialiserHub() — remplacer FindObjectOfType**

Trouver ce bloc dans `InitialiserHub()` :
```csharp
_hubUI = Object.FindObjectOfType<HubUI>(includeInactive: true);
if (_hubUI == null)
    Debug.LogWarning("[HubManager] HubUI introuvable dans UI_Persistent !");
```

Remplacer par :
```csharp
_hubUI = UIManager.Instance?.GetPanel<HubUI>();
if (_hubUI == null)
    Debug.LogWarning("[HubManager] HubUI introuvable via UIManager.GetPanel<HubUI>() !");
```

- [ ] **Step 3: Mettre à jour SelectionnerMission()**

Remplacer la méthode complète :
```csharp
public void SelectionnerMission(MissionData mission)
{
    if (mission == null)
    {
        Debug.LogWarning("[HubManager] SelectionnerMission : mission null ignorée.");
        return;
    }

    GameManager.Instance?.SetMissionSelectionnee(mission);
    Debug.Log($"[HubManager] Mission sélectionnée : {mission.MissionName}");

    _hubUI?.OuvrirPanelMissionDetail(mission);
    _hubUI?.MettreAJourMissionChoisie(mission.MissionName);
}
```

- [ ] **Step 4: Mettre à jour ConfirmerLocationEtPartir()**

Remplacer la méthode complète :
```csharp
public void ConfirmerLocationEtPartir()
{
    var mission = GameManager.Instance?.MissionSelectionnee;
    if (mission == null)
    {
        _hubUI?.AfficherErreur("Aucune mission sélectionnée !\nParle au Chef d'abord.");
        return;
    }

    if (_vehiculeSelectionne == null)
    {
        _hubUI?.AfficherErreur("Aucun véhicule sélectionné.");
        return;
    }

    float solde = GameManager.Instance?.Argent ?? 0f;
    if (solde < _prixLocationVehicule)
    {
        _hubUI?.AfficherErreur(
            $"Fonds insuffisants.\n" +
            $"Location : {_prixLocationVehicule:N0} €\n" +
            $"Ton solde : {solde:N0} €");
        return;
    }

    GameManager.Instance?.Debiter(_prixLocationVehicule);

    Debug.Log($"[HubManager] Départ → {mission.MissionName} " +
              $"avec {_vehiculeSelectionne.VehicleName} ({_prixLocationVehicule:N0} €)");

    GameManager.Instance?.LancerMission(mission, _vehiculeSelectionne);
}
```

- [ ] **Step 5: Mettre à jour les propriétés en bas du fichier**

Remplacer :
```csharp
public MissionData  MissionSelectionnee => _missionSelectionnee;
public bool         MissionChoisie      => _missionSelectionnee != null;
```

Par :
```csharp
public MissionData  MissionSelectionnee => GameManager.Instance?.MissionSelectionnee;
public bool         MissionChoisie      => MissionSelectionnee != null;
```

- [ ] **Step 6: Ajouter OuvrirMissionLibreConfig() dans la section UTILITAIRES**

```csharp
public void OuvrirMissionLibreConfig()
    => UIManager.Instance?.GetPanel<MissionLibreConfigUI>()?.Ouvrir();
```

- [ ] **Step 7: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 8: Test Play Mode — sélection de mission**

1. Press Play depuis `Bootstrap.unity`
2. Naviguer Menu → Hub
3. Interagir avec le Chef → MissionListUI s'affiche
4. Sélectionner une mission → Console : `[HubManager] Mission sélectionnée : [nom]`
5. Vérifier dans l'Inspector (Play Mode) : `GameManager.Instance.MissionSelectionnee` est renseigné
6. Zéro `NullReferenceException`

- [ ] **Step 9: Commit**

```bash
git add Assets/_Script/Features/Hub/HubManager.cs
git commit -m "refactor(hub): HubManager — supprimer _missionSelectionnee dupliqué, FindObjectOfType → GetPanel<HubUI>()"
```

---

### Task 7: CampaignMissionStarter.cs — Nouveau script

**Context:** Les scènes `Mission_01.unity`… sont hand-craftées. Ce script remplace `MissionBuilder` pour ces scènes : il spawne le joueur et le véhicule aux `Empty GameObjects` configurés, injecte les refs dans `ProprietaireAI`, et démarre `MissionSystem`.

**Files:**
- Create: `Assets/_Script/Systems/Mission/CampaignMissionStarter.cs`

- [ ] **Step 1: Créer le dossier Systems/Mission/**

```bash
mkdir -p "c:\BailiffCo_V2\Assets\_Script\Systems\Mission"
```

Note : Unity créera les `.meta` automatiquement à la prochaine ouverture.

- [ ] **Step 2: Créer CampaignMissionStarter.cs**

```csharp
// ============================================================
// CampaignMissionStarter.cs — Bailiff & Co  V2
// Démarre une scène mission hand-crafted (campagne).
// Remplace MissionBuilder pour les scènes Mission_XX.
//
// SETUP UNITY (chaque Mission_XX.unity) :
//   ├── MissionSetup (GameObject)
//   │   └── CampaignMissionStarter (ce script)
//   ├── SpawnPlayer  (Empty — positionner devant la porte)
//   └── SpawnVehicle (Empty — positionner sur la rue)
// ============================================================
using UnityEngine;

public class CampaignMissionStarter : MonoBehaviour
{
    [Header("Systèmes")]
    [SerializeField] private MissionSystem   _missionSystem;
    [SerializeField] private QuotaSystem     _quotaSystem;
    [SerializeField] private ParanoiaSystem  _paranoiaSystem;

    [Header("Spawn Points — GameObjects vides placés dans la scène")]
    [SerializeField] private Transform       _playerSpawnPoint;
    [SerializeField] private Transform       _vehicleSpawnPoint;

    [Header("Scène — refs hand-crafted")]
    [Tooltip("ProprietaireAI présent dans la scène (glissé depuis la hiérarchie).")]
    [SerializeField] private ProprietaireAI  _proprietaireAI;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        var mission = GameManager.Instance?.MissionSelectionnee;
        if (mission == null)
        {
            Debug.LogError("[CampaignMissionStarter] Aucune MissionData en cours — " +
                           "lancer la mission depuis le Hub.");
            return;
        }

        Build(mission);
    }

    // ================================================================
    // CONSTRUCTION
    // ================================================================

    private void Build(MissionData mission)
    {
        SpawnPlayer();
        var vehiculeSpawne = SpawnVehicle();
        InjecterRefsProprietaire(vehiculeSpawne);
        _missionSystem.StartMission(mission);

        Debug.Log($"[CampaignMissionStarter] Scène prête : {mission.MissionName}");
    }

    private void SpawnPlayer()
    {
        Transform pt = _playerSpawnPoint != null ? _playerSpawnPoint : transform;
        GameManager.Instance.SpawnerPlayerSiNecessaire(pt.position, pt.rotation);
    }

    private GameObject SpawnVehicle()
    {
        var vehicule = GameManager.Instance?.VehiculeSelectionne;
        if (vehicule?.Prefab == null || _vehicleSpawnPoint == null)
        {
            Debug.LogWarning("[CampaignMissionStarter] Pas de véhicule ou SpawnVehicle non assigné.");
            return null;
        }

        var spawned = Instantiate(vehicule.Prefab,
            _vehicleSpawnPoint.position,
            _vehicleSpawnPoint.rotation);
        spawned.name = $"Vehicle_{vehicule.VehicleName}";
        return spawned;
    }

    private void InjecterRefsProprietaire(GameObject vehicule)
    {
        if (_proprietaireAI == null) return;
        _proprietaireAI.SetReferences(
            GameManager.Instance.Player.transform,
            vehicule?.transform);
    }
}
```

- [ ] **Step 3: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Script/Systems/Mission/CampaignMissionStarter.cs"
git commit -m "feat(system): CampaignMissionStarter — démarre les scènes mission hand-crafted"
```

---

### Task 8: MissionLibreConfigUI.cs — Panel configuration Mission Libre

**Context:** Panel Hub (hérité de `UIPanel`, `_contexteVisibles = [Hub]`, `_autoAfficher = false`) permettant au joueur de configurer une mission procédurale. Construit un `MissionData` runtime depuis les choix UI et appelle `GameManager.LancerMission()`.

**Files:**
- Create: `Assets/_Script/UI_Persistent/MissionLibreConfigUI.cs`

- [ ] **Step 1: Créer MissionLibreConfigUI.cs**

```csharp
// ============================================================
// MissionLibreConfigUI.cs — Bailiff & Co  V2
// Panel Hub : configuration d'une mission libre procédurale.
// Hérite de UIPanel.
//
// SETUP UNITY (Inspector) :
//   _habitationsDisponibles   → glisser tous les HabitationData SO
//   _proprietairesDisponibles → glisser tous les ProprietaireData SO
//   _objetsSaisissables       → SeizableObjectEntry[] par défaut
//   _sliderReactivite         → Slider UI (min 1, max 10)
//   _sliderMethode            → Slider UI (min 1, max 10)
//   _sliderSociabilite        → Slider UI (min 1, max 10)
//   _btnConfirmer             → Button UI
//   panelType                 → Blocking (Inspector, base UIPanel)
//   _contexteVisibles         → Hub (Inspector, base UIPanel)
//   _autoAfficher             → false (Inspector, base UIPanel)
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class MissionLibreConfigUI : UIPanel
{
    // ================================================================
    // CONFIGURATION — assignée dans l'Inspector
    // ================================================================

    [Header("Pool d'assets disponibles")]
    [SerializeField] private HabitationData[]     _habitationsDisponibles;
    [SerializeField] private ProprietaireData[]   _proprietairesDisponibles;
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

    // ================================================================
    // INITIALISATION
    // ================================================================

    private void InitialiserSliders()
    {
        ConfigurerSlider(_sliderReactivite);
        ConfigurerSlider(_sliderMethode);
        ConfigurerSlider(_sliderSociabilite);
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
        if (HubManager.Instance?.VehiculeSelectionne == null)
        {
            Debug.LogWarning("[MissionLibreConfigUI] Aucun véhicule sélectionné — louer un véhicule d'abord.");
            return false;
        }
        return true;
    }

    private MissionData ConstruireMissionData()
    {
        var habitation    = _habitationsDisponibles[_habitationIndex];
        var proprio       = ConstruireProprietaire();

        var runtime               = ScriptableObject.CreateInstance<MissionData>();
        runtime.MissionName       = "Mission Libre";
        runtime.SceneName         = "";                 // vide → charge SceneNames.MISSION_LIBRE
        runtime.Habitation        = habitation;
        runtime.HousePrefab       = habitation.Prefab;
        runtime.Owner             = proprio;
        runtime.OwnerPrefab       = proprio?.OwnerPrefab;
        runtime.SeizableObjects   = _objetsSaisissables;

        return runtime;
    }

    private ProprietaireData ConstruireProprietaire()
    {
        if (_proprietairesDisponibles == null || _proprietairesDisponibles.Length == 0)
        {
            Debug.LogWarning("[MissionLibreConfigUI] Aucun ProprietaireData configuré.");
            return null;
        }

        int idx = (_proprietaireIndex >= 0 && _proprietaireIndex < _proprietairesDisponibles.Length)
            ? _proprietaireIndex
            : Random.Range(0, _proprietairesDisponibles.Length);

        // Copie runtime — ne modifie pas l'asset source
        var runtime        = Instantiate(_proprietairesDisponibles[idx]);
        runtime.Reactivity = Mathf.RoundToInt(_sliderReactivite?.value  ?? 5f);
        runtime.Method     = Mathf.RoundToInt(_sliderMethode?.value     ?? 5f);
        runtime.Sociability= Mathf.RoundToInt(_sliderSociabilite?.value ?? 5f);

        return runtime;
    }
}
```

- [ ] **Step 2: Vérifier la compilation**

Unity Console → aucune erreur.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Script/UI_Persistent/MissionLibreConfigUI.cs
git commit -m "feat(ui): MissionLibreConfigUI — panel Hub pour configurer et lancer Mission Libre"
```

---

### Task 9: [MANUEL — Unity Editor] Réorganisation dossiers `_Script/`

**⚠️ Effectuer dans Unity Editor (Project window → drag & drop) pour préserver les GUIDs des `.meta` et maintenir les références dans les scènes/prefabs.**

- [ ] **Step 1: Créer les dossiers cibles dans Assets/_Script/**

Project window → `Assets/_Script` :
- Clic droit → Create Folder → **`_Data`**
- Clic droit → Create Folder → **`Systems`**
  - Dans `Systems` → créer : `Player`, `Proprio`, `Inventory`
  - (`Systems/Mission/` existe déjà depuis Task 7)

- [ ] **Step 2: Déplacer les scripts vers _Data/**

Glisser (drag & drop) dans Project window vers `Assets/_Script/_Data/` :

| Fichier | Dossier source |
|---|---|
| `AnimalData.cs` | `Features/Animals/` |
| `CachetteData.cs` | `Features/Cachette/` |
| `HabitationData.cs` | `Features/Habitation/` |
| `MissionData.cs` | `Features/Mission/Config/` |
| `MissionListData.cs` | `Features/Mission/Config/` |
| `NeighbourData.cs` | `Features/Voisins/` |
| `ObjetData.cs` | `Features/Interactables/Objects/` |
| `OutilData.cs` | `Features/Outils/` |
| `ParanoiaConfig.cs` | `Features/Propriétaire/Paranoia/` |
| `PiegeData.cs` | `Features/Trap/` |
| `PlayerConfigData.cs` | `Features/Player/` |
| `ProprietaireData.cs` | `Features/Propriétaire/` |
| `VehiculeData.cs` | `Features/Vehicule/` |

- [ ] **Step 3: Déplacer les scripts vers Systems/**

| Fichier | Vers |
|---|---|
| `PlayerController.cs` | `Systems/Player/` |
| `PlayerCarry.cs` | `Systems/Player/` |
| `PlayerInteractor.cs` | `Systems/Player/` |
| `PlayerAnimator.cs` | `Systems/Player/` |
| `PlayerNoiseEmitter.cs` | `Systems/Player/` |
| `MissionSystem.cs` | `Systems/Mission/` |
| `MissionBuilder.cs` | `Systems/Mission/` |
| `QuotaSystem.cs` | `Systems/Mission/` |
| `ProprietaireAI.cs` | `Systems/Proprio/` |
| `ParanoiaSystem.cs` | `Systems/Proprio/` |
| `InventaireSystem.cs` | `Systems/Inventory/` |

`CampaignMissionStarter.cs` est déjà dans `Systems/Mission/` (Task 7). ✓

- [ ] **Step 4: Supprimer les dossiers devenus vides**

Supprimer dans Project window :
- `Features/Animals/`
- `Features/Cachette/`
- `Features/Habitation/`
- `Features/Mission/Config/` (si vide)
- `Features/Voisins/`
- `Features/Outils/`
- `Features/Trap/`
- `Features/Player/`
- `Features/Inventory/`
- `Features/Propriétaire/Paranoia/`
- `Features/Propriétaire/` (si vide)

- [ ] **Step 5: Vérifier**

Unity Console → 0 erreur de compilation, 0 `Missing Script` dans les scènes ouvertes.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Script/
git commit -m "refactor(structure): _Script — déplacer Data vers _Data/, systèmes vers Systems/"
```

---

### Task 10: [MANUEL — Unity Editor] Structure Assets — Content/ + Missions/

- [ ] **Step 1: Créer Assets/Content/**

Project window → `Assets` → créer `Content/` avec sous-dossiers :
`Habitations/`, `Propriétaires/`, `Objets/`, `Véhicules/`, `Outils/`, `Animaux/`, `Pièges/`

- [ ] **Step 2: Organiser les assets existants dans Content/**

Pour chaque maison, proprio, objet, véhicule existant dans le projet :
1. Créer un sous-dossier `Content/[TypePluriel]/[NomAsset]/`
2. Glisser le `.prefab`, le `.fbx`, et le `_Data.asset` dans ce dossier
3. Sélectionner le `HabitationData_Data.asset` → champ **Prefab** → glisser le `.prefab` correspondant

- [ ] **Step 3: Créer Assets/Missions/**

```
Assets/Missions/
├── Campaign/
│   └── MissionList_Campaign.asset   (MissionListData SO)
└── Libre/
    └── MissionList_Libre.asset      (MissionListData SO)
```

Créer via : clic droit → Create → **BailiffCo/MissionListData**

- [ ] **Step 4: Vérifier**

Ouvrir chaque `HabitationData` SO → champ `Prefab` assigné. Ouvrir chaque `ProprietaireData` SO → champ `OwnerPrefab` assigné.

- [ ] **Step 5: Commit**

```bash
git add Assets/Content/ Assets/Missions/
git commit -m "refactor(assets): Content/ et Missions/ — organisation pool assets réutilisables"
```

---

### Task 11: [MANUEL — Unity Editor] Scènes Mission_Libre + Mission_01

- [ ] **Step 1: Créer Assets/Scenes/Campaign/**

Project window → `Assets/Scenes` → Create Folder → `Campaign`

- [ ] **Step 2: Créer Mission_Libre.unity**

File → New Scene → Empty. Sauvegarder : `Assets/Scenes/Mission_Libre.unity`.

Hierarchy à construire :
```
Mission_Libre
├── MissionBuilder    → script MissionBuilder.cs
├── MissionSystem     → script MissionSystem.cs
├── QuotaSystem       → script QuotaSystem.cs
├── ParanoiaSystem    → script ParanoiaSystem.cs
├── MusicSource       → AudioSource component
└── Ancrages
    ├── AncrageMaison       (Empty)
    ├── AncrageVehicule     (Empty)
    └── AncrageProprietaire (Empty)
```

Dans l'Inspector de **MissionBuilder** : assigner `_anchorHouse`, `_anchorVehicle`, `_anchorOwner`, `_missionSystem`, `_quotaSystem`, `_paranoiaSystem`, `_musicSource`.

- [ ] **Step 3: Créer Mission_01.unity**

File → New Scene → Empty. Sauvegarder : `Assets/Scenes/Campaign/Mission_01.unity`.

Hierarchy à construire :
```
Mission_01
├── MissionSetup        → script CampaignMissionStarter.cs
├── MissionSystem       → script MissionSystem.cs
├── QuotaSystem         → script QuotaSystem.cs
├── ParanoiaSystem      → script ParanoiaSystem.cs
├── SpawnPlayer         (Empty — positionner devant la porte)
├── SpawnVehicle        (Empty — positionner sur la rue)
├── [Maison prefab]     (glisser depuis Content/Habitations/)
└── [Proprio prefab]    (glisser depuis Content/Propriétaires/)
```

Dans l'Inspector de **CampaignMissionStarter** : assigner tous les champs.

- [ ] **Step 4: Créer Mission_01_Data.asset**

Project window → `Assets/Missions/Campaign/` → Create Folder `Mission_01/` → clic droit → Create → **BailiffCo/MissionData**.
- `MissionName` = `"Mission 01"`
- `MissionNumber` = `1`
- `SceneName` = `"Mission_01"`
- Assigner `Owner`, `Habitation`, `SeizableObjects`

Ajouter ce SO à `MissionList_Campaign.asset`.

- [ ] **Step 5: Ajouter un Canvas MissionLibreConfigUI dans UI_Persistent.unity**

Ouvrir `UI_Persistent.unity`. Ajouter :
```
Canvas_MissionLibreConfig
└── [Layout panel avec habitations, sliders, bouton Confirmer]
    └── script MissionLibreConfigUI.cs
```
Inspector UIPanel : `panelType = Blocking`, `_contexteVisibles = [Hub]`, `_autoAfficher = false`.
Assigner dans `MissionLibreConfigUI` : `_habitationsDisponibles`, `_proprietairesDisponibles`, sliders, bouton confirmer.

Ajouter un bouton **"Mode Libre"** dans `Canvas_HubUI` → `OnClick` → `HubManager.OuvrirMissionLibreConfig()`.

- [ ] **Step 6: Build Settings — mettre à jour**

File → Build Settings :
- Index 0 : `Bootstrap`
- Ajouter : `UI_Persistent`, `Menu`, `Hub`, `CharacterCustomization`, `Mission_Libre`, `Campaign/Mission_01`
- Supprimer l'ancienne `Mission` si elle y était

- [ ] **Step 7: Test flux campagne**

1. Press Play depuis `Bootstrap.unity`
2. Menu → Hub → Chef → sélectionner Mission 01 → véhicule → Confirmer départ
3. Vérifier : `Mission_01.unity` se charge, player au SpawnPlayer, véhicule au SpawnVehicle
4. Console : `[CampaignMissionStarter] Scène prête : Mission 01`
5. Terminer → retour Hub sans erreur

- [ ] **Step 8: Test flux Mission Libre**

1. Hub → bouton "Mode Libre" → `MissionLibreConfigUI` s'ouvre
2. Choisir une habitation, ajuster les sliders, Confirmer
3. Vérifier : `Mission_Libre.unity` se charge
4. Console : `[MissionBuilder] Construction terminée`

- [ ] **Step 9: Commit final**

```bash
git add Assets/Scenes/
git commit -m "feat(scenes): Mission_Libre procédurale + Mission_01 campagne hand-crafted"
```

---

## Self-review

**Couverture spec :**
| Exigence spec | Tâche |
|---|---|
| `_Data/` centralisé (scripts) | Task 9 |
| `Systems/` Player, Mission, Proprio, Inventory | Tasks 7 + 9 |
| `Content/` Habitations, Propriétaires, Objets… | Task 10 |
| `MissionData.SceneName` | Task 3 |
| `GameManager` chargement dynamique | Task 4 |
| `GameManager.SetMissionSelectionnee()` | Task 4 |
| `SceneNames.PLAYER_PREFAB_PATH` | Task 1 |
| `SceneNames.MISSION_LIBRE` | Task 1 |
| `UIManager.GetPanel<T>()` | Task 5 |
| `HubManager` état dupliqué supprimé | Task 6 |
| `HubManager` FindObjectOfType supprimé | Task 6 |
| `CampaignMissionStarter` (SpawnPlayer + SpawnVehicle) | Task 7 |
| `MissionLibreConfigUI` (sliders + habitation picker) | Task 8 |
| `HabitationData.Prefab` | Task 2 |
| `Mission_01.unity` hand-crafted | Task 11 |
| `Mission_Libre.unity` procédurale | Task 11 |
| `MissionList_Campaign.asset` + `MissionList_Libre.asset` | Task 10 |
| Build Settings mis à jour | Task 11 |
| Bouton "Mode Libre" → `OuvrirMissionLibreConfig()` | Tasks 6 + 11 |

**Cohérence types :**
- `SetMissionSelectionnee(MissionData)` défini Task 4, utilisé Task 6 ✓
- `GetPanel<T>()` défini Task 5, utilisé Tasks 6 + 8 ✓
- `HabitationData.Prefab` défini Task 2, utilisé Task 8 ✓
- `SceneNames.MISSION_LIBRE` défini Task 1, utilisé Task 4 ✓
- `SceneNames.PLAYER_PREFAB_PATH` défini Task 1, utilisé Task 4 ✓
- `CampaignMissionStarter.SpawnVehicle()` retourne `GameObject`, passé à `InjecterRefsProprietaire(GameObject)` ✓
- `MissionLibreConfigUI.ConstruireProprietaire()` retourne `ProprietaireData`, assigné à `runtime.Owner` ✓
