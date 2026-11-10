using UnityEngine;
using Gimersia.Core;
using Gimersia.Audio;
using Gimersia.Juice;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class MountainCheckpointCamp : MonoBehaviour
    {
        [SerializeField] private int _zoneIndex = 1;
        [SerializeField] private string _zoneName = "Basecamp Lembah";
        [SerializeField] private Light _campfireLight;
        [SerializeField] private ParticleSystem _campfireFlameVfx;
        [SerializeField] private AudioClip _checkpointActivateClip;

        private bool _isUnlocked;

        public int ZoneIndex => _zoneIndex;
        public string ZoneName => _zoneName;
        public bool IsUnlocked => _isUnlocked;

        private void Awake()
        {
            if (_campfireLight != null)
            {
                _campfireLight.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            BeanClimberController climber = other.GetComponentInParent<BeanClimberController>();
            TerraCoreBall ball = other.GetComponentInParent<TerraCoreBall>();

            if (climber == null && ball == null)
            {
                return;
            }

            DualClimberOrchestrator orchestrator = FindFirstObjectByType<DualClimberOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.RegisterCheckpoint(transform.position + (Vector3.up * 1.1f), _zoneIndex, _zoneName);
            }

            if (!_isUnlocked)
            {
                _isUnlocked = true;
                if (_campfireLight != null)
                {
                    _campfireLight.enabled = true;
                }

                if (_campfireFlameVfx != null)
                {
                    _campfireFlameVfx.Play();
                }

                if (_checkpointActivateClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                {
                    audio.PlayClip(_checkpointActivateClip, transform.position, 0.9f, 1.05f);
                }
            }
        }
    }

    [RequireComponent(typeof(SphereCollider))]
    public sealed class SummitAltarGoal : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _victoryFinaleVfx;
        [SerializeField] private Light _altarBeaconLight;
        [SerializeField] private AudioClip _victoryChimeClip;

        private bool _isCompleted;

        public bool IsCompleted => _isCompleted;

        private void OnTriggerEnter(Collider other)
        {
            if (_isCompleted)
            {
                return;
            }

            TerraCoreBall ball = other.GetComponentInParent<TerraCoreBall>();
            BeanClimberController climber = other.GetComponentInParent<BeanClimberController>();

            bool hasBall = ball != null || (climber != null && climber.HasBall);
            if (!hasBall)
            {
                return;
            }

            _isCompleted = true;

            if (_altarBeaconLight != null)
            {
                _altarBeaconLight.intensity = 10f;
                _altarBeaconLight.range = 35f;
            }

            if (_victoryFinaleVfx != null)
            {
                _victoryFinaleVfx.Play();
            }

            if (_victoryChimeClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_victoryChimeClip, transform.position, 1.0f, 1.0f);
            }

            if (ServiceLocator.TryGet<ICameraShakeService>(out ICameraShakeService shaker))
            {
                shaker.AddTrauma(0.65f);
            }

            RestorableFoliage[] allFlora = FindObjectsByType<RestorableFoliage>(FindObjectsSortMode.None);
            for (int i = 0; i < allFlora.Length; i++)
            {
                allFlora[i].RestoreNature();
            }

            DualClimberOrchestrator orchestrator = FindFirstObjectByType<DualClimberOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.TriggerSummitVictory();
            }
        }
    }
}
