// ============================================================
// HUDSystem.cs — Bailiff & Co  V2
// Affichage UNIQUEMENT. S'abonne aux events, met à jour l'UI.
// Aucune logique de jeu. Toutes les refs UI sont ici.
//
// CHANGEMENTS V2 :
//   - Migré vers UI_Persistent (reste chargé en permanence)
//   - Sera activé/désactivé par UIManager selon le contexte
//   - Plus de FindObjectOfType — 100% event-driven
//   - Le label d'interaction est géré par LabelInteractionUI.cs
//
// SETUP UNITY :
//   Placer ce script sur le GameObject "HUDPanel" dans UI_Persistent.
//   UIManager s'occupe de l'activer (Mission) ou le désactiver (Hub/Menu).
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDSystem : MonoBehaviour
{
    [Header("Quota (mission uniquement)")]
    [SerializeField] private Slider          _barreQuota;
    [SerializeField] private TextMeshProUGUI _texteQuota;

    [Header("Paranoïa (mission uniquement)")]
    [SerializeField] private Image           _iconeParanoia;
    [SerializeField] private Sprite[]        _spritesParanoiaPaliers; // 6 sprites (paliers 0–5)
    [SerializeField] private TextMeshProUGUI _texteParanoia;

    [Header("Urgence (mission uniquement)")]
    [SerializeField] private GameObject      _panneauUrgence;
    [SerializeField] private TextMeshProUGUI _texteTimer;
    [SerializeField] private Image           _cadreRouge;

    [Header("Notifications (Hub + Mission)")]
    [SerializeField] private TextMeshProUGUI _notificationChargement;

    private float _timerUrgence  = 0f;
    private bool  _urgenceActive = false;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        if (_panneauUrgence) _panneauUrgence.SetActive(false);
        if (_notificationChargement) _notificationChargement.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus<OnQuotaChanged>.Subscribe(OnQuotaChanged);
        EventBus<OnParanoiaChanged>.Subscribe(OnParanoiaChanged);
        EventBus<OnObjetCharge>.Subscribe(OnObjetCharge);
        EventBus<OnTimerUrgenceDéclenche>.Subscribe(OnTimerUrgence);
        EventBus<OnPerroquetParle>.Subscribe(OnPerroquetParle);
    }

    private void OnDisable()
    {
        EventBus<OnQuotaChanged>.Unsubscribe(OnQuotaChanged);
        EventBus<OnParanoiaChanged>.Unsubscribe(OnParanoiaChanged);
        EventBus<OnObjetCharge>.Unsubscribe(OnObjetCharge);
        EventBus<OnTimerUrgenceDéclenche>.Unsubscribe(OnTimerUrgence);
        EventBus<OnPerroquetParle>.Unsubscribe(OnPerroquetParle);
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

    private void OnQuotaChanged(OnQuotaChanged e)
    {
        if (_barreQuota)
            _barreQuota.value = e.Percentage;  // ← nom anglais V2
        if (_texteQuota)
            _texteQuota.text = $"{e.TotalValue:N0} € / {e.TargetValue:N0} €";  // ← noms anglais V2
    }

    private void OnParanoiaChanged(OnParanoiaChanged e)
    {
        if (_iconeParanoia && _spritesParanoiaPaliers != null
            && e.NewTier < _spritesParanoiaPaliers.Length)  // ← nom anglais V2
            _iconeParanoia.sprite = _spritesParanoiaPaliers[e.NewTier];

        if (_texteParanoia)
            _texteParanoia.text = ParanoiaSystem.GetTierName(e.NewTier);  // ← méthode existe ligne 96 ParanoiaSystem.cs
    }

    private void OnObjetCharge(OnObjetCharge e)
    {
        if (_notificationChargement == null) return;

        _notificationChargement.text = $"+{e.Valeur:N0} €";
        CancelInvoke(nameof(CacherNotification));
        _notificationChargement.gameObject.SetActive(true);
        Invoke(nameof(CacherNotification), 2f);
    }

    private void OnTimerUrgence(OnTimerUrgenceDéclenche e)
    {
        _urgenceActive = true;
        _timerUrgence  = e.DureeSecondes;
        if (_panneauUrgence) _panneauUrgence.SetActive(true);
    }

    private void OnPerroquetParle(OnPerroquetParle e)
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
    // API PUBLIQUE — notification manuelle (ex: argent gagné dans le Hub)
    // UIManager ou HubManager peuvent appeler cette méthode directement
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