# DIRECTION — Slice Verticale

**Doc de direction actif. Prime sur `GDD_MASTER.md` en cas de conflit.**
Le GDD reste l'encyclopédie de référence (tout ce qui est *possible*). Ce doc dit *où on va maintenant*.

*Dernière mise à jour : 2026-06-07*

---

## 1. Le pari

Avant d'empiler du contenu, on prouve que le **cœur est fun** : une **slice verticale** jouable de bout en bout.

- **1 maison, 1 propriétaire excellent, 1 boucle complète et fun.**
- Objectif : une **démo clippable** sortie *vite*. L'EA 2027 est loin — la vague co-op-absurde (R.E.P.O./Lethal) peut refroidir. Tester le hook tôt.
- Tant que le proprio ne crée pas de tension/comédie émergente, le jeu n'est qu'un walking-sim où on ramasse des objets. **Le proprio vivant = la priorité absolue.**

---

## 2. Décisions de design prises (override le GDD)

### Identité unique — la légitimité sociale, pas une checklist
- ❌ Pas de "liste précise d'objets à saisir" (irréaliste + le layer `Interactable` gère déjà saisissable/décor).
- ✅ Le truc unique : **présenter badge/mandat pour désamorcer le proprio** au lieu de se cacher. Deux façons de jouer : **officiel vs furtif**. C'est le vrai "Papers Please" (le document comme interaction sociale). Déjà prévu GDD §2.7 (slot officiel) + état Confront §2.4.3, à activer.
- Le "mandat" = **montant de dette à atteindre** (= quota, existe) + fiction/UI, pas une checklist.

### Économie — radicalement simplifiée
- Garder **3 lignes : Gains / Dégâts / Amendes.**
- Couper ou cacher : taux de commission détaillés, 3 paliers de saisie excessive, flag suspension, bonus temps complexe. (Le GDD §3.4 reste comme référence "version riche" pour plus tard.)
- La sur-saisie rentre dans "Amendes".

### Sur-saisie — garder, mais généreuse
- Concept réaliste et thématique (over-saisir = abus). On le garde.
- ⚠️ **Punir le joueur qui ramasse du loot combat la dopamine du genre.** Donc : **bande de tolérance large + feedback ultra-clair** ("tu dépasses le mandat"). Sinon les joueurs détestent. → curseur exact à fixer (cf. §5).

### Comédie — systémique, pas scriptée
- Le GDD liste 8 scènes virales *scriptées* mais cite Goose Game ("comédie par la mécanique").
- ✅ L'or = **émergent** : le proprio refait ses lacets dans le couloir et bloque le passage parce que sa paranoïa a atteint un palier. Infini, clippable.
- Les **comportements de gêne passive (GDD §2.4.5)** sont exactement ça, déjà *designés* — ils font partie du chantier "proprio vivant".
- Scènes scriptées : 2-3 max pour la slice, le reste émergent.

### Lisibilité de la paranoïa
- **Jeu final : lecture par expression / animation / son du proprio.** Pas de jauge.
- **Pendant le dev : overlay DEBUG** (valeur 0-100 + palier + état IA + dernier bruit entendu), toggle, gated `#if UNITY_EDITOR` ou bool. Indispensable pour régler l'équilibre.
- **Icône paranoïa du HUD (GDD §4.2) : OFF en final.** On ne la recycle pas → vrai overlay debug séparé.

### Coop
- **Solo-first. Architecture coop-ready.** Ne pas construire le netcode avant qu'**une mission solo soit vraiment fun.**
- Plus gros risque (gouffre de temps) ET plus gros levier commercial (R.E.P.O. vendu sur la coop) → décision tranchée plus tard, sur preuve du fun solo.

### Ajouts à fort ROI, faibles coûts
- **Boucle courte ultra-juteuse** : restart rapide, feedback sonore cartoon (coffre qui claque, objet brisé). Fait les clips + la rétention.
- **Bonus "run d'artiste"** (0 dégât, 0 bruit, saisie exacte) mis en avant comme objectif noble opposé au chaos. Déjà dans le GDD, à surfacer.

---

## 3. Scope de la SLICE — IN / OUT

### IN (la slice)
- 1 maison hand-crafted
- 1 propriétaire, **1 archétype** (le code en a déjà 5 — on en règle UN à fond)
- IA proprio : boucle **Idle → Alerte → Cherche → Confronte** lisible
- Système de paranoïa actif + **overlay debug**
- Déplacement (postures, saut), ramasser / poser / jeter, coffre + chargement
- Quota = montant de dette + sur-saisie tolérante
- Bulletin de fin simplifié (Gains / Dégâts / Amendes)
- **Badge : présentation pour désamorcer** (officiel vs furtif)
- Bruit émis (déjà là) + au moins 1-2 comportements de gêne passive
- Boucle juteuse (juice audio/feedback)

### OUT / plus tard
- 27 personnalités (→ 3-5 après la slice)
- Customisation perso (→ post-launch)
- 8 véhicules (→ 3 pour l'EA)
- 11 animaux (→ chien + perroquet d'abord)
- 12 outils (→ 4-5 qui changent vraiment le jeu)
- Pièges, cachettes joueur, voisins/PNJ secondaires
- Scènes virales scriptées (au-delà de 2-3)
- SaveSystem complet, Shop, archives
- Coopératif (netcode)

---

## 4. Ordre de travail

1. **Overlay debug proprio** (paranoïa + état IA + dernier bruit) — petit, mais c'est les lunettes pour tout le reste.
2. **Auditer + activer `OwnerAI.cs` / `ParanoiaSystem.cs`** (pas encore lus en profondeur).
3. **Boucle d'états minimale** Idle → Alerte → Cherche → Confronte, lisible via l'overlay.
4. **Brancher anims / expressions** sur les états + 1-2 comportements de gêne passive. Couper l'overlay.
5. **Badge — présentation** pour désamorcer (officiel vs furtif).
6. **Éco simplifiée** (Gains / Dégâts / Amendes ; sur-saisie tolérante).
7. **Juice / polish** : audio cartoon, run d'artiste, restart rapide.

*Note : les 6 bugs B1-B7 (hors B4) sont déjà corrigés — voir le plan d'audit.*

---

## 5. Décisions encore ouvertes (à trancher)

- **Dureté de la sur-saisie** : marge de tolérance (% au-dessus du mandat) avant pénalité, et montant de la pénalité.
- **Coupes précises** : quels 3 véhicules / quels 2 animaux / quels 4-5 outils pour l'EA.
- **Profondeur coop EA** : solo seul à l'EA, ou coop dès l'EA si le solo est prouvé ?
- **Archétype proprio de la slice** : lequel des 5 existants on règle à fond en premier (suggestion : un "calme/lent" type Marcel, plus facile à équilibrer et pédagogique).

---

## Références
- `GDD_MASTER.md` — encyclopédie complète (version riche, pour plus tard)
- Plan d'audit code — `~/.claude/plans/tu-peux-me-faire-polished-flamingo.md` (bugs vérifiés B1-B7, divergences éco D1-D4, dérive nommage)
