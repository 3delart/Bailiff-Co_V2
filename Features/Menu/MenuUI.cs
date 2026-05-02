// ============================================================
// MenuUI.cs — Bailiff & Co
// Gère les boutons du menu principal.
// À attacher sur le Canvas de la scène Menu.
//
// BOUTONS :
//   Play            → Hub
//   Rejoindre       → Coop (V2)
//   Personnalisation → Scène CharacterCustomization
//   Options         → Panneau Options (V2)
//   Exit            → Quitter
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
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

    private void Start()
    {
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
        _boutonJouer?.onClick.RemoveListener(OnJouer);
        _boutonCoop?.onClick.RemoveListener(OnCoop);
        _boutonPersonnalisation?.onClick.RemoveListener(OnPersonnalisation);
        _boutonOptions?.onClick.RemoveListener(OnOptions);
        _boutonQuitter?.onClick.RemoveListener(OnQuitter);
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
        Debug.Log("[Menu] Coop — non implémenté en V1");
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