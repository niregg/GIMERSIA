using UnityEngine;
using Gimersia.Core;

namespace Gimersia.Juice
{
    public interface ICameraShakeService
    {
        void AddTrauma(float amount);
    }

    public sealed class TraumaCameraShaker : MonoBehaviour, ICameraShakeService
    {
        [SerializeField] private Transform _shakePivot;
        [SerializeField] private float _traumaDecay = 1.4f;
        [SerializeField] private float _noiseFrequency = 22f;
        [SerializeField] private Vector3 _maxPositionOffset = new Vector3(0.25f, 0.25f, 0f);
        [SerializeField] private Vector3 _maxRotationAngles = new Vector3(4f, 4f, 6f);

        private float _trauma;
        private float _seed;

        private void Awake()
        {
            if (_shakePivot == null)
            {
                _shakePivot = transform;
            }

            _seed = Random.value * 100f;
            ServiceLocator.Register<ICameraShakeService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ICameraShakeService>(this);
        }

        private void LateUpdate()
        {
            if (_trauma <= 0.0001f)
            {
                _shakePivot.localPosition = Vector3.zero;
                _shakePivot.localRotation = Quaternion.identity;
                return;
            }

            float shake = _trauma * _trauma;
            float time = Time.unscaledTime * _noiseFrequency;

            float nx = (Mathf.PerlinNoise(_seed, time) * 2f) - 1f;
            float ny = (Mathf.PerlinNoise(_seed + 13.7f, time) * 2f) - 1f;
            float nz = (Mathf.PerlinNoise(_seed + 29.3f, time) * 2f) - 1f;

            _shakePivot.localPosition = new Vector3(
                _maxPositionOffset.x * shake * nx,
                _maxPositionOffset.y * shake * ny,
                _maxPositionOffset.z * shake * nz
            );

            _shakePivot.localRotation = Quaternion.Euler(
                _maxRotationAngles.x * shake * ny,
                _maxRotationAngles.y * shake * nx,
                _maxRotationAngles.z * shake * nz
            );

            _trauma = Mathf.Max(0f, _trauma - (_traumaDecay * Time.unscaledDeltaTime));
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }
    }
}
