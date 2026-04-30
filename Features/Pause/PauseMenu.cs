// ============================================================
// PauseMenu.cs — Bailiff & Co  [v2]
// Menu pause universel — fonctionne en Hub ET en Mission.
// [Echap] → ouvre/ferme le menu.
//
// CHANGEMENTS v1 → v2 :
//   - _estEnMission n'est PLUS un [SerializeField].
//     Il était fragile (état oublié dans l'Inspector, désynchronisé).
//     Il est maintenant géré à l'exécution via EventBus :
//       OnMissionDemarree → _estEnMission = true
//       OnMissionTerminee → _estEnMission = false
//   - Abonnements EventBus dans OnEnable / OnDisable.
//   - Comportement Ouvrir/Fermer/boutons : identique à v1.
//
// EN HUB (par défaut, _estEnMission = false) :
//   - Reprendre          → ferme le menu
//   - Personnalisation   → scène CharacterCustomization
//   - Options            → panneau options
//   - Menu principal     → GameManager.AllerAuMenu() (pas de confirmation)
//   - Bouton Abandonner  → masqué
//
// EN MISSION (_estEnMission = true, via EventBus) :
//   - Reprendre          → ferme le menu + Time.timeScale = 1
//   - Abandonner         → confirme puis retour Hub sans sauvegarder
//   - Options            → panneau options
//   - Menu principal     → confirme puis GameManager.AllerAuMenu()
//   - Bouton Personnalisation → masqué
//
// SETUP UNITY — Canvas (Screen Space Overlay, Sort Order 100) :
//   Dans la scène UI_Persistent (chargée en Additive, toujours présente) :
//
//   PauseMenu (ce script)              → désactivé au départ
//   └── Fond                           → Image noire alpha ~0.6, stretch full
//       └── Carte                      → Image centrée ~280x340px
//           ├── Titre                  → TMP "PAUSE"
//           ├── BoutonReprendre        → Button  → _boutonReprendre
//           ├── BoutonAbandonner       → Button  → _boutonAbandonner
//           ├── BoutonOptions          → Button  → _boutonOptions
//           ├── BoutonPersonnalisation → Button  → _boutonPersonnalisation
//           ├── BoutonMenu             → Button  → _boutonMenu
//           └── PopupConfirmation      → GameObject → _popupConfirmation
//               ├── TexteConfirm       → TMP    → _texteConfirmation
//               ├── BoutonOui          → Button → _boutonConfirmerOui
//               └── BoutonNon         → Button → _boutonConfirmerNon
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Boutons")]
    [SerializeField] private Button _boutonReprendre;
    [SerializeField] private Button _boutonAbandonner;       // caché en Hub
    [SerializeField] private Button _boutonPersonnalisation; // caché en Mission
    [SerializeField] private Button _boutonOptions;
    [SerializeField] private Button _boutonMenu;

    [Header("Popup confirmation")]
    [SerializeField] private GameObject      _popupConfirmation;
    [SerializeField] private TextMeshProUGUI _texteConfirmation;
    [SerializeField] private Button          _boutonConfirmerOui;
    [SerializeField] private Button          _boutonConfirmerNon;

    [Header("Options")]
    [SerializeField] private GameObject _panneauOptions;

    // ================================================================
    // ÉTAT
    // ================================================================

    // v2 : plus de [SerializeField] — géré exclusivement via EventBus
    private bool _estEnMission  = false;
    private bool _ouvert        = false;

    // Action mémorisée pour le popup de confirmation
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

        // État initial : Hub (pas de mission)
        AppliquerContexte();
    }

    // S'abonne au démarrage et à chaque réactivation du GameObject.
    // OnDisable se désabonne pour éviter les fuites si UI_Persistent
    // est rechargée ou si le script est désactivé temporairement.
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

    // ================================================================
    // HANDLERS EVENTBUS
    // ================================================================

    private void OnMissionDemarreeHandler(OnMissionDemarree evt)
    {
        _estEnMission = true;
        AppliquerContexte();
    }

    private void OnMissionTermineeHandler(OnMissionTerminee evt)
    {
        _estEnMission = false;
        AppliquerContexte();

        // Sécurité : si le menu était ouvert quand la mission se termine
        // (ex : résultat forcé), on le ferme proprement.
        if (_ouvert)
            Fermer();
    }

    // ================================================================
    // CONTEXTE — adapte les boutons selon Hub / Mission
    // ================================================================

    private void AppliquerContexte()
    {
        _boutonAbandonner?.gameObject.SetActive(_estEnMission);
        _boutonPersonnalisation?.gameObject.SetActive(!_estEnMission);
    }

    // ================================================================
    // OUVRIR / FERMER
    // ================================================================

    public void Ouvrir()
    {
        _ouvert = true;
        gameObject.SetActive(true);

        if (_estEnMission)
            Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_popupConfirmation != null)
            _popupConfirmation.SetActive(false);
    }

    public void Fermer()
    {
        _ouvert = false;
        gameObject.SetActive(false);

        if (_estEnMission)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else
        {
            // Hub → curseur libre
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    // ================================================================
    // HANDLERS BOUTONS
    // ================================================================

    private void OnAbandonner()
    {
        DemanderConfirmation(
            "Abandonner la mission ?\nLa progression sera perdue.",
            () =>
            {
                Time.timeScale = 1f;
                GameManager.Instance?.AllerAuHub();
            }
        );
    }

    private void OnPersonnalisation()
    {
        // Disponible uniquement en Hub
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
        if (_estEnMission)
        {
            DemanderConfirmation(
                "Retourner au menu ?\nLa mission sera abandonnée.",
                () =>
                {
                    Time.timeScale = 1f;
                    GameManager.Instance?.AllerAuMenu();
                }
            );
        }
        else
        {
            // En Hub : pas besoin de confirmation
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
            // Pas de popup → exécute directement
            onOui?.Invoke();
            return;
        }

        _actionConfirmee = onOui;

        if (_texteConfirmation != null)
            _texteConfirmation.text = message;

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
    // PROPRIÉTÉS
    // ================================================================

    public bool EstOuvert => _ouvert;
}
