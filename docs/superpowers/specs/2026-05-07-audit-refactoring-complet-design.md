# Audit Refactoring Complet — Bailiff & Co V2
**Date :** 2026-05-07
**Projet :** Bailiff & Co V2

---

## Objectif

Réorganiser l'architecture complète du projet (Assets et Scripts), introduire les scènes de missions campagne hand-craftées, ajouter la Mission Libre procédurale, corriger les anti-patterns identifiés, et simplifier le chemin de gameplay. Le refactoring UI en cours (`2026-05-07-ui-persistant-refonte`) reste prioritaire et se poursuit en parallèle — ce plan ne le touche pas.

---

## Contexte & état actuel

- **65 scripts C#**, **6 scènes**, refactoring UI en cours (14/23 tâches complètes)
- `_Script/` mélange données, logique, UI et systèmes sans séparation nette
- `Features/` contient à la fois des systèmes génériques (Player, Proprio, Inventory) et des features spécifiques (Hub, Options, Pause)
- `xxData` et `xxConfig` dispersés dans leurs dossiers features respectifs
- **État actuel (avant refactoring) :** 1 seule scène `Mission.unity` + MissionBuilder procédural pour toutes les missions → **cible :** scènes `Mission_01.unity`…`Mission_0N.unity` hand-craftées + `Mission_Libre.unity` procédurale
- `HubManager` duplique l'état de mission de `GameManager`
- `FindObjectOfType` dans l'initialisation de `HubManager`
- `GameManager.LancerMission()` hardcode `SceneNames.MISSION`

---

## 1. Structure Assets (dossiers Unity)

### Avant
```
Assets/
  _Script/
  Scenes/         (flat — Bootstrap, Menu, Hub, Mission, CharacterCustomization, UI_Persistent)
  [prefabs/fbx/SO dispersés sans organisation claire]
```

### Après
```
Assets/
├── _Script/                        (voir Section 2)
│
├── Content/                        ← tout le contenu de jeu réutilisable
│   ├── Habitations/
│   │   └── [NomMaison]/
│   │       ├── [NomMaison].prefab
│   │       ├── [NomMaison].fbx
│   │       └── [NomMaison]_Data.asset      (HabitationData SO)
│   ├── Propriétaires/
│   │   └── [NomProprio]/
│   │       ├── [NomProprio].prefab
│   │       ├── [NomProprio].fbx
│   │       └── [NomProprio]_Data.asset     (ProprietaireData SO)
│   ├── Objets/                             (saisissables — TV, bijou, tableau...)
│   │   └── [NomObjet]/
│   │       ├── [NomObjet].prefab
│   │       └── [NomObjet]_Data.asset       (ObjetData SO)
│   ├── Véhicules/
│   │   └── [NomVehicule]/
│   │       ├── [NomVehicule].prefab
│   │       └── [NomVehicule]_Data.asset    (VehiculeData SO)
│   ├── Outils/
│   ├── Animaux/
│   └── Pièges/
│
├── Missions/
│   ├── Campaign/
│   │   ├── Mission_01/
│   │   │   └── Mission_01_Data.asset       (MissionData SO — SceneName = "Mission_01")
│   │   ├── Mission_02/
│   │   │   └── Mission_02_Data.asset
│   │   └── MissionList_Campaign.asset      (MissionListData SO — liste campagne)
│   └── Libre/
│       └── MissionList_Libre.asset         (MissionListData SO — pool pour Mission_Libre)
│
└── Scenes/
    ├── Core/
    │   ├── Bootstrap.unity
    │   └── UI_Persistent.unity
    ├── Menu.unity
    ├── CharacterCustomization.unity
    ├── Hub.unity
    ├── Campaign/
    │   ├── Mission_01.unity                ← hand-crafted (prefabs glissés depuis Content/)
    │   └── Mission_02.unity
    └── Mission_Libre.unity                 ← procédurale (MissionBuilder)
```

### Règle de nommage assets
- ScriptableObjects : `[Nom]_Data.asset` pour les données entité, `[Nom]_Config.asset` pour les paramètres système
- Prefabs : `[Nom].prefab` sans suffixe
- Listes : `MissionList_[Type].asset`

---

## 2. Structure `_Script/`

### Avant
```
_Script/
  _Core/            (Events, Interfaces, Services, SharedData)
  Bootstrap/
  Features/         (mélange : systèmes génériques + features scène-spécifiques)
  UI_Persistent/
```

### Après
```
Assets/_Script/
│
├── _Core/                          ← infrastructure pure (inchangé)
│   ├── Events/
│   │   ├── EventBus.cs
│   │   ├── EventBusHelper.cs
│   │   └── GameEvents.cs
│   ├── Interfaces/
│   │   └── IInteractable.cs
│   ├── Services/
│   │   ├── GameManager.cs
│   │   ├── SceneLoader.cs
│   │   └── SceneNames.cs
│   └── SharedData/
│       ├── Enums.cs
│       └── MissionResult.cs
│
├── _Data/                          ← TOUS les scripts ScriptableObject
│   ├── AnimalData.cs
│   ├── CachetteData.cs
│   ├── HabitationData.cs
│   ├── MissionData.cs              ← + champ SceneName (voir Section 3-A)
│   ├── MissionListData.cs
│   ├── NeighbourData.cs
│   ├── ObjetData.cs
│   ├── OutilData.cs
│   ├── ParanoiaConfig.cs           ← déplacé depuis Features/Propriétaire/Paranoia/
│   ├── PiegeData.cs
│   ├── PlayerConfigData.cs
│   ├── ProprietaireData.cs
│   └── VehiculeData.cs
│
├── Systems/                        ← systèmes génériques (anciennement dans Features/)
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   ├── PlayerCarry.cs
│   │   ├── PlayerInteractor.cs
│   │   ├── PlayerAnimator.cs
│   │   └── PlayerNoiseEmitter.cs
│   ├── Mission/
│   │   ├── MissionSystem.cs
│   │   ├── MissionBuilder.cs       ← utilisé uniquement par Mission_Libre
│   │   ├── QuotaSystem.cs
│   │   └── CampaignMissionStarter.cs   ← NOUVEAU (voir Section 3-C)
│   ├── Proprio/
│   │   ├── ProprietaireAI.cs
│   │   └── ParanoiaSystem.cs
│   └── Inventory/
│       └── InventaireSystem.cs
│
├── Features/                       ← logique scène-spécifique uniquement
│   ├── Hub/
│   │   ├── HubManager.cs           ← corrigé (voir Section 3-B)
│   │   └── HubPNJ.cs
│   ├── Options/
│   │   ├── OptionsManager.cs
│   │   ├── OptionsData.cs          ← reste ici (lié à Options uniquement)
│   │   └── OptionsRepository.cs
│   ├── Pause/
│   │   └── PauseMenuTrigger.cs
│   ├── Vehicule/
│   │   ├── VehicleHubSlot.cs
│   │   ├── VehicleRuntime.cs
│   │   ├── VehicleTrunkZone.cs
│   │   └── VehicleAmbiance.cs
│   ├── Interactables/
│   │   ├── FurnitureInteractable.cs
│   │   ├── DrawerInteractable.cs
│   │   ├── OpenableInteractable.cs
│   │   └── FurnitureConfig.cs
│   └── Objets/
│       └── ValueObject.cs
│
├── UI_Persistent/                  ← inchangé (refactoring UI en cours)
│   ├── UIPanel.cs
│   ├── UIManager.cs                ← + GetPanel<T>() (voir Section 3-B)
│   ├── HUDSystem.cs
│   ├── CrosshairUI.cs
│   ├── InventaireWheel.cs
│   ├── LabelInteractionUI.cs
│   ├── HubUI.cs
│   ├── MissionListUI.cs
│   ├── MissionPanelUI.cs
│   ├── VehiclePanelUI.cs
│   ├── MissionLibreConfigUI.cs     ← NOUVEAU (voir Section 3-D)
│   ├── MenuUI.cs
│   ├── OptionsUI.cs
│   ├── KeyRebindUI.cs
│   ├── RebindOverlay.cs
│   └── PauseMenu.cs
│
└── Bootstrap/
    ├── BootstrapLoader.cs
    └── RuntimeBootstrapper.cs
```

---

## 3. Corrections anti-patterns

### A. `MissionData` + chargement de scène dynamique

**Problème :** `GameManager.LancerMission()` hardcode `SceneNames.MISSION` — impossible de charger une scène campagne spécifique.

**Solution :** Ajouter un champ `SceneName` à `MissionData`. Si vide → Mission Libre.

```csharp
// MissionData.cs — ajout
[Header("Scène")]
[Tooltip("Nom de la scène Unity pour cette mission. Vide = Mission_Libre (procédurale).")]
[SerializeField] public string SceneName;
```

```csharp
// GameManager.LancerMission() — adapté
public void LancerMission(MissionData mission, VehiculeData vehicule)
{
    MissionSelectionnee = mission;
    VehiculeSelectionne = vehicule;

    string scene = string.IsNullOrEmpty(mission.SceneName)
        ? SceneNames.MISSION_LIBRE
        : mission.SceneName;

    SetContexte(ContexteJeu.Mission);
    SceneLoader.Instance.ChargerScene(scene);
}
```

```csharp
// SceneNames.cs — ajout
public const string MISSION_LIBRE = "Mission_Libre";
// Campaign scenes référencées par leur MissionData.SceneName directement
```

---

### B. `HubManager` — suppression état dupliqué + FindObjectOfType

**Problème 1 :** `HubManager._missionSelectionnee` duplique `GameManager.MissionSelectionnee`. Les deux peuvent diverger.

**Solution :** `HubManager` ne stocke plus la mission — il délègue à `GameManager`.

```csharp
// HubManager.cs — AVANT
private MissionData _missionSelectionnee;

public void SelectionnerMission(MissionData mission)
{
    _missionSelectionnee = mission;
    _hubUI?.OuvrirPanelMissionDetail(mission);
}
```

```csharp
// HubManager.cs — APRÈS
// Plus de _missionSelectionnee local

public void SelectionnerMission(MissionData mission)
{
    // Stocke directement dans GameManager via setter interne
    GameManager.Instance.SetMissionSelectionnee(mission);
    _hubUI?.OuvrirPanelMissionDetail(mission);
}

// Propriété en lecture — source unique de vérité
public MissionData MissionSelectionnee => GameManager.Instance.MissionSelectionnee;
public bool MissionChoisie => MissionSelectionnee != null;
```

```csharp
// GameManager.cs — nouveau setter interne
public void SetMissionSelectionnee(MissionData mission) => MissionSelectionnee = mission;
```

**Problème 2 :** `FindObjectOfType<HubUI>(includeInactive: true)` dans `InitialiserHub()`.

**Solution :** `UIManager` expose `GetPanel<T>()` qui fait le lookup sur sa liste interne `_tousLesPanels`.

```csharp
// UIManager.cs — nouveau helper
public T GetPanel<T>() where T : UIPanel
{
    foreach (var panel in _tousLesPanels)
        if (panel is T typed) return typed;
    return null;
}
```

```csharp
// HubManager.InitialiserHub() — APRÈS
_hubUI = UIManager.Instance.GetPanel<HubUI>();
```

---

### C. `CampaignMissionStarter` — script pour scènes hand-crafted

**Contexte :** Les scènes `Mission_01.unity`, `Mission_02.unity`, etc. sont entièrement manuelles. Le MissionBuilder n'est pas présent. Ce script minimal démarre les systèmes et spawne le joueur.

```csharp
// Systems/Mission/CampaignMissionStarter.cs
public class CampaignMissionStarter : MonoBehaviour
{
    [Header("Systèmes")]
    [SerializeField] private MissionSystem   _missionSystem;
    [SerializeField] private QuotaSystem     _quotaSystem;
    [SerializeField] private ParanoiaSystem  _paranoiaSystem;

    [Header("Spawn Points — Empty GameObjects placés dans la scène")]
    [SerializeField] private Transform       _playerSpawnPoint;   // Empty "SpawnPlayer"
    [SerializeField] private Transform       _vehicleSpawnPoint;  // Empty "SpawnVehicle"

    [Header("Scène — refs hand-crafted")]
    [SerializeField] private ProprietaireAI  _proprietaireAI;    // glissé depuis la hiérarchie

    private void Start()
    {
        var mission = GameManager.Instance?.MissionSelectionnee;
        if (mission == null)
        {
            Debug.LogError("[CampaignMissionStarter] Aucune MissionData en cours !");
            return;
        }

        // Spawner le joueur au point dédié
        GameManager.Instance.SpawnerPlayerSiNecessaire(
            _playerSpawnPoint.position,
            _playerSpawnPoint.rotation);

        // Spawner le véhicule sélectionné au point dédié
        GameObject spawnedVehicle = null;
        var vehicule = GameManager.Instance.VehiculeSelectionne;
        if (vehicule?.Prefab != null && _vehicleSpawnPoint != null)
        {
            spawnedVehicle = Instantiate(vehicule.Prefab,
                _vehicleSpawnPoint.position,
                _vehicleSpawnPoint.rotation);
        }

        // Injection directe — pas de FindObjectOfType
        if (_proprietaireAI != null)
        {
            _proprietaireAI.SetReferences(
                GameManager.Instance.Player.transform,
                spawnedVehicle?.transform);
        }

        _missionSystem.StartMission(mission);
    }
}
```

**Setup Unity par scène campagne :**
- Hierarchy : `MissionSetup` (GameObject) avec `CampaignMissionStarter`
- Dans la scène : deux GameObjects vides — `SpawnPlayer` et `SpawnVehicle` — positionnés à la main
- Références Inspector : MissionSystem, QuotaSystem, ParanoiaSystem, SpawnPlayer, SpawnVehicle, ProprietaireAI
- Aucun `FindObjectOfType` — toutes les refs assignées dans l'Inspector

---

### D. `MissionLibreConfigUI` — panneau configuration Mission Libre

**Contexte :** Panel accessible depuis le Hub. Le joueur configure ses paramètres, le panel construit un `MissionData` runtime et appelle `GameManager.LancerMission()`.

**Champs UI :**
- Sélecteur d'habitation (liste des HabitationData disponibles)
- Slider niveau de paranoïa du proprio (remplace la valeur de ProprietaireData)
- Sélecteur de proprio (optionnel — si non choisi, un proprio random est tiré)
- Bouton Confirmer

**Logique :**
```csharp
// MissionLibreConfigUI.cs (simplifié)
private void ConfirmerEtLancer()
{
    // Construit un MissionData runtime depuis les choix du joueur
    var runtimeMission = ScriptableObject.CreateInstance<MissionData>();
    runtimeMission.MissionName    = "Mission Libre";
    runtimeMission.SceneName      = "";                    // → charge Mission_Libre
    runtimeMission.HousePrefab    = _habitationChoisie?.Prefab;
    runtimeMission.Owner          = BuildRuntimeOwner();   // applique les sliders
    runtimeMission.SeizableObjects = _objetsPools;

    VehiculeData vehicule = HubManager.Instance.VehiculeSelectionne;
    GameManager.Instance.LancerMission(runtimeMission, vehicule);
}
```

**Visibilité panel :** `_contexteVisibles = [Hub]`, `_autoAfficher = false`, déclenché par bouton "Mode Libre" dans HubUI.

---

### E. Chemin Resources Player — constante explicite

**Problème :** `Resources.Load<GameObject>("Prefabs/Player/PlayerRoot")` — chemin string en dur fragile.

**Solution :** Constante dans `SceneNames` (ou nouveau `GameConstants.cs`) :

```csharp
// SceneNames.cs — ajout
public const string PLAYER_PREFAB_PATH = "Prefabs/Player/PlayerRoot";
```

```csharp
// GameManager.cs — utilisation
GameObject prefab = Resources.Load<GameObject>(SceneNames.PLAYER_PREFAB_PATH);
```

---

## 4. Chemin de gameplay

### Flux complet
```
Bootstrap
  └── charge UI_Persistent (additive, permanent)
  └── charge Menu

Menu
  ├── Jouer         → Hub
  ├── Personnalisation → CharacterCustomization → Menu
  └── Options       → OptionsUI (panel persistant)

CharacterCustomization
  └── Sauvegarder   → GameManager.SauvegarderPersonnalisation() → retour Menu
  (skin appliqué au spawn Hub — non modifiable en Hub/Mission)

Hub
  ├── PNJ Chef      → MissionListUI (campagne)
  ├── Bouton Libre  → MissionLibreConfigUI
  ├── Véhicule      → VehiclePanelUI → confirmer location
  └── Confirmer départ → GameManager.LancerMission()
                         └── Mission_XX.unity (campagne, hand-crafted)
                         └── Mission_Libre.unity (procédurale, MissionBuilder)

Mission_XX / Mission_Libre
  └── TerminerMission → GameManager.TerminerMission() → Hub
```

### Ce qui ne change pas
- `BootstrapLoader.cs` — déjà correct
- Logique EventBus et `OnContextChanged`
- `UIManager.UpdateUIState()` (curseur/input)
- `SceneLoader` fade system
- `ContexteJeu` enum et transitions

---

## 5. Résumé des fichiers impactés

### Scripts à déplacer (refactoring dossier — namespace inchangé)
| Fichier | Depuis | Vers |
|---|---|---|
| `AnimalData.cs` | `Features/Animals/` | `_Data/` |
| `CachetteData.cs` | `Features/Cachette/` | `_Data/` |
| `HabitationData.cs` | `Features/Habitation/` | `_Data/` |
| `MissionData.cs` | `Features/Mission/Config/` | `_Data/` |
| `MissionListData.cs` | `Features/Mission/Config/` | `_Data/` |
| `NeighbourData.cs` | `Features/Voisins/` | `_Data/` |
| `ObjetData.cs` | `Features/Interactables/Objects/` | `_Data/` |
| `OutilData.cs` | `Features/Outils/` | `_Data/` |
| `ParanoiaConfig.cs` | `Features/Propriétaire/Paranoia/` | `_Data/` |
| `PiegeData.cs` | `Features/Trap/` | `_Data/` |
| `PlayerConfigData.cs` | `Features/Player/` | `_Data/` |
| `ProprietaireData.cs` | `Features/Propriétaire/` | `_Data/` |
| `VehiculeData.cs` | `Features/Vehicule/` | `_Data/` |
| `PlayerController.cs` | `Features/Player/` | `Systems/Player/` |
| `PlayerCarry.cs` | `Features/Player/` | `Systems/Player/` |
| `PlayerInteractor.cs` | `Features/Player/` | `Systems/Player/` |
| `PlayerAnimator.cs` | `Features/Player/` | `Systems/Player/` |
| `PlayerNoiseEmitter.cs` | `Features/Player/` | `Systems/Player/` |
| `MissionSystem.cs` | `Features/Mission/` | `Systems/Mission/` |
| `MissionBuilder.cs` | `Features/Mission/` | `Systems/Mission/` |
| `QuotaSystem.cs` | `Features/Mission/` | `Systems/Mission/` |
| `ProprietaireAI.cs` | `Features/Propriétaire/` | `Systems/Proprio/` |
| `ParanoiaSystem.cs` | `Features/Propriétaire/Paranoia/` | `Systems/Proprio/` |
| `InventaireSystem.cs` | `Features/Inventory/` | `Systems/Inventory/` |

### Scripts à créer
| Fichier | Dossier | Description |
|---|---|---|
| `CampaignMissionStarter.cs` | `Systems/Mission/` | Démarre MissionSystem dans scènes campagne hand-crafted |
| `MissionLibreConfigUI.cs` | `UI_Persistent/` | Panel Hub pour configurer et lancer Mission Libre |

### Scripts à modifier
| Fichier | Changement |
|---|---|
| `MissionData.cs` | + champ `SceneName` |
| `GameManager.cs` | `LancerMission()` charge scène dynamique, + `SetMissionSelectionnee()`, + constante Resources path |
| `SceneNames.cs` | + `MISSION_LIBRE`, + `PLAYER_PREFAB_PATH` |
| `HubManager.cs` | Suppression `_missionSelectionnee` local, `FindObjectOfType` → `UIManager.GetPanel<HubUI>()` |
| `UIManager.cs` | + `GetPanel<T>()` |

### Scènes à créer (Unity Editor)
| Scène | Type | Contenu |
|---|---|---|
| `Scenes/Campaign/Mission_01.unity` | Hand-crafted | Maison + Proprio placés manuellement + CampaignMissionStarter |
| `Scenes/Campaign/Mission_02.unity` | Hand-crafted | Idem |
| `Scenes/Mission_Libre.unity` | Procédurale | MissionBuilder + ancrages + systèmes (comme l'actuelle Mission.unity) |

### Assets à créer (Unity Editor)
| Asset | Type | Contenu |
|---|---|---|
| `Missions/Campaign/Mission_0X/Mission_0X_Data.asset` | MissionData SO | SceneName = "Mission_0X", quota, objets, ambiance |
| `Missions/Campaign/MissionList_Campaign.asset` | MissionListData SO | Liste des MissionData campagne |
| `Missions/Libre/MissionList_Libre.asset` | MissionListData SO | Pool de configs pour Mission_Libre |

---

## Non-objectifs (hors scope)

- Save system / persistance entre sessions (prévu V3)
- Système de dialogue NPC (Chef PNJ)
- Animations de transition entre panels
- Refactoring UI_Persistent (couvert par spec séparée `2026-05-07-ui-persistant-refonte-design.md`)
- Toute logique de gameplay non liée à l'architecture
- Suppression des alias événements FR/EN (couvert par la spec UI séparée)
