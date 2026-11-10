using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gimersia.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>(16);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Services.Clear();
        }

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Attempted to register null service for type {typeof(T).Name}.");
                return;
            }

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (Services.TryGetValue(type, out object existing) && ReferenceEquals(existing, service))
            {
                Services.Remove(type);
            }
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            Debug.LogError($"[ServiceLocator] Service of type {typeof(T).Name} is not registered.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object raw))
            {
                service = (T)raw;
                return true;
            }

            service = null;
            return false;
        }
    }
}
