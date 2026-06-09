// ============================================================
// PlaceBehaviour.cs — Bailiff & Co
// Consommables à POSE : viser une surface + clic gauche = spawn du
// WorldPrefab au point visé. Décrémente le stock.
//   • Pose LIBRE (appât)   : toute surface ~horizontale.
//   • Pose CIBLÉE (antivol): valide uniquement sur le coffre véhicule
//     (ConsumableData.TargetVehicleTrunkOnly).
// ============================================================
using UnityEngine;
using BailiffCo;

public sealed class PlaceBehaviour : ToolBehaviour
{
    private const float VALID_SURFACE_DOT = 0.7f;

    public override void OnPrimaryDown()
    {
        if (!Ctx.Raycast(out var hit)) return;
        if (!IsValidSurface(hit)) return;

        var cd = Ctx.Item as ConsumableData;
        if (cd != null && cd.WorldPrefab != null)
        {
            // Aligne le prefab sur la normale de la surface.
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Object.Instantiate(cd.WorldPrefab, hit.point, rot);
        }

        ConsumeOne();
    }

    private bool IsValidSurface(RaycastHit hit)
    {
        var cd = Ctx.Item as ConsumableData;
        if (cd != null && cd.TargetVehicleTrunkOnly)
            return hit.collider.GetComponentInParent<VehicleTrunkZone>() != null;

        // Pose libre : surface suffisamment horizontale.
        return Vector3.Dot(hit.normal, Vector3.up) >= VALID_SURFACE_DOT;
    }
}
