// ============================================================
// HUDSystem.cs — Bailiff & Co  V2
// Affichage UNIQUEMENT. S'abonne aux events, met à jour l'UI.
// Aucune logique de jeu. Toutes les refs UI sont ici.
// panelType = GameUI dans l'Inspector.
//
// CHANGEMENTS V2 :
//   - Migré vers UI_Persistent (reste chargé en permanence)
//   - Sera activé/désactivé par UIManager selon le contexte
//   - Plus de FindObjectOfType — 100% event-driven
//   - Le label d'interaction est géré par LabelInteractionUI.cs
//   - OnEnable/OnDisable : override + base. pour combiner
//     RegisterPanel/UnregisterPanel ET abonnements EventBus
//
// SETUP UNITY :
//   Placer ce script sur le GameObject "HUDPanel" dans UI_Persistent.
//   UIManager s'occupe de l'activer (Mission) ou le désactiver (Hub/Menu).
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDSystem : UIPanel
{
    [Header("Quota (mission uniquement)")]
    [SerializeField] private Slider          _barreQuota;
    [SerializeField] private TextMeshProUGUI _texteQuota;

    [Header("Paranoïa (mission uniquement)")]
    [SerializeField] private Image           _iconeParanoia;
    [SerializeField] private Sprite[]        _spritesParanoiaPaliers;
    [SerializeField] private TextMeshProUGUI _texteParanoia;

    [Header("Urgence (mission uniquement)")]
    [SerializeField] private GameObject      _panneauUrgence;
    [SerializeField] private TextMeshProUGUI _texteTimer;
    [SerializeField] private Image           _cadreRouge;

    [Header("Notifications (Hub + Mission)")]
    [SerializeField] private TextMeshProUGUI _notificationChargement;

    private float _timerUrgence  = 0f;
    private bool  _urgenceActive = false;

    // Dernières valeurs quota — passées au panel de confirmation à l'ouverture
    private float _lastTotalValue  = 0f;
    private float _lastTargetValue = 0f;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        if (_panneauUrgence)         _panneauUrgence.SetActive(false);
        if (_notificationChargement) _notificationChargement.gameObject.SetActive(false);
        ResetQuotaDisplay();
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // RegisterPanel → UIManager gère input + curseur

        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
        EventBus<OnMissionEndRequested>.Subscribe(OnMissionEndRequested);
        EventBus<OnQuotaChanged>.Subscribe(OnQuotaChanged);
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnObjectLoaded>.Subscribe(OnObjectLoaded);
        EventBus<OnUrgencyTimerStarted>.Subscribe(OnUrgencyTimerStarted);
        EventBus<OnParrotSpoke>.Subscribe(OnParrotSpoke);
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // UnregisterPanel → UIManager gère input + curseur

        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
        EventBus<OnMissionEndRequested>.Unsubscribe(OnMissionEndRequested);
        EventBus<OnQuotaChanged>.Unsubscribe(OnQuotaChanged);
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnObjectLoaded>.Unsubscribe(OnObjectLoaded);
        EventBus<OnUrgencyTimerStarted>.Unsubscribe(OnUrgencyTimerStarted);
        EventBus<OnParrotSpoke>.Unsubscribe(OnParrotSpoke);
    }

    public override void ReAbonnerEventBus()
    {
        EventBus<OnMissionStarted>.Unsubscribe(OnMissionStarted);
        EventBus<OnMissionStarted>.Subscribe(OnMissionStarted);
        EventBus<OnMissionEndRequested>.Unsubscribe(OnMissionEndRequested);
        EventBus<OnMissionEndRequested>.Subscribe(OnMissionEndRequested);
        EventBus<OnQuotaChanged>.Unsubscribe(OnQuotaChanged);
        EventBus<OnQuotaChanged>.Subscribe(OnQuotaChanged);
    }

    private void Update()
    {
        if (!_urgenceActive) return;

        _timerUrgence -= Time.deltaTime;
        if (_texteTimer) _texteTimer.text = FormatTimer(_timerUrgence);

        if (_timerUrgence <= 0)
        {
            _urgenceActive = false;
            if (_panneauUrgence) _panneauUrgence.SetActive(false);
        }
    }

    // ================================================================
    // HANDLERS EVENTS
    // ================================================================

    private void OnMissionStarted(OnMissionStarted e)
    {
        _lastTotalValue  = 0f;
        _lastTargetValue = 0f;
        ResetQuotaDisplay();
    }

    private void OnMissionEndRequested(OnMissionEndRequested e)
    {
        // Le HUDSystem ouvre le panel de confirmation car il est toujours actif
        // pendant la mission. DepartureConfirmationUI ne peut pas s'abonner tout
        // seul (il démarre inactif et OnEnable ne feu jamais).
        var panel = UIManager.Instance?.GetPanel<DepartureConfirmationUI>();
        if (panel != null)
        {
            panel.Ouvrir();
            panel.SetValeurs(_lastTotalValue, _lastTargetValue);
        }
    }

    private void OnQuotaChanged(OnQuotaChanged e)
    {
        _lastTotalValue  = e.TotalValue;
        _lastTargetValue = e.TargetValue;
        if (_barreQuota) _barreQuota.value = e.Percentage;
        if (_texteQuota) _texteQuota.text  = $"{e.TotalValue:N0} € / {e.TargetValue:N0} €";
    }

    private void OnParanoiaChanged(OnParanoiaChanged e)
    {
        if (_iconeParanoia && _spritesParanoiaPaliers != null
            && e.NewTier < _spritesParanoiaPaliers.Length)
            _iconeParanoia.sprite = _spritesParanoiaPaliers[e.NewTier];

        if (_texteParanoia)
            _texteParanoia.text = ParanoiaSystem.GetTierName(e.NewTier);
    }

    private void OnObjectLoaded(OnObjectLoaded e)
    {
        if (_notificationChargement == null) return;
        // ✅ Affiche CurrentPrice (valeur réelle après dégâts si cassé)
        _notificationChargement.text = $"+{e.CurrentPrice:N0} €";
        CancelInvoke(nameof(CacherNotification));
        _notificationChargement.gameObject.SetActive(true);
        Invoke(nameof(CacherNotification), 2f);
    }

    private void OnUrgencyTimerStarted(OnUrgencyTimerStarted e)
    {
        _urgenceActive = true;
        _timerUrgence  = e.DurationSeconds;
        if (_panneauUrgence) _panneauUrgence.SetActive(true);
    }

    private void OnParrotSpoke(OnParrotSpoke e)
    {
        if (_notificationChargement == null) return;
        _notificationChargement.text = $"🦜 \"{e.Phrase}\"";
        _notificationChargement.gameObject.SetActive(true);
        CancelInvoke(nameof(CacherNotification));
        Invoke(nameof(CacherNotification), 4f);
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void ResetQuotaDisplay()
    {
        if (_barreQuota) _barreQuota.value = 0f;
        if (_texteQuota) _texteQuota.text  = "0 € / — €";
    }

    private void CacherNotification()
    {
        if (_notificationChargement)
            _notificationChargement.gameObject.SetActive(false);
    }

    private string FormatTimer(float sec)
    {
        int m = Mathf.FloorToInt(sec / 60);
        int s = Mathf.FloorToInt(sec % 60);
        return $"{m}:{s:D2}";
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    public void AfficherNotification(string texte, float duree = 2f)
    {
        if (_notificationChargement == null) return;
        _notificationChargement.text = texte;
        _notificationChargement.gameObject.SetActive(true);
        CancelInvoke(nameof(CacherNotification));
        Invoke(nameof(CacherNotification), duree);
    }
}