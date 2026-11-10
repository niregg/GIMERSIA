using UnityEngine;
using Gimersia.Core;

namespace Gimersia.Juice
{
    public interface ITimeService
    {
        bool IsPaused { get; }
        void SetPaused(bool paused);
        void TriggerHitStop(float duration, float timeScale = 0.02f);
        void StartSlowMotion(float slowTimeScale, float maxDuration);
        void EndSlowMotion();
    }

    public sealed class TimeDilationManager : MonoBehaviour, ITimeService
    {
        private bool _isPaused;
        private bool _isHitStopping;
        private bool _isSlowMotion;

        private float _hitStopTimer;
        private float _hitStopScale = 0.02f;

        private float _slowMoTimer;
        private float _slowMoScale = 0.25f;
        private float _defaultFixedDeltaTime;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
            ServiceLocator.Register<ITimeService>(this);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;
            ServiceLocator.Unregister<ITimeService>(this);
        }

        private void Update()
        {
            if (_isPaused)
            {
                return;
            }

            float unscaledDt = Time.unscaledDeltaTime;

            if (_isHitStopping)
            {
                _hitStopTimer -= unscaledDt;
                if (_hitStopTimer <= 0f)
                {
                    _isHitStopping = false;
                    ApplyCurrentTimeScale();
                }
            }

            if (_isSlowMotion && !_isHitStopping)
            {
                _slowMoTimer -= unscaledDt;
                if (_slowMoTimer <= 0f)
                {
                    _isSlowMotion = false;
                    ApplyCurrentTimeScale();
                }
            }
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            ApplyCurrentTimeScale();
        }

        public void TriggerHitStop(float duration, float timeScale = 0.02f)
        {
            if (_isPaused)
            {
                return;
            }

            _isHitStopping = true;
            _hitStopTimer = Mathf.Max(_hitStopTimer, duration);
            _hitStopScale = Mathf.Clamp(timeScale, 0f, 1f);
            ApplyCurrentTimeScale();
        }

        public void StartSlowMotion(float slowTimeScale, float maxDuration)
        {
            if (_isPaused)
            {
                return;
            }

            _isSlowMotion = true;
            _slowMoScale = Mathf.Clamp(slowTimeScale, 0.05f, 1f);
            _slowMoTimer = maxDuration;
            ApplyCurrentTimeScale();
        }

        public void EndSlowMotion()
        {
            if (!_isSlowMotion)
            {
                return;
            }

            _isSlowMotion = false;
            _slowMoTimer = 0f;
            ApplyCurrentTimeScale();
        }

        private void ApplyCurrentTimeScale()
        {
            if (_isPaused)
            {
                Time.timeScale = 0f;
                return;
            }

            if (_isHitStopping)
            {
                Time.timeScale = _hitStopScale;
                Time.fixedDeltaTime = _defaultFixedDeltaTime * Mathf.Max(_hitStopScale, 0.01f);
                return;
            }

            if (_isSlowMotion)
            {
                Time.timeScale = _slowMoScale;
                Time.fixedDeltaTime = _defaultFixedDeltaTime * _slowMoScale;
                return;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;
        }
    }
}
