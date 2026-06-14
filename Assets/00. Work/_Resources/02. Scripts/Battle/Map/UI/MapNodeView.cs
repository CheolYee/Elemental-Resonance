using System;
using Battle.Map.Enums;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _pulseRingImage;   // 노드 선택 시 원형 드로잉 애니메이션
        [SerializeField] private Image _currentRingImage; // Visited/Current 정적 링

        [SerializeField] private Button _button;

        [Header("Hover")]
        [SerializeField] private float _hoverScale    = 1.18f;
        [SerializeField] private float _hoverDuration = 0.15f;

        [Header("Ring Animation")]
        [SerializeField] private float _ringDuration = 0.35f;
        [SerializeField] private float _ringMaxScale = 1.15f;
        [SerializeField] private Image.Origin360 _ringFillOrigin = Image.Origin360.Top;

        private string _nodeId;
        private Color _baseColor = Color.white;
        private MapNodeVisualState _currentVisualState;

        private MotionHandle _ringFillHandle;
        private MotionHandle _ringScaleHandle;
        private MotionHandle _hoverScaleHandle;
        private MotionHandle _hoverAlphaHandle;

        public string NodeId => _nodeId;

        public event Action<string> OnClicked;

        private void Awake()
        {
            _pulseRingImage.type       = Image.Type.Filled;
            _pulseRingImage.fillMethod = Image.FillMethod.Radial360;
            _pulseRingImage.fillOrigin = (int)_ringFillOrigin;
            _pulseRingImage.fillAmount = 0f;
            _pulseRingImage.gameObject.SetActive(false);
            _currentRingImage.gameObject.SetActive(false);
            _button.onClick.AddListener(() => OnClicked?.Invoke(_nodeId));
        }

        private void OnDestroy()
        {
            CancelAllHandles();
        }

        public void Setup(string nodeId, Sprite icon, Color baseColor, MapNodeVisualState visualState, bool isInteractable)
        {
            _nodeId = nodeId;
            _iconImage.sprite = icon;
            _baseColor = baseColor;
            _button.interactable = isInteractable;
            ApplyVisualState(visualState);
        }

        public void SetInteractable(bool interactable) => _button.interactable = interactable;

        public void MarkTransitionSelected() => ApplyVisualState(MapNodeVisualState.TransitionSelected);

        private void ApplyVisualState(MapNodeVisualState state)
        {
            _currentVisualState = state;
            CancelAllHandles();
            transform.localScale = Vector3.one;
            _pulseRingImage.gameObject.SetActive(false);
            _pulseRingImage.fillAmount = 0f;
            _pulseRingImage.transform.localScale = Vector3.one;
            _currentRingImage.gameObject.SetActive(false);

            switch (state)
            {
                case MapNodeVisualState.Locked:
                    _iconImage.color = WithAlpha(_baseColor, 0.25f);
                    break;

                case MapNodeVisualState.Selectable:
                    _iconImage.color = WithAlpha(_baseColor, 0.6f);
                    break;

                case MapNodeVisualState.Visited:
                    _iconImage.color = WithAlpha(_baseColor, 1f);
                    _currentRingImage.gameObject.SetActive(true);
                    break;

                case MapNodeVisualState.Current:
                    _iconImage.color = WithAlpha(_baseColor, 1f);
                    _currentRingImage.gameObject.SetActive(true);
                    break;

                case MapNodeVisualState.TransitionSelected:
                    _iconImage.color = WithAlpha(_baseColor, 1f);
                    _pulseRingImage.gameObject.SetActive(true);
                    _ringFillHandle = LMotion.Create(0f, 1f, _ringDuration)
                        .WithEase(Ease.OutCubic)
                        .Bind(v => _pulseRingImage.fillAmount = v);
                    _ringScaleHandle = LMotion.Create(1f, _ringMaxScale, _ringDuration)
                        .WithEase(Ease.OutCubic)
                        .Bind(s => _pulseRingImage.transform.localScale = new Vector3(s, s, 1f));
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentVisualState != MapNodeVisualState.Selectable) return;
            if (_hoverScaleHandle.IsActive()) _hoverScaleHandle.Cancel();
            if (_hoverAlphaHandle.IsActive()) _hoverAlphaHandle.Cancel();
            _hoverScaleHandle = LMotion.Create(transform.localScale.x, _hoverScale, _hoverDuration)
                .WithEase(Ease.OutCubic)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f));
            _hoverAlphaHandle = LMotion.Create(_iconImage.color.a, 1f, _hoverDuration)
                .Bind(a => _iconImage.color = WithAlpha(_baseColor, a));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentVisualState != MapNodeVisualState.Selectable) return;
            if (_hoverScaleHandle.IsActive()) _hoverScaleHandle.Cancel();
            if (_hoverAlphaHandle.IsActive()) _hoverAlphaHandle.Cancel();
            _hoverScaleHandle = LMotion.Create(transform.localScale.x, 1f, _hoverDuration)
                .WithEase(Ease.OutCubic)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f));
            _hoverAlphaHandle = LMotion.Create(_iconImage.color.a, 0.6f, _hoverDuration)
                .Bind(a => _iconImage.color = WithAlpha(_baseColor, a));
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private void CancelAllHandles()
        {
            if (_ringFillHandle.IsActive()) _ringFillHandle.Cancel();
            if (_ringScaleHandle.IsActive()) _ringScaleHandle.Cancel();
            if (_hoverScaleHandle.IsActive()) _hoverScaleHandle.Cancel();
            if (_hoverAlphaHandle.IsActive()) _hoverAlphaHandle.Cancel();
        }
    }
}
