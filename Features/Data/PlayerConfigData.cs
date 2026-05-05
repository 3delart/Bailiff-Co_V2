// ============================================================
// PlayerConfigData.cs — Bailiff & Co  V2
// ScriptableObject central pour toutes les valeurs numériques
// du joueur : mouvement, carry, interaction.
// Créer via : clic droit → Create → BailiffCo/PlayerConfigData
//
// UTILISATION :
//   - Créer UN SEUL asset PlayerConfig.asset dans Project
//   - Le référencer dans PlayerController, PlayerCarry, PlayerInteractor
//   - Tweaker les valeurs sans recompiler
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/PlayerConfigData")]
public class PlayerConfigData : ScriptableObject
{
    // ── MOVEMENT ─────────────────────────────────────────────
    [Header("Movement Speeds")]
    [Tooltip("Base walking speed (m/s).")]
    public float NormalSpeed         = 4f;
    [Tooltip("Sprint speed (m/s).")]
    public float SprintSpeed         = 7f;
    [Tooltip("Crouched movement speed (m/s).")]
    public float CrouchSpeed         = 2f;
    [Tooltip("Prone movement speed (m/s).")]
    public float ProneSpeed          = 1f;

    [Header("Jump")]
    [Tooltip("Vertical force applied on jump.")]
    public float JumpForce           = 4f;
    [Tooltip("Cooldown (seconds) between two jumps.")]
    public float JumpCooldown        = 0.5f;

    [Header("Gravity")]
    [Tooltip("Gravity acceleration (m/s²). Negative = down.")]
    public float Gravity             = -9.81f;

    // ── CAMERA ───────────────────────────────────────────────
    [Header("Camera")]
    [Tooltip("Fallback mouse sensitivity if OptionsManager is unavailable.")]
    public float MouseSensitivityFallback = 2f;
    [Tooltip("Vertical look angle clamp (degrees).")]
    public float VerticalClamp           = 60f;
    [Tooltip("Normal camera height (local Y).")]
    public float CameraHeightNormal      = 1.8f;
    [Tooltip("Crouched camera height (local Y).")]
    public float CameraHeightCrouch      = 1.25f;
    [Tooltip("Prone camera height (local Y).")]
    public float CameraHeightProne       = 0.2f;
    [Tooltip("Speed of camera height transition (lerp factor).")]
    public float CameraLerpSpeed         = 8f;

    // ── CHARACTER CONTROLLER HEIGHT ──────────────────────────
    [Header("Character Controller Height")]
    [Tooltip("CharacterController height when standing.")]
    public float HeightNormal            = 1.8f;
    [Tooltip("CharacterController height when crouched.")]
    public float HeightCrouch            = 1.2f;
    [Tooltip("CharacterController height when prone.")]
    public float HeightProne             = 0.15f;
    [Tooltip("Speed of height transition (lerp factor).")]
    public float HeightChangeSpeed       = 8f;

    // ── CARRY ────────────────────────────────────────────────
    [Header("Carry & Throw")]
    [Tooltip("Base throw velocity (m/s) for a 1kg object.")]
    public float BaseThrowVelocity       = 10f;
    [Tooltip("Minimum throw velocity (m/s) for very heavy objects.")]
    public float MinThrowVelocity        = 2f;
    [Tooltip("Maximum throw velocity (m/s) for very light objects.")]
    public float MaxThrowVelocity        = 15f;

    // ── INTERACTION ──────────────────────────────────────────
    [Header("Interaction")]
    [Tooltip("Raycast range (metres) for detecting interactables.")]
    public float InteractionRange        = 2.5f;
}