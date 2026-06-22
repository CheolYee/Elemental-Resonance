using System;
using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Gamelib.SoundSystem;
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
        public AnimParamSO   animParam;
        public AnimationClip animClip;

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
        public SfxSounds sfxSound;
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
        public float               simulationSpeed      = 1f;
        public float               startLifetimeMultiplier = 1f;
        public SkillVfxSpawnTarget spawnTarget          = SkillVfxSpawnTarget.Caster;
        public int                 hitIndex             = 0;   // Target/Between 타입일 때 preResolvedHitTargets 인덱스
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
        // Caster Transform
        CasterPosition,
        CasterRotation,
    }

    // ── 오브젝트 선택 종류 (에디터 선택 상태용) ───────────────────────────────

    public enum SkillObjectKind
    {
        None,
        Animation,
        Effect,
        Vfx,
        Camera,
        Caster,
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
        FireCard_01,
        번개_비구름,
        번개_원형_폭파_장판,
        번개_연속일점폭파_스파크,
        번개_토네이도,
        번개_폭파탄환_낙하,
        번개_원점_연속폭파_스파크,
        번개_일점폭파_장판형_스파크,
        번개_스파크_폭파,
        아케인_마법진_대형폭파,
        아케인_소용돌이,
        아케인_기모으기_폭파,
        아케인_폭파탄환_낙하,
        아케인_많은검낙하,
        아케인_일점폭파,
        아케인_마법탄환,
        아케인_포션폭파,
        아케인_고서_브레스낙하_장판소환,
        아케인_고서_토네이도_종이,
        아케인_일점폭파_강화,
        어둠_일점폭파,
        어둠_브레스,
        어둠_상승폭파,
        어둠_마법탄환_낙하,
        어둠_버프장판_조금상승,
        어둠_일점폭파_강화,
        어둠_일점폭파_소환,
        어둠_소용돌이,
        어둠_불꽃폭파,
        화염_상승폴파,
        화염_낙뢰폭파,
        화염_일점폭파_기모으기,
        화염_기본폭파,
        화염_선형_구,
        화염_미니메테오,
        화염_불길낙하,
        화염_전방회오리,
        화염_원형장판_상승폭파,
        화염_강화폭파,
        화염_일점폭파,
        얼음_고드름낙하,
        얼음_고드름장판,
        얼음_고드름_전방,
        얼음_고드름폭파,
        얼음_날카로운얼음_생성,
        고드름_상승_일점타격,
        얼음_전방_얼음토네이도,
        얼음_고드름상승_단일,
        얼음_고드름_옆으로퍼짐,
        얼음_토네이도,
        얼음_눈상승,
        얼음_장판과_원형구역,
        빛_일점폭파,
        빛_투사체낙하,
        빛_연기버프,
        빛_신성장판,
        빛_상승폭파,
        빛_하단발사,
        빛_망치낙하_장판,
        빛_전방발사,
        빛_상승장판,
        빛_삼각폭파,
        빛_하강빛구름,
        자연_돌낙하,
        자연_지면상승,
        자연_독버섯폭파,
        자연_일점폭파,
        자연_상승폭파,
        자연_빛낙하,
        자연_풀장판,
        자연_풀장판_2,
        자연_땅폭파_장판형,
        버프_적,
        버프_적_엘리트,
        버프_불,
        버프_빛,
        버프_자연,
        버프_번개,
        방어막_아케인,
        방어막_어둠,
        방어막_화염,
        방어막_빛,
        방어막_자연,
        방어막_번개,
        빛_검떨구기,
        기본_타격,
        오라_아케인,
        오라_어둠,
        오라_화염,
        오라_얼음,
        오라_빛,
        오라_자연,
        오라_번개,
        버프_아케인
    }

    public enum SkillVfxSpawnTarget
    {
        Caster,
        Target,
        BetweenCasterAndTarget,
        None,
    }

    public enum SkillUiAction
    {
        None,
        HideBattleUI,
        ShowBattleUI,
    }
}
