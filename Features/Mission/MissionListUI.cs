// ============================================================
// MissionListUI.cs — Bailiff & Co  V2
// Affiche la liste des missions disponibles dans le Hub.
// panelType = Blocking dans l'Inspector.
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BailiffCo.Hub;

public class MissionListUI : UIPanel
{
    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Data")]
    [SerializeField] private MissionListData _missionList;

    [Header("UI")]
    [SerializeField] private Transform  _conteneur;
    [SerializeField] private GameObject _missionPrefab;
    [SerializeField] private Button     _btnFermer;

    [Header("Couleurs")]
    [SerializeField] private Color _couleurDisponible  = new Color(1f,   1f,    1f,   0.9f);
    [SerializeField] private Color _couleurCompletee   = new Color(0.5f, 0.85f, 0.5f, 0.6f);
    [SerializeField] private Color _couleurVerrouillee = new Color(0.4f, 0.4f,  0.4f, 0.5f);

    // ================================================================
    // ÉTAT
    // ================================================================

    private HubManager _hubManager;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _hubManager = FindObjectOfType<HubManager>();
        _btnFermer?.onClick.AddListener(Fermer);
    }

    private void OnDestroy()
    {
        _btnFermer?.onClick.RemoveAllListeners();
    }

    protected override void OnEnable()
    {
        Debug.Log($"[MissionListUI] OnEnable appelé. UIManager.Instance = {UIManager.Instance != null}");
        base.OnEnable(); // RegisterPanel → UIManager gère input + curseur
        RafraichirListe();
    }

    // OnDisable hérité de UIPanel suffit (UnregisterPanel uniquement)

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Ouvre le panel de liste des missions.
    /// </summary>
    public override void Ouvrir()
    {
        base.Ouvrir();
    }

    public void RafraichirListe()
    {
        if (_conteneur != null)
            foreach (Transform child in _conteneur)
                Destroy(child.gameObject);

        if (_missionList == null || _missionPrefab == null || _conteneur == null)
        {
            Debug.LogWarning("[MissionListUI] Références manquantes dans l'Inspector.");
            return;
        }

        int progression = GameManager.Instance?.DerniereMissionCompletee ?? 0;

        if (_missionList.Missions == null || _missionList.Missions.Length == 0)
        {
            Debug.LogWarning("[MissionListUI] Aucune mission dans MissionListData !");
            return;
        }

        foreach (var mission in _missionList.Missions)
        {
            if (mission == null) continue;

            bool completee   = mission.MissionNumber <= progression;
            bool disponible  = mission.MissionNumber <= progression + 1;
            bool verrouillee = !disponible;

            GameObject go = Instantiate(_missionPrefab, _conteneur);

            var textes = go.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);

            if (textes.Length >= 1)
            {
                string prefix = completee ? "✓ " : verrouillee ? "🔒 " : "";
                textes[0].text = prefix + mission.MissionName;
            }

            if (textes.Length >= 2)
            {
                textes[1].text = verrouillee
                    ? "Terminer la mission précédente"
                    : $"Quota : {mission.MinimumQuotaValue:N0} €";
            }

            var img = go.GetComponent<Image>();
            if (img != null)
                img.color = completee   ? _couleurCompletee  :
                            verrouillee ? _couleurVerrouillee :
                                          _couleurDisponible;

            var btn = go.GetComponentInChildren<Button>(includeInactive: true);
            if (btn != null)
            {
                btn.interactable = disponible;

                if (btn.interactable)
                {
                    MissionData captured = mission;
                    btn.onClick.AddListener(() => OnMissionSelectionnee(captured));
                }

                var txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBtn != null)
                    txtBtn.text = completee   ? "Rejouer"    :
                                  verrouillee ? "Verrouillé" :
                                                "Sélectionner";
            }
        }
    }

    // ================================================================
    // HANDLER SÉLECTION
    // ================================================================

    private void OnMissionSelectionnee(MissionData mission)
    {
        if (_hubManager == null)
        {
            Debug.LogWarning("[MissionListUI] HubManager introuvable !");
            return;
        }

        _hubManager.SelectionnerMission(mission); 
    }
}