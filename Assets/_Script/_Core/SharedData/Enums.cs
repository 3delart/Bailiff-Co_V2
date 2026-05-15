// ============================================================
// Enums.cs — Bailiff & Co  V2
// Tous les enums partagés du projet.
// Les enums spécifiques à un seul système restent dans leur
// fichier (ex: ToolCategory dans OutilData.cs).
// ============================================================

public enum NiveauBruit  // Legacy alias — use NoiseLevel instead           { Silencieux, Leger, Fort, Tresfort }
public enum NoiseLevel            { Silent, Light, Loud, VeryLoud }
public enum Posture               { Stand, Crouch, Prone }
public enum AnimalEspece          { Chat, ChienCompagnie, ChienGarde, Perroquet, Poisson, Tortue, Lapin, Perruche }
public enum OwnerArchetypeType { CollectionneurFou, AncienMilitaire, StarDechu, SavantFou, InfluenceurDechu }
public enum OwnerState     { Idle, Alert, Investigate, Confront, Panic, Outdoor, Locked, Furious }
public enum TypeCachette          { DoubleFond, DerriereTableau, SousTapis, TrappePlancher, CoffreMural, ContientBanal, NainJardin, LitiereAnimal, AppareilCuisine, PieceSecrete }
public enum TypePiege             { SeauEau, FauxPlancher, ColleIndustrielle, AlarmeInfrarouge, CaisseChute, FumeeScene, ChienLache, GazSoporifique, DroneTracking }
public enum TypeVehicule          { VeloCargo, Scooter, Pickup, Ane, Fourgon, CamionGlace, Helicoptere, Remorque }
/// <summary>Vehicle rental options — applied at mission start.</summary>
public enum VehicleOptionType      { Remorque, AlarmeAntivol }

/// <summary>Active game context — used by UIManager to enable/disable panels.</summary>
public enum ContexteJeu           { Menu, Hub, Mission, Personnalisation }

public enum UIPanelType
{
    GameUI,     // HUD permanent (non bloquant)
    Overlay,    // Par-dessus gameplay (roue inventaire)
    Blocking,   // Bloque tout (pause, menus)
    Popup       // Léger (confirmations)
}

// Event nouveau
public struct OnInputStateChanged
{
    public bool Actif;
}

public struct OnContextChanged
{
    public ContexteJeu Context;
}