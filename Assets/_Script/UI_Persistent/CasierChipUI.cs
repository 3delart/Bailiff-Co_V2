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

    /// <summary>Mode slot : que l'icône (cache nom/extra/boutons, fond transparent, icône qui remplit le slot).</summary>
    public void ModeSlotIcone()
    {
        SetVisible(false, false);
        ShowBoutons(false, false);
        var bg = GetComponent<Image>();
        if (bg) { var c = bg.color; c.a = 0f; bg.color = c; }

        // Le chip remplit le slot (sinon il garde sa taille "colonne" et déborde/décale).
        if (transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        if (Icon)
        {
            var irt = Icon.rectTransform;
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(6, 6); irt.offsetMax = new Vector2(-6, -6);
        }
    }
}
