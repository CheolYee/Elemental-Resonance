using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class AgentHealthBarView : MonoBehaviour, IModule
    {
        [SerializeField] protected TMP_Text hpText;
        [SerializeField] protected Slider redSlider;
        [SerializeField] protected Slider graySlider;
        [SerializeField] private Slider blockSlider;
        [SerializeField] private GameObject shieldGroup;
        [SerializeField] private TMP_Text blockText;
        [SerializeField] private TMPEffect hpTextEffect;

        [Header("Settings")]
        [SerializeField] private float redDuration = 0.15f;
        [SerializeField] private float grayDelay = 0.5f;
        [SerializeField] private float grayDuration = 0.3f;
        [SerializeField] private Color blockOutlineColor = new Color(0.27f, 0.53f, 1f);

        protected HealthModule Health;
        private MotionHandle _redHandle;
        private MotionHandle _grayHandle;
        private Color _defaultOutlineColor;

        public virtual void Initialize(ModuleOwner owner)
        {
            Health = owner.GetModule<HealthModule>();
            Health.OnHpChanged += OnHpChanged;
            Health.OnBlockChanged += OnBlockChanged;
        }

        protected virtual void Start()
        {
            redSlider.maxValue = Health.MaxHp;
            redSlider.value = Health.CurrentHp;
            graySlider.maxValue = Health.MaxHp;
            graySlider.value = Health.CurrentHp;
            if (blockSlider != null)
            {
                blockSlider.maxValue = Health.MaxHp;
                blockSlider.value = Health.CurrentHp;
            }
            if (hpTextEffect != null) _defaultOutlineColor = hpTextEffect.outlineColor;
            hpText.text = $"{Health.CurrentHp}/{Health.MaxHp}";
            RefreshBlock(Health.Block);
        }

        private void OnHpChanged(int currentHp, int maxHp)
        {
            hpText.text = $"{currentHp}/{maxHp}";

            if (_redHandle.IsActive()) _redHandle.Cancel();
            _redHandle = LMotion.Create(redSlider.value, (float)currentHp, redDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v =>
                {
                    redSlider.value = v;
                    if (blockSlider != null) blockSlider.value = v;
                });

            if (_grayHandle.IsActive()) _grayHandle.Cancel();
            _grayHandle = LMotion.Create(graySlider.value, (float)currentHp, grayDuration)
                .WithDelay(grayDelay)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v => graySlider.value = v);
        }

        private void OnBlockChanged(int currentBlock)
        {
            RefreshBlock(currentBlock);
        }

        private void RefreshBlock(int currentBlock)
        {
            bool hasBlock = currentBlock > 0;
            if (blockSlider != null)
                blockSlider.gameObject.SetActive(hasBlock);
            if (shieldGroup != null) shieldGroup.SetActive(hasBlock);
            if (blockText != null) blockText.text = currentBlock.ToString();
            if (hpTextEffect != null)
            {
                hpTextEffect.underlayColor = hasBlock ? blockOutlineColor : _defaultOutlineColor;
                hpTextEffect.SetupEffect();
            }
        }

        protected virtual void OnDestroy()
        {
            if (Health == null) return;
            Health.OnHpChanged -= OnHpChanged;
            Health.OnBlockChanged -= OnBlockChanged;
        }
    }
}
