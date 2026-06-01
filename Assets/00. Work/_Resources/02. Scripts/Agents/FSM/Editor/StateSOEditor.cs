using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agents.FSM;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work._Resources._02._Scripts.Agents.FSM.Editor
{
    [CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        private StateSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            _targetData = (StateSO)target; //target은 Editor의 내부 변수이다.
            
            VisualElement root = new VisualElement();
            editorView.CloneTree(root);

            FillDropdownField(root);
            return root;
        }

        private void FillDropdownField(VisualElement root)
        {
            DropdownField field = root.Q<DropdownField>("ClassNameDropdown");

            Assembly stateAssembly = Assembly.GetAssembly(typeof(StateSO));
            IEnumerable<string> choices = stateAssembly.GetTypes()
                    .Where(type => type.IsClass 
                                   && !type.IsAbstract 
                                   && type.IsSubclassOf(typeof(AgentState)))
                    .Select(type => type.FullName);
            
            field.choices.AddRange(choices);

            if (_targetData != null && !string.IsNullOrEmpty(_targetData.className)
                                    && field.choices.Contains(_targetData.className))
            {
                field.value = _targetData.className;
            }
            else if (_targetData != null && field.choices.Count > 0)
            {
                _targetData.className = field.choices.First();
                EditorUtility.SetDirty(_targetData);
            }
            
            AssetDatabase.SaveAssetIfDirty(_targetData);
        }
    }
}