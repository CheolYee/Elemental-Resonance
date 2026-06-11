using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 대사 한 줄의 정의를 담는 ScriptableObject 클래스입니다.
    /// 대사 ID, 화자 캐릭터, 대사 텍스트, 다음 대사 ID, 로그 기록 여부, 탭으로 대사 완료 허용 여부,
    /// 캐릭터 초점 정책, 음성 정보, 자동 진행 설정, 그리고 이 대사에 연결된 모듈들의 리스트 등을 포함합니다.
    /// </summary>
    
    [CreateAssetMenu(fileName = "StoryLine", menuName = "Story/Story Line")]
    public sealed class StoryLineSO : ScriptableObject
    {
        [Header("Identity")]
        //스토리 시스템 내에서 이 대사 한 줄을 고유하게 식별하는 ID입니다.
        [SerializeField] private string lineId;

        [Header("Dialogue")]
        //이 대사를 말하는 화자 캐릭터입니다. 화자가 없는 경우(내레이션 등)에는 null로 설정할 수 있습니다.
        [SerializeField] private CharacterDefinitionSO speaker;
        //스토리 진행 중 이 대사를 말하는 화자의 이름을 대사 텍스트와 함께 표시할 때 사용하는 문자열입니다.
        [SerializeField] private string nameOverride;
        //이 대사의 텍스트입니다. 대사 진행 시 플레이어에게 표시되는 실제 대사 내용입니다.
        [TextArea(3, 8)]
        [SerializeField] private string dialogueText;

        [Header("Flow")]
        //이 대사가 끝난 후 자동으로 진행될 다음 대사의 ID입니다. 이 필드를 사용하여 대사 간의 흐름을 정의할 수 있습니다.
        [SerializeField] private string nextLineId;
        //이 대사가 스토리 로그에 기록될 때 표시할지 여부를 나타내는 불리언입니다. 로그에 기록되는 대사만 플레이어가 나중에 다시 볼 수 있습니다.
        [SerializeField] private bool logVisible = true;
        //플레이어가 이 대사를 탭으로 빠르게 완료할 수 있는지 여부를 나타내는 불리언입니다.
        //탭으로 완료할 수 있는 대사는 플레이어가 대사 텍스트가 모두 표시되기 전에 탭을 눌러
        //대사를 빠르게 진행할 수 있습니다.
        [SerializeField] private bool allowTapToComplete = true;
        //스토리 진행 중 이 대사가 말하는 화자에게 자동으로 초점을 맞출지,
        //현재 초점을 유지할지, 아니면 초점을 제거할지를 결정하는 열거형입니다.
        [SerializeField] private StoryFocusPolicy focusPolicy = StoryFocusPolicy.AutoFocusSpeaker;

        [Header("Voice")]
        //이 대사에 음성 정보가 포함될 때 사용하는 참조 클래스입니다.
        //음성 키, 재생 정책, 로그에서 재생 허용 여부 등을 포함합니다.
        [SerializeField] private StoryVoiceRef voice = new();

        [Header("Auto Advance")]
        //이 대사에 대해 자동 진행 설정을 오버라이드할지 여부를 나타내는 불리언입니다.
        [SerializeField] private bool useAutoAdvanceOverride = false;
        //자동 진행 설정을 오버라이드하는 경우, 이 대사가 표시된 후
        //자동으로 다음 대사로넘어가기까지의 지연 시간을 나타냅니다.
        [SerializeField] private float autoAdvanceDelay = 1.0f;

        [Header("Modules")]
        [HideInInspector]
        [SerializeField] private List<StoryModuleSO> modules = new();

        // 에디터 전용 — 런타임에서 사용하지 않음
        [HideInInspector]
        [SerializeField] private Vector2 editorNodePosition = new(-1f, -1f);
        [HideInInspector]
        [SerializeField] private Vector2 editorNodeSize = new(-1f, -1f);

        public string LineId => lineId;
        public CharacterDefinitionSO Speaker => speaker;
        public string NameOverride => nameOverride;
        public string DialogueText => dialogueText;
        public string NextLineId => nextLineId;
        public bool LogVisible => logVisible;
        public bool AllowTapToComplete => allowTapToComplete;
        public StoryFocusPolicy FocusPolicy => focusPolicy;
        public StoryVoiceRef Voice => voice;
        public bool UseAutoAdvanceOverride => useAutoAdvanceOverride;
        public float AutoAdvanceDelay => autoAdvanceDelay;
        public IReadOnlyList<StoryModuleSO> Modules => modules;

        // 에디터 전용 프로퍼티
        public Vector2 EditorNodePosition    => editorNodePosition;
        public bool    HasEditorNodePosition => editorNodePosition.x >= 0f;
        public Vector2 EditorNodeSize        => editorNodeSize;
        public bool    HasEditorNodeSize     => editorNodeSize.x >= 0f;

        //이 대사의 화자 이름을 결정하는 메서드입니다. nameOverride가 설정되어 있으면 그것을 반환하고,
        //그렇지 않으면 speaker의 DisplayName을 반환합니다. speaker가 null인 경우에는 빈 문자열을 반환합니다.
        public string GetResolvedSpeakerName()
        {
            if (!string.IsNullOrWhiteSpace(nameOverride))
                return nameOverride;

            return speaker != null ? speaker.DisplayName : string.Empty;
        }

        //이 대사가 내레이션인지 여부를 판단하는 메서드입니다.
        //speaker가 null인 경우에는 이 대사가 내레이션으로 간주됩니다.
        public bool IsNarration()
        {
            return speaker == null;
        }
    }
}