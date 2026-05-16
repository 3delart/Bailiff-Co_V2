# DrawerZone Setup Guide

## Overview
`DrawerZone` is a trigger volume that automatically parents dropped objects into drawers, allowing them to be stored and retrieved.

## Hierarchy

For each drawer in your scene, set up this hierarchy:

```
Dresser (optional: FurnitureInteractable for pushable furniture)
└── Drawer (DrawerInteractable + BoxCollider layer="Interactable")
    └── DrawerZone (BoxCollider isTrigger=true, with this script)
    └── ValueObject_01 (optional: pre-place objects as children)
    └── ValueObject_02 (...)
```

## Setup Steps

### 1. Create/Locate Drawer GameObject
- Must have `DrawerInteractable` script attached
- Must have a `BoxCollider` (not trigger) on the same GameObject, layer set to `"Interactable"`

### 2. Create DrawerZone Child
- Create a new child GameObject named `"DrawerZone"`
- Add a `BoxCollider` component
  - Set **Is Trigger** = `true`
  - Size it to match the interior of the drawer (X and Y match drawer dimensions, Z covers depth)
  - Position it inside the drawer opening
- Add the `DrawerZone` script component
- In the inspector, assign the parent **Drawer** GameObject to the `_drawer` field

### 3. Test

**Open drawer:**
- Stand in front, press E
- Drawer slides out, contained objects become seizable (gravity on, kinematic off)

**Place object inside:**
- Pick up an object (E to grab)
- Hold E + move mouse left-click to activate placement preview (ghost visible)
- Position the ghost inside the drawer opening
- Release left-click to place
- Object automatically reparents to drawer, physics frozen

**Pick up from drawer:**
- Look at object inside open drawer
- Press E to grab
- Object detaches from drawer, becomes carriyable again

**Close drawer with object inside:**
- If object height ≤ `_interiorClearance` (default 0.15m): drawer closes normally
- If object height > `_interiorClearance`: close attempt is blocked (Interact() returns early)

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Object falls through drawer bottom | Ensure drawer interior has a collider or floor; test with objects at rest first |
| Object doesn't reparent when dropped | Check DrawerZone collider is set to `isTrigger=true` and sized correctly |
| Cannot pick up object inside drawer | Object layer must not be locked; open drawer must have fired `LiberateContents()` |
| Drawer closes but object clips through | Increase `_interiorClearance` or move the object |
| Object keeps phasing through furniture | Layer "ObjetTiroir" issue — confirm layer consistency in PlayerCarry & DrawerZone |

## Debugging

Enable `Debug.Log` in `DrawerZone.OnTriggerStay()` to trace reparenting:

```csharp
void OnTriggerStay(Collider c)
{
    ValueObject vo = c.GetComponent<ValueObject>();
    if (vo == null) return;
    if (c.gameObject.layer == _layerCarried) return;
    if (vo.transform.IsChildOf(_drawer.transform))
    {
        Debug.Log($"Already in drawer: {vo.name}");
        return;
    }

    Debug.Log($"Reparenting {vo.name} to drawer");
    vo.transform.SetParent(_drawer.transform);
    // ... rest of physics setup
}
```

## Notes

- `OnTriggerStay` fires every physics frame while an object is in the zone — the `IsChildOf` guard prevents redundant reparenting
- Objects reparented to the drawer are automatically included in `GetPresentContents()` check (uses `IsChildOf`)
- `LiberateContents()` (on open) and `LockContents()` (on close) apply to all drawer children, including dynamically placed objects
- Anti-clipping: the `DrawerZone` trigger volume must be positioned INSIDE the drawer; the drawer's physical colliders form the walls and ceiling
