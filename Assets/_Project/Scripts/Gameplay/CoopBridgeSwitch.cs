using UnityEngine;
using Gimersia.Core;
using Gimersia.Audio;

namespace Gimersia.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CoopBridgeSwitch : MonoBehaviour
    {
        [SerializeField] private Transform _bridgeToExtend;
        [SerializeField] private Vector3 _retractedScale = new Vector3(3.2f, 0.4f, 0.5f);
        [SerializeField] private Vector3 _extendedScale = new Vector3(3.2f, 0.4f, 9.5f);
        [SerializeField] private bool _stayLockedOncePressed = true;
        [SerializeField] private AudioClip _activateClip;

        private int _occupantCount;
        private bool _isActivated;

        private void Awake()
        {
            if (_bridgeToExtend != null)
            {
                _bridgeToExtend.localScale = _retractedScale;
            }
        }

        private void Update()
        {
            if (_bridgeToExtend == null)
            {
                return;
            }

            Vector3 target = _isActivated ? _extendedScale : _retractedScale;
            _bridgeToExtend.localScale = Vector3.Lerp(_bridgeToExtend.localScale, target, 6.5f * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<BeanClimberController>() != null || other.GetComponentInParent<TerraCoreBall>() != null)
            {
                _occupantCount++;
                if (!_isActivated)
                {
                    _isActivated = true;
                    transform.localPosition += Vector3.down * 0.08f;

                    if (_activateClip != null && ServiceLocator.TryGet<IAudioService>(out IAudioService audio))
                    {
                        audio.PlayClip(_activateClip, transform.position, 0.85f, 1.05f);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<BeanClimberController>() != null || other.GetComponentInParent<TerraCoreBall>() != null)
            {
                _occupantCount = Mathf.Max(0, _occupantCount - 1);
                if (_occupantCount == 0 && !_stayLockedOncePressed)
                {
                    _isActivated = false;
                    transform.localPosition += Vector3.up * 0.08f;
                }
            }
        }
    }
}
