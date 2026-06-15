using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;

namespace Battle.UI
{
    public class DamageTextView : MonoBehaviour, IPoolable
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMPEffect labelEffect;

        [Header("Animation")]
        [SerializeField] private float risePixels = 80f;
        [SerializeField] private float riseDuration = 0.8f;
        [SerializeField] private float fadeDelay = 0.3f;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color damageColor = Color.red;
        [SerializeField] private Color blockColor = new Color(0.27f, 0.53f, 1f);

        [field: SerializeField] public PoolItemSo PoolItem { get; set; }
        public GameObject GameObject => gameObject;

        private PoolManagerSo _pool;
        private CancellationTokenSource _cts;
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Setup(PoolManagerSo pool)
        {
            _pool = pool;
        }

        public void PlayDamage(Vector2 screenPos, int damageToHp, int damageToBlock)
        {
            _rect.position = screenPos;

            if (damageToHp > 0)
            {
                label.text = damageToHp.ToString();
                labelEffect.outlineColor = damageColor;
                labelEffect.underlayColor = damageColor;
            }
            else
            {
                label.text = "방어됨";
                labelEffect.outlineColor = blockColor;
                labelEffect.underlayColor = blockColor;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            AnimateAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid AnimateAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            label.alpha = 1f;

            var startPos = _rect.position;
            var endPos = startPos + Vector3.up * risePixels;

            var riseTask = LMotion.Create(startPos, endPos, riseDuration)
                .WithEase(Ease.OutCubic)
                .Bind(p => _rect.position = p)
                .ToUniTask(cancellationToken: ct);

            var fadeTask = LMotion.Create(1f, 0f, fadeDuration)
                .WithDelay(fadeDelay)
                .Bind(a => label.alpha = a)
                .ToUniTask(cancellationToken: ct);

            await UniTask.WhenAll(riseTask, fadeTask);
            _pool?.Push(this);
        }

        public void ResetItem()
        {
            _cts?.Cancel();
            label.alpha = 1f;
            gameObject.SetActive(false);
        }
    }
}
