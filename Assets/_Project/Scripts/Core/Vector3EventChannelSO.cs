using System;
using UnityEngine;

namespace Gimersia.Core
{
    [CreateAssetMenu(fileName = "EC_Vector3Event", menuName = "Gimersia/Events/Vector3 Event Channel")]
    public sealed class Vector3EventChannelSO : ScriptableObject
    {
        public event Action<Vector3> OnEventRaised;

        public void RaiseEvent(Vector3 value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
