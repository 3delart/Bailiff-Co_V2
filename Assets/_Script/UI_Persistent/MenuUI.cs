// ============================================================
// MenuUI.cs — Bailiff & Co  V2
// Panel du menu principal. Hérite de UIPanel.
// _contexteVisibles = [Menu], _autoAfficher = true (Inspector)
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : UIPanel
{
    [Header("Boutons")]
    [SerializeField] private Button _boutonJouer;
    [SerializeField] private Button _boutonCoop;
    [SerializeField] private Button _boutonPersonnalisation;
    [SerializeField] private Button _boutonOptions;
    [SerializeField] private Button _boutonQuitter;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake(); // requis si UIPanel.Awake() est utilisé dans le futur

        _boutonJouer?.onClick.AddListener(OnJouer);
        _boutonCoop?.onClick.AddListener(OnCoop);
        _boutonPersonnalisation?.onClick.AddListener(OnPersonnalisation);
        _boutonOptions?.onClick.AddListener(OnOptions);
        _boutonQuitter?.onClick.AddListener(OnQuitter);

        if (_boutonCoop != null)
            _boutonCoop.interactable = false;
        if (_boutonPersonnalisation != null)
            _boutonPersonnalisation.interactable = false;
    }

    private void OnDestroy()
    {
        _boutonJouer?.onClick.RemoveAllListeners();
        _boutonCoop?.onClick.RemoveAllListeners();
        _boutonPersonnalisation?.onClick.RemoveAllListeners();
        _boutonOptions?.onClick.RemoveAllListeners();
        _boutonQuitter?.onClick.RemoveAllListeners();
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnJouer()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.EnTransition) return;
        GameManager.Instance?.AllerAuHub();
    }

    private void OnCoop()
    {
        Debug.Log("[Menu] Coop — non implémenté en V2");
    }

    private void OnPersonnalisation()
    {
        SceneLoader.Instance?.ChargerScene(SceneNames.PERSONNALISATION, avecFondu: true);
    }

    private void OnOptions()
    {
        UIManager.Instance?.OuvrirOptions();
    }

    private void OnQuitter()
    {
        GameManager.Instance?.QuitterJeu();
    }
}
