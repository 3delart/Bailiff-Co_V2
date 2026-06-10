// ============================================================
// Player.cs — Bailiff & Co
// Façade / hub central du joueur. À mettre sur le PlayerRoot.
//
// RÔLE :
//   1. Auto-setup : ajouter ce composant tire automatiquement TOUS les
//      composants joueur requis (via [RequireComponent]).
//   2. Accès centralisés : un seul point pour retrouver les sous-systèmes
//      (Controller, Carry, Interactor, ToolUser, Animator, Inventaire…).
//   3. Références aux enfants (caméra, os de main) assignées UNE fois ici,
//      au lieu d'être éparpillées sur chaque script.
//
// Note coop : PAS un singleton — il peut y avoir plusieurs Player.
// ============================================================
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerNoiseEmitter))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCarry))]
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(PlayerToolUser))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(InventaireSystem))]
public class Player : MonoBehaviour
{
    [Header("Enfants (assigner une fois)")]
    [Tooltip("Caméra première personne (enfant).")]
    [SerializeField] private Transform _camera;
    [Tooltip("Os de la main DROITE du rig (IK / montage outil).")]
    [SerializeField] private Transform _handBone;
    [Tooltip("Os de la main GAUCHE du rig (IK 2 mains).")]
    [SerializeField] private Transform _leftHandBone;
    [Tooltip("Ancre de tenue — à parenter sous un os du TORSE (Spine/Chest) pour suivre le balancement du corps.")]
    [SerializeField] private Transform _pointDePort;

    // Sous-systèmes (résolus au réveil — racine ou enfants)
    public CharacterController   CharacterController { get; private set; }
    public PlayerController      Controller          { get; private set; }
    public PlayerCarry           Carry               { get; private set; }
    public PlayerInteractor      Interactor          { get; private set; }
    public PlayerToolUser        ToolUser            { get; private set; }
    public PlayerAnimator        Animator            { get; private set; }
    public PlayerNoiseEmitter    NoiseEmitter        { get; private set; }
    public InventaireSystem      Inventaire          { get; private set; }

    public Transform Camera       => _camera;
    public Transform HandBone     => _handBone;
    public Transform LeftHandBone => _leftHandBone;
    public Transform PointDePort  => _pointDePort;

    private void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        Controller          = GetComponent<PlayerController>();
        Carry               = GetComponent<PlayerCarry>();
        Interactor          = GetComponent<PlayerInteractor>();
        ToolUser            = GetComponent<PlayerToolUser>();
        NoiseEmitter        = GetComponent<PlayerNoiseEmitter>();
        Animator            = GetComponentInChildren<PlayerAnimator>();
        Inventaire          = GetComponentInChildren<InventaireSystem>();
    }
}
