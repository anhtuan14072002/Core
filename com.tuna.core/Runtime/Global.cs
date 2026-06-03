using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [DefaultExecutionOrder(-1)]
    public class Global : MonoBehaviour
    {
        [SerializeField] private List<Manager> _managers;
        private static Signal _signal;
        public static Signal Signal => _signal;
        private void Awake()
        {
            _signal = new Signal();
            foreach (var manager in _managers)
            {
                Add(manager);
                manager.Setup();
            }
        }

        private void Start()
        {
            foreach (var manager in _managers)
                manager.Run();
        }

        private void FixedUpdate()
        {
            float fdt = Time.fixedDeltaTime;
            foreach (var manager in _managers)
                manager.FixedTick(fdt);
            foreach (var manager in _managers)
                manager.LateFixedTick();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var manager in _managers)
                manager.UpdateTick(dt);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (var manager in _managers)
                manager.LateUpdateTick(dt);
        }

        private void OnDestroy()
        {
            _signal.Dispose();
        }

        public static void Send<T>(in T signal)
        {
            _signal.Send(signal);
        }
        
        public static void Add(object obj)
        {
            var type = obj.GetType();
            var interfaces = type.GetInterfaces();
            foreach (Type t in interfaces)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReceive<>))
                    _signal.Add(obj as IReceive, t.GetGenericArguments()[0]);
            }
        }

        public static void Remove(object obj)
        {
            var interfaces = obj.GetType().GetInterfaces();
            foreach (Type type in interfaces)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReceive<>))
                    _signal.Remove(obj as IReceive, type.GetGenericArguments()[0]);
            }
        }
    }

}