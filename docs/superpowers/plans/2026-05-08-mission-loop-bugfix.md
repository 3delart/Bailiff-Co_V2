# Mission Loop — Bugfix Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rendre la boucle de gameplay Mission testable : ramasser des objets, les charger dans le coffre, atteindre le quota, partir.

**Architecture:** Les bugs sont de deux types — code C# (3 fichiers à modifier) et setup Unity Editor (layers, Physics Matrix, Inspector). Les tâches code viennent en premier car elles sont déterministes ; les tâches Unity Editor sont décrites étape par étape pour être exécutées manuellement.

**Tech Stack:** Unity 2022+, C#, EventBus maison, CharacterController (joueur), Rigidbody kinématique (objets portés)

---

## Fichiers touchés

| Fichier | Action | Raison |
|---------|--------|--------|
| `Assets/_Script/Systems/Player/PlayerCarry.cs` | Modifier | MovePosition doit être dans FixedUpdate, pas Update |
| `Assets/_Script/Systems/Mission/CampaignMissionStarter.cs` | Modifier | Ajouter refs QuotaSystem + PlayerCarry, appeler InjectDependencies |
| `Assets/_Script/Systems/Mission/MissionBuilder.cs` | Modifier | Appeler InjectDependencies sur le véhicule spawné |

**Setup Unity Editor (pas de fichier C#) :**
- Project Settings → Tags & Layers : créer layer "ObjetPorte"
- Project Settings → Physics : configurer la collision matrix
- Prefab véhicule : assigner les références dans VehicleRuntime
- Scène MissionTest : mettre les colliders interactables sur le layer "Interactable"

---

## Task 1 — PlayerCarry : MovePosition dans FixedUpdate

**Problème :** `_rbPorte.MovePosition` est appelé dans `Update()`. Les Rigidbody kinématiques doivent être déplacés dans `FixedUpdate()` — sinon le moteur physique reçoit plusieurs positions interpolées entre deux fixed steps, causant des micro-poussées sur le CharacterController.

**Fichiers :**
- Modifier : `Assets/_Script/Systems/Player/PlayerCarry.cs`

- [ ] **Étape 1 : Remplacer Update par la séparation Update / FixedUpdate**

Dans [PlayerCarry.cs](Assets/_Script/Systems/Player/PlayerCarry.cs), remplacer le bloc `Update()` existant par :

```csharp
private void Update()
{
    if (_objetPorte == null) return;

    if (Input.GetMouseButtonDown(0))
        Poser(doux: true);

    if (Input.GetMouseButtonDown(1))
        Lancer();
}

private void FixedUpdate()
{
    if (_objetPorte == null || _pointDePort == null) return;

    _rbPorte.MovePosition(_pointDePort.position);
    _rbPorte.MoveRotation(_pointDePort.rotation);
}
```

- [ ] **Étape 2 : Vérifier que le projet compile**

Ouvrir Unity → Console → aucune erreur de compilation.

- [ ] **Étape 3 : Commit**

```
git add Assets/_Script/Systems/Player/PlayerCarry.cs
git commit -m "fix: MovePosition dans FixedUpdate pour éviter les micro-poussées sur CharacterController"
```

---

## Task 2 — CampaignMissionStarter : InjectDependencies après spawn

**Problème :** `CampaignMissionStarter.SpawnVehicle()` instancie le prefab véhicule mais n'appelle jamais `VehicleRuntime.InjectDependencies()`. Résultat : `_quotaSystem` et `_playerCarry` sont null sur VehicleRuntime — le label porte conducteur est cassé et le compteur de quota ne s'affiche pas.

**Fichiers :**
- Modifier : `Assets/_Script/Systems/Mission/CampaignMissionStarter.cs`

- [ ] **Étape 1 : Ajouter les champs sérialisés QuotaSystem et PlayerCarry**

Dans [CampaignMissionStarter.cs](Assets/_Script/Systems/Mission/CampaignMissionStarter.cs), ajouter après `[SerializeField] private MissionSystem _missionSystem;` :

```csharp
[SerializeField] private QuotaSystem   _quotaSystem;
```

`PlayerCarry` sera récupéré depuis le player spawné, pas besoin de référence sérialisée.

- [ ] **Étape 2 : Modifier Build() pour injecter après les deux spawns**

Remplacer la méthode `Build` :

```csharp
private void Build(MissionData mission)
{
    if (_missionSystem == null)
    {
        Debug.LogError("[CampaignMissionStarter] _missionSystem non assigné dans l'Inspector !");
        return;
    }

    SpawnPlayer();
    var vehiculeSpawne = SpawnVehicle();
    InjecterRefsDansVehicule(vehiculeSpawne);
    InjecterRefsProprietaire(vehiculeSpawne);
    _missionSystem.StartMission(mission);

    Debug.Log($"[CampaignMissionStarter] Scène prête : {mission.MissionName}");
}
```

- [ ] **Étape 3 : Ajouter la méthode InjecterRefsDansVehicule**

Ajouter après `InjecterRefsProprietaire` :

```csharp
private void InjecterRefsDansVehicule(GameObject vehicule)
{
    if (vehicule == null) return;

    var runtime = vehicule.GetComponent<VehicleRuntime>();
    if (runtime == null)
    {
        Debug.LogWarning("[CampaignMissionStarter] VehicleRuntime introuvable sur le véhicule spawné.");
        return;
    }

    var player = GameManager.Instance?.Player;
    var carry  = player != null ? player.GetComponent<PlayerCarry>() : null;

    runtime.InjectDependencies(_missionSystem, carry, _quotaSystem);
}
```

- [ ] **Étape 4 : Vérifier que le projet compile**

Unity Console → aucune erreur.

- [ ] **Étape 5 : Assigner _quotaSystem dans l'Inspector de la scène MissionTest**

Dans la hiérarchie Unity de MissionTest :
- Sélectionner le GameObject qui porte `CampaignMissionStarter`
- Glisser le composant `QuotaSystem` (sur MissionSetup ou MissionSystem) dans le champ `_quotaSystem`

- [ ] **Étape 6 : Commit**

```
git add Assets/_Script/Systems/Mission/CampaignMissionStarter.cs
git commit -m "fix: injecter QuotaSystem+PlayerCarry dans VehicleRuntime après spawn (CampaignMissionStarter)"
```

---

## Task 3 — MissionBuilder : InjectDependencies après spawn

**Problème :** `MissionBuilder.SpawnVehicle()` ne passe pas non plus les refs à `VehicleRuntime`. Même correction que Task 2 mais dans le chemin procédural.

**Fichiers :**
- Modifier : `Assets/_Script/Systems/Mission/MissionBuilder.cs`

- [ ] **Étape 1 : Modifier Build() pour appeler l'injection après SpawnPlayer**

Dans [MissionBuilder.cs](Assets/_Script/Systems/Mission/MissionBuilder.cs), dans la méthode `Build()`, ajouter l'appel après `SpawnPlayer()` :

```csharp
private void Build()
{
    Debug.Log($"[MissionBuilder] Construction de la mission : {_mission.MissionName}");

    SpawnHouse();
    SpawnVehicle();
    SpawnOwner();
    SpawnObjects();
    SpawnPlayer();
    InjecterRefsDansVehicule();   // ← AJOUTER ICI
    ApplyAmbiance();
    _missionSystem.StartMission(_mission);

    Debug.Log("[MissionBuilder] Construction terminée");
}
```

- [ ] **Étape 2 : Ajouter la méthode InjecterRefsDansVehicule**

Ajouter dans la section "HELPERS" de MissionBuilder, après `SpawnPlayer()` :

```csharp
// ================================================================
// INJECTION DEPS VEHICULE
// ================================================================

private void InjecterRefsDansVehicule()
{
    if (_spawnedVehicle == null) return;

    var runtime = _spawnedVehicle.GetComponent<VehicleRuntime>();
    if (runtime == null) return;

    var carry = _spawnedPlayer != null
        ? _spawnedPlayer.GetComponent<PlayerCarry>()
        : null;

    runtime.InjectDependencies(_missionSystem, carry, _quotaSystem);
}
```

- [ ] **Étape 3 : Vérifier que le projet compile**

Unity Console → aucune erreur.

- [ ] **Étape 4 : Commit**

```
git add Assets/_Script/Systems/Mission/MissionBuilder.cs
git commit -m "fix: injecter refs dans VehicleRuntime après spawn (MissionBuilder)"
```

---

## Task 4 — Unity : Créer le layer "ObjetPorte" et configurer la Physics Matrix

**Problème :** Le layer "ObjetPorte" n'existe pas → les objets portés restent sur le layer Default → leur collider entre en conflit avec le CharacterController → le joueur est poussé.

Cette tâche est 100% dans l'éditeur Unity, aucun fichier C# à modifier.

- [ ] **Étape 1 : Créer le layer "ObjetPorte"**

1. Menu Unity : **Edit → Project Settings → Tags and Layers**
2. Dans la section "Layers", trouver un slot vide (User Layer 8 ou plus)
3. Nommer ce slot : `ObjetPorte`
4. Fermer Project Settings

- [ ] **Étape 2 : Configurer la Physics Collision Matrix**

1. Menu Unity : **Edit → Project Settings → Physics**
2. Faire défiler jusqu'à "Layer Collision Matrix"
3. Sur la ligne **ObjetPorte**, décocher les cases d'intersection avec :
   - **Default** (layer du joueur / objets génériques)
   - **Player** (si un layer dédié existe pour le joueur)
   - **ObjetPorte** lui-même (les objets portés ne se poussent pas entre eux)
4. Garder **cochées** les intersections avec : **Interactable**, **Ground**, **Furniture** (les objets posés doivent toujours reposer sur les surfaces)

- [ ] **Étape 3 : Vérifier en Play Mode**

Lancer la scène MissionTest → ramasser un objet → le joueur **ne doit plus être poussé**.
Si toujours poussé, vérifier dans la Console qu'il n'y a pas l'erreur `[PlayerCarry] Layer 'ObjetPorte' introuvable`.

- [ ] **Étape 4 : Commit (sauvegarde des settings)**

```
git add ProjectSettings/TagManager.asset ProjectSettings/DynamicsManager.asset
git commit -m "fix: créer layer ObjetPorte et configurer Physics Matrix (pas de collision joueur)"
```

---

## Task 5 — Unity : Configurer le prefab véhicule (layer + refs Inspector)

**Problème :** Les colliders cliquables du véhicule ne sont pas sur le layer "Interactable" → le raycast de `PlayerInteractor` ne les détecte pas. De plus, les champs `_trunkDoorCollider`, `_trunkDoor`, `_trunkZoneCollider`, `_driverDoorCollider` ne sont pas assignés dans `VehicleRuntime`.

- [ ] **Étape 1 : Ouvrir le prefab du véhicule en mode Prefab Edit**

Dans le Project panel, double-cliquer sur le prefab du véhicule de test pour ouvrir le Prefab Editor.

- [ ] **Étape 2 : Mettre les colliders cliquables sur le layer "Interactable"**

Dans la hiérarchie du prefab, sélectionner un par un :
- `ColliderTrunkDoor` → Inspector → Layer → **Interactable**
- `ColliderDriverDoor` → Inspector → Layer → **Interactable**
- `ColliderCage` (si présent) → Inspector → Layer → **Interactable**

Ne pas changer le layer du reste du véhicule (mesh, TrunkZone, etc.).

- [ ] **Étape 3 : Assigner les références dans VehicleRuntime**

Sélectionner le root du prefab (celui qui porte `VehicleRuntime`). Dans l'Inspector :
- **Driver Door Collider** → glisser `ColliderDriverDoor`
- **Trunk Door** → glisser le Transform `TrunkDoor` (la porte animée)
- **Trunk Door Collider** → glisser `ColliderTrunkDoor`
- **Trunk Zone Collider** → glisser le Collider sur l'enfant `TrunkZone`

- [ ] **Étape 4 : Vérifier que TrunkZone a un BoxCollider IsTrigger**

Sélectionner `TrunkZone` dans le prefab :
- Doit avoir un `BoxCollider` avec **Is Trigger = true**
- Doit avoir le composant `VehicleTrunkZone`

Si `VehicleTrunkZone` est absent → Add Component → VehicleTrunkZone.

- [ ] **Étape 5 : Sauvegarder le prefab**

Ctrl+S dans le Prefab Editor, puis revenir à la scène.

- [ ] **Étape 6 : Vérifier en Play Mode**

Lancer MissionTest → s'approcher de la porte du coffre → le label **"Ouvrir le coffre"** doit apparaître → appuyer E → le coffre s'ouvre (ou l'animation joue).

- [ ] **Étape 7 : Commit**

```
git add Assets/[chemin vers le prefab véhicule]
git commit -m "fix: layers Interactable + refs Inspector sur prefab véhicule"
```

---

## Task 6 — Unity : Mettre les objets interactables de la scène sur le layer "Interactable"

**Problème :** Portes, fenêtres, meubles, et objets saisissables (ValueObject) de la scène MissionTest ne sont pas sur le layer "Interactable" → le raycast ne les voit pas.

- [ ] **Étape 1 : Sélectionner tous les GameObjects avec OpenableInteractable**

Dans la hiérarchie de MissionTest, sélectionner chaque porte et fenêtre. Pour aller plus vite :
- Menu Unity : **Edit → Find References In Scene** sur le script `OpenableInteractable`
- OU filtrer la hiérarchie avec `t:OpenableInteractable`

Pour chaque objet trouvé → Inspector → Layer → **Interactable**.
Quand Unity demande "Change children?" → répondre **"No, this object only"** (sauf si le collider est sur un enfant dédié, auquel cas changer l'enfant).

- [ ] **Étape 2 : Même chose pour DrawerInteractable**

Répéter avec `t:DrawerInteractable`.

- [ ] **Étape 3 : Même chose pour les ValueObject**

Répéter avec `t:ValueObject`. Ces objets doivent aussi être sur "Interactable" pour que le joueur puisse les saisir.

- [ ] **Étape 4 : Vérifier en Play Mode**

Lancer MissionTest :
- Regarder une porte → label "Ouvrir la porte" apparaît → E → porte s'ouvre ✓
- Regarder un objet → label "Saisir (nom objet)" → E → objet suivi par le joueur ✓
- Ramasser l'objet → l'approcher du coffre ouvert → entrer dans la TrunkZone → quitter → le compteur de coffre se met à jour ✓

- [ ] **Étape 5 : Commit**

```
git add Assets/Scenes/MissionTest.unity
git commit -m "fix: layer Interactable sur portes, meubles, ValueObject dans MissionTest"
```

---

## Task 7 — Validation finale du loop complet

**Objectif :** Vérifier que la boucle entière Bootstrap → Hub → Mission → retour Hub fonctionne avec le gameplay core.

- [ ] **Étape 1 : Lancer depuis Bootstrap**

Play depuis la scène Bootstrap. Naviguer dans le Hub → choisir une mission → choisir un véhicule → partir.

- [ ] **Étape 2 : Valider les interactions en mission**

Dans la scène mission :
- [ ] Ramasser un objet (E) → il suit le joueur, pas de pushing ✓
- [ ] Ouvrir le coffre (E sur ColliderTrunkDoor) → animation + label ✓
- [ ] Approcher l'objet du coffre ouvert → le lâcher (clic gauche) → l'objet tombe dans la zone trigger ✓
- [ ] Porte conducteur : label affiche `Partir — X / Y €` (quota affiché) ✓
- [ ] Ouvrir une porte de maison ✓
- [ ] Atteindre le quota → label porte conducteur passe à `Partir ✓` ✓

- [ ] **Étape 3 : Valider le retour Hub**

Interagir avec la porte conducteur → confirmer le départ → fondu noir → retour Hub ✓

- [ ] **Étape 4 : Commit final si tout est vert**

```
git add -A
git commit -m "fix: mission loop validée - pickup, coffre, quota, retour hub"
```
