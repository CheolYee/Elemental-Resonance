using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Agents.FSM.Editor
{
    [CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            // VisualElement는 커스텀 에디터에서 GameObject같은 녀석이다. 뭐든 담을 수 있는 빈 공간이다.
            
            editorView.CloneTree(root);

            DropdownField dropdownField = root.Q<DropdownField>("ClassDropdownField");
            
            FillDropdownField(dropdownField);
            
            return root;
        }

        private void FillDropdownField(DropdownField dropdownField)
        {
            dropdownField.choices.Clear();

            Assembly mainAssembly = Assembly.GetAssembly(typeof(AgentState));

            List<Type> derivedTypes = mainAssembly.GetTypes()
                .Where(type => type.IsClass 
                       && type.IsAbstract == false 
                       && type.IsSubclassOf(typeof(AgentState)))
                .ToList();

            //FullName => 네임스페이스까지 포함된 이름을 말해.
            dropdownField.choices.AddRange(derivedTypes.Select(type => type.FullName));

            if (dropdownField.choices.Count > 0 && string.IsNullOrEmpty(dropdownField.value))
            {
                dropdownField.SetValueWithoutNotify(derivedTypes[0].FullName);
            }
        }
    }
}