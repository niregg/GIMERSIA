using System;
using UnityEngine;

namespace Gimersia.Core
{
    [CreateAssetMenu(fileName = "EC_VoidEvent", menuName = "Gimersia/Events/Void Event Channel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        public event Action OnEventRaised;

        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }
}
