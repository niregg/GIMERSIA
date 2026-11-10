using UnityEngine;

namespace Gimersia.Gameplay
{
    [CreateAssetMenu(fileName = "CFG_CharacterMovement", menuName = "Gimersia/Configs/Character Movement Config")]
    public sealed class CharacterMovementConfigSO : ScriptableObject
    {
        [field: Header("Locomotion")]
        [field: SerializeField] public float MoveSpeed { get; private set; } = 8.2f;
        [field: SerializeField] public float CarryingMoveSpeedMultiplier { get; private set; } = 0.68f;
        [field: SerializeField] public float ChargingMoveSpeedMultiplier { get; private set; } = 0.42f;
        [field: SerializeField] public float Acceleration { get; private set; } = 45f;
        [field: SerializeField] public float Deceleration { get; private set; } = 52f;
        [field: SerializeField] public float RotationSpeed { get; private set; } = 720f;

        [field: Header("Jumping & Gravity")]
        [field: SerializeField] public float JumpForce { get; private set; } = 11.2f;
        [field: SerializeField] public float CarryingJumpMultiplier { get; private set; } = 0.65f;
        [field: SerializeField] public float CoyoteTimeDuration { get; private set; } = 0.14f;
        [field: SerializeField] public float JumpBufferDuration { get; private set; } = 0.15f;
        [field: SerializeField] public float GroundCheckRadius { get; private set; } = 0.35f;
        [field: SerializeField] public float GroundCheckOffset { get; private set; } = 0.08f;

        [field: Header("PEAK Wall Climbing (Free Hands Only)")]
        [field: SerializeField] public float WallClimbSpeed { get; private set; } = 5.8f;
        [field: SerializeField] public float MaxClimbStamina { get; private set; } = 4.5f;
        [field: SerializeField] public float StaminaRechargeRate { get; private set; } = 2.2f;
        [field: SerializeField] public float WallDetectDistance { get; private set; } = 0.85f;
        [field: SerializeField] public float LedgeMantleBoost { get; private set; } = 6.5f;

        [field: Header("Throw & Catch Tuning")]
        [field: SerializeField] public float MinThrowForce { get; private set; } = 10f;
        [field: SerializeField] public float MaxThrowForce { get; private set; } = 26f;
        [field: SerializeField] public float SweetSpotForceMultiplier { get; private set; } = 1.32f;
        [field: SerializeField] public float ChargeSpeed { get; private set; } = 1.10f;
        [field: SerializeField] public float SweetSpotMin { get; private set; } = 0.80f;
        [field: SerializeField] public float SweetSpotMax { get; private set; } = 0.95f;
        [field: SerializeField] public float CatchSlowMoRadius { get; private set; } = 4.2f;
        [field: SerializeField] public float QuickPassMaxDistance { get; private set; } = 12.5f;
    }
}
