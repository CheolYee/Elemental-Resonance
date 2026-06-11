using System;
using System.Threading;
using Battle.Map.Data;
using Battle.Map.Enums;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.Map.UI
{
    public class MapInfoPanelPresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descText;

        [SerializeField] private float _fadeDelay    = 1f;
        [SerializeField] private float _fadeDuration = 0.25f;

        private CancellationTokenSource _delayCts;
        private MotionHandle _fadeHandle;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
        }

        public void ShowNode(MapNodeDefinition node, MapNodeVisualState visualState)
        {
            _delayCts?.Cancel();
            _delayCts = null;

            _typeText.text = node.nodeType.ToString();
            _nameText.text = GetDisplayName(node);
            _descText.text = GetDescription(node);

            FadeTo(1f);
        }

        public void HideDelayed()
        {
            _delayCts?.Cancel();
            _delayCts = new CancellationTokenSource();
            HideAfterDelayAsync(_delayCts.Token).Forget();
        }

        private async UniTaskVoid HideAfterDelayAsync(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_fadeDelay), cancellationToken: ct);
            FadeTo(0f);
        }

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(_canvasGroup.alpha, target, _fadeDuration)
                .Bind(a => _canvasGroup.alpha = a);
        }

        private string GetDisplayName(MapNodeDefinition node)
        {
            if (node.stageRef != null) return node.stageRef.name;
            if (node.restContent != null) return node.restContent.displayName;
            if (node.shopContent != null) return node.shopContent.displayName;
            return node.nodeType.ToString();
        }

        private string GetDescription(MapNodeDefinition node)
        {
            if (node.restContent != null) return node.restContent.descriptionText;
            if (node.shopContent != null) return node.shopContent.descriptionText;
            return string.Empty;
        }
    }
}
