namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        // Phase 7-2에서 구현 예정:
        // - 우클릭 컨텍스트 메뉴 → 노드 추가 (타입별)
        // - 노드 드래그 → xOffset 변경
        // - 포트 드래그 → nextNodeIds 연결 생성
        // - 연결선 클릭 선택 → Delete 키로 삭제
        // - 노드 Delete 키 삭제 → nextNodeIds 참조 자동 정리
        // - Undo.RecordObject 통합
        // - Ctrl+C / Ctrl+V Copy/Paste (새 nodeId, nextNodeIds 초기화, +20px 오프셋)
    }
}
