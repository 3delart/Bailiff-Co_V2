// ============================================================
// PauseMenu.cs — Bailiff & Co  V2
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Boutons")]
    [SerializeField] private Button _boutonReprendre;
    [SerializeField] private Button _boutonAbandonner;
    [SerializeField] private Button _boutonPersonnalisation;
    [SerializeField] private Button _boutonOptions;
    [SerializeField] private Button _boutonMenu;

    [Header("Popup confirmation")]
    [SerializeField] private GameObject      _popupConfirmation;
    [SerializeField] private TextMeshProUGUI _texteConfirmation;
    [SerializeField] private Button          _boutonConfirmerOui;
    [SerializeField] private Button          _boutonConfirmerNon;

    [Header("Options")]
    [SerializeField] private GameObject _panneauOptions;

    [SerializeField] private bool bloqueInput = true;

    private bool _ouvert       = false;
    private System.Action _actionConfirmee;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        gameObject.SetActive(false);

        _boutonReprendre?.onClick.AddListener(Fermer);
        _boutonAbandonner?.onClick.AddListener(OnAbandonner);
        _boutonPersonnalisation?.onClick.AddListener(OnPersonnalisation);
        _boutonOptions?.onClick.AddListener(OnOptions);
        _boutonMenu?.onClick.AddListener(OnMenu);
        _boutonConfirmerOui?.onClick.AddListener(OnConfirmerOui);
        _boutonConfirmerNon?.onClick.AddListener(OnConfirmerNon);

        if (_popupConfirmation != null)
            _popupConfirmation.SetActive(false);

        
    }


    private void OnDestroy()
    {
        _boutonReprendre?.onClick.RemoveAllListeners();
        _boutonAbandonner?.onClick.RemoveAllListeners();
        _boutonPersonnalisation?.onClick.RemoveAllListeners();
        _boutonOptions?.onClick.RemoveAllListeners();
        _boutonMenu?.onClick.RemoveAllListeners();
        _boutonConfirmerOui?.onClick.RemoveAllListeners();
        _boutonConfirmerNon?.onClick.RemoveAllListeners();
    }

    private void OnEnable()
    {
        UIManager.Instance.SetPanelOpen(true, bloqueInput);
    }

    private void OnDisable()
    {
        UIManager.Instance.SetPanelOpen(false, bloqueInput);
    }


    // ================================================================
    // OUVRIR / FERMER
    // ================================================================

    public void Ouvrir()
    {

        if (_ouvert) return;

        if (GameManager.Instance.ContexteActuel == ContexteJeu.Menu) return;
      
        _ouvert = true;
        gameObject.SetActive(true);

        Time.timeScale = 1f;

        bool estEnMission = GameManager.Instance.ContexteActuel == ContexteJeu.Mission;

        // Affiche "Abandonner" seulement en mission
        _boutonAbandonner?.gameObject.SetActive(estEnMission);
        
        // Affiche "Personnalisation" seulement hors mission
        _boutonPersonnalisation?.gameObject.SetActive(!estEnMission);


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


    }

    public void Fermer()
    {

        if (!_ouvert) return;
        
        _ouvert = false;
        gameObject.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ================================================================
    // BOUTONS
    // ================================================================

    private void OnAbandonner()
    {
        DemanderConfirmation(
            "Abandonner la mission ?\nLa progression sera perdue.",
            () => { Time.timeScale = 1f; GameManager.Instance?.AllerAuHub(); }
        );
    }

    private void OnPersonnalisation()
    {
        Fermer();
        SceneLoader.Instance?.ChargerScene(SceneNames.PERSONNALISATION, avecFondu: true);
    }

    private void OnOptions()
    {
        if (_panneauOptions != null)
            _panneauOptions.SetActive(!_panneauOptions.activeSelf);
    }

    private void OnMenu()
    {
        DemanderConfirmation("Retourner au menu ?\nLa mission sera abandonnée.",() => {Time.timeScale = 1f; GameManager.Instance?.AllerAuMenu() ;});
    }

    // ================================================================
    // POPUP CONFIRMATION
    // ================================================================

    private void DemanderConfirmation(string message, System.Action onOui)
    {
        if (_popupConfirmation == null)
        {
            onOui?.Invoke();
            return;
        }

        _actionConfirmee = onOui;
        if (_texteConfirmation != null) _texteConfirmation.text = message;
        _popupConfirmation.SetActive(true);
    }

    private void OnConfirmerOui()
    {
        _popupConfirmation.SetActive(false);
        _actionConfirmee?.Invoke();
        _actionConfirmee = null;
    }

    private void OnConfirmerNon()
    {
        _popupConfirmation.SetActive(false);
        _actionConfirmee = null;
    }

    // ================================================================
    // PROPRIÉTÉ
    // ================================================================

    public bool EstOuvert => _ouvert;
}