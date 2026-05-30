using System;
using Gamelib.ObjectPool.Runtime;
using UnityEngine.UIElements;

namespace Gamelib.ObjectPool.Editor
{
    public class PoolItemView
    {
        private Label _nameLabel;
        private Label _warningLabel;
        private Button _deleteButton;
        private VisualElement _rootElement;
        
        public event Action<PoolItemView> OnDeleteEvent;
        public event Action<PoolItemView> OnSelectEvent;

        public string Name
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }
        
        public PoolItemSo TargetItem { get; private set; }

        public bool IsActive
        {
            get => _rootElement.ClassListContains("active");
            set => _rootElement.EnableInClassList("active", value);
        }
        
        public bool IsEmpty
        {
            get => _warningLabel.ClassListContains("on");
            set => _warningLabel.EnableInClassList("on", value);
        }

        public PoolItemView(VisualElement rootElement, PoolItemSo targetItem)
        {
            TargetItem = targetItem;
            _rootElement = rootElement.Q("PoolItem");
            _nameLabel = _rootElement.Q<Label>("ItemName");
            _deleteButton = _rootElement.Q<Button>("DeleteBtn");
            _warningLabel = _rootElement.Q<Label>("WarningLabel");
            
            _deleteButton.RegisterCallback<ClickEvent>(evt =>
            {
                OnDeleteEvent?.Invoke(this);
                evt.StopPropagation();
            });
            
            _rootElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnSelectEvent?.Invoke(this);
                evt.StopPropagation();
            });
        }
    }
}