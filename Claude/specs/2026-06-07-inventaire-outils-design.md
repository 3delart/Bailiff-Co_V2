# Spec — Chaîne Inventaire / Outils

*Date : 2026-06-07 · Statut : design validé, prêt pour plan d'implémentation*
*Contexte : slice verticale, dev solo, garder simple. Override par `Claude/DIRECTION_SLICE.md`.*

---

## 1. Objectif

Rendre fonctionnelle la **chaîne économique d'équipement** :

```
Shop (acheter / upgrader, coûte de l'argent)
  → outils possédés (InventaireSystem)
    → Casier hub (équiper un loadout limité, drag-and-drop)
      → Mission : roue d'inventaire (outils/conso équipés)
        → outil en main + clic = effet
```

Aujourd'hui : `InventaireSystem` possède outils+niveaux et consommables ; `InventaireWheel`
affiche tout le possédé mais **les outils n'ont aucun effet** ; **Shop et Casier n'existent pas** ;
il n'y a **aucune notion de loadout équipé**.

## 2. Décisions prises (cadrage utilisateur)

- **Loadout limité = choix stratégique.** La roue a **3 slots outils + 3 slots conso**. On possède
  plus qu'on ne peut emporter.
- **Roue 8 directions** : haut = mains/objet porté · bas = badge+mandat (slot officiel fixe) ·
  gauche ×3 = outils équipés · droite ×3 = conso équipés · centre = deadzone (annuler).
- **Casier hub** = 3 colonnes drag-and-drop : gauche (outils possédés) / centre (la roue 3+3) /
  droite (conso possédés).
- **Outil en main** : sélectionner un outil dans la roue le met dans les mains (comme le badge).
  **Mutuellement exclusif** avec porter un objet de valeur.
- **Activation = clic gauche** (vise via crosshair, raycast caméra). **Tap** = instantané ;
  **maintenu** = jauge de canalisation. **E reste l'interaction monde** (portes/tiroirs/grab/PNJ).
- **Architecture** : un `PlayerToolUser` central qui dispatche par `EffectType` (switch). Pas de
  classe par effet (YAGNI).
- **Persistance** : en mémoire via le Player DontDestroyOnLoad. Pas de save disque pour la slice.

## 3. Périmètre

**IN :** loadout équipé (data + API), remap roue 3+3, `PlayerToolUser` + effets des 3 outils
indépendants du proprio (pied-de-biche, crochetage, scanner), `ShopPanel`, `CasierUI`.

**OUT (plus tard) :** outils dépendants du proprio (badge/menottes/spray), marteau de démolition
(même verbe clic+canalisation, à ajouter ensuite), save disque, mini-jeu de serrure, coop.

---

## 4. Composants

### 4.1 `InventaireSystem` (étendu)
Conserve l'existant (`_outils : Dict<OutilData,int>` niveau, `_consommables : Dict<string,int>`).

**Modèle de quantités :**
- **Outils** : 1 seul exemplaire par type (permanent, upgradable — pas de stack). État = niveau (0-2).
- **Consommables** : stock possédé (peut être élevé, acheté au shop) + **limite d'emport par mission
  propre à chaque conso** (`MaxCarryPerMission`, voir 4.1bis). Au casier on choisit le type ET la
  quantité emportée (≤ `MaxCarryPerMission` et ≤ stock possédé). L'usage en mission décrémente le stock.

**Ajouts :**
- `_outilsEquipes : List<OutilData>` (max 3 types)
- `_consosEquipes : List<(string type, int quantite)>` (max 3 types ; `quantite` ≤ MaxCarryPerMission)
- API : `Equiper(OutilData)`, `Deséquiper(OutilData)`, `EquiperConso(string, int quantite)`,
  `DeséquiperConso(string)`, avec gardes : capacité (≤ 3 types), possession, et `quantite ≤ MaxCarryPerMission`.
- Exposition lecture seule : `OutilsEquipes`, `ConsosEquipes`.
- Badge+mandat : **hors** des 3 slots outils — slot officiel fixe (toujours dans la roue, non retirable).

### 4.1bis `OutilData` (petit ajout)
- Nouveau champ conso-only : `int MaxCarryPerMission` (nombre max emporté par mission pour ce
  consommable). 0 = pas de limite (à éviter pour le design stratégique).
- `ShopStackSize` n'est **plus utilisé** : au shop, les conso s'achètent **à l'unité** avec un
  sélecteur de quantité **1 à 10** par achat (prix = prix unitaire × quantité).

Persistance assurée par le composant vivant sur le Player persistant (DontDestroyOnLoad). Pas de
sérialisation disque dans ce spec.

### 4.2 `InventaireWheel` (remap)
Passe de [centre=mains, 4 outils, 4 conso] à :
- Haut = mains / objet porté
- Bas = badge+mandat (slot officiel fixe)
- Gauche ×3 = `OutilsEquipes`
- Droite ×3 = `ConsosEquipes`
- Centre = deadzone (annuler / aucune sélection)

`RafraichirSlots` lit **l'équipé** (pas tout le possédé). Sélection (au relâchement de Tab) :
- slot mains → range l'outil actif (PlayerToolUser.RangerOutil)
- slot outil → `PlayerToolUser.PrendreOutil(outil, niveau)`
- slot conso → `UtiliserConsommable` (existant)
- slot badge/mandat → met les documents en main (présentation — logique d'effet hors scope, juste l'état "en main")

### 4.3 `PlayerToolUser` (nouveau, sur le Player)
État : `_outilActif : OutilData` + `_niveauActif : int` (ou null = mains nues/objet).

**Exclusion mutuelle avec `PlayerCarry`** :
- `PrendreOutil` est **refusé** si un objet de valeur est porté (feedback, no-op).
- `PlayerCarry.Saisir` appelle `PlayerToolUser.RangerOutil()` avant de prendre un objet.

**Activation (clic gauche)** — uniquement si un outil est en main :
- Raycast caméra (portée depuis config). Dispatch par `_outilActif.EffectType` :
  - `ForceDoor` (pied-de-biche) : cible `OpenableInteractable` à l'état `Locked` → `ForceOpen()`
    (bruit Très Fort, déjà implémenté). **Tap.**
  - `Lockpick` (crochetage) : cible `OpenableInteractable` `Locked` → **maintenir** le clic →
    progression de canalisation (durée = `ToolLevel.EffectDuration` du niveau courant ; niveau
    supérieur = plus rapide) → à 100 % : `Unlock()` silencieux. Relâcher / perdre la cible avant la
    fin = annule et reset la jauge. Émet `OnToolChannelProgress`.
  - `ScanObject` (scanner/téléphone) : cible `ValueObject` → `Scan()` (révèle nom+valeur dans le
    label). **Tap.**
- Le `switch` ignore proprement les `EffectType` hors scope (badge, etc.).

**Event nouveau :** `OnToolChannelProgress { float Progress01; bool Active; }` (GameEvents.cs) pour
afficher une jauge de canalisation (HUD).

> Remplace le hack de possession : retirer `AttemptForce`/`PossedePiedDeBiche` de
> `OpenableInteractable.Interact` (l'état `Locked` n'est plus forcé via E, mais via outil-en-main+clic).
> `Interact` sur une porte `Locked` ne fait alors rien (ou un petit retour "verrouillé" — cf. UX backlog).

### 4.4 `CasierUI` (nouveau, UI_Persistent, panel Blocking, contexte Hub)
- **3 colonnes** : **gauche** = outils possédés (non équipés), **centre** = la **roue radiale**
  (rendu identique à l'affichage Tab en jeu), **droite** = conso possédés.
- **Roue radiale au centre** (8 directions) : haut = Mains (fixe) · bas = Badge+Mandat (slot officiel
  fixe) · 3 slots gauche = outils · 3 slots droite = conso · centre = hub déco.
- **Drag-and-drop** : glisser un chip (colonne) vers un slot de la roue = `Equiper` ; glisser hors /
  bouton ✕ = `Deséquiper`. Déplacer un item d'un slot à l'autre = supporté. Respecte la capacité
  (3 types outils + 3 types conso) et le slot officiel fixe (non éditable).
- **Niveaux affichés** : pips ●●○ sur les chips outils ET dans les slots outils de la roue.
- **Conso** : à l'équipement, **stepper qté/max** directement dans le slot (qté ≤ `MaxCarryPerMission`
  et ≤ stock possédé). Ex : possède 10 fumigènes, max 3/mission → 1-3. Le reste reste au stock.
- Ouvert par `HubPNJ` type `Inventaire` (remplacer le stub `:96`).
- Hérite de `UIPanel` (Ouvrir/Fermer, contexte Hub).

### 4.5 `ShopPanel` (nouveau, UI_Persistent, panel Blocking, contexte Hub)
- **2 onglets : Outils / Consommables.**
- **Onglet Outils — layout maître/détail** : liste à gauche, **fiche détaillée à droite** (icône,
  description, **table des niveaux** Niv.1/2/3 avec effet + coût, niveau courant surligné, prochain
  mis en avant). Catalogue assigné en **Inspector** (`OutilData[]`) pour la slice ; catalogue SO plus tard.
  - États par outil : **Verrouillé** si `UnlocksAfterMission > GameManager.DerniereMissionCompletee`
    (grisé, 🔒 Mx) · **Achetable** (non possédé, débloqué) → **Acheter** (`PurchasePrice`) ·
    **Possédé** → **Upgrader** au coût `Levels[niveauActuel+1].UpgradeCost` (masqué si niveau max).
  - Pips ●●○ = niveau courant sur la liste.
- **Onglet Consommables — grille de cartes** : par carte, prix **à l'unité**, **stepper 1-10** +
  total live, bouton **Acheter ×N**, possédés affichés, `max X/mission` indiqué. Gating
  `UnlocksAfterMission` aussi (carte grisée 🔒 Mx).
- Bouton désactivé "Fonds insuffisants" si `!PeutPayer`.
- Toute transaction : `PeutPayer` → `Debiter` → `AjouterOutil` / `UpgraderOutil` /
  `AjouterConsommable`. Met à jour l'affichage argent.
- Ouvert par `HubPNJ` type `Boutique` (`OuvrirPanelShop` existe déjà → il faut juste le panel).

### 4.6 Persistance (slice)
`InventaireSystem` vit sur le Player persistant (DontDestroyOnLoad) → outils possédés, niveaux,
consommables et loadout équipé survivent hub↔mission. **Pas de save disque** ici (reset au
redémarrage du jeu = acceptable pour la slice ; SaveSystem = plus tard).

---

## 5. Flux de données

1. **Hub / Shop** : argent (`GameManager.Argent`) → achat/upgrade → `InventaireSystem._outils`.
2. **Hub / Casier** : `_outils`/`_consommables` possédés → drag → `_outilsEquipes`/`_consosEquipes`.
3. **Mission / Roue** : lit `_outilsEquipes`/`_consosEquipes` (via `OnJoueurSpawne`→`SetRefs`, fix B3).
4. **Mission / Usage** : sélection roue → `PlayerToolUser` (outil en main) → clic → effet sur la
   cible visée → events (`OnToolChannelProgress`, bruit, `Scan`, `ForceOpen/Unlock`).

## 6. Cas limites / erreurs
- Équiper au-delà de 3 types → refusé (feedback UI). Équiper un non-possédé → refusé.
- Équiper une quantité de conso > `MaxCarryPerMission` ou > stock possédé → clampé / refusé.
- Outils : pas de quantité — 1 seul exemplaire par type (on upgrade, on ne stacke pas).
- Prendre un outil en portant un objet → refusé (no-op). Saisir un objet avec outil en main →
  range l'outil automatiquement.
- Clic sans cible valide pour l'effet → no-op (pas de crash, pas de bruit).
- Canalisation interrompue (relâcher, bouger trop ?, perdre la cible) → annule proprement, reset jauge.
- Upgrade au niveau max → bouton masqué. Achat sans fonds → bloqué + message.
- Consommable épuisé en mission → slot affiche 0/—, usage no-op.

## 7. Tests (manuels Unity — pas de tests auto)
- Shop : acheter un outil (argent débité, apparaît au casier) ; upgrader (coût débité, niveau monte) ;
  outil verrouillé grisé tant que mission requise non faite.
- Casier : glisser 3 outils + 3 conso ; le 4e refusé ; retirer libère un slot ; badge/mandat non éditable.
- Roue en mission : n'affiche que l'équipé ; haut=mains, bas=docs, gauche=outils, droite=conso.
- Outils : pied-de-biche en main + clic sur porte verrouillée → s'ouvre + bruit ; crochetage =
  maintenir → jauge → ouverture silencieuse ; scanner = clic sur objet → nom+valeur révélés.
- Exclusion : prendre un outil en portant un objet impossible ; saisir un objet range l'outil.
- Persistance : acheter/équiper, lancer mission, revenir au hub → tout conservé.

## 8. Phasage d'implémentation (le plan détaillera)
1. **Loadout data** : `InventaireSystem` (equip API 3+3) + **remap roue** 8 directions.
2. **`PlayerToolUser`** + 3 effets (clic/canalisation) + event jauge + retrait du hack `PossedePiedDeBiche`.
3. **`ShopPanel`** (acheter/upgrader).
4. **`CasierUI`** (drag-and-drop).

## 9. Hors scope (rappel)
Badge/menottes/spray (dépendants du proprio), marteau démolition, save disque, mini-jeu serrure,
coop, jauge HUD finale stylée (un placeholder suffit pour la slice).
