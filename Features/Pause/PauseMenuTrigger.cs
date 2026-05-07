// ============================================================
// PauseMenuTrigger.cs — Bailiff & Co  [v2]
// Toujours actif — écoute [Echap] et ouvre/ferme le PauseMenu.
// À mettre sur un GameObject SÉPARÉ du PauseMenu, toujours actif.
//
// CHANGEMENTS v1 → v2 :
//   - Déplacé dans Features/Pause/ avec PauseMenu.cs.
//   - Logique inchangée.
//
// SETUP (dans UI_Persistent) :
//   Canvas PauseMenu (Sort Order 100)
//   ├── PauseMenuTrigger  ← CE SCRIPT ici (toujours actif)
//   └── PauseMenu         ← désactivé au départ
// ============================================================
using UnityEngine;

public class PauseMenuTrigger : MonoBehaviour
{
    [SerializeField] private PauseMenu _pauseMenu;

    private void Awake()
    {
        if (_pauseMenu == null)
            _pauseMenu = GetComponentInChildren<PauseMenu>(includeInactive: true);
    }

    private void Update()
    {
        KeyCode touchePause = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Pause)
            : KeyCode.Escape;

        if (Input.GetKeyDown(touchePause))
        {
            if (_pauseMenu == null) return;
            if (_pauseMenu.EstOuvert) _pauseMenu.Fermer();
            else                      _pauseMenu.Ouvrir();
        }
    }
}
