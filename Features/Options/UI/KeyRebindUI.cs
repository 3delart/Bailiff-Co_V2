// ============================================================
// KeyRebindUI.cs — Bailiff & Co  [v2]
// Ligne de rebind : clic → overlay "Appuyez sur une touche"
// → capture → sauvegarde + rafraîchit toutes les lignes.
// Support AZERTY : affichage des noms localisés.
//
// CHANGEMENTS v1 → v2 :
//   - Déplacé dans Features/Options/UI/.
//   - FindObjectOfType<RebindOverlay> conservé intentionnellement :
//     RebindOverlay est garanti présent dans UI_Persistent (même scène).
//     Si une ref [SerializeField] est préférée, l'injecter depuis OptionsUI.
//
// HIERARCHIE (prefab KeyRebindRow) :
//   KeyRebindRow (ce script)
//   ├── LabelAction   TMP  → _labelAction   (ex: "Interagir")
//   └── BoutonTouche  Button → _boutonTouche
//       └── TexteTouche TMP → _texteTouche  (ex: "E")
// ============================================================
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyRebindUI : MonoBehaviour
{
    [Header("Action associee")]
    [SerializeField] private ActionJeu _action;
    [SerializeField] private string    _nomAffiche = "Action";

    [Header("References UI")]
    [SerializeField] private TextMeshProUGUI _labelAction;
    [SerializeField] private Button          _boutonTouche;
    [SerializeField] private TextMeshProUGUI _texteTouche;

    // Données cibles du rebind — injectées par OptionsUI.SetDataSource()
    // pour écrire dans _dataTemp plutôt que dans OptionsManager.Data directement.
    private OptionsData _dataSource;

    // Overlay global — dans UI_Persistent, toujours disponible dans la scène active.
    private RebindOverlay _overlay;

    private OptionsUI _optionsUI;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _optionsUI = GetComponentInParent<OptionsUI>(includeInactive: true);
        _boutonTouche?.onClick.AddListener(CommencerRebind);
        MettreAJourAffichage();
    }

    private void Start()
    {
        if (_labelAction != null)
            _labelAction.text = _nomAffiche;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_labelAction != null)
            _labelAction.text = _nomAffiche;
    }
#endif

    private void OnEnable() => MettreAJourAffichage();

    // ================================================================
    // REBIND
    // ================================================================

    public void CommencerRebind()
    {
        // Re-valide la référence à chaque clic.
        // RebindOverlay est dans UI_Persistent → toujours présent.
        if (_overlay == null || !_overlay)
            _overlay = FindObjectOfType<RebindOverlay>(includeInactive: true);

        if (_overlay == null)
        {
            Debug.LogWarning("[KeyRebindUI] RebindOverlay introuvable dans UI_Persistent ! Vérifiez le prefab.");
            StartCoroutine(CaptureDirecte());
            return;
        }

        _overlay.Afficher(_nomAffiche, OnToucheCapturee);
    }

    private IEnumerator CaptureDirecte()
    {
        if (_texteTouche != null) _texteTouche.text = "...";

        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !Input.anyKey);

        bool captured = false;
        while (!captured)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { captured = true; break; }

            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!EstToucheValide(kc)) continue;
                if (Input.GetKeyDown(kc))
                {
                    OnToucheCapturee(kc);
                    captured = true;
                    break;
                }
            }
            yield return null;
        }
        MettreAJourAffichage();
    }

    private void OnToucheCapturee(KeyCode kc)
    {
        OptionsData data = _dataSource ?? OptionsManager.Instance?.Data;
        if (data == null) return;

        // Résolution des conflits par échange
        foreach (ActionJeu action in System.Enum.GetValues(typeof(ActionJeu)))
        {
            if (action == _action) continue;
            if (data.GetTouche(action) == kc)
            {
                KeyCode ancienne = data.GetTouche(_action);
                data.SetTouche(action, ancienne);
                break;
            }
        }

        data.SetTouche(_action, kc);
        
        // AJOUT — met à jour l'affichage immédiatement
        MettreAJourAffichage();
        
        // Rafraîchit toutes les lignes si OptionsUI est trouvé
        if (_optionsUI != null)
            _optionsUI.RafraichirToutesTouches();
        else
        {
            // Fallback — trouve OptionsUI dans la scène
            var optionsUI = FindObjectOfType<OptionsUI>(includeInactive: true);
            optionsUI?.RafraichirToutesTouches();
        }
    }

    // ================================================================
    // AFFICHAGE
    // ================================================================

    public void MettreAJourAffichage()
    {
        if (_texteTouche == null) return;

        // Lit depuis _dataSource si disponible, sinon OptionsManager
        OptionsData data = _dataSource ?? OptionsManager.Instance?.Data;
        if (data == null) return;

        KeyCode kc = data.GetTouche(_action);
        _texteTouche.text = FormatKeyCode(kc);
    }

    // ================================================================
    // FORMAT AZERTY
    // ================================================================

    public static string FormatKeyCode(KeyCode kc) => kc switch
    {
        // Lettres AZERTY (Unity stocke en QWERTY physique)
        KeyCode.A           => "Q",
        KeyCode.Q           => "A",
        KeyCode.W           => "Z",
        KeyCode.Z           => "W",
        KeyCode.M           => "M",
        KeyCode.Semicolon   => "M",

        // Modificateurs
        KeyCode.LeftShift   => "Shift G",
        KeyCode.RightShift  => "Shift D",
        KeyCode.LeftControl => "Ctrl G",
        KeyCode.RightControl=> "Ctrl D",
        KeyCode.LeftAlt     => "Alt G",
        KeyCode.RightAlt    => "AltGr",

        // Navigation
        KeyCode.Return      => "Entree",
        KeyCode.Backspace   => "Retour",
        KeyCode.Delete      => "Suppr",
        KeyCode.UpArrow     => "↑",
        KeyCode.DownArrow   => "↓",
        KeyCode.LeftArrow   => "←",
        KeyCode.RightArrow  => "→",
        KeyCode.Space       => "Espace",
        KeyCode.Escape      => "Echap",
        KeyCode.Tab         => "Tab",
        KeyCode.CapsLock    => "Verr. Maj",
        KeyCode.PageUp      => "Pg↑",
        KeyCode.PageDown    => "Pg↓",
        KeyCode.Home        => "Debut",
        KeyCode.End         => "Fin",

        // Clics souris
        KeyCode.Mouse0      => "Clic G",
        KeyCode.Mouse1      => "Clic D",
        KeyCode.Mouse2      => "Molette",

        _                   => kc.ToString().ToUpper()
    };

    // ================================================================
    // FILTRE TOUCHES VALIDES
    // ================================================================

    public static bool EstToucheValide(KeyCode kc)
    {
        if (kc == KeyCode.None) return false;
        if ((int)kc >= (int)KeyCode.JoystickButton0) return false;
        return true;
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    public ActionJeu Action => _action;

    /// <summary>Injecte la source de données (dataTemp de OptionsUI).</summary>
    public void SetDataSource(OptionsData data) => _dataSource = data;

    public void AnnulerRebind()
    {
        StopAllCoroutines();
        _overlay?.Cacher();
        MettreAJourAffichage();
    }
}
