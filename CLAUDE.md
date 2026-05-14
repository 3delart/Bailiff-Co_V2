# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Game Overview

**Bailiff Co** — First-person infiltration simulator. Player is a bailiff executing seizure warrants in private homes. 10–30 minute missions, tension-driven by owner paranoia and player noise emission.

- **Language:** C# (Unity)
- **GDD:** `Claude/GDD_MASTER.md` (exhaustive, French)
- **Architecture ref:** `.claude/architect.md` (role-based planning patterns)
- **Backlog:** `Claude/project_deferred_tasks.md` (UI, tools, consumables, NPCs, animals, audio, coop)

---

## Architecture — High Level

### Scene Flow
```
Bootstrap → Menu → Hub → Mission → Mission Summary → Hub
```

### Core Patterns

**GameContext State Machine** — Tracks game state. Scenes load additively on top of persistent UI layer.
- `Bootstrap` — Initial setup, loads configurations
- `Menu` — Main menu UI
- `Hub` — Persistent hub world (pick missions, buy tools, interact with NPCs)
- `Mission` — Active mission gameplay

**GameManager (Singleton)** — Entry point. Initializes core systems on wake. Accessible via static `Instance`.

**EventBus** — C# events for decoupling. Broadcast via `GameEvents.cs`. Subscribe via `EventBusHelper.On()`.

**UIPanel Base Class** — All UI panels inherit from `UIPanel`. Lifecycle: `OnShow()` → active → `OnHide()`.

**ScriptableObjects for Data** — Config, missions, items, animals, NPCs stored as SO. Loaded at bootstrap.

---

## Folder Structure

```
Assets/_Script/
├── _Core/
│   ├── Bootstrap/          # BootstrapLoader, RuntimeBootstrapper
│   ├── Events/             # EventBus, GameEvents, EventBusHelper
│   ├── Interfaces/         # IInteractable
│   ├── Services/           # GameManager, SceneLoader, SceneNames
│   └── SharedData/         # Enums (GameContext, etc.)
├── _Data/
│   ├── *Data.cs            # ScriptableObject configs (Player, Mission, Vehicle, etc.)
│   └── *Repository.cs      # Runtime data access (Options, Inventory)
├── Systems/
│   ├── Player/             # PlayerController, PlayerCarry, PlayerInteractor, PlayerNoiseEmitter
│   ├── Proprietaire/       # ProprietaireAI, ParanoiaSystem
│   ├── Mission/            # MissionSystem, MissionTracker, MissionBuilder, MissionResult
│   ├── Vehicule/           # Vehicle, VehicleTrunkZone
│   ├── Inventory/          # InventaireSystem
│   └── *Interactable.cs    # OpenableInteractable, MeubleInteractable, TiroirInteractable
├── UI_Persistent/          # UIPanel subclasses (HubUI, MissionPanelUI, etc.)
└── Utilities/              # PriceFormatter, etc.
```

---

## Key Systems

### Player
- **PlayerController** — Movement (4 postures: stand/crouch/prone/sprint), jumping, climbing
- **PlayerCarry** — Hold/throw 1 object; throw velocity = BaseThrowVelocity / mass
- **PlayerInteractor** — Raycast-based interaction detection; broadcasts `OnInteractionLabelChanged`
- **PlayerNoiseEmitter** — Emit noise from footsteps, landing, throws, furniture

### Mission Flow
- **MissionSystem** — Main mission coordinator
- **MissionBuilder** — Constructs mission from data
- **MissionTracker** — Real-time mission state (quota, seized items)
- **MissionResult** — End-of-mission evaluation (stars, fines, suspension flags)
- **QuotaSystem** — Tracks seizure value vs. target

### Owner AI
- **ProprietaireAI** — Owner behavior tree. Paranoia-driven actions.
- **ParanoiaSystem** — Paranoia meter (0–100). Increases on noise, decreases over time. Triggers owner stress behaviors.

### Vehicle & Inventory
- **Vehicle** — Car with trunk (VehicleTrunkZone). Interactable doors, carries seized items.
- **InventaireSystem** — Player equipment before mission. Persistent across hub.

### UI
- **UIManager** — Centralized UI control
- **UIPanel** — Base class. Override `OnShow()`, `OnHide()` for state. Subscribe to events in `OnShow()`, unsubscribe in `OnHide()`
- **HubUI**, **MissionPanelUI**, **MissionSummaryUI** — Context-specific panels

---

## Development Workflow

### Play in Unity
1. Open scene `Scenes/Bootstrap.unity`
2. Hit Play. Bootstrap initializes all systems, loads Menu
3. Press Play again or select mission to enter Hub/Mission

### Key Scene Names
```csharp
SceneNames.Bootstrap      // "Bootstrap"
SceneNames.Menu           // "Menu"
SceneNames.Hub            // "Hub"
SceneNames.Mission_01     // "Mission_01" (etc.)
```

### Add New UI Panel
1. Create script in `Assets/_Script/UI_Persistent/` inheriting from `UIPanel`
2. Override `OnShow()` (subscribe to events), `OnHide()` (unsubscribe)
3. Call `Show()` / `Hide()` from GameManager or event handler
4. Assign Canvas to `uiPanelPrefab` in inspector or create at runtime

### Add New Interactable
1. Implement `IInteractable` interface (methods: `Interact()`, `GetInteractionLabel()`)
2. Attach collider to game object
3. Player raycast auto-detects via `PlayerInteractor`

### Emit Event
```csharp
GameEvents.YourEventName?.Invoke(args);
```

Subscribe in `OnShow()`:
```csharp
EventBusHelper.On(GameEvents.YourEventName, OnEventFired);
```

### Access Game State
```csharp
var manager = GameManager.Instance;
var currentContext = manager.CurrentContext;
```

### Create New Event
1. Define struct in `Assets/_Script/_Core/Events/GameEvents.cs`:
```csharp
/// <summary>Description of what triggers this event.</summary>
public struct OnMyEventName
{
    public string SomeData;
    public float SomeValue;
}
```

2. Broadcast from source:
```csharp
GameEvents.OnMyEventName?.Invoke(new OnMyEventName { SomeData = "value", SomeValue = 42f });
```

3. Subscribe in UI panel or system `OnShow()`:
```csharp
EventBusHelper.On(GameEvents.OnMyEventName, OnMyEventFired);

void OnMyEventFired(OnMyEventName evt)
{
    // Handle event
}
```

4. Unsubscribe in `OnHide()`:
```csharp
EventBusHelper.Off(GameEvents.OnMyEventName, OnMyEventFired);
```

### Create New ScriptableObject
1. Define class with `[CreateAssetMenu]`:
```csharp
[CreateAssetMenu(menuName = "BailiffCo/MyData")]
public class MyData : ScriptableObject
{
    public string Name;
    public float Value;
}
```

2. In project, right-click → Create → BailiffCo/MyData

3. Assign in inspector or load at runtime:
```csharp
var data = Resources.Load<MyData>("Path/To/MyData");
```

---

## Git Workflow

**Commit convention** — Conventional Commits format:
```
feat: Add X feature
fix: Resolve Y bug
refactor: Restructure Z system
```

Keep subject ≤50 chars. Add body only if "why" isn't obvious from title.

Example:
```
feat: Vehicle anti-theft alarm

Triggers OnVehicleAlarmTriggered when player approaches trunk with no badge.
Paranoia +15, owner alerted.
```

---

## Testing & Verification

- **No automated test suite yet** — Manual testing in Unity Editor
- **Play mode testing** — Load Bootstrap, progress through scenes, verify state transitions
- **Event verification** — Check EventBus console logs in debug mode

---

## Common Pitfalls

**EventBus subscription timing** — Subscribe in `OnShow()`, not `Awake()`. Event system may not be initialized yet in Awake.

**Single object carry limit** — Player holds 1 object max. Design constraint, not bug. Expanding requires `ExpandCarryCapacity` option.

**Scene additive loading** — Bootstrap loads Menu, Hub loads Mission additively on top. Unload old mission before loading new one or state persists.

**Collider required for interaction** — Player raycast from camera won't detect objects without colliders. All interactables need `Collider` component.

**ScriptableObject references** — Never null-check SO at runtime without fallback. Use Resources.Load or assign default in inspector.

**ProprietaireAI state machine** — Owner behavior driven by paranoia tier (0–5) and state (Calm → Searching → Chasing). Check both before assuming action will happen.

## Common Issues

**Scene won't load?** — Verify scene is in Build Settings and path matches `SceneNames.cs`.

**Interaction not detected?** — Check collider is on object, layer not ignored by PlayerInteractor raycast, object has `IInteractable` implemented.

**EventBus event not firing?** — Verify listener is subscribed in `OnShow()` (not `Awake()`) and unsubscribed in `OnHide()`. Check event is being invoked at source.

**UI panel not showing?** — Check canvas hierarchy, `Show()` is called, `OnShow()` is overridden, and event subscriptions don't error.

---

## References

- **GDD** — `Claude/GDD_MASTER.md` — Full game design, mechanics, progression
- **Architecture** — `.claude/architect.md` — Planning patterns for features
- **Backlog** — `Claude/project_deferred_tasks.md` — Complete feature list by category
- **Recent commits** — Git history for implementation context
