using UnityEngine;
using Gimersia.Core;
using Gimersia.Audio;
using Gimersia.Juice;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class BounceMushroomPad : MonoBehaviour
    {
        [SerializeField] private float _bounceVelocity = 16.5f;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private AudioClip _bounceClip;

        private Vector3 _initialScale;
        private float _springTimer;

        private void Awake()
        {
            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }
            _initialScale = _visualRoot.localScale;
        }

        private void Update()
        {
            if (_springTimer <= 0f)
            {
                return;
            }

            _springTimer -= Time.deltaTime * 5f;
            float t = 1f - Mathf.Clamp01(_springTimer);
            float wave = Mathf.Sin(t * Mathf.PI * 2f) * 0.35f * (1f - t);
            _visualRoot.localScale = new Vector3(
                _initialScale.x * (1f + wave),
                _initialScale.y * (1f - wave),
                _initialScale.z * (1f + wave)
            );

            if (_springTimer <= 0f)
            {
                _visualRoot.localScale = _initialScale;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.rigidbody != null)
            {
                LaunchRigidbody(collision.rigidbody);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                LaunchRigidbody(other.attachedRigidbody);
            }
        }

        private void LaunchRigidbody(Rigidbody rb)
        {
            Vector3 vel = rb.velocity;
            if (vel.y < _bounceVelocity * 0.7f)
            {
                rb.velocity = new Vector3(vel.x, _bounceVelocity, vel.z);
                _springTimer = 1f;

                if (_bounceClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                {
                    audio.PlayClip(_bounceClip, transform.position, 0.85f, Random.Range(0.95f, 1.15f));
                }

                if (ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shaker))
                {
                    shaker.AddTrauma(0.22f);
                }
            }
        }
    }
}
