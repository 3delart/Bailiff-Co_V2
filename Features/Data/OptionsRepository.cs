// ============================================================
// OptionsRepository.cs — Bailiff & Co  [v2 - NOUVEAU]
// Responsable UNIQUEMENT de la persistance PlayerPrefs.
// Extrait de OptionsData v1.
//
// Responsabilités :
//   - Charger()      : lit JSON depuis PlayerPrefs → OptionsData
//   - Sauvegarder()  : sérialise OptionsData → JSON → PlayerPrefs
//   - Reset()        : réinitialise tous les champs aux valeurs par défaut
//
// Usage :
//   OptionsData data = OptionsRepository.Charger();
//   OptionsRepository.Sauvegarder(data);
//   OptionsRepository.Reset(data);
// ============================================================
using UnityEngine;

public static class OptionsRepository
{
    private const string PREFS_KEY = "BailiffCo_Options";

    // ================================================================
    // CHARGER
    // ================================================================

    /// <summary>
    /// Charge les options depuis PlayerPrefs.
    /// Retourne une instance par défaut si aucune sauvegarde n'existe
    /// ou si les données sont corrompues.
    /// </summary>
    public static OptionsData Charger()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            try
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                var data = new OptionsData();
                JsonUtility.FromJsonOverwrite(json, data);
                return data;
            }
            catch
            {
                Debug.LogWarning("[OptionsRepository] Données corrompues, reset aux défauts.");
            }
        }
        return new OptionsData();
    }

    // ================================================================
    // SAUVEGARDER
    // ================================================================

    /// <summary>
    /// Sérialise les options en JSON et les écrit dans PlayerPrefs.
    /// </summary>
    public static void Sauvegarder(OptionsData data)
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    // ================================================================
    // RESET
    // ================================================================

    /// <summary>
    /// Réinitialise tous les champs de <paramref name="data"/>
    /// aux valeurs par défaut (via une instance fraîche).
    /// </summary>
    public static void Reset(OptionsData data)
    {
        var d = new OptionsData();
        data.IndexResolution   = d.IndexResolution;
        data.ModeEcran         = d.ModeEcran;
        data.QualiteGlobale    = d.QualiteGlobale;
        data.VSync             = d.VSync;
        data.LimiteFPS         = d.LimiteFPS;
        data.VolumeMaster      = d.VolumeMaster;
        data.VolumeMusique     = d.VolumeMusique;
        data.VolumeSFX         = d.VolumeSFX;
        data.VolumeAmbiance    = d.VolumeAmbiance;
        data.SensibiliteSouris = d.SensibiliteSouris;
        data.InverserY         = d.InverserY;
        data.ToucheAvancer     = d.ToucheAvancer;
        data.ToucheReculer     = d.ToucheReculer;
        data.ToucheGauche      = d.ToucheGauche;
        data.ToucheDroite      = d.ToucheDroite;
        data.ToucheInteragir   = d.ToucheInteragir;
        data.ToucheSprint      = d.ToucheSprint;
        data.ToucheAccroupi    = d.ToucheAccroupi;
        data.ToucheAllonge     = d.ToucheAllonge;
        data.ToucheSaut        = d.ToucheSaut;
        data.ToucheInventaire  = d.ToucheInventaire;
        data.TouchePause       = d.TouchePause;
        data.TouchePoser       = d.TouchePoser;
        data.ToucheJetter      = d.ToucheJetter;
    }
}
