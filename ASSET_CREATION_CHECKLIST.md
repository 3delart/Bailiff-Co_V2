# Asset Creation Checklist — ObjectValue Damage System

**Lire d'abord:** `RECAP_SESSION.md` pour le contexte complet.

---

## Blender — Créer Modèles (12 assets)

### Fragile Objects (Shatters) — Cell Fracture Pipeline

**Pour chaque modèle:**
1. Modéliser objet intact 
2. Dupliquer → Cell Fracture modifier (10-15 fragments)
3. Positionner fragments comme explosion (dispersés 5-10cm)
4. Export FBX: `ObjectName_Intact.fbx` + `ObjectName_Shattered.fbx`

#### 1. VaseSol (1.2m haut, 25cm base large)
- Céramique blanche/beige, glaçure mate
- Cell Fracture: ~12 fragments épais
- Files: `VaseSol_Intact.fbx` + `VaseSol_Shattered.fbx`

#### 2. VasePetit (30cm haut, 15cm large)
- Céramique glaçurée brillante
- Cell Fracture: ~8 fragments
- Files: `VasePetit_Intact.fbx` + `VasePetit_Shattered.fbx`

#### 3. Assiette (25cm diam)
- Céramique blanche, rebord surélevé
- Cell Fracture: ~6 fragments (éclats épais, réalistes)
- Files: `Assiette_Intact.fbx` + `Assiette_Shattered.fbx`

#### 4. CoffretVin (coffret bois + 4 bouteilles)
- Bois noyer, intérieur velours rouge
- Bouteilles Bordeaux vertes (4 pièces)
- Cell Fracture: bouteilles cassées (~4 pièces verre) + boîte bois fissurée (~2-3 pièces bois)
- Files: `CoffretVin_Intact.fbx` + `CoffretVin_Shattered.fbx`

---

### Deformable Objects (Deforms) — Shape Key + Textures

**Pour chaque modèle:**
1. Modéliser objet intact
2. Dupliquer mesh → Shape Key "Damaged" (dent/pli léger, 5-20mm)
3. Créer 2 textures: pristine + cracked/dented
4. Export FBX avec shape key

#### 5. TV (écran plat 50 pouces)
- 55cm × 35cm × 5cm (approx)
- Écran noir brillant, cadre gris plastique
- Shape Key "Damaged": base légèrement aplatie (5mm dent) + coins pliés (2mm)
- Textures: 
  - `TV_Screen_Pristine.png` (écran lisse)
  - `TV_Screen_Cracked.png` (fissures radiantes)
- File: `TV.fbx` (avec shape key)

#### 6. Horloge murale (30cm diam)
- Cadre bois, verre transparent, aiguilles
- Shape Key "Damaged": craquelures verre + aiguilles tordues (1mm)
- Textures:
  - `Horloge_Glass_Pristine.png`
  - `Horloge_Glass_Cracked.png` (fissures radiantes)
- File: `Horloge.fbx`

#### 7. Lampe (50cm haut)
- Base lourde (métal/céramique 12cm), tige courbée (laiton), abat-jour tissu
- Shape Key "Damaged": dent base (3mm) + léger fléchissement tige + pli abat-jour
- Textures (abat-jour):
  - `Lampe_Abat_jour.png`
  - `Lampe_Abat_jour_Dechire.png` (déchirure légère)
- File: `Lampe.fbx`

#### 8. Console Rétro (Sega) (25cm × 15cm × 10cm)
- Plastique noir/gris, boutons colorés
- Shape Key "Damaged": légère déformation boîtier
- Textures:
  - `Console_Pristine.png`
  - `Console_Rayee.png` (rayures/griffes plastique)
- File: `Console.fbx`

#### 9. Machine à Café (Delonghi) (30cm haut)
- Plastique noir, écran LCD, buse vapeur
- Shape Key "Damaged": dent légère + pli écran (2mm)
- Textures (écran):
  - `Cafe_Ecran_Pristine.png`
  - `Cafe_Ecran_Craque.png` (fissures LCD)
- File: `MachineACafe.fbx`

#### 10. Tableau Huile (80cm × 60cm, cadre)
- Toile peinte, cadre doré, verre/vernis
- Shape Key "Damaged": léger ondulation toile + fissure vernis (1mm)
- Textures (surface):
  - `Tableau_Pristine.png`
  - `Tableau_Dechire.png` (fissures vernis)
- File: `Tableau.fbx`

#### 11. Appareil Photo (Canon AE-1) (15cm × 10cm × 8cm)
- Corps métal noir, verre optique avant
- Shape Key "Damaged": optique légèrement enfoncée, corps plié (2mm)
- Textures (optique):
  - `AppareilPhoto_Pristine.png`
  - `AppareilPhoto_VerreCraque.png` (cracks optiques)
- File: `AppareilPhoto.fbx`

#### 12. Montre (5cm diam, cadran)
- Boîtier métal argenté, verre minéral, bracelet cuir
- Shape Key "Damaged": verre craquelé, boîtier plié (1mm)
- Textures (cadran):
  - `Montre_Pristine.png`
  - `Montre_VerreCraque.png`
- File: `Montre.fbx`

---

## Unity Editor — Importer & Configurer

### Step 1 — Importer tous FBX

1. Créer dossier `Assets/Prefabs/Objects/`
2. Pour chaque objet:
   - [ ] Créer sous-dossier `Assets/Prefabs/Objects/VaseSol/`
   - [ ] Importer `VaseSol_Intact.fbx` → drag dans scene
   - [ ] Importer `VaseSol_Shattered.fbx` → separate folder
   - Idem pour TV, Horloge, Lampe, etc. (12 modèles)

### Step 2 — Créer Matériaux Damaged

Dossier: `Assets/Materials/ValueObjectDamaged/`

Créer 8 matériaux (Standard shader):
- [ ] `TV_ScreenCracked` — texture: `TV_Screen_Cracked.png`, Metallic: 0, Roughness: 1
- [ ] `Horloge_VerreCraque` — idem
- [ ] `Lampe_AbatJourDechire` — idem
- [ ] `Console_Rayee` — plastique rayé
- [ ] `Cafe_EcranCraque` — écran LCD fissuré
- [ ] `Tableau_Dechire` — toile craquelée
- [ ] `AppareilPhoto_VerreCraque` — optique craquelée
- [ ] `Montre_VerreCraque` — verre craquelé

### Step 3 — Créer Prefabs Shattered (4 fragiles)

Pour chaque fragile (VaseSol, VasePetit, Assiette, CoffretVin):

**VaseSol_Shattered prefab:**
1. Importer `VaseSol_Shattered.fbx` → scene
2. Pour chaque fragment enfant (x12):
   - Ajouter **Rigidbody**
     - Mass: auto
     - Drag: 0.1, Angular Drag: 0.1
     - Use Gravity: ✓
     - Is Kinematic: ✗
     - Collision Detection: Continuous
   - Ajouter **Mesh Collider** (convex)
   - Physics Material: friction 0.4, bounce 0.2
3. Sauvegarder comme prefab `Assets/Prefabs/Objects/Vase/VaseSol_Shattered.prefab`

Répéter pour VasePetit_Shattered, Assiette_Shattered, CoffretVin_Shattered.

### Step 4 — Remplir ObjetData BreakProfile (13 objets)

Ouvrir chaque asset et remplir:

#### **TV (400€, TVData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `0.5`
- DurabilityVariance: `0.1`
- DamageImpactThreshold: `4`
- CanTopple: ✗
- ToppleVelocityThreshold: `—`
- DamagedMaterial: `TV_ScreenCracked`
- DamagedBlendShapeWeight: `0` (pas de shape key, juste matériau)
- SoundMaterial: `Electronics`
- BreakSound: `—` (audio phase)

#### **Vase (300€, VaseData.asset)**
- BreakType: `Shatters`
- DamageMultiplier: `3` (grand vase sol)
- DurabilityVariance: `0.3`
- DamageImpactThreshold: `2.5`
- CanTopple: ✓
- ToppleVelocityThreshold: `1.5`
- BrokenVariant: `VaseSol_Shattered prefab`
- SoundMaterial: `Ceramic`

#### **Tableau (650€, TableauData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `1.5`
- DurabilityVariance: `0.2`
- CanTopple: ✓
- DamagedMaterial: `Tableau_Dechire`
- DamagedBlendShapeWeight: `0`
- SoundMaterial: `Wood` (cadre)

#### **Montre (220€, MontreData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `2`
- DurabilityVariance: `0.1`
- CanTopple: ✓
- DamagedMaterial: `Montre_VerreCraque`
- DamagedBlendShapeWeight: `0`
- SoundMaterial: `Glass`

#### **Statue (380€, StatueData.asset)**
- BreakType: `Scratches`
- DamageMultiplier: `0.2` (bronze solide)
- DurabilityVariance: `0`
- CanTopple: ✓
- SoundMaterial: `Metal`

#### **Horloge (250€, HorlogeData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `1.5`
- DurabilityVariance: `0.2`
- CanTopple: ✓
- DamagedMaterial: `Horloge_VerreCraque`
- DamagedBlendShapeWeight: `20` (aiguilles tordues)
- SoundMaterial: `Glass`

#### **Lampe (280€, LampeData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `1.2`
- DurabilityVariance: `0.15`
- CanTopple: ✓
- DamagedMaterial: `Lampe_AbatJourDechire`
- DamagedBlendShapeWeight: `15` (pli abat-jour)
- SoundMaterial: `Glass`

#### **Livre (160€, LivreData.asset)**
- BreakType: `Scratches`
- DamageMultiplier: `0.1`
- DurabilityVariance: `0`
- CanTopple: ✗
- SoundMaterial: `Paper`

#### **Disque (120€, DisqueData.asset)**
- BreakType: `Scratches`
- DamageMultiplier: `0.05`
- DurabilityVariance: `0`
- CanTopple: ✗
- SoundMaterial: `Plastic`

#### **Console (240€, ConsoleData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `0.8`
- DurabilityVariance: `0.1`
- CanTopple: ✓
- DamagedMaterial: `Console_Rayee`
- DamagedBlendShapeWeight: `0`
- SoundMaterial: `Plastic`

#### **AppareilPhoto (190€, AppareilPhotoData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `1.8`
- DurabilityVariance: `0.15`
- CanTopple: ✓
- DamagedMaterial: `AppareilPhoto_VerreCraque`
- DamagedBlendShapeWeight: `0`
- SoundMaterial: `Glass`

#### **Collier (420€, CollierData.asset)**
- BreakType: `Scratches`
- DamageMultiplier: `0.1`
- DurabilityVariance: `0.05`
- CanTopple: ✗
- SoundMaterial: `Metal`

#### **Tapis (480€, TapisData.asset)**
- BreakType: `Scratches` (tissu)
- DamageMultiplier: `0.05`
- DurabilityVariance: `0`
- CanTopple: ✓ (peut s'enrouler)
- SoundMaterial: `Plastic` (bruit tissu)

#### **Vin (130€, VinData.asset)**
- BreakType: `Shatters`
- DamageMultiplier: `4`
- DurabilityVariance: `0.4`
- CanTopple: ✓
- BrokenVariant: `CoffretVin_Shattered prefab`
- SoundMaterial: `Glass`

#### **Café (200€, CafeData.asset)**
- BreakType: `Deforms`
- DamageMultiplier: `0.7`
- DurabilityVariance: `0.1`
- CanTopple: ✓
- DamagedMaterial: `Cafe_EcranCraque`
- DamagedBlendShapeWeight: `10` (pli légérecran)
- SoundMaterial: `Plastic`

### Step 5 — Tester Scenarios

Scene: `Mission_Test.unity`

- [ ] **Vase fragile sur étagère**
  - Joueur passe près → topple → tombe → shatter ✓
  - Fragments volent 8 secondes puis disparaissent ✓
  - Valeur: 0€ après casse ✓

- [ ] **TV porté contre mur**
  - Joueur cogne mur lentement (~3 m/s) → aucun dégât (sous seuil 4)
  - Joueur cogne mur rapide (~6 m/s) → +7.5% dégât, écran fissure légèrement ✓
  - Joueur cogne mur fort (~10 m/s) → +15% dégât, écran très fissuré ✓
  - Matériau swap à 50% dégâts ✓

- [ ] **Fourchette**
  - Quasi indestructible visuellement ✓
  - Valeur quasi-zéro ✓

- [ ] **Variance aléatoire**
  - Tester petit vase sur table basse plusieurs fois
  - Résultats varient (50% casse, 50% survive) ✓

### Step 6 — Configurer MissionData_01

`MissionData_01.asset`:
- [ ] Owner: `MarcelData.asset`
- [ ] Habitation: `HabitationMarcel.asset`
- [ ] SeizableObjects array (15 entries):
  - TV (1, 1, Weight 5)
  - Vase (1, 1, Weight 5)
  - Tableau (1, 1, Weight 5)
  - Montre (1, 1, Weight 4)
  - Statue (1, 1, Weight 4)
  - Horloge (1, 1, Weight 4)
  - Lampe (1, 1, Weight 4)
  - Livre (1, 1, Weight 3)
  - Disque (1, 1, Weight 3)
  - Console (1, 1, Weight 4)
  - AppareilPhoto (1, 1, Weight 3)
  - Collier (1, 1, Weight 5)
  - Tapis (1, 1, Weight 4)
  - Vin (1, 1, Weight 3)
  - Café (1, 1, Weight 3)

### Step 7 — Créer/Configurer Scene Mission_01

`Assets/Scenes/Mission/Mission_01.unity`:

1. **Architecture basique** (placeholder, Blender ou cubes Unity)
   - Maison de ville RdC + étage
   - Intérieur: 6 pièces minimum (couloir, salon, cuisine, bureau, 2 chambres)

2. **Placer meubles**
   - Commode entrée (existant)
   - Canapé salon
   - Table basse
   - Lit chambre

3. **Placer 15 objets valeur**
   - Salon: TV, vase, horloge, tableau
   - Cuisine: café, vin
   - Bureau: statue, lampe, livre, disque, appareil photo
   - Chambre: montre, collier
   - Console: random

4. **Placer propriétaire**
   - ProprioTest prefab, spawn point, waypoints routine

5. **Assigner scene à MissionData_01**
   - SceneName: "Mission_01"

---

## Checklist Finale

- [ ] 12 modèles Blender créés (4 fragile + 8 déformable)
- [ ] 4 prefabs Shattered avec rigidbodies
- [ ] 8 matériaux Damaged créés
- [ ] 13 ObjetData remplis BreakProfile
- [ ] Mission_01.unity créée et configurée
- [ ] Tests: vase fragile → topple → shatter ✓
- [ ] Tests: TV deform → écran craquelé ✓
- [ ] Tests: fourchette indestructible ✓
- [ ] Tests: variance aléatoire ✓
- [ ] Mission_01 jouable bout-en-bout

