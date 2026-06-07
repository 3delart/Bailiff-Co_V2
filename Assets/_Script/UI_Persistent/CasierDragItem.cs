// ============================================================
// CasierDragItem.cs — chip déplaçable du casier (EventSystems).
// À mettre sur le prefab chip ET sur le prefab item-de-slot.
// ============================================================
using UnityEngine;
using UnityEngine.EventSystems;

public class CasierDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Kind { Tool, Conso }
    public Kind   ItemKind;
    public string Id;        // ToolName (outil) ou type (conso)

    private Transform   _root;
    private CanvasGroup _cg;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        var canvas = GetComponentInParent<Canvas>();
        _root = canvas != null ? canvas.transform : transform.root;
        transform.SetParent(_root, true);
        _cg.blocksRaycasts = false; // laisse passer le raycast vers les slots
        CasierUI.Current?.SetDrag(this);
    }

    public void OnDrag(PointerEventData e) { transform.position = e.position; }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = true;
        CasierUI.Current?.EndDrag();
        // CasierUI re-render recrée les éléments → on détruit ce fantôme.
        Destroy(gameObject);
    }
}
