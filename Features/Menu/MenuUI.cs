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

    [Header("Panneaux")]
    [Tooltip("Panneau Options — désactivé par défaut, activé sur clic Options")]
    [SerializeField] private GameObject _panneauOptions;

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

        // Coop désactivé en V1
        if (_boutonCoop != null)
            _boutonCoop.interactable = false;

        // Options désactivées en V1
        if (_boutonPersonnalisation != null)
            _boutonPersonnalisation.interactable = false;

        // Panneau options fermé au départ
        if (_panneauOptions != null)
            _panneauOptions.SetActive(false);
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
        GameManager.Instance?.AllerAuHub();
    }

    private void OnCoop()
    {
        // TODO V2 : lancer le lobby multijoueur
        Debug.Log("[Menu] Coop — non implémenté en V1");
    }

    private void OnPersonnalisation()
    {
        SceneLoader.Instance?.ChargerScene(SceneNames.PERSONNALISATION, avecFondu: true);
    }

    private void OnOptions()
    {
        if (_panneauOptions != null)
            _panneauOptions.SetActive(true);
    }


    private void OnQuitter()
    {
        GameManager.Instance?.QuitterJeu();
    }
}
