using System;
using System.Reflection;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes
{
    /// <summary>
    /// StoryModuleSO 서브클래스 (및 StoryInlineModuleData 서브클래스) 에 부착하는 메타데이터.
    /// 에디터 검색 팝업, 모듈 스택 렌더링, 색상 구분에 활용됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class StoryModuleMetadataAttribute : Attribute
    {
        public string DisplayName    { get; }
        public string Category       { get; }
        public string AccentColorHex { get; }
        public int    SortPriority   { get; }
        public bool   IsDeprecated   { get; }

        /// <param name="displayName">에디터에 표시할 이름</param>
        /// <param name="category">검색 팝업 그룹 이름 (예: "Timing", "Character", "Flow")</param>
        /// <param name="accentColorHex">모듈 카드 강조색 (HTML hex, 예: "#5B9BD5")</param>
        /// <param name="sortPriority">같은 카테고리 내 정렬 순서 (낮을수록 위)</param>
        /// <param name="isDeprecated">true 이면 Add Module 팝업에서 숨겨짐 (기존 자산은 유지)</param>
        public StoryModuleMetadataAttribute(
            string displayName,
            string category       = "General",
            string accentColorHex = "#AAAAAA",
            int    sortPriority   = 0,
            bool   isDeprecated   = false)
        {
            DisplayName    = displayName;
            Category       = category;
            AccentColorHex = accentColorHex;
            SortPriority   = sortPriority;
            IsDeprecated   = isDeprecated;
        }

        // ── 정적 헬퍼 ────────────────────────────────────────────────────────

        /// <summary>타입에 부착된 attribute에서 강조색을 읽습니다. attribute가 없으면 fallback을 반환합니다.</summary>
        public static Color GetColor(Type type, Color fallback = default)
        {
            var attr = type?.GetCustomAttribute<StoryModuleMetadataAttribute>(inherit: false);
            if (attr == null) return fallback == default ? Color.white : fallback;
            return ColorUtility.TryParseHtmlString(attr.AccentColorHex, out var c) ? c : (fallback == default ? Color.white : fallback);
        }

        /// <summary>타입에 부착된 attribute를 반환합니다. 없으면 null.</summary>
        public static StoryModuleMetadataAttribute Get(Type type)
            => type?.GetCustomAttribute<StoryModuleMetadataAttribute>(inherit: false);
    }
}
