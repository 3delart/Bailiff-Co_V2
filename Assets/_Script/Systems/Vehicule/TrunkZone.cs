using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BailiffCo
{
    public class TrunkZone : MonoBehaviour
    {
        [SerializeField] private float _surfaceM2 = 1f;
        [SerializeField] private Collider _zoneCollider;

        private readonly HashSet<ValueObject> _objectsInZone = new();

        public float SurfaceM2 => _surfaceM2;
        public float UsedSurface => _objectsInZone.Sum(o => o.Data.SurfaceM2);
        public IEnumerable<ValueObject> ObjectsInZone => _objectsInZone;
        public bool IsFull => UsedSurface >= _surfaceM2;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<ValueObject>(out var obj))
            {
                _objectsInZone.Add(obj);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<ValueObject>(out var obj))
            {
                _objectsInZone.Remove(obj);
            }
        }

        public void RemoveObject(ValueObject obj)
        {
            _objectsInZone.Remove(obj);
        }
    }
}
