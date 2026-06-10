// ============================================================
// ChannelBehaviour.cs — Bailiff & Co
// Outils permanents à CANALISATION : maintien clic gauche → jauge →
// effet à 100%. Durée = ctx.Stats.ActionDuration (par niveau).
// Pendant la canalisation, l'outil en main "bouge" (tween oscillation).
// Perdre la cible / relâcher = annule et reset la jauge.
//
// Sous-classes (une par effet) : Crowbar, Lockpick, ScannerPhone.
// ============================================================
using UnityEngine;

public abstract class ChannelBehaviour : ToolBehaviour
{
    private bool  _channeling;
    private float _time;
    private float _duration;

    // tween de l'outil en main
    private Vector3    _baseLocalPos;
    private Quaternion _baseLocalRot;
    private bool       _baseStored;

    private const float TWEEN_SPEED  = 22f;  // rad-ish (vitesse oscillation)
    private const float TWEEN_AMPLI  = 9f;    // degrés

    // ---- API sous-classes ----
    /// <summary>Tente d'acquérir une cible valide sous le raycast. Retourne true si valide.</summary>
    protected abstract bool TryAcquireTarget(RaycastHit hit);
    /// <summary>La cible visée est-elle toujours celle acquise ?</summary>
    protected abstract bool TargetStillValid(RaycastHit hit);
    /// <summary>Effet appliqué à 100% de canalisation.</summary>
    protected abstract void ApplyEffect();

    public override void OnPrimaryHold(float dt)
    {
        if (!Ctx.Raycast(out var hit)) { Annuler(); return; }

        if (!_channeling)
        {
            if (!TryAcquireTarget(hit)) return;          // pas de cible valide → rien
            _duration   = Ctx.Stats.ActionDuration > 0.01f ? Ctx.Stats.ActionDuration : 2f;
            _time       = 0f;
            _channeling = true;
            StoreBase();
            Ctx.Animator?.DebutUsage();                  // anim d'usage (boucle haut du corps)
        }
        else if (!TargetStillValid(hit))
        {
            Annuler();
            return;
        }

        _time += dt;
        float p = Mathf.Clamp01(_time / _duration);
        EventBus<OnToolChannelProgress>.Raise(new OnToolChannelProgress { Progress01 = p, Active = true });
        Tween(p);

        if (p >= 1f)
        {
            // Jet de réussite (0 = mécanique désactivée → toujours réussir).
            float chance = Ctx.Stats.SuccessChance <= 0f ? 1f : Ctx.Stats.SuccessChance;
            if (Random.value <= chance) ApplyEffect();
            else                        OnChannelFail();
            Annuler();
        }
    }

    public override void OnPrimaryUp() => Annuler();
    public override void OnUnequip()   => Annuler();

    /// <summary>Échec du jet de réussite. Par défaut : petit bruit de feedback. Surchargé par outil.</summary>
    protected virtual void OnChannelFail() => EmitNoise(NoiseLevel.Light, 4f);

    protected void EmitNoise(NoiseLevel level, float range)
    {
        if (Ctx?.User == null) return;
        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = Ctx.User.transform.position,
            Range    = range,
            Level    = level,
            Source   = Ctx.User.gameObject
        });
    }

    private void Annuler()
    {
        if (!_channeling) return;
        _channeling = false;
        _time       = 0f;
        EventBus<OnToolChannelProgress>.Raise(new OnToolChannelProgress { Progress01 = 0f, Active = false });
        Ctx.Animator?.FinUsage();
        RestoreBase();
    }

    // ---- Tween de l'outil en main ----
    private void StoreBase()
    {
        if (Ctx.ModeleEnMain == null) return;
        _baseLocalPos = Ctx.ModeleEnMain.localPosition;
        _baseLocalRot = Ctx.ModeleEnMain.localRotation;
        _baseStored   = true;
    }

    private void Tween(float p)
    {
        if (Ctx.ModeleEnMain == null || !_baseStored) return;
        float osc = Mathf.Sin(Time.time * TWEEN_SPEED) * TWEEN_AMPLI;
        Ctx.ModeleEnMain.localRotation = _baseLocalRot * Quaternion.Euler(osc, 0f, 0f);
    }

    private void RestoreBase()
    {
        if (Ctx.ModeleEnMain == null || !_baseStored) return;
        Ctx.ModeleEnMain.localPosition = _baseLocalPos;
        Ctx.ModeleEnMain.localRotation = _baseLocalRot;
    }
}

// ============================================================
// CROWBAR — pied-de-biche : force une ouverture verrouillée (BRUYANT)
// ============================================================
public sealed class CrowbarBehaviour : ChannelBehaviour
{
    private OpenableInteractable _target;

    protected override bool TryAcquireTarget(RaycastHit hit)
    {
        var op = hit.collider.GetComponentInParent<OpenableInteractable>();
        if (op != null && op.IsLocked) { _target = op; return true; }
        return false;
    }

    protected override bool TargetStillValid(RaycastHit hit)
        => _target != null && hit.collider.GetComponentInParent<OpenableInteractable>() == _target;

    protected override void ApplyEffect()
    {
        if (_target != null) _target.ForceOpen();   // émet bruit Très Fort
        _target = null;
    }

    // Échec = le pied-de-biche ripe : bruit fort, la serrure tient.
    protected override void OnChannelFail() => EmitNoise(NoiseLevel.Loud, 10f);
}

// ============================================================
// LOCKPICK — kit de crochetage : ouverture SILENCIEUSE
// ============================================================
public sealed class LockpickBehaviour : ChannelBehaviour
{
    private OpenableInteractable _target;

    protected override bool TryAcquireTarget(RaycastHit hit)
    {
        var op = hit.collider.GetComponentInParent<OpenableInteractable>();
        if (op != null && op.IsLocked) { _target = op; return true; }
        return false;
    }

    protected override bool TargetStillValid(RaycastHit hit)
        => _target != null && hit.collider.GetComponentInParent<OpenableInteractable>() == _target;

    protected override void ApplyEffect()
    {
        if (_target != null)
        {
            _target.Unlock();                          // silencieux
            _target.Interact(Ctx.User.gameObject);     // ouvre dans la foulée
        }
        _target = null;
    }

    // Échec = le crochet dérape : léger bruit, la serrure tient.
    protected override void OnChannelFail() => EmitNoise(NoiseLevel.Light, 3f);
}

// ============================================================
// SCANNER PHONE — téléphone d'huissier : révèle nom + valeur
// ============================================================
public sealed class ScannerPhoneBehaviour : ChannelBehaviour
{
    private ValueObject _target;

    protected override bool TryAcquireTarget(RaycastHit hit)
    {
        var vo = hit.collider.GetComponentInParent<ValueObject>();
        if (vo != null) { _target = vo; return true; }
        return false;
    }

    protected override bool TargetStillValid(RaycastHit hit)
        => _target != null && hit.collider.GetComponentInParent<ValueObject>() == _target;

    protected override void ApplyEffect()
    {
        if (_target != null) _target.Scan();
        _target = null;
    }
}
