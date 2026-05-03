// ============================================================
// MissionListUI.cs — Bailiff & Co  V2
// Affiche la liste des missions disponibles dans le Hub.
// Instancie un MissionPrefab par mission disponible.
//
// SETUP UNITY :
//   1. Attacher ce script sur le GameObject parent du ScrollView
//      dans le panel Missions (Canvas_MissionPanel).
//   2. Assigner dans l'Inspector :
//      - _missionList  : MissionListData.asset (créer via clic droit
//                        → Create → BailiffCo/MissionListData)
//      - _conteneur    : le Transform "Content" du ScrollView
//      - _missionPrefab: MissionPrefab.prefab (Features/Mission/)
//   3. Dans chaque MissionData.asset, remplir MissionNumber (1, 2, 3…)
//
// HIÉRARCHIE CANVAS RECOMMANDÉE :
//   Canvas_MissionPanel
//   └── PanelMissions (MissionListUI.cs ici)
//       ├── Titre (TMP "Missions Disponibles")
//       ├── ScrollView
//       │   └── Viewport
//       │       └── Content  ← assigner dans _conteneur
//       └── BtnFermer
// ============================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BailiffCo.Hub;

public class MissionListUI : MonoBehaviour
{
    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("Data")]
    [Tooltip("Asset contenant la liste ordonnée de toutes les missions.")]
    [SerializeField] private MissionListData _missionList;

    [Header("UI")]
    [Tooltip("Transform 'Content' du ScrollView — parent des lignes de mission.")]
    [SerializeField] private Transform  _conteneur;

    [Tooltip("Prefab d'une ligne de mission (MissionPrefab.prefab).")]
    [SerializeField] private GameObject _missionPrefab;

    [Header("Couleurs")]
    [SerializeField] private Color _couleurDisponible = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color _couleurCompletee  = new Color(0.5f, 0.85f, 0.5f, 0.6f);
    [SerializeField] private Color _couleurVerrouillee = new Color(0.4f, 0.4f, 0.4f, 0.5f);

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
    }

    private void OnEnable()
    {
        RafraichirListe();
        // AJOUT — bloque le joueur
        EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Hub });
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void OnDisable()
    {
        // AJOUT — redonne le contrôle
        EventBus<OnContextChanged>.Raise(new OnContextChanged { Context = ContexteJeu.Mission });
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Reconstruit la liste des missions selon la progression actuelle.
    /// Appelé automatiquement à l'activation du panel.
    /// </summary>
    public void RafraichirListe()
    {
        // --- Debug ---
        Debug.Log($"[MissionListUI] _missionList: {_missionList != null} | " +
                  $"_missionPrefab: {_missionPrefab != null} | " +
                  $"_conteneur: {_conteneur != null}");

        // Vide la liste existante
        if (_conteneur != null)
        {
            foreach (Transform child in _conteneur)
                Destroy(child.gameObject);
        }

        if (_missionList == null)
        {
            Debug.LogWarning("[MissionListUI] MissionListData non assigné dans l'Inspector !");
            return;
        }

        if (_missionPrefab == null)
        {
            Debug.LogWarning("[MissionListUI] MissionPrefab non assigné dans l'Inspector !");
            return;
        }

        if (_conteneur == null)
        {
            Debug.LogWarning("[MissionListUI] Conteneur (Content du ScrollView) non assigné !");
            return;
        }

        int progression = GameManager.Instance?.DerniereMissionCompletee ?? 0;

        Debug.Log($"[MissionListUI] Progression: {progression} | " +
                  $"Total missions: {_missionList.Missions?.Length ?? 0}");

        if (_missionList.Missions == null || _missionList.Missions.Length == 0)
        {
            Debug.LogWarning("[MissionListUI] Aucune mission dans MissionListData !");
            return;
        }

        // Affiche TOUTES les missions — disponibles et verrouillées
        foreach (var mission in _missionList.Missions)
        {
            if (mission == null) continue;

            bool completee    = mission.MissionNumber <= progression;
            bool disponible   = mission.MissionNumber <= progression + 1;
            bool verrouillee  = !disponible;

            Debug.Log($"[MissionListUI] Mission '{mission.MissionName}' " +
                      $"n°{mission.MissionNumber} — " +
                      $"complétée: {completee} | dispo: {disponible}");

            GameObject go = Instantiate(_missionPrefab, _conteneur);

            // ── Remplit les textes ────────────────────────────────
            var textes = go.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);

            // Texte nom mission (index 0 dans le prefab)
            if (textes.Length >= 1)
            {
                string prefix = completee  ? "✓ " :
                                verrouillee ? "🔒 " : "";
                textes[0].text = prefix + mission.MissionName;
            }

            // Texte quota (index 1 dans le prefab)
            if (textes.Length >= 2)
            {
                textes[1].text = verrouillee
                    ? "Terminer la mission précédente"
                    : $"Quota : {mission.MinimumQuotaValue:N0} €";
            }

            // ── Couleur de fond ───────────────────────────────────
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = completee   ? _couleurCompletee  :
                            verrouillee ? _couleurVerrouillee :
                                          _couleurDisponible;
            }

            // ── Bouton Sélectionner ───────────────────────────────
            var btn = go.GetComponentInChildren<Button>(includeInactive: true);
            if (btn != null)
            {
                btn.interactable = disponible && !verrouillee;

                if (btn.interactable)
                {
                    MissionData captured = mission; // capture pour le lambda
                    btn.onClick.AddListener(() => OnMissionSelectionnee(captured));
                }

                // Texte du bouton
                var txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBtn != null)
                {
                    txtBtn.text = completee   ? "Rejouer"    :
                                  verrouillee ? "Verrouillé" :
                                                "Sélectionner";
                }
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

        Debug.Log($"[MissionListUI] Mission sélectionnée : {mission.MissionName}");
        _hubManager.SelectionnerMission(mission);
    }
}
