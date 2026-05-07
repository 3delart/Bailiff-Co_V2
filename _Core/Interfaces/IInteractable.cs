// ============================================================
// IInteractable.cs — Bailiff & Co  V2
// Tout objet avec lequel le joueur peut interagir implémente
// cette interface. Un seul bouton côté joueur — le contexte
// détermine l'action.
// Inchangé par rapport à V1 — architecture déjà propre.
// ============================================================
using UnityEngine;

public interface IInteractable
{
    /// <summary>L'objet peut-il être interagi en ce moment ?</summary>
    bool CanInteract(GameObject interacteur);

    /// <summary>Déclenche l'interaction principale.</summary>
    void Interact(GameObject interacteur);

    /// <summary>Texte contextuel affiché au joueur (ex: "Ouvrir", "Saisir").</summary>
    string GetInteractionLabel();
}
