using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gamelib.SoundSystem.Editor
{
    public sealed class EnumPickerPopup : PopupWindowContent
    {
        private readonly string[]    _displayNames;
        private readonly int[]       _intValues;
        private readonly Action<int> _onSelected;

        private string  _searchText = "";
        private Vector2 _scrollPos;
        private bool    _needFocus  = true;

        private const float Width      = 280f;
        private const float SearchBarH = 28f;
        private const float ItemHeight = 22f;
        private const float MaxListH   = 300f;

        public EnumPickerPopup(string[] displayNames, int[] intValues, Action<int> onSelected)
        {
            _displayNames = displayNames;
            _intValues    = intValues;
            _onSelected   = onSelected;
        }

        public override Vector2 GetWindowSize() => new Vector2(Width, SearchBarH + MaxListH + 4f);
        public override void OnOpen() => _needFocus = true;

        public override void OnGUI(Rect rect)
        {
            GUI.SetNextControlName("EnumSearch");
            _searchText = EditorGUI.TextField(new Rect(4f, 4f, rect.width - 8f, 20f), _searchText);
            if (_needFocus) { EditorGUI.FocusTextInControl("EnumSearch"); _needFocus = false; }

            var filtered = new List<(string name, int value)>();
            for (int i = 0; i < _displayNames.Length; i++)
            {
                if (string.IsNullOrEmpty(_searchText) ||
                    _displayNames[i].IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    filtered.Add((_displayNames[i], _intValues[i]));
            }

            bool  needScroll = filtered.Count * ItemHeight > MaxListH;
            float contentW   = needScroll ? rect.width - 16f : rect.width;

            _scrollPos = GUI.BeginScrollView(
                new Rect(0f, SearchBarH, rect.width, MaxListH), _scrollPos,
                new Rect(0f, 0f, contentW, filtered.Count * ItemHeight));

            for (int i = 0; i < filtered.Count; i++)
            {
                var itemRect = new Rect(2f, i * ItemHeight, contentW - 4f, ItemHeight);
                if (Event.current.type == EventType.Repaint && itemRect.Contains(Event.current.mousePosition))
                    EditorGUI.DrawRect(itemRect, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                GUI.Label(itemRect, filtered[i].name, EditorStyles.label);
                if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                {
                    _onSelected(filtered[i].value);
                    editorWindow.Close();
                    Event.current.Use();
                }
            }
            GUI.EndScrollView();

            if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { editorWindow.Close(); Event.current.Use(); }
        }
    }
}
