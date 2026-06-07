# Résumé Complet — Session Avant Damage System

## Phase 1 : Exploration & Planning

**Question initiale:** "Quel est la suite pour le projet? Sur quoi on avance?"

**Réponse:** Consulté GDD + backlog. Priorité Phase 1 = SaveSystem, Badge, Scan, Voisins IA, Cachettes, Police. Mais SaveSystem peut attendre (1 seule mission). Mieux: créer une **Mission_01 testable complète** avec vrais objets de valeur et bon quota.

---

## Phase 2 : Assets Existants & État du Projet

### Exploration

Trouvé:
- **Hub.unity** — existe mais vide (seul Fbxagence.fbx brut, aucun dressing)
- **Mission_01.unity** — N'EXISTE PAS
- **Assets test** — Commode, TV, Vase, Voiture, 2 remorques, PrefabMaisonTest (tous de test)
- **Aucune scene campagne** — seulement Mission_Test (dev) et Mission_Libre (proc)

### Reconnaissance

- ObjetData.cs — structure OK (Value, Weight, IsBreakable, DamageImpactThreshold, etc.)
- PlayerConfigData.cs — **joueur 1.8m height, bonne échelle**
- ValueObject.cs V5 — système dégâts cumulatifs EXISTE DÉJÀ (0-100%, step table par velocity)
- MissionData, HabitationData, OwnerData — structures existent, aucune donnée campaign remplies

---

## Phase 3 : Résolution Échelle

**Problème initial:** "Tout les assets sont des test. Il faut respecter une bonne échelle. Comment on se prépare?"

**Solution établie:**
```
1 Unity unit = 1 mètre (confirmé par PlayerConfigData.HeightNormal = 1.8)
```

**Workflow Blender → Unity:**
1. Blender Scene Properties → Metric, Unit Scale 1.0, Length = Meters
2. FBX Export: Apply Unit ON, Apply Scalings: FBX All, Forward: -Z, Up: Y
3. Unity Import: Scale Factor = 1

**Dimensions de référence:**
- Joueur: 1.8m debout, 1.2m accroupi, 0.15m allongé
- Porte: 2.1m H × 0.9m W
- Plafond RdC: 2.6m, étage: 2.4m
- Marche escalier: 0.18m × 0.28m
- Table: 0.75m, plan cuisine: 0.9m
- Lit double: 0.5m × 1.6m × 2.0m
- Armoire: 2.0m × 1.0m × 0.6m
- TV écran plat: ~50 pouces
- Voiture berline: 4.5m × 1.8m × 1.5m

**Fix technique:**
- `MaxStepHeight = 0.7f` → `0.3f` (était trop haut = table)

---

## Phase 4 : Création Données Mission_01 A REFAIRE

### 15 ObjetData ScriptableObjects

**Objets créés avec prix:**


| Objet | Valeur | Poids | Breakable | Notes |
|-------|--------|-------|-----------|-------|


**Total pool: xxx €**
**Quota Mission_01: xxx€** (joueur grab ~43% objets par valeur)

---

## Phase 5 : Données Campaign

### OwnerData — Marcel Dupont

- Age: 67 ans
- Profession: Retraité
- Archetype: Rêveur (réactivité 2, méthode 2, sociabilité 2)
- Hobbies: philatélie, vie tranquille
- Clue: "Mes souvenirs précieux dans la vitrine du salon"
- Security: 1/5 (faible, tutoriel)

### HabitationData — Maison Marcel

- Type: Maison de ville
- Surface: 120m²
- Étage: RdC + 1er étage
- Accès: Porte + Jardin avant
- Complexité: 8/10 (maison de ville petit)

### MissionData_01 — La Saisie de Marcel

- MissionName: "La Saisie de Marcel"
- MissionNumber: 1
- SceneName: "Mission_01"
- Briefing: "Simple saisie chez retraité tranquille"
- Objectif: "Saisir 2 000€ minimum, objets salon + chambres"
- KnownThreats: ["Propriétaire Calme"]
- MinimumQuotaValue: 2000
- BonusTimeThreshold: 900s (15 min pour 3 stars)
- Commission: 40% si quota atteint, 10% sinon
- Paranoia thresholds: 60 (2★), 30 (3★)

---

## Phase 6 : Architecture Scene Mission_01

### Layout Maison Marcel

**Extérieur:**
- Façade maison de ville (petit modèle)
- Jardin avant (petit, haie, portail)
- Rue + trottoir (voiture garée devant)

**RdC:**
- Couloir entrée (Commode là — tutoriel scan)
- Salon (principale zone valeur: TV, vase, horloge, etc.)
- Cuisine (vin, café)
- Bureau/biblio (statue, lampe, livre, disque, appareil photo)
- WC/SdB (petit)

**Étage:**
- Palier
- Chambre principale (bijoux, montre, parfois cachés)
- Chambre 2 / Bureau Marcel (plus de valeur)
- SdB (optionnel)

### Meubles Essentiels

**Priorité Bloc 1** (purement testable):
- Commode (entrée) ✓ existe
- Canapé salon ❌
- Table basse ❌
- Lit double ❌
- Armoire chambre ❌

Plus: 5 objets valeur minimum (TV, Vase + 3 nouveaux) pour tester quota.

---

## Phase 7 : Système de Dégâts Découvert

### État Antérieur

`ValueObject.cs V5` — dégâts EXISTENT DÉJÀ:
- `_damagePercentage` (0-100%)
- `IsBroken` (true si 100%)
- `ActualValue` (linéaire: value × (100-damage)/100)
- Step table par velocity:
  - < 4 m/s → +5%
  - < 6 m/s → +15%
  - < 9 m/s → +30%
  - < 13 m/s → +50%
  - ≥ 13 m/s → +75%
- Events: `OnObjectDamaged`, `OnNoiseEmitted`
- Drop protection coroutine (0.5s après pose douce)

**MANQUAIT:**
- Zéro visuel (pas de shatter, pas de texture craquelée)
- Zéro topple (objets sur étagère ne peuvent pas renverser)
- Zéro aléatoire (100% déterministe)
- Dégâts identiques tous objets (fragile vase = TV robuste)

---

## Phase 8 : Design Complet ObjectValue Damage System

### Problèmes identifiés

1. **Vase fragile tombe 1.2m** → +15% damage seulement → faut 7 chutes pour casser
2. **Aucun feedback visuel** sur `IsBroken`
3. **Pas de topple** — objets étagère ne réagissent pas à joueur
4. **Zéro comédie** — casse 100% prévisible

### Solution : BreakProfile

Ajout 8 champs `ObjetData`:
- `BreakType` (enum: Shatters / Deforms / Scratches)
- `DamageMultiplier` (0.05–15×) — fragile 5-10, normal 1, solide 0.1-0.2
- `DurabilityVariance` (0–1) — aléatoire ±% sur dégâts
- `CanTopple` (bool) — renversable par collision joueur
- `ToppleVelocityThreshold` (float) — vitesse min pour topple
- `BrokenVariant` (GameObject) — prefab fragments si Shatters
- `DamagedMaterial` (Material) — swap si Deforms
- `DamagedBlendShapeWeight` (float 0-100) — déformation shape key

### 5 Profils Test

**Vase grand format (sol):**
- Shatters, Multiplier=3, Variance=0.3 → 1 bouscule = 15% × 3 = 45%, 2e bouscule = casse
- Peut renverser → topple → tombe → casse sur sol

**Petit vase étagère:**
- Shatters, Multiplier=8, Variance=0.6 → tombe 1.2m = +120% = casse garanti
- Table basse tombe = 50/50 chance casse (comédie!)

**TV:**
- Deforms, Multiplier=0.5, Variance=0.1 → joueur porte, cogne mur = -7-15% dégât
- Visuel: écran fissuré texture swap + shape key pli léger
- Pickupable même à 100% (juste boîtier cassé)

**Statue en métal:**
- Scratches, Multiplier=0.05, Variance=0 → incassable



### Implémentation Code

**Modifications C#:**
- `ObjetData.cs` — +8 champs Break Profile
- `Enums.cs` — `BreakType`, `SoundMaterial`
- `ValueObject.cs` — CalculateDamageFromImpact (multiplier+variance), OnBreak(), OnDamageVisualUpdate(), topple check
- `PlayerCarry.cs` — CheckCarryWallCollision() (SphereCast wall check pendant carry)

**Pipeline Blender:**
- Shatters: model intact + Cell Fracture (~10-15 fragments)
- Deforms: shape key "Damaged" + 2 textures (pristine + cracked)

**Assets Unity:**
- Matériaux Damaged (8 variants)
- Prefabs Shattered (4 variants avec rigidbodies)

---

## Résumé Chiffres

| Catégorie | Nombre | Status |
|-----------|--------|--------|
| ObjetData créés | 13 | ✅ Code |
| OwnerData créés | 1 (Marcel) | ✅ Code |
| HabitationData créés | 1 (Maison Marcel) | ✅ Code |
| MissionData créés | 1 (Mission_01) | ✅ Code (skeleton) |
| Champs ObjetData ajoutés | 8 | ✅ Code |
| Enums ajoutés | 2 | ✅ Code |
| Méthodes ValueObject ajoutées | 3 | ✅ Code |
| Blender assets à créer | 12+ | ❌ À faire |
| Unity prefabs à créer | 4 | ❌ À faire |
| Matériaux à créer | 8 | ❌ À faire |
| Textures à créer | 8+ | ❌ À faire |
| Remplir ObjetData break profile | 13 | ❌ À faire |
| Scène Mission_01 à créer | 1 | ❌ À faire |

---

## Prochaines Étapes

### **User (Blender/Editor):**
1. Créer 15 modèles Blender (fragile + déformable)
2. Cell Fracture sur fragiles, shape keys sur déformables
3. Importer FBX, créer prefabs Shattered avec rigidbodies
4. Créer matériaux Damaged Unity
5. Remplir 13 ObjetData avec BreakProfile
6. Assigner BrokenVariant, DamagedMaterial, etc.
7. Créer/configurer Mission_01.unity basique
8. Tester: vase topple→shatter, TV deform, etc.

### **Code (déjà fait):**
- BreakType enum ✅
- DamageMultiplier + variance dans CalculateDamageFromImpact ✅
- OnBreak() (instantiate fragments) ✅
- OnDamageVisualUpdate() (material swap + shape key) ✅
- Topple logic dans OnCollisionEnter ✅
- CarryWallCheck dans PlayerCarry ✅

