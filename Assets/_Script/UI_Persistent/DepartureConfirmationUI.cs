// ============================================================
// DepartureConfirmationUI.cs — Bailiff & Co  V2
// Panel Oui/Non affiché quand le joueur interagit avec la porte
// conducteur. Ouvert par HUDSystem (toujours actif en mission)
// via UIManager.GetPanel<DepartureConfirmationUI>().
//
// IMPORTANT : ce panel démarre INACTIF (_autoAfficher = false).
// Il ne peut donc pas s'abonner lui-même à OnMissionEndRequested
// (OnEnable ne fire jamais sur un GO inactif). C'est HUDSystem
// qui l'ouvre et lui passe les valeurs via SetValeurs().
//
// SETUP UNITY :
//   panelType          → Blocking  (bloque input joueur pendant la confirmation)
//   _contexteVisibles  → Mission
//   _autoAfficher      → NON
//
//   Refs à assigner :
//     _texteValeur   → TextMeshProUGUI affichant la valeur coffre
//     _texteQuota    → TextMeshProUGUI affichant le quota cible
//     _texteStatut   → TextMeshProUGUI (optionnel)
//     _btnOui        → Button "Partir"
//     _btnNon        → Button "Annuler"
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DepartureConfirmationUI : UIPanel
{
    [Header("Textes informatifs")]
    [SerializeField] private TextMeshProUGUI _texteValeur;
    [SerializeField] private TextMeshProUGUI _texteQuota;
    [SerializeField] private TextMeshProUGUI _texteStatut;

    [Header("Boutons")]
    [SerializeField] private Button _btnOui;
    [SerializeField] private Button _btnNon;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();
        _btnOui?.onClick.AddListener(OnClickOui);
        _btnNon?.onClick.AddListener(OnClickNon);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // OnQuotaChanged pour rafraîchir l'affichage si les valeurs changent
        // pendant que le panel est ouvert (ex: objet qui sort du coffre)
        EventBus<OnQuotaChanged>.Subscribe(OnQuotaChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventBus<OnQuotaChanged>.Unsubscribe(OnQuotaChanged);
    }

    public override void ReAbonnerEventBus()
    {
        EventBus<OnQuotaChanged>.Unsubscribe(OnQuotaChanged);
        EventBus<OnQuotaChanged>.Subscribe(OnQuotaChanged);
    }

    // ================================================================
    // API PUBLIQUE — appelée par HUDSystem avant l'ouverture
    // ================================================================

    public void SetValeurs(float valeur, float cible)
    {
        UpdateDisplay(valeur, cible);
    }

    // ================================================================
    // BOUTONS
    // ================================================================

    private void OnClickOui()
    {
        Fermer();
        EventBus<OnDepartureConfirmed>.Raise(new OnDepartureConfirmed { Confirmed = true });
    }

    private void OnClickNon()
    {
        Fermer();
        EventBus<OnDepartureConfirmed>.Raise(new OnDepartureConfirmed { Confirmed = false });
    }

    // ================================================================
    // HANDLERS
    // ================================================================

    private void OnQuotaChanged(OnQuotaChanged e)
    {
        if (!EstOuvert) return;
        UpdateDisplay(e.TotalValue, e.TargetValue);
    }

    // ================================================================
    // AFFICHAGE
    // ================================================================

    private void UpdateDisplay(float valeur, float cible)
    {
        if (_texteValeur)
            _texteValeur.text = $"{valeur:N0} €";

        if (_texteQuota)
            _texteQuota.text = cible > 0f ? $"/ {cible:N0} €" : string.Empty;

        if (_texteStatut)
        {
            bool quotaAtteint = cible > 0f && valeur >= cible;
            _texteStatut.text = quotaAtteint
                ? "Quota atteint ✓"
                : $"Quota non atteint ({(cible > 0f ? valeur / cible * 100f : 0f):F0}%)";
        }
    }
}
