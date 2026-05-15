// ============================================================
// PlayerController.cs — Bailiff & Co  V2
// ============================================================
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerNoiseEmitter))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PlayerConfigData _config;

    [Header("Caméra")]
    [SerializeField] private Transform _camera;

    [Header("Références injectées")]
    [SerializeField] private PlayerInteractor _interactor;

    [Header("Animation")]
    [SerializeField] private Animator _animator;

    private CharacterController _cc;
    private PlayerNoiseEmitter  _noise;

    private Vector3 _velociteXZ      = Vector3.zero;
    private float   _velociteY       = 0f;
    private float   _rotationX       = 0f;
    private Posture _posture         = Posture.Stand;
    private bool    _estAuSol        = false;
    private float   _dernierSaut     = -999f;
    private string  _tagSol          = "";

    private const float COYOTE_TIME  = 0.15f;
    private float _dernierTempsAuSol = 0f;

    private static readonly Vector3[] GROUND_CHECK_OFFSETS = {
        Vector3.zero,
        Vector3.forward * 0.2f,
        -Vector3.forward * 0.2f,
        Vector3.right * 0.2f,
        -Vector3.right * 0.2f
    };

    // ================================================================
    // ÉTAT INPUT
    // ================================================================

    private bool _inputActif = true;
    private float _escapePressedTime = 0f;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        _cc    = GetComponent<CharacterController>();
        _noise = GetComponent<PlayerNoiseEmitter>();

        if (_config == null)
            Debug.LogError("[PlayerController] PlayerConfigData manquant !");

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        EventBus<OnInputStateChanged>.Subscribe(OnInputStateChanged);
    }

    private void OnDisable()
    {
        EventBus<OnInputStateChanged>.Unsubscribe(OnInputStateChanged);
    }


    private void OnInputStateChanged(OnInputStateChanged e)
    {
        bool wasActif = _inputActif;
        _inputActif = e.Actif;

        if (wasActif && !_inputActif)
        {
            // Input lock: force idle animations
            if (_animator != null)
            {
                _animator.SetBool("Walking", false);
                _animator.SetBool("Crouching", false);
            }
        }
        else if (!wasActif && _inputActif)
        {
            // Input unlock: flush mouse delta + clamp vertical velocity
            ConsumeMouseDelta();
            _velociteY = Mathf.Min(_velociteY, 0f);
        }
    }

private void Update()
{
    // Emergency unstuck: hold Escape 3 seconds
    if (Input.GetKey(KeyCode.Escape))
    {
        _escapePressedTime += Time.deltaTime;
        if (_escapePressedTime >= 3f)
        {
            ForceUnstuck();
            _escapePressedTime = 0f;
        }
    }
    else
    {
        _escapePressedTime = 0f;
    }

    // Physique toujours active (gravité + sol) pour éviter que le joueur flotte
    DetecterSol();

    if (!_inputActif)
    {
        // Freeze déplacements horizontaux
        _velociteXZ = Vector3.zero;

        // Freeze animations en idle
        if (_animator != null)
        {
            _animator.SetBool("Walking", false);
            _animator.SetBool("Crouching", false);
        }

        // Adapter hauteur et caméra même bloqué (transitions propres)
        AdapterHauteur();
        AdapterCamera();
        GererGravite();
        return;
    }

    // Gameplay normal
    GererCamera();
    GererPosture();
    GererMouvement();
    GererSaut();
    GererGravite();
    AdapterHauteur();
    AdapterCamera();
}

    // ================================================================
    // CONSOMMATION DELTA SOURIS
    // Lit et jette les deltas accumulés pendant le blocage pour éviter
    // un saut de caméra brutal au retour du contrôle.
    // ================================================================

    private void ConsumeMouseDelta()
    {
        // Un simple accès suffit à vider le delta interne d'Unity
        Input.GetAxis("Mouse X");
        Input.GetAxis("Mouse Y");
    }

    // ================================================================
    // VÉRIFICATION ESPACE LIBRE
    // ================================================================

    private bool EspaceLibrePour(float hauteurCible)
    {
        float hauteurActuelle = _cc.height;
        float difference      = hauteurCible - hauteurActuelle;
        if (difference <= 0f) return true;

        Vector3 origine = transform.position + Vector3.up * hauteurActuelle;
        float   radius  = _cc.radius * 0.9f;

        bool bloque = Physics.SphereCast(
            origine, radius, Vector3.up, out _, difference,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);

        return !bloque;
    }

    // ================================================================
    // DÉTECTION SOL
    // ================================================================

    private void DetecterSol()
    {
        float basCC = _cc.center.y - _cc.height * 0.5f;
        Vector3 bas = transform.position + Vector3.up * (basCC + 0.05f);
        float dist = 0.35f;

        RaycastHit firstHit = default;
        bool hitAny = _cc.isGrounded;

        for (int i = 0; i < GROUND_CHECK_OFFSETS.Length; i++)
        {
            Vector3 rayOrigin = bas + transform.TransformDirection(GROUND_CHECK_OFFSETS[i]);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, dist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                hitAny = true;
                if (firstHit.collider == null)
                    firstHit = hit;
            }
        }

        _estAuSol = hitAny;
        if (_estAuSol)
        {
            _dernierTempsAuSol = Time.time;
            if (firstHit.collider != null)
                _tagSol = firstHit.collider.tag;
        }
    }

    // ================================================================
    // CAMÉRA
    // ================================================================

    private void GererCamera()
    {
        float sensi = OptionsManager.Instance != null
            ? OptionsManager.Instance.SensibiliteSouris
            : _config.MouseSensitivityFallback;

        bool inverserY = OptionsManager.Instance != null && OptionsManager.Instance.InverserY;

        float mouseX =  Input.GetAxis("Mouse X") * sensi;
        float mouseY =  Input.GetAxis("Mouse Y") * sensi * (inverserY ? -1f : 1f);

        _rotationX -= mouseY;
        _rotationX  = Mathf.Clamp(_rotationX, -_config.VerticalClamp, _config.VerticalClamp);

        if (_camera != null)
            _camera.localRotation = Quaternion.Euler(_rotationX, 0, 0);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void AdapterCamera()
    {
        if (_camera == null) return;

        float cibleY = _posture == Posture.Prone ? _config.CameraHeightProne
                     : _posture == Posture.Crouch ? _config.CameraHeightCrouch
                     : _config.CameraHeightNormal;

        Vector3 pos = _camera.localPosition;
        pos.y = Mathf.Lerp(pos.y, cibleY, Time.deltaTime * _config.CameraLerpSpeed);
        _camera.localPosition = pos;
    }

    // ================================================================
    // POSTURE
    // ================================================================

    private void GererPosture()
    {
        if (Appui(ActionJeu.Accroupi))
        {
            if (_posture == Posture.Prone)
            {
                if (EspaceLibrePour(_config.HeightCrouch))
                    _posture = Posture.Crouch;
            }
            else if (_posture == Posture.Crouch)
            {
                if (EspaceLibrePour(_config.HeightNormal))
                    _posture = Posture.Stand;
            }
            else
            {
                _posture = Posture.Crouch;
            }
        }

        if (Appui(ActionJeu.Allonge))
        {
            if (_posture == Posture.Prone)
            {
                if (EspaceLibrePour(_config.HeightNormal))
                    _posture = Posture.Stand;
                else if (EspaceLibrePour(_config.HeightCrouch))
                    _posture = Posture.Crouch;
            }
            else
            {
                _posture = Posture.Prone;
            }
        }
    }

    // ================================================================
    // MOUVEMENT
    // ================================================================

    private void GererMouvement()
    {
        

        if (_estAuSol)
        {
            bool sprint = Maintenu(ActionJeu.Sprint) && _posture == Posture.Stand;
            float vitesseBase = GetCurrentSpeed(sprint);

            float multiMeuble = _interactor != null ? _interactor.FurnitureSpeedMultiplier : 1f;
            float vitesse     = vitesseBase * multiMeuble;

            float h = 0f;
            float v = 0f;

            if (OptionsManager.Instance != null)
            {
                OptionsData data = OptionsManager.Instance.Data;
                if (Input.GetKey((KeyCode)data.ToucheAvancer)) v += 1f;
                if (Input.GetKey((KeyCode)data.ToucheReculer)) v -= 1f;
                if (Input.GetKey((KeyCode)data.ToucheDroite))  h += 1f;
                if (Input.GetKey((KeyCode)data.ToucheGauche))  h -= 1f;
            }
            else
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }

            Vector3 dir = Vector3.ClampMagnitude(
                transform.right * h + transform.forward * v, 1f);

            _velociteXZ = dir * vitesse;

            Vector3 posAvant = transform.position;
            _cc.Move(_velociteXZ * Time.deltaTime);

            if (_velociteXZ.sqrMagnitude > 0.01f)
                TenterMonteeMarche(posAvant);

            if (dir.magnitude > 0.1f)
                EmitMovementNoise(sprint);
        }
        else
        {
            _cc.Move(_velociteXZ * Time.deltaTime);
        }
    }

    private void EmitMovementNoise(bool sprint)
    {
        if (_posture != Posture.Stand)
        {
            _noise.EmitNoise(NoiseLevel.Silent, 0f);
            return;
        }

        NoiseLevel level = sprint ? NoiseLevel.Loud : NoiseLevel.Light;
        float range = sprint
            ? _tagSol switch { "Carrelage" => 14f, "Parquet" => 12f, "Moquette" => 6f, _ => 10f }
            : _tagSol switch { "Carrelage" => 7f,  "Parquet" => 5f,  "Moquette" => 2f, _ => 5f  };

        _noise.EmitNoise(level, range);
    }

    // ================================================================
    // SAUT
    // ================================================================

    private void GererSaut()
    {
        bool cooldownOk = (Time.time - _dernierSaut) >= _config.JumpCooldown;
        bool coyoteOk   = (Time.time - _dernierTempsAuSol) < COYOTE_TIME;
        bool peutSauter = cooldownOk && (coyoteOk || _estAuSol);

        if (peutSauter
            && _posture == Posture.Stand
            && _velociteY <= 0.1f
            && Appui(ActionJeu.Saut))
        {
            _velociteY         = _config.JumpForce;
            _dernierSaut       = Time.time;
            _dernierTempsAuSol = -999f;
            _noise.EmitNoise(NoiseLevel.Light, 3f);
        }
    }

    // ================================================================
    // GRAVITÉ
    // ================================================================

    private void GererGravite()
    {
        if (_estAuSol && _velociteY < 0)
            _velociteY = -2f;

        _velociteY += _config.Gravity * Time.deltaTime;
        _velociteY = Mathf.Max(_velociteY, -20f);
        _cc.Move(Vector3.up * _velociteY * Time.deltaTime);
    }

    // ================================================================
    // MONTÉE MARCHES (helper — retourne delta hauteur)
    // ================================================================

    // ================================================================
    // TENTATIVE MONTÉE MARCHE
    // ================================================================

    private void TenterMonteeMarche(Vector3 posAvant)
    {
        float attendu = new Vector2(_velociteXZ.x, _velociteXZ.z).magnitude * Time.deltaTime;
        Vector3 deplacement = transform.position - posAvant;
        float reel = new Vector2(deplacement.x, deplacement.z).magnitude;

        if (reel >= attendu * 0.5f) return;

        Vector3 moveDir = new Vector3(_velociteXZ.x, 0f, _velociteXZ.z).normalized;
        float maxStep   = _config.MaxStepHeight;
        float checkDist = _cc.radius + 0.2f;

        float basCC = _cc.center.y - _cc.height * 0.5f;
        Vector3 lowOrigin = transform.position + Vector3.up * (basCC + 0.05f);

        if (!Physics.Raycast(lowOrigin, moveDir, out RaycastHit wallHit, checkDist,
                Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;

        Vector3 probeOrigin = wallHit.point + moveDir * 0.05f + Vector3.up * (maxStep + 0.1f);

        if (!Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit topHit,
                maxStep + 0.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return;

        float delta = topHit.point.y - transform.position.y;
        if (delta <= 0f || delta > maxStep) return;

        _cc.Move(Vector3.up * delta);
    }

    // ================================================================
    // HAUTEUR
    // ================================================================

    private void AdapterHauteur()
    {
        float cible = GetTargetHeight();

        _cc.height = Mathf.Lerp(_cc.height, cible,
                                 Time.deltaTime * _config.HeightChangeSpeed);

        // Snap to target if close enough to prevent lerp entrapment
        if (Mathf.Abs(_cc.height - cible) < 0.01f)
            _cc.height = cible;

        _cc.center = new Vector3(0, _cc.height / 2f, 0);
    }

    // ================================================================
    // INPUT
    // ================================================================

    private float GetCurrentSpeed(bool sprint)
    {
        if (_posture == Posture.Prone) return _config.ProneSpeed;
        if (_posture == Posture.Crouch) return _config.CrouchSpeed;
        if (sprint) return _config.SprintSpeed;
        return _config.NormalSpeed;
    }

    private float GetTargetHeight()
    {
        if (_posture == Posture.Prone) return _config.HeightProne;
        if (_posture == Posture.Crouch) return _config.HeightCrouch;
        return _config.HeightNormal;
    }

    private bool Appui(ActionJeu action)
    {
        if (OptionsManager.Instance != null)
            return Input.GetKeyDown(OptionsManager.Instance.GetTouche(action));

        // Fallback to default AZERTY keys when OptionsManager unavailable
        KeyCode fallback = action switch
        {
            ActionJeu.Accroupi => KeyCode.LeftControl,
            ActionJeu.Allonge  => KeyCode.X,
            ActionJeu.Saut     => KeyCode.Space,
            _                  => KeyCode.None
        };
        return fallback != KeyCode.None && Input.GetKeyDown(fallback);
    }

    private bool Maintenu(ActionJeu action)
    {
        if (OptionsManager.Instance != null)
            return Input.GetKey(OptionsManager.Instance.GetTouche(action));

        // Fallback for held keys
        KeyCode fallback = action switch
        {
            ActionJeu.Sprint => KeyCode.LeftShift,
            _               => KeyCode.None
        };
        return fallback != KeyCode.None && Input.GetKey(fallback);
    }

    // ================================================================
    // EMERGENCY UNSTUCK
    // ================================================================

    private void ForceUnstuck()
    {
        _posture = Posture.Stand;
        _velociteY = 0f;
        _velociteXZ = Vector3.zero;
        _cc.height = _config.HeightNormal;
        _cc.center = new Vector3(0, _cc.height / 2f, 0);
        _inputActif = true;

        if (_animator != null)
        {
            _animator.SetBool("Walking", false);
            _animator.SetBool("Crouching", false);
        }

        // Trigger PlayerInteractor to release any stuck meuble push
        EventBus<OnInputStateChanged>.Raise(new OnInputStateChanged { Actif = false });
        EventBus<OnInputStateChanged>.Raise(new OnInputStateChanged { Actif = true });

        Debug.Log("[PlayerController] Emergency unstuck activated — posture and input reset.");
    }

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    public bool EstAccroupi => _posture == Posture.Crouch;
    public bool EstAllonge  => _posture == Posture.Prone;
    public bool EstAuSol       => _estAuSol;
    public bool EstEnMouvement
    {
        get
        {
            if (OptionsManager.Instance == null) return false;
            OptionsData d = OptionsManager.Instance.Data;
            return Input.GetKey((KeyCode)d.ToucheAvancer)
                || Input.GetKey((KeyCode)d.ToucheReculer)
                || Input.GetKey((KeyCode)d.ToucheGauche)
                || Input.GetKey((KeyCode)d.ToucheDroite);
        }
    }
}
