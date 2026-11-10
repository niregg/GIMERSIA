using UnityEngine;
using UnityEngine.Pool;
using Gimersia.Core;

namespace Gimersia.Audio
{
    public interface IAudioService
    {
        void PlayCue(AudioCueSO cue, Vector3 position);
        void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f);
    }

    public sealed class AudioManager : MonoBehaviour, IAudioService
    {
        [SerializeField] private int _defaultPoolCapacity = 16;
        [SerializeField] private int _maxPoolSize = 48;

        private ObjectPool<AudioSource> _sourcePool;

        private void Awake()
        {
            _sourcePool = new ObjectPool<AudioSource>(
                createFunc: CreatePooledSource,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: false,
                defaultCapacity: _defaultPoolCapacity,
                maxSize: _maxPoolSize
            );

            ServiceLocator.Register<IAudioService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IAudioService>(this);
        }

        public void PlayCue(AudioCueSO cue, Vector3 position)
        {
            if (cue == null)
            {
                return;
            }

            AudioClip clip = cue.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            AudioSource source = _sourcePool.Get();
            source.transform.position = position;
            source.clip = clip;
            source.outputAudioMixerGroup = cue.OutputGroup;
            source.volume = Random.Range(cue.MinVolume, cue.MaxVolume);
            source.pitch = Random.Range(cue.MinPitch, cue.MaxPitch);
            source.spatialBlend = cue.SpatialBlend;
            source.Play();

            PooledEmitterReturner returner = source.GetComponent<PooledEmitterReturner>();
            returner.Arm(clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch)), _sourcePool, source);
        }

        public void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = _sourcePool.Get();
            source.transform.position = position;
            source.clip = clip;
            source.outputAudioMixerGroup = null;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            source.spatialBlend = 0f;
            source.Play();

            PooledEmitterReturner returner = source.GetComponent<PooledEmitterReturner>();
            returner.Arm(clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch)), _sourcePool, source);
        }

        private AudioSource CreatePooledSource()
        {
            GameObject go = new GameObject("PooledAudioEmitter");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            go.AddComponent<PooledEmitterReturner>();
            return source;
        }

        private static void OnTakeFromPool(AudioSource source)
        {
            source.gameObject.SetActive(true);
        }

        private static void OnReturnedToPool(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
        }

        private static void OnDestroyPoolObject(AudioSource source)
        {
            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }
    }

    public sealed class PooledEmitterReturner : MonoBehaviour
    {
        private float _timer;
        private bool _armed;
        private IObjectPool<AudioSource> _pool;
        private AudioSource _source;

        public void Arm(float duration, IObjectPool<AudioSource> pool, AudioSource source)
        {
            _timer = duration;
            _pool = pool;
            _source = source;
            _armed = true;
        }

        private void Update()
        {
            if (!_armed)
            {
                return;
            }

            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                _armed = false;
                _pool.Release(_source);
            }
        }
    }
}
