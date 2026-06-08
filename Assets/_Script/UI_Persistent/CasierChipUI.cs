// ============================================================
// CasierChipUI.cs — binder d'un "chip" du casier (outil ou conso).
// À mettre sur la RACINE du prefab chip (avec CasierDragItem).
// Remove/Moins/Plus ne servent que quand le chip est DANS un slot.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasierChipUI : MonoBehaviour
{
    public Image    Icon;
    public TMP_Text Nom;
    public TMP_Text Extra;   // pips niveau (outil) OU "qté/max" (conso)

    [Header("Optionnels (chip posé dans un slot)")]
    public Button Remove;    // retirer du loadout
    public Button Moins;     // conso : qté −
    public Button Plus;      // conso : qté +

    public void SetIcon(Sprite s)
    {
        if (Icon == null) return;
        Icon.sprite  = s;
        Icon.enabled = s != null;
    }

    public void ShowBoutons(bool remove, bool stepper)
    {
        if (Remove) Remove.gameObject.SetActive(remove);
        if (Moins)  Moins.gameObject.SetActive(stepper);
        if (Plus)   Plus.gameObject.SetActive(stepper);
    }

    /// <summary>Affiche/masque le nom et l'extra (slots roue = icône seule).</summary>
    public void SetVisible(bool nom, bool extra)
    {
        if (Nom)   Nom.gameObject.SetActive(nom);
        if (Extra) Extra.gameObject.SetActive(extra);
    }
}
