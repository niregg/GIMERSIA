using UnityEngine;
using Gimersia.Core;
using Gimersia.Audio;

namespace Gimersia.Gameplay
{
    public sealed class RestorableFoliage : MonoBehaviour
    {
        [SerializeField] private Renderer[] _targetRenderers;
        [SerializeField] private GameObject _scorchedModelVisual;
        [SerializeField] private GameObject _healthyModelVisual;
        [SerializeField] private ParticleSystem _restoreBurstVfx;
        [SerializeField] private AudioClip _restoreChimeClip;
        [SerializeField] private Color _scorchedColor = new Color(0.18f, 0.17f, 0.19f, 1f);
        [SerializeField] private Color _restoredColor = new Color(0.18f, 0.82f, 0.44f, 1f);
        [SerializeField] private bool _startScorched = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propBlock;
        private bool _isRestored;
        private Vector3 _initialScale;
        private float _popTimer;

        public bool IsRestored => _isRestored;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _initialScale = transform.localScale;

            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                _targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (_startScorched)
            {
                SetVisualState(restored: false);
            }
            else
            {
                _isRestored = true;
                SetVisualState(restored: true);
            }
        }

        private void Update()
        {
            if (_popTimer <= 0f)
            {
                return;
            }

            _popTimer -= Time.deltaTime * 4.2f;
            float t = 1f - Mathf.Clamp01(_popTimer);
            float elastic = 1f + (Mathf.Sin(t * Mathf.PI) * 0.35f);
            transform.localScale = _initialScale * elastic;

            if (_popTimer <= 0f)
            {
                transform.localScale = _initialScale;
            }
        }

        public void RestoreNature()
        {
            if (_isRestored)
            {
                return;
            }

            _isRestored = true;
            _popTimer = 1f;
            SetVisualState(restored: true);

            if (_restoreBurstVfx != null)
            {
                _restoreBurstVfx.Play();
            }

            if (_restoreChimeClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
            {
                audio.PlayClip(_restoreChimeClip, transform.position, 0.55f, Random.Range(0.95f, 1.15f));
            }
        }

        private void SetVisualState(bool restored)
        {
            if (_scorchedModelVisual != null && _healthyModelVisual != null)
            {
                _scorchedModelVisual.SetActive(!restored);
                _healthyModelVisual.SetActive(restored);
            }

            Color targetColor = restored ? _restoredColor : _scorchedColor;
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Renderer r = _targetRenderers[i];
                if (r == null || r is ParticleSystemRenderer)
                {
                    continue;
                }

                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, targetColor);
                _propBlock.SetColor(LegacyColorId, targetColor);
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
