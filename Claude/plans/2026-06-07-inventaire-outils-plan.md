# Chaîne Inventaire / Outils — Plan d'implémentation

> **Pour workers agentiques :** SOUS-SKILL REQUISE : `superpowers:subagent-driven-development` (recommandé) ou `superpowers:executing-plans`. Étapes en cases `- [ ]`.

**Goal:** Rendre fonctionnelle la chaîne Shop → outils possédés → Casier (loadout) → roue en mission → outil en main + clic = effet, pour les 3 outils indépendants du proprio.

**Architecture:** Données/loadout dans `InventaireSystem` (sur le Player persistant). Usage outils via un `PlayerToolUser` central qui dispatche par `EffectType`. Deux panels UI (`ShopPanel`, `CasierUI`) héritant de `UIPanel`. Pas de save disque (mémoire via DontDestroyOnLoad).

**Tech Stack:** Unity (C#), uGUI (Canvas + EventSystems pour le drag-drop), EventBus maison. **Pas de tests auto** → vérification manuelle dans l'éditeur Unity à chaque tâche.

**Spec source:** `Claude/specs/2026-06-07-inventaire-outils-design.md`

**Convention commits:** Conventional Commits. Travailler sur une branche dédiée (`feat/inventaire-outils`).

---

## Structure des fichiers

**Créés :**
- `Assets/_Script/Systems/Player/PlayerToolUser.cs` — outil en main + clic → effet (dispatch EffectType)
- `Assets/_Script/UI_Persistent/ShopPanel.cs` — boutique 2 onglets
- `Assets/_Script/UI_Persistent/CasierUI.cs` — casier roue radiale drag-drop
- `Assets/_Script/UI_Persistent/CasierDragItem.cs` — chip draggable (EventSystems)
- `Assets/_Script/UI_Persistent/CasierDropSlot.cs` — slot cible de drop

**Modifiés :**
- `Assets/_Script/_Data/OutilData.cs` — champ `MaxCarryPerMission`
- `Assets/_Script/Systems/Inventory/InventaireSystem.cs` — loadout équipé + registre conso
- `Assets/_Script/_Core/Events/GameEvents.cs` — event `OnToolChannelProgress`
- `Assets/_Script/UI_Persistent/InventaireWheel.cs` — remap 8 directions, lit l'équipé, déclenche PlayerToolUser
- `Assets/_Script/Systems/OpenableInteractable.cs` — retirer le forçage via E (`AttemptForce`)
- `Assets/_Script/Systems/HubPNJ.cs` — brancher Boutique→ShopPanel, Inventaire→CasierUI
- `Assets/_Script/UI_Persistent/UIManager.cs` — déjà ok (GetPanel<T>), rien à coder sauf si besoin d'accès

---

## PHASE 1 — Données & loadout + remap roue

### Task 1 : Champ `MaxCarryPerMission` sur `OutilData`

**Files:** Modify `Assets/_Script/_Data/OutilData.cs`

- [ ] **Step 1 — Ajouter le champ** dans la section USAGE (après `ShopStackSize`) :

```csharp
    [Tooltip("Consommables : nombre max emporté par mission (loadout). 0 = illimité (à éviter).")]
    public int MaxCarryPerMission = 3;
```

- [ ] **Step 2 — Vérif Unity** : ouvrir un asset OutilData consommable dans l'Inspector, confirmer que le champ `Max Carry Per Mission` apparaît. Régler une valeur (ex: 3).

- [ ] **Step 3 — Commit**

```bash
git add Assets/_Script/_Data/OutilData.cs
git commit -m "feat: add MaxCarryPerMission to OutilData"
```

---

### Task 2 : Loadout équipé dans `InventaireSystem`

**Files:** Modify `Assets/_Script/Systems/Inventory/InventaireSystem.cs`

- [ ] **Step 1 — Ajouter les champs d'état** (sous les dictionnaires existants) :

```csharp
    // Loadout équipé (ce qui va dans la roue en mission)
    public const int MAX_OUTILS_EQUIPES = 3;
    public const int MAX_CONSOS_EQUIPES = 3;

    private readonly List<OutilData> _outilsEquipes = new();
    private readonly List<ConsoEquipe> _consosEquipes = new();

    // Registre type→OutilData pour les consommables (pour retrouver MaxCarryPerMission/icône)
    private readonly Dictionary<string, OutilData> _consoDefs = new();
```

- [ ] **Step 2 — Ajouter la struct `ConsoEquipe`** en bas du fichier (hors classe ou imbriquée) :

```csharp
[System.Serializable]
public struct ConsoEquipe
{
    public string Type;
    public int    Quantite;
    public ConsoEquipe(string type, int quantite) { Type = type; Quantite = quantite; }
}
```

- [ ] **Step 3 — Enregistrer les defs conso** : modifier `AjouterConsommable` pour accepter la def. Ajouter une surcharge :

```csharp
    /// <summary>Ajoute un consommable depuis sa définition (enregistre la def pour le loadout).</summary>
    public void AjouterConsommable(OutilData def, int quantite, float prixUnitaire = 0f)
    {
        if (def == null) return;
        _consoDefs[def.ToolName] = def;
        AjouterConsommable(def.ToolName, quantite, prixUnitaire);
    }
```

- [ ] **Step 4 — API loadout outils** :

```csharp
    public IReadOnlyList<OutilData> OutilsEquipes => _outilsEquipes;

    public bool OutilEstEquipe(OutilData def) => def != null && _outilsEquipes.Contains(def);

    public bool EquiperOutil(OutilData def)
    {
        if (def == null || !_outils.ContainsKey(def)) return false; // doit être possédé
        if (_outilsEquipes.Contains(def)) return true;
        if (_outilsEquipes.Count >= MAX_OUTILS_EQUIPES) return false;
        _outilsEquipes.Add(def);
        return true;
    }

    public void DesequiperOutil(OutilData def) => _outilsEquipes.Remove(def);
```

- [ ] **Step 5 — API loadout consommables** :

```csharp
    public IReadOnlyList<ConsoEquipe> ConsosEquipes => _consosEquipes;

    public int MaxCarryConso(string type)
    {
        if (_consoDefs.TryGetValue(type, out var def) && def.MaxCarryPerMission > 0)
            return def.MaxCarryPerMission;
        return 99;
    }

    public bool ConsoEstEquipe(string type) => _consosEquipes.Exists(c => c.Type == type);

    public bool EquiperConso(string type, int quantite)
    {
        if (string.IsNullOrEmpty(type)) return false;
        if (!_consommables.TryGetValue(type, out int stock) || stock <= 0) return false;

        int max = Mathf.Min(MaxCarryConso(type), stock);
        int q   = Mathf.Clamp(quantite, 1, max);

        int idx = _consosEquipes.FindIndex(c => c.Type == type);
        if (idx >= 0) { _consosEquipes[idx] = new ConsoEquipe(type, q); return true; }
        if (_consosEquipes.Count >= MAX_CONSOS_EQUIPES) return false;
        _consosEquipes.Add(new ConsoEquipe(type, q));
        return true;
    }

    public void DesequiperConso(string type) => _consosEquipes.RemoveAll(c => c.Type == type);
```

- [ ] **Step 6 — Récupérer la def d'un conso** (utile pour l'UI) :

```csharp
    public OutilData ConsoDef(string type) => _consoDefs.TryGetValue(type, out var d) ? d : null;
```

- [ ] **Step 7 — Auto-équiper les outils de départ** : à la fin de `Start()`, après l'ajout du badge/téléphone, équiper d'office les outils possédés (≤3) pour que la roue ne soit pas vide en début de slice :

```csharp
        foreach (var kv in _outils)
        {
            if (_outilsEquipes.Count >= MAX_OUTILS_EQUIPES) break;
            _outilsEquipes.Add(kv.Key);
        }
```

- [ ] **Step 8 — Vérif Unity** : entrer en Play, via un `Debug.Log` temporaire dans `Start` afficher `OutilsEquipes.Count`. Confirmer 0–3 selon outils possédés. Retirer le log.

- [ ] **Step 9 — Commit**

```bash
git add Assets/_Script/Systems/Inventory/InventaireSystem.cs
git commit -m "feat: equipped loadout API (tools+consumables) in InventaireSystem"
```

---

### Task 3 : Remap roue 8 directions (lit l'équipé)

**Files:** Modify `Assets/_Script/UI_Persistent/InventaireWheel.cs`

> Nouvelle sémantique des 9 slots (réassigner dans l'Inspector en Step 4) :
> 0=Centre(deadzone) · 1=Haut(MAINS) · 2..4 = Droite/BasDroite/HautDroite = CONSO ·
> 5..7 = Gauche/BasGauche/HautGauche = OUTILS · 8=Bas(BADGE+MANDAT).
> Pour rester simple on garde le tableau `_slots[9]` mais on change quelles cases sont outils/conso/fixes.

- [ ] **Step 1 — Redéfinir les rôles des slots** : remplacer les blocs de remplissage dans `RafraichirSlots()`. Les **outils** lisent `_inventaire.OutilsEquipes`, les **conso** lisent `_inventaire.ConsosEquipes` :

```csharp
    private void RafraichirSlots()
    {
        if (_inventaire == null) return;

        SetSlotMains(_slots[SLOT_HAUT]);          // Mains = HAUT désormais
        SetSlotDocuments(_slots[SLOT_BAS]);       // Badge+Mandat = BAS (fixe)

        // OUTILS équipés → 3 slots gauche
        var outils = _inventaire.OutilsEquipes;
        int[] slotsOutils = { SLOT_GAUCHE, SLOT_BAS_GAUCHE, SLOT_HAUT_GAUCHE };
        for (int i = 0; i < slotsOutils.Length; i++)
        {
            var slot = _slots[slotsOutils[i]];
            if (slot == null) continue;
            if (i < outils.Count) SetSlotOutil(slot, outils[i]);
            else                  SetSlotVide(slot);
        }

        // CONSO équipés → 3 slots droite
        var consos = _inventaire.ConsosEquipes;
        int[] slotsConso = { SLOT_DROIT, SLOT_BAS_DROIT, SLOT_HAUT_DROIT };
        for (int i = 0; i < slotsConso.Length; i++)
        {
            var slot = _slots[slotsConso[i]];
            if (slot == null) continue;
            if (i < consos.Count) SetSlotConsommable(slot, consos[i].Type, _inventaire.QuantiteConsommable(consos[i].Type));
            else                  SetSlotVide(slot);
        }

        MettreAJourVisuels();
    }
```

- [ ] **Step 2 — Ajouter `SetSlotDocuments`** (slot officiel fixe) :

```csharp
    private void SetSlotDocuments(WheelSlot slot)
    {
        if (slot == null) return;
        slot.EstSlotMains = false;
        slot.OutilAssocie = null;
        slot.ConsommableAssocie = "";
        if (slot.Label != null) slot.Label.text = "Badge + Mandat";
        if (slot.Quantite != null) slot.Quantite.gameObject.SetActive(false);
    }
```

- [ ] **Step 3 — Déclencher l'outil sur sélection** : dans `SelectionnerSlot`, brancher l'outil au `PlayerToolUser` au lieu du simple log. Ajouter un champ ref + l'utiliser :

```csharp
    [SerializeField] private PlayerToolUser _toolUser; // injecté via SetRefs (Task 7)
```

Et dans `SelectionnerSlot`, remplacer la branche outil et la branche mains :

```csharp
        if (slot.EstSlotMains)
        {
            _slotActif = SLOT_HAUT;
            _toolUser?.RangerOutil();
            return;
        }

        if (slot.OutilAssocie != null)
        {
            _slotActif = index;
            _toolUser?.PrendreOutil(slot.OutilAssocie, _inventaire.NiveauOutil(slot.OutilAssocie));
            return;
        }
```

> Note : `SLOT_CENTRE` reste la deadzone (aucune sélection). `_slotActif` initial passe à `SLOT_HAUT` (mains). Mettre à jour les valeurs par défaut `_slotSelectionne`/`_slotActif` = `SLOT_HAUT` dans les déclarations.

- [ ] **Step 4 — Réassigner les slots dans l'Inspector** : sur le prefab/scène `Canvas_InventaireWheel`, vérifier que `_slots[1]`=Haut(mains), `_slots[8]`=Bas(docs), `_slots[4,7,8...]`... → suivre la table du Step 0. Repositionner visuellement Haut/Bas/Gauche×3/Droite×3.

- [ ] **Step 5 — Vérif Unity** : Play en mission, maintenir Tab : la roue montre Mains en haut, Badge+Mandat en bas, outils équipés à gauche, conso à droite. Sélectionner un outil → (log temporaire dans PlayerToolUser plus tard) ; pour l'instant vérifier qu'aucune erreur n'apparaît.

- [ ] **Step 6 — Commit**

```bash
git add Assets/_Script/UI_Persistent/InventaireWheel.cs
git commit -m "feat: wheel 8-direction remap reading equipped loadout"
```

---

## PHASE 2 — PlayerToolUser + 3 effets

### Task 4 : Event `OnToolChannelProgress`

**Files:** Modify `Assets/_Script/_Core/Events/GameEvents.cs`

- [ ] **Step 1 — Ajouter le struct event** :

```csharp
/// <summary>Progression de canalisation d'un outil (crochetage, marteau…). Pour la jauge HUD.</summary>
public struct OnToolChannelProgress
{
    public float Progress01;  // 0..1
    public bool  Active;      // false = canalisation finie/annulée
}
```

- [ ] **Step 2 — Vérif** : compile sans erreur (Unity recompile, 0 erreur console).

- [ ] **Step 3 — Commit**

```bash
git add Assets/_Script/_Core/Events/GameEvents.cs
git commit -m "feat: OnToolChannelProgress event"
```

---

### Task 5 : `PlayerToolUser` (outil en main + clic + effets)

**Files:** Create `Assets/_Script/Systems/Player/PlayerToolUser.cs` · Modify `PlayerCarry.cs`

- [ ] **Step 1 — Créer le script** :

```csharp
// ============================================================
// PlayerToolUser.cs — Bailiff & Co
// Outil "en main" + clic gauche = utiliser sur la cible visée.
// Tap = effet instantané ; maintenu = canalisation (jauge).
// Mutuellement exclusif avec PlayerCarry (porter un objet).
// ============================================================
using UnityEngine;

[RequireComponent(typeof(PlayerCarry))]
public class PlayerToolUser : MonoBehaviour
{
    [SerializeField] private PlayerConfigData _config;
    [SerializeField] private Transform _camera;
    [SerializeField] private PlayerCarry _carry;

    private OutilData _outilActif;
    private int       _niveauActif;

    // canalisation
    private bool  _channeling;
    private float _channelTime;
    private float _channelDuration;
    private OpenableInteractable _channelTarget;

    public OutilData OutilActif => _outilActif;
    public bool ARienEnMain => _outilActif == null;

    private void Awake()
    {
        if (_carry == null) _carry = GetComponent<PlayerCarry>();
        if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
    }

    /// <summary>Met un outil en main. Refusé si un objet de valeur est porté.</summary>
    public void PrendreOutil(OutilData outil, int niveau)
    {
        if (outil == null) return;
        if (_carry != null && _carry.EstEnTrain) return; // exclusion : objet porté
        _outilActif  = outil;
        _niveauActif = Mathf.Max(0, niveau);
    }

    public void RangerOutil()
    {
        AnnulerCanalisation();
        _outilActif = null;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.InputJoueurActif)
        {
            AnnulerCanalisation();
            return;
        }
        if (_outilActif == null) return;

        // Tap (instantané) sur clic down
        if (Input.GetMouseButtonDown(0))
            UtiliserTap();

        // Canalisation (maintenu)
        if (Input.GetMouseButton(0))
            TickCanalisation();
        else
            AnnulerCanalisation();
    }

    private bool RaycastCible(out RaycastHit hit)
    {
        Transform o = _camera != null ? _camera : transform;
        float range = _config != null ? _config.InteractionRange : 3f;
        return Physics.Raycast(o.position, o.forward, out hit, range,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
    }

    // ---- TAP : ForceDoor + ScanObject ----
    private void UtiliserTap()
    {
        if (!RaycastCible(out var hit)) return;

        switch (_outilActif.EffectType)
        {
            case ToolEffectType.ForceDoor:
            {
                var op = hit.collider.GetComponentInParent<OpenableInteractable>();
                if (op != null && op.IsLocked) op.ForceOpen(); // bruyant
                break;
            }
            case ToolEffectType.ScanUV: // (placeholder — UV plus tard)
                break;
            case ToolEffectType.Lockpick:
                break; // géré en canalisation
            default:
            {
                // ScanObject (scanner/téléphone) : EffectType utilisé = ScanUV ? non.
                // On scanne via un check de composant ValueObject quel que soit le type "scanner".
                break;
            }
        }

        // Scanner : si l'outil scanne, révéler l'objet visé
        if (EstScanner(_outilActif))
        {
            var vo = hit.collider.GetComponentInParent<ValueObject>();
            if (vo != null) vo.Scan();
        }
    }

    private bool EstScanner(OutilData o)
        => o.EffectType == ToolEffectType.ScanUV || o.EffectType == ToolEffectType.ScanXRay
        || o.Category == ToolCategory.Scanner;

    // ---- CANALISATION : Lockpick ----
    private void TickCanalisation()
    {
        if (_outilActif.EffectType != ToolEffectType.Lockpick) return;

        if (!_channeling)
        {
            if (!RaycastCible(out var hit)) return;
            var op = hit.collider.GetComponentInParent<OpenableInteractable>();
            if (op == null || !op.IsLocked) return;
            _channelTarget   = op;
            _channelDuration = DureeCanalisation();
            _channelTime     = 0f;
            _channeling      = true;
        }

        // perdre la cible = annule
        if (!RaycastCible(out var h) || h.collider.GetComponentInParent<OpenableInteractable>() != _channelTarget)
        {
            AnnulerCanalisation();
            return;
        }

        _channelTime += Time.deltaTime;
        float p = Mathf.Clamp01(_channelTime / _channelDuration);
        EventBus<OnToolChannelProgress>.Raise(new OnToolChannelProgress { Progress01 = p, Active = true });

        if (p >= 1f)
        {
            _channelTarget.Unlock();      // silencieux
            _channelTarget.Interact(gameObject); // ouvre dans la foulée (Closed→Open)
            AnnulerCanalisation();
        }
    }

    private float DureeCanalisation()
    {
        if (_outilActif?.Levels != null && _niveauActif < _outilActif.Levels.Length
            && _outilActif.Levels[_niveauActif] != null
            && _outilActif.Levels[_niveauActif].EffectDuration > 0f)
            return _outilActif.Levels[_niveauActif].EffectDuration;
        return 2f;
    }

    private void AnnulerCanalisation()
    {
        if (!_channeling) return;
        _channeling   = false;
        _channelTarget = null;
        EventBus<OnToolChannelProgress>.Raise(new OnToolChannelProgress { Progress01 = 0f, Active = false });
    }
}
```

- [ ] **Step 2 — Exclusion réciproque dans `PlayerCarry.Saisir`** : au début de `Saisir`, ranger l'outil en main. Ajouter un champ + appel :

```csharp
    [SerializeField] private PlayerToolUser _toolUser;
```

Dans `Saisir`, juste après `if (_objetPorte != null) return;` :

```csharp
        if (_toolUser == null) _toolUser = GetComponent<PlayerToolUser>();
        _toolUser?.RangerOutil();
```

- [ ] **Step 3 — Unity setup** : ajouter le composant `PlayerToolUser` sur le Player (même GameObject que PlayerCarry). Assigner `_config`, `_camera`, `_carry`. Sur PlayerCarry assigner `_toolUser`.

- [ ] **Step 4 — Vérif Unity** : Play mission. Sélectionner pied-de-biche dans la roue → viser une porte verrouillée → clic → elle s'ouvre (bruyant). Sélectionner scanner → viser un objet → clic → label révèle nom+valeur. Sélectionner crochetage → viser porte verrouillée → maintenir clic ~2s → s'ouvre en silence. Porter un objet → impossible de prendre un outil ; prendre un objet range l'outil.

- [ ] **Step 5 — Commit**

```bash
git add Assets/_Script/Systems/Player/PlayerToolUser.cs Assets/_Script/Systems/Player/PlayerCarry.cs
git commit -m "feat: PlayerToolUser — tool-in-hand click effects (force/lockpick/scan)"
```

---

### Task 6 : Retirer le forçage de porte via E (`OpenableInteractable`)

**Files:** Modify `Assets/_Script/Systems/OpenableInteractable.cs`

- [ ] **Step 1 — Neutraliser `AttemptForce`** : le forçage passe désormais par PlayerToolUser. Dans `Interact`, la branche `Locked` ne force plus :

```csharp
            case OpenableState.Locked:
                // Verrouillé : nécessite un outil en main (pied-de-biche / crochetage).
                // Plus de forçage via E. (Optionnel : petit son "verrouillé" — backlog UX.)
                break;
```

- [ ] **Step 2 — Supprimer la méthode `AttemptForce`** (devenue morte) et l'`using` inutile si présent. Garder `ForceOpen()` et `Unlock()` (appelés par PlayerToolUser).

- [ ] **Step 3 — Vérif Unity** : E sur une porte verrouillée ne fait plus rien ; seul l'outil en main l'ouvre (Task 5).

- [ ] **Step 4 — Commit**

```bash
git add Assets/_Script/Systems/OpenableInteractable.cs
git commit -m "refactor: locked doors opened via tool-in-hand, not E ownership check"
```

---

### Task 7 : Brancher `PlayerToolUser` à la roue (injection)

**Files:** Modify `Assets/_Script/UI_Persistent/InventaireWheel.cs` · `Assets/_Script/UI_Persistent/UIManager.cs`

- [ ] **Step 1 — Étendre `SetRefs`** de la roue pour recevoir le toolUser :

```csharp
    public void SetRefs(InventaireSystem inventaire, PlayerCarry carry, PlayerToolUser toolUser)
    {
        _inventaire = inventaire;
        _carry      = carry;
        _toolUser   = toolUser;
        RafraichirSlots();
    }
```

- [ ] **Step 2 — Mettre à jour `UIManager.OnJoueurSpawne`** pour passer le toolUser :

```csharp
    public void OnJoueurSpawne(InventaireSystem inventaire, PlayerCarry carry, PlayerToolUser toolUser)
    {
        var wheels = FindObjectsByType<InventaireWheel>(FindObjectsSortMode.None);
        var wheel = wheels.Length > 0 ? wheels[0] : null;
        if (wheel != null) wheel.SetRefs(inventaire, carry, toolUser);
        else Debug.LogWarning("[UIManager] InventaireWheel introuvable — SetRefs ignoré.");
    }
```

- [ ] **Step 3 — Mettre à jour les 2 appelants** (`MissionBuilder.SpawnPlayer`, `CampaignMissionStarter.SpawnPlayer`) :

```csharp
        UIManager.Instance?.OnJoueurSpawne(
            player.GetComponentInChildren<InventaireSystem>(),
            player.GetComponent<PlayerCarry>(),
            player.GetComponent<PlayerToolUser>());
```

- [ ] **Step 4 — Vérif Unity** : Play depuis le Hub → mission. Sélection d'outil dans la roue déclenche bien l'effet (la ref toolUser n'est plus nulle).

- [ ] **Step 5 — Commit**

```bash
git add Assets/_Script/UI_Persistent/InventaireWheel.cs Assets/_Script/UI_Persistent/UIManager.cs Assets/_Script/Systems/Mission/MissionBuilder.cs Assets/_Script/Systems/Mission/CampaignMissionStarter.cs
git commit -m "feat: inject PlayerToolUser into inventory wheel on spawn"
```

---

## PHASE 3 — ShopPanel

### Task 8 : Script `ShopPanel`

**Files:** Create `Assets/_Script/UI_Persistent/ShopPanel.cs`

> UI uGUI : le script gère la logique + remplit des conteneurs. La structure Canvas (onglets, listes, fiche, grille) est montée dans la scène `UI_Persistent` en Step 3. Le script instancie des lignes/cartes depuis des prefabs simples (TextMeshPro + Button).

- [ ] **Step 1 — Créer le script** (logique complète ; les refs UI sont assignées en Inspector) :

```csharp
// ShopPanel.cs — Boutique Hub (2 onglets : Outils / Consommables)
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : UIPanel
{
    [Header("Catalogue (assigné en Inspector)")]
    [SerializeField] private OutilData[] _catalogue;

    [Header("Onglets")]
    [SerializeField] private Button _ongletOutils;
    [SerializeField] private Button _ongletConso;

    [Header("Outils — maître/détail")]
    [SerializeField] private Transform _listeOutilsRoot;   // contenu scrollable
    [SerializeField] private GameObject _ligneOutilPrefab; // prefab : Button + TMP nom + TMP pips
    [SerializeField] private GameObject _ficheOutil;       // panneau détail
    [SerializeField] private TMP_Text _ficheNom, _ficheDesc, _ficheNiveaux;
    [SerializeField] private Button _ficheBouton;
    [SerializeField] private TMP_Text _ficheBoutonLabel;

    [Header("Conso — grille")]
    [SerializeField] private Transform _grilleConsoRoot;
    [SerializeField] private GameObject _carteConsoPrefab; // prefab : icône + TMP + stepper + Button

    [Header("Argent")]
    [SerializeField] private TMP_Text _argentLabel;

    private InventaireSystem _inv;
    private OutilData _selection;

    protected override void OnEnable()
    {
        base.OnEnable();
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        if (_ongletOutils) _ongletOutils.onClick.AddListener(() => AfficherOnglet(true));
        if (_ongletConso)  _ongletConso.onClick.AddListener(() => AfficherOnglet(false));
        AfficherOnglet(true);
        MajArgent();
    }

    protected override void OnDisable()
    {
        if (_ongletOutils) _ongletOutils.onClick.RemoveAllListeners();
        if (_ongletConso)  _ongletConso.onClick.RemoveAllListeners();
        base.OnDisable();
    }

    private void MajArgent()
    {
        if (_argentLabel) _argentLabel.text = (GameManager.Instance?.Argent ?? 0f).ToString("N0") + " €";
    }

    private void AfficherOnglet(bool outils)
    {
        if (_listeOutilsRoot) _listeOutilsRoot.parent.gameObject.SetActive(outils);
        if (_ficheOutil) _ficheOutil.SetActive(outils);
        if (_grilleConsoRoot) _grilleConsoRoot.parent.gameObject.SetActive(!outils);
        if (outils) RemplirOutils(); else RemplirConso();
    }

    private bool EstVerrouille(OutilData o)
        => o.UnlocksAfterMission > (GameManager.Instance?.DerniereMissionCompletee ?? 0);

    // ---- OUTILS ----
    private void RemplirOutils()
    {
        Vider(_listeOutilsRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || o.IsConsumable) continue;
            var go = Instantiate(_ligneOutilPrefab, _listeOutilsRoot);
            var label = go.GetComponentInChildren<TMP_Text>();
            int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
            label.text = o.ToolName + (niv >= 0 ? "  " + Pips(niv, o.Levels.Length) : (EstVerrouille(o) ? "  🔒 M" + o.UnlocksAfterMission : ""));
            var btn = go.GetComponent<Button>();
            var captured = o;
            if (btn) btn.onClick.AddListener(() => { _selection = captured; RemplirFiche(); });
        }
        if (_selection == null) _selection = System.Array.Find(_catalogue, x => x != null && !x.IsConsumable);
        RemplirFiche();
    }

    private void RemplirFiche()
    {
        if (_selection == null || _ficheOutil == null) return;
        var o = _selection;
        int niv = _inv != null ? _inv.NiveauOutil(o) : -1;
        bool possede = niv >= 0;
        bool verrou = EstVerrouille(o);

        _ficheNom.text  = o.ToolName + (possede ? "  " + Pips(niv, o.Levels.Length) : "");
        _ficheDesc.text = o.Description;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < o.Levels.Length; i++)
        {
            var lv = o.Levels[i]; if (lv == null) continue;
            string cout = i == 0 ? (possede ? "acquis" : o.PurchasePrice + " €") : lv.UpgradeCost + " €";
            string mark = (possede && i == niv) ? " ◄" : "";
            sb.AppendLine($"{lv.LevelName}  —  {lv.EffectDescription}  ({cout}){mark}");
        }
        _ficheNiveaux.text = sb.ToString();

        // Bouton
        _ficheBouton.onClick.RemoveAllListeners();
        if (verrou && !possede)
        {
            _ficheBoutonLabel.text = "🔒 Mission " + o.UnlocksAfterMission;
            _ficheBouton.interactable = false;
        }
        else if (!possede)
        {
            bool ok = (GameManager.Instance?.Argent ?? 0) >= o.PurchasePrice;
            _ficheBoutonLabel.text = ok ? $"Acheter · {o.PurchasePrice} €" : "Fonds insuffisants";
            _ficheBouton.interactable = ok;
            _ficheBouton.onClick.AddListener(() => { Acheter(o); });
        }
        else if (niv >= o.Levels.Length - 1)
        {
            _ficheBoutonLabel.text = "Niveau max";
            _ficheBouton.interactable = false;
        }
        else
        {
            int cost = o.Levels[niv + 1].UpgradeCost;
            bool ok = (GameManager.Instance?.Argent ?? 0) >= cost;
            _ficheBoutonLabel.text = ok ? $"Upgrader · {cost} €" : "Fonds insuffisants";
            _ficheBouton.interactable = ok;
            _ficheBouton.onClick.AddListener(() => { Upgrader(o); });
        }
    }

    private void Acheter(OutilData o)
    {
        if (!(GameManager.Instance?.PeutPayer(o.PurchasePrice) ?? false)) return;
        GameManager.Instance.Debiter(o.PurchasePrice);
        _inv.AjouterOutil(o);
        MajArgent(); RemplirOutils();
    }

    private void Upgrader(OutilData o)
    {
        int niv = _inv.NiveauOutil(o);
        int cost = o.Levels[niv + 1].UpgradeCost;
        if (!(GameManager.Instance?.PeutPayer(cost) ?? false)) return;
        GameManager.Instance.Debiter(cost);
        _inv.UpgraderOutil(o);
        MajArgent(); RemplirOutils();
    }

    // ---- CONSO ----
    private readonly Dictionary<string,int> _qtyAchat = new();

    private void RemplirConso()
    {
        Vider(_grilleConsoRoot);
        foreach (var o in _catalogue)
        {
            if (o == null || !o.IsConsumable) continue;
            var go = Instantiate(_carteConsoPrefab, _grilleConsoRoot);
            var card = go.GetComponent<ShopConsoCard>(); // petit binder (Step 2)
            int qty = _qtyAchat.TryGetValue(o.ToolName, out var q) ? q : 1;
            card.Bind(o, qty, EstVerrouille(o), _inv,
                onStep: (d) => { int nq = Mathf.Clamp(qty + d, 1, 10); _qtyAchat[o.ToolName] = nq; RemplirConso(); },
                onBuy:  () => { AcheterConso(o, qty); });
        }
    }

    private void AcheterConso(OutilData o, int qty)
    {
        int total = o.PurchasePrice * qty;
        if (!(GameManager.Instance?.PeutPayer(total) ?? false)) return;
        GameManager.Instance.Debiter(total);
        _inv.AjouterConsommable(o, qty, o.PurchasePrice);
        _qtyAchat[o.ToolName] = 1;
        MajArgent(); RemplirConso();
    }

    private static string Pips(int level, int total)
    { var s=""; for(int i=0;i<total;i++) s += i<=level?"●":"○"; return s; }

    private static void Vider(Transform t)
    { if (t==null) return; for (int i=t.childCount-1;i>=0;i--) Destroy(t.GetChild(i).gameObject); }
}
```

- [ ] **Step 2 — Créer le binder `ShopConsoCard`** (sur le prefab carte conso) :

```csharp
// ShopConsoCard.cs — binder d'une carte consommable du shop
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopConsoCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _nom, _meta, _possedes, _qty, _total, _boutonLabel;
    [SerializeField] private Button _moins, _plus, _acheter;
    [SerializeField] private GameObject _lockOverlay;

    public void Bind(OutilData o, int qty, bool locked, InventaireSystem inv, Action<int> onStep, Action onBuy)
    {
        _nom.text = o.ToolName;
        if (_lockOverlay) _lockOverlay.SetActive(locked);
        if (locked)
        {
            _meta.text = "🔒 Mission " + o.UnlocksAfterMission;
            _moins.interactable = _plus.interactable = _acheter.interactable = false;
            return;
        }
        int total = o.PurchasePrice * qty;
        bool ok = (GameManager.Instance?.Argent ?? 0) >= total;
        _meta.text     = o.PurchasePrice + " € / unité · max " + o.MaxCarryPerMission + "/mission";
        _possedes.text = "possédés : " + (inv != null ? inv.QuantiteConsommable(o.ToolName) : 0);
        _qty.text      = qty.ToString();
        _total.text    = "total : " + total + " €";
        _boutonLabel.text = ok ? "Acheter ×" + qty : "Fonds insuffisants";
        _moins.onClick.RemoveAllListeners(); _plus.onClick.RemoveAllListeners(); _acheter.onClick.RemoveAllListeners();
        _moins.interactable = qty > 1; _plus.interactable = qty < 10; _acheter.interactable = ok;
        _moins.onClick.AddListener(() => onStep(-1));
        _plus.onClick.AddListener(() => onStep(1));
        _acheter.onClick.AddListener(() => onBuy());
    }
}
```

- [ ] **Step 3 — Unity setup** (scène `UI_Persistent.unity`) :
  1. Créer `Canvas_Shop` (Blocking). Y mettre `ShopPanel` (UIPanel : `panelType=Blocking`, `_contexteVisibles=[Hub]`, `_autoAfficher=false`).
  2. Construire : barre haut (titre + `_argentLabel`), 2 boutons onglets, conteneur Outils (ScrollView `_listeOutilsRoot` + fiche `_ficheOutil` avec `_ficheNom/_ficheDesc/_ficheNiveaux/_ficheBouton/_ficheBoutonLabel`), conteneur Conso (`_grilleConsoRoot` en GridLayoutGroup).
  3. Prefabs : `_ligneOutilPrefab` (Button + TMP), `_carteConsoPrefab` (avec `ShopConsoCard` + ses refs).
  4. Assigner `_catalogue` (les OutilData de la slice).

- [ ] **Step 4 — Vérif Unity** : Hub → ouvrir le shop (temporairement via un bouton ou `UIManager.Instance.GetPanel<ShopPanel>().Ouvrir()`). Acheter/upgrader un outil → argent débité, pips montent. Onglet conso → stepper 1-10, acheter → possédés augmentent. Outil verrouillé grisé.

- [ ] **Step 5 — Commit**

```bash
git add Assets/_Script/UI_Persistent/ShopPanel.cs Assets/_Script/UI_Persistent/ShopConsoCard.cs
git commit -m "feat: ShopPanel (tools master-detail + consumables grid 1-10)"
```

---

### Task 9 : Brancher le PNJ Boutique

**Files:** Modify `Assets/_Script/Systems/HubPNJ.cs` (+ HubManager si besoin)

- [ ] **Step 1 — Ouvrir le ShopPanel** : `HubManager.OuvrirPanelShop` existe et appelle `_hubUI?.OuvrirPanelShop()`. Le plus simple : faire ouvrir le panel via UIManager. Dans `HubPNJ.Interact`, cas `Boutique` :

```csharp
                case TypePanneau.Boutique:
                    UIManager.Instance?.GetPanel<ShopPanel>()?.Ouvrir();
                    break;
```

- [ ] **Step 2 — Vérif Unity** : un PNJ Hub de type Boutique, E dessus → la boutique s'ouvre.

- [ ] **Step 3 — Commit**

```bash
git add Assets/_Script/Systems/HubPNJ.cs
git commit -m "feat: Boutique NPC opens ShopPanel"
```

---

## PHASE 4 — CasierUI (roue radiale drag-drop)

### Task 10 : Scripts drag-drop (`CasierDragItem`, `CasierDropSlot`)

**Files:** Create `CasierDragItem.cs`, `CasierDropSlot.cs`

- [ ] **Step 1 — `CasierDragItem`** (chip déplaçable, EventSystems) :

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class CasierDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Kind { Tool, Conso }
    public Kind   ItemKind;
    public string Id;        // ToolName (outil) ou type (conso)

    private Transform _root;        // canvas root pour le drag visuel
    private CanvasGroup _cg;
    private Vector3 _start;

    private void Awake() { _cg = gameObject.AddComponent<CanvasGroup>(); }

    public void OnBeginDrag(PointerEventData e)
    {
        _root = GetComponentInParent<Canvas>().transform;
        _start = transform.position;
        transform.SetParent(_root, true);
        _cg.blocksRaycasts = false; // laisse passer le raycast vers les slots
        CasierUI.Current?.SetDrag(this);
    }
    public void OnDrag(PointerEventData e) { transform.position = e.position; }
    public void OnEndDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = true;
        CasierUI.Current?.EndDrag();
        // CasierUI re-render remplace les éléments → on détruit le fantôme
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2 — `CasierDropSlot`** (cible de drop) :

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class CasierDropSlot : MonoBehaviour, IDropHandler
{
    public enum Zone { ToolSlot, ConsoSlot, ToolPool, ConsoPool }
    public Zone SlotZone;
    public int  Index; // pour les slots de roue (0..2)

    public void OnDrop(PointerEventData e)
    {
        CasierUI.Current?.HandleDrop(this);
    }
}
```

- [ ] **Step 3 — Commit**

```bash
git add Assets/_Script/UI_Persistent/CasierDragItem.cs Assets/_Script/UI_Persistent/CasierDropSlot.cs
git commit -m "feat: casier drag-drop primitives"
```

---

### Task 11 : Script `CasierUI`

**Files:** Create `Assets/_Script/UI_Persistent/CasierUI.cs`

- [ ] **Step 1 — Créer le script** :

```csharp
// CasierUI.cs — Casier Hub : équiper le loadout (roue radiale, drag-drop)
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasierUI : UIPanel
{
    public static CasierUI Current { get; private set; }

    [Header("Colonnes possédés")]
    [SerializeField] private Transform _poolOutils;
    [SerializeField] private Transform _poolConso;
    [SerializeField] private GameObject _chipPrefab; // CasierDragItem + TMP + (pips/qty)

    [Header("Roue — slots (CasierDropSlot)")]
    [SerializeField] private Transform[] _slotsOutils = new Transform[3];
    [SerializeField] private Transform[] _slotsConso  = new Transform[3];
    [SerializeField] private GameObject _slotItemPrefab; // icône + nom + pips/stepper + ✕

    private InventaireSystem _inv;
    private CasierDragItem _drag;

    protected override void OnEnable()
    {
        base.OnEnable();
        Current = this;
        _inv = GameManager.Instance?.Player?.GetComponentInChildren<InventaireSystem>();
        Render();
    }
    protected override void OnDisable() { if (Current == this) Current = null; base.OnDisable(); }

    public void SetDrag(CasierDragItem d) => _drag = d;
    public void EndDrag() { _drag = null; }

    public void HandleDrop(CasierDropSlot slot)
    {
        if (_drag == null || _inv == null) return;
        switch (slot.SlotZone)
        {
            case CasierDropSlot.Zone.ToolSlot:
                if (_drag.ItemKind == CasierDragItem.Kind.Tool)
                    _inv.EquiperOutil(ResoudreOutil(_drag.Id));
                break;
            case CasierDropSlot.Zone.ConsoSlot:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso)
                    _inv.EquiperConso(_drag.Id, _inv.MaxCarryConso(_drag.Id));
                break;
            case CasierDropSlot.Zone.ToolPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Tool) _inv.DesequiperOutil(ResoudreOutil(_drag.Id));
                break;
            case CasierDropSlot.Zone.ConsoPool:
                if (_drag.ItemKind == CasierDragItem.Kind.Conso) _inv.DesequiperConso(_drag.Id);
                break;
        }
        Render();
    }

    private OutilData ResoudreOutil(string toolName)
    {
        foreach (var kv in _inv.Outils) if (kv.Key.ToolName == toolName) return kv.Key;
        return null;
    }

    private void Render()
    {
        if (_inv == null) return;
        // Pools (non équipés)
        Vider(_poolOutils);
        foreach (var kv in _inv.Outils)
            if (!_inv.OutilEstEquipe(kv.Key))
                ChipOutil(_poolOutils, kv.Key, kv.Value);

        Vider(_poolConso);
        foreach (var kv in _inv.Consommables)
            if (!_inv.ConsoEstEquipe(kv.Key))
                ChipConso(_poolConso, kv.Key, kv.Value);

        // Slots roue
        var outils = _inv.OutilsEquipes;
        for (int i = 0; i < _slotsOutils.Length; i++)
        {
            Vider(_slotsOutils[i]);
            if (i < outils.Count) SlotOutil(_slotsOutils[i], outils[i], _inv.NiveauOutil(outils[i]));
        }
        var consos = _inv.ConsosEquipes;
        for (int i = 0; i < _slotsConso.Length; i++)
        {
            Vider(_slotsConso[i]);
            if (i < consos.Count) SlotConso(_slotsConso[i], consos[i]);
        }
    }

    // --- création des chips/slots (TMP simples ; icônes optionnelles) ---
    private void ChipOutil(Transform parent, OutilData o, int niv)
    {
        var go = Instantiate(_chipPrefab, parent);
        var d = go.GetComponent<CasierDragItem>(); d.ItemKind = CasierDragItem.Kind.Tool; d.Id = o.ToolName;
        go.GetComponentInChildren<TMP_Text>().text = o.ToolName + "  " + Pips(niv, o.Levels.Length);
    }
    private void ChipConso(Transform parent, string type, int stock)
    {
        var go = Instantiate(_chipPrefab, parent);
        var d = go.GetComponent<CasierDragItem>(); d.ItemKind = CasierDragItem.Kind.Conso; d.Id = type;
        go.GetComponentInChildren<TMP_Text>().text = type + "  x" + stock + " · max " + _inv.MaxCarryConso(type);
    }
    private void SlotOutil(Transform slot, OutilData o, int niv)
    {
        var go = Instantiate(_slotItemPrefab, slot);
        var d = go.GetComponent<CasierDragItem>(); if (d){ d.ItemKind = CasierDragItem.Kind.Tool; d.Id = o.ToolName; }
        go.GetComponentInChildren<TMP_Text>().text = o.ToolName + "\n" + Pips(niv, o.Levels.Length);
        var x = go.transform.Find("Remove")?.GetComponent<Button>();
        if (x) x.onClick.AddListener(() => { _inv.DesequiperOutil(o); Render(); });
    }
    private void SlotConso(Transform slot, ConsoEquipe c)
    {
        var go = Instantiate(_slotItemPrefab, slot);
        var d = go.GetComponent<CasierDragItem>(); if (d){ d.ItemKind = CasierDragItem.Kind.Conso; d.Id = c.Type; }
        int mq = Mathf.Min(_inv.MaxCarryConso(c.Type), _inv.QuantiteConsommable(c.Type));
        go.GetComponentInChildren<TMP_Text>().text = c.Type + "\n" + c.Quantite + "/" + mq;
        var moins = go.transform.Find("Moins")?.GetComponent<Button>();
        var plus  = go.transform.Find("Plus")?.GetComponent<Button>();
        var x     = go.transform.Find("Remove")?.GetComponent<Button>();
        if (moins) moins.onClick.AddListener(() => { _inv.EquiperConso(c.Type, c.Quantite - 1); Render(); });
        if (plus)  plus.onClick.AddListener(()  => { _inv.EquiperConso(c.Type, c.Quantite + 1); Render(); });
        if (x)     x.onClick.AddListener(()     => { _inv.DesequiperConso(c.Type); Render(); });
    }

    private static string Pips(int level, int total){ var s=""; for(int i=0;i<total;i++) s+= i<=level?"●":"○"; return s; }
    private static void Vider(Transform t){ if(t==null) return; for(int i=t.childCount-1;i>=0;i--) Destroy(t.GetChild(i).gameObject); }
}
```

- [ ] **Step 2 — Unity setup** (scène `UI_Persistent.unity`) :
  1. `Canvas_Casier` (Blocking, contexte Hub, `_autoAfficher=false`).
  2. 3 colonnes : `_poolOutils` (gauche, avec un `CasierDropSlot` Zone=ToolPool sur le conteneur), roue centrale, `_poolConso` (droite, `CasierDropSlot` Zone=ConsoPool).
  3. Roue : 6 slots ronds → `_slotsOutils[0..2]` (gauche : NW,W,SW) et `_slotsConso[0..2]` (droite : NE,E,SE), chacun avec `CasierDropSlot` (Zone + Index). Slots fixes Mains (haut) et Badge+Mandat (bas) = déco non-droppable.
  4. Prefabs `_chipPrefab` (CasierDragItem + TMP) et `_slotItemPrefab` (TMP + boutons enfants nommés `Remove`/`Moins`/`Plus`). S'assurer qu'un `EventSystem` est présent dans la scène.

- [ ] **Step 3 — Vérif Unity** : Hub → ouvrir le casier. Glisser un outil de la colonne vers un slot roue gauche → équipé ; glisser hors / ✕ → retiré. Conso → équipé avec stepper qté/max. Lancer une mission → la roue (Tab) reflète le loadout choisi.

- [ ] **Step 4 — Commit**

```bash
git add Assets/_Script/UI_Persistent/CasierUI.cs
git commit -m "feat: CasierUI radial wheel drag-drop loadout"
```

---

### Task 12 : Brancher le PNJ Inventaire (Casier)

**Files:** Modify `Assets/_Script/Systems/HubPNJ.cs`

- [ ] **Step 1 — Cas `Inventaire`** :

```csharp
                case TypePanneau.Inventaire:
                    UIManager.Instance?.GetPanel<CasierUI>()?.Ouvrir();
                    break;
```

- [ ] **Step 2 — Vérif Unity** : PNJ Inventaire, E → casier s'ouvre.

- [ ] **Step 3 — Commit**

```bash
git add Assets/_Script/Systems/HubPNJ.cs
git commit -m "feat: Inventaire NPC opens CasierUI"
```

---

## Vérification end-to-end (slice)

1. Hub → PNJ Boutique : acheter pied-de-biche/crochetage/scanner + des conso ; upgrader un outil. Argent débité.
2. Hub → PNJ Inventaire (Casier) : équiper 3 outils (sur 4 possédés) + des conso avec quantité ≤ max.
3. Lancer la mission : Tab → roue = loadout choisi (mains haut, docs bas, outils gauche niveaux affichés, conso droite avec qté).
4. Sélectionner pied-de-biche → clic sur porte verrouillée → ouverture bruyante. Crochetage → maintenir → ouverture silencieuse. Scanner → clic sur objet → nom+valeur révélés.
5. Porter un objet → impossible de sortir un outil ; sortir un outil puis saisir un objet → l'outil se range.
6. Retour Hub → loadout & possédés conservés (persistance mémoire).

---

## Notes de self-review (couverture spec)

- Spec §4.1/4.1bis (loadout + MaxCarryPerMission) → Tasks 1,2.
- §4.2 (roue remap) → Task 3.
- §4.3 (PlayerToolUser + 3 effets + event jauge + retrait hack) → Tasks 4,5,6,7.
- §4.4 (Casier roue radiale drag-drop + niveaux + stepper conso) → Tasks 10,11,12.
- §4.5 (Shop 2 onglets maître/détail + grille conso 1-10 + gating) → Tasks 8,9.
- §4.6 (persistance mémoire) → assurée par InventaireSystem sur Player DontDestroyOnLoad (rien à coder).
- HORS scope (badge/menottes/spray, marteau, save disque, mini-jeu serrure) → non inclus, conforme.

**Jauge HUD de canalisation** : l'event `OnToolChannelProgress` est émis (Task 5). L'affichage visuel (barre HUD) est un petit ajout `HUDSystem` non bloquant — placeholder accepté pour la slice (à câbler quand on touchera le HUD).
