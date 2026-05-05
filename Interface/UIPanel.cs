// ============================================================
// UIPanel.cs — Bailiff & Co  V2
// Classe de base abstraite pour tous les panels UI.
//
// - Enregistre/désenregistre automatiquement le panel dans UIManager
//   via OnEnable/OnDisable → déclenche UpdateUIState (input + curseur).
// - Expose Ouvrir() / Fermer() comme API unifiée pour tous les panels.
//   Les panels avec logique spécifique peuvent override ces méthodes.
// ============================================================
using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [SerializeField] protected UIPanelType panelType;
    public UIPanelType PanelType => panelType;

    // ================================================================
    // LIFECYCLE — ENREGISTREMENT AUTOMATIQUE
    // ================================================================

    protected virtual void OnEnable()
    {
        Debug.Log($"[UIPanel] {GetType().Name} OnEnable - PanelType: {panelType}, UIManager: {UIManager.Instance != null}");
        UIManager.Instance?.RegisterPanel(this);
    }

    protected virtual void OnDisable()
    {
        UIManager.Instance?.UnregisterPanel(this);
    }

    // ================================================================
    // API UNIFIÉE — OUVRIR / FERMER
    // ================================================================

    /// <summary>
    /// Affiche le panel. Override pour ajouter une logique spécifique.
    /// Toujours appeler base.Ouvrir() pour déclencher OnEnable → RegisterPanel.
    /// </summary>
    public virtual void Ouvrir()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else
        {
            // Déjà actif → forcer le RegisterPanel manuellement
            UIManager.Instance?.RegisterPanel(this);
        }
    }

    /// <summary>
    /// Cache le panel. Override pour ajouter une logique spécifique.
    /// Toujours appeler base.Fermer() pour déclencher OnDisable → UnregisterPanel.
    /// </summary>
    public virtual void Fermer() => gameObject.SetActive(false); 

    /// <summary>True si le panel est actuellement affiché.</summary>
    public bool EstOuvert => gameObject.activeSelf;
}