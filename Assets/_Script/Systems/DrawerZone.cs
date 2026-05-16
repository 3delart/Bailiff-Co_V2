using UnityEngine;

public class DrawerZone : MonoBehaviour
{
    [SerializeField] DrawerInteractable _drawer;
    int _layerCarried;

    void Awake()
    {
        _layerCarried = LayerMask.NameToLayer("ObjetPorte");
    }

    void OnTriggerStay(Collider c)
    {
        ValueObject vo = c.GetComponent<ValueObject>();
        if (vo == null) return;

        // Skip objects being carried
        if (c.gameObject.layer == _layerCarried) return;

        // Skip objects already in drawer
        if (vo.transform.IsChildOf(_drawer.transform)) return;

        // Reparent to drawer
        vo.transform.SetParent(_drawer.transform);

        // Freeze physics
        Rigidbody rb = vo.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }
    }
}
