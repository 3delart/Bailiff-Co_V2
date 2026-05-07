// ============================================================
// InventaireWheel.cs — Bailiff & Co  [v2]
// Roue d'inventaire : [Tab] maintenu = visible, relâché = fermé.
// Vit dans UI_Persistent — une seule instance pour tout le jeu.
// panelType = Overlay dans l'Inspector.
// Activée/désactivée par UIManager selon le contexte (Hub/Mission).
//
// CHANGEMENTS v1 → v2 :
//   - Migré dans UI_Persistent (Features/Inventory/ côté script).
//   - FindObjectOfType supprimés → SetRefs() pour injection runtime.
//   - OnEnable/OnDisable : override + base. pour combiner
//     RegisterPanel/UnregisterPanel ET logique roue.
//   - Suppression de _panelInventaireWheel (référence fantôme).
//   - Suppression gestion curseur (déléguée à UIManager).
//
// 9 SLOTS :
//   Centre          → Mains libres (objet porté ou vide)
//   Haut            → Outil permanent 1
//   Droite          → Outil permanent 2
//   Bas             → Outil permanent 3
//   Gauche          → Outil permanent 4
//   Haut-Droite     → Consommable 1
//   Bas-Droite      → Consommable 2
//   Bas-Gauche      → Consommable 3
//   Haut-Gauche     → Consommable 4
// ============================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// COMPOSANT SLOT — à mettre sur chaque quartier de la roue
// ============================================================
[System.Serializable]
public class WheelSlot
{
    [Tooltip("GameObject racine du slot (quartier SVG ou Image)")]
    public GameObject Root;

    [Tooltip("Icône de l'outil ou du consommable")]
    public Image Icone;

    [Tooltip("Nom court affiché sous l'icône")]
    public TextMeshProUGUI Label;

    [Tooltip("Quantité (consommables uniquement)")]
    public TextMeshProUGUI Quantite;

    [HideInInspector] public OutilData OutilAssocie;
    [HideInInspector] public string    ConsommableAssocie;
    [HideInInspector] public bool      EstSlotMains;
}

// ============================================================
// ROUE PRINCIPALE
// ============================================================
public class InventaireWheel : UIPanel
{
    // ================================================================
    // CONSTANTES — index des slots dans le tableau
    // ================================================================
    private const int SLOT_CENTRE      = 0;
    private const int SLOT_HAUT        = 1;
    private const int SLOT_DROIT       = 2;
    private const int SLOT_BAS         = 3;
    private const int SLOT_GAUCHE      = 4;
    private const int SLOT_HAUT_DROIT  = 5;
    private const int SLOT_BAS_DROIT   = 6;
    private const int SLOT_BAS_GAUCHE  = 7;
    private const int SLOT_HAUT_GAUCHE = 8;

    private static readonly float[] ANGLES_SLOTS = {
        -999f,  // Centre → détection par distance
         90f,   // Haut
          0f,   // Droit
        270f,   // Bas
        180f,   // Gauche
         45f,   // Haut-Droit
        315f,   // Bas-Droit
        225f,   // Bas-Gauche
        135f    // Haut-Gauche
    };

    private const float RAYON_MORT = 40f;

    // ================================================================
    // SÉRIALISATION
    // ================================================================

    [Header("9 Slots (ordre : Centre, Haut, Droit, Bas, Gauche, HautDroit, BasDroit, BasGauche, HautGauche)")]
    [SerializeField] private WheelSlot[] _slots = new WheelSlot[9];

    [Header("Couleurs")]
    [SerializeField] private Color _couleurActif   = new Color(0.94f, 0.91f, 0.48f, 1f);
    [SerializeField] private Color _couleurInactif = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    [SerializeField] private Color _couleurVide    = new Color(0.10f, 0.10f, 0.10f, 0.60f);

    [Header("Références — assigner dans l'Inspector ou via SetRefs() au runtime")]
    [SerializeField] private InventaireSystem _inventaire;
    [SerializeField] private PlayerCarry      _carry;
    [SerializeField] private RectTransform    _wheelRoot;

    // ================================================================
    // ÉTAT
    // ================================================================

    private int     _slotSelectionne        = SLOT_CENTRE;
    private int     _slotActif              = SLOT_CENTRE;
    private Vector2 _centreEcran;
    private Vector2 _positionSourisVirtuelle;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Start()
    {
        if (_inventaire == null)
            Debug.LogWarning("[InventaireWheel] InventaireSystem non assigné. " +
                             "Assignez-le dans l'Inspector ou appelez SetRefs().");
        if (_carry == null)
            Debug.LogWarning("[InventaireWheel] PlayerCarry non assigné. " +
                             "Assignez-le dans l'Inspector ou appelez SetRefs().");

        if (_slots[SLOT_CENTRE] != null)
            _slots[SLOT_CENTRE].EstSlotMains = true;

        RafraichirSlots();
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // RegisterPanel

        if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false); // caché jusqu'au Tab
        RafraichirSlots();

        _centreEcran             = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _positionSourisVirtuelle = _centreEcran;
    }

    protected override void OnDisable()
    {
        if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false);
        base.OnDisable(); // UnregisterPanel
    }

    public override void Fermer()
    {
        if (_wheelRoot != null) _wheelRoot.gameObject.SetActive(false);
        base.Fermer(); // SetActive(false) sur le panel GO
    }

    private void Update()
    {
        KeyCode toucheInv = OptionsManager.Instance != null
            ? OptionsManager.Instance.GetTouche(ActionJeu.Inventaire)
            : KeyCode.Tab;

        bool wheelVisible = _wheelRoot != null && _wheelRoot.gameObject.activeSelf;

        if (Input.GetKeyDown(toucheInv) && !wheelVisible)
        {
            _wheelRoot?.gameObject.SetActive(true);
            RafraichirSlots();
            _centreEcran             = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _positionSourisVirtuelle = _centreEcran;
        }

        if (Input.GetKeyUp(toucheInv) && wheelVisible)
        {
            SelectionnerSlot(_slotSelectionne);
            _wheelRoot?.gameObject.SetActive(false);
        }

        if (_wheelRoot != null && _wheelRoot.gameObject.activeSelf)
            MettreAJourSelection();
    }

    // ================================================================
    // API PUBLIQUE — injection de références au runtime
    // ================================================================

    /// <summary>
    /// Injecte les références runtime depuis UIManager ou MissionBuilder
    /// après le spawn du joueur.
    /// </summary>
    public void SetRefs(InventaireSystem inventaire, PlayerCarry carry)
    {
        _inventaire = inventaire;
        _carry      = carry;
        RafraichirSlots();
    }

    // ================================================================
    // SÉLECTION PAR SOURIS VIRTUELLE
    // ================================================================

    private void MettreAJourSelection()
    {
        float dx = Input.GetAxis("Mouse X") * 10f;
        float dy = Input.GetAxis("Mouse Y") * 10f;

        _positionSourisVirtuelle.x = Mathf.Clamp(
            _positionSourisVirtuelle.x + dx,
            _centreEcran.x - 200f, _centreEcran.x + 200f);
        _positionSourisVirtuelle.y = Mathf.Clamp(
            _positionSourisVirtuelle.y + dy,
            _centreEcran.y - 200f, _centreEcran.y + 200f);

        Vector2 delta = _positionSourisVirtuelle - _centreEcran;

        int nouveau;
        if (delta.magnitude < RAYON_MORT)
        {
            nouveau = SLOT_CENTRE;
        }
        else
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            nouveau = TrouverSlotPlusProche(angle);
        }

        if (nouveau != _slotSelectionne)
        {
            _slotSelectionne = nouveau;
            MettreAJourVisuels();
        }
    }

    private int TrouverSlotPlusProche(float angle)
    {
        int   meilleur  = SLOT_HAUT;
        float minDelta  = float.MaxValue;

        for (int i = 1; i < _slots.Length; i++)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, ANGLES_SLOTS[i]));
            if (diff < minDelta)
            {
                minDelta = diff;
                meilleur = i;
            }
        }
        return meilleur;
    }

    // ================================================================
    // SÉLECTION EFFECTIVE (au relâchement de Tab)
    // ================================================================

    private void SelectionnerSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return;
        var slot = _slots[index];
        if (slot == null) return;

        if (slot.EstSlotMains)
        {
            _slotActif = SLOT_CENTRE;
            Debug.Log("[InventaireWheel] Mains libres sélectionnées");
            return;
        }

        if (slot.OutilAssocie != null)
        {
            _slotActif = index;
            Debug.Log($"[InventaireWheel] Outil sélectionné : {slot.OutilAssocie.ToolName}");
            return;
        }

        if (!string.IsNullOrEmpty(slot.ConsommableAssocie))
        {
            if (_inventaire != null && _inventaire.UtiliserConsommable(slot.ConsommableAssocie))
            {
                Debug.Log($"[InventaireWheel] Consommable utilisé : {slot.ConsommableAssocie}");
                RafraichirSlots();
            }
        }
    }

    // ================================================================
    // RAFRAÎCHISSEMENT DES DONNÉES
    // ================================================================

    private void RafraichirSlots()
    {
        if (_inventaire == null) return;

        SetSlotMains(_slots[SLOT_CENTRE]);

        var outils = new System.Collections.Generic.List<OutilData>(_inventaire.Outils.Keys);
        int[] slotIndexOutils = { SLOT_HAUT, SLOT_DROIT, SLOT_BAS, SLOT_GAUCHE };

        for (int i = 0; i < slotIndexOutils.Length; i++)
        {
            var slot = _slots[slotIndexOutils[i]];
            if (slot == null) continue;
            if (i < outils.Count) SetSlotOutil(slot, outils[i]);
            else                  SetSlotVide(slot);
        }

        var cles = new System.Collections.Generic.List<string>(_inventaire.Consommables.Keys);
        int[] slotIndexConsos = { SLOT_HAUT_DROIT, SLOT_BAS_DROIT, SLOT_BAS_GAUCHE, SLOT_HAUT_GAUCHE };

        for (int i = 0; i < slotIndexConsos.Length; i++)
        {
            var slot = _slots[slotIndexConsos[i]];
            if (slot == null) continue;
            if (i < cles.Count) SetSlotConsommable(slot, cles[i], _inventaire.QuantiteConsommable(cles[i]));
            else                SetSlotVide(slot);
        }

        MettreAJourVisuels();
    }

    // ================================================================
    // REMPLISSAGE DES SLOTS
    // ================================================================

    private void SetSlotMains(WheelSlot slot)
    {
        if (slot == null) return;
        slot.EstSlotMains       = true;
        slot.OutilAssocie       = null;
        slot.ConsommableAssocie = "";

        bool objetPorte = _carry != null && _carry.EstEnTrain;
        if (slot.Label    != null)
            slot.Label.text = objetPorte
                ? _carry.ObjetEnMain?.Data?.ObjectName ?? "Objet"
                : "Mains libres";
        if (slot.Quantite != null)
            slot.Quantite.gameObject.SetActive(false);
    }

    private void SetSlotOutil(WheelSlot slot, OutilData outil)
    {
        if (slot == null || outil == null) return;
        slot.OutilAssocie       = outil;
        slot.ConsommableAssocie = "";
        slot.EstSlotMains       = false;

        if (slot.Label  != null)                         slot.Label.text   = outil.ToolName;
        if (slot.Icone  != null && outil.UIIcon != null) slot.Icone.sprite = outil.UIIcon;
        if (slot.Quantite != null)                       slot.Quantite.gameObject.SetActive(false);
    }

    private void SetSlotConsommable(WheelSlot slot, string type, int quantite)
    {
        if (slot == null) return;
        slot.ConsommableAssocie = type;
        slot.OutilAssocie       = null;
        slot.EstSlotMains       = false;

        if (slot.Label    != null) slot.Label.text = type;
        if (slot.Quantite != null)
        {
            slot.Quantite.gameObject.SetActive(true);
            slot.Quantite.text = quantite > 0 ? $"x{quantite}" : "—";
        }
    }

    private void SetSlotVide(WheelSlot slot)
    {
        if (slot == null) return;
        slot.OutilAssocie       = null;
        slot.ConsommableAssocie = "";
        slot.EstSlotMains       = false;

        if (slot.Label    != null) slot.Label.text  = "—";
        if (slot.Icone    != null) slot.Icone.sprite = null;
        if (slot.Quantite != null) slot.Quantite.gameObject.SetActive(false);
    }

    // ================================================================
    // VISUELS — surbrillance du slot sélectionné
    // ================================================================

    private void MettreAJourVisuels()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot?.Root == null) continue;

            var img = slot.Root.GetComponent<Image>();
            if (img == null) continue;

            bool estActif       = (i == _slotActif);
            bool estSelectionne = (i == _slotSelectionne);
            bool estVide        = slot.OutilAssocie == null
                               && string.IsNullOrEmpty(slot.ConsommableAssocie)
                               && !slot.EstSlotMains;

            img.color = estSelectionne ? _couleurActif
                      : estVide        ? _couleurVide
                                       : _couleurInactif;

            slot.Root.transform.localScale = estActif
                ? Vector3.one * 1.08f
                : Vector3.one;
        }
    }
}
