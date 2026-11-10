using UnityEngine;

namespace Gimersia.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class RollingBoulderHazard : MonoBehaviour
    {
        [SerializeField] private Vector3 _spawnLocalPosition;
        [SerializeField] private Vector3 _rollDirection = new Vector3(0f, -0.45f, -1f);
        [SerializeField] private float _rollAcceleration = 14f;
        [SerializeField] private float _maxSpeed = 16f;
        [SerializeField] private float _resetLifetime = 7.5f;
        [SerializeField] private float _knockbackImpulse = 13f;
        [SerializeField] private AudioSource _rumbleAudio;
        [SerializeField] private ParticleSystem _dustTrailParticles;

        private Rigidbody _rb;
        private float _timer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 18f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _spawnLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            ResetBoulder();
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer >= _resetLifetime || transform.position.y < -15f)
            {
                ResetBoulder();
                return;
            }

            Vector3 worldDir = transform.parent != null
                ? transform.parent.TransformDirection(_rollDirection.normalized)
                : _rollDirection.normalized;

            if (_rb.velocity.sqrMagnitude < _maxSpeed * _maxSpeed)
            {
                _rb.AddForce(worldDir * _rollAcceleration, ForceMode.Acceleration);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.TryGetComponent<TerraCoreBall>(out TerraCoreBall ball))
            {
                if (ball.IsSweetSpotCharged || collision.relativeVelocity.sqrMagnitude > 85f)
                {
                    if (_dustTrailParticles != null)
                    {
                        _dustTrailParticles.Play(true);
                    }
                    ResetBoulder();
                    return;
                }
            }

            if (collision.collider.TryGetComponent<BeanClimberController>(out BeanClimberController climber))
            {
                Rigidbody climberRb = climber.GetComponent<Rigidbody>();
                if (climberRb != null)
                {
                    Vector3 pushDir = (climber.transform.position - transform.position).normalized;
                    pushDir.y = 0.55f;
                    climberRb.AddForce(pushDir.normalized * _knockbackImpulse, ForceMode.VelocityChange);
                }

                if (_rumbleAudio != null && _rumbleAudio.clip != null)
                {
                    _rumbleAudio.pitch = Random.Range(0.88f, 1.04f);
                    _rumbleAudio.Play();
                }
            }
        }

        private void ResetBoulder()
        {
            _timer = 0f;
            transform.localPosition = _spawnLocalPosition;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}
