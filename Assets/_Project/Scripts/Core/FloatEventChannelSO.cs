using System;
using UnityEngine;

namespace Gimersia.Core
{
    [CreateAssetMenu(fileName = "EC_FloatEvent", menuName = "Gimersia/Events/Float Event Channel")]
    public sealed class FloatEventChannelSO : ScriptableObject
    {
        public event Action<float> OnEventRaised;

        public void RaiseEvent(float value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
