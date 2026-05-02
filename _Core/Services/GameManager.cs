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
        DontDestroyOnLoad(transform.root.gameObject);
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
    // GESTION PLAYER PERSISTANT
    // ================================================================

    private GameObject _playerInstance;

    /// <summary>Référence au Player persistant (null si pas encore créé).</summary>
    public GameObject Player => _playerInstance;

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
    // API — PLAYER PERSISTANT
    // ================================================================

    /// <summary>
    /// Spawne le Player s'il n'existe pas, ou le déplace s'il existe déjà.
    /// Appelé par HubManager au démarrage du Hub et par MissionBuilder.
    /// </summary>
    public void SpawnerPlayerSiNecessaire(Vector3 position, Quaternion rotation)
    {
        if (_playerInstance != null)
        {
            // Le Player existe déjà, juste le déplacer
            CharacterController cc = _playerInstance.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false; // Désactiver pour téléporter
                _playerInstance.transform.position = position;
                _playerInstance.transform.rotation = rotation;
                cc.enabled = true;
            }
            else
            {
                _playerInstance.transform.position = position;
                _playerInstance.transform.rotation = rotation;
            }
            
            _playerInstance.SetActive(true);
            Debug.Log($"[GameManager] Player déplacé à {position}");
        }
        else
        {
            // Créer le Player pour la première fois
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Player/PlayerRoot");
            
            if (prefab == null)
            {
                Debug.LogError("[GameManager] PlayerRoot prefab introuvable dans Resources/Prefabs/Player/");
                return;
            }
            
            _playerInstance = Instantiate(prefab, position, rotation);
            DontDestroyOnLoad(_playerInstance); // ← PERSISTE entre scènes
            _playerInstance.name = "Player_Persistent";
            Debug.Log($"[GameManager] Player créé à {position}");
        }
    }

    /// <summary>
    /// Cache le Player (utile pour les cutscenes ou menus).
    /// </summary>
    public void CacherPlayer()
    {
        if (_playerInstance != null)
            _playerInstance.SetActive(false);
    }

    /// <summary>
    /// Détruit le Player persistant (uniquement en cas de reset complet).
    /// </summary>
    public void DetruirePlayer()
    {
        if (_playerInstance != null)
        {
            Destroy(_playerInstance);
            _playerInstance = null;
            Debug.Log("[GameManager] Player détruit");
        }
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
