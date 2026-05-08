# GDD MASTER — BAILIFF & CO
> **Version** : MASTER — Mai 2026  
> **Moteur** : Unity (3D, 1ère personne)  
> **Statut** : Document vivant — généré depuis analyse complète du code v5

---

## TABLE DES MATIÈRES

1. [Concept & Vision](#1-concept--vision)
2. [Univers & Narration](#2-univers--narration)
3. [Structure du Jeu](#3-structure-du-jeu)
4. [Mécaniques Fondamentales](#4-mécaniques-fondamentales)
5. [Système de Paranoïa](#5-système-de-paranoïa)
6. [Intelligence Artificielle — Propriétaire](#6-intelligence-artificielle--propriétaire)
7. [Animaux](#7-animaux)
8. [Voisins & PNJ Secondaires](#8-voisins--pnj-secondaires)
9. [Véhicule & Chargement](#9-véhicule--chargement)
10. [Objets, Outils & Inventaire](#10-objets-outils--inventaire)
11. [Interactables — Mobilier & Environnement](#11-interactables--mobilier--environnement)
12. [Cachettes & Pièges](#12-cachettes--pièges)
13. [Progression & Économie](#13-progression--économie)
14. [Hub & Méta-Jeu](#14-hub--méta-jeu)
15. [Interface Utilisateur](#15-interface-utilisateur)
16. [Architecture Technique](#16-architecture-technique)
17. [Scènes & Flux](#17-scènes--flux)
18. [Configuration & Options](#18-configuration--options)
19. [Roadmap & Features à Implémenter](#19-roadmap--features-à-implémenter)

---

## 1. CONCEPT & VISION

### Pitch
**Bailiff & Co** est un jeu de simulation d'infiltration en première personne où le joueur incarne un huissier de justice mandaté pour saisir des biens chez des particuliers. La mission : pénétrer dans une habitation, identifier et charger des objets de valeur dans le véhicule, et partir avant que le propriétaire ne soit trop alarmé — ou avant que la situation ne dégénère.

### Piliers de Design
| Pilier | Description |
|--------|-------------|
| **Tension croissante** | Chaque action monte la paranoïa du propriétaire. Le temps joue contre le joueur. |
| **Lecture de l'environnement** | Chaque maison, chaque propriétaire est différent. S'adapter est la clé. |
| **Économie du mouvement** | Bruit, vitesse, posture — chaque décision a un coût en discrétion. |
| **Rejouabilité** | Missions générées procéduralement depuis des seeds, propriétaires variés, véhicules au choix. |

### Ton
Comédie noire. Le jeu assume l'absurdité de la situation (huissier en mode infiltration) tout en maintenant une tension réelle. Les propriétaires ont une vraie personnalité — leur portrait cartoon, leurs phrases et leur comportement reflètent un archétype humain reconnaissable.

---

## 2. UNIVERS & NARRATION

### Cadre
Le joueur travaille pour l'agence **Bailiff & Co**, cabinet d'huissiers de justice spécialisés dans les saisies difficiles. Les clients (dossiers de mission) sont des personnes endettées ou condamnées par jugement. Chaque mission est un mandat légal — mais l'exécution est loin d'être propre.

### Propriétaires — Archétypes
Cinq archétypes de propriétaires existent (`ProprietaireArchetypeType`), chacun combinant différemment les trois curseurs de personnalité :

| Archétype | Réactivité | Méthode | Sociabilité | Comportement Typique |
|-----------|-----------|---------|------------|---------------------|
| Le Paranoïaque | Haute | Haute | Faible | Pièges partout, réagit au moindre son, ne demande pas d'aide |
| Le Social | Faible | Faible | Haute | Appelle vite ses amis, son avocat, peu de pièges |
| Le Méthodique | Moyenne | Haute | Moyenne | Pièges bien placés, force le coffre, agressif sur ses biens |
| Le Naïf | Faible | Faible | Faible | Peu préparé, facile à éviter mais imprévisible |
| L'Obsessionnel | Très haute | Très haute | Moyenne | La pire combinaison — tout maximisé |

### La Mission comme Dossier
Chaque mission est présentée sous forme d'un **dossier d'huissier** dans le Hub :
- Portrait cartoon du propriétaire
- Fiche de renseignements (profession, hobbies, anecdote)
- Menaces identifiées (pièges connus, animaux, voisins)
- Objets saisissables listés
- Valeur quotidienne minimale (objectif légal)

---

## 3. STRUCTURE DU JEU

### Flux Général
```
[Menu Principal]
       ↓
[Hub / Agence]
  ├─ Choisir Mission (PNJ Missions)
  ├─ Louer Véhicule (PNJ Garage)
  ├─ Acheter Outils (PNJ Boutique)
  └─ Personnalisation (PNJ Personnalisation)
       ↓
[Scène de Mission]
  ├─ Arrivée (spawn joueur + véhicule)
  ├─ Infiltration (boucle principale)
  └─ Départ (confirmation → résultat)
       ↓
[Écran de Résultats]
       ↓
[Hub] (retour automatique)
```

### Contextes de Jeu (`ContexteJeu`)
- `Menu` — écran principal, pas d'input monde
- `Hub` — libre circulation dans le hub, curseur débloqué
- `Mission` — jeu actif, curseur verrouillé, HUD actif
- `Personnalisation` — mode cosmétique

### Scènes Unity (liste exhaustive)
| Constante | Scène |
|-----------|-------|
| `BOOTSTRAP` | Scène de démarrage, charge les singletons |
| `HUD` | Overlay persistant (chargé additif) |
| `MENU` | Menu principal |
| `HUB` | Agence / hub |
| `MISSION_01` → `MISSION_10` | Missions campagne |
| `MISSION_LIBRE` | Mode bac à sable |
| `PERSONNALISATION` | Personnalisation du personnage |

---

## 4. MÉCANIQUES FONDAMENTALES

### 4.1 Mouvement du Joueur (`PlayerController`)

Le joueur est en **première personne** avec un `CharacterController` Unity.

#### Postures
| Posture | Hauteur CC | Hauteur caméra | Vitesse |
|---------|-----------|----------------|---------|
| Debout | Normal (config) | Normal (config) | NormalSpeed |
| Accroupi | Crouch (config) | Crouch (config) | CrouchSpeed |
| Allongé | Prone (config) | Prone (config) | ProneSpeed |
| Sprint | Normal | Normal | SprintSpeed |

- Transitions fluides via lerp (vitesse `HeightChangeSpeed`)
- Coyote Time : 0.15 s après avoir quitté le sol pour sauter

#### Contrôles par défaut
| Action | Touche (défaut) |
|--------|----------------|
| Avancer/Reculer/Gauche/Droite | WASD |
| Sprint | Shift |
| Accroupi | Ctrl |
| Allongé | Alt |
| Saut | Espace |
| Interagir | E |
| Inventaire (roue) | Tab (maintenu) |
| Poser objet | G |
| Jeter | Clic droit |
| Pause | Échap |

Toutes les touches sont **reconfigurables** via `OptionsData.SetTouche()`.

### 4.2 Bruit — Le Cœur de la Discrétion (`PlayerNoiseEmitter`)

Chaque action du joueur génère un événement `OnNoiseEmitted` avec un `NiveauBruit` et une portée.

#### Niveaux de Bruit
| Niveau | Valeur | Déclencheur typique |
|--------|--------|---------------------|
| `Silencieux` | 0 | Accroupi/allongé immobile |
| `Leger` | 1 | Marche normale, ouverture douce |
| `Fort` | 2 | Sprint, meuble poussé, chute |
| `Tresfort` | 3 | Forçage de porte, lancer d'objet lourd, atterrissage brutal |

**Throttling :** Bruit léger max 1/300 ms, bruit fort max 1/100 ms (anti-spam).

#### Sources de Bruit
- **Déplacement** : marche → Leger, sprint → Fort, terrain variable
- **Meubles poussés** (`FurnitureInteractable`) : dépend du revêtement sol
  - Parquet > Carrelage > Moquette (portée décroissante)
- **Portes/tiroirs** : Normal → Leger, grincement → Fort, forçage → Tresfort
- **Objets** : chute proportionnelle à l'impact (`ObjetData.DropNoiseLevel`)
- **Véhicule** : bruits d'ambiance périodiques (`VehicleAmbiance`)
- **Animaux** : alertes périodiques si détection joueur

### 4.3 Portage & Lancer d'Objets (`PlayerCarry`)

- **Saisir** : E sur un `ValueObject` — objet devient cinématique, passe sur layer "ObjetPorte"
- **Poser** (G) : dépose doucement, pas de bruit
- **Lancer** (clic droit) : vitesse = `BaseThrowVelocity / masse`, clampée [Min, Max]
- Un seul objet porté à la fois (sauf capacité étendue par outil)
- Objets `RequiresTwoPlayers` : réservés multijoueur futur ou mécaniques spéciales

### 4.4 Interaction (`PlayerInteractor`)

Le joueur raycast en permanence depuis la caméra :
1. Si `IInteractable` en portée → affiche label `[E] Action`
2. **E appuyé** → `Interact()` sur la cible
3. **E maintenu** sur `FurnitureInteractable` (non porte/tiroir) → pousser le meuble
4. La poussée est bloquée si le joueur porte un objet

---

## 5. SYSTÈME DE PARANOÏA (`ParanoiaSystem`)

La **paranoïa** est la jauge centrale du jeu. Elle mesure l'état mental du propriétaire de 0 à 100 et détermine son comportement.

### Paliers de Paranoïa

| Palier | Valeur | Nom | Comportement IA |
|--------|--------|-----|-----------------|
| 0 | 0–10 | **Calme** | Routine normale, patrouille |
| 1 | 11–25 | **Méfiant** | S'arrête, écoute, s'oriente vers les bruits |
| 2 | 26–50 | **Inquiet** | Enquête activement, vérifie les pièces |
| 3 | 51–75 | **Panique** | Confronte le joueur, peut appeler l'avocat |
| 4 | 76–90 | **Furieux** | Sort pour forcer le coffre, place des pièges dynamiques |
| 5 | 91–100 | **Obsessionnel** | État maximal, tout est déclenché |

### Sources de Paranoïa
| Source | Delta |
|--------|-------|
| Bruit Léger entendu | +3 |
| Bruit Fort entendu | +12 |
| Bruit Très Fort entendu | +25 |
| Objet chargé dans coffre | +Clamp(valeur/5000, 3, 15) |
| Piège déclenché | +valeur du PiegeData |
| Animal alerté | +AnimalData.ParanoiaBonusPerAlert |
| Joueur visible | Arrête la décroissance |

### Décroissance Passive
- Délai avant décroissance : configurable (`ParanoiaConfig.DelayBeforeDecay`, défaut ~10 s)
- Taux : configurable (`ParanoiaConfig.DecayRatePerSecond`, défaut ~0.08/s ≈ 5 pts/minute)
- **Stoppée** tant que le propriétaire voit le joueur

### Événements Associés
- `OnParanoiaChanged` → HUD mis à jour, IA change d'état
- `OnThresholdReached(20%)` → propriétaire sort inspecter le véhicule
- `OnThresholdReached(80%)` → propriétaire passe en état Furieux

---

## 6. INTELLIGENCE ARTIFICIELLE — PROPRIÉTAIRE (`ProprietaireAI`)

### Machine à États (8 états)

```
             ┌──────────┐
             │   IDLE   │◄────────────────────────────────┐
             └────┬─────┘                                 │
                  │ Bruit entendu / objet chargé          │ Retour si calme
                  ▼                                       │
            ┌──────────┐                                  │
            │  ALERT   │                                  │
            └────┬─────┘                                  │
                 │ Palier ≥ 2                             │
                 ▼                                        │
          ┌─────────────┐                                 │
          │ INVESTIGATE │─────────────────────────────────┘
          └─────┬───────┘
                │ Joueur détecté / Palier ≥ 3
                ▼
          ┌──────────────┐
          │   CONFRONT   │ (demande mandat, non-violent)
          └──────┬───────┘
                 │ Palier ≥ 3
                 ▼
            ┌─────────┐
            │  PANIC  │ ──► Appelle avocat (Sociabilité ≥ 7)
            └────┬────┘ ──► Appelle amis
                 │
          ┌──────┴───────┐
          │              │
          ▼              ▼
      ┌────────┐    ┌─────────┐
      │OUTDOOR │    │ FURIOUS │
      └────────┘    └─────────┘
      (Force coffre)  (Pièges dynamiques)
          
     ┌────────┐
     │ LOCKED │ (immobilisé par outil — 60 s)
     └────────┘
```

### Paramètres IA depuis ProprietaireData

Les **trois curseurs** (1–10) génèrent automatiquement tous les paramètres IA :

#### Réactivité (Reactivity)
- `StartingParanoia` = Lerp(0, 30, reactivity/10)
- `NormalSpeed` = 2.0 + (reactivity * 0.1) m/s
- `PanicSpeed` = 3.5 + (reactivity * 0.2) m/s
- `VisionRange`, `HearingBonus`, `VisionAngle`

#### Méthode (Method)
- `PreplacedTrapsCount` → pièges posés avant la mission
- `MaxDynamicTrapsCount` → pièges placés pendant la mission (état Furieux)
- `VehicleForceDuration` → durée pour forcer le coffre

#### Sociabilité (Sociability)
- `AutoCallsLawyer` (bool) = sociability ≥ 7
- `LawyerCallThreshold` (paranoia) = 90 - (sociability * 3)
- `FriendsCount` = max(0, sociability - 4)
- `FriendsArrivalDelay` = 180 - (sociability * 10) secondes

### Détection Visuelle
- Cône de vision : angle + portée depuis `ProprietaireData`
- Raycast (line of sight) vers le joueur
- Coyote time 0.2 s (anti-scintillement)
- La détection arrête la décroissance de paranoïa

### Sons du Propriétaire
- `SpotPlayerSounds[]` — entend/voit le joueur
- `ConfrontSounds[]` — confrontation
- `PanicSounds[]` — panique

---

## 7. ANIMAUX (`AnimalData`)

### Espèces Disponibles (enum `AnimalEspece`)
Chat, Chien, Perroquet, Lapin, Hamster, Poule, Canari, Tortue

### Comportements par Capacité

| Capacité | Paramètre | Effet |
|----------|-----------|-------|
| Détection son | `ReactsToSound`, `MinNoiseLevel` | Réagit aux bruits ≥ niveau |
| Détection vue | `UseLineOfSight`, `VisionAngle` | Cône de vision |
| Alerte | `CanMakeNoise`, `AlertNoiseLevel` | Émet `OnAnimalAlerted` → paranoïa |
| Mouvement | `CanMove`, `PatrolSpeed` | Patrouille la zone |
| Poursuite | `ChasesPlayer`, `ChaseSpeed` | Suit le joueur |
| Cage | `CanBeCaged` | Peut être mis en cage dans le véhicule |
| Libération | `CanBeReleasedByOwner` | Chien de garde lâché par le proprio |
| Perroquet | `CanSpeak`, `Phrases[]` | Phrases aléatoires, certaines sont des indices |

### Parrot Phrase System
Chaque `ParrotPhrase` a :
- `Text` — texte affiché/dit
- `IsClue` — indice sur le proprio (emplacement de valeur, code, etc.)
- `Weight` (1–10) — probabilité relative

### Impact Cage
Mettre un animal en cage dans le véhicule → `OnOwnerNoticeCagedParanoia` (paranoïa bonus si le proprio remarque)

---

## 8. VOISINS & PNJ SECONDAIRES (`NeighbourData`)

### Rôles
| Rôle | Déclencheur | Comportement |
|------|------------|--------------|
| **Ami** | `OnOwnerCall` (Sociabilité ≥ 5) | Renforce le proprio, bonus paranoïa si voit joueur |
| **Avocat** | `OnOwnerCall` (Sociabilité ≥ 7) | Bloque le départ du joueur un temps (`LawyerBlockDuration`) |
| **Curieux** | `OnMissionStart` / seuil | Observe, peut alerter le proprio |
| **Voleur** | `OnTimerExpiry` | Force le coffre, vole TOUT si assez de temps |
| **Livreur** | Aléatoire | Sonne à la porte, réveille le proprio |

### Déclencheurs (`NeighbourTrigger`)
- `OnMissionStart` — présent dès le début
- `OnOwnerCall` — le proprio appelle
- `OnQuotaThreshold` — seuil de valeur atteint
- `OnTimerExpiry` — minuterie mission expirée

---

## 9. VÉHICULE & CHARGEMENT (`VehicleRuntime`)

### Types de Véhicules (`TypeVehicule`)
8 types (camionnette, break, berline, cargo vélo, etc.) avec différentes capacités coffre.

### Interaction Multi-Collider
Le véhicule expose **3 colliders distincts** :
| Collider | Action |
|----------|--------|
| `TrunkDoor` | Ouvrir/fermer le coffre |
| `TrunkZone` | Zone trigger (actif quand ouvert) — objet entrant = chargé |
| `DriverDoor` | Déclenche la confirmation de départ |
| `CageCollider` | Cage animaux (si `HasAnimalCage`) |

### Flux de Chargement
1. Joueur ouvre le coffre (E sur `TrunkDoor`)
2. Zone trigger activée
3. Joueur dépose objet dans la zone → `OnObjectEnteredTrunk` → `QuotaSystem.OnObjectLoaded()`
4. Joueur monte dans le véhicule (E sur `DriverDoor`)
5. Popup confirmation → `OnDepartureConfirmed` → objets convertis en quota final

### Anti-Vol (`ActivateAntiTheft`)
- Activé par outil dédié
- Bloque le vol par le proprio et le voleur voisin
- Ne verrouille PAS le coffre pour le joueur

### Forçage Propriétaire
- Déclenché à 20% de quota atteint (état Outdoor)
- Durée = `ProprietaireData.VehicleForceDuration` (issu du curseur Méthode)
- Si réussit : vole un objet aléatoire (`TakeRandom()`)

---

## 10. OBJETS, OUTILS & INVENTAIRE

### 10.1 Objets Saisissables (`ObjetData`)

Chaque objet a :
- `ValueMin/Max` — valeur randomisée à la spawn
- `WeightKg` — affecte la vitesse de lancer et le portage
- `IsFragile` — peut être cassé (réduction de valeur)
- `RequiresTwoPlayers` — futur multijoueur
- `IsOversized` — encombre davantage le coffre
- `DropNoiseLevel/Range` — bruit à l'impact

#### Système de Dommages
| Seuil | Multiplicateur | Description |
|-------|--------------|-------------|
| < `DamageImpactThreshold` | x1.0 | Intact |
| [Impact, Major) | `MinorDamageMultiplier` | Légèrement abîmé |
| ≥ `MajorDamageThreshold` | `MajorDamageMultiplier` | Sérieusement endommagé |

#### Scan
- `FullNameAfterScan` — nom complet révélé après scan
- `EditionYear` — info bonus (valeur émotionnelle / réelle)

### 10.2 Outils (`OutilData`)

#### Catégories (`ToolCategory`)
| Catégorie | Exemples |
|-----------|---------|
| `ForceEntry` | Pied-de-biche, perceuse |
| `Stealth` | Huile de porte, gants |
| `Legal` | Badge, téléphone |
| `Scanner` | UV, Rayon-X |
| `Immobiliser` | Menottes, spray paralysant |
| `Consumable` | Consommables à usage unique |
| `Utility` | Lampe torche, etc. |

#### Niveaux d'Outil (jusqu'à 3 niveaux)
Chaque `ToolLevel` définit :
- `EffectDuration`, `Power`, `ParanoiaModifier`
- `PlayerSpeedMultiplier`, `Charges`, `Cooldown`

#### Effets d'Outil (`ToolEffectType`)
| Effet | Description |
|-------|-------------|
| `ForceDoor` | Ouvre portes verrouillées |
| `Lockpick` | Crochetage discret |
| `ReduceParanoia` | Réduit directement la paranoïa |
| `ImmobiliseOwner` | Menottes — proprio immobile 60 s |
| `ScanUV` | Révèle pièges/cachettes UV |
| `ScanXRay` | Révèle cachettes X-ray |
| `SprayAnimal` | Neutralise animal |
| `SpraySilence` | Silence les grincements |
| `ExpandCarryCapacity` | +1 objet portables |

#### Outils de Départ (toujours disponibles, Niveau 0)
- **Badge d'huissier** — légitimation (usage IA future)
- **Téléphone** — communication (usage IA future)

### 10.3 Inventaire Roue (`InventaireWheelSystem`)

Interface radiale à **9 slots**, activée par Tab maintenu :

```
        [Outil N]
           ↑
[Outil O] ←  → [Outil E]
           ↓
        [Outil S]

    (diagonales = consommables)
         [Centre = mains vides]
```

- Zone morte centrale 40 px (retour aux mains)
- Sélection à la souris virtuelle, appliquée au relâchement de Tab
- Outils → équiper ; Consommables → utiliser immédiatement

---

## 11. INTERACTABLES — MOBILIER & ENVIRONNEMENT

### Hiérarchie
```
IInteractable (interface)
    └─ FurnitureInteractable (abstract MonoBehaviour)
           ├─ OpenableInteractable  (portes, fenêtres)
           └─ DrawerInteractable    (tiroirs avec objets)
```

### Portes & Fenêtres (`OpenableInteractable`)

#### Modes d'Animation
| Mode | Description |
|------|-------------|
| `Rotation` | Porte classique (angle sur axe local) |
| `TranslationVertical` | Fenêtre à guillotine |
| `TranslationHorizontal` | Porte coulissante |

#### États
| État | Label | Action joueur |
|------|-------|---------------|
| `Closed` | "Ouvrir la [nom]" | Ouvre → animation |
| `Open` | "Fermer la [nom]" | Ferme → animation |
| `Locked` | "Forcer la [nom]" | Requiert outil ForceEntry/Lockpick |
| `Blocked` | Verrouillé programmatiquement | Non interactable |

- Grincement : 40% de chance à l'ouverture/fermeture (`_squeaks = true`)
- Forçage : bruit `Tresfort`, requiert pied-de-biche dans `InventaireSystem`

### Tiroirs (`DrawerInteractable`)
- Contiennent des `ValueObject` qui tombent à l'ouverture
- Objets cinématiques (suivent le tiroir) quand fermés
- Zone trigger activée seulement quand ouvert
- Objects ramassés ne se re-parentent pas à la fermeture

### Pousser les Meubles (`FurnitureInteractable`)
- Tout meuble peut être poussé (E maintenu)
- Ralentit le joueur : multiplicateur = `1 / (1 + masse/20)`
- Génère du bruit de glissement selon le revêtement détecté par raycast
- Mouvement XZ uniquement (Y bloqué)

---

## 12. CACHETTES & PIÈGES

### Cachettes (`CachetteData`, `TypeCachette`)
10 types de cachettes (armoire, sous-lit, cave, etc.)

| Propriété | Effet |
|-----------|-------|
| `OwnerDetectionChance` | % que le proprio fouille la cachette |
| `RevealedByUVScan` | Visible au scan UV |
| `RequiresXRayScan` | Requiert Rayon-X pour confirmer |
| `MakesNoiseOnOpen` | Bruit d'entrée/sortie |
| `MaxObjectCapacity` | Objets cachés dedans |
| `IsMovable` | Peut être déplacée |

### Pièges (`PiegeData`, `TypePiege`)
9 types (fil tendu, alarme, piège à ours, etc.)

| Propriété | Effet |
|-----------|-------|
| `ParanoiaBonusOnTrigger` | Paranoïa ajoutée au déclenchement |
| `AlertsNeighbours` | Active les voisins |
| `AlertsPolice` | Appelle la police (délai `PoliceArrivalTime`) |
| `PlayerSpeedMultiplier` | Ralentit le joueur |
| `ImmobilisesPlayer` | Joueur bloqué |
| `ForcesDrop` | Joueur lâche l'objet porté |
| `ObscuresVision` | Brouille la vision |
| `DisarmToolName` | Outil requis pour désamorcer |
| `UVIndicatorPrefab` | Indicateur UV visible au scanner |

#### Placement
- Pièges **pré-placés** (avant mission) : count = `ProprietaireData.PreplacedTrapsCount`
- Pièges **dynamiques** (pendant mission, état Furieux) : count = `MaxDynamicTrapsCount`

---

## 13. PROGRESSION & ÉCONOMIE

### Scoring par Mission (`MissionSystem.CalculateStars`)

| Étoiles | Conditions |
|---------|-----------|
| ⭐⭐⭐ | Valeur ≥ 2× quota + 0 objet cassé + 0 piège déclenché |
| ⭐⭐ | Valeur ≥ 1.5× quota + cassés ≤ seuil 2★ |
| ⭐ | Quota atteint (valeur ≥ minimum) |
| 0 | Quota non atteint |

Bonus de temps : `BonusTimeThresholdSeconds` (bonus si terminé avant ce seuil).

### Commission Agence
L'agence prélève **15%** sur la valeur récupérée.  
`ArgentGagne = ValeurTotaleRecuperee × 0.85`

### Dépenses
| Poste | Coût |
|-------|------|
| Location véhicule | `VehiculeData.RentalPrice` (0 = gratuit) |
| Achat outil | `OutilData.PurchasePrice` |
| Upgrade outil | `ToolLevel.UpgradeCost` |

### Déblocages
- Véhicules : `UnlocksAfterMission` (numéro de mission)
- Outils : `UnlocksAfterMission`
- Missions : débloquées séquentiellement via `MissionListData.GetMissionsDisponibles()`

---

## 14. HUB & MÉTA-JEU

### PNJ du Hub (`HubPNJ` / `PNJSystem`)

| PNJ | Panneau ouvert | Condition de déblocage |
|-----|---------------|----------------------|
| Agent Missions | Liste des missions | Toujours dispo |
| Mécanicien (Garage) | Sélection véhicule | Dès mission 1 |
| Fournisseur (Boutique) | Achat/upgrade outils | Configurable |
| Archiviste | Archives de dossiers | Configurable |
| Tailleur (Personnalisation) | Cosmétiques | Configurable |

### Flux de Sélection Mission + Véhicule
1. Joueur parle au PNJ Missions → `MissionListUI` → choisit mission
2. Joueur parle au PNJ Garage → `VehiclePanelUI` → choisit véhicule
3. Confirmation → vérifie fonds → déduit location → `GameManager.LancerMission()`

### Spawn Hub
- **Premier passage** : spawn point "départ"
- **Retour de mission** : spawn point "retour" (sortie du véhicule)

---

## 15. INTERFACE UTILISATEUR

### Architecture UI

```
UIManager (singleton persistant)
    ├─ Panels enregistrés (via UIPanel.RegisterPanel)
    │       ├─ MissionListUI
    │       ├─ MissionPanelUI
    │       ├─ VehiclePanelUI
    │       ├─ PauseMenu
    │       └─ OptionsUI
    ├─ HUDSystem (mission uniquement)
    └─ LabelInteractionUI (crosshair label)
```

### HUD Mission (`HUDSystem`)
- **Jauge Quota** : valeur actuelle / valeur cible
- **Jauge Paranoïa** : 0–100 avec couleurs par palier
- **Chronomètre** : temps écoulé
- **Indicateur état propriétaire** : icône état IA courant
- **Label interaction** : "[E] Ouvrir la porte" — mis à jour en temps réel

### Panels Hub
| Panel | Contenu |
|-------|---------|
| `MissionListUI` | Liste missions disponibles, statut complétion |
| `MissionPanelUI` | Détail mission : propriétaire, menaces, objets, quota |
| `VehiclePanelUI` | Détail véhicule : capacité, avantages, tarif |

### Menu Pause (`PauseMenu`)
- Reprendre / Paramètres / Quitter
- Gèle le temps (`Time.timeScale = 0`)

### Options (`OptionsUI` + `OptionsManager`)
| Catégorie | Paramètres |
|-----------|-----------|
| Vidéo | Résolution, plein écran, qualité, VSync, limite FPS |
| Audio | Volume maître, musique, SFX, ambiance |
| Souris | Sensibilité, inverser Y |
| Touches | Reconfiguration complète (12 actions) |

---

## 16. ARCHITECTURE TECHNIQUE

### Pattern Event Bus (`EventBus<T>`)

Communication inter-systèmes **sans référence directe** :
```csharp
// Publication
EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted { Level = NiveauBruit.Fort, Range = 5f });

// Abonnement
EventBus<OnNoiseEmitted>.Subscribe(OnNoise);

// Nettoyage auto (EventBusHelper.ClearAll() à chaque chargement de scène)
```

### Événements du Jeu (`GameEvents`)
40+ événements couvrant :

| Catégorie | Événements clés |
|-----------|----------------|
| Mission | `OnMissionStarted`, `OnMissionEnded`, `OnMissionEndRequested`, `OnDepartureConfirmed` |
| Quota | `OnObjectLoaded`, `OnQuotaChanged`, `OnQuotaReached`, `OnThresholdReached` |
| Paranoïa | `OnParanoiaChanged`, `OnOwnerStateChanged` |
| Bruit | `OnNoiseEmitted` |
| Pièges | `OnTrapTriggered` |
| Animaux | `OnAnimalAlerted` |
| UI | `OnInteractionLabelChanged`, `OnFondNoir`, `OnMissionTerminee` |
| Input | `OnInputStateChanged`, `OnContextChanged` |

### Singletons Persistants (DontDestroyOnLoad)

| Singleton | Rôle |
|-----------|------|
| `GameManager` | État global : mission, véhicule, argent, joueur |
| `SceneLoader` | Transitions avec fondu, chargement additif HUD |
| `OptionsManager` | Paramètres utilisateur, rebind, audio |
| `UIManager` | Coordination des panels UI |

### Data-Driven via ScriptableObjects

Tous les paramètres gameplay sont dans des assets SO :

| Asset | Contenu |
|-------|---------|
| `ProprietaireData` | Profil proprio → génère toute l'IA |
| `MissionData` | Configuration complète d'une mission |
| `MissionListData` | Campagne ordonnée |
| `VehiculeData` | Stats véhicule + coffre |
| `ObjetData` | Stats objet saisissable |
| `OutilData` | Stats outil + niveaux |
| `CachetteData` | Stats cachette |
| `PiegeData` | Stats piège |
| `AnimalData` | Stats animal |
| `NeighbourData` | Stats voisin |
| `PlayerConfigData` | Paramètres joueur (vitesse, caméra, portage) |
| `ParanoiaConfig` | Tweaks paranoïa (facultatif) |
| `FurnitureConfig` | Tweaks mobilier/bruit (facultatif) |

### Construction Procédurale de Mission (`MissionBuilderSystem`)

À chaque mission :
1. `SpawnHouse()` — instancie `HousePrefab` à l'ancre scène
2. `SpawnVehicle()` — instancie le véhicule sélectionné
3. `SpawnOwner()` — trouve tag "OwnerSpawn" dans la maison
4. `SpawnObjects()` — seed → shuffle des SpawnPoints → instancie objets selon `SeizableObjectEntry[]`
5. `SpawnPlayer()` — repositionne le joueur persistant
6. `ApplyAmbiance()` — skybox, brouillard, musique

**Seed reproductibilité** : `FixedSeed` dans `MissionData` (0 = aléatoire). Même seed = même disposition d'objets.

### Bootstrap Startup
```
Bootstrap Scene (démarre toujours en premier)
    ↓ BootstrapLoader (délai configurable)
    ↓ Charge HUD additif (SceneLoader.ChargerHUDAdditive)
    ↓ Charge scène de départ (MENU par défaut)
```

`RuntimeBootstrapper` (Editor only) : force le démarrage depuis Bootstrap quel que soit le contexte Play Mode.

---

## 17. SCÈNES & FLUX

### Transitions
- Toutes les transitions passent par `SceneLoader.ChargerScene()`
- Fondu noir avant/après
- `EventBusHelper.ClearAll()` systématique avant chargement

### Délai Post-Mission
Après `OnMissionEnded` : SceneLoader attend **3 secondes** (écran résultats) puis charge le Hub.

---

## 18. CONFIGURATION & OPTIONS

### Touches Reconfigurables (`ActionJeu`)
Avancer, Reculer, Gauche, Droite, Interagir, Sprint, Accroupi, Allongé, Saut, Inventaire, Pause, Poser, Jeter

### Persistance
- Options sauvegardées en **JSON dans PlayerPrefs** (clé `"BailiffCo_Options"`)
- `OptionsRepository.Charger()` / `Sauvegarder()` / `Reset()`

---

## 19. ROADMAP & FEATURES À IMPLÉMENTER

> Cette section liste les features identifiées dans le code comme **todo / partiellement implémentées**.

### Critique (Core Loop Incomplet)

| Feature | État | Notes code |
|---------|------|-----------|
| **Pièges dynamiques (Furieux)** | Placeholder | `ProprietaireAI` état Furieux marqué "todo in V2" |
| **Badge / Téléphone (outils départ)** | Données OK, logique manquante | `InventaireSystem` les injecte mais sans effet IA |
| **Scan UV/Rayon-X** | Data défini | `ToolEffectType.ScanUV/XRay` non câblé |
| **SaveSystem** | Absent | `HubPNJ.Debloquer()` prévu pour un futur SaveSystem |
| **Voisins** | Data complet | `NeighbourData` riche mais `NeighbourAI` non trouvé |
| **Cachettes (usage joueur)** | Data complet | Interaction "se cacher" non implémentée |

### Haute Priorité

| Feature | État | Notes |
|---------|------|-------|
| **Police** (alertée par pièges) | Data prévu | `PiegeData.AlertsPolice`, `PoliceArrivalTime` |
| **Dommages objets en jeu** | Stat calculée | Score tient compte des cassés, mais feedback visuel ? |
| **Scan objets** (UV reveal full name) | Data OK | `ObjetData.FullNameAfterScan` — mécanisme non câblé |
| **Avocat (gameplay)** | Data OK | `LawyerBlockDuration` mais comportement réel ? |
| **Multijoueur** | Champs prévus | `RequiresTwoPlayers`, `FriendsCount` |

### Confort & Polish

| Feature | État |
|---------|------|
| Personnalisation joueur | Scène prévue (`PERSONNALISATION`), `PlayerConfigData` |
| Archives / Dossiers consultables | PNJ Archiviste prévu |
| Mode Mission Libre | Scène `MISSION_LIBRE` prévue |
| Résolution plein écran dynamique | `OptionsManager.Resolutions[]` OK |
| Musique par mission | `MissionData.MissionMusic` + `MusicVolume` |
| Ambiance sonore véhicule | `VehicleAmbiance` implémenté (jingle, braiment...) |

### Missions Planifiées
| Mission | Numéro | Statut |
|---------|--------|--------|
| Campagne | MISSION_01 → MISSION_10 | Scènes prévues, assets non tous créés |
| Mode Libre | MISSION_LIBRE | Scène prévue |

---

## ANNEXES

### A. Tags Unity Utilisés
| Tag | Usage |
|-----|-------|
| `"OwnerSpawn"` | Position spawn propriétaire dans la maison |
| `"PlayerSpawn"` | Position spawn joueur |
| `"ObjectSpawn"` | Points de spawn d'objets saisissables |

### B. Layers Unity Utilisés
| Layer | Usage |
|-------|-------|
| `"ObjetPorte"` | Objet tenu par le joueur (collision désactivée avec joueur) |
| `Interactable` (LayerMask) | Filtrage raycast interaction |

### C. Formules Clés
```
VehicleForceDuration  = f(Method)
VisionRange           = 5 + (Reactivity * 0.5)
HearingBonus          = (Reactivity - 1) * 0.5  [m ajoutés à la portée]
LawyerCallThreshold   = 90 - (Sociability * 3)
FriendsArrivalDelay   = 180 - (Sociability * 10)  [secondes]
SpeedMultiplier meuble = 1 / (1 + massKg/20)
ThrowVelocity         = BaseThrow / massKg  [clampé Min–Max]
Commission agence     = ValeurBrute × 0.85  (15% prélevés)
```

### D. Structure des Dossiers Scripts
```
Assets/_Script/
├── _Core/
│   ├── Enums.cs              — Tous les enums partagés
│   ├── EventBus.cs           — Pattern pub/sub générique
│   ├── EventBusHelper.cs     — Nettoyage auto des handlers
│   ├── GameEvents.cs         — 40+ définitions d'événements
│   ├── GameManager.cs        — Singleton persistant (état global)
│   ├── SceneLoader.cs        — Transitions + fondu
│   ├── SceneNames.cs         — Constantes noms de scènes
│   ├── BootstrapLoader.cs    — Orchestrateur démarrage
│   └── RuntimeBootstrapper.cs— Bootstrap auto en éditeur
└── Features/
    ├── Data/                 — 16 ScriptableObjects de config
    ├── Player/               — Controller, Carry, Interactor, Noise, Animator
    ├── Systems/              — Mission, Quota, Paranoia, Inventaire, IA, Builder
    ├── Vehicule/             — Runtime, TrunkZone, Ambiance, HubSlot
    ├── Mission/              — MissionResult
    ├── Hub/                  — HubManager, PNJSystem
    └── Interface/            — UIManager, UIPanel + tous les panels
```

---

*Document généré le 07/05/2026 depuis analyse complète du code source BailifCo_v5.*  
*Maintenir ce document à jour à chaque sprint majeur.*
