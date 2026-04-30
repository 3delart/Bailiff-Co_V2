// ============================================================
// PlayerAnimator.cs — Bailiff & Co  V2
// Pilote l'Animator du personnage depuis l'état du PlayerController.
//
// CHANGEMENTS V2 :
//   - Aucun changement structurel — script déjà propre en V1
//   - Juste adaptation des noms de namespace si nécessaire
//
// Paramètres Animator attendus :
//   - bool  "Walking"   → true si le joueur se déplace
//   - bool  "Crouching" → true si le joueur est accroupi
// ============================================================
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Animator         _animator;
    [SerializeField] private PlayerController _controller;

    [Header("Vitesse d'animation")]
    [Tooltip("Multiplie la vitesse de tous les clips. Monte la valeur si les anims sont au ralenti (ex: 2.5 pour des clips 24fps).")]
    [SerializeField] private float _vitesseAnimation = 2.5f;

    // Noms des paramètres dans l'Animator (doit correspondre exactement)
    private static readonly int WALKING   = Animator.StringToHash("Walking");
    private static readonly int CROUCHING = Animator.StringToHash("Crouching");

    private void Start()
    {
        _animator.speed = _vitesseAnimation;
    }

    private void Update()
    {
        bool enMouvement = _controller.EstEnMouvement;
        bool accroupi    = _controller.EstAccroupi;

        _animator.SetBool(WALKING,   enMouvement);
        _animator.SetBool(CROUCHING, accroupi);
    }
}