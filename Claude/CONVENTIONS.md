# Conventions Globales du Projet Unity

## 📋 Status Codes

### GDD Team Status
- `IDEA_PROPOSED` : Nouvelle idée de feature proposée
- `SPEC_IN_PROGRESS` : Spécification en cours d'écriture
- `SPEC_READY` : Spécification complète, en attente de validation
- `INTEGRATION_CHECKING` : Vérification de cohérence avec le reste du jeu
- `INTEGRATION_OK` : Validation cohérence OK
- `INTEGRATION_BLOCKED` : Conflit détecté, nécessite ajustements
- `GDD_UPDATED` : GDD Master mis à jour
- `VALIDATION_PENDING` : En attente de validation finale
- `VALIDATION_FAILED` : Validation échouée, retour en spec
- `READY_FOR_CODE` : ✅ Feature complète, prête pour handoff au code
- `ARCHIVED` : Feature abandonnée ou reportée

### Code Team Status
- `WAITING_GDD` : En attente de spécification GDD
- `ARCHI_VALIDATION` : Validation architecture en cours
- `CODE_IN_PROGRESS` : Implémentation en cours
- `CODE_READY` : Code écrit, attend review
- `REVIEW_IN_PROGRESS` : Code review en cours
- `REVIEW_APPROVED` : Code validé
- `REVIEW_REJECTED` : Code refusé, corrections nécessaires
- `TESTING` : Tests en cours
- `DONE` : Feature complète et testée

### Global Status
- `BLOCKED` : Bloqué, attend action externe
- `ON_HOLD` : En pause temporaire
- `CANCELLED` : Annulé définitivement

## 🎯 Priorités

- `CRITICAL` : Bloquant, à traiter immédiatement
- `HIGH` : Important, à traiter rapidement
- `MEDIUM` : Normal, dans le planning
- `LOW` : Nice to have, quand possible

## 📝 Format de Communication Standard

Tous les messages dans les fichiers SYNC doivent suivre ce format :

```markdown
## [YYYY-MM-DD HH:MM] Agent Source → Agent Destination
**Task/Feature:** [Nom de la tâche ou feature]
**Status:** [STATUS_CODE]
**Priority:** [CRITICAL/HIGH/MEDIUM/LOW]
**Files:** 
- [chemin/vers/fichier1.md]
- [chemin/vers/fichier2.md]
**Action Required:** [Description claire de ce qui est attendu]
**Notes:** [Informations complémentaires optionnelles]
**Deadline:** [Date limite si applicable]
```

## 🗂️ Nommage des Fichiers

### Features
- Format : `feature_nom_en_snake_case.md`
- Exemples : `inventory_system.md`, `combat_mechanics.md`, `quest_system.md`

### Specs GDD
- Localisation : `/AGENTS/GDD_TEAM/FEATURE_SPECS/`
- Format : `feature_[nom].md`
- Exemple : `feature_inventory.md`

### Reports d'Intégration
- Localisation : `/AGENTS/GDD_TEAM/INTEGRATION_REPORTS/`
- Format : `integration_[nom].md`
- Exemple : `integration_inventory.md`

### Handoff Files (GDD → Code)
- Localisation : `/AGENTS/GDD_TEAM/HANDOFF/`
- Format : `[nom]_READY.md`
- Exemple : `inventory_READY.md`

## 📐 Conventions d'Écriture

### Documents GDD
- **Langue :** Français (sauf termes techniques Unity)
- **Ton :** Clair, précis, sans ambiguïté
- **Structure :** Toujours inclure : Description, Mécaniques, UI/UX, Références
- **Mesures :** Utiliser des valeurs concrètes (pas de "beaucoup", "peu")

### Exemples Bons/Mauvais

❌ **Mauvais :**
```
Le joueur a beaucoup de vie et peut se soigner régulièrement.
```

✅ **Bon :**
```
Le joueur dispose de 100 HP maximum. 
Régénération : +5 HP toutes les 3 secondes hors combat.
```

❌ **Mauvais :**
```
L'inventaire est assez grand pour stocker des objets.
```

✅ **Bon :**
```
Inventaire : 20 slots
Stack maximum : 99 unités par item stackable
Poids maximum : Aucune limitation
```

## 🔗 Références entre Documents

Utiliser des liens relatifs entre documents :

```markdown
Voir [Système de Combat](../FEATURE_SPECS/feature_combat.md) pour les dégâts.
Référence : [GDD Master - Section Économie](../GDD_MASTER.md#économie)
```

## ⚠️ Règles de Validation

### Avant READY_FOR_CODE
Une feature DOIT avoir :
- ✅ Spécification complète dans FEATURE_SPECS
- ✅ Rapport d'intégration validé
- ✅ Entrée dans GDD_MASTER.md
- ✅ Validation par GDD Validator
- ✅ Fichier HANDOFF créé

### Qualité des Specs
- Aucune ambiguïté
- Valeurs numériques précises
- Cas limites documentés
- Références visuelles si nécessaire

## 🔄 Workflow de Communication

```
1. Agent écrit dans son SYNC_[TEAM].md
2. Directeur lit et dispatche
3. Agent destinataire lit son SYNC
4. Agent traite et répond dans SYNC
5. Si inter-équipe → passer par GLOBAL_SYNC.md
```

## 📊 Templates de Documents

### Template Feature Spec
```markdown
# Feature: [Nom]

## Description
[Description courte et claire]

## Objectifs
- Objectif 1
- Objectif 2

## Mécaniques Détaillées
### Mécanique 1
[Description précise avec valeurs numériques]

## UI/UX
[Description de l'interface utilisateur]

## Intégration
[Comment cette feature s'intègre au jeu existant]

## Références
[Liens vers autres docs, images, exemples]
```

### Template Integration Report
```markdown
# Integration Report: [Feature]

## Feature Analysée
[Nom de la feature]

## Impact sur Systèmes Existants
### Système 1
- Impact : [Description]
- Modifications nécessaires : [Liste]

## Conflits Détectés
[Liste des conflits ou "Aucun conflit détecté"]

## Recommandations
[Suggestions d'ajustements]

## Conclusion
✅ INTEGRATION_OK / ⚠️ INTEGRATION_BLOCKED
```

## 🎮 Termes Unity Standardisés

- **GameObject** (pas "game object" ou "objet de jeu")
- **MonoBehaviour** (pas "Monobehaviour")
- **ScriptableObject** (pas "Scriptable Object")
- **Prefab** (pas "préfab")
- **Scene** (pas "scène" dans contexte technique)
- **Canvas** (pas "toile")
- **Transform** (pas "transformation")

## 📅 Fréquence de Synchronisation

- **SYNC files** : Lire au début de chaque tâche
- **PROJECT_STATUS** : Mettre à jour à chaque changement majeur
- **GDD_MASTER** : Mettre à jour à chaque feature validée
- **HANDOFF** : Créer immédiatement après validation GDD

---

**Version :** 1.0  
**Dernière mise à jour :** 2024-01-15  
**Maintenu par :** Orchestrateur