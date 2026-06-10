// ============================================================
// ThrowBehaviour.cs — Bailiff & Co
// Consommable à LANCER (fumigène) :
//   • Clic gauche  = lâcher le WorldPrefab aux pieds (devant, sans vélocité).
//   • Clic droit   = lancer le WorldPrefab depuis la caméra.
// La fumée = ParticleSystem porté par le WorldPrefab (auto-play à l'instanciation).
// Décrémente le stock à chaque usage.
// ============================================================
using UnityEngine;

public sealed class ThrowBehaviour : ToolBehaviour
{
    public override void OnPrimaryDown()   => Lancer(throwIt: false);
    public override void OnSecondaryDown() => Lancer(throwIt: true);

    private void Lancer(bool throwIt)
    {
        var cd = Ctx.Item as ConsumableData;
        if (cd == null || cd.WorldPrefab == null || Ctx.Camera == null) { ConsumeOne(); return; }

        Vector3 pos = Ctx.Camera.position + Ctx.Camera.forward * 0.5f;
        var go = Object.Instantiate(cd.WorldPrefab, pos, Ctx.Camera.rotation);

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null && throwIt)
        {
            float v = Ctx.Config != null ? Ctx.Config.BaseThrowVelocity : 10f;
            rb.linearVelocity = Ctx.Camera.forward * v;
        }

        Ctx.Animator?.JouerUsageOneShot();   // anim de lâcher/lancer
        ConsumeOne();
    }
}
