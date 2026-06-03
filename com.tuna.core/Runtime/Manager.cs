using System;
using UnityEngine;

namespace Core
{
    public abstract class Manager : MonoBehaviour
    {
        public virtual void Setup(){}
        public virtual void Run(){}
        public virtual void FixedTick(float fdt){}
        public virtual void LateFixedTick(){}
        public virtual void UpdateTick(float dt){}
        public virtual void LateUpdateTick(float dt){}
    }
}