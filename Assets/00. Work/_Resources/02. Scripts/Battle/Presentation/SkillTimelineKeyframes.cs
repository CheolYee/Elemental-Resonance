using System;
using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using LitMotion;
using UnityEngine;

namespace Battle.Presentation
{
    // ── 키프레임 데이터 (fat 타입, 단일 직렬화 클래스) ─────────────────────────

    [Serializable]
    public class SkillKeyframeData
    {
        public SkillKeyframeProperty property;
        public float timeSeconds;

        // Animation
        public AnimParamSO animParam;

        // Effect
        public string effectSlotId;
        public float  valueMultiplier = 1f;

        // VFX Transform (VfxPosition / VfxRotation / VfxScale — 스킬 절대 시간 기준)
        public Vector3 position;
        public Vector3 rotationEuler;
        public Vector3 scale = Vector3.one;
        public bool    isHold;
        public Ease    easing = Ease.Linear;

        // VFX Active
        public SkillVfxActiveAction vfxActiveAction;

        // Camera
        public Vector3 cameraPosition;
        public Vector3 cameraRotationEuler;
        public float   fieldOfView   = 60f;
        public float   amplitude     = 1f;
        public float   shakeDuration = 0.2f;

        // UI
        public SkillUiAction uiAction;

        // SFX
        public string sfxId;
    }

    // ── 단일 트랙 (하나의 오브젝트 종류가 갖는 키프레임 리스트) ───────────────

    [Serializable]
    public class SkillSingleTrackData
    {
        public List<SkillKeyframeData> keyframes = new();
    }

    // ── VFX 오브젝트 (Story의 StoryActorTrackData에 대응) ─────────────────────

    [Serializable]
    public class SkillVfxObjectData
    {
        public VfxDefinitionSO     vfxDefinition;
        public float               spawnTime;                        // 인스펙터에서 편집 (타임라인 Row 아님)
        public float               lifeTime             = 1f;
        public SkillVfxSpawnTarget spawnTarget          = SkillVfxSpawnTarget.Caster;
        public Vector3             spawnPositionOffset;
        public Vector3             spawnRotationEuler;
        public List<SkillKeyframeData> keyframes        = new();    // VfxPosition / VfxRotation / VfxScale
    }

    // ── 프로퍼티 enum ──────────────────────────────────────────────────────────

    public enum SkillKeyframeProperty
    {
        // Animation
        AnimParam,
        // Effect
        EffectSlot,
        // VFX Transform
        VfxPosition,
        VfxRotation,
        VfxScale,
        VfxActive,
        // Camera
        CamPosition,
        CamRotation,
        CamZoom,
        CamShake,
        // UI
        UiAction,
        // SFX
        SfxId,
        // Timeline End Marker
        TimelineEndTime,
    }

    // ── 오브젝트 선택 종류 (에디터 선택 상태용) ───────────────────────────────

    public enum SkillObjectKind
    {
        None,
        Animation,
        Effect,
        Vfx,
        Camera,
        Ui,
        Sfx,
        EndMarker,
    }

    // ── VFX Active enum ───────────────────────────────────────────────────────

    public enum SkillVfxActiveAction
    {
        Play,
        Stop,
    }

    // ── 도메인 enum ────────────────────────────────────────────────────────────

    public enum SkillVfxKey
    {
        None,
        GuardShield,
        CostGain,
        ThunderEffect,
        FireCard_01
    }

    public enum SkillVfxSpawnTarget
    {
        Caster,
        Target,
        BetweenCasterAndTarget,
    }

    public enum SkillUiAction
    {
        None,
        HideBattleUI,
        ShowBattleUI,
    }
}
