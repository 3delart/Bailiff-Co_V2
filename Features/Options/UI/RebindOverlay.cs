// ============================================================
// RebindOverlay.cs — Bailiff & Co  V2
// Overlay plein écran affiché pendant la capture d'une touche.
// Hérite de UIPanel — géré par le système de contexte.
// _contexteVisibles = [Menu, Hub, Mission], _autoAfficher = false (Inspector)
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

public class RebindOverlay : UIPanel
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
    [SerializeField] private string _prefixeAction = "Rebind : ";
    [SerializeField] private string _consigne       = "Appuyez sur une touche…\n<size=70%>(Échap pour annuler)</size>";

    // ================================================================
    // ÉTAT
    // ================================================================

    private Action<KeyCode> _callback;
    private Coroutine       _captureCoroutine;

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Affiche l'overlay et démarre la capture.
    /// </summary>
    public void Afficher(string nomAction, Action<KeyCode> callback)
    {
        _callback = callback;

        if (_texteAction   != null) _texteAction.text   = _prefixeAction + nomAction;
        if (_texteConsigne != null) _texteConsigne.text = _consigne;

        Ouvrir(); // SetActive(true) → OnEnable → RegisterPanel

        if (_captureCoroutine != null)
            StopCoroutine(_captureCoroutine);
        _captureCoroutine = StartCoroutine(CaptureTouche());
    }

    /// <summary>Cache l'overlay et annule la capture en cours.</summary>
    public void Cacher() => Fermer();

    public override void Fermer()
    {
        if (_captureCoroutine != null)
        {
            StopCoroutine(_captureCoroutine);
            _captureCoroutine = null;
        }
        _callback = null;
        base.Fermer(); // SetActive(false) → OnDisable → UnregisterPanel
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Terminer(KeyCode.None, annule: true);
                yield break;
            }

            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!KeyRebindUI.EstToucheValide(kc)) continue;
                if (kc == KeyCode.Escape)              continue;

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
        var cb = _callback;
        _callback = null;
        base.Fermer(); // SetActive(false) directement — coroutine déjà nulle
        if (!annule && cb != null) cb.Invoke(kc);
    }
}
