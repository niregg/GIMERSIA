using UnityEngine;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MovingLedgePlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 _pointA;
        [SerializeField] private Vector3 _pointB;
        [SerializeField] private float _speed = 0.45f;
        [SerializeField] private float _phaseOffset;

        private Vector3 _lastPosition;

        public void ConfigureWaypoints(Vector3 pointA, Vector3 pointB, float speed, float phaseOffset = 0f)
        {
            _pointA = pointA;
            _pointB = pointB;
            _speed = speed;
            _phaseOffset = phaseOffset;
            transform.position = _pointA;
            _lastPosition = _pointA;
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            float t = (Mathf.Sin((Time.time * _speed * Mathf.PI) + _phaseOffset) + 1f) * 0.5f;
            Vector3 nextPos = Vector3.Lerp(_pointA, _pointB, t);
            Vector3 delta = nextPos - _lastPosition;
            transform.position = nextPos;
            _lastPosition = nextPos;
        }
    }
}
