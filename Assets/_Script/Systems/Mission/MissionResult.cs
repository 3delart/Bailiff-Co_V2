// ============================================================
// MissionResult.cs — Bailiff & Co  V2
// Résultat complet d'une mission : honoraires, retenues,
// salaire net, détails objets et consommables.
// Transporté par OnMissionEnded.
// ============================================================
using System.Collections.Generic;
using BailiffCo;

[System.Serializable]
public class MissionResult
{
    public MissionData Mission;

    // — Résumé mission —
    public bool  MissionReussie;
    public int   Etoiles;               // 0–3
    public float TempsSecondes;
    public float ParanoiaMaxAtteinte;
    public int   NombrePiegesDeclenches;

    // — Valeurs brutes —
    public float ValeurTotaleRecuperee;
    public float ValeurQuotaCible;
    public int   NombreObjetsRecuperes;

    // — Honoraires —
    public float CommissionBase;        // 25% (ou 10%) × ValeurTotaleRecuperee
    public float BonusPerformance;      // bonus étoiles

    // — Retenues A : objets cassés (pénalité = ValeurUnitaire - ValeurActuelle) —
    public List<ObjetEndommage> ObjetsEndommages = new List<ObjetEndommage>();

    // — Retenues B : véhicule —
    public float CoutLocationVehicule;
    public float DegatsVehicule;        // rétroviseur / accident (système TBD)
    public List<VehicleOption> OptionsLouees = new();
    public float CoutOptionsVehicule = 0f;

    // — Retenues C : saisie excessive —
    public float AmendesSaisieExcessive;
    public bool  Suspendu;              // true si excès > SeuilExcesAbusif
    public bool  BonusTempsApplique;   // true si elapsedTime < BonusTimeThresholdSeconds

    // — Retenues D : infractions PNJ (système TBD) —
    public float AmendesInfractions;

    // — Salaire net —
    public float SalaireNet;            // Honoraires − Retenues

    // — Détail objets récupérés (depuis QuotaSystem.LoadedObjects) —
    public List<ObjetRecupere> ObjetsRecuperes = new List<ObjetRecupere>();

    // — Consommables (info uniquement, déjà payé) —
    public List<ConsommableUtilise> ConsommablesUtilises = new List<ConsommableUtilise>();

    // ================================================================
    // STRUCTS
    // ================================================================

    [System.Serializable]
    public struct ObjetEndommage
    {
        public string Nom;
        public float  ValeurUnitaire;    // ✅ Prix original (avant dégâts)
        public float  ValeurActuelle;    // ✅ NOUVEAU: Prix après dégâts
        public float  DamagePercent;     // ✅ NOUVEAU: % de dégâts
        public float  Penalite;          // = (ValeurUnitaire - ValeurActuelle) — 100% pénalité
    }

    [System.Serializable]
    public struct ObjetRecupere
    {
        public string Nom;
        public int    Quantite;
        public float  ValeurUnitaire;   // prix de base unitaire
        public float  ValeurTotale;     // prix de base total
        public float  ValeurActuelle;   // ✅ NOUVEAU: prix après dégâts
        public float  DamagePercent;    // ✅ NOUVEAU: % moyen de dégâts
    }

    [System.Serializable]
    public struct ConsommableUtilise
    {
        public string Nom;
        public int    Quantite;
        public float  CoutUnitaire;
        public float  CoutTotal;
    }
}