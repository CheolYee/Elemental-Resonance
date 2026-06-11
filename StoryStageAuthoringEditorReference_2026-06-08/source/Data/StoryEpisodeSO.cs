using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 에피소드의 정의를 담는 ScriptableObject 클래스입니다.
    /// 에피소드 ID, 제목, 스킵 요약 텍스트, 진입 대사 ID, 그리고 이 에피소드에 포함된 대사들의 리스트를 포함합니다.
    /// </summary>
    
    [CreateAssetMenu(fileName = "StoryEpisode", menuName = "Story/Story Episode")]
    public sealed class StoryEpisodeSO : ScriptableObject
    {
        [Header("Identity")]
        //스토리 시스템 내에서 이 에피소드를 고유하게 식별하는 ID입니다.
        //스토리 진행 로직에서 이 ID를 참조하여 어떤 에피소드가 진행되고 있는지 식별할 수 있습니다.
        [SerializeField] private string episodeId;
        //스토리 에피소드의 제목입니다. 스토리 선택 화면이나 로그에서 이 제목이 표시될 수 있습니다.
        [SerializeField] private string title;

        [Header("Skip")]
        [TextArea(3, 8)]
        //플레이어가 이 에피소드를 건너뛸 때 표시되는 요약 텍스트입니다.
        //이 텍스트는 플레이어가 에피소드를 건너뛸 때 어떤 내용이 있었는지 간략하게 알려주는 역할을 합니다.
        [SerializeField] private string skipSummary;

        [Header("Entry")]
        //이 에피소드의 진입 대사 ID입니다. 스토리 진행 로직에서 이 ID를 참조하여
        //이 에피소드가 시작될 때 어떤 대사부터 시작할지 결정할 수 있습니다.
        [SerializeField] private string entryLineId;

        [Header("Lines")]
        //이 에피소드에 포함된 대사들의 리스트입니다.
        //각 대사는 스토리 진행 중 플레이어에게 표시되는 하나의 대사 한 줄을 나타냅니다.
        [SerializeField] private List<StoryLineSO> lines = new();

        [Header("Sound Defaults")]
        [SerializeField] private StorySoundSettingsData defaultSoundSettings = new();
        [SerializeField] private bool hasExplicitDefaultSoundSettings;

        // 에디터 전용 — 런타임 미사용
        [HideInInspector]
        [SerializeField] private string editorLineSaveFolder = "";

        public string EpisodeId => episodeId;
        public string Title => title;
        public string SkipSummary => skipSummary;
        public string EntryLineId => entryLineId;
        public IReadOnlyList<StoryLineSO> Lines => lines;
        public StorySoundSettingsData DefaultSoundSettings => defaultSoundSettings ??= new StorySoundSettingsData();
        public bool HasExplicitDefaultSoundSettings => hasExplicitDefaultSoundSettings;
        public string EditorLineSaveFolder => editorLineSaveFolder;

#if UNITY_EDITOR
        public StorySoundSettingsData DefaultSoundSettingsEditable => defaultSoundSettings ??= new StorySoundSettingsData();
        public bool HasExplicitDefaultSoundSettingsEditable
        {
            get => hasExplicitDefaultSoundSettings;
            set => hasExplicitDefaultSoundSettings = value;
        }
#endif

        //스토리 에피소드 내에서 특정 대사 ID에 해당하는 대사를 찾아 반환하는 메서드입니다.
        public bool TryGetLine(string lineId, out StoryLineSO line)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                StoryLineSO candidate = lines[i];
                if (candidate != null && candidate.LineId == lineId)
                {
                    line = candidate;
                    return true;
                }
            }

            line = null;
            return false;
        }

        private void OnValidate()
        {
            defaultSoundSettings ??= new StorySoundSettingsData();
        }
    }
}
