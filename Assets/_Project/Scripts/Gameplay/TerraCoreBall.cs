using UnityEngine;
using Gimersia.Core;
using Gimersia.Juice;
using Gimersia.Audio;

namespace Gimersia.Gameplay
{
    public enum BallState
    {
        FreeOnGround,
        HeldByPlayer,
        InFlightThrown,
        InFlightQuickPass
    }

    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class TerraCoreBall : MonoBehaviour
    {
        [SerializeField] private float _sanctuaryRadius = 3.8f;
        [SerializeField] private LayerMask _restorableLayerMask = ~0;
        [SerializeField] private Transform _auraVisualSphere;
        [SerializeField] private ParticleSystem _flightSparkParticles;
        [SerializeField] private ParticleSystem _catchShockwaveParticles;
        [SerializeField] private TrailRenderer _ballTrail;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _throwLaunchClip;
        [SerializeField] private AudioClip _sweetSpotLaunchClip;
        [SerializeField] private AudioClip _slowMoEnterClip;
        [SerializeField] private AudioClip _perfectCatchClip;
        [SerializeField] private AudioClip _bounceImpactClip;

        private readonly Collider[] _overlapBuffer = new Collider[32];
        private Rigidbody _rb;
        private SphereCollider _collider;

        private BallState _state = BallState.FreeOnGround;
        private BeanClimberController _currentHolder;
        private BeanClimberController _intendedReceiver;
        private bool _isSweetSpotCharged;
        private bool _hasTriggeredSlowMoOnFlight;

        private Vector3 _passStartPos;
        private float _passProgress;
        private float _passSpeed = 15f;

        public BallState State => _state;
        public BeanClimberController CurrentHolder => _currentHolder;
        public BeanClimberController IntendedReceiver => _intendedReceiver;
        public bool IsSweetSpotCharged => _isSweetSpotCharged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Update()
        {
            PulseSanctuaryAura();

            if (_state == BallState.HeldByPlayer && _currentHolder != null)
            {
                transform.position = _currentHolder.BallHoldPoint.position;
                transform.Rotate(Vector3.up, 95f * Time.deltaTime, Space.World);
            }
            else if (_state == BallState.InFlightQuickPass && _intendedReceiver != null)
            {
                UpdateQuickPassFlight(Time.deltaTime);
            }
            else if (_state == BallState.InFlightThrown && _intendedReceiver != null)
            {
                CheckProximitySlowMoCatch();
            }
        }

        public void AttachToHolder(BeanClimberController holder)
        {
            if (holder == null)
            {
                return;
            }

            _currentHolder = holder;
            _intendedReceiver = null;
            _isSweetSpotCharged = false;
            _hasTriggeredSlowMoOnFlight = false;
            _state = BallState.HeldByPlayer;

            if (!_rb.isKinematic)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _collider.enabled = false;

            if (_ballTrail != null)
            {
                _ballTrail.emitting = false;
            }

            transform.position = holder.BallHoldPoint.position;
        }

        public void LaunchThrow(Vector3 initialVelocity, BeanClimberController receiver, bool isSweetSpot)
        {
            _currentHolder = null;
            _intendedReceiver = receiver;
            _isSweetSpotCharged = isSweetSpot;
            _hasTriggeredSlowMoOnFlight = false;
            _state = BallState.InFlightThrown;

            _collider.enabled = true;
            _rb.isKinematic = false;
            _rb.velocity = initialVelocity;

            if (_ballTrail != null)
            {
                _ballTrail.Clear();
                _ballTrail.emitting = true;
            }

            if (_flightSparkParticles != null)
            {
                _flightSparkParticles.Play();
            }

            if (ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                AudioClip clip = isSweetSpot && _sweetSpotLaunchClip != null ? _sweetSpotLaunchClip : _throwLaunchClip;
                audio.PlayClip(clip, transform.position, 0.9f, isSweetSpot ? 1.12f : 0.98f);
            }
        }

        public void LaunchQuickPass(BeanClimberController receiver)
        {
            if (receiver == null)
            {
                return;
            }

            _currentHolder = null;
            _intendedReceiver = receiver;
            _isSweetSpotCharged = false;
            _hasTriggeredSlowMoOnFlight = false;
            _state = BallState.InFlightQuickPass;

            _passStartPos = transform.position;
            _passProgress = 0f;

            _rb.isKinematic = true;
            _collider.enabled = false;

            if (_ballTrail != null)
            {
                _ballTrail.Clear();
                _ballTrail.emitting = true;
            }

            if (ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_throwLaunchClip, transform.position, 0.75f, 1.18f);
            }
        }

        public void PlayCatchFeedback(bool isPerfect)
        {
            if (_catchShockwaveParticles != null)
            {
                _catchShockwaveParticles.Play();
            }

            if (ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_perfectCatchClip, transform.position, 0.95f, isPerfect ? 1.08f : 0.98f);
            }
        }

        private void UpdateQuickPassFlight(float dt)
        {
            Vector3 targetPos = _intendedReceiver.BallHoldPoint.position;
            float distance = Vector3.Distance(_passStartPos, targetPos);
            float step = (_passSpeed / Mathf.Max(1f, distance)) * dt;
            _passProgress += step;

            Vector3 linearPos = Vector3.Lerp(_passStartPos, targetPos, _passProgress);
            float arcHeight = Mathf.Sin(_passProgress * Mathf.PI) * 1.4f;
            transform.position = linearPos + (Vector3.up * arcHeight);

            if (_passProgress >= 1f)
            {
                _intendedReceiver.CompleteCatch(isPerfectCatch: false);
            }
        }

        private void CheckProximitySlowMoCatch()
        {
            if (_hasTriggeredSlowMoOnFlight || _intendedReceiver == null)
            {
                return;
            }

            float distSqr = (transform.position - _intendedReceiver.transform.position).sqrMagnitude;
            float triggerRadius = _intendedReceiver.Config.CatchSlowMoRadius;

            if (distSqr <= triggerRadius * triggerRadius)
            {
                _hasTriggeredSlowMoOnFlight = true;
                _intendedReceiver.OpenCatchWindow();

                if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
                {
                    timeService.StartSlowMotion(0.25f, 1.15f);
                }

                if (ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                {
                    audio.PlayClip(_slowMoEnterClip, transform.position, 0.8f, 0.75f);
                }
            }
        }

        private void PulseSanctuaryAura()
        {
            if (_auraVisualSphere != null)
            {
                float diameter = _sanctuaryRadius * 2f;
                float pulse = 1f + (Mathf.Sin(Time.time * 3.2f) * 0.035f);
                _auraVisualSphere.localScale = new Vector3(diameter * pulse, 0.12f, diameter * pulse);
                _auraVisualSphere.position = new Vector3(transform.position.x, transform.position.y - 0.35f, transform.position.z);
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _sanctuaryRadius,
                _overlapBuffer,
                _restorableLayerMask,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col != null)
                {
                    RestorableFoliage foliage = col.GetComponentInParent<RestorableFoliage>();
                    if (foliage != null)
                    {
                        foliage.RestoreNature();
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_state != BallState.InFlightThrown)
            {
                return;
            }

            if (_intendedReceiver != null && collision.gameObject == _intendedReceiver.gameObject)
            {
                if (_intendedReceiver.IsInCatchWindow)
                {
                    _intendedReceiver.TriggerMissedCatchBonk();
                }
            }
            else if (ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_bounceImpactClip, transform.position, 0.7f, Random.Range(0.92f, 1.08f));
            }

            if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
            {
                timeService.EndSlowMotion();
            }

            if (_ballTrail != null)
            {
                _ballTrail.emitting = false;
            }

            _state = BallState.FreeOnGround;
            _intendedReceiver = null;
            _isSweetSpotCharged = false;
        }
    }
}
