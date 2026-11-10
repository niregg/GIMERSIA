using System;
using UnityEngine;

namespace Gimersia.Core
{
    [CreateAssetMenu(fileName = "EC_IntEvent", menuName = "Gimersia/Events/Int Event Channel")]
    public sealed class IntEventChannelSO : ScriptableObject
    {
        public event Action<int> OnEventRaised;

        public void RaiseEvent(int value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
