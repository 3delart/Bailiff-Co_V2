// ============================================================
// PlacementPreview.cs — Bailiff & Co  V3
// Ghost preview system for precise object placement.
// Handles surface detection, rotation input, and validity checking.
// ============================================================
using UnityEngine;
using BailiffCo;

public class PlacementPreview : MonoBehaviour
{
    private GameObject _ghost;
    private Material   _ghostMaterial;
    private Vector3    _ghostPosition;
    private Quaternion _ghostRotation;
    private float      _ghostYaw = 0f;
    private bool       _isValid = false;
    private ValueObject _heldObject;
    private PlayerConfigData _config;
    private Collider   _hitSurface; // Track which collider was hit by raycast

    private const float GHOST_ALPHA = 0.35f;
    private const float VALID_SURFACE_DOT = 0.7f;

    // ================================================================
    // PUBLIC API
    // ================================================================

    public void BeginPreview(ValueObject obj, Transform camera, PlayerConfigData config)
    {
        _heldObject = obj;
        _config = config;
        _ghostYaw = 0f;

        // Instantiate object as ghost (clones full hierarchy with all meshes)
        _ghost = Instantiate(obj.gameObject, obj.transform.position, obj.transform.rotation);
        _ghost.name = "PlacementGhost";

        // Hide the original carried object during preview
        var origRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in origRenderers)
            renderer.enabled = false;

        // Disable ValueObject script and colliders on ghost
        var valueObjComponent = _ghost.GetComponent<ValueObject>();
        if (valueObjComponent != null)
            valueObjComponent.enabled = false;

        foreach (var col in _ghost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Make all renderers transparent green
        var renderers = _ghost.GetComponentsInChildren<MeshRenderer>();
        _ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _ghostMaterial.SetFloat("_Surface", 1);
        _ghostMaterial.SetFloat("_Blend", 0);
        _ghostMaterial.renderQueue = 3000;
        _ghostMaterial.color = new Color(0.2f, 0.8f, 0.2f, GHOST_ALPHA);

        foreach (var renderer in renderers)
        {
            renderer.material = _ghostMaterial;
        }

        _ghostPosition = obj.transform.position;
        _ghostRotation = obj.transform.rotation;
    }

    public void UpdatePreview()
    {
        if (_ghost == null || _heldObject == null || _config == null) return;

        // Read rotation input (scroll wheel)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _ghostYaw += scroll * _config.PlacementRotationSpeed;
        }

        // Raycast for surface
        var camera = Camera.main;
        if (camera != null)
        {
            RaycastHit hit;
            Vector3 rayOrigin = camera.transform.position;

            // Cast straight forward (removed downward bias to detect meubles/remorques at any height)
            Vector3 rayDir = camera.transform.forward;

            const float MIN_DISTANCE = 0.5f;

            _isValid = false;

            // Raycast on all layers except the carried object layer
            int layerMask = Physics.AllLayers;
            int objetPorteLayer = LayerMask.NameToLayer("ObjetPorte");
            if (objetPorteLayer != -1)
                layerMask = ~(1 << objetPorteLayer);

            if (Physics.Raycast(rayOrigin, rayDir, out hit, _config.PlacementRaycastRange, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= MIN_DISTANCE)
                {
                    float dotNormal = Vector3.Dot(hit.normal, Vector3.up);
                    if (dotNormal >= _config.PlacementMaxSlopeDot)
                    {
                        _hitSurface = hit.collider; // Remember hit surface — ignore in collision check

                        // Use object bounds height for proper offset
                        Bounds bounds = GetObjectBounds(_heldObject.gameObject);
                        float boundsHeight = bounds.extents.y * 2f;
                        Vector3 candidatePos = hit.point + Vector3.up * (boundsHeight * 0.5f);

                        // Check for collisions at placement position
                        if (CheckCollisionsAtPosition(candidatePos))
                        {
                            _ghostPosition = candidatePos;
                            _isValid = true;
                        }
                        else
                        {
                            // Collision detected, placement invalid
                            _isValid = false;
                        }
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 0f);
                        Debug.Log($"[Placement] Surface too steep: {hit.collider.gameObject.name}, Slope: {dotNormal:F2}");
                        #endif
                    }
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.DrawRay(rayOrigin, rayDir * _config.PlacementRaycastRange, Color.red, 0f);
                #endif
            }
        }

        // Update ghost appearance
        _ghostRotation = Quaternion.Euler(0, _ghostYaw, 0) * _heldObject.transform.rotation;
        _ghost.transform.position = _ghostPosition;
        _ghost.transform.rotation = _ghostRotation;

        // Color feedback
        if (_ghostMaterial != null)
        {
            Color color = _isValid
                ? new Color(0.2f, 0.8f, 0.2f, GHOST_ALPHA)
                : new Color(0.8f, 0.2f, 0.2f, GHOST_ALPHA);
            _ghostMaterial.color = color;
        }
    }

    public void EndPreview()
    {
        // Restore visibility of original carried object
        if (_heldObject != null)
        {
            var origRenderers = _heldObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in origRenderers)
                renderer.enabled = true;
        }

        if (_ghost != null)
            Destroy(_ghost);
        _ghost = null;
        _ghostMaterial = null;
        _heldObject = null;
        
    }

    public bool IsValid => _isValid;
    public Vector3 PreviewPosition => _ghostPosition;
    public Quaternion PreviewRotation => _ghostRotation;

    // ================================================================
    // PRIVATE
    // ================================================================

    private Bounds GetObjectBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one * 0.1f);

        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private bool CheckCollisionsAtPosition(Vector3 position)
    {
        if (_ghost == null) return false;

        // Get ghost's actual bounds
        Bounds ghostBounds = GetObjectBounds(_ghost);
        Vector3 halfExtents = ghostBounds.extents;

        // Check only for actual game objects, exclude ObjetPorte layer and triggers
        int layerMask = ~LayerMask.GetMask("ObjetPorte");
        Collider[] overlaps = Physics.OverlapBox(position, halfExtents,
            _ghostRotation, layerMask, QueryTriggerInteraction.Ignore);

        // Filter out trunk zones and vehicle/trailer structure — they're containers
        foreach (var col in overlaps)
        {
            // Ignore the surface we're placing on
            if (col == _hitSurface)
                continue;

            // Ignore trunk zones
            if (col.GetComponent<VehicleTrunkZone>() != null)
                continue;

            // Ignore vehicle/trailer bodies — check if part of vehicle hierarchy
            if (col.gameObject.name.Contains("Vehicle") ||
                col.gameObject.name.Contains("Remorque") ||
                col.gameObject.name.Contains("Trailer") ||
                col.gameObject.name.Contains("Coffre") ||
                col.gameObject.name.Contains("Trunk"))
                continue;

            return false; // Real collision detected
        }
        return true; // Safe to place
    }
}
