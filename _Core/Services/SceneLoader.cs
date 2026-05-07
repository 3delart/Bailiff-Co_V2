// ============================================================
// SceneLoader.cs — Bailiff & Co  V2
// Singleton persistant. Gère TOUTES les transitions de scènes.
// Fondu noir via CanvasGroup sur un panneau UI noir.
//
// CHANGEMENTS V2 :
//   - EventBusHelper.ClearAll() appelé avant chaque chargement
//     → corrige la fuite mémoire des handlers orphelins (bug V1)
//   - UI_Persistent chargée en additive UNE SEULE FOIS au démarrage
//     via BootstrapLoader (pas ici)
//   - FondNoir géré en interne, plus via HUDSystem
//
// SETUP UNITY (sur le même GameObject que GameManager) :
//   1. Attacher ce script sur le GameObject "GameManager" (Bootstrap)
//   2. Créer un Canvas enfant "FonduCanvas" :
//        Canvas (Screen Space Overlay, Sort Order 999)
//        └── PanneauNoir (Image noire, stretch full screen)
//            └── CanvasGroup (alpha = 0 au départ)
//   3. Assigner _canvasGroupFondu dans l'Inspector
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // ================================================================
    // SINGLETON
    // ================================================================

    public static SceneLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        if (_canvasGroupFondu != null)
        {
            _canvasGroupFondu.alpha          = 0f;
            _canvasGroupFondu.blocksRaycasts = false;
            _canvasGroupFondu.interactable   = false;
        }
    }

    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Fondu noir")]
    [SerializeField] private CanvasGroup _canvasGroupFondu;
    [SerializeField] private float       _dureeFonduOut = 0.5f;
    [SerializeField] private float       _dureeFonduIn  = 0.5f;

    // ================================================================
    // ÉTAT
    // ================================================================

    private bool _enTransition    = false;

    public bool EnTransition => _enTransition;

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Charge une scène en Single, avec fondu noir optionnel.
    /// Appelle EventBusHelper.ClearAll() avant le chargement.
    /// </summary>
    public void ChargerScene(string nomScene, bool avecFondu = true)
    {
        if (_enTransition)
        {
            return;
        }

        if (avecFondu && _canvasGroupFondu != null)
            StartCoroutine(TransitionAvecFondu(nomScene));
        else
            StartCoroutine(ChargerDirectement(nomScene));
    }

    /// <summary>
    /// Charge UI_Persistent en mode Additive.
    /// Appelé UNE SEULE FOIS depuis BootstrapLoader.
    /// </summary>
    public IEnumerator ChargerUIPersistentAdditive()
    {
        // Si UIManager existe déjà, UI_Persistent est déjà persistante
        if (UIManager.Instance != null)
        {
            yield break;
        }


        AsyncOperation op = SceneManager.LoadSceneAsync(
            SceneNames.UI_PERSISTENT, LoadSceneMode.Additive);
        yield return op;
    }

    /// <summary>Fondu vers le noir uniquement (sans changer de scène).</summary>
    public void FondNoir(float duree = 1f)
    {
        if (_canvasGroupFondu != null)
            StartCoroutine(AnimerFondu(0f, 1f, duree));
    }

    // ================================================================
    // COROUTINES
    // ================================================================

    private IEnumerator TransitionAvecFondu(string nomScene)
    {
        _enTransition = true;
        yield return StartCoroutine(AnimerFondu(0f, 1f, _dureeFonduOut));

        EventBusHelper.ClearAll();
        UIManager.Instance?.ReSubscribe();

        AsyncOperation op = SceneManager.LoadSceneAsync(nomScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;
        yield return null;
        yield return null;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        yield return null;
        yield return null;

        yield return StartCoroutine(ChargerUIPersistentAdditive());
        yield return null;
        yield return null;

        // ← D'ABORD le fondu de sortie
        yield return StartCoroutine(AnimerFondu(1f, 0f, _dureeFonduIn));

        ContexteJeu contexte = nomScene switch
        {
            SceneNames.MENU => ContexteJeu.Menu,
            SceneNames.HUB => ContexteJeu.Hub,
            SceneNames.MISSION => ContexteJeu.Mission,
            SceneNames.PERSONNALISATION => ContexteJeu.Personnalisation,
            _ => GameManager.Instance.ContexteActuel  // garde l'actuel si inconnu
        };
        
        GameManager.Instance.SetContexte(contexte);  // ← AJOUTE
        
        EventBus<OnSceneChargee>.Raise(new OnSceneChargee { NomScene = nomScene });
        _enTransition = false;
    }
    

    private IEnumerator ChargerDirectement(string nomScene)
    {
        _enTransition = true;
        EventBusHelper.ClearAll();
        
        // Single décharge tout SAUF UI_Persistent qu'on recharge en additive
        yield return SceneManager.LoadSceneAsync(nomScene, LoadSceneMode.Single);
        
        // Recharge UI_Persistent si elle a été déchargée
        yield return StartCoroutine(ChargerUIPersistentAdditive());

        yield return null;
        yield return null;

        EventBus<OnSceneChargee>.Raise(new OnSceneChargee { NomScene = nomScene });
        
        _enTransition = false;
    }

    private IEnumerator AnimerFondu(float alphaDebut, float alphaFin, float duree)
    {
        if (_canvasGroupFondu == null) yield break;

        _canvasGroupFondu.alpha          = alphaDebut;
        _canvasGroupFondu.blocksRaycasts = alphaDebut >= 1f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duree;
            _canvasGroupFondu.alpha = Mathf.Lerp(alphaDebut, alphaFin, Mathf.Clamp01(t));
            yield return null;
        }

        _canvasGroupFondu.alpha          = alphaFin;
        _canvasGroupFondu.blocksRaycasts = alphaFin >= 1f;
    }

    // ================================================================
    // ABONNEMENTS AUX EVENTS
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnFondNoir>.Subscribe(OnFondNoirEvent);
        EventBus<OnMissionTerminee>.Subscribe(OnMissionTerminee);
    }

    private void OnDisable()
    {
        EventBus<OnFondNoir>.Unsubscribe(OnFondNoirEvent);
        EventBus<OnMissionTerminee>.Unsubscribe(OnMissionTerminee);
    }

    private void OnFondNoirEvent(OnFondNoir e) => FondNoir(e.DureeSecondes);

    private void OnMissionTerminee(OnMissionTerminee e)
    {
        StartCoroutine(RetourHubApresDelai(e.Resultat, 3f));
    }

    private IEnumerator RetourHubApresDelai(MissionResult resultat, float delai)
    {
        yield return new WaitForSeconds(delai);
        GameManager.Instance?.TerminerMission(resultat);
    }
}
