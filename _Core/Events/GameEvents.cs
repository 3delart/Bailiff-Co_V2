// ============================================================
// GameEvents.cs — Bailiff & Co  V2
// Définition de TOUS les événements du jeu sous forme de structs.
// Aucune logique — juste des conteneurs de données.
//
// CONVENTIONS :
//   - Tous les types Data référencés ici sont des ScriptableObjects
//     définis dans Features/*/Data/*.cs
//   - MissionResult est dans _Core/SharedData/MissionResult.cs
// ============================================================
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// OBJECTS
// ──────────────────────────────────────────────────────────────

/// <summary>An object has been loaded into the vehicle trunk.</summary>
public struct OnObjectLoaded
{
    public ObjetData Object;
    public float     Value;
    public bool      IsFragile;
}

/// <summary>An object has been damaged or broken.</summary>
public struct OnObjectDamaged
{
    public ObjetData Object;
    public float     ValueLost;
    public Vector3   Position;
}

// ──────────────────────────────────────────────────────────────
// PARANOIA
// ──────────────────────────────────────────────────────────────

/// <summary>The owner's paranoia value has changed.</summary>
public struct OnParanoiaChanged
{
    public float NewValue;      // 0–100
    public float OldValue;
    public int   NewTier;       // 0=Calm … 5=Obsessionnel
}

/// <summary>A noise source was emitted (footsteps, door, dropped object…).</summary>
public struct OnNoiseEmitted
{
    public Vector3     Position;
    public float       Range;
    public NiveauBruit Level;
    public GameObject  Source;
}

// ──────────────────────────────────────────────────────────────
// QUOTA
// ──────────────────────────────────────────────────────────────

/// <summary>The total value in the vehicle has changed.</summary>
public struct OnQuotaChanged
{
    public float TotalValue;
    public float TargetValue;
    public float Percentage;    // 0–1
}

/// <summary>The minimum quota has been reached — mission can be validated.</summary>
public struct OnQuotaReached { }

/// <summary>A percentage threshold has been crossed (e.g. 20% = owner may go outside).</summary>
public struct OnThresholdReached
{
    public float Percentage; // e.g. 0.2f for 20%
}

// ──────────────────────────────────────────────────────────────
// OWNER (PROPRIETAIRE)
// ──────────────────────────────────────────────────────────────

/// <summary>The owner's state machine state has changed.</summary>
public struct OnOwnerStateChanged
{
    public ProprietaireState OldState;
    public ProprietaireState NewState;
}

/// <summary>The owner has left the house and is heading to the vehicle.</summary>
public struct OnOwnerLeftHouse { }

/// <summary>The owner has retrieved an object from the vehicle trunk.</summary>
public struct OnOwnerRetrievedObject
{
    public ObjetData Object;
    public float     Value;
}

// ──────────────────────────────────────────────────────────────
// VEHICLE
// ──────────────────────────────────────────────────────────────

/// <summary>The vehicle is being attacked (owner or neighbour).</summary>
public struct OnVehicleAttacked
{
    public GameObject Attacker;
    public bool       IsOwner;
}

// ──────────────────────────────────────────────────────────────
// TRAPS
// ──────────────────────────────────────────────────────────────

/// <summary>A trap has been triggered.</summary>
public struct OnTrapTriggered
{
    public PiegeData  Trap;
    public Vector3    Position;
    public GameObject Victim;
}

// ──────────────────────────────────────────────────────────────
// MISSION
// ──────────────────────────────────────────────────────────────

/// <summary>A mission has started.</summary>
public struct OnMissionStarted
{
    public MissionData Mission;
    public int         Seed;
}

/// <summary>The mission has ended (quota reached, expelled, or voluntary departure).</summary>
public struct OnMissionEnded
{
    public MissionResult Result;
}

// Alias français — utilisés par PauseMenu, UIManager, InventaireWheel, etc.
// OnMissionDemarree transporte optionnellement les refs joueur pour
// que UIManager puisse appeler InventaireWheel.SetRefs() sans passer
// par MissionBuilder.OnJoueurSpawne().

/// <summary>Mission démarrée — alias français de OnMissionStarted.</summary>
public struct OnMissionDemarree
{
    public MissionData      Mission;
    public InventaireSystem Inventaire; // nullable — injecter si disponible
    public PlayerCarry      Carry;      // nullable — injecter si disponible
}

/// <summary>Mission terminée — alias français de OnMissionEnded.</summary>
public struct OnMissionTerminee
{
    public MissionResult Resultat;
}

// ──────────────────────────────────────────────────────────────
// ANIMALS
// ──────────────────────────────────────────────────────────────

/// <summary>An animal has barked or made an alert noise.</summary>
public struct OnAnimalAlerted
{
    public Vector3     Position;
    public float       Intensity;   // 0–1
    public AnimalEspece Species;
    public AnimalData  AnimalData;
}

/// <summary>The parrot has spoken a phrase.</summary>
public struct OnParrotSpoke
{
    public string Phrase;
    public bool   IsClue;
}

// ──────────────────────────────────────────────────────────────
// SCANNER
// ──────────────────────────────────────────────────────────────

/// <summary>A scan has revealed hidden objects.</summary>
public struct OnScanPerformed
{
    public Vector3     ScanPosition;
    public ObjetData[] RevealedObjects;
}

// ──────────────────────────────────────────────────────────────
// URGENCY TIMER
// ──────────────────────────────────────────────────────────────

/// <summary>The police have been called — urgency timer started.</summary>
public struct OnUrgencyTimerStarted
{
    public float DurationSeconds;
}

// ──────────────────────────────────────────────────────────────
// UI / NAVIGATION
// ──────────────────────────────────────────────────────────────

/// <summary>The player interacted with the driver door — request mission end confirmation.</summary>
public struct OnMissionEndRequested { }

/// <summary>Player's answer to the departure confirmation popup.</summary>
public struct OnDepartureConfirmed
{
    public bool Confirmed;
}

/// <summary>Triggers a fade to black — handled by SceneLoader.</summary>
public struct OnFadeToBlack
{
    public float DurationSeconds;
}

/// <summary>Fondu vers le noir — alias français de OnFadeToBlack.</summary>
public struct OnFondNoir
{
    public float DureeSecondes;
}




// ================================================================
// EVENT : Changement de label d'interaction
// Broadcasted par : PlayerInteractor (chaque frame si changement)
// Écouté par : LabelInteractionUI
// ================================================================
public struct OnInteractionLabelChanged
{
    public string Label;
}
 
// ================================================================
// EVENT : Scène chargée
// Broadcasted par : SceneLoader (après chaque LoadSceneAsync)
// Écouté par : UIManager (pour activer le bon contexte UI)
// ================================================================
public struct OnSceneChargee
{
    public string NomScene;
}

// ================================================================
// EVENTS FRANÇAIS ADDITIONNELS — aliases pour compatibilité HUDSystem
// ================================================================

/// <summary>Objet chargé dans le véhicule — alias français de OnObjectLoaded</summary>
public struct OnObjetCharge
{
    public ObjetData Objet;
    public float     Valeur;
    public bool      EstFragile;
}

/// <summary>Timer d'urgence déclenché — alias français de OnUrgencyTimerStarted</summary>
public struct OnTimerUrgenceDéclenche
{
    public float DureeSecondes;
}

/// <summary>Le perroquet a parlé — alias français (mais garder OnParrotSpoke aussi)</summary>
public struct OnPerroquetParle
{
    public string Phrase;
    public bool   EstIndice;
}