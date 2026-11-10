using UnityEngine;
using Gimersia.Core;
using Gimersia.Audio;
using Gimersia.Juice;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BreakableStoneBarricade : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _shatterVfx;
        [SerializeField] private AudioClip _shatterClip;
        [SerializeField] private GameObject _visualRoot;

        private Collider _collider;
        private bool _isBroken;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_visualRoot == null)
            {
                _visualRoot = gameObject;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isBroken)
            {
                return;
            }

            if (collision.gameObject.TryGetComponent(out TerraCoreBall ball))
            {
                if (ball.IsSweetSpotCharged || collision.relativeVelocity.sqrMagnitude > 85f)
                {
                    ShatterBarricade();
                }
            }
        }

        public void ShatterBarricade()
        {
            if (_isBroken)
            {
                return;
            }

            _isBroken = true;
            _collider.enabled = false;

            if (_shatterVfx != null)
            {
                _shatterVfx.transform.SetParent(null);
                _shatterVfx.Play();
            }

            if (_shatterClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_shatterClip, transform.position, 1.0f, 0.85f);
            }

            if (ServiceLocator.TryGet<ITimeService>(out ITimeService timeService))
            {
                timeService.TriggerHitStop(0.045f, 0.02f);
            }

            if (ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shaker))
            {
                shaker.AddTrauma(0.6f);
            }

            _visualRoot.SetActive(false);
        }
    }
}
