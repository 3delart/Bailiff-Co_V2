// ============================================================
// OptionsManager.cs — Bailiff & Co  [v2]
// Singleton persistant. Charge, applique et sauvegarde les options.
// Vit dans UI_Persistent (toujours chargé).
//
// CHANGEMENTS v1 → v2 :
//   - Utilise OptionsRepository.Charger() / Sauvegarder() / Reset()
//     au lieu d'appeler directement OptionsData.Charger() (retiré).
//   - Déplacé de Bootstrap/ vers Features/Options/ (UI_Persistent).
//
// SETUP UNITY :
//   Ajouter ce script sur le GameObject "OptionsManager" dans
//   la scène UI_Persistent. Il persistera via DontDestroyOnLoad
//   grâce au chargement Additive de cette scène.
//
// MIXERS AUDIO :
//   Créer un AudioMixer "MainMixer" dans Project :
//   ├── Master (groupe racine)
//   │   ├── Musique
//   │   ├── SFX
//   │   └── Ambiance
//   Exposer les paramètres volume :
//     "VolumeMaster", "VolumeMusique", "VolumeSFX", "VolumeAmbiance"
//   (clic droit sur le fader → Expose to script)
// ============================================================
using UnityEngine;
using UnityEngine.Audio;

public class OptionsManager : MonoBehaviour
{
    // ================================================================
    // SINGLETON
    // ================================================================

    public static OptionsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        // UI_Persistent est chargée en Additive et ne se décharge jamais :
        // pas besoin de DontDestroyOnLoad supplémentaire.

        // Utilise OptionsRepository pour charger (v2)
        _data = OptionsRepository.Charger();

        // On n'applique QUE l'audio en Awake.
        // La vidéo est différée à Start : _resolutions n'est pas encore
        // peuplé, et SetQualityLevel trop tôt peut forcer Unity à recharger
        // les assets (matériaux/textures), perdant les données ScriptableObject.
        AppliquerAudio();
    }

    // ================================================================
    // RÉFÉRENCES
    // ================================================================

    [Header("Audio Mixer (optionnel — fonctionne sans)")]
    [SerializeField] private AudioMixer _mixer;

    // ================================================================
    // DONNÉES
    // ================================================================

    private OptionsData _data;
    public OptionsData Data => _data;

    // Résolutions disponibles (calculées au démarrage)
    private Resolution[] _resolutions;
    public Resolution[] Resolutions => _resolutions;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        // Calcule les résolutions disponibles une seule fois
        _resolutions = Screen.resolutions;

        // Si première fois, prend la résolution native
        if (_data.IndexResolution < 0 || _data.IndexResolution >= _resolutions.Length)
        {
            _data.IndexResolution = _resolutions.Length - 1;
        }

        // Maintenant que _resolutions est prêt, applique la vidéo
        AppliquerVideo();
    }

    // ================================================================
    // APPLIQUER TOUT
    // ================================================================

    public void AppliquerTout()
    {
        AppliquerVideo();
        AppliquerAudio();
        // Les touches sont lues directement depuis _data par les scripts input
    }

    // ================================================================
    // VIDÉO
    // ================================================================

    public void AppliquerVideo()
    {
        // Qualité
        QualitySettings.SetQualityLevel(_data.QualiteGlobale, applyExpensiveChanges: true);

        // VSync
        QualitySettings.vSyncCount = _data.VSync ? 1 : 0;

        // Limite FPS
        int[] fps = { 0, 30, 60, 120, 144, 240 };
        Application.targetFrameRate = _data.LimiteFPS < fps.Length ? fps[_data.LimiteFPS] : 0;

        // Résolution + mode fenêtre
        if (_resolutions != null && _data.IndexResolution >= 0
            && _data.IndexResolution < _resolutions.Length)
        {
            Resolution r = _resolutions[_data.IndexResolution];
            FullScreenMode mode = _data.ModeEcran switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.FullScreenWindow, // Borderless
                _ => FullScreenMode.ExclusiveFullScreen
            };
            Screen.SetResolution(r.width, r.height, mode, r.refreshRateRatio);
        }
    }

    // ================================================================
    // AUDIO
    // ================================================================

    public void AppliquerAudio()
    {
        SetVolumeMixer("VolumeMaster",   _data.VolumeMaster);
        SetVolumeMixer("VolumeMusique",  _data.VolumeMusique);
        SetVolumeMixer("VolumeSFX",      _data.VolumeSFX);
        SetVolumeMixer("VolumeAmbiance", _data.VolumeAmbiance);
    }

    // Convertit linéaire 0-1 en dB pour le mixer (-80 à 0 dB)
    private void SetVolumeMixer(string parametre, float valeurLineaire)
    {
        if (_mixer == null) return;
        float db = valeurLineaire <= 0.001f ? -80f : Mathf.Log10(valeurLineaire) * 20f;
        _mixer.SetFloat(parametre, db);
    }

    // ================================================================
    // SENSIBILITÉ SOURIS — lue par PlayerController
    // ================================================================

    public float SensibiliteSouris => _data.SensibiliteSouris;
    public bool  InverserY         => _data.InverserY;

    // ================================================================
    // TOUCHES — lues par les scripts input
    // ================================================================

    public KeyCode GetTouche(ActionJeu action) => _data.GetTouche(action);

    public bool ActionPressée(ActionJeu action)
        => Input.GetKeyDown(_data.GetTouche(action));

    public bool ActionMaintenue(ActionJeu action)
        => Input.GetKey(_data.GetTouche(action));

    public bool ActionRelachée(ActionJeu action)
        => Input.GetKeyUp(_data.GetTouche(action));

    // ================================================================
    // SAUVEGARDER — délègue à OptionsRepository (v2)
    // ================================================================

    public void Sauvegarder()
    {
        OptionsRepository.Sauvegarder(_data);
        AppliquerTout();
        Debug.Log("[OptionsManager] Options sauvegardées et appliquées.");
    }

    public void ResetDefauts()
    {
        OptionsRepository.Reset(_data);
        Sauvegarder();
        Debug.Log("[OptionsManager] Options réinitialisées aux défauts.");
    }
}
