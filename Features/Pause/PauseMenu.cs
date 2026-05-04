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

    private void OnEnable()
    {
        EventBus<OnMissionDemarree>.Subscribe(OnMissionDemarreeHandler);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTermineeHandler);
    }

    private void OnDisable()
    {
        EventBus<OnMissionDemarree>.Unsubscribe(OnMissionDemarreeHandler);
        EventBus<OnMissionTerminee>.Unsubscribe(OnMissionTermineeHandler);
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

    // ================================================================
    // HANDLERS EVENTBUS
    // ================================================================

    private void OnMissionDemarreeHandler(OnMissionDemarree evt)
    {
        GameManager.Instance?.ContexteActuel = ContexteJeu.Mission;
    }

    private void OnMissionTermineeHandler(OnMissionTerminee evt)
    {
        GameManager.Instance?.ContexteActuel = ContexteJeu.Hub;
    }

    // ================================================================
    // OUVRIR / FERMER
    // ================================================================

    public void Ouvrir()
    {
        _ouvert = true;
        gameObject.SetActive(true);

        Time.timeScale = 1f;

        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Mission)
            {EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Mission });}

        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Hub)
            {EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Hub });}
        

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_popupConfirmation != null)
            _popupConfirmation.SetActive(false);

    }

    public void Fermer()
    {
        _ouvert = false;
        gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Mission)
            {EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Mission });}
        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Hub)
            {EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Hub });}

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
        if (GameManager.Instance?.ContexteActuel == ContexteJeu.Mission)
        {
            DemanderConfirmation(
                "Retourner au menu ?\nLa mission sera abandonnée.",
                () => { Time.timeScale = 1f; GameManager.Instance?.AllerAuMenu(); }
            );
        }
        else
        {
            GameManager.Instance?.AllerAuMenu();
        }
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