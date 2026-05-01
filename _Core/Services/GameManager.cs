// ============================================================
// GameManager.cs — Bailiff & Co  V2
// Singleton persistant (DontDestroyOnLoad).
// Seul objet qui survit entre toutes les scènes.
//
// CHANGEMENTS V2 :
//   - Ajout de VehiculeSelectionne (transmis Hub → MissionBuilder)
//   - LancerMission() stocke aussi le véhicule
//   - Plus de FindObjectOfType nulle part
//   - TerminerMission() notifie via EventBus avant le retour Hub
//
// RÈGLE : Le GameManager ne contient PAS de logique de jeu.
// Il transporte des données et délègue les transitions.
// ============================================================
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ================================================================
    // SINGLETON
    // ================================================================

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitialiserDonnees();
    }

    // ================================================================
    // DONNÉES PERSISTANTES
    // ================================================================

    /// <summary>Mission sélectionnée dans le Hub.</summary>
    public MissionData  MissionSelectionnee  { get; private set; }

    /// <summary>Véhicule loué dans le Hub — transmis à MissionBuilder.</summary>
    public VehiculeData VehiculeSelectionne  { get; private set; }

    /// <summary>Argent total du joueur (persiste entre missions).</summary>
    public float Argent { get; private set; } = 0f;

    /// <summary>Numéro de la dernière mission complétée.</summary>
    public int DerniereMissionCompletee { get; private set; } = 0;

    /// <summary>Personnalisation du personnage.</summary>
    public PlayerConfigData Personnalisation { get; private set; }

    // ================================================================
    // INITIALISATION
    // ================================================================

    private void InitialiserDonnees()
    {
        // TODO : charger depuis SaveSystem (V3)
        Argent                   = 0f;
        DerniereMissionCompletee = 0;
        MissionSelectionnee      = null;
        VehiculeSelectionne      = null;
        Personnalisation         = new PlayerConfigData();
    }

    // ================================================================
    // API — MISSION
    // ================================================================

    /// <summary>
    /// Lance la mission avec le véhicule choisi.
    /// Appelé par HubManager après confirmation.
    /// </summary>
    public void LancerMission(MissionData mission, VehiculeData vehicule)
    {
        if (mission == null)
        {
            Debug.LogError("[GameManager] LancerMission : MissionData est null !");
            return;
        }

        MissionSelectionnee = mission;
        VehiculeSelectionne = vehicule;

        Debug.Log($"[GameManager] Mission : {mission.MissionName} | Véhicule : {vehicule?.VehicleName ?? "aucun"}");
        
        SceneLoader.Instance.ChargerScene(SceneNames.MISSION);
    }

    /// <summary>
    /// Appelé par SceneLoader après le délai post-mission.
    /// Met à jour l'argent, notifie, puis retourne au Hub.
    /// </summary>
    public void TerminerMission(MissionResult resultat)
    {
        if (resultat.MissionReussie)
        {
            Argent += resultat.ArgentGagne;

            if (MissionSelectionnee != null &&
                MissionSelectionnee.MissionNumber > DerniereMissionCompletee)
                DerniereMissionCompletee = MissionSelectionnee.MissionNumber;
        }

        MissionSelectionnee = null;
        VehiculeSelectionne = null;

        Debug.Log($"[GameManager] Mission terminée — Argent total : {Argent:N0} €");

        // TODO : sauvegarder via SaveSystem (V3)

        SceneLoader.Instance.ChargerScene(SceneNames.HUB, avecFondu: true);
    }

    // ================================================================
    // API — NAVIGATION
    // ================================================================

    public void AllerAuMenu() =>
        SceneLoader.Instance.ChargerScene(SceneNames.MENU, avecFondu: true);

    public void AllerAuHub() =>
        SceneLoader.Instance.ChargerScene(SceneNames.HUB, avecFondu: true);

    public void QuitterJeu()
    {
        Debug.Log("[GameManager] Quitter le jeu.");
        Application.Quit();
    }

    // ================================================================
    // API — PERSONNALISATION
    // ================================================================

    public void SauvegarderPersonnalisation(PlayerConfigData data)
    {
        if (data == null) return;
        Personnalisation = data;
        Debug.Log("[GameManager] Personnalisation sauvegardée.");
        // TODO : SaveSystem (V3)
    }

    // ================================================================
    // API — ARGENT
    // ================================================================

    public bool PeutPayer(float montant) => Argent >= montant;

    public void Debiter(float montant)  => Argent = Mathf.Max(0f, Argent - montant);

    public void Crediter(float montant) => Argent += montant;
}
