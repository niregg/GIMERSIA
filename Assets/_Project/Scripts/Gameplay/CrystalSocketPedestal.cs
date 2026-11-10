using UnityEngine;

namespace Gimersia.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CrystalSocketPedestal : MonoBehaviour
    {
        [Header("Gate Target")]
        [SerializeField] private Transform _gateWallTransform;
        [SerializeField] private Vector3 _openOffset = new Vector3(0f, -9.5f, 0f);
        [SerializeField] private float _gateSmoothTime = 0.65f;

        [Header("Visuals & Audio")]
        [SerializeField] private Renderer _ringRenderer;
        [SerializeField] private Light _socketLight;
        [SerializeField] private ParticleSystem _unlockParticles;
        [SerializeField] private AudioSource _unlockAudio;
        [SerializeField] private Color _lockedColor = new Color(1.0f, 0.45f, 0.1f, 1f);
        [SerializeField] private Color _unlockedColor = new Color(0.15f, 1.0f, 0.65f, 1f);

        private Vector3 _closedLocalPos;
        private Vector3 _openLocalPos;
        private Vector3 _gateVelocity;
        private bool _isUnlocked;
        private MaterialPropertyBlock _propBlock;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsUnlocked => _isUnlocked;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            if (_gateWallTransform != null)
            {
                _closedLocalPos = _gateWallTransform.localPosition;
                _openLocalPos = _closedLocalPos + _openOffset;
            }
            ApplyVisualState(false);
        }

        private void Update()
        {
            if (_gateWallTransform != null)
            {
                Vector3 target = _isUnlocked ? _openLocalPos : _closedLocalPos;
                _gateWallTransform.localPosition = Vector3.SmoothDamp(
                    _gateWallTransform.localPosition,
                    target,
                    ref _gateVelocity,
                    _gateSmoothTime);
            }

            if (!_isUnlocked && _ringRenderer != null)
            {
                _ringRenderer.transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.Self);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryActivateSocket(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryActivateSocket(other);
        }

        private void TryActivateSocket(Collider col)
        {
            if (_isUnlocked || col == null)
            {
                return;
            }

            if (!col.TryGetComponent<TerraCoreBall>(out TerraCoreBall ball))
            {
                return;
            }

            _isUnlocked = true;
            ApplyVisualState(true);

            if (_unlockParticles != null)
            {
                _unlockParticles.Play(true);
            }

            if (_unlockAudio != null && _unlockAudio.clip != null)
            {
                _unlockAudio.pitch = 1.08f;
                _unlockAudio.Play();
            }

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null && !ballRb.isKinematic)
            {
                Vector3 bounceBack = (-ballRb.velocity.normalized + Vector3.up * 0.75f).normalized;
                ballRb.velocity = bounceBack * 7.5f;
            }
        }

        private void ApplyVisualState(bool unlocked)
        {
            Color targetColor = unlocked ? _unlockedColor : _lockedColor;

            if (_ringRenderer != null)
            {
                _ringRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, targetColor);
                _propBlock.SetColor(ColorId, targetColor);
                _propBlock.SetColor(EmissionColorId, targetColor * (unlocked ? 2.4f : 1.3f));
                _ringRenderer.SetPropertyBlock(_propBlock);
            }

            if (_socketLight != null)
            {
                _socketLight.color = targetColor;
                _socketLight.intensity = unlocked ? 4.5f : 2.2f;
            }
        }
    }
}
