// ============================================================
// PushableInteractable.cs — Bailiff & Co  V2
// Meuble déplaçable (commode, frigo, armoire…).
// Toute la logique de poussée vient de FurnitureInteractable.
//
// SETUP UNITY :
//   Commode (ce script + BoxCollider Layer Interactable + Rigidbody)
//   └── Drawer_01 (DrawerInteractable)
//   └── Drawer_02 (DrawerInteractable)
//
// Le BoxCollider doit couvrir la face avant du meuble
// (la face que le joueur pousse).
// ============================================================
using UnityEngine;

public class PushableInteractable : FurnitureInteractable
{
    [Header("Identité")]
    [SerializeField] private string _objectName = "meuble";

    public override string GetInteractionLabel()
    {
        if (_isBeingPushed)
            return $"Pousser la {_objectName} (relâcher E)";

        return $"Déplacer la {_objectName} [E maintenu]";
    }
}
