// ============================================================
// PlayerAnimator.cs — Bailiff & Co  V3
// Pilote l'Animator Humanoid du joueur depuis l'état du PlayerController,
// et expose l'API de la COUCHE HAUT (actions : tenue/usage outil, port,
// ramasser/poser/lancer, portes/tiroirs).
//
// ── CONTRAT ANIMATOR (à reproduire dans PlayerAnimator.controller) ──
//   Couche BASE (locomotion) :
//     float  Speed     — vitesse horizontale (m/s, magnitude) → blend 1D (crouch/prone)
//     float  MoveX     — vitesse latérale locale (m/s, strafe droite+) → blend 2D debout
//     float  MoveY     — vitesse avant/arrière locale (m/s, avant+)    → blend 2D debout
//     int    Posture   — 0 Stand · 1 Crouch · 2 Prone
//     bool   Grounded  — au sol
//     trig   Jump      — saut
//   Couche HAUT (Avatar Mask UpperBody, par-dessus la base) :
//     int    HoldMode  — 0 rien · 1 tenue 1 main · 2 tenue 2 mains · 3 port objet
//     bool   Using     — usage outil CONTINU (canalisation : boucle l'état Use)
//     trig   UseOnce   — usage outil PONCTUEL (pose conso, lancer) → revient à la tenue
//     trig   Grab/Place/Throw/Door/Drawer — actions one-shot (objet porté / monde)
//   AnimatorOverrideController (placeholders assignés dans l'Inspector) :
//     _holdPlaceholder — le clip mis dans l'état Hold ; remplacé par item.HoldPose
//     _usePlaceholder  — le clip mis dans l'état Use ;  remplacé par item.UseAnimation
// ============================================================
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Animator         _animator;
    [SerializeField] private PlayerController _controller;

    [Header("Lissage")]
    [Tooltip("Temps d'amortissement du paramètre Speed (s) — adoucit les transitions idle/marche/course. Monte la valeur pour plus de fondu.")]
    [SerializeField] private float _speedDampTime = 0.12f;
    [Tooltip("Délai (s) avant de considérer le joueur 'en l'air' pour l'anim — évite la chute sur les micro-décrochages (marches, objets).")]
    [SerializeField] private float _fallGraceTime = 0.25f;

    private float _airborneTimer;

    // ── Paramètres BASE ──
    private static readonly int SPEED    = Animator.StringToHash("Speed");
    private static readonly int MOVEX    = Animator.StringToHash("MoveX");
    private static readonly int MOVEY    = Animator.StringToHash("MoveY");
    private static readonly int POSTURE  = Animator.StringToHash("Posture");
    private static readonly int GROUNDED = Animator.StringToHash("Grounded");
    private static readonly int JUMP     = Animator.StringToHash("Jump");

    // ── Paramètres HAUT ──
    private static readonly int HOLDMODE = Animator.StringToHash("HoldMode");
    private static readonly int USING    = Animator.StringToHash("Using");
    private static readonly int USE_ONCE = Animator.StringToHash("UseOnce");
    private static readonly int GRAB     = Animator.StringToHash("Grab");
    private static readonly int PLACE    = Animator.StringToHash("Place");
    private static readonly int THROW    = Animator.StringToHash("Throw");
    private static readonly int DOOR     = Animator.StringToHash("Door");
    private static readonly int DRAWER   = Animator.StringToHash("Drawer");

    [Header("Placeholders couche Haut")]
    [Tooltip("Le clip mis dans l'état Hold de la couche Haut — sera remplacé par item.HoldPose au runtime.")]
    [SerializeField] private AnimationClip _holdPlaceholder;
    [Tooltip("Le clip mis dans l'état Use de la couche Haut — sera remplacé par item.UseAnimation au runtime.")]
    [SerializeField] private AnimationClip _usePlaceholder;

    private AnimatorOverrideController _override;

    private void Awake()
    {
        if (_animator   == null) _animator   = GetComponentInChildren<Animator>();
        if (_controller == null) _controller = GetComponentInParent<PlayerController>();

        // Enveloppe le controller dans un AnimatorOverrideController pour pouvoir
        // remplacer les clips d'usage/tenue par item au runtime.
        if (_animator != null && _animator.runtimeAnimatorController != null
            && !(_animator.runtimeAnimatorController is AnimatorOverrideController))
        {
            _override = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _override;
        }
        else
        {
            _override = _animator?.runtimeAnimatorController as AnimatorOverrideController;
        }
    }

    private void OnEnable()
    {
        if (_controller != null) _controller.OnSaut += DeclencherSaut;
    }

    private void OnDisable()
    {
        if (_controller != null) _controller.OnSaut -= DeclencherSaut;
    }

    private void DeclencherSaut()
    {
        if (_animator != null) _animator.SetTrigger(JUMP);
    }

    // ================================================================
    // COUCHE BASE — locomotion (chaque frame)
    // ================================================================

    private void Update()
    {
        if (_animator == null || _controller == null) return;

        bool inputActif = GameManager.Instance == null || GameManager.Instance.InputJoueurActif;
        float   speed = inputActif ? _controller.VitessePlanaire : 0f;
        Vector3 local = inputActif ? _controller.VitesseLocale   : Vector3.zero;

        // Amorti → montée/descente progressive → blends fluides.
        _animator.SetFloat(SPEED, speed,   _speedDampTime, Time.deltaTime);
        _animator.SetFloat(MOVEX, local.x, _speedDampTime, Time.deltaTime);
        _animator.SetFloat(MOVEY, local.z, _speedDampTime, Time.deltaTime);
        _animator.SetInteger(POSTURE, (int)_controller.Posture);

        // Grounded débouncé : reste "au sol" pendant _fallGraceTime après avoir quitté le sol
        // → pas de chute sur les micro-décrochages (marches, objets).
        bool grounded = _controller.EstAuSol;
        _airborneTimer = grounded ? 0f : _airborneTimer + Time.deltaTime;
        _animator.SetBool(GROUNDED, grounded || _airborneTimer < _fallGraceTime);
    }

    // ================================================================
    // COUCHE HAUT — API actions
    // ================================================================

    /// <summary>Tient un outil/conso en main : pose générique selon HandCount (+ override optionnel),
    /// et arme le clip d'usage.</summary>
    public void TenirOutil(HandCount mains, AnimationClip use, AnimationClip holdOverride)
    {
        if (_animator == null) return;
        SetClip(_usePlaceholder,  use);
        SetClip(_holdPlaceholder, holdOverride);
        _animator.SetBool(USING, false);
        _animator.SetInteger(HOLDMODE, mains == HandCount.TwoHand ? 2 : 1);
    }

    /// <summary>Range les mains (rien en main).</summary>
    public void RangerMains()
    {
        if (_animator == null) return;
        _animator.SetBool(USING, false);
        _animator.SetInteger(HOLDMODE, 0);
        SetClip(_usePlaceholder,  null);
        SetClip(_holdPlaceholder, null);
    }

    /// <summary>Début d'usage (canalisation) : boucle l'anim d'usage sur le haut du corps.</summary>
    public void DebutUsage() { if (_animator != null) _animator.SetBool(USING, true); }

    /// <summary>Fin/annulation d'usage : retour à la pose de tenue.</summary>
    public void FinUsage()   { if (_animator != null) _animator.SetBool(USING, false); }

    /// <summary>Usage ponctuel (pose conso, lancer) : joue le clip d'usage une fois, revient à la tenue.</summary>
    public void JouerUsageOneShot() { if (_animator != null) _animator.SetTrigger(USE_ONCE); }

    public void JouerRamasser() { if (_animator == null) return; _animator.SetInteger(HOLDMODE, 3); _animator.SetTrigger(GRAB); }
    public void JouerPoser()    { if (_animator == null) return; _animator.SetTrigger(PLACE);  _animator.SetInteger(HOLDMODE, 0); }
    public void JouerLancer()   { if (_animator == null) return; _animator.SetTrigger(THROW);  _animator.SetInteger(HOLDMODE, 0); }
    public void JouerPorte()    { if (_animator != null) _animator.SetTrigger(DOOR); }
    public void JouerTiroir()   { if (_animator != null) _animator.SetTrigger(DRAWER); }

    // ================================================================
    // HELPERS
    // ================================================================

    /// <summary>Remplace le clip placeholder par `clip`. clip=null → restaure le placeholder.</summary>
    private void SetClip(AnimationClip placeholder, AnimationClip clip)
    {
        if (_override == null || placeholder == null) return;
        _override[placeholder] = clip != null ? clip : placeholder;
    }
}
