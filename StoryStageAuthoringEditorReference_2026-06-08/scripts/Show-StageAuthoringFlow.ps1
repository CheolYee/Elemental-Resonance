param(
    [ValidateSet("All", "Entry", "Layout", "Selection", "Timeline", "Inspector", "Viewport", "Sound", "Persistence")]
    [string]$Section = "All",
    [switch]$ShowContext,
    [int]$ContextLines = 2,
    [switch]$AsJson
)

$bundleRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $bundleRoot "source"

if (!(Test-Path $sourceRoot)) {
    throw "source folder not found: $sourceRoot"
}

$sections = @(
    @{
        Name = "Entry"
        Items = @(
            @{ Label = "Graph selection pushes line into preview"; File = "Editor/StoryGraphEditorWindow.cs"; Pattern = "StoryPreviewWindow.NotifyLineSelected" },
            @{ Label = "Preview window static entry"; File = "Editor/StoryPreviewWindow.cs"; Pattern = "public static void NotifyLineSelected" },
            @{ Label = "External line selection handler"; File = "Editor/StoryPreviewWindow.cs"; Pattern = "public void OnExternalLineSelected" },
            @{ Label = "Snapshot staging for selected line"; File = "Editor/StoryPreviewWindow.Playback.cs"; Pattern = "private void ShowLineSnapshot" },
            @{ Label = "Accumulated stage state builder"; File = "Editor/StoryPreviewWindow.Playback.cs"; Pattern = "private void BuildStageStateAt" }
        )
    },
    @{
        Name = "Layout"
        Items = @(
            @{ Label = "Top bar with Mode/Episode"; File = "Editor/StoryPreviewWindow.Layout.cs"; Pattern = "private VisualElement BuildTopBar" },
            @{ Label = "Main left panel with viewport + timeline"; File = "Editor/StoryPreviewWindow.Layout.cs"; Pattern = "private VisualElement BuildLeftPanel" },
            @{ Label = "Stage world root"; File = "Editor/StoryPreviewWindow.Layout.cs"; Pattern = "private void BuildStoryRenderingRoot" },
            @{ Label = "Right panel root"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private VisualElement BuildActorInspector" }
        )
    },
    @{
        Name = "Selection"
        Items = @(
            @{ Label = "Object list / hierarchy-like panel"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void RefreshActorList" },
            @{ Label = "Select actor"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SelectActor" },
            @{ Label = "Select background"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SelectBackground" },
            @{ Label = "Select camera"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SelectCamera" },
            @{ Label = "Select sound"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void SelectSound" },
            @{ Label = "Actor viewport selection + drag"; File = "Editor/StoryPreviewWindow.Actors.cs"; Pattern = "private void RegisterActorInteraction" },
            @{ Label = "Background viewport selection + drag"; File = "Editor/StoryPreviewWindow.Background.cs"; Pattern = "private void RegisterBackgroundInteraction" },
            @{ Label = "Camera gizmo click path"; File = "Editor/StoryPreviewWindow.Actors.cs"; Pattern = "private void HandleCameraGizmoPointerDown" }
        )
    },
    @{
        Name = "Timeline"
        Items = @(
            @{ Label = "Timeline panel builder"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private VisualElement BuildTimelinePanel" },
            @{ Label = "Timeline refresh dispatch"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void RefreshTimelinePanel" },
            @{ Label = "Resolve current keyframe list by selection"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private IReadOnlyList<StoryActorKeyframeData> GetCurrentTimelineKeyframes" },
            @{ Label = "Actor rows"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void BuildActorTimelineRows" },
            @{ Label = "Background rows"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void BuildBackgroundTimelineRows" },
            @{ Label = "Camera rows"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void BuildCameraTimelineRows" },
            @{ Label = "Sound rows"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void BuildSoundTimelineRows" },
            @{ Label = "Lane click / right click behavior"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private VisualElement CreateTimelineLane" },
            @{ Label = "Add Property menu"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void ShowAddPropertyMenu" },
            @{ Label = "Add key at playhead"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void AddTimelineKeyAtPlayhead" },
            @{ Label = "Row right-click add/update menu"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void ShowRowKeyContextMenu" }
        )
    },
    @{
        Name = "Inspector"
        Items = @(
            @{ Label = "Selection-driven inspector rebuild"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void RefreshActorInspector" },
            @{ Label = "Selected generic key inspector"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void BuildSelectedTimelineKeyInspector" },
            @{ Label = "Selected key group inspector"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void BuildSelectedTimelineGroupInspector" },
            @{ Label = "Selected segment inspector"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void BuildSelectedTimelineSegmentInspector" },
            @{ Label = "Background inspector"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void BuildBackgroundInspector" },
            @{ Label = "Camera inspector"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void BuildCameraInspector" },
            @{ Label = "Sound inspector"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void BuildSoundInspector" },
            @{ Label = "Selected BGM key inspector"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void BuildSelectedBgmKeyInspector" },
            @{ Label = "Selected SFX key inspector"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void BuildSelectedSfxKeyInspector" }
        )
    },
    @{
        Name = "Viewport"
        Items = @(
            @{ Label = "Timeline record mode toggle"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void ToggleTimelineRecord" },
            @{ Label = "Actor drag can write keys"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void RecordActorKeyframeFromState" },
            @{ Label = "Background drag can write keys"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void RecordBackgroundKeyframeFromState" },
            @{ Label = "Apply selected actor key from viewport"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private bool TryApplySelectedTimelineKeyFromState" },
            @{ Label = "Apply selected background key from viewport"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private bool TryApplySelectedTimelineKeyFromBackground" },
            @{ Label = "Apply selected camera key from viewport"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private bool TryApplySelectedTimelineKeyFromCamera" },
            @{ Label = "Playhead sampling"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void ApplyTimelinePlayheadSample" }
        )
    },
    @{
        Name = "Sound"
        Items = @(
            @{ Label = "Sound proxy creation"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private StoryActorKeyframeData GetOrCreateSoundKeyProxy(StoryBgmKeyframeData keyframe)" },
            @{ Label = "Sound timeline key list bridge"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private IReadOnlyList<StoryActorKeyframeData> GetCurrentSoundTimelineKeyframes" },
            @{ Label = "Update selected real sound key from proxy selection"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void UpdateSelectedSoundKeysFromTimelineSelection" },
            @{ Label = "Sound row right-click key creation"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void ShowSoundRowKeyContextMenu" },
            @{ Label = "Add sound key at time"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void AddSoundKeyAtTime" },
            @{ Label = "Sound key pointer / drag selection"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void HandleSoundKeyPointerDown" }
        )
    },
    @{
        Name = "Persistence"
        Items = @(
            @{ Label = "Find current stage layout module"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private StoryStageLayoutModuleSO FindCurrentStageLayout" },
            @{ Label = "Create stage layout module if missing"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private StoryStageLayoutModuleSO GetOrCreateCurrentStageLayout" },
            @{ Label = "Save actor track"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SaveActorTrackToCurrent" },
            @{ Label = "Save background track"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SaveBackgroundTrackToCurrent" },
            @{ Label = "Save camera track"; File = "Editor/StoryPreviewWindow.Inspector.cs"; Pattern = "private void SaveCameraTrackToCurrent" },
            @{ Label = "Save sound track"; File = "Editor/StoryPreviewWindow.Sound.cs"; Pattern = "private void SaveSoundTrackToCurrent" },
            @{ Label = "Add/update actor key"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void AddOrUpdateKey" },
            @{ Label = "Add/update background key"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void AddOrUpdateBackgroundKey" },
            @{ Label = "Add/update camera key"; File = "Editor/StoryPreviewWindow.Timeline.cs"; Pattern = "private void AddOrUpdateCameraKey" }
        )
    }
)

function Resolve-Items {
    param([string]$WantedSection)

    if ($WantedSection -eq "All") {
        return $sections
    }

    return $sections | Where-Object { $_.Name -eq $WantedSection }
}

function Find-MatchInfo {
    param(
        [string]$File,
        [string]$Pattern
    )

    $fullPath = Join-Path $sourceRoot $File
    if (!(Test-Path $fullPath)) {
        return [pscustomobject]@{
            File = $File
            Line = $null
            Match = $null
            FullPath = $fullPath
            Exists = $false
        }
    }

    $match = Select-String -Path $fullPath -Pattern $Pattern -SimpleMatch | Select-Object -First 1
    return [pscustomobject]@{
        File = $File
        Line = if ($match) { $match.LineNumber } else { $null }
        Match = if ($match) { $match.Line.Trim() } else { $null }
        FullPath = $fullPath
        Exists = $true
    }
}

$result = foreach ($sectionInfo in (Resolve-Items -WantedSection $Section)) {
    foreach ($item in $sectionInfo.Items) {
        $matchInfo = Find-MatchInfo -File $item.File -Pattern $item.Pattern
        [pscustomobject]@{
            Section = $sectionInfo.Name
            Label = $item.Label
            File = $matchInfo.File
            Line = $matchInfo.Line
            Match = $matchInfo.Match
            FullPath = $matchInfo.FullPath
            Exists = $matchInfo.Exists
        }
    }
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 4
    return
}

$grouped = $result | Group-Object Section
foreach ($group in $grouped) {
    Write-Host ""
    Write-Host "=== $($group.Name) ==="
    foreach ($item in $group.Group) {
        $lineText = if ($item.Line) { $item.Line } else { "?" }
        Write-Host ("[{0}] {1}:{2}" -f $item.Label, $item.File, $lineText)
        if ($item.Match) {
            Write-Host ("    {0}" -f $item.Match)
        } elseif (-not $item.Exists) {
            Write-Host ("    MISSING FILE: {0}" -f $item.FullPath)
        } else {
            Write-Host ("    PATTERN NOT FOUND")
        }

        if ($ShowContext -and $item.Line) {
            $start = [Math]::Max(0, $item.Line - $ContextLines - 1)
            $end = $item.Line + $ContextLines - 1
            $lines = Get-Content -Encoding UTF8 $item.FullPath
            for ($i = $start; $i -le [Math]::Min($end, $lines.Count - 1); $i++) {
                Write-Host ("    {0,5}: {1}" -f ($i + 1), $lines[$i])
            }
        }
    }
}
