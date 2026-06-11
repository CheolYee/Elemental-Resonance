using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryEnterMotionType
    {
        Instant,
        FadeIn,
        SlideFromLeft,
        SlideFromRight,
        ZoomIn,
    }

    public enum StoryCharacterSlot
    {
        Left,
        CenterLeft,
        Center,
        CenterRight,
        Right,
    }

    /// <summary>
    /// 캐릭터 등장 연출 모듈. Slot / Motion / Duration 으로 등장 방식을 정의하며,
    /// 실제 런타임은 CharacterEnterModuleExecutor 가 처리한다.
    /// 프리뷰 패널에서도 이 데이터를 직접 표시한다.
    /// </summary>
    [StoryModuleMetadata("Character Enter", category: "Character", accentColorHex: "#E67E22", sortPriority: 5, isDeprecated: true)]
    [CreateAssetMenu(fileName = "CharacterEnterModule", menuName = "Story/Modules/Character Enter")]
    public sealed class StoryCharacterEnterModuleSO : StoryModuleSO
    {
        [Tooltip("등장할 캐릭터")]
        [SerializeField] private CharacterDefinitionSO character;

        [Tooltip("스테이지 위 배치 슬롯")]
        [SerializeField] private StoryCharacterSlot slot = StoryCharacterSlot.Center;

        [Tooltip("등장 연출 방식")]
        [SerializeField] private StoryEnterMotionType motion = StoryEnterMotionType.FadeIn;

        [Tooltip("연출 재생 시간(초). Instant 일 때는 무시됨.")]
        [SerializeField] [Range(0f, 3f)] private float duration = 0.4f;

        public CharacterDefinitionSO Character => character;
        public StoryCharacterSlot    Slot       => slot;
        public StoryEnterMotionType  Motion     => motion;
        public float                 Duration   => duration;

        public override string DisplayName => "Character Enter";
    }
}
