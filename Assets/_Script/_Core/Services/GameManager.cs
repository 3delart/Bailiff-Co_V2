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

    [SerializeField] private GameObject _playerPrefab;

    private GameObject _playerInstance;

    /// <summary>Référence au Player persistant (null si pas encore créé).</summary>
    public GameObject Player => _playerInstance;

    /// <summary>Contexte actuel du jeu (Menu, Hub, Mission).</summary>
    public ContexteJeu ContexteActuel { get; set; } = ContexteJeu.Menu;

    /// <summary>Indique si le joueur peut contrôler son personnage.</summary>
    public bool InputJoueurActif { get; private set; } = true;

    /// <summary>Options de véhicule sélectionnées.</summary>
    private List<VehicleOption> _optionsSelectionnees = new();

    /// <summary>Coût total de la location incluant les options.</summary>
    private float _totalRentalPaid = 0f;

    public List<VehicleOption> OptionsSelectionnees => _optionsSelectionnees;
    public float TotalRentalPaid => _totalRentalPaid;


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
        Personnalisation = ScriptableObject.CreateInstance<PlayerConfigData>();
    }

    /// <summary>Change le contexte et notifie tous les systèmes.</summary>
    public void SetContexte(ContexteJeu nouveauContexte)
    {
        if (ContexteActuel == nouveauContexte) return;
        
        ContexteActuel = nouveauContexte;
        EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = nouveauContexte });
    }

    public void SetInputJoueurActif(bool actif)
    {
        if (InputJoueurActif == actif) return;
        InputJoueurActif = actif;
        EventBus<OnInputStateChanged>.Raise(new OnInputStateChanged { Actif = actif });
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
            // SetActive(false→true) force OnDisable→OnEnable sur tous les composants,
            // ce qui re-souscrit leurs handlers EventBus après un ClearAll() de transition.
            _playerInstance.SetActive(false);
            CharacterController cc = _playerInstance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _playerInstance.transform.position = position;
            _playerInstance.transform.rotation = rotation;
            if (cc != null) cc.enabled = true;
            _playerInstance.SetActive(true);
        }
        else
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[GameManager] _playerPrefab non assigné dans l'Inspector !");
                return;
            }
            _playerInstance = Instantiate(_playerPrefab, position, rotation);
            DontDestroyOnLoad(_playerInstance);
            _playerInstance.name = "Player_Persistent";
        }

        // AJOUT — force la MainCamera du Player comme caméra active
        Camera playerCam = _playerInstance.GetComponentInChildren<Camera>();
        if (playerCam != null)
        {
            playerCam.enabled = true;
        }
        else
        {
            Debug.LogWarning("[GameManager] Aucune caméra trouvée sur le Player !");
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

        _optionsSelectionnees.Clear();
        _totalRentalPaid = 0f;

        MissionSelectionnee = mission;
        VehiculeSelectionne = vehicule;

        string scene = string.IsNullOrEmpty(mission.SceneName)
            ? SceneNames.MISSION_LIBRE
            : mission.SceneName;

        Debug.Log($"[GameManager] Mission : {mission.MissionName} | Scène : {scene} | " +
                  $"Véhicule : {vehicule?.VehicleName ?? "aucun"}");

        SetContexte(ContexteJeu.Mission);
        SceneLoader.Instance.ChargerScene(scene);
    }

    /// <summary>
    /// Stocke la mission sans déclencher de transition.
    /// Appelé par HubManager lors de la sélection dans la liste.
    /// </summary>
    public void SetMissionSelectionnee(MissionData mission) => MissionSelectionnee = mission;

    /// <summary>
    /// Stocke les options sélectionnées et le coût total de la location.
    /// Appelé par HubManager lors de la confirmation.
    /// </summary>
    public void SetOptionsSelectionnees(List<VehicleOption> options, float totalPaid)
    {
        _optionsSelectionnees = new List<VehicleOption>(options);
        _totalRentalPaid = totalPaid;
    }

    /// <summary>
    /// Appelé par SceneLoader après le délai post-mission.
    /// Met à jour l'argent, notifie, puis retourne au Hub.
    /// </summary>
    public void TerminerMission(MissionResult resultat)
    {
        if (resultat.SalaireNet > 0f)
            Argent += resultat.SalaireNet;

        if (resultat.MissionReussie &&
            MissionSelectionnee != null &&
            MissionSelectionnee.MissionNumber > DerniereMissionCompletee)
        {
            DerniereMissionCompletee = MissionSelectionnee.MissionNumber;
        }

        MissionSelectionnee = null;
        VehiculeSelectionne = null;

        Debug.Log($"[GameManager] Mission terminée — Argent total : {Argent:N0} €");

        SetContexte(ContexteJeu.Hub);
        SceneLoader.Instance.ChargerScene(SceneNames.HUB, avecFondu: true);
    }

    // ================================================================
    // API — NAVIGATION
    // ================================================================

    public void AllerAuMenu()
    {
        SetContexte(ContexteJeu.Menu);
        SceneLoader.Instance.ChargerScene(SceneNames.MENU, avecFondu: true);
    }

    public void AllerAuHub()
    {
        SetContexte(ContexteJeu.Hub);
        SceneLoader.Instance.ChargerScene(SceneNames.HUB, avecFondu: true);
    }

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