// ============================================================
// PhoneScreenUI.cs — Bailiff & Co
// Écran de l'app "Huissier Scan" sur le modèle du téléphone (Canvas world-space).
// 3 états : Idle (repos) → Scanning (barre %) → Result (objet scanné).
//
// À mettre sur le Canvas world-space, enfant du prefab Téléphone.
// Apparaît/disparaît avec le tél (instancié/détruit par PlayerToolUser).
//
// Écoute :
//   OnToolChannelProgress → barre de scan (Active true = en cours)
//   OnObjectScanned       → affiche le résultat
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneScreenUI : MonoBehaviour
{
    [Header("États (roots à activer/désactiver)")]
    [SerializeField] private GameObject _idleRoot;
    [SerializeField] private GameObject _scanningRoot;
    [SerializeField] private GameObject _resultRoot;

    [Header("Scanning")]
    [Tooltip("Image type Filled (Horizontal) — fillAmount = progression.")]
    [SerializeField] private Image     _scanBarFill;
    [SerializeField] private TMP_Text  _scanPercent;

    [Header("Result")]
    [SerializeField] private TMP_Text  _resName;
    [SerializeField] private TMP_Text  _resValue;     // prix FINAL (déprécié)
    [SerializeField] private TMP_Text  _resAnnee;
    [SerializeField] private TMP_Text  _resEtat;      // "intact" / "23% endommagé"
    [SerializeField] private Image     _resPhoto;
    [SerializeField] private GameObject _tagFragile;

    private bool _resultPinned;   // garde le résultat affiché après un scan réussi

    private void OnEnable()
    {
        EventBus<OnToolChannelProgress>.Subscribe(OnChannel);
        EventBus<OnObjectScanned>.Subscribe(OnScanned);
        ShowIdle();
    }

    private void OnDisable()
    {
        EventBus<OnToolChannelProgress>.Unsubscribe(OnChannel);
        EventBus<OnObjectScanned>.Unsubscribe(OnScanned);
    }

    // ── Events ───────────────────────────────────────────────

    private void OnChannel(OnToolChannelProgress e)
    {
        if (e.Active)
        {
            _resultPinned = false;
            ShowScanning();
            if (_scanBarFill != null) _scanBarFill.fillAmount = e.Progress01;
            if (_scanPercent != null) _scanPercent.text = Mathf.RoundToInt(e.Progress01 * 100f) + "%";
        }
        else if (!_resultPinned)
        {
            ShowIdle();   // canalisation annulée sans succès → retour repos
        }
    }

    private void OnScanned(OnObjectScanned e)
    {
        _resultPinned = true;
        ShowResult();

        if (_resName != null)
            _resName.text = string.IsNullOrEmpty(e.Annee) ? e.Nom : $"{e.Nom} · {e.Annee}";
        if (_resValue != null)  _resValue.text = e.Valeur.ToString("N0") + " €";
        if (_resAnnee != null)  _resAnnee.text = e.Annee;
        if (_resEtat != null)
            _resEtat.text = e.DamagePercent < 0.5f
                ? "État : intact"
                : $"État : {Mathf.RoundToInt(e.DamagePercent)}% endommagé";
        if (_resPhoto != null)  { _resPhoto.sprite = e.Photo; _resPhoto.enabled = e.Photo != null; }
        if (_tagFragile != null) _tagFragile.SetActive(e.Fragile);
    }

    // ── États ────────────────────────────────────────────────

    private void ShowIdle()     => SetState(idle: true,  scan: false, result: false);
    private void ShowScanning() => SetState(idle: false, scan: true,  result: false);
    private void ShowResult()   => SetState(idle: false, scan: false, result: true);

    private void SetState(bool idle, bool scan, bool result)
    {
        if (_idleRoot     != null) _idleRoot.SetActive(idle);
        if (_scanningRoot != null) _scanningRoot.SetActive(scan);
        if (_resultRoot   != null) _resultRoot.SetActive(result);
    }
}
