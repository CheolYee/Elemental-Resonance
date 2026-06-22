using System;
using Battle.Map.Events;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class RunClearPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _returnButton;
        [SerializeField] private float _fadeInDuration = 0.6f;

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);

            if (_returnButton != null)
                _returnButton.onClick.AddListener(OnReturnClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            ShowAsync().Forget();
        }

        private async UniTaskVoid ShowAsync()
        {
            await LMotion.Create(0f, 1f, _fadeInDuration)
                .WithEase(Ease.OutCubic)
                .Bind(a => _canvasGroup.alpha = a)
                .ToUniTask(destroyCancellationToken);

            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void OnReturnClicked()
        {
            FadeManager.Instance?.GoToScene("Title");
        }
    }
}
