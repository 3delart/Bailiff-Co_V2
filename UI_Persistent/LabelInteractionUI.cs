// ============================================================
// LabelInteractionUI.cs — Bailiff & Co  V2
// Affiche le label d'interaction contextuel.
// panelType = GameUI dans l'Inspector.
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
//   - OnEnable/OnDisable : override + base. pour combiner
//     RegisterPanel/UnregisterPanel ET abonnement EventBus
//
// SETUP UNITY :
//   Placer ce script sur le GameObject "LabelInteractionPanel"
//   dans UI_Persistent.
//   UIManager s'occupe de l'activer (Hub/Mission) ou le désactiver (Menu).
// ============================================================
using TMPro;
using UnityEngine;

public class LabelInteractionUI : UIPanel
{
    [Header("Références")]
    [SerializeField] private TextMeshProUGUI _txtTouche;
    [SerializeField] private TextMeshProUGUI _txtAction;

    private string _labelCourant = string.Empty;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        if (_txtTouche != null) _txtTouche.text = "";
        if (_txtAction != null) _txtAction.text = "";
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // RegisterPanel → UIManager gère input + curseur
        EventBus<OnInteractionLabelChanged>.Subscribe(OnLabelChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // UnregisterPanel → UIManager gère input + curseur
        EventBus<OnInteractionLabelChanged>.Unsubscribe(OnLabelChanged);
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
        string toucheReelle = GetToucheInteragir();

        int debut = label.IndexOf('[');
        int fin   = label.IndexOf(']');

        if (debut >= 0 && fin > debut)
        {
            string action = label.Substring(fin + 1).Trim();
            if (action.StartsWith("—") || action.StartsWith("-"))
                action = action.Substring(1).Trim();

            if (_txtTouche != null) _txtTouche.text = toucheReelle;
            if (_txtAction != null) _txtAction.text = action;
        }
        else
        {
            if (_txtTouche != null) _txtTouche.text = toucheReelle;
            if (_txtAction != null) _txtAction.text = label;
        }
    }

    private string GetToucheInteragir()
    {
        if (OptionsManager.Instance == null) return "E";
        KeyCode kc = OptionsManager.Instance.GetTouche(ActionJeu.Interagir);
        return KeyRebindUI.FormatKeyCode(kc);
    }
}
