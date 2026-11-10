using UnityEngine;
using UnityEngine.Audio;

namespace Gimersia.Audio
{
    [CreateAssetMenu(fileName = "AC_AudioCue", menuName = "Gimersia/Audio/Audio Cue")]
    public sealed class AudioCueSO : ScriptableObject
    {
        [field: SerializeField] public AudioClip[] Clips { get; private set; }
        [field: SerializeField] public AudioMixerGroup OutputGroup { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float MinVolume { get; private set; } = 0.92f;
        [field: SerializeField, Range(0f, 1f)] public float MaxVolume { get; private set; } = 1.00f;
        [field: SerializeField, Range(0.5f, 1.5f)] public float MinPitch { get; private set; } = 0.94f;
        [field: SerializeField, Range(0.5f, 1.5f)] public float MaxPitch { get; private set; } = 1.06f;
        [field: SerializeField, Range(0f, 1f)] public float SpatialBlend { get; private set; } = 0f;

        public AudioClip GetRandomClip()
        {
            if (Clips == null || Clips.Length == 0)
            {
                return null;
            }

            return Clips[Random.Range(0, Clips.Length)];
        }
    }
}
