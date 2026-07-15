using UnityEngine;

namespace Battle.Tutorial
{
    // 튜토리얼 하이라이트 대상 마커. 오브젝트에 부착 후 targetId를 입력.
    // 태그 등록 불필요. 동일 ID를 여러 오브젝트에 붙이면 첫 번째 활성 오브젝트가 사용됨.
    public class TutorialTarget : MonoBehaviour
    {
        [SerializeField] public string targetId;
    }
}
