using UnityEngine;

namespace Gimersia.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BeanClimberController))]
    public sealed class CompanionFollowerAI : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private float _followStartDistance = 2.6f;
        [SerializeField] private float _followStopDistance = 1.65f;
        [SerializeField] private float _teleportCatchupDistance = 22.0f;
        [SerializeField] private float _jumpCheckDistance = 1.45f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        private BeanClimberController _selfClimber;
        private Rigidbody _rb;
        private Transform _targetLeader;
        private bool _isAiActive;
        private float _jumpCooldownTimer;
        private readonly RaycastHit[] _rayBuffer = new RaycastHit[6];

        public bool IsAiActive => _isAiActive;

        private void Awake()
        {
            _selfClimber = GetComponent<BeanClimberController>();
            _rb = GetComponent<Rigidbody>();
        }

        public void SetTargetLeader(Transform leader)
        {
            _targetLeader = leader;
        }

        public void SetAiActive(bool active)
        {
            _isAiActive = active;
            if (!_isAiActive && _selfClimber != null)
            {
                _selfClimber.SetMoveInput(Vector2.zero);
                _selfClimber.SetJumpHeld(false);
            }
        }

        private void Update()
        {
            if (!_isAiActive || _selfClimber == null || _targetLeader == null)
            {
                return;
            }

            if (_jumpCooldownTimer > 0f)
            {
                _jumpCooldownTimer -= Time.deltaTime;
            }

            Vector3 delta = _targetLeader.position - transform.position;
            float verticalDiff = delta.y;
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            float flatDistance = flatDelta.magnitude;

            // Safety catch-up if AI falls way behind a tall cliff or chasm in Solo Mode
            if (flatDistance > _teleportCatchupDistance || verticalDiff > 11.5f)
            {
                Vector3 catchupPos = _targetLeader.position - (_targetLeader.forward * 1.9f) + (Vector3.up * 0.65f);
                transform.position = catchupPos;
                if (_rb != null)
                {
                    _rb.velocity = Vector3.zero;
                }
                return;
            }

            if (flatDistance > _followStartDistance)
            {
                Vector3 moveDir = flatDelta.normalized;
                _selfClimber.SetMoveInput(new Vector2(moveDir.x, moveDir.z));

                // Check for obstacle or cliff in front of AI
                Vector3 shinOrigin = transform.position + (Vector3.up * 0.42f);
                int hitCount = Physics.RaycastNonAlloc(
                    shinOrigin,
                    moveDir,
                    _rayBuffer,
                    _jumpCheckDistance,
                    _obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                bool blockedAhead = false;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider col = _rayBuffer[i].collider;
                    if (col != null &&
                        !col.transform.IsChildOf(transform) &&
                        !col.transform.IsChildOf(_targetLeader) &&
                        _rayBuffer[i].normal.y < 0.72f)
                    {
                        blockedAhead = true;
                        break;
                    }
                }

                if (blockedAhead)
                {
                    if (_jumpCooldownTimer <= 0f)
                    {
                        _selfClimber.RequestJump();
                        _jumpCooldownTimer = 0.55f;
                    }

                    // If leader is above on a cliff and AI has free hands, hold jump to wall-climb!
                    bool shouldWallClimb = verticalDiff > 1.5f && !_selfClimber.HasBall;
                    _selfClimber.SetJumpHeld(shouldWallClimb);
                }
                else
                {
                    _selfClimber.SetJumpHeld(false);
                }
            }
            else if (flatDistance <= _followStopDistance)
            {
                _selfClimber.SetMoveInput(Vector2.zero);
                _selfClimber.SetJumpHeld(false);

                // Face toward the active player when standing nearby
                if (flatDelta.sqrMagnitude > 0.05f && _rb != null)
                {
                    Quaternion lookRot = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 8f * Time.deltaTime);
                }
            }
        }
    }
}
