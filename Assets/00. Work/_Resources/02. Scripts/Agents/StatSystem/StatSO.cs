using System;
using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Systems.Database;
using UnityEngine;

namespace Agents.StatSystem
{
    [CreateAssetMenu(fileName = "New Stat", menuName = "Stat/Stat data", order = 5)]
    public class StatSO : IndexedAsset, ICloneable
    {
        public delegate void ValueChangeHandler(StatSO stat, float current, float previous);
        public event ValueChangeHandler OnValueChanged;
        
        [field: SerializeField] public string StatName { get; private set; }
        [field: SerializeField] public Sprite StatIcon { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public bool IsPercent { get; private set; }

        [SerializeField] private float baseValue;
        [SerializeField] private float minValue, maxValue;
        [SerializeField] private string description;
        
        private Dictionary<object, float> _modifyValueByKey = new Dictionary<object, float>();
        private float _modifiedValue;
        
        public float MaxValue => maxValue;
        public float MinValue => minValue;
        public float Value => Mathf.Clamp(baseValue + _modifiedValue, MinValue, MaxValue);
        public bool IsMax => Mathf.Approximately(Value, MaxValue);
        public bool IsMin => Mathf.Approximately(Value, MinValue);
        
        public float BaseValue
        {
            get => baseValue;
            set
            {
                float previous = Value;
                baseValue = Mathf.Clamp(value, MinValue, MaxValue);
                TryInvokeValueChangeEvent(Value, previous);
            }
        }

        public void AddModifier(object key, float value)
        {
            if (_modifyValueByKey.ContainsKey(key)) return; //이미 해당 오브젝트로 버프를 받는 중이라면
            
            float previous = Value;

            _modifiedValue += value;
            _modifyValueByKey.Add(key, value);
            
            TryInvokeValueChangeEvent(Value, previous);
            Debug.Log(Value);
        }

        public void RemoveModifier(object key)
        {
            if (_modifyValueByKey.TryGetValue(key, out float value))
            {
                float previous = Value;
                _modifiedValue -= value;
                _modifyValueByKey.Remove(key);
                TryInvokeValueChangeEvent(Value, previous);
            }
        }

        public void ClearAllModifiers()
        {
            float previous = Value;
            _modifiedValue = 0;
            _modifyValueByKey.Clear();
            TryInvokeValueChangeEvent(Value, previous);
        }

        private void TryInvokeValueChangeEvent(float current, float previous)
        {
            if (Mathf.Approximately(current, previous) == false)
            {
                OnValueChanged?.Invoke(this, current, previous);
            }
        }

        public object Clone()
        {
            return Instantiate(this); //자기자신을 복제해서 리턴해준다.
        }
    }
}