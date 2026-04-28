// ============================================================
// SceneNames.cs — Bailiff & Co  V2
// Constantes pour tous les noms de scènes du projet.
// Modifier ici = modifier partout. Jamais de string en dur.
//
// CHANGEMENTS V2 :
//   - Ajout de UI_PERSISTENT (scène additive permanente)
//   - Suppression de MISSION_TEST (remplacé par Mission + MissionDef)
// ============================================================
public static class SceneNames
{
    public const string BOOTSTRAP        = "Bootstrap";
    public const string UI_PERSISTENT    = "UI_Persistent";   // ← NOUVEAU chargée en additive
    public const string MENU             = "Menu";
    public const string HUB              = "Hub";
    public const string MISSION          = "Mission";          // 1 seule scène mission procédurale
    public const string PERSONNALISATION = "CharacterCustomization";

    // Ajouter les scènes Maison comme constantes si besoin de les référencer
    // (mais elles sont des prefabs, pas des scènes — voir MissionBuilder)
}
