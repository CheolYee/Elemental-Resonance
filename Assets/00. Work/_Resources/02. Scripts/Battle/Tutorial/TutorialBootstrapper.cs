using System.Threading;
using Battle.Data;
using Battle.Map.Data;
using Battle.Map.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Battle.Tutorial
{
    // Main 씬에 상시 활성 상태로 배치. tutorial_pending 플래그를 감지해 튜토리얼 모드를 초기화.
    // Awake: MapFlowController.Start() 이전에 맵 그래프 교체 + 튜토리얼 덱을 활성 SO에 복사.
    // Start: NextFrame 대기 후 맵 오버레이 열기 + TutorialController 시작.
    public class TutorialBootstrapper : MonoBehaviour
    {
        [SerializeField] private MapFlowController  _mapFlowController;
        [SerializeField] private TutorialController _tutorialController;
        [SerializeField] private MapGraphSO         _tutorialMapGraph;

        [Header("덱")]
        [SerializeField] private TempStartCardSO _tutorialDeckSO;  // 튜토리얼 전용 SO (원본)
        [SerializeField] private TempStartCardSO _activeDeckSO;    // Main 씬 DeckController가 참조하는 SO

        [Header("런 상태")]
        [SerializeField] private PlayerRunStateSO _playerRunState;

        private const string KeyTutorialPending = "tutorial_pending";

        private bool _pending;

        private void Awake()
        {
            _pending = PlayerPrefs.GetInt(KeyTutorialPending, 0) == 1;
            if (!_pending) return;

            // 이전 런의 골드·CurrentPile 스테일 데이터를 제거한 뒤 덱·맵을 세팅한다
            _playerRunState?.Reset();

            _mapFlowController?.OverrideMapGraph(_tutorialMapGraph);

            if (_tutorialDeckSO != null && _activeDeckSO != null)
                _activeDeckSO.SetDeck(_tutorialDeckSO.GetDeck());
        }

        private void Start()
        {
            if (!_pending) return;
            PlayerPrefs.DeleteKey(KeyTutorialPending);
            PlayerPrefs.Save();
            LaunchAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid LaunchAsync(CancellationToken ct)
        {
            // 모든 Start() 완료 후 맵 열기
            await UniTask.NextFrame(ct);
            _mapFlowController?.OpenMapOverlay();
            _tutorialController?.StartTutorial();
        }
    }
}
