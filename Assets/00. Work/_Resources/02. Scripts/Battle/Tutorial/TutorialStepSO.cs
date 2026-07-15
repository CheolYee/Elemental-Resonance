using UnityEngine;

namespace Battle.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step")]
    public class TutorialStepSO : ScriptableObject
    {
        [Header("콘텐츠")]
        public string tooltipText;
        [Tooltip("TutorialTarget 컴포넌트의 targetId와 일치해야 함")]
        public string targetId;
        [Tooltip("동일 targetId가 여러 개일 때 sibling 순서 기준으로 몇 번째 오브젝트를 하이라이트할지 (0부터)")]
        public int targetIndex = 0;

        [Header("말풍선 위치")]
        public TooltipAnchor preferredAnchor = TooltipAnchor.Top;
        public Vector2 anchorOffset;

        [Header("완료 조건")]
        public TutorialCompletionType completionType;
        // WaitForEvent일 때 사용. 예: "Battle.Events.CardDroppedOnTargetEvent"
        public string completionEventTypeName;

        [Header("입력 제어")]
        // true면 ClickToContinue 전용 — overlay가 레이캐스트를 막아 하이라이트 외 입력 차단
        public bool blockAllInput = true;
    }
}
