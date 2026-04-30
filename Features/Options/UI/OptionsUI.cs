// ============================================================
// OptionsUI.cs — Bailiff & Co  [v2]
// Panneau Options complet : Vidéo / Audio / Souris / Touches.
// Peut s'ouvrir depuis UI_Persistent (PauseMenu, HubUI, MenuUI).
//
// CHANGEMENTS v1 → v2 :
//   - Déplacé dans Features/Options/UI/ (plus de copie par scène).
//   - OnReset() délègue à OptionsRepository.Reset() au lieu de
//     _dataTemp.Reset() (méthode retirée de OptionsData).
//   - OnAppliquer() et OnAnnuler() inchangés.
//
// SETUP UNITY — Canvas (Screen Space Overlay, Sort Order 50) :
//
// PanneauOptions (ce script, désactivé par défaut)
// ├── Fond (Image noire alpha ~0.85, stretch fullscreen)
// └── Carte (Image centrée ~700x520px)
//     ├── Titre (TMP "OPTIONS")
//     │
//     ├── BarreOnglets (HorizontalLayoutGroup)
//     │   ├── BtnVideo    → _btnVideo
//     │   ├── BtnAudio    → _btnAudio
//     │   ├── BtnSouris   → _btnSouris
//     │   └── BtnTouches  → _btnTouches
//     │
//     ├── PanneauVideo    → _panneauVideo
//     │   ├── LigneResolution  (Label + Dropdown → _dropResolution)
//     │   ├── LigneModeEcran   (Label + Dropdown → _dropModeEcran)
//     │   ├── LigneQualite     (Label + Dropdown → _dropQualite)
//     │   ├── LigneVSync       (Label + Toggle   → _toggleVSync)
//     │   └── LigneFPS         (Label + Dropdown → _dropFPS)
//     │
//     ├── PanneauAudio    → _panneauAudio
//     │   ├── LigneMaster   (Label + Slider → _sliderMaster   + TMP valeur)
//     │   ├── LigneMusique  (Label + Slider → _sliderMusique  + TMP valeur)
//     │   ├── LigneSFX      (Label + Slider → _sliderSFX      + TMP valeur)
//     │   └── LigneAmbiance (Label + Slider → _sliderAmbiance + TMP valeur)
//     │
//     ├── PanneauSouris   → _panneauSouris
//     │   ├── LigneSensi  (Label + Slider → _sliderSensi + TMP valeur)
//     │   └── LigneInvY   (Label + Toggle → _toggleInvY)
//     │
//     ├── PanneauTouches  → _panneauTouches
//     │   └── ScrollView
//     │       └── Content (VerticalLayoutGroup)
//     │           └── [KeyRebindRow × N] → auto-trouvés
//     │
//     └── BasDePage (HorizontalLayoutGroup)
//         ├── BtnReset      → _btnReset
//         ├── BtnAnnuler    → _btnAnnuler
//         └── BtnAppliquer  → _btnAppliquer
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    // ================================================================
    // ONGLETS
    // ================================================================

    [Header("Boutons onglets")]
    [SerializeField] private Button _btnVideo;
    [SerializeField] private Button _btnAudio;
    [SerializeField] private Button _btnSouris;
    [SerializeField] private Button _btnTouches;

    [Header("Panneaux")]
    [SerializeField] private GameObject _panneauVideo;
    [SerializeField] private GameObject _panneauAudio;
    [SerializeField] private GameObject _panneauSouris;
    [SerializeField] private GameObject _panneauTouches;

    // ================================================================
    // VIDÉO
    // ================================================================

    [Header("Vidéo")]
    [SerializeField] private TMP_Dropdown _dropResolution;
    [SerializeField] private TMP_Dropdown _dropModeEcran;
    [SerializeField] private TMP_Dropdown _dropQualite;
    [SerializeField] private Toggle       _toggleVSync;
    [SerializeField] private TMP_Dropdown _dropFPS;

    // ================================================================
    // AUDIO
    // ================================================================

    [Header("Audio")]
    [SerializeField] private Slider          _sliderMaster;
    [SerializeField] private TextMeshProUGUI _valeurMaster;
    [SerializeField] private Slider          _sliderMusique;
    [SerializeField] private TextMeshProUGUI _valeurMusique;
    [SerializeField] private Slider          _sliderSFX;
    [SerializeField] private TextMeshProUGUI _valeurSFX;
    [SerializeField] private Slider          _sliderAmbiance;
    [SerializeField] private TextMeshProUGUI _valeurAmbiance;

    // ================================================================
    // SOURIS
    // ================================================================

    [Header("Souris")]
    [SerializeField] private Slider          _sliderSensi;
    [SerializeField] private TextMeshProUGUI _valeurSensi;
    [SerializeField] private Toggle          _toggleInvY;

    // ================================================================
    // TOUCHES
    // ================================================================

    [Header("Touches")]
    [SerializeField] private Transform _conteneurTouches; // parent des KeyRebindRow

    // ================================================================
    // BAS DE PAGE
    // ================================================================

    [Header("Bas de page")]
    [SerializeField] private Button _btnReset;
    [SerializeField] private Button _btnAnnuler;
    [SerializeField] private Button _btnAppliquer;

    // ================================================================
    // STYLE
    // ================================================================

    [Header("Style onglets")]
    [SerializeField] private Color _couleurActif   = new Color(0.94f, 0.91f, 0.48f);
    [SerializeField] private Color _couleurInactif = new Color(0.15f, 0.15f, 0.15f);

    // ================================================================
    // ÉTAT INTERNE
    // ================================================================

    private OptionsData          _dataTemp;              // copie de travail
    private KeyRebindUI[]        _rebindRows;
    private bool                 _initialise = false;
    private List<Resolution>     _resolutionsFiltrees = new();

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void OnEnable()
    {
        // Copie de travail — on n'applique que sur "Appliquer"
        if (OptionsManager.Instance != null)
            _dataTemp = JsonUtility.FromJson<OptionsData>(
                JsonUtility.ToJson(OptionsManager.Instance.Data));
        else
            _dataTemp = new OptionsData();

        if (!_initialise)
        {
            Initialiser();
            _initialise = true;
        }

        InjecterDataSource();
        ChargerValeursUI();
        OuvrirOnglet(0);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Initialiser()
    {
        // Onglets
        _btnVideo?.onClick.AddListener(()   => OuvrirOnglet(0));
        _btnAudio?.onClick.AddListener(()   => OuvrirOnglet(1));
        _btnSouris?.onClick.AddListener(()  => OuvrirOnglet(2));
        _btnTouches?.onClick.AddListener(() => OuvrirOnglet(3));

        // Bas de page
        _btnReset?.onClick.AddListener(OnReset);
        _btnAnnuler?.onClick.AddListener(OnAnnuler);
        _btnAppliquer?.onClick.AddListener(OnAppliquer);

        // Résolutions — filtre les doublons et trie
        BuildResolutionList();

        // Mode écran
        if (_dropModeEcran != null)
        {
            _dropModeEcran.ClearOptions();
            _dropModeEcran.AddOptions(new List<string>
                { "Plein écran", "Fenêtré", "Borderless" });
        }

        // Qualité
        if (_dropQualite != null)
        {
            _dropQualite.ClearOptions();
            _dropQualite.AddOptions(new List<string>
                { "Basse", "Moyenne", "Haute", "Ultra" });
        }

        // FPS
        if (_dropFPS != null)
        {
            _dropFPS.ClearOptions();
            _dropFPS.AddOptions(new List<string>
                { "Illimité", "30", "60", "120", "144", "240" });
        }

        // Sliders audio
        ConfigurerSlider(_sliderMaster,   0f, 1f);
        ConfigurerSlider(_sliderMusique,  0f, 1f);
        ConfigurerSlider(_sliderSFX,      0f, 1f);
        ConfigurerSlider(_sliderAmbiance, 0f, 1f);

        // Slider sensibilité
        ConfigurerSlider(_sliderSensi, 0.1f, 10f);

        // Listeners sliders audio
        _sliderMaster?.onValueChanged.AddListener(v =>
            { _dataTemp.VolumeMaster = v;   MettreAJourValeurLabel(_valeurMaster,   v, pct: true); });
        _sliderMusique?.onValueChanged.AddListener(v =>
            { _dataTemp.VolumeMusique = v;  MettreAJourValeurLabel(_valeurMusique,  v, pct: true); });
        _sliderSFX?.onValueChanged.AddListener(v =>
            { _dataTemp.VolumeSFX = v;      MettreAJourValeurLabel(_valeurSFX,      v, pct: true); });
        _sliderAmbiance?.onValueChanged.AddListener(v =>
            { _dataTemp.VolumeAmbiance = v; MettreAJourValeurLabel(_valeurAmbiance, v, pct: true); });

        // Listener sensibilité
        _sliderSensi?.onValueChanged.AddListener(v =>
            { _dataTemp.SensibiliteSouris = v; MettreAJourValeurLabel(_valeurSensi, v, pct: false); });

        // Toggle InverserY
        _toggleInvY?.onValueChanged.AddListener(v => _dataTemp.InverserY = v);

        // Toggle VSync
        _toggleVSync?.onValueChanged.AddListener(v => _dataTemp.VSync = v);

        // Dropdowns
        _dropResolution?.onValueChanged.AddListener(v => _dataTemp.IndexResolution = v);
        _dropModeEcran?.onValueChanged.AddListener(v  => _dataTemp.ModeEcran = v);
        _dropQualite?.onValueChanged.AddListener(v    => _dataTemp.QualiteGlobale = v);
        _dropFPS?.onValueChanged.AddListener(v        => _dataTemp.LimiteFPS = v);

        // Trouve tous les KeyRebindUI dans le conteneur
        if (_conteneurTouches != null)
            _rebindRows = _conteneurTouches.GetComponentsInChildren<KeyRebindUI>(includeInactive: true);

        // Injecte la dataTemp dans chaque row pour que les rebinds
        // écrivent dans la copie de travail, pas dans OptionsManager.Data.
        InjecterDataSource();
    }

    // ================================================================
    // RÉSOLUTIONS — filtre les doublons (même largeur×hauteur)
    // ================================================================

    private void BuildResolutionList()
    {
        if (_dropResolution == null) return;

        _resolutionsFiltrees.Clear();
        var vus = new HashSet<string>();

        Resolution[] toutes = Screen.resolutions;
        for (int i = toutes.Length - 1; i >= 0; i--)
        {
            string cle = $"{toutes[i].width}x{toutes[i].height}";
            if (!vus.Contains(cle))
            {
                vus.Add(cle);
                _resolutionsFiltrees.Insert(0, toutes[i]);
            }
        }

        _dropResolution.ClearOptions();
        var labels = new List<string>();
        foreach (var r in _resolutionsFiltrees)
            labels.Add($"{r.width} × {r.height}");
        _dropResolution.AddOptions(labels);
    }

    // ================================================================
    // CHARGER VALEURS DANS L'UI
    // ================================================================

    private void ChargerValeursUI()
    {
        if (_dataTemp == null) return;

        // Vidéo
        if (_dropResolution != null)
            _dropResolution.SetValueWithoutNotify(
                Mathf.Clamp(_dataTemp.IndexResolution, 0, _resolutionsFiltrees.Count - 1));
        _dropModeEcran?.SetValueWithoutNotify(_dataTemp.ModeEcran);
        _dropQualite?.SetValueWithoutNotify(_dataTemp.QualiteGlobale);
        _toggleVSync?.SetIsOnWithoutNotify(_dataTemp.VSync);
        _dropFPS?.SetValueWithoutNotify(_dataTemp.LimiteFPS);

        // Audio
        _sliderMaster?.SetValueWithoutNotify(_dataTemp.VolumeMaster);
        _sliderMusique?.SetValueWithoutNotify(_dataTemp.VolumeMusique);
        _sliderSFX?.SetValueWithoutNotify(_dataTemp.VolumeSFX);
        _sliderAmbiance?.SetValueWithoutNotify(_dataTemp.VolumeAmbiance);
        MettreAJourValeurLabel(_valeurMaster,   _dataTemp.VolumeMaster,   pct: true);
        MettreAJourValeurLabel(_valeurMusique,  _dataTemp.VolumeMusique,  pct: true);
        MettreAJourValeurLabel(_valeurSFX,      _dataTemp.VolumeSFX,      pct: true);
        MettreAJourValeurLabel(_valeurAmbiance, _dataTemp.VolumeAmbiance, pct: true);

        // Souris
        _sliderSensi?.SetValueWithoutNotify(_dataTemp.SensibiliteSouris);
        MettreAJourValeurLabel(_valeurSensi, _dataTemp.SensibiliteSouris, pct: false);
        _toggleInvY?.SetIsOnWithoutNotify(_dataTemp.InverserY);
    }

    // ================================================================
    // ONGLETS
    // ================================================================

    private void OuvrirOnglet(int index)
    {
        _panneauVideo?.SetActive(index == 0);
        _panneauAudio?.SetActive(index == 1);
        _panneauSouris?.SetActive(index == 2);
        _panneauTouches?.SetActive(index == 3);

        SetCouleurOnglet(_btnVideo,   index == 0);
        SetCouleurOnglet(_btnAudio,   index == 1);
        SetCouleurOnglet(_btnSouris,  index == 2);
        SetCouleurOnglet(_btnTouches, index == 3);
    }

    private void SetCouleurOnglet(Button btn, bool actif)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = actif ? _couleurActif : _couleurInactif;
    }

    // ================================================================
    // TOUCHES
    // ================================================================

    public void RafraichirToutesTouches()
    {
        if (_rebindRows == null) return;
        foreach (var row in _rebindRows)
            row.MettreAJourAffichage();
    }

    private void InjecterDataSource()
    {
        if (_rebindRows == null) return;
        foreach (var row in _rebindRows)
            row.SetDataSource(_dataTemp);
    }

    // ================================================================
    // BAS DE PAGE
    // ================================================================

    private void OnAppliquer()
    {
        // Copie la dataTemp dans le manager et sauvegarde.
        if (OptionsManager.Instance != null)
        {
            JsonUtility.FromJsonOverwrite(
                JsonUtility.ToJson(_dataTemp),
                OptionsManager.Instance.Data);
            OptionsManager.Instance.Sauvegarder();
        }
        gameObject.SetActive(false);
    }

    private void OnAnnuler()
    {
        // Annule les rebinds en cours
        if (_rebindRows != null)
            foreach (var row in _rebindRows)
                row.AnnulerRebind();

        gameObject.SetActive(false);
    }

    private void OnReset()
    {
        // v2 : délègue le reset à OptionsRepository au lieu de _dataTemp.Reset()
        OptionsRepository.Reset(_dataTemp);
        ChargerValeursUI();
        RafraichirToutesTouches();
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void ConfigurerSlider(Slider slider, float min, float max)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
    }

    private void MettreAJourValeurLabel(TextMeshProUGUI label, float valeur, bool pct)
    {
        if (label == null) return;
        label.text = pct
            ? $"{Mathf.RoundToInt(valeur * 100)}%"
            : $"{valeur:F1}";
    }
}
