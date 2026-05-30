using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Modules
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        protected Dictionary<Type, IModule> moduleDict;

        protected virtual void Awake()
        {
            moduleDict = GetComponentsInChildren<IModule>().ToDictionary(module => module.GetType());
            InitializeComponents();
            AfterInitComponents();
        }
        
        protected virtual void InitializeComponents()
        {
            foreach (IModule module in moduleDict.Values)    
            {
                module.Initialize(this);
            }    
        }
        
        protected virtual void AfterInitComponents()
        {
            foreach (IAfterInitModule afterInitModule in moduleDict.Values.OfType<IAfterInitModule>())
            {
                afterInitModule.AfterInit();
            }
        }

        public T GetModule<T>()
        {
            if (moduleDict.TryGetValue(typeof(T), out IModule module))
            {
                return (T)module;
            }
            
            IModule findModule = moduleDict.Values.FirstOrDefault(moduleType => moduleType is T);
            if(findModule is T castedModule)
            {
                return castedModule;
            }
            return default;
        }
    }
}