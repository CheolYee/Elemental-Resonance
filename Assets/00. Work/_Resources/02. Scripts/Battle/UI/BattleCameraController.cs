using Battle.Enums;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleCameraController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;

        [Header("Virtual Cameras")]
        [SerializeField] private GameObject defaultCamera;
        [SerializeField] private GameObject targetingCamera;

        [Header("Foreground Overlay Camera")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera characterOverlayCamera;

        private int _originalCullingMask;

        private void Awake()
        {
            if (mainCamera != null)
                _originalCullingMask = mainCamera.cullingMask;
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void OnTargetingStart(CardTargetingStartEvent evt)
        {
            defaultCamera.SetActive(false);
            targetingCamera.SetActive(true);
            ApplyForeground(evt.CardInstance?.data?.targetType ?? CardTargetType.None);
        }

        private void OnTargetingEnd(CardTargetingEndEvent _)
        {
            targetingCamera.SetActive(false);
            defaultCamera.SetActive(true);
            ClearForeground();
        }

        private void ApplyForeground(CardTargetType targetType)
        {
            if (mainCamera == null || characterOverlayCamera == null) return;

            int foregroundLayer = targetType switch
            {
                CardTargetType.SingleEnemy => 1 << LayerMask.NameToLayer("Enemy"),
                CardTargetType.SingleAlly  => 1 << LayerMask.NameToLayer("Player"),
                CardTargetType.AllEnemies  => 1 << LayerMask.NameToLayer("Enemy"),
                _                          => 0
            };

            // Base camera에서 해당 레이어 제외 → 이중 렌더 방지
            mainCamera.cullingMask = _originalCullingMask & ~foregroundLayer;

            // Overlay camera는 해당 레이어만 렌더 → Canvas 이후에 합성
            characterOverlayCamera.cullingMask = foregroundLayer;
            characterOverlayCamera.gameObject.SetActive(foregroundLayer != 0);
        }

        private void ClearForeground()
        {
            if (mainCamera == null || characterOverlayCamera == null) return;

            mainCamera.cullingMask = _originalCullingMask;
            characterOverlayCamera.cullingMask = 0;
            characterOverlayCamera.gameObject.SetActive(false);
        }

    }
}
