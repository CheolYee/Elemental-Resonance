using System;
using System.Collections.Generic;
using Battle.Enums;
using UnityEngine;

namespace Battle.UI
{
    [Serializable]
    public struct ElementIconEntry
    {
        public ElementType elementType;
        public Sprite      icon;
    }

    [CreateAssetMenu(menuName = "Battle/Element Icon Table")]
    public class ElementIconTableSO : ScriptableObject
    {
        [SerializeField] private ElementIconEntry[] _entries;

        private Dictionary<ElementType, Sprite> _map;

        private void OnEnable() => BuildMap();

        private void BuildMap()
        {
            _map = new Dictionary<ElementType, Sprite>();
            if (_entries == null) return;
            foreach (var e in _entries)
                _map[e.elementType] = e.icon;
        }

        public Sprite GetIcon(ElementType type)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(type, out var sprite) ? sprite : null;
        }
    }
}
