using System;
using UnityEditor;
using UnityEngine;

namespace Gamelib.SoundSystem.Editor
{
    // 제네릭 인스턴스마다 static 필드가 독립 → 타입별 캐시 자동 분리
    public abstract class EnumSearchDrawerBase<T> : PropertyDrawer where T : Enum
    {
        private static int[]       _cachedIntValues;
        private static string[]    _cachedDisplayNames;
        private static SoundListSo _cachedSoundList;
        private static bool        _isCached;

        private void EnsureCache()
        {
            if (_isCached) return;
            var values = (T[])Enum.GetValues(typeof(T));
            _cachedIntValues = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
                _cachedIntValues[i] = Convert.ToInt32(values[i]);
            _cachedSoundList    = GetSoundList();
            _cachedDisplayNames = BuildDisplayNames(values, _cachedSoundList);
            _isCached = true;
        }

        protected virtual SoundListSo GetSoundList()                          => null;
        protected virtual bool        HasPreview                              => false;

        protected virtual string[] BuildDisplayNames(T[] values, SoundListSo list)
        {
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = values[i].ToString();
            return names;
        }

        protected virtual void   OnPreviewClicked(int intValue, SoundListSo soundList) { }
        protected virtual string GetPreviewLabel(int currentVal)                      => "▶";
        protected virtual float  GetExtraHeight()                                     => 0f;
        protected virtual void   DrawExtras(Rect rect, int currentIntValue, SoundListSo soundList) { }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight + GetExtraHeight();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            int    currentVal  = property.intValue;
            int    idx         = Array.IndexOf(_cachedIntValues, currentVal);
            string displayName = idx >= 0 ? _cachedDisplayNames[idx] : currentVal.ToString();

            EditorGUI.BeginProperty(position, label, property);

            var firstLine   = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var buttonRect  = EditorGUI.PrefixLabel(firstLine, label);
            float previewW  = HasPreview ? 26f : 0f;
            var   pickerRect = HasPreview
                ? new Rect(buttonRect.x, buttonRect.y, buttonRect.width - previewW - 2f, buttonRect.height)
                : buttonRect;

            if (GUI.Button(pickerRect, displayName, EditorStyles.popup))
            {
                var captured = property.Copy();
                var popup = new EnumPickerPopup(_cachedDisplayNames, _cachedIntValues, selected =>
                {
                    captured.intValue = selected;
                    captured.serializedObject.ApplyModifiedProperties();
                });
                PopupWindow.Show(pickerRect, popup);
            }

            if (HasPreview)
            {
                var previewRect = new Rect(pickerRect.xMax + 2f, buttonRect.y, previewW, buttonRect.height);
                if (GUI.Button(previewRect, GetPreviewLabel(currentVal)))
                    OnPreviewClicked(currentVal, _cachedSoundList);
            }

            float extraH = GetExtraHeight();
            if (extraH > 0f)
            {
                var extraRect = new Rect(position.x, firstLine.yMax + 2f, position.width, extraH - 2f);
                DrawExtras(extraRect, currentVal, _cachedSoundList);
            }

            EditorGUI.EndProperty();
        }
    }
}
