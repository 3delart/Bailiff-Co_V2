// ============================================================
// FlashlightBehaviour.cs — Bailiff & Co
// Lampe de poche : le HandPrefab porte un Light (cône). Pas d'effet au clic.
// À l'équipement, applique la portée et l'angle du cône depuis Stats
// (cône plus grand / plus loin au niveau supérieur).
// ============================================================
using UnityEngine;

public sealed class FlashlightBehaviour : ToolBehaviour
{
    public override void OnEquip(ToolUseContext ctx)
    {
        base.OnEquip(ctx);
        if (ctx.ModeleEnMain == null) return;

        var light = ctx.ModeleEnMain.GetComponentInChildren<Light>();
        if (light == null) return;

        if (ctx.Stats.Range     > 0.01f) light.range     = ctx.Stats.Range;
        if (ctx.Stats.ConeAngle > 0.01f) light.spotAngle = ctx.Stats.ConeAngle;
        if (ctx.Stats.Magnitude > 0.01f) light.intensity = ctx.Stats.Magnitude;
    }
}
