// ============================================================
// PlayerController.cs — Bailiff & Co  V2
// Déplacements, sprint, accroupissement, allongement, saut.
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
    [SerializeField] private PauseMenu        _pauseMenu;

    private CharacterController _cc;
    private PlayerNoiseEmitter  _noise;

    private Vector3 _velociteXZ    = Vector3.zero;
    private float   _velociteY     = 0f;
    private float   _rotationX     = 0f;
    private bool    _estAccroupi   = false;
    private bool    _estAllonge    = false;
    private bool    _estAuSol      = false;
    private float   _dernierSaut   = -999f;
    private string  _tagSol        = "";

    private const float COYOTE_TIME = 0.15f;
    private float _dernierTempsAuSol = 0f;

    private bool _uiBloquante => UIManager.Instance != null
        ? UIManager.Instance.IsInputBlocked
        : false;

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

    private void Update()
    {
        if (_pauseMenu != null && _pauseMenu.EstOuvert) return;

        GererCurseur();
        DetecterSol();
        GererGravite();

        if (_uiBloquante)
        {
            // UI ouverte : on applique uniquement la gravité et la hauteur
            // pour éviter que le CharacterController se désynchronise
            AdapterHauteur();
            AdapterCamera();
            return;
        }

        GererCamera();
        GererPosture();
        GererMouvement();
        GererSaut();
        AdapterHauteur();
        AdapterCamera();
    }

    // ================================================================
    // CURSEUR
    // ================================================================

    private void GererCurseur()
    {
        if (_uiBloquante)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    // ================================================================
    // VÉRIFICATION ESPACE LIBRE AU-DESSUS
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
        float basCC   = _cc.center.y - _cc.height * 0.5f;
        Vector3 bas   = transform.position + Vector3.up * (basCC + 0.05f);
        float dist    = 0.35f;

        bool c = Physics.Raycast(bas, Vector3.down, out RaycastHit hit, dist,
                     Physics.AllLayers, QueryTriggerInteraction.Ignore);
        bool a = Physics.Raycast(bas + transform.forward  * 0.2f, Vector3.down,
                     dist, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        bool b = Physics.Raycast(bas - transform.forward  * 0.2f, Vector3.down,
                     dist, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        _estAuSol = _cc.isGrounded || c || a || b;

        if (_estAuSol)
        {
            _dernierTempsAuSol = Time.time;
            if (hit.collider != null) _tagSol = hit.collider.tag;
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

        float cibleY = _estAllonge  ? _config.CameraHeightProne
                     : _estAccroupi ? _config.CameraHeightCrouch
                     :                _config.CameraHeightNormal;

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
            if (_estAllonge)
            {
                if (EspaceLibrePour(_config.HeightCrouch))
                { _estAllonge = false; _estAccroupi = true; }
            }
            else if (_estAccroupi)
            {
                if (EspaceLibrePour(_config.HeightNormal))
                    _estAccroupi = false;
            }
            else
            {
                _estAccroupi = true;
            }
        }

        if (Appui(ActionJeu.Allonge))
        {
            if (_estAllonge)
            {
                if (EspaceLibrePour(_config.HeightNormal))
                { _estAllonge = false; _estAccroupi = false; }
                else if (EspaceLibrePour(_config.HeightCrouch))
                { _estAllonge = false; _estAccroupi = true; }
            }
            else
            {
                _estAccroupi = false;
                _estAllonge  = true;
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
            bool sprint = Maintenu(ActionJeu.Sprint) && !_estAccroupi && !_estAllonge;

            float vitesseBase = _estAllonge  ? _config.ProneSpeed
                              : _estAccroupi ? _config.CrouchSpeed
                              : sprint       ? _config.SprintSpeed
                              :                _config.NormalSpeed;

            float multiMeuble = _interactor != null ? _interactor.MultiplicateurVitesseMeuble : 1f;
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
            _cc.Move(_velociteXZ * Time.deltaTime);

            if (dir.magnitude > 0.1f)
                EmettreBruitDeplacement(sprint);
        }
        else
        {
            _cc.Move(_velociteXZ * Time.deltaTime);
        }
    }

    private void EmettreBruitDeplacement(bool sprint)
    {
        if (_estAllonge || _estAccroupi)
        {
            _noise.EmettreBruit(NiveauBruit.Silencieux, 0f);
            return;
        }

        NiveauBruit niveau = sprint ? NiveauBruit.Fort : NiveauBruit.Leger;
        float portee = sprint
            ? _tagSol switch { "Carrelage" => 14f, "Parquet" => 12f, "Moquette" => 6f, _ => 10f }
            : _tagSol switch { "Carrelage" => 7f,  "Parquet" => 5f,  "Moquette" => 2f, _ => 5f  };

        _noise.EmettreBruit(niveau, portee);
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
            && !_estAccroupi
            && !_estAllonge
            && _velociteY <= 0.1f
            && Appui(ActionJeu.Saut))
        {
            _velociteY         = _config.JumpForce;
            _dernierSaut       = Time.time;
            _dernierTempsAuSol = -999f;
            _noise.EmettreBruit(NiveauBruit.Leger, 3f);
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
        _cc.Move(Vector3.up * _velociteY * Time.deltaTime);
    }

    // ================================================================
    // HAUTEUR CHARACTERCONTROLLER
    // ================================================================

    private void AdapterHauteur()
    {
        float cible = _estAllonge  ? _config.HeightProne
                    : _estAccroupi ? _config.HeightCrouch
                    :                _config.HeightNormal;

        _cc.height = Mathf.Lerp(_cc.height, cible,
                                 Time.deltaTime * _config.HeightChangeSpeed);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);
    }

    // ================================================================
    // RACCOURCIS INPUT
    // ================================================================

    private bool Appui(ActionJeu action)
        => OptionsManager.Instance != null
            ? Input.GetKeyDown(OptionsManager.Instance.GetTouche(action))
            : false;

    private bool Maintenu(ActionJeu action)
        => OptionsManager.Instance != null
            ? Input.GetKey(OptionsManager.Instance.GetTouche(action))
            : false;

    // ================================================================
    // PROPRIÉTÉS
    // ================================================================

    public bool EstAccroupi    => _estAccroupi;
    public bool EstAllonge     => _estAllonge;
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
