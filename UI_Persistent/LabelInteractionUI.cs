// ============================================================
// LabelInteractionUI.cs — Bailiff & Co  V2
// Affiche le label d'interaction contextuel.
// Le panel reste TOUJOURS actif — on vide le texte quand
// il n'y a rien à afficher.
// La touche affichée est toujours lue depuis OptionsManager
// pour refléter les rebinds du joueur.
//
// CHANGEMENTS V2 :
//   - Migré vers UI_Persistent (reste chargé en permanence)
//   - SUPPRESSION du FindObjectOfType<PlayerInteractor>()
//   - PlayerInteractor broadcast maintenant son label via event
//   - Sera activé/désactivé par UIManager selon le contexte
//
// SETUP UNITY :
//   Placer ce script sur le GameObject "LabelInteractionPanel" 
//   dans UI_Persistent.
//   UIManager s'occupe de l'activer (Hub/Mission) ou le désactiver (Menu).
// ============================================================
using TMPro;
using UnityEngine;

public class LabelInteractionUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private TextMeshProUGUI _txtTouche;
    [SerializeField] private TextMeshProUGUI _txtAction;

    private string _labelCourant = string.Empty;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        EventBus<OnInteractionLabelChanged>.Subscribe(OnLabelChanged);
    }

    private void OnDisable()
    {
        EventBus<OnInteractionLabelChanged>.Unsubscribe(OnLabelChanged);
    }

    private void Start()
    {
        // S'assurer que les textes sont vides au démarrage
        if (_txtTouche != null) _txtTouche.text = "";
        if (_txtAction != null) _txtAction.text = "";
    }

    // ================================================================
    // HANDLER EVENT
    // ================================================================

    private void OnLabelChanged(OnInteractionLabelChanged e)
    {
        _labelCourant = e.Label;

        if (string.IsNullOrEmpty(_labelCourant))
        {
            if (_txtTouche != null) _txtTouche.text = "";
            if (_txtAction != null) _txtAction.text = "";
            return;
        }

        ParseEtAfficher(_labelCourant);
    }

    // ================================================================
    // PARSING & AFFICHAGE
    // ================================================================

    private void ParseEtAfficher(string label)
    {
        // Touche réelle depuis OptionsManager (tient compte des rebinds)
        string toucheReelle = GetToucheInteragir();

        int debut = label.IndexOf('[');
        int fin   = label.IndexOf(']');

        if (debut >= 0 && fin > debut)
        {
            // Le label contient [X] — on remplace X par la vraie touche rebindée
            string action = label.Substring(fin + 1).Trim();

            if (action.StartsWith("—") || action.StartsWith("-"))
                action = action.Substring(1).Trim();

            if (_txtTouche != null) _txtTouche.text = toucheReelle;
            if (_txtAction != null) _txtAction.text = action;
        }
        else
        {
            // Pas de [X] dans le label — affiche la vraie touche quand même
            if (_txtTouche != null) _txtTouche.text = toucheReelle;
            if (_txtAction != null) _txtAction.text = label;
        }
    }

    private string GetToucheInteragir()
    {
        if (OptionsManager.Instance == null)
            return "E"; // fallback si OptionsManager absent

        KeyCode kc = OptionsManager.Instance.GetTouche(ActionJeu.Interagir);
        return KeyRebindUI.FormatKeyCode(kc);
    }
}