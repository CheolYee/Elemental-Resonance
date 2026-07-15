using System.Threading;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class EnemyLockOnView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cornerTopLeft;
        [SerializeField] private RectTransform cornerTopRight;
        [SerializeField] private RectTransform cornerBottomLeft;
        [SerializeField] private RectTransform cornerBottomRight;

        [Header("Settings")]
        [SerializeField] private float padding = 12f;
        [SerializeField] private float spreadScale = 1.6f;
        [SerializeField] private float convergeDuration = 0.18f;
        [SerializeField] private Ease convergeEase = Ease.OutCubic;
        [SerializeField] private float fadeOutDuration = 0.12f;

        private Camera _camera;
        private Renderer[] _renderers;
        private bool _isTracking;
        private CancellationTokenSource _cts;

        public void Show(AbstractEnemy enemy, Camera camera)
        {
            Show(enemy.GetComponentsInChildren<Renderer>(), camera, enemy.name);
        }

        public void Show(Renderer[] renderers, Camera camera)
        {
            Show(renderers, camera, "Object");
        }

        private void Show(Renderer[] renderers, Camera camera, string sourceName)
        {
            _camera = camera;
            _renderers = renderers;
            if (_renderers == null || _renderers.Length == 0)
            {
                Debug.LogWarning($"[EnemyLockOnView] {sourceName}에 Renderer가 없어 락온 표시를 건너뜁니다.");
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _isTracking = false;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(true);

            var targetRect = CalculateScreenRect();
            ApplyCorners(ScaleRect(targetRect, spreadScale));
            ConvergeAsync(targetRect, _cts.Token).Forget();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            _isTracking = false;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            FadeOutAsync(_cts.Token).Forget();
        }

        private void Update()
        {
            if (!_isTracking) return;
            ApplyCorners(CalculateScreenRect());
        }

        private async UniTaskVoid ConvergeAsync(Rect targetRect, CancellationToken ct)
        {
            var startRect = ScaleRect(targetRect, spreadScale);

            var fadeTask = LMotion.Create(0f, 1f, convergeDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => canvasGroup.alpha = a)
                .ToUniTask(cancellationToken: ct);

            var moveTask = LMotion.Create(0f, 1f, convergeDuration)
                .WithEase(convergeEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(t =>
                {
                    var pos = Vector2.Lerp(startRect.position, targetRect.position, t);
                    var size = Vector2.Lerp(startRect.size, targetRect.size, t);
                    ApplyCorners(new Rect(pos, size));
                })
                .ToUniTask(cancellationToken: ct);

            await UniTask.WhenAll(fadeTask, moveTask);
            if (ct.IsCancellationRequested) return;
            _isTracking = true;
        }

        private async UniTaskVoid FadeOutAsync(CancellationToken ct)
        {
            await LMotion.Create(canvasGroup.alpha, 0f, fadeOutDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => canvasGroup.alpha = a)
                .ToUniTask(cancellationToken: ct);

            if (ct.IsCancellationRequested) return;
            gameObject.SetActive(false);
        }

        private void ApplyCorners(Rect rect)
        {
            cornerTopLeft.position = new Vector2(rect.xMin, rect.yMax);
            cornerTopRight.position = new Vector2(rect.xMax, rect.yMax);
            cornerBottomLeft.position = new Vector2(rect.xMin, rect.yMin);
            cornerBottomRight.position = new Vector2(rect.xMax, rect.yMin);
        }

        private Rect CalculateScreenRect()
        {
            Bounds bounds = _renderers[0].bounds;
            for (int i = 1; i < _renderers.Length; i++)
                bounds.Encapsulate(_renderers[i].bounds);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = c + new Vector3(e.x * x, e.y * y, e.z * z);
                Vector2 sp = _camera.WorldToScreenPoint(corner);
                min = Vector2.Min(min, sp);
                max = Vector2.Max(max, sp);
            }

            min -= Vector2.one * padding;
            max += Vector2.one * padding;

            return new Rect(min, max - min);
        }

        private static Rect ScaleRect(Rect rect, float scale)
        {
            Vector2 center = rect.center;
            Vector2 size = rect.size * scale;
            return new Rect(center - size * 0.5f, size);
        }
    }
}
