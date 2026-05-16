using UnityEngine;

public class DrawerZone : MonoBehaviour
{
    [SerializeField] DrawerInteractable _drawer;
    int _layerCarried;

    void Awake()
    {
        if (_drawer == null) return;

        _layerCarried = LayerMask.NameToLayer("ObjetPorte");

        var col = GetComponent<Collider>();
        if (col == null) return;

        // Detect pre-placed objects at startup
        Collider[] hits = Physics.OverlapBox(
            col.bounds.center,
            col.bounds.extents
        );

        foreach (var hit in hits)
        {
            ValueObject vo = hit.GetComponent<ValueObject>();
            if (vo != null && !vo.transform.IsChildOf(_drawer.transform))
                ReparentObject(vo);
        }
    }

    void OnTriggerStay(Collider c)
    {
        ValueObject vo = c.GetComponent<ValueObject>();
        if (vo == null) return;

        // Skip objects being carried
        if (c.gameObject.layer == _layerCarried) return;

        // Skip objects already in drawer
        if (vo.transform.IsChildOf(_drawer.transform)) return;

        ReparentObject(vo);
    }

    void ReparentObject(ValueObject vo)
    {
        vo.transform.SetParent(_drawer.transform);

        Rigidbody rb = vo.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
