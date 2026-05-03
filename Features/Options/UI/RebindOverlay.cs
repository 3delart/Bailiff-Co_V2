// ============================================================
// RebindOverlay.cs — Bailiff & Co  [v2]
// Overlay plein écran affiché pendant la capture d'une touche.
// Placé dans UI_Persistent — une seule instance pour tout le jeu.
//
// CHANGEMENTS v1 → v2 :
//   - Déplacé dans Features/Options/UI/ (plus de copie par scène).
//   - Logique de capture inchangée.
//
// HIÉRARCHIE (prefab RebindOverlay) :
//   RebindOverlay (ce script, désactivé par défaut)
//   ├── Fond        (Image noire alpha ~0.75, stretch fullscreen)
//   └── Carte       (Image centrée ~400x180px)
//       ├── TexteAction   TMP → _texteAction   (ex : "Interagir")
//       └── TexteConsigne TMP → _texteConsigne (ex : "Appuyez sur une touche…")
//
// KeyRebindUI appelle :
//   _overlay.Afficher(nomAction, callback);
// puis reçoit OnToucheCapturee(KeyCode) via le callback.
// ============================================================
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RebindOverlay : MonoBehaviour
{
    // ================================================================
    // RÉFÉRENCES UI
    // ================================================================

    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI _texteAction;
    [SerializeField] private TextMeshProUGUI _texteConsigne;

    // ================================================================
    // CONFIG
    // ================================================================

    [Header("Textes")]
    [SerializeField] private string _prefixeAction  = "Rebind : ";
    [SerializeField] private string _consigne        = "Appuyez sur une touche…\n<size=70%>(Échap pour annuler)</size>";

    // ================================================================
    // ÉTAT
    // ================================================================

    private Action<KeyCode> _callback;
    private Coroutine       _captureCoroutine;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        // S'assure que l'overlay est caché au démarrage
        gameObject.SetActive(false);
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Affiche l'overlay et démarre la capture.
    /// </summary>
    /// <param name="nomAction">Nom lisible de l'action (ex : "Interagir").</param>
    /// <param name="callback">Appelé avec le KeyCode capturé, ou KeyCode.None si annulé.</param>
    /// 
    
    public void Afficher(string nomAction, Action<KeyCode> callback)
    {
        _callback = callback;

        if (_texteAction   != null) _texteAction.text   = _prefixeAction + nomAction;
        if (_texteConsigne != null) _texteConsigne.text = _consigne;

        // Active AVANT de démarrer la coroutine
        gameObject.SetActive(true);

        if (_captureCoroutine != null)
            StopCoroutine(_captureCoroutine);
        _captureCoroutine = StartCoroutine(CaptureTouche());
    }

    /// <summary>Cache l'overlay sans déclencher le callback.</summary>
    public void Cacher()
    {
        if (_captureCoroutine != null)
        {
            StopCoroutine(_captureCoroutine);
            _captureCoroutine = null;
        }
        _callback = null;
        gameObject.SetActive(false);
    }

    // ================================================================
    // CAPTURE
    // ================================================================

    private IEnumerator CaptureTouche()
    {
        // Attend que toutes les touches soient relâchées
        // (évite de capturer la touche qui a ouvert l'overlay)
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !Input.anyKey);

        while (true)
        {
            // Annulation
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Terminer(KeyCode.None, annule: true);
                yield break;
            }

            // Parcours de tous les KeyCodes
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!KeyRebindUI.EstToucheValide(kc)) continue;
                if (kc == KeyCode.Escape)              continue; // déjà géré

                if (Input.GetKeyDown(kc))
                {
                    Terminer(kc, annule: false);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void Terminer(KeyCode kc, bool annule)
    {
        _captureCoroutine = null;
        gameObject.SetActive(false);

        if (!annule && _callback != null)
            _callback.Invoke(kc);

        _callback = null;
    }
}
