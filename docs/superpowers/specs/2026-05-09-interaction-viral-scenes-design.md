# BailiffCo — Design Spec : Interactions, Paranoia & Scènes Virales
**Date :** 2026-05-09
**Statut :** Draft — à merger dans GDD_MASTER v9.0
**Sections GDD concernées :** 1.2, 2.1.3, 2.3, 2.4, nouvelle section 2.12

---

## 1. Repositionnement — Pas un jeu de ninja

Le jeu n'est pas un simulateur de furtivité pure. Le joueur incarne un huissier **légalement mandaté** : il peut passer devant le propriétaire avec un objet de valeur à la main. La tension vient du **comment**, pas du **si**.

Deux approches légitimes coexistent :

| Approche | Description | Conséquence |
|----------|-------------|-------------|
| **Officielle** | Sonner / frapper → se présenter → opérer ouvertement | Paranoia de base basse, proprio vous surveille |
| **Furtive** | Entrer sans prévenir (fenêtre, grappin, porte forcée) | Paranoia = 0 au départ, mais découverte = proprio vous prend pour un cambrioleur |

Le joueur choisit son style mission par mission. Les deux sont viables, les deux ont des risques distincts.

---

## 2. Système d'Interaction Joueur-PNJ

### 2.1 Slot Officiel — Badge + Mandat

Un seul slot d'inventaire permanent contient les deux documents officiels du joueur :
- **Badge** : identifiant de l'huissier
- **Mandat** : ordre de saisie (= la fiche de mission récupérée dans le Hub avant le départ)

Ce slot est **toujours présent**, non consommable, non perdable. Le joueur récupère la fiche de mission dans le Hub ; le badge est acquis dès le début du jeu.

### 2.2 Double Action sur les Interactables

Les interactables clés supportent deux actions :

| Touche | Action | Exemple |
|--------|--------|---------|
| `E` | Action primaire | Ouvrir une porte, prendre un objet, présenter badge |
| `F` | Action secondaire | Frapper à la porte, discuter, appuyer sur une sonnette |

La **sonnette** et le **frapper à la porte** sont deux interactables distincts sur la porte d'entrée.

### 2.3 Présentation du Badge

Si `PlayerCarry` tient le slot officiel (badge/mandat) :
- `E` face à un PNJ en état `Confront` ou `Idle` → **présenter officiellement** le badge
- `F` face à un PNJ → **engager une discussion** (interaction moins formelle)

Le badge est un objet portable comme les autres : le tenir en main occupe le slot carry, ce qui empêche de porter un objet de valeur simultanément.

**Effets de la présentation selon le PNJ :**

| PNJ | Document demandé | Effet si présenté | Effet si refus |
|-----|-----------------|-------------------|----------------|
| Proprio (Idle/Alert) | Badge | Paranoia -10, stoppe l'escalade | Paranoia +15 |
| Proprio (Confront, paranoia < seuil) | Badge | Paranoia -20, retour Idle | Paranoia +25, appel avocat |
| Proprio (Confront, paranoia ≥ seuil) | Badge + Mandat | Paranoia -10, reste méfiant | État Furious |
| Avocat | Badge + Mandat | Avocat repart, Paranoia -15 | Amendes fin de mission |
| Policier | Badge + Mandat | Policier repart après vérification | Joueur bloqué +120s |

### 2.4 Entrée Furtive — Conséquences

Si le joueur entre sans se présenter (fenêtre, grappin, porte forcée) et est découvert :
1. Proprio passe directement en état `Furious` (pas `Confront`)
2. Il peut **mettre le joueur dehors** physiquement (animation : escorte vers la sortie)
3. Il appelle la police
4. **Amende** pour infiltration en fin de mission
5. Le joueur **peut continuer la mission** s'il présente badge + mandat à la police à son retour

### 2.5 Objets Illégaux — Système de Preuves

Les consommables illégaux (menottes, gaz soporifique, etc.) laissent une trace physique :
- Si le joueur oublie de récupérer l'objet illégal, le **proprio peut le ramasser**
- Proprio avec preuve en main → appelle la police immédiatement
- Police arrive → joueur bloqué pendant `VérificationDurée` (configurable, ~90s)
- **Pendant ce blocage**, le proprio ou un voisin peut voler dans le coffre du véhicule

Le joueur doit donc "faire le ménage" après usage d'un outil illégal.

**Échappatoire — Se cacher de la police :**
- Quand la police est appelée, elle entre dans la maison et **cherche activement** le joueur pendant `PoliceSearchDuration` (configurable, ~120s)
- Si le joueur est dans une **cachette valide** (placard, sous un lit, derrière un rideau) et n'est pas trouvé dans ce délai → le policier repart, **aucune amende**
- Si le joueur est trouvé dans la cachette ou à découvert → bloqué pour vérification + amende
- La police cherche en priorité les pièces où des bruits ont été entendus récemment
- Pendant que le joueur se cache, le proprio (et éventuellement un voisin) peut toujours accéder au coffre

---

## 3. Système de Paranoia — Clarifications

> **TODO :** Renommer `Paranoia` → [nom final à choisir lors d'une refacto] dans `ParanoiaSystem.cs`, `ParanoiaConfig`, `ProprietaireData`. Piste : `Résistance`, `Désespoir`, `Hostilité`.

### 3.1 Sémantique

La jauge ne représente pas une paranoïa irrationnelle. Elle mesure la **réaction proportionnelle du proprio à la perte réelle et aux actions observées**. Il a peur de ce qu'il perd, pas de phantômes.

### 3.2 Sources — Nouveau Modèle

| Source | Condition | Delta |
|--------|-----------|-------|
| `QuotaRatio` | `valeurChargée / quotaMission` × coeff | Passif, toujours actif |
| `VuPortantObjet` | Joueur visible + porte un objet de valeur | +10 à +20 selon valeur |
| `ActionDestructive` | Marteau démolition utilisé **et proprio à portée d'ouïe** | +25 (distinct du bruit — déclenche aussi transition directe vers Investigate) |
| `CachetteTrouvée` | Joueur ouvre coffre/cache caché | +15 |
| `BruitLéger` | Entendu | +3 |
| `BruitFort` | Entendu | +12 |
| `BruitTrèsFort` | Entendu | +25 |
| `PiègeDéclenché` | — | +valeur du piège |
| `AnimalAlerté` | — | +`AnimalData.ParanoiaBonusPerAlert` |

### 3.3 Feedback Joueur — Zéro UI

**Aucun affichage chiffré ou barre de la paranoia.** Le joueur lit l'état du proprio uniquement via :
- Animations corporelles (bras croisés, transpiration, regard nerveux, démarche rapide)
- Expressions faciales exagérées (cartoon)
- Sons distinctifs (grognements, onomatopées, soupirs)
- Actions passives de gêne (liste section 2.4.5 du GDD)

---

## 4. Tutoriel — Mission 1

### 4.1 Contraintes du cadre

- Toujours une **maison individuelle** avec extérieur
- Véhicule garé sur la **rue en face**
- Propriétaire : riche excentrique (archétype tutoriel = "Le Classique")
- Difficulté minimale : paranoia initiale basse, réactivité 3/10

### 4.2 Structure — Appels du Chef

Le tutoriel est délivré via des **appels téléphoniques du chef de l'agence** au joueur. Diegétique, comique, pas de 4ème mur. Le chef appelle automatiquement à chaque étape clé.

| # Appel | Déclencheur | Contenu |
|---------|-------------|---------|
| 1 | Spawn devant la maison | "Frappe à la porte et présente-toi. T'as ton badge ?" |
| 2 | Joueur entre dans la maison | "Il y a une commode à l'entrée. Sors ton téléphone, scanne l'objet." |
| 3 | Joueur a scanné un objet | "Maintenant prends-le et mets-le dans le coffre de ton véhicule." |
| 4 | Premier objet chargé | "Bien. Continue. Une fois le quota atteint, tu rentres." |
| 5 | Joueur s'approche d'un animal | "Oh, il a un [chien/chat]. Surtout ne l'énerve pas. Utilise les sardines." |
| 6 | Joueur déclenche un piège | "T'as marché dans quoi là ?! Regarde où tu mets les pieds." |
| 7 | Quota atteint | "C'est bon, charge et rentre. Laisse pas traîner." |
| 8 | Bonus — joueur casse quelque chose | "...t'as encore tout cassé ? Je te retire ça du salaire." |
| 9 | Bonus — joueur trouve le coffre caché | "Jackpot. Le proprio savait pas que je savais. Check." |

### 4.3 Mécaniques Couvertes par le Tutoriel

| Mécanique | Moment d'introduction |
|-----------|----------------------|
| Déplacement / postures | Libre dès le spawn |
| Frapper à la porte + sonnette | Appel #1 |
| Badge / présentation | Appel #1 |
| Scanner (téléphone UV) | Appel #2 |
| Porter un objet | Appel #3 |
| Coffre véhicule | Appel #3 |
| Quota + départ | Appel #4 |
| Animal | Appel #5 (si animal présent) |
| Piège | Appel #6 (si piège déclenché) |
| Cachette / coffre caché | Appel #9 (bonus, non obligatoire) |
| Consommable (sardines) | Appel #5 |

---

## 5. Scènes Virales

### 5.1 Philosophie

Ces scènes sont le **moteur de diffusion organique** du jeu (TikTok, YouTube, Twitch, Instagram). Elles sont :
- **Scriptées** mais déclenchées par des **conditions procédurales** (le joueur peut les manquer)
- **Indépendantes** les unes des autres (plusieurs peuvent se produire dans une même mission)
- **Filmables** : durée suffisante, personnages bien visibles, action lisible en quelques secondes

### 5.2 Catalogue — V1

| # | Nom | Condition de déclenchement | Description |
|---|-----|---------------------------|-------------|
| 1 | **Voisin Voleur** | Joueur dans maison + quota > 20% + coffre ouvert | Voisin spawn à côté du coffre. Joueur sort de la maison → voit le voisin → voisin fuit avec **1 objet**. Pendant la course poursuite, le proprio retourne au coffre et reprend **1 objet** (le plus précieux). |
| 2 | **Fan de Foot** | Joueur porte une TV | Voisin baraqué spawn à l'entrée, reprend la TV des mains du joueur, la repose sur le meuble et s'installe pour regarder le match. |
| 3 | **Livreur Suspect** | Aléatoire (prob. 30%), après 3min de mission | Livreur sonne à la porte avec un colis suspect (emballage discret, bruits étranges). Proprio très distrait pendant 60s. |
| 4 | **L'Avocat de Minuit** | Paranoia > seuil sociabilité | Avocat en costard impeccable, serviette en main, sonne à la porte. Peu importe l'heure. |
| 5 | **Le Perroquet Traître** | Joueur passe devant la cage à perroquet | Le perroquet répète soit des infos utiles (code du coffre, cachette de clé) soit des infos saboteurs ("LA POLICE ARRIVE", "IL EST LÀ !", répétition de l'appel du chef). Tirage aléatoire parmi les phrases enregistrées du proprio et du chef. |
| 6 | **L'Ami Inattendu** | Paranoia basse + aléatoire | Un ami du proprio arrive avec des bières, s'installe dans le salon, allume la TV. Occupe la pièce principale. |
| 7 | **Proprio Somnambule** | Heure mid-mission (si gameplay nocturne présent) | Proprio yeux fermés traverse la pièce où le joueur est caché. |
| 8 | **Peignoir & Claquettes** | Idle aléatoire extérieur | Proprio en peignoire + claquettes chaussettes fait une action absurde dans le jardin (arroser des plantes mortes, tondre à 14h en chantant, faire des pompes sur le gazon). |

### 5.3 Règles de Déclenchement

**Tirage à la génération de mission :**
- Au chargement de chaque mission, **1 à 4 scènes** sont tirées au sort parmi le catalogue
- Les scènes tirées sont "programmées" : elles **se produiront** dès que leurs conditions sont remplies
- Les scènes non tirées n'arrivent jamais dans ce run (rejouabilité : chaque mission est différente)
- **Jamais deux scènes au même seuil de paranoia** — le tirage écarte les conflits de déclenchement
- Le nombre tiré dépend de la taille de la maison / difficulté de la mission (petite maison = 1-2, grande = 2-4)

**Pendant la mission :**
- Chaque scène tirée attend que ses conditions soient réunies pour se déclencher
- Si le joueur part avant que les conditions soient atteintes, la scène ne se produit pas
- Les scènes sont **loggées dans le résumé de mission** ("Scènes manquées") pour donner envie de rejouer

### 5.4 Perroquet — Mécanique Détaillée

Le perroquet est un animal optionnel dans certaines maisons. Il a deux rôles :

**Info utile (tirage 50%) :**
- Code du coffre : `"Quatre-deux-sept-neuf !"`
- Cachette de clé : `"Sous le pot de fleurs !"`
- Emplacement d'un objet de valeur : `"Le vase est dans le bureau !"`

**Saboteur (tirage 50%) :**
- Répète l'appel du chef au moment le plus inopportun
- Crie des phrases entendues du proprio : `"LA POLICE ARRIVE RHAAA"`, `"QUI EST LÀ ?!"`
- Alerte auditive : le proprio entend le perroquet → `+8 Paranoia`

Le joueur peut **couvrir la cage** avec un tissu (consommable ou objet interactable) pour neutraliser le perroquet.

---

## 6. Points Ouverts (à trancher avant merge GDD)

| # | Question | Décision requise |
|---|----------|-----------------|
| A | Nom final du compteur Paranoia | À choisir lors d'une session dédiée |
| B | Nombre de scènes virales à implémenter pour l'EA | Priorisation V1 |
| C | Le proprio somnambule : seulement si mission nocturne, ou aussi de jour ? | Clarifier si gameplay nocturne prévu |
| D | Layout complet de la Mission 1 | Session dédiée (plan de maison, objets, cachettes) |
| E | Mécaniques non couvertes par le tutoriel appel #1–9 | À compléter |
