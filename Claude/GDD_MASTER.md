# BAILIFF CO — Game Design Document
**Version 8.0 — Mai 2026**
*Document de référence exhaustif — État réel du projet + tout ce qui est prévu*

---

## Sommaire

### [PARTIE 1 — VISION & CONCEPT](#partie-1--vision--concept)
- [1.1 Pitch](#11-pitch)
- [1.2 Ton & Atmosphère](#12-ton--atmosphère)
- [1.3 Analyse de Marché & Positionnement](#13-analyse-de-marché--positionnement)
- [1.4 Piliers de Design](#14-piliers-de-design)
- [1.5 Boucle de Jeu Principale](#15-boucle-de-jeu-principale)
- [1.6 Structure de Progression](#16-structure-de-progression)

### [PARTIE 2 — MÉCANIQUES DE JEU](#partie-2--mécaniques-de-jeu)
- [2.1 Joueur](#21-joueur)
- [2.2 Système de Bruit](#22-système-de-bruit)
- [2.3 Système de Paranoïa](#23-système-de-paranoïa)
- [2.4 IA Propriétaire](#24-ia-propriétaire)
- [2.5 Véhicule & Coffre](#25-véhicule--coffre)
- [2.6 Objets Saisissables](#26-objets-saisissables)
- [2.7 Outils & Consommables](#27-outils--consommables)
- [2.8 Interactables Environnement](#28-interactables-environnement)
- [2.9 Cachettes & Pièges](#29-cachettes--pièges)
- [2.10 Animaux](#210-animaux)
- [2.11 PNJ Secondaires — Voisins](#211-pnj-secondaires--voisins)
- [2.12 Scènes Virales](#212-scènes-virales)

### [PARTIE 3 — PROGRESSION & ÉCONOMIE](#partie-3--progression--économie)
- [3.1 Structure des Missions](#31-structure-des-missions)
- [3.2 Système de Quota](#32-système-de-quota)
- [3.3 Notation par Étoiles](#33-notation-par-étoiles)
- [3.4 Bulletin de Paie — Système Économique](#34-bulletin-de-paie--système-économique)
- [3.5 Hub & Méta-Jeu](#35-hub--méta-jeu)
- [3.6 Coopératif](#36-coopératif)

### [PARTIE 4 — INTERFACE UTILISATEUR](#partie-4--interface-utilisateur)
- [4.1 Architecture UI](#41-architecture-ui)
- [4.2 HUD Mission](#42-hud-mission)
- [4.3 Panels par Contexte](#43-panels-par-contexte)
- [4.4 Options & Rebinding](#44-options--rebinding)
- [4.5 Personnalisation du Personnage](#45-personnalisation-du-personnage)

### [PARTIE 5 — ARCHITECTURE TECHNIQUE](#partie-5--architecture-technique)
- [5.1 Structure des Dossiers](#51-structure-des-dossiers)
- [5.2 Scènes & Flux](#52-scènes--flux)
- [5.3 Services Fondamentaux](#53-services-fondamentaux)
- [5.4 Système d'Événements](#54-système-dévénements)
- [5.5 Données — ScriptableObjects](#55-données--scriptableobjects)
- [5.6 Patterns Architecturaux](#56-patterns-architecturaux)
- [5.7 Bootstrap & Initialisation](#57-bootstrap--initialisation)
- [5.8 Audio & Direction Sonore](#58-audio--direction-sonore)

### [PARTIE 6 — ÉTAT D'IMPLÉMENTATION & ROADMAP](#partie-6--état-dimplémentation--roadmap)
- [6.1 Fonctionnalités Implémentées](#61-fonctionnalités-implémentées-)
- [6.2 Plans Actifs](#62-plans-actifs-)
- [6.3 Fonctionnalités Planifiées](#63-fonctionnalités-planifiées-)
- [6.4 Backlog Complet](#64-backlog-complet)
- [6.5 Ordre Logique d'Implémentation](#65-ordre-logique-dimpl%C3%A9mentation)
- [6.6 Plan Commercial](#66-plan-commercial)

---

## PARTIE 1 — VISION & CONCEPT

### 1.1 Pitch

> **Simulation d'infiltration à la première personne où le joueur incarne un huissier mandaté pour saisir des objets de valeur dans des domiciles privés.**

Tu reçois un mandat. Tu as une liste. Tu dois entrer, trouver, charger… et partir avant que ça tourne mal.

---

### 1.2 Ton & Atmosphère

**Comédie noire** — l'absurdité du scénario (un huissier dans un domicile privé) coexiste avec une vraie tension de jeu.

> **Positionnement clé :** Le jeu n'est pas un simulateur de furtivité pure. Le joueur est légalement mandaté — il peut passer devant le propriétaire avec un objet de valeur à la main. La tension vient du *comment*, pas du *si*. Approche officielle (sonner, se présenter) et approche furtive (grappin, fenêtre) sont toutes les deux viables, avec des risques distincts.

- **Humour** : chaque mission a une saveur comique — le proprio qui cherche ses clés pendant que tu portes sa télé, le perroquet qui balance tes plans, l'avocat en costard qui surgit à 2h du matin.
- **Tension** : le propriétaire est chez lui, il peut revenir à tout moment, le quota tourne, et un seul bruit fort peut tout faire basculer.
- **Satisfaction** : l'équation parfaite — quota atteint, zéro dégât, zéro bruit, zéro trace = le sentiment d'être un artiste.

**Sources d'atmosphère :**
- Éclairage chaleureux des intérieurs de nuit
- Sons cartoon distinctifs (coffre fermé = clic satisfaisant, objet brisé = son exagéré)
- Musique dynamique qui suit le palier de paranoïa

---

### 1.3 Analyse de Marché & Positionnement

**Références inspirationnelles :**
| Jeu | Ce qu'on en prend |
|-----|------------------|
| R.E.P.O. (13M copies vendues) | Preuve que "simulateur d'action coopératif absurde" est bankable |
| Untitled Goose Game | Comédie par la mécanique, pas par le dialogue |
| Hitman | Planification → exécution → évaluation, rejouabilité infinie |
| Papers Please | Tension bureaucratique transformée en gameplay |

**Tableau de positionnement :**
| Critère | BailiffCo | R.E.P.O. | Hitman | Goose Game |
|---------|-----------|----------|--------|------------|
| Ton | Comédie noire | Horreur absurde | Thriller | Comédie pure |
| Profondeur systèmes | Élevée | Moyenne | Très élevée | Basse |
| Accessibilité | Facile à apprendre | Facile | Difficile | Très facile |
| Coop | Optionnel | Central | Non | Non |
| Prix cible | 9,99$ EA → 14,99$ | 9,99$ | 69,99$ | 14,99$ |

---

### 1.4 Piliers de Design

1. **Tension organique** — La pression vient des systèmes, pas de scripts. Le proprio fait ses lacets devant la porte parce que sa paranoïa a atteint 15+, pas parce qu'un trigger a été activé.
2. **Préparation récompensée** — Lire le briefing, choisir ses outils, connaître le proprio : ça fait la différence.
3. **Hub unifié** — Un seul espace entre les missions. Achat, amélioration, choix, briefing, relation avec les PNJ.
4. **Personnalisation totale** — Apparence du personnage, véhicule, équipement : rien de bloqué derrière la progression pour le cosmétique, tout accessible dès le départ.
5. **Exploration récompensée** — Chercher = trouver. Cachettes, objets bonus, indices du perroquet, objets sur le proprio lui-même.
6. **Lisibilité cartoon** — On comprend ce qui se passe sans HUD surchargé. L'icône de paranoïa suffit. Les sons parlent.
7. **Systèmes isolés** — Chaque système (paranoïa, bruit, pièges, animaux) fonctionne indépendamment et s'enrichit par combinaison.
8. **Respect de la loi** — Le mandat existe. Saisir trop = amende. Laisser des preuves = dénonciation. Le jeu te pousse à rester dans les clous… ou à assumer les conséquences.

---

### 1.5 Boucle de Jeu Principale

```
Bootstrap → Menu → Hub → Mission → Résumé de Mission → Hub
```

**Dans une mission :**
```
Planifier (briefing + équipement) → Infiltrer → Saisir → Charger le coffre → S'échapper → Toucher son dû
```

Chaque boucle dure 10–30 minutes. Le joueur décide quand partir — plus il attend, plus c'est risqué mais potentiellement plus rentable.

---

### 1.6 Structure de Progression

- **Early Access (V3)** : 5 missions campagne + 1 sandbox (Mission_Libre)
- **Version Finale** : 10 missions campagne totales (5 post-lancement)
- **DLC** : Packs de 2 gabarits + 1 archétype + 20 objets
- **Déblocage en jeu** : Véhicules (via `UnlocksAfterMission`), PNJ Hub (via condition de déblocage)
- **Suspension de licence** : Si saisie > +25% du quota, flag `MissionResult.Suspendu = true` → missions > 75 000 € bloquées pendant 3 missions

---

## PARTIE 2 — MÉCANIQUES DE JEU

### 2.1 Joueur

**Scripts :** `PlayerController`, `PlayerCarry`, `PlayerInteractor`, `PlayerNoiseEmitter`, `PlayerAnimator`

#### 2.1.1 Déplacement & Postures ✅

- **4 postures** : Debout / Accroupi (Ctrl) / Allongé (Z) / Sprint (Shift)
- Transitions fluides par lerp de hauteur
- `CharacterController` — pas de Rigidbody sur le joueur
- `EspaceLibrePour()` : `SphereCast` avant chaque transition allongé→accroupi→debout

#### 2.1.2 Saut & Coyote Time ✅

- Coyote time : 0,15s après avoir quitté le sol
- Bruit d'atterrissage en fonction de la vitesse de chute

#### 2.1.3 Actions Supplémentaires ⚙️

| Action | Touche | Notes |
|--------|--------|-------|
| Escalader un rebord | Espace (près rebord) | Hauteur max ~2m sans grappin |
| Se cacher | E (dans cachette) | Entrer dans placard, sous lit, derrière rideau |
| Nager | WASD (dans l'eau) | Piscine, aquarium géant — bruit ~8m |
| Frapper à la porte | **F** (porte d'entrée) | Attire le proprio vers l'entrée |
| Sonner à la porte | **E** (sonnette extérieure) | Attire le proprio vers l'entrée |
| Présenter badge/mandat | **E** (face à un PNJ, badge en main) | Présentation officielle — réduit paranoia |
| Discuter avec PNJ | **F** (face à un PNJ) | Interaction informelle |

> **Double Action sur les interactables :** `E` = action primaire (ouvrir, prendre, présenter), `F` = action secondaire (frapper, discuter, sonner). La sonnette et le frapper à la porte sont deux interactables distincts sur la porte d'entrée.

#### 2.1.4 Interaction (`PlayerInteractor`) ✅

- Raycast depuis caméra à chaque frame (portée depuis `PlayerConfigData`)
- Détecte `IInteractable` — `SetTargetCollider()` pour objets multi-colliders (véhicule)
- E appuyé = interaction ; E maintenu = pousser meuble
- Broadcast `OnInteractionLabelChanged` via EventBus (pas de `FindObjectOfType`)

#### 2.1.5 Porter & Lancer (`PlayerCarry`) ✅

- 1 objet à la fois (extensible via outil ExpandCarryCapacity)
- Porté : kinematic, colliders désactivés, layer "ObjetPorte"
- Lancer : `vitesse = BaseThrowVelocity / masse` (clampé min/max)
- Poser doucement = silencieux ; lâcher = bruit selon masse

#### 2.1.6 Bruit Émis (`PlayerNoiseEmitter`) ✅

- Appelle `EmettreBruit(niveau, portée)` depuis : pas, atterrissage, lancer, meuble
- Accroupi/allongé immobile = Silencieux (aucun event)
- EventBus<OnNoiseEmitted>

#### 2.1.7 Animations (`PlayerAnimator`) ✅

- Paramètres Animator : `Walking` (bool), `Crouching` (bool)
- Bloqués si `GameManager.InputJoueurActif == false`

**Animations prévues ⚙️ :** prendre objet, jeter, poser, courir, sauter, accroupi→debout, allongé, nager

---

### 2.2 Système de Bruit

**Script :** `PlayerNoiseEmitter`, `EventBus<OnNoiseEmitted>`

| Niveau | Valeur | Exemples | Throttle | Portée type |
|--------|--------|----------|----------|-------------|
| Silencieux | 0 | Accroupi/allongé immobile | — | 0m |
| Léger | 1 | Marche normale, ouverture douce, tiroir | 300ms | ~4m |
| Fort | 2 | Sprint, meuble poussé, chute | 100ms | ~10m |
| Très Fort | 3 | Forcer porte, lancer objet lourd, sprint 10m | 100ms | ~20m |

**Surfaces :** raycast sol → bois > carrelage > moquette (portée différente selon matériau)

**Sons joueur ⚙️ :**
- Pas normaux : inaudibles hors portée (~4m)
- Sprint : ~10m
- Porte forcée : toute la maison

---

### 2.3 Système de Paranoïa

> **TODO :** Renommer `Paranoia` → [nom final à choisir lors d'une refacto] dans `ParanoiaSystem.cs`, `ParanoiaConfig`, `ProprietaireData`. Piste : `Résistance`, `Désespoir`, `Hostilité`. La jauge mesure la **réaction proportionnelle à la perte réelle et aux actions observées**, pas une paranoïa irrationnelle.

> **Feedback joueur :** Aucun affichage chiffré ou barre de paranoia. Le joueur lit l'état du proprio uniquement via ses animations (bras croisés, transpiration, démarche rapide) et ses sons (grognements, onomatopées).

**Script :** `ParanoiaSystem`, `ParanoiaConfig` (ScriptableObject)

#### Paliers

| Palier | Valeur | Nom |
|--------|--------|-----|
| 0 | 0–10 | Calme |
| 1 | 11–25 | Méfiant |
| 2 | 26–50 | Inquiet |
| 3 | 51–75 | Paniqué |
| 4 | 76–90 | Furieux |
| 5 | 91–100 | Obsessionnel |

#### Sources d'Augmentation

| Source | Condition | Delta |
|--------|-----------|-------|
| `QuotaRatio` | `valeurChargée / quotaMission` × coeff — passif, toujours actif | variable |
| `VuPortantObjet` | Joueur visible + porte un objet de valeur | +10 à +20 selon valeur |
| `ActionDestructive` | Marteau démolition + proprio à portée d'ouïe | +25 + transition directe Investigate |
| `CachetteTrouvée` | Joueur ouvre coffre/cache caché | +15 |
| Bruit Léger entendu | — | +3 |
| Bruit Fort | — | +12 |
| Bruit Très Fort | — | +25 |
| Piège déclenché | — | +valeur du piège |
| Animal alerté | — | +AnimalData.ParanoiaBonusPerAlert |
| Joueur visible | stoppe la décroissance | — |

#### Décroissance

~0,08/s après ~10s de silence (configurable via `ParanoiaConfig`)

#### API

- `Modify(delta)` : Ajustement manuel (badge, outil)
- `SetPlayerVisible(bool)` : Active/stoppe la décroissance

---

### 2.4 IA Propriétaire

**Scripts :** `ProprietaireAI`, `ProprietaireData` (ScriptableObject)

#### 2.4.1 Système de Personnalité — 3 Curseurs (1–10) ✅

| Curseur | Faible (1) | Élevé (10) |
|---------|------------|------------|
| Réactivité | Lent, peu de pièges | Vif, portée élevée, réagit immédiatement |
| Méthode | Improvise, désorganisé | Stratégique, prépare des pièges |
| Sociabilité | Isolé, gère seul | Appelle amis, avocat, police |

**Paramètres calculés :**
| Propriété | Formule |
|-----------|---------|
| Paranoïa initiale | Lerp(0, 30, réactivité/10) |
| Vitesse normale | 2,0 + (réactivité × 0,1) m/s |
| Portée vision | 5 + (réactivité × 0,5) m |
| Nb pièges préplacés | (méthode − 1) / 2 (int) |
| Seuil appel avocat | 90 − (sociabilité × 3) |
| Délai arrivée amis | 180 − (sociabilité × 10) secondes |

#### 2.4.2 27 Personnalités — Grille 3×3×3 ⚙️

*Ces 27 combinaisons sont la référence design. Le code utilise actuellement 5 archétypes (enum `ProprietaireArchetypeType`) ; l'enum sera enrichi lors de l'implémentation des missions scénarisées.*

| R | M | S | Nom | Description |
|---|---|---|-----|-------------|
| Calme | Négl. | Sol. | Le Rêveur | Ignore presque tout, réagit très tard |
| Calme | Négl. | Nor. | Le Distrait | Lent, appelle du monde mais avec délai |
| Calme | Négl. | Gré. | Le Bonhomme | Appelle ses amis sans urgence |
| Calme | Équil. | Sol. | Le Stoïque | Réagit posément, gère seul |
| Calme | Équil. | Nor. | Le Posé | Standard mais notablement lent |
| Calme | Équil. | Gré. | Le Diplomate | Calme mais mobilise vite son réseau |
| Calme | Méth. | Sol. | L'Analyste | Très organisé, silencieux — dangereux |
| Calme | Méth. | Nor. | Le Stratège | Tout préparé, réagit avec méthode |
| Calme | Méth. | Gré. | Le Commandant | Organisation militaire + réseau solide |
| Normal | Négl. | Sol. | L'Indépendant | Réaction moyenne, désorganisé, seul |
| Normal | Négl. | Nor. | Le Classique | Comportement de base — le proprio standard |
| Normal | Négl. | Gré. | Le Bavard | Appelle tout le monde pour pas grand chose |
| Normal | Équil. | Sol. | Le Discret | Équilibré, préfère gérer sans aide |
| Normal | Équil. | Nor. | Le Stable | Parfaitement dans la moyenne |
| Normal | Équil. | Gré. | Le Sociable | Mobilise son réseau dès que nécessaire |
| Normal | Méth. | Sol. | Le Précis | Méthodique et autonome |
| Normal | Méth. | Nor. | L'Organisé | Bien préparé, réagit structuré |
| Normal | Méth. | Gré. | Le Coordinateur | Organisation rigoureuse + renforts |
| Explosif | Négl. | Sol. | L'Impulsif | Réagit fort mais rien préparé |
| Explosif | Négl. | Nor. | L'Énervé | Panique vite, appelle n'importe qui |
| Explosif | Négl. | Gré. | Le Chaotique | Fort + tout le monde — désordre total |
| Explosif | Équil. | Sol. | Le Fougueux | Vite et correctement — entièrement seul |
| Explosif | Équil. | Nor. | Le Combatif | Forte réaction + réseau standard |
| Explosif | Équil. | Gré. | Le Leader | Fort, mobilise vite — très dangereux |
| Explosif | Méth. | Sol. | Le Paranoïaque | Tout préparé, réagit à la moindre action |
| Explosif | Méth. | Nor. | Le Tactique | Très organisé, très réactif |
| Explosif | Méth. | Gré. | **Le Général** | Le plus dangereux — tout + renforts immédiats |

#### 2.4.3 Machine d'États — 8 États ✅

```
IDLE → ALERT → INVESTIGATE → CONFRONT → PANIC
                                      ↓
                          OUTDOOR (force coffre) / FURIOUS (pièges dynamiques)
État spécial : LOCKED (immobilisé via outil — 60s)
```

| État | Déclencheur | Comportement |
|------|-------------|--------------|
| Idle | Début mission | Patrouille / vie quotidienne |
| Alert | Bruit entendu | S'arrête, regarde la source (1,5s) |
| Investigate | Bruit persistant | Se dirige vers la source |
| Confront | Joueur vu (entrée officielle) | Approche, demande badge/mandat |
| Furious (entrée furtive) | Joueur découvert sans présentation préalable | Passe directement Furious — peut escorter le joueur dehors et appeler la police |
| Panic | Palier ≥3 | Appelle avocat/amis, fuit rapidement |
| Outdoor | Quota 20% + palier ≥3 | Sort vers le véhicule |
| Furious | Paranoïa ≥76 | Pose pièges dynamiques, appelle tous |
| Locked | Outil menottes | Immobile 60s |

#### 2.4.4 Senseurs ✅

- **Vision** : raycast chaque frame, angle de vue configurable, coyote 0,2s
- **Ouïe** : `portée = VisionRange + HearingBonus`

#### 2.4.5 Comportements de Gêne Passive ⚙️

Comportements aléatoires déclenchés selon le palier de paranoïa :

| Comportement | Palier | Durée | Effet |
|-------------|--------|-------|-------|
| S'étirer dans l'embrasure | 10+ | 8–15s | Bloque le passage |
| Refaire ses lacets dans le couloir | 15+ | 10–20s | Obstruction partielle |
| Chercher ses clés devant une porte | 20+ | 15–25s | Bloque une porte |
| S'adosser et regarder son téléphone | 25+ | 20–45s | +5 paranoïa si joueur visible |
| Faire de l'exercice dans le salon | 30+ | 30–60s | Occupe la pièce clé |
| Déplacer un meuble lourd dans le couloir | 40+ | 60–90s | Bloque totalement le couloir |
| Appeler et marcher de long en large | 35+ | 60–120s | Patrouille imprévisible |
| S'asseoir sur l'objet recherché | 50+ | Jusqu'à dérangé | Objet inaccessible |

**Animations proprio prévues ⚙️ :** marcher, courir, lacer ses chaussures, s'étirer, fouiller le coffre, prendre/poser objet, chercher ses clés, s'adosser, déplacer meuble

---

### 2.5 Véhicule & Coffre

**Scripts :** `VehicleRuntime`, `VehicleTrunkZone`, `VehicleAmbiance`, `VehicleHubSlot`, `VehiclePanelUI`

#### 2.5.1 8 Types de Véhicules (`TypeVehicule`) ✅

| Véhicule | Disponibilité | Notes |
|----------|--------------|-------|
| VeloCargo | Dès le départ (gratuit) | Coffre limité, silencieux |
| Scooter | Dès le départ (location) | Rapide, petit coffre |
| Pickup | Dès le départ (location) | Polyvalent |
| Ane | Déblocage Mission 2 | Braiements sonores |
| Fourgon | Déblocage Mission 3 | Grand coffre |
| CamionGlace | Déblocage Mission 4 | Jingle = bruit massif |
| Helicoptere | Déblocage Mission 5 | Extraction verticale |
| Remorque | Déblocage Mission 4 | Très grande capacité |

#### 2.5.2 Structure Prefab Véhicule ✅

```
PrefabVoiture
├── ColliderDriverDoor  ← layer Interactable
├── ColliderTrunkDoor   ← layer Interactable
├── TrunkZone (Trigger) ← activé quand coffre ouvert
└── CageCollider        ← (optionnel si HasAnimalCage)
```

#### 2.5.3 Détection Multi-Colliders ✅

`PlayerInteractor` appelle `SetTargetCollider(collider)` → `VehicleRuntime` sait quelle partie est ciblée (porte conducteur / coffre / cage).

#### 2.5.4 Flux de Chargement ✅

1. Joueur ouvre coffre → `TrunkZone.enabled = true`
2. Joueur dépose objet dans zone → `OnObjectEnteredTrunk()` (debounce 2 frames)
3. Objet visible dans coffre en temps réel → `OnQuotaChanged` avec preview
4. Joueur clique porte conducteur → `OnMissionEndRequested`
5. `DepartureConfirmationUI` s'ouvre (popup Blocking)
6. Confirmation → `ConvertObjectsToQuota()` → `MissionSystem.EndMission()` → fondu

#### 2.5.5 Anti-vol & Vol Propriétaire ⚙️

- `AntiTheft` (consommable) : protection contre vol
- Au seuil 20% quota + état Outdoor : proprio tente de forcer le coffre (`VehicleForceDuration`)
- `TakeRandom()` via `ProprietaireAI` → `OnOwnerRetrievedObject` → soustrait du quota

#### 2.5.6 Sons Ambiants (`VehicleAmbiance`) ✅

- Boucle de sons spéciaux (braiement, jingle glace, rotor...) à intervalle aléatoire
- Émet `OnNoiseEmitted` → réaction du propriétaire
- `StopAmbiance()` à la fin de mission

#### 2.5.7 Dégâts Véhicule par PNJ ⚙️

- **Rétroviseur cassé** : bruit + swap prefab véhicule avec rétro cassé → retenue −50€ dans récap
- **Véhicule accidenté** : gros bruit de collision + swap prefab accidenté → retenue −500€
- Event : `OnVehicleDamaged { VehicleDamageType Type, float Cout }` avec enum `Retroviseur / Accident`

---

### 2.6 Objets Saisissables

**Scripts :** `ValueObject`, `ObjetData` (ScriptableObject)

#### Champs ObjetData

| Champ | Description |
|-------|-------------|
| ValueMin / ValueMax | Fourchette de valeur (tirée à la génération) |
| WeightKg | Masse (affecte la vitesse de lancer) |
| IsFragile | Peut être endommagé par un impact |
| IsOversized | Encombre le coffre |
| RequiresTwoPlayers | Nécessite 2 joueurs (coop) |
| DropNoiseLevel | Niveau de bruit à la chute |
| FullNameAfterScan | Nom révélé par scan UV |

#### Système de Dommages ✅

| Impact | Résultat |
|--------|----------|
| < DamageImpactThreshold | x1,0 (intact) |
| [Impact, Major) | MinorDamageMultiplier (~x0,75) |
| ≥ MajorDamageThreshold | MajorDamageMultiplier (~x0,5) |

Émet `OnObjectDamaged { ValueLost, Position }` → Déduit dans bulletin de paie (section A)

#### Scan ✅

`ValueObject.Scan()` → révèle `FullNameAfterScan + valeur` dans le label

#### Chargement ✅

`LoadIntoVehicle()` → émet `OnObjectLoaded` → Destroy → `QuotaSystem.Add()`

---

### 2.7 Outils & Consommables

**Scripts :** `OutilData` (ScriptableObject), `InventaireSystem`, `InventaireWheel`

#### Catégories d'Outils (`OutilCategorie`)

7 catégories : `ForceEntry`, `Stealth`, `Legal`, `Scanner`, `Immobiliser`, `Consumable`, `Utility`

#### Types d'Effets (`ToolEffectType`)

9 types : `ForceDoor`, `Lockpick`, `ReduceParanoia`, `ImmobiliseOwner`, `ScanUV`, `ScanXRay`, `SprayAnimal`, `SpraySilence`, `ExpandCarryCapacity`

#### Système de Niveaux (0–2) ✅

Chaque niveau ajoute : EffectDuration, Power, ParanoiaModifier, PlayerSpeedMultiplier, Charges, Cooldown, UpgradeCost

#### Slot Officiel — Badge + Mandat ✅

Un slot d'inventaire **permanent et non consommable** contient les deux documents officiels :
- **Badge** : identifiant de l'huissier
- **Mandat** : ordre de saisie (= la fiche de mission récupérée dans le Hub avant le départ)

Le joueur tient ce slot en main via `PlayerCarry` pour présenter ses documents (`E` face au PNJ). Tenir le badge occupe le slot carry — le joueur ne peut pas porter simultanément un objet de valeur.

**Effets de présentation par PNJ :**

| PNJ | Document requis | Effet si présenté | Effet si refus |
|-----|----------------|-------------------|----------------|
| Proprio (calme) | Badge | Paranoia −10 | Paranoia +15 |
| Proprio (Confront, paranoia < seuil) | Badge | Paranoia −20, retour Idle | Paranoia +25, appel avocat |
| Proprio (Confront, paranoia ≥ seuil) | Badge + Mandat | Paranoia −10, reste méfiant | État Furious |
| Avocat | Badge + Mandat | Avocat repart, Paranoia −15 | Amendes fin de mission |
| Policier | Badge + Mandat | Policier repart après vérif. | Joueur bloqué +120s |

#### Outils de Départ (IsStartingTool = true) ✅

- Badge + Mandat (slot officiel permanent)
- Téléphone d'Huissier (level 0)

#### Roue d'Inventaire (`InventaireWheel`) — Maintenir Tab ✅

```
           [Slot 1 Haut]
[Slot 8]  [Slot 5]  [Slot 6]
[Slot 4]  [MAINS]   [Slot 2]
[Slot 7]  [Slot 6]  [Slot 3]
          [Slot 9 Bas]
```

- Deadzone centre 40px = mains vides
- Positions cardinales + diagonales pour outils/consommables
- Sélection sur déplacement souris, confirmation au relâchement de Tab
- `InventaireSystem` injecté par `UIManager.OnJoueurSpawne()`

#### Backlog — Outils Permanents ⚙️

| Outil | Effet | Niveau max |
|-------|-------|-----------|
| Badge Officiel | ReduceParanoia en Confront | 2 |
| Téléphone d'Huissier | Scan objet → valeur + nom | 2 |
| Pied de biche | ForceDoor (bruit fort) | 2 |
| Kit de crochetage | Lockpick (mini-jeu) | 2 |
| Grappin | Escalader (2m+ sans bruit) | 2 |
| Lampe UV | ScanUV (révèle pièges, cachettes) | 2 |
| Appareil photo d'huissier | Preuve photographique | 2 |
| Scanner Rayon X | ScanXRay (voir à travers murs) | 2 |
| Marteau de démolition | Forcer murs/planchers | 2 |
| Gants de protection | Protection morsures animaux | 2 |
| Lampe de poche | Éclairage zones sombres | 2 |
| Tournevis | Démonter objets fixés | 2 |

#### Backlog — Consommables ⚙️

Taser, Filet de capture, Appât alimentaire, Cadenas, Faux document, Antivol, Objet leurre, Spray répulsif (animal), Antidote, Kit de premiers secours, Spray soporifique, Gilet de protection, Tracker GPS

---

### 2.8 Interactables Environnement

#### `OpenableInteractable` — Portes, Fenêtres, Trappes ✅

| Mode | Exemple | Mécanique |
|------|---------|-----------|
| Rotation | Porte battante | Lerp quaternion |
| TranslationVerticale | Fenêtre guillotine | MoveTowards Y |
| TranslationHorizontale | Fenêtre coulissante | MoveTowards X |

**États :** Fermé / Ouvert / Verrouillé / Bloqué
40% de chance de grincement à l'ouverture. Forcer → bruit Très Fort (portée 20m)

#### `MeubleInteractable` — Meubles Poussables ✅

- E maintenu = pousser (XZ seulement)
- `SpeedMultiplier` = 1 / (1 + masse/20) → retourne 0,25–1,0
- Rigidbody kinematic, `MovePosition` dans FixedUpdate

#### `TiroirInteractable` — Tiroirs ✅

- Translation locale le long de l'axe Z
- `LiberateContents()` : active les Rigidbody des ValueObject enfants (saisissables quand ouvert)
- `LockContents()` : re-kinematise à la fermeture
- Bruit Léger ~35% de chance

#### Interface `IInteractable` ✅

```csharp
bool CanInteract(GameObject interactor);
void Interact(GameObject interactor);
string GetInteractionLabel();
```

Implémentée par : `ValueObject`, `MeubleInteractable`, `OpenableInteractable`, `TiroirInteractable`, `VehicleRuntime`, `HubPNJ`

---

### 2.9 Cachettes & Pièges

**ScriptableObjects :** `CachetteData`, `PiegeData`

#### Cachettes ⚙️ (data défini, interaction manquante)

**10 Types de Cachettes :**
DoubleFond, DerriereTableau, SousTapis, TrappePlancher, CoffreMural, DansLeCanapé, DerriereRideau, SousLit, DansPlacard, CaveSecrete

**Propriétés :** chance détection owner, révélation UV/Rayon X, bruit ouverture, capacité, déplaçable

#### Contenants Spéciaux ⚙️

- **Aquarium** : 4 couches (poisson / décorations / substrat / aquarium) — main dans l'eau bruit ~6m, anguille électrique = stun
- **Terrarium** : serpent/araignée/scorpion — morsure si sans gants de protection
- **Volière** : oiseaux exotiques — cris si stressés (~12m)
- **"Sur le proprio lui-même"** : menotter le proprio pour accéder aux objets cachés sur lui

#### Pièges ⚙️ (data défini, système de placement manquant)

**9 Types de Pièges :**
SeauEau, FauxPlancher, ColleIndustrielle, AlarmeInfrarouge, ChienDeGarde, AnimalVenimeux, etc.

**Propriétés :** ParanoiaBonus, AlertsNeighbours, AlertsPolice, PlayerSpeedMultiplier, ImmobilisesPlayer, ForcesDrop, ObscuresVision, DisarmToolName, UVIndicatorPrefab

**Placement :**
- Pièges préplacés : avant la mission (nombre selon `Méthode` du proprio)
- Pièges dynamiques : placés par le proprio en état Furieux ⚙️

#### Système de Dénonciation Policière ⚙️

- Menottes laissées sur place → preuve → amende section D
- Gaz soporifique non caché → preuve → amende section D
- Event à créer : `OnFineIssued { string Description, float Montant }` levé par `ProprietaireAI`
- Menottes : timer avant que le proprio se libère (si joueur les retire avant = pas de preuve)
- Bombe spray : joueur doit la cacher dans poubelle, coffre, ou cachette

**Comportement de la police à l'arrivée :**
- La police entre et **cherche activement** le joueur pendant `PoliceSearchDuration` (~120s configurable)
- Si le joueur est dans une cachette valide (`DansPlacard`, `SousLit`, `DerriereRideau`…) et non trouvé → policier repart, **aucune amende**
- Si trouvé (cachette ou à découvert) → joueur bloqué pour vérification + amende section D
- Pendant le blocage ou la recherche : le proprio et les voisins peuvent accéder librement au coffre du véhicule
- La police cherche en priorité les pièces où des bruits ont été entendus récemment

---

### 2.10 Animaux

**ScriptableObject :** `AnimalData`

#### Espèces ⚙️

**Définies dans le data :** Chat, ChienCompagnie, ChienGarde, Perroquet, Lapin, Tortue, Poisson, Perruche

**Backlog :** Serpent, Araignée/Scorpion, Oiseau exotique (volière), Anguille électrique

#### Comportements ⚙️

- Réaction son/vision → émet `OnAnimalAlerted { Position, Intensity, Species }`
- Chien de garde : peut chasser, immobiliser
- Cage : si proprio remarque animal encagé → bonus paranoïa

#### Système Perroquet ⚙️

- Parle des phrases → `OnParrotSpoke { Phrase, IsClue }`
- Tirage 50/50 à chaque déclenchement :
  - **Utile (50%)** : révèle code du coffre, cachette de clé, emplacement d'un objet précieux — phrases entendues du proprio
  - **Saboteur (50%)** : répète l'appel du chef au pire moment, crie `"LA POLICE ARRIVE RHAAA"`, `"QUI EST LÀ ?!"` → proprio +8 paranoia
- Silence soudain du perroquet = signal d'alarme (design intent)
- Le joueur peut **couvrir la cage** avec un tissu (objet interactable ou consommable) pour neutraliser le perroquet

---

### 2.11 PNJ Secondaires — Voisins

**ScriptableObject :** `NeighbourData`

#### Types de Voisins ⚙️

| Type | Déclencheur | Comportement |
|------|-------------|--------------|
| Ami Costaud | Sociabilité ≥5 | Renforce le propriétaire, bloque couloir |
| Avocat | Sociabilité ≥7 | Bloque le départ (`LawyerBlockDuration`) |
| Voisin Curieux | Aléatoire | Observe, peut alerter la police après 3min |
| Voisin Voleur | Joueur dans maison + quota >20% + coffre ouvert | Spawn au coffre. Si joueur sort et le voit → fuit avec **1 objet**. Pendant la poursuite, le proprio reprend **1 objet** (le plus précieux). |
| Fan de Foot | Joueur porte une TV | Spawn à l'entrée, reprend la TV des mains du joueur, la repose sur le meuble et s'installe pour regarder le match. |
| Livreur Suspect | Event aléatoire (prob. 30%), après 3min | Sonne avec colis suspect — proprio très distrait 60s. |
| Agent de Police | Alerte (preuve objet illégal) | Cherche le joueur ~120s. Si non trouvé → repart sans amende. Si trouvé → bloque + amende. |

**Backlog PNJ :** Voisin complice, Journaliste, Voisin dénommé (connait le proprio)

---

### 2.12 Scènes Virales ⚙️

Ces scènes sont le **moteur de diffusion organique** du jeu (TikTok, YouTube, Twitch). Elles sont scriptées mais déclenchées par des conditions procédurales — le joueur peut les manquer s'il part trop tôt.

#### Tirage à la Génération de Mission

- Au chargement, **1 à 4 scènes** sont tirées au sort parmi le catalogue
- Les scènes tirées **se produiront** dès que leurs conditions sont remplies
- Les scènes non tirées n'arrivent pas dans ce run → rejouabilité
- Jamais deux scènes au même seuil de paranoia dans le même tirage
- Nombre selon la taille de la mission : petite = 1–2, grande = 2–4
- Les scènes manquées sont listées dans le **résumé de mission** pour inciter à rejouer

#### Catalogue V1 ⚙️

| # | Nom | Condition | Description |
|---|-----|-----------|-------------|
| 1 | **Voisin Voleur** | Joueur dans maison + quota >20% + coffre ouvert | Voisin au coffre. Joueur sort → voit le voisin → voisin fuit avec 1 objet. Proprio reprend 1 objet pendant la poursuite. |
| 2 | **Fan de Foot** | Joueur porte une TV | Voisin baraqué reprend la TV des mains du joueur, la repose, s'installe pour le match. |
| 3 | **Livreur Suspect** | Aléatoire (30%), après 3min | Colis suspect sonne à la porte — proprio distrait 60s. |
| 4 | **L'Avocat de Minuit** | Paranoia > seuil sociabilité | Avocat en costard impeccable surgit, serviette en main. |
| 5 | **Le Perroquet Traître** | Joueur passe devant la cage | Répète code du coffre / cachette de clé (utile) ou crie des phrases du proprio au pire moment (saboteur). |
| 6 | **L'Ami Inattendu** | Paranoia basse + aléatoire | Ami arrive avec bières, s'installe dans le salon, occupe la pièce principale. |
| 7 | **Proprio Somnambule** | Heure mid-mission | Proprio yeux fermés traverse la pièce où le joueur est caché. |
| 8 | **Peignoir & Claquettes** | Idle aléatoire extérieur | Proprio en peignoire + claquettes chaussettes fait une action absurde dans le jardin. |

---

## PARTIE 3 — PROGRESSION & ÉCONOMIE

### 3.1 Structure des Missions

**ScriptableObject :** `MissionData`

#### Champs Principaux de MissionData ✅

| Champ | Type | Description |
|-------|------|-------------|
| MissionName | string | Nom affiché dans l'UI |
| MissionNumber | int | Indice de progression (1–10+) |
| BriefingText | string | Texte de briefing |
| SceneName | string | Scène Unity (vide = Mission_Libre procédurale) |
| Owner | ProprietaireData | Config IA propriétaire |
| Habitation | HabitationData | Description du logement |
| SeizableObjects | SeizableObjectEntry[] | Objets saisissables + poids de spawn |
| MinimumQuotaValue | float | Cible (0 = auto 50% du max) |
| FixedSeed | int | Graine déterministe (0 = aléatoire) |
| PossibleTraps | PiegeData[] | Pièges disponibles pour cette mission |
| PossibleHidingSpots | CachetteData[] | Cachettes disponibles |
| BonusTimeThresholdSeconds | float | Temps pour bonus performance |
| CommissionTaux | float | 0,25 (quota atteint) |
| CommissionEchecTaux | float | 0,10 (quota raté) |
| SeuilExcesLeger / Modéré / Abusif | float | 5% / 10% / 25% |
| TauxPenaliteExcesLeger / Modéré / Abusif | float | 0,5× / 1,0× / 1,5× |
| SkyboxOverride | Material | Ambiance visuelle |
| MissionMusic | AudioClip | Musique spécifique |

#### Missions Campagne vs Procédurales

- **`CampaignMissionStarter`** : Scène hand-crafted, propriétaire/véhicule/objets déjà placés dans Unity
- **`MissionBuilder`** : Scène `Mission_Libre`, tout instancié depuis `MissionData` (maison, véhicule, owner, objets)

#### 5 Missions Campagne Scénarisées ⚙️ (data + scènes à créer)

| # | Titre | Proprio | R/M/S | Gabarit | Pédagogie |
|---|-------|---------|-------|---------|-----------|
| 1 | La Saisie de Marcel | Marcel Dupont (Le Rêveur) 67 ans | 2/2/2 | Maison de ville Petit | Bases : coffre, scanner, partir, tutoriel chef |
| 2 | Le Colonel et ses gadgets | Colonel Renard (Le Fougueux) 58 ans | 7/3/2 | Maison de ville Moyen | Pièges. Pied-de-biche. IA réactive |
| 3 | Le Clip de Viviane | Viviane de La Fontaine (La Bavarde) 45 ans | 4/4/8 | Maison de ville Grand | Renforts, voisins, gestion du temps |
| 4 | Le Labo du Professeur | Pr. Archibald Knox (Le Précis) 72 ans | 5/9/3 | Laboratoire Moyen | Cachettes complexes, Scanner RX, animaux exo |
| 5 | Le Studio de DJ Pharaon | DJ Pharaon (Le Tactique) 34 ans | 9/8/6 | Villa Grand | Mission complète — tous systèmes |

#### 5 Gabarits de Maisons ⚙️

| Gabarit | Structure | Taille | Complexité |
|---------|-----------|--------|------------|
| Appartement | Salon, cuisine, 2 ch., SdB, balcon. 3ème étage | Pet/Moy | 6/10 |
| Maison de ville | RdC (salon, cuisine, bureau), Étage (3 ch.), Cave, Jardin | Moy/Grd | 8/10 |
| Villa | 2 étages, double garage, piscine, dépendance | Grand | 9/10 |
| Manoir | Grande entrée, bibliothèque, 4 ch., cave, grenier | Grand | 10/10 |
| Laboratoire reconverti | Open space lab, bureau, salle blanche, entrepôt, sous-sol | Moy/Grd | 10/10 |

#### Tutoriel — Mission 1 (Appels du Chef) ⚙️

Le tutoriel est délivré via des **appels téléphoniques automatiques du chef de l'agence**. Diegétique, comique, pas de 4ème mur. Le chef appelle aussi spontanément si le joueur fait une bêtise (casser un objet, déclencher un piège).

> **Cadre fixe :** toujours une maison individuelle avec extérieur. Véhicule garé sur la rue en face. Propriétaire riche excentrique de faible réactivité. Pas d'appartement.

| # | Déclencheur | Message du chef |
|---|------------|----------------|
| 1 | Spawn devant la maison | "Frappe à la porte et présente-toi. T'as ton badge ?" |
| 2 | Joueur entre dans la maison | "Il y a une commode à l'entrée. Sors ton téléphone, scanne l'objet." |
| 3 | Premier objet scanné | "Maintenant prends-le et mets-le dans le coffre de ton véhicule." |
| 4 | Premier objet chargé | "Bien. Continue. Une fois le quota atteint, tu rentres." |
| 5 | Joueur s'approche d'un animal | "Il a un [chien/chat]. Surtout ne l'énerve pas. Utilise les sardines." |
| 6 | Joueur déclenche un piège | "T'as marché dans quoi là ?! Regarde où tu mets les pieds." |
| 7 | Quota atteint | "C'est bon, charge et rentre. Laisse pas traîner." |
| 8★ | Joueur casse un objet | "...t'as encore tout cassé ? Je te retire ça du salaire." |
| 9★ | Joueur trouve le coffre caché | "Jackpot. Le proprio savait pas que je savais. Check." |

★ = appels bonus, non obligatoires.

#### Mode Sandbox — Mission Libre ⚙️

**Configurateur `MissionLibreConfigUI` :**
- Sliders : Réactivité / Méthode / Sociabilité / Difficulté (1–10)
- Type de maison + Taille + Complexité
- Seed partageable (même params + même seed = mission identique)
- Présets : Facile / Normal / Difficile / Cauchemar

---

### 3.2 Système de Quota

**Script :** `QuotaSystem`

#### Seuils de Déclenchement (`OnThresholdReached`) ✅

| Seuil | Effet |
|-------|-------|
| 20% | Proprio commence à surveiller son coffre |
| 50% | — |
| 60% | — |
| 80% | Comportement d'urgence (timer potentiel) |
| 100% | Quota atteint |

#### API ✅

- `TotalValue`, `TargetValue`, `Percentage`, `QuotaReached`
- `LoadedObjects` : `IReadOnlyList<(ObjetData, float)>` pour écran résultat

---

### 3.3 Notation par Étoiles

**Script :** `MissionSystem.CalculateStars()`

| Étoiles | Conditions |
|---------|------------|
| ★★★ | Récupéré ≥ 2× quota + 0 objet cassé + 0 piège déclenché |
| ★★ | Récupéré ≥ 1,5× quota + objets cassés ≤ seuil |
| ★ | Quota atteint (autres conditions non remplies) |
| 0 ★ | Quota non atteint |

**Bonus de temps ⚙️ :** Si mission < `BonusTimeThresholdSeconds` → bonus étoile supplémentaire

---

### 3.4 Bulletin de Paie — Système Économique

**Scripts :** `MissionResult`, `MissionSystem`, `MissionSummaryUI`

#### Formule Complète ✅

```
HONORAIRES
  CommissionBase   = ValeurTotale × taux (25% succès / 10% échec)
  BonusPerformance = stars × bonus% (★★ = 2%, ★★★ = 5%)

DÉDUCTIONS
  A — Objets Endommagés
      Par objet : −(ValeurActuelle / 2)

  B — Véhicule
      Location : −VehiculeData.RentalPrice
      Rétroviseur cassé : −50€                             ⚙️
      Accident : −500€                                     ⚙️

  C — Saisie Excessive (si valeur récupérée > quota)
      +0% à +5%  : tolérance (pas de pénalité)
      +5% à +10% : saisie légère   −(excès × 0,50)
      +10% à +25%: saisie modérée  −(excès × 1,00)
      +25%+      : saisie abusive  −(excès × 1,50) + flag Suspendu

  D — Infractions PNJ                                      ⚙️
      Menottes dénoncées : −100€/occurrence
      Gaz dénoncé : −125€/occurrence

SALAIRE NET = CommissionBase + BonusPerformance − (A + B + C + D)
```

#### `MissionResult` — Structure Complète ✅

```csharp
// Mission
MissionData Mission;
bool MissionReussie;
int Etoiles;
float TempsSecondes;
float ParanoiaMaxAtteinte;
int NombrePiegesDeclenches;

// Valeurs brutes
float ValeurTotaleRecuperee;
float ValeurQuotaCible;
int NombreObjetsRecuperes;

// Honoraires
float CommissionBase;
float BonusPerformance;

// Déductions
List<ObjetEndommage> ObjetsEndommages;
float CoutLocationVehicule;
float DegatsVehicule;
float AmendesSaisieExcessive;
bool Suspendu;
float AmendesInfractions;

// Net
float SalaireNet;

// Listes détaillées (pour UI)
ObjetRecupere[] ObjetsRecuperes;
ConsommableUtilise[] ConsommablesUtilises;
```

#### `MissionSummaryUI` — Affichage ✅

S'ouvre automatiquement sur `OnMissionEnded`. ScrollRect avec sections :
1. En-tête (titre, nom mission, étoiles animées)
2. Objets récupérés (tableau : Nom | Qté | Prix | Total)
3. Honoraires (commission %)
4. Déductions A — Objets endommagés
5. Déductions B — Véhicule (location + dégâts)
6. Déductions C — Saisie excessive
7. Déductions D — Infractions
8. **SALAIRE NET** (vert si positif, rouge si négatif)
9. Consommables utilisés (info seulement)
10. Bouton Continuer

---

### 3.5 Hub & Méta-Jeu

**Scripts :** `HubManager`, `HubPNJ`, `HubUI`

#### 5 PNJ du Hub (`HubPNJ.TypePanneau`)

| PNJ | Action | Panel | Statut |
|-----|--------|-------|--------|
| Agent Missions | Choix de mission | MissionListUI | ✅ |
| Secrétaire / Boutique | Acheter/améliorer outils | ShopPanel | ⚙️ |
| Garagiste | Location + réparation véhicule | VehiclePanelUI | ✅ |
| Archiviste | Consulter dossiers passés | ArchivistePanel | ⚙️ |
| Tailleur / Perso | Personnalisation personnage | PersonnalisationPanel | ⚙️ |

**Déclencheur de déblocage :** `HubPNJ._debloque` + `_conditionDeblocage` (ex: "Compléter Mission 3")
**Label flottant 3D** (billboard caméra) : `"[E] Parler"` ou `"🔒 [Condition]"`

#### Flux Sélection Mission ✅

1. Parler à l'Agent → `MissionListUI.Ouvrir()`
2. Choisir mission → `MissionPanelUI.Ouvrir(mission)` → détails complets
3. Valider → `HubManager.SelectionnerMission(mission)`
4. Parler au Garagiste → `VehiclePanelUI` → choisir véhicule
5. Confirmer → déduire location → `GameManager.LancerMission(mission, vehicule)`

#### Inventaire Hub ⚙️

- Casier Hub : le joueur s'équipe avant chaque mission
- Lien avec `InventaireSystem` (achats en boutique → disponibles dans casier)

---

### 3.6 Coopératif ⚙️

**Base :** Unity Netcode for GameObjects

#### Hub Unifié

- Un seul Hub pour solo et coop — aucune séparation de mode
- Code Hub alphanumérique (6 caractères, ex: `BF-47RX`) affiché dans le Hub
- Max 2 joueurs — salle verrouillée à 2
- Invite Steam alternative au code

#### Table d'Inventaire Partagée

- Une seule table = celle de l'hôte
- Les deux joueurs s'équipent depuis la même table
- Outils permanents : copie d'usage (non retirés de la table)
- Consommables : max 3 du même type par joueur par mission
- Fin de session client : tous ses consommables retournent à la table hôte
- Progression sur la save de l'hôte uniquement
- **Bonus coop :** +20% sur la valeur totale récupérée

#### Mécaniques Coop Exclusives

- Transport lourd à 2 (piano, coffre-fort, sous-marin)
- Distraction coordonnée
- Extraction par fenêtre (un tient, l'autre descend)
- Défense du véhicule pendant extraction
- Libération de piège coopératif
- Transfert d'antidote

---

## PARTIE 4 — INTERFACE UTILISATEUR

### 4.1 Architecture UI

**Scripts :** `UIManager`, `UIPanel`

#### UIManager (Singleton persistant) ✅

- Découvre tous les `UIPanel` en scène (`RegisterPanel` / `UnregisterPanel`)
- Sur `OnContextChanged` → `EvaluerTousLesPanels(contexte)`
- `UpdateUIState()` : curseur + input joueur selon panels ouverts
- `GetPanel<T>()` : lookup générique type-safe
- `OnJoueurSpawne()` : injecte `InventaireSystem` + `PlayerCarry` → `InventaireWheel`
- `FermerTousLesPanels()` : ferme proprement via `Fermer()`

#### UIPanel (Base Class Abstraite) ✅

```csharp
UIPanelType panelType;            // Blocking / Overlay / GameUI / Popup
ContexteJeu[] _contexteVisibles;  // Contextes où ce panel peut exister
bool _autoAfficher;               // Ouvrir automatiquement à l'entrée du contexte

void Ouvrir();
void Fermer();
void EvaluerContexte(ContexteJeu);
void ReAbonnerEventBus();
```

**Règle absolue :** Jamais de `SetActive()` direct depuis l'extérieur — tout passe par `Ouvrir()`/`Fermer()`.

#### Types de Panels (`UIPanelType`)

| Type | Curseur | Input Joueur | Exemple |
|------|---------|--------------|---------|
| Blocking | Libre | Non | DepartureConfirmationUI, MissionSummaryUI |
| Overlay | Selon contexte | Selon contexte | InventaireWheel, LabelInteractionUI |
| GameUI | Selon contexte | Selon contexte | HUDSystem, HubUI |
| Popup | Libre | Non | Erreur, confirmation |

#### Contextes & Curseur

| Contexte | Curseur | Input Joueur |
|----------|---------|--------------|
| Menu | Libre | Non |
| Hub | Libre | Non (clavier navigation) |
| Mission | Verrouillé | Oui |
| Personnalisation | Libre | Non |

Si un panel `Blocking` est ouvert → override → curseur libre + input joueur désactivé (quel que soit le contexte)

---

### 4.2 HUD Mission

**Script :** `HUDSystem`

Visible uniquement en contexte Mission. Souscrit à :

| Event | Réaction |
|-------|----------|
| `OnMissionStarted` | Reset quota |
| `OnQuotaChanged` | Mise à jour barre + texte "€ / €" |
| `OnParanoiaChanged` | Change sprite + nom du palier |
| `OnObjectLoaded` | Notification "+€" (fondu 2s) |
| `OnUrgencyTimerStarted` | Affiche minuteur rouge |
| `OnParrotSpoke` | Affiche citation perroquet (4s) |
| `OnMissionEndRequested` | Ouvre `DepartureConfirmationUI` |

#### Composants HUD ✅

| Élément | Description |
|---------|-------------|
| Barre Quota | Slider 0–100% + texte "€/€" |
| Icône Paranoïa | 6 sprites selon palier + nom textuel |
| Timer | Temps écoulé depuis début mission |
| Cadre rouge | Visible en urgence (police, timer expulsion) |
| Notification | Popup "+€" temporaire (fondu 2s) |

---

### 4.3 Panels par Contexte

#### CONTEXTE MENU

| Panel | Type | Description | Statut |
|-------|------|-------------|--------|
| MenuUI | GameUI | Navigation principale (Jouer, Options, Quitter) | ✅ |

#### CONTEXTE HUB

| Panel | Type | Description | Statut |
|-------|------|-------------|--------|
| HubUI | GameUI | Coordinateur (argent HUD, mission choisie) | ✅ |
| MissionListUI | Overlay | Liste des missions avec statut/étoiles | ✅ |
| MissionPanelUI | Blocking | Fiche mission : portrait, proprio, menaces, quota | ✅ |
| VehiclePanelUI | Blocking | Fiche véhicule : capacité, prix, avantages/inconvénients | ✅ |
| ShopPanel | Blocking | Boutique : acheter/améliorer outils | ⚙️ |
| MissionLibreConfigUI | Blocking | Config sandbox (difficulté, modificateurs) | ⚙️ |
| ArchivistePanel | Blocking | Consultation dossiers passés | ⚙️ |
| PersonnalisationPanel | Blocking | Éditeur de personnage | ⚙️ |

#### CONTEXTE MISSION

| Panel | Type | Description | Statut |
|-------|------|-------------|--------|
| HUDSystem | GameUI | HUD mission (quota, paranoïa, timer) | ✅ |
| LabelInteractionUI | Overlay | Label contextuel "[E] — Ouvrir" | ✅ |
| CrosshairUI | Overlay | Viseur (optionnel) | ✅ |
| InventaireWheel | Overlay | Roue 9 slots (maintenir Tab) | ✅ |
| DepartureConfirmationUI | Blocking | Popup confirmation départ | ✅ |

#### CONTEXTE POST-MISSION

| Panel | Type | Description | Statut |
|-------|------|-------------|--------|
| MissionSummaryUI | Blocking | Bulletin de paie complet | ✅ |

#### TOUS CONTEXTES

| Panel | Type | Description | Statut |
|-------|------|-------------|--------|
| PauseMenu | Blocking | Pause (ESC) | ✅ |
| OptionsUI | Blocking | Vidéo / Audio / Souris / Contrôles | ✅ |

---

### 4.4 Options & Rebinding

**Scripts :** `OptionsManager`, `OptionsUI`, `OptionsData`

#### 13 Actions Rebindables (`ActionJeu`) ✅

`Avancer`, `Reculer`, `GaucheCtrl`, `DroiteCtrl`, `Interagir`, `Sprint`, `Accroupi`, `Allongé`, `Saut`, `Inventaire`, `Pause`, `Poser`, `Jeter`

#### Paramètres Vidéo ✅

Résolution, Plein écran, Qualité (Low/Med/High/Ultra), VSync, Limite FPS

#### Paramètres Audio ✅

Volume master, Musique, SFX, Ambiance

#### Paramètres Souris ✅

Sensibilité, Inversion Y

**Sauvegarde :** JSON dans PlayerPrefs (`"BailiffCo_Options"`)

---

### 4.5 Personnalisation du Personnage ⚙️

**Scène :** `CharacterCustomization.unity` (existe, logique à compléter)

| Catégorie | Options |
|-----------|---------|
| Cheveux | Style (court/long/afro/chignon/tresses…) + Couleur |
| Yeux | Forme + Couleur (hétérochromie possible) |
| Bouche | Sourire/neutre/sérieux/dents |
| Pilosité | Barbe (naissante/courte/longue/moustache/barbichette/pattes) |
| Chapeau | Casquette officielle, chapeau melon, béret, haut-de-forme… |
| Veste | Couleur et style (classique, sport, décontracté, motifs) |
| Pantalon | Coupe (slim, large, bermuda, salopette) |
| Cravate/Col | Nœud papillon, col ouvert, badge mis en valeur |
| Chaussures | Derbies, baskets, bottes, mocassins + couleur |

**Règles :** Tout disponible dès le départ, sans déblocage. Lié à la save individuelle. Visible dans le Hub et en mission.

---

## PARTIE 5 — ARCHITECTURE TECHNIQUE

### 5.1 Structure des Dossiers ✅

```
Assets/
├── _Script/
│   ├── _Core/
│   │   ├── Bootstrap/          BootstrapLoader, RuntimeBootstrapper
│   │   ├── Events/             EventBus<T>, EventBusHelper, GameEvents
│   │   ├── Interfaces/         IInteractable
│   │   ├── Services/           GameManager, SceneLoader, SceneNames
│   │   └── SharedData/         Enums, MissionResult
│   ├── _Data/                  Tous les ScriptableObjects
│   ├── Systems/
│   │   ├── Inventory/          InventaireSystem
│   │   ├── Mission/            MissionSystem, MissionBuilder,
│   │   │                       CampaignMissionStarter, QuotaSystem
│   │   ├── Player/             PlayerController, PlayerCarry,
│   │   │                       PlayerInteractor, PlayerAnimator,
│   │   │                       PlayerNoiseEmitter
│   │   ├── Proprietaire/       ProprietaireAI, ParanoiaSystem
│   │   └── Vehicule/           VehicleRuntime, VehicleTrunkZone,
│   │                           VehicleAmbiance, VehicleHubSlot,
│   │                           VehiclePanelUI
│   │   HubManager, HubPNJ, MeubleInteractable,
│   │   OpenableInteractable, TiroirInteractable,
│   │   ValueObject, OptionsManager
│   └── UI_Persistent/
│       UIManager, UIPanel, HUDSystem,
│       DepartureConfirmationUI, MissionSummaryUI,
│       MenuUI, HubUI, MissionListUI, MissionPanelUI,
│       InventaireWheel, LabelInteractionUI, CrosshairUI,
│       PauseMenu, PauseMenuTrigger, OptionsUI,
│       KeyRebindUI, RebindOverlay, MissionLibreConfigUI
│
├── Prefabs/
│   ├── Player/                 PrefabCharacter, PlayerRoot, PlayerData.asset
│   ├── Habitation/             PrefabMaisonTest + HabitationTest.asset
│   ├── Proprietaire/           ProprioTest + ProprioTest.asset
│   ├── Meubles/                PrefabCommode
│   ├── Vehicules/Voiture/      PrefabVoiture + VoitureData.asset
│   ├── Objects/TV/             TV.prefab + TVData.asset
│   ├── Objects/Vase/           PrefabVase + VaseData.asset
│   └── Missions/               MissionListCampagn.asset,
│                               MissionTest_01.asset
│
├── Scenes/
│   ├── Bootstrap.unity         (Build Index 0)
│   ├── Menu.unity
│   ├── Hub.unity
│   ├── CharacterCustomization.unity
│   ├── UI_Persistent.unity     (chargée Additive, jamais déchargée)
│   └── Mission/
│       ├── Mission_Test.unity  (scène dev)
│       └── Mission_Libre.unity (missions procédurales)
│
└── Settings/
    URP-Balanced.asset, URP-HighFidelity.asset,
    URP-Performant.asset
```

---

### 5.2 Scènes & Flux

#### 7 Scènes Actives ✅

| Scène | Rôle |
|-------|------|
| Bootstrap | Initialisation, chargement des singletons |
| UI_Persistent | Chargée en Additive, jamais déchargée |
| Menu | Menu principal |
| Hub | Hub de l'agence (sélection mission/véhicule/shop) |
| CharacterCustomization | Personnalisation joueur |
| Mission_Test | Scène de test développeur |
| Mission_Libre | Missions procédurales via MissionBuilder |

*Stubs post-lancement dans `SceneNames.cs` :* `MISSION_01` à `MISSION_10`

#### Flux de Scènes ✅

```
[Bootstrap]
    ↓ (Build Index 0 — toujours en premier)
    ↓ Charge UI_Persistent (Additive, une seule fois)
[Menu]
    ↓ Jouer
[Hub]
    ↓ Confirmer mission + véhicule
[Mission_Test] ou [Mission_Libre]
    ↓ Confirmation départ joueur
[Hub] ← retour automatique après MissionSummaryUI
```

---

### 5.3 Services Fondamentaux

#### `GameManager` (Singleton DontDestroyOnLoad) ✅

| Propriété | Type | Rôle |
|-----------|------|------|
| MissionSelectionnee | MissionData | Mission choisie dans le Hub |
| VehiculeSelectionne | VehiculeData | Véhicule loué |
| Argent | float | Argent joueur (persiste entre missions) |
| DerniereMissionCompletee | int | Progression campagne |
| ContexteActuel | ContexteJeu | Contexte UI actuel |
| InputJoueurActif | bool | Input joueur activé/désactivé |

**Méthodes clés :** `SpawnerPlayerSiNecessaire()`, `LancerMission()`, `TerminerMission()`, `Debiter()`, `Crediter()`, `SetContexte()`

#### `SceneLoader` (Singleton DontDestroyOnLoad) ✅

- Seul point d'entrée pour toutes les transitions de scènes
- Appelle `EventBusHelper.ClearAll()` avant chaque transition
- Fade to black (0,5s) en entrée et sortie
- Charge `UI_Persistent` une seule fois en Additive
- Émet `OnSceneChargee` après chaque transition

#### `UIManager` (Singleton DontDestroyOnLoad) ✅

- Évalue tous les panels sur `OnContextChanged`
- `GetPanel<T>()` : accès type-safe
- `UpdateUIState()` : centralise curseur + input selon panels Blocking ouverts
- `FermerTousLesPanels()` : ferme proprement via `Fermer()`

---

### 5.4 Système d'Événements

**Scripts :** `EventBus<T>`, `EventBusHelper`, `GameEvents`

#### Architecture ✅

```csharp
// Publier
EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted {
    Position = pos,
    Level = NiveauBruit.Fort
});

// Écouter
EventBus<OnNoiseEmitted>.Subscribe(OnNoiseFn);

// Nettoyage entre scènes
EventBusHelper.ClearAll(); // appelé par SceneLoader avant chaque transition
```

**Règle fondamentale :** Toute communication inter-systèmes passe par EventBus. Jamais d'appels de méthodes directs entre systèmes distincts (sauf injection explicite de dépendances au spawn).

#### Tableau Complet des Events (`GameEvents.cs`) ✅

| Catégorie | Event | Payload principal |
|-----------|-------|-------------------|
| Objets | OnObjectLoaded | Object, Value, IsFragile |
| Objets | OnObjectDamaged | Object, ValueLost, Position |
| Paranoïa | OnParanoiaChanged | NewValue, OldValue, NewTier |
| Bruit | OnNoiseEmitted | Position, Range, NiveauBruit, Source |
| Quota | OnQuotaChanged | TotalValue, TargetValue, Percentage |
| Quota | OnQuotaReached | — |
| Quota | OnThresholdReached | Percentage |
| Propriétaire | OnOwnerStateChanged | OldState, NewState |
| Propriétaire | OnOwnerLeftHouse | — |
| Propriétaire | OnOwnerRetrievedObject | Object, Value |
| Véhicule | OnVehicleAttacked | Attacker, IsOwner |
| Pièges | OnTrapTriggered | Trap, Position, Victim |
| Inventaire | OnConsommableUsed | Nom, CoutUnitaire, Quantite |
| Mission | OnMissionStarted | Mission, Seed |
| Mission | OnMissionEnded | MissionResult |
| Animaux | OnAnimalAlerted | Position, Intensity, Species |
| Animaux | OnParrotSpoke | Phrase, IsClue |
| Scanner | OnScanPerformed | ScanPosition, RevealedObjects[] |
| Urgence | OnUrgencyTimerStarted | DurationSeconds |
| UI | OnMissionEndRequested | — |
| UI | OnDepartureConfirmed | Confirmed (bool) |
| UI | OnFadeToBlack | DurationSeconds |
| Interaction | OnInteractionLabelChanged | Label (string) |
| Scène | OnSceneChargee | NomScene |
| Input | OnInputStateChanged | InputActif (bool) |
| Contexte | OnContextChanged | NouveauContexte |
| Infractions | OnFineIssued ⚙️ | Description, Montant |
| Véhicule | OnVehicleDamaged ⚙️ | Type (enum), Cout |

---

### 5.5 Données — ScriptableObjects

#### Assets Create Menu ✅

| Asset | Create Menu | Description |
|-------|-------------|-------------|
| MissionData | BailiffCo/MissionData | Définition complète d'une mission |
| VehiculeData | BailiffCo/VehiculeData | Stats véhicule (coffre, sons, prix) |
| ProprietaireData | BailiffCo/ProprietaireData | Personnalité IA (3 curseurs) |
| ObjetData | BailiffCo/ObjetData | Objet saisissable (valeur, fragilité) |
| OutilData | BailiffCo/OutilData | Outil/consommable (3 niveaux) |
| AnimalData | BailiffCo/AnimalData | Espèce animale (réactions, paranoïa) |
| CachetteData | BailiffCo/CachetteData | Type de cachette |
| PiegeData | BailiffCo/PiegeData | Définition de piège |
| HabitationData | BailiffCo/HabitationData | Description du logement |
| NeighbourData | BailiffCo/NeighbourData | Type de voisin |
| PlayerConfigData | BailiffCo/PlayerConfigData | Config joueur (vitesse, portée, etc.) |
| OptionsData | — | Keybindings + préférences |
| ParanoiaConfig | — | Tweaking système paranoïa |
| MissionListData | — | Liste ordonnée de missions |

#### Assets Existants ✅

- `Assets/Prefabs/Missions/Campagn/MissionListCampagn.asset`
- `Assets/Prefabs/Missions/Campagn/Mission_01/MissionTest_01.asset`
- `Assets/Prefabs/Vehicules/Voiture/VoitureData.asset`
- `Assets/Prefabs/Objects/TV/TVData.asset`
- `Assets/Prefabs/Objects/Vase/VaseData.asset`
- `Assets/Prefabs/Player/PlayerData.asset`
- `Assets/Prefabs/Proprietaire/ProprioTest/ProprioTest.asset`
- `Assets/Prefabs/Habitation/HabitationTest/HabitationTest.asset`

---

### 5.6 Patterns Architecturaux

#### 1. Singletons Persistants (DontDestroyOnLoad)

`GameManager`, `SceneLoader`, `UIManager`
Règle : Instance créée une seule fois, doublon détruit à l'`Awake`

#### 2. Injection de Dépendances au Spawn

`MissionBuilder` et `CampaignMissionStarter` injectent après spawn :
- Dans `VehicleRuntime` : MissionSystem, PlayerCarry, QuotaSystem
- Dans `ProprietaireAI` : Transform joueur, Transform véhicule

#### 3. Persistance du Player

Player est DontDestroyOnLoad. `GameManager.SpawnerPlayerSiNecessaire()` le repositionne.
Force `OnDisable` → `OnEnable` pour re-souscrire à l'EventBus après nettoyage.

#### 4. Contextes & Curseur

| Contexte | Curseur verrouillé | Raison |
|----------|--------------------|--------|
| Menu | Non | Clics nécessaires |
| Hub | Non | Navigation PNJ |
| Mission | Oui | Vue FPS |
| Personnalisation | Non | Clics nécessaires |

Panel Blocking ouvert → override → curseur libre + input bloqué quelle que soit la scène

#### 5. Règles d'Or du Code

- ❌ Jamais de `FindObjectOfType` (performance)
- ❌ Jamais d'appel de méthode direct entre systèmes non liés → EventBus
- ❌ Jamais de `SetActive()` direct sur un UIPanel → `Ouvrir()`/`Fermer()`
- ✅ `SceneLoader` = seul moyen de changer de scène
- ✅ `EventBusHelper.ClearAll()` avant chaque transition de scène

---

### 5.7 Bootstrap & Initialisation ✅

#### Séquence Complète

```
1. Unity charge Bootstrap.unity (Build Index 0)
2. RuntimeBootstrapper (Editor) force ce comportement en Play Mode
3. BootstrapLoader.Start()
   ├── Vérifie GameManager.Instance != null
   ├── Vérifie SceneLoader.Instance != null
   ├── SceneLoader.ChargerUIPersistentAdditive()
   │     → LoadSceneAsync("UI_Persistent", Additive) une seule fois
   ├── Attend _delaiDemarrage secondes
   └── SceneLoader.ChargerScene("Menu", avecFondu: false)
4. SceneLoader.ChargerScene()
   ├── EventBusHelper.ClearAll()
   ├── Re-abonne SceneLoader + UIManager
   ├── LoadSceneAsync("Menu", Single)
   ├── GameManager.SetContexte(ContexteJeu.Menu)
   └── Émet OnSceneChargee
5. UIManager.EvaluerTousLesPanels(Menu)
   └── Active MenuUI, désactive tout le reste
```

---

### 5.8 Audio & Direction Sonore ⚙️

#### Sons du Joueur

| Son | Portée | Notes |
|-----|--------|-------|
| Pas normaux | ~4m | Inaudibles hors portée |
| Sprint | ~10m | |
| Atterrissage | Selon hauteur | Proportionnel à la chute |
| Porter objet lourd | ~2m | Effort audible |
| Porte forcée | Toute la maison | Bruit Très Fort |

#### Sons du Propriétaire

- Respiration audible quand proche
- Pas selon vitesse d'état (idle = lent, furious = course)
- Voix par palier de paranoïa (grognements, appels téléphoniques, cris)

#### Musique Dynamique

- Intensité suit le palier de paranoïa — crescendo vers Obsessionnel
- Musique propre à chaque proprio (ex: Marcel = valses de nains, Viviane = ses propres chansons ratées)
- Changement par transition de palier, pas par beat-matching brutal

#### Audio 3D

- Tous les sons ont une source 3D positionnée
- Localisation du proprio à l'oreille sans le voir (design intent)
- Atténuation spatiale réaliste (logarithmique)

#### Sons Cartoon Distinctifs

- Coffre de véhicule fermé = clic satisfaisant métallique
- Objet brisé = son cartoon exagéré
- Silence soudain du perroquet = signal d'alarme (sound design intent)

---

## PARTIE 6 — ÉTAT D'IMPLÉMENTATION & ROADMAP

### 6.1 Fonctionnalités Implémentées ✅

#### Core
- ✅ Bootstrap → Menu → Hub → Mission → Résultat → Hub (flux complet)
- ✅ GameManager, SceneLoader, UIManager (singletons persistants)
- ✅ EventBus architecture (40+ events)
- ✅ Fade to black entre scènes
- ✅ RuntimeBootstrapper (Editor — Play Mode toujours depuis Bootstrap)
- ✅ EventBusHelper.ClearAll() entre scènes

#### Joueur
- ✅ Déplacement 4 postures (debout/accroupi/allongé/sprint) + saut + coyote time
- ✅ Porter & lancer (PlayerCarry, physique masse)
- ✅ Interaction raycast (PlayerInteractor + IInteractable)
- ✅ Bruit émis (4 niveaux, throttle, surfaces)
- ✅ Animations Walking, Crouching

#### IA & Gameplay
- ✅ IA Propriétaire (8 états, 3 curseurs, formules calculées)
- ✅ Système Paranoïa (6 paliers, décroissance, API)
- ✅ Meubles poussables (MeubleInteractable)
- ✅ Portes/fenêtres/trappes (OpenableInteractable, 3 modes)
- ✅ Tiroirs (TiroirInteractable, libération des objets)
- ✅ Objets saisissables (ValueObject, dommages, scan)

#### Véhicule
- ✅ VehicleRuntime (SetTargetCollider, multi-colliders)
- ✅ VehicleTrunkZone (trigger debounce)
- ✅ VehicleAmbiance (sons ambiants)
- ✅ Flux de départ (DepartureConfirmationUI → ConvertObjectsToQuota)

#### Mission & Économie
- ✅ MissionSystem (stats, étoiles, bulletin de paie)
- ✅ QuotaSystem (seuils, events)
- ✅ CampaignMissionStarter (injection dépendances, fallback editor `_fallbackMissionData`)
- ✅ MissionBuilder (génération procédurale complète)
- ✅ MissionResult (structure complète)

#### Interface Utilisateur
- ✅ UIManager (contextes, panels, curseur)
- ✅ UIPanel (base class, EvaluerContexte)
- ✅ HUDSystem (quota, paranoïa, timer, notifications)
- ✅ DepartureConfirmationUI (popup Blocking)
- ✅ MissionSummaryUI (bulletin de paie)
- ✅ InventaireWheel (9 slots radiaux, hold Tab)
- ✅ MissionListUI, MissionPanelUI (fiche mission)
- ✅ VehiclePanelUI (fiche véhicule)
- ✅ HubUI (coordinateur)
- ✅ PauseMenu, OptionsUI (vidéo/audio/souris/rebinding 13 touches)
- ✅ LabelInteractionUI (label contextuel)
- ✅ HubPNJ (labels flottants 3D, conditions de déblocage)

#### Données
- ✅ MissionData, VehiculeData, ProprietaireData, ObjetData, OutilData
- ✅ AnimalData, CachetteData, PiegeData, HabitationData, NeighbourData
- ✅ InventaireSystem (outils + consommables, badge + téléphone de départ)

---

### 6.2 Plans Actifs 🔨

#### Plan 1 — Système Économique + MissionSummaryUI

- Enrichir `MissionResult` (listes détaillées objets/consommables)
- Compléter calcul dans `MissionSystem.EndMission()` (déductions A–D)
- Lever `OnConsommableUsed` depuis `InventaireSystem`
- Implémenter l'affichage complet dans `MissionSummaryUI`

#### Plan 2 — Véhicule & HUD Quota

- Corriger noms colliders dans `PrefabVoiture.prefab`
- Assigner layer Interactable sur ColliderDriverDoor/TrunkDoor
- `VehicleRuntime.AutoFindRefs()` + `OnObjectEnteredTrunk()` preview
- Assigner `_quotaSystem` + `_fallbackMissionData` dans `Mission_Test.unity`

#### Plan 3 — Menu & Gestion des Contextes

- `FermerTousLesPanels()` dans `UIManager` (appelle `Fermer()` proprement)
- Gestion curseur par contexte (`_curseurVerrouille`)
- `ActiverContexteXxx()` : fermer panels → set actifs → set curseur → UpdateUIState

#### Plan 4 — LabelInteractionUI Show/Hide

- `CanvasGroup.alpha` (0/1) au lieu de SetActive
- Label se cache quand `OnInteractionLabelChanged { Label = "" }`
- Keybind lu depuis `OptionsManager.GetTouche(ActionJeu.Interagir)`

---

### 6.3 Fonctionnalités Planifiées ⚙️

#### Critique (boucle de jeu incomplète)

- ⚙️ Pièges dynamiques (état Furieux de l'IA — placement en temps réel)
- ⚙️ Badge/Téléphone — data OK, logique d'effet absente
- ⚙️ Scan UV/Rayon X — data défini, non câblé en gameplay
- ⚙️ SaveSystem — complètement absent
- ⚙️ IA Voisins — NeighbourData complet, pas d'IA
- ⚙️ Cachettes joueur — CachetteData complet, interaction manquante
- ⚙️ Police — alerte par pièges, `OnUrgencyTimerStarted`
- ⚙️ Suspension missions (flag Suspendu → bloquer missions >75k€ pendant 3 missions)

#### Haute Priorité

- ⚙️ Feedback visuel dommages objets (shader/matériau cassé)
- ⚙️ Système de dénonciation policière (menottes, gaz → preuves → amendes section D)
- ⚙️ Dégâts véhicule par PNJ (rétroviseur −50€, accident −500€)
- ⚙️ Avocat (bloque départ pendant `LawyerBlockDuration`)
- ⚙️ Shop Hub (panel + PNJ secrétaire)
- ⚙️ Inventaire/Casier Hub (équipement avant mission)

#### Confort & Polish

- ⚙️ Personnalisation personnage (scène existe, logique éditeur manquante)
- ⚙️ Archives/consultation de dossiers
- ⚙️ Mode Sandbox complet (MissionLibreConfigUI Canvas + bouton Hub)
- ⚙️ Comportements de gêne passive du proprio (8 comportements)
- ⚙️ Musique dynamique par mission
- ⚙️ Sons complets de tous les objets/actions
- ⚙️ Coopératif (Unity Netcode)

---

### 6.4 Backlog Complet

#### UI / Panels

- MissionLibreConfigUI — ajouter Canvas dans `UI_Persistent.unity` + GO interactable Hub (bouton "Mode Libre")
- ShopPanel — panel + PNJ secrétaire interactable dans le Hub
- Inventaire/Casier Hub — équipement avant chaque mission
- PersonnalisationPanel — éditeur complet visage/tenue/chapeau

#### Outils Permanents (Mesh + Data : coût achat, coût upgrade par level)

Badge, Téléphone d'huissier, Pied de biche, Kit de crochetage, Grappin, Lampe UV, Appareil photo d'huissier, Scanner Rayon X, Marteau de démolition, Gants de protection, Lampe de poche, Tournevis

#### Objets Consommables (Mesh + Data : coût achat + utilisation)

Taser, Filet de capture, Appât alimentaire, Cadenas, Faux document, Antivol, Objet leurre, Spray répulsif (animal), Antidote, Kit de premiers secours, Spray soporifique, Gilet de protection, Tracker GPS

#### Assets de Valeur

~20 objets de valeur pour commencer (Mesh + ObjetData)

#### Véhicules (Mesh + VehiculeData)

Vélo cargo, Scooter, Pickup, Âne, Fourgon, Camion de glace, Hélicoptère, Remorque

#### Gameplay — Mécaniques

- Cachettes joueur (double fond tiroir, derrière tableau, sous tapis, cave secrète)
- Pièges préplacés (seau d'eau, faux plancher, colle industrielle, alarme infrarouge, animal venimeux)
- Système dénonciation policière (preuves, `OnFineIssued`)
- Dégâts véhicule par PNJ (`OnVehicleDamaged`)
- Suspension de missions (>25% quota → bloque missions >75k€, 3 missions)
- Actions joueur : escalader rebord, se cacher, nager, frapper/sonner

#### PNJ (Mesh + animation + Data)

2 propriétaires distincts, Voisin curieux, Voisin complice, Voisin voleur, Ami costaud, Avocat, Livreur, Journaliste, Agent de police

#### Animaux (Mesh + animation + AnimalData)

Chat, Chien (compagnie), Chien (garde), Perroquet, Lapin, Tortue, Poisson (aquarium), Serpent (terrarium), Araignée/Scorpion, Oiseau exotique (volière), Anguille électrique

#### Animations

- **Joueur** : prendre objet, jeter, poser, courir, sauter, accroupi, allongé, nager, escalader
- **Proprio** : marcher, courir, lacer ses chaussures, s'étirer, fouiller le coffre, prendre/poser objet, chercher ses clés, s'adosser, déplacer meuble, téléphoner en marchant

#### Hub Assets

Chef, Secrétaire (boutique), Garagiste (réparation véhicule si abîmé → frais déduits post-mission)

#### Audio

Implémentation complète musique + sons (ambiance, actions, PNJ, animaux, UI)

#### Technique

Coopératif (Unity Netcode for GameObjects — `RequiresTwoPlayers`, synchronisation, salle Hub)

---

### 6.5 Ordre Logique d'Implémentation

Organisation par **dépendances techniques** et **valeur gameplay** — chaque phase doit être jouable et testable avant la suivante.

---

#### Phase 1 — Boucle Core Solo (MVP jouable) 🔴 Critique

**But :** Entrer dans une maison, prendre un objet, partir.

1. Déplacement joueur (4 postures, sprint, saut + coyote time)
2. Interaction (raycast IInteractable, label contextuel)
3. Porter & Lancer (PlayerCarry, physique masse)
4. Bruit émis (4 niveaux, throttle)
5. Portes/fenêtres/tiroirs (OpenableInteractable, TiroirInteractable)
6. Objets saisissables (ValueObject, valeur, bruit chute)
7. Véhicule : coffre ouvrable + zone trigger + chargement
8. Quota HUD (QuotaSystem + HUDSystem barre + texte)
9. Départ (DepartureConfirmationUI → EndMission)
10. MissionSummaryUI basique (étoiles + salaire net)

**Critère :** Scène Mission_Test jouable de bout en bout depuis l'éditeur.

---

#### Phase 2 — IA Propriétaire & Paranoïa 🔴 Critique

**But :** Le proprio réagit à tes actions — la tension existe.

1. ParanoiaSystem (sources, paliers, décroissance)
2. ProprietaireAI — états Idle + Alert + Investigate
3. ProprietaireAI — état Confront (badge réduit paranoïa)
4. ProprietaireAI — état Panic + Outdoor (sort vers véhicule)
5. ProprietaireAI — état Furious
6. ProprietaireAI — état Locked (menottes)
7. Comportements de gêne passive (8 comportements selon palier)
8. Icône paranoïa HUD (6 sprites)

**Critère :** Deux proprios avec personnalités différentes ont des comportements visiblement distincts.

---

#### Phase 3 — Menu → Hub → Mission → Hub (Flux Complet) 🔴 Critique

**But :** Le jeu se démarre depuis le menu, pas depuis l'éditeur.

1. Bootstrap correct (RuntimeBootstrapper, BootstrapLoader)
2. MenuUI (Jouer / Options / Quitter)
3. SceneLoader (transitions + fade + EventBusHelper.ClearAll)
4. GameManager transporte MissionData + VehiculeData
5. HubUI basique (argent HUD, état mission choisie)
6. HubPNJ Agent Missions → MissionListUI → MissionPanelUI
7. HubPNJ Garagiste → VehiclePanelUI → location
8. CampaignMissionStarter (injection dépendances, fallback editor)
9. Retour Hub après mission (TerminerMission, Crediter)

**Critère :** Bootstrap → Menu → Hub → Mission → Hub sans crash.

---

#### Phase 4 — Économie Complète & Bulletin de Paie 🟡 Haute Priorité

**But :** Le joueur comprend ce qu'il gagne et pourquoi.

1. MissionResult complet (tous les champs, listes détaillées)
2. MissionSystem.EndMission() calcul complet (A–D)
3. MissionSummaryUI complet (ScrollRect, sections, couleurs vert/rouge)
4. Dommages objets (ValueObject collision, OnObjectDamaged)
5. Saisie excessive (calcul 3 paliers + flag Suspendu)
6. Déductions véhicule (location automatiquement déduite)
7. OnConsommableUsed depuis InventaireSystem

**Critère :** Bulletin de paie correct et lisible après chaque mission.

---

#### Phase 5 — Progression & Méta-Jeu 🟡 Haute Priorité

**But :** Le joueur progresse entre les missions.

1. SaveSystem (argent, étoiles par mission, outils débloqués)
2. InventaireSystem complet (achat, niveaux, consommables)
3. HubPNJ Boutique → ShopPanel (acheter/améliorer outils)
4. HubPNJ Garagiste → VehiclePanelUI (déblocage via UnlocksAfterMission)
5. Roue d'Inventaire en mission (InventaireWheel complet avec effets)
6. 5 missions campagne (MissionData assets + maisons prefabs)

**Critère :** Progression de Marcel à DJ Pharaon sans bloquer.

---

#### Phase 6 — Outils & Effets 🟡 Haute Priorité

**But :** Les outils changent la façon de jouer.

1. Badge (réduction paranoïa en Confront)
2. Téléphone (scan objet → valeur + nom complet)
3. Pied-de-biche (forcer porte verrouillée, bruit toute la maison)
4. Kit de crochetage (mini-jeu ouverture serrure)
5. Menottes certifiées → état Locked du proprio
6. Spray répulsif animal
7. Antivol véhicule (protection coffre)
8. Lampe UV (révèle pièges + cachettes)
9. Scanner Rayon X (voir à travers murs)

**Critère :** 3+ outils utilisables changent visiblement la stratégie.

---

#### Phase 7 — Pièges & Cachettes 🟡 Haute Priorité

**But :** La maison est dangereuse et pleine de surprises.

1. PiegeSystem basique (5 premiers pièges avec indicateurs visuels)
2. Pièges préplacés (nombre selon `Méthode` du proprio)
3. CachetteSystem (double fond, derrière tableau, sous tapis)
4. Interaction cachettes (révélation objets cachés)
5. Pièges dynamiques (état Furious → proprio pose pièges en temps réel)
6. Scanner UV → révèle indicateurs pièges

**Critère :** Mission avec proprio Méthodique 9/10 est vraiment dangereuse.

---

#### Phase 8 — Animaux 🟠 Moyen Terme

**But :** La faune de la maison crée tension et humour.

1. Chat (alerte locale, distraction appât)
2. Chien de compagnie (aboie, distrait avec appât)
3. Chien de garde (patrouille, poursuite, morsure)
4. Perroquet (OnParrotSpoke avec phrases-indices réels)
5. Aquarium / Terrarium (contenants spéciaux, dangers anguille/serpent)
6. Autres espèces (lapin, tortue, reptile)

**Critère :** Perroquet donne un indice réel sur une cachette de valeur.

---

#### Phase 9 — Voisins & PNJ Secondaires 🟠 Moyen Terme

**But :** Le monde extérieur réagit aussi.

1. Voisin curieux (observe, appelle police après 3min)
2. Avocat (bloque le départ, mini-jeu vérification mandat)
3. Ami costaud (bloque couloir, doit être contourné)
4. Voisin voleur (force coffre ouvert >3min)
5. Livreur (event aléatoire, distraction du proprio)
6. Agent de police (timer d'expulsion)

**Critère :** Partie avec proprio Sociabilité 9 = chaos gérable.

---

#### Phase 10 — Polish Mission & Environnement 🟠 Moyen Terme

**But :** L'environnement est cohérent et immersif.

1. Gabarits de maisons (5 types avec SpawnPoints tagués)
2. Meubles poussables (MeubleInteractable, bruit selon surface)
3. Escalader un rebord / Se cacher dans cachette
4. Dégâts véhicule par PNJ (swap prefab rétroviseur/accidenté)
5. Système dénonciation policière (preuves, amende section D)
6. Mode Sandbox complet (MissionLibreConfigUI Canvas + bouton Hub)

**Critère :** Villa Mission 5 (DJ Pharaon) jouable et tendue de bout en bout.

---

#### Phase 11 — Personnalisation & Hub Assets 🟢 Long Terme

**But :** Le joueur s'approprie son personnage et son agence.

1. Éditeur de skin complet (cheveux, tenue, chapeau, etc.)
2. Hub visuel complet (assets PNJ définitifs — Chef, Secrétaire, Garagiste)
3. Inventaire/Casier Hub (équipement avant mission)
4. HubPNJ Archiviste (missions libres, après campagne)
5. HubPNJ Tailleur (personnalisation)
6. Commentaires PNJ selon score (★ = froid, ★★★ = admiratif)

**Critère :** Session Hub → Mission = expérience narrative complète.

---

#### Phase 12 — Audio Complet 🟢 Long Terme

**But :** Le jeu sonne comme un jeu fini.

1. Sons d'actions joueur (pas, sprint, atterrissage, porter/lancer)
2. Sons du proprio (respiration, voix par palier)
3. Sons cartoon distinctifs (coffre fermé, objet brisé, notification quota)
4. Musique dynamique par palier paranoïa
5. Musique spécifique par proprio
6. Audio 3D complet (localisation proprio à l'oreille)
7. Sons animaux (chaque espèce a sa signature)

**Critère :** Joueur peut localiser le proprio à l'oreille sans le voir.

---

#### Phase 13 — Coopératif 🟢 Long Terme / Post-MVP

**But :** 2 joueurs dans le même Hub, même mission.

1. Unity Netcode for GameObjects (setup réseau)
2. Code Hub alphanumérique + invite Steam
3. Synchronisation joueur (position, animation, inventaire)
4. Table d'inventaire partagée (hôte = source de vérité)
5. Règles consommables (max 3/type, retour table au déco)
6. Mécaniques coop exclusives (transport lourd, extraction fenêtre, libération piège)
7. Bonus coop +20% valeur
8. Synchronisation fin de mission sur save hôte

**Critère :** 2 joueurs finissent Mission 1 ensemble sans désync.

---

### 6.6 Plan Commercial

| Version | Prix | Contenu |
|---------|------|---------|
| V1 Prototype | Gratuit (itch.io) | Feedback uniquement |
| V2 Démo Steam | Gratuit | Page Steam, wishlists |
| V3 Early Access | 9,99 $ | 5 missions + sandbox |
| V3 Version Finale | 14,99 $ | 10 missions + polish post-reviews |
| DLC post-lancement | 4,99 $/pack | 2 gabarits + 1 archétype + 20 objets |
| Bundle Coop | 22,99 $ (2 copies) | Jouer avec un ami |

**Lancement Early Access prévu : 2027**

---

*GDD_MASTER.md — Version 8.0 — Mis à jour le 2026-05-09*
*Généré à partir de : GDD v7.0 (docx), plans de session, audit complet des scripts du projet*
