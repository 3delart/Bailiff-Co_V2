---
name: Deferred Tasks — Backlog complet
description: Liste exhaustive de tout ce qui reste à faire, organisé par catégorie. Synchronisé avec GDD 6.3 (Fonctionnalités Planifiées). Mis à jour 2026-05-14 (audit + corrections).
type: project
---

## ⚠️ CRITIQUE — Boucle de jeu incomplète

### SaveSystem
- Complètement absent
- Persister: argent joueur, missions complétées + étoiles, véhicules débloqués, options
- Format: PlayerPrefs JSON ou fichier binaire

### Systèmes Fondamentaux Manquants
- **Badge/Téléphone Officiel** — data OK, gameplay absent. Badge réduit paranoïa quand présenté au proprio. Téléphone: appels du chef (tutoriel), contacter secrétaire.
- **Scan UV/Rayon X** — data définies, non câblés en mission. Dévoile objets cachés (cachettes, derrière objets).
- **IA Voisins** — NeighbourData complet, aucun comportement. Voisin curieux (regarde par fenêtre), complice (aide joueur), voleur (entre coffre), costaud (reprend objets).
- **Interaction Cachettes Joueur** — CachetteData complet, joueur ne peut pas entrer. Permettre se cacher dans placard/sous lit/derrière rideau (Ctrl+E).
- **Police & Urgency Timer** — alerte par pièges, propriétaire dénonciation. Timer rouge, musique urgente, joueur doit partir avant expulsion.
- **Suspension Missions** — si saisie > +25% quota, flag `MissionResult.Suspendu = true`. Implémenter côté Hub: missions > 75 000€ bloquées 3 missions suivantes.

## 🎬 Missions & Contenus Campagne

### 5 Missions Scénarisées — Data + Scènes

| # | Titre | Proprio | Gabarit | Notes |
|---|-------|---------|---------|-------|
| 1 | **La Saisie de Marcel** | Marcel Dupont (Le Rêveur) | Maison de ville Petit | Tutoriel, bases, appels du Chef |
| 2 | **Le Colonel et ses gadgets** | Colonel Renard (Le Fougueux) | Maison de ville Moyen | Pièges, pied-de-biche, IA réactive |
| 3 | **Le Clip de Viviane** | Viviane de La Fontaine (La Bavarde) | Maison de ville Grand | Renforts, voisins, gestion temps |
| 4 | **Le Labo du Professeur** | Pr. Archibald Knox (Le Précis) | Laboratoire Moyen | Cachettes complexes, scanner RX, animaux exo |
| 5 | **Le Studio de DJ Pharaon** | DJ Pharaon (Le Tactique) | Villa Grand | Mission complète — tous systèmes |

**Par mission:** Créer `MissionData` + scène Unity + propriétaire asset + habitation asset.

### 5 Gabarits de Maisons — Mesh + Layout

| Gabarit | Pièces | Taille | Complexité |
|---------|--------|--------|------------|
| **Appartement** | Salon, cuisine, 2 ch., SdB, balcon. 3ème étage | Pet/Moy | 6/10 |
| **Maison de ville** | RdC (salon, cuisine, bureau), Étage (3 ch.), Cave, Jardin | Moy/Grd | 8/10 |
| **Villa** | 2 étages, double garage, piscine, dépendance | Grand | 9/10 |
| **Manoir** | Grande entrée, bibliothèque, 4 ch., cave, grenier | Grand | 10/10 |
| **Laboratoire reconverti** | Open space lab, bureau, salle blanche, entrepôt, sous-sol | Moy/Grd | 10/10 |

### Tutoriel — Mission 1 (Appels du Chef) ⚙️

**Via appels téléphoniques diegétiques** — le chef appelle en cas de bêtise (casse objet, déclenche piège).

- Spawn: "Frappe à la porte et présente-toi. T'as ton badge?"
- Entrée: "Commode à l'entrée. Sors ton téléphone, scanne l'objet."
- Premier objet scanné: "Prends-le, mets-le dans le coffre."
- Premier objet chargé: "Bien. Continue. Une fois quota atteint, tu rentres."
- Animal nearby: "Il a un [chien/chat]. Surtout ne l'énerve pas. Sardines en tool."
- Piège déclenché: "T'as marché dans quoi là?! Regarde où tu mets les pieds."
- Quota atteint: "C'est bon, charge et rentre. Laisse pas traîner."
- Objet cassé: "...t'as encore tout cassé? Je te retire ça du salaire."
- Coffre caché trouvé: "Jackpot. Le proprio savait pas que je savais. Check."

## 🎭 Viral Scenes — 8 Scènes Scripées Aléatoires

Tirage 1–4 scènes au spawn selon taille mission. Se déclenchent si conditions remplies. Manquées = incitation rejouer.

| # | Nom | Condition | Description |
|---|-----|-----------|-------------|
| 1 | **Voisin Voleur** | Joueur en maison + quota >20% + coffre ouvert | Voisin au coffre → joueur sort → voisin fuit avec 1 objet. Proprio reprend 1 objet pendant poursuite. |
| 2 | **Fan de Foot** | Joueur porte une TV | Voisin baraqué reprend TV des mains joueur, s'installe match. |
| 3 | **Livreur Suspect** | Aléatoire 30% après 3min | Colis suspect sonne → proprio distrait 60s. |
| 4 | **L'Avocat de Minuit** | Paranoia > seuil sociabilité | Avocat costard surgit, serviette en main. Bloque départ ⚙️. |
| 5 | **Le Perroquet Traître** | Joueur passe cage | Répète code coffre/clé (utile) ou crie phrases proprio (saboteur). |
| 6 | **L'Ami Inattendu** | Paranoia basse + aléatoire | Ami arrive bières, s'installe salon. |
| 7 | **Proprio Somnambule** | Heure mid-mission | Proprio yeux fermés traverse pièce où joueur caché. |
| 8 | **Peignoir & Claquettes** | Idle aléatoire extérieur | Proprio peignoir + claquettes + chaussettes action absurde jardin. |

## ⚠️ IA PROPRIÉTAIRE — Implémentation POST-Boucle

`ProprietaireAI.cs` structure existe (524 lignes, state machine skeleton). **À compléter une fois boucle mission stable.**

- **État actuel:** Stub fonctionnel. Subs EventBus (paranoïa, bruit, seuils). NavMeshAgent injecté. Animator refs manquantes.
- **À implémenter:**
  - 8 états (Idle → Searching → Chasing → Furious + Calm/Sociable) avec transitions
  - Comportements passifs (faire lacets, s'étirer, manger, regarder TV, etc.)
  - Detection joueur (vision + bruit cones)
  - Pathfinding vers joueur/coffre/vehicle/objets
  - Dénonciation policière (trigger OnFineIssued)
  - Animations par état (idle, walk, run, panic, etc.)
  - Audio respiration, voix paranoïa

**Dépendances:** ParanoiaSystem OK. PlayerNoiseEmitter OK. Animation assets manquantes.

---

## 🎮 Gameplay — Systèmes & Mécaniques

### ⚙️ SYSTÈME DE POSE PRÉCISE (PlacementPreview) — ACTIF

- Ghost preview transparent sur surface (raycast depuis caméra)
- Normal threshold ≤45° (pente acceptable)
- Rotation via scroll wheel (±45°/cran)
- Clic gauche maintenu = preview, relâchage = pose, clic droit = annuler
- Event `OnObjectPlaced` levé au placement
- **Fichiers:** `PlacementPreview.cs` (nouveau), modifs `PlayerCarry.cs`, `PlayerConfigData.cs`, `GameEvents.cs`

### ⚙️ FIX DÉPLACEMENTS BLOQUÉS — ACTIF

1. **ConsumeMouseDelta() jamais appelé** → saut caméra brutal au déblocage input
2. **`_meubleInteractable` coincé en push** → vitesse réduite à 25% permanent
3. **`Appui()`/`Maintenu()` → false si `OptionsManager.Instance == null`** → accroupi/allongé/saut morts
4. **`AdapterHauteur()` lerp asymptotique** → joueur coincé mi-hauteur sous plafond
5. **`_velociteY` accumule pendant lock** → chute brutale au déblocage
6. **ForceUnstuck()** sur hold Escape 3s (reset postures + input)

**Fichiers:** `PlayerController.cs`, `PlayerInteractor.cs`

### ⚙️ MEUBLEINTERACTABLE SILENCE — ACTIF

- Actuellement: push silencieux (zéro bruit)
- À implémenter: `EmettreBruit(Leger)` si vitesse > seuil

### Dommages & Feedback
- **Feedback visuel dommages objets** — shader/matériau cassé (shader fissures, couleur desaturée)
- **Déformation progressive** — % dégâts visible (objet penche, éclat apparaît à 50%+)

### Systèmes Avancés
- **Pièges Dynamiques** — IA état Furieux place pièges en temps réel (seau porte, faux plancher, etc.)
- **Dénonciation Policière Complète** (ProprietaireAI)
  - Menottes: timer avant libération. Retrait avant fin = pas de trace. Sinon tombent = preuve.
  - Gaz soporifique: joueur doit ramasser/cacher (poubelle, coffre, etc.). Visible = preuve.
  - Event: `OnFineIssued { string Description, float Montant }` levé quand propriétaire dénonce.
  - Amendes section D du bulletin de paie.
- **Dégâts Véhicule par PNJ** — pendant joueur en maison, PNJ peut abîmer:
  - Rétroviseur cassé: bruit + swap prefab → retenue −50€ dans récap.
  - Véhicule accidenté: gros bruit collision + swap prefab accidenté → −500€.
  - Event: `OnVehicleDamaged { VehicleDamageType Type, float Cout }` (enum Retroviseur/Accident).

### Comportements Passifs Propriétaire — 8 Idle Animations ⚙️
Intéressé, occupé, paranoia basse/élevée — faire ses lacets, s'étirer, manger, regarder TV, fouiller tiroir, etc.

### Bonus Temps ⚙️
Si mission < `BonusTimeThresholdSeconds` → bonus étoile supplémentaire. Implémenter dans `MissionSystem.CalculateStars()`.

## 🛠️ Outils & Consommables — Mesh + Data

### Outils Permanents (achat + upgrade 3 niveaux)
Badge, Téléphone d'huissier, Pied de biche, Kit de crochetage, Grappin, Lampe UV, Appareil photo d'huissier, Scanner Rayon X, Marteau de démolition, Gants de protection, Lampe de poche, Tournevis

### Consommables (achat + utilisation)
Taser, Filet de capture, Appât alimentaire, Cadenas, Faux document, Antivol, Objet leurre, Spray répulsif (animal), Antidote, Kit de premier secours, Spray soporifique, Gilet de protection, Tracker GPS

## 💎 Assets de Valeur — ~20 Objets

- ✅ Vase (PrefabVase.prefab + VaseData.asset)
- ⏳ Mesh + Data: TV, Tableau, Bijoux, Montre, Statue, Horloge, Lampe design, Livre rare, Disque vinyle, etc.

## 🚗 Véhicules — Mesh + Data Complet

- ✅ Architecture Vehicle.cs (fusion VehicleRuntime + VehicleHubSlot + VehicleAmbiance)
- ✅ Remorques: Petite remorque + Moyenne remorque (avec AttachTrailer)
- ✅ Système d'options véhicule (VehicleOption + VehicleOptionType)
- ⏳ Mesh + data complets pour tous véhicules: VeloCargo, Scooter, Pickup, Âne, Fourgon, Camion Glace, Hélicoptère

## 👥 PNJ & Animaux

### Propriétaires — 2 Archetypes Minimum
Valider sliders Réactivité/Méthode/Sociabilité avec 2 proprios distincts.

### Voisins & NPCs — Mesh + Animation + Data
Voisin curieux, complice, voleur, Ami costaud (renfort), Avocat (renfort légal, bloque départ ⚙️), Livreur (aléatoire), Journaliste, Agent de police

### Animaux — Mesh + Animation + Data
Chat, Chien (compagnie), Chien (garde), Perroquet, Lapin, Tortue, Poisson (aquarium), Serpent (terrarium), Araignée/Scorpion, Oiseau exotique (volière), Anguille électrique

## 🎬 Animations

### Joueur
Prendre objet, jeter, poser, courir, sauter, accroupi, allongé, sprint, climber, interact (ouvrir, scanner, etc.)

### Propriétaire
Marcher, courir, faire lacets, s'étirer, fouiller coffre, prendre/poser objet, chercher clés, s'adosser mur, déplacer meuble, panique, colère

## 🏢 Hub — NPCs & Assets Visuels

- **NPCs:** Chef, Secrétaire (shop), Garagiste (réparation véhicule si abîmé → frais déduits après mission)
- **Scène visuels complets:** Revêtement murs, meubles, décoration, éclairage, ambiance

## 🎵 Audio

### Musique Dynamique
- Intensité suit paranoïa — crescendo vers Obsessionnel
- Musique propre par proprio (ex: Marcel = valses, Viviane = ses chansons ratées)
- Changement par palier, pas beat-matching brutal

### Sons Thématiques
- Coffre véhicule fermé = clic satisfaisant métallique
- Objet brisé = cartoon exagéré
- Silence perroquet = signal alarme (sound design)
- Respiration proprio quand proche
- Pas selon vitesse état (idle lent, furieux course)
- Voix par paranoïa (grognements, appels, cris)

## 🎨 UI & Customisation

### MissionLibreConfigUI — Complète ⚙️
- Canvas dans `UI_Persistent.unity` (existe partiellement)
- Bouton Hub → `HubManager.OuvrirMissionLibreConfig()`
- Sliders: Réactivité/Méthode/Sociabilité/Difficulté (1–10)
- Type maison + Taille + Complexité
- Seed partageable
- Présets: Facile/Normal/Difficile/Cauchemar

### Shop Panel ⚙️
Panel + GO interactable Hub (secrétaire). Acheter/améliorer outils.

### Inventaire / Casier Hub ⚙️
Espace rangement — joueur s'équipe avant mission.

### Archives / Consultation Dossiers ⚙️
Consulter missions passées, étoiles, temps, bulletins.

### Personnalisation Personnage ⚙️
Scène existe (`CharacterCustomization.unity`), logique éditeur manquante.
- Cheveux: style + couleur
- Yeux: forme + couleur (hétérochromie)
- Bouche: sourire/neutre/sérieux/dents
- Pilosité: barbe (naissante/courte/longue/moustache/barbichette/pattes)
- Chapeau: casquette, melon, béret, haut-de-forme
- Veste: couleur, style (classique, sport, décontracté, motifs)
- Pantalon: slim, large, bermuda, salopette
- Cravate/Col: nœud pap, col ouvert, badge mis en valeur
- Chaussures: derbies, baskets, bottes, mocassins + couleur

**Règle:** Tout dispo dès départ, pas de déblocage. Lié à save. Visible Hub + mission.

## 🔧 Technique

### Coopératif
Rendre jeu coop possible (Unity Netcode ou Photon).

### Misc
- Système complet d'inventaire joueur
- HUB prise d'objets avant mission

---

## 📋 Priorité d'Implémentation

1. **Phase 1 — Critiques:** SaveSystem, Badge/Téléphone, Scan, Voisins IA, Cachettes joueur, Police/Urgency
2. **Phase 2 — Missions:** 5 missions campagne + scènes, 5 gabarits maisons, tutoriel
3. **Phase 3 — Systèmes:** Viral scenes, dégâts véhicule, dénonciation complète, bonus temps
4. **Phase 4 — Polish:** Audio musique dynamique, animations passives, shop, archives, coop

---

## Notes

Ce backlog est exhaustif et synchronisé avec GDD 6.3. Priorité actuelle: boucle campagne complète (Mission 1–5 jouables + SaveSystem). Chaque item devient tâche concrète lors travail.
