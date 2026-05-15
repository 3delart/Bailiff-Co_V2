// ============================================================
// MissionSummaryUI.cs — Bailiff & Co  V2 (CORRIGÉ - AFFICHE PRIX BASE + CASSÉ)
// Bulletin de paie affiché après chaque mission.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BailiffCo.Hub;

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
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargeeHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
        EventBus<OnSceneChargee>.Unsubscribe(OnSceneChargeeHandler);
    }

    public override void ReAbonnerEventBus()
    {
        EventBus<OnMissionEnded>.Unsubscribe(OnMissionEnded);
        EventBus<OnMissionEnded>.Subscribe(OnMissionEnded);
        EventBus<OnSceneChargee>.Unsubscribe(OnSceneChargeeHandler);
        EventBus<OnSceneChargee>.Subscribe(OnSceneChargeeHandler);
    }

    // ================================================================
    // HANDLER
    // ================================================================

    private void OnMissionEnded(OnMissionEnded e)
    {
        _result = e.Result;
    }

    private void OnSceneChargeeHandler(OnSceneChargee e)
    {
        if (_result == null) return;
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
        HubManager.Instance?.MettreAJourAffichageArgent();
        _result = null;
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
        {
            string bonusLabel = r.BonusTempsApplique ? "\n⏱ Bonus Vitesse" : string.Empty;
            _texteEtoiles.text = "Etoiles reçu\n" + BuildEtoiles(r.Etoiles) + bonusLabel;
        }
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
            _texteTotalObjets.text = $"Total      {r.NombreObjetsRecuperes} Objets      ~{PriceFormatter.Format(valeurTotale)}";
        }

        // Ligne "Salaire prévu 25.00% 1250€"
        if (_texteSalairePrevu)
        {
            float montantSalairePrevu = r.CommissionBase + r.BonusPerformance;
            float tauxAffiche = r.MissionReussie
                ? r.Mission?.CommissionTaux ?? 0.40f
                : r.Mission?.CommissionEchecTaux ?? 0.10f;

            _texteSalairePrevu.text = $"Salaire prévu      {tauxAffiche * 100f:F0}%      {PriceFormatter.Format(montantSalairePrevu)}";
        }
    }

    // ── Objets Cassés/Abîmés ──────────────────────────────────

    private void AfficherObjetsAbimes(MissionResult r)
    {
        VideContainer(_containerObjetsAbimes);

        float totalPenalite = 0f;
        int totalQte = 0;

        foreach (var obj in r.ObjetsEndommages)
        {
            CreerLigneItemAbime(
                _containerObjetsAbimes,
                obj.Nom,
                1,
                obj.ValeurUnitaire,
                obj.DamagePercent,
                obj.ValeurActuelle,
                obj.Penalite,
                alternerBackground: true
            );
            totalPenalite += obj.Penalite;
            totalQte++;
        }

        if (_texteSubTotalObjetsAbimes)
        {
            if (totalQte > 0)
            {
                _texteSubTotalObjetsAbimes.text = 
                    $"Total cassés      {totalQte} objet(s)      Retenu: {PriceFormatter.Format(totalPenalite)}";
            }
            else
            {
                _texteSubTotalObjetsAbimes.text = "Aucun objet cassé";
            }
        }
    }

    // ── Véhicule ───────────────────────────────────────────────

    private void AfficherVehicule(MissionResult r)
    {
        VideContainer(_containerVehicule);

        float totalVehicule = 0f;
        int nbFactures = 0;

        // 1. Coût location
        if (r.CoutLocationVehicule > 0f)
        {
            string vehiculeName = GameManager.Instance?.VehiculeSelectionne?.VehicleName ?? "Véhicule";
            CreerLigneVehiculeSimple(
                _containerVehicule,
                $"Location {vehiculeName}",
                r.CoutLocationVehicule
            );
            totalVehicule += r.CoutLocationVehicule;
            nbFactures++;
        }

        // 1.5. Options louées
        if (r.OptionsLouees != null && r.OptionsLouees.Count > 0)
        {
            foreach (var option in r.OptionsLouees)
            {
                CreerLigneVehiculeSimple(
                    _containerVehicule,
                    option.OptionName,
                    option.Price
                );
                totalVehicule += option.Price;
                nbFactures++;
            }
        }

        // 2. Rétroviseur cassé (si dégâts)
        if (r.DegatsVehicule > 0f)
        {
            CreerLigneVehiculeSimple(
                _containerVehicule,
                "Retroviseur cassé (Cas rare)",
                r.DegatsVehicule
            );
            totalVehicule += r.DegatsVehicule;
            nbFactures++;
        }

        if (_texteSubTotalVehicule)
        {
            _texteSubTotalVehicule.text = $"Total      {nbFactures} factures      {PriceFormatter.Format(totalVehicule)}";
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
            _texteSubTotalAmendes.text = $"Total      {nbInfractions} infractions      {PriceFormatter.Format(totalAmendes)}";
        }
    }

    // ── Totaux ────────────────────────────────────────────────

    private void AfficherTotaux(MissionResult r)
    {
        // ✅ Calcul des pénalités depuis ObjetsEndommages directement
        float penaliteObjets = 0f;
        foreach (var o in r.ObjetsEndommages) 
            penaliteObjets += o.Penalite;  // ✅ Utilise la pénalité calculée correctement

        float totalVehicule = r.CoutLocationVehicule + r.CoutOptionsVehicule + r.DegatsVehicule;
        float totalAmendes = r.AmendesSaisieExcessive + r.AmendesInfractions;

        float totalRetenues = penaliteObjets + totalVehicule + totalAmendes;

        // Affichage dans la section SectionTotauxGlobal
        if (_texteTotalRetenues)
            _texteTotalRetenues.text = PriceFormatter.Format(r.CommissionBase + r.BonusPerformance);

        if (_texteTotalCasse)
            _texteTotalCasse.text = PriceFormatter.Format(penaliteObjets);

        if (_texteTotalVehicule)
            _texteTotalVehicule.text = PriceFormatter.Format(totalVehicule);

        if (_texteTotalAmande)
            _texteTotalAmande.text = PriceFormatter.Format(totalAmendes);

        // Salaire final
        if (_texteSalaireAvantImpots)
        {
            _texteSalaireAvantImpots.text = PriceFormatter.Format(r.SalaireNet);
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
                _texteSubTotalConsommables.text = $"Total      {totalQte}      {PriceFormatter.Format(totalConsommables)}";
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

    private void CreerLigneVehiculeSimple(Transform container, string libelle, float price, bool alternerBackground = true)
    {
        if (container == null || _prefabLigneItem == null) return;

        GameObject ligneGO = Instantiate(_prefabLigneItem, container);

        if (alternerBackground)
        {
            Image bg = ligneGO.GetComponent<Image>();
            if (bg == null)
                bg = ligneGO.AddComponent<Image>();

            int index = container.childCount - 1;
            bg.color = (index % 2 == 0)
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);
        }

        CanvasGroup cg = ligneGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = ligneGO.AddComponent<CanvasGroup>();
        StartCoroutine(FadeInLigne(cg, 0.3f));

        TextMeshProUGUI[] texts = ligneGO.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 4)
        {
            texts[0].text = libelle;
            texts[1].text = string.Empty;
            texts[2].text = string.Empty;
            texts[3].text = PriceFormatter.Format(price);
        }
        else
        {
            Debug.LogWarning($"[MissionSummaryUI] Le prefab _prefabLigneItem n'a pas 4 TextMeshProUGUI enfants (trouvé: {texts.Length})");
        }
    }

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
        
        // Animation fade-in
        CanvasGroup cg = ligneGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = ligneGO.AddComponent<CanvasGroup>();
        StartCoroutine(FadeInLigne(cg, 0.3f));
        
        // Textes
        TextMeshProUGUI[] texts = ligneGO.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 4)
        {
            texts[0].text = libelle;
            texts[1].text = quantite.ToString();
            texts[2].text = PriceFormatter.Format(prixUnitaire);
            texts[3].text = PriceFormatter.Format(total);
        }
        else
        {
            Debug.LogWarning($"[MissionSummaryUI] Le prefab _prefabLigneItem n'a pas 4 TextMeshProUGUI enfants (trouvé: {texts.Length})");
        }
    }
    
    // ✅ Affiche UNE SEULE instance endommagée avec ses dégâts spécifiques
    private void CreerLigneItemAbime(Transform container, string libelle, int quantite, float prixOriginal, float damagePercent, float prixActuel, float penalite, bool alternerBackground = true)
    {
        if (container == null || _prefabLigneItem == null) return;
        
        GameObject ligneGO = Instantiate(_prefabLigneItem, container);
        
        if (alternerBackground)
        {
            Image bg = ligneGO.GetComponent<Image>();
            if (bg == null)
                bg = ligneGO.AddComponent<Image>();
            
            int index = container.childCount - 1;
            bg.color = (index % 2 == 0) 
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);
        }
        
        CanvasGroup cg = ligneGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = ligneGO.AddComponent<CanvasGroup>();
        StartCoroutine(FadeInLigne(cg, 0.3f));
        
        TextMeshProUGUI[] texts = ligneGO.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 4)
        {
            texts[0].text = libelle;
            texts[1].text = PriceFormatter.Format(prixOriginal);
            texts[2].text = $"{damagePercent:F0}%";
            texts[3].text = PriceFormatter.Format(penalite);
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