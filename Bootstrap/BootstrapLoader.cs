// ============================================================
// BootstrapLoader.cs — Bailiff & Co  V2
// Sur la scène Bootstrap (index 0 dans Build Settings).
// Attend que GameManager et SceneLoader soient initialisés,
// charge UI_Persistent en ADDITIVE (jamais déchargée),
// puis charge Menu en Single.
//
// CHANGEMENTS V2 :
//   - Charge UI_Persistent en additive avant le Menu
//   - UI_Persistent reste chargée tout au long de la session
//
// SETUP UNITY :
//   GameObject "GameManager" dans la scène Bootstrap :
//   ├── GameManager.cs
//   ├── SceneLoader.cs
//   ├── OptionsManager.cs
//   └── BootstrapLoader.cs
// ============================================================
using System.Collections;
using UnityEngine;

public class BootstrapLoader : MonoBehaviour
{
    [Header("Scène à charger après Bootstrap")]
    [SerializeField] private string _sceneDepart = SceneNames.MENU;

    [Tooltip("Délai en secondes avant de charger (0 = immédiat).")]
    [SerializeField] private float _delaiDemarrage = 0f;

    private IEnumerator Start()
    {
        // Vérifie que les singletons sont présents
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Bootstrap] GameManager introuvable !");
            yield break;
        }
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[Bootstrap] SceneLoader introuvable !");
            yield break;
        }

        yield return StartCoroutine(SceneLoader.Instance.ChargerUIPersistentAdditive());
    
        Debug.Log("[Bootstrap] UI_Persistent chargée — lancement Menu");
        
        if (_delaiDemarrage > 0f)
            yield return new WaitForSeconds(_delaiDemarrage);

        // 3 — Charger la scène de départ
        SceneLoader.Instance.ChargerScene(_sceneDepart, avecFondu: false);
    }
}
