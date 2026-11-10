using UnityEngine;
using Gimersia.Core;
using Gimersia.Juice;
using Gimersia.Audio;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class BeanClimberController : MonoBehaviour
    {
        [SerializeField] private CharacterMovementConfigSO _config;
        [SerializeField] private Transform _visualModelRoot;
        [SerializeField] private Transform _ballHoldPoint;
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private ParticleSystem _footstepDustVfx;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private AudioClip _jumpClip;
        [SerializeField] private AudioClip _bonkStumbleClip;

        private readonly Collider[] _groundHits = new Collider[4];
        private readonly RaycastHit[] _wallHits = new RaycastHit[4];
        private Rigidbody _rb;

        private Vector2 _moveInput;
        private bool _isJumpHeld;
        private bool _isGrounded;
        private float _lastGroundedTime = -10f;
        private float _lastJumpPressedTime = -10f;
        private float _footstepTimer;

        // PEAK Wall Climbing State
        private bool _isWallClimbing;
        private float _currentClimbStamina = 4.5f;
        private Vector3 _wallNormal;
        private float _mantleGraceTimer;

        private bool _isChargingThrow;
        private float _chargeNormalized;
        private float _chargeDirection = 1f;

        private bool _isInCatchWindow;
        private float _catchWindowTimer;
        private float _bonkStunTimer;

        private Vector3 _visualTargetScale = Vector3.one;
        private Vector3 _visualScaleVelocity;

        private TerraCoreBall _activeBall;
        private BeanClimberController _partnerClimber;

        public CharacterMovementConfigSO Config => _config;
        public Transform BallHoldPoint => _ballHoldPoint != null ? _ballHoldPoint : transform;
        public bool HasBall => _activeBall != null && _activeBall.CurrentHolder == this;
        public bool IsChargingThrow => _isChargingThrow;
        public float ChargeNormalized => _chargeNormalized;
        public bool IsInCatchWindow => _isInCatchWindow;
        public bool IsStunned => _bonkStunTimer > 0f;
        public bool IsWallClimbing => _isWallClimbing;
        public float ClimbStaminaNormalized => _config != null ? Mathf.Clamp01(_currentClimbStamina / _config.MaxClimbStamina) : 1f;
        public int TotalTeamworkActions { get; private set; }

        public bool CanAssistPullUpPartner
        {
            get
            {
                if (!_isGrounded || HasBall || IsStunned || _partnerClimber == null)
                {
                    return false;
                }

                Vector3 delta = transform.position - _partnerClimber.transform.position;
                float vertDiff = delta.y;
                float horizDistSqr = (delta.x * delta.x) + (delta.z * delta.z);

                return vertDiff >= 0.85f && vertDiff <= 5.2f && horizDistSqr <= 18.5f &&
                       (_partnerClimber.IsWallClimbing || !_partnerClimber._isGrounded || _partnerClimber.ClimbStaminaNormalized < 0.65f);
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (_visualModelRoot == null)
            {
                _visualModelRoot = transform;
            }

            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 0;
                _trajectoryLine.enabled = false;
            }
        }

        public void InitializeClimber(CharacterMovementConfigSO config, TerraCoreBall ball, BeanClimberController partner)
        {
            _config = config;
            _activeBall = ball;
            _partnerClimber = partner;
            _currentClimbStamina = _config.MaxClimbStamina;
        }

        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
        }

        public void SetJumpHeld(bool isHeld)
        {
            _isJumpHeld = isHeld;
        }

        public void RequestJump()
        {
            _lastJumpPressedTime = Time.time;
        }

        public void BeginThrowCharge()
        {
            if (!HasBall || IsStunned)
            {
                return;
            }

            _isChargingThrow = true;
            _chargeNormalized = 0f;
            _chargeDirection = 1f;

            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = true;
            }
        }

        public void ReleaseThrow()
        {
            if (!_isChargingThrow || !HasBall)
            {
                return;
            }

            _isChargingThrow = false;
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }

            bool isSweetSpot = _chargeNormalized >= _config.SweetSpotMin && _chargeNormalized <= _config.SweetSpotMax;
            bool isOvercharged = _chargeNormalized > _config.SweetSpotMax;

            Vector3 throwVelocity = CalculateThrowVelocity(_chargeNormalized, isSweetSpot);
            _activeBall.LaunchThrow(throwVelocity, _partnerClimber, isSweetSpot);

            ApplySquashAndStretch(new Vector3(0.78f, 1.32f, 0.78f));

            if (isSweetSpot)
            {
                if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
                {
                    timeService.TriggerHitStop(0.035f, 0.02f);
                }

                if (ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shakeService))
                {
                    shakeService.AddTrauma(0.48f);
                }
            }
            else if (isOvercharged)
            {
                _rb.AddForce(-transform.forward * 5.5f, ForceMode.VelocityChange);
                ApplySquashAndStretch(new Vector3(1.32f, 0.65f, 1.32f));
            }
        }

        public void TryExecuteQuickPass()
        {
            if (!HasBall || _partnerClimber == null || IsStunned)
            {
                return;
            }

            float dist = Vector3.Distance(transform.position, _partnerClimber.transform.position);
            if (dist <= _config.QuickPassMaxDistance)
            {
                _isChargingThrow = false;
                if (_trajectoryLine != null)
                {
                    _trajectoryLine.enabled = false;
                }

                _activeBall.LaunchQuickPass(_partnerClimber);
                ApplySquashAndStretch(new Vector3(1.20f, 0.80f, 1.20f));
            }
        }

        public void TryExecuteCatchInput()
        {
            if (IsStunned || _activeBall == null)
            {
                return;
            }

            if (_isInCatchWindow)
            {
                CompleteCatch(isPerfectCatch: true);
                return;
            }

            // Ledge Pull-Up Assist (Uluran Tangan Penyelamat)
            if (CanAssistPullUpPartner)
            {
                Vector3 safeLedgeSpot = transform.position + (transform.forward * 0.85f) + (Vector3.up * 0.25f);
                _partnerClimber.ReceiveLedgePullUpAssist(safeLedgeSpot);
                TotalTeamworkActions++;
                ApplySquashAndStretch(new Vector3(1.25f, 0.72f, 1.25f));
                return;
            }

            if (_activeBall.State == BallState.FreeOnGround)
            {
                float distSqr = (_activeBall.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= 5.8f)
                {
                    _activeBall.AttachToHolder(this);
                    _activeBall.PlayCatchFeedback(isPerfect: false);
                    ApplySquashAndStretch(new Vector3(1.2f, 0.8f, 1.2f));
                }
            }
        }

        public void ReceiveLedgePullUpAssist(Vector3 targetSafePos)
        {
            _isWallClimbing = false;
            _mantleGraceTimer = 0.35f;
            _currentClimbStamina = _config != null ? _config.MaxClimbStamina : 4.5f;
            _rb.position = targetSafePos;
            _rb.velocity = Vector3.up * 3.5f;
            ApplySquashAndStretch(new Vector3(0.76f, 1.32f, 0.76f));

            if (_footstepDustVfx != null)
            {
                _footstepDustVfx.Emit(14);
            }

            if (_jumpClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_jumpClip, transform.position, 0.85f, 1.22f);
            }
        }

        public void OpenCatchWindow()
        {
            _isInCatchWindow = true;
            _catchWindowTimer = 1.25f;
        }

        public void CompleteCatch(bool isPerfectCatch)
        {
            _isInCatchWindow = false;
            _catchWindowTimer = 0f;
            _currentClimbStamina = _config != null ? _config.MaxClimbStamina : 4.5f;
            TotalTeamworkActions++;

            if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
            {
                timeService.EndSlowMotion();
                if (isPerfectCatch)
                {
                    timeService.TriggerHitStop(0.03f, 0.05f);
                }
            }

            if (isPerfectCatch && ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shakeService))
            {
                shakeService.AddTrauma(0.38f);
            }

            _activeBall.AttachToHolder(this);
            _activeBall.PlayCatchFeedback(isPerfectCatch);
            ApplySquashAndStretch(new Vector3(1.28f, 0.68f, 1.28f));
        }

        public void TriggerMissedCatchBonk()
        {
            _isInCatchWindow = false;
            _catchWindowTimer = 0f;
            _bonkStunTimer = 0.85f;
            _isWallClimbing = false;

            ApplySquashAndStretch(new Vector3(1.45f, 0.50f, 1.45f));
            _rb.AddForce(Vector3.up * 3.8f - transform.forward * 3.2f, ForceMode.VelocityChange);

            if (ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shakeService))
            {
                shakeService.AddTrauma(0.45f);
            }

            if (_bonkStumbleClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_bonkStumbleClip, transform.position, 0.95f, 0.95f);
            }
        }

        private void Update()
        {
            if (_bonkStunTimer > 0f)
            {
                _bonkStunTimer -= Time.deltaTime;
            }

            if (_isInCatchWindow)
            {
                _catchWindowTimer -= Time.unscaledDeltaTime;
                if (_catchWindowTimer <= 0f)
                {
                    _isInCatchWindow = false;
                }
            }

            if (_isChargingThrow)
            {
                _chargeNormalized += _chargeDirection * _config.ChargeSpeed * Time.deltaTime;
                if (_chargeNormalized >= 1f)
                {
                    _chargeNormalized = 1f;
                    _chargeDirection = -1f;
                }
                else if (_chargeNormalized <= 0f)
                {
                    _chargeNormalized = 0f;
                    _chargeDirection = 1f;
                }

                RenderTrajectoryPreview();
            }

            UpdateFootstepAudio(Time.deltaTime);
            UpdateVisualSpring(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_mantleGraceTimer > 0f)
            {
                _mantleGraceTimer -= Time.fixedDeltaTime;
            }

            CheckGrounded();
            CheckAndExecuteWallClimb(Time.fixedDeltaTime);

            if (!_isWallClimbing)
            {
                HandleMovement(Time.fixedDeltaTime);
                HandleJump();
            }

            // PhysX anti-launch safety clamp (prevents rogue depenetration impulses from ejecting character into the sky)
            if (_rb.velocity.y > 16.0f)
            {
                _rb.velocity = new Vector3(_rb.velocity.x, 16.0f, _rb.velocity.z);
            }
        }

        private void CheckAndExecuteWallClimb(float fixedDt)
        {
            if (_mantleGraceTimer > 0f)
            {
                _isWallClimbing = false;
                return;
            }

            // Cannot wall climb while holding the heavy Terra Core crystal ball!
            if (HasBall || IsStunned || !_isJumpHeld || _currentClimbStamina <= 0.05f)
            {
                if (_isWallClimbing)
                {
                    _isWallClimbing = false;
                }
                return;
            }

            Vector3 castDir = transform.forward;
            if (_moveInput.sqrMagnitude > 0.05f)
            {
                castDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            }

            // Dual-height SphereCast (waist + chest) so jagged 3D Kenney rock meshes never miss or snag!
            Vector3 waistOrigin = transform.position + (Vector3.up * 0.48f) - (castDir * 0.15f);
            Vector3 chestOrigin = transform.position + (Vector3.up * 1.05f) - (castDir * 0.15f);
            float castDist = _config.WallDetectDistance + 0.45f;

            bool foundClimbableWall = TryDetectClimbableSurface(chestOrigin, castDir, castDist, out Vector3 detectedNormal) ||
                                      TryDetectClimbableSurface(waistOrigin, castDir, castDist, out detectedNormal);

            if (foundClimbableWall)
            {
                _isWallClimbing = true;
                _wallNormal = detectedNormal;
                _currentClimbStamina = Mathf.Max(0f, _currentClimbStamina - fixedDt);

                // Smooth upward climb velocity without jamming capsule into overhang triangles
                Vector3 wallHorizNormal = new Vector3(_wallNormal.x, 0f, _wallNormal.z);
                if (wallHorizNormal.sqrMagnitude > 0.001f)
                {
                    wallHorizNormal.Normalize();
                    Quaternion faceWallRot = Quaternion.LookRotation(-wallHorizNormal, Vector3.up);
                    _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, faceWallRot, 720f * fixedDt));
                }

                Vector3 climbVel = (Vector3.up * (_config.WallClimbSpeed * 1.15f)) + (castDir * 2.2f);
                _rb.velocity = climbVel;

                // Ledge Vault Mantle Check: Is the space above the cliff lip open?
                Vector3 headOrigin = transform.position + (Vector3.up * 1.85f);
                bool headBlocked = Physics.Raycast(headOrigin, castDir, _config.WallDetectDistance + 0.45f, _groundMask, QueryTriggerInteraction.Ignore);

                if (!headBlocked)
                {
                    Vector3 overLedgeProbe = headOrigin + (castDir * 0.95f) + (Vector3.up * 0.35f);
                    if (Physics.Raycast(overLedgeProbe, Vector3.down, out RaycastHit downHit, 2.4f, _groundMask, QueryTriggerInteraction.Ignore) &&
                        downHit.normal.y >= 0.55f)
                    {
                        // Deterministic Ledge Vault onto the top surface!
                        _rb.position = downHit.point + (Vector3.up * 0.18f) + (castDir * 0.35f);
                        _rb.velocity = (castDir * 5.2f) + (Vector3.up * 3.8f);
                    }
                    else
                    {
                        _rb.velocity = (Vector3.up * _config.LedgeMantleBoost) + (castDir * 5.5f);
                    }

                    _mantleGraceTimer = 0.28f;
                    _isWallClimbing = false;
                    ApplySquashAndStretch(new Vector3(1.24f, 0.72f, 1.24f));
                }
            }
            else
            {
                _isWallClimbing = false;
            }
        }

        private bool TryDetectClimbableSurface(Vector3 origin, Vector3 dir, float maxDist, out Vector3 normal)
        {
            normal = Vector3.up;
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                0.34f,
                dir,
                _wallHits,
                maxDist,
                _groundMask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _wallHits[i];
                if (hit.collider != null &&
                    hit.collider.gameObject != gameObject &&
                    !hit.collider.transform.IsChildOf(transform) &&
                    (_partnerClimber == null || !hit.collider.transform.IsChildOf(_partnerClimber.transform)))
                {
                    if (hit.normal.y < 0.76f && hit.normal.y > -0.45f)
                    {
                        normal = hit.normal;
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateFootstepAudio(float dt)
        {
            if ((!_isGrounded && !_isWallClimbing) || (_moveInput.sqrMagnitude < 0.05f && !_isWallClimbing) || IsStunned)
            {
                _footstepTimer = 0.05f;
                return;
            }

            _footstepTimer -= dt;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _isWallClimbing ? 0.22f : (HasBall ? 0.36f : 0.27f);
                if (_footstepClips != null && _footstepClips.Length > 0 && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                {
                    AudioClip clip = _footstepClips[Random.Range(0, _footstepClips.Length)];
                    float pitch = _isWallClimbing ? Random.Range(1.12f, 1.28f) : Random.Range(0.92f, 1.08f);
                    audio.PlayClip(clip, transform.position, 0.45f, pitch);
                }

                if (_footstepDustVfx != null)
                {
                    _footstepDustVfx.Emit(_isWallClimbing ? 2 : 4);
                }

                if (_visualModelRoot != null)
                {
                    float tilt = Mathf.Sin(Time.time * 18f) * (_isWallClimbing ? 8.5f : 5.5f);
                    _visualModelRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
                }
            }
        }

        private void CheckGrounded()
        {
            Vector3 spherePos = transform.position + (Vector3.up * (_config.GroundCheckRadius - _config.GroundCheckOffset));
            int count = Physics.OverlapSphereNonAlloc(spherePos, _config.GroundCheckRadius, _groundHits, _groundMask, QueryTriggerInteraction.Ignore);

            bool wasGrounded = _isGrounded;
            _isGrounded = false;

            for (int i = 0; i < count; i++)
            {
                if (_groundHits[i] != null && _groundHits[i].gameObject != gameObject)
                {
                    _isGrounded = true;
                    break;
                }
            }

            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                _currentClimbStamina = Mathf.MoveTowards(_currentClimbStamina, _config.MaxClimbStamina, _config.StaminaRechargeRate * Time.fixedDeltaTime);

                if (!wasGrounded && _rb.velocity.y <= 0.1f)
                {
                    ApplySquashAndStretch(new Vector3(1.24f, 0.70f, 1.24f));
                    if (_footstepDustVfx != null)
                    {
                        _footstepDustVfx.Emit(12);
                    }
                }
            }
        }

        private void HandleMovement(float fixedDt)
        {
            if (IsStunned)
            {
                return;
            }

            Vector3 desiredDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (desiredDir.sqrMagnitude > 1f)
            {
                desiredDir.Normalize();
            }

            float speedMultiplier = 1f;
            if (_isChargingThrow)
            {
                speedMultiplier = _config.ChargingMoveSpeedMultiplier;
            }
            else if (HasBall)
            {
                speedMultiplier = _config.CarryingMoveSpeedMultiplier;
            }

            Vector3 targetVelocity = desiredDir * (_config.MoveSpeed * speedMultiplier);
            Vector3 currentHorizontal = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            float accelRate = desiredDir.sqrMagnitude > 0.01f ? _config.Acceleration : _config.Deceleration;

            Vector3 nextHorizontal = Vector3.MoveTowards(currentHorizontal, targetVelocity, accelRate * fixedDt);
            _rb.velocity = new Vector3(nextHorizontal.x, _rb.velocity.y, nextHorizontal.z);

            if (desiredDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, targetRot, _config.RotationSpeed * fixedDt));
            }
        }

        private void HandleJump()
        {
            if (IsStunned || _isChargingThrow)
            {
                return;
            }

            bool canCoyoteJump = (Time.time - _lastGroundedTime) <= _config.CoyoteTimeDuration;
            bool hasBufferedJump = (Time.time - _lastJumpPressedTime) <= _config.JumpBufferDuration;

            if (canCoyoteJump && hasBufferedJump)
            {
                _lastJumpPressedTime = -10f;
                _lastGroundedTime = -10f;
                _isGrounded = false;

                float jumpPower = _config.JumpForce * (HasBall ? _config.CarryingJumpMultiplier : 1f);
                _rb.velocity = new Vector3(_rb.velocity.x, jumpPower, _rb.velocity.z);
                ApplySquashAndStretch(new Vector3(0.75f, 1.34f, 0.75f));

                if (_jumpClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                {
                    audio.PlayClip(_jumpClip, transform.position, 0.65f, Random.Range(1.05f, 1.18f));
                }
            }
        }

        private Vector3 CalculateThrowVelocity(float charge01, bool isSweetSpot)
        {
            Vector3 forwardAim = transform.forward;
            float upwardPitch = 0.78f;

            if (_partnerClimber != null)
            {
                Vector3 toPartner = _partnerClimber.transform.position - transform.position;
                float heightDiff = toPartner.y;
                toPartner.y = 0f;
                if (toPartner.sqrMagnitude > 0.25f)
                {
                    forwardAim = Vector3.Lerp(transform.forward, toPartner.normalized, 0.72f).normalized;
                }

                // Automatically lob higher if partner has climbed onto a high cliff!
                if (heightDiff > 2.0f)
                {
                    upwardPitch = Mathf.Clamp(0.85f + (heightDiff * 0.08f), 0.85f, 1.45f);
                }
            }

            Vector3 launchDir = (forwardAim + (Vector3.up * upwardPitch)).normalized;
            float force = Mathf.Lerp(_config.MinThrowForce, _config.MaxThrowForce, charge01);
            if (isSweetSpot)
            {
                force *= _config.SweetSpotForceMultiplier;
            }

            return launchDir * force;
        }

        private void RenderTrajectoryPreview()
        {
            if (_trajectoryLine == null)
            {
                return;
            }

            const int steps = 22;
            _trajectoryLine.positionCount = steps;

            bool sweet = _chargeNormalized >= _config.SweetSpotMin && _chargeNormalized <= _config.SweetSpotMax;
            Vector3 startPos = BallHoldPoint.position;
            Vector3 velocity = CalculateThrowVelocity(_chargeNormalized, sweet);
            Vector3 gravity = Physics.gravity;

            for (int i = 0; i < steps; i++)
            {
                float t = i * 0.065f;
                Vector3 point = startPos + (velocity * t) + (0.5f * gravity * (t * t));
                _trajectoryLine.SetPosition(i, point);
            }
        }

        private void ApplySquashAndStretch(Vector3 deformedScale)
        {
            if (_visualModelRoot == null)
            {
                return;
            }

            _visualModelRoot.localScale = deformedScale;
        }

        private void UpdateVisualSpring(float dt)
        {
            if (_visualModelRoot == null)
            {
                return;
            }

            _visualModelRoot.localScale = Vector3.SmoothDamp(
                _visualModelRoot.localScale,
                _visualTargetScale,
                ref _visualScaleVelocity,
                0.085f,
                Mathf.Infinity,
                dt
            );

            if (_moveInput.sqrMagnitude < 0.05f && !_isWallClimbing)
            {
                _visualModelRoot.localRotation = Quaternion.Slerp(_visualModelRoot.localRotation, Quaternion.identity, 10f * dt);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_activeBall != null && _activeBall.State == BallState.FreeOnGround && other.gameObject == _activeBall.gameObject)
            {
                _activeBall.AttachToHolder(this);
                _activeBall.PlayCatchFeedback(isPerfect: false);
                ApplySquashAndStretch(new Vector3(1.18f, 0.82f, 1.18f));
            }
        }
    }
}
