// ============================================================
// MissionSummaryUI.cs — Bailiff & Co  V2 (CORRIGÉ POUR VOTRE STRUCTURE)
// Bulletin de paie affiché après chaque mission.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionSummaryUI : UIPanel
{
    // ================================================================
    // HEADER
    // ================================================================
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI _texteTitre;          // TitleLabel
    [SerializeField] private TextMeshProUGUI _texteNomMission;     // MissionName
    [SerializeField] private TextMeshProUGUI _texteEtoiles;        // EtoilesTxt

    // ================================================================
    // SECTION: OBJETS RÉCUPÉRÉS
    // ================================================================
    [Header("Objets récupérés")]
    [SerializeField] private Transform       _containerObjetsRecuperes;  // Content dans ScrollView de SectionObjetsRecup
    [SerializeField] private TextMeshProUGUI _texteTotalObjets;          // LigneObjetTotal → Libelle
    [SerializeField] private TextMeshProUGUI _texteSalairePrevu;         // LigneSalairePrevu → Libelle

    // ================================================================
    // SECTION: OBJETS CASSÉS/ABÎMÉS
    // ================================================================
    [Header("Objets Cassés")]
    [SerializeField] private Transform       _containerObjetsAbimes;     // Content dans ScrollView de SectionObjetsCasse
    [SerializeField] private TextMeshProUGUI _texteSubTotalObjetsAbimes; // LigneObjetTotal → Libelle

    // ================================================================
    // SECTION: VÉHICULE
    // ================================================================
    [Header("Véhicule")]
    [SerializeField] private Transform       _containerVehicule;         // Content dans ScrollView de SectionVehicle
    [SerializeField] private TextMeshProUGUI _texteSubTotalVehicule;     // LigneTotalVehicule

    // ================================================================
    // SECTION: AMENDES
    // ================================================================
    [Header("Amendes")]
    [SerializeField] private GameObject      _sectionAmendes;            // SectionAmande (GameObject parent)
    [SerializeField] private Transform       _containerAmendes;          // Content dans ScrollView
    [SerializeField] private TextMeshProUGUI _texteSubTotalAmendes;      // LigneTotalAmande

    // ================================================================
    // SECTION: TOTAUX GLOBAUX
    // ================================================================
    [Header("Totaux")]
    [SerializeField] private TextMeshProUGUI _texteTotalRetenues;        // Total revenus → Value
    [SerializeField] private TextMeshProUGUI _texteTotalCasse;           // Total casse → Value
    [SerializeField] private TextMeshProUGUI _texteTotalVehicule;        // Total Vehicle → Value
    [SerializeField] private TextMeshProUGUI _texteTotalAmande;          // Total Amande → Value
    [SerializeField] private TextMeshProUGUI _texteSalaireAvantImpots;  // TotalFinal → Value

    // ================================================================
    // SECTION: CONSOMMABLES
    // ================================================================
    [Header("Consommables")]
    [SerializeField] private Transform       _containerConsommables;     // Content dans ScrollView
    [SerializeField] private TextMeshProUGUI _texteSubTotalConsommables; // LigneTotalConsumable

    // ================================================================
    // BOUTON
    // ================================================================
    [Header("Bouton")]
    [SerializeField] private Button _btnContinuer;

    // ================================================================
    // PREFAB UNIVERSEL
    // ================================================================
    [Header("Prefab ligne universelle")]
    [Tooltip("Prefab avec 4 TextMeshProUGUI: Libellé, Quantité, Prix, Total")]
    [SerializeField] private GameObject _prefabLigneItem;

    // ================================================================
    // DONNÉES
    // ================================================================
    private MissionResult _result;

    // Couleurs
    private static readonly Color CouleurPositif = new Color(0.2f, 0.85f, 0.3f);
    private static readonly Color CouleurNegatif = new Color(0.95f, 0.25f, 0.25f);

    // ================================================================
    // LIFECYCLE
    // ================================================================

    protected override void Awake()
    {
        base.Awake();
        _btnContinuer?.onClick.AddListener(OnClickContinuer);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventBus<OnMissionEnded>.Subscribe(OnMissionEnded);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
    }

    public override void ReAbonnerEventBus()
    {
        EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
        EventBus<OnMissionEnded>.Subscribe(OnMissionEnded);
    }

    // ================================================================
    // HANDLER
    // ================================================================

    private void OnMissionEnded(OnMissionEnded e)
    {
        _result = e.Result;
        AfficherBulletin(_result);
        Ouvrir();
    }

    // ================================================================
    // BOUTON
    // ================================================================

    private void OnClickContinuer()
    {
        Fermer();
        GameManager.Instance?.TerminerMission(_result);
    }

    // ================================================================
    // AFFICHAGE PRINCIPAL
    // ================================================================

    private void AfficherBulletin(MissionResult r)
    {
        AfficherHeader(r);
        AfficherObjetsRecuperes(r);
        AfficherObjetsAbimes(r);
        AfficherVehicule(r);
        AfficherAmendes(r);
        AfficherTotaux(r);
        AfficherConsommables(r);
    }

    // ── Header ───────────────────────────────────────────────

    private void AfficherHeader(MissionResult r)
    {
        if (_texteTitre)
            _texteTitre.text = "Résultat de mission";

        if (_texteNomMission)
            _texteNomMission.text = r.Mission != null ? r.Mission.MissionName : string.Empty;

        if (_texteEtoiles)
            _texteEtoiles.text = "Etoiles reçu\n" + BuildEtoiles(r.Etoiles);
    }

    // ── Objets récupérés ─────────────────────────────────────

    private void AfficherObjetsRecuperes(MissionResult r)
    {
        VideContainer(_containerObjetsRecuperes);

        float valeurTotale = 0f;
        
        foreach (var obj in r.ObjetsRecuperes)
        {
            CreerLigneItem(
                _containerObjetsRecuperes,
                obj.Nom,
                obj.Quantite,
                obj.ValeurUnitaire,
                obj.ValeurTotale,
                alternerBackground: true
            );
            valeurTotale += obj.ValeurTotale;
        }

        // Ligne "Total X Objets ~5000€"
        if (_texteTotalObjets)
        {
            _texteTotalObjets.text = $"Total      {r.NombreObjetsRecuperes} Objets      ~{valeurTotale:N0}€";
        }

        // Ligne "Salaire prévu 25.00% 1250€"
        if (_texteSalairePrevu)
        {
            float montantSalairePrevu = r.CommissionBase + r.BonusPerformance;
            float tauxAffiche = r.MissionReussie
                ? r.Mission?.CommissionTaux ?? 0.25f
                : r.Mission?.CommissionEchecTaux ?? 0.10f;

            _texteSalairePrevu.text = $"Salaire prévu      {tauxAffiche * 100f:F0}%      {montantSalairePrevu:N0} €";
        }
    }

    // ── Objets Cassés/Abîmés ──────────────────────────────────

    private void AfficherObjetsAbimes(MissionResult r)
    {
        VideContainer(_containerObjetsAbimes);

        float totalPenalites = 0f;
        int totalQte = 0;

        // Regrouper par nom
        var grouped = new Dictionary<string, (int qty, float penTotale, float prixUnit)>();
        foreach (var obj in r.ObjetsEndommages)
        {
            if (grouped.TryGetValue(obj.Nom, out var prev))
                grouped[obj.Nom] = (prev.qty + 1, prev.penTotale + obj.Penalite, obj.ValeurUnitaire);
            else
                grouped[obj.Nom] = (1, obj.Penalite, obj.ValeurUnitaire);
        }

        foreach (var kv in grouped)
        {
            float prixUnitaireAbime = kv.Value.prixUnit * 0.5f;
            
            CreerLigneItem(
                _containerObjetsAbimes,
                kv.Key,
                kv.Value.qty,
                prixUnitaireAbime,
                kv.Value.penTotale
            );
            
            totalPenalites += kv.Value.penTotale;
            totalQte += kv.Value.qty;
        }

        if (_texteSubTotalObjetsAbimes)
        {
            _texteSubTotalObjetsAbimes.text = $"Total      {totalQte} objets      {totalPenalites:N0} €";
        }
    }

    // ── Véhicule ───────────────────────────────────────────────

    private void AfficherVehicule(MissionResult r)
    {
        VideContainer(_containerVehicule);

        string nomVehicule = GameManager.Instance?.VehiculeSelectionne?.VehicleName ?? "pickup";
        
        float totalVehicule = 0f;
        int nbFactures = 0;

        // 1. Location (toujours présente)
        CreerLigneItem(
            _containerVehicule,
            $"Location ({nomVehicule})",
            1,
            r.CoutLocationVehicule,
            r.CoutLocationVehicule
        );
        totalVehicule += r.CoutLocationVehicule;
        nbFactures++;

        // 2. Rétroviseur cassé (si dégâts)
        if (r.DegatsVehicule > 0f)
        {
            CreerLigneItem(
                _containerVehicule,
                "Retroviseur cassé (Cas rare)",
                1,
                r.DegatsVehicule,
                r.DegatsVehicule
            );
            totalVehicule += r.DegatsVehicule;
            nbFactures++;
        }

        if (_texteSubTotalVehicule)
        {
            _texteSubTotalVehicule.text = $"Total      {nbFactures} factures      {totalVehicule:N0} €";
        }
    }

    // ── Amendes ───────────────────────────────────────────────

    private void AfficherAmendes(MissionResult r)
    {
        bool aSaisieExcessive = r.AmendesSaisieExcessive > 0f;
        bool aInfractions = r.AmendesInfractions > 0f;
        bool aAmendes = aSaisieExcessive || aInfractions;

        SetActif(_sectionAmendes, aAmendes);
        if (!aAmendes) return;

        VideContainer(_containerAmendes);

        float totalAmendes = 0f;
        int nbInfractions = 0;

        // Infractions
        if (aInfractions)
        {
            // Exemple simplifié - adaptez selon vos données
            CreerLigneItem(
                _containerAmendes,
                "Infractions diverses",
                1,
                r.AmendesInfractions,
                r.AmendesInfractions
            );
            totalAmendes += r.AmendesInfractions;
            nbInfractions++;
        }

        // Saisie excessive
        if (aSaisieExcessive)
        {
            string libelleSaisie = r.Suspendu 
                ? "Saisie abusive (SUSPENSION)" 
                : "Saisie excessive";
            
            CreerLigneItem(
                _containerAmendes,
                libelleSaisie,
                1,
                r.AmendesSaisieExcessive,
                r.AmendesSaisieExcessive
            );
            totalAmendes += r.AmendesSaisieExcessive;
            nbInfractions++;
        }

        if (_texteSubTotalAmendes)
        {
            _texteSubTotalAmendes.text = $"Total      {nbInfractions} infractions      {totalAmendes:N0} €";
        }
    }

    // ── Totaux ────────────────────────────────────────────────

    private void AfficherTotaux(MissionResult r)
    {
        // Calcul des totaux par catégorie
        float penaliteObjets = 0f;
        foreach (var o in r.ObjetsEndommages) 
            penaliteObjets += o.Penalite;

        float totalVehicule = r.CoutLocationVehicule + r.DegatsVehicule;
        float totalAmendes = r.AmendesSaisieExcessive + r.AmendesInfractions;
        
        float totalRetenues = penaliteObjets + totalVehicule + totalAmendes;

        // Affichage dans la section SectionTotauxGlobal
        if (_texteTotalRetenues)
            _texteTotalRetenues.text = $"{r.CommissionBase + r.BonusPerformance:N0} €";

        if (_texteTotalCasse)
            _texteTotalCasse.text = $"{penaliteObjets:N0} €";

        if (_texteTotalVehicule)
            _texteTotalVehicule.text = $"{totalVehicule:N0} €";

        if (_texteTotalAmande)
            _texteTotalAmande.text = $"{totalAmendes:N0} €";

        // Salaire final
        if (_texteSalaireAvantImpots)
        {
            _texteSalaireAvantImpots.text = $"{r.SalaireNet:N0} €";
            _texteSalaireAvantImpots.color = r.SalaireNet >= 0f ? CouleurPositif : CouleurNegatif;
        }
    }

    // ── Consommables ──────────────────────────────────────────

    private void AfficherConsommables(MissionResult r)
    {
        VideContainer(_containerConsommables);

        float totalConsommables = 0f;
        int totalQte = 0;

        foreach (var c in r.ConsommablesUtilises)
        {
            CreerLigneItem(
                _containerConsommables,
                c.Nom,
                c.Quantite,
                c.CoutUnitaire,
                c.CoutTotal
            );
            totalConsommables += c.CoutTotal;
            totalQte += c.Quantite;
        }

        if (_texteSubTotalConsommables)
        {
            if (r.ConsommablesUtilises.Count > 0)
            {
                _texteSubTotalConsommables.text = $"Total      {totalQte}      {totalConsommables:N0} €";
            }
            else
            {
                _texteSubTotalConsommables.text = "Aucun consommable utilisé";
            }
        }
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private void CreerLigneItem(Transform container, string libelle, int quantite, float prixUnitaire, float total, bool alternerBackground = true)
    {
        if (container == null || _prefabLigneItem == null) return;
        
        GameObject ligneGO = Instantiate(_prefabLigneItem, container);
        
        // Background alterné
        if (alternerBackground)
        {
            Image bg = ligneGO.GetComponent<Image>();
            if (bg == null)
            {
                bg = ligneGO.AddComponent<Image>();
            }
            
            int index = container.childCount - 1;
            bg.color = (index % 2 == 0) 
                ? new Color(1f, 1f, 1f, 0f)          // Transparent
                : new Color(0.95f, 0.95f, 0.95f, 1f); // Gris léger
        }
        
        // Animation fade-in (optionnel - commentez si vous ne voulez pas d'animation)
        CanvasGroup cg = ligneGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = ligneGO.AddComponent<CanvasGroup>();
        StartCoroutine(FadeInLigne(cg, 0.3f));
        
        // Textes
        TextMeshProUGUI[] texts = ligneGO.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 4)
        {
            texts[0].text = libelle;
            texts[1].text = quantite.ToString();
            texts[2].text = $"{prixUnitaire:N2} €";
            texts[3].text = $"{total:N2} €";
        }
        else
        {
            Debug.LogWarning($"[MissionSummaryUI] Le prefab _prefabLigneItem n'a pas 4 TextMeshProUGUI enfants (trouvé: {texts.Length})");
        }
    }

    private IEnumerator FadeInLigne(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    private static string BuildEtoiles(int count)
    {
        count = Mathf.Clamp(count, 0, 3);
        return new string('★', count) + new string('☆', 3 - count);
    }

    private static void VideContainer(Transform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    private static void SetActif(GameObject go, bool actif)
    {
        if (go != null) go.SetActive(actif);
    }
}