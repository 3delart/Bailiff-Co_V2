// ============================================================
// MissionResult.cs — Bailiff & Co  V2
// Extrait de Enums.cs (V1).
// Classe de données pure — résultat d'une mission terminée.
// Transportée par OnMissionTerminee vers GameManager,
// HUDSystem (écran de résultat) et SceneLoader.
// ============================================================

[System.Serializable]
public class MissionResult
{
    public MissionData Mission;

    // — Valeurs financières —
    public float ValeurTotaleRecuperee;
    public float ValeurQuotaCible;
    public float ArgentGagne;           // après frais d'agence (85%)

    // — Statistiques —
    public int   NombreObjetsRecuperes;
    public int   NombreObjetsCasses;
    public int   NombrePiegesDeclenches;
    public float TempsSecondes;
    public float ParanoiaMaxAtteinte;

    // — Résultat —
    public bool  MissionReussie;
    public int   Etoiles;               // 0–3
}
