// ============================================================
// VehicleTrunkZone.cs — Bailiff & Co  V2
// Composant à mettre sur le GameObject enfant "TrunkZone".
// Relaie les événements OnTriggerEnter/Exit vers VehicleRuntime
// sur le parent. Le coffre doit être ouvert pour que les objets
// soient acceptés (VehicleRuntime active/désactive ce collider).
//
// CHANGEMENTS V2 :
//   - ZoneCoffreTrigger → VehicleTrunkZone (anglais cohérent)
//   - ObjetValeur → ValueObject
//   - Véhicule → VehicleRuntime
//   - Commentaires traduits en anglais pour cohérence
//
// HIÉRARCHIE RECOMMANDÉE :
//   VehiclePrefab (root)
//   ├── VehicleRuntime.cs
//   └── TrunkZone (ce script + BoxCollider IsTrigger)
//       └── Le BoxCollider définit la zone de dépôt physique
//
// FONCTIONNEMENT :
//   - VehicleRuntime active/désactive le collider selon l'état
//     du coffre (_trunkZoneCollider.enabled = true/false).
//   - Quand un ValueObject entre/sort, ce script relaie
//     l'information au VehicleRuntime parent via les méthodes
//     publiques OnObjectEnteredTrunk() / OnObjectLeftTrunk().
// ============================================================
using UnityEngine;

public class VehicleTrunkZone : MonoBehaviour
{
    private VehicleRuntime _vehicle;

    private void Awake()
    {
        _vehicle = GetComponentInParent<VehicleRuntime>();
        
        if (_vehicle == null)
        {
            Debug.LogError("[VehicleTrunkZone] Pas de VehicleRuntime trouvé sur le parent ! " +
                          "Ce script doit être sur un enfant du véhicule.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ValueObject>(out var obj))
            _vehicle?.OnObjectEnteredTrunk(obj);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<ValueObject>(out var obj))
            _vehicle?.OnObjectLeftTrunk(obj);
    }
}