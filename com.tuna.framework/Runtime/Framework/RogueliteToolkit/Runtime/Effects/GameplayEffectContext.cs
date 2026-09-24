using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace RogueliteToolkit.Effects
{
    [MovedFrom(true, "RogueliteToolkit.Cards", null, "CardEffectContext")]
    public class GameplayEffectContext
    {
        private readonly Dictionary<Type, object> _services = new();

        public GameplayEffectContext Fork()
        {
            GameplayEffectContext copy = new();
            foreach (var entry in _services)
                copy._services.Add(entry.Key, entry.Value);
            return copy;
        }

        public GameplayEffectContext Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
            return this;
        }

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object value) && value is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public T GetRequired<T>() where T : class
        {
            if (TryGet(out T service))
                return service;
            throw new InvalidOperationException(
                $"Gameplay effect context does not contain a service of type {typeof(T).Name}.");
        }
    }
}