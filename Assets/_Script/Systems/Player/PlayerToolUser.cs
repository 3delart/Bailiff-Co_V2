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

    // ---- TAP : ForceDoor + Scan ----
    private void UtiliserTap()
    {
        if (!RaycastCible(out var hit)) return;

        if (_outilActif.EffectType == ToolEffectType.ForceDoor)
        {
            var op = hit.collider.GetComponentInParent<OpenableInteractable>();
            if (op != null && op.IsLocked) op.ForceOpen(); // bruyant
            return;
        }

        // Scanner : révèle l'objet visé (téléphone d'huissier / scanner)
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
            _channelTarget.Unlock();              // silencieux
            _channelTarget.Interact(gameObject);  // ouvre dans la foulée (Closed→Open)
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
        _channeling    = false;
        _channelTarget = null;
        EventBus<OnToolChannelProgress>.Raise(new OnToolChannelProgress { Progress01 = 0f, Active = false });
    }
}
