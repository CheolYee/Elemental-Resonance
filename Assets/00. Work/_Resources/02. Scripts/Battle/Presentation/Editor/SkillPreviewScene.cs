using System.Collections.Generic;
using LitMotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battle.Presentation.Editor
{
    internal sealed class SkillPreviewScene
    {
        private const int PreviewLayer = 31;

        // ── VFX 엔트리 (spawn 시 계산한 기준값 + 캐시된 필터 리스트) ────────────
        private readonly struct VfxEntry
        {
            public readonly SkillVfxObjectData      Data;
            public readonly GameObject              Go;
            public readonly ParticleSystem[]        Particles;
            public readonly Vector3                 BasePosition;
            public readonly Quaternion              BaseRotation;
            public readonly Vector3                 BaseScale;
            public readonly List<SkillKeyframeData> PosKeys;
            public readonly List<SkillKeyframeData> RotKeys;
            public readonly List<SkillKeyframeData> ScaleKeys;
            public readonly List<SkillKeyframeData> ActiveKeys;

            public VfxEntry(SkillVfxObjectData data, GameObject go, Vector3 basePos, Quaternion baseRot, Vector3 baseScale)
            {
                Data         = data;
                Go           = go;
                Particles    = go.GetComponentsInChildren<ParticleSystem>(true);
                BasePosition = basePos;
                BaseRotation = baseRot;
                BaseScale    = baseScale;
                PosKeys    = SortedFilter(data.keyframes, SkillKeyframeProperty.VfxPosition);
                RotKeys    = SortedFilter(data.keyframes, SkillKeyframeProperty.VfxRotation);
                ScaleKeys  = SortedFilter(data.keyframes, SkillKeyframeProperty.VfxScale);
                ActiveKeys = SortedFilter(data.keyframes, SkillKeyframeProperty.VfxActive);
            }

            private static List<SkillKeyframeData> SortedFilter(List<SkillKeyframeData> src, SkillKeyframeProperty prop)
            {
                var result = new List<SkillKeyframeData>();
                if (src == null) return result;
                foreach (var k in src)
                    if (k != null && k.property == prop) result.Add(k);
                result.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
                return result;
            }
        }

        // ── 필드 ─────────────────────────────────────────────────────────────────
        private Scene           _scene;
        private Camera          _camera;
        private RenderTexture   _renderTexture;
        private GameObject      _environment;
        private GameObject      _caster;
        private List<GameObject> _targets    = new();
        private List<VfxEntry>   _vfxEntries = new();
        private double           _lastRenderLogTime;

        private SkillPreviewLayoutSO      _layout;
        private SkillPresentationTimeline _timeline;
        private Animator                  _casterAnimator;
        private bool                      _isRecordingCamera;

        public bool IsRecordingCamera
        {
            get => _isRecordingCamera;
            set => _isRecordingCamera = value;
        }

        public void SetRecordingCameraTransform(Vector3 pos, Vector3 euler)
        {
            if (_camera == null) return;
            _camera.transform.position    = pos;
            _camera.transform.eulerAngles = euler;
        }

        public void GetCameraTransform(out Vector3 position, out Vector3 euler)
        {
            if (_camera != null)
            {
                position = _camera.transform.position;
                euler    = _camera.transform.eulerAngles;
            }
            else
            {
                position = _layout?.cameraPosition      ?? Vector3.zero;
                euler    = _layout?.cameraRotationEuler ?? Vector3.zero;
            }
        }

        public RenderTexture RenderTexture => _renderTexture;

        // ── 생성 ─────────────────────────────────────────────────────────────────
        public static SkillPreviewScene Create(SkillPreviewLayoutSO layout)
        {
            var instance = new SkillPreviewScene();
            instance.Initialize(layout);
            return instance;
        }

        private void Initialize(SkillPreviewLayoutSO layout)
        {
            _scene = EditorSceneManager.NewPreviewScene();
            Debug.Log($"[SkillPreview] 프리뷰 씬 생성. layout={layout?.name ?? "NULL"}");

            _renderTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.Default);
            _renderTexture.Create();

            var camGo = new GameObject("PreviewCamera");
            SceneManager.MoveGameObjectToScene(camGo, _scene);
            _camera                 = camGo.AddComponent<Camera>();
            _camera.targetTexture   = _renderTexture;
            _camera.clearFlags      = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f);
            _camera.nearClipPlane   = 0.1f;
            _camera.farClipPlane    = 100f;
            _camera.cullingMask     = -1;
            _camera.cameraType      = CameraType.Preview;
            _camera.scene           = _scene;
            _camera.enabled         = false;

            var lightGo = new GameObject("PreviewLight");
            SceneManager.MoveGameObjectToScene(lightGo, _scene);
            var light       = lightGo.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            SetLayerRecursively(lightGo, PreviewLayer);

            if (layout != null)
                Rebuild(layout, null);
        }

        // ── Rebuild ───────────────────────────────────────────────────────────────
        public void Rebuild(SkillPreviewLayoutSO layout, SkillPresentationTimeline timeline)
        {
            _layout   = layout;
            _timeline = timeline;

            if (layout != null)
            {
                ApplyCameraFromLayout(layout);
                RebuildEnvironment(layout);
                RebuildCharacters(layout);
            }
            RebuildVfx(timeline, layout);
        }

        private void ApplyCameraFromLayout(SkillPreviewLayoutSO layout)
        {
            if (_camera == null) return;
            _camera.transform.position = layout.cameraPosition;
            _camera.transform.rotation = Quaternion.Euler(layout.cameraRotationEuler);
            _camera.fieldOfView        = layout.cameraFov;
        }

        private void RebuildEnvironment(SkillPreviewLayoutSO layout)
        {
            if (_environment != null) Object.DestroyImmediate(_environment);
            _environment = null;

            if (layout.environmentPrefab == null) return;

            _environment = Object.Instantiate(layout.environmentPrefab);
            SceneManager.MoveGameObjectToScene(_environment, _scene);
            SetLayerRecursively(_environment, PreviewLayer);
        }

        public void RebuildCharacters(SkillPreviewLayoutSO layout)
        {
            if (_caster != null) Object.DestroyImmediate(_caster);
            _caster         = null;
            _casterAnimator = null;
            foreach (var t in _targets) if (t != null) Object.DestroyImmediate(t);
            _targets.Clear();

            if (layout == null)
            {
                Debug.Log("[SkillPreview] RebuildCharacters: layout이 null — 스폰 건너뜀");
                return;
            }

            Debug.Log($"[SkillPreview] RebuildCharacters: casterPrefab={layout.casterPrefab?.name ?? "NULL"}, casterPos={layout.casterPosition}");

            if (layout.casterPrefab != null)
            {
                var (posOff, rotOff) = GetSpawnOffsets(layout.casterPrefab);
                _caster = Object.Instantiate(layout.casterPrefab, layout.casterPosition + posOff, rotOff);
                SceneManager.MoveGameObjectToScene(_caster, _scene);
                SetLayerRecursively(_caster, PreviewLayer);
                _casterAnimator = _caster.GetComponentInChildren<Animator>();
                Debug.Log($"[SkillPreview] Caster 스폰: {_caster.name} at {layout.casterPosition + posOff}, layer={_caster.layer}");
            }

            int slotCount = Mathf.Min(
                layout.targetPrefabs   != null ? layout.targetPrefabs.Length   : 0,
                layout.targetPositions != null ? layout.targetPositions.Length : 0);

            for (int i = 0; i < slotCount; i++)
            {
                if (layout.targetPrefabs[i] == null) continue;
                var (posOff, rotOff) = GetSpawnOffsets(layout.targetPrefabs[i]);
                var go = Object.Instantiate(layout.targetPrefabs[i], layout.targetPositions[i] + posOff, rotOff);
                SceneManager.MoveGameObjectToScene(go, _scene);
                SetLayerRecursively(go, PreviewLayer);
                _targets.Add(go);
                Debug.Log($"[SkillPreview] Target[{i}] 스폰: {go.name} at {layout.targetPositions[i] + posOff}, layer={go.layer}");
            }
        }

        public void RebuildVfx(SkillPresentationTimeline timeline, SkillPreviewLayoutSO layout)
        {
            foreach (var e in _vfxEntries) if (e.Go != null) Object.DestroyImmediate(e.Go);
            _vfxEntries.Clear();

            if (timeline?.vfxObjects == null || layout == null) return;

            foreach (var vfxData in timeline.vfxObjects)
            {
                if (vfxData?.vfxDefinition?.prefab == null) continue;

                Vector3    spawnPos = ResolveSpawnPosition(vfxData, layout);
                Quaternion spawnRot = Quaternion.Euler(vfxData.spawnRotationEuler);
                var go = Object.Instantiate(vfxData.vfxDefinition.prefab, spawnPos, spawnRot);
                SceneManager.MoveGameObjectToScene(go, _scene);
                SetLayerRecursively(go, PreviewLayer);
                go.SetActive(false);

                _vfxEntries.Add(new VfxEntry(vfxData, go, spawnPos, spawnRot, go.transform.localScale));
            }
        }

        // ── Sample ────────────────────────────────────────────────────────────────
        public void Sample(float t)
        {
            SampleAnimators(t);
            SampleVfx(t);
            SampleCamera(t);
        }

        private void SampleAnimators(float t)
        {
            if (_casterAnimator == null || _timeline?.animationTrack?.keyframes == null) return;

            // t 이하 마지막 AnimParam 키프레임만 필요 — 트랜지션 없이 PlayClip으로만 제어하므로
            SkillKeyframeData lastKf = null;
            foreach (var kf in _timeline.animationTrack.keyframes)
            {
                if (kf?.animParam == null) continue;
                if (kf.timeSeconds > t) continue;
                if (lastKf == null || kf.timeSeconds >= lastKf.timeSeconds)
                    lastKf = kf;
            }

            _casterAnimator.Rebind();

            if (lastKf == null) return;

            float timeIntoClip = t - lastKf.timeSeconds;
            _casterAnimator.Play(lastKf.animParam.ParamHash, 0, 0f);
            _casterAnimator.Update(0f);
            if (timeIntoClip > 0f)
                _casterAnimator.Update(timeIntoClip);
        }

        private void SampleVfx(float t)
        {
            foreach (var entry in _vfxEntries)
            {
                float localT = t - entry.Data.spawnTime;

                // Active 판정
                bool   isActive;
                float  simulateT;

                if (localT <= 0f)
                {
                    isActive  = false;
                    simulateT = 0f;
                }
                else if (entry.ActiveKeys.Count > 0)
                {
                    // VfxActive 키프레임 기반: t 이전 마지막 Play/Stop 확인
                    SkillKeyframeData lastActive = null;
                    foreach (var kf in entry.ActiveKeys)
                    {
                        if (kf.timeSeconds <= t) lastActive = kf;
                    }
                    isActive  = lastActive != null && lastActive.vfxActiveAction == SkillVfxActiveAction.Play;
                    simulateT = lastActive != null && isActive ? t - lastActive.timeSeconds : 0f;
                }
                else
                {
                    isActive  = true;
                    simulateT = localT;
                }

                entry.Go.SetActive(isActive);

                if (!isActive) continue;

                // ParticleSystem 스크러빙
                foreach (var ps in entry.Particles)
                    if (ps != null) ps.Simulate(Mathf.Max(0f, simulateT), true, true);

                // Transform 보간 (base + additive delta)
                entry.Go.transform.position   = entry.BasePosition + InterpolateDelta(entry.PosKeys,   t, kf => kf.position);
                entry.Go.transform.eulerAngles = entry.BaseRotation.eulerAngles + InterpolateDelta(entry.RotKeys, t, kf => kf.rotationEuler);
                entry.Go.transform.localScale  = entry.BaseScale + InterpolateDelta(entry.ScaleKeys, t, kf => kf.scale);
            }
        }

        // ── 보간 유틸 ─────────────────────────────────────────────────────────────

        // 런타임 SkillVfxExecutionService.AnimatePositionAsync 동일 로직 (base + additive delta)
        // isHold=true → 이전 키프레임 값 유지(snap), false → EaseUtility 보간
        private static Vector3 InterpolateDelta(
            List<SkillKeyframeData> sortedKeys,
            float t,
            System.Func<SkillKeyframeData, Vector3> getDelta)
        {
            if (sortedKeys.Count == 0) return Vector3.zero;

            SkillKeyframeData prev = null;
            SkillKeyframeData next = null;

            foreach (var kf in sortedKeys)
            {
                if (kf.timeSeconds <= t) prev = kf;
                else if (next == null)   next = kf;
            }

            if (prev == null) return Vector3.zero;

            Vector3 prevDelta = getDelta(prev);
            if (next == null)  return prevDelta;
            if (next.isHold)   return prevDelta;

            Vector3 nextDelta   = getDelta(next);
            float   seg         = next.timeSeconds - prev.timeSeconds;
            float   normalizedT = seg > 0f ? (t - prev.timeSeconds) / seg : 1f;
            float   easedT      = EaseUtility.Evaluate(normalizedT, next.easing);
            return Vector3.Lerp(prevDelta, nextDelta, easedT);
        }

        private void SampleCamera(float t)
        {
            if (_isRecordingCamera || _camera == null || _layout == null) return;

            var posKeys  = FilterCameraKeys(_timeline?.cameraTrack, SkillKeyframeProperty.CamPosition);
            var rotKeys  = FilterCameraKeys(_timeline?.cameraTrack, SkillKeyframeProperty.CamRotation);
            var zoomKeys = FilterCameraKeys(_timeline?.cameraTrack, SkillKeyframeProperty.CamZoom);

            _camera.transform.position    = _layout.cameraPosition      + InterpolateDelta(posKeys,  t, kf => kf.cameraPosition);
            _camera.transform.eulerAngles = _layout.cameraRotationEuler + InterpolateDelta(rotKeys,  t, kf => kf.cameraRotationEuler);
            _camera.fieldOfView           = InterpolateFov(zoomKeys, t, _layout.cameraFov);
        }

        private static List<SkillKeyframeData> FilterCameraKeys(SkillSingleTrackData track, SkillKeyframeProperty prop)
        {
            var result = new List<SkillKeyframeData>();
            if (track?.keyframes == null) return result;
            foreach (var k in track.keyframes)
                if (k != null && k.property == prop) result.Add(k);
            return result;
        }

        // CamZoom은 absolute FOV — 런타임 SkillCameraExecutionService.PlayZoomAsync 동일 로직
        private static float InterpolateFov(List<SkillKeyframeData> sortedKeys, float t, float baseFov)
        {
            if (sortedKeys.Count == 0) return baseFov;

            SkillKeyframeData prevKey = null;
            SkillKeyframeData nextKey = null;
            foreach (var kf in sortedKeys)
            {
                if (kf.timeSeconds <= t) prevKey = kf;
                else if (nextKey == null) nextKey = kf;
            }

            if (nextKey == null) return prevKey.fieldOfView;

            float fromFov  = prevKey?.fieldOfView ?? baseFov;
            float fromTime = prevKey?.timeSeconds ?? 0f;
            if (nextKey.isHold) return fromFov;

            float seg    = nextKey.timeSeconds - fromTime;
            float normT  = seg > 0f ? (t - fromTime) / seg : 1f;
            float easedT = EaseUtility.Evaluate(Mathf.Clamp01(normT), nextKey.easing);
            return Mathf.Lerp(fromFov, nextKey.fieldOfView, easedT);
        }

        // ── Render / Destroy ──────────────────────────────────────────────────────
        public void RenderCamera()
        {
            if (_camera == null || _renderTexture == null) return;

            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRenderLogTime > 2.0)
            {
                _lastRenderLogTime = now;
                Debug.Log($"[SkillPreview] RenderCamera. cam.enabled={_camera.enabled}, RT.created={_renderTexture.IsCreated()}, scene.valid={_scene.IsValid()}, caster={(_caster != null ? _caster.name : "NULL")}, targets={_targets.Count}, vfx={_vfxEntries.Count}");
            }

            _camera.Render();
        }

        public void Destroy()
        {
            foreach (var e in _vfxEntries) if (e.Go != null) Object.DestroyImmediate(e.Go);
            _vfxEntries.Clear();

            if (_caster != null) Object.DestroyImmediate(_caster);
            _caster = null;
            foreach (var t in _targets) if (t != null) Object.DestroyImmediate(t);
            _targets.Clear();

            if (_environment != null) Object.DestroyImmediate(_environment);
            _environment = null;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.DestroyImmediate(_renderTexture);
                _renderTexture = null;
            }

            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────────────────
        private static (Vector3 pos, Quaternion rot) GetSpawnOffsets(GameObject prefab)
        {
            var tag = prefab.GetComponent<SkillPreviewAgentTag>();
            if (tag == null) return (Vector3.zero, Quaternion.identity);
            return (tag.spawnOffset, Quaternion.Euler(tag.spawnRotationOffset));
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }

        private static Vector3 ResolveSpawnPosition(SkillVfxObjectData vfxData, SkillPreviewLayoutSO layout)
        {
            Vector3 casterPos = layout.casterPosition;
            Vector3 targetPos = layout.targetPositions != null && layout.targetPositions.Length > 0
                ? layout.targetPositions[0]
                : Vector3.zero;

            Vector3 basePos = vfxData.spawnTarget switch
            {
                SkillVfxSpawnTarget.Caster                 => casterPos,
                SkillVfxSpawnTarget.Target                 => targetPos,
                SkillVfxSpawnTarget.BetweenCasterAndTarget => (casterPos + targetPos) * 0.5f,
                _                                           => casterPos
            };

            return basePos + vfxData.spawnPositionOffset;
        }
    }
}
