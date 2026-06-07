// ============================================================
// CasierDropSlot.cs — cible de drop du casier (EventSystems).
// À mettre sur chaque slot de la roue et sur les 2 colonnes (pools).
// ============================================================
using UnityEngine;
using UnityEngine.EventSystems;

public class CasierDropSlot : MonoBehaviour, IDropHandler
{
    public enum Zone { ToolSlot, ConsoSlot, ToolPool, ConsoPool }
    public Zone SlotZone;
    public int  Index; // 0..2 pour les slots de roue

    public void OnDrop(PointerEventData e)
    {
        CasierUI.Current?.HandleDrop(this);
    }
}
