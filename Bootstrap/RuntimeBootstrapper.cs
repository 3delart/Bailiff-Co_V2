// ============================================================
// RuntimeBootstrapper.cs — Bailiff & Co  V2
// Peu importe la scène ouverte dans l'éditeur,
// appuyer sur Play démarre TOUJOURS depuis Bootstrap.
// Script Editor Only — inchangé par rapport à V1.
// ============================================================
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class RuntimeBootstrapper
{
    static RuntimeBootstrapper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (EditorSceneManager.GetActiveScene().name != SceneNames.BOOTSTRAP)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");
                Debug.Log("[RuntimeBootstrapper] Démarrage forcé depuis Bootstrap.");
            }
        }
    }
}
#endif
