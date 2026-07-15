using Battle.Fusion;
using Battle.UI;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace DeckBuilding
{
    public class FusionRecipePanel : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private FusionRecipeTableSO _recipeTable;
        [SerializeField] private ElementIconTableSO  _iconTable;

        [Header("UI")]
        [SerializeField] private GameObject          _panelRoot;
        [SerializeField] private CanvasGroup         _canvasGroup;
        [SerializeField] private Transform           _rowContainer;
        [SerializeField] private FusionRecipeRowView _rowPrefab;
        [SerializeField] private Button              _toggleButton;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.2f;

        private MotionHandle _fadeHandle;
        private bool _isVisible;

        private void Awake()
        {
            BuildRows();
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            _panelRoot.SetActive(false);
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.RemoveListener(Toggle);
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        }

        private void BuildRows()
        {
            if (_recipeTable == null || _rowPrefab == null) return;
            foreach (var recipe in _recipeTable.recipes)
            {
                var row = Instantiate(_rowPrefab, _rowContainer);
                row.Setup(recipe, _iconTable);
            }
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            FadeTo(_isVisible ? 1f : 0f);
        }

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();

            if (target > 0f)
                _panelRoot.SetActive(true);

            _canvasGroup.interactable   = target > 0f;
            _canvasGroup.blocksRaycasts = target > 0f;

            _fadeHandle = LMotion.Create(_canvasGroup.alpha, target, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    if (this == null) return;
                    if (target <= 0f) _panelRoot.SetActive(false);
                })
                .Bind(a => { if (this != null) _canvasGroup.alpha = a; });
        }
    }
}
