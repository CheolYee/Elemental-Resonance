using System;
using Battle.Map.Data;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class RestPanelController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private Button _exitButton;

        public event Action OnExited;

        private void Awake()
        {
            _exitButton.onClick.AddListener(() => OnExited?.Invoke());
            SetPanelVisible(false);
            if (_camera != null) _camera.Priority = -5;
        }

        public void Open(RestContentSO content)
        {
            if (content != null)
            {
                _titleText.text = content.displayName;
                _descText.text  = content.descriptionText;
            }
            SetPanelVisible(true);
            if (_camera != null) _camera.Priority = 20;
        }

        public void Close()
        {
            SetPanelVisible(false);
            if (_camera != null) _camera.Priority = -5;
        }

        // 나중에 카메라 연출 + LitMotion 페이드로 확장 예정
        private void SetPanelVisible(bool visible)
        {
            _panelGroup.alpha          = visible ? 1f : 0f;
            _panelGroup.interactable   = visible;
            _panelGroup.blocksRaycasts = visible;
        }
    }
}
