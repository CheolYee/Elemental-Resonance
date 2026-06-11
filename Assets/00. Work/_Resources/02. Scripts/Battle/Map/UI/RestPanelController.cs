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
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private Button _exitButton;

        public event Action OnExited;

        private void Awake()
        {
            _exitButton.onClick.AddListener(() => OnExited?.Invoke());
            _panelRoot.SetActive(false);
            if (_camera != null) _camera.Priority = 0;
        }

        public void Open(RestContentSO content)
        {
            if (content != null)
            {
                _titleText.text = content.displayName;
                _descText.text  = content.descriptionText;
            }
            _panelRoot.SetActive(true);
            if (_camera != null) _camera.Priority = 20;
        }

        public void Close()
        {
            _panelRoot.SetActive(false);
            if (_camera != null) _camera.Priority = 0;
        }
    }
}
