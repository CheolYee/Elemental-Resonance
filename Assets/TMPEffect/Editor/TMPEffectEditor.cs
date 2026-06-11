using UnityEditor;
using UnityEngine;

namespace TMProEffect.EditorUtilities
{
    [CustomEditor(typeof(TMPEffect))]
    public class TMPEffectEditor : Editor
    {
        SerializedProperty useVertexColorProp;

        SerializedProperty outlineProp;
        SerializedProperty outlineColorProp;
        SerializedProperty outlineWidthProp;

        SerializedProperty outline2Prop;
        SerializedProperty outlineColor2Prop;
        SerializedProperty outlineWidth2Prop;
        
        SerializedProperty underlayProp;
        SerializedProperty underlayColorProp;
        SerializedProperty shadowAngleProp;
        SerializedProperty shadowDisProp;
        SerializedProperty shadowBlurProp;

        private void OnEnable()
        {
            useVertexColorProp = serializedObject.FindProperty("useVertexColor");

            outlineProp = serializedObject.FindProperty("outline");
            outlineColorProp = serializedObject.FindProperty("outlineColor");
            outlineWidthProp = serializedObject.FindProperty("outlineWidth");

            outline2Prop = serializedObject.FindProperty("outline2");
            outlineColor2Prop = serializedObject.FindProperty("outlineColor2");
            outlineWidth2Prop = serializedObject.FindProperty("outlineWidth2");

            underlayProp = serializedObject.FindProperty("underlay");
            underlayColorProp = serializedObject.FindProperty("underlayColor");
            shadowAngleProp = serializedObject.FindProperty("shadowAngle");
            shadowDisProp = serializedObject.FindProperty("shadowDis");
            shadowBlurProp = serializedObject.FindProperty("shadowBlur");
        }

        public override void OnInspectorGUI()
        {
            TMPEffect effect = (TMPEffect)target;
            if (effect == null || effect.text == null)
            {
                EditorGUILayout.LabelField("No TMP component found.");
                return;
            }

            float scale = effect.text.font.faceInfo.pointSize / effect.text.fontSize / effect.text.font.atlasPadding;
            float maxValue = scale > 0f ? 1f / scale : float.MaxValue;
            EditorGUILayout.HelpBox("Outline width, shadow distance and shadow blur are in pixels. " +
                "The max value is releated to the font size and atlas padding" +
                "\nMax value: " + maxValue.ToString("F2") + " px", MessageType.Info);

            EditorGUILayout.PropertyField(useVertexColorProp);
            EditorGUILayout.PropertyField(outlineProp);
            if (outlineProp.boolValue)
            {
                EditorGUILayout.PropertyField(outlineColorProp);
                EditorGUILayout.PropertyField(outlineWidthProp);
            }
            EditorGUILayout.PropertyField(outline2Prop);
            if (outline2Prop.boolValue)
            {
                EditorGUILayout.PropertyField(outlineColor2Prop);
                EditorGUILayout.PropertyField(outlineWidth2Prop);
            }
            EditorGUILayout.PropertyField(underlayProp);
            if (underlayProp.boolValue)
            {
                EditorGUILayout.PropertyField(underlayColorProp);
                EditorGUILayout.PropertyField(shadowAngleProp);
                EditorGUILayout.PropertyField(shadowDisProp);
                EditorGUILayout.PropertyField(shadowBlurProp);
            }
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh"))
            {
                effect.SetupEffect();
            }
        }
    }
}
