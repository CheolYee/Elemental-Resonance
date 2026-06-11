using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 모듈의 기본 정의를 담는 SO 베이스 클래스입니다.
    /// 각 모듈은 대사 진행 타이밍, 차단성 여부, 건너뛸 수 있는지 여부,
    /// 자동 진행에 영향을 미치는지 여부 등의 공통 속성을 가집니다.
    /// </summary>
    public abstract class StoryModuleSO : ScriptableObject
    {
        [Header("Flow")]
        [SerializeField] private StoryModuleTiming timing = StoryModuleTiming.BeforeDialogue;
        //스토리 대사 진행 타이밍을 지정하는 열거형입니다.
        //이 모듈이 대사 진행 전에 실행될지, 대사와 함께 실행될지, 대사 후에 실행될지를 결정합니다.
        [SerializeField] private bool isBlocking = false;
        //이 모듈이 스토리 진행을 차단하는지 여부를 나타내는 불리언입니다.
        //차단성 모듈이 실행되는 동안에는 플레이어가 대사 진행을 수동으로 진행할 수 없습니다.
        [SerializeField] private bool canSkip = true;
        //플레이어가 이 모듈을 건너뛸 수 있는지 여부를 나타내는 불리언입니다.
        [SerializeField] private bool affectsAutoAdvance = false;
        //이 모듈이 스토리의 자동 진행에 영향을 미치는지 여부를 나타내는 불리언입니다.
        //자동 진행에 영향을 미치는 모듈이 실행되는 동안에는 스토리가 자동으로 다음 대사로 넘어가지 않습니다.

        //각 필드에 대한 공개 읽기 전용 프로퍼티입니다.
        public virtual StoryModuleTiming Timing => timing;
        public bool IsBlocking => isBlocking;
        public bool CanSkip => canSkip;
        public bool AffectsAutoAdvance => affectsAutoAdvance;

        //모듈의 이름을 반환하는 가상 프로퍼티입니다.
        public virtual string DisplayName => GetType().Name;
    }
}