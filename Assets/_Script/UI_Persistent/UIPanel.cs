// ============================================================
// UIPanel.cs — Bailiff & Co  V2
// Classe de base pour tous les panels UI.
//
// Chaque panel déclare dans l'Inspector :
//   _contexteVisibles : les contextes où il a le droit d'exister
//   _autoAfficher     : s'ouvre automatiquement à l'entrée du contexte
//
// UIManager appelle EvaluerContexte() sur tous les panels
// à chaque changement de contexte (OnContextChanged).
//
// RÈGLE ABSOLUE : aucun SetActive() direct depuis l'extérieur.
// Tout passe par Ouvrir() / Fermer(). Override autorisé —
// doit toujours appeler base.Ouvrir() / base.Fermer().
// ============================================================
using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    [Header("Type de panel")]
    [SerializeField] protected UIPanelType panelType;
    public UIPanelType PanelType => panelType;

    [Header("Contexte de visibilité")]
    [Tooltip("Contextes dans lesquels ce panel a le droit d'exister.")]
    [SerializeField] private ContexteJeu[] _contexteVisibles;

    [Tooltip("Si coché, s'ouvre automatiquement dès l'entrée dans un contexte autorisé.\n" +
             "Si non coché, attend un déclencheur explicite (Ouvrir() ou input joueur).")]
    [SerializeField] private bool _autoAfficher;

    // ================================================================
    // LIFECYCLE — ENREGISTREMENT ACTIF
    // ================================================================

    protected virtual void Awake() { }

    protected virtual void OnEnable()
        => UIManager.Instance?.RegisterPanel(this);

    protected virtual void OnDisable()
        => UIManager.Instance?.UnregisterPanel(this);

    // ================================================================
    // ÉVALUATION CONTEXTE — appelé par UIManager sur OnContextChanged
    // ================================================================

    /// <summary>
    /// Évalue si ce panel doit être visible dans le contexte donné.
    /// Appelé par UIManager à chaque changement de ContexteJeu.
    /// </summary>
    public void EvaluerContexte(ContexteJeu contexte)
    {
        bool autorise = _contexteVisibles != null
            && System.Array.IndexOf(_contexteVisibles, contexte) >= 0;

        if (!autorise)
        {
            Fermer();
        }
        else if (_autoAfficher)
        {
            Ouvrir();
        }
        // contexte autorisé + _autoAfficher = false → ne rien faire
    }

    // ================================================================
    // API UNIFIÉE — OUVRIR / FERMER
    // ================================================================

    public virtual void Ouvrir()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        else
            UIManager.Instance?.RegisterPanel(this);
    }

    public virtual void Fermer() => gameObject.SetActive(false);

    public bool EstOuvert => gameObject.activeSelf;
}
