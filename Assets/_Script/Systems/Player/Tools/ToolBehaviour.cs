// ============================================================
// ToolBehaviour.cs — Bailiff & Co
// Archi "comportement-par-outil" : PlayerToolUser orchestre, chaque
// mécanique d'usage = une sous-classe de ToolBehaviour.
//
// Hooks d'input (routés par PlayerToolUser) :
//   OnEquip / OnUnequip          — prise / rangement de l'objet en main
//   OnPrimaryDown / Hold / Up    — clic gauche (tap / maintien / relâché)
//   OnSecondaryDown              — clic droit
//   Tick                         — chaque frame (lumière, états continus)
//
// Ajouter un nouvel outil = ajouter une sous-classe + un case dans la
// factory. Aucun switch géant à maintenir.
// ============================================================
using UnityEngine;

// ------------------------------------------------------------
// CONTEXTE — passé aux hooks ; porte tout ce dont un behaviour a besoin
// ------------------------------------------------------------
public class ToolUseContext
{
    public Transform        Camera;
    public PlayerConfigData Config;
    public ItemData         Item;
    public int              Niveau;
    public EffectStats      Stats;        // résolus pour le niveau courant
    public string           ConsoId;      // null si outil permanent
    public Transform        ModeleEnMain; // pour le tween de l'outil en main
    public InventaireSystem Inventaire;
    public PlayerAnimator   Animator;     // couche haut : anim d'usage
    public PlayerToolUser   User;

    /// <summary>Portée d'usage : Stats.Range si défini, sinon InteractionRange de la config.</summary>
    public float Range => Stats.Range > 0.01f
        ? Stats.Range
        : (Config != null ? Config.InteractionRange : 3f);

    /// <summary>Raycast depuis la caméra — exclut le joueur et l'objet porté
    /// (sinon le rayon tape la capsule du joueur, caméra à l'intérieur).</summary>
    public bool Raycast(out RaycastHit hit)
    {
        Transform o = Camera != null ? Camera : (User != null ? User.transform : null);
        if (o == null) { hit = default; return false; }

        int mask = Physics.AllLayers;
        int player = LayerMask.NameToLayer("Player");
        if (player != -1) mask &= ~(1 << player);
        int porte = LayerMask.NameToLayer("ObjetPorte");
        if (porte != -1) mask &= ~(1 << porte);

        return Physics.Raycast(o.position, o.forward, out hit, Range,
            mask, QueryTriggerInteraction.Ignore);
    }
}

// ------------------------------------------------------------
// BASE
// ------------------------------------------------------------
public abstract class ToolBehaviour
{
    protected ToolUseContext Ctx;

    public virtual void OnEquip(ToolUseContext ctx) { Ctx = ctx; }
    public virtual void OnUnequip() { }
    public virtual void OnPrimaryDown() { }
    public virtual void OnPrimaryHold(float dt) { }
    public virtual void OnPrimaryUp() { }
    public virtual void OnSecondaryDown() { }
    public virtual void Tick(float dt) { }

    /// <summary>Décrémente le stock du consommable courant ; range l'outil si épuisé.</summary>
    protected void ConsumeOne()
    {
        if (Ctx?.Inventaire == null || string.IsNullOrEmpty(Ctx.ConsoId)) return;
        Ctx.Inventaire.UtiliserConsommable(Ctx.ConsoId);
        if (Ctx.Inventaire.QuantiteConsommable(Ctx.ConsoId) <= 0)
            Ctx.User?.RangerOutil();
    }
}

/// <summary>Comportement vide (badge en main, items passifs, modes hors scope).</summary>
public sealed class NoOpBehaviour : ToolBehaviour { }

// ------------------------------------------------------------
// FACTORY — seul point de dispatch
// ------------------------------------------------------------
public static class ToolBehaviourFactory
{
    public static ToolBehaviour Create(ItemData item)
    {
        if (item == null) return new NoOpBehaviour();

        switch (item.UsageMode)
        {
            case ToolUsageMode.Channel:
                // Sous-dispatch sur l'effet concret de l'outil.
                if (item is ToolData t)
                {
                    switch (t.EffectType)
                    {
                        case ToolEffectType.ForceDoor: return new CrowbarBehaviour();
                        case ToolEffectType.Lockpick:  return new LockpickBehaviour();
                        case ToolEffectType.ScanValue: return new ScannerPhoneBehaviour();
                    }
                }
                return new NoOpBehaviour();

            case ToolUsageMode.Place:  return new PlaceBehaviour();
            case ToolUsageMode.Throw:  return new ThrowBehaviour();
            case ToolUsageMode.Light:  return new FlashlightBehaviour();

            default: return new NoOpBehaviour();
        }
    }
}
