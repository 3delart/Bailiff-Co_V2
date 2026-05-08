# Refonte UI — Scène Persistante & Système de Panels Contextuel
**Date :** 2026-05-07
**Projet :** Bailiff & Co V2

---

## Objectif

Regrouper tous les Canvas Unity dans une scène additive `UI_Persistent` (jamais déchargée) et remplacer le système manuel d'activation/désactivation des panels par un système déclaratif : chaque `UIPanel` déclare lui-même dans quels contextes de jeu il est visible, et s'il s'ouvre automatiquement ou sur déclencheur explicite.

**Problèmes actuels résolus :**
- Panels s'affichant dans le mauvais contexte (ex. PauseMenu visible dans Menu)
- `TryResolveRefs()` avec `GameObject.Find` fragile — refs perdues entre scènes
- Ajouter un panel = modifier `UIManager.cs` (3 méthodes à éditer)
- Duplication d'événements français/anglais dans `GameEvents.cs`

---

## 1. Architecture — Scène `UI_Persistent`

### Chargement
`BootstrapLoader` charge `UI_Persistent` en **additif** avant toute autre scène via `SceneLoader.ChargerUIPersistentAdditive()` (déjà implémenté). La scène n'est jamais déchargée pendant toute la session.

Les scènes de gameplay (Menu, Hub, Mission) ne contiennent **aucun canvas**.

### Contenu de `UI_Persistent`
```
UI_Persistent (scène additive)
└── UIManager (GameObject)
    └── UIManager.cs
└── Canvases (tous déplacés ici depuis les autres scènes)
    ├── Canvas_FadeToBlack
    ├── Canvas_PauseMenu
    ├── Canvas_OptionsUI
    ├── Canvas_LabelInteraction
    ├── Canvas_InventaireWheel
    ├── Canvas_Crosshair
    ├── Canvas_HUD
    ├── Canvas_MenuUI
    └── Canvas_HubUI
        ├── MissionListPanel
        ├── MissionDetailPanel
        └── VehiclePanel
```

---

## 2. Nouveau Système — `UIPanel.cs`

### Principe
Chaque panel hérite de `UIPanel` et déclare dans l'Inspector :
- `_contexteVisibles : ContexteJeu[]` — contextes où il a le droit d'exister
- `_autoAfficher : bool` — s'ouvre automatiquement à l'entrée du contexte, ou attend un déclencheur explicite

### Logique sur changement de contexte (`EvaluerContexte`)
```
contexte actuel NON dans _contexteVisibles → Fermer()
contexte actuel DANS _contexteVisibles ET _autoAfficher = true → Ouvrir()
contexte actuel DANS _contexteVisibles ET _autoAfficher = false → ne rien faire
```

### Enregistrement
- `Awake()` → `UIManager.RegisterPanelGlobal(this)` — enregistre dans la liste complète (y compris panels inactifs) pour recevoir `EvaluerContexte`
- `OnDestroy()` → `UIManager.UnregisterPanelGlobal(this)`
- `OnEnable()` → `UIManager.RegisterPanel(this)` — liste des panels actifs (pour curseur/input)
- `OnDisable()` → `UIManager.UnregisterPanel(this)`

### API unifiée — règle absolue
Toute ouverture/fermeture de panel passe par `Ouvrir()` / `Fermer()`. Aucun `SetActive` direct depuis l'extérieur.

```csharp
public virtual void Ouvrir()  → gameObject.SetActive(true)
public virtual void Fermer()  → gameObject.SetActive(false)
// Override autorisé — doit toujours appeler base.Ouvrir() / base.Fermer()
```

### Exemple : PauseMenu
```csharp
public class PauseMenu : UIPanel
{
    // Inspector : _contexteVisibles = [Hub, Mission], _autoAfficher = false

    public override void Ouvrir()
    {
        base.Ouvrir();          // SetActive(true) + RegisterPanel
        // logique spécifique (animation, pause Time.timeScale, etc.)
    }

    public override void Fermer()
    {
        // logique spécifique (reprendre Time.timeScale, etc.)
        base.Fermer();          // SetActive(false) + UnregisterPanel
    }
}
```

---

## 3. Table de configuration des panels

| Panel | `_contexteVisibles` | `_autoAfficher` | Déclencheur si non-auto |
|---|---|---|---|
| MenuUI | Menu | true | — |
| MissionListUI | Hub | false | Interaction avec le Chef (NPC) |
| MissionDetailPanel | Hub | false | Sélection d'une mission |
| VehiclePanel | Hub | false | Sélection d'un véhicule |
| HUD | Mission | true | — |
| LabelInteractionUI | Hub, Mission | false | PlayerInteractor (GO layer Interactable en portée) |
| Crosshair | Hub, Mission | true | — |
| InventaireWheel | Hub, Mission | false | Input joueur (touche dédiée "Tab") |
| PauseMenu | Hub, Mission | false | ESC / PauseMenuTrigger |
| OptionsUI | Menu, Hub, Mission | false | Bouton Options |
| Canvas_FadeToBlack | Menu, Hub, Mission | false | Event `OnFadeToBlack` |

---

## 4. Nouveau `UIManager.cs`

### Ce qui est supprimé
- `TryResolveRefs()` et tous les `GameObject.Find`
- `ActiverContexteMenu()`, `ActiverContexteHub()`, `ActiverContexteMission()`
- Toutes les références `[SerializeField]` aux panels individuels
- Abonnements à `OnMissionDemarree`, `OnMissionTerminee` (devenus inutiles)
- Méthode `ReSubscribe()` (plus nécessaire)

### Ce qui reste
- `_panelsActifs : HashSet<UIPanel>` — panels actuellement actifs (pour curseur/input)
- `_tousLesPanels : List<UIPanel>` — tous les panels enregistrés (y compris inactifs)
- `RegisterPanel / UnregisterPanel` — gestion des panels actifs
- `RegisterPanelGlobal / UnregisterPanelGlobal` — gestion de la liste complète
- `UpdateUIState()` — source unique de vérité pour input joueur + curseur
- `OnContextChanged` → itère `_tousLesPanels`, appelle `panel.EvaluerContexte(ctx)`
- `OnJoueurSpawne()` — injection InventaireSystem/PlayerCarry dans InventaireWheel (inchangé)
- `GetHUDSystem()` (inchangé)

---

## 5. Consolidation des événements — Anglais uniquement

Tous les alias français sont supprimés. Les 3 scripts qui les consomment sont mis à jour.

| Alias français (supprimé) | Événement anglais (conservé) | Action |
|---|---|---|
| `OnMissionDemarree` | `OnMissionStarted` | `UIManager.cs` — **supprimer** l'abonnement (plus `ActiverContexteMission`) |
| `OnMissionTerminee` | `OnMissionEnded` | `UIManager.cs` — **supprimer** / `SceneLoader.cs` — **migrer** vers `OnMissionEnded` |
| `OnObjetCharge` | `OnObjectLoaded` | `HUDSystem.cs` — **migrer** |
| `OnFondNoir` | `OnFadeToBlack` | `SceneLoader.cs` — **migrer** |
| `OnTimerUrgenceDéclenche` | `OnUrgencyTimerStarted` | `HUDSystem.cs` — **migrer** |
| `OnPerroquetParle` | `OnParrotSpoke` | `HUDSystem.cs` — **migrer** |

Le commentaire dans `EventBus.cs` (exemples avec `OnObjetCharge`) est aussi mis à jour.

---

## 6. Liste complète des changements

### Scripts C# à modifier

| Fichier | Nature du changement |
|---|---|
| `UIPanel.cs` | Refonte complète — ajout `_contexteVisibles`, `_autoAfficher`, `EvaluerContexte()`, double enregistrement Awake/OnDestroy + OnEnable/OnDisable |
| `UIManager.cs` | Simplification majeure — suppression méthodes contexte, ajout `_tousLesPanels`, `RegisterPanelGlobal`, logique `OnContextChanged` |
| `GameEvents.cs` | Suppression des 6 structs alias français + mise à jour commentaires |
| `SceneLoader.cs` | `OnFondNoir` → `OnFadeToBlack`, `OnMissionTerminee` → `OnMissionEnded` |
| `HUDSystem.cs` | `OnObjetCharge` → `OnObjectLoaded`, `OnTimerUrgenceDéclenche` → `OnUrgencyTimerStarted`, `OnPerroquetParle` → `OnParrotSpoke` |
| `EventBus.cs` | Mise à jour des commentaires exemples |
| `MissionResult.cs` | Mise à jour du commentaire (`OnMissionTerminee` → `OnMissionEnded`) |

### Panels à vérifier / faire hériter de UIPanel

Chaque script de panel doit hériter de `UIPanel` et configurer ses champs dans l'Inspector. Vérifier que `Ouvrir()`/`Fermer()` sont bien les seuls points d'entrée.

| Panel | Hérite déjà de UIPanel ? | Action |
|---|---|---|
| PauseMenu | À vérifier | Ajouter héritage si manquant, override Ouvrir/Fermer |
| OptionsUI | À vérifier | Idem |
| MissionListUI | À vérifier | Idem |
| MissionPanelUI | À vérifier | Idem |
| VehiclePanelUI | À vérifier | Idem |
| HUDSystem | À vérifier | Idem |
| LabelInteractionUI | À vérifier | Idem |
| InventaireWheel | À vérifier | Idem |
| MenuUI | À vérifier | Idem |

### Unity — Scènes

| Action | Détail |
|---|---|
| Créer `UI_Persistent` | Nouvelle scène dans Build Settings |
| Déplacer tous les canvas | Depuis Menu, Hub, Mission vers `UI_Persistent` |
| Supprimer canvas résiduels | Nettoyer les scènes source |
| Configurer l'Inspector | `_contexteVisibles` + `_autoAfficher` sur chaque UIPanel selon la table §3 |
| Vérifier Build Settings | `UI_Persistent` présente (pas forcément index 0 — Bootstrap gère le chargement) |

---

## 7. Ce qui ne change pas

- `BootstrapLoader.cs` — déjà correct (`ChargerUIPersistentAdditive`)
- `SceneNames.cs` — `UI_PERSISTENT` déjà défini
- `EventBus.cs` — architecture inchangée
- `GameManager.cs` — `ContexteActuel`, `SetContexte()`, `SetInputJoueurActif()` inchangés
- `UIPanelType` enum — inchangé (Blocking, Overlay, GameUI, Popup toujours utilisés)
- Logique curseur/input dans `UpdateUIState()` — inchangée

---

## Non-objectifs (hors scope)

- Animations de transition entre panels
- Save system / persistance
- Système de dialogue avec le Chef NPC (déclencheur MissionList)
- Toute logique de gameplay non liée à l'UI
